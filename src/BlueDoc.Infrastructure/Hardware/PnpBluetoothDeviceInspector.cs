using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BlueDoc.Core.Enums;
using BlueDoc.Core.Models;
using BlueDoc.Infrastructure.Native;
using BlueDoc.Infrastructure.Platform;

namespace BlueDoc.Infrastructure.Hardware
{
    public static class PnpBluetoothDeviceInspector
    {
        public static IReadOnlyList<BluetoothDeviceInfo> EnumeratePairedDevices()
        {
            var result = new Dictionary<string, BluetoothDeviceInfo>(StringComparer.OrdinalIgnoreCase);

            var classGuid = SetupApiNative.GUID_DEVCLASS_BLUETOOTH;
            IntPtr devInfo = SetupApiNative.SetupDiGetClassDevs(ref classGuid, null, IntPtr.Zero, 0);

            if (devInfo == (IntPtr)(-1) || devInfo == IntPtr.Zero)
            {
                return new List<BluetoothDeviceInfo>();
            }

            try
            {
                var devInfoData = new SetupApiNative.SP_DEVINFO_DATA();
                devInfoData.cbSize = Marshal.SizeOf(devInfoData);

                int memberIndex = 0;
                while (SetupApiNative.SetupDiEnumDeviceInfo(devInfo, memberIndex, ref devInfoData))
                {
                    memberIndex++;

                    var sbInstanceId = new StringBuilder(1024);
                    if (!SetupApiNative.SetupDiGetDeviceInstanceId(devInfo, ref devInfoData, sbInstanceId, sbInstanceId.Capacity, out _))
                    {
                        continue;
                    }

                    string instanceId = sbInstanceId.ToString();

                    // Match BTHENUM\DEV_XXXXXXXXXXXX or BTHLE\DEV_XXXXXXXXXXXX
                    // Filter out secondary service profiles like BTHENUM\{GUID}...
                    var match = Regex.Match(instanceId, @"(BTHENUM|BTHLE)\\DEV_([0-9A-Fa-f]{12})", RegexOptions.IgnoreCase);
                    if (!match.Success)
                    {
                        continue;
                    }

                    string busKind = match.Groups[1].Value.ToUpperInvariant();
                    string rawAddressHex = match.Groups[2].Value.ToUpperInvariant();

                    string formattedAddress = string.Join(":", Enumerable.Range(0, 6).Select(i => rawAddressHex.Substring(i * 2, 2)));

                    string friendlyName = GetDeviceStringProperty(devInfo, ref devInfoData, SetupApiNative.SPDRP_FRIENDLYNAME);
                    string devDesc = GetDeviceStringProperty(devInfo, ref devInfoData, SetupApiNative.SPDRP_DEVICEDESC);
                    string bestName = !string.IsNullOrWhiteSpace(friendlyName) ? friendlyName : devDesc;

                    // Skip generic Microsoft Bluetooth Enumerator nodes
                    if (string.Equals(bestName, "Microsoft Bluetooth Enumerator", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(bestName, "Microsoft Bluetooth LE Enumerator", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    ulong rawAddress = 0;
                    try
                    {
                        rawAddress = Convert.ToUInt64(rawAddressHex, 16);
                    }
                    catch { }

                    // Determine real physical connection state (not just whether the PnP driver is loaded)
                    bool isConnected = false;
                    int? batteryLevel = null;
                    if (rawAddress != 0)
                    {
                        if (busKind == "BTHENUM")
                        {
                            if (OperatingSystemInspector.IsWindows10OrGreater())
                            {
                                isConnected = CheckClassicDeviceConnectedWinRt(rawAddress);
                                if (isConnected)
                                {
                                    batteryLevel = QueryDeviceBatteryLevelWinRt(rawAddress, false);
                                }
                            }
                            else
                            {
                                var bthInfo = new BluetoothNative.BLUETOOTH_DEVICE_INFO
                                {
                                    dwSize = (uint)Marshal.SizeOf(typeof(BluetoothNative.BLUETOOTH_DEVICE_INFO)),
                                    Address = rawAddress
                                };
                                if (BluetoothNative.BluetoothGetDeviceInfo(IntPtr.Zero, ref bthInfo) == 0)
                                {
                                    isConnected = bthInfo.fConnected;
                                }
                            }
                        }
                        else if (busKind == "BTHLE" && OperatingSystemInspector.IsWindows10OrGreater())
                        {
                            isConnected = CheckBleDeviceConnectedWinRt(rawAddress);
                            if (isConnected)
                            {
                                batteryLevel = QueryDeviceBatteryLevelWinRt(rawAddress, true);
                            }
                        }
                    }

                    var device = new BluetoothDeviceInfo
                    {
                        DeviceName = bestName,
                        BluetoothAddress = formattedAddress,
                        RawAddress = rawAddress,
                        InstanceId = instanceId,
                        IsPaired = true,
                        IsRemembered = true,
                        IsConnected = isConnected,
                        BatteryLevel = batteryLevel,
                        DeviceType = busKind == "BTHLE" ? DeviceType.LowEnergy : DeviceType.Classic
                    };

                    string category = "Bluetooth Device";
                    string major = "Uncategorized";
                    string icon = "📶";
                    DeviceClassDecoder.InferFromNameOrInstance(bestName, instanceId, ref category, ref major, ref icon);

                    device.CategoryName = category;
                    device.MajorDeviceClass = major;
                    device.CategoryIcon = icon;

                    // Deduplicate by address
                    if (result.TryGetValue(formattedAddress, out var existing))
                    {
                        // Prefer operational/connected entry or more specific name
                        if (!existing.IsConnected && device.IsConnected)
                        {
                            result[formattedAddress] = device;
                        }
                    }
                    else
                    {
                        result[formattedAddress] = device;
                    }
                }
            }
            finally
            {
                SetupApiNative.SetupDiDestroyDeviceInfoList(devInfo);
            }

            return result.Values.ToList();
        }

        private static string GetDeviceStringProperty(IntPtr devInfo, ref SetupApiNative.SP_DEVINFO_DATA devInfoData, int property)
        {
            byte[] buffer = new byte[1024];
            if (SetupApiNative.SetupDiGetDeviceRegistryProperty(devInfo, ref devInfoData, property, out _, buffer, (uint)buffer.Length, out uint reqSize))
            {
                return Encoding.Unicode.GetString(buffer, 0, (int)reqSize).TrimEnd('\0');
            }
            return string.Empty;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool CheckDeviceConnectedWinRt(ulong rawAddress, bool isBle)
        {
            return isBle ? CheckBleDeviceConnectedWinRt(rawAddress) : CheckClassicDeviceConnectedWinRt(rawAddress);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool CheckBleDeviceConnectedWinRt(ulong rawAddress)
        {
            try
            {
                var task = Task.Run(async () =>
                {
                    var bleDev = await Windows.Devices.Bluetooth.BluetoothLEDevice.FromBluetoothAddressAsync(rawAddress);
                    return bleDev != null && bleDev.ConnectionStatus == Windows.Devices.Bluetooth.BluetoothConnectionStatus.Connected;
                });
                if (task.Wait(300))
                {
                    return task.Result;
                }
            }
            catch { }
            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool CheckClassicDeviceConnectedWinRt(ulong rawAddress)
        {
            try
            {
                var task = Task.Run(async () =>
                {
                    var classicDev = await Windows.Devices.Bluetooth.BluetoothDevice.FromBluetoothAddressAsync(rawAddress);
                    return classicDev != null && classicDev.ConnectionStatus == Windows.Devices.Bluetooth.BluetoothConnectionStatus.Connected;
                });
                if (task.Wait(300))
                {
                    return task.Result;
                }
            }
            catch { }
            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int? QueryDeviceBatteryLevelWinRt(ulong rawAddress, bool isBle)
        {
            try
            {
                var task = Task.Run(async () =>
                {
                    string deviceId = null;
                    if (isBle)
                    {
                        var ble = await Windows.Devices.Bluetooth.BluetoothLEDevice.FromBluetoothAddressAsync(rawAddress);
                        if (ble != null && ble.ConnectionStatus == Windows.Devices.Bluetooth.BluetoothConnectionStatus.Connected)
                        {
                            deviceId = ble.DeviceId;
                        }
                    }
                    else
                    {
                        var classic = await Windows.Devices.Bluetooth.BluetoothDevice.FromBluetoothAddressAsync(rawAddress);
                        if (classic != null && classic.ConnectionStatus == Windows.Devices.Bluetooth.BluetoothConnectionStatus.Connected)
                        {
                            deviceId = classic.DeviceId;
                        }
                    }

                    if (!string.IsNullOrEmpty(deviceId))
                    {
                        var devInfo = await Windows.Devices.Enumeration.DeviceInformation.CreateFromIdAsync(
                            deviceId, 
                            new[] { "System.Devices.BatteryLifePercent" });
                        if (devInfo != null && devInfo.Properties.TryGetValue("System.Devices.BatteryLifePercent", out var val) && val != null)
                        {
                            return (int?)Convert.ToInt32(val);
                        }
                    }
                    return null;
                });

                if (task.Wait(350))
                {
                    return task.Result;
                }
            }
            catch { }
            return null;
        }
    }
}

