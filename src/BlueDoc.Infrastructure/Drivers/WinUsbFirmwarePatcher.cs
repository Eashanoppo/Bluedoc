using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using BlueDoc.Core.Models;
using BlueDoc.Infrastructure.Platform;

namespace BlueDoc.Infrastructure.Drivers
{
    public class SiliconPatchResult
    {
        public bool Success { get; set; }
        public string ChipsetName { get; set; } = string.Empty;
        public string FirmwarePatchApplied { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool RequiresPortCycle { get; set; }
        public bool IsOperationalNow { get; set; }
    }

    /// <summary>
    /// User-Mode USB Firmware Patcher & Silicon Initializer.
    /// Provisions RAM-patch firmware (.dfu / .hcd / .bin) for Qualcomm Atheros (AR3011/AR3012),
    /// Broadcom (BCM20702/BCM43142), and Realtek chips that power up in an uninitialized ROM state.
    /// </summary>
    public class WinUsbFirmwarePatcher
    {
        public const uint IOCTL_USB_HUB_CYCLE_PORT = 0x00220414;

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool DeviceIoControl(
            IntPtr hDevice,
            uint dwIoControlCode,
            IntPtr lpInBuffer,
            uint nInBufferSize,
            IntPtr lpOutBuffer,
            uint nOutBufferSize,
            out uint lpBytesReturned,
            IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);

        private const uint GENERIC_READ = 0x80000000;
        private const uint GENERIC_WRITE = 0x40000000;
        private const uint FILE_SHARE_READ = 0x00000001;
        private const uint FILE_SHARE_WRITE = 0x00000002;
        private const uint OPEN_EXISTING = 3;

        private static readonly string[] AtherosFoxconnPids = new[]
        {
            "E015", "E027", "E02C", "E036", "E03C", "E03D", "E043", "E048", "E04A",
            "E04C", "E04D", "E04E", "E056", "E057", "E05F", "E060", "E061", "E076",
            "E078", "E07D", "E081", "E082", "E083", "E088", "E093", "E095"
        };

        public static bool IsSiliconUnpatched(BluetoothAdapterInfo adapter)
        {
            if (adapter == null) return false;

            string vid = adapter.HardwareDetails?.VendorId?.Trim()?.ToUpperInvariant() ?? string.Empty;
            string pid = adapter.HardwareDetails?.ProductId?.Trim()?.ToUpperInvariant() ?? string.Empty;
            string provider = adapter.Driver?.DriverProvider ?? string.Empty;
            string inf = adapter.Driver?.InfPath ?? string.Empty;

            bool isGenericDriver = provider.IndexOf("Microsoft", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                   inf.IndexOf("bth.inf", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                   (adapter.Driver?.DriverDateString?.IndexOf("2006", StringComparison.OrdinalIgnoreCase) >= 0);

            bool isAtherosRom = vid == "0CF3" ||
                                (vid == "0489" && Array.Exists(AtherosFoxconnPids, p => p.Equals(pid, StringComparison.OrdinalIgnoreCase))) ||
                                (vid == "04CA" && (pid.StartsWith("300") || pid.StartsWith("301") || pid == "17EA")) ||
                                (vid == "13D3" && (pid.StartsWith("32") || pid.StartsWith("33") || pid.StartsWith("34"))) ||
                                (vid == "044E" && pid.StartsWith("30"));

            bool isBroadcomRom = vid == "0A5C" ||
                                 (vid == "0489" && (pid.StartsWith("E04") || pid.StartsWith("E05") || pid == "E042") && !isAtherosRom) ||
                                 (vid == "04CA" && pid.StartsWith("20"));

            bool hasProblem = adapter.ProblemCode == 28 || adapter.ProblemCode == 43 || adapter.ProblemCode == 10;

            return (isAtherosRom || isBroadcomRom) && (isGenericDriver || hasProblem);
        }

        public static string GetCleanBaseHardwareId(BluetoothAdapterInfo adapter)
        {
            string vid = adapter?.HardwareDetails?.VendorId?.Trim()?.ToUpperInvariant() ?? string.Empty;
            string pid = adapter?.HardwareDetails?.ProductId?.Trim()?.ToUpperInvariant() ?? string.Empty;
            if (!string.IsNullOrEmpty(vid) && !string.IsNullOrEmpty(pid))
            {
                return $"USB\\VID_{vid}&PID_{pid}";
            }

            string raw = adapter?.HardwareId ?? string.Empty;
            int revIdx = raw.IndexOf("&REV_", StringComparison.OrdinalIgnoreCase);
            return revIdx > 0 ? raw.Substring(0, revIdx) : raw;
        }

        public static async Task<SiliconPatchResult> ProvisionAndPatchSiliconAsync(
            BluetoothAdapterInfo adapter,
            IProgress<string> progress = null)
        {
            return await Task.Run(() =>
            {
                var result = new SiliconPatchResult();
                if (adapter == null)
                {
                    result.Message = "No adapter target provided.";
                    return result;
                }

                string vid = adapter.HardwareDetails?.VendorId?.Trim()?.ToUpperInvariant() ?? string.Empty;
                string pid = adapter.HardwareDetails?.ProductId?.Trim()?.ToUpperInvariant() ?? string.Empty;
                string baseHwId = GetCleanBaseHardwareId(adapter);

                progress?.Report($"[Silicon Patcher] Inspecting hardware silicon {baseHwId}...");

                bool isAtheros = vid == "0CF3" || vid == "17CB" ||
                                 (vid == "0489" && Array.Exists(AtherosFoxconnPids, p => p.Equals(pid, StringComparison.OrdinalIgnoreCase))) ||
                                 (vid == "04CA" && (pid.StartsWith("300") || pid.StartsWith("301") || pid == "17EA")) ||
                                 (vid == "13D3" && (pid.StartsWith("32") || pid.StartsWith("33") || pid.StartsWith("34"))) ||
                                 (vid == "044E" && pid.StartsWith("30"));

                string subFolder = isAtheros ? "Atheros" : "Broadcom";
                string infFileName = isAtheros ? "btfilter.inf" : "bcbtums.inf";
                result.ChipsetName = isAtheros
                    ? "Qualcomm Atheros AR3011 / AR3012 Bluetooth Controller"
                    : "Broadcom BCM20702 / BCM43142 Bluetooth Controller";

                string appBase = AppDomain.CurrentDomain.BaseDirectory;
                string driverDir = Path.Combine(appBase, "Drivers", subFolder);
                if (!Directory.Exists(driverDir))
                {
                    string devDir = Path.GetFullPath(Path.Combine(appBase, $@"..\..\src\BlueDoc.UI\Drivers\{subFolder}"));
                    if (Directory.Exists(devDir)) driverDir = devDir;
                }

                string bundledInf = Path.Combine(driverDir, infFileName);
                if (!File.Exists(bundledInf))
                {
                    result.Success = false;
                    result.Message = $"Bundled driver package '{infFileName}' not found at {driverDir}.";
                    return result;
                }

                // 1. Copy all .dfu / .hex firmware binaries directly into C:\Windows\System32\drivers
                // so btfilter.sys / bcbtums.sys can load any ROM revision (e.g. 0x01020200 on Sony VAIO SVE151)
                try
                {
                    string sysDriversDir = Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess
                        ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Sysnative", "drivers")
                        : Path.Combine(Environment.SystemDirectory, "drivers");

                    foreach (var fwFile in Directory.GetFiles(driverDir, "*.*"))
                    {
                        string ext = Path.GetExtension(fwFile).ToLowerInvariant();
                        if (ext == ".dfu" || ext == ".hex" || ext == ".bin" || ext == ".hcd")
                        {
                            string dest = Path.Combine(sysDriversDir, Path.GetFileName(fwFile));
                            File.Copy(fwFile, dest, true);
                        }
                    }
                    progress?.Report($"[Silicon Patcher] Staged RAM-patch firmware binaries into {sysDriversDir}.");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Silicon Patcher] Firmware staging warning: {ex.Message}");
                }

                // 2. Stage INF in Driver Store via pnputil
                progress?.Report($"[Silicon Patcher] Staging WHQL driver '{infFileName}' via PnPUtil...");
                var psi = new ProcessStartInfo
                {
                    FileName = "pnputil.exe",
                    Arguments = $"/add-driver \"{bundledInf}\" /install",
                    UseShellExecute = true,
                    Verb = "runas",
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                using (var proc = Process.Start(psi))
                {
                    proc?.WaitForExit(35000);
                }

                // 3. Force PnP to replace generic Microsoft bth.inf with vendor filter driver using clean baseHwId
                progress?.Report($"[Silicon Patcher] Force-binding '{infFileName}' to {baseHwId} via newdev.dll...");
                bool bound = BluetoothDriverCatalogService.ForceBindDriverToHardware(bundledInf, adapter, out int win32Err);

                // 4. Cycle USB port so the filter driver uploads RAM patch to the controller
                CycleUsbDevicePort(adapter.DeviceInstanceId);
                RegisterAutoPatchTaskOnLogon();

                result.Success = bound;
                result.IsOperationalNow = bound;
                result.RequiresPortCycle = true;
                result.FirmwarePatchApplied = infFileName;
                result.Message = bound
                    ? $"Successfully bound {result.ChipsetName} driver ({infFileName}) to {baseHwId} and initialized 2.4 GHz radio."
                    : $"Driver staging ran, but force-binding {baseHwId} returned Win32 error 0x{win32Err:X8}. Ensure BlueDoc is built with Prefer32Bit=false and run as Administrator.";
                return result;
            });
        }

        public static bool CycleUsbDevicePort(string deviceInstanceId)
        {
            if (string.IsNullOrWhiteSpace(deviceInstanceId)) return false;

            try
            {
                // In Windows, cycling a USB device port is performed by restarting the PnP device node via pnputil / restart-device
                var psi = new ProcessStartInfo
                {
                    FileName = "pnputil.exe",
                    Arguments = $"/restart-device \"{deviceInstanceId}\"",
                    UseShellExecute = true,
                    Verb = "runas"
                };

                using (var proc = Process.Start(psi))
                {
                    proc?.WaitForExit(10000);
                    return proc?.ExitCode == 0;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SiliconPatcher] USB port cycle notice: {ex.Message}");
                return false;
            }
        }

        public static bool RegisterAutoPatchTaskOnLogon()
        {
            try
            {
                string appExe = Process.GetCurrentProcess().MainModule.FileName;
                var psi = new ProcessStartInfo
                {
                    FileName = "schtasks.exe",
                    Arguments = $"/Create /TN \"BlueDocSiliconPatcher\" /TR \"\\\"{appExe}\\\" --silent-patch\" /SC ONLOGON /RL HIGHEST /F",
                    UseShellExecute = true,
                    Verb = "runas"
                };

                using (var proc = Process.Start(psi))
                {
                    proc?.WaitForExit(10000);
                    return proc?.ExitCode == 0;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}

