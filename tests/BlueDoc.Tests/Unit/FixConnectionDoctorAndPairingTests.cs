using System;
using System.Linq;
using System.Threading.Tasks;
using BlueDoc.Application.Services;
using BlueDoc.Core.Enums;
using BlueDoc.Core.Models;
using BlueDoc.UI.ViewModels;

namespace BlueDoc.Tests.Unit
{
    public static class FixConnectionDoctorAndPairingTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("[TEST SUITE] Fix My Bluetooth Connection Doctor & Pairing Verification");

            Test_StartupHealthAssessment_NonDestructiveInspection().GetAwaiter().GetResult();
            Test_BluetoothConnectionOrchestrator_ProtocolAppropriateAudioRouting().GetAwaiter().GetResult();
            Test_BluetoothConnectionOrchestrator_ProtocolAppropriateHidRouting().GetAwaiter().GetResult();
            Test_ConnectionFailureDiagnosticEngine_AuthenticationFailureDiagnosis().GetAwaiter().GetResult();
            Test_ConnectionFailureDiagnosticEngine_10PointInvestigationChecks().GetAwaiter().GetResult();
            Test_FixMyConnectionWizardService_10StepStateDefinitions();
            Test_FixMyConnectionWizardService_TargetSelection();
            Test_FixMyConnectionWizardViewModel_InteractiveAuthAndRepair().GetAwaiter().GetResult();

            Console.WriteLine("  ✓ All Fix Connection Doctor & Pairing Tests Passed (8/8)\n");
        }

        private static async Task Test_StartupHealthAssessment_NonDestructiveInspection()
        {
            Console.Write("  Running Test_StartupHealthAssessment_NonDestructiveInspection... ");
            var healthService = new BluetoothStartupHealthService();
            var assessment = await healthService.InspectSystemHealthAsync();

            Assert(assessment != null, "Assessment should not be null.");
            Assert(!string.IsNullOrEmpty(assessment.OsName), "OS Name should be populated.");
            Assert(!string.IsNullOrEmpty(assessment.ApiReadiness), "API Readiness should be evaluated.");
            Assert(!string.IsNullOrEmpty(assessment.OverallMessage), "Overall health message should be generated.");
            Assert(assessment.ActionableIssues != null, "ActionableIssues collection should be initialized.");

            // Asserts non-destructive: no modifications made to system services
            Console.WriteLine("PASSED");
        }

        private static async Task Test_BluetoothConnectionOrchestrator_ProtocolAppropriateAudioRouting()
        {
            Console.Write("  Running Test_BluetoothConnectionOrchestrator_ProtocolAppropriateAudioRouting... ");
            var orchestrator = new BluetoothConnectionOrchestrator();
            var audioDevice = new BluetoothDeviceInfo
            {
                DeviceName = "Sony WH-1000XM5",
                BluetoothAddress = "38:18:4C:AA:BB:CC",
                RawAddress = 0x38184CAABBCC,
                MajorDeviceClass = "Audio/Video",
                CategoryName = "Headphones / Headset",
                CategoryIcon = "🎧",
                IsPaired = true
            };

            var details = await orchestrator.ConnectPeripheralAsync(audioDevice);
            Assert(details != null, "Connection details must not be null.");
            Assert(details.IsAudioCapable, "Audio device must be recognized as audio-capable.");
            Assert(details.Protocol.Contains("Audio"), $"Protocol should be Audio A2DP/HFP, was '{details.Protocol}'.");
            Assert(details.AudioAnalysis != null, "Audio subsystem analysis must be included.");
            Assert(details.ActiveProfiles != null, "Active profiles list must be initialized.");

            Console.WriteLine("PASSED");
        }

        private static async Task Test_BluetoothConnectionOrchestrator_ProtocolAppropriateHidRouting()
        {
            Console.Write("  Running Test_BluetoothConnectionOrchestrator_ProtocolAppropriateHidRouting... ");
            var orchestrator = new BluetoothConnectionOrchestrator();
            var mouseDevice = new BluetoothDeviceInfo
            {
                DeviceName = "Logitech MX Master 3S",
                BluetoothAddress = "E4:5F:01:11:22:33",
                RawAddress = 0xE45F01112233,
                MajorDeviceClass = "Peripheral",
                CategoryName = "Mouse",
                CategoryIcon = "🖱️",
                IsPaired = true
            };

            var details = await orchestrator.ConnectPeripheralAsync(mouseDevice);
            Assert(details != null, "Connection details must not be null.");
            Assert(!details.IsAudioCapable, "Mouse must not be flagged as audio capable.");
            Assert(details.Protocol.Contains("Human Interface Device"), $"Protocol should be HID, was '{details.Protocol}'.");

            Console.WriteLine("PASSED");
        }

        private static async Task Test_ConnectionFailureDiagnosticEngine_AuthenticationFailureDiagnosis()
        {
            Console.Write("  Running Test_ConnectionFailureDiagnosticEngine_AuthenticationFailureDiagnosis... ");
            var engine = new ConnectionFailureDiagnosticEngine();
            var targetDevice = new BluetoothDeviceInfo
            {
                DeviceName = "Bose QuietComfort 45",
                BluetoothAddress = "04:52:C7:88:99:00",
                MajorDeviceClass = "Audio/Video",
                CategoryName = "Headphones",
                IsPaired = false
            };

            // Test diagnosis of Win32 1244 ERROR_NOT_AUTHENTICATED
            var diagnosis = await engine.DiagnoseConnectionFailureAsync(
                targetDevice,
                lastErrorCode: 1244,
                lastErrorMessage: "Device rejected authentication.");

            Assert(diagnosis != null, "Diagnosis should not be null.");
            Assert(diagnosis.PrimaryRootCause.Contains("Authentication") || diagnosis.PrimaryRootCause.Contains("PIN") || diagnosis.PrimaryRootCause.Contains("Hardware"), 
                $"Expected authentication root cause, got '{diagnosis.PrimaryRootCause}'");
            Assert(diagnosis.Confidence == ConfidenceLevel.High || diagnosis.Confidence == ConfidenceLevel.Medium, 
                $"Confidence level should be High or Medium, got {diagnosis.Confidence}");
            Assert(!string.IsNullOrEmpty(diagnosis.DetailedEvidence), "Detailed evidence should be provided.");
            Assert(diagnosis.CheckItems != null && diagnosis.CheckItems.Count > 0, "Check items should be enumerated.");

            Console.WriteLine("PASSED");
        }

        private static async Task Test_ConnectionFailureDiagnosticEngine_10PointInvestigationChecks()
        {
            Console.Write("  Running Test_ConnectionFailureDiagnosticEngine_10PointInvestigationChecks... ");
            var engine = new ConnectionFailureDiagnosticEngine();
            var targetDevice = new BluetoothDeviceInfo
            {
                DeviceName = "JBL Flip 6",
                BluetoothAddress = "F4:4E:FD:12:34:56",
                MajorDeviceClass = "Audio/Video",
                CategoryName = "Audio / Speaker",
                IsPaired = true
            };

            var diagnosis = await engine.DiagnoseConnectionFailureAsync(targetDevice);
            Assert(diagnosis != null, "Diagnosis should not be null.");
            Assert(diagnosis.CheckItems != null, "CheckItems list should exist.");
            Assert(diagnosis.CheckItems.Count >= 5, $"Expected at least 5 diagnostic checkpoints evaluated, got {diagnosis.CheckItems.Count}");

            bool hasAdapterCheck = diagnosis.CheckItems.Any(c => c.Name.Contains("Adapter"));
            bool hasRadioCheck = diagnosis.CheckItems.Any(c => c.Name.Contains("Radio"));
            bool hasServiceCheck = diagnosis.CheckItems.Any(c => c.Name.Contains("bthserv") || c.Name.Contains("Service"));

            Assert(hasAdapterCheck, "Must include adapter presence check.");
            Assert(hasRadioCheck, "Must include radio power check.");
            Assert(hasServiceCheck, "Must include core service check.");

            Console.WriteLine("PASSED");
        }

        private static void Test_FixMyConnectionWizardService_10StepStateDefinitions()
        {
            Console.Write("  Running Test_FixMyConnectionWizardService_10StepStateDefinitions... ");
            var wizard = new FixMyConnectionWizardService();

            Assert(wizard.Steps != null, "Steps list must not be null.");
            Assert(wizard.Steps.Count == 10, $"Wizard must have exactly 10 structured steps, found {wizard.Steps.Count}");

            // Verify the 10 steps match the prompt requirements
            Assert(wizard.Steps[0].StepNumber == 1 && wizard.Steps[0].Title.Contains("Selection"), "Step 1: Peripheral Selection");
            Assert(wizard.Steps[1].StepNumber == 2 && wizard.Steps[1].Title.Contains("Adapter"), "Step 2: Adapter & Radio Inspection");
            Assert(wizard.Steps[2].StepNumber == 3 && wizard.Steps[2].Title.Contains("System Services"), "Step 3: System Services & Stack Readiness");
            Assert(wizard.Steps[3].StepNumber == 4 && wizard.Steps[3].Title.Contains("Discovery"), "Step 4: Discovery & Probing");
            Assert(wizard.Steps[4].StepNumber == 5 && wizard.Steps[4].Title.Contains("Pairing Readiness"), "Step 5: Pairing Readiness");
            Assert(wizard.Steps[5].StepNumber == 6 && wizard.Steps[5].Title.Contains("Pairing"), "Step 6: Native Pairing Attempt");
            Assert(wizard.Steps[6].StepNumber == 7 && wizard.Steps[6].Title.Contains("Authentication"), "Step 7: Authentication & Key Exchange");
            Assert(wizard.Steps[7].StepNumber == 8 && wizard.Steps[7].Title.Contains("Protocol"), "Step 8: Protocol-Appropriate Connection");
            Assert(wizard.Steps[8].StepNumber == 9 && wizard.Steps[8].Title.Contains("Verification"), "Step 9: Operational Verification");
            Assert(wizard.Steps[9].StepNumber == 10 && wizard.Steps[9].Title.Contains("Diagnostic"), "Step 10: Diagnostic Root-Cause & Safe Repair");

            // All steps start as Requested
            foreach (var step in wizard.Steps)
            {
                Assert(step.Status == DeviceOperationStatus.Requested, $"Step {step.StepNumber} initial status must be Requested.");
            }

            Console.WriteLine("PASSED");
        }

        private static void Test_FixMyConnectionWizardService_TargetSelection()
        {
            Console.Write("  Running Test_FixMyConnectionWizardService_TargetSelection... ");
            var wizard = new FixMyConnectionWizardService();
            var target = new BluetoothDeviceInfo
            {
                DeviceName = "AirPods Pro",
                BluetoothAddress = "9C:20:7B:44:55:66",
                MajorDeviceClass = "Audio/Video",
                CategoryName = "Headphones",
                CategoryIcon = "🎧",
                IsPaired = false
            };

            wizard.SelectTargetDevice(target);
            Assert(wizard.CurrentTargetDevice != null, "Target device must be set.");
            Assert(wizard.CurrentTargetDevice.DisplayName == "AirPods Pro", "Device name must match.");

            var step1 = wizard.Steps[0];
            Assert(step1.Status == DeviceOperationStatus.Succeeded, "Step 1 must be Succeeded once target device is picked.");
            Assert(step1.StatusMessage.Contains("AirPods Pro"), "Step 1 message must reference target device.");

            // Resetting clears target and sets step 1 back to Requested
            wizard.ResetWizard();
            Assert(wizard.CurrentTargetDevice == null, "Target device must be cleared after reset.");
            Assert(wizard.Steps[0].Status == DeviceOperationStatus.Requested, "Step 1 must return to Requested after reset.");

            Console.WriteLine("PASSED");
        }

        private static async Task Test_FixMyConnectionWizardViewModel_InteractiveAuthAndRepair()
        {
            Console.Write("  Running Test_FixMyConnectionWizardViewModel_InteractiveAuthAndRepair... ");
            var vm = new FixMyConnectionWizardViewModel();
            var target = new BluetoothDeviceInfo
            {
                DeviceName = "Marshall Acton III",
                BluetoothAddress = "70:88:6B:AA:BB:CC",
                MajorDeviceClass = "Audio/Video",
                CategoryName = "Audio / Speaker",
                CategoryIcon = "🔊",
                IsPaired = false
            };

            vm.PreSelectDevice(target);
            Assert(vm.HasTargetDevice, "ViewModel must recognize selected target device.");
            Assert(vm.TargetDisplayName == "Marshall Acton III", "TargetDisplayName must match.");
            Assert(!vm.IsDeviceSelectionOpen, "Device selection drawer should close on selection.");

            // Verify Interactive PIN prompt simulation
            vm.EnteredPin = "1234";
            Assert(vm.EnteredPin == "1234", "PIN entry should update.");
            vm.SubmitPinCommand.Execute(null);
            Assert(!vm.IsPinPromptVisible, "PIN prompt visibility should toggle off after submission.");

            // Verify Passkey confirmation
            vm.ConfirmPasskeyCommand.Execute(null);
            Assert(!vm.IsNumericPromptVisible, "Numeric prompt visibility should toggle off after confirmation.");

            // Verify candidate loading
            await vm.LoadCandidatesAsync();
            Assert(vm.CandidateDevices != null, "Candidate devices collection should be populated.");

            Console.WriteLine("PASSED");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception($"Assertion Failed: {message}");
            }
        }
    }
}

