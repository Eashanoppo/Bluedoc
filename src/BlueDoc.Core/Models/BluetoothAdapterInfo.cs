using System;
using System.Collections.Generic;
using BlueDoc.Core.Enums;

namespace BlueDoc.Core.Models
{
    public class BluetoothAdapterInfo
    {
        public string DeviceInstanceId { get; set; } = string.Empty;
        public string FriendlyName { get; set; } = string.Empty;
        public string DeviceDescription { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = string.Empty;
        public string HardwareId { get; set; } = string.Empty;
        public List<string> HardwareIds { get; set; } = new List<string>();
        public HardwareIdDetails HardwareDetails { get; set; } = new HardwareIdDetails();
        public DriverInfo Driver { get; set; } = new DriverInfo();
        public AdapterStatus Status { get; set; } = AdapterStatus.Unknown;
        public uint ProblemCode { get; set; }
        public string ProblemDescription { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public bool IsPresent { get; set; }
        public string RadioAddress { get; set; } = string.Empty;
        public string RadioName { get; set; } = string.Empty;
        public string HciVersion { get; set; } = string.Empty;
        public string LmpVersion { get; set; } = string.Empty;
        public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

        public string StatusDisplay
        {
            get
            {
                switch (Status)
                {
                    case AdapterStatus.Operational: return "Operational (Working Properly)";
                    case AdapterStatus.Disabled: return "Disabled in System";
                    case AdapterStatus.DeviceError: return $"Device Error (Code {ProblemCode})";
                    case AdapterStatus.DriverMissing: return "Driver Missing or Damaged";
                    case AdapterStatus.NotPresent: return "Adapter Not Detected";
                    default: return "Unknown Status";
                }
            }
        }
    }
}

