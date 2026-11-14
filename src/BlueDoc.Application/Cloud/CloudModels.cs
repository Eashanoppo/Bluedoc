using System;
using System.Collections.Generic;

namespace BlueDoc.Application.Cloud
{
    public class TenantRegistrationResult
    {
        public bool Success { get; set; }
        public string TenantId { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty; // Shown once!
        public string Message { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class CloudAlertList
    {
        public int TotalCount { get; set; }
        public int UnacknowledgedCount { get; set; }
        public List<CloudAlert> Alerts { get; set; } = new List<CloudAlert>();
    }

    public class CloudAlert
    {
        public string AlertId { get; set; } = string.Empty;
        public string MachineId { get; set; } = string.Empty;
        public string Hostname { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty; // Critical|Warning|Info
        public string RuleId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Detail { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool Acknowledged { get; set; }
    }
}

