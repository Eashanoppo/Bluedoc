using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using BlueDoc.Application.Services;
using BlueDoc.Core.Models;
using BlueDoc.UI.Common;

namespace BlueDoc.UI.ViewModels
{
    public class BluetoothMainViewModel : ViewModelBase
    {
        private readonly DeviceManagementService _managementService;
        private readonly DeviceDiscoveryService _discoveryService;
        private readonly AdapterService _adapterService;
        private readonly Action<string> _navigateTo;

        private bool _isLoading;
        private bool _isScanning;
        private bool _isRadioOn = true;
        private string _adapterName = "Realtek Bluetooth Adapter";
        private string _scanStatusText = "Looking for nearby devices...";
        private CancellationTokenSource _scanCts;

        public ObservableCollection<BluetoothDeviceInfo> AudioDevices { get; } = new ObservableCollection<BluetoothDeviceInfo>();
        public ObservableCollection<BluetoothDeviceInfo> Peripherals { get; } = new ObservableCollection<BluetoothDeviceInfo>();
        public ObservableCollection<BluetoothDeviceInfo> OtherDevices { get; } = new ObservableCollection<BluetoothDeviceInfo>();
        public ObservableCollection<BluetoothDeviceInfo> NearbyDevices { get; } = new ObservableCollection<BluetoothDeviceInfo>();

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public bool IsScanning
        {
            get => _isScanning;
            set
            {
                if (SetProperty(ref _isScanning, value))
                {
                    OnPropertyChanged(nameof(ScanButtonText));
                }
            }
        }

        public string ScanButtonText => IsScanning ? "Stop Scan" : "Scan for Devices";

        public bool IsRadioOn
        {
            get => _isRadioOn;
            set => SetProperty(ref _isRadioOn, value);
        }

        public string AdapterName
        {
            get => _adapterName;
            set => SetProperty(ref _adapterName, value);
        }

        public string MachineName => Environment.MachineName;

        public string ScanStatusText
        {
            get => _scanStatusText;
            set => SetProperty(ref _scanStatusText, value);
        }

        public bool HasAudioDevices => AudioDevices.Count > 0;
        public bool HasPeripherals => Peripherals.Count > 0;
        public bool HasOtherDevices => OtherDevices.Count > 0;
        public bool HasNearbyDevices => NearbyDevices.Count > 0;

        public ICommand RefreshCommand { get; }
        public ICommand ToggleScanCommand { get; }
        public ICommand ConnectCommand { get; }
        public ICommand DisconnectCommand { get; }
        public ICommand PairCommand { get; }
        public ICommand ManageCommand { get; }
        public ICommand UnpairCommand { get; }
        public ICommand NavigateToDoctorCommand { get; }

        public BluetoothMainViewModel(
            DeviceManagementService managementService = null,
            DeviceDiscoveryService discoveryService = null,
            AdapterService adapterService = null,
            Action<string> navigateTo = null)
        {
            _managementService = managementService ?? new DeviceManagementService();
            _discoveryService = discoveryService ?? new DeviceDiscoveryService();
            _adapterService = adapterService ?? new AdapterService();
            _navigateTo = navigateTo;

            RefreshCommand = new RelayCommand(async () => await LoadDataAsync());
            ToggleScanCommand = new RelayCommand(async () =>
            {
                if (IsScanning)
                {
                    StopScanning();
                }
                else
                {
                    await StartScanningAsync();
                }
            });

            ConnectCommand = new RelayCommand(async param =>
            {
                if (param is BluetoothDeviceInfo device)
                {
                    await ConnectDeviceAsync(device);
                }
            });

            DisconnectCommand = new RelayCommand(async param =>
            {
                if (param is BluetoothDeviceInfo device)
                {
                    await DisconnectDeviceAsync(device);
                }
            });

            PairCommand = new RelayCommand(async param =>
            {
                if (param is BluetoothDeviceInfo device)
                {
                    await PairDeviceAsync(device);
                }
            });

            ManageCommand = new RelayCommand(param =>
            {
                if (param is BluetoothDeviceInfo device)
                {
                    _navigateTo?.Invoke("Bluetooth Devices");
                }
            });

            UnpairCommand = new RelayCommand(async param =>
            {
                if (param is BluetoothDeviceInfo device)
                {
                    await UnpairDeviceAsync(device);
                }
            });

            NavigateToDoctorCommand = new RelayCommand(param =>
            {
                _navigateTo?.Invoke("Fix My Bluetooth Connection");
            });

            // Auto-load data
            _ = LoadDataAsync();
        }

        public async Task LoadDataAsync()
        {
            IsLoading = true;
            try
            {
                var adapter = await _adapterService.GetPrimaryAdapterAsync();
                if (adapter != null)
                {
                    AdapterName = adapter.FriendlyName;
                    IsRadioOn = adapter.Status == Core.Enums.AdapterStatus.Operational;
                }

                var paired = await _managementService.GetPairedDevicesAsync();

                AudioDevices.Clear();
                Peripherals.Clear();
                OtherDevices.Clear();

                foreach (var dev in paired)
                {
                    string cat = dev.CategoryName ?? string.Empty;
                    string name = dev.DisplayName ?? string.Empty;

                    if (IsAudioDevice(cat, name))
                    {
                        AudioDevices.Add(dev);
                    }
                    else if (IsPeripheral(cat, name))
                    {
                        Peripherals.Add(dev);
                    }
                    else
                    {
                        OtherDevices.Add(dev);
                    }
                }

                OnPropertyChanged(nameof(HasAudioDevices));
                OnPropertyChanged(nameof(HasPeripherals));
                OnPropertyChanged(nameof(HasOtherDevices));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"[BluetoothMainViewModel.LoadDataAsync] Error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private static bool IsAudioDevice(string cat, string name)
        {
            return cat.IndexOf("Head", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   cat.IndexOf("Audio", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   cat.IndexOf("Speaker", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   cat.IndexOf("Earbud", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   cat.IndexOf("Sound", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   name.IndexOf("Buds", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   name.IndexOf("Speaker", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   name.IndexOf("Headphones", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsPeripheral(string cat, string name)
        {
            return cat.IndexOf("Mouse", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   cat.IndexOf("Keyboard", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   cat.IndexOf("Peripheral", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   cat.IndexOf("HID", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   cat.IndexOf("Pointer", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   cat.IndexOf("Game", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   cat.IndexOf("Controller", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   name.IndexOf("Mouse", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   name.IndexOf("Keyboard", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public async Task StartScanningAsync()
        {
            if (IsScanning) return;
            IsScanning = true;
            ScanStatusText = "Scanning for nearby Bluetooth devices...";
            _scanCts = new CancellationTokenSource();

            try
            {
                var discovered = await _discoveryService.StartScanningAsync(8);
                NearbyDevices.Clear();

                foreach (var dev in discovered)
                {
                    // Exclude already paired devices
                    bool isPaired = AudioDevices.Any(d => d.BluetoothAddress == dev.BluetoothAddress) ||
                                    Peripherals.Any(d => d.BluetoothAddress == dev.BluetoothAddress) ||
                                    OtherDevices.Any(d => d.BluetoothAddress == dev.BluetoothAddress);

                    if (!isPaired && !NearbyDevices.Any(d => d.BluetoothAddress == dev.BluetoothAddress))
                    {
                        NearbyDevices.Add(dev);
                    }
                }

                ScanStatusText = NearbyDevices.Count > 0
                    ? $"{NearbyDevices.Count} nearby device(s) found."
                    : "No new nearby Bluetooth devices found. Make sure your device is in pairing mode.";

                OnPropertyChanged(nameof(HasNearbyDevices));
            }
            catch (Exception ex)
            {
                ScanStatusText = $"Scan failed: {ex.Message}";
            }
            finally
            {
                IsScanning = false;
            }
        }

        public void StopScanning()
        {
            _scanCts?.Cancel();
            _discoveryService.StopScanning();
            IsScanning = false;
            ScanStatusText = "Discovery paused.";
        }

        private async Task ConnectDeviceAsync(BluetoothDeviceInfo device)
        {
            var result = await _managementService.ConnectDeviceAsync(device);
            if (result.Success)
            {
                device.IsConnected = true;
            }
            else
            {
                MessageBox.Show($"Failed to connect to '{device.DisplayName}':\n\n{result.Message}\n\nTip: You can use the Fix Connection Doctor to resolve this issue.", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            await LoadDataAsync();
        }

        private async Task DisconnectDeviceAsync(BluetoothDeviceInfo device)
        {
            var result = await _managementService.DisconnectDeviceAsync(device);
            if (result.Success)
            {
                device.IsConnected = false;
            }
            await LoadDataAsync();
        }

        private async Task PairDeviceAsync(BluetoothDeviceInfo device)
        {
            var result = await _managementService.PairDeviceAsync(device);
            if (result.Success)
            {
                MessageBox.Show($"Successfully paired with '{device.DisplayName}'.", "Pairing Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                NearbyDevices.Remove(device);
                await LoadDataAsync();
            }
            else
            {
                MessageBox.Show($"Pairing failed with '{device.DisplayName}':\n\n{result.Message}", "Pairing Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async Task UnpairDeviceAsync(BluetoothDeviceInfo device)
        {
            var confirm = MessageBox.Show($"Are you sure you want to forget and remove '{device.DisplayName}'?", "Confirm Remove Device", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm == MessageBoxResult.Yes)
            {
                bool ok = await _managementService.UnpairDeviceAsync(device.BluetoothAddress);
                if (ok)
                {
                    await LoadDataAsync();
                }
            }
        }
    }
}
