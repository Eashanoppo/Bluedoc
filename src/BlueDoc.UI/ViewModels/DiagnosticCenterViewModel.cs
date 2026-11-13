using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using BlueDoc.Application.Services;
using BlueDoc.Core.Enums;
using BlueDoc.Core.Models;
using BlueDoc.UI.Common;

namespace BlueDoc.UI.ViewModels
{
    public class DiagnosticCenterViewModel : ViewModelBase
    {
        private readonly DiagnosticsService _diagnosticsService;
        private readonly Action<string> _navigateAction;

        private DiagnosticSession _currentSession;
        private DiagnosticResult _selectedResult;
        private bool _isRunning;
        private bool _isLoading;
        private bool _isRefreshing;
        private int _progressPercentage;
        private string _statusMessage = "Ready to run diagnostics.";
        private string _currentTestName = string.Empty;
        private int _completedTestsCount;
        private int _totalTestsCount = 12;
        private string _estimatedRemainingDisplay = string.Empty;
        private int _passedCount;
        private int _failedCount;
        private int _warningCount;

        private CancellationTokenSource _diagnosticsCts;

        public ObservableCollection<DiagnosticResult> Results { get; } = new ObservableCollection<DiagnosticResult>();

        public DiagnosticSession CurrentSession
        {
            get => _currentSession;
            set => SetProperty(ref _currentSession, value);
        }

        public DiagnosticResult SelectedResult
        {
            get => _selectedResult;
            set => SetProperty(ref _selectedResult, value);
        }

        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                if (SetProperty(ref _isRunning, value))
                {
                    OnPropertyChanged(nameof(CanRunDiagnostics));
                    OnPropertyChanged(nameof(CanCancelDiagnostics));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public bool CanRunDiagnostics => !IsRunning;
        public bool CanCancelDiagnostics => IsRunning;

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public bool IsRefreshing
        {
            get => _isRefreshing;
            set => SetProperty(ref _isRefreshing, value);
        }

        public int ProgressPercentage
        {
            get => _progressPercentage;
            set => SetProperty(ref _progressPercentage, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public string CurrentTestName
        {
            get => _currentTestName;
            set => SetProperty(ref _currentTestName, value);
        }

        public int CompletedTestsCount
        {
            get => _completedTestsCount;
            set
            {
                if (SetProperty(ref _completedTestsCount, value))
                {
                    OnPropertyChanged(nameof(TestsProgressDisplay));
                }
            }
        }

        public int TotalTestsCount
        {
            get => _totalTestsCount;
            set
            {
                if (SetProperty(ref _totalTestsCount, value))
                {
                    OnPropertyChanged(nameof(TestsProgressDisplay));
                }
            }
        }

        public string TestsProgressDisplay => $"{CompletedTestsCount} of {TotalTestsCount} tests completed";

        public string EstimatedRemainingDisplay
        {
            get => _estimatedRemainingDisplay;
            set => SetProperty(ref _estimatedRemainingDisplay, value);
        }

        public int PassedCount
        {
            get => _passedCount;
            set => SetProperty(ref _passedCount, value);
        }

        public int FailedCount
        {
            get => _failedCount;
            set => SetProperty(ref _failedCount, value);
        }

        public int WarningCount
        {
            get => _warningCount;
            set => SetProperty(ref _warningCount, value);
        }

        public ICommand RunFullDiagnosticsCommand { get; }
        public ICommand CancelDiagnosticsCommand { get; }
        public ICommand RunSingleTestCommand { get; }
        public ICommand NavigateToRepairCommand { get; }

        public DiagnosticCenterViewModel(DiagnosticsService diagnosticsService, Action<string> navigateAction = null)
        {
            _diagnosticsService = diagnosticsService ?? new DiagnosticsService();
            _navigateAction = navigateAction;

            InitializeTestCatalog();

            RunFullDiagnosticsCommand = new RelayCommand(async () => await ExecuteFullDiagnosticsAsync(), () => CanRunDiagnostics);
            CancelDiagnosticsCommand = new RelayCommand(() =>
            {
                if (_diagnosticsCts != null && !_diagnosticsCts.IsCancellationRequested)
                {
                    _diagnosticsCts.Cancel();
                    StatusMessage = "Cancelling diagnostic run...";
                    EstimatedRemainingDisplay = "Cancelled";
                }
            }, () => CanCancelDiagnostics);

            RunSingleTestCommand = new RelayCommand(async param =>
            {
                if (param is DiagnosticResult target)
                {
                    await ExecuteSingleTestAsync(target);
                }
                else if (param is string ruleId)
                {
                    var targetResult = Results.FirstOrDefault(r => r.RuleId == ruleId);
                    if (targetResult != null) await ExecuteSingleTestAsync(targetResult);
                }
            }, param => CanRunDiagnostics);

            NavigateToRepairCommand = new RelayCommand(() =>
            {
                _navigateAction?.Invoke("Safe Repair Center");
            });
        }

        public void InitializeTestCatalog()
        {
            IsLoading = true;
            try
            {
                Results.Clear();
                var initialTests = _diagnosticsService.GetInitialTestList();
                foreach (var test in initialTests)
                {
                    Results.Add(test);
                }

                TotalTestsCount = Results.Count > 0 ? Results.Count : 12;
                CompletedTestsCount = 0;

                if (Results.Count > 0)
                {
                    SelectedResult = Results[0];
                }
                UpdateStatusCounts();
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task ExecuteFullDiagnosticsAsync()
        {
            if (IsRunning) return;

            _diagnosticsCts = new CancellationTokenSource();
            IsRunning = true;
            ProgressPercentage = 0;
            CompletedTestsCount = 0;
            TotalTestsCount = Results.Count > 0 ? Results.Count : 12;
            StatusMessage = "Initializing diagnostic test pipeline...";
            CurrentTestName = "Preparing rules...";
            EstimatedRemainingDisplay = "Estimating...";

            var stopwatch = Stopwatch.StartNew();

            var progress = new Progress<DiagnosticResult>(result =>
            {
                RunOnUI(() =>
                {
                    CompletedTestsCount++;
                    CurrentTestName = result.TestName;

                    int index = -1;
                    for (int i = 0; i < Results.Count; i++)
                    {
                        if (Results[i].RuleId == result.RuleId)
                        {
                            index = i;
                            break;
                        }
                    }

                    if (index >= 0)
                    {
                        Results[index] = result;
                    }
                    else
                    {
                        Results.Add(result);
                    }

                    SelectedResult = result;
                    ProgressPercentage = Math.Min(100, (CompletedTestsCount * 100) / TotalTestsCount);
                    StatusMessage = $"Tested: {result.TestName} -> [{result.Status}]";

                    // Calculate reliable estimated remaining time based on actual test execution latency
                    int remaining = TotalTestsCount - CompletedTestsCount;
                    if (remaining > 0 && CompletedTestsCount >= 2)
                    {
                        double avgSecs = stopwatch.Elapsed.TotalSeconds / CompletedTestsCount;
                        int estSecs = (int)Math.Ceiling(avgSecs * remaining);
                        EstimatedRemainingDisplay = $"Est. {Math.Max(1, estSecs)}s remaining";
                    }
                    else if (remaining == 0)
                    {
                        EstimatedRemainingDisplay = "Complete";
                    }

                    UpdateStatusCounts();
                });
            });

            try
            {
                CurrentSession = await _diagnosticsService.RunDiagnosticsAsync(progress, _diagnosticsCts.Token);
                ProgressPercentage = 100;
                StatusMessage = $"Diagnostics completed. Overall evaluation: {CurrentSession.OverallStatus}.";
                EstimatedRemainingDisplay = $"Finished in {stopwatch.Elapsed.TotalSeconds:F1}s";
                UpdateStatusCounts();
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Diagnostic session cancelled by user.";
                EstimatedRemainingDisplay = "Cancelled";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Diagnostic execution error: {ex.Message}";
                EstimatedRemainingDisplay = "Failed";
            }
            finally
            {
                stopwatch.Stop();
                IsRunning = false;
                _diagnosticsCts?.Dispose();
                _diagnosticsCts = null;
            }
        }

        public async Task ExecuteSingleTestAsync(DiagnosticResult test)
        {
            if (test == null || IsRunning) return;

            IsRunning = true;
            CurrentTestName = test.TestName;
            StatusMessage = $"Running single diagnostic test: {test.TestName}...";

            try
            {
                var result = await _diagnosticsService.RunTestAsync(test.RuleId);
                int index = -1;
                for (int i = 0; i < Results.Count; i++)
                {
                    if (Results[i].RuleId == result.RuleId)
                    {
                        index = i;
                        break;
                    }
                }

                if (index >= 0)
                {
                    Results[index] = result;
                }
                SelectedResult = result;
                StatusMessage = $"Test complete: {result.TestName} -> [{result.Status}]";
                UpdateStatusCounts();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error testing {test.TestName}: {ex.Message}";
            }
            finally
            {
                IsRunning = false;
            }
        }

        private void UpdateStatusCounts()
        {
            PassedCount = Results.Count(r => r.Status == DiagnosticStatus.Passed);
            WarningCount = Results.Count(r => r.Status == DiagnosticStatus.Warning);
            FailedCount = Results.Count(r => r.Status == DiagnosticStatus.Failed);
        }

        private void RunOnUI(Action action)
        {
            var dispatcher = System.Windows.Application.Current?.Dispatcher;
            if (dispatcher != null && !dispatcher.CheckAccess())
            {
                dispatcher.BeginInvoke(action);
            }
            else
            {
                action?.Invoke();
            }
        }
    }
}
