using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using BlueDoc.Application.Services;
using BlueDoc.Core.Enums;
using BlueDoc.Core.Models;
using BlueDoc.Infrastructure.Hardware;
using BlueDoc.Infrastructure.Platform;
using BlueDoc.UI.Common;

namespace BlueDoc.UI.ViewModels
{
    public class DashboardViewModel : ViewModelBase
    {
        private readonly AdapterService _adapterService;
        private readonly DiagnosticsService _diagnosticsService;
        private readonly DeviceManagementService _managementService;
        private readonly DeviceDiscoveryService _discoveryService;
        private readonly HistoryService _historyService;
        private readonly Action _navigateToDiagnostics;
        private readonly Action<string> _navigateAction;
        private readonly HardwareRadioManager _radioManager;

        private BluetoothAdapterInfo _primaryAdapter;
        private ServiceInfo _serviceInfo;
        private OsVersionInfo _osVersion;
        private DiagnosticSession _recentSession;
        private bool _isLoading;
        private bool _isRefreshing;
        private bool _isQuickScanning;
        private bool _isRadioOn = true;
        private string _statusMessage = "Ready";

        public ObservableCollection<BluetoothDeviceInfo> ConnectedDevices { get; } = new ObservableCollection<BluetoothDeviceInfo>();
        public ObservableCollection<BluetoothDeviceInfo> AudioDevices { get; } = new ObservableCollection<BluetoothDeviceInfo>();
        public ObservableCollection<BluetoothDeviceInfo> PeripheralDevices { get; } = new ObservableCollection<BluetoothDeviceInfo>();
        public ObservableCollection<BluetoothDeviceInfo> OtherDevices { get; } = new ObservableCollection<BluetoothDeviceInfo>();
        public ObservableCollection<ConnectionHistoryRecord> RecentFailures { get; } = new ObservableCollection<ConnectionHistoryRecord>();
        public ObservableCollection<BluetoothDeviceInfo> NearbyDevices { get; } = new ObservableCollection<BluetoothDeviceInfo>();

        public BluetoothAdapterInfo PrimaryAdapter
        {
            get => _primaryAdapter;
            set => SetProperty(ref _primaryAdapter, value);
        }

        public ServiceInfo ServiceInfo
        {
            get => _serviceInfo;
            set => SetProperty(ref _serviceInfo, value);
        }

        public OsVersionInfo OsVersion
        {
            get => _osVersion;
            set => SetProperty(ref _osVersion, value);
        }

        public DiagnosticSession RecentSession
        {
            get => _recentSession;
            set => SetProperty(ref _recentSession, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public bool IsRefreshing
        {
            get => _isRefreshing;
            set => SetProperty(ref _isRefreshing, value);
        }

        public bool IsQuickScanning
        {
            get => _isQuickScanning;
            set
            {
                if (SetProperty(ref _isQuickScanning, value))
                {
                    OnPropertyChanged(nameof(QuickScanButtonText));
                }
            }
        }

        public string QuickScanButtonText => IsQuickScanning ? "Scanning..." : "Quick Device Scan";

        public bool IsRadioOn
        {
            get => _isRadioOn;
            set
            {
                if (SetProperty(ref _isRadioOn, value))
                {
                    OnPropertyChanged(nameof(RadioButtonText));
                }
            }
        }

        public string RadioButtonText => IsRadioOn ? "Turn Bluetooth OFF" : "Turn Bluetooth ON";

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public int ConnectedCount => ConnectedDevices.Count;
        public int RecentFailuresCount => RecentFailures.Count;
        public int NearbyCount => NearbyDevices.Count;
        public bool HasAudioDevices => AudioDevices.Count > 0;
        public bool HasPeripheralDevices => PeripheralDevices.Count > 0;
        public bool HasOtherDevices => OtherDevices.Count > 0;
        public int TotalPairedCount => AudioDevices.Count + PeripheralDevices.Count + OtherDevices.Count;

        public ICommand RefreshCommand { get; }
        public ICommand RunDiagnosisCommand { get; }
        public ICommand QuickScanCommand { get; }
        public ICommand ToggleRadioPowerCommand { get; }
        public ICommand NavigateToSectionCommand { get; }

        public DashboardViewModel(
            AdapterService adapterService, 
            DiagnosticsService diagnosticsService, 
            Action navigateToDiagnostics,
            DeviceManagementService managementService = null,
            DeviceDiscoveryService discoveryService = null,
            HistoryService historyService = null,
            Action<string> navigateAction = null)
        {
            _adapterService = adapterService;
            _diagnosticsService = diagnosticsService;
            _navigateToDiagnostics = navigateToDiagnostics;
            _managementService = managementService ?? new DeviceManagementService();
            _discoveryService = discoveryService ?? new DeviceDiscoveryService();
            _historyService = historyService ?? new HistoryService();
            _navigateAction = navigateAction ?? (target => _navigateToDiagnostics?.Invoke());
            _radioManager = new HardwareRadioManager();

            RefreshCommand = new RelayCommand(async () => await LoadDataAsync());
            RunDiagnosisCommand = new RelayCommand(() => _navigateToDiagnostics?.Invoke());
            QuickScanCommand = new RelayCommand(async () => await ExecuteQuickScanAsync(), () => !IsQuickScanning);
            ToggleRadioPowerCommand = new RelayCommand(async () => await ToggleRadioPowerAsync());
            NavigateToSectionCommand = new RelayCommand(param => _navigateAction?.Invoke(param?.ToString() ?? "Dashboard"));

            _osVersion = OperatingSystemInspector.GetOperatingSystemInfo();
        }

        public async Task ToggleRadioPowerAsync()
        {
            await SetRadioPowerAsync(!IsRadioOn);
        }

        public async Task SetRadioPowerAsync(bool targetState)
        {
            StatusMessage = targetState 
                ? "Powering on Bluetooth radio transceiver via Radio Subsystem..."
                : "Powering down Bluetooth radio transceiver via Radio Subsystem...";

            bool success = await _radioManager.SetRadioPowerStateAsync(targetState, PrimaryAdapter?.DeviceInstanceId);
            await Task.Delay(400);

            if (success)
            {
                IsRadioOn = targetState;
            }
            else
            {
                IsRadioOn = _radioManager.IsRadioPoweredOn(PrimaryAdapter?.DeviceInstanceId);
            }

            StatusMessage = IsRadioOn
                ? "Bluetooth radio transceiver active (RF Active, PnP Operational)."
                : "Bluetooth radio transceiver powered off (RF Stopped, Radio Disabled).";

            await LoadDataAsync();
        }

        public async Task LoadDataAsync()
        {
            if (PrimaryAdapter == null)
            {
                IsLoading = true;
            }
            else
            {
                IsRefreshing = true;
            }

            try
            {
                PrimaryAdapter = await _adapterService.GetPrimaryAdapterAsync();
                var services = await _adapterService.GetServicesAsync();
                ServiceInfo = services != null && services.Count > 0 ? services[0] : new ServiceInfo { ServiceName = "bthserv", Status = "Unknown" };

                if (_diagnosticsService.LastSession != null)
                {
                    RecentSession = _diagnosticsService.LastSession;
                }

                IsRadioOn = _radioManager.IsRadioPoweredOn(PrimaryAdapter?.DeviceInstanceId);
                StatusMessage = IsRadioOn
                    ? "Bluetooth hardware operational."
                    : "Bluetooth radio is currently powered off.";

                // Load real paired devices categorized into Audio Devices, Peripherals & Other
                var paired = await _managementService.GetPairedDevicesAsync();
                ConnectedDevices.Clear();
                AudioDevices.Clear();
                PeripheralDevices.Clear();
                OtherDevices.Clear();

                foreach (var d in paired)
                {
                    if (d.IsConnected)
                    {
                        ConnectedDevices.Add(d);
                    }

                    if (d.IsAudio)
                    {
                        AudioDevices.Add(d);
                    }
                    else if (d.IsPeripheral)
                    {
                        PeripheralDevices.Add(d);
                    }
                    else
                    {
                        OtherDevices.Add(d);
                    }
                }

                OnPropertyChanged(nameof(ConnectedCount));
                OnPropertyChanged(nameof(HasAudioDevices));
                OnPropertyChanged(nameof(HasPeripheralDevices));
                OnPropertyChanged(nameof(HasOtherDevices));
                OnPropertyChanged(nameof(TotalPairedCount));

                // Load real recent connection failures from SQLite audit trail
                var history = await _historyService.GetHistoryAsync(50);
                RecentFailures.Clear();
                foreach (var h in history.Where(x => !x.IsSuccess).Take(5))
                {
                    RecentFailures.Add(h);
                }
                OnPropertyChanged(nameof(RecentFailuresCount));
            }
            catch (Exception ex)
            {
                StatusMessage = $"Inspection error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
                IsRefreshing = false;
            }
        }

        public async Task ExecuteQuickScanAsync()
        {
            if (IsQuickScanning) return;
            IsQuickScanning = true;
            StatusMessage = "Running fast 6-second inquiry discovery for nearby peripherals...";

            try
            {
                var discovered = await _discoveryService.StartScanningAsync(6);
                NearbyDevices.Clear();
                foreach (var d in discovered.Take(6))
                {
                    NearbyDevices.Add(d);
                }
                OnPropertyChanged(nameof(NearbyCount));
                StatusMessage = NearbyDevices.Count > 0 
                    ? $"Quick scan found {NearbyDevices.Count} nearby peripheral(s)." 
                    : "Quick scan complete. No active discoverable devices found.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Quick scan notice: {ex.Message}";
            }
            finally
            {
                IsQuickScanning = false;
            }
        }
    }
}
