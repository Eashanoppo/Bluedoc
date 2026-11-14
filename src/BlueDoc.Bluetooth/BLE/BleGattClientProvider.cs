using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;
using BlueDoc.Infrastructure.Platform;

namespace BlueDoc.Bluetooth.BLE
{
    /// <summary>
    /// Native Windows 10/11 WinRT GATT Client Provider.
    /// Interacts directly with physical Bluetooth Low Energy peripherals to discover
    /// live GATT attribute tables, read values, write payloads, and listen for notifications.
    /// </summary>
    public class BleGattClientProvider : IDisposable
    {
        private BluetoothLEDevice _currentDevice;
        private readonly Dictionary<string, GattCharacteristic> _activeCharacteristics
            = new Dictionary<string, GattCharacteristic>(StringComparer.OrdinalIgnoreCase);

        public bool IsSupported => OperatingSystemInspector.IsWindows10OrGreater();

        public async Task<List<GattServiceItem>> DiscoverLiveGattProfileAsync(ulong rawAddress)
        {
            if (!IsSupported || rawAddress == 0)
            {
                return new List<GattServiceItem>();
            }

            try
            {
                Cleanup();

                _currentDevice = await BluetoothLEDevice.FromBluetoothAddressAsync(rawAddress);
                if (_currentDevice == null)
                {
                    return new List<GattServiceItem>();
                }

                // 1. Query GATT Services (Uncached first, fallback to cached)
                var servicesResult = await _currentDevice.GetGattServicesAsync(BluetoothCacheMode.Uncached);
                if (servicesResult.Status != GattCommunicationStatus.Success || servicesResult.Services == null || servicesResult.Services.Count == 0)
                {
                    servicesResult = await _currentDevice.GetGattServicesAsync(BluetoothCacheMode.Cached);
                }

                if (servicesResult.Status != GattCommunicationStatus.Success || servicesResult.Services == null)
                {
                    return new List<GattServiceItem>();
                }

                var serviceItemList = new List<GattServiceItem>();

                foreach (var service in servicesResult.Services)
                {
                    string sUuid = service.Uuid.ToString().ToUpperInvariant();
                    string sName = GattProfileCatalog.LookupServiceName(sUuid);

                    var serviceItem = new GattServiceItem
                    {
                        Uuid = sUuid,
                        Name = $"{sName} ({sUuid.Substring(0, 8)})",
                        Description = "Live GATT service discovered from connected BLE peripheral attribute table."
                    };

                    // 2. Query Characteristics for each Service
                    var charResult = await service.GetCharacteristicsAsync(BluetoothCacheMode.Uncached);
                    if (charResult.Status != GattCommunicationStatus.Success || charResult.Characteristics == null || charResult.Characteristics.Count == 0)
                    {
                        charResult = await service.GetCharacteristicsAsync(BluetoothCacheMode.Cached);
                    }

                    if (charResult.Status == GattCommunicationStatus.Success && charResult.Characteristics != null)
                    {
                        foreach (var c in charResult.Characteristics)
                        {
                            string cUuid = c.Uuid.ToString().ToUpperInvariant();
                            string cName = GattProfileCatalog.LookupCharacteristicName(cUuid);

                            var props = c.CharacteristicProperties;
                            var propList = new List<string>();
                            if (props.HasFlag(GattCharacteristicProperties.Read)) propList.Add("Read");
                            if (props.HasFlag(GattCharacteristicProperties.Write)) propList.Add("Write");
                            if (props.HasFlag(GattCharacteristicProperties.WriteWithoutResponse)) propList.Add("WriteWithoutResponse");
                            if (props.HasFlag(GattCharacteristicProperties.Notify)) propList.Add("Notify");
                            if (props.HasFlag(GattCharacteristicProperties.Indicate)) propList.Add("Indicate");

                            var charItem = new GattCharacteristicItem
                            {
                                Uuid = cUuid,
                                Name = cName,
                                Properties = string.Join(", ", propList),
                                CanRead = props.HasFlag(GattCharacteristicProperties.Read),
                                CanWrite = props.HasFlag(GattCharacteristicProperties.Write) || props.HasFlag(GattCharacteristicProperties.WriteWithoutResponse),
                                CanNotify = props.HasFlag(GattCharacteristicProperties.Notify) || props.HasFlag(GattCharacteristicProperties.Indicate),
                                ValueDisplay = props.HasFlag(GattCharacteristicProperties.Read) ? "Ready to read" : "Write/Notify only"
                            };

                            string key = $"{sUuid}_{cUuid}";
                            _activeCharacteristics[key] = c;

                            serviceItem.Characteristics.Add(charItem);
                        }
                    }

                    serviceItemList.Add(serviceItem);
                }

                return serviceItemList;
            }
            catch
            {
                return new List<GattServiceItem>();
            }
        }

        public async Task<string> ReadCharacteristicValueAsync(string serviceUuid, string characteristicUuid)
        {
            if (!IsSupported)
            {
                return "WinRT GATT API is not supported on this operating system.";
            }

            try
            {
                var characteristic = FindCharacteristic(serviceUuid, characteristicUuid);
                if (characteristic == null)
                {
                    return "Characteristic not found in active session table.";
                }

                var readResult = await characteristic.ReadValueAsync(BluetoothCacheMode.Uncached);
                if (readResult.Status != GattCommunicationStatus.Success)
                {
                    readResult = await characteristic.ReadValueAsync(BluetoothCacheMode.Cached);
                }

                if (readResult.Status == GattCommunicationStatus.Success && readResult.Value != null)
                {
                    var reader = DataReader.FromBuffer(readResult.Value);
                    byte[] data = new byte[readResult.Value.Length];
                    reader.ReadBytes(data);

                    return FormatPayload(characteristicUuid, data);
                }

                return $"Read failed ({readResult.Status})";
            }
            catch (Exception ex)
            {
                return $"Error reading characteristic: {ex.Message}";
            }
        }

        public async Task<bool> WriteCharacteristicValueAsync(string serviceUuid, string characteristicUuid, byte[] data)
        {
            if (!IsSupported || data == null)
            {
                return false;
            }

            try
            {
                var characteristic = FindCharacteristic(serviceUuid, characteristicUuid);
                if (characteristic == null)
                {
                    return false;
                }

                var buffer = CryptographicBuffer.CreateFromByteArray(data);
                var writeResult = await characteristic.WriteValueWithResultAsync(buffer);
                return writeResult.Status == GattCommunicationStatus.Success;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ToggleNotificationAsync(string serviceUuid, string characteristicUuid, Action<string> onNotificationReceived)
        {
            if (!IsSupported)
            {
                return false;
            }

            try
            {
                var characteristic = FindCharacteristic(serviceUuid, characteristicUuid);
                if (characteristic == null)
                {
                    return false;
                }

                var cccdValue = GattClientCharacteristicConfigurationDescriptorValue.Notify;
                if (characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Indicate))
                {
                    cccdValue = GattClientCharacteristicConfigurationDescriptorValue.Indicate;
                }

                var status = await characteristic.WriteClientCharacteristicConfigurationDescriptorWithResultAsync(cccdValue);
                if (status.Status == GattCommunicationStatus.Success)
                {
                    characteristic.ValueChanged += (s, args) =>
                    {
                        var reader = DataReader.FromBuffer(args.CharacteristicValue);
                        byte[] data = new byte[args.CharacteristicValue.Length];
                        reader.ReadBytes(data);
                        string display = FormatPayload(characteristicUuid, data);
                        onNotificationReceived?.Invoke(display);
                    };
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        private GattCharacteristic FindCharacteristic(string serviceUuid, string characteristicUuid)
        {
            if (!string.IsNullOrEmpty(serviceUuid) && !string.IsNullOrEmpty(characteristicUuid))
            {
                string key = $"{serviceUuid.ToUpperInvariant()}_{characteristicUuid.ToUpperInvariant()}";
                if (_activeCharacteristics.TryGetValue(key, out var exact))
                {
                    return exact;
                }
            }

            return _activeCharacteristics.Values.FirstOrDefault(ch =>
                string.Equals(ch.Uuid.ToString(), characteristicUuid, StringComparison.OrdinalIgnoreCase));
        }

        private static string FormatPayload(string uuid, byte[] data)
        {
            if (data == null || data.Length == 0) return "[Empty Payload]";

            // Standard textual descriptors
            if (uuid.IndexOf("2A00", StringComparison.OrdinalIgnoreCase) >= 0 || // Device Name
                uuid.IndexOf("2A24", StringComparison.OrdinalIgnoreCase) >= 0 || // Model Number
                uuid.IndexOf("2A25", StringComparison.OrdinalIgnoreCase) >= 0 || // Serial Number
                uuid.IndexOf("2A26", StringComparison.OrdinalIgnoreCase) >= 0 || // Firmware Revision
                uuid.IndexOf("2A27", StringComparison.OrdinalIgnoreCase) >= 0 || // Hardware Revision
                uuid.IndexOf("2A28", StringComparison.OrdinalIgnoreCase) >= 0 || // Software Revision
                uuid.IndexOf("2A29", StringComparison.OrdinalIgnoreCase) >= 0)   // Manufacturer Name
            {
                try
                {
                    return Encoding.UTF8.GetString(data).TrimEnd('\0');
                }
                catch { }
            }

            // Battery Level (1 byte: 0-100%)
            if (uuid.IndexOf("2A19", StringComparison.OrdinalIgnoreCase) >= 0 && data.Length >= 1)
            {
                return $"{data[0]}% (Live read at {DateTime.Now:HH:mm:ss})";
            }

            // General binary/hexadecimal formatting
            string hex = BitConverter.ToString(data).Replace("-", " ");
            return $"Hex: {hex} ({data.Length} bytes, {DateTime.Now:HH:mm:ss})";
        }

        public void Cleanup()
        {
            _activeCharacteristics.Clear();
            _currentDevice?.Dispose();
            _currentDevice = null;
        }

        public void Dispose()
        {
            Cleanup();
        }
    }
}

