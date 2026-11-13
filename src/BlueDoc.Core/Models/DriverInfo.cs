using System;

namespace BlueDoc.Core.Models
{
    public class DriverInfo
    {
        public string DriverProvider { get; set; } = string.Empty;
        public string DriverVersion { get; set; } = string.Empty;
        public DateTime? DriverDate { get; set; }
        public string DriverDateString { get; set; } = string.Empty;
        public string MatchingDeviceId { get; set; } = string.Empty;
        public string InfPath { get; set; } = string.Empty;
        public string InfSection { get; set; } = string.Empty;
        public bool IsDriverInstalled => !string.IsNullOrEmpty(DriverVersion) || !string.IsNullOrEmpty(DriverProvider);
    }
}

