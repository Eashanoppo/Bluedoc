using System;

namespace BlueDoc.Core.Models
{
    public class BluetoothDriverPackageInfo
    {
        public string PackageName { get; set; } = string.Empty;
        public string ChipsetFamily { get; set; } = string.Empty;
        public string VendorId { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public string RecommendedVersion { get; set; } = string.Empty;
        public string ReleaseDate { get; set; } = string.Empty;
        public string FileSizeFormatted { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public string DownloadUrl { get; set; } = string.Empty;
        public string CatalogSearchUrl { get; set; } = string.Empty;
        public string VendorPortalUrl { get; set; } = string.Empty;
        public string ReleaseNotes { get; set; } = string.Empty;
        public bool IsUpdateAvailable { get; set; }
        public string InstalledVersion { get; set; } = string.Empty;
        public string InstalledDate { get; set; } = string.Empty;
        public string UpdateStatusSummary { get; set; } = "Inspecting driver state...";
        public string CompatibilityNotes { get; set; } = "Fully certified for Windows 10 & Windows 11 (64-bit).";
        public bool IsGenericMicrosoftDriver { get; set; }
    }

    public class DownloadProgressReport
    {
        public int Percentage { get; set; }
        public long BytesReceived { get; set; }
        public long TotalBytesToReceive { get; set; }
        public double SpeedKbs { get; set; }
        public string StatusMessage { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public bool HasError { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string DownloadedFilePath { get; set; } = string.Empty;
    }
}

