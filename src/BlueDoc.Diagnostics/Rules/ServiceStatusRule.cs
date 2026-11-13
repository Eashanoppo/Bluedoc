using System.Threading;
using System.Threading.Tasks;
using BlueDoc.Core.Enums;
using BlueDoc.Core.Models;

namespace BlueDoc.Diagnostics.Rules
{
    public class ServiceStatusRule : IDiagnosticRule
    {
        public string RuleId => "RULE_SERVICE_STATUS";
        public string RuleName => "Bluetooth Support Service Status";
        public string Category => "Services Diagnostics";
        public string Description => "Monitors the Windows Bluetooth Support Service (bthserv) execution and startup configuration.";

        public Task<DiagnosticResult> EvaluateAsync(DiagnosticContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = new DiagnosticResult
            {
                RuleId = RuleId,
                TestName = RuleName,
                Category = Category,
                Confidence = "Confirmed Fact"
            };

            var svc = context.CoreService;
            if (svc == null)
            {
                result.Status = DiagnosticStatus.Inconclusive;
                result.Summary = "Bluetooth Support Service information could not be queried.";
                result.PossibleCause = "Service Control Manager access error.";
                result.RecommendedAction = "Verify Windows services permissions.";
                return Task.FromResult(result);
            }

            if (!svc.Exists)
            {
                result.Status = DiagnosticStatus.Failed;
                result.Summary = "Bluetooth Support Service (bthserv) is NOT installed on this computer.";
                result.TechnicalDetails = "The service 'bthserv' does not exist in the Windows Service Control Manager registry.";
                result.Evidence = $"Service: bthserv, Exists: false, Error: {svc.ErrorMessage}";
                result.PossibleCause = "The Windows Bluetooth subsystem or core operating system components may be corrupted or stripped.";
                result.RecommendedAction = "Run 'sfc /scannow' or DISM health restore in an elevated Command Prompt to repair Windows system services.";
                result.RequiresAdmin = true;
                return Task.FromResult(result);
            }

            if (svc.IsRunning)
            {
                result.Status = DiagnosticStatus.Passed;
                result.Summary = $"Bluetooth Support Service is running ({svc.StartupType}).";
                result.TechnicalDetails = $"Service: {svc.DisplayName} ({svc.ServiceName}), Status: {svc.Status}, Startup: {svc.StartupType}";
                result.Evidence = $"ServiceName: '{svc.ServiceName}', Status: '{svc.Status}', StartupType: '{svc.StartupType}'";
                result.RecommendedAction = "No action required. Core Bluetooth service is active.";
            }
            else
            {
                if (string.Equals(svc.StartupType, "Disabled", global::System.StringComparison.OrdinalIgnoreCase))
                {
                    result.Status = DiagnosticStatus.Failed;
                    result.Summary = "Bluetooth Support Service is DISABLED and cannot start.";
                    result.TechnicalDetails = "Startup type is configured as 'Disabled'. Windows Bluetooth stack cannot run.";
                    result.Evidence = $"Status: {svc.Status}, StartupType: Disabled";
                    result.PossibleCause = "Service was intentionally or accidentally disabled by system configuration or optimization utilities.";
                    result.RecommendedAction = "Change startup type to 'Manual' or 'Automatic' and start the service in Safe Repair Center.";
                    result.RequiresAdmin = true;
                }
                else
                {
                    result.Status = DiagnosticStatus.Warning;
                    result.Summary = $"Bluetooth Support Service is stopped (Status: {svc.Status}).";
                    result.TechnicalDetails = $"Status: {svc.Status}, StartupType: {svc.StartupType}. In Windows, bthserv is usually configured as Manual (Trigger Start) and starts when Bluetooth is used.";
                    result.Evidence = $"Status: {svc.Status}, StartupType: {svc.StartupType}";
                    result.PossibleCause = "The service has not been triggered yet or stopped due to idle timeout.";
                    result.RecommendedAction = "Trigger a service restart using the Safe Repair Center.";
                    result.RequiresAdmin = true;
                }
            }

            return Task.FromResult(result);
        }
    }
}
