using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using BlueDoc.Application.Services;
using BlueDoc.Core.Models;
using BlueDoc.UI.Common;
using Microsoft.Win32;

namespace BlueDoc.UI.ViewModels
{
    public class ConnectionHistoryViewModel : ViewModelBase
    {
        private readonly HistoryService _historyService;
        private List<ConnectionHistoryRecord> _rawRecords = new List<ConnectionHistoryRecord>();
        private ConnectionHistoryRecord _selectedRecord;
        private string _searchFilter = string.Empty;
        private string _selectedFilter = "All";
        private bool _isLoading;
        private int _totalEvents;

        public ObservableCollection<ConnectionHistoryRecord> HistoryRecords { get; } = new ObservableCollection<ConnectionHistoryRecord>();
        public ObservableCollection<BluetoothDeviceInfo> LifetimePairedDevices { get; } = new ObservableCollection<BluetoothDeviceInfo>();

        public int LifetimePairedCount => LifetimePairedDevices.Count;
        public bool HasLifetimePairedDevices => LifetimePairedDevices.Count > 0;

        public ConnectionHistoryRecord SelectedRecord
        {
            get => _selectedRecord;
            set => SetProperty(ref _selectedRecord, value);
        }

        public string SearchFilter
        {
            get => _searchFilter;
            set
            {
                if (SetProperty(ref _searchFilter, value))
                {
                    ApplyFilters();
                }
            }
        }

        public string SelectedFilter
        {
            get => _selectedFilter;
            set
            {
                if (SetProperty(ref _selectedFilter, value))
                {
                    ApplyFilters();
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public int TotalEvents
        {
            get => _totalEvents;
            set => SetProperty(ref _totalEvents, value);
        }

        public ICommand RefreshCommand { get; }
        public ICommand ClearHistoryCommand { get; }
        public ICommand ExportCsvCommand { get; }
        public ICommand SetFilterCommand { get; }
        public ICommand UnpairDeviceCommand { get; }

        public ConnectionHistoryViewModel(HistoryService historyService = null)
        {
            _historyService = historyService ?? new HistoryService();

            RefreshCommand = new RelayCommand(async () => await LoadHistoryAsync());
            ClearHistoryCommand = new RelayCommand(async () =>
            {
                var confirm = MessageBox.Show(
                    "Are you sure you want to clear all device connection history? This action cannot be undone.",
                    "Clear Connection History",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirm == MessageBoxResult.Yes)
                {
                    await _historyService.ClearAllHistoryAsync();
                    await LoadHistoryAsync();
                }
            });

            UnpairDeviceCommand = new RelayCommand(async param =>
            {
                if (param is BluetoothDeviceInfo dev)
                {
                    var confirm = MessageBox.Show(
                        $"Remove and unpair '{dev.DisplayName}' ({dev.BluetoothAddress}) from Windows?",
                        "Unpair Bluetooth Peripheral",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (confirm == MessageBoxResult.Yes)
                    {
                        var mgmt = new BlueDoc.Application.Services.DeviceManagementService();
                        bool ok = await mgmt.UnpairDeviceAsync(dev.BluetoothAddress);
                        if (ok)
                        {
                            LifetimePairedDevices.Remove(dev);
                            OnPropertyChanged(nameof(LifetimePairedCount));
                            OnPropertyChanged(nameof(HasLifetimePairedDevices));
                        }
                    }
                }
            });

            ExportCsvCommand = new RelayCommand(ExportCsv, () => HistoryRecords.Count > 0);
            SetFilterCommand = new RelayCommand(filter => SelectedFilter = filter?.ToString() ?? "All");
        }

        public async Task LoadHistoryAsync()
        {
            IsLoading = true;
            try
            {
                var pairedTask = Task.Run(() => BlueDoc.Infrastructure.Hardware.PnpBluetoothDeviceInspector.EnumeratePairedDevices());
                var historyTask = _historyService.GetHistoryAsync(250, null);

                await Task.WhenAll(pairedTask, historyTask);

                var lifetimeList = await pairedTask;
                _rawRecords = await historyTask;

                LifetimePairedDevices.Clear();
                foreach (var d in lifetimeList)
                {
                    LifetimePairedDevices.Add(d);
                }
                OnPropertyChanged(nameof(LifetimePairedCount));
                OnPropertyChanged(nameof(HasLifetimePairedDevices));

                TotalEvents = _rawRecords.Count;
                ApplyFilters();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ApplyFilters()
        {
            HistoryRecords.Clear();

            var query = _rawRecords.AsEnumerable();

            if (!string.Equals(SelectedFilter, "All", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(SelectedFilter, "Connected", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(r => r.IsSuccess && (r.EventType.IndexOf("Connected", StringComparison.OrdinalIgnoreCase) >= 0 || r.EventType.IndexOf("Pair", StringComparison.OrdinalIgnoreCase) >= 0));
                }
                else if (string.Equals(SelectedFilter, "Failed", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(r => !r.IsSuccess || r.EventType.IndexOf("Fail", StringComparison.OrdinalIgnoreCase) >= 0);
                }
                else if (string.Equals(SelectedFilter, "Discovered", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(r => r.EventType.IndexOf("Discover", StringComparison.OrdinalIgnoreCase) >= 0);
                }
            }

            if (!string.IsNullOrWhiteSpace(SearchFilter))
            {
                string term = SearchFilter.Trim().ToLowerInvariant();
                query = query.Where(r =>
                    (r.DeviceName != null && r.DeviceName.ToLowerInvariant().Contains(term)) ||
                    (r.DeviceAddress != null && r.DeviceAddress.ToLowerInvariant().Contains(term)) ||
                    (r.EventType != null && r.EventType.ToLowerInvariant().Contains(term)) ||
                    (r.Details != null && r.Details.ToLowerInvariant().Contains(term)));
            }

            foreach (var rec in query)
            {
                HistoryRecords.Add(rec);
            }

            if (HistoryRecords.Count > 0)
            {
                SelectedRecord = HistoryRecords[0];
            }
            else
            {
                SelectedRecord = null;
            }
        }

        private void ExportCsv()
        {
            if (HistoryRecords.Count == 0) return;

            var sfd = new SaveFileDialog
            {
                Filter = "CSV Spreadsheet (*.csv)|*.csv|All Files (*.*)|*.*",
                FileName = $"BlueDoc_Connection_History_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
                Title = "Export Connection History Audit Log"
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("Id,TimestampUtc,DeviceName,DeviceAddress,DeviceType,EventType,IsSuccess,Details");
                    foreach (var r in HistoryRecords)
                    {
                        string safeName = (r.DeviceName ?? string.Empty).Replace("\"", "\"\"");
                        string safeDetails = (r.Details ?? string.Empty).Replace("\"", "\"\"");
                        sb.AppendLine($"{r.Id},{r.TimestampUtc:O},\"{safeName}\",{r.DeviceAddress},{r.DeviceType},{r.EventType},{r.IsSuccess},\"{safeDetails}\"");
                    }

                    File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
                    MessageBox.Show($"Connection history exported successfully to:\n{sfd.FileName}\n\nTotal records: {HistoryRecords.Count}", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to export history: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
