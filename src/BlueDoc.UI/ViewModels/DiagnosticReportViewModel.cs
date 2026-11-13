using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using BlueDoc.Application.Services;
using BlueDoc.Core.Models;
using BlueDoc.Infrastructure.Platform;
using BlueDoc.Infrastructure.Services;
using BlueDoc.UI.Common;
using Microsoft.Win32;

namespace BlueDoc.UI.ViewModels
{
    public class DiagnosticReportViewModel : ViewModelBase
    {
        private readonly AdapterService _adapterService;
        private readonly DiagnosticsService _diagnosticsService;
        private readonly EventAnalysisService _eventService;
        private readonly DeviceManagementService _deviceService;
        private readonly ReportService _reportService;

        private DiagnosticReport _currentReport;
        private string _reportPreviewText = string.Empty;
        private string _statusMessage = string.Empty;
        private bool _isGenerating;
        private string _selectedFormat = "HTML";

        public DiagnosticReport CurrentReport
        {
            get => _currentReport;
            set => SetProperty(ref _currentReport, value);
        }

        public string ReportPreviewText
        {
            get => _reportPreviewText;
            set => SetProperty(ref _reportPreviewText, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool IsGenerating
        {
            get => _isGenerating;
            set => SetProperty(ref _isGenerating, value);
        }

        public string SelectedFormat
        {
            get => _selectedFormat;
            set
            {
                if (SetProperty(ref _selectedFormat, value))
                {
                    UpdatePreviewForSelectedFormat();
                }
            }
        }

        public ICommand GenerateReportCommand { get; }
        public ICommand SaveReportCommand { get; }
        public ICommand CopyReportCommand { get; }

        public DiagnosticReportViewModel(
            AdapterService adapterService = null,
            DiagnosticsService diagnosticsService = null,
            EventAnalysisService eventService = null,
            DeviceManagementService deviceService = null,
            ReportService reportService = null)
        {
            _adapterService = adapterService ?? new AdapterService();
            _diagnosticsService = diagnosticsService ?? new DiagnosticsService();
            _eventService = eventService ?? new EventAnalysisService();
            _deviceService = deviceService ?? new DeviceManagementService();
            _reportService = reportService ?? new ReportService();

            GenerateReportCommand = new RelayCommand(async () => await GenerateReportAsync());
            SaveReportCommand = new RelayCommand(async () => await SaveReportToFileAsync(), () => CurrentReport != null);
            CopyReportCommand = new RelayCommand(() =>
            {
                if (!string.IsNullOrWhiteSpace(ReportPreviewText))
                {
                    try { Clipboard.SetText(ReportPreviewText); StatusMessage = "Report text copied to clipboard."; } catch { }
                }
            }, () => !string.IsNullOrWhiteSpace(ReportPreviewText));
        }

        public async Task GenerateReportAsync()
        {
            IsGenerating = true;
            StatusMessage = "Compiling system diagnostics, adapter specifications, and event records...";

            try
            {
                var osInfo = OperatingSystemInspector.GetOperatingSystemInfo();
                var adapter = await _adapterService.GetPrimaryAdapterAsync();
                var serviceInspector = new WindowsServiceInspector();
                var service = serviceInspector.GetServiceInfo("bthserv");
                var events = await _eventService.GetRecentBluetoothEventsAsync(25, 7);
                var devices = await _deviceService.GetPairedDevicesAsync();

                var session = _diagnosticsService.LastSession;
                if (session == null)
                {
                    // If full diagnostics was not run yet, perform quick diagnostics
                    session = await _diagnosticsService.RunDiagnosticsAsync();
                }

                CurrentReport = new DiagnosticReport
                {
                    ReportId = Guid.NewGuid(),
                    GeneratedAtUtc = DateTime.UtcNow,
                    OsVersion = osInfo,
                    Adapter = adapter,
                    Service = service,
                    Session = session,
                    RelevantEvents = events,
                    Devices = new List<BluetoothDeviceInfo>(devices),
                    ApplicationVersion = "1.0.0 (Production Release)",
                    OverallHealthSummary = session?.OverallStatus.ToString() ?? "Unknown"
                };

                UpdatePreviewForSelectedFormat();
                StatusMessage = $"Diagnostic report compiled successfully ({CurrentReport.Session?.Results?.Count ?? 0} tests evaluated).";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error generating report: {ex.Message}";
            }
            finally
            {
                IsGenerating = false;
            }
        }

        private void UpdatePreviewForSelectedFormat()
        {
            if (CurrentReport == null) return;

            string fmt = SelectedFormat?.ToUpperInvariant() ?? "HTML";
            switch (fmt)
            {
                case "TXT":
                    ReportPreviewText = _reportService.GeneratePlainText(CurrentReport);
                    break;
                case "JSON":
                    ReportPreviewText = _reportService.GenerateJson(CurrentReport);
                    break;
                case "PDF":
                    ReportPreviewText = "================================================================================\n" +
                                        "BLUEDOC EXECUTIVE PDF REPORT READY FOR EXPORT\n" +
                                        "================================================================================\n\n" +
                                        $"Report ID:       {CurrentReport.ReportId}\n" +
                                        $"Generated:       {CurrentReport.GeneratedAtUtc:yyyy-MM-dd HH:mm:ss} UTC\n" +
                                        $"Overall Status:  {CurrentReport.OverallHealthSummary}\n" +
                                        $"Format Standard: Adobe PDF-1.4 (Zero-Dependency Vector Output)\n" +
                                        $"Page Layout:     Letter (612 x 792 pt)\n" +
                                        $"Sections:        Hardware, Drivers, Services, 12 Diagnostic Tests, Event Log, Devices\n\n" +
                                        "Click 'Save Report As...' to generate and save the formatted PDF document locally.";
                    break;
                case "HTML":
                default:
                    ReportPreviewText = _reportService.GenerateHtml(CurrentReport);
                    break;
            }
        }

        private async Task SaveReportToFileAsync()
        {
            if (CurrentReport == null) return;

            var sfd = new SaveFileDialog();
            string ext = SelectedFormat.ToLowerInvariant();
            switch (ext)
            {
                case "pdf":
                    sfd.Filter = "PDF Document (*.pdf)|*.pdf";
                    sfd.FileName = $"BlueDoc_Report_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                    break;
                case "json":
                    sfd.Filter = "JSON Files (*.json)|*.json";
                    sfd.FileName = $"BlueDoc_Report_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                    break;
                case "txt":
                    sfd.Filter = "Text Files (*.txt)|*.txt";
                    sfd.FileName = $"BlueDoc_Report_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                    break;
                case "html":
                default:
                    sfd.Filter = "HTML Document (*.html)|*.html";
                    sfd.FileName = $"BlueDoc_Report_{DateTime.Now:yyyyMMdd_HHmmss}.html";
                    break;
            }

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    await _reportService.SaveReportToFileAsync(CurrentReport, sfd.FileName, ext);
                    StatusMessage = $"Report successfully saved to: {Path.GetFileName(sfd.FileName)}";
                    MessageBox.Show($"Diagnostic report saved successfully to:\n{sfd.FileName}", "Report Saved", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Failed to save report: {ex.Message}";
                    MessageBox.Show($"Error saving report file:\n{ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
