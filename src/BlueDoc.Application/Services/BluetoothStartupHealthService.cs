using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlueDoc.Core.Enums;
using BlueDoc.Core.Models;
using BlueDoc.Infrastructure.Platform;
using BlueDoc.Infrastructure.Services;

namespace BlueDoc.Application.Services
{
    public class BluetoothStartupHealthService
    {
        private readonly AdapterService _adapterService;
        private readonly WindowsServiceInspector _serviceInspector;

        public BluetoothStartupHealthService(AdapterService adapterService = null)
        {
            _adapterService = adapterService ?? new AdapterService();
            _serviceInspector = new WindowsServiceInspector();
        }

        public async Task<StartupHealthAssessment> InspectSystemHealthAsync()
        {
            var assessment = new StartupHealthAssessment();
            var os = OperatingSystemInspector.GetOperatingSystemInfo();
            assessment.OsName = $"{os.OperatingSystemName} ({os.ArchitectureDisplay})";

            try
            {
                // 1. Adapter Availability & Hardware Status
                var adapter = await _adapterService.GetPrimaryAdapterAsync();
                if (adapter == null)
                {
                    assessment.IsHealthy = false;
                    assessment.AdapterStatus = AdapterStatus.NotPresent;
                    assessment.PrimaryAdapterName = "No Adapter Detected";
                    assessment.ActionableIssues.Add(new StartupHealthIssue
                    {
                        Title = "No Bluetooth Adapter Detected",
                        Description = "Windows does not detect an operational Bluetooth transceiver. If using a USB dongle, unplug and replug it into another USB port.",
                        Severity = "Critical",
                        SuggestedRepairKey = "OpenDeviceManager",
                        ActionButtonText = "Check Device Manager"
                    });
                }
                else
                {
                    assessment.PrimaryAdapterName = adapter.FriendlyName;
                    assessment.AdapterStatus = adapter.Status;

                    if (adapter.ProblemCode != 0)
                    {
                        assessment.IsHealthy = false;
                        assessment.DriverOperational = false;
                        string desc = $"Adapter reported problem code CM_PROB ({(int)adapter.ProblemCode}).";
                        if ((int)adapter.ProblemCode == 22) // Disabled
                        {
                            desc = "Bluetooth adapter is disabled in Windows Configuration Manager.";
                            assessment.ActionableIssues.Add(new StartupHealthIssue
                            {
                                Title = "Bluetooth Adapter Disabled",
                                Description = "The primary Bluetooth transceiver is disabled in Windows.",
                                Severity = "Critical",
                                SuggestedRepairKey = "OpenDeviceManager",
                                ActionButtonText = "Enable in Device Manager"
                            });
                        }
                        else
                        {
                            assessment.ActionableIssues.Add(new StartupHealthIssue
                            {
                                Title = $"Driver Hardware Error (Code {(int)adapter.ProblemCode})",
                                Description = desc,
                                Severity = "Critical",
                                SuggestedRepairKey = "OpenDeviceManager",
                                ActionButtonText = "Inspect Driver"
                            });
                        }
                    }

                    // 1b. Check for unprovisioned silicon (cold-boot ROM mode with generic Microsoft driver)
                    if (BlueDoc.Infrastructure.Drivers.WinUsbFirmwarePatcher.IsSiliconUnpatched(adapter))
                    {
                        assessment.IsHealthy = false;
                        assessment.DriverOperational = false;
                        assessment.ActionableIssues.Add(new StartupHealthIssue
                        {
                            Title = "Unprovisioned Radio Firmware Detected",
                            Description = $"Chipset '{adapter.FriendlyName}' is running in unprovisioned cold-boot mode under generic Microsoft 2006 driver. Radio RF transceiver is asleep. Certified WHQL driver & RAM-patch firmware are bundled and ready to load.",
                            Severity = "Critical",
                            SuggestedRepairKey = "ProvisionSiliconFirmware",
                            ActionButtonText = "Provision Firmware Now"
                        });
                    }

                    // 2. Radio Transceiver Availability
                    assessment.RadioAvailable = adapter.IsEnabled && adapter.Status == AdapterStatus.Operational;
                    if (!assessment.RadioAvailable)
                    {
                        assessment.IsHealthy = false;
                        assessment.ActionableIssues.Add(new StartupHealthIssue
                        {
                            Title = "Bluetooth Radio Turned Off or Disabled",
                            Description = "The Bluetooth transceiver radio is turned off, disabled, or in Airplane Mode.",
                            Severity = "Warning",
                            SuggestedRepairKey = "CycleBluetoothRadio",
                            ActionButtonText = "Turn On Radio"
                        });
                    }
                }

                // 3. Core Bluetooth Services
                var bthserv = _serviceInspector.GetServiceInfo("bthserv");
                if (bthserv == null || !bthserv.IsRunning)
                {
                    assessment.IsHealthy = false;
                    assessment.ServicesRunning = false;
                    assessment.ActionableIssues.Add(new StartupHealthIssue
                    {
                        Title = "Bluetooth Support Service Stopped",
                        Description = "The core Windows Bluetooth Support Service (bthserv) is not running, preventing peripheral discovery and pairing.",
                        Severity = "Critical",
                        SuggestedRepairKey = "RestartBthServ",
                        ActionButtonText = "Start Service"
                    });
                }

                // Check Audio Gateway service if modern Windows
                if (os.Platform == WindowsPlatformType.Windows10 || os.Platform == WindowsPlatformType.Windows11)
                {
                    var btag = _serviceInspector.GetServiceInfo("BTAGService");
                    if (btag != null && !btag.IsRunning && btag.StartupType != "Disabled")
                    {
                        assessment.ActionableIssues.Add(new StartupHealthIssue
                        {
                            Title = "Audio Gateway Service Inactive",
                            Description = "Bluetooth Audio Gateway Service (BTAGService) is stopped. Audio streaming may experience delays.",
                            Severity = "Advisory",
                            SuggestedRepairKey = "RestartAudioGateway",
                            ActionButtonText = "Start Audio Service"
                        });
                    }
                }

                // 4. Supported Bluetooth APIs
                if (os.SupportsBleModernApis)
                {
                    assessment.ApiReadiness = "Classic Win32 + Windows Runtime BLE GATT Fully Ready";
                }
                else
                {
                    assessment.ApiReadiness = "Classic Win32 Mode Active (Win7 Legacy Stack)";
                }

                if (assessment.ActionableIssues.Count == 0)
                {
                    assessment.IsHealthy = true;
                    assessment.OverallMessage = $"Bluetooth system is healthy and ready for connections on {os.OperatingSystemName}.";
                }
                else
                {
                    assessment.OverallMessage = $"Bluetooth system requires attention ({assessment.ActionableIssues.Count} issue(s) identified).";
                }
            }
            catch (Exception ex)
            {
                assessment.OverallMessage = $"Startup health check encountered an error: {ex.Message}";
            }

            return assessment;
        }
    }
}
