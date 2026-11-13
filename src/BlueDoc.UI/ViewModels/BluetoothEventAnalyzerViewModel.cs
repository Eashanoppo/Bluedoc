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
    public class BluetoothEventAnalyzerViewModel : ViewModelBase
    {
        private readonly EventAnalysisService _eventService;
        private List<BluetoothEventRecord> _allEvents = new List<BluetoothEventRecord>();
        private BluetoothEventRecord _selectedEvent;
        private EventCorrelationResult _correlationResult;
        private bool _isLoading;
        private string _selectedSeverity = "All";
        private string _searchFilter = string.Empty;
        private int _errorCount;
        private int _warningCount;
        private int _infoCount;

        public ObservableCollection<BluetoothEventRecord> FilteredEvents { get; } = new ObservableCollection<BluetoothEventRecord>();

        public BluetoothEventRecord SelectedEvent
        {
            get => _selectedEvent;
            set => SetProperty(ref _selectedEvent, value);
        }

        public EventCorrelationResult CorrelationResult
        {
            get => _correlationResult;
            set => SetProperty(ref _correlationResult, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string SelectedSeverity
        {
            get => _selectedSeverity;
            set
            {
                if (SetProperty(ref _selectedSeverity, value))
                {
                    ApplyFilters();
                }
            }
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

        public int ErrorCount
        {
            get => _errorCount;
            private set => SetProperty(ref _errorCount, value);
        }

        public int WarningCount
        {
            get => _warningCount;
            private set => SetProperty(ref _warningCount, value);
        }

        public int InfoCount
        {
            get => _infoCount;
            private set => SetProperty(ref _infoCount, value);
        }

        public ICommand RefreshCommand { get; }
        public ICommand CopyEventDetailsCommand { get; }
        public ICommand ExportAuditLogCommand { get; }

        public BluetoothEventAnalyzerViewModel(EventAnalysisService eventService = null)
        {
            _eventService = eventService ?? new EventAnalysisService();

            RefreshCommand = new RelayCommand(async () => await LoadEventsAsync());
            CopyEventDetailsCommand = new RelayCommand(CopySelectedEventDetails, () => SelectedEvent != null);
            ExportAuditLogCommand = new RelayCommand(ExportAuditLog, () => FilteredEvents.Count > 0);
        }

        public async Task LoadEventsAsync()
        {
            IsLoading = true;
            try
            {
                _allEvents = await _eventService.GetRecentBluetoothEventsAsync(100, 14);

                ErrorCount = _allEvents.Count(e => string.Equals(e.LevelDisplayName, "Error", StringComparison.OrdinalIgnoreCase));
                WarningCount = _allEvents.Count(e => string.Equals(e.LevelDisplayName, "Warning", StringComparison.OrdinalIgnoreCase));
                InfoCount = _allEvents.Count(e => string.Equals(e.LevelDisplayName, "Information", StringComparison.OrdinalIgnoreCase));

                CorrelationResult = _eventService.CorrelateWithDiagnostics(null, _allEvents);

                ApplyFilters();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ApplyFilters()
        {
            FilteredEvents.Clear();

            var query = _allEvents.AsEnumerable();

            if (!string.Equals(SelectedSeverity, "All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(e => string.Equals(e.LevelDisplayName, SelectedSeverity, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(SearchFilter))
            {
                string term = SearchFilter.Trim().ToLowerInvariant();
                query = query.Where(e =>
                    e.ProviderName.ToLowerInvariant().Contains(term) ||
                    e.EventId.ToString().Contains(term) ||
                    e.HumanAnalysis.ToLowerInvariant().Contains(term) ||
                    e.RawMessage.ToLowerInvariant().Contains(term));
            }

            foreach (var item in query)
            {
                FilteredEvents.Add(item);
            }

            if (FilteredEvents.Count > 0)
            {
                SelectedEvent = FilteredEvents[0];
            }
            else
            {
                SelectedEvent = null;
            }
        }

        private void CopySelectedEventDetails()
        {
            if (SelectedEvent == null) return;
            string text = $"Event ID: {SelectedEvent.EventId}\nProvider: {SelectedEvent.ProviderName}\nTime: {SelectedEvent.LocalTimeDisplay}\nSeverity: {SelectedEvent.LevelDisplayName}\nOccurrences: {SelectedEvent.OccurrenceCount}\nAnalysis: {SelectedEvent.HumanAnalysis}\nPossible Cause: {SelectedEvent.PossibleCause}\nRecommendation: {SelectedEvent.RecommendedTroubleshooting}\nRaw Message: {SelectedEvent.RawMessage}";
            try
            {
                Clipboard.SetText(text);
                MessageBox.Show("Event details copied to clipboard.", "BlueDoc", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch { }
        }

        private void ExportAuditLog()
        {
            if (FilteredEvents.Count == 0) return;

            var dialog = new SaveFileDialog
            {
                Filter = "CSV Spreadsheet (*.csv)|*.csv|All Files (*.*)|*.*",
                FileName = $"bluedoc_event_audit_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
                Title = "Export Bluetooth Event Audit Log"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    string csv = _eventService.GenerateCsvAuditLog(FilteredEvents);
                    File.WriteAllText(dialog.FileName, csv, Encoding.UTF8);
                    MessageBox.Show($"Event audit log successfully exported to:\n{dialog.FileName}\n\nTotal records: {FilteredEvents.Count}", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to export audit log: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
