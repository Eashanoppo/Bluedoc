using System.Threading;
using System.Threading.Tasks;
using BlueDoc.Core.Enums;
using BlueDoc.Core.Models;

namespace BlueDoc.Diagnostics.Rules
{
    public class RadioAvailabilityRule : IDiagnosticRule
    {
        public string RuleId => "RULE_RADIO_AVAIL";
        public string RuleName => "Bluetooth Radio Availability";
        public string Category => "Radio Diagnostics";
        public string Description => "Checks if the Bluetooth radio transceiver is enabled, powered, and accessible to Windows Bluetooth APIs.";

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
                result.Summary = "Skipped: No adapter detected.";
                return Task.FromResult(result);
            }

            if (context.IsRadioEnabled.HasValue && context.IsRadioEnabled.Value)
            {
                result.Status = DiagnosticStatus.Passed;
                result.Summary = "Bluetooth radio is active and responsive.";
                result.TechnicalDetails = !string.IsNullOrEmpty(adapter.RadioAddress)
                    ? $"Radio Address: {adapter.RadioAddress}, Radio Name: {adapter.RadioName}"
                    : "Radio handle opened successfully via Win32 Bluetooth API.";
                result.Evidence = $"IsRadioEnabled: true, RadioAddress: '{adapter.RadioAddress}', RadioName: '{adapter.RadioName}'";
                result.RecommendedAction = "No action required. The Bluetooth transceiver is ready for device discovery.";
            }
            else
            {
                result.Status = DiagnosticStatus.Warning;
                result.Summary = "Bluetooth radio appears disabled or unresponsive.";
                result.TechnicalDetails = "Win32 BluetoothFindFirstRadio could not find an active operational radio handle.";
                result.Evidence = "IsRadioEnabled: false";
                result.PossibleCause = "1. Airplane mode is enabled in Windows Settings.\n2. Bluetooth is toggled OFF in Windows Settings.\n3. Hardware wireless key (Fn + F-key) turned off the radio.";
                result.RecommendedAction = "Check Windows Action Center / Settings to toggle Bluetooth ON and disable Airplane Mode.";
            }

            return Task.FromResult(result);
        }
    }
}

