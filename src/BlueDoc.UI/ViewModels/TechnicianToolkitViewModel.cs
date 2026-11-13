using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using BlueDoc.Application.Cloud;
using BlueDoc.Application.KnowledgeBase;
using BlueDoc.Application.Services;
using BlueDoc.Core.Models;
using BlueDoc.Diagnostics.Benchmark;
using BlueDoc.UI.Common;

namespace BlueDoc.UI.ViewModels
{
    public class TechnicianToolkitViewModel : ViewModelBase
    {
        private readonly BluetoothBenchmarkEngine _benchmarkEngine;
        private readonly OfflineKnowledgeBaseService _kbService;
        private readonly ISaasTelemetryService _saasService;
        private readonly AdapterService _adapterService;
        private readonly DiagnosticsService _diagnosticsService;
        private readonly Action<string> _navigateAction;

        private int _selectedTabIndex;

        // Benchmark Properties
        private BenchmarkResult _benchmark;
        private bool _isBenchmarking;
        private string _benchmarkStatus = "Ready to run hardware & latency benchmark.";

        // Knowledge Base Properties
        private string _searchQuery = string.Empty;
        private string _selectedCategory = "All";
        private KnowledgeBaseArticle _selectedArticle;
        private ObservableCollection<KnowledgeBaseArticle> _filteredArticles;
        private ObservableCollection<string> _categories;

        // SaaS Fleet Telemetry Properties
        private string _endpointUrl = "https://api.bluedoc.unleft.com/v1/telemetry";
        private string _tenantId = "UNLEFT-CORP-01";
        private string _apiKey = string.Empty;
        private string _previewPayloadJson = string.Empty;
        private string _syncStatus = "SaaS Cloud Telemetry is strictly opt-in. Review outgoing payload before synchronization.";
        private bool _isSyncing;

        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set => SetProperty(ref _selectedTabIndex, value);
        }

        // Benchmark bindings
        public BenchmarkResult Benchmark
        {
            get => _benchmark;
            set => SetProperty(ref _benchmark, value);
        }

        public bool IsBenchmarking
        {
            get => _isBenchmarking;
            set => SetProperty(ref _isBenchmarking, value);
        }

        public string BenchmarkStatus
        {
            get => _benchmarkStatus;
            set => SetProperty(ref _benchmarkStatus, value);
        }

        // KB bindings
        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                if (SetProperty(ref _searchQuery, value))
                {
                    FilterKnowledgeBase();
                }
            }
        }

        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value))
                {
                    FilterKnowledgeBase();
                }
            }
        }

        public KnowledgeBaseArticle SelectedArticle
        {
            get => _selectedArticle;
            set => SetProperty(ref _selectedArticle, value);
        }

        public ObservableCollection<KnowledgeBaseArticle> FilteredArticles
        {
            get => _filteredArticles;
            set => SetProperty(ref _filteredArticles, value);
        }

        public ObservableCollection<string> Categories
        {
            get => _categories;
            set => SetProperty(ref _categories, value);
        }

        // SaaS bindings
        public string EndpointUrl
        {
            get => _endpointUrl;
            set => SetProperty(ref _endpointUrl, value);
        }

        public string TenantId
        {
            get => _tenantId;
            set => SetProperty(ref _tenantId, value);
        }

        public string ApiKey
        {
            get => _apiKey;
            set => SetProperty(ref _apiKey, value);
        }

        public string PreviewPayloadJson
        {
            get => _previewPayloadJson;
            set => SetProperty(ref _previewPayloadJson, value);
        }

        public string SyncStatus
        {
            get => _syncStatus;
            set => SetProperty(ref _syncStatus, value);
        }

        public bool IsSyncing
        {
            get => _isSyncing;
            set => SetProperty(ref _isSyncing, value);
        }

        // Commands
        public ICommand RunBenchmarkCommand { get; }
        public ICommand FilterCategoryCommand { get; }
        public ICommand ExecuteRepairShortcutCommand { get; }
        public ICommand GeneratePayloadPreviewCommand { get; }
        public ICommand SyncFleetTelemetryCommand { get; }

        public TechnicianToolkitViewModel(
            AdapterService adapterService = null,
            DiagnosticsService diagnosticsService = null,
            Action<string> navigateAction = null)
        {
            _adapterService = adapterService ?? new AdapterService();
            _diagnosticsService = diagnosticsService ?? new DiagnosticsService();
            _navigateAction = navigateAction;

            _benchmarkEngine = new BluetoothBenchmarkEngine();
            _kbService = new OfflineKnowledgeBaseService();
            _saasService = new SaasTelemetryService();

            var cats = new ObservableCollection<string> { "All" };
            foreach (var c in _kbService.GetCategories()) cats.Add(c);
            Categories = cats;

            FilteredArticles = new ObservableCollection<KnowledgeBaseArticle>(_kbService.GetAllArticles());
            SelectedArticle = FilteredArticles.FirstOrDefault();

            RunBenchmarkCommand = new RelayCommand(async () => await RunBenchmarkAsync());
            FilterCategoryCommand = new RelayCommand(param => SelectedCategory = param?.ToString() ?? "All");
            ExecuteRepairShortcutCommand = new RelayCommand(param =>
            {
                if (_navigateAction != null)
                {
                    _navigateAction("Safe Repair Center");
                }
            });

            GeneratePayloadPreviewCommand = new RelayCommand(async () => await GeneratePayloadPreviewAsync());
            SyncFleetTelemetryCommand = new RelayCommand(async () => await SyncFleetTelemetryAsync());
        }

        public async Task RunBenchmarkAsync()
        {
            if (IsBenchmarking) return;

            IsBenchmarking = true;
            BenchmarkStatus = "Running SetupAPI enumeration, SCM turnaround, and radio inquiries...";

            try
            {
                Benchmark = await _benchmarkEngine.RunBenchmarkAsync();
                BenchmarkStatus = $"Benchmark completed: Score {Benchmark.OverallScore:F1}/100 [{Benchmark.Rating}].";
            }
            catch (Exception ex)
            {
                BenchmarkStatus = $"Benchmark execution error: {ex.Message}";
            }
            finally
            {
                IsBenchmarking = false;
            }
        }

        public void FilterKnowledgeBase()
        {
            var results = _kbService.Search(SearchQuery);
            if (!string.IsNullOrEmpty(SelectedCategory) && SelectedCategory != "All")
            {
                results = results.Where(a => string.Equals(a.Category, SelectedCategory, StringComparison.OrdinalIgnoreCase));
            }

            FilteredArticles = new ObservableCollection<KnowledgeBaseArticle>(results);
            if (!FilteredArticles.Contains(SelectedArticle))
            {
                SelectedArticle = FilteredArticles.FirstOrDefault();
            }
        }

        public async Task GeneratePayloadPreviewAsync()
        {
            SyncStatus = "Generating anonymized fleet diagnostic payload...";
            try
            {
                var adapter = await _adapterService.GetPrimaryAdapterAsync();
                var session = await _diagnosticsService.RunDiagnosticsAsync();
                var bench = Benchmark ?? await _benchmarkEngine.RunBenchmarkAsync();

                var payload = await _saasService.BuildPayloadAsync(adapter, session, bench);
                PreviewPayloadJson = _saasService.ExportPayloadJson(payload);
                SyncStatus = "Fleet payload generated successfully. Review the JSON below before transmitting.";
            }
            catch (Exception ex)
            {
                SyncStatus = $"Error generating fleet payload: {ex.Message}";
            }
        }

        public async Task SyncFleetTelemetryAsync()
        {
            if (IsSyncing) return;

            if (string.IsNullOrWhiteSpace(PreviewPayloadJson))
            {
                await GeneratePayloadPreviewAsync();
            }

            IsSyncing = true;
            SyncStatus = $"Transmitting payload to SaaS endpoint: {EndpointUrl}...";

            try
            {
                var adapter = await _adapterService.GetPrimaryAdapterAsync();
                var session = await _diagnosticsService.RunDiagnosticsAsync();
                var bench = Benchmark ?? await _benchmarkEngine.RunBenchmarkAsync();

                var payload = await _saasService.BuildPayloadAsync(adapter, session, bench);
                var res = await _saasService.SyncTelemetryAsync(payload, EndpointUrl, TenantId, ApiKey);

                if (res.Success)
                {
                    SyncStatus = $"✓ {res.Message}";
                }
                else
                {
                    SyncStatus = $"Notice: {res.Message}";
                }
            }
            catch (Exception ex)
            {
                SyncStatus = $"Fleet sync failed: {ex.Message}";
            }
            finally
            {
                IsSyncing = false;
            }
        }
    }
}
