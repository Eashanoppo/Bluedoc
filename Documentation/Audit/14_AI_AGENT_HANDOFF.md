# BlueDoc AI Agent Engineering Handoff Document

**Document ID:** `BD-AUDIT-14`  
**Auditor Roles:** Principal Software Architect, Senior C# / .NET Engineer, Technical Documentation Architect  
**Target Solution:** `BlueDoc.sln`  
**Audit Date:** November 2026  
**Status:** COMPLETE / PRIMARY HANDOFF DIRECTIVE  

---

## 1. Welcome & Primary Directive for the Next AI Coding Agent

You are taking over the engineering codebase for **BlueDoc** ("A Bluetooth Doctor for Windows"), developed by **UNLEFT LLC** (Author: **Golam Morshed Eashan**).

Your objective is to execute the prioritized remediation roadmap (`BD-AUDIT-13`) and bring BlueDoc to commercial-grade, rock-solid release status.

> **CRITICAL DIRECTIVE:** You do NOT need to re-read or re-audit the entire 24,000-line codebase. The 15 forensic audit documents in `Documentation\Audit\` provide the complete, evidence-based architectural, functional, and line-level truth.

---

## 2. Solution Structure & Repository Directory Guide

```
E:\BlueDoc\
├── BlueDoc.sln                         <-- Master Visual Studio Solution (8 projects)
├── src\
│   ├── BlueDoc.Core\                   <-- Enums, DTOs, Base Interfaces, MVVM Base (Zero OS dependencies)
│   ├── BlueDoc.Infrastructure\         <-- P/Invoke, SCM, SetupAPI, WMI, EventLogs, SafeRepairEngine
│   ├── BlueDoc.Bluetooth\              <-- RadioManager, DeviceDiscovery, DeviceManager, BLE Explorer
│   ├── BlueDoc.Diagnostics\            <-- DiagnosticEngine, 12 Diagnostic Rules, Benchmarks
│   ├── BlueDoc.Data\                   <-- SQLite Repositories (ConnectionHistory, RepairHistory)
│   ├── BlueDoc.Application\            <-- TrayGuardManager, TelemetryCoordinator, SaaS Sync
│   └── BlueDoc.UI\                     <-- WPF Shell, 19 Views, 19 ViewModels, Themes, Converters
├── tests\
│   └── BlueDoc.Tests\                  <-- Test Runner, 16 Unit Suites, 18-Scenario Hardware Matrix
├── installer\
│   └── bluedoc_setup.iss               <-- Inno Setup 6+ Packaging Script
├── screenshots\                        <-- UI Visuals (Dark & Light, 720p to 1080p resolutions)
└── Documentation\
    ├── prd.md                          <-- Product Requirements Document (Baseline)
    ├── design.md                       <-- Software Design Document (Architecture)
    └── Audit\                          <-- Complete 15 Forensic Audit Documents (The Truth)
```

---

## 3. High-Level Architecture & Startup Sequence

### 3.1 Startup Flow (`App.xaml.cs` -> `MainWindow.xaml`)
1. **Entry Point:** `App.xaml.cs` -> `OnStartup(StartupEventArgs e)`.
2. **CLI Mode:** If arguments are present, runs `CliRunner.RunAsync(e.Args)` (Headless diagnostic engine mode).
3. **GUI Mode:** Initializes WPF application resources (`DarkTheme.xaml` by default), sets up database initialization via `SqliteDatabaseInitializer.Initialize()`, and instantiates `MainWindow`.
4. **Shell Architecture:** `MainWindow.xaml` holds the left navigation rail and a central `ContentControl` bound to `MainViewModel.CurrentView`.
5. **Navigation:** User clicks navigation rail item -> triggers `MainViewModel.NavigateCommand` with string parameter (e.g. `"Dashboard"`, `"DeviceScanner"`, `"DiagnosticCenter"`) -> swaps `CurrentView` and calls `vm.LoadDataAsync()`.

---

## 4. Bluetooth Backend Mechanics: Win32 vs WinRT

Understanding how BlueDoc interacts with Windows Bluetooth is critical before writing code:

### 4.1 Classic Bluetooth (BR/EDR)
- **APIs:** Win32 P/Invoke (`bthprops.cpl`, `BluetoothAPIs.dll`, `SetupAPI.dll`, `CfgMgr32.dll`).
- **Files:** `src\BlueDoc.Infrastructure\Native\*.cs`, `src\BlueDoc.Bluetooth\Services\SetupApiHardwareInspector.cs`.
- **Functions:** `BluetoothFindFirstRadio`, `BluetoothFindFirstDevice`, `BluetoothAuthenticateDeviceEx`, `BluetoothRemoveDevice`, `CM_Get_DevNode_Status`.
- **Operating Systems:** Fully supported on Windows 7 SP1, Windows 10, and Windows 11.

### 4.2 Bluetooth Low Energy (BLE) & Modern Radios
- **APIs:** Windows Runtime (WinRT) projections via `Windows.winmd` (`Windows.Devices.Bluetooth`, `Windows.Devices.Radios`).
- **Files:** `src\BlueDoc.Bluetooth\Providers\BleAdvertisementWatcherProvider.cs`, `BleExplorerService.cs`.
- **Operating Systems:** Supported on Windows 10 (Build 10240+) and Windows 11. **Strictly absent on Windows 7 SP1**.

---

## 5. What You MUST NOT Change (Preserve These Invariants)

1. **DO NOT change the core UI Visual Theme or Color Palette:**
   - Slate 900 (`#0F172A`) base dark styling, Segoe UI typography, and card-based hierarchy are validated and approved.
2. **DO NOT change the 12-Rule Diagnostic Architecture:**
   - The sequential evaluation pipeline (`IDiagnosticRule` -> `DiagnosticContext` -> `DiagnosticResult`) is mathematically sound and 100% verified by test suites.
3. **DO NOT delete or rewrite SQLite Database Schemas:**
   - The tables `ConnectionHistory`, `RepairHistory`, and `DiscoveredDeviceCache` have verified migrations and unit tests in `SqliteRepositoryTests.cs`.
4. **DO NOT introduce heavy third-party UI component libraries:**
   - Do NOT add Syncfusion, DevExpress, or MahApps.Metro. The pure WPF XAML architecture with custom control templates is clean, fast, and dependency-light.

---

## 6. What You MUST Change (The Defect Hit-List)

Refer to `BD-AUDIT-12` (`12_BUG_REGISTER.md`) for complete line-level details:

1. **Upgrade Target Framework to .NET 4.8 (BD-CRIT-001):**
   - Retarget all 8 `.csproj` files to `<TargetFrameworkVersion>v4.8</TargetFrameworkVersion>`.
2. **Decouple WinRT from Windows 7 SP1 (BD-CRIT-002):**
   - Isolate `BleAdvertisementWatcherProvider` behind `IBleWatcherProvider` and guard instantiation with `OperatingSystemInspector.IsWindows10OrGreater()`.
3. **Replace Mock BLE Explorer with Live WinRT APIs (BD-HIGH-001):**
   - In `BleExplorerService.cs` and `BleExplorerViewModel.cs`, eliminate `GattProfileCatalog` and call `BluetoothLEDevice.FromBluetoothAddressAsync()`, `GetGattServicesAsync()`, and `ReadValueAsync()`.
4. **Fix Simulated BLE Peripheral Connection (BD-HIGH-002):**
   - In `BluetoothConnectionOrchestrator.cs` (lines 244-261), replace simulated success with real `BluetoothLEDevice` session management.
5. **Connect SafeRepairEngine to Genuine PnP Radio Reset (BD-HIGH-003):**
   - Update `SafeRepairEngine.CycleRadioAsync()` to invoke `IHardwareRadioManager` DevNode cycling.
6. **Wrap P/Invoke Handles in `try ... finally` (BD-MED-003):**
   - In `SetupApiHardwareInspector.cs` (`EnrichWithRadioInfo`), guarantee `CloseHandle(hRadio)` and `BluetoothFindRadioClose(hFind)`.
7. **Fix Cross-Thread UI Crash in Connection History (BD-MED-007):**
   - In `ConnectionHistoryViewModel.cs`, wrap collection updates in `App.Current.Dispatcher.InvokeAsync`.
8. **Make WMI Audio Query Asynchronous (BD-MED-004):**
   - In `BluetoothAudioInspector.cs`, wrap synchronous WMI query in `Task.Run`.
9. **Implement In-App UAC Elevation Restart (BD-MED-008):**
   - Provide "Restart as Administrator" with `Verb = "runas"` when executing admin-required repairs.
10. **Fix Windows 11 Troubleshooter Deprecation (BD-MED-005):**
    - Route troubleshooter to `ms-settings:troubleshoot` on Windows 11.
11. **Clean Duplicate Tests in Program.cs (BD-LOW-001):**
    - Remove lines 39-42 in `tests\BlueDoc.Tests\Program.cs`.

---

## 7. Step-by-Step Instructions to Commence Development

Follow this exact sequence when beginning your implementation session:

### Step 1: Environment & Tooling Verification
- Visual Studio 2022 Community MSBuild is located at:
  `C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe`
- Test the baseline compilation:
  ```cmd
  "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" BlueDoc.sln /p:Configuration=Release /v:m
  ```

### Step 2: Execute Phase 0 (Critical Blockers)
- Open all 8 `.csproj` files. Update target framework to `v4.8`.
- Apply WinRT decoupling to `DeviceDiscoveryService.cs`.
- Implement live GATT discovery in `BleExplorerService.cs`.
- Rebuild solution and run tests (`tests\BlueDoc.Tests\bin\Release\BlueDoc.Tests.exe`).

### Step 3: Execute Phase 1 (Core Reliability Fixes)
- Fix native handle leak in `SetupApiHardwareInspector.cs`.
- Fix cross-thread dispatching in `ConnectionHistoryViewModel.cs`.
- Make WMI audio query async in `BluetoothAudioInspector.cs`.
- Wire real PnP DevNode cycling into `SafeRepairEngine.cs`.

### Step 4: Verification & Testing
- Run test harness: `tests\BlueDoc.Tests\bin\Release\BlueDoc.Tests.exe`.
- Verify all 18 functional scenarios and benchmarks pass in ~20 seconds.
- Run UI verification script via PowerShell STA:
  ```powershell
  powershell.exe -ExecutionPolicy Bypass -File test_all_views.ps1
  powershell.exe -ExecutionPolicy Bypass -File test_animations_and_loading.ps1
  ```

### Step 5: Packaging & Release
- Open `installer\bluedoc_setup.iss`. Update .NET 4.8 release check to `528040`.
- Compile installer with Inno Setup Compiler.
- Confirm installer `dist\BlueDoc_v1.0.0_Setup.exe` installs cleanly.

You have the complete forensic blueprint. Proceed with confidence.

