using BlueDoc.Core.Enums;

namespace BlueDoc.Core.Models
{
    public class OsVersionInfo
    {
        public WindowsPlatformType Platform { get; set; } = WindowsPlatformType.Unknown;
        public string OperatingSystemName { get; set; } = string.Empty;
        public int MajorVersion { get; set; }
        public int MinorVersion { get; set; }
        public int BuildNumber { get; set; }
        public string ServicePack { get; set; } = string.Empty;
        public bool Is64BitOperatingSystem { get; set; }
        public bool SupportsBleModernApis => Platform == WindowsPlatformType.Windows10 || Platform == WindowsPlatformType.Windows11;
        public bool SupportsWin32ClassicApis => true;
        public string ArchitectureDisplay => Is64BitOperatingSystem ? "64-bit" : "32-bit";

        public override string ToString()
        {
            return $"{OperatingSystemName} (Build {BuildNumber}) {ArchitectureDisplay}";
        }
    }
}

