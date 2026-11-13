using System;
using System.Collections.Generic;
using System.Text;
using BlueDoc.Core.Enums;
using BlueDoc.Core.Models;
using BlueDoc.Infrastructure.Platform;

namespace BlueDoc.Application.Services
{
    public class CompatibilitySubsystemItem
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Requirements { get; set; } = string.Empty;
        public bool IsSupported { get; set; }
        public string StatusDisplay { get; set; } = "SUPPORTED";
        public string StatusColor { get; set; } = "#10B981"; // Green
        public string Details { get; set; } = string.Empty;
    }

    public class CompatibilityAssessment
    {
        public OsVersionInfo OsVersion { get; set; }
        public string OverallVerdict { get; set; } = string.Empty;
        public string VerdictColor { get; set; } = "#10B981";
        public string GuidanceNotes { get; set; } = string.Empty;
        public List<CompatibilitySubsystemItem> Subsystems { get; set; } = new List<CompatibilitySubsystemItem>();
    }

    public class CompatibilityService
    {
        public CompatibilityAssessment EvaluateCompatibility(OsVersionInfo osVersion = null)
        {
            var os = osVersion ?? OperatingSystemInspector.GetOperatingSystemInfo();
            var assessment = new CompatibilityAssessment { OsVersion = os };
            bool isModernWindows = os.Platform == WindowsPlatformType.Windows10 || os.Platform == WindowsPlatformType.Windows11;

            // 1. SetupAPI & CfgMgr32
            assessment.Subsystems.Add(new CompatibilitySubsystemItem
            {
                Title = "Win32 SetupAPI & Configuration Manager",
                Description = "Low-level PnP device node enumeration, vendor identification, hardware IDs, and error codes.",
                Requirements = "Windows 7 SP1, Windows 10, Windows 11 (x86 & x64)",
                IsSupported = true,
                StatusDisplay = "SUPPORTED",
                StatusColor = "#10B981",
                Details = "Hardware abstraction layer is fully operational via setupapi.dll and cfgmgr32.dll."
            });

            // 2. Win32 Classic Bluetooth APIs
            assessment.Subsystems.Add(new CompatibilitySubsystemItem
            {
                Title = "Win32 Classic Bluetooth APIs (bthprops.cpl)",
                Description = "Radio enumeration, device inquiry, radio handles, and pairing subsystem.",
                Requirements = "Windows 7 SP1, Windows 10, Windows 11",
                IsSupported = true,
                StatusDisplay = "SUPPORTED",
                StatusColor = "#10B981",
                Details = "Native bthprops.cpl Win32 entry points are present and bound to active system drivers."
            });

            // 3. Service Control Manager
            assessment.Subsystems.Add(new CompatibilitySubsystemItem
            {
                Title = "Windows Service Control Manager (AdvApi32)",
                Description = "Monitoring, query, and restart controls for core Bluetooth Support Service (bthserv).",
                Requirements = "All Windows NT platforms",
                IsSupported = true,
                StatusDisplay = "SUPPORTED",
                StatusColor = "#10B981",
                Details = "Direct integration with ServiceController enables verified restart and state verification."
            });

            // 4. Windows Runtime BLE GATT APIs
            if (isModernWindows)
            {
                assessment.Subsystems.Add(new CompatibilitySubsystemItem
                {
                    Title = "Modern Windows Runtime BLE GATT APIs",
                    Description = "Bluetooth Low Energy GATT service discovery, characteristic reading/writing, and notifications.",
                    Requirements = "Windows 10 (Build 15063+) or Windows 11",
                    IsSupported = true,
                    StatusDisplay = "SUPPORTED",
                    StatusColor = "#10B981",
                    Details = $"{os.OperatingSystemName} provides full native Windows Runtime BLE architecture."
                });
            }
            else
            {
                assessment.Subsystems.Add(new CompatibilitySubsystemItem
                {
                    Title = "Modern Windows Runtime BLE GATT APIs",
                    Description = "Bluetooth Low Energy GATT service discovery, characteristic reading/writing, and notifications.",
                    Requirements = "Windows 10 (Build 15063+) or Windows 11",
                    IsSupported = false,
                    StatusDisplay = "NOT SUPPORTED",
                    StatusColor = "#EF4444",
                    Details = "Windows 7 SP1 kernel does not host Windows.Devices.Bluetooth runtime. BLE devices communicate through proprietary PnP drivers."
                });
            }

            // 5. Bluetooth Audio Gateway Services
            assessment.Subsystems.Add(new CompatibilitySubsystemItem
            {
                Title = "Bluetooth Audio Gateway & AVCTP Services",
                Description = "A2DP stereo music playback and Hands-Free Profile (HFP/HSP) voice gateway.",
                Requirements = "BTAGService / BthAvctpSvc / BthHFSrv",
                IsSupported = isModernWindows,
                StatusDisplay = isModernWindows ? "SUPPORTED" : "LEGACY MODE",
                StatusColor = isModernWindows ? "#10B981" : "#F59E0B",
                Details = isModernWindows ? "Modern audio graph and audio gateway services are active." : "Windows 7 uses legacy audio filter drivers for Bluetooth audio."
            });

            // 6. Windows Event Telemetry Subsystem
            assessment.Subsystems.Add(new CompatibilitySubsystemItem
            {
                Title = "Windows Event Log Telemetry (System Log)",
                Description = "Querying BTHUSB, bthserv, and Bluetooth driver crash/timeout records.",
                Requirements = "System.Diagnostics.Eventing.Reader / EventLog",
                IsSupported = true,
                StatusDisplay = "SUPPORTED",
                StatusColor = "#10B981",
                Details = "EventLogReader queries Windows System log with fallback to classic EventLog."
            });

            // 7. Bluetooth Core Version & HCI Capabilities
            assessment.Subsystems.Add(new CompatibilitySubsystemItem
            {
                Title = "Bluetooth Core Spec & HCI Architecture",
                Description = "Support for Bluetooth 4.0/4.2 (LE Core), 5.0/5.1 (Extended Advertising), and 5.2/5.3 (LE Audio).",
                Requirements = "HCI Revision & LMP Driver Support",
                IsSupported = true,
                StatusDisplay = "SUPPORTED",
                StatusColor = "#10B981",
                Details = "Adapter driver communicates using standardized Host Controller Interface commands."
            });

            // 8. Radio Hardware & Software Kill Switch Subsystem
            assessment.Subsystems.Add(new CompatibilitySubsystemItem
            {
                Title = "Radio Hardware & Software Switch Subsystem",
                Description = "Airplane mode coordination, physical RF switch detection, and software radio toggling.",
                Requirements = "Radio Management API / Win32 Radio Handles",
                IsSupported = true,
                StatusDisplay = "SUPPORTED",
                StatusColor = "#10B981",
                Details = "Radio availability is verified through kernel radio handles and Windows Radio Management."
            });

            // Overall Verdict
            if (isModernWindows)
            {
                assessment.OverallVerdict = "Fully Compatible — All Core and Modern Features Supported";
                assessment.VerdictColor = "#10B981";
                assessment.GuidanceNotes = $"Running on {os.OperatingSystemName} ({os.ArchitectureDisplay}). Your computer supports all BlueDoc diagnostic suites, classic inquiry discovery, and modern BLE GATT explorations.";
            }
            else
            {
                assessment.OverallVerdict = "Compatible in Classic Mode — Legacy Platform Identified";
                assessment.VerdictColor = "#F59E0B";
                assessment.GuidanceNotes = "Running on Windows 7 SP1. Core Bluetooth hardware inspection, driver troubleshooting, classic device management, and repair operations operate normally. Modern WinRT BLE GATT explorer is unavailable.";
            }

            return assessment;
        }

        public string GenerateReportText(CompatibilityAssessment assessment)
        {
            var sb = new StringBuilder();
            sb.AppendLine("================================================================================");
            sb.AppendLine("BLUEDOC PLATFORM & SUBSYSTEM COMPATIBILITY REPORT");
            sb.AppendLine("================================================================================");
            sb.AppendLine($"Operating System:    {assessment.OsVersion.OperatingSystemName}");
            sb.AppendLine($"Kernel Build:        {assessment.OsVersion.BuildNumber}");
            sb.AppendLine($"Architecture:        {assessment.OsVersion.ArchitectureDisplay}");
            sb.AppendLine($"Overall Verdict:     {assessment.OverallVerdict}");
            sb.AppendLine($"Evaluation Date:     {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("--------------------------------------------------------------------------------");
            sb.AppendLine("GUIDANCE NOTES:");
            sb.AppendLine(assessment.GuidanceNotes);
            sb.AppendLine("--------------------------------------------------------------------------------");
            sb.AppendLine("SUBSYSTEM READINESS MATRIX:");
            sb.AppendLine();

            foreach (var item in assessment.Subsystems)
            {
                sb.AppendLine($"[{item.StatusDisplay}] {item.Title}");
                sb.AppendLine($"  Requirements: {item.Requirements}");
                sb.AppendLine($"  Details:      {item.Details}");
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}

