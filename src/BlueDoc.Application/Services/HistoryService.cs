using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlueDoc.Core.Interfaces;
using BlueDoc.Core.Models;

namespace BlueDoc.Application.Services
{
    public class HistoryService
    {
        private readonly IConnectionHistoryRepository _historyRepo;
        private readonly IDeviceCacheRepository _deviceRepo;

        public HistoryService(IConnectionHistoryRepository historyRepo = null, IDeviceCacheRepository deviceRepo = null)
        {
            _historyRepo = historyRepo;
            _deviceRepo = deviceRepo;
        }

        public async Task RecordDeviceDiscoveredAsync(BluetoothDeviceInfo device, Guid? sessionId = null)
        {
            if (device == null) return;

            if (_deviceRepo != null)
            {
                await _deviceRepo.UpsertDiscoveredDeviceAsync(device);
            }

            if (_historyRepo != null)
            {
                await _historyRepo.RecordEventAsync(new ConnectionHistoryRecord
                {
                    DeviceAddress = device.BluetoothAddress,
                    DeviceName = device.DeviceName,
                    DeviceType = device.DeviceType.ToString(),
                    EventType = device.IsConnected ? "Connected" : (device.IsPaired ? "Paired" : "Discovered"),
                    TimestampUtc = DateTime.UtcNow,
                    Details = $"Signal: {device.SignalStrengthDisplay}, Class: {device.MajorDeviceClass}",
                    IsSuccess = true,
                    DiagnosticSessionId = sessionId
                });
            }
        }

        public async Task RecordConnectionAttemptAsync(string deviceAddress, string deviceName, bool isSuccess, string details = "")
        {
            if (_historyRepo != null)
            {
                await _historyRepo.RecordEventAsync(new ConnectionHistoryRecord
                {
                    DeviceAddress = deviceAddress,
                    DeviceName = deviceName,
                    EventType = isSuccess ? "Connected" : "ConnectionFailed",
                    TimestampUtc = DateTime.UtcNow,
                    Details = details,
                    IsSuccess = isSuccess
                });
            }
        }

        public async Task<List<ConnectionHistoryRecord>> GetHistoryAsync(int limit = 100, string search = null)
        {
            if (_historyRepo == null) return new List<ConnectionHistoryRecord>();
            return await _historyRepo.GetHistoryAsync(limit, search);
        }

        public async Task<List<BluetoothDeviceInfo>> GetDiscoveredDevicesAsync()
        {
            if (_deviceRepo == null) return new List<BluetoothDeviceInfo>();
            return await _deviceRepo.GetDiscoveredDevicesAsync();
        }

        public async Task ClearAllHistoryAsync()
        {
            if (_historyRepo != null) await _historyRepo.ClearHistoryAsync();
            if (_deviceRepo != null) await _deviceRepo.ClearDevicesAsync();
        }
    }
}
