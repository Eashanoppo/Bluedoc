using System;
using System.Threading;
using System.Threading.Tasks;
using BlueDoc.Core.Enums;
using BlueDoc.Core.Models;

namespace BlueDoc.Diagnostics.Rules
{
    public class BleGattSupportRule : IDiagnosticRule
    {
        public string RuleId => "RULE_BLE_GATT";
        public string RuleName => "BLE GATT Client & Protocol Support";
        public string Category => "BLE Diagnostics";
        public string Description => "Evaluates whether the system supports Generic Attribute Profile (GATT) client operations.";

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

            if (context.OsInfo != null && context.OsInfo.SupportsBleModernApis)
            {
                result.Status = DiagnosticStatus.Passed;
                result.Summary = "GATT Client and Attribute Protocol (ATT) APIs are fully supported.";
                result.TechnicalDetails = "Windows.Devices.Bluetooth.GenericAttributeProfile runtime framework is available on this system.";
                result.Evidence = $"Operating System: {context.OsInfo.OperatingSystemName} (Build {context.OsInfo.BuildNumber}). GATT Client: Ready.";
                result.RecommendedAction = "No action required. GATT service and characteristic inspection is operational.";
            }
            else
            {
                result.Status = DiagnosticStatus.NotSupported;
                result.Summary = "Advanced GATT operations are not supported on Windows 7 SP1.";
                result.TechnicalDetails = "The GATT client API was introduced in Windows 10 (Build 10240+). It is not available on Windows 7 SP1 kernel.";
                result.PossibleCause = "Target operating system does not support the Windows.Devices.Bluetooth runtime.";
                result.RecommendedAction = "Use Classic Bluetooth management or run BlueDoc on Windows 10/11 for GATT inspection.";
            }

            return Task.FromResult(result);
        }
    }
}

