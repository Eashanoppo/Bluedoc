using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using BlueDoc.Application.Services;
using BlueDoc.Core.Enums;
using BlueDoc.Core.Models;
using BlueDoc.Infrastructure.Platform;
using BlueDoc.UI.Common;

namespace BlueDoc.UI.ViewModels
{
    public class RadarBlipItem : ViewModelBase
    {
        public BluetoothDeviceInfo Device { get; }
        public double X { get; set; }
        public double Y { get; set; }
        public double DistanceMeters { get; set; }
        public double AngleDegrees { get; set; }
        public string DisplayName => Device?.DisplayName ?? "Device";
        public string CategoryIcon => Device?.CategoryIcon ?? "📶";
        public string SignalDisplay => Device?.SignalStrengthDisplay ?? "Signal not exposed";
        public bool IsConnected => Device?.IsConnected ?? false;
        public bool IsPaired => Device?.IsPaired ?? false;
        public string BluetoothAddress => Device?.BluetoothAddress ?? string.Empty;

        public RadarBlipItem(BluetoothDeviceInfo device, double x, double y, double distance, double angle)
        {
            Device = device;
            X = x;
            Y = y;
            DistanceMeters = distance;
            AngleDegrees = angle;
        }
    }

    public class DeviceScannerViewModel : ViewModelBase
    {
        private readonly DeviceDiscoveryService _discoveryService;
        private readonly DeviceManagementService _managementService;
        private readonly Action<string> _navigationCallback;
        private readonly DispatcherTimer _timer;

        private BluetoothDeviceInfo _selectedDevice;
        private bool _isScanning;
        private int _elapsedSeconds;
        private int _scanDurationSeconds = 20;
        private string _selectedFilter = "All";
        private string _searchText = string.Empty;
        private string _statusMessage = "Ready to discover nearby Bluetooth devices.";
        private DiscoveryMode _selectedDiscoveryMode = DiscoveryMode.DualMode;
        private BluetoothAdapterInfo _selectedAdapter;

        // Pairing & Post-Pairing state
        private bool _isPairingInProgress;
        private BluetoothPairingStage _pairingStage = BluetoothPairingStage.Idle;
        private BluetoothConnectionStage _connectionStage = BluetoothConnectionStage.Idle;
        private bool _isConnecting;
        private bool _isDisconnecting;
        private string _pairingStatusText = string.Empty;
        private string _pinInput = string.Empty;
        private bool _showPinInput;
        private bool _showNumericComparison;
        private string _numericComparisonPin = string.Empty;
        private TaskCompletionSource<bool> _numericComparisonTcs;
        private DeviceOperationResult _lastPairingResult;
        private string _postPairingMessage = string.Empty;

        public ObservableCollection<BluetoothDeviceInfo> DiscoveredDevices { get; } = new ObservableCollection<BluetoothDeviceInfo>();
        public ObservableCollection<BluetoothAdapterInfo> AvailableAdapters { get; } = new ObservableCollection<BluetoothAdapterInfo>();
        public ObservableCollection<string> SelectedDeviceServices { get; } = new ObservableCollection<string>();
        public ObservableCollection<RadarBlipItem> RadarBlips { get; } = new ObservableCollection<RadarBlipItem>();
        public ICollectionView FilteredDevices { get; }

        private bool _isRadarView;
        public bool IsRadarView
        {
            get => _isRadarView;
            set
            {
                if (SetProperty(ref _isRadarView, value))
                {
                    OnPropertyChanged(nameof(IsListView));
                    if (value) UpdateRadarBlips();
                }
            }
        }

        public bool IsListView
        {
            get => !_isRadarView;
            set => IsRadarView = !value;
        }

        public List<int> AvailableDurations { get; } = new List<int> { 10, 20, 30, 60 };

        public IEnumerable<DiscoveryMode> AvailableDiscoveryModes
        {
            get
            {
                if (OperatingSystemInspector.IsWindows10OrGreater())
                {
                    return new[] { DiscoveryMode.DualMode, DiscoveryMode.ClassicOnly, DiscoveryMode.BleOnly };
                }
                return new[] { DiscoveryMode.ClassicOnly };
            }
        }

        public BluetoothDeviceInfo SelectedDevice
        {
            get => _selectedDevice;
            set
            {
                if (SetProperty(ref _selectedDevice, value))
                {
                    OnPropertyChanged(nameof(HasSelectedDevice));
                    OnPropertyChanged(nameof(CanConnectSelectedDevice));
                    OnPropertyChanged(nameof(CanDisconnectSelectedDevice));
                    OnPropertyChanged(nameof(CanPairSelectedDevice));
                    PostPairingMessage = string.Empty;
                    LastPairingResult = null;
                    _ = LoadServicesForSelectedDevice();
                }
            }
        }

        public bool HasSelectedDevice => SelectedDevice != null;

        public bool IsScanning
        {
            get => _isScanning;
            set
            {
                if (SetProperty(ref _isScanning, value))
                {
                    OnPropertyChanged(nameof(ScanButtonText));
                    OnPropertyChanged(nameof(IsIdle));
                    OnPropertyChanged(nameof(ShowScanSkeleton));
                    OnPropertyChanged(nameof(ShowEmptyState));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public bool ShowScanSkeleton => IsScanning && DiscoveredDevices.Count == 0;
        public bool ShowEmptyState => !IsScanning && FilteredDevices.IsEmpty;

        public BluetoothPairingStage PairingStage
        {
            get => _pairingStage;
            set
            {
                if (SetProperty(ref _pairingStage, value))
                {
                    OnPropertyChanged(nameof(PairingStageDisplay));
                }
            }
        }

        public string PairingStageDisplay
        {
            get
            {
                switch (PairingStage)
                {
                    case BluetoothPairingStage.Discovering:
                        return "Discovering Nearby Peripherals...";
                    case BluetoothPairingStage.PairingRequested:
                        return "Pairing Handshake Requested...";
                    case BluetoothPairingStage.WaitingForAuthentication:
                        return "Waiting for User Authentication...";
                    case BluetoothPairingStage.Authenticating:
                        return "Authenticating Security Keys...";
                    case BluetoothPairingStage.PairingCompleted:
                        return "Pairing Completed Successfully";
                    case BluetoothPairingStage.PairingFailed:
                        return "Pairing Handshake Failed";
                    default:
                        return string.Empty;
                }
            }
        }

        public BluetoothConnectionStage ConnectionStage
        {
            get => _connectionStage;
            set
            {
                if (SetProperty(ref _connectionStage, value))
                {
                    OnPropertyChanged(nameof(ConnectionStageDisplay));
                }
            }
        }

        public string ConnectionStageDisplay
        {
            get
            {
                switch (ConnectionStage)
                {
                    case BluetoothConnectionStage.PreparingConnection:
                        return "Preparing Connection...";
                    case BluetoothConnectionStage.Connecting:
                        return "Establishing RFCOMM / Audio Link...";
                    case BluetoothConnectionStage.VerifyingState:
                        return "Verifying PnP & Audio Endpoints...";
                    case BluetoothConnectionStage.Connected:
                        return "Connected";
                    case BluetoothConnectionStage.ConnectionFailed:
                        return "Connection Failed";
                    default:
                        return string.Empty;
                }
            }
        }

        public bool IsConnecting
        {
            get => _isConnecting;
            set
            {
                if (SetProperty(ref _isConnecting, value))
                {
                    OnPropertyChanged(nameof(CanConnectSelectedDevice));
                    OnPropertyChanged(nameof(CanDisconnectSelectedDevice));
                }
            }
        }

        public bool IsDisconnecting
        {
            get => _isDisconnecting;
            set
            {
                if (SetProperty(ref _isDisconnecting, value))
                {
                    OnPropertyChanged(nameof(CanConnectSelectedDevice));
                    OnPropertyChanged(nameof(CanDisconnectSelectedDevice));
                }
            }
        }

        public bool IsIdle => !IsScanning;

        public string ScanButtonText => IsScanning ? "Stop Scan" : "Start Scan";

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

        public string ElapsedDisplay
        {
            get
            {
                int mins = ElapsedSeconds / 60;
                int secs = ElapsedSeconds % 60;
                return $"{mins:D2}:{secs:D2} / {ScanDurationSeconds:D2}s";
            }
        }

        public int ScanDurationSeconds
        {
            get => _scanDurationSeconds;
            set
            {
                if (SetProperty(ref _scanDurationSeconds, value))
                {
                    OnPropertyChanged(nameof(ElapsedDisplay));
                }
            }
        }

        public DiscoveryMode SelectedDiscoveryMode
        {
            get => _selectedDiscoveryMode;
            set
            {
                if (SetProperty(ref _selectedDiscoveryMode, value))
                {
                    OnPropertyChanged(nameof(DiscoveryModeDisplay));
                }
            }
        }

        public string DiscoveryModeDisplay
        {
            get
            {
                switch (SelectedDiscoveryMode)
                {
                    case DiscoveryMode.DualMode: return "Dual Stack (Classic + BLE)";
                    case DiscoveryMode.ClassicOnly: return "Classic Inquiry Only (BR/EDR)";
                    case DiscoveryMode.BleOnly: return "BLE Watcher Only";
                    default: return "Bluetooth Discovery";
                }
            }
        }

        public BluetoothAdapterInfo SelectedAdapter
        {
            get => _selectedAdapter;
            set => SetProperty(ref _selectedAdapter, value);
        }

        public string SelectedFilter
        {
            get => _selectedFilter;
            set
            {
                if (SetProperty(ref _selectedFilter, value))
                {
                    FilteredDevices.Refresh();
                }
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
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

        // Metrics
        public int TotalCount => DiscoveredDevices.Count;
        public int ClassicCount => DiscoveredDevices.Count(d => d.DeviceType == DeviceType.Classic || d.DeviceType == DeviceType.DualMode);
        public int BleCount => DiscoveredDevices.Count(d => d.DeviceType == DeviceType.LowEnergy || d.DeviceType == DeviceType.DualMode);
        public int AudioCount => DiscoveredDevices.Count(d => d.IsAudio);
        public int PeripheralsCount => DiscoveredDevices.Count(d => d.IsPeripheral);

        // Pairing State
        public bool IsPairingInProgress
        {
            get => _isPairingInProgress;
            set
            {
                if (SetProperty(ref _isPairingInProgress, value))
                {
                    OnPropertyChanged(nameof(CanPairSelectedDevice));
                }
            }
        }

        public string PairingStatusText
        {
            get => _pairingStatusText;
            set => SetProperty(ref _pairingStatusText, value);
        }

        public string PinInput
        {
            get => _pinInput;
            set => SetProperty(ref _pinInput, value);
        }

        public bool ShowPinInput
        {
            get => _showPinInput;
            set => SetProperty(ref _showPinInput, value);
        }

        public bool ShowNumericComparison
        {
            get => _showNumericComparison;
            set => SetProperty(ref _showNumericComparison, value);
        }

        public string NumericComparisonPin
        {
            get => _numericComparisonPin;
            set => SetProperty(ref _numericComparisonPin, value);
        }

        public DeviceOperationResult LastPairingResult
        {
            get => _lastPairingResult;
            set => SetProperty(ref _lastPairingResult, value);
        }

        public string PostPairingMessage
        {
            get => _postPairingMessage;
            set => SetProperty(ref _postPairingMessage, value);
        }

        public bool CanPairSelectedDevice => SelectedDevice != null && !SelectedDevice.IsPaired && !IsPairingInProgress;
        public bool CanConnectSelectedDevice => SelectedDevice != null && SelectedDevice.IsPaired && !SelectedDevice.IsConnected && !IsConnecting && !IsDisconnecting;
        public bool CanDisconnectSelectedDevice => SelectedDevice != null && SelectedDevice.IsConnected && !IsConnecting && !IsDisconnecting;

        // Commands
        public ICommand ToggleScanCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand SetFilterCommand { get; }
        public ICommand SwitchToListViewCommand { get; }
        public ICommand SwitchToRadarViewCommand { get; }
        public ICommand SelectRadarBlipCommand { get; }
        public ICommand TogglePinInputCommand { get; }
        public ICommand PairSelectedDeviceCommand { get; }
        public ICommand ConfirmNumericComparisonCommand { get; }
        public ICommand RejectNumericComparisonCommand { get; }
        public ICommand ConnectSelectedDeviceCommand { get; }
        public ICommand DisconnectSelectedDeviceCommand { get; }
        public ICommand UnpairSelectedDeviceCommand { get; }
        public ICommand NavigateToAudioDiagnosticsCommand { get; }
        public ICommand NavigateToBluetoothDevicesCommand { get; }

        public DeviceScannerViewModel(
            DeviceDiscoveryService discoveryService, 
            DeviceManagementService managementService, 
            Action<string> navigationCallback = null)
        {
            _discoveryService = discoveryService;
            _managementService = managementService;
            _navigationCallback = navigationCallback;

            if (!OperatingSystemInspector.IsWindows10OrGreater())
            {
                _selectedDiscoveryMode = DiscoveryMode.ClassicOnly;
            }

            FilteredDevices = CollectionViewSource.GetDefaultView(DiscoveredDevices);
            FilteredDevices.Filter = FilterDevicePredicate;

            _discoveryService.DeviceDiscovered += OnDeviceDiscovered;
            _discoveryService.DeviceUpdated += OnDeviceUpdated;
            _discoveryService.ScanningStateChanged += (s, scanning) => IsScanning = scanning;
            _discoveryService.StatusChanged += (s, msg) => StatusMessage = msg;

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (s, e) =>
            {
                ElapsedSeconds++;
                if (ElapsedSeconds >= ScanDurationSeconds)
                {
                    _discoveryService.StopScanning();
                    _timer.Stop();
                    IsScanning = false;
                    StatusMessage = $"Scan timed out. Discovered {DiscoveredDevices.Count} nearby devices.";
                }
            };

            ToggleScanCommand = new RelayCommand(async () =>
            {
                if (IsScanning)
                {
                    _discoveryService.StopScanning();
                    _timer.Stop();
                    IsScanning = false;
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
                _discoveryService.Clear();
                SelectedDevice = null;
                UpdateMetrics();
                StatusMessage = "Discovered device catalog cleared.";
            }, () => !IsScanning);

            SetFilterCommand = new RelayCommand(param =>
            {
                SelectedFilter = param?.ToString() ?? "All";
            });

            TogglePinInputCommand = new RelayCommand(() =>
            {
                ShowPinInput = !ShowPinInput;
            });

            SwitchToListViewCommand = new RelayCommand(() => IsRadarView = false);
            SwitchToRadarViewCommand = new RelayCommand(() => IsRadarView = true);
            SelectRadarBlipCommand = new RelayCommand(param =>
            {
                if (param is RadarBlipItem blip && blip.Device != null)
                {
                    SelectedDevice = blip.Device;
                }
                else if (param is BluetoothDeviceInfo dev)
                {
                    SelectedDevice = dev;
                }
            });

            PairSelectedDeviceCommand = new RelayCommand(async () =>
            {
                await ExecutePairSelectedDeviceAsync();
            }, () => CanPairSelectedDevice);

            ConfirmNumericComparisonCommand = new RelayCommand(() =>
            {
                ShowNumericComparison = false;
                _numericComparisonTcs?.TrySetResult(true);
            });

            RejectNumericComparisonCommand = new RelayCommand(() =>
            {
                ShowNumericComparison = false;
                _numericComparisonTcs?.TrySetResult(false);
            });

            ConnectSelectedDeviceCommand = new RelayCommand(async () =>
            {
                await ExecuteConnectSelectedDeviceAsync();
            }, () => CanConnectSelectedDevice);

            DisconnectSelectedDeviceCommand = new RelayCommand(async () =>
            {
                await ExecuteDisconnectSelectedDeviceAsync();
            }, () => CanDisconnectSelectedDevice);

            UnpairSelectedDeviceCommand = new RelayCommand(async () =>
            {
                await ExecuteUnpairSelectedDeviceAsync();
            });

            NavigateToAudioDiagnosticsCommand = new RelayCommand(() =>
            {
                _navigationCallback?.Invoke("Audio & Packet Diagnostics");
            });

            NavigateToBluetoothDevicesCommand = new RelayCommand(() =>
            {
                _navigationCallback?.Invoke("Bluetooth Devices");
            });

            LoadAdaptersAsync();
        }

        private async void LoadAdaptersAsync()
        {
            try
            {
                var inspector = new BlueDoc.Infrastructure.Hardware.SetupApiHardwareInspector();
                var adapters = await Task.Run(() => inspector.EnumerateAdapters());
                RunOnUI(() =>
                {
                    AvailableAdapters.Clear();
                    foreach (var a in adapters)
                    {
                        AvailableAdapters.Add(a);
                    }
                    SelectedAdapter = AvailableAdapters.FirstOrDefault(a => a.Status == AdapterStatus.Operational && (a.HardwareDetails.BusType == "USB" || a.HardwareDetails.BusType == "PCI") && a.HardwareDetails.IsValid)
                                   ?? AvailableAdapters.FirstOrDefault(a => a.Status == AdapterStatus.Operational && a.HardwareDetails.IsValid)
                                   ?? AvailableAdapters.FirstOrDefault(a => a.Status == AdapterStatus.Operational) 
                                   ?? AvailableAdapters.FirstOrDefault();
                });
            }
            catch { }
        }

        public async Task ExecuteScanAsync()
        {
            DiscoveredDevices.Clear();
            _discoveryService.Clear();
            SelectedDevice = null;
            ElapsedSeconds = 0;
            IsScanning = true;
            PairingStage = BluetoothPairingStage.Discovering;
            _timer.Start();
            UpdateMetrics();

            StatusMessage = $"Scanning nearby devices via {DiscoveryModeDisplay}...";

            try
            {
                await _discoveryService.StartScanningAsync(ScanDurationSeconds, SelectedDiscoveryMode);
                StatusMessage = $"Scan completed. Found {DiscoveredDevices.Count} devices.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Scan error: {ex.Message}";
            }
            finally
            {
                _timer.Stop();
                IsScanning = false;
                if (PairingStage == BluetoothPairingStage.Discovering)
                {
                    PairingStage = BluetoothPairingStage.Idle;
                }
                UpdateMetrics();
            }
        }

        private async Task ExecutePairSelectedDeviceAsync()
        {
            if (SelectedDevice == null) return;

            IsPairingInProgress = true;
            PairingStage = BluetoothPairingStage.PairingRequested;
            PairingStatusText = $"Pairing with '{SelectedDevice.DisplayName}'...";
            LastPairingResult = null;
            PostPairingMessage = string.Empty;

            try
            {
                Func<string, Task<bool>> numericCallback = pin =>
                {
                    PairingStage = BluetoothPairingStage.WaitingForAuthentication;
                    var tcs = new TaskCompletionSource<bool>();
                    _numericComparisonTcs = tcs;
                    RunOnUI(() =>
                    {
                        NumericComparisonPin = pin;
                        ShowNumericComparison = true;
                        PairingStatusText = $"Confirm passkey: {pin}";
                    });
                    return tcs.Task;
                };

                string pinToUse = ShowPinInput && !string.IsNullOrWhiteSpace(PinInput) ? PinInput : null;
                if (pinToUse != null)
                {
                    PairingStage = BluetoothPairingStage.Authenticating;
                }

                var result = await _managementService.PairDeviceAsync(
                    SelectedDevice, 
                    pinToUse, 
                    numericCallback);

                LastPairingResult = result;

                if (result.Success)
                {
                    PairingStage = BluetoothPairingStage.PairingCompleted;
                    SelectedDevice.IsPaired = true;
                    SelectedDevice.IsRemembered = true;
                    PairingStatusText = $"Successfully paired with '{SelectedDevice.DisplayName}'.";

                    // Re-query installed services to verify connection readiness
                    await LoadServicesForSelectedDevice();

                    if (SelectedDevice.IsAudio)
                    {
                        PostPairingMessage = "Device is paired. Click 'Connect Device' to establish active audio streaming (A2DP / Handsfree). Note: Pairing registers security keys, while Connecting enables sound.";
                    }
                    else if (SelectedDevice.IsPeripheral)
                    {
                        PostPairingMessage = "Peripheral paired. Windows is binding the HID driver. Device should become active automatically.";
                    }
                    else
                    {
                        PostPairingMessage = "Pairing successful. You can now manage connections directly in BlueDoc.";
                    }
                }
                else
                {
                    PairingStage = BluetoothPairingStage.PairingFailed;
                    PairingStatusText = $"Pairing failed: {result.Message}";
                }
            }
            catch (Exception ex)
            {
                PairingStage = BluetoothPairingStage.PairingFailed;
                LastPairingResult = DeviceOperationResult.Fail($"Pairing error: {ex.Message}");
                PairingStatusText = $"Pairing failed: {ex.Message}";
            }
            finally
            {
                IsPairingInProgress = false;
                ShowNumericComparison = false;
                OnPropertyChanged(nameof(CanPairSelectedDevice));
                OnPropertyChanged(nameof(CanConnectSelectedDevice));
                OnPropertyChanged(nameof(CanDisconnectSelectedDevice));
            }
        }

        private async Task ExecuteConnectSelectedDeviceAsync()
        {
            if (SelectedDevice == null) return;

            IsConnecting = true;
            ConnectionStage = BluetoothConnectionStage.PreparingConnection;
            PairingStatusText = $"Connecting '{SelectedDevice.DisplayName}'...";
            ConnectionStage = BluetoothConnectionStage.Connecting;

            try
            {
                var result = await _managementService.ConnectDeviceAsync(SelectedDevice);
                ConnectionStage = BluetoothConnectionStage.VerifyingState;
                LastPairingResult = result;
                if (result.Success)
                {
                    ConnectionStage = BluetoothConnectionStage.Connected;
                    SelectedDevice.IsConnected = true;
                    PairingStatusText = $"Connected to '{SelectedDevice.DisplayName}'. Audio/data link established.";
                    PostPairingMessage = "Peripheral is now connected and operational in Windows!";
                }
                else
                {
                    ConnectionStage = BluetoothConnectionStage.ConnectionFailed;
                    PairingStatusText = $"Connection failed: {result.Message}";
                }
            }
            catch (Exception ex)
            {
                ConnectionStage = BluetoothConnectionStage.ConnectionFailed;
                PairingStatusText = $"Connection error: {ex.Message}";
            }
            finally
            {
                IsConnecting = false;
                OnPropertyChanged(nameof(CanConnectSelectedDevice));
                OnPropertyChanged(nameof(CanDisconnectSelectedDevice));
            }
        }

        private async Task ExecuteDisconnectSelectedDeviceAsync()
        {
            if (SelectedDevice == null) return;

            IsDisconnecting = true;
            PairingStatusText = $"Disconnecting '{SelectedDevice.DisplayName}'...";
            try
            {
                var result = await _managementService.DisconnectDeviceAsync(SelectedDevice);
                LastPairingResult = result;
                if (result.Success)
                {
                    SelectedDevice.IsConnected = false;
                    ConnectionStage = BluetoothConnectionStage.Idle;
                    PairingStatusText = $"Disconnected '{SelectedDevice.DisplayName}'.";
                }
                else
                {
                    PairingStatusText = $"Disconnection failed: {result.Message}";
                }
            }
            catch (Exception ex)
            {
                PairingStatusText = $"Disconnection error: {ex.Message}";
            }
            finally
            {
                IsDisconnecting = false;
                OnPropertyChanged(nameof(CanConnectSelectedDevice));
                OnPropertyChanged(nameof(CanDisconnectSelectedDevice));
            }
        }

        private async Task ExecuteUnpairSelectedDeviceAsync()
        {
            if (SelectedDevice == null) return;

            try
            {
                bool removed = await _managementService.UnpairDeviceAsync(SelectedDevice.BluetoothAddress);
                if (removed)
                {
                    SelectedDevice.IsPaired = false;
                    SelectedDevice.IsConnected = false;
                    SelectedDevice.IsRemembered = false;
                    PairingStatusText = $"Device '{SelectedDevice.DisplayName}' removed from Windows pairing records.";
                    PostPairingMessage = string.Empty;
                }
                else
                {
                    PairingStatusText = "Could not remove device. Access denied or device already unlinked.";
                }
            }
            catch (Exception ex)
            {
                PairingStatusText = $"Unpair error: {ex.Message}";
            }
            finally
            {
                OnPropertyChanged(nameof(CanPairSelectedDevice));
                OnPropertyChanged(nameof(CanConnectSelectedDevice));
                OnPropertyChanged(nameof(CanDisconnectSelectedDevice));
            }
        }

        private async Task LoadServicesForSelectedDevice()
        {
            SelectedDeviceServices.Clear();
            if (SelectedDevice == null) return;

            try
            {
                var services = await _managementService.GetDeviceInstalledServicesAsync(SelectedDevice);
                RunOnUI(() =>
                {
                    SelectedDeviceServices.Clear();
                    foreach (var s in services)
                    {
                        SelectedDeviceServices.Add(s);
                    }
                });
            }
            catch { }
        }

        private void OnDeviceDiscovered(object sender, BluetoothDeviceInfo device)
        {
            RunOnUI(() =>
            {
                var existing = DiscoveredDevices.FirstOrDefault(d => string.Equals(d.BluetoothAddress, device.BluetoothAddress, StringComparison.OrdinalIgnoreCase));
                if (existing == null)
                {
                    DiscoveredDevices.Add(device);
                    if (SelectedDevice == null)
                    {
                        SelectedDevice = device;
                    }
                    UpdateMetrics();
                }
            });
        }

        private void OnDeviceUpdated(object sender, BluetoothDeviceInfo device)
        {
            RunOnUI(() =>
            {
                // In-place updates trigger INotifyPropertyChanged on device instance
                UpdateMetrics();
            });
        }

        private void UpdateMetrics()
        {
            OnPropertyChanged(nameof(TotalCount));
            OnPropertyChanged(nameof(ClassicCount));
            OnPropertyChanged(nameof(BleCount));
            OnPropertyChanged(nameof(AudioCount));
            OnPropertyChanged(nameof(PeripheralsCount));
            OnPropertyChanged(nameof(ShowScanSkeleton));
            OnPropertyChanged(nameof(ShowEmptyState));
            UpdateRadarBlips();
        }

        public void UpdateRadarBlips()
        {
            RunOnUI(() =>
            {
                RadarBlips.Clear();
                var devices = FilteredDevices?.OfType<BluetoothDeviceInfo>().ToList() ?? new List<BluetoothDeviceInfo>();
                if (devices.Count == 0) return;

                double cx = 220;
                double cy = 220;
                double maxR = 175;

                for (int i = 0; i < devices.Count; i++)
                {
                    var dev = devices[i];
                    double normRadius;
                    double approxMeters;

                    if (dev.Rssi.HasValue)
                    {
                        int rssi = dev.Rssi.Value;
                        if (rssi >= -52)
                        {
                            normRadius = 0.25;
                            approxMeters = 0.8;
                        }
                        else if (rssi >= -68)
                        {
                            normRadius = 0.48;
                            approxMeters = 2.5;
                        }
                        else if (rssi >= -82)
                        {
                            normRadius = 0.72;
                            approxMeters = 6.0;
                        }
                        else
                        {
                            normRadius = 0.90;
                            approxMeters = 12.0;
                        }
                    }
                    else
                    {
                        if (dev.IsConnected)
                        {
                            normRadius = 0.28;
                            approxMeters = 1.0;
                        }
                        else if (dev.IsPaired)
                        {
                            normRadius = 0.55;
                            approxMeters = 4.0;
                        }
                        else
                        {
                            normRadius = 0.82;
                            approxMeters = 8.5;
                        }
                    }

                    double angleDeg = (i * (360.0 / Math.Max(1, devices.Count))) + 28.0;
                    double rad = angleDeg * Math.PI / 180.0;
                    double r = normRadius * maxR;

                    double x = cx + r * Math.Cos(rad) - 22;
                    double y = cy + r * Math.Sin(rad) - 22;

                    RadarBlips.Add(new RadarBlipItem(dev, x, y, approxMeters, angleDeg));
                }
            });
        }

        private bool FilterDevicePredicate(object obj)
        {
            if (obj is BluetoothDeviceInfo d)
            {
                // Search Text filter
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    string q = SearchText.Trim();
                    bool nameMatches = d.DisplayName != null && d.DisplayName.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0;
                    bool macMatches = d.BluetoothAddress != null && d.BluetoothAddress.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0;
                    if (!nameMatches && !macMatches) return false;
                }

                // Category Filter
                switch (SelectedFilter)
                {
                    case "All": return true;
                    case "Classic": return d.DeviceType == DeviceType.Classic || d.DeviceType == DeviceType.DualMode;
                    case "BLE": return d.DeviceType == DeviceType.LowEnergy || d.DeviceType == DeviceType.DualMode;
                    case "Audio": return d.IsAudio;
                    case "Headphones": return d.IsHeadphone;
                    case "Speakers": return d.IsSpeaker;
                    case "Keyboards": return d.IsKeyboard;
                    case "Mice": return d.IsMouse;
                    case "Phones": return d.IsPhone;
                    case "Sensors": return d.IsSensor;
                    case "Unknown": return d.IsUnknown;
                    default: return true;
                }
            }
            return false;
        }

        private void RunOnUI(Action action)
        {
            var dispatcher = System.Windows.Application.Current?.Dispatcher;
            if (dispatcher != null && !dispatcher.CheckAccess())
            {
                dispatcher.BeginInvoke(action);
            }
            else
            {
                action?.Invoke();
            }
        }
    }
}
