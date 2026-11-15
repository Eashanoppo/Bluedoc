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
                var primary = _inspector.GetPrimaryAdapter();
                var targetId = !string.IsNullOrEmpty(deviceInstanceId)
                    ? deviceInstanceId
                    : primary?.DeviceInstanceId;

                // 1. If turning ON and the adapter is administratively disabled (Code 22), 
                // prioritize clearing the hardware disable state first!
                bool isDisabledInPnp = primary != null && (primary.Status == AdapterStatus.Disabled || primary.ProblemCode == 22 || !primary.IsEnabled);
                if (enable && isDisabledInPnp && !string.IsNullOrEmpty(targetId))
                {
                    await Task.Run(() => SetPnpDeviceState(targetId, true));
                }

                // 2. On Windows 10/11: Use Windows.Devices.Radios.Radio (No UAC elevation required)
                if (OperatingSystemInspector.IsWindows10OrGreater())
                {
                    bool winRtSuccess = await SetWinRtRadioStateInternalAsync(enable);
                    if (winRtSuccess) success = true;
                }

                // 3. Guarantee the Bluetooth Support Service is running
                if (enable)
                {
                    EnsureBluetoothServiceStarted();
                }
                else
                {
                    StopBluetoothService();
                }

                // 4. Fallback: Silicon Provisioning (if controller needs firmware or has problem code 10/43/28)
                if (enable && !success)
                {
                    if (BlueDoc.Infrastructure.Drivers.WinUsbFirmwarePatcher.IsSiliconUnpatched(primary))
                    {
                        var patchRes = await BlueDoc.Infrastructure.Drivers.WinUsbFirmwarePatcher.ProvisionAndPatchSiliconAsync(primary);
                        if (patchRes.Success)
                        {
                            success = true;
                            if (OperatingSystemInspector.IsWindows10OrGreater())
                            {
                                await SetWinRtRadioStateInternalAsync(true);
                            }
                        }
                    }
                }

                // 5. Fallback for Windows 7 or if WinRT radio control is not supported
                if (!success && !string.IsNullOrEmpty(targetId))
                {
                    success = await Task.Run(() => SetPnpDeviceState(targetId, enable));
                    if (success && enable && OperatingSystemInspector.IsWindows10OrGreater())
                    {
                        await SetWinRtRadioStateInternalAsync(true);
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
            if (string.IsNullOrWhiteSpace(instanceId)) return false;

            // Method 1: SetupAPI DIF_PROPERTYCHANGE (Direct Windows Kernel/PnP state manager)
            try
            {
                IntPtr hDevInfo = SetupApiNative.SetupDiGetClassDevs(IntPtr.Zero, null, IntPtr.Zero, SetupApiNative.DIGCF_ALLCLASSES);
                if (hDevInfo != (IntPtr)(-1) && hDevInfo != IntPtr.Zero)
                {
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
                            if (SetupApiNative.SetupDiGetDeviceInstanceId(hDevInfo, ref devData, sbId, sbId.Capacity, out _))
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
                                        SetupApiNative.SetupDiChangeState(hDevInfo, ref devData);
                                        SetupApiNative.SetupDiCallClassInstaller(SetupApiNative.DIF_PROPERTYCHANGE, hDevInfo, ref devData);
                                    }

                                    // If enabling, also apply to config-specific profile (HwProfile 0)
                                    if (enable)
                                    {
                                        propChange.Scope = 0x00000002; // DICS_FLAG_CONFIGSPECIFIC
                                        if (SetupApiNative.SetupDiSetClassInstallParams(hDevInfo, ref devData, ref propChange, size))
                                        {
                                            SetupApiNative.SetupDiChangeState(hDevInfo, ref devData);
                                            SetupApiNative.SetupDiCallClassInstaller(SetupApiNative.DIF_PROPERTYCHANGE, hDevInfo, ref devData);
                                        }
                                    }

                                    // Clear install params
                                    SetupApiNative.SetupDiSetClassInstallParams(hDevInfo, ref devData, IntPtr.Zero, 0);
                                    break;
                                }
                            }
                        }
                    }
                    finally
                    {
                        SetupApiNative.SetupDiDestroyDeviceInfoList(hDevInfo);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PnP] SetupAPI PropertyChange notice: {ex.Message}");
            }

            // Method 2: pnputil.exe (/enable-device or /disable-device)
            try
            {
                string pnpArg = enable ? $"/enable-device \"{instanceId}\"" : $"/disable-device \"{instanceId}\"";
                var psi = new ProcessStartInfo
                {
                    FileName = "pnputil.exe",
                    Arguments = pnpArg,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using (var proc = Process.Start(psi))
                {
                    proc?.WaitForExit(5000);
                }

                if (enable)
                {
                    var classPsi = new ProcessStartInfo
                    {
                        FileName = "pnputil.exe",
                        Arguments = "/enable-device /class \"Bluetooth\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };
                    using (var proc = Process.Start(classPsi))
                    {
                        proc?.WaitForExit(5000);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PnP] pnputil enable notice: {ex.Message}");
            }

            // Method 3: PowerShell Enable-PnpDevice / Disable-PnpDevice
            try
            {
                string psCmd = enable
                    ? $"Get-PnpDevice | Where-Object {{ $_.InstanceId -eq '{instanceId}' -or $_.Class -eq 'Bluetooth' }} | Enable-PnpDevice -Confirm:$false"
                    : $"Get-PnpDevice | Where-Object {{ $_.InstanceId -eq '{instanceId}' }} | Disable-PnpDevice -Confirm:$false";

                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{psCmd}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using (var proc = Process.Start(psi))
                {
                    proc?.WaitForExit(7000);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PnP] PowerShell PnpDevice notice: {ex.Message}");
            }

            // Method 4: CfgMgr32 devnode state toggle & re-enumeration
            uint devInst;
            if (CfgMgr32Native.CM_Locate_DevNode(out devInst, instanceId, 0) == CfgMgr32Native.CR_SUCCESS)
            {
                if (enable)
                {
                    CfgMgr32Native.CM_Enable_DevNode(devInst, 0);
                    CfgMgr32Native.CM_Reenumerate_DevNode(devInst, CfgMgr32Native.CM_REENUMERATE_NORMAL);
                }
                else
                {
                    CfgMgr32Native.CM_Disable_DevNode(devInst, 0);
                }
            }

            // Verification
            if (enable)
            {
                System.Threading.Thread.Sleep(500);
                if (CfgMgr32Native.CM_Locate_DevNode(out devInst, instanceId, 0) == CfgMgr32Native.CR_SUCCESS)
                {
                    uint status, problem;
                    if (CfgMgr32Native.CM_Get_DevNode_Status(out status, out problem, devInst, 0) == CfgMgr32Native.CR_SUCCESS)
                    {
                        return problem != CfgMgr32Native.CM_PROB_DISABLED;
                    }
                }
            }

            return true;
        }

        public void EnsureBluetoothServiceStarted()
        {
            try
            {
                try
                {
                    using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\bthserv", true))
                    {
                        if (key != null)
                        {
                            object val = key.GetValue("Start");
                            if (val is int startVal && startVal == 4) // 4 = Disabled
                            {
                                key.SetValue("Start", 2, Microsoft.Win32.RegistryValueKind.DWord); // 2 = Auto
                            }
                        }
                    }
                }
                catch { }

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
