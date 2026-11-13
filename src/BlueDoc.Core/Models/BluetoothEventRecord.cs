using System;

namespace BlueDoc.Core.Models
{
    public class BluetoothEventRecord
    {
        public long RecordId { get; set; }
        public DateTime? TimeCreated { get; set; }
        public string ProviderName { get; set; } = string.Empty;
        public int EventId { get; set; }
        public string LevelDisplayName { get; set; } = "Information"; // Error, Warning, Information
        public string RawMessage { get; set; } = string.Empty;
        public string HumanAnalysis { get; set; } = string.Empty;
        public string PossibleCause { get; set; } = string.Empty;
        public string RecommendedTroubleshooting { get; set; } = string.Empty;
        public int OccurrenceCount { get; set; } = 1;

        public string LocalTimeDisplay => TimeCreated.HasValue 
            ? TimeCreated.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") 
            : "Unknown";

        public string SeverityIcon
        {
            get
            {
                if (string.Equals(LevelDisplayName, "Error", StringComparison.OrdinalIgnoreCase)) return "❌";
                if (string.Equals(LevelDisplayName, "Warning", StringComparison.OrdinalIgnoreCase)) return "⚠️";
                return "ℹ️";
            }
        }
    }
}
