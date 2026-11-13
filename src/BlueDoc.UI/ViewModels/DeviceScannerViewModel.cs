using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using BlueDoc.Application.Services;
using BlueDoc.Core.Models;
using BlueDoc.UI.Common;

namespace BlueDoc.UI.ViewModels
{
    public class DeviceScannerViewModel : ViewModelBase
    {
        private readonly DeviceDiscoveryService _discoveryService;
        private readonly DeviceManagementService _managementService;
        private readonly DispatcherTimer _timer;

        private BluetoothDeviceInfo _selectedDevice;
        private bool _isScanning;
        private int _elapsedSeconds;
        private string _selectedCategory = "All";
        private string _statusMessage = "Ready to discover nearby Bluetooth devices.";

        public ObservableCollection<BluetoothDeviceInfo> DiscoveredDevices { get; } = new ObservableCollection<BluetoothDeviceInfo>();
        public ICollectionView FilteredDevices { get; }

        public BluetoothDeviceInfo SelectedDevice
        {
            get => _selectedDevice;
            set => SetProperty(ref _selectedDevice, value);
        }

        public bool IsScanning
        {
            get => _isScanning;
            set
            {
                if (SetProperty(ref _isScanning, value))
                {
                    OnPropertyChanged(nameof(ScanButtonText));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public string ScanButtonText => IsScanning ? "Stop Scanning" : "Start Scan";

        public int ElapsedSeconds
        {
            get => _elapsedSeconds;
            set
            {
                if (SetProperty(ref _elapsedSeconds, value))
                {
                    OnPropertyChanged(nameof(ElapsedDisplay));
                }
            }
        }

        public string ElapsedDisplay => $"{ElapsedSeconds}s elapsed";

        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value))
                {
                    FilteredDevices.Refresh();
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ICommand ToggleScanCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand FilterCommand { get; }
        public ICommand PairWithDeviceCommand { get; }

        public DeviceScannerViewModel(DeviceDiscoveryService discoveryService, DeviceManagementService managementService)
        {
            _discoveryService = discoveryService;
            _managementService = managementService;

            FilteredDevices = CollectionViewSource.GetDefaultView(DiscoveredDevices);
            FilteredDevices.Filter = FilterDevicePredicate;

            _discoveryService.DeviceDiscovered += OnDeviceDiscovered;
            _discoveryService.ScanningStateChanged += (s, scanning) => IsScanning = scanning;

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (s, e) => ElapsedSeconds++;

            ToggleScanCommand = new RelayCommand(async () =>
            {
                if (IsScanning)
                {
                    _discoveryService.StopScanning();
                    _timer.Stop();
                    StatusMessage = "Scan cancelled by user.";
                }
                else
                {
                    await ExecuteScanAsync();
                }
            });

            ClearCommand = new RelayCommand(() =>
            {
                DiscoveredDevices.Clear();
                SelectedDevice = null;
                StatusMessage = "Discovered device list cleared.";
            }, () => !IsScanning);

            FilterCommand = new RelayCommand(param =>
            {
                SelectedCategory = param?.ToString() ?? "All";
            });

            PairWithDeviceCommand = new RelayCommand(() =>
            {
                _managementService.LaunchWindowsPairingWizard();
            });
        }

        public async Task ExecuteScanAsync()
        {
            DiscoveredDevices.Clear();
            SelectedDevice = null;
            ElapsedSeconds = 0;
            _timer.Start();
            StatusMessage = "Inquiring nearby Classic Bluetooth & PnP devices...";

            try
            {
                await _discoveryService.StartScanningAsync(10);
                StatusMessage = $"Scan completed. Discovered {DiscoveredDevices.Count} devices.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Scan error: {ex.Message}";
            }
            finally
            {
                _timer.Stop();
                IsScanning = false;
            }
        }

        private void OnDeviceDiscovered(object sender, BluetoothDeviceInfo device)
        {
            System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
            {
                for (int i = 0; i < DiscoveredDevices.Count; i++)
                {
                    if (string.Equals(DiscoveredDevices[i].BluetoothAddress, device.BluetoothAddress, StringComparison.OrdinalIgnoreCase))
                    {
                        DiscoveredDevices[i] = device;
                        return;
                    }
                }

                DiscoveredDevices.Add(device);
                if (SelectedDevice == null)
                {
                    SelectedDevice = device;
                }
            });
        }

        private bool FilterDevicePredicate(object obj)
        {
            if (obj is BluetoothDeviceInfo d)
            {
                if (SelectedCategory == "All") return true;
                if (SelectedCategory == "Audio" && d.MajorDeviceClass == "Audio/Video") return true;
                if (SelectedCategory == "Peripherals" && d.MajorDeviceClass == "Peripheral") return true;
                if (SelectedCategory == "Phones/PCs" && (d.MajorDeviceClass == "Phone" || d.MajorDeviceClass == "Computer")) return true;
                if (SelectedCategory == "Paired" && (d.IsPaired || d.IsRemembered)) return true;
            }
            return false;
        }
    }
}

