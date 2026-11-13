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

        private string _theme = "Dark (Default)";
        private int _scanTimeout = 10;
        private int _historyRetentionDays = 30;
        private bool _anonymizeMacs;
        private bool _softwareRenderingMode;
        private bool _lowResourceMode;
        private string _databasePath;
        private string _databaseSize = "Calculating...";
        private string _statusMessage = string.Empty;

        public string Theme
        {
            get => _theme;
            set => SetProperty(ref _theme, value);
        }

        public int ScanTimeout
        {
            get => _scanTimeout;
            set => SetProperty(ref _scanTimeout, value);
        }

        public int HistoryRetentionDays
        {
            get => _historyRetentionDays;
            set => SetProperty(ref _historyRetentionDays, value);
        }

        public bool AnonymizeMacs
        {
            get => _anonymizeMacs;
            set => SetProperty(ref _anonymizeMacs, value);
        }

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

        public bool LowResourceMode
        {
            get => _lowResourceMode;
            set => SetProperty(ref _lowResourceMode, value);
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
                string timeoutStr = await _settingsRepo.GetSettingAsync("ScanTimeout", "10");
                if (int.TryParse(timeoutStr, out int st)) ScanTimeout = st;
                string retentionStr = await _settingsRepo.GetSettingAsync("HistoryRetentionDays", "30");
                if (int.TryParse(retentionStr, out int hr)) HistoryRetentionDays = hr;
                AnonymizeMacs = await _settingsRepo.GetBoolSettingAsync("AnonymizeMacs", false);
                SoftwareRenderingMode = await _settingsRepo.GetBoolSettingAsync("SoftwareRenderingMode", false);
                LowResourceMode = await _settingsRepo.GetBoolSettingAsync("LowResourceMode", false);

                UpdateDatabaseSize();
            }
            catch { }
        }

        public async Task SaveSettingsAsync()
        {
            try
            {
                await _settingsRepo.SetSettingAsync("AppTheme", Theme);
                await _settingsRepo.SetSettingAsync("ScanTimeout", ScanTimeout.ToString());
                await _settingsRepo.SetSettingAsync("HistoryRetentionDays", HistoryRetentionDays.ToString());
                await _settingsRepo.SetBoolSettingAsync("AnonymizeMacs", AnonymizeMacs);
                await _settingsRepo.SetBoolSettingAsync("SoftwareRenderingMode", SoftwareRenderingMode);
                await _settingsRepo.SetBoolSettingAsync("LowResourceMode", LowResourceMode);

                ApplyRenderMode(SoftwareRenderingMode);

                StatusMessage = "Settings saved successfully.";
                MessageBox.Show("Settings have been saved.", "Preferences Saved", MessageBoxButton.OK, MessageBoxImage.Information);
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
