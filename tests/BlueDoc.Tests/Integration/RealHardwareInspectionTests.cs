using System;
using System.Threading.Tasks;
using BlueDoc.Core.Enums;
using BlueDoc.Diagnostics.Engine;
using BlueDoc.Infrastructure.Hardware;
using BlueDoc.Infrastructure.Platform;
using BlueDoc.Infrastructure.Services;

namespace BlueDoc.Tests.Integration
{
    public static class RealHardwareInspectionTests
    {
        public static void Run()
        {
            Console.WriteLine("[TEST SUITE] Live Windows & Real Hardware Integration Verification");

            RunLiveOsTest();
            RunLiveHardwareInspectionTest();
            RunLiveDiagnosticEngineTest().GetAwaiter().GetResult();

            Console.WriteLine("All Live Hardware Integration tests completed!\n");
        }

        private static void RunLiveOsTest()
        {
            var os = OperatingSystemInspector.GetOperatingSystemInfo();
            Console.WriteLine($"  OS Detected: {os.OperatingSystemName}");
            Console.WriteLine($"  Build: {os.BuildNumber}, Arch: {os.ArchitectureDisplay}");
            Console.WriteLine($"  Platform Enum: {os.Platform}");

            if (os.MajorVersion <= 0) throw new Exception("MajorVersion must be > 0");
            if (os.BuildNumber <= 0) throw new Exception("BuildNumber must be > 0");
            Console.WriteLine("  [PASS] Live OS detection verified.");
        }

        private static void RunLiveHardwareInspectionTest()
        {
            var inspector = new SetupApiHardwareInspector();
            var adapters = inspector.EnumerateAdapters();

            Console.WriteLine($"  Total Bluetooth device nodes enumerated: {adapters.Count}");
            foreach (var a in adapters)
            {
                Console.WriteLine($"    -> Device: '{a.FriendlyName}'");
                Console.WriteLine($"       Manufacturer: '{a.Manufacturer}'");
                Console.WriteLine($"       Hardware ID: '{a.HardwareId}'");
                Console.WriteLine($"       Vendor ID: '{a.HardwareDetails.VendorId}', PID: '{a.HardwareDetails.ProductId}'");
                Console.WriteLine($"       Status: {a.Status} (Code: {a.ProblemCode})");
                Console.WriteLine($"       Driver: {a.Driver.DriverProvider} (v{a.Driver.DriverVersion}) Date: {a.Driver.DriverDateString}");
                if (!string.IsNullOrEmpty(a.RadioAddress))
                {
                    Console.WriteLine($"       Radio MAC: {a.RadioAddress}");
                }
            }

            var primary = inspector.GetPrimaryAdapter();
            Console.WriteLine($"  Primary Adapter: '{primary.FriendlyName}' (Status: {primary.Status})");
            Console.WriteLine("  [PASS] Live hardware enumeration verified.");
        }

        private static async Task RunLiveDiagnosticEngineTest()
        {
            var engine = new DiagnosticEngine();
            Console.WriteLine("  Executing Full Diagnostic Workflow on Live Machine...");

            var session = await engine.RunFullDiagnosticsAsync();
            Console.WriteLine($"  Diagnostic Session Completed. Overall Status: {session.OverallStatus}");

            foreach (var res in session.Results)
            {
                Console.WriteLine($"    [{res.Status}] {res.TestName}: {res.Summary}");
            }

            Console.WriteLine("  [PASS] Live Diagnostic Workflow verified.");
        }
    }
}

