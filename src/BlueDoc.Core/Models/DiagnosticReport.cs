using System;
using System.Collections.Generic;

namespace BlueDoc.Core.Models
{
    public class DiagnosticReport
    {
        public Guid ReportId { get; set; } = Guid.NewGuid();
        public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
        public string ApplicationVersion { get; set; } = "1.0.0";
        public string MachineName { get; set; } = Environment.MachineName;
        public OsVersionInfo OsVersion { get; set; } = new OsVersionInfo();
        public BluetoothAdapterInfo Adapter { get; set; }
        public ServiceInfo Service { get; set; }
        public DiagnosticSession Session { get; set; }
        public List<BluetoothDeviceInfo> Devices { get; set; } = new List<BluetoothDeviceInfo>();
        public List<BluetoothEventRecord> RelevantEvents { get; set; } = new List<BluetoothEventRecord>();
        public List<string> RecommendedActions { get; set; } = new List<string>();
        public string OverallHealthSummary { get; set; } = string.Empty;
    }
}
