using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using BlueDoc.Core.Enums;
using BlueDoc.Core.Models;
using BlueDoc.Infrastructure.Native;

namespace BlueDoc.Infrastructure.Hardware
{
    public class HardwareRadioManager
    {
        private readonly SetupApiHardwareInspector _inspector;

        public HardwareRadioManager(SetupApiHardwareInspector inspector = null)
        {
            _inspector = inspector ?? new SetupApiHardwareInspector();
        }

        public bool IsRadioPoweredOn(string deviceInstanceId = null)
        {
            try
            {
                var primary = _inspector.GetPrimaryAdapter();
                if (primary == null) return false;

                if (!string.IsNullOrEmpty(deviceInstanceId))
                {
                    uint devInst;
                    if (CfgMgr32Native.CM_Locate_DevNode(out devInst, deviceInstanceId, 0) == CfgMgr32Native.CR_SUCCESS)
                    {
                        uint status, problem;
                        if (CfgMgr32Native.CM_Get_DevNode_Status(out status, out problem, devInst, 0) == CfgMgr32Native.CR_SUCCESS)
                        {
                            if (problem == CfgMgr32Native.CM_PROB_DISABLED) return false;
                            return (status & CfgMgr32Native.DN_STARTED) != 0;
                        }
                    }
                }

                // Check primary adapter problem code
                if (primary.ProblemCode == 22 || !primary.IsEnabled)
                {
                    return false;
                }

                // Check Win32 radio handle
                var findParams = new BluetoothNative.BLUETOOTH_FIND_RADIO_PARAMS
                {
                    dwSize = (uint)Marshal.SizeOf(typeof(BluetoothNative.BLUETOOTH_FIND_RADIO_PARAMS))
                };
                IntPtr hRadio;
                IntPtr hFind = BluetoothNative.BluetoothFindFirstRadio(ref findParams, out hRadio);
                if (hFind != IntPtr.Zero && hFind != (IntPtr)(-1))
                {
                    BluetoothNative.BluetoothFindRadioClose(hFind);
                    if (hRadio != IntPtr.Zero)
                    {
                        BluetoothNative.CloseHandle(hRadio);
                        return true;
                    }
                }

                return primary.Status == AdapterStatus.Operational;
            }
            catch
            {
                return false;
            }
        }

        public Task<bool> SetRadioPowerStateAsync(bool enable, string deviceInstanceId = null)
        {
            return Task.Run(() =>
            {
                try
                {
                    var primary = _inspector.GetPrimaryAdapter();
                    var targetId = !string.IsNullOrEmpty(deviceInstanceId)
                        ? deviceInstanceId
                        : primary?.DeviceInstanceId;

                    if (string.IsNullOrEmpty(targetId))
                    {
                        return false;
                    }

                    bool pnpSuccess = SetPnpDeviceState(targetId, enable);

                    // If turning ON and bthserv is stopped, ensure bthserv starts so Windows apps can communicate
                    if (enable)
                    {
                        EnsureBluetoothServiceStarted();
                    }

                    return pnpSuccess;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"HardwareRadioManager error: {ex.Message}");
                    return false;
                }
            });
        }

        private bool SetPnpDeviceState(string instanceId, bool enable)
        {
            try
            {
                // Method 1: CfgMgr32 direct DevNode power control
                uint devInst;
                if (CfgMgr32Native.CM_Locate_DevNode(out devInst, instanceId, 0) == CfgMgr32Native.CR_SUCCESS)
                {
                    uint cr = enable
                        ? CfgMgr32Native.CM_Enable_DevNode(devInst, 0)
                        : CfgMgr32Native.CM_Disable_DevNode(devInst, 0);

                    if (cr == CfgMgr32Native.CR_SUCCESS)
                    {
                        return true;
                    }
                }

                // Method 2: SetupAPI DIF_PROPERTYCHANGE Class Installer
                Guid btGuid = SetupApiNative.GUID_DEVCLASS_BLUETOOTH;
                IntPtr hDevInfo = SetupApiNative.SetupDiGetClassDevs(ref btGuid, null, IntPtr.Zero, 0);
                if (hDevInfo == (IntPtr)(-1) || hDevInfo == IntPtr.Zero)
                {
                    hDevInfo = SetupApiNative.SetupDiGetClassDevs(ref btGuid, null, IntPtr.Zero, SetupApiNative.DIGCF_PRESENT);
                }
                if (hDevInfo == (IntPtr)(-1) || hDevInfo == IntPtr.Zero) return false;

                try
                {
                    var devData = new SetupApiNative.SP_DEVINFO_DATA
                    {
                        cbSize = Marshal.SizeOf(typeof(SetupApiNative.SP_DEVINFO_DATA))
                    };

                    int index = 0;
                    while (SetupApiNative.SetupDiEnumDeviceInfo(hDevInfo, index++, ref devData))
                    {
                        var sbId = new StringBuilder(512);
                        int required;
                        if (SetupApiNative.SetupDiGetDeviceInstanceId(hDevInfo, ref devData, sbId, sbId.Capacity, out required))
                        {
                            if (string.Equals(sbId.ToString(), instanceId, StringComparison.OrdinalIgnoreCase))
                            {
                                var propChange = new SetupApiNative.SP_PROPCHANGE_PARAMS
                                {
                                    ClassInstallHeader = new SetupApiNative.SP_CLASSINSTALL_HEADER
                                    {
                                        cbSize = Marshal.SizeOf(typeof(SetupApiNative.SP_CLASSINSTALL_HEADER)),
                                        InstallFunction = SetupApiNative.DIF_PROPERTYCHANGE
                                    },
                                    StateChange = enable ? SetupApiNative.DICS_ENABLE : SetupApiNative.DICS_DISABLE,
                                    Scope = SetupApiNative.DICS_FLAG_GLOBAL,
                                    HwProfile = 0
                                };

                                int size = Marshal.SizeOf(typeof(SetupApiNative.SP_PROPCHANGE_PARAMS));
                                if (SetupApiNative.SetupDiSetClassInstallParams(hDevInfo, ref devData, ref propChange, size))
                                {
                                    return SetupApiNative.SetupDiCallClassInstaller(SetupApiNative.DIF_PROPERTYCHANGE, hDevInfo, ref devData);
                                }
                            }
                        }
                    }
                }
                finally
                {
                    SetupApiNative.SetupDiDestroyDeviceInfoList(hDevInfo);
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private void EnsureBluetoothServiceStarted()
        {
            try
            {
                using (var sc = new ServiceController("bthserv"))
                {
                    if (sc.Status != ServiceControllerStatus.Running && sc.Status != ServiceControllerStatus.StartPending)
                    {
                        sc.Start();
                        sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(5));
                    }
                }
            }
            catch { }
        }
    }
}
