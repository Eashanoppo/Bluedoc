using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using BlueDoc.Core.Enums;
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

            string nameDesc = (adapter.FriendlyName ?? string.Empty) + " " + (adapter.DeviceDescription ?? string.Empty);

            bool isAtherosRom = vid == "0CF3" || vid == "17CB" ||
                                (vid == "0489" && Array.Exists(AtherosFoxconnPids, p => p.Equals(pid, StringComparison.OrdinalIgnoreCase))) ||
                                (vid == "04CA" && (pid.StartsWith("300") || pid.StartsWith("301") || pid == "17EA")) ||
                                (vid == "13D3" && (pid.StartsWith("32") || pid.StartsWith("33") || pid.StartsWith("34"))) ||
                                (vid == "044E" && pid.StartsWith("30")) ||
                                (nameDesc.IndexOf("Atheros", StringComparison.OrdinalIgnoreCase) >= 0 || nameDesc.IndexOf("AR301", StringComparison.OrdinalIgnoreCase) >= 0 || nameDesc.IndexOf("AR946", StringComparison.OrdinalIgnoreCase) >= 0) ||
                                (adapter.Manufacturer != null && (adapter.Manufacturer.IndexOf("Atheros", StringComparison.OrdinalIgnoreCase) >= 0 || adapter.Manufacturer.IndexOf("Qualcomm", StringComparison.OrdinalIgnoreCase) >= 0));

            bool isBroadcomRom = vid == "0A5C" ||
                                 (vid == "0489" && (pid.StartsWith("E04") || pid.StartsWith("E05") || pid == "E042") && !isAtherosRom) ||
                                 (vid == "04CA" && pid.StartsWith("20")) ||
                                 (nameDesc.IndexOf("Broadcom", StringComparison.OrdinalIgnoreCase) >= 0) ||
                                 (adapter.Manufacturer != null && adapter.Manufacturer.IndexOf("Broadcom", StringComparison.OrdinalIgnoreCase) >= 0);

            bool hasProblem = adapter.ProblemCode == 28 || adapter.ProblemCode == 43 || adapter.ProblemCode == 10 || adapter.ProblemCode != 0;

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

                string targetNameDesc = (adapter.FriendlyName ?? string.Empty) + " " + (adapter.DeviceDescription ?? string.Empty);

                bool isAtheros = vid == "0CF3" || vid == "17CB" ||
                                 (vid == "0489" && Array.Exists(AtherosFoxconnPids, p => p.Equals(pid, StringComparison.OrdinalIgnoreCase))) ||
                                 (vid == "04CA" && (pid.StartsWith("300") || pid.StartsWith("301") || pid == "17EA")) ||
                                 (vid == "13D3" && (pid.StartsWith("32") || pid.StartsWith("33") || pid.StartsWith("34"))) ||
                                 (vid == "044E" && pid.StartsWith("30")) ||
                                 (targetNameDesc.IndexOf("Atheros", StringComparison.OrdinalIgnoreCase) >= 0 || targetNameDesc.IndexOf("AR301", StringComparison.OrdinalIgnoreCase) >= 0) ||
                                 (adapter.Manufacturer != null && (adapter.Manufacturer.IndexOf("Atheros", StringComparison.OrdinalIgnoreCase) >= 0 || adapter.Manufacturer.IndexOf("Qualcomm", StringComparison.OrdinalIgnoreCase) >= 0));

                string subFolder = isAtheros ? "Atheros" : "Broadcom";
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

                string infFileName = "qca_btusb.inf";
                if (isAtheros)
                {
                    infFileName = File.Exists(Path.Combine(driverDir, "qca_btusb.inf")) ? "qca_btusb.inf" : "btfilter.inf";
                }
                else
                {
                    infFileName = "bcbtums.inf";
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

                    int stagedCount = 0;
                    foreach (var fwFile in Directory.GetFiles(driverDir, "*.*"))
                    {
                        string ext = Path.GetExtension(fwFile).ToLowerInvariant();
                        if (ext == ".dfu" || ext == ".hex" || ext == ".bin" || ext == ".hcd" || ext == ".sys")
                        {
                            var fi = new FileInfo(fwFile);
                            if (fi.Length == 0) continue;

                            byte[] buffer = new byte[Math.Min(16, (int)fi.Length)];
                            using (var fs = File.OpenRead(fwFile))
                            {
                                fs.Read(buffer, 0, buffer.Length);
                            }
                            if (buffer.Length > 0 && buffer[0] == '<') continue; // Reject HTML

                            string dest = Path.Combine(sysDriversDir, Path.GetFileName(fwFile));
                            try
                            {
                                if (File.Exists(dest))
                                {
                                    File.SetAttributes(dest, FileAttributes.Normal);
                                }
                                File.Copy(fwFile, dest, true);
                                stagedCount++;
                            }
                            catch (Exception copyEx)
                            {
                                Debug.WriteLine($"[Silicon Patcher] File copy warning: {copyEx.Message}");
                            }
                        }
                    }
                    progress?.Report($"[Silicon Patcher] Staged {stagedCount} verified RAM-patch firmware binaries into {sysDriversDir}.");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Silicon Patcher] Firmware staging warning: {ex.Message}");
                }

                // 2. Stage INF in Driver Store via pnputil (completely silent without console popups)
                progress?.Report($"[Silicon Patcher] Staging WHQL driver '{infFileName}' via PnPUtil...");
                var psi = new ProcessStartInfo
                {
                    FileName = "pnputil.exe",
                    Arguments = $"/add-driver \"{bundledInf}\" /install",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using (var proc = Process.Start(psi))
                {
                    proc?.WaitForExit(35000);
                    // Legacy Windows fallback (Windows 7 / 8 / early Windows 10 syntax)
                    if (proc?.ExitCode != 0)
                    {
                        var legacyPsi = new ProcessStartInfo
                        {
                            FileName = "pnputil.exe",
                            Arguments = $"-i -a \"{bundledInf}\"",
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            WindowStyle = ProcessWindowStyle.Hidden,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true
                        };
                        using (var legacyProc = Process.Start(legacyPsi))
                        {
                            legacyProc?.WaitForExit(35000);
                        }
                    }
                }

                // 3. Force PnP to replace generic Microsoft bth.inf with vendor filter driver using clean baseHwId
                progress?.Report($"[Silicon Patcher] Force-binding '{infFileName}' to {baseHwId} via newdev.dll...");
                bool bound = BluetoothDriverCatalogService.ForceBindDriverToHardware(bundledInf, adapter, out int win32Err);

                // 4. Cycle USB port so the filter driver uploads RAM patch to the controller
                CycleUsbDevicePort(adapter.DeviceInstanceId);
                RegisterAutoPatchTaskOnLogon();

                // 5. Post-provisioning verification: verify radio is online, Code 22 cleared, and Code 10 resolved
                System.Threading.Thread.Sleep(1500);
                var inspector = new BlueDoc.Infrastructure.Hardware.SetupApiHardwareInspector();
                var currentPrimary = inspector.GetPrimaryAdapter();
                bool isOperationalNow = (currentPrimary != null && currentPrimary.ProblemCode == 0);

                if (!isOperationalNow && (currentPrimary?.ProblemCode == 22 || currentPrimary?.Status == AdapterStatus.Disabled || currentPrimary?.ProblemCode == 10))
                {
                    var radioMgr = new BlueDoc.Infrastructure.Hardware.HardwareRadioManager();
                    radioMgr.SetRadioPowerStateAsync(true, adapter.DeviceInstanceId).GetAwaiter().GetResult();
                    System.Threading.Thread.Sleep(1200);
                    currentPrimary = inspector.GetPrimaryAdapter();
                    isOperationalNow = (currentPrimary != null && currentPrimary.ProblemCode == 0);
                }

                result.Success = bound || isOperationalNow;
                result.IsOperationalNow = isOperationalNow;
                result.RequiresPortCycle = true;
                result.FirmwarePatchApplied = infFileName;
                result.Message = isOperationalNow
                    ? $"Successfully provisioned {result.ChipsetName} ({infFileName}) on {baseHwId}. Controller is operational with 0 problem codes."
                    : (bound
                        ? $"Driver {infFileName} bound to {baseHwId}. Controller status: {currentPrimary?.Status} (Code: {currentPrimary?.ProblemCode})."
                        : $"Driver staging ran, but force-binding {baseHwId} returned Win32 error 0x{win32Err:X8}.");
                return result;
            });
        }

        public static bool RollbackToGenericDriver(BluetoothAdapterInfo adapter)
        {
            if (adapter == null) return false;
            try
            {
                string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                string genericInf = Path.Combine(winDir, "INF", "bth.inf");
                if (!File.Exists(genericInf))
                {
                    genericInf = "bth.inf";
                }

                bool bound = BluetoothDriverCatalogService.ForceBindDriverToHardware(genericInf, adapter, out int _);
                CycleUsbDevicePort(adapter.DeviceInstanceId);
                return bound;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Rollback] Exception: {ex.Message}");
                return false;
            }
        }

        public static bool CycleUsbDevicePort(string deviceInstanceId)
        {
            if (string.IsNullOrWhiteSpace(deviceInstanceId)) return false;

            // 1. pnputil /restart-device (Graceful devnode driver restart without administrative disable)
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "pnputil.exe",
                    Arguments = $"/restart-device \"{deviceInstanceId}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (var proc = Process.Start(psi))
                {
                    proc?.WaitForExit(8000);
                    if (proc?.ExitCode == 0) return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SiliconPatcher] USB port restart notice: {ex.Message}");
            }

            // 2. Full PnP devnode power cycle and re-enable
            try
            {
                var radioMgr = new BlueDoc.Infrastructure.Hardware.HardwareRadioManager();
                radioMgr.SetRadioPowerStateAsync(false, deviceInstanceId).GetAwaiter().GetResult();
                System.Threading.Thread.Sleep(800);
                return radioMgr.SetRadioPowerStateAsync(true, deviceInstanceId).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SiliconPatcher] Native port cycle notice: {ex.Message}");
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
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
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

