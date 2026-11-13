using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlueDoc.Core.Enums;
using BlueDoc.Core.Models;
using BlueDoc.Infrastructure.EventLogs;

namespace BlueDoc.Application.Services
{
    public class EventCorrelationResult
    {
        public bool HasCorrelation { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public List<string> CorrelatedFindings { get; set; } = new List<string>();
        public string RecommendedNextStep { get; set; } = string.Empty;
    }

    public class EventAnalysisService
    {
        private readonly BluetoothEventAnalyzer _analyzer;

        public EventAnalysisService(BluetoothEventAnalyzer analyzer = null)
        {
            _analyzer = analyzer ?? new BluetoothEventAnalyzer();
        }

        public Task<List<BluetoothEventRecord>> GetRecentBluetoothEventsAsync(int maxRecords = 50, int lookbackDays = 14)
        {
            return Task.Run(() => _analyzer.QueryBluetoothEvents(maxRecords, lookbackDays));
        }

        public EventCorrelationResult CorrelateWithDiagnostics(
            IReadOnlyList<DiagnosticResult> diagnosticResults, 
            IReadOnlyList<BluetoothEventRecord> events)
        {
            var result = new EventCorrelationResult();

            if (events == null || events.Count == 0)
            {
                result.HasCorrelation = false;
                result.Summary = "No recent Bluetooth system event log records found in the lookback window.";
                return result;
            }

            var errorEvents = events.Where(e => string.Equals(e.LevelDisplayName, "Error", StringComparison.OrdinalIgnoreCase) ||
                                                string.Equals(e.LevelDisplayName, "Critical", StringComparison.OrdinalIgnoreCase)).ToList();

            var timeoutEvents = events.Where(e => e.EventId == 3 || e.EventId == 5 || e.EventId == 17).ToList();

            if (diagnosticResults != null)
            {
                foreach (var diag in diagnosticResults)
                {
                    if (diag.Status == DiagnosticStatus.Failed || diag.Status == DiagnosticStatus.Warning)
                    {
                        if (diag.RuleId == "BTH-002" || diag.RuleId == "BTH-003" || diag.RuleId == "BTH-004")
                        {
                            if (timeoutEvents.Count > 0)
                            {
                                result.CorrelatedFindings.Add(
                                    $"Hardware/Driver diagnostic ({diag.TestName}) status '{diag.Status}' correlates with {timeoutEvents.Count} BTHUSB driver timeout event(s) recorded in Windows event log.");
                            }
                        }

                        if (diag.RuleId == "BTH-005")
                        {
                            var bthservEvents = events.Where(e => string.Equals(e.ProviderName, "bthserv", StringComparison.OrdinalIgnoreCase)).ToList();
                            if (bthservEvents.Count > 0)
                            {
                                result.CorrelatedFindings.Add(
                                    $"Bluetooth Support Service diagnostic status '{diag.Status}' is corroborated by {bthservEvents.Count} service lifecycle event(s).");
                            }
                        }
                    }
                }
            }

            if (result.CorrelatedFindings.Count > 0)
            {
                result.HasCorrelation = true;
                result.Title = "Diagnostic & System Event Correlation Identified";
                result.Summary = $"Found {result.CorrelatedFindings.Count} corroborating event log pattern(s) that match active diagnostic findings.";
                result.RecommendedNextStep = "Review the Safe Repair Center to restart services or reload the adapter driver.";
            }
            else if (errorEvents.Count > 0)
            {
                result.HasCorrelation = true;
                result.Title = "Historical Event Log Warnings Detected";
                result.Summary = $"Windows Event Log recorded {errorEvents.Count} Bluetooth error/warning entry(ies), although current real-time diagnostics may currently report healthy.";
                result.RecommendedNextStep = "Inspect individual event records below to review historical controller resets or bus timeouts.";
            }
            else
            {
                result.HasCorrelation = false;
                result.Title = "Event Logs Clean";
                result.Summary = "No active error or timeout correlations detected between event logs and system diagnostics.";
                result.RecommendedNextStep = "System event log indicates stable Bluetooth controller and service communication.";
            }

            return result;
        }

        public string GenerateCsvAuditLog(IEnumerable<BluetoothEventRecord> events)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Timestamp (UTC),Local Time,Provider,Event ID,Severity,Occurrences,Human Analysis,Possible Cause,Recommended Troubleshooting,Raw Message");

            foreach (var e in events)
            {
                string timeUtc = e.TimeCreated.HasValue ? e.TimeCreated.Value.ToUniversalTime().ToString("o") : "";
                string localTime = EscapeCsv(e.LocalTimeDisplay);
                string provider = EscapeCsv(e.ProviderName);
                string id = e.EventId.ToString();
                string level = EscapeCsv(e.LevelDisplayName);
                string count = e.OccurrenceCount.ToString();
                string analysis = EscapeCsv(e.HumanAnalysis);
                string cause = EscapeCsv(e.PossibleCause);
                string reco = EscapeCsv(e.RecommendedTroubleshooting);
                string raw = EscapeCsv(e.RawMessage);

                sb.AppendLine($"{timeUtc},{localTime},{provider},{id},{level},{count},{analysis},{cause},{reco},{raw}");
            }

            return sb.ToString();
        }

        private static string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }
            return value;
        }
    }
}
