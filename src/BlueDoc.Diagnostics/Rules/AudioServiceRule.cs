using System;
using System.Threading;
using System.Threading.Tasks;
using BlueDoc.Core.Enums;
using BlueDoc.Core.Models;

namespace BlueDoc.Diagnostics.Rules
{
    public class AudioServiceRule : IDiagnosticRule
    {
        public string RuleId => "RULE_AUDIO_SERVICE";
        public string RuleName => "Bluetooth Audio Services Status";
        public string Category => "Services Diagnostics";
        public string Description => "Checks the state of Bluetooth Audio Gateway (BTAGService / BthHFSrv) and AVCTP services.";

        public Task<DiagnosticResult> EvaluateAsync(DiagnosticContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = new DiagnosticResult
            {
                RuleId = RuleId,
                TestName = RuleName,
                Category = Category,
                Confidence = "Confirmed Fact",
                ExecutedAt = DateTime.UtcNow
            };

            var audioSvc = context.AudioGatewayService;
            var avctpSvc = context.AvctpService;

            if (audioSvc == null && avctpSvc == null)
            {
                result.Status = DiagnosticStatus.Inconclusive;
                result.Summary = "Audio gateway service inspection was skipped.";
                result.RecommendedAction = "Verify Windows services.";
                return Task.FromResult(result);
            }

            bool hasRunningAudio = (audioSvc != null && audioSvc.IsRunning) || (avctpSvc != null && avctpSvc.IsRunning);
            bool audioDisabled = (audioSvc != null && string.Equals(audioSvc.StartupType, "Disabled", StringComparison.OrdinalIgnoreCase)) ||
                                 (avctpSvc != null && string.Equals(avctpSvc.StartupType, "Disabled", StringComparison.OrdinalIgnoreCase));

            if (audioDisabled)
            {
                result.Status = DiagnosticStatus.Warning;
                result.Summary = "Bluetooth Audio Gateway Service is DISABLED.";
                result.TechnicalDetails = $"Audio service '{audioSvc?.ServiceName}' is configured as Disabled. Bluetooth headphones and speakers may pair successfully but produce no sound.";
                result.PossibleCause = "Audio services were disabled by a user optimization tool or policy.";
                result.RecommendedAction = "Set Bluetooth Audio Gateway services to Manual or Automatic using Windows Services manager or Safe Repair Center.";
                result.RequiresAdmin = true;
                return Task.FromResult(result);
            }

            if (hasRunningAudio)
            {
                result.Status = DiagnosticStatus.Passed;
                result.Summary = "Bluetooth Audio Gateway services are active and operational.";
                result.TechnicalDetails = $"BTAGService / Avctp status: Running. Ready for A2DP / Hands-Free audio streams.";
                result.RecommendedAction = "No action required. Audio profiles supported.";
                return Task.FromResult(result);
            }

            // Both exist but are stopped (normal on modern Windows until an audio device actively connects)
            result.Status = DiagnosticStatus.Passed;
            result.Summary = "Bluetooth Audio Services are registered and ready to start on demand.";
            result.TechnicalDetails = $"Audio services configured as {audioSvc?.StartupType ?? avctpSvc?.StartupType ?? "Manual"} (Triggered on device connection).";
            result.RecommendedAction = "No action required.";
            return Task.FromResult(result);
        }
    }
}

