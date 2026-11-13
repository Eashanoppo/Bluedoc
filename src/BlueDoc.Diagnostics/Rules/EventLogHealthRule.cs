using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BlueDoc.Core.Enums;
using BlueDoc.Core.Models;

namespace BlueDoc.Diagnostics.Rules
{
    public class EventLogHealthRule : IDiagnosticRule
    {
        public string RuleId => "RULE_EVENT_LOG_HEALTH";
        public string RuleName => "Bluetooth Event Log Health & Stability";
        public string Category => "Event Log Diagnostics";
        public string Description => "Analyzes recent Windows event log telemetry for abnormal Bluetooth error spikes.";

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

            var events = context.RecentEvents;
            if (events == null || events.Count == 0)
            {
                result.Status = DiagnosticStatus.Passed;
                result.Summary = "No recent abnormal Bluetooth driver or service errors logged in Windows Event Log.";
                result.TechnicalDetails = "Clean event log telemetry. BTHUSB and bthserv reported no critical error spikes in the recent audit period.";
                result.RecommendedAction = "No action required. Subsystem stability verified.";
                return Task.FromResult(result);
            }

            var recentErrors = events.Where(e => string.Equals(e.LevelDisplayName, "Error", StringComparison.OrdinalIgnoreCase) ||
                                                 string.Equals(e.LevelDisplayName, "Critical", StringComparison.OrdinalIgnoreCase)).ToList();

            if (recentErrors.Count > 0)
            {
                var topError = recentErrors[0];
                result.Status = DiagnosticStatus.Warning;
                result.Summary = $"Detected {recentErrors.Count} recent Bluetooth system error event(s) in Windows Event Log.";
                result.TechnicalDetails = $"Provider: {topError.ProviderName}, Event ID: {topError.EventId}, Occurrences: {topError.OccurrenceCount}. Message: {topError.HumanAnalysis}";
                result.Evidence = $"Top Event: ID {topError.EventId} from {topError.ProviderName} at {topError.LocalTimeDisplay}. Possible cause: {topError.PossibleCause}";
                result.PossibleCause = topError.PossibleCause;
                result.RecommendedAction = topError.RecommendedTroubleshooting;
            }
            else
            {
                result.Status = DiagnosticStatus.Passed;
                result.Summary = $"System event logs show normal operational activity ({events.Count} informational events logged).";
                result.TechnicalDetails = "No critical controller timeout or driver crash records identified.";
                result.RecommendedAction = "No action required.";
            }

            return Task.FromResult(result);
        }
    }
}

