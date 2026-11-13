using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlueDoc.Core.Enums;
using BlueDoc.Core.Models;
using BlueDoc.Diagnostics.Rules;

namespace BlueDoc.Tests.Unit
{
    public static class DiagnosticEngineTests
    {
        public static void Run()
        {
            Console.WriteLine("[TEST SUITE] Diagnostic Engine Rules Tests");

            RunAdapterDetectionRuleTests().GetAwaiter().GetResult();
            RunHardwareStatusRuleTests().GetAwaiter().GetResult();
            RunServiceStatusRuleTests().GetAwaiter().GetResult();
            RunDriverHealthRuleTests().GetAwaiter().GetResult();
            RunNewPhase4RuleTests().GetAwaiter().GetResult();
            RunEngineCatalogTests().GetAwaiter().GetResult();

            Console.WriteLine("All Diagnostic Rules tests passed!\n");
        }

        private static async Task RunNewPhase4RuleTests()
        {
            // AudioServiceRule
            var audioRule = new AudioServiceRule();
            var ctxAudioDis = new DiagnosticContext
            {
                AudioGatewayService = new ServiceInfo { ServiceName = "BTAGService", StartupType = "Disabled" }
            };
            var resAudioDis = await audioRule.EvaluateAsync(ctxAudioDis);
            Assert(resAudioDis.Status == DiagnosticStatus.Warning, "AudioService: Disabled service reports Warning");

            var ctxAudioRun = new DiagnosticContext
            {
                AudioGatewayService = new ServiceInfo { ServiceName = "BTAGService", Status = "Running" }
            };
            var resAudioRun = await audioRule.EvaluateAsync(ctxAudioRun);
            Assert(resAudioRun.Status == DiagnosticStatus.Passed, "AudioService: Running service reports Passed");

            // DiscoveryReadinessRule
            var discRule = new DiscoveryReadinessRule();
            var ctxNoRadio = new DiagnosticContext
            {
                PrimaryAdapter = new BluetoothAdapterInfo { Status = AdapterStatus.Operational },
                IsRadioEnabled = false
            };
            var resNoRadio = await discRule.EvaluateAsync(ctxNoRadio);
            Assert(resNoRadio.Status == DiagnosticStatus.Failed, "DiscoveryReadiness: Disabled radio reports Failed");

            // PairingReadinessRule
            var pairRule = new PairingReadinessRule();
            var ctxStoppedServ = new DiagnosticContext
            {
                CoreService = new ServiceInfo { ServiceName = "bthserv", Status = "Stopped" }
            };
            var resStoppedServ = await pairRule.EvaluateAsync(ctxStoppedServ);
            Assert(resStoppedServ.Status == DiagnosticStatus.Failed, "PairingReadiness: Stopped bthserv reports Failed");

            // BleGattSupportRule
            var gattRule = new BleGattSupportRule();
            var ctxWin11 = new DiagnosticContext
            {
                OsInfo = new OsVersionInfo { Platform = WindowsPlatformType.Windows11, OperatingSystemName = "Windows 11" }
            };
            var resGattWin11 = await gattRule.EvaluateAsync(ctxWin11);
            Assert(resGattWin11.Status == DiagnosticStatus.Passed, "BleGattSupport: Win11 reports Passed");

            var ctxWin7 = new DiagnosticContext
            {
                OsInfo = new OsVersionInfo { Platform = WindowsPlatformType.Windows7, OperatingSystemName = "Windows 7 SP1" }
            };
            var resGattWin7 = await gattRule.EvaluateAsync(ctxWin7);
            Assert(resGattWin7.Status == DiagnosticStatus.NotSupported, "BleGattSupport: Win7 reports NotSupported");

            // EventLogHealthRule
            var evtRule = new EventLogHealthRule();
            var ctxEvtErr = new DiagnosticContext
            {
                RecentEvents = new List<BluetoothEventRecord>
                {
                    new BluetoothEventRecord { EventId = 3, ProviderName = "BTHUSB", LevelDisplayName = "Error", HumanAnalysis = "HCI Timeout" }
                }
            };
            var resEvtErr = await evtRule.EvaluateAsync(ctxEvtErr);
            Assert(resEvtErr.Status == DiagnosticStatus.Warning, "EventLogHealth: Critical error records report Warning");
        }

        private static async Task RunEngineCatalogTests()
        {
            var engine = new BlueDoc.Diagnostics.Engine.DiagnosticEngine();
            var testList = engine.GetInitialTestList();
            Assert(testList.Count == 12, $"Engine Catalog: Registered exactly 12 rules (Found: {testList.Count})");
            Assert(testList.All(t => t.Status == DiagnosticStatus.NotTested), "Engine Catalog: Initial tests start in NotTested state");
            await Task.Yield();
        }

        private static async Task RunAdapterDetectionRuleTests()
        {
            var rule = new AdapterDetectionRule();

            // Scenario A: No adapters found
            var ctxNoAdapter = new DiagnosticContext
            {
                Adapters = new List<BluetoothAdapterInfo>(),
                PrimaryAdapter = null
            };
            var resA = await rule.EvaluateAsync(ctxNoAdapter);
            Assert(resA.Status == DiagnosticStatus.Failed, "AdapterDetection: Empty list reports Failed");

            // Scenario B: Adapter found
            var ctxWithAdapter = new DiagnosticContext
            {
                Adapters = new List<BluetoothAdapterInfo>
                {
                    new BluetoothAdapterInfo { FriendlyName = "Realtek Adapter", IsPresent = true }
                },
                PrimaryAdapter = new BluetoothAdapterInfo { FriendlyName = "Realtek Adapter", IsPresent = true }
            };
            var resB = await rule.EvaluateAsync(ctxWithAdapter);
            Assert(resB.Status == DiagnosticStatus.Passed, "AdapterDetection: Found adapter reports Passed");
        }

        private static async Task RunHardwareStatusRuleTests()
        {
            var rule = new HardwareStatusRule();

            // Scenario A: Operational
            var ctxOp = new DiagnosticContext
            {
                PrimaryAdapter = new BluetoothAdapterInfo { Status = AdapterStatus.Operational, ProblemCode = 0, IsPresent = true }
            };
            var resOp = await rule.EvaluateAsync(ctxOp);
            Assert(resOp.Status == DiagnosticStatus.Passed, "HardwareStatus: Operational reports Passed");

            // Scenario B: Disabled (Code 22)
            var ctxDis = new DiagnosticContext
            {
                PrimaryAdapter = new BluetoothAdapterInfo { Status = AdapterStatus.Disabled, ProblemCode = 22, IsPresent = true }
            };
            var resDis = await rule.EvaluateAsync(ctxDis);
            Assert(resDis.Status == DiagnosticStatus.Warning, "HardwareStatus: Code 22 reports Warning");
            Assert(resDis.RequiresAdmin, "HardwareStatus: Code 22 requires admin to repair");

            // Scenario C: Device start error (Code 10)
            var ctxErr10 = new DiagnosticContext
            {
                PrimaryAdapter = new BluetoothAdapterInfo { Status = AdapterStatus.DeviceError, ProblemCode = 10, IsPresent = true }
            };
            var resErr10 = await rule.EvaluateAsync(ctxErr10);
            Assert(resErr10.Status == DiagnosticStatus.Failed, "HardwareStatus: Code 10 reports Failed");
        }

        private static async Task RunServiceStatusRuleTests()
        {
            var rule = new ServiceStatusRule();

            // Scenario A: Service running
            var ctxRunning = new DiagnosticContext
            {
                CoreService = new ServiceInfo { ServiceName = "bthserv", Status = "Running", StartupType = "Manual", Exists = true }
            };
            var resRunning = await rule.EvaluateAsync(ctxRunning);
            Assert(resRunning.Status == DiagnosticStatus.Passed, "ServiceStatus: Running service reports Passed");

            // Scenario B: Service stopped
            var ctxStopped = new DiagnosticContext
            {
                CoreService = new ServiceInfo { ServiceName = "bthserv", Status = "Stopped", StartupType = "Manual", Exists = true }
            };
            var resStopped = await rule.EvaluateAsync(ctxStopped);
            Assert(resStopped.Status == DiagnosticStatus.Warning, "ServiceStatus: Stopped service reports Warning");

            // Scenario C: Service disabled
            var ctxDisabled = new DiagnosticContext
            {
                CoreService = new ServiceInfo { ServiceName = "bthserv", Status = "Stopped", StartupType = "Disabled", Exists = true }
            };
            var resDisabled = await rule.EvaluateAsync(ctxDisabled);
            Assert(resDisabled.Status == DiagnosticStatus.Failed, "ServiceStatus: Disabled service reports Failed");
        }

        private static async Task RunDriverHealthRuleTests()
        {
            var rule = new DriverHealthRule();

            // Scenario A: Driver installed
            var ctxWithDriver = new DiagnosticContext
            {
                PrimaryAdapter = new BluetoothAdapterInfo
                {
                    IsPresent = true,
                    Driver = new DriverInfo { DriverProvider = "Realtek", DriverVersion = "1.9.1051.3005" }
                }
            };
            var resWithDriver = await rule.EvaluateAsync(ctxWithDriver);
            Assert(resWithDriver.Status == DiagnosticStatus.Passed, "DriverHealth: Valid driver reports Passed");

            // Scenario B: Driver missing
            var ctxNoDriver = new DiagnosticContext
            {
                PrimaryAdapter = new BluetoothAdapterInfo
                {
                    IsPresent = true,
                    Driver = new DriverInfo { DriverProvider = "", DriverVersion = "" }
                }
            };
            var resNoDriver = await rule.EvaluateAsync(ctxNoDriver);
            Assert(resNoDriver.Status == DiagnosticStatus.Failed, "DriverHealth: Missing driver reports Failed");
        }

        private static void Assert(bool condition, string testName)
        {
            if (condition)
            {
                Console.WriteLine($"  [PASS] {testName}");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  [FAIL] {testName}");
                Console.ResetColor();
                throw new Exception($"Assertion failed for: {testName}");
            }
        }
    }
}

