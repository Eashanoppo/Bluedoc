using System.Threading;
using System.Threading.Tasks;
using BlueDoc.Core.Enums;
using BlueDoc.Core.Models;

namespace BlueDoc.Diagnostics.Rules
{
    public class HardwareStatusRule : IDiagnosticRule
    {
        public string RuleId => "RULE_HARDWARE_STATUS";
        public string RuleName => "Adapter Hardware Status";
        public string Category => "Hardware Diagnostics";
        public string Description => "Inspects device node status flags and problem codes reported by the Windows Configuration Manager.";

        public Task<DiagnosticResult> EvaluateAsync(DiagnosticContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = new DiagnosticResult
            {
                RuleId = RuleId,
                TestName = RuleName,
                Category = Category,
                Confidence = "Confirmed Fact"
            };

            var adapter = context.PrimaryAdapter;
            if (adapter == null || !adapter.IsPresent)
            {
                result.Status = DiagnosticStatus.NotSupported;
                result.Summary = "Skipped: No Bluetooth adapter present to inspect.";
                result.TechnicalDetails = "Cannot inspect device status without an enumerated adapter.";
                result.RecommendedAction = "Run adapter detection test first.";
                return Task.FromResult(result);
            }

            if (adapter.Status == AdapterStatus.Operational && adapter.ProblemCode == 0)
            {
                result.Status = DiagnosticStatus.Passed;
                result.Summary = $"Adapter is operational: {adapter.FriendlyName}";
                result.TechnicalDetails = "Windows Configuration Manager reports CR_SUCCESS with no device problem flags (Problem Code 0).";
                result.Evidence = $"Device Status: Operational, ProblemCode: 0, DeviceInstanceId: '{adapter.DeviceInstanceId}'";
                result.RecommendedAction = "No action required. Hardware device is enabled and functioning normally.";
            }
            else if (adapter.Status == AdapterStatus.Disabled || adapter.ProblemCode == 22)
            {
                result.Status = DiagnosticStatus.Warning;
                result.Summary = "Adapter is disabled in Windows Device Manager (Code 22).";
                result.TechnicalDetails = "Windows reports problem code 22 (CM_PROB_DISABLED). The device driver is loaded but the hardware node is administratively disabled.";
                result.Evidence = $"ProblemCode: 22 (CM_PROB_DISABLED), Status: Disabled";
                result.PossibleCause = "The adapter was disabled by the user, a power-management utility, or automated policy.";
                result.RecommendedAction = "Enable the Bluetooth adapter via Safe Repair Center or Windows Device Manager.";
                result.RequiresAdmin = true;
            }
            else if (adapter.ProblemCode == 10)
            {
                result.Status = DiagnosticStatus.Failed;
                result.Summary = "Device failed to start (Code 10).";
                result.TechnicalDetails = "Windows reports problem code 10 (CM_PROB_FAILED_START). The driver failed to initialize the hardware communication interface or firmware.";
                result.Evidence = $"ProblemCode: 10 (CM_PROB_FAILED_START)";
                result.PossibleCause = "1. Corrupted driver installation.\n2. Power interruption during wake/sleep.\n3. Incompatible driver update.\n4. USB port power suspension.";
                result.RecommendedAction = "Run 'Provision Bluetooth Silicon Firmware' in Safe Repair Center to stage certified binary firmware or use 'Rollback to Microsoft Generic Driver'.";
                result.RequiresAdmin = true;
            }
            else if (adapter.ProblemCode == 43)
            {
                result.Status = DiagnosticStatus.Failed;
                result.Summary = "Device stopped due to errors (Code 43).";
                result.TechnicalDetails = "Windows reports problem code 43 (CM_PROB_FAILED_POST_START). The driver reported a communication fault, controller crash, or USB bus reset.";
                result.Evidence = $"ProblemCode: 43 (CM_PROB_FAILED_POST_START)";
                result.PossibleCause = "1. Bluetooth controller firmware lockup.\n2. USB bus communication fault.\n3. Physical USB port power suspension or loose contact.";
                result.RecommendedAction = "Unplug and replug the USB Bluetooth dongle or perform a full system shutdown to reset adapter power.";
                result.RequiresAdmin = true;
            }
            else
            {
                result.Status = DiagnosticStatus.Failed;
                result.Summary = $"Device reported problem code {adapter.ProblemCode}: {adapter.ProblemDescription}";
                result.TechnicalDetails = $"Configuration Manager Problem Code: {adapter.ProblemCode}";
                result.Evidence = $"ProblemCode: {adapter.ProblemCode}, Description: '{adapter.ProblemDescription}'";
                result.PossibleCause = "Windows encountered an error while managing this Bluetooth adapter.";
                result.RecommendedAction = "Inspect driver details in Driver Assistant and review Windows event logs.";
                result.RequiresAdmin = true;
            }

            return Task.FromResult(result);
        }
    }
}

