using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BlueDoc.Bluetooth.Abstractions;
using BlueDoc.Core.Models;

namespace BlueDoc.Application.Services
{
    public class DeviceDiscoveryService
    {
        private readonly BluetoothManager _bluetoothManager;
        private CancellationTokenSource _scanCts;
        private bool _isScanning;

        public bool IsScanning => _isScanning;

        public event EventHandler<BluetoothDeviceInfo> DeviceDiscovered;
        public event EventHandler<bool> ScanningStateChanged;

        public DeviceDiscoveryService(BluetoothManager bluetoothManager = null)
        {
            _bluetoothManager = bluetoothManager ?? new BluetoothManager();
        }

        public async Task<IReadOnlyList<BluetoothDeviceInfo>> StartScanningAsync(
            int timeoutSeconds = 10, 
            CancellationToken externalToken = default(CancellationToken))
        {
            if (_isScanning)
            {
                return new List<BluetoothDeviceInfo>();
            }

            _scanCts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
            _isScanning = true;
            ScanningStateChanged?.Invoke(this, true);

            var progress = new Progress<BluetoothDeviceInfo>(device =>
            {
                DeviceDiscovered?.Invoke(this, device);
            });

            try
            {
                return await _bluetoothManager.DiscoverDevicesAsync(timeoutSeconds, progress, _scanCts.Token);
            }
            finally
            {
                _isScanning = false;
                ScanningStateChanged?.Invoke(this, false);
                _scanCts?.Dispose();
                _scanCts = null;
            }
        }

        public void StopScanning()
        {
            if (_isScanning && _scanCts != null && !_scanCts.IsCancellationRequested)
            {
                _scanCts.Cancel();
            }
        }
    }
}

