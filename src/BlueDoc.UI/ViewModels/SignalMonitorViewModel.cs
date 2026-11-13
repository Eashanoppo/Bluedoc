using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using BlueDoc.Application.Services;
using BlueDoc.Core.Models;
using BlueDoc.UI.Common;
using Microsoft.Win32;

namespace BlueDoc.UI.ViewModels
{
    public class SignalMonitorViewModel : ViewModelBase
    {
        private readonly DeviceDiscoveryService _discoveryService;
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private readonly DispatcherTimer _timer;

        private bool _isMonitoring;
        private bool _continuousMode = true;
        private string _elapsedTimeDisplay = "00:00";
        private int _totalObserved;
        private int _devicesWithRssiCount;
        private int _devicesWithoutRssiCount;
        private string _averageRssiDisplay = "N/A";
        private string _monitorStatus = "Idle. Click 'Start Continuous Monitor' to capture live telemetry.";
        private string _searchFilter = string.Empty;

        private readonly List<BluetoothDeviceInfo> _allMonitoredDevices = new List<BluetoothDeviceInfo>();

        public ObservableCollection<BluetoothDeviceInfo> FilteredDevices { get; } = new ObservableCollection<BluetoothDeviceInfo>();

        public bool IsMonitoring
        {
            get => _isMonitoring;
            set => SetProperty(ref _isMonitoring, value);
        }

        public bool ContinuousMode
        {
            get => _continuousMode;
            set => SetProperty(ref _continuousMode, value);
        }

        public string ElapsedTimeDisplay
        {
            get => _elapsedTimeDisplay;
            set => SetProperty(ref _elapsedTimeDisplay, value);
        }

        public int TotalObserved
        {
            get => _totalObserved;
            set => SetProperty(ref _totalObserved, value);
        }

        public int DevicesWithRssiCount
        {
            get => _devicesWithRssiCount;
            set => SetProperty(ref _devicesWithRssiCount, value);
        }

        public int DevicesWithoutRssiCount
        {
            get => _devicesWithoutRssiCount;
            set => SetProperty(ref _devicesWithoutRssiCount, value);
        }

        public string AverageRssiDisplay
        {
            get => _averageRssiDisplay;
            set => SetProperty(ref _averageRssiDisplay, value);
        }

        public string MonitorStatus
        {
            get => _monitorStatus;
            set => SetProperty(ref _monitorStatus, value);
        }

        public string SearchFilter
        {
            get => _searchFilter;
            set
            {
                if (SetProperty(ref _searchFilter, value))
                {
                    ApplyFilter();
                }
            }
        }

        public ICommand StartMonitorCommand { get; }
        public ICommand StopMonitorCommand { get; }
        public ICommand ClearListCommand { get; }
        public ICommand ExportCsvCommand { get; }

        public SignalMonitorViewModel(DeviceDiscoveryService discoveryService = null)
        {
            _discoveryService = discoveryService ?? new DeviceDiscoveryService();

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (s, e) =>
            {
                ElapsedTimeDisplay = _stopwatch.Elapsed.ToString(@"mm\:ss");
            };

            _discoveryService.DeviceDiscovered += OnDeviceCaptured;

            StartMonitorCommand = new RelayCommand(async () => await StartMonitoringAsync(), () => !IsMonitoring);
            StopMonitorCommand = new RelayCommand(StopMonitoring, () => IsMonitoring);
            ClearListCommand = new RelayCommand(() =>
            {
                lock (_allMonitoredDevices)
                {
                    _allMonitoredDevices.Clear();
                }
                FilteredDevices.Clear();
                TotalObserved = 0;
                DevicesWithRssiCount = 0;
                DevicesWithoutRssiCount = 0;
                AverageRssiDisplay = "N/A";
            });

            ExportCsvCommand = new RelayCommand(ExportTelemetryCsv, () => _allMonitoredDevices.Count > 0);
        }

        public async Task StartMonitoringAsync()
        {
            if (IsMonitoring) return;

            IsMonitoring = true;
            MonitorStatus = ContinuousMode 
                ? "Continuous signal monitor active: listening for Bluetooth broadcasts..." 
                : "Single-cycle monitor active: scanning for 15 seconds...";
            _stopwatch.Restart();
            _timer.Start();

            try
            {
                while (IsMonitoring)
                {
                    await _discoveryService.StartScanningAsync(15);
                    if (!ContinuousMode || !IsMonitoring) break;
                    await Task.Delay(1000);
                }
                MonitorStatus = "Signal monitor stopped.";
            }
            catch (Exception ex)
            {
                MonitorStatus = $"Monitor stopped: {ex.Message}";
            }
            finally
            {
                StopMonitoring();
            }
        }

        public void StopMonitoring()
        {
            if (!IsMonitoring) return;
            _discoveryService.StopScanning();
            _stopwatch.Stop();
            _timer.Stop();
            IsMonitoring = false;
        }

        private void OnDeviceCaptured(object sender, BluetoothDeviceInfo device)
        {
            System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
            {
                lock (_allMonitoredDevices)
                {
                    var existing = _allMonitoredDevices.FirstOrDefault(d => d.BluetoothAddress == device.BluetoothAddress);
                    if (existing != null)
                    {
                        existing.LastSeen = device.LastSeen;
                        if (device.Rssi.HasValue) existing.Rssi = device.Rssi;
                        if (!string.IsNullOrWhiteSpace(device.DeviceName)) existing.DeviceName = device.DeviceName;
                    }
                    else
                    {
                        _allMonitoredDevices.Insert(0, device);
                    }

                    UpdateMetrics();
                    ApplyFilter();
                }
            });
        }

        private void UpdateMetrics()
        {
            TotalObserved = _allMonitoredDevices.Count;
            var withRssi = _allMonitoredDevices.Where(d => d.Rssi.HasValue).ToList();
            DevicesWithRssiCount = withRssi.Count;
            DevicesWithoutRssiCount = TotalObserved - DevicesWithRssiCount;

            if (withRssi.Count > 0)
            {
                double avg = withRssi.Average(d => d.Rssi.Value);
                AverageRssiDisplay = $"{avg:F1} dBm";
            }
            else
            {
                AverageRssiDisplay = "N/A";
            }
        }

        private void ApplyFilter()
        {
            FilteredDevices.Clear();
            var query = _allMonitoredDevices.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchFilter))
            {
                string term = SearchFilter.Trim().ToLowerInvariant();
                query = query.Where(d =>
                    (d.DeviceName != null && d.DeviceName.ToLowerInvariant().Contains(term)) ||
                    (d.BluetoothAddress != null && d.BluetoothAddress.ToLowerInvariant().Contains(term)) ||
                    (d.CategoryName != null && d.CategoryName.ToLowerInvariant().Contains(term)));
            }

            foreach (var d in query)
            {
                FilteredDevices.Add(d);
            }
        }

        private void ExportTelemetryCsv()
        {
            if (_allMonitoredDevices.Count == 0) return;

            var dialog = new SaveFileDialog
            {
                Filter = "CSV Spreadsheet (*.csv)|*.csv|All Files (*.*)|*.*",
                FileName = $"bluedoc_signal_telemetry_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
                Title = "Export Signal & Discovery Telemetry"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("Device Name,Bluetooth Address,Category,RSSI (dBm),Signal Quality,Status,First Discovered (UTC),Last Seen (UTC),Instance ID");

                    lock (_allMonitoredDevices)
                    {
                        foreach (var d in _allMonitoredDevices)
                        {
                            string name = EscapeCsv(d.DisplayName);
                            string mac = EscapeCsv(d.BluetoothAddress);
                            string category = EscapeCsv(d.CategoryName);
                            string rssi = d.Rssi.HasValue ? d.Rssi.Value.ToString() : "Not Exposed";
                            string quality = EscapeCsv(d.SignalQualityBadge);
                            string status = EscapeCsv(d.StatusDisplay);
                            string firstSeen = d.FirstDiscovered.ToString("o");
                            string lastSeen = d.LastSeen.ToString("o");
                            string instanceId = EscapeCsv(d.InstanceId);

                            sb.AppendLine($"{name},{mac},{category},{rssi},{quality},{status},{firstSeen},{lastSeen},{instanceId}");
                        }
                    }

                    File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);
                    MessageBox.Show($"Telemetry successfully exported to:\n{dialog.FileName}\n\nTotal records: {_allMonitoredDevices.Count}", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to export CSV: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
    }
}
