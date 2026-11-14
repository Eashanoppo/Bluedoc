using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using BlueDoc.Core.Enums;
using BlueDoc.Core.Interfaces;
using BlueDoc.Core.Models;
using BlueDoc.Infrastructure.Native;

namespace BlueDoc.Infrastructure.Hardware
{
    public class SetupApiHardwareInspector : IHardwareInspector
    {
        public IReadOnlyList<BluetoothAdapterInfo> EnumerateAdapters()
        {
            var adapters = new List<BluetoothAdapterInfo>();

            try
            {
                var setupApiAdapters = EnumerateViaSetupApi();
                adapters.AddRange(setupApiAdapters);
            }
            catch
            {
                // Fallback to WMI if SetupAPI encounters unexpected exceptions
            }

            if (adapters.Count == 0)
            {
                try
                {
                    var wmiAdapters = EnumerateViaWmi();
                    adapters.AddRange(wmiAdapters);
                }
                catch
                {
                    // Ignore WMI errors
                }
            }

            // Enrich with Win32 Radio information if possible
            EnrichWithRadioInfo(adapters);

            return adapters;
        }

        private List<BluetoothAdapterInfo> EnumerateViaSetupApi()
        {
            var result = new List<BluetoothAdapterInfo>();
            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 1. Enumerate standard Bluetooth class (both non-present and present)
            var classGuid = SetupApiNative.GUID_DEVCLASS_BLUETOOTH;
            EnumerateClass(ref classGuid, 0, result, seenIds);
            EnumerateClass(ref classGuid, SetupApiNative.DIGCF_PRESENT, result, seenIds);

            // 2. Also scan ALLCLASSES for unconfigured, missing-driver (Code 28/43), or generic USB Class E0 devices
            var emptyGuid = Guid.Empty;
            EnumerateClass(ref emptyGuid, SetupApiNative.DIGCF_ALLCLASSES | SetupApiNative.DIGCF_PRESENT, result, seenIds);

            return result;
        }

        private void EnumerateClass(ref Guid classGuid, int flags, List<BluetoothAdapterInfo> result, HashSet<string> seenIds)
        {
            IntPtr devInfo = SetupApiNative.SetupDiGetClassDevs(
                ref classGuid,
                null,
                IntPtr.Zero,
                flags);

            if (devInfo == (IntPtr)(-1) || devInfo == IntPtr.Zero)
            {
                return;
            }

            try
            {
                var devInfoData = new SetupApiNative.SP_DEVINFO_DATA();
                devInfoData.cbSize = Marshal.SizeOf(devInfoData);

                int memberIndex = 0;
                while (SetupApiNative.SetupDiEnumDeviceInfo(devInfo, memberIndex, ref devInfoData))
                {
                    memberIndex++;

                    var adapter = ProcessDeviceInfo(devInfo, ref devInfoData);
                    if (adapter != null && !string.IsNullOrEmpty(adapter.DeviceInstanceId))
                    {
                        if (!seenIds.Contains(adapter.DeviceInstanceId) && IsBluetoothAdapterOrDevice(adapter))
                        {
                            seenIds.Add(adapter.DeviceInstanceId);
                            result.Add(adapter);
                        }
                    }
                }
            }
            finally
            {
                SetupApiNative.SetupDiDestroyDeviceInfoList(devInfo);
            }
        }

        private bool IsBluetoothAdapterOrDevice(BluetoothAdapterInfo adapter)
        {
            if (string.IsNullOrEmpty(adapter.DeviceInstanceId) && string.IsNullOrEmpty(adapter.FriendlyName))
            {
                return false;
            }

            string id = adapter.DeviceInstanceId ?? "";
            string desc = adapter.DeviceDescription ?? "";
            string name = adapter.FriendlyName ?? "";
            string hwId = adapter.HardwareId ?? "";
            string vid = adapter.HardwareDetails?.VendorId ?? "";

            // Discard peripheral endpoint enumerator child nodes (e.g. BTHENUM audio endpoints, BTHLE GATT devices)
            if (id.StartsWith("BTHENUM\\", StringComparison.OrdinalIgnoreCase) ||
                id.StartsWith("BTHLE\\", StringComparison.OrdinalIgnoreCase) ||
                hwId.StartsWith("BTHENUM\\", StringComparison.OrdinalIgnoreCase) ||
                hwId.StartsWith("BTHLE\\", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Discard virtual software protocol nodes and Microsoft enumerators (RFCOMM, BTHBRB, BTHLE enumerators)
            if (id.StartsWith("BTH\\MS_RFCOMM", StringComparison.OrdinalIgnoreCase) ||
                id.StartsWith("BTH\\MS_BTHBRB", StringComparison.OrdinalIgnoreCase) ||
                id.StartsWith("BTH\\MS_BTHLE", StringComparison.OrdinalIgnoreCase) ||
                hwId.StartsWith("BTH\\MS_RFCOMM", StringComparison.OrdinalIgnoreCase) ||
                hwId.StartsWith("BTH\\MS_BTHBRB", StringComparison.OrdinalIgnoreCase) ||
                hwId.StartsWith("BTH\\MS_BTHLE", StringComparison.OrdinalIgnoreCase) ||
                name.IndexOf("RFCOMM", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Enumerator", StringComparison.OrdinalIgnoreCase) >= 0 ||
                desc.IndexOf("RFCOMM", StringComparison.OrdinalIgnoreCase) >= 0 ||
                desc.IndexOf("Enumerator", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return false;
            }

            // Check known Bluetooth vendor IDs (Atheros, Broadcom, Intel, Realtek, Foxconn Sony, Lite-On, CSR, MediaTek, Alps, Asus, AzureWave)
            bool isKnownBtVendor = IsKnownBluetoothVendor(vid);

            // Check USB Class E0 (Wireless Controller / Bluetooth)
            bool isUsbClassE0 = hwId.IndexOf("Class_E0", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                id.IndexOf("Class_E0", StringComparison.OrdinalIgnoreCase) >= 0;

            // Must match physical Bluetooth controller hardware bus patterns (USB, PCI, or physical radio adapter)
            bool isPhysicalBus = id.StartsWith("USB\\", StringComparison.OrdinalIgnoreCase) ||
                                id.StartsWith("PCI\\", StringComparison.OrdinalIgnoreCase) ||
                                hwId.StartsWith("USB\\", StringComparison.OrdinalIgnoreCase) ||
                                hwId.StartsWith("PCI\\", StringComparison.OrdinalIgnoreCase);

            bool hasBtKeyword = desc.IndexOf("Bluetooth", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                name.IndexOf("Bluetooth", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                desc.IndexOf("Wireless Bluetooth", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                name.IndexOf("Wireless Bluetooth", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                desc.IndexOf("AR301", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                name.IndexOf("AR301", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                desc.IndexOf("BCM207", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                name.IndexOf("BCM207", StringComparison.OrdinalIgnoreCase) >= 0;

            return (isPhysicalBus && (hasBtKeyword || isKnownBtVendor || isUsbClassE0)) ||
                   hasBtKeyword;
        }

        private static bool IsKnownBluetoothVendor(string vid)
        {
            if (string.IsNullOrWhiteSpace(vid)) return false;
            vid = vid.Trim().ToUpperInvariant();
            return vid == "0BDA" || // Realtek
                   vid == "8087" || // Intel
                   vid == "0A5C" || // Broadcom
                   vid == "0CF3" || // Qualcomm Atheros
                   vid == "17CB" || // Qualcomm
                   vid == "0489" || // Foxconn (Sony VAIO / HP OEM for Atheros/Broadcom)
                   vid == "04CA" || // Lite-On (OEM for Atheros/Broadcom)
                   vid == "13D3" || // AzureWave
                   vid == "044E" || // Alps Electric
                   vid == "0B05" || // ASUS
                   vid == "0A12" || // Cambridge Silicon Radio (CSR)
                   vid == "0E8D";   // MediaTek
        }

        public BluetoothAdapterInfo GetPrimaryAdapter()
        {
            var adapters = EnumerateAdapters();
            if (adapters.Count == 0)
            {
                return new BluetoothAdapterInfo
                {
                    Status = AdapterStatus.NotPresent,
                    IsPresent = false,
                    FriendlyName = "No Bluetooth Adapter Detected",
                    Manufacturer = "Unknown",
                    ProblemDescription = "No Bluetooth adapter was found in Windows Device enumeration."
                };
            }

            // Priority:
            // 1. Physical Controller (USB\VID_ or PCI\VEN_ with Operational status and valid HardwareDetails)
            // 2. Any physical controller (USB/PCI)
            // 3. Operational Bluetooth device
            // 4. First detected device
            var physicalOperational = adapters.FirstOrDefault(a => 
                a.Status == AdapterStatus.Operational && 
                (a.HardwareDetails.BusType == "USB" || a.HardwareDetails.BusType == "PCI") &&
                a.HardwareDetails.IsValid);

            if (physicalOperational != null) return physicalOperational;

            var physicalAny = adapters.FirstOrDefault(a => 
                (a.HardwareDetails.BusType == "USB" || a.HardwareDetails.BusType == "PCI") &&
                a.HardwareDetails.IsValid);

            if (physicalAny != null) return physicalAny;

            return adapters.FirstOrDefault(a => a.Status == AdapterStatus.Operational) ?? adapters[0];
        }

        private BluetoothAdapterInfo ProcessDeviceInfo(IntPtr devInfo, ref SetupApiNative.SP_DEVINFO_DATA devInfoData)
        {
            var adapter = new BluetoothAdapterInfo();

            // Device Instance ID
            var sbInstanceId = new StringBuilder(1024);
            if (SetupApiNative.SetupDiGetDeviceInstanceId(devInfo, ref devInfoData, sbInstanceId, sbInstanceId.Capacity, out _))
            {
                adapter.DeviceInstanceId = sbInstanceId.ToString();
            }

            // Friendly Name & Description
            string friendlyName = GetDeviceStringProperty(devInfo, ref devInfoData, SetupApiNative.SPDRP_FRIENDLYNAME);
            string devDesc = GetDeviceStringProperty(devInfo, ref devInfoData, SetupApiNative.SPDRP_DEVICEDESC);
            adapter.FriendlyName = !string.IsNullOrEmpty(friendlyName) ? friendlyName : (!string.IsNullOrEmpty(devDesc) ? devDesc : "Bluetooth Device");
            adapter.DeviceDescription = devDesc;

            // Manufacturer
            adapter.Manufacturer = GetDeviceStringProperty(devInfo, ref devInfoData, SetupApiNative.SPDRP_MFG);

            // Hardware IDs
            var hwIds = GetDeviceMultiStringProperty(devInfo, ref devInfoData, SetupApiNative.SPDRP_HARDWAREID);
            adapter.HardwareIds = hwIds;
            if (hwIds.Count > 0)
            {
                adapter.HardwareId = hwIds[0];
                adapter.HardwareDetails = HardwareIdParser.Parse(hwIds[0]);

                if (string.IsNullOrEmpty(adapter.Manufacturer) && !string.IsNullOrEmpty(adapter.HardwareDetails.IdentifiedManufacturer))
                {
                    adapter.Manufacturer = adapter.HardwareDetails.IdentifiedManufacturer;
                }
            }

            // Inspect Status via Configuration Manager
            uint statusFlags = 0;
            uint problemCode = 0;
            uint cr = CfgMgr32Native.CM_Get_DevNode_Status(out statusFlags, out problemCode, devInfoData.DevInst, 0);

            if (cr == CfgMgr32Native.CR_SUCCESS)
            {
                adapter.IsPresent = true;
                adapter.ProblemCode = problemCode;

                if ((statusFlags & CfgMgr32Native.DN_HAS_PROBLEM) != 0)
                {
                    if (problemCode == CfgMgr32Native.CM_PROB_DISABLED)
                    {
                        adapter.Status = AdapterStatus.Disabled;
                        adapter.IsEnabled = false;
                        adapter.ProblemDescription = "The device is disabled in Windows Device Manager (Code 22).";
                    }
                    else if (problemCode == CfgMgr32Native.CM_PROB_FAILED_START)
                    {
                        adapter.Status = AdapterStatus.DeviceError;
                        adapter.IsEnabled = true;
                        adapter.ProblemDescription = "This device cannot start (Code 10). Firmware or driver initialization failed.";
                    }
                    else if (problemCode == CfgMgr32Native.CM_PROB_FAILED_INSTALL)
                    {
                        adapter.Status = AdapterStatus.DriverMissing;
                        adapter.IsEnabled = false;
                        adapter.ProblemDescription = "The drivers for this device are not installed (Code 28). Missing vendor firmware package.";
                    }
                    else if (problemCode == CfgMgr32Native.CM_PROB_FAILED_POST_START)
                    {
                        adapter.Status = AdapterStatus.DeviceError;
                        adapter.IsEnabled = true;
                        adapter.ProblemDescription = "Windows stopped this device because it reported problems (Code 43). Missing vendor firmware RAM patch.";
                    }
                    else
                    {
                        adapter.Status = AdapterStatus.DeviceError;
                        adapter.IsEnabled = true;
                        adapter.ProblemDescription = $"Windows reported problem code {problemCode} for this device.";
                    }
                }
                else if ((statusFlags & CfgMgr32Native.DN_STARTED) != 0)
                {
                    adapter.Status = AdapterStatus.Operational;
                    adapter.IsEnabled = true;
                }
                else
                {
                    adapter.Status = AdapterStatus.Operational;
                    adapter.IsEnabled = true;
                }
            }
            else
            {
                // If CM_Get_DevNode_Status fails, device might be phantom or not present
                adapter.IsPresent = false;
                adapter.Status = AdapterStatus.NotPresent;
                adapter.ProblemDescription = "Device node status could not be queried.";
            }

            // Inspect Driver Information via Driver Registry Key
            InspectDriverInfo(devInfo, ref devInfoData, adapter);

            // Filter out generic enumeration nodes (e.g. Microsoft Bluetooth Enumerator or LE Enumerator)
            // if it's the primary physical adapter we want to identify
            return adapter;
        }

        private void InspectDriverInfo(IntPtr devInfo, ref SetupApiNative.SP_DEVINFO_DATA devInfoData, BluetoothAdapterInfo adapter)
        {
            var hKey = SetupApiNative.SetupDiOpenDevRegKey(
                devInfo,
                ref devInfoData,
                SetupApiNative.DICS_FLAG_GLOBAL,
                0,
                SetupApiNative.DIREG_DRV,
                SetupApiNative.KEY_READ);

            if (hKey != IntPtr.Zero && hKey != (IntPtr)(-1))
            {
                try
                {
                    adapter.Driver.DriverProvider = ReadRegString(hKey, "ProviderName");
                    adapter.Driver.DriverVersion = ReadRegString(hKey, "DriverVersion");
                    adapter.Driver.DriverDateString = ReadRegString(hKey, "DriverDate");
                    adapter.Driver.InfPath = ReadRegString(hKey, "InfPath");
                    adapter.Driver.InfSection = ReadRegString(hKey, "InfSection");
                    adapter.Driver.MatchingDeviceId = ReadRegString(hKey, "MatchingDeviceId");

                    if (DateTime.TryParse(adapter.Driver.DriverDateString, out var dt))
                    {
                        adapter.Driver.DriverDate = dt;
                    }
                }
                finally
                {
                    SetupApiNative.RegCloseKey(hKey);
                }
            }
        }

        private string ReadRegString(IntPtr hKey, string valueName)
        {
            uint type = 0;
            uint size = 0;
            if (SetupApiNative.RegQueryValueEx(hKey, valueName, 0, out type, null, ref size) == 0 && size > 0)
            {
                byte[] buffer = new byte[size];
                if (SetupApiNative.RegQueryValueEx(hKey, valueName, 0, out type, buffer, ref size) == 0)
                {
                    return Encoding.Unicode.GetString(buffer).TrimEnd('\0');
                }
            }
            return string.Empty;
        }

        private string GetDeviceStringProperty(IntPtr devInfo, ref SetupApiNative.SP_DEVINFO_DATA devInfoData, int property)
        {
            byte[] buffer = new byte[1024];
            if (SetupApiNative.SetupDiGetDeviceRegistryProperty(devInfo, ref devInfoData, property, out _, buffer, (uint)buffer.Length, out uint reqSize))
            {
                return Encoding.Unicode.GetString(buffer, 0, (int)reqSize).TrimEnd('\0');
            }
            return string.Empty;
        }

        private List<string> GetDeviceMultiStringProperty(IntPtr devInfo, ref SetupApiNative.SP_DEVINFO_DATA devInfoData, int property)
        {
            var list = new List<string>();
            byte[] buffer = new byte[2048];
            if (SetupApiNative.SetupDiGetDeviceRegistryProperty(devInfo, ref devInfoData, property, out _, buffer, (uint)buffer.Length, out uint reqSize))
            {
                string raw = Encoding.Unicode.GetString(buffer, 0, (int)reqSize);
                var parts = raw.Split(new[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);
                list.AddRange(parts);
            }
            return list;
        }

        private List<BluetoothAdapterInfo> EnumerateViaWmi()
        {
            var list = new List<BluetoothAdapterInfo>();
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE PNPClass = 'Bluetooth' OR Name LIKE '%Bluetooth%'"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    var adapter = new BluetoothAdapterInfo();
                    adapter.DeviceInstanceId = obj["DeviceID"]?.ToString() ?? string.Empty;
                    adapter.FriendlyName = obj["Name"]?.ToString() ?? "Bluetooth Device";
                    adapter.DeviceDescription = obj["Description"]?.ToString() ?? string.Empty;
                    adapter.Manufacturer = obj["Manufacturer"]?.ToString() ?? string.Empty;

                    string status = obj["Status"]?.ToString() ?? string.Empty;
                    if (string.Equals(status, "OK", StringComparison.OrdinalIgnoreCase))
                    {
                        adapter.Status = AdapterStatus.Operational;
                        adapter.IsEnabled = true;
                        adapter.IsPresent = true;
                    }
                    else
                    {
                        adapter.Status = AdapterStatus.DeviceError;
                        adapter.ProblemDescription = $"WMI Status: {status}";
                    }

                    if (obj["HardwareID"] is string[] hwIds && hwIds.Length > 0)
                    {
                        adapter.HardwareIds = hwIds.ToList();
                        adapter.HardwareId = hwIds[0];
                        adapter.HardwareDetails = HardwareIdParser.Parse(hwIds[0]);
                    }

                    list.Add(adapter);
                }
            }
            return list;
        }

        private void EnrichWithRadioInfo(List<BluetoothAdapterInfo> adapters)
        {
            try
            {
                var findParams = new BluetoothNative.BLUETOOTH_FIND_RADIO_PARAMS
                {
                    dwSize = (uint)Marshal.SizeOf(typeof(BluetoothNative.BLUETOOTH_FIND_RADIO_PARAMS))
                };

                IntPtr hRadio = IntPtr.Zero;
                IntPtr hFind = BluetoothNative.BluetoothFindFirstRadio(ref findParams, out hRadio);

                if (hFind != IntPtr.Zero && hFind != (IntPtr)(-1))
                {
                    try
                    {
                        do
                        {
                            if (hRadio != IntPtr.Zero)
                            {
                                var radioInfo = new BluetoothNative.BLUETOOTH_RADIO_INFO();
                                radioInfo.dwSize = (uint)Marshal.SizeOf(typeof(BluetoothNative.BLUETOOTH_RADIO_INFO));

                                uint err = BluetoothNative.BluetoothGetRadioInfo(hRadio, ref radioInfo);
                                if (err == 0) // SUCCESS
                                {
                                    string address = string.Format("{0:X12}", radioInfo.address);
                                    if (address.Length == 12)
                                    {
                                        address = string.Join(":", Enumerable.Range(0, 6).Select(i => address.Substring(i * 2, 2)));
                                    }

                                    // Match with physical adapter or assign to first operational
                                    var target = adapters.FirstOrDefault(a => a.Status == AdapterStatus.Operational) ?? adapters.FirstOrDefault();
                                    if (target != null)
                                    {
                                        target.RadioAddress = address;
                                        target.RadioName = radioInfo.szName;
                                    }
                                }

                                BluetoothNative.CloseHandle(hRadio);
                                hRadio = IntPtr.Zero;
                            }
                        }
                        while (BluetoothNative.BluetoothFindNextRadio(hFind, out hRadio));
                    }
                    finally
                    {
                        BluetoothNative.BluetoothFindRadioClose(hFind);
                    }
                }
            }
            catch
            {
                // Radio API not available or bthprops error
            }
        }
    }
}
