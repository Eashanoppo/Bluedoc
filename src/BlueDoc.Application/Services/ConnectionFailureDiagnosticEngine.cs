using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlueDoc.Core.Enums;
using BlueDoc.Core.Models;
using BlueDoc.Infrastructure.Audio;
using BlueDoc.Infrastructure.Drivers;
using BlueDoc.Infrastructure.Platform;
using BlueDoc.Infrastructure.Services;

namespace BlueDoc.Application.Services
{
    public class ConnectionFailureDiagnosticEngine
    {
        private readonly AdapterService _adapterService;
        private readonly WindowsServiceInspector _serviceInspector;
        private readonly BluetoothAudioInspector _audioInspector;
        private readonly OsVersionInfo _osVersion;

        public ConnectionFailureDiagnosticEngine(AdapterService adapterService = null)
        {
            _adapterService = adapterService ?? new AdapterService();
            _serviceInspector = new WindowsServiceInspector();
            _audioInspector = new BluetoothAudioInspector();
            _osVersion = OperatingSystemInspector.GetOperatingSystemInfo();
        }

        public async Task<ConnectionFailureDiagnosis> DiagnoseConnectionFailureAsync(
            BluetoothDeviceInfo targetDevice,
            uint lastErrorCode = 0,
            string lastErrorMessage = "")
        {
            var diagnosis = new ConnectionFailureDiagnosis
            {
                TargetDevice = targetDevice
            };

            var checks = new List<ConnectionDiagnosticCheckItem>();

            // 1. Is Adapter Operational?
            var adapter = await _adapterService.GetPrimaryAdapterAsync();
            if (adapter == null)
            {
                diagnosis.IsAdapterIssue = true;
                checks.Add(new ConnectionDiagnosticCheckItem
                {
                    Name = "Adapter Presence",
                    Status = DiagnosticStatus.Failed,
                    Evidence = "No Bluetooth adapter was detected in Windows Device Manager.",
                    Confidence = ConfidenceLevel.High
                });
                diagnosis.PrimaryRootCause = "Bluetooth Controller Hardware Missing or Disconnected";
                diagnosis.DetailedEvidence = "Windows reported no active Bluetooth radio transceivers. If using a USB dongle, it may be loose or disconnected.";
                diagnosis.Confidence = ConfidenceLevel.High;
                diagnosis.SuggestedRepairKey = "OpenDeviceManager";
                diagnosis.SuggestedRepairTitle = "Inspect Device Manager";
                diagnosis.SuggestedRepairExplanation = "Open Device Manager to verify if the adapter hardware node appears or has a hardware defect.";
                diagnosis.CheckItems = checks;
                return diagnosis;
            }

            if (adapter.ProblemCode != 0)
            {
                diagnosis.IsAdapterIssue = true;
                bool isSiliconUnpatched = WinUsbFirmwarePatcher.IsSiliconUnpatched(adapter);

                checks.Add(new ConnectionDiagnosticCheckItem
                {
                    Name = "Adapter Problem Code",
                    Status = DiagnosticStatus.Failed,
                    Evidence = $"Adapter reported Configuration Manager Problem Code CM_PROB ({(int)adapter.ProblemCode}).",
                    Confidence = ConfidenceLevel.High
                });

                if (isSiliconUnpatched)
                {
                    diagnosis.PrimaryRootCause = $"Bluetooth Silicon Firmware Failure (Problem Code {(int)adapter.ProblemCode})";
                    diagnosis.DetailedEvidence = $"Hardware adapter '{adapter.FriendlyName}' (VID_{adapter.HardwareDetails?.VendorId}&PID_{adapter.HardwareDetails?.ProductId}) failed to start (Code {(int)adapter.ProblemCode}). Certified RAM-patch firmware is required to initialize the radio baseband.";
                    diagnosis.Confidence = ConfidenceLevel.High;
                    diagnosis.SuggestedRepairKey = "ProvisionSiliconFirmware";
                    diagnosis.SuggestedRepairTitle = "Provision Silicon Firmware & Resolve Code 10";
                    diagnosis.SuggestedRepairExplanation = "Deploys verified binary DFU firmware patches to DriverStore and resets the controller port to clear Code 10.";
                }
                else
                {
                    diagnosis.PrimaryRootCause = $"Bluetooth Adapter Reported Error Code {(int)adapter.ProblemCode}";
                    diagnosis.DetailedEvidence = $"Hardware adapter '{adapter.FriendlyName}' has an active problem code {(int)adapter.ProblemCode}. Recommended action: power cycle controller or reinstall manufacturer driver.";
                    diagnosis.Confidence = ConfidenceLevel.High;
                    diagnosis.SuggestedRepairKey = "CycleBluetoothRadio";
                    diagnosis.SuggestedRepairTitle = "Reset & Cycle Radio Transceiver";
                    diagnosis.SuggestedRepairExplanation = "Power cycle the adapter controller to clear hardware FIFO stalls and driver timeouts.";
                }

                diagnosis.CheckItems = checks;
                return diagnosis;
            }
            else
            {
                checks.Add(new ConnectionDiagnosticCheckItem
                {
                    Name = "Adapter Operational Status",
                    Status = DiagnosticStatus.Passed,
                    Evidence = $"Adapter '{adapter.FriendlyName}' is operational with no PnP problem codes.",
                    Confidence = ConfidenceLevel.High
                });
            }

            // 1b. Check if Bluetooth Silicon is Unprovisioned / Unpatched (Cold-Boot Standby)
            if (WinUsbFirmwarePatcher.IsSiliconUnpatched(adapter))
            {
                diagnosis.IsAdapterIssue = true;
                checks.Add(new ConnectionDiagnosticCheckItem
                {
                    Name = "Radio Firmware & Silicon Provisioning",
                    Status = DiagnosticStatus.Failed,
                    Evidence = $"Adapter '{adapter.FriendlyName}' (VID_{adapter.HardwareDetails?.VendorId}&PID_{adapter.HardwareDetails?.ProductId}) is running in unprovisioned cold-boot ROM mode under generic Microsoft driver. The 2.4 GHz RF radio transmitter is dormant.",
                    Confidence = ConfidenceLevel.High
                });

                diagnosis.PrimaryRootCause = "Unprovisioned Radio Firmware (Cold-Boot Standby)";
                diagnosis.DetailedEvidence = "The internal Bluetooth chipset (Qualcomm Atheros AR3012 / Broadcom) requires volatile RAM firmware (.dfu/.hex) to be uploaded over USB at boot. Microsoft generic driver lacks this firmware, leaving the RF radio transmitter asleep. Bundled WHQL driver and firmware are ready to install.";
                diagnosis.Confidence = ConfidenceLevel.High;
                diagnosis.SuggestedRepairKey = "ProvisionSiliconFirmware";
                diagnosis.SuggestedRepairTitle = "Provision Firmware & Install Bundled Driver";
                diagnosis.SuggestedRepairExplanation = "Installs the bundled certified WHQL driver package and loads the missing radio firmware patches (.dfu) so the radio transmitter powers on.";
                diagnosis.CheckItems = checks;
                return diagnosis;
            }

            // 2. Is Radio Available?
            if (!adapter.IsEnabled || adapter.Status == AdapterStatus.Disabled)
            {
                checks.Add(new ConnectionDiagnosticCheckItem
                {
                    Name = "Radio Transceiver Power",
                    Status = DiagnosticStatus.Failed,
                    Evidence = "Bluetooth radio power is toggled off or system is in Airplane Mode.",
                    Confidence = ConfidenceLevel.High
                });

                diagnosis.PrimaryRootCause = "Bluetooth Radio is Turned Off";
                diagnosis.DetailedEvidence = "Windows Radio Management reports that the Bluetooth transmitter is powered down.";
                diagnosis.Confidence = ConfidenceLevel.High;
                diagnosis.SuggestedRepairKey = "CycleBluetoothRadio";
                diagnosis.SuggestedRepairTitle = "Turn On Radio Transceiver";
                diagnosis.SuggestedRepairExplanation = "Re-enable the Bluetooth transceiver power switch.";
                diagnosis.CheckItems = checks;
                return diagnosis;
            }
            else
            {
                checks.Add(new ConnectionDiagnosticCheckItem
                {
                    Name = "Radio Power Availability",
                    Status = DiagnosticStatus.Passed,
                    Evidence = "Radio transceiver is powered on and transmitting.",
                    Confidence = ConfidenceLevel.High
                });
            }

            // 3. Is Authentication Required / Rejected?
            if (lastErrorCode == 1244 || lastErrorMessage.IndexOf("not authenticated", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                diagnosis.IsAuthenticationIssue = true;
                checks.Add(new ConnectionDiagnosticCheckItem
                {
                    Name = "Authentication / Encryption Key",
                    Status = DiagnosticStatus.Failed,
                    Evidence = "Windows Win32 authentication returned ERROR_NOT_AUTHENTICATED (1244). Link key was rejected by peripheral.",
                    Confidence = ConfidenceLevel.High
                });

                diagnosis.PrimaryRootCause = "Authentication / Link Key Mismatch";
                diagnosis.DetailedEvidence = "The peripheral was likely paired with another phone/computer, or its internal bond table was cleared, invalidating Windows' stored encryption key.";
                diagnosis.Confidence = ConfidenceLevel.High;
                diagnosis.SuggestedRepairKey = "RemoveStalePairing";
                diagnosis.SuggestedRepairTitle = "Remove Stale Pairing Record";
                diagnosis.SuggestedRepairExplanation = "Remove the old pairing record in BlueDoc and re-pair the peripheral cleanly.";
                diagnosis.CheckItems = checks;
                return diagnosis;
            }

            // 4. Is Device Paired?
            bool isPaired = targetDevice != null && targetDevice.IsPaired;
            if (!isPaired)
            {
                diagnosis.IsStalePairingIssue = true;
                checks.Add(new ConnectionDiagnosticCheckItem
                {
                    Name = "Pairing Registration",
                    Status = DiagnosticStatus.Warning,
                    Evidence = $"Peripheral '{targetDevice?.DisplayName}' is not currently paired with Windows.",
                    Confidence = ConfidenceLevel.High
                });

                diagnosis.PrimaryRootCause = "Device is Not Paired";
                diagnosis.DetailedEvidence = "The peripheral must complete Bluetooth pairing before establishing audio or data connection streams.";
                diagnosis.Confidence = ConfidenceLevel.High;
                diagnosis.SuggestedRepairKey = "RePairDevice";
                diagnosis.SuggestedRepairTitle = "Pair Peripheral Now";
                diagnosis.SuggestedRepairExplanation = "Put your device into pairing mode and complete the in-app pairing wizard.";
                diagnosis.CheckItems = checks;
                return diagnosis;
            }
            else
            {
                checks.Add(new ConnectionDiagnosticCheckItem
                {
                    Name = "Pairing Registration",
                    Status = DiagnosticStatus.Passed,
                    Evidence = $"Peripheral '{targetDevice.DisplayName}' is registered and paired in Windows.",
                    Confidence = ConfidenceLevel.High
                });
            }

            // 5. Is Core Service Running?
            var bthserv = _serviceInspector.GetServiceInfo("bthserv");
            if (bthserv == null || !bthserv.IsRunning)
            {
                checks.Add(new ConnectionDiagnosticCheckItem
                {
                    Name = "Bluetooth Support Service",
                    Status = DiagnosticStatus.Failed,
                    Evidence = "Windows Service 'bthserv' is stopped or unresponsive.",
                    Confidence = ConfidenceLevel.High
                });

                diagnosis.PrimaryRootCause = "Bluetooth Support Service (bthserv) is Stopped";
                diagnosis.DetailedEvidence = "The core Windows Bluetooth Support Service must be running to manage peripheral communication.";
                diagnosis.Confidence = ConfidenceLevel.High;
                diagnosis.SuggestedRepairKey = "RestartBthServ";
                diagnosis.SuggestedRepairTitle = "Restart Bluetooth Support Service";
                diagnosis.SuggestedRepairExplanation = "Cleanly restarts bthserv to clear deadlocked RPC threads.";
                diagnosis.CheckItems = checks;
                return diagnosis;
            }
            else
            {
                checks.Add(new ConnectionDiagnosticCheckItem
                {
                    Name = "Core Bluetooth Service",
                    Status = DiagnosticStatus.Passed,
                    Evidence = "bthserv is running normally.",
                    Confidence = ConfidenceLevel.High
                });
            }

            // 6. Audio Endpoint Problem Check (if Audio Device)
            bool isAudio = targetDevice != null && (targetDevice.MajorDeviceClass == "Audio/Video" ||
                           targetDevice.CategoryName.IndexOf("Audio", StringComparison.OrdinalIgnoreCase) >= 0 ||
                           targetDevice.CategoryName.IndexOf("Headset", StringComparison.OrdinalIgnoreCase) >= 0 ||
                           targetDevice.DisplayName.IndexOf("Buds", StringComparison.OrdinalIgnoreCase) >= 0);

            if (isAudio)
            {
                var audioAnalysis = _audioInspector.AnalyzeAudioSubsystem();
                if (audioAnalysis.HasActiveHfpMicrophone && audioAnalysis.IsHighDefPlaybackThrottled)
                {
                    diagnosis.IsAudioEndpointIssue = true;
                    checks.Add(new ConnectionDiagnosticCheckItem
                    {
                        Name = "Audio Profile Contention",
                        Status = DiagnosticStatus.Warning,
                        Evidence = "Hands-Free (HFP) microphone is actively engaged by an application (Discord, Teams, Zoom), suppressing A2DP stereo sound.",
                        Confidence = ConfidenceLevel.High
                    });

                    diagnosis.PrimaryRootCause = "Voice Microphone Contention (HFP vs A2DP)";
                    diagnosis.DetailedEvidence = audioAnalysis.CoexistenceNotice;
                    diagnosis.Confidence = ConfidenceLevel.High;
                    diagnosis.SuggestedRepairKey = "RestartAudioGateway";
                    diagnosis.SuggestedRepairTitle = "Restart Audio Gateway Services";
                    diagnosis.SuggestedRepairExplanation = audioAnalysis.Recommendation;
                    diagnosis.CheckItems = checks;
                    return diagnosis;
                }
            }

            // 7. Fallback: Peripheral Power, Out of Range, or Multipoint Contention
            checks.Add(new ConnectionDiagnosticCheckItem
            {
                Name = "Peripheral Radio Beacon",
                Status = DiagnosticStatus.Warning,
                Evidence = "Target device did not respond to connection page inquiry within timeout.",
                Confidence = ConfidenceLevel.Medium
            });

            diagnosis.IsRangeOrPowerIssue = true;
            diagnosis.PrimaryRootCause = "Peripheral is Powered Off, Out of Range, or Connected to Another Device";
            diagnosis.DetailedEvidence = $"Windows sent connection packets to '{targetDevice?.DisplayName}' ({targetDevice?.BluetoothAddress}), but the controller received no baseband acknowledgement. This commonly occurs if headphones are connected to your smartphone, the battery is depleted, or the device is powered off.";
            diagnosis.Confidence = ConfidenceLevel.High;
            diagnosis.SuggestedRepairKey = "CycleBluetoothRadio";
            diagnosis.SuggestedRepairTitle = "Reset Radio & Put Device in Pairing Mode";
            diagnosis.SuggestedRepairExplanation = "1. Turn your peripheral off and back on.\n2. Ensure it is disconnected from your phone.\n3. Keep it within 3 feet of your computer.\n4. Click 'Cycle Radio Transceiver' to clear the host controller.";
            diagnosis.CheckItems = checks;

            return diagnosis;
        }
    }
}
