using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BlueDoc.Core.Enums;
using BlueDoc.Core.Models;

namespace BlueDoc.Diagnostics.Rules
{
    public class PairingReadinessRule : IDiagnosticRule
    {
        public string RuleId => "RULE_PAIRING_READINESS";
        public string RuleName => "Pairing & Security Subsystem Readiness";
        public string Category => "Pairing Diagnostics";
        public string Description => "Checks the Windows Bluetooth Security Manager and authentication subsystem readiness.";

        public Task<DiagnosticResult> EvaluateAsync(DiagnosticContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = new DiagnosticResult
            {
                RuleId = RuleId,
                TestName = RuleName,
                Category = Category,
                Confidence = "Strong Indicator",
                ExecutedAt = DateTime.UtcNow
            };

            if (context.CoreService == null || !context.CoreService.IsRunning)
            {
                result.Status = DiagnosticStatus.Failed;
                result.Summary = "Pairing subsystem is unavailable: Bluetooth Support Service (bthserv) is stopped.";
                result.PossibleCause = "Core service responsible for PIN authentication and key storage is inactive.";
                result.RecommendedAction = "Start Bluetooth Support Service in Safe Repair Center.";
                result.RequiresAdmin = true;
                return Task.FromResult(result);
            }

            int pairedCount = context.PairedDevices?.Count ?? 0;
            result.Status = DiagnosticStatus.Passed;
            result.Summary = $"Pairing & Security Subsystem is ready ({pairedCount} device(s) currently paired).";
            result.TechnicalDetails = $"Windows bthserv is active. Security Manager handles SSP (Secure Simple Pairing) and legacy PIN exchange.";
            result.Evidence = $"Service 'bthserv' running. Registered paired nodes: {pairedCount}.";
            result.RecommendedAction = "No action required. Ready to pair new devices.";
            return Task.FromResult(result);
        }
    }
}

