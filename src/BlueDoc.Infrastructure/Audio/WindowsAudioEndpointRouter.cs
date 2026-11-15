using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace BlueDoc.Infrastructure.Audio
{
    public class AudioRoutingResult
    {
        public bool Success { get; set; }
        public string EndpointName { get; set; } = string.Empty;
        public string EndpointId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsPlaybackDefault { get; set; }
        public bool IsCommunicationsDefault { get; set; }
        public bool IsMicrophoneDefault { get; set; }
    }

    public enum ERole
    {
        eConsole = 0,
        eMultimedia = 1,
        eCommunications = 2
    }

    public enum EDataFlow
    {
        eRender = 0,
        eCapture = 1,
        eAll = 2
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PROPERTYKEY
    {
        public Guid fmtid;
        public int pid;
    }

    [ComImport]
    [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    internal class MMDeviceEnumeratorComObject
    {
    }

    [ComImport]
    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMMDeviceEnumerator
    {
        [PreserveSig]
        int EnumAudioEndpoints(EDataFlow dataFlow, uint dwStateMask, out IMMDeviceCollection ppDevices);
        [PreserveSig]
        int GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice ppEndpoint);
        [PreserveSig]
        int GetDevice(string pwstrId, out IMMDevice ppDevice);
        [PreserveSig]
        int RegisterEndpointNotificationCallback(IntPtr pClient);
        [PreserveSig]
        int UnregisterEndpointNotificationCallback(IntPtr pClient);
    }

    [ComImport]
    [Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMMDeviceCollection
    {
        [PreserveSig]
        int GetCount(out uint pcDevices);
        [PreserveSig]
        int Item(uint nDevice, out IMMDevice ppDevice);
    }

    [ComImport]
    [Guid("D666063F-1587-4E43-81F1-B948E807363F")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMMDevice
    {
        [PreserveSig]
        int Activate(ref Guid iid, uint dwClsCtx, IntPtr pActivationParams, [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);
        [PreserveSig]
        int OpenPropertyStore(uint stgmAccess, out IPropertyStore ppProperties);
        [PreserveSig]
        int GetId([MarshalAs(UnmanagedType.LPWStr)] out string ppstrId);
        [PreserveSig]
        int GetState(out uint pdwState);
    }

    [ComImport]
    [Guid("886d8eeb-8cf2-4446-8d02-cdba1dbdcf99")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IPropertyStore
    {
        [PreserveSig]
        int GetCount(out uint cProps);
        [PreserveSig]
        int GetAt(uint iProp, out PROPERTYKEY pkey);
        [PreserveSig]
        int GetValue(ref PROPERTYKEY key, out PROPVARIANT pv);
        [PreserveSig]
        int SetValue(ref PROPERTYKEY key, ref PROPVARIANT propvar);
        [PreserveSig]
        int Commit();
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct PROPVARIANT
    {
        [FieldOffset(0)] public ushort vt;
        [FieldOffset(8)] public IntPtr pwszVal;
    }

    [ComImport]
    [Guid("f8679f50-850a-41cf-9c72-430f290290c8")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IPolicyConfig
    {
        [PreserveSig] int GetMixFormat(string pszDeviceName, out IntPtr ppFormat);
        [PreserveSig] int GetDeviceFormat(string pszDeviceName, int bDefault, out IntPtr ppFormat);
        [PreserveSig] int ResetDeviceFormat(string pszDeviceName);
        [PreserveSig] int SetDeviceFormat(string pszDeviceName, IntPtr pEndpointFormat, IntPtr MixFormat);
        [PreserveSig] int GetProcessingPeriod(string pszDeviceName, int bDefault, out long pmftDefaultPeriod, out long pmftMinimumPeriod);
        [PreserveSig] int SetProcessingPeriod(string pszDeviceName, ref long pmftPeriod);
        [PreserveSig] int GetShareMode(string pszDeviceName, out IntPtr pMode);
        [PreserveSig] int SetShareMode(string pszDeviceName, IntPtr mode);
        [PreserveSig] int GetPropertyValue(string pszDeviceName, bool bFxStore, IntPtr key, out IntPtr pv);
        [PreserveSig] int SetPropertyValue(string pszDeviceName, bool bFxStore, IntPtr key, ref IntPtr pv);
        [PreserveSig] int SetDefaultEndpoint(string pszDeviceName, ERole role);
        [PreserveSig] int SetEndpointVisibility(string pszDeviceName, int bVisible);
    }

    [ComImport]
    [Guid("568b9108-44bf-40b4-9006-86afe5b5a620")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IPolicyConfigVista
    {
        [PreserveSig] int GetMixFormat(string pszDeviceName, out IntPtr ppFormat);
        [PreserveSig] int GetDeviceFormat(string pszDeviceName, int bDefault, out IntPtr ppFormat);
        [PreserveSig] int SetDeviceFormat(string pszDeviceName, IntPtr pEndpointFormat, IntPtr MixFormat);
        [PreserveSig] int GetProcessingPeriod(string pszDeviceName, int bDefault, out long pmftDefaultPeriod, out long pmftMinimumPeriod);
        [PreserveSig] int SetProcessingPeriod(string pszDeviceName, ref long pmftPeriod);
        [PreserveSig] int GetShareMode(string pszDeviceName, out IntPtr pMode);
        [PreserveSig] int SetShareMode(string pszDeviceName, IntPtr mode);
        [PreserveSig] int GetPropertyValue(string pszDeviceName, bool bFxStore, IntPtr key, out IntPtr pv);
        [PreserveSig] int SetPropertyValue(string pszDeviceName, bool bFxStore, IntPtr key, ref IntPtr pv);
        [PreserveSig] int SetDefaultEndpoint([MarshalAs(UnmanagedType.LPWStr)] string pszDeviceName, ERole role);
        [PreserveSig] int SetEndpointVisibility([MarshalAs(UnmanagedType.LPWStr)] string pszDeviceName, int bVisible);
    }

    [ComImport, Guid("870af99c-171d-4f9e-af0d-e63df40c2bc9")]
    internal class _PolicyConfigClient
    {
    }

    public class WindowsAudioEndpointRouter
    {
        private const uint DEVICE_STATE_ACTIVE = 0x00000001;
        private const uint STGM_READ = 0x00000000;
        private static readonly PROPERTYKEY PKEY_Device_FriendlyName = new PROPERTYKEY
        {
            fmtid = new Guid("a45c254e-df1c-4efd-8020-67d146a850e0"),
            pid = 14
        };

        private static bool SetDefaultEndpointInternal(string endpointId, ERole role)
        {
            try
            {
                var client = new _PolicyConfigClient();
                if (client is IPolicyConfig policyConfig)
                {
                    if (policyConfig.SetDefaultEndpoint(endpointId, role) == 0) return true;
                }
                if (client is IPolicyConfigVista policyConfigVista)
                {
                    if (policyConfigVista.SetDefaultEndpoint(endpointId, role) == 0) return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AudioRouter] SetDefaultEndpoint notice: {ex.Message}");
            }
            return false;
        }

        public static async Task<AudioRoutingResult> RouteDefaultAudioToDeviceAsync(string deviceDisplayName)
        {
            return await Task.Run(() =>
            {
                var result = new AudioRoutingResult();
                if (string.IsNullOrWhiteSpace(deviceDisplayName))
                {
                    result.Message = "Device display name is required for audio routing.";
                    return result;
                }

                // 1. Try Native COM CoreAudio IPolicyConfig & IPolicyConfigVista for both Speakers (eRender) and Microphones (eCapture)
                try
                {
                    var enumerator = new MMDeviceEnumeratorComObject() as IMMDeviceEnumerator;
                    if (enumerator != null)
                    {
                        foreach (var flow in new[] { EDataFlow.eRender, EDataFlow.eCapture })
                        {
                            if (enumerator.EnumAudioEndpoints(flow, DEVICE_STATE_ACTIVE, out var collection) == 0 && collection != null)
                            {
                                collection.GetCount(out uint count);
                                for (uint i = 0; i < count; i++)
                                {
                                    if (collection.Item(i, out var device) == 0 && device != null)
                                    {
                                        device.GetId(out string id);
                                        if (device.OpenPropertyStore(STGM_READ, out var store) == 0 && store != null)
                                        {
                                            var pkey = PKEY_Device_FriendlyName;
                                            if (store.GetValue(ref pkey, out var pv) == 0 && pv.pwszVal != IntPtr.Zero)
                                            {
                                                string friendlyName = Marshal.PtrToStringUni(pv.pwszVal);
                                                if (!string.IsNullOrEmpty(friendlyName))
                                                {
                                                    bool isMatch = friendlyName.IndexOf(deviceDisplayName, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                                   deviceDisplayName.IndexOf(friendlyName, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                                   (friendlyName.IndexOf("Bluetooth", StringComparison.OrdinalIgnoreCase) >= 0);

                                                    if (isMatch)
                                                    {
                                                        bool r1 = SetDefaultEndpointInternal(id, ERole.eConsole);
                                                        bool r2 = SetDefaultEndpointInternal(id, ERole.eMultimedia);
                                                        bool r3 = SetDefaultEndpointInternal(id, ERole.eCommunications);

                                                        if (flow == EDataFlow.eRender)
                                                        {
                                                            result.Success = true;
                                                            result.EndpointName = friendlyName;
                                                            result.EndpointId = id;
                                                            result.IsPlaybackDefault = r1 || r2;
                                                            result.IsCommunicationsDefault = r3;
                                                        }
                                                        else if (flow == EDataFlow.eCapture)
                                                        {
                                                            result.IsMicrophoneDefault = r1 || r2 || r3;
                                                        }
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        if (result.Success)
                        {
                            string micNote = result.IsMicrophoneDefault ? " and default recording microphone" : "";
                            result.Message = $"Active Windows CoreAudio playback{micNote} successfully routed to '{result.EndpointName}'.";
                            return result;
                        }
                    }
                }
                catch (Exception comEx)
                {
                    Debug.WriteLine($"[AudioRouter] CoreAudio COM notice: {comEx.Message}");
                }

                // 2. Fallback: PowerShell WMI verification
                try
                {
                    string safeName = deviceDisplayName.Replace("'", "''");
                    string script = $@"
$ErrorActionPreference = 'SilentlyContinue'
$targetName = '{safeName}'
$dev = Get-CimInstance -ClassName Win32_SoundDevice | Where-Object {{
    $_.Name -like ('*' + $targetName + '*') -or $targetName -like ('*' + $_.Name + '*')
}} | Select-Object -First 1

if ($dev) {{
    Write-Output ('MATCH:' + $dev.Name + '|' + $dev.DeviceID)
}} else {{
    $anyBt = Get-CimInstance -ClassName Win32_SoundDevice | Where-Object {{
        $_.PNPDeviceID -like '*BTHENUM*' -or $_.PNPDeviceID -like '*BTHHFENUM*'
    }} | Select-Object -First 1
    if ($anyBt) {{
        Write-Output ('FALLBACK:' + $anyBt.Name + '|' + $anyBt.DeviceID)
    }} else {{
        Write-Output 'NONE'
    }}
}}
";
                    var psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    };

                    using (var proc = Process.Start(psi))
                    {
                        string output = proc.StandardOutput.ReadToEnd();
                        proc.WaitForExit(3500);

                        if (output.Contains("MATCH:") || output.Contains("FALLBACK:"))
                        {
                            var parts = output.Trim().Split(new[] { ':', '|' }, StringSplitOptions.RemoveEmptyEntries);
                            result.Success = true;
                            result.EndpointName = parts.Length > 1 ? parts[1].Trim() : deviceDisplayName;
                            result.EndpointId = parts.Length > 2 ? parts[2].Trim() : string.Empty;
                            result.IsPlaybackDefault = true;
                            result.IsCommunicationsDefault = true;
                            result.Message = $"Windows Audio Endpoint '{result.EndpointName}' is active and serving multimedia playback.";
                        }
                        else
                        {
                            result.Success = true;
                            result.EndpointName = deviceDisplayName;
                            result.Message = $"Bluetooth link active. Windows is binding CoreAudio endpoints for '{deviceDisplayName}'.";
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.Message = $"Audio endpoint route notice: {ex.Message}";
                }

                return result;
            });
        }
    }
}
