# 01. Complete File Inventory — BlueDoc Forensic Audit

**Product:** BlueDoc  
**Company:** UNLEFT LLC  
**Lead Developer:** Golam Morshed Eashan  
**Audit Date:** November 14, 2026  
**Auditor Team:** Senior Forensic Audit Consortium (Architectural, .NET, WPF, Systems, Bluetooth, QA, UI/UX, Security, Reliability, Documentation)  

---

## 1. Executive Summary & Inventory Statistics

A complete recursive enumeration of the BlueDoc codebase was executed across all root, source, test, script, and documentation directories.

| Metric | Count | Details |
|---|---|---|
| **Total Non-Generated Files** | 410 | Source files, scripts, markup, styles, configs, and assets |
| **C# Source Files (`.cs`)** | 183 | Totaling 23,532 lines of source code |
| **XAML Files (`.xaml`)** | 33 | Views, application resources, and merged styles (9,354 lines) |
| **Project Files (`.csproj`)** | 8 | All targeting .NET Framework 4.7.1 |
| **Solution File (`.sln`)** | 1 | Visual Studio 2022 Format |
| **PowerShell Scripts (`.ps1`)** | 8 | Build, test automation, rendering, and resolution checks |
| **Inno Setup Script (`.iss`)** | 1 | Windows setup distribution definition |
| **Configuration & Manifests** | 3 | `App.config`, `app.manifest`, `.gitignore` |
| **Markdown Documents (`.md`)** | 24 | Original PRD, SDD, design systems, and interim test reports |
| **Visual Assets (`.png`, `.ico`)** | 75 | Window icons and automated screenshot captures |
| **Vendor Packages & Binaries** | 74 | Statically committed NuGet binaries (`Newtonsoft.Json`, `System.Data.SQLite`) |

---

## 2. Directory Structure Overview

```text
E:\BlueDoc\
├── .gitignore                          # Git VCS ignore rules
├── BlueDoc.sln                         # Solution descriptor (8 projects)
├── build_release.ps1                   # Build, verification, and packaging script
├── generate_icon.ps1                   # System tray and window icon generator
├── render_screenshots.ps1              # STA automated screen capture script
├── test_all_views.ps1                  # View instantiation test script
├── test_animations_and_loading.ps1     # Animation and skeleton loading test script
├── test_bluetooth_devices.ps1          # Bluetooth devices screen test script
├── test_device_scanner.ps1             # Device scanner verification script
├── verify_resolutions.ps1              # Multi-resolution UI test script
├── Doc\                                # Product specification documents
│   ├── prd.md                          # Product Requirements Document (PRD v1.0)
│   └── design.md                       # Software Design Document (SDD v1.0)
├── installer\                          # Setup and deployment packaging
│   └── bluedoc_setup.iss               # Inno Setup 6+ script
├── packages\                           # Committed NuGet package assemblies
├── screenshots\                        # Visual audit captures & multi-resolution test renders
├── src\                                # Application source code
│   ├── BlueDoc.Core\                   # Domain models, enums, interfaces, exceptions
│   ├── BlueDoc.Infrastructure\         # SetupAPI, CfgMgr32, Event Logs, WMI, Audio, Repairs, Reports
│   ├── BlueDoc.Bluetooth\              # Win32 Bluetooth APIs, WinRT BLE Watcher, Pairing, GATT catalog
│   ├── BlueDoc.Diagnostics\            # Diagnostic Engine, 12 Diagnostic Rules, Benchmarks
│   ├── BlueDoc.Data\                   # SQLite database manager & repository implementations
│   ├── BlueDoc.Application\            # Application services, orchestration, cloud telemetry, KB
│   └── BlueDoc.UI\                     # WPF MVVM presentation layer, Views, ViewModels, Styles
├── tests\                              # Verification test suites
│   └── BlueDoc.Tests\                  # Unit, Integration, Scenario Matrix, Benchmarks
└── tools\                              # Build utilities (nuget.exe)
```

---

## 3. Project-by-Project File Inventory

### 3.1 Project: `BlueDoc.Core`
**Responsibility:** Clean Domain models, contracts, domain primitives, enums, and repository interfaces. No third-party or platform native dependencies.
**Target Framework:** .NET Framework 4.7.1  
**Assembly Output:** `BlueDoc.Core.dll`  

| Relative File Path | Type | Lines | Bytes | Purpose & Responsibility | Audit Status | Key Dependencies |
|---|---|---|---|---|---|---|
| `src\BlueDoc.Core\BlueDoc.Core.csproj` | XML | 75 | 3,892 | MSBuild project definition | Audited | None |
| `src\BlueDoc.Core\Properties\AssemblyInfo.cs` | C# | 23 | 630 | Assembly attributes and versioning | Audited | System.Reflection |
| `src\BlueDoc.Core\Enums\AdapterStatus.cs` | C# | 14 | 219 | Hardware operational status enum | Audited | None |
| `src\BlueDoc.Core\Enums\BluetoothConnectionStage.cs` | C# | 13 | 241 | Connection state machine enum | Audited | None |
| `src\BlueDoc.Core\Enums\BluetoothPairingStage.cs` | C# | 13 | 278 | Pairing state machine enum | Audited | None |
| `src\BlueDoc.Core\Enums\BluetoothStackType.cs` | C# | 12 | 231 | Bluetooth driver stack categorization | Audited | None |
| `src\BlueDoc.Core\Enums\ConfidenceLevel.cs` | C# | 11 | 135 | Diagnostic result confidence enum | Audited | None |
| `src\BlueDoc.Core\Enums\DeviceOperationStatus.cs` | C# | 12 | 226 | Async operation outcome state | Audited | None |
| `src\BlueDoc.Core\Enums\DeviceType.cs` | C# | 11 | 159 | Classic, BLE, DualMode categorization | Audited | None |
| `src\BlueDoc.Core\Enums\DiagnosticStatus.cs` | C# | 12 | 214 | Passed, Failed, Warning, Inconclusive | Audited | None |
| `src\BlueDoc.Core\Enums\DiscoveryMode.cs` | C# | 13 | 326 | DualMode, ClassicOnly, BleOnly | Audited | None |
| `src\BlueDoc.Core\Enums\WindowsPlatformType.cs` | C# | 12 | 217 | Target Windows OS generation | Audited | None |
| `src\BlueDoc.Core\Interfaces\IBluetoothProvider.cs` | C# | 18 | 955 | Abstract contract for Bluetooth providers | Audited | BluetoothDeviceInfo |
| `src\BlueDoc.Core\Interfaces\IHardwareInspector.cs` | C# | 11 | 268 | Contract for adapter enumeration | Audited | BluetoothAdapterInfo |
| `src\BlueDoc.Core\Interfaces\IServiceInspector.cs` | C# | 11 | 270 | Contract for service monitoring | Audited | ServiceInfo |
| `src\BlueDoc.Core\Interfaces\IDiagnosticEngine.cs` | C# | 17 | 635 | Contract for diagnostic execution | Audited | DiagnosticSession |
| `src\BlueDoc.Core\Interfaces\IConnectionHistoryRepository.cs` | C# | 13 | 403 | Contract for connection history persistence | Audited | ConnectionHistoryRecord |
| `src\BlueDoc.Core\Interfaces\IDeviceCacheRepository.cs` | C# | 14 | 373 | Contract for caching discovered devices | Audited | BluetoothDeviceInfo |
| `src\BlueDoc.Core\Interfaces\IDiagnosticSessionRepository.cs` | C# | 15 | 449 | Contract for diagnostic history persistence | Audited | DiagnosticSession |
| `src\BlueDoc.Core\Interfaces\IRepairHistoryRepository.cs` | C# | 13 | 381 | Contract for repair audit logging | Audited | RepairHistoryRecord |
| `src\BlueDoc.Core\Interfaces\ISettingsRepository.cs` | C# | 13 | 391 | Contract for application settings storage | Audited | Key-Value pairs |
| `src\BlueDoc.Core\Models\BluetoothAdapterInfo.cs` | C# | 56 | 1,989 | Adapter hardware specifications model | Audited | HardwareIdDetails |
| `src\BlueDoc.Core\Models\BluetoothDeviceInfo.cs` | C# | 344 | 14,052 | Discovered peripheral telemetry model | Audited | INotifyPropertyChanged |
| `src\BlueDoc.Core\Models\BluetoothEventRecord.cs` | C# | 35 | 1,259 | Windows Event Log record model | Audited | None |
| `src\BlueDoc.Core\Models\ConnectionFailureDiagnosis.cs` | C# | 42 | 1,517 | Root cause analysis output model | Audited | None |
| `src\BlueDoc.Core\Models\ConnectionHistoryRecord.cs` | C# | 23 | 790 | Historical connection entry model | Audited | None |
| `src\BlueDoc.Core\Models\DeviceOperationResult.cs` | C# | 58 | 2,147 | Standardized operation result container | Audited | None |
| `src\BlueDoc.Core\Models\DiagnosticReport.cs` | C# | 26 | 1,002 | Exportable diagnostic report data model | Audited | DiagnosticSession |
| `src\BlueDoc.Core\Models\DiagnosticResult.cs` | C# | 26 | 934 | Individual rule evaluation result | Audited | DiagnosticStatus |
| `src\BlueDoc.Core\Models\DiagnosticSession.cs` | C# | 42 | 1,443 | Complete diagnostic run container | Audited | DiagnosticResult |
| `src\BlueDoc.Core\Models\DriverInfo.cs` | C# | 17 | 654 | Driver metadata and provider model | Audited | None |
| `src\BlueDoc.Core\Models\HardwareIdDetails.cs` | C# | 23 | 796 | Parsed VID, PID, BusType model | Audited | None |
| `src\BlueDoc.Core\Models\OsVersionInfo.cs` | C# | 28 | 954 | Operating system metadata model | Audited | WindowsPlatformType |
| `src\BlueDoc.Core\Models\RepairHistoryRecord.cs` | C# | 23 | 841 | Executed repair audit record | Audited | None |
| `src\BlueDoc.Core\Models\ServiceInfo.cs` | C# | 16 | 608 | Windows service status model | Audited | None |
| `src\BlueDoc.Core\Models\StartupHealthAssessment.cs` | C# | 36 | 1,460 | Initial system health summary model | Audited | None |

---

### 3.2 Project: `BlueDoc.Infrastructure`
**Responsibility:** Low-level Windows platform integration, SetupAPI device enumeration, CfgMgr32 problem codes, Windows Event Log reader, WMI audio inspection, P/Invoke definitions, safe repairs, PDF and multi-format report generation.
**Target Framework:** .NET Framework 4.7.1  
**Assembly Output:** `BlueDoc.Infrastructure.dll`  

| Relative File Path | Type | Lines | Bytes | Purpose & Responsibility | Audit Status | Key Dependencies |
|---|---|---|---|---|---|---|
| `src\BlueDoc.Infrastructure\BlueDoc.Infrastructure.csproj` | XML | 86 | 5,124 | MSBuild project definition | Audited | SetupAPI, CfgMgr32, AdvApi32 |
| `src\BlueDoc.Infrastructure\Properties\AssemblyInfo.cs` | C# | 23 | 640 | Assembly attributes | Audited | System.Reflection |
| `src\BlueDoc.Infrastructure\Native\BluetoothNative.cs` | C# | 154 | 5,941 | Win32 P/Invoke signatures (`bthprops.cpl`) | Audited | bthprops.cpl, kernel32 |
| `src\BlueDoc.Infrastructure\Native\CfgMgr32Native.cs` | C# | 89 | 4,414 | Configuration Manager P/Invoke (`cfgmgr32.dll`) | Audited | cfgmgr32.dll |
| `src\BlueDoc.Infrastructure\Native\NtdllNative.cs` | C# | 28 | 943 | True OS version detector (`RtlGetVersion`) | Audited | ntdll.dll |
| `src\BlueDoc.Infrastructure\Native\SetupApiNative.cs` | C# | 160 | 6,496 | SetupAPI device tree P/Invoke (`setupapi.dll`) | Audited | setupapi.dll |
| `src\BlueDoc.Infrastructure\Platform\OperatingSystemInspector.cs` | C# | 97 | 3,282 | Windows OS, build, and architecture detection | Audited | NtdllNative |
| `src\BlueDoc.Infrastructure\Hardware\DeviceClassDecoder.cs` | C# | 148 | 6,214 | Bluetooth Class of Device (CoD) bitmask decoder | Audited | None |
| `src\BlueDoc.Infrastructure\Hardware\HardwareIdParser.cs` | C# | 195 | 8,024 | USB/PCI hardware identifier parser & vendor map | Audited | Regex |
| `src\BlueDoc.Infrastructure\Hardware\HardwareRadioManager.cs` | C# | 201 | 7,951 | PnP DevNode enable/disable & radio power control | Audited | CfgMgr32Native, SetupApiNative |
| `src\BlueDoc.Infrastructure\Hardware\PnpBluetoothDeviceInspector.cs` | C# | 143 | 6,150 | Enumerates remembered paired devices via PnP | Audited | SetupApiNative, DeviceClassDecoder |
| `src\BlueDoc.Infrastructure\Hardware\SetupApiHardwareInspector.cs` | C# | 419 | 17,695 | SetupAPI Bluetooth adapter enumeration & driver parsing | Audited | SetupApiNative, CfgMgr32Native |
| `src\BlueDoc.Infrastructure\Services\WindowsServiceInspector.cs` | C# | 65 | 2,400 | Service Control Manager inspector | Audited | System.ServiceProcess |
| `src\BlueDoc.Infrastructure\Audio\BluetoothAudioInspector.cs` | C# | 100 | 5,222 | WMI `Win32_SoundDevice` inspector & HFP contention | Audited | System.Management |
| `src\BlueDoc.Infrastructure\Audio\BluetoothAudioModels.cs` | C# | 34 | 1,308 | Audio endpoint and analysis data models | Audited | None |
| `src\BlueDoc.Infrastructure\Diagnostics\HciTraceAssistant.cs` | C# | 138 | 5,794 | Packet capture assistant via `netsh.exe trace` | Audited | Process.Start (netsh.exe) |
| `src\BlueDoc.Infrastructure\Drivers\DriverAssistant.cs` | C# | 162 | 6,501 | Manufacturer URL resolution & Device Manager launch | Audited | Process.Start (devmgmt.msc) |
| `src\BlueDoc.Infrastructure\Drivers\DriverComparisonModels.cs` | C# | 20 | 766 | Driver regression comparison data models | Audited | None |
| `src\BlueDoc.Infrastructure\Drivers\DriverComparisonService.cs` | C# | 162 | 8,176 | Offline database of known vendor driver regressions | Audited | BluetoothAdapterInfo |
| `src\BlueDoc.Infrastructure\EventLogs\BluetoothEventAnalyzer.cs` | C# | 242 | 11,434 | EventLogReader query, filtering, and grouping | Audited | System.Diagnostics.Eventing.Reader |
| `src\BlueDoc.Infrastructure\Repairs\SafeRepairEngine.cs` | C# | 477 | 21,854 | 8 safe repair actions, verification, and audit trail | Audited | ServiceController, Process.Start |
| `src\BlueDoc.Infrastructure\Reporting\PdfReportGenerator.cs` | C# | 387 | 18,581 | Zero-dependency PDF 1.4 report compiler | Audited | DiagnosticReport |
| `src\BlueDoc.Infrastructure\Reporting\ReportGenerator.cs` | C# | 230 | 13,151 | TXT, HTML, JSON, and PDF report orchestration | Audited | Newtonsoft.Json |

---

### 3.3 Project: `BlueDoc.Bluetooth`
**Responsibility:** Bluetooth abstraction layer, Win32 Classic Bluetooth radio provider, Windows 10/11 WinRT BLE Watcher, WinRT Custom Pairing, and GATT profile catalog.
**Target Framework:** .NET Framework 4.7.1  
**Assembly Output:** `BlueDoc.Bluetooth.dll`  

| Relative File Path | Type | Lines | Bytes | Purpose & Responsibility | Audit Status | Key Dependencies |
|---|---|---|---|---|---|---|
| `src\BlueDoc.Bluetooth\BlueDoc.Bluetooth.csproj` | XML | 63 | 3,745 | MSBuild project definition | Audited | Windows.winmd (Win10 SDK) |
| `src\BlueDoc.Bluetooth\Properties\AssemblyInfo.cs` | C# | 23 | 644 | Assembly attributes | Audited | System.Reflection |
| `src\BlueDoc.Bluetooth\Abstractions\BluetoothManager.cs` | C# | 60 | 2,418 | Provider facade for Bluetooth operations | Audited | IBluetoothProvider |
| `src\BlueDoc.Bluetooth\Legacy\Win32BluetoothRadioProvider.cs` | C# | 361 | 16,398 | Native Win32 inquiry (`BluetoothFindFirstDevice`) | Audited | bthprops.cpl, PnpInspector |
| `src\BlueDoc.Bluetooth\BLE\BleAdvertisementWatcherProvider.cs` | C# | 249 | 9,376 | WinRT BLE Advertisement Watcher | Audited | Windows.Devices.Bluetooth |
| `src\BlueDoc.Bluetooth\BLE\BlePairingProvider.cs` | C# | 176 | 8,137 | WinRT BLE Custom Pairing with PIN & comparison | Audited | Windows.Devices.Enumeration |
| `src\BlueDoc.Bluetooth\BLE\GattProfileCatalog.cs` | C# | 263 | 9,660 | Static database of standard GATT UUIDs & profiles | Audited | Synthetic profile synthesizer |

---

### 3.4 Project: `BlueDoc.Diagnostics`
**Responsibility:** Deterministic diagnostic engine, rule evaluation pipeline, 12 specialized diagnostic rules, and performance benchmark engine.
**Target Framework:** .NET Framework 4.7.1  
**Assembly Output:** `BlueDoc.Diagnostics.dll`  

| Relative File Path | Type | Lines | Bytes | Purpose & Responsibility | Audit Status | Key Dependencies |
|---|---|---|---|---|---|---|
| `src\BlueDoc.Diagnostics\BlueDoc.Diagnostics.csproj` | XML | 75 | 4,210 | MSBuild project definition | Audited | BlueDoc.Core, Infrastructure |
| `src\BlueDoc.Diagnostics\Properties\AssemblyInfo.cs` | C# | 23 | 644 | Assembly attributes | Audited | System.Reflection |
| `src\BlueDoc.Diagnostics\Engine\DiagnosticEngine.cs` | C# | 165 | 6,365 | Orchestrates context building & rule execution | Audited | IDiagnosticRule, SetupAPI |
| `src\BlueDoc.Diagnostics\Benchmark\BenchmarkModels.cs` | C# | 35 | 1,360 | Performance benchmark data models | Audited | None |
| `src\BlueDoc.Diagnostics\Benchmark\BluetoothBenchmarkEngine.cs` | C# | 225 | 10,680 | Controller latency, inquiry jitter, and grading | Audited | Stopwatch |
| `src\BlueDoc.Diagnostics\Rules\IDiagnosticRule.cs` | C# | 14 | 445 | Interface for diagnostic rules | Audited | DiagnosticContext |
| `src\BlueDoc.Diagnostics\Rules\DiagnosticContext.cs` | C# | 24 | 873 | Shared system evidence container | Audited | Adapters, Services, Events |
| `src\BlueDoc.Diagnostics\Rules\OsCompatibilityRule.cs` | C# | 114 | 4,282 | RULE_OS_COMPAT: Evaluates OS & architecture | Audited | OperatingSystemInspector |
| `src\BlueDoc.Diagnostics\Rules\AdapterDetectionRule.cs` | C# | 63 | 2,646 | RULE_ADAPTER_DETECT: Checks adapter presence | Audited | BluetoothAdapterInfo |
| `src\BlueDoc.Diagnostics\Rules\HardwareStatusRule.cs` | C# | 88 | 5,356 | RULE_HW_STATUS: Evaluates PnP problem codes (0, 10, 22, 43) | Audited | CfgMgr32 problem codes |
| `src\BlueDoc.Diagnostics\Rules\DriverHealthRule.cs` | C# | 92 | 3,522 | RULE_DRIVER_HEALTH: Checks driver provider & age | Audited | DriverInfo |
| `src\BlueDoc.Diagnostics\Rules\ServiceStatusRule.cs` | C# | 104 | 4,560 | RULE_SERVICE_STATUS: Inspects bthserv running & start type | Audited | ServiceInfo |
| `src\BlueDoc.Diagnostics\Rules\AudioServiceRule.cs` | C# | 78 | 3,606 | RULE_AUDIO_SERVICE: Checks BTAGService and BthAvctpSvc | Audited | ServiceInfo |
| `src\BlueDoc.Diagnostics\Rules\RadioAvailabilityRule.cs` | C# | 76 | 2,763 | RULE_RADIO_AVAIL: Evaluates radio transceiver readiness | Audited | BluetoothManager |
| `src\BlueDoc.Diagnostics\Rules\DiscoveryReadinessRule.cs` | C# | 80 | 3,087 | RULE_DISCOVERY_READY: Checks adapter and service readiness | Audited | DiagnosticContext |
| `src\BlueDoc.Diagnostics\Rules\PairingReadinessRule.cs` | C# | 59 | 2,220 | RULE_PAIRING_READY: Checks bthserv and paired devices | Audited | DiagnosticContext |
| `src\BlueDoc.Diagnostics\Rules\BleAvailabilityRule.cs` | C# | 71 | 2,704 | RULE_BLE_AVAIL: Validates BLE 4.0+ OS & controller support | Audited | OsVersionInfo |
| `src\BlueDoc.Diagnostics\Rules\BleGattSupportRule.cs` | C# | 61 | 2,319 | RULE_BLE_GATT: Validates WinRT GATT API compatibility | Audited | OsVersionInfo |
| `src\BlueDoc.Diagnostics\Rules\EventLogHealthRule.cs` | C# | 84 | 3,143 | RULE_EVENTLOG_HEALTH: Analyzes BTHUSB error frequencies | Audited | BluetoothEventRecord |

---

### 3.5 Project: `BlueDoc.Data`
**Responsibility:** Local-first SQLite database management, schema migration, and repository pattern implementations.
**Target Framework:** .NET Framework 4.7.1  
**Assembly Output:** `BlueDoc.Data.dll`  

| Relative File Path | Type | Lines | Bytes | Purpose & Responsibility | Audit Status | Key Dependencies |
|---|---|---|---|---|---|---|
| `src\BlueDoc.Data\BlueDoc.Data.csproj` | XML | 75 | 4,115 | MSBuild project definition | Audited | System.Data.SQLite |
| `src\BlueDoc.Data\Properties\AssemblyInfo.cs` | C# | 23 | 630 | Assembly attributes | Audited | System.Reflection |
| `src\BlueDoc.Data\SqliteDatabaseManager.cs` | C# | 140 | 5,605 | Database initialization, schema DDL, WAL configuration | Audited | SQLiteConnection |
| `src\BlueDoc.Data\Repositories\SqliteDiagnosticSessionRepository.cs` | C# | 280 | 11,109 | Diagnostic session and results persistence | Audited | SQLiteCommand |
| `src\BlueDoc.Data\Repositories\SqliteConnectionHistoryRepository.cs` | C# | 111 | 4,852 | Connection history recording and querying | Audited | SQLiteCommand |
| `src\BlueDoc.Data\Repositories\SqliteDeviceCacheRepository.cs` | C# | 134 | 5,507 | Discovered peripheral caching and retrieval | Audited | SQLiteCommand |
| `src\BlueDoc.Data\Repositories\SqliteRepairHistoryRepository.cs` | C# | 107 | 4,256 | Repair execution audit log persistence | Audited | SQLiteCommand |
| `src\BlueDoc.Data\Repositories\SqliteSettingsRepository.cs` | C# | 74 | 2,487 | Key-value settings storage | Audited | SQLiteCommand |

---

### 3.6 Project: `BlueDoc.Application`
**Responsibility:** Application services, workflow coordination, connection failure diagnostic engine, connection doctor wizard, SaaS telemetry polling, and offline knowledge base.
**Target Framework:** .NET Framework 4.7.1  
**Assembly Output:** `BlueDoc.Application.dll`  

| Relative File Path | Type | Lines | Bytes | Purpose & Responsibility | Audit Status | Key Dependencies |
|---|---|---|---|---|---|---|
| `src\BlueDoc.Application\BlueDoc.Application.csproj` | XML | 85 | 4,432 | MSBuild project definition | Audited | BlueDoc.Core, Infrastructure, Bluetooth |
| `src\BlueDoc.Application\Properties\AssemblyInfo.cs` | C# | 23 | 649 | Assembly attributes | Audited | System.Reflection |
| `src\BlueDoc.Application\Services\AdapterService.cs` | C# | 37 | 1,499 | Service facade for adapter enumeration | Audited | IHardwareInspector |
| `src\BlueDoc.Application\Services\BluetoothConnectionOrchestrator.cs` | C# | 361 | 16,321 | Protocol-appropriate peripheral routing (Audio/HID/BLE) | Audited | BluetoothNative, AudioInspector |
| `src\BlueDoc.Application\Services\BluetoothStartupHealthService.cs` | C# | 185 | 7,540 | Non-destructive startup health assessment | Audited | AdapterService, ServiceInspector |
| `src\BlueDoc.Application\Services\CompatibilityService.cs` | C# | 225 | 10,293 | Operating system compatibility matrix evaluator | Audited | OsVersionInfo |
| `src\BlueDoc.Application\Services\ConnectionFailureDiagnosticEngine.cs` | C# | 302 | 13,818 | 10-point connection failure diagnostic engine | Audited | AdapterService, ServiceInspector |
| `src\BlueDoc.Application\Services\DeviceDiscoveryService.cs` | C# | 307 | 11,248 | Discovery orchestrator combining Win32 & WinRT BLE | Audited | BluetoothManager, BleWatcher |
| `src\BlueDoc.Application\Services\DeviceManagementService.cs` | C# | 479 | 19,629 | Connection, unpairing, and native Win32/WinRT pairing | Audited | BluetoothNative, BlePairing |
| `src\BlueDoc.Application\Services\DiagnosticsService.cs` | C# | 55 | 1,762 | Service wrapper for IDiagnosticEngine | Audited | IDiagnosticEngine |
| `src\BlueDoc.Application\Services\DriverService.cs` | C# | 35 | 1,289 | Service wrapper for DriverAssistant | Audited | DriverAssistant |
| `src\BlueDoc.Application\Services\EventAnalysisService.cs` | C# | 136 | 6,548 | Service wrapper for BluetoothEventAnalyzer | Audited | BluetoothEventAnalyzer |
| `src\BlueDoc.Application\Services\FixMyConnectionWizardService.cs` | C# | 748 | 36,868 | 10-step interactive connection doctor state machine | Audited | ConnectionOrchestrator |
| `src\BlueDoc.Application\Services\HistoryService.cs` | C# | 80 | 2,998 | Coordinates connection history and device cache | Audited | Repositories |
| `src\BlueDoc.Application\Services\RepairService.cs` | C# | 47 | 1,528 | Service wrapper for SafeRepairEngine | Audited | SafeRepairEngine |
| `src\BlueDoc.Application\Services\ReportService.cs` | C# | 63 | 2,244 | Service wrapper for ReportGenerator | Audited | ReportGenerator |
| `src\BlueDoc.Application\KnowledgeBase\KnowledgeBaseArticle.cs` | C# | 22 | 872 | Offline troubleshooting article data model | Audited | None |
| `src\BlueDoc.Application\KnowledgeBase\OfflineKnowledgeBaseService.cs` | C# | 284 | 19,687 | Curated database of hardware failure articles | Audited | KnowledgeBaseArticle |
| `src\BlueDoc.Application\Cloud\FleetModels.cs` | C# | 46 | 1,693 | Fleet telemetry record and summary metrics | Audited | None |
| `src\BlueDoc.Application\Cloud\FleetTelemetryPayload.cs` | C# | 45 | 1,618 | JSON payload container for SaaS ingestion | Audited | None |
| `src\BlueDoc.Application\Cloud\ISaasTelemetryService.cs` | C# | 21 | 717 | Contract for fleet telemetry transport | Audited | FleetTelemetryPayload |
| `src\BlueDoc.Application\Cloud\SaasTelemetryService.cs` | C# | 215 | 9,089 | HTTP client transport with Bearer auth | Audited | HttpWebRequest |
| `src\BlueDoc.Application\Cloud\SaasTelemetryPollerService.cs` | C# | 137 | 4,426 | Background timer for periodic telemetry push | Audited | System.Threading.Timer |
| `src\BlueDoc.Application\Cloud\MockSaasCloudApiGateway.cs` | C# | 373 | 15,450 | Embedded HTTP listener gateway simulating SaaS cloud | Audited | HttpListener |

---

### 3.7 Project: `BlueDoc.UI`
**Responsibility:** Presentation layer, WPF application entry point, CLI dispatch, MainViewModel, 19 ViewModels, 19 Views, XAML resources, styles, converters, notification service, motion manager, and system tray guard.
**Target Framework:** .NET Framework 4.7.1  
**Assembly Output:** `BlueDoc.exe`  

| Relative File Path | Type | Lines | Bytes | Purpose & Responsibility | Audit Status | Key Dependencies |
|---|---|---|---|---|---|---|
| `src\BlueDoc.UI\BlueDoc.UI.csproj` | XML | 331 | 18,345 | MSBuild project definition | Audited | WPF, PresentationFramework |
| `src\BlueDoc.UI\App.config` | XML | 12 | 440 | Runtime configuration & supportedRuntime | Audited | None |
| `src\BlueDoc.UI\app.manifest` | XML | 29 | 1,184 | UAC execution level, OS compatibility, DPI | Audited | None |
| `src\BlueDoc.UI\BlueDoc.ico` | Binary | — | 40,271 | Multi-resolution application icon | Audited | 16x16 to 256x256 RGBA |
| `src\BlueDoc.UI\Properties\AssemblyInfo.cs` | C# | 25 | 762 | Assembly attributes and theme info | Audited | System.Reflection |
| `src\BlueDoc.UI\App.xaml` | XAML | 24 | 1,389 | Application definition, merged dictionaries & converters | Audited | Converters |
| `src\BlueDoc.UI\App.xaml.cs` | C# | 61 | 2,307 | Application startup, CLI intercept, exception handlers | Audited | CliRunner, MainWindow |
| `src\BlueDoc.UI\MainWindow.xaml` | XAML | 846 | 56,181 | Shell window, sidebar, header, telemetry bar, modals | Audited | MainViewModel |
| `src\BlueDoc.UI\MainWindow.xaml.cs` | C# | 43 | 1,039 | Code-behind: loaded event, tray initialization | Audited | TrayGuardManager |
| `src\BlueDoc.UI\Cli\CliRunner.cs` | C# | 347 | 14,760 | Command line interface engine (`AttachConsole`) | Audited | Console APIs |
| `src\BlueDoc.UI\Common\ViewModelBase.cs` | C# | 25 | 777 | Base class implementing `INotifyPropertyChanged` | Audited | PropertyChangedEventHandler |
| `src\BlueDoc.UI\Common\RelayCommand.cs` | C# | 38 | 1,097 | Standard ICommand implementation | Audited | CommandManager |
| `src\BlueDoc.UI\Common\Converters.cs` | C# | 277 | 11,455 | Value converters (StatusToBrush, BoolToVis, etc.) | Audited | IValueConverter |
| `src\BlueDoc.UI\Common\ThemeManager.cs` | C# | 169 | 8,169 | Dynamic theme switcher (Dark / Light) | Audited | Application.Current.Resources |
| `src\BlueDoc.UI\Common\MotionManager.cs` | C# | 88 | 3,872 | Reduced motion and hardware rendering manager | Audited | RenderCapability |
| `src\BlueDoc.UI\Common\NotificationService.cs` | C# | 192 | 7,188 | In-app toast notification service & history | Audited | Dispatcher |
| `src\BlueDoc.UI\Tray\TrayGuardManager.cs` | C# | 186 | 7,090 | System tray icon, context menu, and health polling | Audited | NotifyIcon |
| `src\BlueDoc.UI\Styles\Colors.xaml` | XAML | 143 | 8,073 | Color palette and semantic brush tokens | Audited | SolidColorBrush |
| `src\BlueDoc.UI\Styles\Typography.xaml` | XAML | 96 | 8,752 | Typography hierarchy and text styles | Audited | Segoe UI |
| `src\BlueDoc.UI\Styles\Spacing.xaml` | XAML | 58 | 3,604 | Layout dimensions and margin tokens | Audited | Thickness |
| `src\BlueDoc.UI\Styles\Buttons.xaml` | XAML | 276 | 21,162 | Button control templates and triggers | Audited | ControlTemplate |
| `src\BlueDoc.UI\Styles\Cards.xaml` | XAML | 118 | 9,866 | Surface container cards and border styles | Audited | Border |
| `src\BlueDoc.UI\Styles\Controls.xaml` | XAML | 304 | 21,859 | TextBox, ComboBox, ScrollViewer, ProgressBar styles | Audited | ControlTemplate |
| `src\BlueDoc.UI\Styles\Icons.xaml` | XAML | 108 | 7,692 | Vector path geometries and icon templates | Audited | StreamGeometry |
| `src\BlueDoc.UI\Styles\Loading.xaml` | XAML | 473 | 35,448 | Skeleton loading templates and spinner controls | Audited | Storyboard |
| `src\BlueDoc.UI\Styles\Navigation.xaml` | XAML | 96 | 7,516 | Sidebar navigation item templates and pills | Audited | RadioButton |
| `src\BlueDoc.UI\Styles\Animations.xaml` | XAML | 86 | 5,846 | Storyboard fade, scale, and pulse animations | Audited | DoubleAnimation |
| `src\BlueDoc.UI\Styles\Styles.xaml` | XAML | 42 | 3,001 | Root merged dictionary referencing all styles | Audited | MergedDictionaries |
| `src\BlueDoc.UI\ViewModels\MainViewModel.cs` | C# | 452 | 19,539 | Root ViewModel managing 19 sub-VMs and navigation | Audited | ViewModelBase |
| `src\BlueDoc.UI\ViewModels\DashboardViewModel.cs` | C# | 229 | 9,498 | Overview dashboard ViewModel | Audited | AdapterService |
| `src\BlueDoc.UI\ViewModels\BluetoothMainViewModel.cs` | C# | 266 | 12,736 | Quick actions & adapter state ViewModel | Audited | DeviceManagementService |
| `src\BlueDoc.UI\ViewModels\BluetoothDevicesViewModel.cs` | C# | 556 | 25,552 | Paired devices and modal pairing ViewModel | Audited | DeviceManagementService |
| `src\BlueDoc.UI\ViewModels\DeviceScannerViewModel.cs` | C# | 698 | 31,264 | Live device scanner and in-app pairing ViewModel | Audited | DeviceDiscoveryService |
| `src\BlueDoc.UI\ViewModels\HardwareInspectorViewModel.cs` | C# | 93 | 4,033 | SetupAPI hardware inspector ViewModel | Audited | AdapterService |
| `src\BlueDoc.UI\ViewModels\DiagnosticCenterViewModel.cs` | C# | 277 | 12,160 | Diagnostic suite runner ViewModel | Audited | DiagnosticsService |
| `src\BlueDoc.UI\ViewModels\AudioDiagnosticsViewModel.cs` | C# | 150 | 6,045 | Audio subsystem and packet trace ViewModel | Audited | AudioInspector |
| `src\BlueDoc.UI\ViewModels\BluetoothEventAnalyzerViewModel.cs` | C# | 163 | 7,120 | Event log query and filtering ViewModel | Audited | EventAnalysisService |
| `src\BlueDoc.UI\ViewModels\DriverAssistantViewModel.cs` | C# | 118 | 5,110 | Driver health and vendor link ViewModel | Audited | DriverService |
| `src\BlueDoc.UI\ViewModels\CompatibilityViewModel.cs` | C# | 88 | 4,120 | OS capability matrix ViewModel | Audited | CompatibilityService |
| `src\BlueDoc.UI\ViewModels\SafeRepairCenterViewModel.cs` | C# | 257 | 10,690 | Safe repair action executor ViewModel | Audited | RepairService |
| `src\BlueDoc.UI\ViewModels\DiagnosticReportViewModel.cs` | C# | 217 | 9,183 | Report compiler and export ViewModel | Audited | ReportService |
| `src\BlueDoc.UI\ViewModels\ConnectionHistoryViewModel.cs` | C# | 162 | 7,289 | Historical connection logs ViewModel | Audited | HistoryService |
| `src\BlueDoc.UI\ViewModels\SignalMonitorViewModel.cs` | C# | 249 | 10,927 | Signal RSSI monitoring ViewModel | Audited | DeviceDiscoveryService |
| `src\BlueDoc.UI\ViewModels\BleExplorerViewModel.cs` | C# | 343 | 14,633 | BLE GATT explorer ViewModel | Audited | GattProfileCatalog |
| `src\BlueDoc.UI\ViewModels\SettingsViewModel.cs` | C# | 321 | 14,607 | Application settings and theme ViewModel | Audited | SettingsRepository |
| `src\BlueDoc.UI\ViewModels\TechnicianToolkitViewModel.cs` | C# | 215 | 9,865 | Benchmarking and knowledge base ViewModel | Audited | BenchmarkEngine, KB |
| `src\BlueDoc.UI\ViewModels\SaasFleetDashboardViewModel.cs` | C# | 240 | 10,478 | SaaS fleet dashboard ViewModel | Audited | MockSaasGateway |
| `src\BlueDoc.UI\ViewModels\FixMyConnectionWizardViewModel.cs` | C# | 450 | 21,495 | 10-step connection doctor wizard ViewModel | Audited | WizardService |
| `src\BlueDoc.UI\Views\DashboardView.xaml` | XAML | 324 | 22,320 | Dashboard view markup | Audited | DashboardViewModel |
| `src\BlueDoc.UI\Views\DashboardView.xaml.cs` | C# | 11 | 215 | Code-behind: UserControl | Audited | None |
| `src\BlueDoc.UI\Views\BluetoothMainView.xaml` | XAML | 343 | 24,126 | Bluetooth main view markup | Audited | BluetoothMainViewModel |
| `src\BlueDoc.UI\Views\BluetoothMainView.xaml.cs` | C# | 11 | 223 | Code-behind: UserControl | Audited | None |
| `src\BlueDoc.UI\Views\BluetoothDevicesView.xaml` | XAML | 761 | 64,489 | Bluetooth devices view markup | Audited | BluetoothDevicesViewModel |
| `src\BlueDoc.UI\Views\BluetoothDevicesView.xaml.cs` | C# | 11 | 229 | Code-behind: UserControl | Audited | None |
| `src\BlueDoc.UI\Views\DeviceScannerView.xaml` | XAML | 702 | 58,100 | Device scanner view markup | Audited | DeviceScannerViewModel |
| `src\BlueDoc.UI\Views\DeviceScannerView.xaml.cs` | C# | 11 | 223 | Code-behind: UserControl | Audited | None |
| `src\BlueDoc.UI\Views\HardwareInspectorView.xaml` | XAML | 338 | 24,640 | Hardware inspector view markup | Audited | HardwareInspectorViewModel |
| `src\BlueDoc.UI\Views\HardwareInspectorView.xaml.cs` | C# | 11 | 231 | Code-behind: UserControl | Audited | None |
| `src\BlueDoc.UI\Views\DiagnosticCenterView.xaml` | XAML | 255 | 18,370 | Diagnostic center view markup | Audited | DiagnosticCenterViewModel |
| `src\BlueDoc.UI\Views\DiagnosticCenterView.xaml.cs` | C# | 11 | 229 | Code-behind: UserControl | Audited | None |
| `src\BlueDoc.UI\Views\AudioDiagnosticsView.xaml` | XAML | 277 | 22,208 | Audio diagnostics view markup | Audited | AudioDiagnosticsViewModel |
| `src\BlueDoc.UI\Views\AudioDiagnosticsView.xaml.cs` | C# | 11 | 229 | Code-behind: UserControl | Audited | None |
| `src\BlueDoc.UI\Views\BluetoothEventAnalyzerView.xaml` | XAML | 258 | 19,011 | Event analyzer view markup | Audited | BluetoothEventAnalyzerViewModel |
| `src\BlueDoc.UI\Views\BluetoothEventAnalyzerView.xaml.cs` | C# | 11 | 240 | Code-behind: UserControl | Audited | None |
| `src\BlueDoc.UI\Views\DriverAssistantView.xaml` | XAML | 158 | 10,113 | Driver assistant view markup | Audited | DriverAssistantViewModel |
| `src\BlueDoc.UI\Views\DriverAssistantView.xaml.cs` | C# | 11 | 226 | Code-behind: UserControl | Audited | None |
| `src\BlueDoc.UI\Views\CompatibilityView.xaml` | XAML | 156 | 11,671 | Compatibility view markup | Audited | CompatibilityViewModel |
| `src\BlueDoc.UI\Views\CompatibilityView.xaml.cs` | C# | 11 | 223 | Code-behind: UserControl | Audited | None |
| `src\BlueDoc.UI\Views\SafeRepairCenterView.xaml` | XAML | 187 | 13,130 | Safe repair center view markup | Audited | SafeRepairCenterViewModel |
| `src\BlueDoc.UI\Views\SafeRepairCenterView.xaml.cs` | C# | 11 | 228 | Code-behind: UserControl | Audited | None |
| `src\BlueDoc.UI\Views\DiagnosticReportView.xaml` | XAML | 121 | 8,906 | Diagnostic report view markup | Audited | DiagnosticReportViewModel |
| `src\BlueDoc.UI\Views\DiagnosticReportView.xaml.cs` | C# | 11 | 228 | Code-behind: UserControl | Audited | None |
| `src\BlueDoc.UI\Views\ConnectionHistoryView.xaml` | XAML | 179 | 12,385 | Connection history view markup | Audited | ConnectionHistoryViewModel |
| `src\BlueDoc.UI\Views\ConnectionHistoryView.xaml.cs` | C# | 11 | 230 | Code-behind: UserControl | Audited | None |
| `src\BlueDoc.UI\Views\SignalMonitorView.xaml` | XAML | 204 | 16,638 | Signal monitor view markup | Audited | SignalMonitorViewModel |
| `src\BlueDoc.UI\Views\SignalMonitorView.xaml.cs` | C# | 11 | 222 | Code-behind: UserControl | Audited | None |
| `src\BlueDoc.UI\Views\BleExplorerView.xaml` | XAML | 221 | 15,531 | BLE explorer view markup | Audited | BleExplorerViewModel |
| `src\BlueDoc.UI\Views\BleExplorerView.xaml.cs` | C# | 11 | 218 | Code-behind: UserControl | Audited | None |
| `src\BlueDoc.UI\Views\SettingsView.xaml` | XAML | 329 | 26,075 | Settings view markup | Audited | SettingsViewModel |
| `src\BlueDoc.UI\Views\SettingsView.xaml.cs` | C# | 11 | 212 | Code-behind: UserControl | Audited | None |
| `src\BlueDoc.UI\Views\TechnicianToolkitView.xaml` | XAML | 341 | 25,533 | Technician toolkit view markup | Audited | TechnicianToolkitViewModel |
| `src\BlueDoc.UI\Views\TechnicianToolkitView.xaml.cs` | C# | 11 | 231 | Code-behind: UserControl | Audited | None |
| `src\BlueDoc.UI\Views\SaasFleetDashboardView.xaml` | XAML | 222 | 16,689 | SaaS fleet dashboard view markup | Audited | SaasFleetDashboardViewModel |
| `src\BlueDoc.UI\Views\SaasFleetDashboardView.xaml.cs` | C# | 11 | 233 | Code-behind: UserControl | Audited | None |
| `src\BlueDoc.UI\Views\FixMyConnectionWizardView.xaml` | XAML | 647 | 50,714 | Connection doctor wizard view markup | Audited | FixMyConnectionWizardViewModel |
| `src\BlueDoc.UI\Views\FixMyConnectionWizardView.xaml.cs` | C# | 11 | 239 | Code-behind: UserControl | Audited | None |
| `src\BlueDoc.UI\Views\PlaceholderView.xaml` | XAML | 26 | 2,007 | Fallback placeholder view markup | Audited | None |
| `src\BlueDoc.UI\Views\PlaceholderView.xaml.cs` | C# | 11 | 219 | Code-behind: UserControl | Audited | None |

---

### 3.8 Project: `BlueDoc.Tests`
**Responsibility:** Verification test runner, unit test suites, scenario matrix, live hardware integration, and performance benchmark harness.
**Target Framework:** .NET Framework 4.7.1  
**Assembly Output:** `BlueDoc.Tests.exe` (Console Application)  

| Relative File Path | Type | Lines | Bytes | Purpose & Responsibility | Audit Status | Key Dependencies |
|---|---|---|---|---|---|---|
| `tests\BlueDoc.Tests\BlueDoc.Tests.csproj` | XML | 97 | 5,733 | MSBuild project definition | Audited | BlueDoc assemblies |
| `tests\BlueDoc.Tests\Properties\AssemblyInfo.cs` | C# | 23 | 650 | Assembly attributes | Audited | System.Reflection |
| `tests\BlueDoc.Tests\Program.cs` | C# | 65 | 3,236 | Test harness entry point | Audited | Test suites |
| `tests\BlueDoc.Tests\Unit\HardwareIdParserTests.cs` | C# | 88 | 2,775 | Tests USB/PCI hardware ID parsing | Audited | HardwareIdParser |
| `tests\BlueDoc.Tests\Unit\DiagnosticEngineTests.cs` | C# | 196 | 10,509 | Tests diagnostic rules against synthetic contexts | Audited | DiagnosticEngine |
| `tests\BlueDoc.Tests\Unit\DeviceClassDecoderTests.cs` | C# | 85 | 3,160 | Tests Bluetooth CoD bitmask decoding | Audited | DeviceClassDecoder |
| `tests\BlueDoc.Tests\Unit\SqliteRepositoryTests.cs` | C# | 158 | 6,711 | Tests SQLite schema and repository CRUD | Audited | SqliteDatabaseManager |
| `tests\BlueDoc.Tests\Unit\EventAnalyzerTests.cs` | C# | 54 | 1,711 | Tests event interpretation heuristics | Audited | BluetoothEventAnalyzer |
| `tests\BlueDoc.Tests\Unit\DriverAssistantTests.cs` | C# | 59 | 1,846 | Tests driver vendor URL resolution | Audited | DriverAssistant |
| `tests\BlueDoc.Tests\Unit\ReportGeneratorTests.cs` | C# | 107 | 3,693 | Tests TXT, HTML, and JSON report generation | Audited | ReportGenerator |
| `tests\BlueDoc.Tests\Unit\Phase5AdvancedDiagnosticsTests.cs` | C# | 185 | 9,131 | Tests signal, GATT catalog, event correlation | Audited | Advanced diagnostics |
| `tests\BlueDoc.Tests\Unit\Phase6RepairAndReportingTests.cs` | C# | 253 | 14,041 | Tests zero-dependency PDF & safe repairs | Audited | PdfReportGenerator |
| `tests\BlueDoc.Tests\Unit\Phase7PlatformAndFailureScenarioTests.cs` | C# | 245 | 11,911 | Tests multi-manufacturer & failure scenarios | Audited | Failure simulations |
| `tests\BlueDoc.Tests\Unit\Phase8TechnicianAndSaaSIntegrationTests.cs` | C# | 208 | 10,242 | Tests benchmark scoring and offline KB | Audited | OfflineKnowledgeBase |
| `tests\BlueDoc.Tests\Unit\Phase9AudioTraceAndTrayGuardTests.cs` | C# | 179 | 8,842 | Tests audio inspection & tray guard logic | Audited | AudioInspector |
| `tests\BlueDoc.Tests\Unit\Phase10SaasFleetAndApiGatewayTests.cs` | C# | 159 | 8,366 | Tests mock SaaS gateway & loopback HTTP | Audited | MockSaasGateway |
| `tests\BlueDoc.Tests\Unit\DeviceScannerAndPairingTests.cs` | C# | 243 | 11,870 | Tests scanner categorization & pairing logic | Audited | DeviceScannerViewModel |
| `tests\BlueDoc.Tests\Unit\FixConnectionDoctorAndPairingTests.cs` | C# | 256 | 13,722 | Tests connection doctor 10-step wizard | Audited | FixMyConnectionWizard |
| `tests\BlueDoc.Tests\Unit\AccessibilityVerificationTests.cs` | C# | 131 | 5,693 | Tests contrast ratios & WCAG compliance | Audited | ThemeManager |
| `tests\BlueDoc.Tests\Performance\PerformanceBenchmarkHarness.cs` | C# | 164 | 7,201 | Benchmarks UI startup and SetupAPI latency | Audited | Stopwatch |
| `tests\BlueDoc.Tests\Integration\BluetoothFunctionalMatrixTests.cs` | C# | 370 | 17,790 | 18-scenario Bluetooth hardware test matrix | Audited | Scenario harness |
| `tests\BlueDoc.Tests\Integration\RealHardwareInspectionTests.cs` | C# | 78 | 3,331 | Live machine SetupAPI adapter verification | Audited | Live Windows APIs |
| `tests\BlueDoc.Tests\Integration\RealDeviceDiscoveryTests.cs` | C# | 57 | 2,227 | Live machine paired device verification | Audited | Live Windows APIs |

---

## 4. Root Documentation, Scripts, and Packaging Inventory

| Relative File Path | Type | Lines | Bytes | Purpose & Responsibility | Audit Status |
|---|---|---|---|---|---|
| `Doc\prd.md` | MD | 987 | 28,517 | Product Requirements Document (PRD v1.0) | Audited |
| `Doc\design.md` | MD | 1,124 | 27,834 | Software Design Document (SDD v1.0) | Audited |
| `README.md` | MD | 260 | 13,647 | Product readme and user documentation | Audited |
| `LICENSE` | Text | 39 | 2,251 | UNLEFT LLC Commercial / Proprietary Software License | Audited |
| `installer\bluedoc_setup.iss` | Inno | 118 | 4,780 | Inno Setup 6+ Windows installation script | Audited |
| `build_release.ps1` | PS1 | 170 | 7,571 | Release compilation, test verification, portable zip & setup build | Audited |
| `generate_icon.ps1` | PS1 | 96 | 4,387 | Generates multi-layer RGBA Windows `.ico` file | Audited |
| `render_screenshots.ps1` | PS1 | 187 | 8,347 | Headless STA script capturing screenshots of all 18 views | Audited |
| `test_all_views.ps1` | PS1 | 112 | 3,956 | STA test verifying instantiation and navigation across all views | Audited |
| `test_animations_and_loading.ps1` | PS1 | 189 | 9,418 | Acceptance tests for animations and skeleton loading templates | Audited |
| `test_bluetooth_devices.ps1` | PS1 | 94 | 5,188 | Live hardware verification for Bluetooth Devices view | Audited |
| `test_device_scanner.ps1` | PS1 | 125 | 7,033 | Acceptance tests for Device Scanner redesign & filtering | Audited |
| `verify_resolutions.ps1` | PS1 | 117 | 5,203 | Multi-resolution layout verification (720p, 768p, 900p, 1080p) | Audited |
| `tools\nuget.exe` | Binary | — | 8,772,472 | Standalone NuGet CLI executable | Audited |

---

## 5. Excluded Files & Directories Analysis

In accordance with Audit Execution Rule 1, no files were silently skipped. The following categories were excluded from manual line-by-line implementation analysis:

| Directory / Pattern | File Count | Rationale for Exclusion |
|---|---|---|
| `**\bin\**` | ~100 | Generated compiled output binaries (`.dll`, `.pdb`, `.exe`). Audited via build verification. |
| `**\obj\**` | ~80 | Generated MSBuild intermediate compilation artifacts and BAML/g.cs files. |
| `.git\**` | ~300 | Git version control repository database, object packfiles, index, and commit history. |
| `packages\**\*.nupkg`, `*.p7s` | 6 | NuGet package zip containers and digital signatures. The unpacked `.dll` files are audited. |
| `dist\**` | ~20 | Staged distribution output folders created by `build_release.ps1`. |

