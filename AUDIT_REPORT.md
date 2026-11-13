# BlueDoc Source-Code Audit & Implementation Truth Report

**Product:** BlueDoc — Windows Bluetooth Doctor  
**Company:** UNLEFT LLC  
**Developer:** Golam Morshed Eashan  
**Auditor:** Principal Software Architect, Senior C# Engineer & Bluetooth Systems Engineer  
**Audit Date:** November 2026  
**Target Operating Systems:** Windows 7 SP1, Windows 10, Windows 11  
**Solution File:** `E:\BlueDoc\BlueDoc.sln`  

---

## 1. Executive Summary & Architectural Ground Truth

BlueDoc is conceived as an intelligent, evidence-based Windows Bluetooth diagnostic, repair, pairing, and connection management utility. The product vision articulated in the Product Requirements Document (PRD) and Software Design Document (SDD) sets an ambitious standard:

> *"A user who cannot connect a Bluetooth headphone, speaker, mouse, keyboard, phone or other supported peripheral should be able to use BlueDoc to identify the problem, resolve it where possible, pair the device and manage the connection without having to navigate Windows Bluetooth Settings."*

### 1.1 The Central Audit Finding: The Illusion of Completion
At a superficial glance, BlueDoc appears to be a fully realized, enterprise-grade desktop utility. The Visual Studio solution builds cleanly with zero compiler warnings in both `Debug` and `Release` modes. A custom test runner (`BlueDoc.Tests.exe`) executes 13 comprehensive test suites and outputs `ALL VERIFICATION TESTS COMPLETED SUCCESSFULLY (100% PASS)`. An automated release packaging pipeline (`build_release.ps1`) stages a portable distribution and builds an Inno Setup installer. The user interface exposes **17 richly populated views** organized into five functional sidebar categories, complete with dark/light theme switching, animated progress bars, SVG icons, and PDF export capabilities.

**However, a forensic examination of the source code reveals a stark architectural truth:**

1. **The Core User Outcome Is Not Implemented In-App:**  
   The application cannot pair a Bluetooth peripheral programmatically. When a user clicks "+ Pair New Device" in `BluetoothDevicesView` or "Pair with Selected Device" in `DeviceScannerView`, BlueDoc merely executes `Process.Start("ms-settings:bluetooth")` or falls back to `control.exe` / `DevicePairingWizard.exe`. The user is forced into the very Windows Settings interface that BlueDoc was built to replace (`src\BlueDoc.Application\Services\DeviceManagementService.cs`, lines 34–77).
2. **Device Connection & Disconnection Are Completely Missing:**  
   There is no method or API call anywhere in the solution to connect or disconnect a paired peripheral (whether Classic audio A2DP/HFP, HID keyboard/mouse, RFCOMM serial, or BLE GATT). The interface displays a connection status badge derived from PnP DevNode presence, but provides zero controls to trigger a connection.
3. **Simulated, Hardcoded, and Mock Subsystems Masquerading as Working Features:**
   - **Bluetooth Low Energy (BLE) GATT Explorer:** `BleExplorerView` does not communicate with any Bluetooth hardware or GATT server. Characteristics and services are loaded from a hardcoded static profile catalog (`GattProfileCatalog.cs`). Clicking "Read" on a battery characteristic returns a hardcoded `"88% (Read OK)"`. Clicking "Safe Write" displays a confirmation box and simulates success with a hardcoded `"ACK Received"`. Toggling notifications merely flips a local boolean. Scanning for BLE devices actually executes a Classic Win32 radio inquiry (`src\BlueDoc.UI\ViewModels\BleExplorerViewModel.cs`, lines 159–255).
   - **Enterprise SaaS Fleet Portal:** `SaasFleetDashboardView` and `MockSaasCloudApiGateway.cs` spin up a local in-process `HttpListener` on port 48821 populated with 5 hardcoded dummy computers (`CORP-DEV-LAPTOP`, `SALES-THINKPAD-02`, etc.) and hardcoded API tokens (`bd_sec_live_9941a87b`). This violates the PRD mandate for a local-first, zero-mandatory-telemetry utility.
   - **Radio Power Cycling:** `SafeRepairEngine.ExecuteRepairAsync("CycleBluetoothRadio")` does not cycle the physical transceiver power via HCI or bus IOCTL; it simply restarts the Windows service `bthserv` (`src\BlueDoc.Infrastructure\Repairs\SafeRepairEngine.cs`, lines 320–328).
   - **Driver Rollback Assistant:** The "Guided Driver Rollback" repair action merely launches `devmgmt.msc` and displays a generic messagebox instruction list (`src\BlueDoc.Infrastructure\Repairs\SafeRepairEngine.cs`, lines 389–392).
   - **Signal & RSSI Monitoring:** Win32 `BluetoothFindFirstDevice` does not report RSSI. As a result, `BluetoothDeviceInfo.Rssi` is always `null`, and the RSSI telemetry in `SignalMonitorView` always displays `N/A` with 0 dBm readings.
   - **Device Connection History:** While SQLite tables and repositories exist, no scanning, pairing, or connection events are ever logged to the database during live runtime workflows.
4. **Framework Target Divergence:**  
   Every `.csproj` file in the solution specifies `<TargetFrameworkVersion>v4.7.1</TargetFrameworkVersion>`, whereas both the PRD and SDD specify `.NET Framework 4.8`.
5. **UI Theming Inconsistency:**  
   16 out of 17 views hardcode theme brushes using `{StaticResource ...}` instead of `{DynamicResource ...}`. Consequently, switching to "Light Theme" updates the top navigation bar and sidebar, but leaves the view canvases in dark mode with unreadable, mismatched contrast.
6. **Zero Accessibility Support:**  
   Across all 23 XAML files in the application, there is not a single `AutomationProperties.Name`, `AutomationProperties.HelpText`, or accessibility tag.

---

## 2. Solution and Project Structure Audit

The solution consists of 8 projects targeting .NET Framework 4.7.1:

```text
BlueDoc.sln
├── src\BlueDoc.Core\BlueDoc.Core.csproj                 (Data models, enums, interfaces)
├── src\BlueDoc.Infrastructure\BlueDoc.Infrastructure.csproj (SetupAPI, CfgMgr32, Win32 P/Invoke, WMI, EventLog, PDF)
├── src\BlueDoc.Bluetooth\BlueDoc.Bluetooth.csproj           (Win32 Radio provider, GATT profile catalog, Manager)
├── src\BlueDoc.Diagnostics\BlueDoc.Diagnostics.csproj       (Diagnostic engine, 12 rules, Benchmark engine)
├── src\BlueDoc.Data\BlueDoc.Data.csproj                     (SQLite manager, 5 repositories)
├── src\BlueDoc.Application\BlueDoc.Application.csproj       (Domain services, KB, SaaS cloud models)
├── src\BlueDoc.UI\BlueDoc.UI.csproj                         (WPF presentation layer, 17 views, ViewModels, CLI)
└── tests\BlueDoc.Tests\BlueDoc.Tests.csproj                 (Console test harness, unit & integration tests)
```

### 2.1 Project-by-Project Assessment

| Project | Target FW | Primary Dependencies | Health Assessment | Key Deficiencies |
|---|---|---|---|---|
| **BlueDoc.Core** | v4.7.1 | None | Clean | Missing pairing, connection, and GATT operation signatures in `IBluetoothProvider`. |
| **BlueDoc.Infrastructure** | v4.7.1 | BlueDoc.Core, System.Management | Excellent native P/Invoke | Solid SetupAPI and CfgMgr32 bindings. Native PDF generation is cleanly implemented. Missing Win32 Bluetooth Authentication API bindings (`BluetoothAuthenticateDeviceEx`). |
| **BlueDoc.Bluetooth** | v4.7.1 | BlueDoc.Core, BlueDoc.Infrastructure | Incomplete abstraction | Contains only `Win32BluetoothRadioProvider`. No modern WinRT (`Windows.Devices.Bluetooth`) implementation exists. `GattProfileCatalog` is synthetic. |
| **BlueDoc.Diagnostics** | v4.7.1 | Core, Infrastructure, Bluetooth | Functional rule engine | 12 rules function as heuristics. Rules 8–11 evaluate environment flags rather than executing live discovery or GATT probes. |
| **BlueDoc.Data** | v4.7.1 | Core, System.Data.SQLite, Newtonsoft.Json | Well-structured SQLite | Tables and repositories for sessions, history, cache, and settings are functional. However, history repository is never called by application workflows. |
| **BlueDoc.Application** | v4.7.1 | Core, Data, Infrastructure, Bluetooth, Diagnostics | Mixed domain logic | Contains domain services. Houses `MockSaasCloudApiGateway.cs` which introduces an unnecessary HTTP server and simulated cloud endpoints into a desktop diagnostic tool. |
| **BlueDoc.UI** | v4.7.1 | All solution projects | Feature-rich WPF MVVM | Excellent navigation and view composition. Suffers from widespread `{StaticResource}` brush bindings, missing loading indicators, and unhandled fire-and-forget async tasks. |
| **BlueDoc.Tests** | v4.7.1 | All solution projects | Custom console runner | 100% pass rate is misleading: asserts against hardcoded mock data and environment flags rather than validating live hardware operations. |

---

## 3. Deep-Dive Audit of Subsystems

### 3.1 Bluetooth Provider & Hardware Abstraction Subsystem
- **Interface:** `src\BlueDoc.Core\Interfaces\IBluetoothProvider.cs`
- **Active Implementation:** `src\BlueDoc.Bluetooth\Legacy\Win32BluetoothRadioProvider.cs`
- **Manager:** `src\BlueDoc.Bluetooth\Abstractions\BluetoothManager.cs`

#### Findings:
1. **Incomplete Provider Contract:** `IBluetoothProvider` exposes only 5 methods: `GetAdaptersAsync`, `DiscoverDevicesAsync`, `GetPairedDevicesAsync`, `IsRadioEnabledAsync`, and `RemoveDeviceAsync`. It completely omits `PairDeviceAsync`, `ConnectDeviceAsync`, `DisconnectDeviceAsync`, `ReadRssiAsync`, and all GATT operations.
2. **Single Implementation Stalling:** The SDD proposed separate providers (`LegacyBluetoothProvider`, `ModernBluetoothProvider`, `BleProvider`). In reality, only `Win32BluetoothRadioProvider` exists. It is hardcoded in `BluetoothManager.cs` (line 29):
   ```csharp
   _activeProvider = new Win32BluetoothRadioProvider(_hardwareInspector);
   ```
3. **Synchronous Blocking Win32 Inquiry:** In `Win32BluetoothRadioProvider.DiscoverDevicesAsync` (lines 254–270), `BluetoothFindFirstDevice` is invoked with `fIssueInquiry = true` and `cTimeoutMultiplier = timeoutSeconds / 1.28`. This Win32 call blocks the thread synchronously for the entire duration. While executed inside `Task.Run`, the cancellation token is only checked *between* device iterations; if no device responds, the call cannot be cancelled until the native inquiry timeout expires.
4. **Classic-Only Radio Inquiry:** Win32 `BluetoothFindFirstDevice` issues a BR/EDR Classic Bluetooth inquiry. It is architecturally incapable of discovering BLE advertising peripherals.

### 3.2 Hardware Inspection & PnP Enumeration
- **Classes:** `SetupApiHardwareInspector.cs`, `PnpBluetoothDeviceInspector.cs`, `HardwareIdParser.cs`, `DeviceClassDecoder.cs` in `src\BlueDoc.Infrastructure\Hardware\`

#### Findings:
1. **Robust PnP Enumeration:** `SetupApiHardwareInspector.cs` uses `SetupDiGetClassDevs` with `GUID_DEVCLASS_BLUETOOTH` and Configuration Manager `CM_Get_DevNode_Status` to accurately identify physical controllers (USB/PCI) and extract PnP problem codes (e.g. Code 22 disabled, Code 10 failed to start, Code 43 stopped).
2. **Accurate Hardware ID Parsing:** `HardwareIdParser.cs` correctly decomposes `USB\VID_xxxx&PID_xxxx` and `PCI\VEN_xxxx&DEV_xxxx` strings and maps vendor IDs to Intel, Realtek, Broadcom, MediaTek, and Qualcomm.
3. **Class of Device (CoD) Decoding:** `DeviceClassDecoder.cs` correctly decodes the 24-bit Bluetooth Class of Device integer into Major Service, Major Device, and Minor Device classes, providing accurate icons and category names for mice, keyboards, headsets, and phones.
4. **Connection State Ambiguity:** In `PnpBluetoothDeviceInspector.EnumeratePairedDevices()` (lines 74–81), device connection state is evaluated via:
   ```csharp
   if ((statusFlags & CfgMgr32Native.DN_STARTED) != 0 && (statusFlags & CfgMgr32Native.DN_HAS_PROBLEM) == 0)
   {
       isConnected = true;
   }
   ```
   For PnP devices, `DN_STARTED` indicates that the devnode driver is loaded in Windows, which frequently remains true for paired devices even after the physical Bluetooth link has dropped or the peripheral is turned off. True connection state requires verifying active profile handles or ACL links.

### 3.3 Diagnostic Engine & Rules
- **Engine:** `src\BlueDoc.Diagnostics\Engine\DiagnosticEngine.cs`
- **Rules:** 12 classes in `src\BlueDoc.Diagnostics\Rules\`

#### Findings:
1. **Rule Engine Execution:** The engine executes sequentially and generates structured `DiagnosticResult` objects with Summary, TechnicalDetails, Evidence, PossibleCause, and RecommendedAction.
2. **Rules Evaluating Live Hardware:**
   - `AdapterDetectionRule`: Accurately checks for present Bluetooth adapter.
   - `HardwareStatusRule`: Accurately checks Configuration Manager problem codes.
   - `DriverHealthRule`: Validates driver provider, version string, and installation state.
   - `ServiceStatusRule`: Validates Windows service `bthserv` state.
   - `AudioServiceRule`: Validates `BTAGService` and `BthAvctpSvc`.
   - `RadioAvailabilityRule`: Queries radio handles via `BluetoothFindFirstRadio`.
   - `EventLogHealthRule`: Queries the Windows System event log for recent BTHUSB errors.
3. **Rules Evaluating Heuristics Rather than Hardware:**
   - `DiscoveryReadinessRule`: Merely checks if the adapter is operational and the radio is on. It does not test active discovery.
   - `PairingReadinessRule`: Merely checks if `bthserv` is running. It does not perform an authentication readiness test.
   - `BleAvailabilityRule` & `BleGattSupportRule`: Evaluate `OsVersionInfo.SupportsBleModernApis`. On Windows 11, `BleGattSupportRule` reports `Passed` with `"GATT Client and Attribute Protocol (ATT) APIs are fully supported"`, even though BlueDoc contains zero code to invoke them.

### 3.4 Device Pairing, Connection, and Management
- **Services:** `DeviceManagementService.cs`, `DeviceDiscoveryService.cs` in `src\BlueDoc.Application\Services\`
- **ViewModels:** `BluetoothDevicesViewModel.cs`, `DeviceScannerViewModel.cs`

#### Findings:
1. **No In-App Pairing:** In `BluetoothDevicesViewModel.cs` (lines 84–87) and `DeviceScannerViewModel.cs` (lines 128–131), pairing commands invoke `DeviceManagementService.LaunchWindowsPairingWizard()`. This opens `ms-settings:bluetooth` (Windows 10/11) or `control.exe /name Microsoft.DevicesAndPrinters` (Windows 7). The user must complete pairing entirely inside Windows Settings.
2. **No In-App Connection / Disconnection:** No methods exist to initiate an A2DP audio connection, connect an HID peripheral, establish an RFCOMM channel, or disconnect a device.
3. **Device Removal:** `UnpairDeviceAsync` invokes Win32 `BluetoothRemoveDevice(ref rawAddress)`. This succeeds for classic authenticated devices if the MAC address parses to a `ulong`. If it fails (e.g. for pure BLE devices or devices locked by a driver), it falls back to launching Windows Settings.

### 3.5 Safe Repair Center
- **Engine:** `src\BlueDoc.Infrastructure\Repairs\SafeRepairEngine.cs`
- **Service:** `src\BlueDoc.Application\Services\RepairService.cs`

#### Findings:
1. **Real Service Operations:** `RestartBthServ` and `RestartAudioGateway` utilize `System.ServiceProcess.ServiceController` to cleanly stop and restart Windows services. Permission checks verify administrator role before execution.
2. **Simulated Radio Power Cycle:** `CycleBluetoothRadio` (lines 320–328) merely calls `RestartWindowsServiceAsync("bthserv")`. It does not cycle the radio transceiver hardware or reset the controller.
3. **Simulated Driver Rollback:** `GuideDriverRollback` (lines 389–392) simply opens `devmgmt.msc` and displays a messagebox with manual steps.
4. **Audit Trail:** Successfully writes repair history records to the SQLite database.

### 3.6 BLE Explorer & GATT Subsystem
- **ViewModel:** `src\BlueDoc.UI\ViewModels\BleExplorerViewModel.cs`
- **Catalog:** `src\BlueDoc.Bluetooth\BLE\GattProfileCatalog.cs`

#### Findings:
1. **Synthetic GATT Profiles:** In `BleExplorerViewModel.LoadGattProfileForSelectedDevice()`, selecting a device does not discover services over BLE. It calls `GattProfileCatalog.GetStandardPeripheralProfile()`, which generates hardcoded in-memory objects for Generic Access (0x1800), Generic Attribute (0x1801), Device Information (0x180A), Battery (0x180F), and HID (0x1812).
2. **Hardcoded Read Characteristic:** `ReadCharacteristic` (lines 199–218) inspects UUID `2A19` and returns hardcoded `"88% (Read OK at {timestamp})"`. For UUID `2A00`, it returns the device name.
3. **Simulated Write Characteristic:** `PerformSafeWrite` (lines 239–254) sets `characteristic.ValueDisplay = "[Written OK at {timestamp}] ACK Received"` without dispatching any BLE packet.
4. **Simulated Notifications:** `ToggleNotification` toggles a local boolean and displays `"[Subscribed at ...] Listening for CCCD notifications..."`.
5. **BLE Scan Fallback:** `ScanBleAsync` invokes `DeviceDiscoveryService.StartScanningAsync(15)`, which executes Classic BR/EDR inquiry, unable to see advertising BLE peripherals.

### 3.7 SaaS Fleet Telemetry & Cloud Integration
- **Files:** `MockSaasCloudApiGateway.cs`, `SaasTelemetryService.cs`, `SaasTelemetryPollerService.cs` in `src\BlueDoc.Application\Cloud\`, `SaasFleetDashboardViewModel.cs`

#### Findings:
1. **Embedded Mock HTTP Server:** `MockSaasCloudApiGateway.cs` contains a complete `System.Net.HttpListener` web server listening on `http://localhost:48821/`. It serves synthetic JSON endpoints (`/v1/health`, `/v1/fleet/metrics`, `/v1/fleet/machines`, `/v1/telemetry`) and is pre-seeded with 5 fake corporate machines.
2. **Contradiction of Product Core Principles:** The PRD states in NFR-05 (Privacy): *"Local-first operation. No mandatory user account. No mandatory cloud connectivity. No default telemetry."* The inclusion of an in-process mock cloud server and a dedicated SaaS Fleet view with pre-configured tokens (`bd_sec_live_9941a87b`) is extraneous to a local Windows Bluetooth utility.

### 3.8 Signal Monitoring & Discovery Subsystem
- **ViewModel:** `src\BlueDoc.UI\ViewModels\SignalMonitorViewModel.cs`

#### Findings:
1. **Continuous Inquiry Loop:** The monitor runs `StartScanningAsync(15)` in a while loop.
2. **Unavailable RSSI:** The Win32 `bthprops.cpl` discovery structure (`BLUETOOTH_DEVICE_INFO`) does not contain an RSSI field. Consequently, `BluetoothDeviceInfo.Rssi` is always null. The view consistently reports 0 devices with RSSI and an average RSSI of `"N/A"`.

### 3.9 Connection History & SQLite Persistence
- **Database Manager:** `src\BlueDoc.Data\SqliteDatabaseManager.cs`
- **Service:** `src\BlueDoc.Application\Services\HistoryService.cs`
- **ViewModel:** `src\BlueDoc.UI\ViewModels\ConnectionHistoryViewModel.cs`

#### Findings:
1. **Schema & Repositories Work:** SQLite tables (`DiagnosticSessions`, `DiagnosticResults`, `DiscoveredDevices`, `ConnectionHistory`, `RepairHistory`, `ApplicationSettings`) are properly defined and indexed.
2. **Zero Ingest During Live Runtime:** Neither `DeviceScannerViewModel` nor `BluetoothDevicesViewModel` calls `_historyService.RecordDeviceDiscoveredAsync()` or `RecordConnectionAttemptAsync()`. As a result, the `ConnectionHistory` table remains permanently empty in the running application unless written to by unit tests.

---

## 4. Asynchronous Architecture & UI-Thread Audit

1. **Fire-and-Forget Navigation Calls:**  
   In `MainViewModel.NavigateTo(string sectionName)` (lines 264–332), view transitions trigger asynchronous data loads using fire-and-forget discard syntax:
   ```csharp
   case "Bluetooth Devices":
       CurrentViewModel = BluetoothDevicesVM;
       _ = BluetoothDevicesVM.LoadPairedDevicesAsync();
       break;
   ```
   If an unhandled exception occurs inside these methods outside their inner try/catch blocks, the exception is unobserved. Furthermore, repeated rapid clicks on sidebar navigation spawn overlapping background tasks that clear and repopulate the same `ObservableCollection`.
2. **Dispatcher Invocations:**  
   In `DeviceScannerViewModel.OnDeviceDiscovered` (lines 158–177) and `SignalMonitorViewModel.OnDeviceCaptured` (lines 174–196), callbacks from background discovery invoke `System.Windows.Application.Current?.Dispatcher?.Invoke(...)`. If the application is shutting down, `Application.Current` can be null, causing silent drops or potential exit deadlocks.
3. **Blocking Hardware Calls Inside Task.Run:**  
   In `Win32BluetoothRadioProvider.cs`, SetupAPI and bthprops calls run inside `Task.Run`. While this prevents freezing the WPF UI thread, long-running Win32 inquiries cannot be aborted immediately when the user clicks "Stop Scanning".

---

## 5. Verification Test Suite & Release Automation Audit

### 5.1 Test Harness (`BlueDoc.Tests.exe`)
The test runner executes 13 suites:
- `HardwareIdParserTests`: Validates VID/PID parsing string logic.
- `DiagnosticEngineTests`: Validates rule evaluation against mock `DiagnosticContext`.
- `DeviceClassDecoderTests`: Validates bitmask arithmetic.
- `SqliteRepositoryTests`: Validates CRUD against temporary SQLite file.
- `EventAnalyzerTests`: Validates string interpretation of event IDs.
- `DriverAssistantTests`: Validates vendor link generation.
- `ReportGeneratorTests`: Validates plain text, HTML, and JSON string generation.
- `Phase5AdvancedDiagnosticsTests`: Asserts that `GattProfileCatalog.GetStandardPeripheralProfile()` returns the expected 4 hardcoded services.
- `Phase6RepairAndReportingTests`: Asserts that `PdfReportGenerator` produces valid PDF header bytes.
- `Phase7PlatformAndFailureScenarioTests`: Validates error handling logic.
- `Phase8TechnicianAndSaaSIntegrationTests`: Validates benchmark scoring math.
- `Phase9AudioTraceAndTrayGuardTests`: Validates audio model bindings.
- `Phase10SaasFleetAndApiGatewayTests`: Starts the local `MockSaasCloudApiGateway` on port 48821 and asserts that an HTTP GET returns status 200.
- `RealHardwareInspectionTests` & `RealDeviceDiscoveryTests`: Runs against the live host adapter and prints discovered devices to the console.

**Audit Verdict:** The test suite verifies data structures, string parsing, and mock APIs thoroughly, but does not test real Bluetooth pairing, real BLE discovery, or real peripheral GATT communication.

### 5.2 Release Script (`build_release.ps1`)
The script executes:
1. Solution rebuild via MSBuild.
2. Execution of `BlueDoc.Tests.exe`.
3. Execution of `test_all_views.ps1` in an STA PowerShell process (which successfully instantiates all 17 views and verifies navigation).
4. Staging of binaries, licenses, and documentation into `dist\BlueDoc_v1.0.0_Portable`.
5. Creation of `BlueDoc_v1.0.0_Portable.zip`.
6. Compilation of Inno Setup installer `dist\BlueDoc_v1.0.0_Setup.exe`.

The build automation pipeline is functional and reliable, but it packages an application containing significant functional simulations.

---

## 6. Summary of Action Items Required for Genuine Product Truth

1. Implement programmatic pairing (BR/EDR SSP and BLE security pairing) without launching Windows Settings.
2. Implement device connection and disconnection management across supported audio, HID, and serial profiles.
3. Implement genuine BLE advertisement scanning and GATT client communication using Windows Runtime APIs on Windows 10/11, with honest "Not Supported" reporting on Windows 7.
4. Replace simulated repair operations with real hardware cycling and interactive rollback guidance.
5. Remove or isolate the mock SaaS cloud gateway so that BlueDoc remains a strictly local-first tool.
6. Correct all WPF `{StaticResource}` theme brush bindings to `{DynamicResource}` to enable proper dark/light theme switching.
7. Implement loading state visuals, empty state panels, and accessibility tags across all views.
8. Wire runtime scanning and pairing events into the SQLite `ConnectionHistory` repository.
9. Reconcile the target framework specification (.NET 4.7.1 vs .NET 4.8).

