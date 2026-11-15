using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using BlueDoc.Core.Models;

namespace BlueDoc.Infrastructure.Hardware
{
    public static class HardwareIdParser
    {
        private static readonly Dictionary<string, string> KnownUsbVendors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "0BDA", "Realtek Semiconductor Corp." },
            { "8087", "Intel Corporation" },
            { "0A5C", "Broadcom Corp." },
            { "0CF3", "Qualcomm Atheros Communications" },
            { "0E8D", "MediaTek Inc." },
            { "0A12", "Cambridge Silicon Radio (CSR)" },
            { "045E", "Microsoft Corporation" },
            { "0489", "Foxconn / Hon Hai Precision" },
            { "13D3", "AzureWave Technologies" },
            { "04CA", "Lite-On Technology Corp." },
            { "0B05", "ASUSTeK Computer Inc." },
            { "044E", "Alps Electric Co., Ltd." },
            { "05AC", "Apple Inc." },
            { "0930", "Toshiba Corporation" },
            { "413C", "Dell Inc." },
            { "10AB", "USRobotics" },
            { "047D", "Kensington" },
            { "050D", "Belkin International" },
            { "07D1", "D-Link System" },
            { "2001", "D-Link Corporation" },
            { "0586", "ZyXEL Communications Corp." },
            { "1286", "Marvell Semiconductor, Inc." },
            { "04B4", "Cypress Semiconductor" }
        };

        private static readonly Dictionary<string, string> KnownPciVendors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "8086", "Intel Corporation" },
            { "10EC", "Realtek Semiconductor Corp." },
            { "14E4", "Broadcom Inc." },
            { "168C", "Qualcomm Atheros" },
            { "17CB", "Qualcomm" },
            { "11AB", "Marvell Technology Group" },
            { "1022", "Advanced Micro Devices (AMD)" },
            { "14C3", "MediaTek Inc." }
        };

        public static HardwareIdDetails Parse(string hardwareId)
        {
            var result = new HardwareIdDetails();
            if (string.IsNullOrWhiteSpace(hardwareId))
            {
                return result;
            }

            result.RawHardwareId = hardwareId.Trim();

            // Check Bus Type
            if (result.RawHardwareId.StartsWith("USB\\", StringComparison.OrdinalIgnoreCase))
            {
                result.BusType = "USB";
                ParseUsbId(result);
            }
            else if (result.RawHardwareId.StartsWith("PCI\\", StringComparison.OrdinalIgnoreCase))
            {
                result.BusType = "PCI";
                ParsePciId(result);
            }
            else if (result.RawHardwareId.StartsWith("BTH\\", StringComparison.OrdinalIgnoreCase))
            {
                result.BusType = "Bluetooth Enumerator";
            }
            else
            {
                result.BusType = "Other/PnP";
                // Attempt generic VID/PID parse
                ParseUsbId(result);
            }

            return result;
        }

        private static void ParseUsbId(HardwareIdDetails details)
        {
            // Regex for VID_[0-9A-Fa-f]{4}&PID_[0-9A-Fa-f]{4}
            var vidMatch = Regex.Match(details.RawHardwareId, @"VID_([0-9A-Fa-f]{4})", RegexOptions.IgnoreCase);
            if (vidMatch.Success)
            {
                details.VendorId = vidMatch.Groups[1].Value.ToUpperInvariant();
                if (KnownUsbVendors.TryGetValue(details.VendorId, out var vendorName))
                {
                    details.IdentifiedManufacturer = vendorName;
                }
            }

            var pidMatch = Regex.Match(details.RawHardwareId, @"PID_([0-9A-Fa-f]{4})", RegexOptions.IgnoreCase);
            if (pidMatch.Success)
            {
                details.ProductId = pidMatch.Groups[1].Value.ToUpperInvariant();
            }

            var revMatch = Regex.Match(details.RawHardwareId, @"REV_([0-9A-Fa-f]{4})", RegexOptions.IgnoreCase);
            if (revMatch.Success)
            {
                details.Revision = revMatch.Groups[1].Value.ToUpperInvariant();
            }

            IdentifyChipsetHeuristic(details);
        }

        private static void ParsePciId(HardwareIdDetails details)
        {
            var venMatch = Regex.Match(details.RawHardwareId, @"VEN_([0-9A-Fa-f]{4})", RegexOptions.IgnoreCase);
            if (venMatch.Success)
            {
                details.VendorId = venMatch.Groups[1].Value.ToUpperInvariant();
                if (KnownPciVendors.TryGetValue(details.VendorId, out var vendorName))
                {
                    details.IdentifiedManufacturer = vendorName;
                }
            }

            var devMatch = Regex.Match(details.RawHardwareId, @"DEV_([0-9A-Fa-f]{4})", RegexOptions.IgnoreCase);
            if (devMatch.Success)
            {
                details.ProductId = devMatch.Groups[1].Value.ToUpperInvariant();
            }

            var subSysMatch = Regex.Match(details.RawHardwareId, @"SUBSYS_([0-9A-Fa-f]{8})", RegexOptions.IgnoreCase);
            if (subSysMatch.Success)
            {
                details.SubsystemId = subSysMatch.Groups[1].Value.ToUpperInvariant();
            }

            IdentifyChipsetHeuristic(details);
        }

        private static void IdentifyChipsetHeuristic(HardwareIdDetails details)
        {
            if (string.IsNullOrEmpty(details.VendorId)) return;

            // Realtek heuristics
            if (details.VendorId == "0BDA" || details.VendorId == "10EC")
            {
                if (details.ProductId.StartsWith("C8", StringComparison.OrdinalIgnoreCase) ||
                    details.ProductId.StartsWith("B8", StringComparison.OrdinalIgnoreCase) ||
                    details.ProductId == "8723" || details.ProductId == "8821" || details.ProductId == "8822")
                {
                    details.IdentifiedChipsetFamily = "Realtek RTL8723 / RTL8821 / RTL8822 Series";
                }
                else
                {
                    details.IdentifiedChipsetFamily = "Realtek Bluetooth Controller";
                }
            }
            // Intel heuristics
            else if (details.VendorId == "8087" || details.VendorId == "8086")
            {
                if (details.ProductId == "0026" || details.ProductId == "0029" || details.ProductId == "0032" || details.ProductId == "0033")
                {
                    details.IdentifiedChipsetFamily = "Intel Wireless AX200 / AX201 / AX210 / BE200 Series";
                }
                else if (details.ProductId == "07DC" || details.ProductId == "0AA7" || details.ProductId == "0AAA")
                {
                    details.IdentifiedChipsetFamily = "Intel Wireless 7260 / 8260 / 9260 Series";
                }
                else
                {
                    details.IdentifiedChipsetFamily = "Intel Integrated Bluetooth Adapter";
                }
            }
            // Broadcom heuristics
            else if (details.VendorId == "0A5C" || details.VendorId == "14E4")
            {
                details.IdentifiedChipsetFamily = "Broadcom BCM2070 / BCM43xx Series";
            }
            // Qualcomm / Atheros heuristics
            else if (details.VendorId == "0CF3" || details.VendorId == "168C")
            {
                details.IdentifiedChipsetFamily = "Qualcomm Atheros AR3011 / AR3012 / QCA61x4 Series";
            }
            // OEM Sub-vendors (Foxconn 0489, Lite-On 04CA, AzureWave 13D3, Alps 044E)
            else if (details.VendorId == "0489" || details.VendorId == "04CA" || details.VendorId == "13D3" || details.VendorId == "044E")
            {
                if (details.ProductId.StartsWith("E03", StringComparison.OrdinalIgnoreCase) ||
                    details.ProductId.StartsWith("300", StringComparison.OrdinalIgnoreCase) ||
                    details.ProductId.StartsWith("E02", StringComparison.OrdinalIgnoreCase) ||
                    details.ProductId == "E036" || details.ProductId == "E03D")
                {
                    details.IdentifiedChipsetFamily = "Qualcomm Atheros AR3011 / AR3012 Series (OEM)";
                }
                else if (details.ProductId.StartsWith("E04", StringComparison.OrdinalIgnoreCase) ||
                         details.ProductId.StartsWith("E05", StringComparison.OrdinalIgnoreCase) ||
                         details.ProductId.StartsWith("E07", StringComparison.OrdinalIgnoreCase) ||
                         details.ProductId == "E042")
                {
                    details.IdentifiedChipsetFamily = "Broadcom BCM20702 / BCM43142 Series (OEM)";
                }
                else
                {
                    details.IdentifiedChipsetFamily = "OEM Integrated Bluetooth Controller";
                }
            }
            // CSR heuristics
            else if (details.VendorId == "0A12")
            {
                details.IdentifiedChipsetFamily = "Cambridge Silicon Radio CSR8510 / BlueCore Series";
            }
            // MediaTek heuristics
            else if (details.VendorId == "0E8D" || details.VendorId == "14C3")
            {
                details.IdentifiedChipsetFamily = "MediaTek MT7921 / MT7922 / MT76xx Series";
            }
        }
    }
}
