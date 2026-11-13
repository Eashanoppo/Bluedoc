using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
using BlueDoc.Application.Services;
using BlueDoc.Infrastructure.Audio;
using BlueDoc.Infrastructure.Diagnostics;
using BlueDoc.Infrastructure.Drivers;
using BlueDoc.UI.Common;

namespace BlueDoc.UI.ViewModels
{
    public class AudioDiagnosticsViewModel : ViewModelBase
    {
        private readonly BluetoothAudioInspector _audioInspector;
        private readonly DriverComparisonService _driverComparisonService;
        private readonly HciTraceAssistant _traceAssistant;
        private readonly AdapterService _adapterService;

        private int _selectedTabIndex;

        // Audio Tab Bindings
        private BluetoothAudioAnalysisResult _audioAnalysis;
        private ObservableCollection<BluetoothAudioEndpoint> _audioEndpoints;
        private bool _isAnalyzingAudio;

        // Driver Comparison Tab Bindings
        private DriverComparisonResult _driverComparison;
        private bool _isComparingDrivers;

        // HCI Trace Tab Bindings
        private HciTraceSession _traceSession;
        private string _wiresharkGuide = string.Empty;
        private bool _isTracingActive;

        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set => SetProperty(ref _selectedTabIndex, value);
        }

        public BluetoothAudioAnalysisResult AudioAnalysis
        {
            get => _audioAnalysis;
            set => SetProperty(ref _audioAnalysis, value);
        }

        public ObservableCollection<BluetoothAudioEndpoint> AudioEndpoints
        {
            get => _audioEndpoints;
            set => SetProperty(ref _audioEndpoints, value);
        }

        public bool IsAnalyzingAudio
        {
            get => _isAnalyzingAudio;
            set => SetProperty(ref _isAnalyzingAudio, value);
        }

        public DriverComparisonResult DriverComparison
        {
            get => _driverComparison;
            set => SetProperty(ref _driverComparison, value);
        }

        public bool IsComparingDrivers
        {
            get => _isComparingDrivers;
            set => SetProperty(ref _isComparingDrivers, value);
        }

        public HciTraceSession TraceSession
        {
            get => _traceSession;
            set => SetProperty(ref _traceSession, value);
        }

        public string WiresharkGuide
        {
            get => _wiresharkGuide;
            set => SetProperty(ref _wiresharkGuide, value);
        }

        public bool IsTracingActive
        {
            get => _isTracingActive;
            set => SetProperty(ref _isTracingActive, value);
        }

        public ICommand RefreshAudioCommand { get; }
        public ICommand CheckDriverVersionCommand { get; }
        public ICommand OpenManufacturerPortalCommand { get; }
        public ICommand StartTraceCommand { get; }
        public ICommand StopTraceCommand { get; }

        public AudioDiagnosticsViewModel(AdapterService adapterService = null)
        {
            _adapterService = adapterService ?? new AdapterService();
            _audioInspector = new BluetoothAudioInspector();
            _driverComparisonService = new DriverComparisonService();
            _traceAssistant = new HciTraceAssistant();

            TraceSession = _traceAssistant.CurrentSession;
            AudioEndpoints = new ObservableCollection<BluetoothAudioEndpoint>();

            RefreshAudioCommand = new RelayCommand(async () => await LoadAudioAnalysisAsync());
            CheckDriverVersionCommand = new RelayCommand(async () => await CompareDriverAsync());
            OpenManufacturerPortalCommand = new RelayCommand(param =>
            {
                var url = DriverComparison?.OfficialDownloadUrl;
                if (!string.IsNullOrEmpty(url))
                {
                    try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); } catch { }
                }
            });

            StartTraceCommand = new RelayCommand(async () => await StartTraceAsync());
            StopTraceCommand = new RelayCommand(async () => await StopTraceAsync());

            // Initial auto-load
            _ = LoadAllDataAsync();
        }

        public async Task LoadAllDataAsync()
        {
            await LoadAudioAnalysisAsync();
            await CompareDriverAsync();
        }

        public Task LoadAudioAnalysisAsync()
        {
            return Task.Run(() =>
            {
                IsAnalyzingAudio = true;
                try
                {
                    var analysis = _audioInspector.AnalyzeAudioSubsystem();
                    AudioAnalysis = analysis;
                    AudioEndpoints = new ObservableCollection<BluetoothAudioEndpoint>(analysis.Endpoints);
                }
                catch { }
                finally
                {
                    IsAnalyzingAudio = false;
                }
            });
        }

        public async Task CompareDriverAsync()
        {
            IsComparingDrivers = true;
            try
            {
                var adapter = await _adapterService.GetPrimaryAdapterAsync();
                DriverComparison = _driverComparisonService.CompareDriverVersion(adapter);
            }
            catch { }
            finally
            {
                IsComparingDrivers = false;
            }
        }

        public async Task StartTraceAsync()
        {
            TraceSession = await _traceAssistant.StartTraceAsync();
            IsTracingActive = TraceSession.IsActive;
            WiresharkGuide = _traceAssistant.GetWiresharkAnalysisGuide(TraceSession.TraceFilePath);
        }

        public async Task StopTraceAsync()
        {
            TraceSession = await _traceAssistant.StopTraceAsync();
            IsTracingActive = TraceSession.IsActive;
            WiresharkGuide = _traceAssistant.GetWiresharkAnalysisGuide(TraceSession.TraceFilePath);
        }
    }
}

