using System;
using System.Threading.Tasks;
using System.Windows.Input;
using BlueDoc.Application.Services;
using BlueDoc.Core.Models;
using BlueDoc.Data;
using BlueDoc.Data.Repositories;
using BlueDoc.Infrastructure.Platform;
using BlueDoc.UI.Common;

namespace BlueDoc.UI.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly SqliteDatabaseManager _db;
        private readonly AdapterService _adapterService;
        private readonly DiagnosticsService _diagnosticsService;
        private readonly DeviceDiscoveryService _discoveryService;
        private readonly DeviceManagementService _managementService;
        private readonly HistoryService _historyService;
        private readonly EventAnalysisService _eventService;
        private readonly DriverService _driverService;
        private readonly RepairService _repairService;
        private readonly ReportService _reportService;

        private object _currentViewModel;
        private string _activeSection = "Dashboard";
        private string _activeGroup = "OVERVIEW & HARDWARE";
        private OsVersionInfo _osVersion;
        private BluetoothAdapterInfo _primaryAdapter;
        private bool _isDarkTheme = true;
        private bool _isLightTheme = false;
        private bool _isRadioOn = true;

        public DashboardViewModel DashboardVM { get; }
        public HardwareInspectorViewModel HardwareInspectorVM { get; }
        public DiagnosticCenterViewModel DiagnosticCenterVM { get; }
        public CompatibilityViewModel CompatibilityVM { get; }
        public BluetoothDevicesViewModel BluetoothDevicesVM { get; }
        public DeviceScannerViewModel DeviceScannerVM { get; }
        public BluetoothEventAnalyzerViewModel BluetoothEventAnalyzerVM { get; }
        public DriverAssistantViewModel DriverAssistantVM { get; }
        public SafeRepairCenterViewModel SafeRepairCenterVM { get; }
        public DiagnosticReportViewModel DiagnosticReportVM { get; }
        public ConnectionHistoryViewModel ConnectionHistoryVM { get; }
        public SignalMonitorViewModel SignalMonitorVM { get; }
        public BleExplorerViewModel BleExplorerVM { get; }
        public SettingsViewModel SettingsVM { get; }
        public TechnicianToolkitViewModel TechnicianToolkitVM { get; }
        public AudioDiagnosticsViewModel AudioDiagnosticsVM { get; }
        public SaasFleetDashboardViewModel SaasFleetDashboardVM { get; }

        public object CurrentViewModel
        {
            get => _currentViewModel;
            set => SetProperty(ref _currentViewModel, value);
        }

        public string ActiveSection
        {
            get => _activeSection;
            set => SetProperty(ref _activeSection, value);
        }

        public string ActiveGroup
        {
            get => _activeGroup;
            set => SetProperty(ref _activeGroup, value);
        }

        public bool IsDarkTheme
        {
            get => _isDarkTheme;
            set
            {
                if (SetProperty(ref _isDarkTheme, value) && value)
                {
                    _isLightTheme = false;
                    OnPropertyChanged(nameof(IsLightTheme));
                    ThemeManager.SetTheme(AppTheme.Dark);
                }
            }
        }

        public bool IsLightTheme
        {
            get => _isLightTheme;
            set
            {
                if (SetProperty(ref _isLightTheme, value) && value)
                {
                    _isDarkTheme = false;
                    OnPropertyChanged(nameof(IsDarkTheme));
                    ThemeManager.SetTheme(AppTheme.Light);
                }
            }
        }

        public OsVersionInfo OsVersion
        {
            get => _osVersion;
            set => SetProperty(ref _osVersion, value);
        }

        public BluetoothAdapterInfo PrimaryAdapter
        {
            get => _primaryAdapter;
            set => SetProperty(ref _primaryAdapter, value);
        }

        public bool IsRadioOn
        {
            get => _isRadioOn;
            set
            {
                if (SetProperty(ref _isRadioOn, value))
                {
                    OnPropertyChanged(nameof(RadioStateDisplay));
                }
            }
        }

        public string RadioStateDisplay => IsRadioOn ? "Bluetooth ON" : "Bluetooth OFF";

        public ICommand NavigateCommand { get; }
        public ICommand QuickDiagnosisCommand { get; }
        public ICommand SetThemeCommand { get; }
        public ICommand ToggleRadioPowerCommand { get; }

        public MainViewModel()
        {
            // Initialize SQLite Local Database
            _db = new SqliteDatabaseManager();
            try { _db.InitializeDatabase(); } catch { }

            var sessionRepo = new SqliteDiagnosticSessionRepository(_db);
            var historyRepo = new SqliteConnectionHistoryRepository(_db);
            var deviceRepo = new SqliteDeviceCacheRepository(_db);
            var repairRepo = new SqliteRepairHistoryRepository(_db);
            var settingsRepo = new SqliteSettingsRepository(_db);

            _adapterService = new AdapterService();
            _diagnosticsService = new DiagnosticsService(null, sessionRepo);
            _discoveryService = new DeviceDiscoveryService();
            _managementService = new DeviceManagementService();
            _historyService = new HistoryService(historyRepo, deviceRepo);
            _eventService = new EventAnalysisService();
            _driverService = new DriverService();
            _repairService = new RepairService(repairRepo);
            _reportService = new ReportService();

            _osVersion = OperatingSystemInspector.GetOperatingSystemInfo();

            // Instantiate ViewModels
            DashboardVM = new DashboardViewModel(_adapterService, _diagnosticsService, () => NavigateTo("Diagnostic Test Center"));
            HardwareInspectorVM = new HardwareInspectorViewModel(_adapterService);
            DiagnosticCenterVM = new DiagnosticCenterViewModel(_diagnosticsService, target => NavigateTo(target));
            CompatibilityVM = new CompatibilityViewModel();
            BluetoothDevicesVM = new BluetoothDevicesViewModel(_managementService);
            DeviceScannerVM = new DeviceScannerViewModel(_discoveryService, _managementService);
            BluetoothEventAnalyzerVM = new BluetoothEventAnalyzerViewModel(_eventService);
            DriverAssistantVM = new DriverAssistantViewModel(_adapterService, _driverService);
            SafeRepairCenterVM = new SafeRepairCenterViewModel(_repairService);
            DiagnosticReportVM = new DiagnosticReportViewModel(_adapterService, _diagnosticsService, _eventService, _managementService, _reportService);
            ConnectionHistoryVM = new ConnectionHistoryViewModel(_historyService);
            SignalMonitorVM = new SignalMonitorViewModel(_discoveryService);
            BleExplorerVM = new BleExplorerViewModel(_managementService, _discoveryService);
            SettingsVM = new SettingsViewModel(_db, settingsRepo);
            TechnicianToolkitVM = new TechnicianToolkitViewModel(_adapterService, _diagnosticsService, target => NavigateTo(target));
            AudioDiagnosticsVM = new AudioDiagnosticsViewModel(_adapterService);
            SaasFleetDashboardVM = new SaasFleetDashboardViewModel(_adapterService);

            NavigateCommand = new RelayCommand(param => NavigateTo(param?.ToString() ?? "Dashboard"));
            SetThemeCommand = new RelayCommand(param =>
            {
                var themeStr = param?.ToString();
                if (themeStr == "Light")
                {
                    IsLightTheme = true;
                }
                else
                {
                    IsDarkTheme = true;
                }
            });
            QuickDiagnosisCommand = new RelayCommand(async () =>
            {
                NavigateTo("Diagnostic Test Center");
                await DiagnosticCenterVM.ExecuteFullDiagnosticsAsync();
            });

            ToggleRadioPowerCommand = new RelayCommand(async () =>
            {
                await DashboardVM.ToggleRadioPowerAsync();
                IsRadioOn = DashboardVM.IsRadioOn;
                PrimaryAdapter = DashboardVM.PrimaryAdapter;
            });

            DashboardVM.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(DashboardVM.IsRadioOn))
                {
                    IsRadioOn = DashboardVM.IsRadioOn;
                }
                if (e.PropertyName == nameof(DashboardVM.PrimaryAdapter))
                {
                    PrimaryAdapter = DashboardVM.PrimaryAdapter;
                }
            };

            // Start on Dashboard
            CurrentViewModel = DashboardVM;
        }

        public async Task InitializeAsync()
        {
            try
            {
                PrimaryAdapter = await _adapterService.GetPrimaryAdapterAsync();
                await DashboardVM.LoadDataAsync();
                IsRadioOn = DashboardVM.IsRadioOn;
            }
            catch { }
        }

        public void NavigateTo(string sectionName)
        {
            ActiveSection = sectionName;

            switch (sectionName)
            {
                case "Dashboard":
                case "Bluetooth Devices":
                case "Device Scanner":
                case "Advanced Hardware Inspector":
                    ActiveGroup = "OVERVIEW & HARDWARE";
                    break;
                case "Diagnostic Test Center":
                case "Audio & Packet Diagnostics":
                case "Bluetooth Event Analyzer":
                case "Driver Assistant":
                case "Compatibility Checker":
                    ActiveGroup = "DIAGNOSTIC LAB";
                    break;
                case "Safe Repair Center":
                case "Diagnostic Reports":
                case "Device Connection History":
                    ActiveGroup = "SAFE REPAIR & AUDIT";
                    break;
                case "Signal & Discovery Monitor":
                case "Bluetooth LE Explorer":
                case "Technician Toolkit":
                    ActiveGroup = "TELEMETRY & PROTOCOLS";
                    break;
                case "SaaS Fleet Portal":
                case "Settings":
                    ActiveGroup = "ENTERPRISE & CLOUD";
                    break;
                default:
                    ActiveGroup = "OVERVIEW & HARDWARE";
                    break;
            }

            switch (sectionName)
            {
                case "Dashboard":
                    CurrentViewModel = DashboardVM;
                    _ = DashboardVM.LoadDataAsync();
                    break;
                case "Bluetooth Devices":
                    CurrentViewModel = BluetoothDevicesVM;
                    _ = BluetoothDevicesVM.LoadPairedDevicesAsync();
                    break;
                case "Device Scanner":
                    CurrentViewModel = DeviceScannerVM;
                    break;
                case "Advanced Hardware Inspector":
                    CurrentViewModel = HardwareInspectorVM;
                    _ = HardwareInspectorVM.LoadAdaptersAsync();
                    break;
                case "Diagnostic Test Center":
                    CurrentViewModel = DiagnosticCenterVM;
                    break;
                case "Bluetooth Event Analyzer":
                    CurrentViewModel = BluetoothEventAnalyzerVM;
                    _ = BluetoothEventAnalyzerVM.LoadEventsAsync();
                    break;
                case "Driver Assistant":
                    CurrentViewModel = DriverAssistantVM;
                    _ = DriverAssistantVM.LoadDriverInfoAsync();
                    break;
                case "Compatibility Checker":
                    CurrentViewModel = CompatibilityVM;
                    break;
                case "Safe Repair Center":
                    CurrentViewModel = SafeRepairCenterVM;
                    _ = SafeRepairCenterVM.LoadHistoryAsync();
                    break;
                case "Diagnostic Reports":
                    CurrentViewModel = DiagnosticReportVM;
                    _ = DiagnosticReportVM.GenerateReportAsync();
                    break;
                case "Device Connection History":
                    CurrentViewModel = ConnectionHistoryVM;
                    _ = ConnectionHistoryVM.LoadHistoryAsync();
                    break;
                case "Signal & Discovery Monitor":
                    CurrentViewModel = SignalMonitorVM;
                    break;
                case "Bluetooth LE Explorer":
                    CurrentViewModel = BleExplorerVM;
                    _ = BleExplorerVM.LoadBleDevicesAsync();
                    break;
                case "Settings":
                    CurrentViewModel = SettingsVM;
                    _ = SettingsVM.LoadSettingsAsync();
                    break;
                case "Technician Toolkit":
                    CurrentViewModel = TechnicianToolkitVM;
                    break;
                case "Audio & Packet Diagnostics":
                    CurrentViewModel = AudioDiagnosticsVM;
                    _ = AudioDiagnosticsVM.LoadAllDataAsync();
                    break;
                case "SaaS Fleet Portal":
                    CurrentViewModel = SaasFleetDashboardVM;
                    SaasFleetDashboardVM.RefreshFleetData();
                    break;
                default:
                    CurrentViewModel = DashboardVM;
                    break;
            }
        }
    }
}
