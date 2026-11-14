using System;
using System.IO;
using System.Management;
using BlueDoc.Core.Models;
using BlueDoc.Infrastructure.Platform;
using Microsoft.Win32;

namespace BlueDoc.Infrastructure.HostSystem
{
    public class SystemHardwareModelInspector
    {
        public static LaptopSystemInfo InspectSystem()
        {
            var info = new LaptopSystemInfo();

            // 1. Direct registry interrogation (Ultra-fast, zero WMI overhead, 100% Win7-Win11 reliable)
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\BIOS"))
                {
                    if (key != null)
                    {
                        info.Manufacturer = (key.GetValue("SystemManufacturer") as string)?.Trim() ?? info.Manufacturer;
                        info.Model = (key.GetValue("SystemProductName") as string)?.Trim() ?? info.Model;
                        info.SystemFamily = (key.GetValue("SystemFamily") as string)?.Trim() ?? string.Empty;
                        info.BaseBoardProduct = (key.GetValue("BaseBoardProduct") as string)?.Trim() ?? string.Empty;
                        info.BiosVersion = (key.GetValue("BIOSVersion") as string)?.Trim() ?? string.Empty;
                        info.BiosReleaseDate = (key.GetValue("BIOSReleaseDate") as string)?.Trim() ?? string.Empty;
                    }
                }
            }
            catch { }

            // 2. Query OS Information
            try
            {
                var osInfo = OperatingSystemInspector.GetOperatingSystemInfo();
                info.OperatingSystemName = osInfo?.OperatingSystemName ?? (Environment.OSVersion.VersionString);
                info.Architecture = Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit";
            }
            catch { }

            // 3. WMI Enhancement (if registry had generic or missing data)
            if (string.IsNullOrWhiteSpace(info.Model) || info.Model.Equals("System Product Name", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using (var searcher = new ManagementObjectSearcher("SELECT Manufacturer, Model, SystemFamily FROM Win32_ComputerSystem"))
                    using (var col = searcher.Get())
                    {
                        foreach (ManagementObject obj in col)
                        {
                            var mfg = obj["Manufacturer"]?.ToString();
                            var mdl = obj["Model"]?.ToString();
                            var fam = obj["SystemFamily"]?.ToString();

                            if (!string.IsNullOrWhiteSpace(mfg)) info.Manufacturer = mfg.Trim();
                            if (!string.IsNullOrWhiteSpace(mdl)) info.Model = mdl.Trim();
                            if (!string.IsNullOrWhiteSpace(fam)) info.SystemFamily = fam.Trim();
                        }
                    }
                }
                catch { }
            }

            // 4. Form Factor Detection (Detect Laptop vs Desktop)
            info.IsLaptop = DetectIsLaptop(info.Model, info.SystemFamily);

            // 5. Generate OEM Support Portal URL
            info.OemSupportUrl = ResolveOemSupportUrl(info.Manufacturer, info.Model);

            return info;
        }

        private static bool DetectIsLaptop(string model, string family)
        {
            // Check battery presence (true indicator of laptop/portable hardware)
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT EstimatedChargeRemaining FROM Win32_Battery"))
                using (var col = searcher.Get())
                {
                    if (col.Count > 0) return true;
                }
            }
            catch { }

            // Keyword heuristics
            string combined = $"{model} {family}".ToLowerInvariant();
            if (combined.Contains("book") || combined.Contains("laptop") || combined.Contains("notebook") ||
                combined.Contains("thinkpad") || combined.Contains("ideapad") || combined.Contains("yoga") ||
                combined.Contains("xps") || combined.Contains("inspiron") || combined.Contains("latitude") ||
                combined.Contains("elitebook") || combined.Contains("pavilion") || combined.Contains("envy") ||
                combined.Contains("zenbook") || combined.Contains("vivobook") || combined.Contains("surface"))
            {
                return true;
            }

            return false;
        }

        private static string ResolveOemSupportUrl(string manufacturer, string model)
        {
            if (string.IsNullOrWhiteSpace(manufacturer))
                return "https://support.microsoft.com/en-us/windows/fix-bluetooth-problems-in-windows-723e05f8-0376-46a4-73ac-eeb2d8442a66";

            string mfg = manufacturer.ToUpperInvariant();
            string encodedModel = Uri.EscapeDataString(model ?? string.Empty);

            if (mfg.Contains("TECNO"))
            {
                return "https://www.tecno-mobile.com/support/";
            }
            if (mfg.Contains("LENOVO"))
            {
                return string.IsNullOrWhiteSpace(model) 
                    ? "https://pcsupport.lenovo.com/" 
                    : $"https://pcsupport.lenovo.com/us/en/products/search?query={encodedModel}";
            }
            if (mfg.Contains("DELL"))
            {
                return "https://www.dell.com/support/home/en-us?app=drivers";
            }
            if (mfg.Contains("HP") || mfg.Contains("HEWLETT"))
            {
                return "https://support.hp.com/us-en/drivers";
            }
            if (mfg.Contains("ASUS"))
            {
                return "https://www.asus.com/support/Download-Center/";
            }
            if (mfg.Contains("ACER"))
            {
                return "https://www.acer.com/ac/en/US/content/drivers";
            }
            if (mfg.Contains("MICROSOFT"))
            {
                return "https://support.microsoft.com/en-us/surface";
            }
            if (mfg.Contains("APPLE"))
            {
                return "https://support.apple.com/downloads";
            }
            if (mfg.Contains("MSI") || mfg.Contains("MICRO-STAR"))
            {
                return "https://www.msi.com/support/download";
            }

            return "https://support.microsoft.com/en-us/windows/fix-bluetooth-problems-in-windows-723e05f8-0376-46a4-73ac-eeb2d8442a66";
        }
    }
}
