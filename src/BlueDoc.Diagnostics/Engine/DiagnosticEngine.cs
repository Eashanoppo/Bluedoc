using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BlueDoc.Bluetooth.Abstractions;
using BlueDoc.Core.Enums;
using BlueDoc.Core.Interfaces;
using BlueDoc.Core.Models;
using BlueDoc.Diagnostics.Rules;
using BlueDoc.Infrastructure.EventLogs;
using BlueDoc.Infrastructure.Hardware;
using BlueDoc.Infrastructure.Platform;
using BlueDoc.Infrastructure.Services;

namespace BlueDoc.Diagnostics.Engine
{
    public class DiagnosticEngine : IDiagnosticEngine
    {
        private readonly List<IDiagnosticRule> _rules = new List<IDiagnosticRule>();
        private readonly IHardwareInspector _hardwareInspector;
        private readonly IServiceInspector _serviceInspector;
        private readonly BluetoothManager _bluetoothManager;

        public DiagnosticEngine(
            IHardwareInspector hardwareInspector = null,
            IServiceInspector serviceInspector = null,
            BluetoothManager bluetoothManager = null)
        {
            _hardwareInspector = hardwareInspector ?? new SetupApiHardwareInspector();
            _serviceInspector = serviceInspector ?? new WindowsServiceInspector();
            _bluetoothManager = bluetoothManager ?? new BluetoothManager(_hardwareInspector);

            RegisterDefaultRules();
        }

        private void RegisterDefaultRules()
        {
            _rules.Add(new OsCompatibilityRule());
            _rules.Add(new AdapterDetectionRule());
            _rules.Add(new HardwareStatusRule());
            _rules.Add(new DriverHealthRule());
            _rules.Add(new ServiceStatusRule());
            _rules.Add(new AudioServiceRule());
            _rules.Add(new RadioAvailabilityRule());
            _rules.Add(new DiscoveryReadinessRule());
            _rules.Add(new PairingReadinessRule());
            _rules.Add(new BleAvailabilityRule());
            _rules.Add(new BleGattSupportRule());
            _rules.Add(new EventLogHealthRule());
        }

        public IReadOnlyList<string> GetAvailableRuleIds()
        {
            return _rules.Select(r => r.RuleId).ToList();
        }

        public IReadOnlyList<DiagnosticResult> GetInitialTestList()
        {
            return _rules.Select(r => new DiagnosticResult
            {
                RuleId = r.RuleId,
                TestName = r.RuleName,
                Category = r.Category,
                Status = DiagnosticStatus.NotTested,
                Summary = r.Description,
                Confidence = "Pending Evaluation",
                ExecutedAt = DateTime.UtcNow
            }).ToList();
        }

        public async Task<DiagnosticSession> RunFullDiagnosticsAsync(IProgress<DiagnosticResult> progress = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var session = new DiagnosticSession
            {
                StartedAt = DateTime.UtcNow,
                OsVersion = OperatingSystemInspector.GetOperatingSystemInfo()
            };

            var context = await BuildContextAsync(session.OsVersion, cancellationToken);
            session.AdapterInfo = context.PrimaryAdapter;
            session.BluetoothService = context.CoreService;

            foreach (var rule in _rules)
            {
                if (cancellationToken.IsCancellationRequested) break;

                var result = await rule.EvaluateAsync(context, cancellationToken);
                context.PreviousResults.Add(result);
                session.Results.Add(result);

                progress?.Report(result);
            }

            session.CompletedAt = DateTime.UtcNow;
            return session;
        }

        public async Task<DiagnosticResult> RunSingleTestAsync(string ruleId, CancellationToken cancellationToken = default(CancellationToken))
        {
            var rule = _rules.FirstOrDefault(r => string.Equals(r.RuleId, ruleId, StringComparison.OrdinalIgnoreCase));
            if (rule == null)
            {
                throw new ArgumentException($"Diagnostic rule '{ruleId}' not found.", nameof(ruleId));
            }

            var osVersion = OperatingSystemInspector.GetOperatingSystemInfo();
            var context = await BuildContextAsync(osVersion, cancellationToken);
            return await rule.EvaluateAsync(context, cancellationToken);
        }

        private async Task<DiagnosticContext> BuildContextAsync(OsVersionInfo osInfo, CancellationToken cancellationToken)
        {
            var context = new DiagnosticContext
            {
                OsInfo = osInfo
            };

            // Inspect hardware
            var adapters = await Task.Run(() => _hardwareInspector.EnumerateAdapters(), cancellationToken);
            context.Adapters = adapters;
            context.PrimaryAdapter = _hardwareInspector.GetPrimaryAdapter();

            // Inspect services
            var services = await Task.Run(() => _serviceInspector.GetAllBluetoothServices(), cancellationToken);
            context.Services = services;
            context.CoreService = _serviceInspector.GetServiceInfo("bthserv");
            context.AudioGatewayService = _serviceInspector.GetServiceInfo("BTAGService");
            context.AvctpService = _serviceInspector.GetServiceInfo("BthAvctpSvc");

            // Inspect radio state
            try
            {
                context.IsRadioEnabled = await _bluetoothManager.IsRadioEnabledAsync(cancellationToken);
            }
            catch
            {
                context.IsRadioEnabled = false;
            }

            // Inspect paired devices via PnP
            try
            {
                context.PairedDevices = await Task.Run(() => PnpBluetoothDeviceInspector.EnumeratePairedDevices(), cancellationToken);
            }
            catch
            {
                context.PairedDevices = new List<BluetoothDeviceInfo>();
            }

            // Inspect recent events
            try
            {
                var eventAnalyzer = new BluetoothEventAnalyzer();
                context.RecentEvents = await Task.Run(() => eventAnalyzer.QueryBluetoothEvents(30, 7), cancellationToken);
            }
            catch
            {
                context.RecentEvents = new List<BluetoothEventRecord>();
            }

            return context;
        }
    }
}
