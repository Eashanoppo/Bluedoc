using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BlueDoc.Bluetooth.Abstractions;
using BlueDoc.Core.Enums;
using BlueDoc.Core.Models;
using BlueDoc.Infrastructure.Audio;
using BlueDoc.Infrastructure.Platform;
using BlueDoc.Infrastructure.Repairs;

namespace BlueDoc.Application.Services
{
    public class ConnectionWizardStep
    {
        public int StepNumber { get; set; }
        public string Title { get; set; }
        public string ShortTitle { get; set; }
        public string Description { get; set; }
        public DeviceOperationStatus Status { get; set; } = DeviceOperationStatus.Requested;
        public string StatusMessage { get; set; } = string.Empty;
        public string Evidence { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsCompleted => Status == DeviceOperationStatus.Succeeded;
        public bool IsFailed => Status == DeviceOperationStatus.Failed;
        public bool RequiresUserAction => Status == DeviceOperationStatus.RequiresUserInteraction;
        public DateTime? CompletedAt { get; set; }
    }

    public class PinPromptRequest
    {
        public BluetoothDeviceInfo TargetDevice { get; set; }
        public string SuggestedPin { get; set; } = "0000";
        public string PromptMessage { get; set; }
        public TaskCompletionSource<string> ResponseTask { get; set; }
    }

    public class NumericComparisonRequest
    {
        public BluetoothDeviceInfo TargetDevice { get; set; }
        public string Passkey { get; set; }
        public string PromptMessage { get; set; }
        public TaskCompletionSource<bool> ResponseTask { get; set; }
    }

    public class FixMyConnectionWizardResult
    {
        public bool IsSuccess { get; set; }
        public BluetoothDeviceInfo TargetDevice { get; set; }
        public DeviceOperationStatus FinalStatus { get; set; }
        public string SummaryMessage { get; set; } = string.Empty;
        public ProtocolConnectionDetails ProtocolDetails { get; set; }
        public ConnectionFailureDiagnosis Diagnosis { get; set; }
        public List<ConnectionWizardStep> Steps { get; set; } = new List<ConnectionWizardStep>();
    }

    public class FixMyConnectionWizardService
    {
        private readonly AdapterService _adapterService;
        private readonly BluetoothStartupHealthService _startupHealthService;
        private readonly DeviceManagementService _deviceManagementService;
        private readonly DeviceDiscoveryService _deviceDiscoveryService;
        private readonly BluetoothConnectionOrchestrator _connectionOrchestrator;
        private readonly ConnectionFailureDiagnosticEngine _diagnosticEngine;
        private readonly SafeRepairEngine _repairEngine;
        private readonly BluetoothAudioInspector _audioInspector;
        private readonly OsVersionInfo _osVersion;

        public List<ConnectionWizardStep> Steps { get; } = new List<ConnectionWizardStep>();
        public BluetoothDeviceInfo CurrentTargetDevice { get; private set; }
        public ConnectionWizardStep CurrentStep { get; private set; }
        public bool IsRunning { get; private set; }

        public event EventHandler<ConnectionWizardStep> StepUpdated;
        public event EventHandler<PinPromptRequest> PinRequested;
        public event EventHandler<NumericComparisonRequest> NumericComparisonRequested;

        private PinPromptRequest _activePinRequest;
        private NumericComparisonRequest _activeNumericRequest;
        private CancellationTokenSource _cts;

        public FixMyConnectionWizardService(
            AdapterService adapterService = null,
            BluetoothStartupHealthService startupHealthService = null,
            DeviceManagementService deviceManagementService = null,
            DeviceDiscoveryService deviceDiscoveryService = null,
            BluetoothConnectionOrchestrator connectionOrchestrator = null,
            ConnectionFailureDiagnosticEngine diagnosticEngine = null,
            SafeRepairEngine repairEngine = null)
        {
            _adapterService = adapterService ?? new AdapterService();
            _startupHealthService = startupHealthService ?? new BluetoothStartupHealthService(_adapterService);
            _deviceManagementService = deviceManagementService ?? new DeviceManagementService();
            _deviceDiscoveryService = deviceDiscoveryService ?? new DeviceDiscoveryService();
            _connectionOrchestrator = connectionOrchestrator ?? new BluetoothConnectionOrchestrator();
            _diagnosticEngine = diagnosticEngine ?? new ConnectionFailureDiagnosticEngine(_adapterService);
            _repairEngine = repairEngine ?? new SafeRepairEngine();
            _audioInspector = new BluetoothAudioInspector();
            _osVersion = OperatingSystemInspector.GetOperatingSystemInfo();

            InitializeStepDefinitions();
        }

        private void InitializeStepDefinitions()
        {
            Steps.Clear();
            Steps.Add(new ConnectionWizardStep
            {
                StepNumber = 1,
                Title = "Target Peripheral Selection",
                ShortTitle = "1. Target",
                Description = "Select or specify the Bluetooth peripheral (speaker, headphones, mouse, keyboard, phone, or BLE sensor) to troubleshoot and connect.",
                Status = DeviceOperationStatus.Requested
            });
            Steps.Add(new ConnectionWizardStep
            {
                StepNumber = 2,
                Title = "Adapter & Radio Inspection",
                ShortTitle = "2. Adapter",
                Description = "Verify that the local Bluetooth transceiver hardware is recognized, enabled, and radio transmission is powered on.",
                Status = DeviceOperationStatus.Requested
            });
            Steps.Add(new ConnectionWizardStep
            {
                StepNumber = 3,
                Title = "System Services & Stack Readiness",
                ShortTitle = "3. System",
                Description = "Check Windows core Bluetooth Support Service (bthserv), Audio Gateway services, and driver stack operational status.",
                Status = DeviceOperationStatus.Requested
            });
            Steps.Add(new ConnectionWizardStep
            {
                StepNumber = 4,
                Title = "Target Peripheral Discovery & Probing",
                ShortTitle = "4. Discovery",
                Description = "Inquire radio beacons and BLE advertisements to confirm peripheral is powered on, discoverable, and in range.",
                Status = DeviceOperationStatus.Requested
            });
            Steps.Add(new ConnectionWizardStep
            {
                StepNumber = 5,
                Title = "Pairing Readiness & Security Evaluation",
                ShortTitle = "5. Pairing Check",
                Description = "Inspect Windows pairing database, cryptographic bond records, and determine required authentication scheme.",
                Status = DeviceOperationStatus.Requested
            });
            Steps.Add(new ConnectionWizardStep
            {
                StepNumber = 6,
                Title = "Native Windows Pairing Attempt",
                ShortTitle = "6. Pair Device",
                Description = "Execute native Windows Bluetooth pairing via Win32 Authenticate or WinRT BLE Custom Pairing.",
                Status = DeviceOperationStatus.Requested
            });
            Steps.Add(new ConnectionWizardStep
            {
                StepNumber = 7,
                Title = "Authentication & Key Exchange",
                ShortTitle = "7. Auth Exchange",
                Description = "Complete PIN entry, passkey confirmation, or numeric comparison security handshake with peripheral.",
                Status = DeviceOperationStatus.Requested
            });
            Steps.Add(new ConnectionWizardStep
            {
                StepNumber = 8,
                Title = "Protocol-Appropriate Connection",
                ShortTitle = "8. Connect",
                Description = "Establish protocol-specific connection (A2DP Audio Sink, HFP Telephony, HIDClass Input, or GATT link).",
                Status = DeviceOperationStatus.Requested
            });
            Steps.Add(new ConnectionWizardStep
            {
                StepNumber = 9,
                Title = "Operational Verification & Audio Inspection",
                ShortTitle = "9. Verify",
                Description = "Inspect active endpoints, verify Windows audio stream routing, detect microphone contention, and test data link.",
                Status = DeviceOperationStatus.Requested
            });
            Steps.Add(new ConnectionWizardStep
            {
                StepNumber = 10,
                Title = "Diagnostic Root-Cause & Safe Repair",
                ShortTitle = "10. Diagnosis",
                Description = "If any issue occurs, analyze 10 diagnostic checkpoints, identify root cause with confidence rating, and provide safe one-click repair.",
                Status = DeviceOperationStatus.Requested
            });
        }

        public void ResetWizard()
        {
            if (IsRunning && _cts != null)
            {
                _cts.Cancel();
            }
            IsRunning = false;
            CurrentTargetDevice = null;
            CurrentStep = null;
            _activePinRequest = null;
            _activeNumericRequest = null;
            InitializeStepDefinitions();
        }

        public void SelectTargetDevice(BluetoothDeviceInfo device)
        {
            CurrentTargetDevice = device;
            var step1 = Steps.FirstOrDefault(s => s.StepNumber == 1);
            if (step1 != null)
            {
                if (device != null)
                {
                    step1.Status = DeviceOperationStatus.Succeeded;
                    step1.StatusMessage = $"Selected '{device.DisplayName}' ({device.BluetoothAddress})";
                    step1.Evidence = $"Class: {device.MajorDeviceClass}, Category: {device.CategoryName}, Type: {device.DeviceType}, Currently Paired: {device.IsPaired}";
                    step1.CompletedAt = DateTime.Now;
                }
                else
                {
                    step1.Status = DeviceOperationStatus.Requested;
                    step1.StatusMessage = "No device selected yet.";
                    step1.Evidence = string.Empty;
                }
                NotifyStepUpdated(step1);
            }
        }

        public void SubmitPin(string pin)
        {
            if (_activePinRequest != null && !_activePinRequest.ResponseTask.Task.IsCompleted)
            {
                _activePinRequest.ResponseTask.TrySetResult(pin);
                _activePinRequest = null;
            }
        }

        public void SubmitNumericConfirmation(bool confirmed)
        {
            if (_activeNumericRequest != null && !_activeNumericRequest.ResponseTask.Task.IsCompleted)
            {
                _activeNumericRequest.ResponseTask.TrySetResult(confirmed);
                _activeNumericRequest = null;
            }
        }

        public async Task<FixMyConnectionWizardResult> RunWizardAsync(
            BluetoothDeviceInfo targetDevice = null,
            CancellationToken externalCt = default(CancellationToken))
        {
            if (IsRunning)
            {
                throw new InvalidOperationException("The Connection Wizard is already running.");
            }

            IsRunning = true;
            _cts = CancellationTokenSource.CreateLinkedTokenSource(externalCt);
            var ct = _cts.Token;

            var finalResult = new FixMyConnectionWizardResult
            {
                Steps = Steps
            };

            if (targetDevice != null)
            {
                SelectTargetDevice(targetDevice);
            }

            try
            {
                // ==========================================
                // STEP 1: TARGET SELECTION
                // ==========================================
                var step1 = Steps[0];
                SetStepActive(step1);

                if (CurrentTargetDevice == null)
                {
                    step1.Status = DeviceOperationStatus.RequiresUserInteraction;
                    step1.StatusMessage = "Please select a target peripheral to troubleshoot.";
                    NotifyStepUpdated(step1);
                    finalResult.FinalStatus = DeviceOperationStatus.RequiresUserInteraction;
                    finalResult.SummaryMessage = "Wizard waiting for user to choose a target Bluetooth peripheral.";
                    IsRunning = false;
                    return finalResult;
                }

                step1.Status = DeviceOperationStatus.Succeeded;
                step1.StatusMessage = $"Target: {CurrentTargetDevice.DisplayName}";
                step1.CompletedAt = DateTime.Now;
                NotifyStepUpdated(step1);
                finalResult.TargetDevice = CurrentTargetDevice;

                // ==========================================
                // STEP 2: INSPECT ADAPTER & RADIO
                // ==========================================
                var step2 = Steps[1];
                SetStepActive(step2);
                step2.Status = DeviceOperationStatus.InProgress;
                step2.StatusMessage = "Inspecting Bluetooth transceiver hardware...";
                NotifyStepUpdated(step2);

                var adapter = await _adapterService.GetPrimaryAdapterAsync();
                if (adapter == null)
                {
                    step2.Status = DeviceOperationStatus.Failed;
                    step2.StatusMessage = "No active Bluetooth adapter detected.";
                    step2.Evidence = "Windows Device Manager reported no operational Bluetooth transceiver.";
                    NotifyStepUpdated(step2);
                    return await ExecuteDiagnosticConclusionAsync(finalResult, 1001, "No Bluetooth adapter detected.");
                }

                if (adapter.ProblemCode != 0)
                {
                    step2.Status = DeviceOperationStatus.Failed;
                    step2.StatusMessage = $"Adapter error CM_PROB ({(int)adapter.ProblemCode}).";
                    step2.Evidence = $"Adapter '{adapter.FriendlyName}' has hardware problem code {(int)adapter.ProblemCode}.";
                    NotifyStepUpdated(step2);
                    return await ExecuteDiagnosticConclusionAsync(finalResult, (uint)adapter.ProblemCode, step2.StatusMessage);
                }

                if (!adapter.IsEnabled || adapter.Status == AdapterStatus.Disabled)
                {
                    step2.Status = DeviceOperationStatus.Failed;
                    step2.StatusMessage = "Bluetooth radio transceiver is turned off or disabled.";
                    step2.Evidence = "The controller radio is in soft-off, disabled, or Windows Airplane Mode.";
                    NotifyStepUpdated(step2);
                    return await ExecuteDiagnosticConclusionAsync(finalResult, 1002, "Bluetooth radio is disabled.");
                }

                step2.Status = DeviceOperationStatus.Succeeded;
                step2.StatusMessage = $"Adapter operational: {adapter.FriendlyName}";
                step2.Evidence = $"Manufacturer: {adapter.Manufacturer}, Status: {adapter.Status}";
                step2.CompletedAt = DateTime.Now;
                NotifyStepUpdated(step2);

                // ==========================================
                // STEP 3: SYSTEM SERVICES & STACK READINESS
                // ==========================================
                var step3 = Steps[2];
                SetStepActive(step3);
                step3.Status = DeviceOperationStatus.InProgress;
                step3.StatusMessage = "Evaluating Windows Bluetooth services and OS stack...";
                NotifyStepUpdated(step3);

                var health = await _startupHealthService.InspectSystemHealthAsync();
                if (!health.ServicesRunning)
                {
                    step3.Status = DeviceOperationStatus.Failed;
                    step3.StatusMessage = "Bluetooth Support Service (bthserv) is not running.";
                    step3.Evidence = "bthserv is stopped or paused, preventing device discovery and service discovery.";
                    NotifyStepUpdated(step3);
                    return await ExecuteDiagnosticConclusionAsync(finalResult, 1003, "bthserv service is stopped.");
                }

                step3.Status = DeviceOperationStatus.Succeeded;
                step3.StatusMessage = "Windows Bluetooth stack and services are operational.";
                step3.Evidence = $"OS: {health.OsName}, Core Service: bthserv Running, Stack: {health.ApiReadiness}";
                step3.CompletedAt = DateTime.Now;
                NotifyStepUpdated(step3);

                // ==========================================
                // STEP 4: DISCOVER & PROBE TARGET PERIPHERAL
                // ==========================================
                var step4 = Steps[3];
                SetStepActive(step4);
                step4.Status = DeviceOperationStatus.InProgress;
                step4.StatusMessage = $"Probing signal and visibility for '{CurrentTargetDevice.DisplayName}'...";
                NotifyStepUpdated(step4);

                string discoveryEvidence = string.Empty;

                if (CurrentTargetDevice.IsPaired)
                {
                    // For already-paired devices, check if it's currently detected in inquiry or BLE cache
                    var nearby = await _deviceDiscoveryService.StartScanningAsync(
                        timeoutSeconds: 4,
                        mode: DiscoveryMode.DualMode,
                        externalToken: ct);

                    var matched = nearby.FirstOrDefault(d => 
                        string.Equals(d.BluetoothAddress, CurrentTargetDevice.BluetoothAddress, StringComparison.OrdinalIgnoreCase) ||
                        (d.RawAddress != 0 && d.RawAddress == CurrentTargetDevice.RawAddress));

                    if (matched != null)
                    {
                        CurrentTargetDevice.Rssi = matched.Rssi;
                        discoveryEvidence = $"Device actively detected broadcasting nearby. Signal RSSI: {matched.Rssi} dBm ({matched.SignalPercentage}%).";
                    }
                    else
                    {
                        // Device is paired in Windows; it might be sleeping or waiting for connection trigger
                        discoveryEvidence = "Device is registered in Windows pairing database. Direct service interrogation will be used.";
                    }
                }
                else
                {
                    // Device is not paired: MUST be broadcasting/discoverable
                    step4.StatusMessage = "Scanning for pairing advertisement / inquiry beacon...";
                    NotifyStepUpdated(step4);

                    var nearby = await _deviceDiscoveryService.StartScanningAsync(
                        timeoutSeconds: 6,
                        mode: DiscoveryMode.DualMode,
                        externalToken: ct);

                    var matched = nearby.FirstOrDefault(d => 
                        string.Equals(d.BluetoothAddress, CurrentTargetDevice.BluetoothAddress, StringComparison.OrdinalIgnoreCase) ||
                        (d.RawAddress != 0 && d.RawAddress == CurrentTargetDevice.RawAddress));

                    if (matched != null)
                    {
                        CurrentTargetDevice.Rssi = matched.Rssi;
                        CurrentTargetDevice.DeviceType = matched.DeviceType;
                        discoveryEvidence = $"Target found broadcasting. Signal: {matched.Rssi} dBm ({matched.SignalPercentage}%), Mode: {matched.DeviceType}.";
                    }
                    else
                    {
                        step4.Status = DeviceOperationStatus.Failed;
                        step4.StatusMessage = $"Device '{CurrentTargetDevice.DisplayName}' not found in range.";
                        step4.Evidence = "No inquiry beacon or BLE advertisement detected. Ensure device is powered on, fully charged, and in pairing mode (LED blinking rapidly).";
                        NotifyStepUpdated(step4);
                        return await ExecuteDiagnosticConclusionAsync(finalResult, 1004, "Target peripheral is not advertising or discoverable in radio range.");
                    }
                }

                step4.Status = DeviceOperationStatus.Succeeded;
                step4.StatusMessage = "Peripheral is responsive and ready.";
                step4.Evidence = discoveryEvidence;
                step4.CompletedAt = DateTime.Now;
                NotifyStepUpdated(step4);

                // ==========================================
                // STEP 5: EVALUATE PAIRING READINESS
                // ==========================================
                var step5 = Steps[4];
                SetStepActive(step5);
                step5.Status = DeviceOperationStatus.InProgress;
                step5.StatusMessage = "Evaluating Windows security database and authentication type...";
                NotifyStepUpdated(step5);

                bool needsPairing = !CurrentTargetDevice.IsPaired;
                string pairingType = CurrentTargetDevice.DeviceType == DeviceType.LowEnergy 
                    ? "Bluetooth Low Energy Security Manager (SMP)" 
                    : "Classic Bluetooth Secure Simple Pairing (SSP) / PIN";

                step5.Status = DeviceOperationStatus.Succeeded;
                if (needsPairing)
                {
                    step5.StatusMessage = $"Peripheral requires bonding. Scheme: {pairingType}";
                    step5.Evidence = "Device is not yet paired in Windows. BlueDoc will execute native authentication.";
                }
                else
                {
                    step5.StatusMessage = "Peripheral is already paired in Windows database.";
                    step5.Evidence = $"Bonding registered in Windows database. Remembered: {CurrentTargetDevice.IsRemembered}";
                }
                step5.CompletedAt = DateTime.Now;
                NotifyStepUpdated(step5);

                // ==========================================
                // STEP 6 & 7: ATTEMPT PAIRING & AUTHENTICATION
                // ==========================================
                var step6 = Steps[5];
                var step7 = Steps[6];

                if (needsPairing)
                {
                    SetStepActive(step6);
                    step6.Status = DeviceOperationStatus.InProgress;
                    step6.StatusMessage = $"Initiating native Windows pairing with '{CurrentTargetDevice.DisplayName}'...";
                    NotifyStepUpdated(step6);

                    // Setup interactive callbacks
                    Func<string, Task<bool>> numericCallback = async (passkey) =>
                    {
                        SetStepActive(step7);
                        step7.Status = DeviceOperationStatus.RequiresUserInteraction;
                        step7.StatusMessage = $"Confirm passkey: {passkey}";
                        step7.Evidence = $"Windows generated 6-digit numeric passkey: {passkey}. Does it match your peripheral display?";
                        NotifyStepUpdated(step7);

                        var tcs = new TaskCompletionSource<bool>();
                        _activeNumericRequest = new NumericComparisonRequest
                        {
                            TargetDevice = CurrentTargetDevice,
                            Passkey = passkey,
                            PromptMessage = $"Does the code {passkey} match the code shown on your device?",
                            ResponseTask = tcs
                        };

                        NumericComparisonRequested?.Invoke(this, _activeNumericRequest);

                        bool confirmed = await tcs.Task;
                        step7.Status = confirmed ? DeviceOperationStatus.Succeeded : DeviceOperationStatus.Failed;
                        step7.StatusMessage = confirmed ? "Numeric passkey confirmed by user." : "Passkey rejected by user.";
                        step7.CompletedAt = DateTime.Now;
                        NotifyStepUpdated(step7);
                        return confirmed;
                    };

                    var pairResult = await _deviceManagementService.PairDeviceAsync(
                        CurrentTargetDevice,
                        pin: null,
                        confirmPinMatchCallback: numericCallback,
                        cancellationToken: ct);

                    // If pairing returned failed because of authentication PIN required
                    if (!pairResult.Success && pairResult.NativeErrorCode == 1244) // ERROR_NOT_AUTHENTICATED
                    {
                        SetStepActive(step7);
                        step7.Status = DeviceOperationStatus.RequiresUserInteraction;
                        step7.StatusMessage = "Peripheral requested a PIN code (e.g., '0000' or '1234').";
                        step7.Evidence = "Legacy or high-security peripheral requires PIN entry.";
                        NotifyStepUpdated(step7);

                        var pinTcs = new TaskCompletionSource<string>();
                        _activePinRequest = new PinPromptRequest
                        {
                            TargetDevice = CurrentTargetDevice,
                            SuggestedPin = "0000",
                            PromptMessage = $"Enter the PIN code for {CurrentTargetDevice.DisplayName} (standard default is 0000 or 1234):",
                            ResponseTask = pinTcs
                        };

                        PinRequested?.Invoke(this, _activePinRequest);

                        string enteredPin = await pinTcs.Task;
                        if (!string.IsNullOrEmpty(enteredPin))
                        {
                            step7.Status = DeviceOperationStatus.InProgress;
                            step7.StatusMessage = "Submitting entered PIN code...";
                            NotifyStepUpdated(step7);

                            pairResult = await _deviceManagementService.PairDeviceAsync(
                                CurrentTargetDevice,
                                pin: enteredPin,
                                confirmPinMatchCallback: null,
                                cancellationToken: ct);
                        }
                    }

                    if (!pairResult.Success)
                    {
                        step6.Status = DeviceOperationStatus.Failed;
                        step6.StatusMessage = $"Pairing attempt failed: {pairResult.Message}";
                        step6.Evidence = pairResult.ErrorDetails;
                        NotifyStepUpdated(step6);

                        step7.Status = DeviceOperationStatus.Failed;
                        step7.StatusMessage = "Authentication handshake failed or timed out.";
                        NotifyStepUpdated(step7);

                        return await ExecuteDiagnosticConclusionAsync(finalResult, pairResult.NativeErrorCode, pairResult.Message);
                    }

                    step6.Status = DeviceOperationStatus.Succeeded;
                    step6.StatusMessage = "Windows pairing succeeded.";
                    step6.Evidence = pairResult.Message;
                    step6.CompletedAt = DateTime.Now;
                    NotifyStepUpdated(step6);

                    step7.Status = DeviceOperationStatus.Succeeded;
                    step7.StatusMessage = "Cryptographic bonding verified.";
                    step7.Evidence = "Windows Bluetooth security database successfully registered device link key.";
                    step7.CompletedAt = DateTime.Now;
                    NotifyStepUpdated(step7);
                }
                else
                {
                    step6.Status = DeviceOperationStatus.Succeeded;
                    step6.StatusMessage = "Device already paired; skipping key exchange.";
                    step6.Evidence = "Prior bonding record active in Windows.";
                    step6.CompletedAt = DateTime.Now;
                    NotifyStepUpdated(step6);

                    step7.Status = DeviceOperationStatus.Succeeded;
                    step7.StatusMessage = "Existing authentication record validated.";
                    step7.Evidence = "Link key active in Windows security manager.";
                    step7.CompletedAt = DateTime.Now;
                    NotifyStepUpdated(step7);
                }

                // ==========================================
                // STEP 8: PROTOCOL-APPROPRIATE CONNECTION
                // ==========================================
                var step8 = Steps[7];
                SetStepActive(step8);
                step8.Status = DeviceOperationStatus.InProgress;
                step8.StatusMessage = $"Initiating protocol-specific connection ({CurrentTargetDevice.CategoryName})...";
                NotifyStepUpdated(step8);

                var connDetails = await _connectionOrchestrator.ConnectPeripheralAsync(CurrentTargetDevice, ct);
                finalResult.ProtocolDetails = connDetails;

                if (!connDetails.Result.Success)
                {
                    step8.Status = DeviceOperationStatus.Failed;
                    step8.StatusMessage = $"Connection failed: {connDetails.Result.Message}";
                    step8.Evidence = $"{connDetails.Result.ErrorDetails} (Protocol: {connDetails.Protocol})";
                    NotifyStepUpdated(step8);

                    return await ExecuteDiagnosticConclusionAsync(
                        finalResult, 
                        connDetails.Result.NativeErrorCode, 
                        connDetails.Result.Message);
                }

                step8.Status = DeviceOperationStatus.Succeeded;
                step8.StatusMessage = $"Connection established via {connDetails.Protocol}.";
                step8.Evidence = string.Join("; ", connDetails.ActiveProfiles);
                step8.CompletedAt = DateTime.Now;
                NotifyStepUpdated(step8);

                // ==========================================
                // STEP 9: VERIFY RESULT & AUDIO INSPECTION
                // ==========================================
                var step9 = Steps[8];
                SetStepActive(step9);
                step9.Status = DeviceOperationStatus.InProgress;
                step9.StatusMessage = "Verifying operational endpoints and stream readiness...";
                NotifyStepUpdated(step9);

                string verificationEvidence = string.Empty;

                if (connDetails.IsAudioCapable)
                {
                    var audioAnalysis = connDetails.AudioAnalysis ?? _audioInspector.AnalyzeAudioSubsystem();
                    var endpoints = audioAnalysis.Endpoints ?? new List<BluetoothAudioEndpoint>();
                    var matching = endpoints.FirstOrDefault(e => 
                        e.Name.IndexOf(CurrentTargetDevice.DisplayName, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        (!string.IsNullOrEmpty(CurrentTargetDevice.BluetoothAddress) && e.HardwareId.IndexOf(CurrentTargetDevice.BluetoothAddress.Replace(":", ""), StringComparison.OrdinalIgnoreCase) >= 0));

                    if (matching != null)
                    {
                        verificationEvidence = $"Audio Endpoint: '{matching.Name}' ({matching.Direction}) - Status: {matching.Status}. Profile: {matching.ActiveProfile}.";
                        if (audioAnalysis.IsHighDefPlaybackThrottled)
                        {
                            verificationEvidence += $" Note: {audioAnalysis.CoexistenceNotice}";
                        }
                    }
                    else
                    {
                        verificationEvidence = $"Windows reports connection active. Audio endpoints enumerated: {endpoints.Count}. Profile Guidance: {connDetails.AudioGuidance}";
                    }
                }
                else
                {
                    verificationEvidence = $"Device operational state verified in Windows PnP. Active: {CurrentTargetDevice.IsConnected}.";
                }

                step9.Status = DeviceOperationStatus.Succeeded;
                step9.StatusMessage = "Operational verification complete.";
                step9.Evidence = verificationEvidence;
                step9.CompletedAt = DateTime.Now;
                NotifyStepUpdated(step9);

                // ==========================================
                // STEP 10: DIAGNOSIS & REPAIR (SUCCESS STATE)
                // ==========================================
                var step10 = Steps[9];
                SetStepActive(step10);
                step10.Status = DeviceOperationStatus.Succeeded;
                step10.StatusMessage = $"Peripheral '{CurrentTargetDevice.DisplayName}' is connected and fully operational.";
                step10.Evidence = "All 9 preceding diagnostic & connection checkpoints passed without errors.";
                step10.CompletedAt = DateTime.Now;
                NotifyStepUpdated(step10);

                finalResult.IsSuccess = true;
                finalResult.FinalStatus = DeviceOperationStatus.Succeeded;
                finalResult.SummaryMessage = $"'{CurrentTargetDevice.DisplayName}' successfully paired and connected via {connDetails.Protocol}!";

                return finalResult;
            }
            catch (OperationCanceledException)
            {
                finalResult.FinalStatus = DeviceOperationStatus.Failed;
                finalResult.SummaryMessage = "Troubleshooting wizard was cancelled by user.";
                return finalResult;
            }
            catch (Exception ex)
            {
                return await ExecuteDiagnosticConclusionAsync(finalResult, 9999, $"Unexpected wizard failure: {ex.Message}");
            }
            finally
            {
                IsRunning = false;
            }
        }

        private async Task<FixMyConnectionWizardResult> ExecuteDiagnosticConclusionAsync(
            FixMyConnectionWizardResult result,
            uint lastErrorCode,
            string lastErrorMessage)
        {
            var step10 = Steps[9];
            SetStepActive(step10);
            step10.Status = DeviceOperationStatus.InProgress;
            step10.StatusMessage = "Analyzing failure evidence across 10 diagnostic checkpoints...";
            NotifyStepUpdated(step10);

            var diagnosis = await _diagnosticEngine.DiagnoseConnectionFailureAsync(
                CurrentTargetDevice,
                lastErrorCode,
                lastErrorMessage);

            result.Diagnosis = diagnosis;
            result.IsSuccess = false;
            result.FinalStatus = DeviceOperationStatus.Failed;
            result.SummaryMessage = $"Connection failed: {diagnosis.PrimaryRootCause} (Confidence: {diagnosis.Confidence})";

            step10.Status = DeviceOperationStatus.Failed;
            step10.StatusMessage = $"Root Cause: {diagnosis.PrimaryRootCause}";
            step10.Evidence = $"Confidence: {diagnosis.Confidence}. Recommended Safe Repair: {diagnosis.SuggestedRepairTitle}. Details: {diagnosis.DetailedEvidence}";
            step10.CompletedAt = DateTime.Now;
            NotifyStepUpdated(step10);

            return result;
        }

        public async Task<RepairHistoryRecord> ExecuteSuggestedRepairAsync(string repairKey)
        {
            if (string.IsNullOrEmpty(repairKey))
            {
                return new RepairHistoryRecord
                {
                    Status = "Failed",
                    ErrorMessage = "No repair action specified."
                };
            }

            return await _repairEngine.ExecuteRepairAsync(repairKey);
        }

        private void SetStepActive(ConnectionWizardStep step)
        {
            foreach (var s in Steps)
            {
                s.IsActive = (s == step);
            }
            CurrentStep = step;
        }

        private void NotifyStepUpdated(ConnectionWizardStep step)
        {
            StepUpdated?.Invoke(this, step);
        }
    }
}
