using System;
using BlueDoc.Core.Enums;

namespace BlueDoc.Core.Models
{
    public class BluetoothDeviceInfo
    {
        public string DeviceName { get; set; } = string.Empty;
        public string BluetoothAddress { get; set; } = string.Empty;
        public ulong RawAddress { get; set; }
        public DeviceType DeviceType { get; set; } = DeviceType.Unknown;
        public bool IsPaired { get; set; }
        public bool IsConnected { get; set; }
        public int? Rssi { get; set; }
        public uint DeviceClass { get; set; }
        public string MajorDeviceClass { get; set; } = "Uncategorized";
        public string CategoryName { get; set; } = "Bluetooth Device";
        public string CategoryIcon { get; set; } = "📶";
        public string InstanceId { get; set; } = string.Empty;
        public DateTime FirstDiscovered { get; set; } = DateTime.UtcNow;
        public DateTime LastSeen { get; set; } = DateTime.UtcNow;
        public bool IsRemembered { get; set; }

        public string DisplayName => string.IsNullOrWhiteSpace(DeviceName) 
            ? (string.IsNullOrWhiteSpace(BluetoothAddress) ? "Unknown Device" : BluetoothAddress) 
            : DeviceName;

        public string StatusDisplay
        {
            get
            {
                if (IsConnected) return "Connected";
                if (IsPaired) return "Paired";
                if (IsRemembered) return "Remembered";
                return "Discovered";
            }
        }

        public string SignalStrengthDisplay
        {
            get
            {
                if (Rssi.HasValue)
                {
                    return $"{Rssi.Value} dBm";
                }
                return "Signal not exposed";
            }
        }

        public string SignalQualityBadge
        {
            get
            {
                if (!Rssi.HasValue) return "Not Exposed";
                if (Rssi.Value >= -60) return "Excellent";
                if (Rssi.Value >= -70) return "Good";
                if (Rssi.Value >= -85) return "Fair";
                return "Poor";
            }
        }

        public string SignalBadgeColor
        {
            get
            {
                if (!Rssi.HasValue) return "#64748B";
                if (Rssi.Value >= -60) return "#10B981";
                if (Rssi.Value >= -70) return "#3B82F6";
                if (Rssi.Value >= -85) return "#F59E0B";
                return "#EF4444";
            }
        }

        public string SignalBars
        {
            get
            {
                if (!Rssi.HasValue) return "----";
                if (Rssi.Value >= -60) return "▂▄▆█";
                if (Rssi.Value >= -70) return "▂▄▆_";
                if (Rssi.Value >= -85) return "▂▄__";
                return "▂___";
            }
        }

        public int SignalPercentage
        {
            get
            {
                if (!Rssi.HasValue) return 0;
                int val = (int)((Rssi.Value - (-100)) * (100.0 / (-40 - (-100))));
                return Math.Max(0, Math.Min(100, val));
            }
        }
    }
}
