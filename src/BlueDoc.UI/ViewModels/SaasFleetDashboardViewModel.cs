using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using BlueDoc.Application.Cloud;
using BlueDoc.Application.Services;
using BlueDoc.UI.Common;

namespace BlueDoc.UI.ViewModels
{
    public class SaasFleetDashboardViewModel : ViewModelBase
    {
        // ── Dependencies ───────────────────────────────────────────
        private readonly ICloudFleetClient _cloud;
        private readonly SaasTelemetryPollerService _poller;
        private readonly CloudCredentialStore _creds;
        private readonly AdapterService _adapterService;
        private readonly MockSaasCloudApiGateway _gateway;
        private readonly IOfflineTelemetryQueue _offlineQueue;

        // ── State ──────────────────────────────────────────────────
        private FleetSummaryMetrics _metrics;
        private ObservableCollection<FleetMachineRecord> _filteredMachines;
        private FleetMachineRecord _selectedMachine;
        private CloudAlertList _alerts = new CloudAlertList();

        private string _endpointUrl = string.Empty;
        private string _tenantId = string.Empty;
        private string _apiKey = string.Empty;
        private bool _autoSyncEnabled;
        private int _syncIntervalMinutes = 30;
        private bool _isEnabled;

        private string _statusMessage = "Not connected. Configure credentials below.";
        private bool _isSyncing;
        private bool _isLoadingFleet;
        private int _pendingQueueCount = 0;
        private string _searchFilter = string.Empty;
        private string _selectedDepartment = "All";
        private string _selectedStatusFilter = "All";

        // Registration workflow
        private string _regOrgName = string.Empty;
        private string _regAdminEmail = string.Empty;
        private string _regPlan = "free";
        private string _regStatus = string.Empty;

        // ── Commands ───────────────────────────────────────────────
        public ICommand SyncNowCommand { get; }
        public ICommand RefreshFleetCommand { get; }
        public ICommand SaveCredentialsCommand { get; }
        public ICommand RegisterTenantCommand { get; }
        public ICommand AcknowledgeAlertCommand { get; }
        public ICommand SelectDepartmentCommand { get; }
        public ICommand SelectStatusCommand { get; }
        public ICommand ToggleGatewayCommand { get; }
        public ICommand IngestLocalMachineTelemetryCommand { get; }

        // ── Properties ─────────────────────────────────────────────
        public bool IsConfigured => _cloud != null && _cloud.IsConfigured;

        public string EndpointUrl
        {
            get => _endpointUrl;
            set
            {
                if (SetProperty(ref _endpointUrl, value))
                {
                    ApplyCredentialsToClient();
                    OnPropertyChanged(nameof(IsConfigured));
                    OnPropertyChanged(nameof(CloudEndpointUrl));
                }
            }
        }

        public string CloudEndpointUrl
        {
            get => _endpointUrl;
            set => EndpointUrl = value;
        }

        public string TenantId
        {
            get => _tenantId;
            set
            {
                if (SetProperty(ref _tenantId, value))
                {
                    ApplyCredentialsToClient();
                }
            }
        }

        public string ApiKey
        {
            get => _apiKey;
            set
            {
                if (SetProperty(ref _apiKey, value))
                {
                    ApplyCredentialsToClient();
                    OnPropertyChanged(nameof(IsConfigured));
                }
            }
        }

        public bool AutoSyncEnabled
        {
            get => _autoSyncEnabled;
            set
            {
                if (SetProperty(ref _autoSyncEnabled, value))
                {
                    OnAutoSyncToggled();
                }
            }
        }

        public int SyncIntervalMinutes
        {
            get => _syncIntervalMinutes;
            set
            {
                if (SetProperty(ref _syncIntervalMinutes, value))
                {
                    _poller.IntervalMinutes = value;
                }
            }
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (SetProperty(ref _statusMessage, value))
                {
                    OnPropertyChanged(nameof(GatewayStatusDisplay));
                }
            }
        }

        public string GatewayStatusDisplay
        {
            get => _statusMessage;
            set => StatusMessage = value;
        }

        public bool IsSyncing
        {
            get => _isSyncing;
            set => SetProperty(ref _isSyncing, value);
        }

        public bool IsLoadingFleet
        {
            get => _isLoadingFleet;
            set => SetProperty(ref _isLoadingFleet, value);
        }

        public int PendingQueueCount
        {
            get => _pendingQueueCount;
            set
            {
                if (SetProperty(ref _pendingQueueCount, value))
                {
                    OnPropertyChanged(nameof(PendingTelemetryCount));
                }
            }
        }

        public int PendingTelemetryCount
        {
            get => _pendingQueueCount;
            set => PendingQueueCount = value;
        }

        public string LocalMachineHostname => Environment.MachineName;

        public string SearchFilter
        {
            get => _searchFilter;
            set
            {
                if (SetProperty(ref _searchFilter, value))
                {
                    ApplyFilters();
                }
            }
        }

        public string SelectedDepartment
        {
            get => _selectedDepartment;
            set
            {
                if (SetProperty(ref _selectedDepartment, value))
                {
                    ApplyFilters();
                }
            }
        }

        public string SelectedStatusFilter
        {
            get => _selectedStatusFilter;
            set
            {
                if (SetProperty(ref _selectedStatusFilter, value))
                {
                    ApplyFilters();
                }
            }
        }

        public FleetSummaryMetrics Metrics
        {
            get => _metrics;
            set => SetProperty(ref _metrics, value);
        }

        public ObservableCollection<FleetMachineRecord> FilteredMachines
        {
            get => _filteredMachines;
            set => SetProperty(ref _filteredMachines, value);
        }

        public FleetMachineRecord SelectedMachine
        {
            get => _selectedMachine;
            set => SetProperty(ref _selectedMachine, value);
        }

        public CloudAlertList Alerts
        {
            get => _alerts;
            set => SetProperty(ref _alerts, value);
        }

        // Registration form properties
        public string RegOrgName
        {
            get => _regOrgName;
            set => SetProperty(ref _regOrgName, value);
        }

        public string RegAdminEmail
        {
            get => _regAdminEmail;
            set => SetProperty(ref _regAdminEmail, value);
        }

        public string RegPlan
        {
            get => _regPlan;
            set => SetProperty(ref _regPlan, value);
        }

        public string RegStatus
        {
            get => _regStatus;
            set => SetProperty(ref _regStatus, value);
        }

        public ObservableCollection<string> Departments { get; }
            = new ObservableCollection<string> { "All", "Engineering", "Sales", "Operations", "Manufacturing", "Finance" };

        public ObservableCollection<string> StatusFilters { get; }
            = new ObservableCollection<string> { "All", "Healthy", "Warning", "Critical" };

        public SaasFleetDashboardViewModel(
            AdapterService adapterService = null,
            ICloudFleetClient cloud = null,
            SaasTelemetryPollerService poller = null,
            CloudCredentialStore creds = null,
            IOfflineTelemetryQueue offlineQueue = null)
        {
            _adapterService = adapterService ?? new AdapterService();
            _offlineQueue = offlineQueue ?? new SqliteOfflineTelemetryQueue();
            _cloud = cloud ?? new CloudFleetClient(_offlineQueue);
            _creds = creds ?? new CloudCredentialStore();
            _poller = poller ?? new SaasTelemetryPollerService();
            _gateway = new MockSaasCloudApiGateway();

            SyncNowCommand = new RelayCommand(async () => await SyncNowAsync(), () => !IsSyncing);
            RefreshFleetCommand = new RelayCommand(async () => await RefreshFleetAsync());
            SaveCredentialsCommand = new RelayCommand(async () => await SaveCredentialsAsync());
            RegisterTenantCommand = new RelayCommand(async () => await RegisterTenantAsync());
            AcknowledgeAlertCommand = new RelayCommand(async param => await AcknowledgeAlertAsync(param?.ToString()));
            SelectDepartmentCommand = new RelayCommand(param => SelectedDepartment = param?.ToString() ?? "All");
            SelectStatusCommand = new RelayCommand(param => SelectedStatusFilter = param?.ToString() ?? "All");
            ToggleGatewayCommand = new RelayCommand(async () => await RefreshFleetAsync());
            IngestLocalMachineTelemetryCommand = new RelayCommand(async () => await SyncNowAsync());

            // Initialize default filter lists
            RefreshFleetData();

            // Load stored credentials asynchronously
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            await LoadCredentialsAsync();
            await RefreshQueueCountAsync();

            if (_cloud.IsConfigured)
            {
                await RefreshFleetAsync();
                await RefreshAlertsAsync();
            }
        }

        public async Task LoadCredentialsAsync()
        {
            try
            {
                EndpointUrl = await _creds.GetEndpointUrlAsync();
                TenantId = await _creds.GetTenantIdAsync();
                ApiKey = await _creds.GetApiKeyAsync();
                AutoSyncEnabled = await _creds.GetAutoSyncAsync();
                SyncIntervalMinutes = await _creds.GetIntervalMinutesAsync();
                IsEnabled = await _creds.GetEnabledAsync();

                ApplyCredentialsToClient();

                StatusMessage = _cloud.IsConfigured
                    ? "✔ Cloud credentials active. Ready for live fleet synchronization."
                    : "⚪ Not connected. Configure cloud credentials or register below.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to load credentials: {ex.Message}";
            }
        }

        private void ApplyCredentialsToClient()
        {
            if (_cloud != null)
            {
                _cloud.EndpointUrl = _endpointUrl;
                _cloud.ApiKey = _apiKey;
                _cloud.TenantId = _tenantId;
            }

            if (_poller != null)
            {
                _poller.EndpointUrl = _endpointUrl;
                _poller.ApiKey = _apiKey;
                _poller.TenantId = _tenantId;
            }
        }

        public async Task SaveCredentialsAsync()
        {
            try
            {
                await _creds.SetEndpointUrlAsync(_endpointUrl);
                await _creds.SetTenantIdAsync(_tenantId);
                await _creds.SetApiKeyAsync(_apiKey);
                await _creds.SetAutoSyncAsync(_autoSyncEnabled);
                await _creds.SetIntervalMinutesAsync(_syncIntervalMinutes);
                await _creds.SetEnabledAsync(true);

                ApplyCredentialsToClient();
                OnPropertyChanged(nameof(IsConfigured));

                StatusMessage = "Credentials saved. Verifying connection...";
                await SyncNowAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to save credentials: {ex.Message}";
            }
        }

        public async Task SyncNowAsync()
        {
            if (IsSyncing) return;
            IsSyncing = true;
            StatusMessage = "Diagnosing Bluetooth hardware and transmitting telemetry...";

            try
            {
                var adapter = await _adapterService.GetPrimaryAdapterAsync();
                var diagEngine = new BlueDoc.Diagnostics.Engine.DiagnosticEngine();
                var session = await diagEngine.RunFullDiagnosticsAsync();
                var svc = new SaasTelemetryService();
                var payload = await svc.BuildPayloadAsync(adapter, session, null);

                if (_cloud.IsConfigured)
                {
                    var result = await _cloud.PushTelemetryAsync(payload);
                    StatusMessage = result.Success
                        ? $"✔ Synced with Cloud ({TenantId}) at {DateTime.Now:HH:mm:ss}."
                        : $"⚠ Cloud notice: {result.Message}";

                    await RefreshFleetAsync();
                    await RefreshAlertsAsync();
                }
                else
                {
                    // Fallback to local offline queue
                    await _offlineQueue.EnqueueAsync(payload);
                    StatusMessage = "Saved to local offline queue (Cloud endpoint not configured).";
                }

                await RefreshQueueCountAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Sync failed: {ex.Message}";
            }
            finally
            {
                IsSyncing = false;
            }
        }

        public async Task RefreshFleetAsync()
        {
            IsLoadingFleet = true;
            try
            {
                if (_cloud != null && _cloud.IsConfigured)
                {
                    var remoteMetrics = await _cloud.GetFleetMetricsAsync(_selectedStatusFilter, _searchFilter);
                    if (remoteMetrics != null && remoteMetrics.Machines != null && remoteMetrics.Machines.Count > 0)
                    {
                        Metrics = remoteMetrics;
                        ApplyFilters();
                        return;
                    }
                }

                // Fallback to in-memory gateway calculation (e.g. for unit tests or unconfigured preview)
                RefreshFleetData();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Fleet refresh notice: {ex.Message}";
                RefreshFleetData();
            }
            finally
            {
                IsLoadingFleet = false;
            }
        }

        public void RefreshFleetData()
        {
            Metrics = _gateway.GetFleetMetrics();
            ApplyFilters();
        }

        public async Task RefreshAlertsAsync()
        {
            if (_cloud != null && _cloud.IsConfigured)
            {
                try
                {
                    Alerts = await _cloud.GetAlertsAsync();
                }
                catch
                {
                    // Non-critical alert retrieval
                }
            }
        }

        public async Task AcknowledgeAlertAsync(string alertId)
        {
            if (string.IsNullOrEmpty(alertId)) return;

            try
            {
                if (_cloud != null && _cloud.IsConfigured)
                {
                    bool ok = await _cloud.AcknowledgeAlertAsync(alertId);
                    if (ok)
                    {
                        await RefreshAlertsAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to acknowledge alert: {ex.Message}";
            }
        }

        public async Task RegisterTenantAsync()
        {
            if (string.IsNullOrWhiteSpace(EndpointUrl) ||
                string.IsNullOrWhiteSpace(RegOrgName) ||
                string.IsNullOrWhiteSpace(RegAdminEmail))
            {
                RegStatus = "Please enter Endpoint URL, Organization Name, and Admin Email.";
                return;
            }

            RegStatus = "Registering organization with cloud backend...";
            try
            {
                var result = await _cloud.RegisterTenantAsync(RegOrgName, RegAdminEmail, RegPlan);
                if (result.Success)
                {
                    TenantId = result.TenantId.ToString();
                    ApiKey = result.ApiKey;
                    RegStatus = $"✔ Registered tenant '{result.Slug}'! API key generated and applied.";
                    await SaveCredentialsAsync();
                }
                else
                {
                    RegStatus = $"✖ Registration failed: {result.ErrorMessage}";
                }
            }
            catch (Exception ex)
            {
                RegStatus = $"✖ Registration error: {ex.Message}";
            }
        }

        private async Task RefreshQueueCountAsync()
        {
            try
            {
                PendingQueueCount = await _offlineQueue.GetPendingCountAsync();
            }
            catch
            {
                PendingQueueCount = 0;
            }
        }

        private void OnAutoSyncToggled()
        {
            if (_autoSyncEnabled)
            {
                _poller.StartAutoSync();
            }
            else
            {
                _poller.StopAutoSync();
            }
        }

        public void ApplyFilters()
        {
            if (Metrics == null || Metrics.Machines == null)
            {
                FilteredMachines = new ObservableCollection<FleetMachineRecord>();
                return;
            }

            var query = Metrics.Machines.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchFilter))
            {
                var q = SearchFilter.Trim().ToLowerInvariant();
                query = query.Where(m =>
                    (m.Hostname != null && m.Hostname.ToLowerInvariant().Contains(q)) ||
                    (m.PrimaryAdapter != null && m.PrimaryAdapter.ToLowerInvariant().Contains(q)) ||
                    (m.ChipsetManufacturer != null && m.ChipsetManufacturer.ToLowerInvariant().Contains(q)) ||
                    (m.Department != null && m.Department.ToLowerInvariant().Contains(q))
                );
            }

            if (!string.IsNullOrEmpty(SelectedDepartment) && SelectedDepartment != "All")
            {
                query = query.Where(m => string.Equals(m.Department, SelectedDepartment, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(SelectedStatusFilter) && SelectedStatusFilter != "All")
            {
                query = query.Where(m => string.Equals(m.HealthStatus, SelectedStatusFilter, StringComparison.OrdinalIgnoreCase));
            }

            FilteredMachines = new ObservableCollection<FleetMachineRecord>(query);
        }
    }
}
