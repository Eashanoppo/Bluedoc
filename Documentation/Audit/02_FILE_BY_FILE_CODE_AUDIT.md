# 02. File-by-File Forensic Code Audit — BlueDoc

**Product:** BlueDoc  
**Company:** UNLEFT LLC  
**Lead Developer:** Golam Morshed Eashan  
**Audit Date:** November 14, 2026  
**Auditor:** Senior Forensic Audit Consortium  

---

## 1. Domain Layer: `BlueDoc.Core`

### File: `src\BlueDoc.Core\Models\BluetoothDeviceInfo.cs`
- **File Type:** C# Source
- **Project:** BlueDoc.Core
- **Line Count:** 344 lines
- **Responsibility:** Primary domain entity representing a Bluetooth peripheral or endpoint. Tracks address, pairing state, connection status, signal strength, device class, battery level, and dynamic visual badges.
- **Referenced by:** DeviceManagementService, DeviceDiscoveryService, Win32BluetoothRadioProvider, BlePairingProvider, BluetoothDevicesViewModel, DeviceScannerViewModel.
- **Dependencies:** `System.ComponentModel.INotifyPropertyChanged`.

#### Implementation Analysis
The class implements `INotifyPropertyChanged` to allow in-place UI telemetry updates without collection recreation. It encapsulates rich signal quality calculations (`RssiToSignalBars`, `SignalQualityBadge`), category icon heuristics (`ResolveCategoryIcon`), and connection duration tracking.

#### Class and Method Analysis
- `public short? Rssi`: Signal strength in dBm. Triggers property changes for `SignalBars`, `SignalQualityBadge`, and `SignalStrengthDisplay`.
- `public bool IsConnected`: Active connection state flag.
- `public bool IsPaired`: Windows pairing database registration flag.
- `public string SignalBars`: Computes 0 to 4 signal bar indicators (`----`, `▂___`, `▂▄__`, `▂▄▆_`, `▂▄▆█`) based on dBm thresholds.
- `public string CategoryIcon`: Visual emoji/glyph indicator based on major device class and name parsing.

#### Line-Level Findings
| Line | Code/Method | Finding | Severity | Explanation |
|---|---|---|---|---|
| L49-L55 | `Rssi` setter | High property change churn | BD-MED-012 | During rapid BLE advertisement bursts, setting `Rssi` fires 4 `PropertyChanged` events per packet, causing UI dispatcher pressure. Should throttle or coalesce updates. |
| L268-L295 | `ResolveCategoryIcon()` | Name-based heuristic fallback | Informational | Soundly infers audio peripherals (e.g. "Buds", "Pods", "Headset", "WH-1000") even when the device class is reported as Generic or Unknown by the driver. |

#### Functional Assessment
- **Implemented:** In-place INPC telemetry, address normalization, RSSI conversion, category icon mapping.
- **Partially implemented:** Profile capabilities list (only populated on explicit demand).
- **Broken:** None.

---

### File: `src\BlueDoc.Core\Models\BluetoothAdapterInfo.cs`
- **File Type:** C# Source
- **Project:** BlueDoc.Core
- **Line Count:** 56 lines
- **Responsibility:** Domain model representing a physical or virtual Bluetooth adapter node enumerated by the Windows operating system.
- **Referenced by:** SetupApiHardwareInspector, AdapterService, DashboardViewModel, HardwareInspectorViewModel.

#### Class and Method Analysis
- `public string DeviceInstanceId`: PnP device instance identifier (e.g. `USB\VID_0BDA&PID_C822\00e04c000001`).
- `public uint ProblemCode`: Configuration Manager problem number (e.g., Code 10, Code 22, Code 43).
- `public AdapterStatus Status`: Operational, Disabled, DeviceError, NotPresent.
- `public HardwareIdDetails HardwareDetails`: Parsed VID, PID, and subsystem identifiers.
- `public DriverInfo Driver`: Associated driver metadata (provider, version, date).

#### Functional Assessment
- **Implemented:** Full PnP device node property encapsulation, status mapping, problem code tracking.

---

## 2. Infrastructure Layer: `BlueDoc.Infrastructure`

### File: `src\BlueDoc.Infrastructure\Hardware\SetupApiHardwareInspector.cs`
- **File Type:** C# Source
- **Project:** BlueDoc.Infrastructure
- **Line Count:** 419 lines
- **Responsibility:** Inspects the Windows Plug and Play device tree via SetupAPI and Configuration Manager (`cfgmgr32.dll`) to detect and profile Bluetooth controllers.
- **Referenced by:** AdapterService, BluetoothStartupHealthService, TrayGuardManager, DiagnosticEngine.
- **Dependencies:** `SetupApiNative`, `CfgMgr32Native`, `BluetoothNative`.

#### Implementation Analysis
Uses `SetupDiGetClassDevs` with `GUID_DEVCLASS_BLUETOOTH` (`{e0cbf06c-cd8b-4647-bb8a-263b43f0f974}`) to query all devices. For each device, calls `SetupDiEnumDeviceInfo`, queries device properties via `SetupDiGetDeviceRegistryProperty`, and calls `CM_Get_DevNode_Status` to extract real-time hardware status and problem numbers. Reads driver properties directly from the registry key opened via `SetupDiOpenDevRegKey`.

#### Class and Method Analysis
- `EnumerateAdapters()`: Enumerate all Bluetooth controllers via SetupAPI; falls back to WMI if SetupAPI fails.
- `GetPrimaryAdapter()`: Selects the primary physical controller using a prioritized heuristic:
  1. Operational physical controller (USB or PCI).
  2. Any physical controller.
  3. First detected operational node.
- `ProcessDeviceInfo()`: Extracts FriendlyName, HardwareId, DeviceDescription, DriverVersion, DriverDate, and ProblemCode.

#### Line-Level Findings
| Line | Code/Method | Finding | Severity | Explanation |
|---|---|---|---|---|
| L56-L61 | `SetupDiGetClassDevs(..., 0)` | Flags set to 0 | BD-LOW-005 | Using `Flags = 0` queries the global device database including non-present (phantom) devices. While filtered later via `IsPresent`, querying `DIGCF_PRESENT` directly is more efficient when searching for active controllers. |
| L365-L395 | `EnrichWithRadioInfo()` | P/Invoke handle leak risk | BD-MED-004 | Calls `BluetoothFindFirstRadio` and `BluetoothFindRadioClose`, but if `BluetoothGetRadioInfo` throws an exception, `CloseHandle(hRadio)` in line 390 might be bypassed. Must be in a strict `finally` block. |

#### Functional Assessment
- **Implemented:** Robust SetupAPI enumeration, PnP problem code extraction, driver registry resolution, primary controller selection.
- **Verified:** Tested on live Windows 11 system; correctly detected Realtek Bluetooth Controller (`USB\VID_0BDA&PID_C822`, Problem Code 0).

---

### File: `src\BlueDoc.Infrastructure\Hardware\HardwareRadioManager.cs`
- **File Type:** C# Source
- **Project:** BlueDoc.Infrastructure
- **Line Count:** 201 lines
- **Responsibility:** Hardware-level radio power control, PnP DevNode enabling and disabling via Configuration Manager and SetupAPI Class Installers.
- **Referenced by:** DashboardViewModel, MainViewModel.

#### Implementation Analysis
Implements two distinct mechanisms for enabling and disabling Bluetooth controllers:
1. Direct devnode power control via `CfgMgr32.CM_Enable_DevNode` and `CM_Disable_DevNode`.
2. SetupAPI Class Installer invocation via `DIF_PROPERTYCHANGE` with `DICS_ENABLE` / `DICS_DISABLE`.

#### Line-Level Findings
| Line | Code/Method | Finding | Severity | Explanation |
|---|---|---|---|---|
| L114-L124 | `CM_Enable_DevNode` / `CM_Disable_DevNode` | Requires UAC elevation | Confirmed Behavior | These APIs require administrator privileges (`SE_LOAD_DRIVER_NAME` / elevated token). When called without admin rights, returns `CR_ACCESS_DENIED`. Correctly wrapped in try/catch. |
| L185-L199 | `EnsureBluetoothServiceStarted()` | Synchronous wait on service | BD-LOW-006 | Uses `sc.WaitForStatus(..., TimeSpan.FromSeconds(5))` synchronously. Safe because it is wrapped in `Task.Run` at line 76. |

---

### File: `src\BlueDoc.Infrastructure\Audio\BluetoothAudioInspector.cs`
- **File Type:** C# Source
- **Project:** BlueDoc.Infrastructure
- **Line Count:** 100 lines
- **Responsibility:** Evaluates Windows Audio subsystem endpoints (`Win32_SoundDevice`), detects active Bluetooth audio endpoints, and identifies hands-free telephony (HFP) vs high-fidelity stereo (A2DP) bandwidth contention.
- **Referenced by:** BluetoothConnectionOrchestrator, AudioDiagnosticsViewModel.

#### Class and Method Analysis
- `AnalyzeAudioSubsystem()`: Executes a WMI query against `Win32_SoundDevice`, identifies devices with `BTHENUM` or `BTHHFENUM` hardware IDs, and inspects whether the microphone endpoint is actively engaged in recording mode.

#### Line-Level Findings
| Line | Code/Method | Finding | Severity | Explanation |
|---|---|---|---|---|
| L17 | `ManagementObjectSearcher` | Synchronous WMI call | BD-MED-005 | Querying WMI synchronously can block the calling thread for 500ms-2000ms if the WMI service (`winmgmt`) is under load or initializing. Should be executed asynchronously with a strict timeout. |
| L77-L88 | Contention Detection | Accurate bandwidth analysis | Feature Highlight | Correctly identifies when Windows drops Bluetooth headphones from A2DP (stereo 48kHz) to HFP (mono 16kHz voice) due to an open microphone channel (e.g. Discord/Teams). |

---

### File: `src\BlueDoc.Infrastructure\Repairs\SafeRepairEngine.cs`
- **File Type:** C# Source
- **Project:** BlueDoc.Infrastructure
- **Line Count:** 477 lines
- **Responsibility:** Safe, guided execution of 8 approved repair operations, permission checking, independent post-repair verification, and audit logging to SQLite.
- **Referenced by:** RepairService, SafeRepairCenterViewModel.

#### Class and Method Analysis
- `GetAllRepairDefinitions()`: Returns metadata for 8 repair actions (Key, Title, Description, Consequences, Admin requirements, Verification procedure).
- `ExecuteRepairAsync(repairKey)`: Verifies administrator elevation for protected operations, executes repair, runs independent verification, and records outcome in SQLite.
- `RestartWindowsServiceAsync()`: Stops and restarts a Windows service using `ServiceController` with timeout.
- `CycleRadioAsync()`: Intended to power-cycle the controller radio transceiver.

#### Line-Level Findings
| Line | Code/Method | Finding | Severity | Explanation |
|---|---|---|---|---|
| L343-L351 | `CycleRadioAsync()` | Does not power-cycle hardware | BD-HIGH-004 | The method claims to cycle the radio transceiver, but simply calls `RestartWindowsServiceAsync("bthserv")`. It does NOT invoke `HardwareRadioManager.SetRadioPowerStateAsync()` to toggle the actual PnP devnode. |
| L421-L428 | `LaunchNativeTroubleshooter()` | Invokes deprecated `msdt.exe` | BD-MED-006 | Calls `msdt.exe /id BluetoothDiagnostic`. In Windows 11 Build 22631+ (23H2/24H2), Microsoft has officially deprecated MSDT, causing command failure. The fallback to `ms-settings:troubleshoot` is present, but MSDT should be bypassed on modern builds. |
| L395-L410 | `LaunchDeviceManager()` | External process launch | Expected | Intentionally launches `devmgmt.msc` via `Process.Start` as a documented diagnostic aid for the user. |

---

### File: `src\BlueDoc.Infrastructure\Reporting\PdfReportGenerator.cs`
- **File Type:** C# Source
- **Project:** BlueDoc.Infrastructure
- **Line Count:** 387 lines
- **Responsibility:** Custom zero-dependency PDF 1.4 document generator. Produces branded diagnostic reports with tables, status badges, and metadata without third-party libraries.
- **Referenced by:** ReportGenerator, DiagnosticReportViewModel.

#### Implementation Analysis
Constructs a valid PDF-1.4 binary stream using standard PDF operators (`BT`, `ET`, `Tf`, `rg`, `re`, `f`, `m`, `l`, `S`), object dictionary cross-reference tables (`xref`), and catalog roots. Verified in Phase 6 tests; produces valid 9 KB PDF files readable by Adobe Acrobat, Microsoft Edge, and Chrome.

---

## 3. Bluetooth Layer: `BlueDoc.Bluetooth`

### File: `src\BlueDoc.Bluetooth\Legacy\Win32BluetoothRadioProvider.cs`
- **File Type:** C# Source
- **Project:** BlueDoc.Bluetooth
- **Line Count:** 361 lines
- **Responsibility:** Classic Bluetooth discovery and device enumeration using the Win32 Bluetooth API (`bthprops.cpl`). Works across Windows 7 SP1, 10, and 11.
- **Referenced by:** BluetoothManager.
- **Dependencies:** `BluetoothNative`, `PnpBluetoothDeviceInspector`, `DeviceClassDecoder`.

#### Class and Method Analysis
- `GetPairedDevicesAsync()`: Combines PnP device enumeration with cached Win32 inquiry (`fIssueInquiry = false`) for instant response.
- `DiscoverDevicesAsync()`: Executes active radio inquiry (`fIssueInquiry = true`) with configurable timeout multiplier (`cTimeoutMultiplier`).
- `RemoveDeviceAsync()`: Calls `BluetoothNative.BluetoothRemoveDevice` to unpair devices.

#### Line-Level Findings
| Line | Code/Method | Finding | Severity | Explanation |
|---|---|---|---|---|
| L262 | `cTimeoutMultiplier` | Timeout calculation clamped | Verified OK | Calculates multiplier as `timeoutSeconds / 1.28`, clamped between 1 and 10 (1.28s to 12.8s inquiry window). |
| L219, L328 | `BluetoothFindDeviceClose()` | Safe handle disposal | Verified OK | Properly calls `BluetoothFindDeviceClose(hFind)` in `finally` blocks, preventing kernel resource leaks during discovery. |

---

### File: `src\BlueDoc.Bluetooth\BLE\BleAdvertisementWatcherProvider.cs`
- **File Type:** C# Source
- **Project:** BlueDoc.Bluetooth
- **Line Count:** 249 lines
- **Responsibility:** Real-time BLE beacon and peripheral discovery on Windows 10/11 using `Windows.Devices.Bluetooth.Advertisement.BluetoothLEAdvertisementWatcher`.
- **Referenced by:** DeviceDiscoveryService.
- **Dependencies:** `Windows.winmd`.

#### Class and Method Analysis
- `StartScanning()`: Initializes active scanning mode (`BluetoothLEScanningMode.Active`) and subscribes to `Received` and `Stopped` events.
- `OnAdvertisementReceived()`: Extracts MAC address, LocalName, RSSI (dBm), manufacturer data, and updates device records.

#### Line-Level Findings
| Line | Code/Method | Finding | Severity | Explanation |
|---|---|---|---|---|
| L4 | `using Windows.Devices...` | Direct WinMD reference | BD-CRIT-002 | Direct reference to WinRT types in class fields causes `TypeLoadException` if this class is loaded by the CLR on Windows 7 SP1, where `Windows.winmd` is absent. |

---

### File: `src\BlueDoc.Bluetooth\BLE\BlePairingProvider.cs`
- **File Type:** C# Source
- **Project:** BlueDoc.Bluetooth
- **Line Count:** 176 lines
- **Responsibility:** In-app custom pairing for BLE peripherals using `Windows.Devices.Enumeration.DeviceInformationCustomPairing`.
- **Referenced by:** DeviceManagementService.

#### Class and Method Analysis
- `PairBleDeviceAsync()`: Connects to peripheral via `BluetoothLEDevice.FromBluetoothAddressAsync()`, registers custom pairing event handlers for `PairingRequested`, supports `ConfirmOnly`, `ProvidePin`, and `ConfirmPinMatch` with async callback.

#### Line-Level Findings
| Line | Code/Method | Finding | Severity | Explanation |
|---|---|---|---|---|
| L54-L101 | `PairingRequested` handler | Proper deferral management | Verified OK | Correctly obtains `args.GetDeferral()` and calls `deferral.Complete()` in a `finally` block to support asynchronous user PIN input. |

---

### File: `src\BlueDoc.Bluetooth\BLE\GattProfileCatalog.cs`
- **File Type:** C# Source
- **Project:** BlueDoc.Bluetooth
- **Line Count:** 263 lines
- **Responsibility:** Static catalog of standard Bluetooth SIG GATT service and characteristic UUIDs (Generic Access, Device Information, Battery, HID, etc.).
- **Referenced by:** BleExplorerViewModel.

#### Line-Level Findings
| Line | Code/Method | Finding | Severity | Explanation |
|---|---|---|---|---|
| L121-L260 | `GetStandardPeripheralProfile()` | Fabricated / Synthetic Profile | BD-HIGH-001 | Synthesizes an artificial GATT profile based on device category rather than performing live attribute discovery against the connected device. Used by `BleExplorerViewModel`. |

---

## 4. Diagnostics Layer: `BlueDoc.Diagnostics`

### File: `src\BlueDoc.Diagnostics\Engine\DiagnosticEngine.cs`
- **File Type:** C# Source
- **Project:** BlueDoc.Diagnostics
- **Line Count:** 165 lines
- **Responsibility:** Orchestrates system evidence collection (`DiagnosticContext`) and sequential execution of the 12 diagnostic rules.
- **Referenced by:** DiagnosticsService, MainViewModel.

#### Class and Method Analysis
- `RegisterDefaultRules()`: Registers all 12 diagnostic rules in deterministic order:
  1. `OsCompatibilityRule`
  2. `AdapterDetectionRule`
  3. `HardwareStatusRule`
  4. `DriverHealthRule`
  5. `ServiceStatusRule`
  6. `AudioServiceRule`
  7. `RadioAvailabilityRule`
  8. `DiscoveryReadinessRule`
  9. `PairingReadinessRule`
  10. `BleAvailabilityRule`
  11. `BleGattSupportRule`
  12. `EventLogHealthRule`
- `RunFullDiagnosticsAsync()`: Builds context asynchronously, evaluates each rule, tracks elapsed time, and logs results.

#### Line-Level Findings
| Line | Code/Method | Finding | Severity | Explanation |
|---|---|---|---|---|
| L86 | Cancellation Check | Responsive cancellation | Verified OK | Checks `cancellationToken.IsCancellationRequested` before each rule, allowing immediate user abort. |
| L89 | Result Propagation | Sequential context enrichment | Verified OK | Appends previous test results to `context.PreviousResults`, allowing dependent rules (e.g. Discovery) to inspect prior hardware findings. |

---

## 5. Application Layer: `BlueDoc.Application`

### File: `src\BlueDoc.Application\Services\BluetoothConnectionOrchestrator.cs`
- **File Type:** C# Source
- **Project:** BlueDoc.Application
- **Line Count:** 361 lines
- **Responsibility:** Routes peripheral connections through protocol-appropriate paths (Audio A2DP/HFP, HID Input, BLE GATT, or RFCOMM).
- **Referenced by:** FixMyConnectionWizardService.
- **Dependencies:** `BluetoothNative`, `BluetoothAudioInspector`.

#### Line-Level Findings
| Line | Code/Method | Finding | Severity | Explanation |
|---|---|---|---|---|
| L108-L142 | `ConnectAudioPeripheralAsync()` | Direct Win32 Profile Binding | Verified OK | Calls `BluetoothSetServiceState` for `AudioSinkServiceGuid`, `HandsfreeServiceGuid`, `HeadsetServiceGuid`, and `AvrcpServiceGuid`. |
| L244-L261 | `ConnectBlePeripheralAsync()` | Mocked / Fake Connection | BD-HIGH-002 | Does NOT establish a live GATT session or invoke `BluetoothLEDevice`. Merely sets `device.IsConnected = true` and returns a fake `DeviceOperationResult.Ok`. |

---

### File: `src\BlueDoc.Application\Services\DeviceManagementService.cs`
- **File Type:** C# Source
- **Project:** BlueDoc.Application
- **Line Count:** 479 lines
- **Responsibility:** Connects, disconnects, pairs, and unpairs devices using native Win32 and WinRT APIs.
- **Referenced by:** BluetoothDevicesViewModel, DeviceScannerViewModel, FixMyConnectionWizardService.

#### Line-Level Findings
| Line | Code/Method | Finding | Severity | Explanation |
|---|---|---|---|---|
| L67-L72 | `BluetoothSetServiceState` | Native Win32 Service Binding | Verified OK | Successfully enables target profile GUIDs on the primary radio controller. |
| L225-L230 | `BluetoothAuthenticateDevice` | Native Classic Pairing | Verified OK | Invokes `bthprops.cpl!BluetoothAuthenticateDevice` with optional PIN parameter. Handles error codes 1244 (auth rejected) and 1223 (cancelled). |
| L420-L460 | `LaunchWindowsPairingWizard()` | External launch fallback | BD-MED-002 | Contains code to launch `ms-settings:bluetooth` or `control.exe /name Microsoft.DevicesAndPrinters`. Orphaned from the primary UI, but exists as dead code. |

---

### File: `src\BlueDoc.Application\Services\FixMyConnectionWizardService.cs`
- **File Type:** C# Source
- **Project:** BlueDoc.Application
- **Line Count:** 748 lines
- **Responsibility:** 10-step interactive connection doctor wizard state machine.
- **Referenced by:** FixMyConnectionWizardViewModel.

#### Implementation Analysis
Comprehensive 10-step interactive state machine covering:
1. Target Selection
2. Adapter Inspection
3. System Services
4. Discovery Probing
5. Pairing Readiness
6. Native Windows Pairing
7. Authentication Handshake
8. Protocol-Appropriate Connection
9. Audio / Operational Verification
10. Root-Cause Diagnosis & Safe Repair

---

## 6. Presentation Layer: `BlueDoc.UI`

### File: `src\BlueDoc.UI\ViewModels\MainViewModel.cs`
- **File Type:** C# Source
- **Project:** BlueDoc.UI
- **Line Count:** 452 lines
- **Responsibility:** Root application ViewModel. Hosts all 19 sub-ViewModels, manages sidebar navigation, global shortcuts, and theme switching.
- **Referenced by:** `MainWindow.xaml`.

#### Line-Level Findings
| Line | Code/Method | Finding | Severity | Explanation |
|---|---|---|---|---|
| L158-L200 | Constructor | Tight coupling / No DI Container | BD-MED-001 | Manually instantiates 19 ViewModels and all application services using `new` keywords. Violates Inversion of Control and makes unit testing of MainViewModel difficult. |
| L367, L371, L380 | `NavigateTo()` | Fire-and-forget unawaited tasks | BD-LOW-002 | Uses `_ = DashboardVM.LoadDataAsync();` and `_ = BluetoothDevicesVM.LoadPairedDevicesAsync();`. Any unhandled exception thrown in these methods cannot be observed by the caller. |

---

### File: `src\BlueDoc.UI\ViewModels\BleExplorerViewModel.cs`
- **File Type:** C# Source
- **Project:** BlueDoc.UI
- **Line Count:** 343 lines
- **Responsibility:** Bluetooth Low Energy GATT Explorer screen ViewModel.
- **Referenced by:** `BleExplorerView.xaml`.

#### Line-Level Findings
| Line | Code/Method | Finding | Severity | Explanation |
|---|---|---|---|---|
| L164 | `LoadGattProfile...` | Synthetic profile loaded | BD-HIGH-001 | Calls `GattProfileCatalog.GetStandardPeripheralProfile` instead of discovering real GATT services on the peripheral. |
| L218-L229 | `ReadCharacteristic` | Fabricated characteristic read | BD-HIGH-001 | Formats synthetic string `Verified GATT Read at {timestamp}: [GattCommunicationStatus.Success]` instead of reading value bytes from the device. |
| L294 | `PerformSafeWrite` | Fabricated characteristic write | BD-HIGH-001 | Displays confirmation dialog and then logs a fake success message without sending any byte buffer to the peripheral. |
| L256-L264 | `ToggleNotification` | Fake CCCD subscription | BD-HIGH-001 | Toggles a local boolean without writing to the Client Characteristic Configuration Descriptor on the device. |

---

### File: `src\BlueDoc.UI\Tray\TrayGuardManager.cs`
- **File Type:** C# Source
- **Project:** BlueDoc.UI
- **Line Count:** 186 lines
- **Responsibility:** System tray icon, notifications, and background adapter health monitoring.
- **Referenced by:** `MainWindow.xaml.cs`.

#### Line-Level Findings
| Line | Code/Method | Finding | Severity | Explanation |
|---|---|---|---|---|
| L67 | `StartMonitoring()` | 10-second polling interval | BD-MED-003 | Executes a full SetupAPI hardware enumeration every 10 seconds. On older laptops, event-driven `WM_DEVICECHANGE` is preferred to prevent continuous PnP registry querying. |

---

## 7. Testing Layer: `BlueDoc.Tests`

### File: `tests\BlueDoc.Tests\Program.cs`
- **File Type:** C# Source
- **Project:** BlueDoc.Tests
- **Line Count:** 65 lines
- **Responsibility:** Entry point for automated test runner. Executes unit tests, scenario matrix, benchmarks, and live integration tests.

#### Line-Level Findings
| Line | Code/Method | Finding | Severity | Explanation |
|---|---|---|---|---|
| L39-L42 | Duplicate Test Calls | Redundant test execution | BD-LOW-001 | Lines 39-42 duplicate lines 35-38, executing `FixConnectionDoctorAndPairingTests`, `AccessibilityVerificationTests`, `PerformanceBenchmarkHarness`, and `BluetoothFunctionalMatrixTests` twice. Doubles test execution time. |

