using System;

namespace BlueDoc.Core.Models
{
    public class ConnectionHistoryRecord
    {
        public long Id { get; set; }
        public string DeviceAddress { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public string DeviceType { get; set; } = "Unknown";
        public string EventType { get; set; } = "Discovered"; // Discovered, Paired, Connected, Disconnected, Failed
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
        public string Details { get; set; } = string.Empty;
        public bool IsSuccess { get; set; } = true;
        public Guid? DiagnosticSessionId { get; set; }

        public string LocalTimestampDisplay => TimestampUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
    }
}
