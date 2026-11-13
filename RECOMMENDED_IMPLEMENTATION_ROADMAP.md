# BlueDoc Recommended Implementation Roadmap

**Product:** BlueDoc — Windows Bluetooth Doctor  
**Company:** UNLEFT LLC  
**Author:** Principal Software Architect, Senior C# Engineer & Bluetooth Systems Engineer  
**Document Version:** 1.0 — Architecture Roadmap  
**Date:** November 2026  
**Target Operating Systems:** Windows 7 SP1, Windows 10, Windows 11  

---

## 1. Principles of the Redesign & Implementation

To transform BlueDoc from its current state (with functional diagnostics and inspection, but simulated BLE, simulated SaaS, missing in-app pairing, and missing connections) into a genuine, production-grade Bluetooth Doctor, development must adhere strictly to these engineering principles:

1. **Do Not Rewrite the Application:** Preserve the existing multi-project architecture, WPF MVVM patterns, SQLite data models, and verified SetupAPI/PnP enumeration logic.
2. **Do Not Introduce Mock Success Responses:** All operations must communicate with real Windows Bluetooth APIs and report genuine hardware status. If an operation is unsupported on the host platform, report `Not Supported` honestly.
3. **No Breaking Framework Changes:** Maintain full compatibility across Windows 7 SP1, Windows 10, and Windows 11. Reconcile `.csproj` targets between .NET 4.7.1 and 4.8 without abandoning Windows 7 SP1 support.
4. **Fulfill the Core Product Promise:** Deliver in-app device pairing and profile connection management so that users never have to navigate Windows Settings.
5. **Local-First Integrity:** Isolate or repurpose the mock SaaS fleet server so the core application remains 100% local-first and privacy-respecting as mandated by the PRD.

---

## 2. Phased Architectural Roadmap

```text
+----------------------------------------------------------------------------------------------------+
|                                    BLUEDOC IMPLEMENTATION PHASES                                   |
+----------------------------------------------------------------------------------------------------+
| PHASE 1: Real In-App Pairing & Authentication Subsystem                                            |
| - Eliminate external Windows Settings launcher                                                     |
| - Implement Win32 BluetoothAuthenticateDeviceEx callback pump for Windows 7                        |
| - Implement Windows.Devices.Enumeration.DeviceInformationCustomPairing for Windows 10/11           |
+----------------------------------------------------------------------------------------------------+
| PHASE 2: Profile-Aware Device Connection & Disconnection Management                                |
| - Implement Core Audio (IMMDeviceEnumerator / MMDevice) connection trigger for A2DP/HFP headsets   |
| - Implement HID peripheral link verification and wake detection                                    |
| - Implement RFCOMM / SPP Winsock connection management                                             |
| - Implement genuine device disconnection and unpairing                                             |
+----------------------------------------------------------------------------------------------------+
| PHASE 3: Dual-Stack Modern BLE & GATT Explorer Architecture                                        |
| - Implement real BluetoothLEAdvertisementWatcher for active BLE discovery & live RSSI (Win 10/11)  |
| - Replace synthetic GattProfileCatalog with real Windows.Devices.Bluetooth.GenericAttributeProfile  |
| - Implement real characteristic Read, WriteWithResult, and ValueChanged notification listeners     |
| - Provide honest "Not Supported on Windows 7 SP1" degradation                                      |
+----------------------------------------------------------------------------------------------------+
| PHASE 4: UI/UX Modernization, Theming & Accessibility                                              |
| - Replace all {StaticResource} theme brush bindings with {DynamicResource} for working Light theme |
| - Convert rigid fixed-width columns (420px) into responsive proportional grids                     |
| - Bind IsLoading properties to visual progress overlays across all views                           |
| - Add friendly empty-state panels to all device and history lists                                  |
| - Implement AutomationProperties (Name, HelpText) for complete screen reader accessibility         |
+----------------------------------------------------------------------------------------------------+
| PHASE 5: Runtime History Recording, Safe Repairs & Local-First Isolation                           |
| - Wire HistoryService into discovery, pairing, and connection workflows in real time              |
| - Replace simulated CycleBluetoothRadio with true controller reset                                 |
| - Isolate MockSaasCloudApiGateway into an optional developer diagnostic demo module                |
+----------------------------------------------------------------------------------------------------+
```

---

## 3. Prioritized Task List (Awaiting Approval)

Below is the granular, file-by-file implementation task backlog. **No code modifications will be executed until the user explicitly reviews and approves this list.**

### Priority 1: Real In-App Pairing & PIN Handling (Core User Outcome)
- [ ] **Task 1.1: Expand `IBluetoothProvider` Interface**
  - **File:** `src\BlueDoc.Core\Interfaces\IBluetoothProvider.cs`
  - **Changes:** Add method signatures for `PairDeviceAsync(string address, string pin = null)`, `CancelPairingAsync(string address)`, `ConnectProfileAsync(string address, string profileType)`, `DisconnectDeviceAsync(string address)`, and event `EventHandler<PairingRequestEventArgs> PairingRequested`.
- [ ] **Task 1.2: Implement Win32 Authentication Callback Pump**
  - **File:** `src\BlueDoc.Infrastructure\Native\BluetoothNative.cs` & `src\BlueDoc.Bluetooth\Legacy\Win32BluetoothRadioProvider.cs`
  - **Changes:** P/Invoke `BluetoothRegisterForAuthenticationEx`, `BluetoothAuthenticateDeviceEx`, `BluetoothSendAuthenticationResponseEx`, and `BluetoothUnregisterAuthentication`. Implement the unmanaged authentication callback to support Numeric Comparison (SSP) and Legacy PIN entry on Windows 7, 10, and 11.
- [ ] **Task 1.3: Implement Modern WinRT Pairing Provider for Windows 10 & 11**
  - **File:** `src\BlueDoc.Bluetooth\Modern\WinRtBluetoothProvider.cs` *(New Class)*
  - **Changes:** Reference Windows Runtime contracts and implement `DeviceInformationCustomPairing`. Wire `PairingRequested` event to surface PIN display or confirmation prompt to the user interface.
- [ ] **Task 1.4: Build In-App Pairing Modal Dialog in WPF**
  - **File:** `src\BlueDoc.UI\Views\PairingDialog.xaml` *(New View & ViewModel)*
  - **Changes:** Create a clean modal prompt displaying peripheral name, MAC address, PIN entry field or 6-digit comparison match buttons ("Yes, Passkey Matches" / "No").
- [ ] **Task 1.5: Wire Device Scanner & Device Management to Real In-App Pairing**
  - **File:** `src\BlueDoc.Application\Services\DeviceManagementService.cs`, `BluetoothDevicesViewModel.cs`, `DeviceScannerViewModel.cs`
  - **Changes:** Replace `LaunchWindowsPairingWizard()` calls with `_managementService.PairDeviceAsync(SelectedDevice)`.

---

### Priority 2: Profile-Aware Connection & Disconnection Management
- [ ] **Task 2.1: Implement Audio Endpoint Connection Manager (A2DP / HFP)**
  - **File:** `src\BlueDoc.Infrastructure\Audio\BluetoothAudioInspector.cs` & `src\BlueDoc.Application\Services\DeviceManagementService.cs`
  - **Changes:** Implement audio connection trigger using Windows Core Audio (`IMMDeviceEnumerator`, `IPolicyConfig::SetDefaultEndpoint`). Allow users to explicitly "Connect Audio" or "Disconnect Audio" for paired Bluetooth headphones and speakers.
- [ ] **Task 2.2: Implement Device Disconnection Logic**
  - **File:** `src\BlueDoc.Bluetooth\Abstractions\BluetoothManager.cs`, `DeviceManagementService.cs`
  - **Changes:** Provide profile-specific disconnection commands. Add "Connect" and "Disconnect" buttons to `BluetoothDevicesView.xaml` alongside "Unpair".
- [ ] **Task 2.3: Accurate Real-Time Connection State Detection**
  - **File:** `src\BlueDoc.Infrastructure\Hardware\PnpBluetoothDeviceInspector.cs`
  - **Changes:** Supplement `CM_Get_DevNode_Status` with Core Audio endpoint state (`DEVICE_STATE_ACTIVE`) and Win32 radio handle state so that turned-off paired devices are not erroneously displayed as "Connected".

---

### Priority 3: Dual-Stack Modern BLE & GATT Explorer
- [ ] **Task 3.1: Implement Real BLE Advertisement Scanner**
  - **File:** `src\BlueDoc.Bluetooth\Modern\WinRtBleAdvertisementScanner.cs` *(New Class)*
  - **Changes:** Implement `BluetoothLEAdvertisementWatcher` on Windows 10/11. Capture real BLE advertising frames (Manufacturer Data, Local Name, Tx Power) and live RSSI.
- [ ] **Task 3.2: Wire Live RSSI into Signal Monitor**
  - **File:** `src\BlueDoc.UI\ViewModels\SignalMonitorViewModel.cs`
  - **Changes:** Consume real RSSI dBm values from the BLE watcher. Populate real signal bars, metrics, and CSV exports. On Windows 7, display honest "RSSI requires Windows 10/11 BLE controller" label.
- [ ] **Task 3.3: Implement Real WinRT GATT Client**
  - **File:** `src\BlueDoc.Bluetooth\Modern\WinRtGattClient.cs` *(New Class)* & `BleExplorerViewModel.cs`
  - **Changes:** Replace `GattProfileCatalog.GetStandardPeripheralProfile()` with real calls to `BluetoothLEDevice.GetGattServicesAsync()` and `service.GetCharacteristicsAsync()`.
- [ ] **Task 3.4: Real Characteristic Read, Write, and Subscribe**
  - **File:** `src\BlueDoc.UI\ViewModels\BleExplorerViewModel.cs`
  - **Changes:** Replace simulated strings (`"88%"`, `"[Written OK] ACK"`) with real calls to `GattCharacteristic.ReadValueAsync()` and `WriteValueWithResultAsync()`. Hook `ValueChanged` event for live CCCD notifications. On Windows 7, show clear disabled state with explanation.

---

### Priority 4: UI/UX Modernization, Theming & Accessibility
- [ ] **Task 4.1: Convert Theme Brushes from StaticResource to DynamicResource**
  - **Files:** All 16 affected views in `src\BlueDoc.UI\Views\`
  - **Changes:** Replace `{StaticResource TextPrimaryBrush}`, `{StaticResource CardBgBrush}`, etc., with `{DynamicResource ...}` so that clicking "Light Theme" updates all view canvases seamlessly.
- [ ] **Task 4.2: Replace Fixed Column Widths with Responsive Proportions**
  - **Files:** `BluetoothDevicesView.xaml`, `DeviceScannerView.xaml`, `HardwareInspectorView.xaml`, `SafeRepairCenterView.xaml`
  - **Changes:** Replace `<ColumnDefinition Width="420" />` with `<ColumnDefinition Width="1.2*" MinWidth="280" />` and set `ScrollViewer.HorizontalScrollBarVisibility="Auto"` to eliminate content clipping on small displays.
- [ ] **Task 4.3: Add Visual Loading Overlays**
  - **Files:** `BluetoothDevicesView.xaml`, `HardwareInspectorView.xaml`, `DriverAssistantView.xaml`, `BluetoothEventAnalyzerView.xaml`
  - **Changes:** Add centered `ProgressBar` / loading spinner bound to `IsLoading` with smooth fade animation.
- [ ] **Task 4.4: Add Informative Empty-State Panels**
  - **Files:** `BluetoothDevicesView.xaml`, `DeviceScannerView.xaml`, `ConnectionHistoryView.xaml`, `BluetoothEventAnalyzerView.xaml`
  - **Changes:** Add empty-state card panels (icon + title + guidance text + refresh button) displayed when collections have zero items.
- [ ] **Task 4.5: Implement Complete Accessibility Tags**
  - **Files:** All 23 XAML files in `src\BlueDoc.UI\`
  - **Changes:** Add `AutomationProperties.Name` and `AutomationProperties.HelpText` to all buttons, lists, textboxes, and tab controls. Add keyboard access keys (`_Refresh`, `_Scan`).

---

### Priority 5: Runtime Persistence, Safe Repairs & Local-First Integrity
- [ ] **Task 5.1: Wire HistoryService into Live Workflows**
  - **Files:** `DeviceDiscoveryService.cs`, `DeviceManagementService.cs`, `HistoryService.cs`
  - **Changes:** Call `_historyService.RecordDeviceDiscoveredAsync()` on every device found and `RecordConnectionAttemptAsync()` on every pair/connect operation so `ConnectionHistoryView` contains real audit records.
- [ ] **Task 5.2: Replace Simulated CycleBluetoothRadio with True Controller Power Cycle**
  - **File:** `src\BlueDoc.Infrastructure\Repairs\SafeRepairEngine.cs`
  - **Changes:** Update `CycleRadioAsync` to invoke `HardwareRadioManager.SetRadioPowerStateAsync(false)` followed by `SetRadioPowerStateAsync(true)` via CfgMgr32 / SetupAPI instead of merely restarting `bthserv`.
- [ ] **Task 5.3: Isolate Mock SaaS Fleet Server & Label as Demo**
  - **Files:** `MockSaasCloudApiGateway.cs`, `SaasFleetDashboardViewModel.cs`, `SaasFleetDashboardView.xaml`
  - **Changes:** Ensure the mock HTTP server does not auto-start on port 48821 by default. Add clear "Demonstration / Simulation Mode" badge on `SaasFleetDashboardView` to preserve local-first privacy.
- [ ] **Task 5.4: Clean Up Orphaned Views**
  - **File:** `src\BlueDoc.UI\Views\PlaceholderView.xaml`
  - **Changes:** Safely remove or wire orphaned placeholder view.

---

## 4. Next Step & Confirmation Gate

As mandated by the instructions:
> *"At the end, provide a prioritized list of actual implementation tasks and wait for approval before beginning the redesign."*

The prioritized task backlog above provides the exact architectural roadmap to resolve all findings. **Please review and provide your approval or adjustments to proceed with the implementation.**

