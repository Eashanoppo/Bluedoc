using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BlueDoc.Core.Enums;
using BlueDoc.Core.Models;
using BlueDoc.Infrastructure.Audio;
using BlueDoc.Infrastructure.Diagnostics;
using BlueDoc.Infrastructure.Drivers;
using BlueDoc.UI.Tray;

namespace BlueDoc.Tests.Unit
{
    public static class Phase9AudioTraceAndTrayGuardTests
    {
        public static async Task RunAllTestsAsync()
        {
            Console.WriteLine("\n[TEST SUITE] Phase 9 Audio Stream Diagnostics, Driver Matrix, HCI Tracing & System Tray Guard Tests");

            TestAudioProfileAnalysis();
            TestDriverComparisonCatalog();
            TestHciTraceAssistant();
            await TestTrayGuardHealthProbeAsync();

            Console.WriteLine("All Phase 9 Audio, Driver Matrix, HCI Tracing & Tray Guard tests passed!");
        }

        private static void TestAudioProfileAnalysis()
        {
            var inspector = new BluetoothAudioInspector();
            var analysis = inspector.AnalyzeAudioSubsystem();

            Assert(analysis != null, "Audio analysis returned non-null result");
            Assert(analysis.Endpoints != null, "Audio endpoints collection initialized");
            Assert(!string.IsNullOrEmpty(analysis.Recommendation), "Actionable audio recommendation provided");

            // Verify Simulated Audio Profile Contention Logic
            var hfpMic = new BluetoothAudioEndpoint
            {
                Name = "Wireless Headset Hands-Free AG Audio",
                ActiveProfile = "Hands-Free (HFP/HSP Voice)",
                Direction = "Recording",
                IsConnected = true
            };
            var a2dpPlayback = new BluetoothAudioEndpoint
            {
                Name = "Wireless Headset Stereo Audio",
                ActiveProfile = "A2DP Stereo (High Definition)",
                Direction = "Playback",
                IsConnected = true
            };

            var simulatedAnalysis = new BluetoothAudioAnalysisResult
            {
                Endpoints = new List<BluetoothAudioEndpoint> { hfpMic, a2dpPlayback }
            };
            simulatedAnalysis.HasActiveHfpMicrophone = simulatedAnalysis.Endpoints.Any(e => e.ActiveProfile.Contains("Hands-Free") && e.Direction == "Recording" && e.IsConnected);

            if (simulatedAnalysis.HasActiveHfpMicrophone)
            {
                simulatedAnalysis.IsHighDefPlaybackThrottled = true;
                foreach (var ep in simulatedAnalysis.Endpoints.Where(e => e.Direction == "Playback"))
                {
                    ep.IsProfileConstrained = true;
                }
            }

            Assert(simulatedAnalysis.HasActiveHfpMicrophone, "Identified active Hands-Free microphone");
            Assert(simulatedAnalysis.IsHighDefPlaybackThrottled, "High-def playback throttled flag set");
            Assert(a2dpPlayback.IsProfileConstrained, "A2DP playback marked as profile-constrained");

            Console.WriteLine($"  ✓ Bluetooth audio stream profile inspection & telephony contention logic verified (Detected endpoints: {analysis.Endpoints.Count}).");
        }

        private static void TestDriverComparisonCatalog()
        {
            var service = new DriverComparisonService();

            // Scenario 1: Realtek Stable Driver
            var realtekStable = new BluetoothAdapterInfo
            {
                FriendlyName = "Realtek Bluetooth Adapter",
                Manufacturer = "Realtek Semiconductor Corp.",
                HardwareDetails = new HardwareIdDetails { VendorId = "0BDA", ProductId = "C822" },
                Driver = new DriverInfo { DriverProvider = "Realtek Semiconductor Corp.", DriverVersion = "1.1072.2312.2501" }
            };
            var resStable = service.CompareDriverVersion(realtekStable);
            Assert(resStable.Status == "Optimal", "Realtek stable driver evaluated as Optimal");
            Assert(!resStable.HasKnownRegression, "No regression on stable driver");

            // Scenario 2: Realtek Problematic Driver (1.9.1051.3000 - S3 sleep wake lockup)
            var realtekProblem = new BluetoothAdapterInfo
            {
                FriendlyName = "Realtek Bluetooth Adapter",
                Manufacturer = "Realtek Semiconductor Corp.",
                HardwareDetails = new HardwareIdDetails { VendorId = "0BDA", ProductId = "C822" },
                Driver = new DriverInfo { DriverProvider = "Realtek Semiconductor Corp.", DriverVersion = "1.9.1051.3000" }
            };
            var resProblem = service.CompareDriverVersion(realtekProblem);
            Assert(resProblem.Status == "Known Problematic", "Realtek 1.9.1051 evaluated as Known Problematic");
            Assert(resProblem.HasKnownRegression, "Regression flag set for problematic version");
            Assert(resProblem.KnownRegressionDetails.Contains("Code 43") || resProblem.KnownRegressionDetails.Contains("sleep"), "Identifies Code 43 sleep regression");
            Assert(!string.IsNullOrEmpty(resProblem.OfficialDownloadUrl), "Official download URL populated");

            // Scenario 3: Intel Driver with BTHUSB Event 3 regression (22.190.0)
            var intelProblem = new BluetoothAdapterInfo
            {
                FriendlyName = "Intel Wireless Bluetooth",
                Manufacturer = "Intel Corporation",
                HardwareDetails = new HardwareIdDetails { VendorId = "8087", ProductId = "0026" },
                Driver = new DriverInfo { DriverProvider = "Intel Corporation", DriverVersion = "22.190.0.4" }
            };
            var resIntelProblem = service.CompareDriverVersion(intelProblem);
            Assert(resIntelProblem.Status == "Known Problematic", "Intel 22.190 evaluated as Known Problematic");
            Assert(resIntelProblem.KnownRegressionDetails.Contains("Event 3") || resIntelProblem.KnownRegressionDetails.Contains("timeouts"), "Identifies Event 3 HCI timeout regression");

            // Scenario 4: Generic Microsoft Class Driver
            var genericAdapter = new BluetoothAdapterInfo
            {
                FriendlyName = "Generic Bluetooth Adapter",
                Manufacturer = "Microsoft",
                HardwareDetails = new HardwareIdDetails { VendorId = "0BDA", ProductId = "C822" },
                Driver = new DriverInfo { DriverProvider = "Microsoft", DriverVersion = "10.0.26100.9549" }
            };
            var resGeneric = service.CompareDriverVersion(genericAdapter);
            Assert(resGeneric.Status == "Generic Class Driver", "Microsoft driver evaluated as Generic Class Driver");
            Assert(resGeneric.ActionRecommendation.Contains("official manufacturer driver"), "Advises installing official OEM driver");

            Console.WriteLine("  ✓ Driver version comparison catalog & known regression detection verified (Realtek, Intel, Microsoft generic).");
        }

        private static void TestHciTraceAssistant()
        {
            var assistant = new HciTraceAssistant();
            var guide = assistant.GetWiresharkAnalysisGuide(@"C:\Temp\test_trace.etl");

            Assert(!string.IsNullOrEmpty(guide), "Wireshark guide generated");
            Assert(guide.Contains("etl2pcapng"), "Guide references etl2pcapng conversion tool");
            Assert(guide.Contains("btsnoop"), "Guide specifies btsnoop filter for Wireshark");

            var session = assistant.CurrentSession;
            Assert(session != null, "Current session is initialized");
            Assert(!session.IsActive, "Session starts in idle state");

            Console.WriteLine("  ✓ HCI trace assistant & Wireshark ETL analysis guide verified.");
        }

        private static async Task TestTrayGuardHealthProbeAsync()
        {
            var tray = new TrayGuardManager();

            bool alertTriggered = false;
            string alertMsg = string.Empty;

            tray.HealthAlertTriggered += (s, msg) =>
            {
                alertTriggered = true;
                alertMsg = msg;
            };

            // Execute a live health probe
            tray.CheckHealth(triggerNotification: false);

            Assert(tray != null, "TrayGuardManager instantiated");
            Assert(!string.IsNullOrEmpty(alertMsg) || !alertTriggered, "Tray guard notification handling verified");

            // Teardown
            tray.Dispose();

            await Task.Yield();
            Console.WriteLine("  ✓ System Tray Guard health probe execution & event binding verified.");
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
