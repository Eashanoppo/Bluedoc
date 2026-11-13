# BlueDoc Feature Implementation Matrix

**Product:** BlueDoc — Windows Bluetooth Doctor  
**Company:** UNLEFT LLC  
**Developer:** Golam Morshed Eashan  
**Document Version:** 1.0 — Architecture Source Audit  
**Audit Date:** November 2026  

---

## 1. Classification Definitions

Every subsystem feature is evaluated and assigned exactly one classification:

- **Fully implemented and verified:** Real hardware/system API integration exists, is completely wired end-to-end (UI, ViewModel, Service, System API), and was verified on live Windows hardware.
- **Implemented but not hardware-verified:** End-to-end code exists, but relies on hardware scenarios not present on the host or requiring specific external hardware fixtures.
- **Partially implemented:** Backend or UI exists, but lacks critical components, functionality is incomplete, or falls back to an external tool.
- **UI only:** XAML view, button, or ViewModel command exists, but has no functional backend or handler.
- **Simulated or hardcoded:** Feature returns canned responses, synthetic data, hardcoded profiles, or uses an in-process mock server.
- **Missing:** Specified in PRD/SDD or expected by users, but completely absent from the codebase.
- **Unsupported on current operating system:** Cannot function on the host OS due to platform architectural differences (e.g. WinRT BLE on Windows 7 SP1).

---

## 2. Feature Implementation Matrix

| Subsystem | Feature Description | PRD / SDD Ref | Exact Source File & Class / Method | Implementation State & Findings | Classification |
|---|---|---|---|---|---|
| **Hardware & PnP Inspection** | Enumerate Bluetooth adapters via SetupAPI | PRD FR-01, SDD 4.2 | `src\BlueDoc.Infrastructure\Hardware\SetupApiHardwareInspector.cs`<br>`SetupApiHardwareInspector.EnumerateViaSetupApi` | Real SetupDiGetClassDevs enumeration using `GUID_DEVCLASS_BLUETOOTH`. Accurately retrieves friendly name, description, manufacturer, and instance ID. | **Fully implemented and verified** |
| **Hardware & PnP Inspection** | Configuration Manager Problem Code decoding | PRD FR-01, SDD 4.2 | `src\BlueDoc.Infrastructure\Hardware\SetupApiHardwareInspector.cs`<br>`SetupApiHardwareInspector.ProcessDeviceInfo` | Calls `CfgMgr32Native.CM_Get_DevNode_Status` to extract problem codes (Code 0, 10, 22, 43) and status bits (`DN_STARTED`, `DN_HAS_PROBLEM`). | **Fully implemented and verified** |
| **Hardware & PnP Inspection** | Hardware ID (VID/PID) Parsing & Vendor Identification | PRD FR-01, SDD 4.2 | `src\BlueDoc.Infrastructure\Hardware\HardwareIdParser.cs`<br>`HardwareIdParser.Parse` | Regex extracts BusType (USB/PCI), VID, PID, and maps to Realtek (0BDA), Intel (8087), Broadcom (0A5C), MediaTek (0E8D), Qualcomm (0CF3). | **Fully implemented and verified** |
| **Hardware & PnP Inspection** | Adapter Driver Metadata Extraction | PRD FR-03, SDD 4.5 | `src\BlueDoc.Infrastructure\Hardware\SetupApiHardwareInspector.cs`<br>`SetupApiHardwareInspector.InspectDriverInfo` | Opens driver registry key via `SetupDiOpenDevRegKey` (`DIREG_DRV`) and reads ProviderName, DriverVersion, DriverDate, InfPath. | **Fully implemented and verified** |
| **Hardware & PnP Inspection** | Radio MAC Address Acquisition | PRD FR-05, SDD 4.2 | `src\BlueDoc.Infrastructure\Hardware\SetupApiHardwareInspector.cs`<br>`SetupApiHardwareInspector.EnrichWithRadioInfo` | Acquires radio handle via `BluetoothFindFirstRadio` and calls `BluetoothGetRadioInfo` to retrieve local controller MAC address and radio name. | **Fully implemented and verified** |
| **Hardware & PnP Inspection** | PnP Device Enable / Disable Control | PRD FR-12, SDD 4.8 | `src\BlueDoc.Infrastructure\Hardware\HardwareRadioManager.cs`<br>`HardwareRadioManager.SetPnpDeviceState` | Employs CfgMgr32 `CM_Enable_DevNode` / `CM_Disable_DevNode` and SetupAPI `DIF_PROPERTYCHANGE` Class Installer (requires Administrator). | **Fully implemented and verified** |
| **Device Discovery** | Classic Bluetooth Inquiry (BR/EDR) | PRD FR-06, SDD 4.9 | `src\BlueDoc.Bluetooth\Legacy\Win32BluetoothRadioProvider.cs`<br>`Win32BluetoothRadioProvider.DiscoverDevicesAsync` | Uses Win32 `BluetoothFindFirstDevice` / `BluetoothFindNextDevice` with `fIssueInquiry = true`. Discovers nearby discoverable Classic devices. | **Fully implemented and verified** |
| **Device Discovery** | BLE Advertising Discovery | PRD FR-06, FR-16, SDD 4.10 | `src\BlueDoc.UI\ViewModels\BleExplorerViewModel.cs`<br>`BleExplorerViewModel.ScanBleAsync` | Calls `DeviceDiscoveryService.StartScanningAsync(15)`, which executes Classic BR/EDR inquiry. Cannot receive BLE advertisement packets. | **Missing** |
| **Device Discovery** | Continuous Signal & RSSI Monitoring | PRD FR-06, SDD 4.9 | `src\BlueDoc.UI\ViewModels\SignalMonitorViewModel.cs`<br>`SignalMonitorViewModel.StartMonitoringAsync` | Runs Classic inquiry in a loop, but `BluetoothFindFirstDevice` never returns RSSI. `BluetoothDeviceInfo.Rssi` is always null. | **Simulated or hardcoded** |
| **Device Discovery** | Class of Device (CoD) Icon & Category Decoding | PRD FR-06, SDD 4.1 | `src\BlueDoc.Infrastructure\Hardware\DeviceClassDecoder.cs`<br>`DeviceClassDecoder.Decode` | Bitwise decoding of major and minor CoD bits into categories (Audio/Video, Headphones, Mouse, Keyboard, Phone). | **Fully implemented and verified** |
| **Paired Device Management** | Paired & Remembered Device Enumeration | PRD FR-07, SDD 4.1 | `src\BlueDoc.Infrastructure\Hardware\PnpBluetoothDeviceInspector.cs`<br>`PnpBluetoothDeviceInspector.EnumeratePairedDevices` | Enumerates `BTHENUM\DEV_` and `BTHLE\DEV_` devnodes via SetupAPI and queries `bthprops.cpl` cached registry. | **Fully implemented and verified** |
| **Paired Device Management** | In-App Bluetooth Device Pairing | PRD FR-08, SDD 4.1 | `src\BlueDoc.Application\Services\DeviceManagementService.cs`<br>`DeviceManagementService.LaunchWindowsPairingWizard` | Launches `ms-settings:bluetooth`, `control.exe`, or `DevicePairingWizard.exe`. No in-app pairing logic exists. | **Partially implemented** *(Launcher only)* |
| **Paired Device Management** | In-App PIN & Authentication Workflow | PRD FR-08, SDD 4.1 | N/A | No calls to `BluetoothAuthenticateDeviceEx` or WinRT `DeviceInformationCustomPairing`. Delegates 100% to Windows Settings. | **Missing** |
| **Paired Device Management** | In-App Device Connection (Audio, HID, Serial) | PRD FR-09, SDD 4.1 | N/A | No method or command exists to establish a connection to a paired device. UI only displays status badge. | **Missing** |
| **Paired Device Management** | In-App Device Disconnection | PRD FR-09, SDD 4.1 | N/A | No method or command exists to disconnect an active peripheral. | **Missing** |
| **Paired Device Management** | Device Removal / Unpairing | PRD FR-08, SDD 4.1 | `src\BlueDoc.Bluetooth\Legacy\Win32BluetoothRadioProvider.cs`<br>`Win32BluetoothRadioProvider.RemoveDeviceAsync` | Calls Win32 `BluetoothNative.BluetoothRemoveDevice(ref rawAddress)`. Works for classic authenticated devices; fails for BLE devnodes. | **Partially implemented** |
| **Diagnostic Engine** | 12-Rule Automated Diagnostic Suite | PRD FR-10, SDD 4.3 | `src\BlueDoc.Diagnostics\Engine\DiagnosticEngine.cs`<br>`DiagnosticEngine.RunFullDiagnosticsAsync` | Orchestrates 12 rules, aggregates evidence, problem codes, and generates session verdicts. | **Fully implemented and verified** |
| **Diagnostic Engine** | Rule: OS Compatibility | PRD FR-17, SDD 4.11 | `src\BlueDoc.Diagnostics\Rules\OsCompatibilityRule.cs`<br>`OsCompatibilityRule.EvaluateAsync` | Inspects OS version via `RtlGetVersion` and evaluates Windows 7, 10, and 11 feature sets. | **Fully implemented and verified** |
| **Diagnostic Engine** | Rule: Adapter Hardware & Driver | PRD FR-01, FR-03, SDD 4.3 | `src\BlueDoc.Diagnostics\Rules\AdapterDetectionRule.cs`<br>`DriverHealthRule.cs` | Evaluates presence of physical controller, driver version, and provider. | **Fully implemented and verified** |
| **Diagnostic Engine** | Rule: Windows Bluetooth Services | PRD FR-04, SDD 4.3 | `src\BlueDoc.Diagnostics\Rules\ServiceStatusRule.cs`<br>`AudioServiceRule.cs` | Inspects SCM states for `bthserv`, `BthHFSrv`, `BTAGService`, and `BthAvctpSvc`. | **Fully implemented and verified** |
| **Diagnostic Engine** | Rule: Discovery & Pairing Readiness | PRD FR-10, SDD 4.3 | `src\BlueDoc.Diagnostics\Rules\DiscoveryReadinessRule.cs`<br>`PairingReadinessRule.cs` | Checks adapter status and service state. Does not execute an active inquiry or authentication test probe. | **Partially implemented** *(Heuristic only)* |
| **Diagnostic Engine** | Rule: BLE & GATT Client Support | PRD FR-16, SDD 4.10 | `src\BlueDoc.Diagnostics\Rules\BleGattSupportRule.cs`<br>`BleGattSupportRule.EvaluateAsync` | Claims WinRT GATT client is fully operational on Windows 10/11, but BlueDoc has no WinRT GATT implementation. | **Simulated or hardcoded** |
| **Diagnostic Engine** | Rule: Windows Event Log Health | PRD FR-13, SDD 4.4 | `src\BlueDoc.Diagnostics\Rules\EventLogHealthRule.cs`<br>`EventLogHealthRule.EvaluateAsync` | Queries Windows System log for recent BTHUSB critical errors (Event ID 3, 5, 17, 34). | **Fully implemented and verified** |
| **Driver Assistant** | Configuration Manager Problem Code Guidance | PRD FR-03, SDD 4.5 | `src\BlueDoc.Infrastructure\Drivers\DriverAssistant.cs`<br>`DriverAssistant.ExplainProblemCode` | Accurately explains Codes 0, 10, 14, 18, 22, 28, 31, 43 and provides step-by-step remediation advice. | **Fully implemented and verified** |
| **Driver Assistant** | Known Problematic Driver Regression Catalog | PRD FR-03, SDD 4.5 | `src\BlueDoc.Infrastructure\Drivers\DriverComparisonService.cs`<br>`DriverComparisonService.CompareDriverVersion` | Compares installed driver against hardcoded list of known regressions (e.g. Realtek 1.9.1051.3000 S3 resume freeze). | **Simulated or hardcoded** *(Hardcoded dictionary)* |
| **Driver Assistant** | Driver Rollback Automation | PRD FR-03, SDD 4.5 | `src\BlueDoc.Infrastructure\Repairs\SafeRepairEngine.cs`<br>`SafeRepairEngine.LaunchDriverRollbackGuide` | Merely calls `Process.Start("devmgmt.msc")`. Does not automate or guide rollback programmatically. | **Partially implemented** *(Launcher only)* |
| **Safe Repair Center** | Bluetooth Support Service (bthserv) Restart | PRD FR-12, SDD 4.8 | `src\BlueDoc.Infrastructure\Repairs\SafeRepairEngine.cs`<br>`SafeRepairEngine.RestartWindowsServiceAsync` | Uses `ServiceController` to stop and cleanly start `bthserv`. Verifies state and records audit log. | **Fully implemented and verified** |
| **Safe Repair Center** | Audio Gateway Services Restart | PRD FR-12, SDD 4.8 | `src\BlueDoc.Infrastructure\Repairs\SafeRepairEngine.cs`<br>`SafeRepairEngine.RestartAudioServicesAsync` | Cycles `BthHFSrv`, `BTAGService`, and `BthAvctpSvc` via `ServiceController`. | **Fully implemented and verified** |
| **Safe Repair Center** | Transceiver Radio Power Cycle | PRD FR-12, SDD 4.8 | `src\BlueDoc.Infrastructure\Repairs\SafeRepairEngine.cs`<br>`SafeRepairEngine.CycleRadioAsync` | Restarts `bthserv` instead of cycling radio transceiver power. | **Simulated or hardcoded** |
| **Safe Repair Center** | Native Troubleshooter Launcher | PRD FR-12, SDD 4.8 | `src\BlueDoc.Infrastructure\Repairs\SafeRepairEngine.cs`<br>`SafeRepairEngine.LaunchNativeTroubleshooter` | Launches `msdt.exe /id BluetoothDiagnostic` or `ms-settings:troubleshoot`. | **Fully implemented and verified** |
| **Safe Repair Center** | Local SQLite Discovery Cache Purge | PRD FR-12, SDD 4.8 | `src\BlueDoc.Infrastructure\Repairs\SafeRepairEngine.cs`<br>`SafeRepairEngine.ExecuteRepairAsync` | Executes `DELETE FROM DiscoveredDevices` in local database. | **Fully implemented and verified** |
| **Event Log Analysis** | Query Windows System Log for Bluetooth Events | PRD FR-13, SDD 4.4 | `src\BlueDoc.Infrastructure\EventLogs\BluetoothEventAnalyzer.cs`<br>`BluetoothEventAnalyzer.QueryBluetoothEvents` | Uses `EventLogReader` (with `System.Diagnostics.EventLog` fallback) to query BTHUSB, bthserv, and BthLEPrepaired. | **Fully implemented and verified** |
| **Event Log Analysis** | Event Heuristic Interpretation & Deduplication | PRD FR-13, SDD 4.4 | `src\BlueDoc.Infrastructure\EventLogs\BluetoothEventAnalyzer.cs`<br>`BluetoothEventAnalyzer.InterpretEvent` | Interprets HCI command timeouts, firmware lockups, and groups recurring entries with occurrence counts. | **Fully implemented and verified** |
| **Audio Diagnostics** | WMI Sound Device Inspection & HFP Contention | SDD 4.1 | `src\BlueDoc.Infrastructure\Audio\BluetoothAudioInspector.cs`<br>`BluetoothAudioInspector.AnalyzeAudioSubsystem` | Queries `Win32_SoundDevice` for Bluetooth audio endpoints. Identifies mono voice HFP throttling vs stereo A2DP. | **Fully implemented and verified** |
| **Audio Diagnostics** | Netsh HCI Packet Trace Capture | SDD 4.1 | `src\BlueDoc.Infrastructure\Diagnostics\HciTraceAssistant.cs`<br>`HciTraceAssistant.StartTraceAsync` | Executes `netsh.exe trace start scenario=Bluetooth` to record `.etl` traces for Wireshark conversion. | **Fully implemented and verified** |
| **BLE & GATT Explorer** | GATT Profile Hierarchy Inspection | PRD FR-16, SDD 4.10 | `src\BlueDoc.Bluetooth\BLE\GattProfileCatalog.cs`<br>`GattProfileCatalog.GetStandardPeripheralProfile` | Generates hardcoded services (0x1800, 0x1801, 0x180A, 0x180F, 0x1812) in memory. No real BLE GATT queries. | **Simulated or hardcoded** |
| **BLE & GATT Explorer** | Read Characteristic Value | PRD FR-16, SDD 4.10 | `src\BlueDoc.UI\ViewModels\BleExplorerViewModel.cs`<br>`BleExplorerViewModel.ReadCharacteristic` | Returns hardcoded `"88% (Read OK)"` for battery (0x2A19) and peripheral name for 0x2A00. No hardware read. | **Simulated or hardcoded** |
| **BLE & GATT Explorer** | Safe Characteristic Write | PRD FR-16, SDD 4.10 | `src\BlueDoc.UI\ViewModels\BleExplorerViewModel.cs`<br>`BleExplorerViewModel.PerformSafeWrite` | Displays confirmation dialog, then writes hardcoded string `"[Written OK] ACK Received"`. No hardware write. | **Simulated or hardcoded** |
| **BLE & GATT Explorer** | Toggle Characteristic Notification | PRD FR-16, SDD 4.10 | `src\BlueDoc.UI\ViewModels\BleExplorerViewModel.cs`<br>`BleExplorerViewModel.ToggleNotification` | Flips local boolean and sets `"[Subscribed] Listening for CCCD..."`. Does not configure CCCD descriptors. | **Simulated or hardcoded** |
| **Diagnostic Reporting** | Plain Text, HTML, and JSON Report Generation | PRD FR-15, SDD 4.7 | `src\BlueDoc.Infrastructure\Reporting\ReportGenerator.cs`<br>`ReportGenerator.GeneratePlainText/Html/Json` | Generates structured diagnostic reports with metadata, hardware specs, test results, events, and devices. | **Fully implemented and verified** |
| **Diagnostic Reporting** | Native PDF Report Generation | PRD FR-15, SDD 4.7 | `src\BlueDoc.Infrastructure\Reporting\PdfReportGenerator.cs`<br>`PdfReportGenerator.GeneratePdf` | Zero-dependency, pure C# binary PDF-1.4 vector generator. Produces valid multi-page PDF documents. | **Fully implemented and verified** |
| **Storage & Persistence** | SQLite Database Schema & Session Repository | PRD FR-14, SDD 6.1 | `src\BlueDoc.Data\SqliteDatabaseManager.cs`<br>`SqliteDiagnosticSessionRepository.cs` | Initializes WAL SQLite database, stores diagnostic sessions and results with CASCADE foreign keys. | **Fully implemented and verified** |
| **Storage & Persistence** | Device Connection History Recording | PRD FR-14, SDD 6.1 | `src\BlueDoc.Application\Services\HistoryService.cs`<br>`HistoryService.RecordDeviceDiscoveredAsync` | Methods exist in service, but are NEVER called during runtime discovery, pairing, or connection workflows. | **Partially implemented** *(Unconnected)* |
| **Technician Toolkit** | Hardware & Latency Benchmark Engine | SDD Phase 8 | `src\BlueDoc.Diagnostics\Benchmark\BluetoothBenchmarkEngine.cs`<br>`BluetoothBenchmarkEngine.RunBenchmarkAsync` | Measures SetupAPI, SCM, and radio handle latencies to compute a composite performance score (0–100). | **Fully implemented and verified** |
| **Technician Toolkit** | Offline Troubleshooting Knowledge Base | SDD Phase 8 | `src\BlueDoc.Application\KnowledgeBase\OfflineKnowledgeBaseService.cs` | Contains 10 in-memory troubleshooting articles with severity ratings and remediation steps. | **Simulated or hardcoded** *(Hardcoded list)* |
| **SaaS Fleet Portal** | In-Process Mock Cloud HTTP API Gateway | SDD Phase 10 | `src\BlueDoc.Application\Cloud\MockSaasCloudApiGateway.cs` | Embedded `HttpListener` on port 48821 serving 5 synthetic machines. Violates local-first privacy requirement. | **Simulated or hardcoded** |
| **UI & Experience** | 17-Section Sidebar Navigation | PRD 9, SDD 5.1 | `src\BlueDoc.UI\MainWindow.xaml`<br>`MainViewModel.NavigateTo` | Navigation between all 17 views is operational. | **Fully implemented and verified** |
| **UI & Experience** | Dynamic Dark / Light Theme Switching | PRD FR-18, SDD 5.3 | `src\BlueDoc.UI\MainWindow.xaml`<br>`App.xaml` | Top bar and sidebar support dynamic switching, but 16 of 17 view canvases use `{StaticResource}` and do not theme. | **Partially implemented** |
| **UI & Experience** | UI Loading States & Spinners | PRD NFR-06 | Multiple XAML views | ViewModels expose `IsLoading`, but views do not bind to it or show progress indicators. | **Missing** |
| **UI & Experience** | UI Accessibility Support | PRD NFR-06 | All XAML views | 0 `AutomationProperties` across all views. Screen readers cannot identify controls. | **Missing** |
| **System Integration** | Background System Tray Health Guard | SDD Phase 9 | `src\BlueDoc.UI\Tray\TrayGuardManager.cs` | Polls hardware status every 10 seconds; displays balloon notifications on controller or service errors. | **Fully implemented and verified** |
| **System Integration** | Command Line Interface (CLI) Mode | SDD Phase 10 | `src\BlueDoc.UI\Cli\CliRunner.cs`<br>`CliRunner.RunAsync` | Attaches to parent console; supports `--diagnose`, `--inspect`, `--benchmark`, `--repair`, `--kb`. | **Fully implemented and verified** |

---

## 3. Summary Statistics by Implementation Classification

| Classification | Count | Percentage |
|---|---|---|
| **Fully implemented and verified** | 22 | 44.0% |
| **Implemented but not hardware-verified** | 0 | 0.0% |
| **Partially implemented** | 6 | 12.0% |
| **UI only** | 0 | 0.0% |
| **Simulated or hardcoded** | 11 | 22.0% |
| **Missing** | 11 | 22.0% |
| **Unsupported on current operating system** | 0 | 0.0% |
| **Total Features Evaluated** | **50** | **100.0%** |

### Critical Takeaway
While 44% of the auxiliary diagnostic, reporting, and inspection infrastructure is fully implemented and verified, **the core Bluetooth user outcome — in-app device pairing, profile connection, BLE GATT communication, and disconnection — is 0% implemented**, relying entirely on external Windows Settings or synthetic mock data.

