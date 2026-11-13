using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using BlueDoc.Bluetooth.Abstractions;
using BlueDoc.Core.Enums;
using BlueDoc.Core.Models;
using BlueDoc.Infrastructure.Platform;

namespace BlueDoc.Application.Services
{
    public class DeviceManagementService
    {
        private readonly BluetoothManager _bluetoothManager;
        private readonly OsVersionInfo _osVersion;

        public DeviceManagementService(BluetoothManager bluetoothManager = null)
        {
            _bluetoothManager = bluetoothManager ?? new BluetoothManager();
            _osVersion = OperatingSystemInspector.GetOperatingSystemInfo();
        }

        public Task<IReadOnlyList<BluetoothDeviceInfo>> GetPairedDevicesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return _bluetoothManager.GetPairedDevicesAsync(cancellationToken);
        }

        public Task<bool> UnpairDeviceAsync(string bluetoothAddress)
        {
            return _bluetoothManager.RemoveDeviceAsync(bluetoothAddress);
        }

        public bool LaunchWindowsPairingWizard()
        {
            try
            {
                if (_osVersion.Platform == WindowsPlatformType.Windows10 || _osVersion.Platform == WindowsPlatformType.Windows11)
                {
                    // Open Windows 10/11 Bluetooth settings page directly
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "ms-settings:bluetooth",
                        UseShellExecute = true
                    });
                    return true;
                }
                else
                {
                    // Open Windows 7 Classic Devices and Printers / Add a Device
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "control.exe",
                        Arguments = "/name Microsoft.DevicesAndPrinters",
                        UseShellExecute = true
                    });
                    return true;
                }
            }
            catch
            {
                // Fallback to classic device pairing wizard if ms-settings fails
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "DevicePairingWizard.exe",
                        UseShellExecute = true
                    });
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool LaunchDeviceManager()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "devmgmt.msc",
                    UseShellExecute = true
                });
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}

