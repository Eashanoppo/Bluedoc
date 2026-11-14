using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using BlueDoc.Core.Enums;
using BlueDoc.Core.Interfaces;
using BlueDoc.Core.Models;
using BlueDoc.Infrastructure.Hardware;
using BlueDoc.Infrastructure.Native;
using BlueDoc.Infrastructure.Platform;

namespace BlueDoc.Bluetooth.Legacy
{
    public class Win32BluetoothRadioProvider : IBluetoothProvider
    {
        public string ProviderName => "Win32 Classic & PnP Bluetooth Provider (bthprops.cpl)";
        public bool IsSupportedOnCurrentPlatform => true; // Supported on Windows 7 SP1, 10, and 11

        private readonly IHardwareInspector _hardwareInspector;

        public Win32BluetoothRadioProvider(IHardwareInspector hardwareInspector = null)
        {
            _hardwareInspector = hardwareInspector ?? new SetupApiHardwareInspector();
        }

        public Task<IReadOnlyList<BluetoothAdapterInfo>> GetAdaptersAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.Run(() => _hardwareInspector.EnumerateAdapters(), cancellationToken);
        }

        public Task<bool> IsRadioEnabledAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.Run(() =>
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
                        if (hRadio != IntPtr.Zero)
                        {
                            BluetoothNative.CloseHandle(hRadio);
                        }
                        BluetoothNative.BluetoothFindRadioClose(hFind);
                        return true;
                    }
                }
                catch
                {
                    // Fallback to checking adapter status
                }

                var primary = _hardwareInspector.GetPrimaryAdapter();
                return primary != null && primary.Status == AdapterStatus.Operational;
            }, cancellationToken);
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct BLUETOOTH_DEVICE_SEARCH_PARAMS
        {
            public uint dwSize;
            public bool fReturnAuthenticated;
            public bool fReturnRemembered;
            public bool fReturnUnknown;
            public bool fReturnConnected;
            public bool fIssueInquiry;
            public byte cTimeoutMultiplier;
            public IntPtr hRadio;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct BLUETOOTH_DEVICE_INFO
        {
            public uint dwSize;
            public ulong Address;
            public uint ulClassofDevice;
            public bool fConnected;
            public bool fRemembered;
            public bool fAuthenticated;
            public SYSTEMTIME stLastSeen;
            public SYSTEMTIME stLastUsed;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 248)]
            public string szName;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SYSTEMTIME
        {
            public ushort wYear;
            public ushort wMonth;
            public ushort wDayOfWeek;
            public ushort wDay;
            public ushort wHour;
            public ushort wMinute;
            public ushort wSecond;
            public ushort wMilliseconds;

            public DateTime ToDateTime()
            {
                try
                {
                    if (wYear >= 1601 && wYear <= 30827 && wMonth >= 1 && wMonth <= 12 && wDay >= 1 && wDay <= 31)
                    {
                        return new DateTime(wYear, wMonth, wDay, wHour, wMinute, wSecond, wMilliseconds, DateTimeKind.Utc);
                    }
                }
                catch { }
                return DateTime.UtcNow;
            }
        }

        [DllImport("bthprops.cpl", SetLastError = true)]
        private static extern IntPtr BluetoothFindFirstDevice(
            ref BLUETOOTH_DEVICE_SEARCH_PARAMS pbtsp,
            ref BLUETOOTH_DEVICE_INFO pbtdi);

        [DllImport("bthprops.cpl", SetLastError = true)]
        private static extern bool BluetoothFindNextDevice(
            IntPtr hFind,
            ref BLUETOOTH_DEVICE_INFO pbtdi);

        [DllImport("bthprops.cpl", SetLastError = true)]
        private static extern bool BluetoothFindDeviceClose(IntPtr hFind);

        public Task<IReadOnlyList<BluetoothDeviceInfo>> GetPairedDevicesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.Run<IReadOnlyList<BluetoothDeviceInfo>>(() =>
            {
                var dictionary = new Dictionary<string, BluetoothDeviceInfo>(StringComparer.OrdinalIgnoreCase);

                // 1. Enumerate via PnP device tree (reliable for both Classic and BLE paired devices)
                var pnpDevices = PnpBluetoothDeviceInspector.EnumeratePairedDevices();
                foreach (var d in pnpDevices)
                {
                    dictionary[d.BluetoothAddress] = d;
                }

                // 2. Query Win32 API without inquiry (instant cached return)
                try
                {
                    var searchParams = new BLUETOOTH_DEVICE_SEARCH_PARAMS
                    {
                        dwSize = (uint)Marshal.SizeOf(typeof(BLUETOOTH_DEVICE_SEARCH_PARAMS)),
                        fReturnAuthenticated = true,
                        fReturnRemembered = true,
                        fReturnConnected = true,
                        fReturnUnknown = false,
                        fIssueInquiry = false,
                        cTimeoutMultiplier = 1,
                        hRadio = IntPtr.Zero
                    };

                    var devInfo = new BLUETOOTH_DEVICE_INFO
                    {
                        dwSize = (uint)Marshal.SizeOf(typeof(BLUETOOTH_DEVICE_INFO))
                    };

                    IntPtr hFind = BluetoothFindFirstDevice(ref searchParams, ref devInfo);
                    if ((hFind == IntPtr.Zero || hFind == (IntPtr)(-1)) && !cancellationToken.IsCancellationRequested)
                    {
                        // Fallback: On Windows 7 or specific vendor stacks, query explicit radio handle
                        try
                        {
                            var pRadioParams = new BluetoothNative.BLUETOOTH_FIND_RADIO_PARAMS
                            {
                                dwSize = (uint)Marshal.SizeOf(typeof(BluetoothNative.BLUETOOTH_FIND_RADIO_PARAMS))
                            };
                            IntPtr hRadio = IntPtr.Zero;
                            IntPtr hFindRadio = BluetoothNative.BluetoothFindFirstRadio(ref pRadioParams, out hRadio);
                            if (hFindRadio != IntPtr.Zero && hFindRadio != (IntPtr)(-1))
                            {
                                try
                                {
                                    if (hRadio != IntPtr.Zero)
                                    {
                                        searchParams.hRadio = hRadio;
                                        hFind = BluetoothFindFirstDevice(ref searchParams, ref devInfo);
                                    }
                                }
                                finally
                                {
                                    if (hRadio != IntPtr.Zero) BluetoothNative.CloseHandle(hRadio);
                                    BluetoothNative.BluetoothFindRadioClose(hFindRadio);
                                }
                            }
                        }
                        catch { }
                    }

                    if (hFind != IntPtr.Zero && hFind != (IntPtr)(-1))
                    {
                        try
                        {
                            do
                            {
                                if (cancellationToken.IsCancellationRequested) break;

                                string addressHex = string.Format("{0:X12}", devInfo.Address);
                                string formattedAddress = string.Join(":", Enumerable.Range(0, 6).Select(i => addressHex.Substring(i * 2, 2)));

                                DeviceClassDecoder.Decode(devInfo.ulClassofDevice, out var category, out var major, out var icon);

                                if (dictionary.TryGetValue(formattedAddress, out var existing))
                                {
                                    // Preserve verified link status on Win10/11; only accept devInfo.fConnected on Win7
                                    if (!OperatingSystemInspector.IsWindows10OrGreater())
                                    {
                                        existing.IsConnected = devInfo.fConnected;
                                    }
                                    existing.IsPaired = existing.IsPaired || devInfo.fAuthenticated;
                                    existing.IsRemembered = existing.IsRemembered || devInfo.fRemembered;
                                    if (!string.IsNullOrWhiteSpace(devInfo.szName)) existing.DeviceName = devInfo.szName;
                                    if (devInfo.ulClassofDevice != 0)
                                    {
                                        existing.DeviceClass = devInfo.ulClassofDevice;
                                        existing.CategoryName = category;
                                        existing.MajorDeviceClass = major;
                                        existing.CategoryIcon = icon;
                                    }
                                }
                                else
                                {
                                    bool isConn = devInfo.fConnected;
                                    if (OperatingSystemInspector.IsWindows10OrGreater() && devInfo.Address != 0)
                                    {
                                        isConn = PnpBluetoothDeviceInspector.CheckDeviceConnectedWinRt(devInfo.Address, false);
                                    }

                                    var device = new BluetoothDeviceInfo
                                    {
                                        DeviceName = devInfo.szName,
                                        BluetoothAddress = formattedAddress,
                                        RawAddress = devInfo.Address,
                                        IsConnected = isConn,
                                        IsPaired = devInfo.fAuthenticated,
                                        IsRemembered = devInfo.fRemembered,
                                        DeviceType = DeviceType.Classic,
                                        DeviceClass = devInfo.ulClassofDevice,
                                        CategoryName = category,
                                        MajorDeviceClass = major,
                                        CategoryIcon = icon,
                                        LastSeen = devInfo.stLastSeen.ToDateTime()
                                    };
                                    dictionary[formattedAddress] = device;
                                }

                                devInfo.dwSize = (uint)Marshal.SizeOf(typeof(BLUETOOTH_DEVICE_INFO));
                            }
                            while (BluetoothFindNextDevice(hFind, ref devInfo));
                        }
                        finally
                        {
                            BluetoothFindDeviceClose(hFind);
                        }
                    }
                }
                catch { }

                return dictionary.Values.ToList();
            }, cancellationToken);
        }

        public Task<IReadOnlyList<BluetoothDeviceInfo>> DiscoverDevicesAsync(
            int timeoutSeconds, 
            IProgress<BluetoothDeviceInfo> progress = null, 
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.Run<IReadOnlyList<BluetoothDeviceInfo>>(() =>
            {
                var discovered = new Dictionary<string, BluetoothDeviceInfo>(StringComparer.OrdinalIgnoreCase);

                if (cancellationToken.IsCancellationRequested)
                {
                    return discovered.Values.ToList();
                }

                // Active radio inquiry: ONLY return devices that physically respond to RF inquiry or are actively connected
                try
                {
                    var searchParams = new BLUETOOTH_DEVICE_SEARCH_PARAMS
                    {
                        dwSize = (uint)Marshal.SizeOf(typeof(BLUETOOTH_DEVICE_SEARCH_PARAMS)),
                        fReturnAuthenticated = true,  // Return paired devices if responding to inquiry
                        fReturnRemembered = false,    // Do NOT return offline cached devices
                        fReturnUnknown = true,       // Return newly broadcasting devices discovered in the air
                        fReturnConnected = true,     // Return actively connected devices
                        fIssueInquiry = true,        // Send physical 1.28s RF inquiry packets
                        cTimeoutMultiplier = (byte)Math.Min(Math.Max(2, timeoutSeconds / 1.28), 10),
                        hRadio = IntPtr.Zero         // Set to NULL to perform search across all local Bluetooth hardware radios
                    };

                    var devInfo = new BLUETOOTH_DEVICE_INFO
                    {
                        dwSize = (uint)Marshal.SizeOf(typeof(BLUETOOTH_DEVICE_INFO))
                    };

                    IntPtr hFind = BluetoothFindFirstDevice(ref searchParams, ref devInfo);
                    if ((hFind == IntPtr.Zero || hFind == (IntPtr)(-1)) && !cancellationToken.IsCancellationRequested)
                    {
                        // Fallback: On Windows 7 or specific vendor stacks, query explicit radio handle
                        try
                        {
                            var pRadioParams = new BluetoothNative.BLUETOOTH_FIND_RADIO_PARAMS
                            {
                                dwSize = (uint)Marshal.SizeOf(typeof(BluetoothNative.BLUETOOTH_FIND_RADIO_PARAMS))
                            };
                            IntPtr hRadio = IntPtr.Zero;
                            IntPtr hFindRadio = BluetoothNative.BluetoothFindFirstRadio(ref pRadioParams, out hRadio);
                            if (hFindRadio != IntPtr.Zero && hFindRadio != (IntPtr)(-1))
                            {
                                try
                                {
                                    if (hRadio != IntPtr.Zero)
                                    {
                                        searchParams.hRadio = hRadio;
                                        hFind = BluetoothFindFirstDevice(ref searchParams, ref devInfo);
                                    }
                                }
                                finally
                                {
                                    if (hRadio != IntPtr.Zero) BluetoothNative.CloseHandle(hRadio);
                                    BluetoothNative.BluetoothFindRadioClose(hFindRadio);
                                }
                            }
                        }
                        catch { }
                    }

                    if (hFind != IntPtr.Zero && hFind != (IntPtr)(-1))
                    {
                        try
                        {
                            do
                            {
                                if (cancellationToken.IsCancellationRequested) break;

                                string addressHex = string.Format("{0:X12}", devInfo.Address);
                                string formattedAddress = string.Join(":", Enumerable.Range(0, 6).Select(i => addressHex.Substring(i * 2, 2)));

                                DeviceClassDecoder.Decode(devInfo.ulClassofDevice, out var category, out var major, out var icon);

                                bool isConn = devInfo.fConnected;
                                if (OperatingSystemInspector.IsWindows10OrGreater() && devInfo.Address != 0)
                                {
                                    isConn = PnpBluetoothDeviceInspector.CheckDeviceConnectedWinRt(devInfo.Address, false);
                                }

                                if (discovered.TryGetValue(formattedAddress, out var existing))
                                {
                                    existing.IsConnected = isConn;
                                    existing.IsPaired = existing.IsPaired || devInfo.fAuthenticated;
                                    existing.IsRemembered = existing.IsRemembered || devInfo.fRemembered;
                                    if (!string.IsNullOrWhiteSpace(devInfo.szName)) existing.DeviceName = devInfo.szName;
                                    if (devInfo.ulClassofDevice != 0)
                                    {
                                        existing.DeviceClass = devInfo.ulClassofDevice;
                                        existing.CategoryName = category;
                                        existing.MajorDeviceClass = major;
                                        existing.CategoryIcon = icon;
                                    }
                                    existing.LastSeen = DateTime.UtcNow;
                                    progress?.Report(existing);
                                }
                                else
                                {
                                    var device = new BluetoothDeviceInfo
                                    {
                                        DeviceName = !string.IsNullOrWhiteSpace(devInfo.szName) ? devInfo.szName : "Bluetooth Device",
                                        BluetoothAddress = formattedAddress,
                                        RawAddress = devInfo.Address,
                                        IsConnected = isConn,
                                        IsPaired = devInfo.fAuthenticated,
                                        IsRemembered = devInfo.fRemembered,
                                        IsAvailable = true,
                                        DeviceType = DeviceType.Classic,
                                        DeviceClass = devInfo.ulClassofDevice,
                                        CategoryName = category,
                                        MajorDeviceClass = major,
                                        CategoryIcon = icon,
                                        LastSeen = DateTime.UtcNow
                                    };
                                    discovered[formattedAddress] = device;
                                    progress?.Report(device);
                                }

                                devInfo.dwSize = (uint)Marshal.SizeOf(typeof(BLUETOOTH_DEVICE_INFO));
                            }
                            while (BluetoothFindNextDevice(hFind, ref devInfo));
                        }
                        finally
                        {
                            BluetoothFindDeviceClose(hFind);
                        }
                    }
                }
                catch { }

                return discovered.Values.ToList();
            }, cancellationToken);
        }

        public Task<bool> RemoveDeviceAsync(string bluetoothAddress)
        {
            return Task.Run(() =>
            {
                if (string.IsNullOrWhiteSpace(bluetoothAddress)) return false;

                // Strip colons/hyphens
                string cleanHex = bluetoothAddress.Replace(":", "").Replace("-", "").Trim();
                if (cleanHex.Length != 12) return false;

                try
                {
                    ulong rawAddress = Convert.ToUInt64(cleanHex, 16);
                    uint result = BluetoothNative.BluetoothRemoveDevice(ref rawAddress);
                    return result == 0; // ERROR_SUCCESS
                }
                catch
                {
                    return false;
                }
            });
        }
    }
}
