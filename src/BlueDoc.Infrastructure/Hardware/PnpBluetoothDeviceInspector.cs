using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using BlueDoc.Core.Enums;
using BlueDoc.Core.Models;
using BlueDoc.Infrastructure.Native;

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

                    // Check dev node status
                    uint statusFlags = 0;
                    uint problemCode = 0;
                    uint cr = CfgMgr32Native.CM_Get_DevNode_Status(out statusFlags, out problemCode, devInfoData.DevInst, 0);

                    bool isConnected = false;
                    if (cr == CfgMgr32Native.CR_SUCCESS)
                    {
                        if ((statusFlags & CfgMgr32Native.DN_STARTED) != 0 && (statusFlags & CfgMgr32Native.DN_HAS_PROBLEM) == 0)
                        {
                            isConnected = true;
                        }
                    }

                    ulong rawAddress = 0;
                    try
                    {
                        rawAddress = Convert.ToUInt64(rawAddressHex, 16);
                    }
                    catch { }

                    var device = new BluetoothDeviceInfo
                    {
                        DeviceName = bestName,
                        BluetoothAddress = formattedAddress,
                        RawAddress = rawAddress,
                        InstanceId = instanceId,
                        IsPaired = true,
                        IsRemembered = true,
                        IsConnected = isConnected,
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
    }
}

