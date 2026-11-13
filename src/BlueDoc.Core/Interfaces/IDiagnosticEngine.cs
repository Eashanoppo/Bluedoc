using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BlueDoc.Core.Models;

namespace BlueDoc.Core.Interfaces
{
    public interface IDiagnosticEngine
    {
        Task<DiagnosticSession> RunFullDiagnosticsAsync(IProgress<DiagnosticResult> progress = null, CancellationToken cancellationToken = default(CancellationToken));
        Task<DiagnosticResult> RunSingleTestAsync(string ruleId, CancellationToken cancellationToken = default(CancellationToken));
        IReadOnlyList<string> GetAvailableRuleIds();
        IReadOnlyList<DiagnosticResult> GetInitialTestList();
    }
}

