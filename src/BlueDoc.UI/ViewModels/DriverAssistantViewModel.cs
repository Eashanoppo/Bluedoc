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
            set => SetProperty(ref _downloadReport, value);
        }

        public bool IsDownloading
        {
            get => _isDownloading;
            set => SetProperty(ref _isDownloading, value);
        }

        public bool IsInstalling
        {
            get => _isInstalling;
            set => SetProperty(ref _isInstalling, value);
        }

        public string DownloadedFilePath
        {
            get => _downloadedFilePath;
            set
            {
                if (SetProperty(ref _downloadedFilePath, value))
                {
                    OnPropertyChanged(nameof(HasDownloadedFile));
                }
            }
        }

        public bool HasDownloadedFile => !string.IsNullOrEmpty(DownloadedFilePath) && File.Exists(DownloadedFilePath);

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
            InstallPackageCommand = new RelayCommand(async () => await InstallDriverAsync(), () => HasDownloadedFile && !IsInstalling);
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

            IsDownloading = true;
            _downloadCts = new CancellationTokenSource();
            DownloadReport = new DownloadProgressReport { StatusMessage = "Initiating download from verified catalog..." };

            var progress = new Progress<DownloadProgressReport>(report =>
            {
                DownloadReport = report;
                StatusMessage = report.StatusMessage;
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
            if (!HasDownloadedFile) return;

            IsInstalling = true;
            StatusMessage = "Launching Windows driver installer with administrative elevation...";
            try
            {
                bool success = await _driverService.InstallDriverPackageAsync(DownloadedFilePath, Adapter?.HardwareId);
                if (success)
                {
                    StatusMessage = "Driver installation process launched. Windows is refreshing the hardware stack.";
                    NotificationService.Instance.ShowSuccess("Installation Started", "Windows is updating the Bluetooth driver.");
                    await Task.Delay(2000);
                    await LoadDriverInfoAsync();
                }
                else
                {
                    StatusMessage = "Installation completed or requires manual setup confirmation.";
                }
            }
            catch (Exception ex)
            {
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
