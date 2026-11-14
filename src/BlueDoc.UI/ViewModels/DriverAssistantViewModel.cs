using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using BlueDoc.Application.Services;
using BlueDoc.Core.Models;
using BlueDoc.UI.Common;

namespace BlueDoc.UI.ViewModels
{
    public class DriverAssistantViewModel : ViewModelBase
    {
        private readonly AdapterService _adapterService;
        private readonly DriverService _driverService;

        private BluetoothAdapterInfo _adapter;
        private LaptopSystemInfo _systemInfo;
        private BluetoothDriverPackageInfo _driverPackage;
        private DownloadProgressReport _downloadReport = new DownloadProgressReport();
        private bool _isDownloading;
        private bool _isInstalling;
        private string _downloadedFilePath = string.Empty;
        private string _statusMessage = "Ready to inspect laptop hardware & drivers.";

        private string _driverStatusExplanation = string.Empty;
        private string _rollbackGuide = string.Empty;
        private string _reinstallGuide = string.Empty;
        private string _supportUrl = string.Empty;
        private string _installStatusDetail = "Ready for driver deployment.";
        private bool _isLoading;

        private CancellationTokenSource _downloadCts;

        public BluetoothAdapterInfo Adapter
        {
            get => _adapter;
            set => SetProperty(ref _adapter, value);
        }

        public LaptopSystemInfo SystemInfo
        {
            get => _systemInfo;
            set => SetProperty(ref _systemInfo, value);
        }

        public BluetoothDriverPackageInfo DriverPackage
        {
            get => _driverPackage;
            set
            {
                if (SetProperty(ref _driverPackage, value))
                {
                    OnPropertyChanged(nameof(HasDriverPackage));
                    OnPropertyChanged(nameof(IsUpdateAvailable));
                    OnPropertyChanged(nameof(RecommendedVersionDisplay));
                }
            }
        }

        public bool HasDriverPackage => DriverPackage != null;
        public bool IsUpdateAvailable => DriverPackage?.IsUpdateAvailable ?? false;
        public string RecommendedVersionDisplay => DriverPackage?.RecommendedVersion ?? "N/A";

        public DownloadProgressReport DownloadReport
        {
            get => _downloadReport;
            set
            {
                if (SetProperty(ref _downloadReport, value))
                {
                    OnPropertyChanged(nameof(DownloadButtonText));
                }
            }
        }

        public bool IsDownloading
        {
            get => _isDownloading;
            set
            {
                if (SetProperty(ref _isDownloading, value))
                {
                    OnPropertyChanged(nameof(DownloadButtonText));
                    OnPropertyChanged(nameof(IsOperationInProgress));
                    System.Windows.Input.CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public bool IsInstalling
        {
            get => _isInstalling;
            set
            {
                if (SetProperty(ref _isInstalling, value))
                {
                    OnPropertyChanged(nameof(CanInstallPackage));
                    OnPropertyChanged(nameof(InstallButtonText));
                    OnPropertyChanged(nameof(IsOperationInProgress));
                    System.Windows.Input.CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public bool IsOperationInProgress => IsDownloading || IsInstalling || IsLoading;

        public string DownloadButtonText => IsDownloading 
            ? $"Downloading ({DownloadReport?.Percentage ?? 0}%)..." 
            : "Download Driver Package";

        public string InstallButtonText => IsInstalling 
            ? "Installing Driver Package (PnPUtil)..." 
            : "Install Driver Package";

        public string InstallStatusDetail
        {
            get => _installStatusDetail;
            set => SetProperty(ref _installStatusDetail, value);
        }

        public string DownloadedFilePath
        {
            get => _downloadedFilePath;
            set
            {
                if (SetProperty(ref _downloadedFilePath, value))
                {
                    OnPropertyChanged(nameof(HasDownloadedFile));
                    OnPropertyChanged(nameof(CanInstallPackage));
                    System.Windows.Input.CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public bool HasDownloadedFile => !string.IsNullOrEmpty(DownloadedFilePath) && File.Exists(DownloadedFilePath);
        public bool CanInstallPackage => (HasDownloadedFile || (DriverPackage != null && DriverPackage.IsLocallyBundled)) && !IsInstalling;

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public string DriverStatusExplanation
        {
            get => _driverStatusExplanation;
            set => SetProperty(ref _driverStatusExplanation, value);
        }

        public string RollbackGuide
        {
            get => _rollbackGuide;
            set => SetProperty(ref _rollbackGuide, value);
        }

        public string ReinstallGuide
        {
            get => _reinstallGuide;
            set => SetProperty(ref _reinstallGuide, value);
        }

        public string SupportUrl
        {
            get => _supportUrl;
            set => SetProperty(ref _supportUrl, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ICommand RefreshCommand { get; }
        public ICommand CheckUpdatesCommand { get; }
        public ICommand DownloadPackageCommand { get; }
        public ICommand CancelDownloadCommand { get; }
        public ICommand InstallPackageCommand { get; }
        public ICommand BackupDriverCommand { get; }
        public ICommand OpenOemSupportCommand { get; }
        public ICommand OpenCatalogSearchCommand { get; }
        public ICommand OpenDeviceManagerCommand { get; }
        public ICommand OpenSupportPageCommand { get; }
        public ICommand OpenSettingsCommand { get; }

        public DriverAssistantViewModel(AdapterService adapterService = null, DriverService driverService = null)
        {
            _adapterService = adapterService ?? new AdapterService();
            _driverService = driverService ?? new DriverService();

            RefreshCommand = new RelayCommand(async () => await LoadDriverInfoAsync());
            CheckUpdatesCommand = new RelayCommand(async () => await LoadDriverInfoAsync());
            DownloadPackageCommand = new RelayCommand(async () => await DownloadDriverAsync(), () => !IsDownloading && HasDriverPackage);
            CancelDownloadCommand = new RelayCommand(() => _downloadCts?.Cancel(), () => IsDownloading);
            InstallPackageCommand = new RelayCommand(async () => await InstallDriverAsync(), () => CanInstallPackage);
            BackupDriverCommand = new RelayCommand(async () => await BackupDriverAsync(), () => !IsLoading);

            OpenOemSupportCommand = new RelayCommand(() => OpenUrl(SystemInfo?.OemSupportUrl));
            OpenCatalogSearchCommand = new RelayCommand(() => OpenUrl(DriverPackage?.CatalogSearchUrl));
            OpenDeviceManagerCommand = new RelayCommand(() => _driverService.OpenDeviceManager());
            OpenSupportPageCommand = new RelayCommand(() => OpenUrl(SupportUrl));
            OpenSettingsCommand = new RelayCommand(() => _driverService.OpenWindowsBluetoothSettings());
        }

        private void OpenUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return;
            try
            {
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
            catch { }
        }

        public async Task LoadDriverInfoAsync()
        {
            IsLoading = true;
            StatusMessage = "Querying system hardware model & Bluetooth chipset profile...";
            try
            {
                // 1. Fetch System & Laptop Info
                SystemInfo = await Task.Run(() => _driverService.GetLaptopSystemInfo());

                // 2. Fetch Primary Bluetooth Adapter
                Adapter = await _adapterService.GetPrimaryAdapterAsync();

                if (Adapter != null)
                {
                    DriverStatusExplanation = _driverService.ExplainProblemCode((int)Adapter.ProblemCode);
                    SupportUrl = _driverService.GetManufacturerSupportLink(Adapter.HardwareDetails);
                    RollbackGuide = _driverService.GetRollbackGuide(Adapter.FriendlyName);
                    ReinstallGuide = _driverService.GetReinstallationGuide(Adapter.FriendlyName, SupportUrl);

                    // 3. Match Driver Package
                    DriverPackage = await Task.Run(() => _driverService.GetMatchingDriverPackage(Adapter, SystemInfo));
                    StatusMessage = DriverPackage.UpdateStatusSummary;

                    if (DriverPackage != null && DriverPackage.IsLocallyBundled && File.Exists(DriverPackage.LocalInfPath))
                    {
                        DownloadedFilePath = DriverPackage.LocalInfPath;
                    }
                }
                else
                {
                    DriverStatusExplanation = "No Bluetooth adapter detected in the system. Check if the adapter is disconnected or disabled.";
                    SupportUrl = "https://support.microsoft.com/en-us/windows/fix-bluetooth-problems-in-windows-723e05f8-0376-46a4-73ac-eeb2d8442a66";
                    RollbackGuide = _driverService.GetRollbackGuide(null);
                    ReinstallGuide = _driverService.GetReinstallationGuide(null, SupportUrl);
                    DriverPackage = _driverService.GetMatchingDriverPackage(null, SystemInfo);
                    StatusMessage = "No Bluetooth hardware detected.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Driver inspection error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task DownloadDriverAsync()
        {
            if (DriverPackage == null) return;

            if (DriverPackage.IsLocallyBundled && File.Exists(DriverPackage.LocalInfPath))
            {
                DownloadedFilePath = DriverPackage.LocalInfPath;
                StatusMessage = $"Certified offline driver package '{Path.GetFileName(DriverPackage.LocalInfPath)}' is loaded. Click 'Install Driver Package'.";
                NotificationService.Instance.ShowSuccess("Package Ready", "Bundled WHQL driver and firmware ready for 1-click install.");
                return;
            }

            IsDownloading = true;
            _downloadCts = new CancellationTokenSource();
            DownloadReport = new DownloadProgressReport { StatusMessage = "Initiating download from verified catalog..." };
            NotificationService.Instance.ShowInfo("Download Started", $"Downloading {DriverPackage.PackageName} from Microsoft Update Catalog...");

            var progress = new Progress<DownloadProgressReport>(report =>
            {
                DownloadReport = report;
                StatusMessage = $"Downloading {DriverPackage.PackageName}: {report.Percentage}% ({report.SpeedKbs:F1} KB/s)...";
                OnPropertyChanged(nameof(DownloadButtonText));
            });

            try
            {
                string savedPath = await _driverService.DownloadDriverPackageAsync(DriverPackage, progress, _downloadCts.Token);
                DownloadedFilePath = savedPath;
                StatusMessage = $"Driver package ready: {Path.GetFileName(savedPath)}";
                NotificationService.Instance.ShowSuccess("Driver Ready", $"Downloaded {DriverPackage.PackageName}. Ready for installation.");
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Driver download cancelled by user.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Download error: {ex.Message}";
                NotificationService.Instance.ShowWarning("Download Notice", ex.Message);
            }
            finally
            {
                IsDownloading = false;
                _downloadCts?.Dispose();
                _downloadCts = null;
            }
        }

        public async Task InstallDriverAsync()
        {
            if (string.IsNullOrEmpty(DownloadedFilePath) && DriverPackage != null && DriverPackage.IsLocallyBundled && File.Exists(DriverPackage.LocalInfPath))
            {
                DownloadedFilePath = DriverPackage.LocalInfPath;
            }

            if (!HasDownloadedFile)
            {
                StatusMessage = "No driver package file is ready. Please click 'Download Driver Package' first.";
                NotificationService.Instance.ShowWarning("Setup Required", "Please click 'Download Driver Package' before installing.");
                return;
            }

            IsInstalling = true;
            InstallStatusDetail = "Windows Plug and Play installer is registering the WHQL driver package into the Driver Store. If Windows prompts for Administrator approval (UAC), please click 'Yes' to proceed.";
            StatusMessage = "Installing driver package into Windows Driver Store...";
            NotificationService.Instance.ShowInfo("Installation in Progress", "Installing driver package into Windows Driver Store. Please approve any Administrator (UAC) prompt.");
            try
            {
                InstallStatusDetail = "Invoking PnPUtil to register INF and stage hardware firmware...";
                bool success = await _driverService.InstallDriverPackageAsync(DownloadedFilePath, Adapter?.HardwareId);
                if (success)
                {
                    InstallStatusDetail = "Driver successfully installed. Windows is refreshing the Bluetooth radio stack...";
                    StatusMessage = "Driver installation process launched. Windows is refreshing the hardware stack.";
                    NotificationService.Instance.ShowSuccess("Installation Completed", "Windows Bluetooth driver has been successfully installed.");
                    await Task.Delay(2000);
                    await LoadDriverInfoAsync();
                }
                else
                {
                    InstallStatusDetail = "Driver installation returned. Verifying adapter status in Device Manager...";
                    StatusMessage = "Installation completed or requires manual setup confirmation.";
                }
            }
            catch (Exception ex)
            {
                InstallStatusDetail = $"Installation error: {ex.Message}";
                StatusMessage = $"Installation exception: {ex.Message}";
                NotificationService.Instance.ShowError("Installation Error", ex.Message);
            }
            finally
            {
                IsInstalling = false;
            }
        }

        public async Task BackupDriverAsync()
        {
            StatusMessage = "Exporting active Bluetooth driver store files...";
            try
            {
                string backupDir = await _driverService.BackupCurrentDriverAsync();
                StatusMessage = $"Driver files backed up to: {backupDir}";
                NotificationService.Instance.ShowSuccess("Driver Backup Complete", $"Active Bluetooth drivers saved to {backupDir}");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Driver backup error: {ex.Message}";
            }
        }
    }
}
