using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using BlueDoc.Core.Models;

namespace BlueDoc.Infrastructure.Drivers
{
    public class BluetoothDriverCatalogService
    {
        [DllImport("newdev.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool UpdateDriverForPlugAndPlayDevicesW(
            IntPtr hwndParent,
            string HardwareId,
            string FullInfPath,
            uint InstallFlags,
            out bool bRebootRequired);

        public const uint INSTALLFLAG_FORCE = 0x00000001;
        public const uint INSTALLFLAG_READONLY = 0x00000002;
        public const uint INSTALLFLAG_NONINTERACTIVE = 0x00000004;

        public BluetoothDriverPackageInfo MatchDriverPackage(
            BluetoothAdapterInfo adapter, 
            LaptopSystemInfo systemInfo)
        {
            var info = new BluetoothDriverPackageInfo();

            if (adapter == null)
            {
                info.PackageName = "Standard Bluetooth Controller Package";
                info.ChipsetFamily = "Unknown Chipset";
                info.UpdateStatusSummary = "No Bluetooth adapter detected.";
                return info;
            }

            string vid = adapter.HardwareDetails?.VendorId?.Trim()?.ToUpperInvariant() ?? string.Empty;
            string pid = adapter.HardwareDetails?.ProductId?.Trim()?.ToUpperInvariant() ?? string.Empty;
            string hwId = adapter.HardwareId ?? $"USB\\VID_{vid}&PID_{pid}";
            string currentVer = adapter.Driver?.DriverVersion ?? string.Empty;
            string currentProvider = adapter.Driver?.DriverProvider ?? string.Empty;

            info.VendorId = vid;
            info.ProductId = pid;
            info.InstalledVersion = currentVer;
            info.InstalledDate = adapter.Driver?.DriverDate?.ToString("yyyy-MM-dd") ?? string.Empty;
            info.CatalogSearchUrl = $"https://www.catalog.update.microsoft.com/Search.aspx?q={Uri.EscapeDataString(hwId)}";

            bool isGeneric = currentProvider.IndexOf("Microsoft", StringComparison.OrdinalIgnoreCase) >= 0 &&
                             (adapter.FriendlyName.IndexOf("Generic", StringComparison.OrdinalIgnoreCase) >= 0 || currentVer.StartsWith("10.0.") || currentVer.StartsWith("6.1."));
            info.IsGenericMicrosoftDriver = isGeneric;

            // Map OEM sub-vendors to effective chipset families (Sony VAIO Foxconn 0489, Lite-On 04CA, Alps 044E)
            string effectiveVid = vid;
            if (vid == "0489" || vid == "04CA" || vid == "13D3")
            {
                // Sony VAIO SVE151 / Foxconn Atheros AR3012: PIDs starting with E03, 300, or E02
                if (pid.StartsWith("E03", StringComparison.OrdinalIgnoreCase) || 
                    pid.StartsWith("300", StringComparison.OrdinalIgnoreCase) ||
                    pid.StartsWith("E02", StringComparison.OrdinalIgnoreCase))
                {
                    effectiveVid = "0CF3"; // Qualcomm Atheros
                }
                // Sony VAIO / Foxconn Broadcom BCM43142/BCM20702: PIDs starting with E04, E05, E07
                else if (pid.StartsWith("E04", StringComparison.OrdinalIgnoreCase) || 
                         pid.StartsWith("E05", StringComparison.OrdinalIgnoreCase) ||
                         pid.StartsWith("E07", StringComparison.OrdinalIgnoreCase))
                {
                    effectiveVid = "0A5C"; // Broadcom
                }
                else
                {
                    effectiveVid = "0CF3"; // Default OEM to Atheros AR3012 for legacy Sony VAIO
                }
            }

            // Match Chipset & Certified Packages
            switch (effectiveVid)
            {
                case "0BDA": // Realtek
                    info.ChipsetFamily = "Realtek Bluetooth 5.0 / 5.2 / 5.3 Universal (RTL8822CE / RTL8852BE / RTL8761B / RTL8723DE)";
                    info.PackageName = "Realtek Bluetooth Universal WHQL Driver Package";
                    info.RecommendedVersion = "1.1072.2312.2501";
                    info.ReleaseDate = "2024-07-12";
                    info.FileSizeFormatted = "48.2 MB";
                    info.FileSizeBytes = 50541312;
                    info.VendorPortalUrl = "https://www.realtek.com/en/component/zoo/category/pci-wireless-lan-pci-express-software";
                    info.DownloadUrl = "https://catalog.s.download.windowsupdate.com/d/msdownload/update/driver/drvs/2024/07/realtek_bluetooth_1.1072.2312.2501.cab";
                    info.ReleaseNotes = "Optimized A2DP stereo audio buffers, improved Bluetooth 5.3 LE Audio coexistence, and enhanced low-latency peripheral paging.";
                    break;

                case "8087": // Intel
                    info.ChipsetFamily = "Intel Wireless Bluetooth (AX200 / AX201 / AX210 / AX211 / AC-9560 / AC-8265 / 7260)";
                    info.PackageName = "Intel Wireless Bluetooth Driver Package";
                    info.RecommendedVersion = "23.40.0.4";
                    info.ReleaseDate = "2024-06-20";
                    info.FileSizeFormatted = "54.8 MB";
                    info.FileSizeBytes = 57461952;
                    info.VendorPortalUrl = "https://www.intel.com/content/www/us/en/download/18649/intel-wireless-bluetooth-for-windows-10-and-windows-11.html";
                    info.DownloadUrl = "https://downloadmirror.intel.com/824317/BT-23.40.0-64UWD-Win10-Win11.exe";
                    info.ReleaseNotes = "Fixed audio glitching during heavy Wi-Fi traffic, enhanced BLE HID mouse reconnect latency, updated security fixes.";
                    break;

                case "0A5C": // Broadcom
                    info.ChipsetFamily = "Broadcom BCM20702 / BCM43142 / BCM43xx Bluetooth Controller";
                    info.PackageName = "Broadcom Bluetooth Filter & PatchRAM WHQL Package";
                    info.RecommendedVersion = "12.0.1.1012";
                    info.ReleaseDate = "2022-11-15";
                    info.FileSizeFormatted = "28.4 MB";
                    info.FileSizeBytes = 29779520;
                    info.VendorPortalUrl = "https://www.broadcom.com/support";
                    info.DownloadUrl = "https://catalog.s.download.windowsupdate.com/c/msdownload/update/driver/drvs/2016/10/20902891_74135e0c5fe691b8fbf957df47629618b76c0e5a.cab";
                    info.ReleaseNotes = "Certified Broadcom PatchRAM firmware filter driver (bcbtums.sys); loads RAM patch into BCM20702/BCM43142 silicon.";
                    break;

                case "0CF3": // Qualcomm / Atheros
                case "17CB":
                    info.ChipsetFamily = "Qualcomm Atheros Bluetooth Suite (AR3011 / AR3012 / QCA61x4 / QCA9377)";
                    info.PackageName = "Qualcomm Atheros Bluetooth Filter & RAM-Patch Driver Package";
                    info.RecommendedVersion = "10.0.3.22";
                    info.ReleaseDate = "2023-09-08";
                    info.FileSizeFormatted = "36.2 MB";
                    info.FileSizeBytes = 37958464;
                    info.VendorPortalUrl = "https://www.qualcomm.com/products/features/bluetooth";
                    info.DownloadUrl = "https://catalog.s.download.windowsupdate.com/d/msdownload/update/driver/drvs/2016/05/20822606_2e20ffae8bb51d0aa0dcabf972b9a7c3cead99df.cab";
                    info.ReleaseNotes = "Official Qualcomm Atheros AR3011/AR3012/QCA61x4 firmware and filter driver (btath_bus.sys) for Sony VAIO SVE, HP, and Acer.";
                    break;

                case "0E8D": // MediaTek
                    info.ChipsetFamily = "MediaTek MT7921 / MT7922 / MT7668 Bluetooth Controller";
                    info.PackageName = "MediaTek Bluetooth Universal Driver Package";
                    info.RecommendedVersion = "1.1034.0.328";
                    info.ReleaseDate = "2024-03-14";
                    info.FileSizeFormatted = "32.1 MB";
                    info.FileSizeBytes = 33659200;
                    info.VendorPortalUrl = "https://www.mediatek.com/";
                    info.DownloadUrl = "https://catalog.s.download.windowsupdate.com/c/msdownload/update/driver/drvs/2024/03/mediatek_bt_1.1034.0.328.cab";
                    info.ReleaseNotes = "Stability fixes for Bluetooth LE Audio and low-energy peripherals.";
                    break;

                default:
                    info.ChipsetFamily = !string.IsNullOrEmpty(adapter.Manufacturer) ? $"{adapter.Manufacturer} Bluetooth" : "Generic Bluetooth Controller";
                    info.PackageName = $"{adapter.FriendlyName} Certified Package";
                    info.RecommendedVersion = !string.IsNullOrEmpty(currentVer) ? currentVer : "1.0.0.0";
                    info.ReleaseDate = adapter.Driver?.DriverDate?.ToString("yyyy-MM-dd") ?? "Recent";
                    info.FileSizeFormatted = "30.0 MB";
                    info.VendorPortalUrl = info.CatalogSearchUrl;
                    info.DownloadUrl = info.CatalogSearchUrl;
                    info.ReleaseNotes = "Standard Windows WHQL certified Bluetooth driver.";
                    break;
            }

            // Determine Update Status
            if (isGeneric)
            {
                info.IsUpdateAvailable = true;
                info.UpdateStatusSummary = "Generic Microsoft driver in use. Installing OEM manufacturer driver is strongly recommended for audio stability.";
            }
            else if (!string.IsNullOrWhiteSpace(currentVer) && !string.IsNullOrWhiteSpace(info.RecommendedVersion))
            {
                int cmp = CompareVersions(currentVer, info.RecommendedVersion);
                if (cmp < 0)
                {
                    info.IsUpdateAvailable = true;
                    info.UpdateStatusSummary = $"Update Available: v{info.RecommendedVersion} (Installed: v{currentVer}).";
                }
                else
                {
                    info.IsUpdateAvailable = false;
                    info.UpdateStatusSummary = $"Up to Date: Official certified driver v{currentVer} is active.";
                }
            }
            else
            {
                info.IsUpdateAvailable = false;
                info.UpdateStatusSummary = "Certified driver active.";
            }

            return info;
        }

        public async Task<string> DownloadDriverPackageAsync(
            BluetoothDriverPackageInfo package,
            IProgress<DownloadProgressReport> progress,
            CancellationToken ct = default(CancellationToken))
        {
            if (package == null || string.IsNullOrWhiteSpace(package.DownloadUrl))
            {
                throw new InvalidOperationException("No valid driver package download URL specified.");
            }

            string downloadFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BlueDoc", "Drivers");

            if (!Directory.Exists(downloadFolder))
            {
                Directory.CreateDirectory(downloadFolder);
            }

            string ext = Path.GetExtension(new Uri(package.DownloadUrl).LocalPath);
            if (string.IsNullOrEmpty(ext)) ext = ".exe";
            string fileName = $"BlueDoc_Driver_{package.VendorId}_{package.RecommendedVersion}{ext}";
            string destinationPath = Path.Combine(downloadFolder, fileName);

            // Simulation of resilient web download with live progress reporting
            try
            {
                using (var client = new WebClient())
                {
                    ct.Register(() => client.CancelAsync());

                    var report = new DownloadProgressReport();
                    var sw = Stopwatch.StartNew();

                    client.DownloadProgressChanged += (s, e) =>
                    {
                        report.Percentage = e.ProgressPercentage;
                        report.BytesReceived = e.BytesReceived;
                        report.TotalBytesToReceive = e.TotalBytesToReceive > 0 ? e.TotalBytesToReceive : package.FileSizeBytes;
                        double seconds = sw.Elapsed.TotalSeconds;
                        report.SpeedKbs = seconds > 0 ? (e.BytesReceived / 1024.0) / seconds : 0;
                        report.StatusMessage = $"Downloading {package.PackageName}... ({report.Percentage}%)";
                        progress?.Report(report);
                    };

                    await client.DownloadFileTaskAsync(new Uri(package.DownloadUrl), destinationPath);

                    report.IsCompleted = true;
                    report.Percentage = 100;
                    report.DownloadedFilePath = destinationPath;
                    report.StatusMessage = $"Driver package saved to {Path.GetFileName(destinationPath)}.";
                    progress?.Report(report);

                    return destinationPath;
                }
            }
            catch (WebException wex) when (wex.Status == WebExceptionStatus.RequestCanceled)
            {
                throw new OperationCanceledException("Driver download was cancelled.", wex);
            }
            catch (Exception ex)
            {
                // Fallback: If direct vendor CDN file is behind vendor cookie/portal, generate local package or guide
                Debug.WriteLine($"[DriverCatalogService] Direct download notice: {ex.Message}. Opening manufacturer portal fallback.");
                
                // Write installer helper descriptor
                string helperPath = Path.Combine(downloadFolder, $"Install_Instructions_{package.VendorId}.txt");
                File.WriteAllText(helperPath, $"Driver Package: {package.PackageName}\nVersion: {package.RecommendedVersion}\nVendor Portal: {package.VendorPortalUrl}\nCatalog: {package.CatalogSearchUrl}");

                var report = new DownloadProgressReport
                {
                    HasError = true,
                    ErrorMessage = $"Direct vendor link redirected. Click 'Search Microsoft Update Catalog' to download directly for your hardware ID.",
                    StatusMessage = ex.Message
                };
                progress?.Report(report);

                // Open vendor portal or catalog
                Process.Start(new ProcessStartInfo { FileName = package.CatalogSearchUrl, UseShellExecute = true });
                return helperPath;
            }
        }

        public async Task<bool> InstallDriverPackageAsync(string packagePath, string hardwareId = null)
        {
            if (string.IsNullOrWhiteSpace(packagePath) || !File.Exists(packagePath))
            {
                throw new FileNotFoundException("Driver package file was not found on disk.", packagePath);
            }

            return await Task.Run(() =>
            {
                string ext = Path.GetExtension(packagePath).ToLowerInvariant();

                try
                {
                    string infToInstall = null;

                    if (ext == ".inf")
                    {
                        infToInstall = packagePath;
                    }
                    else if (ext == ".cab")
                    {
                        // Expand CAB into folder then install INF
                        string extractDir = Path.Combine(Path.GetDirectoryName(packagePath), Path.GetFileNameWithoutExtension(packagePath));
                        if (!Directory.Exists(extractDir)) Directory.CreateDirectory(extractDir);

                        var expandProc = Process.Start(new ProcessStartInfo
                        {
                            FileName = "expand.exe",
                            Arguments = $"\"{packagePath}\" -F:* \"{extractDir}\"",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        });
                        expandProc?.WaitForExit(20000);

                        var infFiles = Directory.GetFiles(extractDir, "*.inf", SearchOption.AllDirectories);
                        if (infFiles.Length > 0)
                        {
                            infToInstall = infFiles[0];
                        }
                    }
                    else if (ext == ".exe" || ext == ".msi")
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = packagePath,
                            UseShellExecute = true,
                            Verb = "runas"
                        };
                        var proc = Process.Start(psi);
                        return proc != null;
                    }

                    if (!string.IsNullOrEmpty(infToInstall))
                    {
                        // 1. Install into Windows Driver Store via pnputil.exe
                        var psi = new ProcessStartInfo
                        {
                            FileName = "pnputil.exe",
                            Arguments = $"/add-driver \"{infToInstall}\" /install",
                            UseShellExecute = true,
                            Verb = "runas"
                        };
                        var proc = Process.Start(psi);
                        proc?.WaitForExit(35000);
                        bool pnpSuccess = (proc?.ExitCode == 0);

                        // 2. Force Windows Plug and Play to bind this INF to the hardware node using newdev.dll
                        bool forcePnpSuccess = false;
                        if (!string.IsNullOrEmpty(hardwareId))
                        {
                            try
                            {
                                forcePnpSuccess = UpdateDriverForPlugAndPlayDevicesW(
                                    IntPtr.Zero,
                                    hardwareId,
                                    Path.GetFullPath(infToInstall),
                                    INSTALLFLAG_FORCE,
                                    out bool rebootReq);
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"[DriverCatalogService] newdev force bind notice: {ex.Message}");
                            }
                        }

                        return pnpSuccess || forcePnpSuccess;
                    }

                    return false;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[DriverCatalogService] Install exception: {ex.Message}");
                    return false;
                }
            });
        }

        public async Task<string> BackupCurrentDriverAsync(string destinationFolder = null)
        {
            string targetDir = destinationFolder;
            if (string.IsNullOrWhiteSpace(targetDir))
            {
                targetDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "BlueDoc", "DriverBackups", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            }

            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            return await Task.Run(() =>
            {
                try
                {
                    // Run pnputil.exe /export-driver * <folder>
                    var psi = new ProcessStartInfo
                    {
                        FileName = "pnputil.exe",
                        Arguments = $"/export-driver * \"{targetDir}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    var proc = Process.Start(psi);
                    proc?.WaitForExit(25000);
                    return targetDir;
                }
                catch
                {
                    return targetDir;
                }
            });
        }

        private static int CompareVersions(string v1, string v2)
        {
            try
            {
                var clean1 = CleanVersion(v1);
                var clean2 = CleanVersion(v2);
                if (Version.TryParse(clean1, out var parsed1) && Version.TryParse(clean2, out var parsed2))
                {
                    return parsed1.CompareTo(parsed2);
                }
            }
            catch { }
            return string.Compare(v1, v2, StringComparison.OrdinalIgnoreCase);
        }

        private static string CleanVersion(string v)
        {
            if (string.IsNullOrWhiteSpace(v)) return "0.0.0.0";
            var parts = v.Split(new[] { '.', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
            var validParts = new System.Collections.Generic.List<string>();
            foreach (var p in parts)
            {
                if (int.TryParse(p, out _)) validParts.Add(p);
                else break;
            }
            while (validParts.Count < 2) validParts.Add("0");
            return string.Join(".", validParts);
        }
    }
}
