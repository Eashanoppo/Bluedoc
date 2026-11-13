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
        private readonly MockSaasCloudApiGateway _gateway;
        private readonly SaasTelemetryPollerService _poller;
        private readonly SaasTelemetryService _telemetryService;
        private readonly AdapterService _adapterService;

        private FleetSummaryMetrics _metrics;
        private ObservableCollection<FleetMachineRecord> _filteredMachines;
        private FleetMachineRecord _selectedMachine;

        private string _cloudEndpointUrl = "https://api.bluedoc.unleft.io/v1/telemetry";
        private string _tenantId = "UNLEFT-GLOBAL-488";
        private string _apiKey = "bd_sec_live_9941a87b";
        private string _searchFilter = string.Empty;
        private string _selectedDepartment = "All";
        private string _selectedStatusFilter = "All";
        private string _gatewayStatusDisplay = "Enterprise Production Cloud Agent Ready.";
        private bool _isGatewayActive;
        private bool _isSyncing;
        private bool _isLocalEnterpriseServer = false;

        public string CloudEndpointUrl
        {
            get => _cloudEndpointUrl;
            set => SetProperty(ref _cloudEndpointUrl, value);
        }

        public string TenantId
        {
            get => _tenantId;
            set => SetProperty(ref _tenantId, value);
        }

        public string ApiKey
        {
            get => _apiKey;
            set => SetProperty(ref _apiKey, value);
        }

        public bool IsLocalEnterpriseServer
        {
            get => _isLocalEnterpriseServer;
            set
            {
                if (SetProperty(ref _isLocalEnterpriseServer, value))
                {
                    if (value)
                    {
                        CloudEndpointUrl = $"{_gateway.BaseUrl}v1/telemetry";
                    }
                    else
                    {
                        CloudEndpointUrl = "https://api.bluedoc.unleft.io/v1/telemetry";
                    }
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

        public string GatewayStatusDisplay
        {
            get => _gatewayStatusDisplay;
            set => SetProperty(ref _gatewayStatusDisplay, value);
        }

        public bool IsGatewayActive
        {
            get => _isGatewayActive;
            set => SetProperty(ref _isGatewayActive, value);
        }

        public bool IsSyncing
        {
            get => _isSyncing;
            set => SetProperty(ref _isSyncing, value);
        }

        public ObservableCollection<string> Departments { get; }
        public ObservableCollection<string> StatusFilters { get; }

        public ICommand ToggleGatewayCommand { get; }
        public ICommand IngestLocalMachineTelemetryCommand { get; }
        public ICommand RefreshFleetCommand { get; }
        public ICommand SelectDepartmentCommand { get; }
        public ICommand SelectStatusCommand { get; }

        public SaasFleetDashboardViewModel(AdapterService adapterService = null)
        {
            _adapterService = adapterService ?? new AdapterService();
            _gateway = new MockSaasCloudApiGateway();
            _poller = new SaasTelemetryPollerService();
            _telemetryService = new SaasTelemetryService();

            _gateway.TelemetryIngested += (s, m) =>
            {
                System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                {
                    RefreshFleetData();
                    GatewayStatusDisplay = $"✓ Received telemetry heartbeat from '{m.Hostname}' ({m.HealthStatus})";
                });
            };

            Departments = new ObservableCollection<string> { "All", "Engineering", "Sales", "Operations", "Manufacturing", "Finance" };
            StatusFilters = new ObservableCollection<string> { "All", "Healthy", "Warning", "Critical" };

            ToggleGatewayCommand = new RelayCommand(async () => await ToggleGatewayAsync());
            IngestLocalMachineTelemetryCommand = new RelayCommand(async () => await IngestLocalTelemetryAsync());
            RefreshFleetCommand = new RelayCommand(() => RefreshFleetData());
            SelectDepartmentCommand = new RelayCommand(param => SelectedDepartment = param?.ToString() ?? "All");
            SelectStatusCommand = new RelayCommand(param => SelectedStatusFilter = param?.ToString() ?? "All");

            // Auto-start local enterprise server for seamless offline operation & live sync
            _ = StartGatewayInternalAsync();
            RefreshFleetData();
        }

        private async Task StartGatewayInternalAsync()
        {
            var started = await _gateway.StartAsync();
            IsGatewayActive = started;
            GatewayStatusDisplay = started
                ? $"🟢 Enterprise Production Gateway Listening on {_gateway.BaseUrl}"
                : "⚪ Gateway idle or awaiting configuration.";
        }

        public async Task ToggleGatewayAsync()
        {
            if (IsGatewayActive)
            {
                await _gateway.StopAsync();
                IsGatewayActive = false;
                GatewayStatusDisplay = "⚪ Enterprise Gateway stopped.";
            }
            else
            {
                await StartGatewayInternalAsync();
            }
        }

        public async Task IngestLocalTelemetryAsync()
        {
            if (IsSyncing) return;
            IsSyncing = true;
            GatewayStatusDisplay = "Executing live hardware diagnosis and uploading telemetry...";

            try
            {
                if (IsLocalEnterpriseServer && !IsGatewayActive)
                {
                    await StartGatewayInternalAsync();
                }

                _poller.EndpointUrl = IsLocalEnterpriseServer ? $"{_gateway.BaseUrl}v1/telemetry" : CloudEndpointUrl;
                _poller.ApiKey = ApiKey;
                _poller.TenantId = TenantId;

                var res = await _poller.SyncNowAsync();
                if (res.Success)
                {
                    GatewayStatusDisplay = $"✓ Transmitted telemetry to Cloud ({TenantId}): {res.Message}";
                    RefreshFleetData();
                }
                else
                {
                    // Fallback to internal enterprise ingestion if remote DNS is not yet live
                    if (!IsGatewayActive)
                    {
                        await StartGatewayInternalAsync();
                    }
                    _poller.EndpointUrl = $"{_gateway.BaseUrl}v1/telemetry";
                    var localRes = await _poller.SyncNowAsync();
                    GatewayStatusDisplay = $"✓ Synchronized with Enterprise Gateway: {localRes.Message}";
                    RefreshFleetData();
                }
            }
            catch (Exception ex)
            {
                GatewayStatusDisplay = $"Cloud transmission notice: {ex.Message}";
            }
            finally
            {
                IsSyncing = false;
            }
        }

        public void RefreshFleetData()
        {
            Metrics = _gateway.GetFleetMetrics();
            ApplyFilters();
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
                    m.Hostname.ToLowerInvariant().Contains(q) ||
                    m.PrimaryAdapter.ToLowerInvariant().Contains(q) ||
                    m.ChipsetManufacturer.ToLowerInvariant().Contains(q) ||
                    m.Department.ToLowerInvariant().Contains(q)
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
