using System.Threading;
using System.Threading.Tasks;
using BlueDoc.Core.Enums;
using BlueDoc.Core.Models;

namespace BlueDoc.Diagnostics.Rules
{
    public class OsCompatibilityRule : IDiagnosticRule
    {
        public string RuleId => "RULE_OS_COMPAT";
        public string RuleName => "Operating System Compatibility";
        public string Category => "System Compatibility";
        public string Description => "Verifies Windows operating system version, architecture, and platform Bluetooth API readiness.";

        public Task<DiagnosticResult> EvaluateAsync(DiagnosticContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            var os = context.OsInfo;
            var result = new DiagnosticResult
            {
                RuleId = RuleId,
                TestName = RuleName,
                Category = Category,
                Confidence = "Confirmed Fact"
            };

            if (os == null)
            {
                result.Status = DiagnosticStatus.Inconclusive;
                result.Summary = "Unable to determine operating system version.";
                result.PossibleCause = "Operating system query returned null.";
                result.RecommendedAction = "Verify Windows environment permissions.";
                return Task.FromResult(result);
            }

            if (os.Platform == WindowsPlatformType.Windows11)
            {
                result.Status = DiagnosticStatus.Passed;
                result.Summary = $"Supported: Windows 11 ({os.ArchitectureDisplay}, Build {os.BuildNumber})";
                result.TechnicalDetails = "Full support for modern Windows Bluetooth Runtime APIs, Win32 stack, BLE advertising, and GATT client/server operations.";
                result.Evidence = $"Major: {os.MajorVersion}, Minor: {os.MinorVersion}, Build: {os.BuildNumber}, Arch: {os.ArchitectureDisplay}";
                result.RecommendedAction = "No action required. Operating system fully supports all BlueDoc diagnostic and management features.";
            }
            else if (os.Platform == WindowsPlatformType.Windows10)
            {
                result.Status = DiagnosticStatus.Passed;
                result.Summary = $"Supported: Windows 10 ({os.ArchitectureDisplay}, Build {os.BuildNumber})";
                result.TechnicalDetails = "Full support for Windows 10 Bluetooth stack, Classic Bluetooth discovery, and BLE GATT operations.";
                result.Evidence = $"Major: {os.MajorVersion}, Minor: {os.MinorVersion}, Build: {os.BuildNumber}, Arch: {os.ArchitectureDisplay}";
                result.RecommendedAction = "No action required. Standard modern Bluetooth functionality is supported.";
            }
            else if (os.Platform == WindowsPlatformType.Windows7)
            {
                result.Status = DiagnosticStatus.Warning;
                result.Summary = $"Legacy Platform: Windows 7 ({os.ArchitectureDisplay})";
                result.TechnicalDetails = "Classic Bluetooth Win32 APIs, SetupAPI, and Service Control Manager are supported. Modern Windows Runtime BLE GATT APIs are not available on Windows 7.";
                result.Evidence = $"ServicePack: {os.ServicePack}, Build: {os.BuildNumber}, Arch: {os.ArchitectureDisplay}";
                result.PossibleCause = "Operating system is Windows 7 SP1. Windows Runtime APIs were introduced in Windows 8/10.";
                result.RecommendedAction = "Classic Bluetooth devices and adapter diagnostics can proceed normally. BLE Explorer is disabled on Windows 7.";
            }
            else
            {
                result.Status = DiagnosticStatus.Warning;
                result.Summary = $"Unverified Windows Version: {os.OperatingSystemName}";
                result.TechnicalDetails = $"Detected OS: {os.OperatingSystemName} Build {os.BuildNumber}.";
                result.Evidence = $"Major: {os.MajorVersion}, Minor: {os.MinorVersion}, Build: {os.BuildNumber}";
                result.PossibleCause = "Operating system version is outside standard BlueDoc targets (Windows 7 SP1, 10, 11).";
                result.RecommendedAction = "Use legacy Win32 Bluetooth mode.";
            }

            return Task.FromResult(result);
        }
    }
}

