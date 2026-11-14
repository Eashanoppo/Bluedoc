using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BlueDoc.Bluetooth.Abstractions;
using BlueDoc.Bluetooth.BLE;
using BlueDoc.Core.Enums;
using BlueDoc.Core.Models;
using BlueDoc.Infrastructure.Platform;

namespace BlueDoc.Application.Services
{
    public class DeviceDiscoveryService : IDisposable
    {
        private readonly BluetoothManager _bluetoothManager;
        private readonly BleAdvertisementWatcherProvider _bleWatcher;
        private readonly Dictionary<string, BluetoothDeviceInfo> _discoveredMaster = new Dictionary<string, BluetoothDeviceInfo>(StringComparer.OrdinalIgnoreCase);
        private readonly object _lock = new object();

        private CancellationTokenSource _scanCts;
        private bool _isScanning;
        private DiscoveryMode _activeMode = DiscoveryMode.DualMode;

        public bool IsScanning => _isScanning;
        public DiscoveryMode ActiveMode => _activeMode;
        public bool IsBleSupported => _bleWatcher != null && _bleWatcher.IsSupported;

        public event EventHandler<BluetoothDeviceInfo> DeviceDiscovered;
        public event EventHandler<BluetoothDeviceInfo> DeviceUpdated;
        public event EventHandler<bool> ScanningStateChanged;
        public event EventHandler<string> StatusChanged;

        public DeviceDiscoveryService(BluetoothManager bluetoothManager = null)
        {
            _bluetoothManager = bluetoothManager ?? new BluetoothManager();

            if (OperatingSystemInspector.IsWindows10OrGreater())
            {
                try
                {
                    _bleWatcher = new BleAdvertisementWatcherProvider();
                    _bleWatcher.DeviceDiscovered += OnBleDeviceDiscovered;
                    _bleWatcher.DeviceUpdated += OnBleDeviceUpdated;
                    _bleWatcher.ScanStatusChanged += (s, msg) => StatusChanged?.Invoke(this, msg);
                }
                catch
                {
                    _bleWatcher = null;
                }
            }
        }

        public async Task<IReadOnlyList<BluetoothDeviceInfo>> StartScanningAsync(
            int timeoutSeconds = 15,
            DiscoveryMode mode = DiscoveryMode.DualMode,
            CancellationToken externalToken = default(CancellationToken))
        {
            if (_isScanning)
            {
                lock (_lock)
                {
                    return _discoveredMaster.Values.ToList();
                }
            }

            _activeMode = mode;
            // On Windows 7, force ClassicOnly
            if (!IsBleSupported && _activeMode != DiscoveryMode.ClassicOnly)
            {
                _activeMode = DiscoveryMode.ClassicOnly;
                StatusChanged?.Invoke(this, "BLE Advertisement Watching unavailable on Windows 7. Running Classic Inquiry only.");
            }

            _scanCts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
            _isScanning = true;
            ScanningStateChanged?.Invoke(this, true);

            lock (_lock)
            {
                _discoveredMaster.Clear();
            }

            // Immediately load already paired/remembered peripherals into master catalog so user sees existing context
            try
            {
                var paired = await _bluetoothManager.GetPairedDevicesAsync(_scanCts.Token);
                foreach (var p in paired)
                {
                    lock (_lock)
                    {
                        _discoveredMaster[p.BluetoothAddress] = p;
                    }
                    DeviceDiscovered?.Invoke(this, p);
                }
            }
            catch { }

            var tasks = new List<Task>();

            // 1. Classic Bluetooth Inquiry Branch
            if (_activeMode == DiscoveryMode.DualMode || _activeMode == DiscoveryMode.ClassicOnly)
            {
                tasks.Add(Task.Run(async () =>
                {
                    StatusChanged?.Invoke(this, "Initiating Classic Bluetooth radio inquiry...");
                    var progress = new Progress<BluetoothDeviceInfo>(classicDev =>
                    {
                        MergeClassicDevice(classicDev);
                    });

                    try
                    {
                        await _bluetoothManager.DiscoverDevicesAsync(timeoutSeconds, progress, _scanCts.Token);
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex)
                    {
                        StatusChanged?.Invoke(this, $"Classic Inquiry: {ex.Message}");
                    }
                }));
            }

            // 2. Bluetooth Low Energy Advertisement Watcher Branch
            if ((_activeMode == DiscoveryMode.DualMode || _activeMode == DiscoveryMode.BleOnly) && IsBleSupported)
            {
                tasks.Add(Task.Run(async () =>
                {
                    StatusChanged?.Invoke(this, "Starting BLE active advertisement watcher...");
                    _bleWatcher.StartScanning();

                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(timeoutSeconds), _scanCts.Token);
                    }
                    catch (OperationCanceledException) { }
                    finally
                    {
                        _bleWatcher.StopScanning();
                    }
                }));
            }

            try
            {
                await Task.WhenAll(tasks);
                StatusChanged?.Invoke(this, $"Discovery finished. {_discoveredMaster.Count} device(s) identified.");
            }
            finally
            {
                _isScanning = false;
                ScanningStateChanged?.Invoke(this, false);
                _bleWatcher?.StopScanning();
                _scanCts?.Dispose();
                _scanCts = null;
            }

            lock (_lock)
            {
                return _discoveredMaster.Values.ToList();
            }
        }

        public void StopScanning()
        {
            if (_isScanning)
            {
                _bleWatcher?.StopScanning();
                if (_scanCts != null && !_scanCts.IsCancellationRequested)
                {
                    _scanCts.Cancel();
                }
                StatusChanged?.Invoke(this, "Discovery scan stopped by user.");
            }
        }

        private void MergeClassicDevice(BluetoothDeviceInfo classicDev)
        {
            bool isNew = false;
            BluetoothDeviceInfo target;

            lock (_lock)
            {
                if (_discoveredMaster.TryGetValue(classicDev.BluetoothAddress, out target))
                {
                    target.IsConnected = classicDev.IsConnected;
                    target.IsPaired = target.IsPaired || classicDev.IsPaired;
                    target.IsRemembered = target.IsRemembered || classicDev.IsRemembered;
                    if (!string.IsNullOrWhiteSpace(classicDev.DeviceName)) target.DeviceName = classicDev.DeviceName;
                    if (classicDev.DeviceClass != 0)
                    {
                        target.DeviceClass = classicDev.DeviceClass;
                        target.CategoryName = classicDev.CategoryName;
                        target.MajorDeviceClass = classicDev.MajorDeviceClass;
                        target.CategoryIcon = classicDev.CategoryIcon;
                    }
                    if (target.DeviceType == DeviceType.LowEnergy)
                    {
                        target.DeviceType = DeviceType.DualMode;
                    }
                    target.LastSeen = DateTime.UtcNow;
                }
                else
                {
                    isNew = true;
                    classicDev.DiscoverySource = "Classic Inquiry";
                    _discoveredMaster[classicDev.BluetoothAddress] = classicDev;
                    target = classicDev;
                }
            }

            if (isNew)
            {
                DeviceDiscovered?.Invoke(this, target);
            }
            else
            {
                DeviceUpdated?.Invoke(this, target);
            }
        }

        private void OnBleDeviceDiscovered(object sender, BluetoothDeviceInfo bleDev)
        {
            bool isNew = false;
            BluetoothDeviceInfo target;

            lock (_lock)
            {
                if (_discoveredMaster.TryGetValue(bleDev.BluetoothAddress, out target))
                {
                    // Merge BLE RSSI and advertisement into existing Classic entry
                    target.Rssi = bleDev.Rssi;
                    if (!string.IsNullOrWhiteSpace(bleDev.AdvertisedServices)) target.AdvertisedServices = bleDev.AdvertisedServices;
                    if (!string.IsNullOrWhiteSpace(bleDev.DeviceName) && (string.IsNullOrWhiteSpace(target.DeviceName) || target.DeviceName == target.BluetoothAddress))
                    {
                        target.DeviceName = bleDev.DeviceName;
                    }
                    if (target.DeviceType == DeviceType.Classic)
                    {
                        target.DeviceType = DeviceType.DualMode;
                    }
                    target.LastSeen = DateTime.UtcNow;
                }
                else
                {
                    isNew = true;
                    _discoveredMaster[bleDev.BluetoothAddress] = bleDev;
                    target = bleDev;
                }
            }

            if (isNew)
            {
                DeviceDiscovered?.Invoke(this, target);
            }
            else
            {
                DeviceUpdated?.Invoke(this, target);
            }
        }

        private void OnBleDeviceUpdated(object sender, BluetoothDeviceInfo bleDev)
        {
            BluetoothDeviceInfo target;
            lock (_lock)
            {
                if (_discoveredMaster.TryGetValue(bleDev.BluetoothAddress, out target))
                {
                    target.Rssi = bleDev.Rssi;
                    target.LastSeen = DateTime.UtcNow;
                    if (!string.IsNullOrWhiteSpace(bleDev.DeviceName) && (string.IsNullOrWhiteSpace(target.DeviceName) || target.DeviceName == target.BluetoothAddress))
                    {
                        target.DeviceName = bleDev.DeviceName;
                    }
                }
                else
                {
                    target = bleDev;
                    _discoveredMaster[bleDev.BluetoothAddress] = bleDev;
                }
            }
            DeviceUpdated?.Invoke(this, target);
        }

        public IReadOnlyList<BluetoothDeviceInfo> GetCurrentlyDiscoveredDevices()
        {
            lock (_lock)
            {
                return _discoveredMaster.Values.ToList();
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _discoveredMaster.Clear();
            }
        }

        public void Dispose()
        {
            StopScanning();
            _bleWatcher?.Dispose();
        }
    }
}
