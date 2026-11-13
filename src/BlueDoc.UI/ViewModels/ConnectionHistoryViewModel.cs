using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using BlueDoc.Application.Services;
using BlueDoc.Core.Models;
using BlueDoc.UI.Common;

namespace BlueDoc.UI.ViewModels
{
    public class ConnectionHistoryViewModel : ViewModelBase
    {
        private readonly HistoryService _historyService;
        private ConnectionHistoryRecord _selectedRecord;
        private string _searchFilter = string.Empty;
        private bool _isLoading;
        private int _totalEvents;

        public ObservableCollection<ConnectionHistoryRecord> HistoryRecords { get; } = new ObservableCollection<ConnectionHistoryRecord>();

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
                    _ = LoadHistoryAsync();
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
        }

        public async Task LoadHistoryAsync()
        {
            IsLoading = true;
            try
            {
                var records = await _historyService.GetHistoryAsync(100, SearchFilter);
                HistoryRecords.Clear();
                foreach (var rec in records)
                {
                    HistoryRecords.Add(rec);
                }
                TotalEvents = HistoryRecords.Count;
                if (HistoryRecords.Count > 0)
                {
                    SelectedRecord = HistoryRecords[0];
                }
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
