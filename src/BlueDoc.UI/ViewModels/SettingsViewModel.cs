using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using BlueDoc.Core.Interfaces;
using BlueDoc.Data;
using BlueDoc.Data.Repositories;
using BlueDoc.UI.Common;

namespace BlueDoc.UI.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly ISettingsRepository _settingsRepo;
        private readonly SqliteDatabaseManager _db;

        // 1. Appearance
        private string _theme = "Dark (Default)";
        private bool _enableAnimations = true;

        // 2. General
        private bool _startWithWindows;
        private bool _minimizeToTray = true;
        private bool _enableNotificationToasts = true;

        // 3. Scanning
        private int _scanTimeout = 10;
        private bool _continuousScanningDefault;
        private bool _autoDeduplicateDevices = true;

        // 4. Bluetooth Behavior
        private bool _autoReconnectTrustedDevices = true;
        private string _audioProfilePrioritization = "Balanced Automatic";

        // 5. Diagnostics
        private int _historyRetentionDays = 30;
        private bool _automatedDiagnosticValidation = true;
        private string _etwLoggingLevel = "Informational";

        // 6. Performance
        private bool _lowResourceMode;

        // 7. Privacy
        private bool _anonymizeMacs;
        private bool _strictOfflineZeroCloudPolicy = true;
        private string _databasePath;
        private string _databaseSize = "Calculating...";

        // 8. Legacy Hardware Compatibility
        private bool _softwareRenderingMode;
        private bool _legacyWin7StackFallback = true;
        private bool _disableGpuDropShadows = true;

        // 9. Status Message
        private string _statusMessage = string.Empty;

        // 1. Appearance
        public string Theme
        {
            get => _theme;
            set
            {
                if (SetProperty(ref _theme, value))
                {
                    if (value.IndexOf("Light", StringComparison.OrdinalIgnoreCase) >= 0)
                        ThemeManager.SetTheme(AppTheme.Light);
                    else
                        ThemeManager.SetTheme(AppTheme.Dark);
                }
            }
        }

        public bool EnableAnimations
        {
            get => _enableAnimations;
            set
            {
                if (SetProperty(ref _enableAnimations, value))
                {
                    MotionManager.Instance.ForceReducedMotion = !value;
                }
            }
        }

        // 2. General
        public bool StartWithWindows
        {
            get => _startWithWindows;
            set => SetProperty(ref _startWithWindows, value);
        }

        public bool MinimizeToTray
        {
            get => _minimizeToTray;
            set => SetProperty(ref _minimizeToTray, value);
        }

        public bool EnableNotificationToasts
        {
            get => _enableNotificationToasts;
            set => SetProperty(ref _enableNotificationToasts, value);
        }

        // 3. Scanning
        public int ScanTimeout
        {
            get => _scanTimeout;
            set => SetProperty(ref _scanTimeout, value);
        }

        public bool ContinuousScanningDefault
        {
            get => _continuousScanningDefault;
            set => SetProperty(ref _continuousScanningDefault, value);
        }

        public bool AutoDeduplicateDevices
        {
            get => _autoDeduplicateDevices;
            set => SetProperty(ref _autoDeduplicateDevices, value);
        }

        // 4. Bluetooth Behavior
        public bool AutoReconnectTrustedDevices
        {
            get => _autoReconnectTrustedDevices;
            set => SetProperty(ref _autoReconnectTrustedDevices, value);
        }

        public string AudioProfilePrioritization
        {
            get => _audioProfilePrioritization;
            set => SetProperty(ref _audioProfilePrioritization, value);
        }

        // 5. Diagnostics
        public int HistoryRetentionDays
        {
            get => _historyRetentionDays;
            set => SetProperty(ref _historyRetentionDays, value);
        }

        public bool AutomatedDiagnosticValidation
        {
            get => _automatedDiagnosticValidation;
            set => SetProperty(ref _automatedDiagnosticValidation, value);
        }

        public string EtwLoggingLevel
        {
            get => _etwLoggingLevel;
            set => SetProperty(ref _etwLoggingLevel, value);
        }

        // 6. Performance
        public bool LowResourceMode
        {
            get => _lowResourceMode;
            set => SetProperty(ref _lowResourceMode, value);
        }

        // 7. Privacy
        public bool AnonymizeMacs
        {
            get => _anonymizeMacs;
            set => SetProperty(ref _anonymizeMacs, value);
        }

        public bool StrictOfflineZeroCloudPolicy
        {
            get => _strictOfflineZeroCloudPolicy;
            set => SetProperty(ref _strictOfflineZeroCloudPolicy, value);
        }

        public string DatabasePath
        {
            get => _databasePath;
            set => SetProperty(ref _databasePath, value);
        }

        public string DatabaseSize
        {
            get => _databaseSize;
            set => SetProperty(ref _databaseSize, value);
        }

        // 8. Legacy Hardware Compatibility
        public bool SoftwareRenderingMode
        {
            get => _softwareRenderingMode;
            set
            {
                if (SetProperty(ref _softwareRenderingMode, value))
                {
                    ApplyRenderMode(value);
                }
            }
        }

        public bool LegacyWin7StackFallback
        {
            get => _legacyWin7StackFallback;
            set => SetProperty(ref _legacyWin7StackFallback, value);
        }

        public bool DisableGpuDropShadows
        {
            get => _disableGpuDropShadows;
            set => SetProperty(ref _disableGpuDropShadows, value);
        }

        // 9. About BlueDoc metadata
        public string AppName => "BlueDoc — Windows Bluetooth Doctor";
        public string AppVersion => "1.0.0 (Production Release)";
        public string CompanyName => "UNLEFT LLC";
        public string DeveloperName => "Golam Morshed Eashan";
        public string TargetPlatforms => "Windows 7 SP1, Windows 10, Windows 11 (x86 & x64)";

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ICommand SaveSettingsCommand { get; }
        public ICommand ClearDatabaseCommand { get; }

        public SettingsViewModel(SqliteDatabaseManager db = null, ISettingsRepository settingsRepo = null)
        {
            _db = db ?? new SqliteDatabaseManager();
            _settingsRepo = settingsRepo ?? new SqliteSettingsRepository(_db);

            DatabasePath = _db.DatabasePath;
            UpdateDatabaseSize();

            SaveSettingsCommand = new RelayCommand(async () => await SaveSettingsAsync());
            ClearDatabaseCommand = new RelayCommand(async () =>
            {
                var confirm = MessageBox.Show(
                    "Are you sure you want to clear all stored sessions, cached devices, and history from the SQLite database?",
                    "Clear Database",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirm == MessageBoxResult.Yes)
                {
                    try
                    {
                        var sessionRepo = new SqliteDiagnosticSessionRepository(_db);
                        var historyRepo = new SqliteConnectionHistoryRepository(_db);
                        var deviceRepo = new SqliteDeviceCacheRepository(_db);
                        var repairRepo = new SqliteRepairHistoryRepository(_db);

                        await sessionRepo.ClearAllSessionsAsync();
                        await historyRepo.ClearHistoryAsync();
                        await deviceRepo.ClearDevicesAsync();
                        await repairRepo.ClearRepairHistoryAsync();

                        UpdateDatabaseSize();
                        StatusMessage = "Database cleared successfully.";
                        MessageBox.Show("All local SQLite database records have been cleared.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        StatusMessage = $"Error clearing database: {ex.Message}";
                    }
                }
            });
        }

        public async Task LoadSettingsAsync()
        {
            try
            {
                Theme = await _settingsRepo.GetSettingAsync("AppTheme", "Dark (Default)");
                EnableAnimations = await _settingsRepo.GetBoolSettingAsync("EnableAnimations", true);
                StartWithWindows = await _settingsRepo.GetBoolSettingAsync("StartWithWindows", false);
                MinimizeToTray = await _settingsRepo.GetBoolSettingAsync("MinimizeToTray", true);
                EnableNotificationToasts = await _settingsRepo.GetBoolSettingAsync("EnableNotificationToasts", true);

                string timeoutStr = await _settingsRepo.GetSettingAsync("ScanTimeout", "10");
                if (int.TryParse(timeoutStr, out int st)) ScanTimeout = st;
                ContinuousScanningDefault = await _settingsRepo.GetBoolSettingAsync("ContinuousScanningDefault", false);
                AutoDeduplicateDevices = await _settingsRepo.GetBoolSettingAsync("AutoDeduplicateDevices", true);

                AutoReconnectTrustedDevices = await _settingsRepo.GetBoolSettingAsync("AutoReconnectTrustedDevices", true);
                AudioProfilePrioritization = await _settingsRepo.GetSettingAsync("AudioProfilePrioritization", "Balanced Automatic");

                string retentionStr = await _settingsRepo.GetSettingAsync("HistoryRetentionDays", "30");
                if (int.TryParse(retentionStr, out int hr)) HistoryRetentionDays = hr;
                AutomatedDiagnosticValidation = await _settingsRepo.GetBoolSettingAsync("AutomatedDiagnosticValidation", true);
                EtwLoggingLevel = await _settingsRepo.GetSettingAsync("EtwLoggingLevel", "Informational");

                LowResourceMode = await _settingsRepo.GetBoolSettingAsync("LowResourceMode", false);

                AnonymizeMacs = await _settingsRepo.GetBoolSettingAsync("AnonymizeMacs", false);
                StrictOfflineZeroCloudPolicy = await _settingsRepo.GetBoolSettingAsync("StrictOfflineZeroCloudPolicy", true);

                SoftwareRenderingMode = await _settingsRepo.GetBoolSettingAsync("SoftwareRenderingMode", false);
                LegacyWin7StackFallback = await _settingsRepo.GetBoolSettingAsync("LegacyWin7StackFallback", true);
                DisableGpuDropShadows = await _settingsRepo.GetBoolSettingAsync("DisableGpuDropShadows", true);

                UpdateDatabaseSize();
            }
            catch { }
        }

        public async Task SaveSettingsAsync()
        {
            try
            {
                await _settingsRepo.SetSettingAsync("AppTheme", Theme);
                await _settingsRepo.SetBoolSettingAsync("EnableAnimations", EnableAnimations);
                await _settingsRepo.SetBoolSettingAsync("StartWithWindows", StartWithWindows);
                await _settingsRepo.SetBoolSettingAsync("MinimizeToTray", MinimizeToTray);
                await _settingsRepo.SetBoolSettingAsync("EnableNotificationToasts", EnableNotificationToasts);

                await _settingsRepo.SetSettingAsync("ScanTimeout", ScanTimeout.ToString());
                await _settingsRepo.SetBoolSettingAsync("ContinuousScanningDefault", ContinuousScanningDefault);
                await _settingsRepo.SetBoolSettingAsync("AutoDeduplicateDevices", AutoDeduplicateDevices);

                await _settingsRepo.SetBoolSettingAsync("AutoReconnectTrustedDevices", AutoReconnectTrustedDevices);
                await _settingsRepo.SetSettingAsync("AudioProfilePrioritization", AudioProfilePrioritization);

                await _settingsRepo.SetSettingAsync("HistoryRetentionDays", HistoryRetentionDays.ToString());
                await _settingsRepo.SetBoolSettingAsync("AutomatedDiagnosticValidation", AutomatedDiagnosticValidation);
                await _settingsRepo.SetSettingAsync("EtwLoggingLevel", EtwLoggingLevel);

                await _settingsRepo.SetBoolSettingAsync("LowResourceMode", LowResourceMode);

                await _settingsRepo.SetBoolSettingAsync("AnonymizeMacs", AnonymizeMacs);
                await _settingsRepo.SetBoolSettingAsync("StrictOfflineZeroCloudPolicy", StrictOfflineZeroCloudPolicy);

                await _settingsRepo.SetBoolSettingAsync("SoftwareRenderingMode", SoftwareRenderingMode);
                await _settingsRepo.SetBoolSettingAsync("LegacyWin7StackFallback", LegacyWin7StackFallback);
                await _settingsRepo.SetBoolSettingAsync("DisableGpuDropShadows", DisableGpuDropShadows);

                ApplyRenderMode(SoftwareRenderingMode);

                StatusMessage = "Preferences saved successfully.";
                MessageBox.Show("All configuration preferences have been saved.", "Preferences Saved", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error saving settings: {ex.Message}";
            }
        }

        private void ApplyRenderMode(bool softwareOnly)
        {
            try
            {
                System.Windows.Media.RenderOptions.ProcessRenderMode = softwareOnly
                    ? System.Windows.Interop.RenderMode.SoftwareOnly
                    : System.Windows.Interop.RenderMode.Default;
            }
            catch { }
        }

        private void UpdateDatabaseSize()
        {
            try
            {
                if (File.Exists(DatabasePath))
                {
                    long bytes = new FileInfo(DatabasePath).Length;
                    DatabaseSize = $"{bytes / 1024.0:F1} KB";
                }
                else
                {
                    DatabaseSize = "0 KB (Pending creation)";
                }
            }
            catch
            {
                DatabaseSize = "Unavailable";
            }
        }
    }
}
