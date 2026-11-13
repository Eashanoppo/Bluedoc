using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BlueDoc.Bluetooth.Legacy;
using BlueDoc.Core.Interfaces;
using BlueDoc.Core.Models;
using BlueDoc.Infrastructure.Hardware;
using BlueDoc.Infrastructure.Platform;

namespace BlueDoc.Bluetooth.Abstractions
{
    public class BluetoothManager
    {
        private readonly IBluetoothProvider _activeProvider;
        private readonly IHardwareInspector _hardwareInspector;
        private readonly OsVersionInfo _osVersion;

        public IBluetoothProvider ActiveProvider => _activeProvider;
        public IHardwareInspector HardwareInspector => _hardwareInspector;
        public OsVersionInfo CurrentOs => _osVersion;

        public BluetoothManager(IHardwareInspector hardwareInspector = null)
        {
            _hardwareInspector = hardwareInspector ?? new SetupApiHardwareInspector();
            _osVersion = OperatingSystemInspector.GetOperatingSystemInfo();

            // Default to Win32 Classic & PnP Provider which has 100% compatibility across Win7, Win10, and Win11
            _activeProvider = new Win32BluetoothRadioProvider(_hardwareInspector);
        }

        public Task<IReadOnlyList<BluetoothAdapterInfo>> GetAdaptersAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return _activeProvider.GetAdaptersAsync(cancellationToken);
        }

        public Task<IReadOnlyList<BluetoothDeviceInfo>> GetPairedDevicesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return _activeProvider.GetPairedDevicesAsync(cancellationToken);
        }

        public Task<IReadOnlyList<BluetoothDeviceInfo>> DiscoverDevicesAsync(
            int timeoutSeconds = 8, 
            IProgress<BluetoothDeviceInfo> progress = null, 
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return _activeProvider.DiscoverDevicesAsync(timeoutSeconds, progress, cancellationToken);
        }

        public Task<bool> IsRadioEnabledAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return _activeProvider.IsRadioEnabledAsync(cancellationToken);
        }

        public Task<bool> RemoveDeviceAsync(string bluetoothAddress)
        {
            return _activeProvider.RemoveDeviceAsync(bluetoothAddress);
        }
    }
}
