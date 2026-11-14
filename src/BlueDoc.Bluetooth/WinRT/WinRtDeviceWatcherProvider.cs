using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using BlueDoc.Core.Enums;
using BlueDoc.Core.Models;
using BlueDoc.Infrastructure.Hardware;
using BlueDoc.Infrastructure.Platform;

namespace BlueDoc.Bluetooth.WinRT
{
    /// <summary>
    /// Native Windows 10 & 11 DeviceWatcher provider for Classic Bluetooth & BR/EDR audio peripherals (Speakers, Headsets, Mics, Mice).
    /// Uses AssociationEndpoint inquiry to discover nearby discoverable and active devices.
    /// </summary>
    public class WinRtDeviceWatcherProvider : IDisposable
    {
        private DeviceWatcher _watcher;
        private readonly Dictionary<string, BluetoothDeviceInfo> _discoveredClassic = new Dictionary<string, BluetoothDeviceInfo>(StringComparer.OrdinalIgnoreCase);
        private readonly object _lock = new object();
        private bool _isScanning;

        public event EventHandler<BluetoothDeviceInfo> DeviceDiscovered;
        public event EventHandler<BluetoothDeviceInfo> DeviceUpdated;
        public event EventHandler<string> ScanStatusChanged;

        public bool IsSupported => OperatingSystemInspector.IsWindows10OrGreater();
        public bool IsScanning => _isScanning;

        public WinRtDeviceWatcherProvider()
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
                // Unpaired selector triggers physical baseband RF inquiry on local Bluetooth adapter
                string aqs = BluetoothDevice.GetDeviceSelectorFromPairingState(false);

                string[] requestedProperties = new string[]
                {
                    "System.Devices.Aep.DeviceAddress",
                    "System.Devices.Aep.IsConnected",
                    "System.Devices.Aep.IsPaired",
                    "System.Devices.Aep.IsPresent",
                    "System.Devices.Aep.SignalStrength",
                    "System.Devices.Aep.Bluetooth.Cod.MajorDeviceClass",
                    "System.ItemNameDisplay"
                };

                _watcher = DeviceInformation.CreateWatcher(
                    aqs,
                    requestedProperties,
                    DeviceInformationKind.AssociationEndpoint
                );

                _watcher.Added += OnDeviceAdded;
                _watcher.Updated += OnDeviceUpdated;
                _watcher.EnumerationCompleted += OnEnumerationCompleted;
                _watcher.Stopped += OnWatcherStopped;
            }
            catch (Exception ex)
            {
                ScanStatusChanged?.Invoke(this, $"Failed to initialize Classic DeviceWatcher: {ex.Message}");
            }
        }

        public void StartScanning()
        {
            if (!IsSupported) return;

            if (_watcher == null)
            {
                InitializeWatcher();
            }

            if (_watcher == null) return;

            lock (_lock)
            {
                _discoveredClassic.Clear();
            }

            try
            {
                if (_watcher.Status == DeviceWatcherStatus.Created ||
                    _watcher.Status == DeviceWatcherStatus.Stopped ||
                    _watcher.Status == DeviceWatcherStatus.Aborted)
                {
                    _watcher.Start();
                    _isScanning = true;
                    ScanStatusChanged?.Invoke(this, "Classic Bluetooth RF inquiry active (Searching nearby speakers, headsets & devices)...");
                }
            }
            catch (Exception ex)
            {
                _isScanning = false;
                ScanStatusChanged?.Invoke(this, $"Classic Watcher start error: {ex.Message}");
            }
        }

        public void StopScanning()
        {
            if (_watcher != null && _isScanning)
            {
                try
                {
                    if (_watcher.Status == DeviceWatcherStatus.Started ||
                        _watcher.Status == DeviceWatcherStatus.EnumerationCompleted)
                    {
                        _watcher.Stop();
                    }
                }
                catch { }
                finally
                {
                    _isScanning = false;
                }
            }
        }

        private void OnDeviceAdded(DeviceWatcher sender, DeviceInformation devInfo)
        {
            try
            {
                if (devInfo == null || string.IsNullOrWhiteSpace(devInfo.Id)) return;

                string address = ExtractBluetoothAddress(devInfo);
                if (string.IsNullOrWhiteSpace(address)) return;

                string name = !string.IsNullOrWhiteSpace(devInfo.Name) ? devInfo.Name : "Bluetooth Device";

                bool isConnected = false;
                if (devInfo.Properties.TryGetValue("System.Devices.Aep.IsConnected", out var connObj) && connObj is bool bConn)
                {
                    isConnected = bConn;
                }

                bool isPaired = false;
                if (devInfo.Properties.TryGetValue("System.Devices.Aep.IsPaired", out var pairObj) && pairObj is bool bPair)
                {
                    isPaired = bPair;
                }

                int? rssi = null;
                if (devInfo.Properties.TryGetValue("System.Devices.Aep.SignalStrength", out var rssiObj) && rssiObj != null)
                {
                    if (int.TryParse(rssiObj.ToString(), out int r)) rssi = r;
                }

                string catName = "Audio / Headphones";
                string major = "Audio/Video";
                string icon = "🎧";
                DeviceClassDecoder.InferFromNameOrInstance(name, string.Empty, ref catName, ref major, ref icon);

                ulong rawAddress = 0;
                string cleanHex = address.Replace(":", "").Replace("-", "").Trim();
                if (cleanHex.Length == 12)
                {
                    ulong.TryParse(cleanHex, System.Globalization.NumberStyles.HexNumber, null, out rawAddress);
                }

                BluetoothDeviceInfo dev;
                bool isNew = false;

                lock (_lock)
                {
                    if (_discoveredClassic.TryGetValue(address, out dev))
                    {
                        dev.DeviceName = name;
                        dev.IsConnected = isConnected;
                        dev.IsPaired = dev.IsPaired || isPaired;
                        dev.IsAvailable = true;
                        if (rssi.HasValue) dev.Rssi = rssi.Value;
                        dev.LastSeen = DateTime.UtcNow;
                    }
                    else
                    {
                        isNew = true;
                        dev = new BluetoothDeviceInfo
                        {
                            DeviceName = name,
                            BluetoothAddress = address,
                            RawAddress = rawAddress,
                            IsConnected = isConnected,
                            IsPaired = isPaired,
                            IsAvailable = true,
                            DeviceType = DeviceType.Classic,
                            CategoryName = catName,
                            MajorDeviceClass = major,
                            CategoryIcon = icon,
                            Rssi = rssi ?? -60,
                            LastSeen = DateTime.UtcNow
                        };
                        _discoveredClassic[address] = dev;
                    }
                }

                if (isNew)
                {
                    DeviceDiscovered?.Invoke(this, dev);
                }
                else
                {
                    DeviceUpdated?.Invoke(this, dev);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WinRtDeviceWatcherProvider] OnDeviceAdded error: {ex.Message}");
            }
        }

        private void OnDeviceUpdated(DeviceWatcher sender, DeviceInformationUpdate update)
        {
            try
            {
                if (update == null || string.IsNullOrWhiteSpace(update.Id)) return;

                var match = Regex.Match(update.Id, @"([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})");
                string address = match.Success ? match.Value.ToUpperInvariant().Replace('-', ':') : null;

                if (string.IsNullOrWhiteSpace(address)) return;

                lock (_lock)
                {
                    if (_discoveredClassic.TryGetValue(address, out var dev))
                    {
                        if (update.Properties.TryGetValue("System.Devices.Aep.IsConnected", out var connObj) && connObj is bool bConn)
                        {
                            dev.IsConnected = bConn;
                        }

                        if (update.Properties.TryGetValue("System.Devices.Aep.SignalStrength", out var rssiObj) && rssiObj != null)
                        {
                            if (int.TryParse(rssiObj.ToString(), out int r)) dev.Rssi = r;
                        }

                        dev.LastSeen = DateTime.UtcNow;
                        DeviceUpdated?.Invoke(this, dev);
                    }
                }
            }
            catch { }
        }

        private void OnEnumerationCompleted(DeviceWatcher sender, object args)
        {
            ScanStatusChanged?.Invoke(this, "Classic inquiry completed. Listening for incoming device responses...");
        }

        private void OnWatcherStopped(DeviceWatcher sender, object args)
        {
            _isScanning = false;
        }

        private static string ExtractBluetoothAddress(DeviceInformation devInfo)
        {
            // 1. Try property
            if (devInfo.Properties.TryGetValue("System.Devices.Aep.DeviceAddress", out var addrObj) && addrObj != null)
            {
                string raw = addrObj.ToString().Trim().ToUpperInvariant();
                string clean = raw.Replace(":", "").Replace("-", "");
                if (clean.Length == 12 && Regex.IsMatch(clean, "^[0-9A-F]{12}$"))
                {
                    return string.Join(":", Enumerable.Range(0, 6).Select(i => clean.Substring(i * 2, 2)));
                }
            }

            // 2. Try ID string: e.g., "Bluetooth#Bluetooth00:1a:7d:da:71:11-41:42:29:86:6b:a5"
            if (!string.IsNullOrWhiteSpace(devInfo.Id))
            {
                var match = Regex.Match(devInfo.Id, @"([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})");
                if (match.Success)
                {
                    return match.Value.ToUpperInvariant().Replace('-', ':');
                }

                var hexMatch = Regex.Match(devInfo.Id, @"([0-9A-Fa-f]{12})");
                if (hexMatch.Success)
                {
                    string clean = hexMatch.Value.ToUpperInvariant();
                    return string.Join(":", Enumerable.Range(0, 6).Select(i => clean.Substring(i * 2, 2)));
                }
            }

            return null;
        }

        public IReadOnlyList<BluetoothDeviceInfo> GetDiscoveredDevices()
        {
            lock (_lock)
            {
                return _discoveredClassic.Values.ToList();
            }
        }

        public void Dispose()
        {
            StopScanning();
            if (_watcher != null)
            {
                try
                {
                    _watcher.Added -= OnDeviceAdded;
                    _watcher.Updated -= OnDeviceUpdated;
                    _watcher.EnumerationCompleted -= OnEnumerationCompleted;
                    _watcher.Stopped -= OnWatcherStopped;
                }
                catch { }
                _watcher = null;
            }
        }
    }
}
