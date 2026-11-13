using System;
using System.Runtime.InteropServices;
using System.Text;

namespace BlueDoc.Infrastructure.Native
{
    public static class SetupApiNative
    {
        public const int DIGCF_DEFAULT = 0x00000001;
        public const int DIGCF_PRESENT = 0x00000002;
        public const int DIGCF_ALLCLASSES = 0x00000004;
        public const int DIGCF_PROFILE = 0x00000008;
        public const int DIGCF_DEVICEINTERFACE = 0x00000010;

        public const int SPDRP_DEVICEDESC = 0x00000000;
        public const int SPDRP_HARDWAREID = 0x00000001;
        public const int SPDRP_COMPATIBLEIDS = 0x00000002;
        public const int SPDRP_CLASS = 0x00000007;
        public const int SPDRP_CLASSGUID = 0x00000008;
        public const int SPDRP_DRIVER = 0x00000009;
        public const int SPDRP_MFG = 0x0000000B;
        public const int SPDRP_FRIENDLYNAME = 0x0000000C;
        public const int SPDRP_LOCATION_INFORMATION = 0x0000000D;
        public const int SPDRP_PHYSICAL_DEVICE_OBJECT_NAME = 0x0000000E;
        public const int SPDRP_CAPABILITIES = 0x0000000F;
        public const int SPDRP_UI_NUMBER = 0x00000010;
        public const int SPDRP_UPPERFILTERS = 0x00000011;
        public const int SPDRP_LOWERFILTERS = 0x00000012;
        public const int SPDRP_BUSTYPEGUID = 0x00000013;
        public const int SPDRP_LEGACYBUSTYPE = 0x00000014;
        public const int SPDRP_BUSNUMBER = 0x00000015;
        public const int SPDRP_ENUMERATOR_NAME = 0x00000016;
        public const int SPDRP_DEVTYPE = 0x00000019;
        public const int SPDRP_EXCLUSIVE = 0x0000001A;
        public const int SPDRP_CHARACTERISTICS = 0x0000001B;
        public const int SPDRP_ADDRESS = 0x0000001C;
        public const int SPDRP_UI_NUMBER_DESC_FORMAT = 0x0000001D;
        public const int SPDRP_DEVICE_POWER_DATA = 0x0000001E;
        public const int SPDRP_REMOVAL_POLICY = 0x0000001F;
        public const int SPDRP_REMOVAL_POLICY_HW_DEFAULT = 0x00000020;
        public const int SPDRP_REMOVAL_POLICY_OVERRIDE = 0x00000021;
        public const int SPDRP_INSTALL_STATE = 0x00000022;

        public const int DICS_FLAG_GLOBAL = 0x00000001;
        public const int DIREG_DEV = 0x00000001;
        public const int DIREG_DRV = 0x00000002;
        public const int KEY_READ = 0x20019;

        public const int DIF_PROPERTYCHANGE = 0x00000012;
        public const uint DICS_ENABLE = 1;
        public const uint DICS_DISABLE = 2;
        public const uint DICS_PROPCHANGE = 3;

        [StructLayout(LayoutKind.Sequential)]
        public struct SP_CLASSINSTALL_HEADER
        {
            public int cbSize;
            public int InstallFunction;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SP_PROPCHANGE_PARAMS
        {
            public SP_CLASSINSTALL_HEADER ClassInstallHeader;
            public uint StateChange;
            public uint Scope;
            public uint HwProfile;
        }

        // Bluetooth Setup Class GUID: {e0cbf06c-cd8b-4647-bb8a-263b43f0f974}
        public static readonly Guid GUID_DEVCLASS_BLUETOOTH = new Guid("{e0cbf06c-cd8b-4647-bb8a-263b43f0f974}");

        // Bluetooth Radio Interface GUID: {0850302a-b344-4be4-9e83-506a92203794}
        public static readonly Guid GUID_BTHPORT_DEVICE_INTERFACE = new Guid("{0850302a-b344-4be4-9e83-506a92203794}");

        [StructLayout(LayoutKind.Sequential)]
        public struct SP_DEVINFO_DATA
        {
            public int cbSize;
            public Guid ClassGuid;
            public uint DevInst;
            public IntPtr Reserved;
        }

        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern IntPtr SetupDiGetClassDevs(
            ref Guid ClassGuid,
            [MarshalAs(UnmanagedType.LPWStr)] string Enumerator,
            IntPtr hwndParent,
            int Flags);

        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern IntPtr SetupDiGetClassDevs(
            IntPtr ClassGuid,
            [MarshalAs(UnmanagedType.LPWStr)] string Enumerator,
            IntPtr hwndParent,
            int Flags);

        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern bool SetupDiEnumDeviceInfo(
            IntPtr DeviceInfoSet,
            int MemberIndex,
            ref SP_DEVINFO_DATA DeviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool SetupDiGetDeviceRegistryProperty(
            IntPtr DeviceInfoSet,
            ref SP_DEVINFO_DATA DeviceInfoData,
            int Property,
            out uint PropertyRegDataType,
            byte[] PropertyBuffer,
            uint PropertyBufferSize,
            out uint RequiredSize);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool SetupDiGetDeviceInstanceId(
            IntPtr DeviceInfoSet,
            ref SP_DEVINFO_DATA DeviceInfoData,
            StringBuilder DeviceInstanceId,
            int DeviceInstanceIdSize,
            out int RequiredSize);

        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern IntPtr SetupDiOpenDevRegKey(
            IntPtr DeviceInfoSet,
            ref SP_DEVINFO_DATA DeviceInfoData,
            int Scope,
            int HwProfile,
            int KeyType,
            int samDesired);

        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);

        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern bool SetupDiSetClassInstallParams(
            IntPtr DeviceInfoSet,
            ref SP_DEVINFO_DATA DeviceInfoData,
            ref SP_PROPCHANGE_PARAMS ClassInstallParams,
            int ClassInstallParamsSize);

        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern bool SetupDiCallClassInstaller(
            int InstallFunction,
            IntPtr DeviceInfoSet,
            ref SP_DEVINFO_DATA DeviceInfoData);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern int RegCloseKey(IntPtr hKey);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int RegQueryValueEx(
            IntPtr hKey,
            string lpValueName,
            int lpReserved,
            out uint lpType,
            byte[] lpData,
            ref uint lpcbData);
    }
}
