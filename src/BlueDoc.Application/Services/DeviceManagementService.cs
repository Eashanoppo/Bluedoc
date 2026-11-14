using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using BlueDoc.Bluetooth.Abstractions;
using BlueDoc.Core.Enums;
using BlueDoc.Core.Models;
using BlueDoc.Infrastructure.Native;
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

        public Task<IReadOnlyList<BluetoothDeviceInfo>> DiscoverNearbyDevicesAsync(
            int timeoutSeconds = 8,
            IProgress<BluetoothDeviceInfo> progress = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return _bluetoothManager.DiscoverDevicesAsync(timeoutSeconds, progress, cancellationToken);
        }

        public async Task<DeviceOperationResult> ConnectDeviceAsync(BluetoothDeviceInfo device)
        {
            if (device == null || string.IsNullOrWhiteSpace(device.BluetoothAddress))
            {
                return DeviceOperationResult.Fail("No valid device address specified.");
            }

            var orchestrator = new BluetoothConnectionOrchestrator();
            var details = await orchestrator.ConnectPeripheralAsync(device);
            return details.Result;
        }

        public Task<DeviceOperationResult> DisconnectDeviceAsync(BluetoothDeviceInfo device)
        {
            return Task.Run(() =>
            {
                if (device == null || string.IsNullOrWhiteSpace(device.BluetoothAddress))
                {
                    return DeviceOperationResult.Fail("No valid device address specified.");
                }

                IntPtr hRadio = GetPrimaryRadioHandle();
                if (hRadio == IntPtr.Zero)
                {
                    return DeviceOperationResult.Fail("No active operational Bluetooth radio controller found in Windows.");
                }

                try
                {
                    var devInfo = CreateDeviceInfoFromAddress(device.RawAddress, device.DeviceName);
                    var guidsToDisable = GetTargetGuidsForDevice(device);
                    bool anySuccess = false;
                    uint lastError = 0;

                    foreach (var guid in guidsToDisable)
                    {
                        var targetGuid = guid;
                        uint res = BluetoothNative.BluetoothSetServiceState(
                            hRadio,
                            ref devInfo,
                            ref targetGuid,
                            BluetoothNative.BLUETOOTH_SERVICE_DISABLE);

                        if (res == 0)
                        {
                            anySuccess = true;
                        }
                        else
                        {
                            lastError = res;
                        }
                    }

                    if (anySuccess)
                    {
                        device.IsConnected = false;
                        return DeviceOperationResult.Ok($"Successfully disconnected '{device.DisplayName}'.");
                    }
                    else
                    {
                        string errorDesc = DecodeWin32Error(lastError);
                        return DeviceOperationResult.Fail($"Disconnection failed: {errorDesc}", errorCode: lastError);
                    }
                }
                catch (Exception ex)
                {
                    return DeviceOperationResult.Fail($"Exception during disconnection: {ex.Message}");
                }
                finally
                {
                    BluetoothNative.CloseHandle(hRadio);
                }
            });
        }

        public async Task<DeviceOperationResult> PairDeviceAsync(
            BluetoothDeviceInfo device, 
            string pin = null,
            Func<string, Task<bool>> confirmPinMatchCallback = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (device == null || string.IsNullOrWhiteSpace(device.BluetoothAddress))
            {
                return DeviceOperationResult.Fail("Invalid device target for pairing.");
            }

            // 1. If peripheral is Bluetooth Low Energy and running on Windows 10/11, use modern WinRT Pairing
            if (device.DeviceType == DeviceType.LowEnergy && OperatingSystemInspector.IsWindows10OrGreater())
            {
                var blePairing = new BlueDoc.Bluetooth.BLE.BlePairingProvider();
                var bleResult = await blePairing.PairBleDeviceAsync(device.RawAddress, pin, confirmPinMatchCallback, cancellationToken);
                if (bleResult.Success)
                {
                    device.IsPaired = true;
                    device.IsRemembered = true;
                    try
                    {
                        var svcs = await GetDeviceInstalledServicesAsync(device);
                        if (svcs != null && svcs.Count > 0)
                        {
                            device.AdvertisedServices = string.Join(", ", svcs);
                        }
                    }
                    catch { }
                    return bleResult;
                }
                // If BLE pairing returned an explicit failure like cancelled or rejected, return that directly
                if (bleResult.Message.Contains("rejected") || bleResult.Message.Contains("cancelled") || bleResult.Message.Contains("timed out"))
                {
                    return bleResult;
                }
                // Otherwise fall through to Classic / Win32 Authenticate Device
            }

            // 2. Classic Bluetooth & Win32 Authentication Path
            return await Task.Run(() =>
            {
                IntPtr hRadio = GetPrimaryRadioHandle();
                if (hRadio == IntPtr.Zero)
                {
                    return DeviceOperationResult.Fail("Bluetooth radio controller is disabled or not present.");
                }

                try
                {
                    var devInfo = CreateDeviceInfoFromAddress(device.RawAddress, device.DeviceName);
                    ulong pinLen = (ulong)(string.IsNullOrEmpty(pin) ? 0 : pin.Length);

                    // Execute native Win32 Authenticate Device
                    uint authResult = BluetoothNative.BluetoothAuthenticateDevice(
                        IntPtr.Zero,
                        hRadio,
                        ref devInfo,
                        pin,
                        pinLen);

                    if (authResult == 0)
                    {
                        device.IsPaired = true;
                        device.IsRemembered = true;
                        return DeviceOperationResult.Ok($"Device '{device.DisplayName}' paired successfully with Windows.");
                    }
                    else if (authResult == 1244) // ERROR_NOT_AUTHENTICATED
                    {
                        return DeviceOperationResult.Fail(
                            $"Pairing failed: Device '{device.DisplayName}' rejected authentication. If required, verify the PIN or put the peripheral into pairing mode.",
                            "ERROR_NOT_AUTHENTICATED (1244)",
                            authResult);
                    }
                    else if (authResult == 1223) // ERROR_CANCELLED
                    {
                        return DeviceOperationResult.Fail(
                            "Pairing operation was cancelled by the user or the peripheral.",
                            "ERROR_CANCELLED (1223)",
                            authResult);
                    }
                    else
                    {
                        string desc = DecodeWin32Error(authResult);
                        return DeviceOperationResult.Fail(
                            $"Windows authentication returned: {desc}",
                            $"Code: 0x{authResult:X8}",
                            authResult);
                    }
                }
                catch (Exception ex)
                {
                    return DeviceOperationResult.Fail($"Pairing exception: {ex.Message}");
                }
                finally
                {
                    BluetoothNative.CloseHandle(hRadio);
                }
            });
        }

        public Task<bool> UnpairDeviceAsync(string bluetoothAddress)
        {
            return _bluetoothManager.RemoveDeviceAsync(bluetoothAddress);
        }

        public Task<List<string>> GetDeviceInstalledServicesAsync(BluetoothDeviceInfo device)
        {
            return Task.Run(() =>
            {
                var services = new List<string>();
                if (device == null) return services;

                IntPtr hRadio = GetPrimaryRadioHandle();
                if (hRadio == IntPtr.Zero) return services;

                try
                {
                    var devInfo = CreateDeviceInfoFromAddress(device.RawAddress, device.DeviceName);
                    uint numServices = 32;
                    Guid[] guids = new Guid[numServices];

                    uint res = BluetoothNative.BluetoothEnumerateInstalledServices(hRadio, ref devInfo, ref numServices, guids);
                    if (res == 0 && numServices > 0)
                    {
                        for (int i = 0; i < numServices; i++)
                        {
                            services.Add(ResolveServiceGuidName(guids[i]));
                        }
                    }
                }
                catch { }
                finally
                {
                    BluetoothNative.CloseHandle(hRadio);
                }

                if (services.Count == 0)
                {
                    // Provide profile based on device class
                    if (device.MajorDeviceClass == "Audio/Video")
                    {
                        services.Add("Advanced Audio Distribution (A2DP)");
                        services.Add("Audio/Video Remote Control (AVRCP)");
                        services.Add("Handsfree & Headset (HFP/HSP)");
                    }
                    else if (device.MajorDeviceClass == "Peripheral")
                    {
                        services.Add("Human Interface Device (HID)");
                    }
                    else if (device.DeviceType == DeviceType.LowEnergy)
                    {
                        services.Add("Generic Attribute Profile (GATT)");
                        services.Add("Device Information Service");
                    }
                    else
                    {
                        services.Add("Standard Bluetooth Protocol Node");
                    }
                }

                return services;
            });
        }

        private static string ResolveServiceGuidName(Guid guid)
        {
            if (guid == BluetoothNative.AudioSinkServiceGuid) return "Audio Sink (A2DP Stereo Audio)";
            if (guid == BluetoothNative.HandsfreeServiceGuid) return "Handsfree Telephony (HFP)";
            if (guid == BluetoothNative.HeadsetServiceGuid) return "Headset Voice Audio (HSP)";
            if (guid == BluetoothNative.AvrcpServiceGuid) return "Audio/Video Remote Control (AVRCP)";
            if (guid == BluetoothNative.HumanInterfaceDeviceServiceGuid) return "Human Interface Device (HID)";
            if (guid == BluetoothNative.SerialPortServiceGuid) return "Serial Port Profile (SPP)";
            return $"Service {guid:D}";
        }

        private static List<Guid> GetTargetGuidsForDevice(BluetoothDeviceInfo device)
        {
            var list = new List<Guid>();
            if (device.MajorDeviceClass == "Audio/Video" ||
                device.CategoryName.IndexOf("Headset", StringComparison.OrdinalIgnoreCase) >= 0 ||
                device.CategoryName.IndexOf("Audio", StringComparison.OrdinalIgnoreCase) >= 0 ||
                device.DisplayName.IndexOf("Buds", StringComparison.OrdinalIgnoreCase) >= 0 ||
                device.DisplayName.IndexOf("Speaker", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                list.Add(BluetoothNative.AudioSinkServiceGuid);
                list.Add(BluetoothNative.HandsfreeServiceGuid);
                list.Add(BluetoothNative.HeadsetServiceGuid);
            }
            else if (device.MajorDeviceClass == "Peripheral" ||
                     device.CategoryName.IndexOf("Mouse", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     device.CategoryName.IndexOf("Keyboard", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                list.Add(BluetoothNative.HumanInterfaceDeviceServiceGuid);
            }
            else
            {
                list.Add(BluetoothNative.SerialPortServiceGuid);
                list.Add(BluetoothNative.HumanInterfaceDeviceServiceGuid);
                list.Add(BluetoothNative.AudioSinkServiceGuid);
            }
            return list;
        }

        private static IntPtr GetPrimaryRadioHandle()
        {
            try
            {
                var p = new BluetoothNative.BLUETOOTH_FIND_RADIO_PARAMS
                {
                    dwSize = (uint)Marshal.SizeOf(typeof(BluetoothNative.BLUETOOTH_FIND_RADIO_PARAMS))
                };

                IntPtr hRadio;
                IntPtr hFind = BluetoothNative.BluetoothFindFirstRadio(ref p, out hRadio);
                if (hFind != IntPtr.Zero && hFind != (IntPtr)(-1))
                {
                    BluetoothNative.BluetoothFindRadioClose(hFind);
                    return hRadio;
                }
            }
            catch { }
            return IntPtr.Zero;
        }

        private static BluetoothNative.BLUETOOTH_DEVICE_INFO CreateDeviceInfoFromAddress(ulong address, string name)
        {
            return new BluetoothNative.BLUETOOTH_DEVICE_INFO
            {
                dwSize = (uint)Marshal.SizeOf(typeof(BluetoothNative.BLUETOOTH_DEVICE_INFO)),
                Address = address,
                szName = name ?? string.Empty
            };
        }

        private static string DecodeWin32Error(uint errorCode)
        {
            switch (errorCode)
            {
                case 0: return "Success";
                case 1244: return "Device rejected authentication credentials (PIN mismatch or device not in pairing mode).";
                case 1223: return "Operation cancelled by user or device.";
                case 1168: return "Element not found (device may be powered down or out of range).";
                case 1205: return "Service does not exist or device profile is not currently supported.";
                case 5: return "Access Denied. Windows requires elevated administrator privileges.";
                default: return $"System error code 0x{errorCode:X8}";
            }
        }

        public bool LaunchWindowsPairingWizard()
        {
            try
            {
                if (_osVersion.Platform == WindowsPlatformType.Windows10 || _osVersion.Platform == WindowsPlatformType.Windows11)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "ms-settings:bluetooth",
                        UseShellExecute = true
                    });
                    return true;
                }
                else
                {
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
