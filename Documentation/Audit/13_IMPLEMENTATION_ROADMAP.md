# BlueDoc Implementation & Engineering Remediation Roadmap

**Document ID:** `BD-AUDIT-13`  
**Auditor Roles:** Principal Software Architect, Senior C# / .NET Engineer, Senior WPF Engineer  
**Target Solution:** `BlueDoc.sln`  
**Audit Date:** November 2026  
**Status:** COMPLETE / EVIDENCE-BASED  

---

## 1. Executive Summary & Phased Implementation Strategy

This roadmap provides a battle-tested, prioritized engineering execution plan for the next software engineer or AI coding agent. The plan is organized into five discrete phases (Phase 0 through Phase 4), ordered strictly by risk, dependency hierarchy, and system stability impact:

```mermaid
flowchart LR
    P0[Phase 0: Critical Blockers & Hardware Backend] --> P1[Phase 1: Core System & Reliability Fixes]
    P1 --> P2[Phase 2: Architecture & UAC Hardening]
    P2 --> P3[Phase 3: Performance & Optimization]
    P3 --> P4[Phase 4: UI/UX Polish & Installer Finalization]
```

---

## 2. Phase 0: Critical Blockers & Core Bluetooth Backend (P0)

> **Objective:** Eliminate runtime crashes on Windows 7, align the solution to .NET Framework 4.8, and replace simulated BLE mock implementations with genuine Windows APIs.

### Work Item 0.1: Retarget Solution to .NET Framework 4.8 (BD-CRIT-001)
- **Files Modified:**
  - `src\BlueDoc.Core\BlueDoc.Core.csproj`
  - `src\BlueDoc.Infrastructure\BlueDoc.Infrastructure.csproj`
  - `src\BlueDoc.Bluetooth\BlueDoc.Bluetooth.csproj`
  - `src\BlueDoc.Diagnostics\BlueDoc.Diagnostics.csproj`
  - `src\BlueDoc.Data\BlueDoc.Data.csproj`
  - `src\BlueDoc.Application\BlueDoc.Application.csproj`
  - `src\BlueDoc.UI\BlueDoc.UI.csproj`
  - `tests\BlueDoc.Tests\BlueDoc.Tests.csproj`
- **Actions:**
  1. Change `<TargetFrameworkVersion>v4.7.1</TargetFrameworkVersion>` to `<TargetFrameworkVersion>v4.8</TargetFrameworkVersion>` across all 8 projects.
  2. Verify package references in `packages.config` and NuGet restore compatibility.
  3. Compile with MSBuild; confirm 0 errors.

### Work Item 0.2: Decouple WinRT Dependencies for Windows 7 SP1 (BD-CRIT-002)
- **Files Modified:**
  - `src\BlueDoc.Bluetooth\Services\DeviceDiscoveryService.cs`
  - `src\BlueDoc.Bluetooth\Providers\BleAdvertisementWatcherProvider.cs`
- **Actions:**
  1. Create interface `IBleAdvertisementWatcherProvider` in `BlueDoc.Core.Interfaces`.
  2. Guard instantiation of WinRT `BluetoothLEAdvertisementWatcher` behind an OS check:
     ```csharp
     if (OperatingSystemInspector.IsWindows10OrGreater())
     {
         _bleWatcher = new Win10BleAdvertisementWatcherProvider();
     }
     else
     {
         _bleWatcher = new NullBleAdvertisementWatcherProvider(); // Graceful fallback
     }
     ```
  3. Ensure no direct references to WinRT types exist as static or instance fields in classes compiled for Windows 7.

### Work Item 0.3: Implement Live BLE GATT Explorer Backend (BD-HIGH-001 & FR-16)
- **Files Modified:**
  - `src\BlueDoc.Bluetooth\Services\BleExplorerService.cs`
  - `src\BlueDoc.UI\ViewModels\BleExplorerViewModel.cs`
- **Actions:**
  1. Remove calls to mock generator `GattProfileCatalog.GetStandardPeripheralProfile`.
  2. Implement live GATT discovery using `BluetoothLEDevice.FromBluetoothAddressAsync(ulong address)`.
  3. Call `device.GetGattServicesAsync()` -> `service.GetCharacteristicsAsync()`.
  4. Implement live `ReadCharacteristicAsync` via `characteristic.ReadValueAsync(BluetoothCacheMode.Uncached)`.
  5. Implement live `WriteCharacteristicAsync` via `characteristic.WriteValueWithResultAsync(buffer)`.
  6. Implement live CCCD notification toggle via `characteristic.WriteClientCharacteristicConfigurationDescriptorWithResultAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify)`.

### Work Item 0.4: Implement Real BLE Connection in Orchestrator (BD-HIGH-002)
- **Files Modified:**
  - `src\BlueDoc.Bluetooth\Services\BluetoothConnectionOrchestrator.cs`
- **Actions:**
  1. Replace lines 244-261 in `ConnectBlePeripheralAsync`.
  2. Open connection using `BluetoothLEDevice.FromBluetoothAddressAsync(address)` and hold active `GattSession` handle.
  3. Return genuine `DeviceOperationResult` based on native connection status.

---

## 3. Phase 1: Core System & Reliability Fixes (P1)

> **Objective:** Fix native handle leaks, correct radio hardware power cycling, eliminate cross-thread UI crashes, and prevent UI freezes.

### Work Item 1.1: Connect SafeRepairEngine to Genuine PnP Radio Cycling (BD-HIGH-004)
- **Files Modified:**
  - `src\BlueDoc.Infrastructure\Repairs\SafeRepairEngine.cs`
- **Actions:**
  1. Inject `IHardwareRadioManager` into `SafeRepairEngine`.
  2. Update `CycleRadioAsync()` to execute real DevNode disable and re-enable using SetupAPI (`CM_Disable_DevNode` / `CM_Enable_DevNode` or `DIF_PROPERTYCHANGE`).

### Work Item 1.2: Enforce Native Handle Cleanup in Hardware Inspector (BD-MED-003)
- **Files Modified:**
  - `src\BlueDoc.Bluetooth\Services\SetupApiHardwareInspector.cs`
- **Actions:**
  1. Wrap `BluetoothFindFirstRadio` and `BluetoothGetRadioInfo` in `try ... finally` blocks.
  2. Guarantee `CloseHandle(hRadio)` and `BluetoothFindRadioClose(hFind)` execution even if an exception occurs.

### Work Item 1.3: Thread-Safe Dispatcher Mutation in Connection History (BD-MED-007)
- **Files Modified:**
  - `src\BlueDoc.UI\ViewModels\ConnectionHistoryViewModel.cs`
- **Actions:**
  1. Enclose `RecentConnections.Clear()` and `RecentConnections.Add()` in `App.Current.Dispatcher.InvokeAsync(() => { ... })`.

### Work Item 1.4: Asynchronous WMI Query in Audio Inspector (BD-MED-004)
- **Files Modified:**
  - `src\BlueDoc.Infrastructure\Audio\BluetoothAudioInspector.cs`
- **Actions:**
  1. Wrap `ManagementObjectSearcher` query in `await Task.Run(() => ...).ConfigureAwait(false)`.
  2. Eliminate the 500-2000ms UI freeze when opening Audio Diagnostics.

---

## 4. Phase 2: Architecture & UAC Security Hardening (P2)

> **Objective:** Decouple ViewModel creation via Dependency Injection, add in-app UAC elevation workflow, and handle Windows 11 troubleshooter deprecation.

### Work Item 2.1: Implement Dependency Injection Container in UI Shell (BD-MED-001)
- **Files Modified:**
  - `src\BlueDoc.UI\App.xaml.cs`
  - `src\BlueDoc.UI\ViewModels\MainViewModel.cs`
- **Actions:**
  1. Add `Microsoft.Extensions.DependencyInjection` NuGet package.
  2. Configure services and ViewModels in `App.xaml.cs` (`ConfigureServices(IServiceCollection services)`).
  3. Refactor `MainViewModel` to resolve ViewModels via `IServiceProvider` or factory delegates rather than concrete `new` keywords.

### Work Item 2.2: Implement In-App UAC Elevation Restart Workflow (BD-MED-008)
- **Files Modified:**
  - `src\BlueDoc.Infrastructure\Repairs\SafeRepairEngine.cs`
  - `src\BlueDoc.UI\Views\SafeRepairCenterView.xaml`
  - `src\BlueDoc.UI\ViewModels\SafeRepairCenterViewModel.cs`
- **Actions:**
  1. Add helper method `RestartAsAdministrator()` in `App.xaml.cs`:
     ```csharp
     var psi = new ProcessStartInfo(Assembly.GetEntryAssembly().Location)
     {
         UseShellExecute = true,
         Verb = "runas"
     };
     Process.Start(psi);
     Application.Current.Shutdown();
     ```
  2. When an admin repair is attempted non-elevated, display a dialog: "Administrator Privileges Required. Would you like to restart BlueDoc as Administrator?" with standard UAC shield icon.

### Work Item 2.3: Windows 11 Troubleshooter Routing (BD-MED-005)
- **Files Modified:**
  - `src\BlueDoc.Infrastructure\Repairs\SafeRepairEngine.cs`
- **Actions:**
  1. In `LaunchNativeTroubleshooter()`, check if `Environment.OSVersion.Version.Major == 10 && build >= 22000` (Windows 11).
  2. If Windows 11, execute `ms-settings:troubleshoot`.
  3. If Windows 10/7, execute `msdt.exe /id BluetoothDiagnostic`.

---

## 5. Phase 3: Performance & Power Optimization (P3)

> **Objective:** Replace polling with native Windows event notifications, clean test runner duplication, and optimize background CPU usage.

### Work Item 3.1: Replace TrayGuard Timer with WM_DEVICECHANGE (BD-MED-002)
- **Files Modified:**
  - `src\BlueDoc.Application\Services\TrayGuardManager.cs`
  - `src\BlueDoc.UI\MainWindow.xaml.cs`
- **Actions:**
  1. Hook `WM_DEVICECHANGE` (0x0219) via `HwndSource.FromHwnd()`.
  2. Listen for `DBT_DEVICEARRIVAL` (0x8000) and `DBT_DEVICEREMOVECOMPLETE` (0x8004).
  3. Remove the 10-second polling timer, reducing background CPU usage to 0%.

### Work Item 3.2: Clean Duplicate Tests in Program.cs (BD-LOW-001)
- **Files Modified:**
  - `tests\BlueDoc.Tests\Program.cs`
- **Actions:**
  1. Delete lines 39-42 (duplicate benchmark and matrix calls).
  2. Verify test execution time reduces from 42s to 20s.

### Work Item 3.3: Propagate Cancellation Tokens in DiagnosticEngine (BD-LOW-006)
- **Files Modified:**
  - `src\BlueDoc.Diagnostics\Engine\DiagnosticEngine.cs`
- **Actions:**
  1. Pass `cancellationToken` into all internal helper tasks and SetupAPI loops to ensure instantaneous test cancellation.

---

## 6. Phase 4: UI/UX Polish, Accessibility & Installer Finalization (P4)

> **Objective:** Fix Light Theme contrast failures, ensure dynamic theme resource resolution, clean redundant XAML converters, and update Inno Setup prerequisites.

### Work Item 4.1: Fix Light Theme Warning Contrast (BD-MED-006)
- **Files Modified:**
  - `src\BlueDoc.UI\Themes\LightTheme.xaml`
- **Actions:**
  1. Update `WarningBrush` from `#F59E0B` to `#B45309` (Amber 700) or `#92400E` (Amber 800).
  2. Verify WCAG AA contrast ratio (> 4.5:1) against white backgrounds.

### Work Item 4.2: Replace StaticResource with DynamicResource in PlaceholderView (BD-LOW-003)
- **Files Modified:**
  - `src\BlueDoc.UI\Views\PlaceholderView.xaml`
- **Actions:**
  1. Replace `{StaticResource CardBackgroundBrush}` with `{DynamicResource CardBackgroundBrush}`.
  2. Replace `{StaticResource TextPrimaryBrush}` with `{DynamicResource TextPrimaryBrush}`.

### Work Item 4.3: Consolidate Redundant Converters (BD-LOW-004)
- **Files Modified:**
  - `src\BlueDoc.UI\Views\FixMyConnectionWizardView.xaml`
- **Actions:**
  1. Remove local `StatusBrushConverter` declaration; use global `StatusToBrushConverter`.

### Work Item 4.4: Update Inno Setup Prerequisites for .NET 4.8 (BD-LOW-005)
- **Files Modified:**
  - `installer\bluedoc_setup.iss`
- **Actions:**
  1. Change line 100 check from `installedRelease >= 461308` (.NET 4.7.1) to `installedRelease >= 528040` (.NET 4.8).
  2. Verify installer build using Inno Setup Compiler.

---

## 7. Implementation Checklist & Verification Criteria

| Phase | Milestone | Primary Verification Command | Success Condition |
|---|---|---|---|
| **Phase 0** | .NET 4.8 Retargeting | `MSBuild.exe BlueDoc.sln /p:Configuration=Release` | 0 Errors, Targets .NET 4.8 |
| **Phase 0** | Live BLE GATT Backend | Run `BleExplorerView` against BLE peripheral | Real UUIDs & live byte reads |
| **Phase 1** | PnP Radio Cycling | Trigger "Cycle Radio" from Safe Repair Center | Device Node disabled and re-enabled |
| **Phase 1** | Cross-Thread Dispatcher | 100 rapid events on `ConnectionHistoryView` | 0 `NotSupportedException` |
| **Phase 2** | Dependency Injection | Launch BlueDoc; navigate all 19 views | All ViewModels resolve from IoC |
| **Phase 2** | In-App UAC Elevation | Click "Restart bthserv" as non-admin | UAC elevation prompt appears cleanly |
| **Phase 3** | Zero-Polling Idle | Profile BlueDoc in system tray | 0.0% CPU usage while idle |
| **Phase 3** | Test Suite Cleanup | Run `BlueDoc.Tests.exe` | 100% Pass in ~20 seconds |
| **Phase 4** | Light Theme Accessibility | Run `AccessibilityVerificationTests.cs` | 100% WCAG AA contrast pass |
| **Phase 4** | Final Inno Setup Build | Compile `bluedoc_setup.iss` | Generates `BlueDoc_v1.0.0_Setup.exe` |

