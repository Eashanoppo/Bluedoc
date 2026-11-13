using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BlueDoc.Core.Models;

namespace BlueDoc.Core.Interfaces
{
    public interface IBluetoothProvider
    {
        string ProviderName { get; }
        bool IsSupportedOnCurrentPlatform { get; }

        Task<IReadOnlyList<BluetoothAdapterInfo>> GetAdaptersAsync(CancellationToken cancellationToken = default(CancellationToken));
        Task<IReadOnlyList<BluetoothDeviceInfo>> DiscoverDevicesAsync(int timeoutSeconds, IProgress<BluetoothDeviceInfo> progress = null, CancellationToken cancellationToken = default(CancellationToken));
        Task<IReadOnlyList<BluetoothDeviceInfo>> GetPairedDevicesAsync(CancellationToken cancellationToken = default(CancellationToken));
        Task<bool> IsRadioEnabledAsync(CancellationToken cancellationToken = default(CancellationToken));
        Task<bool> RemoveDeviceAsync(string bluetoothAddress);
    }
}
