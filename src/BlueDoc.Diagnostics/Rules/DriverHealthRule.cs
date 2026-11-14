using System;
using System.Threading;
using System.Threading.Tasks;
using BlueDoc.Core.Enums;
using BlueDoc.Core.Models;

namespace BlueDoc.Diagnostics.Rules
{
    public class DriverHealthRule : IDiagnosticRule
    {
        public string RuleId => "RULE_DRIVER_HEALTH";
        public string RuleName => "Bluetooth Driver Health";
        public string Category => "Driver Diagnostics";
        public string Description => "Examines driver installation, provider credentials, version, and driver registry binding.";

        public Task<DiagnosticResult> EvaluateAsync(DiagnosticContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = new DiagnosticResult
            {
                RuleId = RuleId,
                TestName = RuleName,
                Category = Category,
                Confidence = "Confirmed Fact"
            };

            var adapter = context.PrimaryAdapter;
            if (adapter == null || !adapter.IsPresent)
            {
                result.Status = DiagnosticStatus.NotSupported;
                result.Summary = "Skipped: No adapter detected to verify driver.";
                return Task.FromResult(result);
            }

            var driver = adapter.Driver;
            if (driver == null || !driver.IsDriverInstalled)
            {
                result.Status = DiagnosticStatus.Failed;
                result.Summary = "No functional driver is installed for this Bluetooth adapter.";
                result.TechnicalDetails = "Device registry does not have ProviderName or DriverVersion registered.";
                result.Evidence = $"Adapter: '{adapter.FriendlyName}', DriverProvider: null, DriverVersion: null";
                result.PossibleCause = "Driver files are missing, corrupted, or the device was uninstalled without driver package restoration.";
                result.RecommendedAction = "Visit the adapter manufacturer website (or PC OEM) to download and install the official Bluetooth driver package.";
                result.RequiresAdmin = true;
                return Task.FromResult(result);
            }

            // Check for Generic Microsoft fallback driver on RAM-patch silicon
            string provider = driver.DriverProvider ?? string.Empty;
            string inf = driver.InfPath ?? string.Empty;
            string vid = adapter.HardwareDetails?.VendorId?.ToUpperInvariant() ?? string.Empty;
            bool isGenericProvider = provider.IndexOf("Microsoft", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                     inf.IndexOf("bth.inf", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                     (driver.DriverDateString != null && driver.DriverDateString.IndexOf("2006", StringComparison.OrdinalIgnoreCase) >= 0);

            // Chips requiring RAM patch: Qualcomm Atheros (0CF3, 17CB), Foxconn Sony VAIO (0489), Lite-On (04CA), Broadcom (0A5C), Realtek (0BDA), Intel (8087)
            bool isRamPatchSilicon = vid == "0CF3" || vid == "17CB" || vid == "0489" || vid == "04CA" || 
                                     vid == "0A5C" || vid == "0BDA" || vid == "8087" ||
                                     adapter.FriendlyName.IndexOf("Generic Bluetooth", StringComparison.OrdinalIgnoreCase) >= 0;

            if (isGenericProvider && isRamPatchSilicon)
            {
                result.Status = DiagnosticStatus.Failed;
                result.Summary = "Missing Vendor Firmware Patch: Generic Microsoft Driver (06-21-2006) Bound to Uninitialized Silicon";
                result.TechnicalDetails = $"Hardware VID={vid}, PID={adapter.HardwareDetails?.ProductId} is bound to generic inbox driver '{inf}' without required vendor RAM-patch firmware (.dfu/.hcd). The 2.4 GHz physical radio transmitter is dormant.";
                result.Evidence = $"Adapter: '{adapter.FriendlyName}', Provider: '{provider}', Version: '{driver.DriverVersion}', Date: '{driver.DriverDateString}', HardwareId: '{adapter.HardwareId}'";
                result.PossibleCause = "The Bluetooth silicon requires vendor RAM-patch firmware loaded at boot over USB. Because Microsoft's generic fallback driver lacks this patch, Windows reports Code 0 but the physical radio cannot transmit or discover peripherals.";
                result.RecommendedAction = "Use BlueDoc Driver Assistant to automatically provision and install the certified vendor driver and firmware patch package.";
                result.RequiresAdmin = true;
                return Task.FromResult(result);
            }

            // Check if driver is valid
            result.Status = DiagnosticStatus.Passed;
            result.Summary = $"Driver is installed: {driver.DriverProvider} (v{driver.DriverVersion})";
            result.TechnicalDetails = $"Provider: {driver.DriverProvider}, Version: {driver.DriverVersion}, Date: {driver.DriverDateString}, Inf: {driver.InfPath}";
            result.Evidence = $"Provider: '{driver.DriverProvider}', Version: '{driver.DriverVersion}', Date: '{driver.DriverDateString}'";

            // If driver is older than 10 years, raise a warning advisory
            if (driver.DriverDate.HasValue && driver.DriverDate.Value < DateTime.UtcNow.AddYears(-10))
            {
                result.Status = DiagnosticStatus.Warning;
                result.Summary = $"Outdated Driver: {driver.DriverProvider} (v{driver.DriverVersion}, Date: {driver.DriverDateString})";
                result.PossibleCause = "Driver release date is over 10 years old. May have compatibility issues with newer Bluetooth LE devices.";
                result.RecommendedAction = "Check the manufacturer website for an updated driver package.";
            }
            else
            {
                result.RecommendedAction = "No action required. Driver is properly bound and registered.";
            }

            return Task.FromResult(result);
        }
    }
}

