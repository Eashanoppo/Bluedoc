using System;
using System.Runtime.InteropServices;

namespace BlueDoc.Infrastructure.Native
{
    public static class BluetoothNative
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct BLUETOOTH_FIND_RADIO_PARAMS
        {
            public uint dwSize;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct BLUETOOTH_RADIO_INFO
        {
            public uint dwSize;
            public ulong address;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 248)]
            public string szName;
            public uint ulClassofDevice;
            public ushort lmpSubversion;
            public ushort manufacturer;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct BLUETOOTH_DEVICE_SEARCH_PARAMS
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

        [StructLayout(LayoutKind.Sequential)]
        public struct SYSTEMTIME
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

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct BLUETOOTH_DEVICE_INFO
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

        public const uint BLUETOOTH_SERVICE_DISABLE = 0x00;
        public const uint BLUETOOTH_SERVICE_ENABLE = 0x01;

        public static readonly Guid AudioSinkServiceGuid = new Guid("0000110b-0000-1000-8000-00805f9b34fb");
        public static readonly Guid HandsfreeServiceGuid = new Guid("0000111e-0000-1000-8000-00805f9b34fb");
        public static readonly Guid HeadsetServiceGuid = new Guid("00001108-0000-1000-8000-00805f9b34fb");
        public static readonly Guid HumanInterfaceDeviceServiceGuid = new Guid("00001124-0000-1000-8000-00805f9b34fb");
        public static readonly Guid SerialPortServiceGuid = new Guid("00001101-0000-1000-8000-00805f9b34fb");
        public static readonly Guid AvrcpServiceGuid = new Guid("0000110e-0000-1000-8000-00805f9b34fb");

        [DllImport("bthprops.cpl", SetLastError = true)]
        public static extern IntPtr BluetoothFindFirstRadio(
            ref BLUETOOTH_FIND_RADIO_PARAMS pbtfrp,
            out IntPtr phRadio);

        [DllImport("bthprops.cpl", SetLastError = true)]
        public static extern bool BluetoothFindNextRadio(
            IntPtr hFind,
            out IntPtr phRadio);

        [DllImport("bthprops.cpl", SetLastError = true)]
        public static extern bool BluetoothFindRadioClose(IntPtr hFind);

        [DllImport("bthprops.cpl", SetLastError = true)]
        public static extern uint BluetoothGetRadioInfo(
            IntPtr hRadio,
            ref BLUETOOTH_RADIO_INFO pRadioInfo);

        [DllImport("bthprops.cpl", SetLastError = true)]
        public static extern IntPtr BluetoothFindFirstDevice(
            ref BLUETOOTH_DEVICE_SEARCH_PARAMS pbtsp,
            ref BLUETOOTH_DEVICE_INFO pbtdi);

        [DllImport("bthprops.cpl", SetLastError = true)]
        public static extern bool BluetoothFindNextDevice(
            IntPtr hFind,
            ref BLUETOOTH_DEVICE_INFO pbtdi);

        [DllImport("bthprops.cpl", SetLastError = true)]
        public static extern bool BluetoothFindDeviceClose(IntPtr hFind);

        [DllImport("bthprops.cpl", SetLastError = true)]
        public static extern uint BluetoothGetDeviceInfo(
            IntPtr hRadio,
            ref BLUETOOTH_DEVICE_INFO pbtdi);

        [DllImport("bthprops.cpl", SetLastError = true)]
        public static extern uint BluetoothAuthenticateDevice(
            IntPtr hwndParent,
            IntPtr hRadio,
            ref BLUETOOTH_DEVICE_INFO pbtdi,
            [MarshalAs(UnmanagedType.LPWStr)] string pszPasskey,
            ulong ulPasskeyLength);

        [DllImport("bthprops.cpl", SetLastError = true)]
        public static extern uint BluetoothSetServiceState(
            IntPtr hRadio,
            ref BLUETOOTH_DEVICE_INFO pbtdi,
            ref Guid pGuidService,
            uint dwServiceFlags);

        [DllImport("bthprops.cpl", SetLastError = true)]
        public static extern uint BluetoothEnumerateInstalledServices(
            IntPtr hRadio,
            ref BLUETOOTH_DEVICE_INFO pbtdi,
            ref uint pcServices,
            [Out] Guid[] pGuidServices);

        [DllImport("bthprops.cpl", SetLastError = true)]
        public static extern uint BluetoothRemoveDevice(ref ulong pAddress);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr handle);
    }
}
