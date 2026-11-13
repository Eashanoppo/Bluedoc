using System;

namespace BlueDoc.Core.Models
{
    public class RepairHistoryRecord
    {
        public long Id { get; set; }
        public string RepairKey { get; set; } = string.Empty;
        public string ActionTitle { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string TargetComponent { get; set; } = string.Empty;
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
        public bool RequiredAdmin { get; set; }
        public string Status { get; set; } = "Pending"; // Success, Failed, Cancelled
        public string VerificationResult { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;

        public string LocalTimestampDisplay => TimestampUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
    }
}
