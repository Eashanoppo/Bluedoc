namespace BlueDoc.Core.Models
{
    public class HardwareIdDetails
    {
        public string RawHardwareId { get; set; } = string.Empty;
        public string BusType { get; set; } = string.Empty; // USB, PCI, BTH, etc.
        public string VendorId { get; set; } = string.Empty; // e.g. 0BDA, 8087
        public string ProductId { get; set; } = string.Empty; // e.g. C822
        public string Revision { get; set; } = string.Empty;
        public string SubsystemVendorId { get; set; } = string.Empty;
        public string SubsystemId { get; set; } = string.Empty;
        public string IdentifiedManufacturer { get; set; } = string.Empty;
        public string IdentifiedChipsetFamily { get; set; } = string.Empty;
        public bool IsValid => !string.IsNullOrEmpty(VendorId);
    }
}

