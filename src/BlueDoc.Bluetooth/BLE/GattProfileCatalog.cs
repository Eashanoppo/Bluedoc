using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BlueDoc.Bluetooth.BLE
{
    public class GattCharacteristicItem : INotifyPropertyChanged
    {
        private string _valueDisplay = string.Empty;
        private bool _isSubscribed;

        public string Uuid { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Properties { get; set; } = "Read";

        public string ValueDisplay
        {
            get => _valueDisplay;
            set
            {
                if (_valueDisplay != value)
                {
                    _valueDisplay = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsSubscribed
        {
            get => _isSubscribed;
            set
            {
                if (_isSubscribed != value)
                {
                    _isSubscribed = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(NotifyButtonText));
                }
            }
        }

        public string NotifyButtonText => IsSubscribed ? "Unsubscribe" : "Subscribe";

        public bool CanRead { get; set; } = true;
        public bool CanWrite { get; set; }
        public bool CanNotify { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class GattServiceItem
    {
        public string Uuid { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<GattCharacteristicItem> Characteristics { get; set; } = new List<GattCharacteristicItem>();
    }

    public static class GattProfileCatalog
    {
        private static readonly Dictionary<string, string> Services = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "1800", "Generic Access" },
            { "1801", "Generic Attribute" },
            { "1802", "Immediate Alert" },
            { "1803", "Link Loss" },
            { "1804", "Tx Power" },
            { "180A", "Device Information Service" },
            { "180D", "Heart Rate" },
            { "180F", "Battery Service" },
            { "1812", "Human Interface Device (HID)" },
            { "181A", "Environmental Sensing" }
        };

        private static readonly Dictionary<string, string> Characteristics = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "2A00", "Device Name" },
            { "2A01", "Appearance" },
            { "2A04", "Peripheral Preferred Connection Parameters" },
            { "2A05", "Service Changed" },
            { "2A19", "Battery Level" },
            { "2A24", "Model Number String" },
            { "2A25", "Serial Number String" },
            { "2A26", "Firmware Revision String" },
            { "2A27", "Hardware Revision String" },
            { "2A28", "Software Revision String" },
            { "2A29", "Manufacturer Name String" },
            { "2A37", "Heart Rate Measurement" },
            { "2A4D", "Report" },
            { "2A4E", "Protocol Mode" }
        };

        public static string LookupServiceName(string uuid)
        {
            if (string.IsNullOrWhiteSpace(uuid)) return "Unknown Service";
            string shortUuid = NormalizeUuid(uuid);
            if (Services.TryGetValue(shortUuid, out string name))
            {
                return name;
            }
            return $"Custom Service ({uuid})";
        }

        public static string LookupCharacteristicName(string uuid)
        {
            if (string.IsNullOrWhiteSpace(uuid)) return "Unknown Characteristic";
            string shortUuid = NormalizeUuid(uuid);
            if (Characteristics.TryGetValue(shortUuid, out string name))
            {
                return name;
            }
            return $"Characteristic ({uuid})";
        }

        public static List<GattServiceItem> GetStandardPeripheralProfile(string deviceName, string category)
        {
            var list = new List<GattServiceItem>();

            // Generic Access
            var gas = new GattServiceItem
            {
                Uuid = "00001800-0000-1000-8000-00805F9B34FB",
                Name = "Generic Access (0x1800)",
                Description = "Core device identification and connection parameter definitions."
            };
            gas.Characteristics.Add(new GattCharacteristicItem
            {
                Uuid = "0x2A00",
                Name = "Device Name",
                Properties = "Read",
                ValueDisplay = string.IsNullOrWhiteSpace(deviceName) ? "Bluetooth Peripheral" : deviceName,
                CanRead = true
            });
            gas.Characteristics.Add(new GattCharacteristicItem
            {
                Uuid = "0x2A01",
                Name = "Appearance",
                Properties = "Read",
                ValueDisplay = category,
                CanRead = true
            });
            list.Add(gas);

            // Generic Attribute
            var gat = new GattServiceItem
            {
                Uuid = "00001801-0000-1000-8000-00805F9B34FB",
                Name = "Generic Attribute (0x1801)",
                Description = "Service changed notifications and attribute caching."
            };
            gat.Characteristics.Add(new GattCharacteristicItem
            {
                Uuid = "0x2A05",
                Name = "Service Changed",
                Properties = "Indicate",
                ValueDisplay = "Subscribed to GATT structure updates",
                CanNotify = true
            });
            list.Add(gat);

            // Device Information Service
            var dis = new GattServiceItem
            {
                Uuid = "0000180A-0000-1000-8000-00805F9B34FB",
                Name = "Device Information Service (0x180A)",
                Description = "Manufacturer, model, and hardware descriptors."
            };
            dis.Characteristics.Add(new GattCharacteristicItem
            {
                Uuid = "0x2A29",
                Name = "Manufacturer Name String",
                Properties = "Read",
                ValueDisplay = "Identified via PnP registration",
                CanRead = true
            });
            dis.Characteristics.Add(new GattCharacteristicItem
            {
                Uuid = "0x2A26",
                Name = "Firmware Revision String",
                Properties = "Read",
                ValueDisplay = "Reported by peripheral",
                CanRead = true
            });
            list.Add(dis);

            // If mouse or keyboard, add HID
            if (category.IndexOf("Mouse", StringComparison.OrdinalIgnoreCase) >= 0 ||
                category.IndexOf("Keyboard", StringComparison.OrdinalIgnoreCase) >= 0 ||
                category.IndexOf("Peripheral", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var hid = new GattServiceItem
                {
                    Uuid = "00001812-0000-1000-8000-00805F9B34FB",
                    Name = "Human Interface Device (HID 0x1812)",
                    Description = "Low Energy HID reports, input vectors, and protocol mode."
                };
                hid.Characteristics.Add(new GattCharacteristicItem
                {
                    Uuid = "0x2A4D",
                    Name = "Report (Input / Motion)",
                    Properties = "Read, Notify",
                    ValueDisplay = "Active Input Stream",
                    CanRead = true,
                    CanNotify = true
                });
                hid.Characteristics.Add(new GattCharacteristicItem
                {
                    Uuid = "0x2A4E",
                    Name = "Protocol Mode",
                    Properties = "Read, Write Without Response",
                    ValueDisplay = "Report Protocol Mode (0x01)",
                    CanRead = true,
                    CanWrite = true
                });
                list.Add(hid);
            }

            // Battery Service
            var bat = new GattServiceItem
            {
                Uuid = "0000180F-0000-1000-8000-00805F9B34FB",
                Name = "Battery Service (0x180F)",
                Description = "Peripheral battery state."
            };
            bat.Characteristics.Add(new GattCharacteristicItem
            {
                Uuid = "0x2A19",
                Name = "Battery Level",
                Properties = "Read, Notify",
                ValueDisplay = "Exposed when connected",
                CanRead = true,
                CanNotify = true
            });
            list.Add(bat);

            return list;
        }

        private static string NormalizeUuid(string uuid)
        {
            if (uuid.Length == 4) return uuid;
            if (uuid.StartsWith("0x", StringComparison.OrdinalIgnoreCase) && uuid.Length == 6)
            {
                return uuid.Substring(2);
            }
            if (uuid.Length >= 8)
            {
                // check if 0000XXXX-...
                if (uuid.StartsWith("0000", StringComparison.OrdinalIgnoreCase))
                {
                    return uuid.Substring(4, 4);
                }
            }
            return uuid;
        }
    }
}
