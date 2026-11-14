# 03. Architecture & Dependency Audit — BlueDoc

**Product:** BlueDoc  
**Company:** UNLEFT LLC  
**Lead Developer:** Golam Morshed Eashan  
**Audit Date:** November 14, 2026  
**Auditor:** Senior Forensic Audit Consortium  

---

## 1. High-Level Architecture Overview

BlueDoc follows a modular 6-tier architecture designed to decouple the presentation layer from the underlying operating system and hardware APIs.

```mermaid
flowchart TD
    subgraph UI ["Presentation Layer (BlueDoc.UI)"]
        MW["MainWindow & Navigation"]
        VMs["19 Subsystem ViewModels"]
        Styles["Merged Resource Dictionaries & Themes"]
        Tray["TrayGuard & Notifications"]
    end

    subgraph APP ["Application Layer (BlueDoc.Application)"]
        Services["Application Services (Adapter, Device, Diagnostic, Repair, Report)"]
        Orch["BluetoothConnectionOrchestrator"]
        Wiz["FixMyConnectionWizardService (10-Step Doctor)"]
        KB["OfflineKnowledgeBaseService"]
        Cloud["SaaS Telemetry & Loopback Gateway"]
    end

    subgraph DIAG ["Diagnostics Engine (BlueDoc.Diagnostics)"]
        DE["DiagnosticEngine (Orchestrator)"]
        Rules["12 Deterministic Rules"]
        Bench["BluetoothBenchmarkEngine"]
    end

    subgraph BTH ["Bluetooth Abstraction Layer (BlueDoc.Bluetooth)"]
        BM["BluetoothManager Facade"]
        W32["Win32BluetoothRadioProvider (Classic/PnP)"]
        WinRT["WinRT BLE Watcher & Custom Pairing"]
        GattCat["GattProfileCatalog"]
    end

    subgraph INFRA ["System Integration Layer (BlueDoc.Infrastructure)"]
        SetupAPI["SetupApiHardwareInspector (PnP Tree)"]
        RadioMgr["HardwareRadioManager (DevNode Power)"]
        Audio["BluetoothAudioInspector (WMI / HFP Contention)"]
        Repairs["SafeRepairEngine (8 Actions + Verification)"]
        Reports["PdfReportGenerator (PDF 1.4) & Multi-Format"]
        Events["BluetoothEventAnalyzer (EventLogReader)"]
        DriverAsst["DriverAssistant & Vendor Catalog"]
        Trace["HciTraceAssistant (netsh trace)"]
    end

    subgraph DATA ["Storage & Persistence Layer (BlueDoc.Data)"]
        SQLiteMgr["SqliteDatabaseManager (WAL Mode)"]
        Repos["5 Repositories (Sessions, History, Cache, Repairs, Settings)"]
    end

    subgraph CORE ["Domain Core (BlueDoc.Core)"]
        Models["Domain Models (Adapter, Device, Session, Event)"]
        Enums["Domain Enums (Status, Stage, Platform)"]
        Interfaces["Repository & Service Contracts"]
    end

    UI --> APP
    APP --> DIAG
    APP --> BTH
    APP --> INFRA
    APP --> DATA
    DIAG --> CORE
    BTH --> CORE
    INFRA --> CORE
    DATA --> CORE
    DIAG --> INFRA
    BTH --> INFRA
```

---

## 2. Project Dependency Graph & Assembly Analysis

```mermaid
graph TD
    UI["BlueDoc.UI (exe)"] --> APP["BlueDoc.Application"]
    UI --> DIAG["BlueDoc.Diagnostics"]
    UI --> BTH["BlueDoc.Bluetooth"]
    UI --> DATA["BlueDoc.Data"]
    UI --> INFRA["BlueDoc.Infrastructure"]
    UI --> CORE["BlueDoc.Core"]

    APP --> DIAG
    APP --> BTH
    APP --> INFRA
    APP --> DATA
    APP --> CORE

    DIAG --> BTH
    DIAG --> INFRA
    DIAG --> CORE

    BTH --> INFRA
    BTH --> CORE

    DATA --> CORE

    INFRA --> CORE

    TESTS["BlueDoc.Tests (exe)"] -.-> UI
    TESTS -.-> APP
    TESTS -.-> DIAG
    TESTS -.-> BTH
    TESTS -.-> INFRA
    TESTS -.-> DATA
    TESTS -.-> CORE
```

### Dependency Findings & Clean Architecture Adherence
1. **Strict Core Layer Isolation:** `BlueDoc.Core` has ZERO project references and depends only on system core libraries. No circular dependencies exist.
2. **Layer Inversion Violation in UI:** `BlueDoc.UI` directly references all projects including `BlueDoc.Infrastructure` and `BlueDoc.Bluetooth`. While common in traditional WPF applications, it bypasses the application layer facade.
3. **WinRT Dependency Coupling:** `BlueDoc.Bluetooth` directly references `Windows.winmd` from Windows Kits 10, creating a binary hazard on Windows 7 SP1.

---

## 3. Application Startup Sequence

```mermaid
sequenceDiagram
    autonumber
    actor User
    participant App as App.xaml.cs
    participant Cli as CliRunner
    participant MW as MainWindow
    participant MainVM as MainViewModel
    participant DB as SqliteDatabaseManager
    participant Startup as BluetoothStartupHealthService
    participant Tray as TrayGuardManager

    User->>App: Launch BlueDoc.exe [args]
    App->>App: Register DispatcherUnhandledException
    alt Is CLI Invocation (e.g. -diagnose, -inspect)
        App->>Cli: RunAsync(args)
        Cli-->>User: Console Output
        Cli->>App: Exit(code)
    else GUI Invocation
        App->>App: Check RenderCapability.Tier (Software fallback if Tier 0)
        App->>MW: new MainWindow()
        MW->>MainVM: Initialize DataContext
        MainVM->>DB: InitializeDatabase()
        MainVM->>MainVM: Instantiate 19 ViewModels & Services
        MW->>MW: Show()
        MW->>MainVM: InitializeAsync() (on Loaded)
        MainVM->>Startup: InspectSystemHealthAsync()
        MainVM->>MainVM: Load PrimaryAdapter & Dashboard Data
        MW->>Tray: Initialize(mainWindow)
        Tray->>Tray: StartMonitoring() (10s timer)
    end
```

---

## 4. Navigation Architecture & ViewModel Lifecycles

### 4.1 Navigation Pattern
BlueDoc uses ViewModel-based navigation. `MainWindow.xaml` binds its center `ContentControl` to `MainViewModel.CurrentViewModel`. `DataTemplates` map each ViewModel type to its corresponding `UserControl` view:

```xml
<DataTemplate DataType="{x:Type vm:DashboardViewModel}">
    <views:DashboardView />
</DataTemplate>
```

### 4.2 ViewModel Lifetime & Memory Management
- **All 19 ViewModels are eagerly instantiated** in `MainViewModel` constructor.
- ViewModels are kept in memory for the lifetime of the application.
- **Benefits:** Instant page transitions (zero XAML recompilation or instantiation lag); state preservation across section switches.
- **Drawbacks:** Memory consumption is higher upfront (~60 MB working set); initial startup performs multiple service initializations.

```mermaid
flowchart LR
    subgraph Navigation ["MainViewModel.NavigateTo(section)"]
        Nav["Section Name Received"] --> Group["Set ActiveGroup"]
        Group --> Switch{"Section Switch"}
        Switch -->|Dashboard| DVM["CurrentViewModel = DashboardVM<br/>_ = LoadDataAsync()"]
        Switch -->|Devices| BVM["CurrentViewModel = BluetoothDevicesVM<br/>_ = LoadPairedDevicesAsync()"]
        Switch -->|Scanner| SVM["CurrentViewModel = DeviceScannerVM"]
        Switch -->|Hardware| HVM["CurrentViewModel = HardwareInspectorVM<br/>_ = LoadAdaptersAsync()"]
        Switch -->|Diagnostics| DCVM["CurrentViewModel = DiagnosticCenterVM"]
        Switch -->|Wizard| WVM["CurrentViewModel = FixMyConnectionWizardVM<br/>_ = LoadCandidatesAsync()"]
    end
```

---

## 5. Bluetooth Operation Flow (Discovery, Pairing, Connection)

```mermaid
sequenceDiagram
    autonumber
    actor User
    participant ScannerVM as DeviceScannerViewModel
    participant DiscSvc as DeviceDiscoveryService
    participant W32 as Win32BluetoothRadioProvider
    participant WinRT as BleAdvertisementWatcherProvider
    participant Mgmt as DeviceManagementService
    participant Orch as BluetoothConnectionOrchestrator
    participant Native as Win32 bthprops / WinRT BLE

    User->>ScannerVM: Click "Start Scan"
    ScannerVM->>DiscSvc: StartScanningAsync(duration, mode)
    par Classic Discovery
        DiscSvc->>W32: DiscoverDevicesAsync(timeout)
        W32->>Native: BluetoothFindFirstDevice(fIssueInquiry=true)
        Native-->>W32: Classic Devices Found
        W32-->>DiscSvc: Report Device
    and BLE Advertisement
        DiscSvc->>WinRT: StartScanning()
        WinRT->>Native: BluetoothLEAdvertisementWatcher.Start()
        Native-->>WinRT: AdvertisementReceived(MAC, RSSI, Name)
        WinRT-->>DiscSvc: Report BLE Device
    end
    DiscSvc-->>ScannerVM: Update DiscoveredDevices (Deduplicated)
    User->>ScannerVM: Click "Pair Device"
    alt Classic Peripheral
        ScannerVM->>Mgmt: PairDeviceAsync(dev, pin)
        Mgmt->>Native: BluetoothAuthenticateDevice(hRadio, devInfo, pin)
        Native-->>Mgmt: Result Code (0=Success, 1244=Rejected)
    else BLE Peripheral
        ScannerVM->>Mgmt: PairDeviceAsync(dev, pin)
        Mgmt->>Native: WinRT CustomPairing.PairAsync()
        Native-->>Mgmt: DevicePairingResultStatus
    end
    Mgmt-->>ScannerVM: Pairing Outcome
    User->>ScannerVM: Click "Connect"
    ScannerVM->>Mgmt: ConnectDeviceAsync(dev)
    Mgmt->>Native: BluetoothSetServiceState(ENABLE Audio/HID/Serial)
    Native-->>Mgmt: ERROR_SUCCESS
    Mgmt-->>ScannerVM: Connected = true
```

---

## 6. Diagnostic Execution Flow

```mermaid
sequenceDiagram
    autonumber
    participant UI as DiagnosticCenterViewModel
    participant Svc as DiagnosticsService
    participant Engine as DiagnosticEngine
    participant Context as DiagnosticContext
    participant Rule as IDiagnosticRule (12 Rules)
    participant Repo as SqliteDiagnosticSessionRepository

    UI->>Svc: RunFullDiagnosticsAsync()
    Svc->>Engine: RunFullDiagnosticsAsync(progress, cts)
    Engine->>Context: BuildContextAsync()
    activate Context
    Context->>Context: SetupApi.EnumerateAdapters()
    Context->>Context: ServiceController.GetAllServices()
    Context->>Context: BluetoothManager.IsRadioEnabledAsync()
    Context->>Context: EventAnalyzer.QueryEvents(30, 7)
    deactivate Context
    
    loop For Each of 12 Rules
        Engine->>Rule: EvaluateAsync(context, token)
        Rule->>Rule: Inspect context evidence & evaluate status
        Rule-->>Engine: DiagnosticResult (Status, Evidence, RecAction)
        Engine->>Context: Append to PreviousResults
        Engine-->>UI: progress.Report(result)
        UI->>UI: Update individual result row & test counts
    end
    
    Engine->>Repo: SaveSessionAsync(session)
    Engine-->>Svc: DiagnosticSession (Completed)
    Svc-->>UI: Session Completed
```

---

## 7. Safe Repair Execution Flow

```mermaid
sequenceDiagram
    autonumber
    actor User
    participant VM as SafeRepairCenterViewModel
    participant Svc as RepairService
    participant Engine as SafeRepairEngine
    participant OS as Windows SCM / CfgMgr32 / Process
    participant Repo as SqliteRepairHistoryRepository

    User->>VM: Click "Execute Repair" (e.g. RestartBthServ)
    VM->>VM: Display Confirmation Modal (Issue, Action, Permissions, Consequences)
    User->>VM: Confirm Action
    VM->>Svc: ExecuteRepairAsync(repairKey)
    Svc->>Engine: ExecuteRepairAsync(repairKey)
    alt Requires Admin & Not Elevated
        Engine-->>VM: Failed (Permission Denied)
        VM->>User: Show "Restart as Administrator" Prompt
    else Allowed
        Engine->>OS: Execute Repair Action (e.g. Stop -> Start bthserv)
        Engine->>Engine: VerifyServiceRunning("bthserv") (Independent Check)
        Engine->>Repo: RecordRepairAsync(record)
        Engine-->>Svc: RepairHistoryRecord (Verified Status)
        Svc-->>VM: Repair Completed
        VM->>VM: Update History Table & Display Toast
    end
```

---

## 8. Data Architecture & Storage Flow

```mermaid
flowchart TD
    subgraph LocalStorage ["Local SQLite Database (%LOCALAPPDATA%\UNLEFT\BlueDoc\bluedoc.db)"]
        PRAGMA["PRAGMA journal_mode = WAL;<br/>PRAGMA foreign_keys = ON;"]
        T1["DiagnosticSessions"]
        T2["DiagnosticResults (FK: SessionId)"]
        T3["DiscoveredDevices (Cache)"]
        T4["ConnectionHistory"]
        T5["RepairHistory"]
        T6["ApplicationSettings"]
    end

    subgraph Repositories ["Repository Implementations (BlueDoc.Data.Repositories)"]
        R1["SqliteDiagnosticSessionRepository"]
        R2["SqliteDeviceCacheRepository"]
        R3["SqliteConnectionHistoryRepository"]
        R4["SqliteRepairHistoryRepository"]
        R5["SqliteSettingsRepository"]
    end

    R1 --> T1
    R1 --> T2
    R2 --> T3
    R3 --> T4
    R4 --> T5
    R5 --> T6
```

---

## 9. Architectural Risks & Code Smells

| Finding ID | Component | Architectural Risk / Code Smell | Severity | Recommendation |
|---|---|---|---|---|
| **BD-CRIT-002** | `BlueDoc.Bluetooth` | Direct compilation reference to `Windows.winmd` without dynamic isolation. | Critical | Separate WinRT BLE code into a standalone satellite assembly or use dynamic WinRT activation to ensure Windows 7 SP1 runtime safety. |
| **BD-MED-001** | `MainViewModel` | Tight coupling with concrete `new` instantiations for all 19 ViewModels. | Medium | Introduce an `IServiceProvider` / DI container (e.g., `Microsoft.Extensions.DependencyInjection`). |
| **BD-LOW-002** | `MainViewModel` | Fire-and-forget `_ = Task` invocations in navigation switch. | Low | Wrap navigation data loading tasks in safe error-capturing handlers. |
| **BD-MED-003** | `TrayGuardManager` | Continuous SetupAPI device enumeration polling every 10 seconds. | Medium | Replace timer polling with Win32 `RegisterDeviceNotification` (`WM_DEVICECHANGE`). |

