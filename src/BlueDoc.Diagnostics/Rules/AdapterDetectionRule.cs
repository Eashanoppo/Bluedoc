using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BlueDoc.Core.Enums;
using BlueDoc.Core.Models;

namespace BlueDoc.Diagnostics.Rules
{
    public class AdapterDetectionRule : IDiagnosticRule
    {
        public string RuleId => "RULE_ADAPTER_DETECT";
        public string RuleName => "Bluetooth Adapter Detection";
        public string Category => "Hardware Diagnostics";
        public string Description => "Checks whether a Bluetooth radio or adapter is physically present and enumerated by Windows.";

        public Task<DiagnosticResult> EvaluateAsync(DiagnosticContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = new DiagnosticResult
            {
                RuleId = RuleId,
                TestName = RuleName,
                Category = Category,
                Confidence = "Confirmed Fact"
            };

            var adapters = context.Adapters;
            if (adapters == null || adapters.Count == 0 || !adapters.Any(a => a.IsPresent))
            {
                result.Status = DiagnosticStatus.Failed;
                result.Summary = "No Bluetooth adapter was detected by Windows.";
                result.TechnicalDetails = "Plug and Play enumeration (SetupAPI / Win32_PnPEntity) returned 0 active Bluetooth devices.";
                result.Evidence = "No devices found matching GUID_DEVCLASS_BLUETOOTH or Bluetooth PnP class.";
                result.PossibleCause = "1. Computer does not have Bluetooth hardware.\n2. Internal adapter is disabled in BIOS/UEFI.\n3. Physical wireless switch/button is switched off.\n4. USB Bluetooth dongle is unplugged or faulty.";
                result.RecommendedAction = "Verify if your computer has built-in Bluetooth. If using a USB dongle, reconnect it to a different USB port. Check BIOS settings for Bluetooth state.";
                return Task.FromResult(result);
            }

            var primary = context.PrimaryAdapter ?? adapters[0];
            result.Status = DiagnosticStatus.Passed;
            result.Summary = $"Bluetooth adapter detected: {primary.FriendlyName}";
            result.TechnicalDetails = $"Manufacturer: {primary.Manufacturer}, Hardware ID: {primary.HardwareId}, Instance: {primary.DeviceInstanceId}";
            result.Evidence = $"DeviceInstanceId: '{primary.DeviceInstanceId}', HardwareId: '{primary.HardwareId}', Total detected: {adapters.Count}";
            result.RecommendedAction = "No action required. Bluetooth hardware is recognized by the operating system.";

            return Task.FromResult(result);
        }
    }
}

