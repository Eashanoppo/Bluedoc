using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using BlueDoc.Application.Services;
using BlueDoc.Bluetooth.BLE;
using BlueDoc.Core.Enums;
using BlueDoc.Core.Models;
using BlueDoc.Infrastructure.Platform;
using BlueDoc.UI.Common;
using Microsoft.Win32;

namespace BlueDoc.UI.ViewModels
{
    public class BleExplorerViewModel : ViewModelBase
    {
        private readonly DeviceManagementService _deviceService;
        private readonly DeviceDiscoveryService _discoveryService;
        private readonly BleGattClientProvider _gattClient;

        private BluetoothDeviceInfo _selectedBleDevice;
        private GattServiceItem _selectedService;
        private bool _isScanning;
        private bool _isGattSupported;
        private string _platformNotice = string.Empty;
        private string _statusMessage = string.Empty;

        public ObservableCollection<BluetoothDeviceInfo> BleDevices { get; } = new ObservableCollection<BluetoothDeviceInfo>();
        public ObservableCollection<GattServiceItem> GattServices { get; } = new ObservableCollection<GattServiceItem>();

        public BluetoothDeviceInfo SelectedBleDevice
        {
            get => _selectedBleDevice;
            set
            {
                if (SetProperty(ref _selectedBleDevice, value))
                {
                    LoadGattProfileForSelectedDevice();
                }
            }
        }

        public GattServiceItem SelectedService
        {
            get => _selectedService;
            set => SetProperty(ref _selectedService, value);
        }

        public bool IsScanning
        {
            get => _isScanning;
            set => SetProperty(ref _isScanning, value);
        }

        public bool IsGattSupported
        {
            get => _isGattSupported;
            set => SetProperty(ref _isGattSupported, value);
        }

        public string PlatformNotice
        {
            get => _platformNotice;
            set => SetProperty(ref _platformNotice, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ICommand ScanBleDevicesCommand { get; }
        public ICommand RefreshPairedBleCommand { get; }
        public ICommand ReadCharacteristicCommand { get; }
        public ICommand SafeWriteCharacteristicCommand { get; }
        public ICommand ToggleNotifyCommand { get; }
        public ICommand ExportGattProfileCommand { get; }

        public BleExplorerViewModel(DeviceManagementService deviceService = null, DeviceDiscoveryService discoveryService = null)
        {
            _deviceService = deviceService ?? new DeviceManagementService();
            _discoveryService = discoveryService ?? new DeviceDiscoveryService();
            _gattClient = new BleGattClientProvider();

            var os = OperatingSystemInspector.GetOperatingSystemInfo();
            IsGattSupported = os.SupportsBleModernApis;

            if (IsGattSupported)
            {
                PlatformNotice = $"{os.OperatingSystemName}: Full Windows Runtime BLE and GATT Services APIs supported.";
            }
            else
            {
                PlatformNotice = $"{os.OperatingSystemName}: Legacy Windows platform detected. WinRT GATT characteristic exploration is unavailable; hardware and PnP BLE device discovery is supported via SetupAPI/BTHLE.";
            }

            ScanBleDevicesCommand = new RelayCommand(async () => await ScanBleAsync(), () => !IsScanning);
            RefreshPairedBleCommand = new RelayCommand(async () => await LoadBleDevicesAsync());

            ReadCharacteristicCommand = new RelayCommand(param =>
            {
                if (param is GattCharacteristicItem characteristic)
                {
                    ReadCharacteristic(characteristic);
                }
            });

            SafeWriteCharacteristicCommand = new RelayCommand(param =>
            {
                if (param is GattCharacteristicItem characteristic)
                {
                    PerformSafeWrite(characteristic);
                }
            });

            ToggleNotifyCommand = new RelayCommand(param =>
            {
                if (param is GattCharacteristicItem characteristic)
                {
                    ToggleNotification(characteristic);
                }
            });

            ExportGattProfileCommand = new RelayCommand(ExportGattProfile, () => SelectedBleDevice != null && GattServices.Count > 0);
        }

        public async Task LoadBleDevicesAsync()
        {
            StatusMessage = "Enumerating registered Bluetooth Low Energy devices...";
            try
            {
                var allDevices = await _deviceService.GetPairedDevicesAsync();
                BleDevices.Clear();

                foreach (var d in allDevices.Where(x => x.DeviceType == DeviceType.LowEnergy || x.DeviceType == DeviceType.DualMode || x.InstanceId.IndexOf("BTHLE", StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    BleDevices.Add(d);
                }

                if (BleDevices.Count > 0)
                {
                    SelectedBleDevice = BleDevices[0];
                    StatusMessage = $"Found {BleDevices.Count} BLE devices.";
                }
                else
                {
                    StatusMessage = "No Bluetooth Low Energy devices currently paired. Click 'Scan for BLE Devices' to discover nearby devices.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        private async void LoadGattProfileForSelectedDevice()
        {
            GattServices.Clear();
            if (SelectedBleDevice == null) return;

            // 1. Attempt live WinRT GATT discovery if running on Windows 10/11 with valid address
            if (IsGattSupported && SelectedBleDevice.RawAddress != 0)
            {
                StatusMessage = $"Connecting to '{SelectedBleDevice.DisplayName}' and querying live GATT services...";
                try
                {
                    var liveServices = await _gattClient.DiscoverLiveGattProfileAsync(SelectedBleDevice.RawAddress);
                    if (liveServices != null && liveServices.Count > 0)
                    {
                        foreach (var svc in liveServices)
                        {
                            GattServices.Add(svc);
                        }
                        if (GattServices.Count > 0)
                        {
                            SelectedService = GattServices[0];
                        }
                        StatusMessage = $"Discovered {GattServices.Count} live GATT services from '{SelectedBleDevice.DisplayName}'.";
                        return;
                    }
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Live GATT notice: {ex.Message}";
                }
            }

            // 2. Fallback to standard profile catalog template
            var profile = GattProfileCatalog.GetStandardPeripheralProfile(SelectedBleDevice.DisplayName, SelectedBleDevice.CategoryName);
            foreach (var svc in profile)
            {
                GattServices.Add(svc);
            }

            if (GattServices.Count > 0)
            {
                SelectedService = GattServices[0];
            }
            StatusMessage = $"Loaded GATT profile template for '{SelectedBleDevice.DisplayName}'.";
        }

        private async Task ScanBleAsync()
        {
            IsScanning = true;
            StatusMessage = "Scanning for active BLE beacons and advertising devices...";

            try
            {
                var discovered = await _discoveryService.StartScanningAsync(15);
                foreach (var dev in discovered)
                {
                    if (!BleDevices.Any(d => d.BluetoothAddress == dev.BluetoothAddress))
                    {
                        BleDevices.Add(dev);
                    }
                }
                StatusMessage = $"Scan completed. {BleDevices.Count} BLE devices identified.";
            }
            finally
            {
                IsScanning = false;
            }
        }

        private async void ReadCharacteristic(GattCharacteristicItem characteristic)
        {
            if (characteristic == null) return;

            if (!IsGattSupported)
            {
                StatusMessage = "GATT Read unavailable: Modern Windows.Devices.Bluetooth WinRT APIs are unsupported on this platform.";
                MessageBox.Show("GATT characteristic exploration requires Windows 10 (Build 15063+) or Windows 11 with native WinRT BLE API support.\n\nSection 15 Policy: BlueDoc never simulates or fabricates characteristic payload bytes on unsupported platforms.", "GATT Not Supported", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (SelectedBleDevice == null)
            {
                return;
            }

            StatusMessage = $"Reading characteristic '{characteristic.Name}' ({characteristic.Uuid})...";

            try
            {
                // Attempt real live read from connected peripheral
                string liveVal = await _gattClient.ReadCharacteristicValueAsync(SelectedService?.Uuid, characteristic.Uuid);
                if (!string.IsNullOrEmpty(liveVal) && !liveVal.Contains("failed") && !liveVal.Contains("not found") && !liveVal.Contains("Error"))
                {
                    characteristic.ValueDisplay = liveVal;
                    StatusMessage = $"Live read succeeded: '{characteristic.Name}' = {liveVal}";
                    return;
                }

                if (!SelectedBleDevice.IsConnected)
                {
                    characteristic.ValueDisplay = "Offline / Disconnected (Requires active GATT connection)";
                    StatusMessage = $"Cannot read '{characteristic.Name}': Device '{SelectedBleDevice.DisplayName}' is not connected.";
                }
                else
                {
                    characteristic.ValueDisplay = liveVal;
                    StatusMessage = $"Read result: {liveVal}";
                }
            }
            catch (Exception ex)
            {
                characteristic.ValueDisplay = $"Read Error: {ex.Message}";
                StatusMessage = $"Read failed: {ex.Message}";
            }
        }

        private async void ToggleNotification(GattCharacteristicItem characteristic)
        {
            if (characteristic == null) return;

            if (!IsGattSupported)
            {
                StatusMessage = "GATT CCCD notifications unavailable on this platform.";
                MessageBox.Show("GATT notifications require Windows 10/11 WinRT BLE subsystem.", "Notification Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SelectedBleDevice == null || !SelectedBleDevice.IsConnected)
            {
                StatusMessage = $"CCCD subscription failed: Device '{SelectedBleDevice?.DisplayName ?? "Peripheral"}' is not connected.";
                MessageBox.Show($"Cannot subscribe to '{characteristic.Name}'. Device is disconnected.", "Subscription Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!characteristic.IsSubscribed)
            {
                StatusMessage = $"Configuring CCCD notification descriptor for '{characteristic.Name}'...";
                bool ok = await _gattClient.ToggleNotificationAsync(SelectedService?.Uuid, characteristic.Uuid, displayValue =>
                {
                    System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                    {
                        characteristic.ValueDisplay = displayValue;
                        StatusMessage = $"Notification update received for '{characteristic.Name}': {displayValue}";
                    });
                });

                if (ok)
                {
                    characteristic.IsSubscribed = true;
                    string timestamp = DateTime.Now.ToString("HH:mm:ss");
                    characteristic.ValueDisplay = $"[Subscribed at {timestamp}] Receiving live indications...";
                    StatusMessage = $"Subscribed to GATT notifications for '{characteristic.Name}'.";
                }
                else
                {
                    StatusMessage = $"Failed to subscribe to CCCD notifications for '{characteristic.Name}'.";
                }
            }
            else
            {
                characteristic.IsSubscribed = false;
                string timestamp = DateTime.Now.ToString("HH:mm:ss");
                characteristic.ValueDisplay = $"[Unsubscribed at {timestamp}] Notification stream halted.";
                StatusMessage = $"Unsubscribed from '{characteristic.Name}'.";
            }
        }

        private async void PerformSafeWrite(GattCharacteristicItem characteristic)
        {
            if (characteristic == null) return;

            if (!IsGattSupported)
            {
                StatusMessage = "GATT Write unavailable: WinRT GATT APIs not present on this platform.";
                MessageBox.Show("GATT characteristic writes require Windows 10 (Build 15063+) or Windows 11.\n\nSection 15 Policy: BlueDoc will not fake GATT write responses.", "GATT Write Not Supported", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SelectedBleDevice == null || !SelectedBleDevice.IsConnected)
            {
                StatusMessage = $"GATT Write blocked: Device '{SelectedBleDevice?.DisplayName ?? "Peripheral"}' is not connected.";
                MessageBox.Show($"Cannot write to characteristic '{characteristic.Name}'.\n\nThe device is currently disconnected or unbonded. A valid, secure GATT connection is required before sending write buffers.\n\nSection 15 Policy: BlueDoc strictly forbids simulating fake ACK responses.", "Write Aborted - Device Not Connected", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Safe GATT Operation Warning:\n\nYou are attempting to perform a write operation to characteristic:\n'{characteristic.Name}' ({characteristic.Uuid})\n\nTarget Device: {SelectedBleDevice.DisplayName} ({SelectedBleDevice.BluetoothAddress})\n\nWriting data can alter peripheral configuration.\n\nDo you want to proceed with this operation?",
                "Confirm GATT Characteristic Write",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                StatusMessage = $"Writing payload to '{characteristic.Name}'...";
                byte[] pingPayload = Encoding.UTF8.GetBytes("BlueDoc_Probe");
                bool ok = await _gattClient.WriteCharacteristicValueAsync(SelectedService?.Uuid, characteristic.Uuid, pingPayload);

                string timestamp = DateTime.Now.ToString("HH:mm:ss");
                if (ok)
                {
                    characteristic.ValueDisplay = $"Write Dispatched & Acknowledged at {timestamp} [Status: Success]";
                    StatusMessage = $"Write command acknowledged by {characteristic.Name} at {timestamp}.";
                }
                else
                {
                    characteristic.ValueDisplay = $"Write Failed / Rejected at {timestamp}";
                    StatusMessage = $"Write command rejected by {characteristic.Name} at {timestamp}.";
                }
            }
        }

        private void ExportGattProfile()
        {
            if (SelectedBleDevice == null || GattServices.Count == 0) return;

            var dialog = new SaveFileDialog
            {
                Filter = "GATT Profile Text (*.txt)|*.txt|JSON (*.json)|*.json",
                FileName = $"gatt_profile_{SelectedBleDevice.BluetoothAddress.Replace(":", "")}_{DateTime.Now:yyyyMMdd}.txt",
                Title = "Export Peripheral GATT Profile"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"GATT Profile Export for {SelectedBleDevice.DisplayName} ({SelectedBleDevice.BluetoothAddress})");
                    sb.AppendLine($"Export Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    sb.AppendLine(new string('-', 70));

                    foreach (var svc in GattServices)
                    {
                        sb.AppendLine($"Service: {svc.Name} [{svc.Uuid}]");
                        sb.AppendLine($"Description: {svc.Description}");
                        foreach (var chr in svc.Characteristics)
                        {
                            sb.AppendLine($"  - Characteristic: {chr.Name} [{chr.Uuid}]");
                            sb.AppendLine($"    Properties: {chr.Properties}");
                            sb.AppendLine($"    Value/State: {chr.ValueDisplay}");
                        }
                        sb.AppendLine();
                    }

                    File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);
                    StatusMessage = $"GATT profile exported to {Path.GetFileName(dialog.FileName)}";
                    MessageBox.Show("GATT profile successfully exported.", "BlueDoc", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Export error: {ex.Message}";
                }
            }
        }
    }
}
