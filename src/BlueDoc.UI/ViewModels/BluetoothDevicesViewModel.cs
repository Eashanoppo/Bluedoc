using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using BlueDoc.Application.Services;
using BlueDoc.Core.Enums;
using BlueDoc.Core.Models;
using BlueDoc.UI.Common;

namespace BlueDoc.UI.ViewModels
{
    public class BluetoothDevicesViewModel : ViewModelBase
    {
        private readonly DeviceManagementService _managementService;
        private readonly Action<string> _navigateCallback;

        private BluetoothDeviceInfo _selectedDevice;
        private bool _isLoading;
        private bool _isRefreshing;
        private bool _isDeviceBusy;
        private BluetoothConnectionStage _connectionStage = BluetoothConnectionStage.Idle;
        private BluetoothPairingStage _pairingStage = BluetoothPairingStage.Idle;
        private string _statusMessage = "Ready";
        private string _searchText = string.Empty;
        private string _selectedFilter = "All";

        // In-App Native Pairing Workflow State
        private bool _isPairingModalOpen;
        private bool _isDiscoveringNearby;
        private BluetoothDeviceInfo _selectedNearbyDevice;
        private string _pairingPin = string.Empty;
        private string _pairingStatusText = string.Empty;
        private string _pairingStep = "Discovery"; // Discovery, Confirm, Pairing, Success, Failed
        private string _pairingErrorMessage = string.Empty;
        private CancellationTokenSource _pairingCts;

        public ObservableCollection<BluetoothDeviceInfo> PairedDevices { get; } = new ObservableCollection<BluetoothDeviceInfo>();
        public ObservableCollection<BluetoothDeviceInfo> DiscoveredNearbyDevices { get; } = new ObservableCollection<BluetoothDeviceInfo>();
        public ObservableCollection<string> SelectedDeviceServices { get; } = new ObservableCollection<string>();
        public ICollectionView FilteredDevices { get; }

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
                    _ = LoadSelectedDeviceServicesAsync();
                }
            }
        }

        public bool HasSelectedDevice => SelectedDevice != null;
        public bool CanConnectSelectedDevice => SelectedDevice != null && !SelectedDevice.IsConnected && !IsDeviceBusy;
        public bool CanDisconnectSelectedDevice => SelectedDevice != null && SelectedDevice.IsConnected && !IsDeviceBusy;

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

        public BluetoothConnectionStage ConnectionStage
        {
            get => _connectionStage;
            set
            {
                if (SetProperty(ref _connectionStage, value))
                {
                    OnPropertyChanged(nameof(ConnectionStageDisplay));
                    OnPropertyChanged(nameof(IsConnecting));
                }
            }
        }

        public bool IsConnecting => ConnectionStage == BluetoothConnectionStage.PreparingConnection ||
                                    ConnectionStage == BluetoothConnectionStage.Connecting ||
                                    ConnectionStage == BluetoothConnectionStage.VerifyingState;

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

        public BluetoothPairingStage PairingStage
        {
            get => _pairingStage;
            set
            {
                if (SetProperty(ref _pairingStage, value))
                {
                    OnPropertyChanged(nameof(PairingStageDisplay));
                    OnPropertyChanged(nameof(IsPairing));
                }
            }
        }

        public bool IsPairing => PairingStage == BluetoothPairingStage.PairingRequested ||
                                 PairingStage == BluetoothPairingStage.WaitingForAuthentication ||
                                 PairingStage == BluetoothPairingStage.Authenticating;

        public string PairingStageDisplay
        {
            get
            {
                switch (PairingStage)
                {
                    case BluetoothPairingStage.Discovering:
                        return "Discovering Nearby Devices...";
                    case BluetoothPairingStage.PairingRequested:
                        return "Pairing Handshake Requested...";
                    case BluetoothPairingStage.WaitingForAuthentication:
                        return "Waiting for Authentication...";
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

        public bool IsDeviceBusy
        {
            get => _isDeviceBusy;
            set
            {
                if (SetProperty(ref _isDeviceBusy, value))
                {
                    OnPropertyChanged(nameof(CanConnectSelectedDevice));
                    OnPropertyChanged(nameof(CanDisconnectSelectedDevice));
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
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

        public int TotalRegisteredCount => PairedDevices.Count;
        public int ConnectedCount => PairedDevices.Count(d => d.IsConnected);
        public int DisconnectedCount => PairedDevices.Count(d => !d.IsConnected);

        // Pairing Modal Properties
        public bool IsPairingModalOpen
        {
            get => _isPairingModalOpen;
            set => SetProperty(ref _isPairingModalOpen, value);
        }

        public bool IsDiscoveringNearby
        {
            get => _isDiscoveringNearby;
            set => SetProperty(ref _isDiscoveringNearby, value);
        }

        public BluetoothDeviceInfo SelectedNearbyDevice
        {
            get => _selectedNearbyDevice;
            set
            {
                if (SetProperty(ref _selectedNearbyDevice, value))
                {
                    OnPropertyChanged(nameof(HasSelectedNearbyDevice));
                    if (value != null && PairingStep == "Discovery")
                    {
                        PairingStep = "Confirm";
                    }
                }
            }
        }

        public bool HasSelectedNearbyDevice => SelectedNearbyDevice != null;

        public string PairingPin
        {
            get => _pairingPin;
            set => SetProperty(ref _pairingPin, value);
        }

        public string PairingStatusText
        {
            get => _pairingStatusText;
            set => SetProperty(ref _pairingStatusText, value);
        }

        public string PairingStep
        {
            get => _pairingStep;
            set => SetProperty(ref _pairingStep, value);
        }

        public string PairingErrorMessage
        {
            get => _pairingErrorMessage;
            set => SetProperty(ref _pairingErrorMessage, value);
        }

        // Commands
        public ICommand RefreshCommand { get; }
        public ICommand SetFilterCommand { get; }
        public ICommand ConnectCommand { get; }
        public ICommand DisconnectCommand { get; }
        public ICommand UnpairDeviceCommand { get; }
        public ICommand DiagnoseDeviceCommand { get; }
        public ICommand ViewHistoryCommand { get; }
        public ICommand OpenDeviceManagerCommand { get; }

        // Pairing Modal Commands
        public ICommand OpenPairingModalCommand { get; }
        public ICommand ClosePairingModalCommand { get; }
        public ICommand RestartDiscoveryCommand { get; }
        public ICommand ExecutePairCommand { get; }

        public BluetoothDevicesViewModel(DeviceManagementService managementService, Action<string> navigateCallback = null)
        {
            _managementService = managementService;
            _navigateCallback = navigateCallback;

            FilteredDevices = CollectionViewSource.GetDefaultView(PairedDevices);
            FilteredDevices.Filter = FilterDevicePredicate;

            RefreshCommand = new RelayCommand(async () => await LoadPairedDevicesAsync());

            SetFilterCommand = new RelayCommand(param =>
            {
                SelectedFilter = param?.ToString() ?? "All";
            });

            ConnectCommand = new RelayCommand(async () =>
            {
                if (SelectedDevice == null || IsDeviceBusy) return;

                IsDeviceBusy = true;
                ConnectionStage = BluetoothConnectionStage.PreparingConnection;
                var op = NotificationService.Instance.ShowRunning("Connecting Device", $"Connecting to '{SelectedDevice.DisplayName}'...");
                StatusMessage = $"Establishing connection to '{SelectedDevice.DisplayName}'...";
                ConnectionStage = BluetoothConnectionStage.Connecting;

                var result = await _managementService.ConnectDeviceAsync(SelectedDevice);
                ConnectionStage = BluetoothConnectionStage.VerifyingState;
                if (result.Success)
                {
                    ConnectionStage = BluetoothConnectionStage.Connected;
                    op.CompleteSuccess("Connected", result.Message);
                    StatusMessage = result.Message;
                    await LoadPairedDevicesAsync();
                }
                else
                {
                    ConnectionStage = BluetoothConnectionStage.ConnectionFailed;
                    op.CompleteError("Connection Failed", result.Message);
                    StatusMessage = result.Message;
                }
                IsDeviceBusy = false;
            }, () => CanConnectSelectedDevice);

            DisconnectCommand = new RelayCommand(async () =>
            {
                if (SelectedDevice == null || IsDeviceBusy) return;

                IsDeviceBusy = true;
                var op = NotificationService.Instance.ShowRunning("Disconnecting Device", $"Disconnecting from '{SelectedDevice.DisplayName}'...");
                StatusMessage = $"Disconnecting from '{SelectedDevice.DisplayName}'...";

                var result = await _managementService.DisconnectDeviceAsync(SelectedDevice);
                if (result.Success)
                {
                    op.CompleteSuccess("Disconnected", result.Message);
                    StatusMessage = result.Message;
                    await LoadPairedDevicesAsync();
                }
                else
                {
                    op.CompleteError("Disconnection Failed", result.Message);
                    StatusMessage = result.Message;
                }
                IsDeviceBusy = false;
            }, () => CanDisconnectSelectedDevice);

            UnpairDeviceCommand = new RelayCommand(async () =>
            {
                if (SelectedDevice == null || IsDeviceBusy) return;

                var confirm = MessageBox.Show(
                    $"Are you sure you want to remove and unpair '{SelectedDevice.DisplayName}' ({SelectedDevice.BluetoothAddress}) from Windows?\n\nYou will need to put the device in pairing mode to reconnect.",
                    "Unpair Bluetooth Device Confirmation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirm == MessageBoxResult.Yes)
                {
                    IsDeviceBusy = true;
                    string targetName = SelectedDevice.DisplayName;
                    string targetAddress = SelectedDevice.BluetoothAddress;
                    StatusMessage = $"Removing device '{targetName}' from Windows...";

                    bool success = await _managementService.UnpairDeviceAsync(targetAddress);
                    if (success)
                    {
                        NotificationService.Instance.ShowSuccess("Device Removed", $"'{targetName}' was successfully removed from Windows.");
                        StatusMessage = $"Device '{targetName}' removed successfully.";
                        await LoadPairedDevicesAsync();
                    }
                    else
                    {
                        NotificationService.Instance.ShowWarning("Removal Incomplete", $"Could not unpair '{targetName}'. The peripheral may be actively in use.");
                        StatusMessage = $"Could not remove device '{targetName}'. Device may be locked by an active Windows driver.";
                    }
                    IsDeviceBusy = false;
                }
            }, () => SelectedDevice != null && !IsDeviceBusy);

            DiagnoseDeviceCommand = new RelayCommand(() =>
            {
                if (SelectedDevice != null)
                {
                    NotificationService.Instance.ShowInfo("Diagnostic Center", $"Initiating diagnostics test targeting '{SelectedDevice.DisplayName}'...");
                    _navigateCallback?.Invoke("Diagnostic Test Center");
                }
            }, () => SelectedDevice != null);

            ViewHistoryCommand = new RelayCommand(() =>
            {
                if (SelectedDevice != null)
                {
                    _navigateCallback?.Invoke("Device Connection History");
                }
            }, () => SelectedDevice != null);

            OpenDeviceManagerCommand = new RelayCommand(() =>
            {
                _managementService.LaunchDeviceManager();
            });

            // Pairing Modal Wiring
            OpenPairingModalCommand = new RelayCommand(async () =>
            {
                await StartInAppPairingWorkflowAsync();
            });

            ClosePairingModalCommand = new RelayCommand(() =>
            {
                CancelInAppPairing();
                IsPairingModalOpen = false;
            });

            RestartDiscoveryCommand = new RelayCommand(async () =>
            {
                await StartInAppPairingWorkflowAsync();
            });

            ExecutePairCommand = new RelayCommand(async () =>
            {
                await ExecuteNativePairingAsync();
            }, () => HasSelectedNearbyDevice && PairingStep != "Pairing");
        }

        public async Task LoadPairedDevicesAsync()
        {
            if (PairedDevices.Count == 0)
            {
                IsLoading = true;
            }
            else
            {
                IsRefreshing = true;
            }

            StatusMessage = "Retrieving registered Bluetooth devices from Windows Configuration Manager & PnP...";

            try
            {
                var list = await _managementService.GetPairedDevicesAsync();

                RunOnUI(() =>
                {
                    PairedDevices.Clear();
                    foreach (var device in list)
                    {
                        PairedDevices.Add(device);
                    }

                    OnPropertyChanged(nameof(TotalRegisteredCount));
                    OnPropertyChanged(nameof(ConnectedCount));
                    OnPropertyChanged(nameof(DisconnectedCount));

                    if (PairedDevices.Count > 0)
                    {
                        if (SelectedDevice == null || !PairedDevices.Any(d => d.BluetoothAddress == SelectedDevice.BluetoothAddress))
                        {
                            SelectedDevice = PairedDevices[0];
                        }
                        else
                        {
                            SelectedDevice = PairedDevices.FirstOrDefault(d => d.BluetoothAddress == SelectedDevice.BluetoothAddress);
                        }
                        StatusMessage = $"Found {PairedDevices.Count} registered Bluetooth peripheral(s). {ConnectedCount} connected, {DisconnectedCount} disconnected.";
                    }
                    else
                    {
                        SelectedDevice = null;
                        StatusMessage = "No paired Bluetooth devices currently registered in Windows.";
                    }
                });
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error retrieving paired devices: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
                IsRefreshing = false;
            }
        }

        private async Task LoadSelectedDeviceServicesAsync()
        {
            if (SelectedDevice == null)
            {
                SelectedDeviceServices.Clear();
                return;
            }

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

        private static void RunOnUI(Action action)
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

        private bool FilterDevicePredicate(object obj)
        {
            if (!(obj is BluetoothDeviceInfo device)) return false;

            // Search filter
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string query = SearchText.Trim();
                bool matchesName = device.DisplayName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
                bool matchesAddress = device.BluetoothAddress.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
                bool matchesCategory = device.CategoryName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
                if (!matchesName && !matchesAddress && !matchesCategory) return false;
            }

            // Category / State filter
            switch (SelectedFilter)
            {
                case "Connected":
                    return device.IsConnected;
                case "Disconnected":
                    return !device.IsConnected;
                case "Audio":
                    return device.MajorDeviceClass == "Audio/Video" ||
                           device.CategoryName.IndexOf("Headset", StringComparison.OrdinalIgnoreCase) >= 0 ||
                           device.CategoryName.IndexOf("Audio", StringComparison.OrdinalIgnoreCase) >= 0 ||
                           device.DisplayName.IndexOf("Buds", StringComparison.OrdinalIgnoreCase) >= 0 ||
                           device.DisplayName.IndexOf("Speaker", StringComparison.OrdinalIgnoreCase) >= 0;
                case "Peripherals":
                    return device.MajorDeviceClass == "Peripheral" ||
                           device.CategoryName.IndexOf("Mouse", StringComparison.OrdinalIgnoreCase) >= 0 ||
                           device.CategoryName.IndexOf("Keyboard", StringComparison.OrdinalIgnoreCase) >= 0;
                case "BLE":
                    return device.DeviceType == DeviceType.LowEnergy;
                case "Classic":
                    return device.DeviceType == DeviceType.Classic;
                default:
                    return true;
            }
        }

        private async Task StartInAppPairingWorkflowAsync()
        {
            CancelInAppPairing();
            _pairingCts = new CancellationTokenSource();

            DiscoveredNearbyDevices.Clear();
            SelectedNearbyDevice = null;
            PairingPin = string.Empty;
            PairingStep = "Discovery";
            PairingErrorMessage = string.Empty;
            PairingStage = BluetoothPairingStage.Discovering;
            PairingStatusText = "Scanning on 2.4 GHz Bluetooth spectrum for nearby discoverable peripherals...";
            IsDiscoveringNearby = true;
            IsPairingModalOpen = true;

            var progress = new Progress<BluetoothDeviceInfo>(discovered =>
            {
                System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                {
                    if (!DiscoveredNearbyDevices.Any(d => string.Equals(d.BluetoothAddress, discovered.BluetoothAddress, StringComparison.OrdinalIgnoreCase)))
                    {
                        DiscoveredNearbyDevices.Add(discovered);
                        PairingStatusText = $"Discovered {DiscoveredNearbyDevices.Count} nearby Bluetooth device(s)...";
                    }
                });
            });

            try
            {
                await _managementService.DiscoverNearbyDevicesAsync(10, progress, _pairingCts.Token);
                PairingStatusText = DiscoveredNearbyDevices.Count > 0
                    ? $"Discovery complete. Found {DiscoveredNearbyDevices.Count} peripheral(s). Select a device to pair."
                    : "No nearby discoverable devices found. Ensure peripheral is in pairing mode.";
            }
            catch (OperationCanceledException)
            {
                PairingStatusText = "Device discovery stopped.";
            }
            catch (Exception ex)
            {
                PairingStatusText = $"Discovery error: {ex.Message}";
            }
            finally
            {
                IsDiscoveringNearby = false;
            }
        }

        private async Task ExecuteNativePairingAsync()
        {
            if (SelectedNearbyDevice == null) return;

            PairingStage = BluetoothPairingStage.PairingRequested;
            PairingStep = "Pairing";
            PairingStatusText = $"Initiating native Windows pairing handshake with '{SelectedNearbyDevice.DisplayName}'...";
            PairingErrorMessage = string.Empty;

            PairingStage = BluetoothPairingStage.Authenticating;
            var result = await _managementService.PairDeviceAsync(SelectedNearbyDevice, PairingPin);
            if (result.Success)
            {
                PairingStage = BluetoothPairingStage.PairingCompleted;
                PairingStep = "Success";
                PairingStatusText = result.Message;
                NotificationService.Instance.ShowSuccess("Pairing Successful", $"'{SelectedNearbyDevice.DisplayName}' is now paired and registered in Windows.");
                await LoadPairedDevicesAsync();
            }
            else
            {
                PairingStage = BluetoothPairingStage.PairingFailed;
                PairingStep = "Failed";
                PairingErrorMessage = result.Message;
                PairingStatusText = "Pairing handshake did not succeed.";
                NotificationService.Instance.ShowError("Pairing Failed", result.Message);
            }
        }

        private void CancelInAppPairing()
        {
            try
            {
                _pairingCts?.Cancel();
                _pairingCts?.Dispose();
                _pairingCts = null;
            }
            catch { }
            IsDiscoveringNearby = false;
            PairingStage = BluetoothPairingStage.Idle;
        }
    }
}
