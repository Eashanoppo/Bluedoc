# 04. Functional Requirements Traceability Matrix — BlueDoc

**Product:** BlueDoc  
**Company:** UNLEFT LLC  
**Lead Developer:** Golam Morshed Eashan  
**Audit Date:** November 14, 2026  
**Auditor:** Senior Forensic Audit Consortium  

---

## 1. Traceability Methodology & Status Classifications

In accordance with Audit Execution Rule 3 and Rule 4, requirements are never marked as "VERIFIED WORKING" merely because a button or method exists. Each requirement has been audited across four separate dimensions:
1. **UI Status:** Does the XAML interface provide the necessary controls, bindings, and states?
2. **Backend Status:** Do the application and infrastructure services implement the required operations?
3. **Integration Status:** Are the UI commands, ViewModels, and services genuinely connected end-to-end?
4. **Runtime Status:** Has the operation been executed and observed on live Windows hardware or verified through automated tests?

### Status Definitions
- **VERIFIED WORKING:** Traced end-to-end and verified on live Windows operating system / hardware.
- **PARTIALLY WORKING:** Core functionality works, but specific sub-features or edge cases are missing or incomplete.
- **UI ONLY:** Interface exists and binds, but backend operation is missing or disconnected.
- **BACKEND ONLY:** Backend service functions, but no UI screen or button exposes it.
- **PLACEHOLDER:** UI or backend returns hardcoded, mock, or synthetic responses.
- **BROKEN:** Implemented but fails at runtime due to code defects or unhandled exceptions.
- **NOT IMPLEMENTED:** Planned in PRD but absent from the codebase.
- **BLOCKED BY PLATFORM:** Feature requires OS APIs unavailable on specific target platforms (e.g. WinRT GATT on Win7).

---

## 2. Complete PRD Functional Requirements Matrix (FR-01 to FR-18)

| Requirement ID | PRD Requirement | Actual Implementation Files | Overall Status | UI Status | Backend Status | Integration Status | Runtime Status | Associated Bugs | Missing Components / Defect Summary | Recommendation |
|---|---|---|---|---|---|---|---|---|---|---|
| **FR-01** | Bluetooth Adapter Detection | `SetupApiHardwareInspector.cs`<br/>`AdapterService.cs`<br/>`DashboardViewModel.cs`<br/>`HardwareInspectorViewModel.cs`<br/>`HardwareInspectorView.xaml` | **VERIFIED WORKING** | Complete (Cards, detail sheets, telemetry) | Complete (SetupAPI, CfgMgr32, registry) | Fully Connected | Tested on live Win11 hardware | None | None. Multiple adapter identification, FriendlyName, Manufacturer, HardwareID, and PnP ProblemCode verified. | Maintain existing implementation. Add unit tests for non-USB bus adapters. |
| **FR-02** | Hardware Inspection | `SetupApiHardwareInspector.cs`<br/>`HardwareIdParser.cs`<br/>`HardwareRadioManager.cs`<br/>`HardwareInspectorView.xaml` | **VERIFIED WORKING** | Complete (Bus type, VID/PID badges, status) | Complete (SetupAPI device tree & parser) | Fully Connected | Tested on live Win11 hardware | None | Identifies internal USB controllers, PCI controllers, and extracts VID/PID accurately. | Maintain existing implementation. |
| **FR-03** | Driver Diagnosis | `SetupApiHardwareInspector.cs`<br/>`DriverAssistant.cs`<br/>`DriverComparisonService.cs`<br/>`DriverAssistantViewModel.cs`<br/>`DriverAssistantView.xaml` | **VERIFIED WORKING** | Complete (Version badges, vendor links) | Complete (Registry parsing, vendor database) | Fully Connected | Tested via automated unit & live tests | None | Full driver version comparison against known Realtek, Intel, Broadcom regressions. Direct manufacturer URL mapping. | Expand vendor database with newer 2026 driver revisions. |
| **FR-04** | Bluetooth Service Monitoring | `WindowsServiceInspector.cs`<br/>`ServiceStatusRule.cs`<br/>`AudioServiceRule.cs`<br/>`SafeRepairEngine.cs` | **VERIFIED WORKING** | Complete (Service cards, running badges) | Complete (`System.ServiceProcess`) | Fully Connected | Tested on live system | None | Inspects `bthserv`, `BTAGService`, and `BthAvctpSvc`. Detects Stopped, Running, and Disabled states. | Maintain existing implementation. |
| **FR-05** | Bluetooth Radio Diagnostics | `Win32BluetoothRadioProvider.cs`<br/>`HardwareRadioManager.cs`<br/>`RadioAvailabilityRule.cs`<br/>`DashboardView.xaml` | **VERIFIED WORKING** | Complete (Radio ON/OFF badge & quick toggle) | Complete (`BluetoothFindFirstRadio`, CfgMgr32) | Fully Connected | Tested on live Win11 hardware | None | Radio handle validation, DevNode status verification, radio toggle via hardware devnode. | Enhance airplane mode API detection on Win10/11. |
| **FR-06** | Nearby Bluetooth Scanner | `Win32BluetoothRadioProvider.cs`<br/>`BleAdvertisementWatcherProvider.cs`<br/>`DeviceDiscoveryService.cs`<br/>`DeviceScannerViewModel.cs`<br/>`DeviceScannerView.xaml` | **VERIFIED WORKING** | Complete (Filters, signal bars, skeletons) | Complete (DualMode: Win32 Classic + WinRT BLE) | Fully Connected | Tested on live system | None | DualMode scanning, in-place RSSI telemetry updates, multi-category filters, and deduplication verified. | Maintain existing architecture. |
| **FR-07** | Device Information | `BluetoothDeviceInfo.cs`<br/>`DeviceClassDecoder.cs`<br/>`PnpBluetoothDeviceInspector.cs`<br/>`BluetoothDevicesView.xaml` | **VERIFIED WORKING** | Complete (Inspector side sheet, icon, badges) | Complete (PnP tree & Win32 inquiry) | Fully Connected | Tested on live Win11 hardware | None | Displays Name, MAC address, Major class, Category, Type, Connected, Paired, and Service profiles. | Maintain existing implementation. |
| **FR-08** | Device Pairing | `DeviceManagementService.cs`<br/>`BlePairingProvider.cs`<br/>`BluetoothDevicesViewModel.cs`<br/>`DeviceScannerViewModel.cs` | **VERIFIED WORKING** | Complete (In-app pairing sheet, PIN modal) | Complete (Win32 Authenticate & WinRT Custom) | Fully Connected | Tested via automated test harness | BD-MED-002 | Complete in-app pairing without external settings redirection. External launcher exists as dead code. | Clean up dead `LaunchWindowsPairingWizard` fallback method. |
| **FR-09** | Device Connection Management | `DeviceManagementService.cs`<br/>`BluetoothConnectionOrchestrator.cs`<br/>`BluetoothDevicesViewModel.cs`<br/>`DeviceScannerViewModel.cs` | **PARTIALLY WORKING** | Complete (Connect/Disconnect buttons, stages) | Audio/HID/RFCOMM works; BLE mocked | Partially Connected | Live tested for Classic/Audio | BD-HIGH-002 | `BluetoothConnectionOrchestrator.ConnectBlePeripheralAsync` simulates success without calling WinRT APIs. | Replace mock BLE connect with true `BluetoothLEDevice` GATT connection link. |
| **FR-10** | Intelligent Diagnostic Engine | `DiagnosticEngine.cs`<br/>`12 Diagnostic Rules`<br/>`DiagnosticCenterViewModel.cs`<br/>`DiagnosticCenterView.xaml` | **VERIFIED WORKING** | Complete (Progress bar, counters, result rows) | Complete (Sequential rule pipeline) | Fully Connected | Tested on live system (12/12 Passed) | None | Complete 12-rule pipeline collecting hardware, driver, service, radio, event, and BLE evidence. | Add diagnostic rule export to CSV. |
| **FR-11** | Troubleshooting Assistant | `OfflineKnowledgeBaseService.cs`<br/>`TechnicianToolkitViewModel.cs`<br/>`TechnicianToolkitView.xaml` | **VERIFIED WORKING** | Complete (Articles list, symptom filters, steps) | Complete (Offline knowledge base engine) | Fully Connected | Tested via automated tests | None | Curated offline knowledge base with step-by-step remediation for Code 43, Sleep desync, Audio stutter, etc. | Maintain existing implementation. |
| **FR-12** | Safe Repair Center | `SafeRepairEngine.cs`<br/>`RepairService.cs`<br/>`SafeRepairCenterViewModel.cs`<br/>`SafeRepairCenterView.xaml` | **PARTIALLY WORKING** | Complete (5-point safety modal, audit log) | 7 of 8 repairs functional; Radio cycle flawed | Fully Connected | Tested via automated test harness | BD-HIGH-004<br/>BD-MED-006 | `CycleBluetoothRadio` restarts service instead of cycling hardware. `LaunchTroubleshooter` calls deprecated `msdt.exe`. | Wire `CycleBluetoothRadio` to `HardwareRadioManager.SetRadioPowerStateAsync`. Bypass `msdt.exe` on Win11. |
| **FR-13** | Bluetooth Event Analyzer | `BluetoothEventAnalyzer.cs`<br/>`EventAnalysisService.cs`<br/>`BluetoothEventAnalyzerViewModel.cs`<br/>`BluetoothEventAnalyzerView.xaml` | **VERIFIED WORKING** | Complete (Filter bar, severity icons, stats) | Complete (`EventLogReader` & grouping) | Fully Connected | Tested on live Win11 system | None | Queries System event log for BTHUSB, bthserv, groups recurring errors, and provides human analysis. | Maintain existing implementation. |
| **FR-14** | Device Connection History | `SqliteConnectionHistoryRepository.cs`<br/>`HistoryService.cs`<br/>`ConnectionHistoryViewModel.cs`<br/>`ConnectionHistoryView.xaml` | **VERIFIED WORKING** | Complete (Search, timeline rows, clear button) | Complete (SQLite with indexed timestamps) | Fully Connected | Tested via SQLite test suite | None | Records discovered, paired, connected, disconnected, and failed events with timestamps and details. | Maintain existing implementation. |
| **FR-15** | Diagnostic Report Generator | `ReportGenerator.cs`<br/>`PdfReportGenerator.cs`<br/>`DiagnosticReportViewModel.cs`<br/>`DiagnosticReportView.xaml` | **VERIFIED WORKING** | Complete (Format selector, live preview, save) | Complete (PDF 1.4, HTML, TXT, JSON) | Fully Connected | Tested via unit & export tests | None | Zero-dependency PDF generation, styled HTML, structured JSON, and plain text reports verified. | Maintain existing implementation. |
| **FR-16** | Advanced Bluetooth LE Explorer | `GattProfileCatalog.cs`<br/>`BleExplorerViewModel.cs`<br/>`BleExplorerView.xaml` | **PLACEHOLDER** | Complete (Tree view, characteristic values) | Synthetic Catalog (Not live GATT query) | Disconnected from live GATT | Tested in UI | BD-HIGH-001 | GATT exploration loads a static catalog (`GetStandardPeripheralProfile`). Read, write, and notify operations are simulated. | Implement live GATT discovery using `BluetoothLEDevice.GetGattServicesAsync()` and `GattCharacteristic.ReadValueAsync()`. |
| **FR-17** | Compatibility Checker | `CompatibilityService.cs`<br/>`OperatingSystemInspector.cs`<br/>`CompatibilityViewModel.cs`<br/>`CompatibilityView.xaml` | **VERIFIED WORKING** | Complete (Matrix table, verdicts, details) | Complete (Subsystem evaluation engine) | Fully Connected | Tested across Win7/10/11 profiles | None | Accurately assesses Classic Win32, SetupAPI, WinRT BLE, and Event Log APIs across Windows versions. | Maintain existing implementation. |
| **FR-18** | Application Settings | `SqliteSettingsRepository.cs`<br/>`ThemeManager.cs`<br/>`SettingsViewModel.cs`<br/>`SettingsView.xaml` | **VERIFIED WORKING** | Complete (Theme pills, duration combos, toggles) | Complete (SQLite key-value persistence) | Fully Connected | Tested in UI & SQLite tests | None | Theme toggling (Dark/Light), scan duration preference, startup health check toggle, and reset history verified. | Maintain existing implementation. |

---

## 3. Non-Functional Requirements Traceability (NFR-01 to NFR-06)

| NFR ID | Non-Functional Requirement | Architectural Implementation | Verification Method | Status | Findings / Risks |
|---|---|---|---|---|---|
| **NFR-01** | Performance | Async scanning (`Task.Run`), WPF virtualization, non-blocking UI, WAL SQLite mode. | Benchmarked via `PerformanceBenchmarkHarness`. | **MET WITH CONCERN** | UI launch < 150ms. However, `TrayGuardManager` polls SetupAPI every 10 seconds (BD-MED-003). |
| **NFR-02** | Compatibility | Dual-mode platform detection (`RtlGetVersion`), Win32 Classic fallback for Win7. | Tested on Win11; simulated profiles for Win7/10. | **MET WITH CRITICAL RISK** | Binary dependency on `Windows.winmd` in `BlueDoc.Bluetooth` creates `TypeLoadException` risk on Windows 7 SP1 (BD-CRIT-002). Target framework is .NET 4.7.1 instead of 4.8 (BD-MED-007). |
| **NFR-03** | Reliability | Structured try/catch, non-destructive health checks, timeout multipliers on Win32 inquiry. | 18-Scenario hardware matrix tested (100% pass). | **MET** | Gracefully handles missing adapter, stopped service, and radio off without crashing. |
| **NFR-04** | Security | `asInvoker` manifest level, elevation checks for repair actions, input validation. | Audited via static analysis & permissions tests. | **MET** | UAC token validated prior to service restarts. No arbitrary shell execution. |
| **NFR-05** | Privacy | Local-first SQLite database, no default telemetry, local knowledge base, user-triggered reports. | Audited via network inspection & poller audit. | **MET** | SaaS telemetry poller is disabled by default (`_isAutoSyncEnabled = false`). Report export requires explicit user save. |
| **NFR-06** | Usability | Linear/Fluent design language, consistent Segoe UI typography, WCAG AA contrast, skeletons. | Audited via UI tests & multi-resolution renders. | **MET** | All 18 screens render cleanly at 720p, 768p, 900p, and 1080p. Responsive sidebar collapse supported. |

