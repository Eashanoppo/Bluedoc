using System;
using System.Runtime.InteropServices;

namespace BlueDoc.Infrastructure.Native
{
    public static class CfgMgr32Native
    {
        public const uint CR_SUCCESS = 0x00000000;

        // Device Status Flags (DN_*)
        public const uint DN_ROOT_ENUMERATED = 0x00000001;
        public const uint DN_DRIVER_LOADED = 0x00000002;
        public const uint DN_ENUM_LOADED = 0x00000004;
        public const uint DN_STARTED = 0x00000008;
        public const uint DN_MANUAL = 0x00000010;
        public const uint DN_NEED_TO_ENUM = 0x00000020;
        public const uint DN_NOT_FIRST_TIME = 0x00000040;
        public const uint DN_HARDWARE_ENUM = 0x00000080;
        public const uint DN_LIAR = 0x00000100;
        public const uint DN_HAS_MARK = 0x00000200;
        public const uint DN_HAS_PROBLEM = 0x00000400;
        public const uint DN_FILTERED = 0x00000800;
        public const uint DN_MOVED = 0x00001000;
        public const uint DN_DISABLEABLE = 0x00002000;
        public const uint DN_REMOVABLE = 0x00004000;
        public const uint DN_PRIVATE_PROBLEM = 0x00008000;
        public const uint DN_MF_PARENT = 0x00010000;
        public const uint DN_MF_CHILD = 0x00020000;
        public const uint DN_WILL_BE_REMOVED = 0x00040000;

        // Configuration Manager Problem Codes (CM_PROB_*)
        public const uint CM_PROB_NOT_CONFIGURED = 1;
        public const uint CM_PROB_FAILED_FILTER = 3;
        public const uint CM_PROB_DEVLOADER_NOT_FOUND = 4;
        public const uint CM_PROB_INVALID_DATA = 5;
        public const uint CM_PROB_FAILED_START = 10;
        public const uint CM_PROB_LIAR = 11;
        public const uint CM_PROB_NORMAL_CONFLICT = 12;
        public const uint CM_PROB_NOT_VERIFIED = 13;
        public const uint CM_PROB_NEED_RESTART = 14;
        public const uint CM_PROB_REENUMERATION = 15;
        public const uint CM_PROB_PARTIAL_LOG_CONF = 16;
        public const uint CM_PROB_UNKNOWN_RESOURCE = 17;
        public const uint CM_PROB_REINSTALL = 18;
        public const uint CM_PROB_REGISTRY = 19;
        public const uint CM_PROB_VXDLDR = 20;
        public const uint CM_PROB_WILL_BE_REMOVED = 21;
        public const uint CM_PROB_DISABLED = 22;
        public const uint CM_PROB_DEVLOADER_NOT_READY = 23;
        public const uint CM_PROB_DEVICE_NOT_THERE = 24;
        public const uint CM_PROB_MOVED = 25;
        public const uint CM_PROB_TOO_EARLY = 26;
        public const uint CM_PROB_NO_VALID_LOG_CONF = 27;
        public const uint CM_PROB_FAILED_INSTALL = 28;
        public const uint CM_PROB_HARDWARE_DISABLED = 29;
        public const uint CM_PROB_CANT_SHARE_IRQ = 30;
        public const uint CM_PROB_FAILED_ADD = 31;
        public const uint CM_PROB_DISABLED_SERVICE = 32;
        public const uint CM_PROB_TRANSLATION_FAILED = 33;
        public const uint CM_PROB_NO_SOFTCONFIG = 34;
        public const uint CM_PROB_BIOS_TABLE = 35;
        public const uint CM_PROB_IRQ_TRANSLATION_FAILED = 36;
        public const uint CM_PROB_FAILED_DRIVER_ENTRY = 37;
        public const uint CM_PROB_DRIVER_FAILED_PRIOR_UNLOAD = 38;
        public const uint CM_PROB_DRIVER_FAILED_LOAD = 39;
        public const uint CM_PROB_DRIVER_SERVICE_KEY_INVALID = 40;
        public const uint CM_PROB_LEGACY_SERVICE_NO_DEVICES = 41;
        public const uint CM_PROB_DUPLICATE_DEVICE = 42;
        public const uint CM_PROB_FAILED_POST_START = 43;
        public const uint CM_PROB_HALTED = 44;
        public const uint CM_PROB_PHANTOM = 45;
        public const uint CM_PROB_SYSTEM_SHUTDOWN = 46;
        public const uint CM_PROB_HELD_VXD = 47;
        public const uint CM_PROB_WAITING_ON_DEPENDENCY = 48;
        public const uint CM_PROB_UNSIGNED_DRIVER = 49;

        [DllImport("cfgmgr32.dll", SetLastError = true)]
        public static extern uint CM_Get_DevNode_Status(out uint pulStatus, out uint pulProblemNumber, uint dnDevInst, uint ulFlags);

        [DllImport("cfgmgr32.dll", SetLastError = true)]
        public static extern uint CM_Enable_DevNode(uint dnDevInst, uint ulFlags);

        [DllImport("cfgmgr32.dll", SetLastError = true)]
        public static extern uint CM_Disable_DevNode(uint dnDevInst, uint ulFlags);

        public const uint CM_REENUMERATE_NORMAL = 0x00000000;

        [DllImport("cfgmgr32.dll", SetLastError = true)]
        public static extern uint CM_Reenumerate_DevNode(uint dnDevInst, uint ulFlags);

        [DllImport("cfgmgr32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern uint CM_Locate_DevNode(out uint pdnDevInst, string pDeviceID, uint ulFlags);
    }
}
