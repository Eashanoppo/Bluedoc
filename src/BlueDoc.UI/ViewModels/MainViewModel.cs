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
        private bool _isDarkTheme = false;
        private bool _isLightTheme = true;
        private bool _isRadioOn = true;

        public DashboardViewModel DashboardVM { get; }
        public BluetoothMainViewModel BluetoothMainVM { get; }
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
        public FixMyConnectionWizardViewModel FixMyConnectionWizardVM { get; }

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
                if (SetProperty(ref _isDarkTheme, value))
                {
                    _isLightTheme = !value;
                    OnPropertyChanged(nameof(IsLightTheme));
                    ThemeManager.SetTheme(value ? AppTheme.Dark : AppTheme.Light);
                }
            }
        }

        public bool IsLightTheme
        {
            get => _isLightTheme;
            set
            {
                if (SetProperty(ref _isLightTheme, value))
                {
                    _isDarkTheme = !value;
                    OnPropertyChanged(nameof(IsDarkTheme));
                    ThemeManager.SetTheme(value ? AppTheme.Light : AppTheme.Dark);
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
                    _ = UpdateRadioPowerAsync(value);
                }
            }
        }

        public string RadioStateDisplay => IsRadioOn ? "Bluetooth ON" : "Bluetooth OFF";

        private bool _isSidebarCollapsed = false;

        public bool IsSidebarCollapsed
        {
            get => _isSidebarCollapsed;
            set
            {
                if (SetProperty(ref _isSidebarCollapsed, value))
                {
                    OnPropertyChanged(nameof(IsSidebarExpanded));
                    OnPropertyChanged(nameof(SidebarWidth));
                    OnPropertyChanged(nameof(SidebarToggleTooltip));
                }
            }
        }

        public bool IsSidebarExpanded => !IsSidebarCollapsed;
        public double SidebarWidth => IsSidebarCollapsed ? 68.0 : 270.0;
        public string SidebarToggleTooltip => IsSidebarCollapsed ? "Expand sidebar (Ctrl+[)" : "Collapse sidebar (Ctrl+[)";
        public ICommand ToggleSidebarCommand => new RelayCommand(() => IsSidebarCollapsed = !IsSidebarCollapsed);

        public NotificationService Notifications => NotificationService.Instance;

        public ICommand NavigateCommand { get; }
        public ICommand QuickDiagnosisCommand { get; }
        public ICommand OpenFixConnectionWizardCommand { get; }
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
            DashboardVM = new DashboardViewModel(_adapterService, _diagnosticsService, () => NavigateTo("Diagnostic Test Center"), _managementService, _discoveryService, _historyService, target => NavigateTo(target));
            BluetoothMainVM = new BluetoothMainViewModel(_managementService, _discoveryService, _adapterService, target => NavigateTo(target));
            HardwareInspectorVM = new HardwareInspectorViewModel(_adapterService);
            DiagnosticCenterVM = new DiagnosticCenterViewModel(_diagnosticsService, target => NavigateTo(target));
            CompatibilityVM = new CompatibilityViewModel();
            BluetoothDevicesVM = new BluetoothDevicesViewModel(_managementService, target => NavigateTo(target));
            DeviceScannerVM = new DeviceScannerViewModel(_discoveryService, _managementService, target => NavigateTo(target));
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
            FixMyConnectionWizardVM = new FixMyConnectionWizardViewModel(_managementService, _discoveryService, _adapterService, target => NavigateTo(target));

            NavigateCommand = new RelayCommand(param => NavigateTo(param?.ToString() ?? "Dashboard"));
            OpenFixConnectionWizardCommand = new RelayCommand(param =>
            {
                if (param is BluetoothDeviceInfo dev)
                {
                    FixMyConnectionWizardVM.PreSelectDevice(dev);
                }
                NavigateTo("Fix My Bluetooth Connection");
            });
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
                var op = Notifications.ShowRunning("Quick Diagnostics", "Executing comprehensive 12-rule hardware and service diagnostic pipeline...");
                try
                {
                    await DiagnosticCenterVM.ExecuteFullDiagnosticsAsync();
                    int passed = DiagnosticCenterVM.PassedCount;
                    int failed = DiagnosticCenterVM.FailedCount;
                    int warnings = DiagnosticCenterVM.WarningCount;
                    if (failed > 0)
                    {
                        op.CompleteError("Diagnosis Alert", $"Diagnostics completed with {failed} failure(s) and {warnings} warning(s).");
                    }
                    else if (warnings > 0)
                    {
                        op.CompleteSuccess("Diagnosis Passed with Warnings", $"All checks passed with {warnings} advisory warning(s).");
                    }
                    else
                    {
                        op.CompleteSuccess("Diagnostics Healthy", $"All {passed} diagnostic rules passed successfully.");
                    }
                }
                catch (Exception ex)
                {
                    op.CompleteError("Diagnostics Error", ex.Message);
                }
            });

            ToggleRadioPowerCommand = new RelayCommand(() =>
            {
                IsRadioOn = !IsRadioOn;
            });

            DashboardVM.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(DashboardVM.IsRadioOn) && !_isUpdatingRadioPower)
                {
                    _isRadioOn = DashboardVM.IsRadioOn;
                    OnPropertyChanged(nameof(IsRadioOn));
                    OnPropertyChanged(nameof(RadioStateDisplay));
                }
                if (e.PropertyName == nameof(DashboardVM.PrimaryAdapter))
                {
                    PrimaryAdapter = DashboardVM.PrimaryAdapter;
                }
            };

            // Start on Dashboard
            CurrentViewModel = DashboardVM;
        }

        private bool _isUpdatingRadioPower = false;
        private async Task UpdateRadioPowerAsync(bool targetState)
        {
            if (_isUpdatingRadioPower) return;
            _isUpdatingRadioPower = true;

            string target = targetState ? "ON" : "OFF";
            var op = Notifications.ShowRunning("Bluetooth Radio", $"Switching radio power {target} via Hardware Bus...");
            try
            {
                await DashboardVM.SetRadioPowerAsync(targetState);
                if (_isRadioOn != DashboardVM.IsRadioOn)
                {
                    _isRadioOn = DashboardVM.IsRadioOn;
                    OnPropertyChanged(nameof(IsRadioOn));
                    OnPropertyChanged(nameof(RadioStateDisplay));
                }
                PrimaryAdapter = DashboardVM.PrimaryAdapter;
                op.CompleteSuccess("Radio Power Updated", $"Bluetooth controller radio is now {RadioStateDisplay}.");
            }
            catch (Exception ex)
            {
                _isRadioOn = DashboardVM.IsRadioOn;
                OnPropertyChanged(nameof(IsRadioOn));
                OnPropertyChanged(nameof(RadioStateDisplay));
                op.CompleteError("Radio Power Error", ex.Message);
            }
            finally
            {
                _isUpdatingRadioPower = false;
            }
        }

        public async Task InitializeAsync()
        {
            try
            {
                PrimaryAdapter = await _adapterService.GetPrimaryAdapterAsync();
                await DashboardVM.LoadDataAsync();
                IsRadioOn = DashboardVM.IsRadioOn;

                var startupHealthService = new BluetoothStartupHealthService(_adapterService);
                var health = await startupHealthService.InspectSystemHealthAsync();
                if (!health.IsHealthy)
                {
                    Notifications.ShowWarning("Bluetooth Health Alert", health.OverallMessage);
                }
                else
                {
                    Notifications.ShowInfo("BlueDoc Ready", $"System: {_osVersion?.OperatingSystemName ?? "Windows"} • Adapter: {PrimaryAdapter?.FriendlyName ?? "Operational"}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"[MainViewModel.InitializeAsync] Non-fatal startup warning: {ex.Message}");
            }
        }

        public void NavigateTo(string sectionName)
        {
            ActiveSection = sectionName;

            switch (sectionName)
            {
                case "Dashboard":
                case "Overview":
                case "Bluetooth":
                case "Bluetooth Devices":
                case "Device Scanner":
                    ActiveGroup = "GENERAL";
                    break;
                case "Advanced Hardware Inspector":
                case "Hardware Inspector":
                case "Driver Assistant":
                case "Compatibility Checker":
                case "Compatibility":
                    ActiveGroup = "DEVICES & HARDWARE";
                    break;
                case "Diagnostics":
                case "Diagnostic Test Center":
                case "Diagnostic Center":
                case "Audio & Packet Diagnostics":
                case "Audio Diagnostics":
                case "Bluetooth Event Analyzer":
                case "Event Analyzer":
                case "Signal & Discovery Monitor":
                case "Signal Monitor":
                case "Device Connection History":
                case "Connection History":
                    ActiveGroup = "DIAGNOSTICS";
                    break;
                case "Fix My Bluetooth Connection":
                case "Fix Connection Doctor":
                case "Repair Center":
                case "Safe Repair Center":
                case "Reports":
                case "Diagnostic Reports":
                    ActiveGroup = "TOOLS";
                    break;
                case "Bluetooth LE Explorer":
                case "BLE Explorer":
                case "Technician Toolkit":
                case "SaaS Fleet Portal":
                case "SaaS Fleet Dashboard":
                    ActiveGroup = "ADVANCED TOOLS";
                    break;
                case "Settings":
                    ActiveGroup = "PREFERENCES";
                    break;
                default:
                    ActiveGroup = "GENERAL";
                    break;
            }

            switch (sectionName)
            {
                case "Dashboard":
                case "Overview":
                    CurrentViewModel = DashboardVM;
                    _ = DashboardVM.LoadDataAsync();
                    break;
                case "Bluetooth":
                    CurrentViewModel = BluetoothMainVM;
                    _ = BluetoothMainVM.LoadDataAsync();
                    break;
                case "Fix My Bluetooth Connection":
                case "Fix Connection Doctor":
                    CurrentViewModel = FixMyConnectionWizardVM;
                    _ = FixMyConnectionWizardVM.LoadCandidatesAsync();
                    break;
                case "Bluetooth Devices":
                    CurrentViewModel = BluetoothDevicesVM;
                    _ = BluetoothDevicesVM.LoadPairedDevicesAsync();
                    break;
                case "Device Scanner":
                    CurrentViewModel = DeviceScannerVM;
                    break;
                case "Advanced Hardware Inspector":
                case "Hardware Inspector":
                    CurrentViewModel = HardwareInspectorVM;
                    _ = HardwareInspectorVM.LoadAdaptersAsync();
                    break;
                case "Diagnostics":
                case "Diagnostic Test Center":
                case "Diagnostic Center":
                    CurrentViewModel = DiagnosticCenterVM;
                    break;
                case "Bluetooth Event Analyzer":
                case "Event Analyzer":
                    CurrentViewModel = BluetoothEventAnalyzerVM;
                    _ = BluetoothEventAnalyzerVM.LoadEventsAsync();
                    break;
                case "Driver Assistant":
                    CurrentViewModel = DriverAssistantVM;
                    _ = DriverAssistantVM.LoadDriverInfoAsync();
                    break;
                case "Compatibility Checker":
                case "Compatibility":
                    CurrentViewModel = CompatibilityVM;
                    break;
                case "Repair Center":
                case "Safe Repair Center":
                    CurrentViewModel = SafeRepairCenterVM;
                    _ = SafeRepairCenterVM.LoadHistoryAsync();
                    break;
                case "Reports":
                case "Diagnostic Reports":
                    CurrentViewModel = DiagnosticReportVM;
                    _ = DiagnosticReportVM.GenerateReportAsync();
                    break;
                case "Device Connection History":
                case "Connection History":
                    CurrentViewModel = ConnectionHistoryVM;
                    _ = ConnectionHistoryVM.LoadHistoryAsync();
                    break;
                case "Signal & Discovery Monitor":
                case "Signal Monitor":
                    CurrentViewModel = SignalMonitorVM;
                    break;
                case "Bluetooth LE Explorer":
                case "BLE Explorer":
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
                case "Audio Diagnostics":
                    CurrentViewModel = AudioDiagnosticsVM;
                    _ = AudioDiagnosticsVM.LoadAllDataAsync();
                    break;
                case "SaaS Fleet Portal":
                case "SaaS Fleet Dashboard":
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
