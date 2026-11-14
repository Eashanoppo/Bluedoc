using System;
using System.Collections.Generic;

namespace BlueDoc.Cloud.Api.Models
{
    public class FleetTelemetryPayload
    {
        public string PayloadId { get; set; } = Guid.NewGuid().ToString();
        public string MachineGuid { get; set; } = string.Empty;
        public string Hostname { get; set; } = string.Empty;
        public string OperatingSystem { get; set; } = string.Empty;
        public string OsBuild { get; set; } = string.Empty;
        public string Architecture { get; set; } = string.Empty;
        public string AdapterName { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = string.Empty;
        public string HardwareId { get; set; } = string.Empty;
        public string DriverVersion { get; set; } = string.Empty;
        public string OverallHealthStatus { get; set; } = string.Empty;
        public int TotalTests { get; set; }
        public int PassedTests { get; set; }
        public int WarningTests { get; set; }
        public int FailedTests { get; set; }
        public List<string> FailedRuleSummaries { get; set; } = new();
        public double? BenchmarkScore { get; set; }
        public string? BenchmarkRating { get; set; }
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    }
}

