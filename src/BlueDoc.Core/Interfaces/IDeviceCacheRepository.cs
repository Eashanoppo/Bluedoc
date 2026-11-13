using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlueDoc.Core.Models;

namespace BlueDoc.Core.Interfaces
{
    public interface IDeviceCacheRepository
    {
        Task UpsertDiscoveredDeviceAsync(BluetoothDeviceInfo device);
        Task<List<BluetoothDeviceInfo>> GetDiscoveredDevicesAsync();
        Task ClearDevicesAsync();
    }
}
