using System;
using System.Collections.Generic;
using BlueDoc.Core.Enums;

namespace BlueDoc.Core.Models
{
    public class StartupHealthIssue
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = "Warning"; // Critical, Warning, Advisory
        public string SuggestedRepairKey { get; set; } = string.Empty;
        public string ActionButtonText { get; set; } = "Resolve Now";
    }

    public class StartupHealthAssessment
    {
        public bool IsHealthy { get; set; } = true;
        public string OverallMessage { get; set; } = "All Bluetooth hardware, services, and drivers are operational.";
        public AdapterStatus AdapterStatus { get; set; } = AdapterStatus.Operational;
        public string PrimaryAdapterName { get; set; } = "Bluetooth Adapter";
        public bool RadioAvailable { get; set; } = true;
        public bool DriverOperational { get; set; } = true;
        public bool ServicesRunning { get; set; } = true;
        public string ApiReadiness { get; set; } = "Classic Win32 + BLE GATT Ready";
        public string OsName { get; set; } = "Windows";
        public List<StartupHealthIssue> ActionableIssues { get; set; } = new List<StartupHealthIssue>();
        public bool RequiresAttention => ActionableIssues.Count > 0;
        public DateTime AssessedAtUtc { get; set; } = DateTime.UtcNow;
    }
}

