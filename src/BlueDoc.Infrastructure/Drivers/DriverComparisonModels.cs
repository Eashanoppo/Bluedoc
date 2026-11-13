using System;

namespace BlueDoc.Infrastructure.Drivers
{
    public class DriverComparisonResult
    {
        public string VendorId { get; set; } = string.Empty;
        public string VendorName { get; set; } = "Unknown";
        public string InstalledVersion { get; set; } = "None";
        public string RecommendedVersion { get; set; } = "Latest OEM Signed";
        public string Status { get; set; } = "Unknown"; // Optimal, Outdated, Known Problematic, Generic Class Driver
        public bool HasKnownRegression { get; set; }
        public string KnownRegressionDetails { get; set; } = string.Empty;
        public string OfficialDownloadUrl { get; set; } = string.Empty;
        public string ActionRecommendation { get; set; } = string.Empty;
    }
}

