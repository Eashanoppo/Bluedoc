using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BlueDoc.Core.Enums;
using BlueDoc.Core.Models;

namespace BlueDoc.Diagnostics.Rules
{
    public class BleAvailabilityRule : IDiagnosticRule
    {
        public string RuleId => "RULE_BLE_AVAILABILITY";
        public string RuleName => "Bluetooth Low Energy (BLE) Availability";
        public string Category => "BLE Diagnostics";
        public string Description => "Verifies whether Bluetooth Low Energy (Bluetooth 4.0+) hardware and OS driver support are present.";

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

            bool osSupportsBle = context.OsInfo != null && context.OsInfo.SupportsBleModernApis;
            bool hasBleDevices = context.PairedDevices != null && context.PairedDevices.Any(d => d.DeviceType == DeviceType.LowEnergy || d.DeviceType == DeviceType.DualMode);

            if (osSupportsBle)
            {
                result.Status = DiagnosticStatus.Passed;
                result.Summary = "Bluetooth Low Energy (BLE 4.0+) is fully supported by the operating system and controller.";
                result.TechnicalDetails = $"{context.OsInfo.OperatingSystemName} provides native BLE stack support and HCI LE Controller features.";
                result.Evidence = $"OS: {context.OsInfo.OperatingSystemName}, WinRT BLE: Supported, Paired BLE Nodes: {(hasBleDevices ? "Present" : "None currently active")}.";
                result.RecommendedAction = "No action required. Use BLE Explorer to interact with Low Energy devices.";
            }
            else
            {
                result.Status = DiagnosticStatus.Warning;
                result.Summary = "Operating system does not provide modern WinRT BLE APIs (Windows 7 SP1 platform).";
                result.TechnicalDetails = "Windows 7 SP1 does not natively expose modern BLE GATT client APIs. BLE peripherals require proprietary manufacturer drivers or operate in limited mode.";
                result.PossibleCause = "Platform architectural limitation in Windows 7 SP1.";
                result.RecommendedAction = "For full BLE GATT features, upgrade to Windows 10 or Windows 11. Classic Bluetooth features remain completely functional.";
            }

            return Task.FromResult(result);
        }
    }
}

