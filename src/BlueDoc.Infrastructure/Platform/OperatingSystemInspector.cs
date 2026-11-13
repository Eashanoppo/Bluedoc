using System;
using System.Runtime.InteropServices;
using BlueDoc.Core.Enums;
using BlueDoc.Core.Models;
using BlueDoc.Infrastructure.Native;

namespace BlueDoc.Infrastructure.Platform
{
    public static class OperatingSystemInspector
    {
        public static OsVersionInfo GetOperatingSystemInfo()
        {
            var info = new OsVersionInfo
            {
                Is64BitOperatingSystem = Environment.Is64BitOperatingSystem
            };

            var osVersion = new NtdllNative.OSVERSIONINFOEX();
            osVersion.dwOSVersionInfoSize = Marshal.SizeOf(typeof(NtdllNative.OSVERSIONINFOEX));

            int status = NtdllNative.RtlGetVersion(ref osVersion);
            if (status == 0) // STATUS_SUCCESS
            {
                info.MajorVersion = osVersion.dwMajorVersion;
                info.MinorVersion = osVersion.dwMinorVersion;
                info.BuildNumber = osVersion.dwBuildNumber;
                info.ServicePack = osVersion.szCSDVersion ?? string.Empty;

                DetermineWindowsPlatform(info);
            }
            else
            {
                // Fallback to Environment.OSVersion
                var v = Environment.OSVersion.Version;
                info.MajorVersion = v.Major;
                info.MinorVersion = v.Minor;
                info.BuildNumber = v.Build;
                info.ServicePack = Environment.OSVersion.ServicePack;

                DetermineWindowsPlatform(info);
            }

            return info;
        }

        private static void DetermineWindowsPlatform(OsVersionInfo info)
        {
            if (info.MajorVersion == 6 && info.MinorVersion == 1)
            {
                info.Platform = WindowsPlatformType.Windows7;
                info.OperatingSystemName = string.IsNullOrEmpty(info.ServicePack) ? "Windows 7" : $"Windows 7 {info.ServicePack}";
            }
            else if (info.MajorVersion == 6 && info.MinorVersion == 2)
            {
                info.Platform = WindowsPlatformType.Windows8;
                info.OperatingSystemName = "Windows 8";
            }
            else if (info.MajorVersion == 6 && info.MinorVersion == 3)
            {
                info.Platform = WindowsPlatformType.Windows81;
                info.OperatingSystemName = "Windows 8.1";
            }
            else if (info.MajorVersion == 10 && info.MinorVersion == 0)
            {
                if (info.BuildNumber >= 22000)
                {
                    info.Platform = WindowsPlatformType.Windows11;
                    info.OperatingSystemName = $"Windows 11 (Build {info.BuildNumber})";
                }
                else
                {
                    info.Platform = WindowsPlatformType.Windows10;
                    info.OperatingSystemName = $"Windows 10 (Build {info.BuildNumber})";
                }
            }
            else
            {
                info.Platform = WindowsPlatformType.Unknown;
                info.OperatingSystemName = $"Windows NT {info.MajorVersion}.{info.MinorVersion} (Build {info.BuildNumber})";
            }
        }
    }
}
