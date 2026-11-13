using System;
using System.Collections.ObjectModel;
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
    public class FixMyConnectionWizardViewModel : ViewModelBase
    {
        private readonly FixMyConnectionWizardService _wizardService;
        private readonly DeviceManagementService _deviceManagementService;
        private readonly DeviceDiscoveryService _deviceDiscoveryService;
        private readonly Action<string> _navigateCallback;

        private BluetoothDeviceInfo _targetDevice;
        private ConnectionWizardStep _currentStep;
        private int _currentStepNumber = 1;
        private bool _isRunning;
        private bool _isCompleted;
        private bool _isSuccess;
        private string _statusMessage = "Select a Bluetooth peripheral to start the 10-step connection doctor.";
        private string _summaryBanner = string.Empty;
        private CancellationTokenSource _cts;

        // Interactive Auth Prompt State
        private bool _isPinPromptVisible;
        private string _pinPromptMessage = string.Empty;
        private string _enteredPin = "0000";

        private bool _isNumericPromptVisible;
        private string _numericPasskey = string.Empty;
        private string _numericPromptMessage = string.Empty;

        // Diagnosis & Repair State
        private ConnectionFailureDiagnosis _failureDiagnosis;
        private ProtocolConnectionDetails _protocolDetails;
        private bool _isRepairing;
        private string _repairExecutionMessage = string.Empty;

        // Device Selection Source
        private bool _isDeviceSelectionOpen = true;
        private bool _isScanningForTargets;
        private string _manualAddressInput = string.Empty;

        public ObservableCollection<ConnectionWizardStep> Steps { get; } = new ObservableCollection<ConnectionWizardStep>();
        public ObservableCollection<BluetoothDeviceInfo> CandidateDevices { get; } = new ObservableCollection<BluetoothDeviceInfo>();

        public BluetoothDeviceInfo TargetDevice
        {
            get => _targetDevice;
            set
            {
                if (SetProperty(ref _targetDevice, value))
                {
                    OnPropertyChanged(nameof(HasTargetDevice));
                    OnPropertyChanged(nameof(TargetDisplayName));
                    OnPropertyChanged(nameof(TargetAddress));
                    OnPropertyChanged(nameof(TargetCategoryIcon));
                    OnPropertyChanged(nameof(TargetCategoryName));
                    _wizardService.SelectTargetDevice(value);
                }
            }
        }

        public bool HasTargetDevice => TargetDevice != null;
        public string TargetDisplayName => TargetDevice?.DisplayName ?? "No Device Selected";
        public string TargetAddress => TargetDevice?.BluetoothAddress ?? "00:00:00:00:00:00";
        public string TargetCategoryIcon => TargetDevice?.CategoryIcon ?? "🎧";
        public string TargetCategoryName => TargetDevice?.CategoryName ?? "Peripheral";

        public ConnectionWizardStep CurrentStep
        {
            get => _currentStep;
            set
            {
                if (SetProperty(ref _currentStep, value))
                {
                    OnPropertyChanged(nameof(CurrentStepTitle));
                    OnPropertyChanged(nameof(CurrentStepDescription));
                    OnPropertyChanged(nameof(CurrentStepStatus));
                    OnPropertyChanged(nameof(CurrentStepStatusMessage));
                    OnPropertyChanged(nameof(CurrentStepEvidence));
                }
            }
        }

        public int CurrentStepNumber
        {
            get => _currentStepNumber;
            set => SetProperty(ref _currentStepNumber, value);
        }

        public string CurrentStepTitle => CurrentStep?.Title ?? "Ready";
        public string CurrentStepDescription => CurrentStep?.Description ?? string.Empty;
        public DeviceOperationStatus CurrentStepStatus => CurrentStep?.Status ?? DeviceOperationStatus.Requested;
        public string CurrentStepStatusMessage => CurrentStep?.StatusMessage ?? string.Empty;
        public string CurrentStepEvidence => CurrentStep?.Evidence ?? string.Empty;

        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                if (SetProperty(ref _isRunning, value))
                {
                    OnPropertyChanged(nameof(CanStart));
                    OnPropertyChanged(nameof(CanCancel));
                    OnPropertyChanged(nameof(CanReset));
                }
            }
        }

        public bool IsCompleted
        {
            get => _isCompleted;
            set => SetProperty(ref _isCompleted, value);
        }

        public bool IsSuccess
        {
            get => _isSuccess;
            set => SetProperty(ref _isSuccess, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public string SummaryBanner
        {
            get => _summaryBanner;
            set => SetProperty(ref _summaryBanner, value);
        }

        public bool IsDeviceSelectionOpen
        {
            get => _isDeviceSelectionOpen;
            set => SetProperty(ref _isDeviceSelectionOpen, value);
        }

        public bool IsScanningForTargets
        {
            get => _isScanningForTargets;
            set => SetProperty(ref _isScanningForTargets, value);
        }

        public string ManualAddressInput
        {
            get => _manualAddressInput;
            set => SetProperty(ref _manualAddressInput, value);
        }

        // Auth Properties
        public bool IsPinPromptVisible
        {
            get => _isPinPromptVisible;
            set => SetProperty(ref _isPinPromptVisible, value);
        }

        public string PinPromptMessage
        {
            get => _pinPromptMessage;
            set => SetProperty(ref _pinPromptMessage, value);
        }

        public string EnteredPin
        {
            get => _enteredPin;
            set => SetProperty(ref _enteredPin, value);
        }

        public bool IsNumericPromptVisible
        {
            get => _isNumericPromptVisible;
            set => SetProperty(ref _isNumericPromptVisible, value);
        }

        public string NumericPasskey
        {
            get => _numericPasskey;
            set => SetProperty(ref _numericPasskey, value);
        }

        public string NumericPromptMessage
        {
            get => _numericPromptMessage;
            set => SetProperty(ref _numericPromptMessage, value);
        }

        // Diagnosis & Repair Properties
        public ConnectionFailureDiagnosis FailureDiagnosis
        {
            get => _failureDiagnosis;
            set
            {
                if (SetProperty(ref _failureDiagnosis, value))
                {
                    OnPropertyChanged(nameof(HasDiagnosis));
                    OnPropertyChanged(nameof(HasSuggestedRepair));
                    OnPropertyChanged(nameof(SuggestedRepairTitle));
                    OnPropertyChanged(nameof(SuggestedRepairExplanation));
                    OnPropertyChanged(nameof(ConfidenceDisplay));
                }
            }
        }

        public bool HasDiagnosis => FailureDiagnosis != null;
        public bool HasSuggestedRepair => FailureDiagnosis != null && !string.IsNullOrEmpty(FailureDiagnosis.SuggestedRepairKey);
        public string SuggestedRepairTitle => FailureDiagnosis?.SuggestedRepairTitle ?? "Safe Diagnostic Repair";
        public string SuggestedRepairExplanation => FailureDiagnosis?.SuggestedRepairExplanation ?? string.Empty;
        public string ConfidenceDisplay => FailureDiagnosis != null ? $"{FailureDiagnosis.Confidence} Confidence" : string.Empty;

        public ProtocolConnectionDetails ProtocolDetails
        {
            get => _protocolDetails;
            set
            {
                if (SetProperty(ref _protocolDetails, value))
                {
                    OnPropertyChanged(nameof(HasProtocolDetails));
                    OnPropertyChanged(nameof(ProtocolName));
                    OnPropertyChanged(nameof(HasAudioContentionNotice));
                    OnPropertyChanged(nameof(AudioContentionNotice));
                }
            }
        }

        public bool HasProtocolDetails => ProtocolDetails != null;
        public string ProtocolName => ProtocolDetails?.Protocol ?? "Standard Bluetooth";
        public bool HasAudioContentionNotice => ProtocolDetails?.AudioAnalysis?.IsHighDefPlaybackThrottled ?? false;
        public string AudioContentionNotice => ProtocolDetails?.AudioAnalysis?.CoexistenceNotice ?? string.Empty;

        public bool IsRepairing
        {
            get => _isRepairing;
            set => SetProperty(ref _isRepairing, value);
        }

        public string RepairExecutionMessage
        {
            get => _repairExecutionMessage;
            set => SetProperty(ref _repairExecutionMessage, value);
        }

        public bool CanStart => !IsRunning && TargetDevice != null;
        public bool CanCancel => IsRunning;
        public bool CanReset => !IsRunning && (IsCompleted || Steps.Any(s => s.Status != DeviceOperationStatus.Requested));

        // Commands
        public ICommand StartWizardCommand { get; }
        public ICommand CancelWizardCommand { get; }
        public ICommand ResetWizardCommand { get; }
        public ICommand RetryCommand { get; }
        public ICommand RefreshCandidatesCommand { get; }
        public ICommand SelectDeviceCommand { get; }
        public ICommand AddManualDeviceCommand { get; }
        public ICommand SubmitPinCommand { get; }
        public ICommand CancelPinCommand { get; }
        public ICommand ConfirmPasskeyCommand { get; }
        public ICommand RejectPasskeyCommand { get; }
        public ICommand ExecuteSafeRepairCommand { get; }
        public ICommand ToggleDeviceSelectionCommand { get; }

        public FixMyConnectionWizardViewModel(
            DeviceManagementService deviceManagementService = null,
            DeviceDiscoveryService deviceDiscoveryService = null,
            AdapterService adapterService = null,
            Action<string> navigateCallback = null)
        {
            _deviceManagementService = deviceManagementService ?? new DeviceManagementService();
            _deviceDiscoveryService = deviceDiscoveryService ?? new DeviceDiscoveryService();
            _navigateCallback = navigateCallback;

            _wizardService = new FixMyConnectionWizardService(
                adapterService: adapterService,
                deviceManagementService: _deviceManagementService,
                deviceDiscoveryService: _deviceDiscoveryService);

            // Wire wizard callbacks
            _wizardService.StepUpdated += OnWizardStepUpdated;
            _wizardService.PinRequested += OnPinRequested;
            _wizardService.NumericComparisonRequested += OnNumericComparisonRequested;

            // Initialize commands
            StartWizardCommand = new RelayCommand(async () => await StartTroubleshootingAsync(), () => CanStart);
            CancelWizardCommand = new RelayCommand(() => CancelTroubleshooting(), () => CanCancel);
            ResetWizardCommand = new RelayCommand(() => ResetTroubleshooting(), () => CanReset);
            RetryCommand = new RelayCommand(async () => await StartTroubleshootingAsync(), () => !IsRunning && TargetDevice != null);
            RefreshCandidatesCommand = new RelayCommand(async () => await LoadCandidatesAsync());
            SelectDeviceCommand = new RelayCommand(param =>
            {
                if (param is BluetoothDeviceInfo dev)
                {
                    TargetDevice = dev;
                    IsDeviceSelectionOpen = false;
                }
            });
            AddManualDeviceCommand = new RelayCommand(() =>
            {
                if (!string.IsNullOrWhiteSpace(ManualAddressInput))
                {
                    var dev = new BluetoothDeviceInfo
                    {
                        DeviceName = "Manual Peripheral Target",
                        BluetoothAddress = ManualAddressInput.Trim().ToUpperInvariant(),
                        CategoryName = "Audio / Peripheral",
                        CategoryIcon = "🎧",
                        IsPaired = false
                    };
                    TargetDevice = dev;
                    IsDeviceSelectionOpen = false;
                }
            });

            SubmitPinCommand = new RelayCommand(() =>
            {
                IsPinPromptVisible = false;
                _wizardService.SubmitPin(EnteredPin);
            });

            CancelPinCommand = new RelayCommand(() =>
            {
                IsPinPromptVisible = false;
                _wizardService.SubmitPin(null);
            });

            ConfirmPasskeyCommand = new RelayCommand(() =>
            {
                IsNumericPromptVisible = false;
                _wizardService.SubmitNumericConfirmation(true);
            });

            RejectPasskeyCommand = new RelayCommand(() =>
            {
                IsNumericPromptVisible = false;
                _wizardService.SubmitNumericConfirmation(false);
            });

            ExecuteSafeRepairCommand = new RelayCommand(async () => await ExecuteRepairAsync());
            ToggleDeviceSelectionCommand = new RelayCommand(() => IsDeviceSelectionOpen = !IsDeviceSelectionOpen);

            SyncStepsFromService();
            _ = LoadCandidatesAsync();
        }

        public void PreSelectDevice(BluetoothDeviceInfo device)
        {
            if (device != null)
            {
                TargetDevice = device;
                IsDeviceSelectionOpen = false;
            }
        }

        private void SyncStepsFromService()
        {
            Steps.Clear();
            foreach (var step in _wizardService.Steps)
            {
                Steps.Add(step);
            }
            CurrentStep = Steps.FirstOrDefault();
            CurrentStepNumber = 1;
        }

        public async Task LoadCandidatesAsync()
        {
            IsScanningForTargets = true;
            try
            {
                CandidateDevices.Clear();
                var paired = await _deviceManagementService.GetPairedDevicesAsync();
                foreach (var d in paired)
                {
                    CandidateDevices.Add(d);
                }

                // If no device is currently selected, pick the first disconnected audio or HID device
                if (TargetDevice == null && CandidateDevices.Count > 0)
                {
                    var troubled = CandidateDevices.FirstOrDefault(d => !d.IsConnected) ?? CandidateDevices.FirstOrDefault();
                    if (troubled != null)
                    {
                        TargetDevice = troubled;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"[FixMyConnectionWizardVM.LoadCandidatesAsync] Exception: {ex.Message}");
            }
            finally
            {
                IsScanningForTargets = false;
            }
        }

        private async Task StartTroubleshootingAsync()
        {
            if (TargetDevice == null)
            {
                StatusMessage = "Please select a target peripheral first.";
                IsDeviceSelectionOpen = true;
                return;
            }

            IsRunning = true;
            IsCompleted = false;
            IsSuccess = false;
            IsDeviceSelectionOpen = false;
            FailureDiagnosis = null;
            ProtocolDetails = null;
            RepairExecutionMessage = string.Empty;
            SummaryBanner = $"Running 10-step diagnostic & connection pipeline for '{TargetDevice.DisplayName}'...";

            _cts = new CancellationTokenSource();

            try
            {
                var result = await _wizardService.RunWizardAsync(TargetDevice, _cts.Token);
                IsCompleted = true;
                IsSuccess = result.IsSuccess;
                ProtocolDetails = result.ProtocolDetails;
                FailureDiagnosis = result.Diagnosis;

                if (result.IsSuccess)
                {
                    StatusMessage = $"Connection successful! '{TargetDevice.DisplayName}' is active and operational.";
                    SummaryBanner = result.SummaryMessage;
                    NotificationService.Instance.ShowSuccess("Bluetooth Connection Established", result.SummaryMessage);
                }
                else
                {
                    StatusMessage = $"Connection could not be established. Root cause: {result.Diagnosis?.PrimaryRootCause ?? "Check failed."}";
                    SummaryBanner = result.SummaryMessage;
                    NotificationService.Instance.ShowError("Bluetooth Connection Incomplete", result.SummaryMessage);
                }
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Troubleshooting cancelled by user.";
                SummaryBanner = "Troubleshooting was cancelled.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Troubleshooting error: {ex.Message}";
                SummaryBanner = ex.Message;
            }
            finally
            {
                IsRunning = false;
                SyncStepsFromService();
            }
        }

        private void CancelTroubleshooting()
        {
            if (_cts != null && !_cts.IsCancellationRequested)
            {
                _cts.Cancel();
            }
            StatusMessage = "Cancelling troubleshooting...";
        }

        private void ResetTroubleshooting()
        {
            _wizardService.ResetWizard();
            SyncStepsFromService();
            IsCompleted = false;
            IsSuccess = false;
            FailureDiagnosis = null;
            ProtocolDetails = null;
            RepairExecutionMessage = string.Empty;
            StatusMessage = "Ready. Select target peripheral to begin.";
            SummaryBanner = string.Empty;
        }

        private async Task ExecuteRepairAsync()
        {
            if (FailureDiagnosis == null || string.IsNullOrEmpty(FailureDiagnosis.SuggestedRepairKey))
                return;

            IsRepairing = true;
            RepairExecutionMessage = $"Executing safe repair: {FailureDiagnosis.SuggestedRepairTitle}...";

            try
            {
                var record = await _wizardService.ExecuteSuggestedRepairAsync(FailureDiagnosis.SuggestedRepairKey);
                if (record.Status == "Success")
                {
                    RepairExecutionMessage = $"Repair executed successfully! ({record.VerificationResult})";
                    NotificationService.Instance.ShowSuccess("Safe Repair Complete", $"{FailureDiagnosis.SuggestedRepairTitle} completed.");

                    // Automatically offer or trigger retry
                    StatusMessage = "Repair applied. Retrying connection pipeline...";
                    await Task.Delay(1500);
                    await StartTroubleshootingAsync();
                }
                else
                {
                    RepairExecutionMessage = $"Repair encountered an issue: {record.ErrorMessage}";
                    NotificationService.Instance.ShowWarning("Repair Alert", record.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                RepairExecutionMessage = $"Repair failed: {ex.Message}";
            }
            finally
            {
                IsRepairing = false;
            }
        }

        private void OnWizardStepUpdated(object sender, ConnectionWizardStep step)
        {
            App.Current?.Dispatcher?.Invoke(() =>
            {
                CurrentStep = step;
                CurrentStepNumber = step.StepNumber;
                StatusMessage = $"{step.ShortTitle}: {step.StatusMessage}";

                // Update step in list
                var existing = Steps.FirstOrDefault(s => s.StepNumber == step.StepNumber);
                if (existing != null)
                {
                    int index = Steps.IndexOf(existing);
                    Steps[index] = step;
                }
            });
        }

        private void OnPinRequested(object sender, PinPromptRequest request)
        {
            App.Current?.Dispatcher?.Invoke(() =>
            {
                PinPromptMessage = request.PromptMessage;
                EnteredPin = request.SuggestedPin;
                IsPinPromptVisible = true;
            });
        }

        private void OnNumericComparisonRequested(object sender, NumericComparisonRequest request)
        {
            App.Current?.Dispatcher?.Invoke(() =>
            {
                NumericPasskey = request.Passkey;
                NumericPromptMessage = request.PromptMessage;
                IsNumericPromptVisible = true;
            });
        }
    }
}
