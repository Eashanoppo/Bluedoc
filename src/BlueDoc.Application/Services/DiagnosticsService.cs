using System;
using System.Threading;
using System.Threading.Tasks;
using BlueDoc.Core.Interfaces;
using BlueDoc.Core.Models;
using BlueDoc.Diagnostics.Engine;

namespace BlueDoc.Application.Services
{
    public class DiagnosticsService
    {
        private readonly IDiagnosticEngine _engine;
        private readonly IDiagnosticSessionRepository _sessionRepo;
        private DiagnosticSession _lastSession;

        public DiagnosticSession LastSession => _lastSession;

        public DiagnosticsService(IDiagnosticEngine engine = null, IDiagnosticSessionRepository sessionRepo = null)
        {
            _engine = engine ?? new DiagnosticEngine();
            _sessionRepo = sessionRepo;
        }

        public async Task<DiagnosticSession> RunDiagnosticsAsync(IProgress<DiagnosticResult> progress = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            _lastSession = await _engine.RunFullDiagnosticsAsync(progress, cancellationToken);
            if (_sessionRepo != null && _lastSession != null)
            {
                try
                {
                    await _sessionRepo.SaveSessionAsync(_lastSession);
                }
                catch
                {
                    // Non-fatal persistence logging
                }
            }
            return _lastSession;
        }

        public Task<DiagnosticResult> RunTestAsync(string ruleId, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _engine.RunSingleTestAsync(ruleId, cancellationToken);
        }

        public System.Collections.Generic.IReadOnlyList<DiagnosticResult> GetInitialTestList()
        {
            return _engine.GetInitialTestList();
        }
    }
}

