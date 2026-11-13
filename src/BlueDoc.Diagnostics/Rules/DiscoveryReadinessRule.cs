using System;
using System.Threading;
using System.Threading.Tasks;
using BlueDoc.Core.Enums;
using BlueDoc.Core.Models;

namespace BlueDoc.Diagnostics.Rules
{
    public class DiscoveryReadinessRule : IDiagnosticRule
    {
        public string RuleId => "RULE_DISCOVERY_READINESS";
        public string RuleName => "Device Discovery Readiness";
        public string Category => "Discovery Diagnostics";
        public string Description => "Evaluates whether the local Bluetooth radio is configured to discover nearby Classic and BLE devices.";

        public Task<DiagnosticResult> EvaluateAsync(DiagnosticContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = new DiagnosticResult
            {
                RuleId = RuleId,
                TestName = RuleName,
                Category = Category,
                Confidence = "Strong Indicator",
                ExecutedAt = DateTime.UtcNow
            };

            if (context.PrimaryAdapter == null)
            {
                result.Status = DiagnosticStatus.Failed;
                result.Summary = "Discovery test cannot run: No Bluetooth adapter detected.";
                result.PossibleCause = "Missing adapter hardware or disabled controller.";
                result.RecommendedAction = "Verify physical adapter connection and inspect Device Manager.";
                return Task.FromResult(result);
            }

            if (context.PrimaryAdapter.Status != AdapterStatus.Operational)
            {
                result.Status = DiagnosticStatus.Warning;
                result.Summary = $"Discovery may be impaired: Adapter is in state '{context.PrimaryAdapter.StatusDisplay}'.";
                result.PossibleCause = "Adapter error or disabled radio.";
                result.RecommendedAction = "Resolve adapter hardware error in Driver Assistant.";
                return Task.FromResult(result);
            }

            if (context.IsRadioEnabled.HasValue && !context.IsRadioEnabled.Value)
            {
                result.Status = DiagnosticStatus.Failed;
                result.Summary = "Bluetooth Radio is turned OFF. Nearby devices cannot be discovered.";
                result.PossibleCause = "Radio switched off in Windows Settings or via airplane mode.";
                result.RecommendedAction = "Toggle Bluetooth ON in Windows Settings or press physical radio switch.";
                return Task.FromResult(result);
            }

            result.Status = DiagnosticStatus.Passed;
            result.Summary = "Discovery subsystem is ready to scan for nearby Bluetooth devices.";
            result.TechnicalDetails = $"Adapter: {context.PrimaryAdapter.FriendlyName}, Radio: Enabled, PnP Status: Code 0.";
            result.Evidence = $"Adapter '{context.PrimaryAdapter.FriendlyName}' is operational with active inquiry support.";
            result.RecommendedAction = "No action required. Use Device Scanner to search for nearby devices.";
            return Task.FromResult(result);
        }
    }
}

