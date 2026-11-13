using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using BlueDoc.Application.Services;
using BlueDoc.Core.Models;
using BlueDoc.Infrastructure.Platform;
using BlueDoc.UI.Common;
using Microsoft.Win32;

namespace BlueDoc.UI.ViewModels
{
    public class CompatibilityViewModel : ViewModelBase
    {
        private readonly AdapterService _adapterService;
        private readonly CompatibilityService _compatibilityService;
        private CompatibilityAssessment _assessment;
        private BluetoothAdapterInfo _primaryAdapter;

        public ObservableCollection<CompatibilitySubsystemItem> Subsystems { get; } = new ObservableCollection<CompatibilitySubsystemItem>();

        public OsVersionInfo OsVersion => _assessment?.OsVersion;

        public BluetoothAdapterInfo PrimaryAdapter
        {
            get => _primaryAdapter;
            set => SetProperty(ref _primaryAdapter, value);
        }

        public string OverallVerdict => _assessment?.OverallVerdict ?? string.Empty;
        public string VerdictColor => _assessment?.VerdictColor ?? "#10B981";
        public string GuidanceNotes => _assessment?.GuidanceNotes ?? string.Empty;

        public ICommand ExportReportCommand { get; }
        public ICommand CopyReportCommand { get; }

        public CompatibilityViewModel(AdapterService adapterService = null, CompatibilityService compatibilityService = null)
        {
            _adapterService = adapterService ?? new AdapterService();
            _compatibilityService = compatibilityService ?? new CompatibilityService();

            RefreshAssessment();

            ExportReportCommand = new RelayCommand(ExportCompatibilityReport);
            CopyReportCommand = new RelayCommand(CopyCompatibilityReport);
        }

        public void RefreshAssessment()
        {
            _assessment = _compatibilityService.EvaluateCompatibility();
            Subsystems.Clear();
            foreach (var item in _assessment.Subsystems)
            {
                Subsystems.Add(item);
            }
            OnPropertyChanged(nameof(OsVersion));
            OnPropertyChanged(nameof(OverallVerdict));
            OnPropertyChanged(nameof(VerdictColor));
            OnPropertyChanged(nameof(GuidanceNotes));
        }

        public async Task LoadAdapterAsync()
        {
            try
            {
                PrimaryAdapter = await _adapterService.GetPrimaryAdapterAsync();
            }
            catch { }
        }

        private void CopyCompatibilityReport()
        {
            if (_assessment == null) return;
            string report = _compatibilityService.GenerateReportText(_assessment);
            try
            {
                Clipboard.SetText(report);
                MessageBox.Show("Compatibility report copied to clipboard.", "BlueDoc", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch { }
        }

        private void ExportCompatibilityReport()
        {
            if (_assessment == null) return;

            var dialog = new SaveFileDialog
            {
                Filter = "Text Document (*.txt)|*.txt|All Files (*.*)|*.*",
                FileName = $"bluedoc_compatibility_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
                Title = "Export Platform Compatibility Report"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    string text = _compatibilityService.GenerateReportText(_assessment);
                    File.WriteAllText(dialog.FileName, text, Encoding.UTF8);
                    MessageBox.Show($"Compatibility report saved to:\n{dialog.FileName}", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to save report: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
