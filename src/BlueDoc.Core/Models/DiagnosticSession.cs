using System;
using System.Collections.Generic;
using BlueDoc.Core.Enums;

namespace BlueDoc.Core.Models
{
    public class DiagnosticSession
    {
        public Guid SessionId { get; set; } = Guid.NewGuid();
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public OsVersionInfo OsVersion { get; set; } = new OsVersionInfo();
        public BluetoothAdapterInfo AdapterInfo { get; set; }
        public ServiceInfo BluetoothService { get; set; }
        public List<DiagnosticResult> Results { get; set; } = new List<DiagnosticResult>();

        public DiagnosticStatus OverallStatus
        {
            get
            {
                bool hasFailure = false;
                bool hasWarning = false;
                bool hasPassed = false;

                foreach (var res in Results)
                {
                    if (res.Status == DiagnosticStatus.Failed) hasFailure = true;
                    else if (res.Status == DiagnosticStatus.Warning) hasWarning = true;
                    else if (res.Status == DiagnosticStatus.Passed) hasPassed = true;
                }

                if (hasFailure) return DiagnosticStatus.Failed;
                if (hasWarning) return DiagnosticStatus.Warning;
                if (hasPassed) return DiagnosticStatus.Passed;
                return DiagnosticStatus.Inconclusive;
            }
        }
    }
}

