# BlueDoc Centralized Forensic Bug Register

**Document ID:** `BD-AUDIT-12`  
**Auditor Roles:** Principal Software Architect, Senior C# / .NET Engineer, Senior WPF Engineer, Bluetooth Protocol Specialist, QA Engineer, Application Security Engineer  
**Target Solution:** `BlueDoc.sln`  
**Audit Date:** November 2026  
**Status:** COMPLETE / EVIDENCE-BASED  

---

## 1. Defect Severity Classification Matrix

| Severity Level | Definition | Count |
|---|---|---|
| **CRITICAL** | Application crash on supported OS, architectural specification violation preventing deployment, or catastrophic unhandled exception. | **2** |
| **HIGH** | Core functional requirement simulated with mock data, feature completely non-operational at runtime, or misleading user action. | **4** |
| **MEDIUM** | Performance degradation, resource leak, missing elevation workflow, UI cross-thread exception hazard, or accessibility violation. | **8** |
| **LOW** | Code duplication, styling inconsistency, minor resource key collision, or non-fatal timing redundancy. | **6** |
| **TOTAL** | | **20** |

---

## 2. Complete Forensic Defect Register

---

### BD-CRIT-001: Target Framework Mismatch (.NET 4.7.1 vs .NET 4.8)
- **Severity:** **CRITICAL**
- **Category:** Architecture & Platform
- **File Path:** All 8 `.csproj` files (`src\*\*.csproj`, `tests\*\*.csproj`)
- **Line Numbers:** e.g., `src\BlueDoc.UI\BlueDoc.UI.csproj` line 22
- **Technical Explanation:** PRD Section 10 and SDD Section 2.1 mandate target framework `Microsoft .NET Framework 4.8`. All 8 project files explicitly compile against `<TargetFrameworkVersion>v4.7.1</TargetFrameworkVersion>`.
- **Reproduction Steps:**
  1. Inspect any project file in text editor.
  2. Observe `<TargetFrameworkVersion>v4.7.1</TargetFrameworkVersion>`.
- **Root Cause:** Initial project scaffolding targeted v4.7.1 for early WinRT projection compatibility and was never updated to the PRD mandate.
- **Actual Impact:** Misses .NET 4.8 High-DPI v2 enhancements, modern WCF/WPF accessibility improvements, and introduces ambiguity for enterprise deployment.
- **Recommended Solution:** Retarget all eight `.csproj` files to `<TargetFrameworkVersion>v4.8</TargetFrameworkVersion>`. Update `packages.config` and Inno Setup script accordingly.
- **Verification Method:** Rebuild solution using MSBuild with `/p:TargetFrameworkVersion=v4.8`; verify zero build errors.

---

### BD-CRIT-002: Fatal WinRT TypeLoadException Hazard on Windows 7 SP1
- **Severity:** **CRITICAL**
- **Category:** Platform Compatibility & Runtime Crash
- **File Path:** `src\BlueDoc.Bluetooth\Services\DeviceDiscoveryService.cs`
- **Line Numbers:** Line 36
- **Technical Explanation:** `DeviceDiscoveryService` declares a private instance field of type `BleAdvertisementWatcherProvider`:
  ```csharp
  private readonly BleAdvertisementWatcherProvider _bleWatcher;
  ```
  `BleAdvertisementWatcherProvider` directly references `Windows.Devices.Bluetooth.Advertisement.BluetoothLEAdvertisementWatcher` from `Windows.winmd`. On Windows 7 SP1, `Windows.winmd` does not exist. When the CLR JIT-compiles `DeviceDiscoveryService` on Windows 7, it attempts to load WinRT type metadata and immediately throws an unhandled `TypeLoadException` or `FileNotFoundException`, terminating the process.
- **Reproduction Steps:**
  1. Deploy BlueDoc binary to a clean Windows 7 SP1 x64 virtual machine.
  2. Launch `BlueDoc.exe`.
  3. Navigate to Device Scanner or initiate discovery.
  4. Application crashes immediately with `TypeLoadException`.
- **Root Cause:** Concrete compile-time binding to Windows 10 WinRT SDK types in a core service instantiated unconditionally on all operating systems.
- **Actual Impact:** Immediate application crash on Windows 7 SP1, violating PRD Section 1 OS support guarantee.
- **Recommended Solution:** Extract BLE watcher into an interface `IBleWatcherProvider`. Implement dynamic OS version detection. On Windows 7, instantiate a `NullBleWatcherProvider`; on Windows 10/11, dynamically load the WinRT implementation from an isolated adapter assembly.
- **Verification Method:** Launch application in Windows 7 SP1 environment; verify clean startup and graceful fallback to Classic Bluetooth discovery.

---

### BD-HIGH-001: Fabricated / Mock BLE GATT Explorer Implementation (FR-16)
- **Severity:** **HIGH**
- **Category:** Bluetooth Core & Functional Requirement
- **File Path:** `src\BlueDoc.UI\ViewModels\BleExplorerViewModel.cs`
- **Line Numbers:** Lines 162-178, 220-275
- **Technical Explanation:** PRD FR-16 requires an Advanced Bluetooth LE Explorer capable of exploring live GATT services, reading characteristics, writing byte buffers, and subscribing to CCCD notifications. The actual implementation in `BleExplorerViewModel` calls `GattProfileCatalog.GetStandardPeripheralProfile(device.Name)` which returns hardcoded synthetic GATT structures. Characteristic reads return simulated strings (`"Verified GATT Read at {timestamp}"`), and writes do not emit packets to any physical radio.
- **Reproduction Steps:**
  1. Open BlueDoc and navigate to "BLE Explorer".
  2. Select any connected BLE peripheral.
  3. Observe that regardless of the device type, identical generic services appear.
  4. Click "Read" on any characteristic; observe synthesized timestamp string.
- **Root Cause:** Implementation was scaffolded with a mock catalog during initial development and never replaced with live WinRT `GattDeviceService` and `GattCharacteristic` APIs.
- **Actual Impact:** FR-16 is non-functional against real hardware; users cannot inspect or troubleshoot BLE devices.
- **Recommended Solution:** Replace `GattProfileCatalog` with native calls to `BluetoothLEDevice.FromBluetoothAddressAsync()`, `GetGattServicesAsync()`, and `characteristic.ReadValueAsync()`.
- **Verification Method:** Connect a real BLE peripheral (e.g. Heart Rate Monitor or Nordic nRF development board); verify live UUIDs and dynamic sensor byte payloads are displayed.

---

### BD-HIGH-002: Simulated BLE Peripheral Connection in Orchestrator
- **Severity:** **HIGH**
- **Category:** Bluetooth Connection Management
- **File Path:** `src\BlueDoc.Bluetooth\Services\BluetoothConnectionOrchestrator.cs`
- **Line Numbers:** Lines 244-261 (`ConnectBlePeripheralAsync`)
- **Technical Explanation:** In `BluetoothConnectionOrchestrator`, connecting to a BLE peripheral executes:
  ```csharp
  device.IsConnected = true;
  return DeviceOperationResult.Ok("Connected successfully (Simulated)");
  ```
  No GATT connection is established, no WinRT `BluetoothLEDevice` handle is opened, and no radio connection is created.
- **Reproduction Steps:**
  1. In Device Scanner, select a discovered BLE peripheral.
  2. Click "Connect".
  3. UI displays device as "Connected", but Windows Bluetooth settings and physical device show it is disconnected.
- **Root Cause:** Incomplete method stub with placeholder success return.
- **Actual Impact:** False connection status presented to user; BLE devices cannot be managed.
- **Recommended Solution:** Implement live connection logic via `BluetoothLEDevice.FromBluetoothAddressAsync` and establish a maintained GATT session.
- **Verification Method:** Pair a BLE sensor; click Connect; verify Windows Bluetooth status transitions to connected.

---

### BD-HIGH-003: Inaccurate Radio Power Cycle Action in Safe Repair Engine
- **Severity:** **HIGH**
- **Category:** Diagnostics & Repair
- **File Path:** `src\BlueDoc.Infrastructure\Repairs\SafeRepairEngine.cs`
- **Line Numbers:** Lines 343-351 (`CycleRadioAsync`)
- **Technical Explanation:** The repair action "Cycle Bluetooth Radio" claims to reset the controller transceiver hardware. However, the method merely calls `RestartWindowsServiceAsync("bthserv")`. Meanwhile, `HardwareRadioManager.cs` already contains genuine PnP DevNode cycling (`CM_Disable_DevNode` / `CM_Enable_DevNode` / `DIF_PROPERTYCHANGE`). The repair engine fails to invoke the real hardware radio manager.
- **Reproduction Steps:**
  1. Trigger "Reset & Cycle Bluetooth Radio Transceiver" from Safe Repair Center.
  2. Observe system services; `bthserv` restarts, but the physical USB/PCIe radio DevNode is never disabled or power-cycled.
- **Root Cause:** Disconnected orchestration; `SafeRepairEngine` does not inject or utilize `IHardwareRadioManager`.
- **Actual Impact:** Hardware FIFO stalls and controller firmware desynchronization are not resolved by the repair action.
- **Recommended Solution:** Inject `IHardwareRadioManager` into `SafeRepairEngine` and invoke `SetRadioStateAsync(false)` followed by `SetRadioStateAsync(true)` or call `CM_Disable_DevNode`/`CM_Enable_DevNode`.
- **Verification Method:** Monitor Device Manager during repair; verify adapter disappears and reappears as hardware is power-cycled.

---

### BD-HIGH-004: Dead / Orphaned External Pairing Fallback
- **Severity:** **HIGH**
- **Category:** Architecture & Code Hygiene
- **File Path:** `src\BlueDoc.Bluetooth\Services\DeviceManagementService.cs`
- **Line Numbers:** Lines 145-165 (`LaunchWindowsPairingWizard`)
- **Technical Explanation:** `DeviceManagementService` contains a method `LaunchWindowsPairingWizard()` that launches `ms-settings:bluetooth` or `control.exe /name Microsoft.DevicesAndPrinters`. This method is orphaned dead code; it is never called by any ViewModel or command in the application.
- **Reproduction Steps:**
  1. Search codebase for references to `LaunchWindowsPairingWizard`.
  2. Observe 0 callers.
- **Root Cause:** Leftover fallback code from early prototyping.
- **Actual Impact:** Dead code bloat; potential confusion for future developers.
- **Recommended Solution:** Remove the unused method or document it explicitly as an external diagnostic utility.
- **Verification Method:** Code analysis confirming 0 references.

---

### BD-MED-001: Missing Dependency Injection Container in UI Shell
- **Severity:** **MEDIUM**
- **Category:** MVVM & Architecture
- **File Path:** `src\BlueDoc.UI\ViewModels\MainViewModel.cs`
- **Line Numbers:** Lines 85-142
- **Technical Explanation:** `MainViewModel` manually instantiates all 19 ViewModels and all domain services using concrete `new` keywords in its constructor.
- **Reproduction Steps:**
  1. Inspect `MainViewModel.cs` constructor.
  2. Observe tight coupling: `new DashboardViewModel(...)`, `new BluetoothDevicesViewModel(...)`, etc.
- **Root Cause:** Rapid prototyping without establishing an IoC container bootstrapper.
- **Actual Impact:** High coupling, inability to mock services for automated UI testing, redundant memory overhead from eager instantiation of all 19 views at startup.
- **Recommended Solution:** Introduce `Microsoft.Extensions.DependencyInjection` in `App.xaml.cs`. Register ViewModels as transient or scoped and inject dependencies via constructor injection.
- **Verification Method:** Verify all ViewModels resolve through `IServiceProvider`.

---

### BD-MED-002: Inefficient SetupAPI Polling in Tray Guard
- **Severity:** **MEDIUM**
- **Category:** Performance & Battery Efficiency
- **File Path:** `src\BlueDoc.Application\Services\TrayGuardManager.cs`
- **Line Numbers:** Lines 75-98
- **Technical Explanation:** `TrayGuardManager` runs a `System.Timers.Timer` every 10 seconds that executes a full SetupAPI hardware enumeration and WMI query.
- **Reproduction Steps:**
  1. Run BlueDoc minimized to system tray.
  2. Monitor CPU wakeups and kernel polling frequency using Windows Performance Analyzer.
- **Root Cause:** Polling timer used instead of Windows event notifications.
- **Actual Impact:** Increased CPU cycles, memory allocations, and battery consumption on mobile laptops.
- **Recommended Solution:** Register for native `WM_DEVICECHANGE` window messages via `HwndSource.AddHook`. Windows automatically sends `DBT_DEVICEARRIVAL` and `DBT_DEVICEREMOVECOMPLETE` when Bluetooth state changes.
- **Verification Method:** Measure 0% CPU consumption while idle in tray; verify instant notification when a USB Bluetooth adapter is inserted.

---

### BD-MED-003: Native P/Invoke Handle Leak in Hardware Inspector
- **Severity:** **MEDIUM**
- **Category:** Reliability & Kernel Resource Management
- **File Path:** `src\BlueDoc.Bluetooth\Services\SetupApiHardwareInspector.cs`
- **Line Numbers:** Lines 145-162 (`EnrichWithRadioInfo`)
- **Technical Explanation:** `BluetoothFindFirstRadio` returns `IntPtr hRadio` and `IntPtr hFind`. These handles are closed inside normal execution flow, but are NOT protected by a `try ... finally` block. If `BluetoothGetRadioInfo` throws an exception, handles remain unclosed in the OS kernel.
- **Reproduction Steps:**
  1. Simulate an exception during `BluetoothGetRadioInfo`.
  2. Inspect handle table of `BlueDoc.exe` in Process Explorer; observe leaked radio handles.
- **Root Cause:** Missing `try/finally` resource cleanup pattern.
- **Actual Impact:** Slow resource leakage leading to handle exhaustion over long running sessions.
- **Recommended Solution:** Enclose handle operations in standard `try { ... } finally { CloseHandle(hRadio); BluetoothFindRadioClose(hFind); }` blocks.
- **Verification Method:** Perform 1,000 rapid hardware refreshes; verify handle count remains constant.

---

### BD-MED-004: Synchronous WMI Query Blocking UI Thread in Audio Inspector
- **Severity:** **MEDIUM**
- **Category:** Performance & UI Responsiveness
- **File Path:** `src\BlueDoc.Infrastructure\Audio\BluetoothAudioInspector.cs`
- **Line Numbers:** Lines 88-112
- **Technical Explanation:** Querying `Win32_SoundDevice` via WMI `ManagementObjectSearcher` is executed synchronously on the calling thread. When navigating to `AudioDiagnosticsView`, the UI thread freezes for 500ms to 2000ms while WMI queries the system repository.
- **Reproduction Steps:**
  1. Click "Audio Diagnostics" in the navigation rail.
  2. Notice 1-second perceptible UI hesitation / freeze before the page loads.
- **Root Cause:** Synchronous WMI query invoked during view navigation.
- **Actual Impact:** Noticeable UI hitch and poor user experience.
- **Recommended Solution:** Wrap WMI query in `Task.Run(() => ...)` and await asynchronously.
- **Verification Method:** Profile navigation to `AudioDiagnosticsView`; verify UI thread execution time is < 16ms (60 FPS).

---

### BD-MED-005: MSDT Troubleshooter Deprecation on Windows 11 (23H2 / 24H2)
- **Severity:** **MEDIUM**
- **Category:** Windows Systems Integration
- **File Path:** `src\BlueDoc.Infrastructure\Repairs\SafeRepairEngine.cs`
- **Line Numbers:** Line 374
- **Technical Explanation:** `LaunchNativeTroubleshooter()` invokes `Process.Start("msdt.exe", "/id BluetoothDiagnostic")`. Microsoft has officially retired MSDT in Windows 11. On modern builds, this command produces an error dialog or fails silently.
- **Reproduction Steps:**
  1. Run BlueDoc on Windows 11 Build 22631 (23H2) or Build 26100 (24H2).
  2. Click "Launch Windows Troubleshooter" in Safe Repair Center.
  3. Observe MSDT retirement error message dialog.
- **Root Cause:** Invoking legacy diagnostic executable on operating systems where it has been decommissioned.
- **Actual Impact:** Repair action fails on all modern Windows 11 installations.
- **Recommended Solution:** Add OS version check. On Windows 11, launch `ms-settings:troubleshoot`. On Windows 10/7, launch `msdt.exe`.
- **Verification Method:** Execute action on Windows 11; verify Settings troubleshooter page opens cleanly.

---

### BD-MED-006: Hardcoded Light Theme Contrast Failure on Warning Badges
- **Severity:** **MEDIUM**
- **Category:** UI/UX & Accessibility (WCAG 2.1 AA)
- **File Path:** `src\BlueDoc.UI\Themes\LightTheme.xaml`
- **Line Numbers:** Line 42 (`WarningBrush`)
- **Technical Explanation:** `WarningBrush` is defined as `#F59E0B` (Amber 500). In Light Theme, warning text and badge borders are rendered with this brush against white (`#FFFFFF`) card backgrounds. The resulting contrast ratio is **2.1:1**, severely violating WCAG 2.1 AA requirement (minimum 4.5:1 for normal text).
- **Reproduction Steps:**
  1. Switch to Light Theme in Settings.
  2. Navigate to Diagnostic Center with an active warning rule.
  3. Inspect warning text; text is difficult to read against white background.
- **Root Cause:** Reusing dark-theme amber palette without adjusting luminance for light backgrounds.
- **Actual Impact:** Fails accessibility standards; illegible for users with visual impairments.
- **Recommended Solution:** Change `WarningBrush` in `LightTheme.xaml` to `#B45309` (Amber 700) or `#92400E` (Amber 800), achieving a contrast ratio of > 5.2:1.
- **Verification Method:** Run `AccessibilityVerificationTests.cs`; verify all contrast assertions pass.

---

### BD-MED-007: Unchecked Cross-Thread Collection Mutation in Connection History
- **Severity:** **MEDIUM**
- **Category:** WPF Threading & Reliability
- **File Path:** `src\BlueDoc.UI\ViewModels\ConnectionHistoryViewModel.cs`
- **Line Numbers:** Lines 115-122
- **Technical Explanation:** When history records refresh from the background SQLite poller, `RecentConnections.Clear()` and `Add()` are called directly from a worker task thread without `App.Current.Dispatcher.InvokeAsync()`.
- **Reproduction Steps:**
  1. Open Connection History view.
  2. Trigger rapid background device events.
  3. Application crashes intermittently with `System.NotSupportedException`: "This type of CollectionView does not support changes to its SourceCollection from a thread different from the Dispatcher thread."
- **Root Cause:** Missing Dispatcher synchronization wrapper.
- **Actual Impact:** Random crashes when viewing connection history.
- **Recommended Solution:** Wrap collection mutation in `App.Current.Dispatcher.InvokeAsync(() => { RecentConnections.Clear(); ... });`.
- **Verification Method:** Run 100 simultaneous simulated connection events while on the view; verify zero crashes.

---

### BD-MED-008: Missing In-App UAC Elevation Workflow for Safe Repairs
- **Severity:** **MEDIUM**
- **Category:** Security & User Experience
- **File Path:** `src\BlueDoc.Infrastructure\Repairs\SafeRepairEngine.cs` & `app.manifest`
- **Line Numbers:** Lines 192-200 in `SafeRepairEngine.cs`
- **Technical Explanation:** Multiple repairs require administrative elevation (`RequiresAdmin = true`). When run non-elevated, repairs fail with an error message instructing the user to restart as admin. However, the UI does NOT provide an elevation button or UAC restart workflow.
- **Reproduction Steps:**
  1. Launch BlueDoc as a standard non-admin user.
  2. Attempt "Restart Bluetooth Support Service".
  3. Operation fails with "Administrative privileges required." No button exists to elevate.
- **Root Cause:** Lack of an elevation bootstrap helper (`ProcessStartInfo.Verb = "runas"`).
- **Actual Impact:** Standard users are blocked from repairing Bluetooth without manual technical workarounds.
- **Recommended Solution:** Add an in-app "Elevate & Restart" dialog that re-launches `BlueDoc.exe` with `Verb = "runas"`. Display UAC shield icon on admin repair buttons.
- **Verification Method:** Test non-elevated repair; click "Elevate"; verify standard Windows UAC prompt appears and BlueDoc restarts in elevated mode.

---

### BD-LOW-001: Duplicate Test Execution in Program.cs
- **Severity:** **LOW**
- **Category:** Testing & QA
- **File Path:** `tests\BlueDoc.Tests\Program.cs`
- **Line Numbers:** Lines 39-42
- **Technical Explanation:** Lines 39-42 duplicate lines 35-38, executing the entire benchmark suite and 18-scenario functional matrix twice in every test run.
- **Reproduction Steps:** Run `BlueDoc.Tests.exe`; observe console logs showing duplicate test runs.
- **Root Cause:** Copy-paste merge error.
- **Actual Impact:** Doubles automated test execution time from 20s to 42s.
- **Recommended Solution:** Remove lines 39-42.
- **Verification Method:** Run tests; verify single execution and halved runtime.

---

### BD-LOW-002: Fire-and-Forget Navigation Tasks in MainViewModel
- **Severity:** **LOW**
- **Category:** Concurrency & Error Handling
- **File Path:** `src\BlueDoc.UI\ViewModels\MainViewModel.cs`
- **Line Numbers:** Lines 195-210 (`NavigateTo`)
- **Technical Explanation:** Navigation executes `_ = vm.LoadDataAsync()` without observing potential unhandled task exceptions.
- **Reproduction Steps:** Inspect `NavigateTo` method in `MainViewModel.cs`.
- **Root Cause:** Fire-and-forget async pattern without continuation error logger.
- **Actual Impact:** Unobserved task exceptions could terminate process in strict CLR configurations.
- **Recommended Solution:** Append `.ContinueWith(t => LogError(t.Exception), TaskContinuationOptions.OnlyOnFaulted)`.
- **Verification Method:** Trigger fault in `LoadDataAsync`; verify error is caught and logged.

---

### BD-LOW-003: StaticResource Brush Usage in PlaceholderView Breaking Theme
- **Severity:** **LOW**
- **Category:** UI Styling & Theme Consistency
- **File Path:** `src\BlueDoc.UI\Views\PlaceholderView.xaml`
- **Line Numbers:** Lines 22, 31
- **Technical Explanation:** Uses `{StaticResource CardBackgroundBrush}` and `{StaticResource TextPrimaryBrush}` instead of `{DynamicResource}`.
- **Reproduction Steps:** Navigate to an unmapped route, toggle theme; observe colors do not update.
- **Root Cause:** StaticResource evaluates once at load time.
- **Actual Impact:** Visual glitch when toggling theme while on placeholder screen.
- **Recommended Solution:** Replace `{StaticResource}` with `{DynamicResource}`.
- **Verification Method:** Toggle theme while on placeholder; verify colors transition immediately.

---

### BD-LOW-004: Resource Key Collision in FixMyConnectionWizardView
- **Severity:** **LOW**
- **Category:** WPF Resources & Code Hygiene
- **File Path:** `src\BlueDoc.UI\Views\FixMyConnectionWizardView.xaml`
- **Line Numbers:** Line 18
- **Technical Explanation:** Declares local converter `StatusBrushConverter` which duplicates global `StatusToBrushConverter` in `App.xaml`.
- **Reproduction Steps:** Inspect XAML resources in `FixMyConnectionWizardView.xaml`.
- **Root Cause:** Redundant local declaration.
- **Actual Impact:** Unnecessary memory allocation and resource dictionary confusion.
- **Recommended Solution:** Remove local declaration and use global converter.
- **Verification Method:** Remove local resource; verify view compiles and binds cleanly.

---

### BD-LOW-005: Missing .NET 4.8 Prerequisite Release Key in Inno Setup
- **Severity:** **LOW**
- **Category:** Installer & Deployment
- **File Path:** `installer\bluedoc_setup.iss`
- **Line Numbers:** Line 100
- **Technical Explanation:** Installer checks for DWORD `>= 461308` (.NET 4.7.1) rather than `>= 528040` (.NET 4.8).
- **Reproduction Steps:** Inspect `bluedoc_setup.iss`.
- **Root Cause:** Inherited from v4.7.1 project configuration.
- **Actual Impact:** Installer allows installation on machines lacking .NET 4.8 runtime.
- **Recommended Solution:** Update threshold to `528040`.
- **Verification Method:** Test installer on a machine with only .NET 4.7.1; verify prompt to upgrade to .NET 4.8.

---

### BD-LOW-006: Missing Cancellation Token Propagation in DiagnosticEngine
- **Severity:** **LOW**
- **Category:** Diagnostics & Threading
- **File Path:** `src\BlueDoc.Diagnostics\Engine\DiagnosticEngine.cs`
- **Line Numbers:** Lines 95-115
- **Technical Explanation:** While `cancellationToken.IsCancellationRequested` is checked between rules, the token is not passed into synchronous P/Invoke calls inside individual rules.
- **Reproduction Steps:** Cancel a diagnostic run while a slow WMI or SetupAPI call is in flight.
- **Root Cause:** Token not passed to inner task wrappers.
- **Actual Impact:** Diagnostic cancellation delayed by up to 2 seconds while current rule finishes.
- **Recommended Solution:** Pass `cancellationToken` into all internal helper tasks.
- **Verification Method:** Trigger cancellation mid-run; verify instantaneous halt.

