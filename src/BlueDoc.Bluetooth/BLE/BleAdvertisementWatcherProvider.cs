using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using BlueDoc.Core.Enums;
using BlueDoc.Core.Models;
using BlueDoc.Infrastructure.Hardware;
using BlueDoc.Infrastructure.Platform;

namespace BlueDoc.Bluetooth.BLE
{
    public class BleAdvertisementWatcherProvider : IDisposable
    {
        private BluetoothLEAdvertisementWatcher _watcher;
        private readonly Dictionary<string, BluetoothDeviceInfo> _discoveredBle = new Dictionary<string, BluetoothDeviceInfo>(StringComparer.OrdinalIgnoreCase);
        private readonly object _lock = new object();
        private bool _isScanning;

        public event EventHandler<BluetoothDeviceInfo> DeviceDiscovered;
        public event EventHandler<BluetoothDeviceInfo> DeviceUpdated;
        public event EventHandler<string> ScanStatusChanged;

        public bool IsSupported => OperatingSystemInspector.IsWindows10OrGreater();
        public bool IsScanning => _isScanning;

        public BleAdvertisementWatcherProvider()
        {
            if (IsSupported)
            {
                InitializeWatcher();
            }
        }

        private void InitializeWatcher()
        {
            try
            {
                _watcher = new BluetoothLEAdvertisementWatcher
                {
                    ScanningMode = BluetoothLEScanningMode.Active
                };

                _watcher.Received += OnAdvertisementReceived;
                _watcher.Stopped += OnWatcherStopped;
            }
            catch (Exception ex)
            {
                ScanStatusChanged?.Invoke(this, $"Failed to initialize BLE Watcher: {ex.Message}");
            }
        }

        public void StartScanning()
        {
            if (!IsSupported)
            {
                ScanStatusChanged?.Invoke(this, "Bluetooth Low Energy Advertisement Watcher requires Windows 10 or Windows 11.");
                return;
            }

            if (_watcher == null)
            {
                InitializeWatcher();
            }

            lock (_lock)
            {
                _discoveredBle.Clear();
            }

            try
            {
                _watcher.Start();
                _isScanning = true;
                ScanStatusChanged?.Invoke(this, "BLE Advertisement Watcher active (Active Scanning Mode).");
            }
            catch (Exception ex)
            {
                _isScanning = false;
                ScanStatusChanged?.Invoke(this, $"BLE Watcher start error: {ex.Message}");
            }
        }

        public void StopScanning()
        {
            if (_watcher != null && _isScanning)
            {
                try
                {
                    _watcher.Stop();
                }
                catch { }
                finally
                {
                    _isScanning = false;
                    ScanStatusChanged?.Invoke(this, "BLE Advertisement Watcher stopped.");
                }
            }
        }

        private void OnAdvertisementReceived(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            try
            {
                string addressHex = string.Format("{0:X12}", args.BluetoothAddress);
                string formattedAddress = string.Join(":", Enumerable.Range(0, 6).Select(i => addressHex.Substring(i * 2, 2)));

                string localName = args.Advertisement?.LocalName;
                short rssi = args.RawSignalStrengthInDBm;

                BluetoothDeviceInfo dev;
                bool isNew = false;

                lock (_lock)
                {
                    if (_discoveredBle.TryGetValue(formattedAddress, out dev))
                    {
                        // Update existing device telemetry
                        dev.Rssi = rssi;
                        dev.LastSeen = DateTime.UtcNow;

                        if (!string.IsNullOrWhiteSpace(localName) && (string.IsNullOrWhiteSpace(dev.DeviceName) || dev.DeviceName == formattedAddress))
                        {
                            dev.DeviceName = localName;
                        }
                    }
                    else
                    {
                        isNew = true;
                        dev = new BluetoothDeviceInfo
                        {
                            BluetoothAddress = formattedAddress,
                            RawAddress = args.BluetoothAddress,
                            DeviceName = string.IsNullOrWhiteSpace(localName) ? formattedAddress : localName,
                            DeviceType = DeviceType.LowEnergy,
                            Rssi = rssi,
                            FirstDiscovered = DateTime.UtcNow,
                            LastSeen = DateTime.UtcNow,
                            CategoryIcon = "📶",
                            CategoryName = "BLE Peripheral",
                            MajorDeviceClass = "Peripheral",
                            DiscoverySource = "BLE Advertisement"
                        };

                        _discoveredBle[formattedAddress] = dev;
                    }
                }

                // Parse and assign advertised Service UUIDs
                if (args.Advertisement?.ServiceUuids != null && args.Advertisement.ServiceUuids.Count > 0)
                {
                    var services = new List<string>();
                    foreach (var uuid in args.Advertisement.ServiceUuids)
                    {
                        string uuidStr = uuid.ToString().ToUpperInvariant();
                        if (uuidStr.StartsWith("00001812"))
                        {
                            services.Add("HID (0x1812)");
                            dev.CategoryName = "BLE Mouse / Keyboard (HID)";
                            dev.CategoryIcon = "🖱️";
                            dev.MajorDeviceClass = "Peripheral";
                        }
                        else if (uuidStr.StartsWith("0000180D"))
                        {
                            services.Add("Heart Rate (0x180D)");
                            dev.CategoryName = "Heart Rate Sensor";
                            dev.CategoryIcon = "❤️";
                            dev.MajorDeviceClass = "Health";
                        }
                        else if (uuidStr.StartsWith("0000181A"))
                        {
                            services.Add("Environmental (0x181A)");
                            dev.CategoryName = "Environmental Sensor";
                            dev.CategoryIcon = "🌡️";
                            dev.MajorDeviceClass = "Health";
                        }
                        else if (uuidStr.StartsWith("0000180F"))
                        {
                            services.Add("Battery (0x180F)");
                        }
                        else if (uuidStr.StartsWith("0000180A"))
                        {
                            services.Add("Device Info (0x180A)");
                        }
                        else if (uuidStr.StartsWith("0000110B") || uuidStr.StartsWith("0000110A"))
                        {
                            services.Add("Audio (0x110B)");
                            dev.CategoryName = "Audio / Headphones";
                            dev.CategoryIcon = "🎧";
                            dev.MajorDeviceClass = "Audio/Video";
                        }
                        else
                        {
                            services.Add(uuid.ToString("B"));
                        }
                    }
                    dev.AdvertisedServices = string.Join(", ", services);
                }

                // If name gives clues, infer category
                string catName = dev.CategoryName;
                string major = dev.MajorDeviceClass;
                string icon = dev.CategoryIcon;
                DeviceClassDecoder.InferFromNameOrInstance(dev.DeviceName, string.Empty, ref catName, ref major, ref icon);
                dev.CategoryName = catName;
                dev.MajorDeviceClass = major;
                dev.CategoryIcon = icon;

                if (isNew)
                {
                    DeviceDiscovered?.Invoke(this, dev);
                }
                else
                {
                    DeviceUpdated?.Invoke(this, dev);
                }
            }
            catch { }
        }

        private void OnWatcherStopped(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementWatcherStoppedEventArgs args)
        {
            _isScanning = false;
            if (args.Error != BluetoothError.Success)
            {
                ScanStatusChanged?.Invoke(this, $"Classic RF inquiry active (Searching speakers & peripherals). BLE: {args.Error}.");
            }
        }

        public IReadOnlyList<BluetoothDeviceInfo> GetDiscoveredDevices()
        {
            lock (_lock)
            {
                return _discoveredBle.Values.ToList();
            }
        }

        public void Dispose()
        {
            StopScanning();
            if (_watcher != null)
            {
                try
                {
                    _watcher.Received -= OnAdvertisementReceived;
                    _watcher.Stopped -= OnWatcherStopped;
                }
                catch { }
                _watcher = null;
            }
        }
    }
}
