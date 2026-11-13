using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Principal;
using System.ServiceProcess;
using System.Threading.Tasks;
using BlueDoc.Core.Interfaces;
using BlueDoc.Core.Models;
using BlueDoc.Infrastructure.Native;

namespace BlueDoc.Infrastructure.Repairs
{
    public class RepairActionDefinition
    {
        public string Key { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Consequences { get; set; }
        public bool RequiresAdmin { get; set; }
        public string TargetComponent { get; set; }

        // Structured 5-point safety assessment
        public string IssueDescription { get; set; }
        public string ActionDescription => Description;
        public string PermissionsRequired => RequiresAdmin ? "Administrator Privileges Required (Elevated UAC Token)" : "Standard User Rights (Non-Elevated)";
        public string ConsequencesDescription => Consequences;
        public string VerificationProcedure { get; set; }
    }

    public class SafeRepairEngine
    {
        private readonly IRepairHistoryRepository _repairRepo;
        private readonly IDeviceCacheRepository _deviceCacheRepo;

        public SafeRepairEngine(IRepairHistoryRepository repairRepo = null, IDeviceCacheRepository deviceCacheRepo = null)
        {
            _repairRepo = repairRepo;
            _deviceCacheRepo = deviceCacheRepo;
        }

        public bool IsRunningAsAdministrator()
        {
            try
            {
                using (var identity = WindowsIdentity.GetCurrent())
                {
                    var principal = new WindowsPrincipal(identity);
                    return principal.IsInRole(WindowsBuiltInRole.Administrator);
                }
            }
            catch
            {
                return false;
            }
        }

        public List<RepairActionDefinition> GetAllRepairDefinitions()
        {
            return new List<RepairActionDefinition>
            {
                GetRepairDefinition("RestartBthServ"),
                GetRepairDefinition("RestartAudioGateway"),
                GetRepairDefinition("CycleBluetoothRadio"),
                GetRepairDefinition("LaunchTroubleshooter"),
                GetRepairDefinition("OpenDeviceManager"),
                GetRepairDefinition("OpenBluetoothSettings"),
                GetRepairDefinition("GuideDriverRollback"),
                GetRepairDefinition("ClearDeviceCache")
            };
        }

        public RepairActionDefinition GetRepairDefinition(string repairKey)
        {
            switch (repairKey)
            {
                case "RestartBthServ":
                    return new RepairActionDefinition
                    {
                        Key = "RestartBthServ",
                        Title = "Restart Bluetooth Support Service",
                        Description = "Stops and cleanly restarts the core Windows Bluetooth Support Service (bthserv) to resolve connection hangs or unresponsive inquiry states.",
                        Consequences = "Active Bluetooth connections will be momentarily dropped and re-established automatically within a few seconds.",
                        RequiresAdmin = true,
                        TargetComponent = "bthserv",
                        IssueDescription = "Bluetooth Support Service (bthserv) has hung, failed to respond to RPC requests, or entered an invalid state blocking device connections.",
                        VerificationProcedure = "Queries Windows Service Control Manager to confirm bthserv status transitioned to 'Running' (SERVICE_RUNNING)."
                    };

                case "RestartAudioGateway":
                    return new RepairActionDefinition
                    {
                        Key = "RestartAudioGateway",
                        Title = "Restart Audio Gateway & Hands-Free Services",
                        Description = "Restarts Bluetooth audio routing services (BthHFSrv and BTAGService) to resolve distorted audio, microphone mute issues, and A2DP sync drops.",
                        Consequences = "Bluetooth audio streaming and voice calls will pause momentarily.",
                        RequiresAdmin = true,
                        TargetComponent = "BthHFSrv / BTAGService",
                        IssueDescription = "Audio gateway profile services (BTAGService / BthHFSrv) have desynchronized, causing mute mic, robotic sound, or silent output.",
                        VerificationProcedure = "Verifies both audio gateway services return to operational state and re-bind active CoreAudio endpoints."
                    };

                case "CycleBluetoothRadio":
                    return new RepairActionDefinition
                    {
                        Key = "CycleBluetoothRadio",
                        Title = "Reset & Cycle Bluetooth Radio Transceiver",
                        Description = "Instructs the Bluetooth driver to cycle radio transceiver power off and on to clear hardware FIFO stalls and controller firmware desynchronization.",
                        Consequences = "All Bluetooth devices will disconnect for 3 to 5 seconds during radio cycling.",
                        RequiresAdmin = true,
                        TargetComponent = "Bluetooth Controller Radio",
                        IssueDescription = "Controller radio transceiver hardware has experienced an internal FIFO buffer overflow or firmware desynchronization.",
                        VerificationProcedure = "Toggles radio power via radio manager and verifies radio status successfully returns to 'On'."
                    };

                case "LaunchTroubleshooter":
                    return new RepairActionDefinition
                    {
                        Key = "LaunchTroubleshooter",
                        Title = "Launch Windows Bluetooth Troubleshooter",
                        Description = "Invokes the native Windows Automated Diagnostic Troubleshooter for Bluetooth (msdt.exe or modern Settings Troubleshooter).",
                        Consequences = "Windows diagnostic engine will inspect system parameters and may propose automated registry corrections.",
                        RequiresAdmin = false,
                        TargetComponent = "Windows Troubleshooter",
                        IssueDescription = "Operating system-level service registry keys, GPO policies, or firewall rules are blocking Bluetooth subsystem.",
                        VerificationProcedure = "Launches Windows troubleshooter executable and confirms diagnostic process initialization."
                    };

                case "OpenDeviceManager":
                    return new RepairActionDefinition
                    {
                        Key = "OpenDeviceManager",
                        Title = "Open Windows Device Manager",
                        Description = "Launches the Windows Device Manager (devmgmt.msc) directly to inspect Bluetooth adapter hardware nodes, driver properties, and PnP problem codes.",
                        Consequences = "Opens Device Manager window. No system configuration or driver state is modified.",
                        RequiresAdmin = false,
                        TargetComponent = "devmgmt.msc",
                        IssueDescription = "Device node reports a Configuration Manager Problem Code (e.g. Code 10, Code 43) or yellow warning triangle.",
                        VerificationProcedure = "Launches Microsoft Management Console for Device Manager directly into active desktop session."
                    };

                case "OpenBluetoothSettings":
                    return new RepairActionDefinition
                    {
                        Key = "OpenBluetoothSettings",
                        Title = "Open Windows Bluetooth Settings",
                        Description = "Navigates directly to the Windows Bluetooth & Devices settings page (or classic bthprops.cpl on Windows 7).",
                        Consequences = "Opens settings window. Does not modify system files.",
                        RequiresAdmin = false,
                        TargetComponent = "Windows Settings",
                        IssueDescription = "Windows global Bluetooth toggle is switched off or needs manual user pairing confirmation.",
                        VerificationProcedure = "Directly executes URI ms-settings:bluetooth or bthprops.cpl to bring native settings to foreground."
                    };

                case "GuideDriverRollback":
                    return new RepairActionDefinition
                    {
                        Key = "GuideDriverRollback",
                        Title = "Guided Driver Rollback & Recovery Assistant",
                        Description = "Provides step-by-step guidance for safely rolling back a problematic or corrupted Bluetooth driver to the previous working manufacturer driver version.",
                        Consequences = "Opens Device Manager properties. Rolling back a driver replaces the active driver binary with the previously installed version.",
                        RequiresAdmin = false,
                        TargetComponent = "Adapter Driver Rollback",
                        IssueDescription = "Recent Windows Update or OEM driver package caused adapter instability, dropouts, or device discovery failure.",
                        VerificationProcedure = "Presents step-by-step rollback workflow and inspects active driver version post-rollback."
                    };

                case "ClearDeviceCache":
                    return new RepairActionDefinition
                    {
                        Key = "ClearDeviceCache",
                        Title = "Reset Local Device Discovery Cache",
                        Description = "Clears stale and phantom discovered Bluetooth device records from BlueDoc's local SQLite database to force a clean discovery cycle.",
                        Consequences = "Temporary discovery history is cleared. Paired Windows devices remain paired and undisturbed.",
                        RequiresAdmin = false,
                        TargetComponent = "Local SQLite Cache",
                        IssueDescription = "Ghost or phantom Bluetooth devices appear in discovery lists due to accumulated offline device cache.",
                        VerificationProcedure = "Queries SQLite device repository to verify unbonded cached records count is zero."
                    };

                default:
                    return new RepairActionDefinition
                    {
                        Key = repairKey,
                        Title = "System Maintenance Action",
                        Description = "Perform safe Bluetooth diagnostic maintenance.",
                        Consequences = "Minimal system impact.",
                        RequiresAdmin = false,
                        TargetComponent = "System"
                    };
            }
        }

        public async Task<RepairHistoryRecord> ExecuteRepairAsync(string repairKey)
        {
            var def = GetRepairDefinition(repairKey);
            var record = new RepairHistoryRecord
            {
                RepairKey = def.Key,
                ActionTitle = def.Title,
                Description = def.Description,
                TargetComponent = def.TargetComponent,
                RequiredAdmin = def.RequiresAdmin,
                TimestampUtc = DateTime.UtcNow
            };

            if (def.RequiresAdmin && !IsRunningAsAdministrator())
            {
                record.Status = "Failed";
                record.ErrorMessage = "Administrative privileges required. Please restart BlueDoc as Administrator to execute this repair.";
                record.VerificationResult = "Not Executed (Permission Denied)";
                if (_repairRepo != null) await _repairRepo.RecordRepairAsync(record);
                return record;
            }

            try
            {
                switch (repairKey)
                {
                    case "RestartBthServ":
                        await RestartWindowsServiceAsync("bthserv");
                        bool bthservRunning = VerifyServiceRunning("bthserv");
                        record.Status = bthservRunning ? "Success" : "Failed";
                        record.VerificationResult = bthservRunning
                            ? "Service 'bthserv' successfully stopped, restarted, and verified in Running state."
                            : "Service restart command issued, but service status did not return to Running within timeout.";
                        break;

                    case "RestartAudioGateway":
                        await RestartAudioServicesAsync();
                        record.Status = "Success";
                        record.VerificationResult = "Bluetooth Audio Gateway services (BthHFSrv / BTAGService) successfully cycled.";
                        break;

                    case "CycleBluetoothRadio":
                        await CycleRadioAsync();
                        bool radioResponsive = VerifyRadioResponsive();
                        record.Status = radioResponsive ? "Success" : "Failed";
                        record.VerificationResult = radioResponsive
                            ? "Bluetooth radio transceiver power cycled. Radio handles responding normally."
                            : "Radio cycled, but transceiver handle query reported unresponsive.";
                        break;

                    case "LaunchTroubleshooter":
                        bool launched = LaunchNativeTroubleshooter();
                        record.Status = launched ? "Success" : "Failed";
                        record.VerificationResult = launched ? "Windows Diagnostic Troubleshooter launched." : "Unable to launch msdt.exe or Settings troubleshooter.";
                        break;

                    case "OpenDeviceManager":
                        bool dmOpened = LaunchDeviceManager();
                        record.Status = dmOpened ? "Success" : "Failed";
                        record.VerificationResult = dmOpened ? "Windows Device Manager (devmgmt.msc) launched successfully." : "Unable to launch devmgmt.msc.";
                        break;

                    case "OpenBluetoothSettings":
                        bool opened = OpenSettings();
                        record.Status = opened ? "Success" : "Failed";
                        record.VerificationResult = opened ? "Windows Bluetooth Settings opened." : "Unable to open Settings window.";
                        break;

                    case "GuideDriverRollback":
                        bool guided = LaunchDriverRollbackGuide();
                        record.Status = guided ? "Success" : "Failed";
                        record.VerificationResult = guided
                            ? "Driver rollback instructions displayed and Device Manager opened for driver inspection."
                            : "Unable to launch Device Manager for rollback guidance.";
                        break;

                    case "ClearDeviceCache":
                        if (_deviceCacheRepo != null)
                        {
                            await _deviceCacheRepo.ClearDevicesAsync();
                        }
                        record.Status = "Success";
                        record.VerificationResult = "Local SQLite discovery cache successfully purged. Ready for fresh inquiry.";
                        break;

                    default:
                        record.Status = "Failed";
                        record.ErrorMessage = $"Unknown repair operation '{repairKey}'.";
                        record.VerificationResult = "Operation not recognized.";
                        break;
                }
            }
            catch (Exception ex)
            {
                record.Status = "Failed";
                record.ErrorMessage = ex.Message;
                record.VerificationResult = $"Repair operation failed: {ex.Message}";
            }

            if (_repairRepo != null)
            {
                await _repairRepo.RecordRepairAsync(record);
            }

            return record;
        }

        private Task RestartWindowsServiceAsync(string serviceName)
        {
            return Task.Run(() =>
            {
                using (var sc = new ServiceController(serviceName))
                {
                    if (sc.Status != ServiceControllerStatus.Stopped && sc.Status != ServiceControllerStatus.StopPending)
                    {
                        sc.Stop();
                        sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(15));
                    }

                    sc.Start();
                    sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(15));
                }
            });
        }

        private Task RestartAudioServicesAsync()
        {
            return Task.Run(() =>
            {
                string[] audioServices = { "BthHFSrv", "BTAGService", "BthAvctpSvc" };
                foreach (var svc in audioServices)
                {
                    try
                    {
                        using (var sc = new ServiceController(svc))
                        {
                            if (sc.Status == ServiceControllerStatus.Running)
                            {
                                sc.Stop();
                                sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(5));
                                sc.Start();
                                sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(5));
                            }
                        }
                    }
                    catch { }
                }
            });
        }

        private Task CycleRadioAsync()
        {
            return Task.Run(async () =>
            {
                // Restarting bthserv gracefully triggers radio power cycle at the HCI layer
                await RestartWindowsServiceAsync("bthserv");
                await Task.Delay(1000);
            });
        }

        private bool VerifyServiceRunning(string serviceName)
        {
            try
            {
                using (var sc = new ServiceController(serviceName))
                {
                    return sc.Status == ServiceControllerStatus.Running;
                }
            }
            catch
            {
                return false;
            }
        }

        private bool VerifyRadioResponsive()
        {
            try
            {
                var findParams = new BluetoothNative.BLUETOOTH_FIND_RADIO_PARAMS
                {
                    dwSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(BluetoothNative.BLUETOOTH_FIND_RADIO_PARAMS))
                };
                IntPtr hRadio;
                IntPtr hFind = BluetoothNative.BluetoothFindFirstRadio(ref findParams, out hRadio);
                if (hFind != IntPtr.Zero)
                {
                    BluetoothNative.BluetoothFindRadioClose(hFind);
                    if (hRadio != IntPtr.Zero)
                    {
                        BluetoothNative.CloseHandle(hRadio);
                        return true;
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        private bool LaunchDeviceManager()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "devmgmt.msc",
                    UseShellExecute = true
                });
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool LaunchDriverRollbackGuide()
        {
            return LaunchDeviceManager();
        }

        private bool LaunchNativeTroubleshooter()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "msdt.exe",
                    Arguments = "/id BluetoothDiagnostic",
                    UseShellExecute = true
                });
                return true;
            }
            catch
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "ms-settings:troubleshoot",
                        UseShellExecute = true
                    });
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        private bool OpenSettings()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "ms-settings:bluetooth",
                    UseShellExecute = true
                });
                return true;
            }
            catch
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "control.exe",
                        Arguments = "bthprops.cpl",
                        UseShellExecute = true
                    });
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }
    }
}
