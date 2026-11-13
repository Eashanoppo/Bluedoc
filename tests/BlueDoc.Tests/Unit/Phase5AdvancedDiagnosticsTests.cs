using System;
using System.Collections.Generic;
using System.Linq;
using BlueDoc.Application.Services;
using BlueDoc.Bluetooth.BLE;
using BlueDoc.Core.Enums;
using BlueDoc.Core.Models;

namespace BlueDoc.Tests.Unit
{
    public static class Phase5AdvancedDiagnosticsTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("[TEST SUITE] Phase 5 Advanced Diagnostics Tests (Signal, Event Correlation, BLE GATT, Compatibility)");

            TestSignalMetricsAndFormatting();
            TestGattProfileCatalog();
            TestEventLogCorrelationAndExport();
            TestCompatibilityMatrix();

            Console.WriteLine("All Phase 5 Advanced Diagnostics tests passed!\n");
        }

        private static void TestSignalMetricsAndFormatting()
        {
            // Test 1: Excellent Signal (>= -60 dBm)
            var dev1 = new BluetoothDeviceInfo { DeviceName = "Nearby Beacon", Rssi = -52 };
            Assert(dev1.SignalQualityBadge == "Excellent", "dev1.SignalQualityBadge == 'Excellent'");
            Assert(dev1.SignalBadgeColor == "#10B981", "dev1.SignalBadgeColor == '#10B981'");
            Assert(dev1.SignalBars == "▂▄▆█", "dev1.SignalBars == '▂▄▆█'");
            Assert(dev1.SignalStrengthDisplay == "-52 dBm", "dev1.SignalStrengthDisplay == '-52 dBm'");
            Assert(dev1.SignalPercentage > 75, "dev1.SignalPercentage > 75");

            // Test 2: Good Signal (-61 to -70 dBm)
            var dev2 = new BluetoothDeviceInfo { DeviceName = "Office Speaker", Rssi = -68 };
            Assert(dev2.SignalQualityBadge == "Good", "dev2.SignalQualityBadge == 'Good'");
            Assert(dev2.SignalBadgeColor == "#3B82F6", "dev2.SignalBadgeColor == '#3B82F6'");
            Assert(dev2.SignalBars == "▂▄▆_", "dev2.SignalBars == '▂▄▆_'");
            Assert(dev2.SignalStrengthDisplay == "-68 dBm", "dev2.SignalStrengthDisplay == '-68 dBm'");

            // Test 3: Fair Signal (-71 to -85 dBm)
            var dev3 = new BluetoothDeviceInfo { DeviceName = "Next Door TV", Rssi = -79 };
            Assert(dev3.SignalQualityBadge == "Fair", "dev3.SignalQualityBadge == 'Fair'");
            Assert(dev3.SignalBadgeColor == "#F59E0B", "dev3.SignalBadgeColor == '#F59E0B'");
            Assert(dev3.SignalBars == "▂▄__", "dev3.SignalBars == '▂▄__'");

            // Test 4: Poor Signal (< -85 dBm)
            var dev4 = new BluetoothDeviceInfo { DeviceName = "Faint Sensor", Rssi = -92 };
            Assert(dev4.SignalQualityBadge == "Poor", "dev4.SignalQualityBadge == 'Poor'");
            Assert(dev4.SignalBadgeColor == "#EF4444", "dev4.SignalBadgeColor == '#EF4444'");
            Assert(dev4.SignalBars == "▂___", "dev4.SignalBars == '▂___'");

            // Test 5: Section 15 Strict Policy: Never fabricate RSSI when not exposed by hardware
            var dev5 = new BluetoothDeviceInfo { DeviceName = "Legacy Peripheral", Rssi = null };
            Assert(dev5.SignalQualityBadge == "Not Exposed", "dev5.SignalQualityBadge == 'Not Exposed'");
            Assert(dev5.SignalBadgeColor == "#64748B", "dev5.SignalBadgeColor == '#64748B'");
            Assert(dev5.SignalBars == "----", "dev5.SignalBars == '----'");
            Assert(dev5.SignalStrengthDisplay == "Signal not exposed", "dev5.SignalStrengthDisplay == 'Signal not exposed'");
            Assert(dev5.SignalPercentage == 0, "dev5.SignalPercentage == 0");

            Console.WriteLine("  ✓ Signal telemetry formatting & Section 15 honest reporting verified.");
        }

        private static void TestGattProfileCatalog()
        {
            // Test 1: Service UUID resolution
            Assert(GattProfileCatalog.LookupServiceName("1800") == "Generic Access", "LookupServiceName 1800");
            Assert(GattProfileCatalog.LookupServiceName("0x180A") == "Device Information Service", "LookupServiceName 0x180A");
            Assert(GattProfileCatalog.LookupServiceName("180F") == "Battery Service", "LookupServiceName 180F");
            Assert(GattProfileCatalog.LookupServiceName("1812") == "Human Interface Device (HID)", "LookupServiceName 1812");

            // Test 2: Characteristic UUID resolution
            Assert(GattProfileCatalog.LookupCharacteristicName("2A00") == "Device Name", "LookupCharacteristicName 2A00");
            Assert(GattProfileCatalog.LookupCharacteristicName("2A19") == "Battery Level", "LookupCharacteristicName 2A19");
            Assert(GattProfileCatalog.LookupCharacteristicName("2A4D") == "Report", "LookupCharacteristicName 2A4D");

            // Test 3: Standard peripheral profile generation for Mouse
            var mouseProfile = GattProfileCatalog.GetStandardPeripheralProfile("BT Mouse", "Mouse");
            Assert(mouseProfile.Count >= 4, "Mouse profile should have at least 4 services");
            Assert(mouseProfile.Any(s => s.Name.Contains("HID")), "Mouse profile must contain HID service");

            // Test 4: Standard peripheral profile for Audio Headset
            var audioProfile = GattProfileCatalog.GetStandardPeripheralProfile("ANC Headphones", "Headphones");
            Assert(audioProfile.Any(s => s.Name.Contains("Battery Service")), "Audio profile must contain Battery service");
            Assert(!audioProfile.Any(s => s.Name.Contains("HID")), "Audio profile should not contain HID service");

            Console.WriteLine("  ✓ GATT Profile catalog, UUID resolution, and peripheral profiling verified.");
        }

        private static void TestEventLogCorrelationAndExport()
        {
            var service = new EventAnalysisService();

            // Scenario 1: Diagnostic failure correlates with BTHUSB timeout event
            var diagResults = new List<DiagnosticResult>
            {
                new DiagnosticResult
                {
                    RuleId = "BTH-003",
                    TestName = "Adapter Hardware Status",
                    Status = DiagnosticStatus.Failed,
                    Summary = "Device reported problem Code 43."
                }
            };

            var eventRecords = new List<BluetoothEventRecord>
            {
                new BluetoothEventRecord
                {
                    ProviderName = "BTHUSB",
                    EventId = 3,
                    LevelDisplayName = "Error",
                    HumanAnalysis = "A command sent to the adapter timed out.",
                    TimeCreated = DateTime.UtcNow.AddMinutes(-10)
                }
            };

            var correlation = service.CorrelateWithDiagnostics(diagResults, eventRecords);
            Assert(correlation.HasCorrelation == true, "correlation.HasCorrelation == true");
            Assert(correlation.CorrelatedFindings.Count > 0, "Correlated findings count > 0");
            Assert(correlation.CorrelatedFindings[0].Contains("BTHUSB driver timeout"), "Correlation text mentions BTHUSB timeout");

            // Scenario 2: CSV Audit Log generation
            string csv = service.GenerateCsvAuditLog(eventRecords);
            Assert(csv.Contains("Timestamp (UTC),Local Time,Provider,Event ID"), "CSV contains proper header");
            Assert(csv.Contains("BTHUSB,3,Error"), "CSV contains event record row");

            Console.WriteLine("  ✓ Event log correlation engine & audit CSV export verified.");
        }

        private static void TestCompatibilityMatrix()
        {
            var service = new CompatibilityService();
            var assessment = service.EvaluateCompatibility();
            Assert(assessment.Subsystems.Count == 8, $"Subsystems count must be 8 (Found: {assessment.Subsystems.Count})");

            var setupApi = assessment.Subsystems.FirstOrDefault(s => s.Title.Contains("SetupAPI"));
            Assert(setupApi != null && setupApi.IsSupported, "SetupAPI must be supported");

            var classic = assessment.Subsystems.FirstOrDefault(s => s.Title.Contains("Classic Bluetooth"));
            Assert(classic != null && classic.IsSupported, "Classic Bluetooth APIs must be supported");

            var scm = assessment.Subsystems.FirstOrDefault(s => s.Title.Contains("Service Control"));
            Assert(scm != null && scm.IsSupported, "Service Control Manager must be supported");

            Assert(!string.IsNullOrWhiteSpace(assessment.OverallVerdict), "Overall verdict must be populated");
            Assert(!string.IsNullOrWhiteSpace(assessment.GuidanceNotes), "Guidance notes must be populated");

            string reportText = service.GenerateReportText(assessment);
            Assert(reportText.Contains("BLUEDOC PLATFORM & SUBSYSTEM COMPATIBILITY REPORT"), "reportText header");
            Assert(reportText.Contains("SUBSYSTEM READINESS MATRIX:"), "reportText matrix");

            Console.WriteLine("  ✓ Subsystem compatibility matrix & verdict generation verified.");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception($"[TEST FAILURE] Assertion failed: {message}");
            }
        }
    }
}
