using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using BlueDoc.Core.Enums;
using BlueDoc.Core.Models;
using BlueDoc.Infrastructure.Native;
using BlueDoc.Infrastructure.Platform;
using Windows.Devices.Radios;

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
                // 1. On Windows 10/11, check the modern Radio Manager first
                if (OperatingSystemInspector.IsWindows10OrGreater())
                {
                    bool? winRtState = GetWinRtRadioStateInternal();
                    if (winRtState.HasValue)
                    {
                        return winRtState.Value;
                    }
                }

                // 2. Fallback / Windows 7 check: verify adapter DevNode
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

                // Check primary adapter problem code (e.g. Code 22 = disabled)
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

        public async Task<bool> SetRadioPowerStateAsync(bool enable, string deviceInstanceId = null)
        {
            try
            {
                bool success = false;

                // 1. On Windows 10/11: Use Windows.Devices.Radios.Radio (No UAC elevation required)
                if (OperatingSystemInspector.IsWindows10OrGreater())
                {
                    success = await SetWinRtRadioStateInternalAsync(enable);
                }

                // 2. If turning ON, guarantee the Bluetooth Support Service is running
                if (enable)
                {
                    EnsureBluetoothServiceStarted();
                }
                else
                {
                    StopBluetoothService();
                }

                // 3. Fallback for Windows 7 or if WinRT radio control is not supported
                if (!success)
                {
                    var primary = _inspector.GetPrimaryAdapter();
                    var targetId = !string.IsNullOrEmpty(deviceInstanceId)
                        ? deviceInstanceId
                        : primary?.DeviceInstanceId;

                    if (!string.IsNullOrEmpty(targetId))
                    {
                        success = await Task.Run(() => SetPnpDeviceState(targetId, enable));
                    }
                }

                return success;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"HardwareRadioManager error: {ex.Message}");
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task<bool> SetWinRtRadioStateInternalAsync(bool enable)
        {
            try
            {
                try
                {
                    await Radio.RequestAccessAsync();
                }
                catch { }

                var radios = await Radio.GetRadiosAsync();
                var btRadio = radios.FirstOrDefault(r => r.Kind == RadioKind.Bluetooth);
                if (btRadio != null)
                {
                    var targetState = enable ? RadioState.On : RadioState.Off;
                    var result = await btRadio.SetStateAsync(targetState);
                    return result == RadioAccessStatus.Allowed;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HardwareRadioManager] WinRT Radio set state error: {ex.Message}");
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool? GetWinRtRadioStateInternal()
        {
            try
            {
                var task = Task.Run(async () =>
                {
                    var radios = await Radio.GetRadiosAsync();
                    return radios.FirstOrDefault(r => r.Kind == RadioKind.Bluetooth);
                });

                if (task.Wait(1500) && task.Result != null)
                {
                    return task.Result.State == RadioState.On;
                }
            }
            catch
            {
            }
            return null;
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

        private void StopBluetoothService()
        {
            try
            {
                using (var sc = new ServiceController("bthserv"))
                {
                    if (sc.Status == ServiceControllerStatus.Running)
                    {
                        sc.Stop();
                        sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(3));
                    }
                }
            }
            catch { }
        }
    }
}
