using System;
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
        private readonly Action _navigateToDiagnostics;
        private readonly HardwareRadioManager _radioManager;

        private BluetoothAdapterInfo _primaryAdapter;
        private ServiceInfo _serviceInfo;
        private OsVersionInfo _osVersion;
        private DiagnosticSession _recentSession;
        private bool _isLoading;
        private bool _isRadioOn = true;
        private string _statusMessage = "Ready";

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

        public ICommand RefreshCommand { get; }
        public ICommand RunDiagnosisCommand { get; }
        public ICommand ToggleRadioPowerCommand { get; }

        public DashboardViewModel(AdapterService adapterService, DiagnosticsService diagnosticsService, Action navigateToDiagnostics)
        {
            _adapterService = adapterService;
            _diagnosticsService = diagnosticsService;
            _navigateToDiagnostics = navigateToDiagnostics;
            _radioManager = new HardwareRadioManager();

            RefreshCommand = new RelayCommand(async () => await LoadDataAsync());
            RunDiagnosisCommand = new RelayCommand(() => _navigateToDiagnostics?.Invoke());
            ToggleRadioPowerCommand = new RelayCommand(async () => await ToggleRadioPowerAsync());

            _osVersion = OperatingSystemInspector.GetOperatingSystemInfo();
        }

        public async Task ToggleRadioPowerAsync()
        {
            bool targetState = !IsRadioOn;
            StatusMessage = targetState 
                ? "Powering on Bluetooth radio hardware via PnP controller..."
                : "Powering down Bluetooth radio transceiver via PnP controller...";

            bool success = await _radioManager.SetRadioPowerStateAsync(targetState, PrimaryAdapter?.DeviceInstanceId);
            await Task.Delay(800); // allow bus settling

            IsRadioOn = _radioManager.IsRadioPoweredOn(PrimaryAdapter?.DeviceInstanceId);
            StatusMessage = IsRadioOn
                ? "Bluetooth radio transceiver active (RF Active, PnP Operational)."
                : "Bluetooth radio transceiver powered off (RF Stopped, Controller Disabled).";

            await LoadDataAsync();
        }

        public async Task LoadDataAsync()
        {
            IsLoading = true;

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
            }
            catch (Exception ex)
            {
                StatusMessage = $"Inspection error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
