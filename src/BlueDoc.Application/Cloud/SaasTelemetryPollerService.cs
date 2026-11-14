using System;
using System.Threading;
using System.Threading.Tasks;
using BlueDoc.Application.Services;
using BlueDoc.Diagnostics.Benchmark;

namespace BlueDoc.Application.Cloud
{
    public class SaasTelemetryPollerService : IDisposable
    {
        private readonly ISaasTelemetryService _telemetryService;
        private readonly AdapterService _adapterService;
        private readonly DiagnosticsService _diagnosticsService;
        private readonly BluetoothBenchmarkEngine _benchmarkEngine;

        private System.Threading.Timer _timer;
        private bool _isAutoSyncEnabled;
        private int _intervalMinutes = 30;
        private string _endpointUrl = string.Empty;
        private string _tenantId = string.Empty;
        private string _apiKey = string.Empty;

        public bool IsAutoSyncEnabled => _isAutoSyncEnabled;
        public int IntervalMinutes
        {
            get => _intervalMinutes;
            set
            {
                _intervalMinutes = Math.Max(1, value);
                if (_isAutoSyncEnabled)
                {
                    RestartTimer();
                }
            }
        }

        public string EndpointUrl
        {
            get => _endpointUrl;
            set => _endpointUrl = value;
        }

        public string TenantId
        {
            get => _tenantId;
            set => _tenantId = value;
        }

        public string ApiKey
        {
            get => _apiKey;
            set => _apiKey = value;
        }

        public DateTime? LastSyncTimestamp { get; private set; }
        public string LastSyncResult { get; private set; } = "No telemetry transmission recorded yet.";

        public event EventHandler<SaasSyncResult> TelemetrySynced;

        public SaasTelemetryPollerService(
            ISaasTelemetryService telemetryService = null,
            AdapterService adapterService = null,
            DiagnosticsService diagnosticsService = null)
        {
            _telemetryService = telemetryService ?? new SaasTelemetryService();
            _adapterService = adapterService ?? new AdapterService();
            _diagnosticsService = diagnosticsService ?? new DiagnosticsService();
            _benchmarkEngine = new BluetoothBenchmarkEngine();
        }

        public void StartAutoSync()
        {
            if (_isAutoSyncEnabled) return;
            _isAutoSyncEnabled = true;
            RestartTimer();
        }

        public void StopAutoSync()
        {
            _isAutoSyncEnabled = false;
            _timer?.Change(Timeout.Infinite, Timeout.Infinite);
        }

        private void RestartTimer()
        {
            _timer?.Dispose();
            int ms = _intervalMinutes * 60 * 1000;
            _timer = new System.Threading.Timer(OnTimerTick, null, ms, ms);
        }

        private async void OnTimerTick(object state)
        {
            await SyncNowAsync();
        }

        public async Task<SaasSyncResult> SyncNowAsync()
        {
            try
            {
                var adapter = await _adapterService.GetPrimaryAdapterAsync();
                var session = await _diagnosticsService.RunDiagnosticsAsync();
                var bench = await _benchmarkEngine.RunBenchmarkAsync();

                var payload = await _telemetryService.BuildPayloadAsync(adapter, session, bench);
                var result = await _telemetryService.SyncTelemetryAsync(payload, _endpointUrl, _tenantId, _apiKey);

                LastSyncTimestamp = DateTime.UtcNow;
                LastSyncResult = result.Success
                    ? $"Success (HTTP {result.HttpStatusCode}): {result.Message}"
                    : $"Failure: {result.Message}";

                TelemetrySynced?.Invoke(this, result);
                return result;
            }
            catch (Exception ex)
            {
                var fail = new SaasSyncResult
                {
                    Success = false,
                    Message = $"Auto-sync exception: {ex.Message}"
                };
                LastSyncTimestamp = DateTime.UtcNow;
                LastSyncResult = fail.Message;
                TelemetrySynced?.Invoke(this, fail);
                return fail;
            }
        }

        public void Dispose()
        {
            StopAutoSync();
            _timer?.Dispose();
            _timer = null;
        }
    }
}

