using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Security;
using BlueDoc.Core.Models;

namespace BlueDoc.Infrastructure.EventLogs
{
    public class BluetoothEventAnalyzer
    {
        private static readonly HashSet<string> BluetoothSources = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "BTHUSB",
            "bthserv",
            "Microsoft-Windows-BTHUSB",
            "Microsoft-Windows-Bluetooth-BthLEPrepaired",
            "Microsoft-Windows-Bluetooth-Policy",
            "BthMini",
            "Bluetooth"
        };

        public List<BluetoothEventRecord> QueryBluetoothEvents(int maxRecords = 100, int lookbackDays = 14)
        {
            var results = new List<BluetoothEventRecord>();

            try
            {
                // Try modern EventLogReader first (supported on Win7 SP1, Win10, Win11)
                DateTime cutoff = DateTime.UtcNow.AddDays(-lookbackDays);
                string queryString = "*[System[TimeCreated[@SystemTime >= '" + cutoff.ToString("o") + "']]]";
                var query = new EventLogQuery("System", PathType.LogName, queryString)
                {
                    ReverseDirection = true
                };

                using (var reader = new EventLogReader(query))
                {
                    EventRecord record;
                    int count = 0;
                    while ((record = reader.ReadEvent()) != null && count < maxRecords * 5)
                    {
                        using (record)
                        {
                            if (record.ProviderName != null && BluetoothSources.Contains(record.ProviderName))
                            {
                                string level = record.Level.HasValue ? GetLevelName(record.Level.Value) : "Information";
                                string message = "";
                                try { message = record.FormatDescription(); } catch { }
                                if (string.IsNullOrWhiteSpace(message))
                                {
                                    message = $"Bluetooth event {record.Id} logged by provider {record.ProviderName}.";
                                }

                                var interpreted = InterpretEvent(record.ProviderName, record.Id, level, message);
                                interpreted.TimeCreated = record.TimeCreated;
                                results.Add(interpreted);
                                count++;
                                if (results.Count >= maxRecords) break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException || ex is SecurityException || ex is EventLogNotFoundException || ex is EventLogException)
            {
                // Fallback to legacy System.Diagnostics.EventLog
                try
                {
                    results.AddRange(QueryLegacyEventLog(maxRecords, lookbackDays));
                }
                catch
                {
                    // If event logs are completely restricted by corporate policy or disabled
                }
            }
            catch (Exception)
            {
                // General fallback
                try
                {
                    results.AddRange(QueryLegacyEventLog(maxRecords, lookbackDays));
                }
                catch { }
            }

            return GroupRecurringEvents(results);
        }

        private List<BluetoothEventRecord> QueryLegacyEventLog(int maxRecords, int lookbackDays)
        {
            var results = new List<BluetoothEventRecord>();
            DateTime cutoff = DateTime.UtcNow.AddDays(-lookbackDays);

            if (!EventLog.Exists("System")) return results;

            using (var log = new EventLog("System"))
            {
                int total = log.Entries.Count;
                for (int i = total - 1; i >= 0 && results.Count < maxRecords; i--)
                {
                    try
                    {
                        var entry = log.Entries[i];
                        if (entry.TimeGenerated < cutoff) break;

                        if (BluetoothSources.Contains(entry.Source))
                        {
                            string level = entry.EntryType.ToString();
                            var item = InterpretEvent(entry.Source, (int)(entry.InstanceId & 0xFFFF), level, entry.Message);
                            item.TimeCreated = entry.TimeGenerated;
                            results.Add(item);
                        }
                    }
                    catch { }
                }
            }

            return results;
        }

        public BluetoothEventRecord InterpretEvent(string provider, int eventId, string level, string rawMessage)
        {
            var record = new BluetoothEventRecord
            {
                ProviderName = provider,
                EventId = eventId,
                LevelDisplayName = level,
                RawMessage = rawMessage
            };

            // Heuristic analysis based on known Windows Bluetooth event IDs
            if (string.Equals(provider, "BTHUSB", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(provider, "Microsoft-Windows-BTHUSB", StringComparison.OrdinalIgnoreCase))
            {
                switch (eventId)
                {
                    case 3:
                        record.HumanAnalysis = "A command sent to the adapter timed out. The Bluetooth adapter did not respond within the expected timeframe.";
                        record.PossibleCause = "USB power suspend issue, controller firmware lockup, or temporary bus transfer congestion.";
                        record.RecommendedTroubleshooting = "Disable USB selective suspend for this adapter, or disable and re-enable the device in Device Manager.";
                        break;
                    case 5:
                        record.HumanAnalysis = "The local Bluetooth adapter failed an initialization sequence.";
                        record.PossibleCause = "Firmware initialization failure or driver handshake timeout during system resume/boot.";
                        record.RecommendedTroubleshooting = "Perform a clean restart or update the adapter driver from the official manufacturer website.";
                        break;
                    case 17:
                        record.HumanAnalysis = "The adapter did not receive an expected HCI command complete event.";
                        record.PossibleCause = "Bluetooth controller became unresponsive or dropped HCI packets.";
                        record.RecommendedTroubleshooting = "Cycle the Bluetooth radio off and on in Windows Settings.";
                        break;
                    case 18:
                        record.HumanAnalysis = "An unexpected HCI event was received from the Bluetooth adapter.";
                        record.PossibleCause = "Minor protocol desynchronization between driver and controller firmware.";
                        record.RecommendedTroubleshooting = "No immediate action required unless Bluetooth connection drops occur frequently.";
                        break;
                    case 34:
                        record.HumanAnalysis = "The adapter returned an unexpected status code during communication.";
                        record.PossibleCause = "Driver/firmware version mismatch or radio interference during communication.";
                        record.RecommendedTroubleshooting = "Verify the latest driver package provided by the hardware manufacturer.";
                        break;
                    default:
                        record.HumanAnalysis = $"BTHUSB event {eventId} logged.";
                        record.PossibleCause = "Standard USB Bluetooth driver operational notification.";
                        record.RecommendedTroubleshooting = "Review diagnostic session results to correlate with current hardware health.";
                        break;
                }
            }
            else if (string.Equals(provider, "bthserv", StringComparison.OrdinalIgnoreCase))
            {
                record.HumanAnalysis = $"Bluetooth Support Service notification (Event ID {eventId}).";
                if (string.Equals(level, "Error", StringComparison.OrdinalIgnoreCase))
                {
                    record.PossibleCause = "The Bluetooth Support Service encountered an internal exception or stopped unexpectedly.";
                    record.RecommendedTroubleshooting = "Use Safe Repair Center to restart the Bluetooth Support Service.";
                }
                else
                {
                    record.PossibleCause = "Normal service lifecycle or state transition.";
                    record.RecommendedTroubleshooting = "No action necessary.";
                }
            }
            else if (provider.IndexOf("BthLEPrepaired", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                record.HumanAnalysis = "Bluetooth Low Energy pre-paired device event.";
                record.PossibleCause = "BLE device state change, advertisement reception, or GATT pairing event.";
                record.RecommendedTroubleshooting = "Check BLE Explorer or Bluetooth Devices to review device connection status.";
            }
            else
            {
                record.HumanAnalysis = $"Event {eventId} recorded by {provider}.";
                record.PossibleCause = "System Bluetooth component operational activity.";
                record.RecommendedTroubleshooting = "Review diagnostic test results if experiencing connectivity problems.";
            }

            return record;
        }

        private List<BluetoothEventRecord> GroupRecurringEvents(List<BluetoothEventRecord> records)
        {
            var grouped = new List<BluetoothEventRecord>();

            foreach (var rec in records)
            {
                var existing = grouped.FirstOrDefault(g => 
                    g.EventId == rec.EventId && 
                    string.Equals(g.ProviderName, rec.ProviderName, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(g.LevelDisplayName, rec.LevelDisplayName, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    existing.OccurrenceCount++;
                    if (rec.TimeCreated.HasValue && (!existing.TimeCreated.HasValue || rec.TimeCreated > existing.TimeCreated))
                    {
                        existing.TimeCreated = rec.TimeCreated;
                    }
                }
                else
                {
                    rec.OccurrenceCount = 1;
                    grouped.Add(rec);
                }
            }

            return grouped;
        }

        private string GetLevelName(byte level)
        {
            switch (level)
            {
                case 1: return "Critical";
                case 2: return "Error";
                case 3: return "Warning";
                case 4: return "Information";
                default: return "Information";
            }
        }
    }
}
