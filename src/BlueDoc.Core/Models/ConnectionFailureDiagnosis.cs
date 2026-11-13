using System;
using System.Collections.Generic;
using BlueDoc.Core.Enums;

namespace BlueDoc.Core.Models
{
    public class ConnectionDiagnosticCheckItem
    {
        public string Name { get; set; } = string.Empty;
        public DiagnosticStatus Status { get; set; } = DiagnosticStatus.Passed;
        public string Evidence { get; set; } = string.Empty;
        public ConfidenceLevel Confidence { get; set; } = ConfidenceLevel.High;
    }

    public class ConnectionFailureDiagnosis
    {
        public BluetoothDeviceInfo TargetDevice { get; set; }
        public string PrimaryRootCause { get; set; } = string.Empty;
        public string DetailedEvidence { get; set; } = string.Empty;
        public ConfidenceLevel Confidence { get; set; } = ConfidenceLevel.High;
        public string SuggestedRepairKey { get; set; } = string.Empty;
        public string SuggestedRepairTitle { get; set; } = string.Empty;
        public string SuggestedRepairExplanation { get; set; } = string.Empty;
        public List<ConnectionDiagnosticCheckItem> CheckItems { get; set; } = new List<ConnectionDiagnosticCheckItem>();

        public bool IsAudioEndpointIssue { get; set; }
        public bool IsAuthenticationIssue { get; set; }
        public bool IsAdapterIssue { get; set; }
        public bool IsStalePairingIssue { get; set; }
        public bool IsRangeOrPowerIssue { get; set; }
        public bool IsDriverIssue { get; set; }
        public DateTime DiagnosedAtUtc { get; set; } = DateTime.UtcNow;
    }
}

