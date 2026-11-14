using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Rfcomm;

namespace BlueDoc.Bluetooth
{
    public static class WinRtClassicConnector
    {
        public static async Task<bool> TryWakeAndConnectDeviceAsync(ulong bluetoothAddress)
        {
            if (bluetoothAddress == 0) return false;

            try
            {
                var device = await BluetoothDevice.FromBluetoothAddressAsync(bluetoothAddress);
                if (device != null)
                {
                    // Accessing RFCOMM service catalog initiates physical RF paging to the target device
                    var rfcommResult = await device.GetRfcommServicesAsync(BluetoothCacheMode.Uncached);
                    if (rfcommResult != null && rfcommResult.Services != null && rfcommResult.Services.Count > 0)
                    {
                        return true;
                    }

                    return device.ConnectionStatus == BluetoothConnectionStatus.Connected;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WinRtClassicConnector] Exception: {ex.Message}");
            }

            return false;
        }
    }
}

