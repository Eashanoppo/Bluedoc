using System;

namespace BlueDoc.Core.Models
{
    public class LaptopSystemInfo
    {
        public string Manufacturer { get; set; } = "Unknown";
        public string Model { get; set; } = "Standard PC";
        public string SystemFamily { get; set; } = string.Empty;
        public string BaseBoardProduct { get; set; } = string.Empty;
        public string BiosVersion { get; set; } = string.Empty;
        public string BiosReleaseDate { get; set; } = string.Empty;
        public string OperatingSystemName { get; set; } = string.Empty;
        public string Architecture { get; set; } = "64-bit";
        public bool IsLaptop { get; set; } = true;
        public string OemSupportUrl { get; set; } = string.Empty;

        public string DisplayName => string.IsNullOrWhiteSpace(Manufacturer) || Manufacturer.Equals("Unknown", StringComparison.OrdinalIgnoreCase)
            ? Model
            : $"{Manufacturer} {Model}";

        public string FormFactorDescription => IsLaptop ? "Notebook / Laptop" : "Desktop Workstation / PC";
    }
}

