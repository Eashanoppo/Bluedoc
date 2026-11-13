# BlueDoc — Software Design Document (SDD)

**Product:** BlueDoc  
**Company:** UNLEFT LLC  
**Developer:** Golam Morshed Eashan  
**Document Version:** 1.0  
**Status:** Initial Design  
**Target Platforms:** Windows 7 SP1, Windows 10, Windows 11  
**Based on:** BlueDoc Product Requirements Document (PRD)

---

# 1. Introduction

## 1.1 Purpose

This Software Design Document defines the technical architecture, components, interfaces, data structures, workflows, and implementation strategy for BlueDoc.

BlueDoc is a Windows desktop application designed to detect, inspect, connect, diagnose, and troubleshoot Bluetooth hardware and devices.

The application will particularly focus on helping users resolve Bluetooth problems on older laptops and computers.

## 1.2 Product Vision

BlueDoc aims to become a comprehensive Bluetooth diagnostic and management utility for Windows.

Its mission:

**Detect the problem. Understand the cause. Guide the repair.**

## 1.3 Main Goals

- Detect available Bluetooth adapters.
- Identify hardware manufacturers, chipsets, and hardware IDs where available.
- Inspect Bluetooth drivers and services.
- Discover nearby Bluetooth devices.
- Help users pair and manage devices.
- Diagnose common Bluetooth failures.
- Analyze relevant Windows event logs.
- Provide safe, guided repair procedures.
- Generate diagnostic reports.

---

# 2. Technology Stack

## 2.1 Recommended Technologies

| Component | Technology |
|---|---|
| Programming language | C# |
| Desktop UI | WPF |
| Development environment | Visual Studio |
| Initial framework | .NET Framework 4.8 |
| Bluetooth integration | Windows Bluetooth APIs |
| Hardware detection | SetupAPI / Configuration Manager |
| Windows services | Service Control Manager APIs |
| Event analysis | Windows Event Log APIs |
| Local database | SQLite |
| Configuration | JSON |
| Logging | Structured application logging |
| Installer | Windows installer package |

## 2.2 Platform Compatibility Strategy

BlueDoc must account for differences between Windows versions.

**Windows 7 SP1**
- Use compatible Classic Bluetooth APIs.
- Use legacy Windows hardware and driver interfaces.
- Support adapter and service diagnostics.
- Clearly identify features unavailable on this platform.

**Windows 10**
- Use supported Windows Bluetooth APIs.
- Support Classic Bluetooth and compatible BLE functionality.
- Enable advanced diagnostics where APIs permit.

**Windows 11**
- Use supported modern Windows APIs.
- Provide the fullest available Bluetooth and BLE functionality.

The application must detect the operating system and select compatible implementations.

Important: Windows 7 SP1 support requires testing on actual Windows 7 systems. Modern .NET and Windows Runtime APIs must not be assumed to work on every target OS.

---

# 3. System Architecture

BlueDoc will use a layered architecture.

## 3.1 Architecture Overview

The application will contain six primary layers:

1. Presentation Layer
2. Application Layer
3. Bluetooth Abstraction Layer
4. Diagnostics Engine
5. System Integration Layer
6. Data and Storage Layer

### Architecture Diagram

```text
+------------------------------------------+
|              PRESENTATION                |
| Dashboard | Devices | Diagnostics        |
| Drivers | Reports | Settings             |
+------------------------------------------+
                    |
                    v
+------------------------------------------+
|             APPLICATION                  |
| Application Services                     |
| Device Management                        |
| Diagnostic Orchestration                 |
| Repair Coordination                      |
+------------------------------------------+
                    |
          +---------+---------+
          |                   |
          v                   v
+--------------------+  +-------------------+
| BLUETOOTH          |  | DIAGNOSTICS       |
| ABSTRACTION        |  | ENGINE            |
| Adapter Detection  |  | Test Execution    |
| Discovery          |  | Rules & Analysis  |
| Pairing            |  | Recommendations   |
| Device Management  |  | Result Evaluation |
| BLE Operations     |  | Event Analysis    |
+--------------------+  +-------------------+
          |                   |
          +---------+---------+
                    |
                    v
+------------------------------------------+
|           SYSTEM INTEGRATION             |
| Windows APIs | Drivers | Services        |
| Hardware | Event Logs | Permissions      |
+------------------------------------------+
                    |
                    v
+------------------------------------------+
|             DATA & STORAGE               |
| SQLite | JSON Settings | Logs | Reports  |
+------------------------------------------+
```

## 3.2 Presentation Layer

Responsible for displaying application screens and receiving user input.

Responsibilities:
- Display Bluetooth adapter information.
- Display nearby devices.
- Present diagnostic results.
- Show repair instructions.
- Display connection history.
- Generate report previews.
- Manage application settings.

Recommended pattern: **MVVM (Model-View-ViewModel)**.

This separates the interface from application logic and makes the software easier to test and maintain.

## 3.3 Application Layer

Coordinates the main application operations.

Main services:

- AdapterManagementService
- DeviceDiscoveryService
- DeviceConnectionService
- DiagnosticService
- DriverInspectionService
- EventAnalysisService
- RepairService
- ReportService
- CompatibilityService

## 3.4 Bluetooth Abstraction Layer

Provides a consistent interface for Bluetooth operations.

The application should not directly depend on one Windows Bluetooth API throughout the entire codebase.

Instead, define interfaces such as:

```csharp
public interface IBluetoothProvider
{
    Task<IReadOnlyList<BluetoothAdapter>>
        GetAdaptersAsync();

    Task<IReadOnlyList<BluetoothDevice>>
        DiscoverDevicesAsync();

    Task<bool> IsBluetoothAvailableAsync();
}
```

Implement compatible providers for the supported Windows environments.

For example:
- Legacy Bluetooth Provider
- Modern Windows Bluetooth Provider
- BLE Provider

The provider selection mechanism should check operating system and API capabilities before using a feature.

---

# 4. Application Modules

# 4.1 Dashboard

**Purpose:** Give users an overview of Bluetooth health.

Display:

- Bluetooth adapter status.
- Adapter manufacturer and name.
- Driver status.
- Bluetooth service status.
- Connected device count.
- Recent diagnostic results.
- Recent connection failures.
- Quick diagnostic button.

Possible dashboard layout:

```text
+--------------------------------------------------+
| BlueDoc                           Settings       |
+--------------------------------------------------+
|                                                  |
|  BLUETOOTH STATUS                                |
|  Adapter: Intel Wireless Bluetooth              |
|  Status: Available                               |
|  Driver: Installed                              |
|                                                  |
+--------------------------------------------------+
|                                                  |
|  [Run Full Diagnosis]     [Scan Devices]         |
|                                                  |
+--------------------------------------------------+
|  HARDWARE       DRIVER       SERVICES            |
|  Passed         Passed       Warning             |
+--------------------------------------------------+
|  Recent Devices                                  |
|  Headphones       Connected                       |
|  Mouse            Previously connected           |
+--------------------------------------------------+
```

The example is illustrative; actual information must come from the system.

# 4.2 Advanced Hardware Inspector

**Priority:** High

**Purpose:** Identify the Bluetooth hardware and its capabilities.

Information to collect:

- Adapter name.
- Manufacturer.
- Device description.
- Hardware ID.
- Vendor ID.
- Product ID.
- Driver provider.
- Bluetooth version, when reliably identifiable.
- Supported capabilities, where exposed.
- Device status.
- PnP device instance ID.
- Connection interface information, where available.

Example:

```text
Bluetooth Hardware Inspector

Adapter: Intel Wireless Bluetooth

Manufacturer: Intel
Vendor ID: 8087
Product ID: XXXX
Hardware ID: USB\VID_8087&PID_XXXX
Driver Provider: Intel
Driver Version: System detected
Device Status: Working

Bluetooth Version:
Not reliably identified

Capabilities:
Classic Bluetooth: Supported
Bluetooth LE: Unknown
```

The application must distinguish detected facts from inferred information.

For example, a hardware ID may identify a chipset family, but it does not always prove the exact Bluetooth version or capabilities.

# 4.3 Diagnostic Test Center

**Priority:** High

**Purpose:** Run individual tests or a complete diagnostic sequence.

### Individual Tests

1. Bluetooth adapter detection.
2. Hardware availability.
3. Device status.
4. Driver installation.
5. Driver information.
6. Bluetooth service status.
7. Bluetooth radio availability.
8. Device discovery.
9. Pairing readiness.
10. BLE availability, where supported.

### Result Statuses

| Status | Meaning |
|---|---|
| Passed | Test completed successfully |
| Failed | Test detected a failure |
| Warning | Potential problem detected |
| Inconclusive | Not enough evidence |
| Not Supported | Platform or hardware does not support the test |
| Not Tested | Test has not been executed |

### Full Diagnostic Workflow

```text
Start Diagnosis
      |
      v
Detect Windows Version
      |
      v
Detect Bluetooth Adapter
      |
      v
Inspect Hardware
      |
      v
Inspect Driver
      |
      v
Check Bluetooth Services
      |
      v
Check Radio Availability
      |
      v
Test Device Discovery
      |
      v
Analyze Relevant Events
      |
      v
Generate Diagnostic Results
      |
      v
Show Recommended Actions
```

Tests that require user interaction, such as pairing or checking a physical hardware switch, should be clearly identified rather than automatically reported as failures.

# 4.4 Bluetooth Event Analyzer

**Priority:** High

**Purpose:** Analyze relevant Windows event logs to identify recurring Bluetooth problems.

Responsibilities:

- Retrieve relevant Windows event records.
- Identify Bluetooth-related providers and events.
- Extract event IDs, timestamps, severity, and messages.
- Group recurring failures.
- Correlate events with diagnostic sessions.
- Suggest possible causes.

Example:

```text
Bluetooth Event Analysis

Issue: Bluetooth device connection failure

Detected Events:
- Bluetooth service error
- Device connection attempt failed
- Adapter initialization warning

Possible Causes:
- Driver instability
- Bluetooth service interruption
- Adapter communication problem

Recommendation:
Inspect the driver and Bluetooth service.
```

Event interpretation must be evidence-based. An event message alone should not be treated as proof of a defective hardware component.

# 4.5 Driver Assistant

**Priority:** High

**Purpose:** Help users understand and troubleshoot Bluetooth driver problems.

Display:

- Driver name.
- Provider.
- Version.
- Installation date.
- Device status.
- Hardware ID.
- Driver compatibility information, where available.

Actions:

- Open Windows Device Manager.
- View driver details.
- Guide users to official manufacturer websites.
- Provide rollback instructions.
- Provide reinstall instructions.

The application should never silently download or install an unverified driver.

# 4.6 Device Connection History

**Purpose:** Maintain a record of Bluetooth discovery and connection activity.

Store:

- Device name.
- Device identifier, where available.
- First discovered time.
- Last discovered time.
- Pairing status.
- Connection status.
- Connection failure information.
- Diagnostic session reference.

Users should be able to clear their history.

# 4.7 One-Click Diagnostic Report

**Purpose:** Generate a shareable report.

Report contents:

1. Operating system information.
2. Bluetooth adapter information.
3. Hardware IDs.
4. Driver information.
5. Service status.
6. Diagnostic test results.
7. Relevant event findings.
8. Possible causes.
9. Recommended solutions.

Supported formats:

- TXT
- HTML
- JSON
- PDF (planned)

The report must distinguish confirmed findings, warnings, and possible explanations.

# 4.8 Safe Repair Center

**Purpose:** Provide guided troubleshooting and approved repair actions.

Potential actions:

- Restart Bluetooth services.
- Re-enable a disabled adapter.
- Open Device Manager.
- Guide driver rollback.
- Guide driver reinstallation.
- Open relevant Windows settings.

### Repair Workflow

```text
Identify Problem
      |
      v
Explain Proposed Repair
      |
      v
Show Required Permissions
      |
      v
Ask User for Confirmation
      |
      v
Execute Approved Action
      |
      v
Verify Result
      |
      v
Save Repair Log
```

Safety requirements:

- No silent system modifications.
- Require confirmation before administrative operations.
- Explain what each repair changes.
- Record the action and its result.
- Provide rollback guidance where possible.
- Never claim a repair succeeded until verification confirms it.

# 4.9 Signal & Discovery Monitor

**Purpose:** Monitor device discovery and signal information when exposed by supported hardware and APIs.

Display:

- Scan duration.
- Number of discovered devices.
- Device visibility.
- Discovery timestamps.
- Signal strength when available.
- Discovery errors.

RSSI and signal strength are not guaranteed to be available for every Bluetooth device or Windows API. Display unavailable data honestly.

# 4.10 Bluetooth LE Explorer

**Purpose:** Explore Bluetooth Low Energy devices.

Features where supported:

- Discover BLE devices.
- Connect to a BLE device.
- Discover GATT services.
- Discover characteristics.
- Discover descriptors.
- Read permitted characteristics.
- Write supported characteristics.
- Subscribe to notifications.

Important:

BLE operations depend on operating system support, device permissions, and the characteristics exposed by the device.

Writing data to a characteristic can change device behavior. The application should clearly show the operation and require user confirmation.

# 4.11 Compatibility Checker

**Purpose:** Identify operating system and Bluetooth capability limitations.

Check:

- Windows version and build.
- Available Bluetooth APIs.
- Adapter availability.
- Classic Bluetooth support.
- BLE support.
- Driver compatibility indicators.
- Unsupported application features.

Example:

```text
Compatibility Report

Operating System: Windows 7 SP1

Classic Bluetooth: Supported
Modern BLE Explorer: Limited / API dependent
Advanced GATT Operations: Not supported
Some modern Windows features: Unavailable

Recommendation:
Use the compatible legacy Bluetooth features.
```

---

# 5. User Interface Design

## 5.1 Navigation Structure

The application will use a sidebar navigation layout.

```text
BlueDoc
|
+-- Dashboard
|
+-- Bluetooth Devices
|
+-- Device Scanner
|
+-- Advanced Hardware Inspector
|
+-- Diagnostic Test Center
|
+-- Bluetooth Event Analyzer
|
+-- Driver Assistant
|
+-- Device Connection History
|
+-- Diagnostic Reports
|
+-- Safe Repair Center
|
+-- Signal & Discovery Monitor
|
+-- Bluetooth LE Explorer
|
+-- Compatibility Checker
|
+-- Settings
```

## 5.2 UI Design Principles

- Simple and beginner-friendly.
- Modern Windows desktop appearance.
- Clear diagnostic status indicators.
- Consistent navigation.
- Avoid overwhelming users with technical details.
- Provide expandable advanced information.
- Make repair instructions easy to understand.

## 5.3 Diagnostic Status Colors

- Green: Passed
- Red: Failed
- Yellow: Warning
- Gray: Inconclusive or Not Tested
- Blue: Information

Color must not be the only way to communicate status; always include readable labels.

---

# 6. Data Architecture

## 6.1 Database

SQLite will be used for local application data.

Suggested tables:

### BluetoothAdapters

| Field | Type |
|---|---|
| Id | INTEGER |
| Name | TEXT |
| Manufacturer | TEXT |
| HardwareId | TEXT |
| VendorId | TEXT |
| ProductId | TEXT |
| DriverVersion | TEXT |
| DeviceStatus | TEXT |
| LastDetected | TEXT |

### BluetoothDevices

| Field | Type |
|---|---|
| Id | INTEGER |
| DeviceName | TEXT |
| DeviceIdentifier | TEXT |
| DeviceType | TEXT |
| Paired | INTEGER |
| LastSeen | TEXT |

### DiagnosticSessions

| Field | Type |
|---|---|
| Id | INTEGER |
| StartedAt | TEXT |
| CompletedAt | TEXT |
| OperatingSystem | TEXT |
| OverallStatus | TEXT |

### DiagnosticResults

| Field | Type |
|---|---|
| Id | INTEGER |
| SessionId | INTEGER |
| TestName | TEXT |
| Status | TEXT |
| Details | TEXT |
| Recommendation | TEXT |

### ConnectionHistory

| Field | Type |
|---|---|
| Id | INTEGER |
| DeviceIdentifier | TEXT |
| DeviceName | TEXT |
| EventType | TEXT |
| Timestamp | TEXT |
| Result | TEXT |

### RepairHistory

| Field | Type |
|---|---|
| Id | INTEGER |
| ActionName | TEXT |
| Description | TEXT |
| ExecutedAt | TEXT |
| Result | TEXT |

Sensitive identifiers and logs should be stored only when necessary and handled according to the application's privacy policy.

## 6.2 Data Storage Rules

- Store data locally by default.
- Do not upload diagnostic reports automatically.
- Allow users to delete stored history.
- Avoid collecting unrelated personal information.
- Exclude sensitive information from reports unless explicitly necessary and authorized.

---

# 7. Diagnostic Intelligence Engine

## 7.1 Purpose

The Diagnostic Engine is one of BlueDoc's most important components.

It evaluates hardware, driver, service, radio, discovery, and event information to identify possible causes of Bluetooth problems.

## 7.2 Rule-Based Diagnosis

The first version will use a rule-based diagnostic system.

Example rules:

**Rule 1: Adapter Not Detected**

Condition:
No Bluetooth adapter is detected.

Possible explanations:
- No Bluetooth hardware is installed.
- Adapter is disabled.
- Driver is missing or not loading.
- Hardware is unavailable.

Recommendation:
Check Device Manager, hardware availability, and driver status.

**Rule 2: Bluetooth Service Not Running**

Condition:
A required Bluetooth service is stopped or unavailable.

Recommendation:
Inspect the service configuration and offer a guided restart when appropriate.

**Rule 3: Driver Problem**

Condition:
Windows reports an adapter device error.

Recommendation:
Inspect driver details and guide the user through official driver troubleshooting.

**Rule 4: Discovery Failure**

Condition:
Adapter is available, but a discovery test cannot find devices.

Recommendation:
Check radio state, device discoverability, range, and supported discovery mode.

## 7.3 Diagnostic Evidence

Every diagnostic conclusion should include:

- Test name.
- Observed condition.
- Evidence.
- Confidence or certainty level.
- Possible cause.
- Recommended action.

BlueDoc should never label a hardware component as physically damaged based only on an unsuccessful software test.

---

# 8. Security and Permissions

BlueDoc must operate with the minimum permissions necessary.

## 8.1 Standard User Mode

Allow:

- Viewing adapter information.
- Viewing driver details.
- Scanning for devices where permitted.
- Viewing diagnostic history.
- Generating reports.

## 8.2 Administrator Mode

Some repair operations may require administrator privileges.

Examples:

- Restarting protected services.
- Changing adapter state.
- Certain driver-related operations.

Administrator access should be requested only when required.

## 8.3 Security Requirements

- Validate system operation parameters.
- Avoid executing arbitrary commands from external sources.
- Use trusted Windows APIs.
- Avoid downloading drivers from unofficial sources.
- Require confirmation for system modifications.
- Maintain repair logs.
- Protect local diagnostic data.

---

# 9. Error Handling and Logging

All major modules must handle errors gracefully.

Examples:

- Bluetooth adapter unavailable.
- Driver information inaccessible.
- Bluetooth service unavailable.
- Device discovery timeout.
- Pairing rejected.
- BLE operation unsupported.
- Permission denied.
- Windows API unavailable.

Every error should provide:

1. A user-friendly explanation.
2. Technical details for advanced users.
3. A recommended next step.
4. A log entry where appropriate.

---

# 10. Testing Strategy

## 10.1 Unit Testing

Test:

- Diagnostic rules.
- Hardware ID parsing.
- Compatibility checks.
- Data models.
- Report generation.
- Status evaluation.

## 10.2 Integration Testing

Test:

- Windows Bluetooth API integration.
- Hardware detection.
- Driver inspection.
- Service monitoring.
- Event log retrieval.
- SQLite operations.

## 10.3 Platform Testing

Test on actual supported environments:

- Windows 7 SP1.
- Windows 10.
- Windows 11.

## 10.4 Hardware Testing

Use multiple Bluetooth adapter types where possible:

- Intel Bluetooth adapters.
- Realtek Bluetooth adapters.
- Qualcomm Bluetooth adapters.
- USB Bluetooth dongles.
- Built-in laptop Bluetooth modules.

## 10.5 Failure Scenario Testing

Simulate or safely reproduce:

- Disabled adapter.
- Missing driver.
- Stopped Bluetooth service.
- Unavailable radio.
- Discovery timeout.
- Failed pairing.
- Disconnected device.
- Unsupported BLE operation.

---

# 11. Project Structure

Recommended Visual Studio solution structure:

```text
BlueDoc/
|
+-- BlueDoc.sln
|
+-- BlueDoc.UI/
|   +-- Views/
|   +-- ViewModels/
|   +-- Resources/
|   +-- Converters/
|
+-- BlueDoc.Application/
|   +-- Services/
|   +-- Interfaces/
|   +-- Workflows/
|
+-- BlueDoc.Core/
|   +-- Models/
|   +-- Enums/
|   +-- Exceptions/
|
+-- BlueDoc.Bluetooth/
|   +-- Abstractions/
|   +-- Legacy/
|   +-- Modern/
|   +-- BLE/
|
+-- BlueDoc.Diagnostics/
|   +-- Tests/
|   +-- Rules/
|   +-- Engine/
|   +-- Recommendations/
|
+-- BlueDoc.System/
|   +-- Hardware/
|   +-- Drivers/
|   +-- Services/
|   +-- EventLogs/
|
+-- BlueDoc.Data/
|   +-- Database/
|   +-- Repositories/
|   +-- Settings/
|
+-- BlueDoc.Reporting/
|   +-- Generators/
|   +-- Templates/
|
+-- BlueDoc.Tests/
    +-- Unit/
    +-- Integration/
```

This is the proposed logical structure. The final project and assembly boundaries should be adjusted after the Windows 7 API and framework compatibility prototypes are tested.

---

# 12. Development Roadmap

## Phase 1 — Foundation

- Create Visual Studio solution.
- Configure WPF.
- Establish MVVM architecture.
- Create the main window and navigation.
- Implement operating system detection.

## Phase 2 — Bluetooth Detection

- Detect Bluetooth adapters.
- Retrieve hardware IDs.
- Retrieve manufacturer information where available.
- Inspect driver information.
- Check Bluetooth services.

## Phase 3 — Device Management

- Discover nearby devices.
- Display device information.
- Implement supported pairing and connection workflows.
- Maintain connection history.

## Phase 4 — Diagnostic Engine

- Implement individual tests.
- Implement complete diagnostic workflow.
- Create rule-based diagnosis.
- Display recommendations.

## Phase 5 — Advanced Diagnostics

- Implement event analysis.
- Implement signal and discovery monitoring.
- Develop compatibility checking.
- Add BLE Explorer where supported.

## Phase 6 — Repair and Reporting

- Implement guided repair operations.
- Add user confirmation and verification.
- Generate TXT, HTML, and JSON reports.
- Add PDF generation.

## Phase 7 — Testing and Release

- Test Windows 7, 10, and 11.
- Test multiple hardware manufacturers.
- Test failure scenarios.
- Fix compatibility issues.
- Prepare the installer.
- Release BlueDoc v1.0.

---

# 13. Key Technical Limitations

The following limitations must be considered during development:

1. Windows does not expose every Bluetooth hardware capability through a universal API.
2. Exact chipset identification may require hardware ID databases.
3. Bluetooth version cannot always be determined reliably from the adapter name or hardware ID alone.
4. Signal strength may not be exposed for every device.
5. Some BLE functionality requires modern Windows APIs.
6. Certain repair actions require administrator privileges.
7. Windows 7 has substantially more limited support for modern Bluetooth and BLE functionality.
8. Device pairing and connection behavior depends on the Bluetooth device and its supported profiles.
9. Event logs may not contain enough evidence to identify the root cause of every failure.

BlueDoc must report these limitations honestly rather than inventing information or claiming unsupported functionality.

---

# 14. Version 1.0 Release Scope

The first release should prioritize a reliable diagnostic foundation.

**Core release features:**

- Dashboard.
- Bluetooth adapter detection.
- Advanced Hardware Inspector.
- Driver Assistant.
- Bluetooth service monitoring.
- Device Scanner.
- Diagnostic Test Center.
- Rule-based Diagnostic Engine.
- Bluetooth Event Analyzer.
- Diagnostic Report Generator.
- Basic guided repair operations.
- Compatibility Checker.

**Advanced features to follow:**

- Full device connection management.
- Signal monitoring.
- Advanced BLE Explorer.
- Expanded repair automation.
- Advanced event correlation.

The release scope should be finalized after API feasibility testing.

---

# 15. Final Design Principles

BlueDoc must follow these principles throughout development:

1. Reliability before feature quantity.
2. Evidence-based diagnostics.
3. Safe system modifications.
4. Compatibility-aware architecture.
5. Clear separation between Classic Bluetooth and BLE.
6. Beginner-friendly explanations.
7. Advanced technical details when requested.
8. Local-first data storage.
9. Modular and maintainable code.
10. Honest reporting of unsupported or inconclusive results.

---

# 16. Conclusion

BlueDoc will be developed as a modular Windows Bluetooth diagnostic and management application.

Its most important capability will be the Diagnostic Engine, supported by hardware inspection, driver analysis, Windows service monitoring, event analysis, and guided repair workflows.

The architecture is designed to support Windows 7 SP1, Windows 10, and Windows 11 through compatible system-specific implementations.

**BlueDoc — Detect the problem. Understand the cause. Guide the repair.**

**UNLEFT LLC**