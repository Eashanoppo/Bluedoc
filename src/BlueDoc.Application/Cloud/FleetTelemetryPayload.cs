using System;
using System.Collections.Generic;

namespace BlueDoc.Application.Cloud
{
    public class FleetTelemetryPayload
    {
        public string PayloadId { get; set; } = Guid.NewGuid().ToString();
        public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
        public string Hostname { get; set; } = Environment.MachineName;
        public string MachineGuid { get; set; } = string.Empty;
        public string OperatingSystem { get; set; } = string.Empty;
        public string OsBuild { get; set; } = string.Empty;
        public string Architecture { get; set; } = Environment.Is64BitOperatingSystem ? "x64" : "x86";
        public string AdapterName { get; set; } = "None";
        public string Manufacturer { get; set; } = "Unknown";
        public string HardwareId { get; set; } = string.Empty;
        public string DriverVersion { get; set; } = string.Empty;
        public string OverallHealthStatus { get; set; } = "Unknown";
        public int TotalTests { get; set; }
        public int PassedTests { get; set; }
        public int WarningTests { get; set; }
        public int FailedTests { get; set; }
        public List<string> FailedRuleSummaries { get; set; } = new List<string>();
        public double? BenchmarkScore { get; set; }
        public string BenchmarkRating { get; set; } = string.Empty;
    }

    public class SaasSyncResult
    {
        public bool Success { get; set; }
        public int HttpStatusCode { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime SyncedAt { get; set; } = DateTime.UtcNow;
    }
}

