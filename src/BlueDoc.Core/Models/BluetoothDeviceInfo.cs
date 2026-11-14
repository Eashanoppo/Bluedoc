using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using BlueDoc.Core.Enums;

namespace BlueDoc.Core.Models
{
    public class BluetoothDeviceInfo : INotifyPropertyChanged
    {
        private string _deviceName = string.Empty;
        private string _bluetoothAddress = string.Empty;
        private ulong _rawAddress;
        private DeviceType _deviceType = DeviceType.Unknown;
        private bool _isPaired;
        private bool _isConnected;
        private int? _rssi;
        private uint _deviceClass;
        private string _majorDeviceClass = "Uncategorized";
        private string _categoryName = "Bluetooth Device";
        private string _categoryIcon = "📶";
        private string _instanceId = string.Empty;
        private DateTime _firstDiscovered = DateTime.UtcNow;
        private DateTime _lastSeen = DateTime.UtcNow;
        private bool _isRemembered;
        private string _discoverySource = "Inquiry";
        private string _advertisedServices = string.Empty;
        private int? _batteryLevel;
        private bool _isAvailable;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string DeviceName
        {
            get => _deviceName;
            set
            {
                if (_deviceName != value)
                {
                    _deviceName = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DisplayName));
                }
            }
        }

        public string BluetoothAddress
        {
            get => _bluetoothAddress;
            set
            {
                if (_bluetoothAddress != value)
                {
                    _bluetoothAddress = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DisplayName));
                }
            }
        }

        public ulong RawAddress
        {
            get => _rawAddress;
            set
            {
                if (_rawAddress != value)
                {
                    _rawAddress = value;
                    OnPropertyChanged();
                }
            }
        }

        public DeviceType DeviceType
        {
            get => _deviceType;
            set
            {
                if (_deviceType != value)
                {
                    _deviceType = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DeviceTypeDisplay));
                }
            }
        }

        public string DeviceTypeDisplay
        {
            get
            {
                switch (DeviceType)
                {
                    case DeviceType.Classic: return "Classic (BR/EDR)";
                    case DeviceType.LowEnergy: return "Bluetooth LE (BLE)";
                    case DeviceType.DualMode: return "Dual Mode (BR/EDR + BLE)";
                    default: return "Bluetooth";
                }
            }
        }

        public bool IsPaired
        {
            get => _isPaired;
            set
            {
                if (_isPaired != value)
                {
                    _isPaired = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(StatusDisplay));
                }
            }
        }

        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                if (_isConnected != value)
                {
                    _isConnected = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(StatusDisplay));
                    OnPropertyChanged(nameof(StatusColor));
                }
            }
        }

        public bool IsAvailable
        {
            get => _isAvailable || _isConnected;
            set
            {
                if (_isAvailable != value)
                {
                    _isAvailable = value;
                    OnPropertyChanged();
                }
            }
        }

        public int? Rssi
        {
            get => _rssi;
            set
            {
                if (_rssi != value)
                {
                    _rssi = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(SignalStrengthDisplay));
                    OnPropertyChanged(nameof(SignalQualityBadge));
                    OnPropertyChanged(nameof(SignalBadgeColor));
                    OnPropertyChanged(nameof(SignalBars));
                    OnPropertyChanged(nameof(SignalPercentage));
                }
            }
        }

        public uint DeviceClass
        {
            get => _deviceClass;
            set
            {
                if (_deviceClass != value)
                {
                    _deviceClass = value;
                    OnPropertyChanged();
                }
            }
        }

        public string MajorDeviceClass
        {
            get => _majorDeviceClass;
            set
            {
                if (_majorDeviceClass != value)
                {
                    _majorDeviceClass = value;
                    OnPropertyChanged();
                }
            }
        }

        public string CategoryName
        {
            get => _categoryName;
            set
            {
                if (_categoryName != value)
                {
                    _categoryName = value;
                    OnPropertyChanged();
                }
            }
        }

        public string CategoryIcon
        {
            get => _categoryIcon;
            set
            {
                if (_categoryIcon != value)
                {
                    _categoryIcon = value;
                    OnPropertyChanged();
                }
            }
        }

        public string InstanceId
        {
            get => _instanceId;
            set
            {
                if (_instanceId != value)
                {
                    _instanceId = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime FirstDiscovered
        {
            get => _firstDiscovered;
            set
            {
                if (_firstDiscovered != value)
                {
                    _firstDiscovered = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime LastSeen
        {
            get => _lastSeen;
            set
            {
                if (_lastSeen != value)
                {
                    _lastSeen = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(LastSeenDisplay));
                }
            }
        }

        public string LastSeenDisplay
        {
            get
            {
                var diff = DateTime.UtcNow - LastSeen;
                if (diff.TotalSeconds < 5) return "Just now";
                if (diff.TotalSeconds < 60) return $"{Math.Max(1, (int)diff.TotalSeconds)}s ago";
                if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
                return LastSeen.ToLocalTime().ToString("g");
            }
        }

        public bool IsRemembered
        {
            get => _isRemembered;
            set
            {
                if (_isRemembered != value)
                {
                    _isRemembered = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(StatusDisplay));
                }
            }
        }

        public string DiscoverySource
        {
            get => _discoverySource;
            set
            {
                if (_discoverySource != value)
                {
                    _discoverySource = value;
                    OnPropertyChanged();
                }
            }
        }

        public string AdvertisedServices
        {
            get => _advertisedServices;
            set
            {
                if (_advertisedServices != value)
                {
                    _advertisedServices = value;
                    OnPropertyChanged();
                }
            }
        }

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

        public int? BatteryLevel
        {
            get => _batteryLevel;
            set
            {
                if (_batteryLevel != value)
                {
                    _batteryLevel = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(StatusSubtitleDisplay));
                    OnPropertyChanged(nameof(BatteryDisplay));
                }
            }
        }

        public string BatteryDisplay => BatteryLevel.HasValue ? $"{BatteryLevel.Value}%" : string.Empty;

        public string StatusSubtitleDisplay
        {
            get
            {
                if (IsConnected)
                {
                    if (BatteryLevel.HasValue)
                    {
                        return $"Connected {BatteryLevel.Value}% 🔋";
                    }
                    return "Connected";
                }
                return "Not Connected";
            }
        }

        public string StatusColor => IsConnected ? "#1B8C3D" : "#86868B";

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

        // Category Classification Helpers based on verified evidence
        public bool IsSpeaker => 
            CategoryName.IndexOf("Speaker", StringComparison.OrdinalIgnoreCase) >= 0 ||
            CategoryName.IndexOf("Loudspeaker", StringComparison.OrdinalIgnoreCase) >= 0 ||
            DeviceName.IndexOf("Speaker", StringComparison.OrdinalIgnoreCase) >= 0 ||
            DeviceName.IndexOf("Soundbar", StringComparison.OrdinalIgnoreCase) >= 0 ||
            DeviceName.IndexOf("Boombox", StringComparison.OrdinalIgnoreCase) >= 0;

        public bool IsHeadphone =>
            (!IsSpeaker) && (
                CategoryName.IndexOf("Headphone", StringComparison.OrdinalIgnoreCase) >= 0 ||
                CategoryName.IndexOf("Headset", StringComparison.OrdinalIgnoreCase) >= 0 ||
                CategoryName.IndexOf("Earphone", StringComparison.OrdinalIgnoreCase) >= 0 ||
                DeviceName.IndexOf("Headset", StringComparison.OrdinalIgnoreCase) >= 0 ||
                DeviceName.IndexOf("Headphone", StringComparison.OrdinalIgnoreCase) >= 0 ||
                DeviceName.IndexOf("Buds", StringComparison.OrdinalIgnoreCase) >= 0 ||
                DeviceName.IndexOf("AirPods", StringComparison.OrdinalIgnoreCase) >= 0 ||
                DeviceName.IndexOf("Earbuds", StringComparison.OrdinalIgnoreCase) >= 0 ||
                DeviceName.IndexOf("Necklace", StringComparison.OrdinalIgnoreCase) >= 0 ||
                DeviceName.IndexOf("FreeBuds", StringComparison.OrdinalIgnoreCase) >= 0
            );

        public bool IsAudio => IsSpeaker || IsHeadphone || MajorDeviceClass == "Audio/Video";

        public bool IsKeyboard =>
            CategoryName.IndexOf("Keyboard", StringComparison.OrdinalIgnoreCase) >= 0 ||
            DeviceName.IndexOf("Keyboard", StringComparison.OrdinalIgnoreCase) >= 0 ||
            DeviceName.IndexOf("Keypad", StringComparison.OrdinalIgnoreCase) >= 0;

        public bool IsMouse =>
            CategoryName.IndexOf("Mouse", StringComparison.OrdinalIgnoreCase) >= 0 ||
            DeviceName.IndexOf("Mouse", StringComparison.OrdinalIgnoreCase) >= 0 ||
            DeviceName.IndexOf("Trackpad", StringComparison.OrdinalIgnoreCase) >= 0;

        public bool IsPeripheral => MajorDeviceClass == "Peripheral" || IsMouse || IsKeyboard;

        public bool IsPhone =>
            !IsHeadphone && !IsSpeaker && (
                MajorDeviceClass == "Phone" ||
                DeviceName.IndexOf("iPhone", StringComparison.OrdinalIgnoreCase) >= 0 ||
                DeviceName.IndexOf("Galaxy", StringComparison.OrdinalIgnoreCase) >= 0 ||
                DeviceName.IndexOf("Pixel", StringComparison.OrdinalIgnoreCase) >= 0 ||
                (DeviceName.IndexOf("Phone", StringComparison.OrdinalIgnoreCase) >= 0 &&
                 DeviceName.IndexOf("Headphone", StringComparison.OrdinalIgnoreCase) < 0 &&
                 DeviceName.IndexOf("Earphone", StringComparison.OrdinalIgnoreCase) < 0)
            );

        public bool IsSensor =>
            MajorDeviceClass == "Health" ||
            CategoryName.IndexOf("Sensor", StringComparison.OrdinalIgnoreCase) >= 0 ||
            CategoryName.IndexOf("Heart Rate", StringComparison.OrdinalIgnoreCase) >= 0 ||
            CategoryName.IndexOf("Environmental", StringComparison.OrdinalIgnoreCase) >= 0 ||
            DeviceName.IndexOf("Sensor", StringComparison.OrdinalIgnoreCase) >= 0 ||
            DeviceName.IndexOf("Thermometer", StringComparison.OrdinalIgnoreCase) >= 0;

        public bool IsUnknown =>
            !IsAudio && !IsPeripheral && !IsPhone && !IsSensor &&
            (MajorDeviceClass == "Uncategorized" || CategoryName == "Bluetooth Device" || string.IsNullOrWhiteSpace(DeviceName) || DeviceName == BluetoothAddress);
    }
}
