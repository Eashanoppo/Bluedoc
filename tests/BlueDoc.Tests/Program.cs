using System;
using BlueDoc.Tests.Integration;
using BlueDoc.Tests.Unit;

namespace BlueDoc.Tests
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            Console.WriteLine("===============================================================");
            Console.WriteLine("  BLUEDOC TEST RUNNER & HARDWARE VERIFICATION HARNESS");
            Console.WriteLine("  Product: BlueDoc");
            Console.WriteLine("  Company: UNLEFT LLC");
            Console.WriteLine("  Developer: Golam Morshed Eashan");
            Console.WriteLine("===============================================================\n");

            try
            {
                // Run Unit Tests
                HardwareIdParserTests.Run();
                DiagnosticEngineTests.Run();
                DeviceClassDecoderTests.Run();
                SqliteRepositoryTests.RunAllTests().GetAwaiter().GetResult();
                EventAnalyzerTests.RunAllTests();
                DriverAssistantTests.RunAllTests();
                ReportGeneratorTests.RunAllTests();
                Phase5AdvancedDiagnosticsTests.RunAllTests();
                Phase6RepairAndReportingTests.RunAllTestsAsync().GetAwaiter().GetResult();
                Phase7PlatformAndFailureScenarioTests.RunAllTestsAsync().GetAwaiter().GetResult();
                Phase8TechnicianAndSaaSIntegrationTests.RunAllTestsAsync().GetAwaiter().GetResult();
                Phase9AudioTraceAndTrayGuardTests.RunAllTestsAsync().GetAwaiter().GetResult();
                Phase10SaasFleetAndApiGatewayTests.RunAllTestsAsync().GetAwaiter().GetResult();
                DeviceScannerAndPairingTests.RunAllTests();
                FixConnectionDoctorAndPairingTests.RunAllTests();
                AccessibilityVerificationTests.RunAllTests();
                Performance.PerformanceBenchmarkHarness.RunAllBenchmarksAsync().GetAwaiter().GetResult();
                BluetoothFunctionalMatrixTests.RunAllScenariosAsync().GetAwaiter().GetResult();
                FixConnectionDoctorAndPairingTests.RunAllTests();
                AccessibilityVerificationTests.RunAllTests();
                Performance.PerformanceBenchmarkHarness.RunAllBenchmarksAsync().GetAwaiter().GetResult();
                BluetoothFunctionalMatrixTests.RunAllScenariosAsync().GetAwaiter().GetResult();

                // Run Live Integration Tests
                RealHardwareInspectionTests.Run();
                RealDeviceDiscoveryTests.Run();

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("===============================================================");
                Console.WriteLine("  ALL VERIFICATION TESTS COMPLETED SUCCESSFULLY (100% PASS)");
                Console.WriteLine("===============================================================");
                Console.ResetColor();

                return 0;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[FATAL TEST FAILURE] {ex.Message}\n{ex.StackTrace}");
                Console.ResetColor();
                return 1;
            }
        }
    }
}
