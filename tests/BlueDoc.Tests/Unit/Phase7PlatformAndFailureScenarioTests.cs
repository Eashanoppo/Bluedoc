using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlueDoc.Application.Services;
using BlueDoc.Core.Enums;
using BlueDoc.Core.Models;
using BlueDoc.Diagnostics.Rules;
using BlueDoc.Infrastructure.Drivers;
using BlueDoc.Infrastructure.Hardware;
using BlueDoc.Infrastructure.Platform;

namespace BlueDoc.Tests.Unit
{
    public static class Phase7PlatformAndFailureScenarioTests
    {
        public static async Task RunAllTestsAsync()
        {
            Console.WriteLine("[TEST SUITE] Phase 7 Cross-Platform & Failure Scenario Tests");

            TestMultiManufacturerProfiles();
            await TestComprehensiveFailureScenariosAsync();
            TestCrossPlatformCompatibilityProfiles();

            Console.WriteLine("All Phase 7 Cross-Platform & Failure Scenario tests passed!\n");
        }

        private static void TestMultiManufacturerProfiles()
        {
            var assistant = new DriverAssistant();

            // 1. Realtek
            var realtekId = HardwareIdParser.Parse(@"USB\VID_0BDA&PID_C822");
            Assert(realtekId.IdentifiedManufacturer.Contains("Realtek"), "Realtek manufacturer parsed");
            string realtekUrl = assistant.GetManufacturerSupportUrl(realtekId);
            Assert(realtekUrl.Contains("realtek.com"), "Realtek official URL mapped");

            // 2. Intel
            var intelId = HardwareIdParser.Parse(@"USB\VID_8087&PID_0026");
            Assert(intelId.IdentifiedManufacturer.Contains("Intel"), "Intel manufacturer parsed");
            string intelUrl = assistant.GetManufacturerSupportUrl(intelId);
            Assert(intelUrl.Contains("intel.com"), "Intel official URL mapped");

            // 3. Broadcom
            var broadcomId = HardwareIdParser.Parse(@"USB\VID_0A5C&PID_21E8");
            Assert(broadcomId.IdentifiedManufacturer.Contains("Broadcom"), "Broadcom manufacturer parsed");
            string broadcomUrl = assistant.GetManufacturerSupportUrl(broadcomId);
            Assert(broadcomUrl.Contains("broadcom.com"), "Broadcom official URL mapped");

            // 4. Qualcomm / Atheros
            var qualcommId = HardwareIdParser.Parse(@"USB\VID_0CF3&PID_E005");
            Assert(qualcommId.IdentifiedManufacturer.Contains("Qualcomm"), "Qualcomm manufacturer parsed");
            string qualcommUrl = assistant.GetManufacturerSupportUrl(qualcommId);
            Assert(qualcommUrl.Contains("qualcomm.com"), "Qualcomm official URL mapped");

            // 5. MediaTek
            var mediatekId = HardwareIdParser.Parse(@"USB\VID_0E8D&PID_7961");
            Assert(mediatekId.IdentifiedManufacturer.Contains("MediaTek"), "MediaTek manufacturer parsed");
            string mediatekUrl = assistant.GetManufacturerSupportUrl(mediatekId);
            Assert(mediatekUrl.Contains("mediatek.com"), "MediaTek official URL mapped");

            // 6. Texas Instruments
            var tiId = HardwareIdParser.Parse(@"USB\VID_0451&PID_8041");
            Assert(tiId.VendorId == "0451", "TI Vendor ID parsed");
            string tiUrl = assistant.GetManufacturerSupportUrl(tiId);
            Assert(tiUrl.Contains("ti.com"), "TI official URL mapped");

            Console.WriteLine("  ✓ Multi-manufacturer identification (Realtek, Intel, Broadcom, Qualcomm, MediaTek, TI) verified.");
        }

        private static async Task TestComprehensiveFailureScenariosAsync()
        {
            // Scenario 1: Adapter physically missing
            var adapterRule = new AdapterDetectionRule();
            var missingContext = new DiagnosticContext { Adapters = new List<BluetoothAdapterInfo>() };
            var resMissing = await adapterRule.EvaluateAsync(missingContext);
            Assert(resMissing.Status == DiagnosticStatus.Failed, "Missing adapter returns Failed");
            Assert(resMissing.RecommendedAction.Contains("USB dongle") || resMissing.RecommendedAction.Contains("Bluetooth"), "Actionable advice for missing adapter");

            // Scenario 2: Adapter Disabled (PnP Code 22)
            var hwRule = new HardwareStatusRule();
            var disabledAdapter = new BluetoothAdapterInfo
            {
                FriendlyName = "Disabled Bluetooth Adapter",
                ProblemCode = 22,
                Status = AdapterStatus.Disabled,
                IsPresent = true
            };
            var disabledContext = new DiagnosticContext
            {
                Adapters = new List<BluetoothAdapterInfo> { disabledAdapter },
                PrimaryAdapter = disabledAdapter
            };
            var resDisabled = await hwRule.EvaluateAsync(disabledContext);
            Assert(resDisabled.Status == DiagnosticStatus.Warning, "Code 22 returns Warning");
            Assert(resDisabled.RequiresAdmin == true, "Re-enabling requires admin");
            Assert(resDisabled.RecommendedAction.Contains("Device Manager") || resDisabled.RecommendedAction.Contains("Safe Repair"), "Re-enabling advises action");

            // Scenario 3: Controller Firmware / Bus Malfunction (PnP Code 43)
            var error43Adapter = new BluetoothAdapterInfo
            {
                FriendlyName = "Failing Bluetooth Adapter",
                ProblemCode = 43,
                Status = AdapterStatus.DeviceError,
                IsPresent = true
            };
            var error43Context = new DiagnosticContext
            {
                Adapters = new List<BluetoothAdapterInfo> { error43Adapter },
                PrimaryAdapter = error43Adapter
            };
            var resError43 = await hwRule.EvaluateAsync(error43Context);
            Assert(resError43.Status == DiagnosticStatus.Failed, "Code 43 returns Failed");
            Assert(resError43.PossibleCause.Contains("firmware") || resError43.PossibleCause.Contains("controller"), "Possible cause notes firmware lockup or controller error");

            // Scenario 4: Core Bluetooth Service (bthserv) Stopped
            var svcRule = new ServiceStatusRule();
            var stoppedSvcContext = new DiagnosticContext
            {
                CoreService = new ServiceInfo
                {
                    ServiceName = "bthserv",
                    Exists = true,
                    Status = "Stopped",
                    StartupType = "Manual"
                }
            };
            var resStopped = await svcRule.EvaluateAsync(stoppedSvcContext);
            Assert(resStopped.Status == DiagnosticStatus.Warning, "Stopped service returns Warning");
            Assert(resStopped.RecommendedAction.Contains("Safe Repair") || resStopped.RecommendedAction.Contains("restart"), "Advises using Safe Repair Center to restart");

            // Scenario 5: Service Disabled in SCM
            var disabledSvcContext = new DiagnosticContext
            {
                CoreService = new ServiceInfo
                {
                    ServiceName = "bthserv",
                    Exists = true,
                    Status = "Stopped",
                    StartupType = "Disabled"
                }
            };
            var resDisabledSvc = await svcRule.EvaluateAsync(disabledSvcContext);
            Assert(resDisabledSvc.Status == DiagnosticStatus.Failed, "Disabled service returns Failed");

            // Scenario 6: Radio Switch Turned Off
            var radioRule = new RadioAvailabilityRule();
            var radioOffAdapter = new BluetoothAdapterInfo
            {
                FriendlyName = "Internal Bluetooth Transceiver",
                IsPresent = true
            };
            var radioOffContext = new DiagnosticContext
            {
                PrimaryAdapter = radioOffAdapter,
                IsRadioEnabled = false
            };
            var resRadioOff = await radioRule.EvaluateAsync(radioOffContext);
            Assert(resRadioOff.Status == DiagnosticStatus.Warning, "Radio off returns Warning");
            Assert(resRadioOff.PossibleCause.Contains("Airplane mode") || resRadioOff.PossibleCause.Contains("Settings"), "Identifies Airplane mode or switch toggle");

            // Scenario 7: Outdated / Generic Driver Failure
            var driverRule = new DriverHealthRule();
            var missingDriverContext = new DiagnosticContext
            {
                PrimaryAdapter = new BluetoothAdapterInfo
                {
                    FriendlyName = "Generic Adapter",
                    IsPresent = true,
                    Driver = null
                }
            };
            var resMissingDriver = await driverRule.EvaluateAsync(missingDriverContext);
            Assert(resMissingDriver.Status == DiagnosticStatus.Failed, "Missing driver returns Failed");

            Console.WriteLine("  ✓ Comprehensive failure scenario simulation (Missing HW, Code 22, Code 43, Service Stopped/Disabled, Radio Off) verified.");
        }

        private static void TestCrossPlatformCompatibilityProfiles()
        {
            var compatService = new CompatibilityService();

            // Profile 1: Windows 7 SP1 (Legacy Environment)
            var win7Os = new OsVersionInfo
            {
                Platform = WindowsPlatformType.Windows7,
                OperatingSystemName = "Windows 7 SP1",
                MajorVersion = 6,
                MinorVersion = 1,
                BuildNumber = 7601,
                Is64BitOperatingSystem = true
            };

            var win7Assessment = compatService.EvaluateCompatibility(win7Os);
            Assert(win7Assessment.OverallVerdict.Contains("Classic Mode"), "Win7 assessment reflects Classic Mode");

            var win7Ble = win7Assessment.Subsystems.FirstOrDefault(s => s.Title.Contains("Windows Runtime BLE"));
            Assert(win7Ble != null && !win7Ble.IsSupported, "WinRT BLE must be marked NOT SUPPORTED on Win7");
            Assert(win7Ble.StatusDisplay == "NOT SUPPORTED", "Honest NOT SUPPORTED display for Win7 BLE");

            var win7SetupApi = win7Assessment.Subsystems.FirstOrDefault(s => s.Title.Contains("SetupAPI"));
            Assert(win7SetupApi != null && win7SetupApi.IsSupported, "SetupAPI supported on Win7");

            // Profile 2: Windows 10
            var win10Os = new OsVersionInfo
            {
                Platform = WindowsPlatformType.Windows10,
                OperatingSystemName = "Windows 10",
                MajorVersion = 10,
                MinorVersion = 0,
                BuildNumber = 19045,
                Is64BitOperatingSystem = true
            };

            var win10Assessment = compatService.EvaluateCompatibility(win10Os);
            Assert(win10Assessment.OverallVerdict.Contains("Fully Compatible"), "Win10 assessment reflects Fully Compatible");
            var win10Ble = win10Assessment.Subsystems.FirstOrDefault(s => s.Title.Contains("Windows Runtime BLE"));
            Assert(win10Ble != null && win10Ble.IsSupported, "WinRT BLE supported on Win10");

            // Profile 3: Windows 11
            var win11Os = new OsVersionInfo
            {
                Platform = WindowsPlatformType.Windows11,
                OperatingSystemName = "Windows 11",
                MajorVersion = 10,
                MinorVersion = 0,
                BuildNumber = 26200,
                Is64BitOperatingSystem = true
            };

            var win11Assessment = compatService.EvaluateCompatibility(win11Os);
            Assert(win11Assessment.OverallVerdict.Contains("Fully Compatible"), "Win11 assessment reflects Fully Compatible");

            Console.WriteLine("  ✓ Cross-platform compatibility profiles (Win7 SP1, Win10, Win11) & legacy fallback verified.");
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
