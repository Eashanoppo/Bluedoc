using System;
using System.Collections.Generic;

namespace BlueDoc.Application.Cloud
{
    public class FleetMachineRecord
    {
        public string MachineId { get; set; } = Guid.NewGuid().ToString();
        public string Hostname { get; set; } = string.Empty;
        public string OrganizationId { get; set; } = "UNLEFT-CORP";
        public string Department { get; set; } = "Engineering";
        public string OperatingSystem { get; set; } = "Windows 11 (64-bit)";
        public string PrimaryAdapter { get; set; } = "Realtek Bluetooth Adapter";
        public string ChipsetManufacturer { get; set; } = "Realtek";
        public string DriverVersion { get; set; } = "1.1072.2312.2501";
        public string HealthStatus { get; set; } = "Healthy"; // Healthy, Warning, Critical
        public double BenchmarkScore { get; set; } = 90.0;
        public DateTime LastHeartbeatUtc { get; set; } = DateTime.UtcNow;
        public List<string> FailureAlerts { get; set; } = new List<string>();
        public bool IsCompliant { get; set; } = true;
        public string ComplianceIssue { get; set; } = "Fully Compliant";

        public string LocalHeartbeatDisplay => LastHeartbeatUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
    }

    public class FleetSummaryMetrics
    {
        public int TotalEndpoints { get; set; }
        public int HealthyCount { get; set; }
        public int WarningCount { get; set; }
        public int CriticalCount { get; set; }
        public double FleetAverageScore { get; set; }
        public double CompliancePercentage { get; set; }
        public List<FleetMachineRecord> Machines { get; set; } = new List<FleetMachineRecord>();
    }
}

