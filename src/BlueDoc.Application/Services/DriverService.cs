using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using BlueDoc.Core.Models;
using BlueDoc.Infrastructure.Drivers;
using BlueDoc.Infrastructure.HostSystem;

namespace BlueDoc.Application.Services
{
    public class DriverService
    {
        private readonly DriverAssistant _assistant;
        private readonly BluetoothDriverCatalogService _catalogService;

        public DriverService(
            DriverAssistant assistant = null, 
            BluetoothDriverCatalogService catalogService = null)
        {
            _assistant = assistant ?? new DriverAssistant();
            _catalogService = catalogService ?? new BluetoothDriverCatalogService();
        }

        public LaptopSystemInfo GetLaptopSystemInfo()
        {
            return SystemHardwareModelInspector.InspectSystem();
        }

        public BluetoothDriverPackageInfo GetMatchingDriverPackage(BluetoothAdapterInfo adapter, LaptopSystemInfo systemInfo)
        {
            return _catalogService.MatchDriverPackage(adapter, systemInfo);
        }

        public Task<string> DownloadDriverPackageAsync(
            BluetoothDriverPackageInfo package, 
            IProgress<DownloadProgressReport> progress, 
            CancellationToken ct = default(CancellationToken))
        {
            return _catalogService.DownloadDriverPackageAsync(package, progress, ct);
        }

        public Task<bool> InstallDriverPackageAsync(string packagePath, string hardwareId = null)
        {
            return _catalogService.InstallDriverPackageAsync(packagePath, hardwareId);
        }

        public Task<string> BackupCurrentDriverAsync(string destinationFolder = null)
        {
            return _catalogService.BackupCurrentDriverAsync(destinationFolder);
        }

        public string GetManufacturerSupportLink(HardwareIdDetails hwDetails)
        {
            return _assistant.GetManufacturerSupportUrl(hwDetails);
        }

        public string ExplainProblemCode(int problemCode)
        {
            return _assistant.ExplainProblemCode(problemCode);
        }

        public string GetRollbackGuide(string adapterName)
        {
            return _assistant.GenerateRollbackInstructions(adapterName);
        }

        public string GetReinstallationGuide(string adapterName, string supportUrl)
        {
            return _assistant.GenerateReinstallInstructions(adapterName, supportUrl);
        }

        public bool OpenDeviceManager()
        {
            return _assistant.OpenDeviceManager();
        }

        public bool OpenWindowsBluetoothSettings()
        {
            return _assistant.OpenWindowsBluetoothSettings();
        }
    }
}
