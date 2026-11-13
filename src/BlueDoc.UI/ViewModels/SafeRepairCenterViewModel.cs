using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using BlueDoc.Application.Services;
using BlueDoc.Core.Models;
using BlueDoc.Infrastructure.Repairs;
using BlueDoc.UI.Common;
using Microsoft.Win32;

namespace BlueDoc.UI.ViewModels
{
    public class SafeRepairCenterViewModel : ViewModelBase
    {
        private readonly RepairService _repairService;
        private RepairActionDefinition _selectedRepair;
        private bool _isExecuting;
        private string _statusMessage = string.Empty;
        private string _lastVerificationResult = string.Empty;

        public ObservableCollection<RepairActionDefinition> AvailableRepairs { get; } = new ObservableCollection<RepairActionDefinition>();
        public ObservableCollection<RepairHistoryRecord> RepairHistory { get; } = new ObservableCollection<RepairHistoryRecord>();

        public RepairActionDefinition SelectedRepair
        {
            get => _selectedRepair;
            set => SetProperty(ref _selectedRepair, value);
        }

        public bool IsRunningAsAdmin => _repairService.IsRunningAsAdmin;

        public bool IsExecuting
        {
            get => _isExecuting;
            set => SetProperty(ref _isExecuting, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public string LastVerificationResult
        {
            get => _lastVerificationResult;
            set => SetProperty(ref _lastVerificationResult, value);
        }

        public ICommand ExecuteRepairCommand { get; }
        public ICommand RefreshHistoryCommand { get; }
        public ICommand ClearHistoryCommand { get; }
        public ICommand ExportHistoryCsvCommand { get; }
        public ICommand RestartAsAdminCommand { get; }

        public SafeRepairCenterViewModel(RepairService repairService = null)
        {
            _repairService = repairService ?? new RepairService();

            PopulateAvailableRepairs();

            ExecuteRepairCommand = new RelayCommand(async () => await ExecuteSelectedRepairAsync(), () => SelectedRepair != null && !IsExecuting);
            RefreshHistoryCommand = new RelayCommand(async () => await LoadHistoryAsync());
            ClearHistoryCommand = new RelayCommand(async () => await ClearHistoryAsync());
            ExportHistoryCsvCommand = new RelayCommand(ExportHistoryCsv, () => RepairHistory.Count > 0);
            RestartAsAdminCommand = new RelayCommand(RelaunchApplicationAsAdmin);
        }

        private void PopulateAvailableRepairs()
        {
            AvailableRepairs.Clear();
            var list = _repairService.GetAllRepairDefinitions();
            foreach (var r in list)
            {
                AvailableRepairs.Add(r);
            }

            if (AvailableRepairs.Count > 0)
            {
                SelectedRepair = AvailableRepairs[0];
            }
        }

        public async Task LoadHistoryAsync()
        {
            var history = await _repairService.GetRepairHistoryAsync(50);
            RepairHistory.Clear();
            foreach (var h in history)
            {
                RepairHistory.Add(h);
            }
        }

        private async Task ExecuteSelectedRepairAsync()
        {
            if (SelectedRepair == null) return;

            // Step 4: Check Permissions
            if (SelectedRepair.RequiresAdmin && !IsRunningAsAdmin)
            {
                var elevate = MessageBox.Show(
                    $"The action '{SelectedRepair.Title}' requires Administrator privileges.\n\nWould you like to restart BlueDoc as Administrator to execute this repair?",
                    "Administrator Privileges Required",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (elevate == MessageBoxResult.Yes)
                {
                    RelaunchApplicationAsAdmin();
                }
                return;
            }

            // Step 5: Explicit User Confirmation
            string confirmMsg = $"Safety Confirmation:\n\nAction: {SelectedRepair.Title}\nDescription: {SelectedRepair.Description}\nConsequences: {SelectedRepair.Consequences}\n\nDo you want to proceed with this safe repair operation?";
            var confirmResult = MessageBox.Show(confirmMsg, "Confirm Safe Repair", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirmResult != MessageBoxResult.Yes)
            {
                StatusMessage = "Repair cancelled by user.";
                return;
            }

            // If GuideDriverRollback, display guided instructions dialog
            if (SelectedRepair.Key == "GuideDriverRollback")
            {
                MessageBox.Show(
                    "Guided Driver Rollback & Recovery Steps:\n\n" +
                    "1. Windows Device Manager has been launched for you.\n" +
                    "2. Expand the 'Bluetooth' category.\n" +
                    "3. Right-click your primary Bluetooth adapter and select 'Properties'.\n" +
                    "4. Switch to the 'Driver' tab.\n" +
                    "5. Click 'Roll Back Driver' to revert to the previous verified driver.\n" +
                    "6. If Roll Back is unavailable, select 'Update driver' -> 'Browse my computer' -> 'Let me pick from a list' to choose a manufacturer driver.\n" +
                    "7. Click OK and restart if prompted.",
                    "Driver Rollback Guidance",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }

            // Step 6 & 7: Execute & Verify
            IsExecuting = true;
            StatusMessage = $"Executing repair: {SelectedRepair.Title}...";

            try
            {
                var record = await _repairService.ExecuteRepairAsync(SelectedRepair.Key);
                LastVerificationResult = record.VerificationResult;

                if (record.Status == "Success")
                {
                    StatusMessage = $"Repair '{SelectedRepair.Title}' completed successfully.";
                    MessageBox.Show($"Repair completed successfully.\n\nVerification: {record.VerificationResult}", "Repair Succeeded", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    StatusMessage = $"Repair did not succeed: {record.ErrorMessage}";
                    MessageBox.Show($"Repair did not succeed.\n\nError: {record.ErrorMessage}\nVerification: {record.VerificationResult}", "Repair Notice", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                await LoadHistoryAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Unexpected error during repair: {ex.Message}";
            }
            finally
            {
                IsExecuting = false;
            }
        }

        private async Task ClearHistoryAsync()
        {
            var res = MessageBox.Show(
                "Are you sure you want to clear your local repair audit history?\nThis action cannot be undone.",
                "Confirm Clear History",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (res == MessageBoxResult.Yes)
            {
                await _repairService.ClearRepairHistoryAsync();
                RepairHistory.Clear();
                StatusMessage = "Repair history cleared.";
            }
        }

        private void ExportHistoryCsv()
        {
            if (RepairHistory.Count == 0) return;

            var dialog = new SaveFileDialog
            {
                Filter = "CSV Spreadsheet (*.csv)|*.csv|All Files (*.*)|*.*",
                FileName = $"bluedoc_repair_history_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
                Title = "Export Repair Audit Trail"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("Timestamp (UTC),Local Time,Action Title,Target Component,Required Admin,Status,Verification Details,Error Message");

                    foreach (var h in RepairHistory)
                    {
                        string utc = h.TimestampUtc.ToString("o");
                        string local = EscapeCsv(h.LocalTimestampDisplay);
                        string title = EscapeCsv(h.ActionTitle);
                        string target = EscapeCsv(h.TargetComponent);
                        string admin = h.RequiredAdmin ? "Yes" : "No";
                        string status = EscapeCsv(h.Status);
                        string verif = EscapeCsv(h.VerificationResult);
                        string err = EscapeCsv(h.ErrorMessage);

                        sb.AppendLine($"{utc},{local},{title},{target},{admin},{status},{verif},{err}");
                    }

                    File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);
                    MessageBox.Show($"Repair audit trail saved to:\n{dialog.FileName}", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to export repair CSV: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private static string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }
            return value;
        }

        private void RelaunchApplicationAsAdmin()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = System.Reflection.Assembly.GetEntryAssembly().Location,
                    UseShellExecute = true,
                    Verb = "runas"
                };
                Process.Start(psi);
                System.Windows.Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to elevate application:\n{ex.Message}", "Elevation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
