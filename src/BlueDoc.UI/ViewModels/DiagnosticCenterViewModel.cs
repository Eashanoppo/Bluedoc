using System;
using System.Collections.ObjectModel;
using System.Linq;
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
        private int _progressPercentage;
        private string _statusMessage = "Ready to run diagnostics.";
        private int _passedCount;
        private int _failedCount;
        private int _warningCount;

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
            set => SetProperty(ref _isRunning, value);
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
        public ICommand RunSingleTestCommand { get; }
        public ICommand NavigateToRepairCommand { get; }

        public DiagnosticCenterViewModel(DiagnosticsService diagnosticsService, Action<string> navigateAction = null)
        {
            _diagnosticsService = diagnosticsService ?? new DiagnosticsService();
            _navigateAction = navigateAction;

            InitializeTestCatalog();

            RunFullDiagnosticsCommand = new RelayCommand(async () => await ExecuteFullDiagnosticsAsync(), () => !IsRunning);
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
            }, param => !IsRunning);

            NavigateToRepairCommand = new RelayCommand(() =>
            {
                _navigateAction?.Invoke("Safe Repair Center");
            });
        }

        public void InitializeTestCatalog()
        {
            Results.Clear();
            var initialTests = _diagnosticsService.GetInitialTestList();
            foreach (var test in initialTests)
            {
                Results.Add(test);
            }

            if (Results.Count > 0)
            {
                SelectedResult = Results[0];
            }
            UpdateStatusCounts();
        }

        public async Task ExecuteFullDiagnosticsAsync()
        {
            IsRunning = true;
            ProgressPercentage = 0;
            StatusMessage = "Initializing diagnostic pipeline (12 rules)...";

            int totalTests = Results.Count > 0 ? Results.Count : 12;
            int completedCount = 0;

            var progress = new Progress<DiagnosticResult>(result =>
            {
                completedCount++;
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
                ProgressPercentage = Math.Min(100, completedCount * 100 / totalTests);
                StatusMessage = $"Running: {result.TestName} -> [{result.Status}]";
                UpdateStatusCounts();
            });

            try
            {
                CurrentSession = await _diagnosticsService.RunDiagnosticsAsync(progress);
                ProgressPercentage = 100;
                StatusMessage = $"Diagnostics completed. Overall evaluation: {CurrentSession.OverallStatus}.";
                UpdateStatusCounts();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Diagnostic execution error: {ex.Message}";
            }
            finally
            {
                IsRunning = false;
            }
        }

        public async Task ExecuteSingleTestAsync(DiagnosticResult test)
        {
            if (test == null || IsRunning) return;

            IsRunning = true;
            StatusMessage = $"Evaluating individual test '{test.TestName}'...";

            try
            {
                var updated = await _diagnosticsService.RunTestAsync(test.RuleId);
                int index = Results.IndexOf(test);
                if (index >= 0)
                {
                    Results[index] = updated;
                }
                SelectedResult = updated;
                StatusMessage = $"Test '{test.TestName}' completed with status: [{updated.Status}].";
                UpdateStatusCounts();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error executing test '{test.TestName}': {ex.Message}";
            }
            finally
            {
                IsRunning = false;
            }
        }

        private void UpdateStatusCounts()
        {
            PassedCount = Results.Count(r => r.Status == DiagnosticStatus.Passed);
            FailedCount = Results.Count(r => r.Status == DiagnosticStatus.Failed);
            WarningCount = Results.Count(r => r.Status == DiagnosticStatus.Warning);
        }
    }
}
