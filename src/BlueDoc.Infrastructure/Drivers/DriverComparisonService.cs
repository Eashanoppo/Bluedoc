using System;
using System.Collections.Generic;
using BlueDoc.Core.Models;

namespace BlueDoc.Infrastructure.Drivers
{
    public class DriverComparisonService
    {
        private class VendorDriverCatalog
        {
            public string VendorName { get; set; } = string.Empty;
            public string RecommendedVersion { get; set; } = string.Empty;
            public string OfficialUrl { get; set; } = string.Empty;
            public List<string> KnownProblematicVersions { get; set; } = new List<string>();
            public Dictionary<string, string> ProblemDetails { get; set; } = new Dictionary<string, string>();
        }

        private static readonly Dictionary<string, VendorDriverCatalog> Catalog = new Dictionary<string, VendorDriverCatalog>(StringComparer.OrdinalIgnoreCase)
        {
            ["0BDA"] = new VendorDriverCatalog
            {
                VendorName = "Realtek Semiconductor Corp.",
                RecommendedVersion = "1.1072.2312.2501",
                OfficialUrl = "https://www.realtek.com/en/component/zoo/advanced-search/72?Itemid=276",
                KnownProblematicVersions = new List<string> { "1.9.1051.3000", "1.6.1015.3000" },
                ProblemDetails = new Dictionary<string, string>
                {
                    ["1.9.1051.3000"] = "Known regression: Controller firmware freezes on S3 sleep resume (Code 43).",
                    ["1.6.1015.3000"] = "Known regression: A2DP audio stutter and buffer underflow on 2.4 GHz coexistence."
                }
            },
            ["8087"] = new VendorDriverCatalog
            {
                VendorName = "Intel Corporation",
                RecommendedVersion = "23.40.0.4",
                OfficialUrl = "https://www.intel.com/content/www/us/en/download/18649/intel-wireless-bluetooth-for-windows-10-and-windows-11.html",
                KnownProblematicVersions = new List<string> { "22.190.0", "20.100.0" },
                ProblemDetails = new Dictionary<string, string>
                {
                    ["22.190.0"] = "Known regression: Dispatches frequent HCI timeouts (BTHUSB Event 3) under high traffic.",
                    ["20.100.0"] = "Known regression: Incompatible with Windows 11 modern BLE GATT service discovery."
                }
            },
            ["0A5C"] = new VendorDriverCatalog
            {
                VendorName = "Broadcom Inc.",
                RecommendedVersion = "12.0.1.940",
                OfficialUrl = "https://www.broadcom.com/support/download-search",
                KnownProblematicVersions = new List<string> { "6.5.1.2500" },
                ProblemDetails = new Dictionary<string, string>
                {
                    ["6.5.1.2500"] = "Known legacy regression: Windows 7 driver causes kernel bluescreen (BSOD) on Windows 10/11."
                }
            },
            ["0E8D"] = new VendorDriverCatalog
            {
                VendorName = "MediaTek Inc.",
                RecommendedVersion = "1.1034.0.328",
                OfficialUrl = "https://www.mediatek.com/products/broadband-wifi",
                KnownProblematicVersions = new List<string> { "1.3.14.120" },
                ProblemDetails = new Dictionary<string, string>
                {
                    ["1.3.14.120"] = "Known regression: MT7921 Bluetooth controller drops from PnP bus during active Wi-Fi 6 streaming."
                }
            },
            ["0CF3"] = new VendorDriverCatalog
            {
                VendorName = "Qualcomm Atheros",
                RecommendedVersion = "10.0.3.22",
                OfficialUrl = "https://www.qualcomm.com/support",
                KnownProblematicVersions = new List<string> { "10.0.0.285" },
                ProblemDetails = new Dictionary<string, string>
                {
                    ["10.0.0.285"] = "Known regression: Selective suspend power drop prevents radio wake."
                }
            }
        };

        public DriverComparisonResult CompareDriverVersion(BluetoothAdapterInfo adapter)
        {
            var res = new DriverComparisonResult();

            if (adapter == null)
            {
                res.Status = "No Adapter";
                res.ActionRecommendation = "No Bluetooth adapter detected.";
                return res;
            }

            var vid = adapter.HardwareDetails?.VendorId ?? string.Empty;
            res.VendorId = vid;
            res.VendorName = !string.IsNullOrEmpty(adapter.Manufacturer) ? adapter.Manufacturer : adapter.HardwareDetails?.IdentifiedManufacturer ?? "Unknown";
            res.InstalledVersion = adapter.Driver?.DriverVersion ?? "None";

            // 1. Check for Microsoft generic class driver fallback
            if (string.Equals(adapter.Driver?.DriverProvider, "Microsoft", StringComparison.OrdinalIgnoreCase))
            {
                res.Status = "Generic Class Driver";
                res.HasKnownRegression = false;
                res.ActionRecommendation = "Windows is using a basic Microsoft class driver (bth.inf). For full Bluetooth 5.x capabilities, low latency, and vendor firmware patches, install the official manufacturer driver.";
                if (Catalog.TryGetValue(vid, out var vendorInfo))
                {
                    res.RecommendedVersion = vendorInfo.RecommendedVersion;
                    res.OfficialDownloadUrl = vendorInfo.OfficialUrl;
                }
                return res;
            }

            // 2. Check vendor catalog
            if (!string.IsNullOrEmpty(vid) && Catalog.TryGetValue(vid, out var catalogEntry))
            {
                res.VendorName = catalogEntry.VendorName;
                res.RecommendedVersion = catalogEntry.RecommendedVersion;
                res.OfficialDownloadUrl = catalogEntry.OfficialUrl;

                // Check for known problematic versions
                foreach (var probVer in catalogEntry.KnownProblematicVersions)
                {
                    if (res.InstalledVersion.StartsWith(probVer, StringComparison.OrdinalIgnoreCase))
                    {
                        res.Status = "Known Problematic";
                        res.HasKnownRegression = true;
                        res.KnownRegressionDetails = catalogEntry.ProblemDetails.TryGetValue(probVer, out var det) ? det : "Known driver stability issue.";
                        res.ActionRecommendation = $"WARNING: Installed driver version '{res.InstalledVersion}' has a confirmed stability regression. Download recommended version '{catalogEntry.RecommendedVersion}' from the official portal.";
                        return res;
                    }
                }

                // Check version comparison (simple version string compare)
                if (CompareVersions(res.InstalledVersion, catalogEntry.RecommendedVersion) >= 0)
                {
                    res.Status = "Optimal";
                    res.HasKnownRegression = false;
                    res.ActionRecommendation = "Driver version is up to date and verified against the stable hardware release catalog.";
                }
                else
                {
                    res.Status = "Outdated";
                    res.HasKnownRegression = false;
                    res.ActionRecommendation = $"A newer verified stable driver release ('{catalogEntry.RecommendedVersion}') is available. Updating may resolve intermittent connection or audio drops.";
                }

                return res;
            }

            // Fallback for third party or unknown vendors
            res.Status = "Standard Driver Installed";
            res.ActionRecommendation = "Proprietary driver installed. Consult PC OEM support for manufacturer-specific updates.";
            return res;
        }

        private static int CompareVersions(string v1, string v2)
        {
            if (Version.TryParse(v1, out Version ver1) && Version.TryParse(v2, out Version ver2))
            {
                return ver1.CompareTo(ver2);
            }
            return string.Compare(v1, v2, StringComparison.OrdinalIgnoreCase);
        }
    }
}

