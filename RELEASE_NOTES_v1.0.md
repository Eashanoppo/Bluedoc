# BlueDoc v1.0.0 Release Notes

**Product:** BlueDoc - A Bluetooth Doctor for Windows  
**Release Version:** 1.0.0  
**Release Date:** November 2026  
**Company:** UNLEFT LLC  
**Lead Architect & Developer:** Golam Morshed Eashan  
**Target Operating Systems:** Windows 7 SP1, Windows 10, Windows 11 (32-bit & 64-bit)  
**Target Runtime:** Microsoft .NET Framework 4.7.1 / 4.8  

---

## 1. Executive Overview

UNLEFT LLC is proud to present **BlueDoc v1.0.0**, the definitive diagnostic, troubleshooting, hardware inspection, and device management platform for the Microsoft Windows Bluetooth ecosystem.

Bluetooth on Windows is notoriously opaque: when headphones disconnect, adapters disappear, or pairing hangs indefinitely, Windows Settings rarely explains *why*. BlueDoc operates under the guiding mission: **Detect the problem. Understand the cause. Guide the repair.**

BlueDoc combines kernel-level SetupAPI queries, Configuration Manager (CfgMgr32) hardware problem analysis, Service Control Manager (SCM) telemetry, Windows Event Log heuristics, Win32 Bluetooth Classic APIs, and modern Windows Runtime (WinRT) Bluetooth Low Energy (BLE) Gatt APIs into a cohesive, professional WPF desktop experience.

---

## 2. Core Modules Implemented (v1.0.0 Scope)

### Module 1: Executive Dashboard
- Real-time overall Bluetooth health score indicator (Healthy, Degraded, Critical).
- Adapter operational status tile with vendor logos and hardware state.
- Live Bluetooth Support Service (`bthserv`) and Audio Gateway service monitors.
- Connected and paired device counter.
- Quick action triggers: *Run Full Diagnosis*, *Scan Devices*, and *Repair Center*.
- Recent diagnostic alerts feed and quick-link fixes.

### Module 2: Bluetooth Device Scanner
- Dual-engine discovery: Native Win32 Bluetooth Classic device enumeration and modern WinRT BLE advertisement scanner.
- Rich device information: Friendly name, sanitized MAC address (`XX:XX:XX:XX:XX:XX`), device type (Classic, LowEnergy, DualMode), and Bluetooth Class of Device (CoD) decoding.
- Honest signal strength reporting: Exposes live RSSI in dBm only when reliably provided by the controller; never fabricates synthetic values.
- Real-time device search, type filtering, and pairing status indicators.

### Module 3: Advanced Hardware Inspector
- Deep device enumeration via SetupAPI and Configuration Manager.
- Comprehensive hardware identity extraction: Vendor ID (VID), Product ID (PID), Revision, Subsystem IDs, and PnP Device Instance Path.
- Multi-manufacturer chipset database identifying Realtek, Intel, Broadcom, Qualcomm, MediaTek, Texas Instruments, Cambridge Silicon Radio (CSR), and generic controllers.
- Configuration Manager problem code translation: Code 0 (Operational), Code 10 (CM_PROB_FAILED_START), Code 22 (CM_PROB_DISABLED), Code 43 (CM_PROB_FAILED_POST_START), Code 28 (CM_PROB_FAILED_INSTALL).
- Driver metadata: Provider name, signed driver version, installation date, and INF registry file bindings.

### Module 4: Diagnostic Test Center & Rules Engine
- Deterministic 12-rule diagnostic execution engine:
  1. `RULE_OS_COMPAT`: Operating System Version & Subsystem Compatibility
  2. `RULE_ADAPTER_DETECT`: Bluetooth Controller / Transceiver Detection
  3. `RULE_HW_STATUS`: Hardware Device State & CM Problem Codes
  4. `RULE_DRIVER_HEALTH`: Driver Installation, Provider, and Registry Bindings
  5. `RULE_SERVICE_STATUS`: Bluetooth Support Service (`bthserv`) Execution & Startup Type
  6. `RULE_AUDIO_SERVICE`: Hands-Free & Audio Gateway Services (`BthHFSrv` / `BTAGService`)
  7. `RULE_RADIO_AVAIL`: Radio Power, Transceiver State & Soft/Hard Switch Status
  8. `RULE_DISCOVERY_READY`: Bluetooth Stack Discovery Subsystem Readiness
  9. `RULE_PAIRING_READY`: Security & Device Pairing Subsystem Verification
  10. `RULE_BLE_AVAIL`: Bluetooth Low Energy (BLE 4.0+) Host & Controller Support
  11. `RULE_GATT_SUPPORT`: Attribute Protocol (ATT) & GATT Client Support
  12. `RULE_EVENT_LOG_HEALTH`: System Event Log Error Frequency & Driver Crash Analysis
- Structured result statuses: `PASSED`, `WARNING`, `FAILED`, `NOT SUPPORTED`, `INCONCLUSIVE`, `NOT TESTED`.
- Every test surfaces Confirmed Facts, Technical Evidence, Probable Causes, and Actionable Remediation Guidance.

### Module 5: Bluetooth Event Analyzer
- Windows Event Log telemetry scanner targeting `System` log providers (`BTHUSB`, `bthserv`, `BTHMINI`, `Bluetooth-BthLEPrePairing`).
- Pattern recognition heuristics decoding critical driver issues:
  - Event 3: HCI Command Timeout (Controller lockup / firmware freeze)
  - Event 5: Initialization Failure (Firmware patch download failed)
  - Event 17: Controller not responding / power state transition failure
  - Service control error events (SCM timeout, missing dependencies)
- Correlation engine cross-referencing event occurrences directly with diagnostic rule findings.
- CSV export for technical support dispatch and IT department auditing.

### Module 6: Driver Assistant
- Complete driver profile inspector displaying Provider, Driver Version, Driver Date, and Driver Node INF path.
- Manufacturer Support Portal directory: Direct official links to Intel Customer Support, Realtek Download Center, Broadcom, Qualcomm, MediaTek, and OEM PC portals.
- Integrated Device Manager shortcut (`devmgmt.msc`) targeting Bluetooth controllers.
- Step-by-step driver rollback guidance to revert corrupted Windows Update patches.
- Driver reinstallation guidance without unverified or dangerous third-party tools.

### Module 7: Device Connection History & Local Cache
- Local persistent SQLite database storage (`BlueDoc.db`) located in `%LocalAppData%\UNLEFT\BlueDoc\Data\`.
- Historical tracking of discovered devices, paired peripherals, connection timestamps, and last-seen dates.
- Diagnostic session archiving with complete test result histories.
- Safe repair audit logging with before/after state verification.
- User privacy controls: One-click cache purging and full database history clearing.

### Module 8: Multi-Format Diagnostic Reporting
- Native zero-dependency vector PDF generator (`PdfReportGenerator`):
  - Produces standards-compliant PDF-1.4 documents.
  - Formatted Letter layout with executive navy banners, status badges, test breakdown tables, and running headers/footers.
  - Zero external bloat (no third-party PDF libraries required).
- Plain text (.txt) diagnostic export.
- Styled, self-contained HTML (.html) reports with print CSS stylesheet.
- Machine-readable JSON (.json) export adhering to strict schema for enterprise SIEM ingestion.
- Local-only file persistence: Never uploads user telemetry or hardware reports to external cloud servers.

### Module 9: Safe Repair Center
- Guided 8-point safe repair operation catalog:
  1. `RestartBthServ`: Restart Windows Bluetooth Support Service (`bthserv`)
  2. `RestartAudioGateway`: Restart Audio Gateway and Hands-Free Audio Services (`BTAGService` / `BthHFSrv`)
  3. `CycleBluetoothRadio`: Reset & power-cycle Bluetooth radio transceiver
  4. `LaunchTroubleshooter`: Launch native Windows Bluetooth Diagnostic Troubleshooter (`msdt.exe`)
  5. `OpenDeviceManager`: Open Windows Device Manager (`devmgmt.msc`)
  6. `OpenBluetoothSettings`: Launch Windows Modern Settings / Classic Bluetooth Control Panel (`bthprops.cpl`)
  7. `GuideDriverRollback`: Step-by-step guided Device Manager rollback workflow
  8. `ClearDeviceCache`: Purge stale device cache from local SQLite storage
- Rigorous 8-step safety process: Problem identification, repair explanation, consequence disclosure, elevation check, explicit user confirmation, execution, verification, and audit recording.
- Strictly adheres to Section 15: Zero silent repairs.

### Module 10: Signal and Discovery Monitor
- Continuous telemetry scanning loop monitoring RSSI fluctuation.
- Honest RSSI metrics in dBm with visual signal bar indicators (`▂▄▆█`).
- Signal quality categorization: *Excellent* (≥ -50 dBm), *Good* (-51 to -65 dBm), *Fair* (-66 to -80 dBm), *Poor* (< -80 dBm), or *Not Reported*.
- Real-time search, sorting, and CSV telemetry export.

### Module 11: Bluetooth Low Energy (BLE) GATT Explorer
- WinRT GATT client for Windows 10 and Windows 11.
- Service discovery resolving standard Bluetooth SIG 16-bit UUIDs (e.g., Battery Service `0x180F`, Heart Rate `0x180D`, Device Information `0x180A`) and custom 128-bit vendor UUIDs.
- Characteristic discovery exposing properties: Read, Write, WriteWithoutResponse, Notify, Indicate.
- Characteristic read execution with ASCII and hex decoding.
- Safe characteristic write tool with mandatory confirmation modal explaining payload impact.
- Live GATT notification subscription engine with streaming byte updates.
- Full GATT profile tree export to TXT and JSON.

### Module 12: Compatibility Checker
- Comprehensive 8-subsystem readiness matrix:
  1. SetupAPI Device Enumeration
  2. Win32 Classic Bluetooth APIs (`BluetoothAPIs.dll`)
  3. Windows Service Control Manager (SCM)
  4. Windows Runtime (WinRT) BLE GATT Infrastructure
  5. Bluetooth Audio Gateway & Hands-Free Profile
  6. Windows Event Telemetry & Logging
  7. Bluetooth Core & HCI Controller Capabilities
  8. Radio Power Management & Hardware Kill Switches
- Graceful degradation: Explicitly marks WinRT BLE as `NOT SUPPORTED` on Windows 7 SP1 while ensuring full Classic discovery and SetupAPI diagnostics continue without errors.

### Module 13: Settings & Privacy
- Theme customization (Dark / Light mode).
- Discovery timeout and scan preferences.
- Privacy-first data retention options.
- Diagnostic log management.
- Application and licensing credits.

### Module 14: Technician Toolkit & SaaS Fleet Operations (Phase 8)
- **Headless CLI Automation Engine:** Non-interactive execution for system administrators, technician thumbdrives, and enterprise remote management agents (`--diagnose`, `--inspect`, `--benchmark`, `--kb`, `--repair <id> --confirm`).
- **Bluetooth Performance & Latency Benchmark:** Automated probe measuring SetupAPI PnP enumeration latency, SCM service database turnaround, and Win32 radio inquiry latency, computing a composite score (0..100) and spec grade.
- **Offline Troubleshooting Knowledge Base:** 10 curated deep-dive engineering articles with symptom-cause-remediation guides, category filtering, and direct links to Safe Repair actions.
- **Enterprise SaaS Fleet Telemetry Sync:** Opt-in JSON Schema v1.0 telemetry client allowing IT organizations to aggregate diagnostic assessments, hardware inventories, driver versions, and failure signatures to central fleet endpoints.

### Module 15: Audio Diagnostics, Driver Matrix & HCI Packet Tracer (Phase 9)
- **Audio Stream & Profile Contention Analyzer:** Identifies whether Bluetooth audio endpoints operate in high-definition A2DP stereo mode or are throttled into mono telephony mode (HFP/HSP) due to active communications microphones (Discord, Teams, Zoom).
- **Driver Version Comparison & Regression Matrix:** Cross-checks installed driver versions against confirmed stable OEM baselines and flags known crash regressions (e.g. S3 sleep wake lockup, high-traffic HCI timeouts).
- **HCI Packet Trace Assistant:** Built-in integration with Windows native ETW Bluetooth provider (`netsh trace start scenario=Bluetooth`) capturing raw HCI packets with an integrated Wireshark conversion guide.
- **Real-Time System Tray Guard:** Background health probe monitoring adapter connectivity, SCM service states, and Configuration Manager status, delivering instant desktop alerts upon controller disconnection or driver crashes.

### Module 16: SaaS Fleet Cloud Portal & REST API Gateway (Phase 10)
- **Centralized SaaS Fleet Dashboard:** Multi-endpoint enterprise monitoring portal visualizing real-time health states, benchmark scores, driver versions, and failure signatures across managed endpoints.
- **Embedded Mock SaaS Cloud REST API Gateway:** Self-contained HTTP listener (`MockSaasCloudApiGateway`) providing authenticated `/v1/telemetry`, `/v1/fleet/metrics`, and `/v1/fleet/machines` endpoints for staging, testing, and offline enterprise demonstrations.
- **Automated Background Telemetry Poller:** Autonomous scheduler (`SaasTelemetryPollerService`) transmitting diagnostic heartbeats to cloud endpoints on admin-configurable intervals without disrupting user workflow.
- **Enterprise Policy Compliance Engine:** Automated checks identifying non-compliant driver revisions (e.g. Generic Microsoft or regression-prone builds) and service misconfigurations across corporate endpoints.

### Module 14: Technician Toolkit & SaaS Fleet Operations (Phase 8)
- **Headless CLI Automation Engine:** Non-interactive execution for system administrators, technician thumbdrives, and enterprise remote management agents (`--diagnose`, `--inspect`, `--benchmark`, `--kb`, `--repair <id> --confirm`).
- **Bluetooth Performance & Latency Benchmark:** Automated probe measuring SetupAPI PnP enumeration latency, SCM service database turnaround, and Win32 radio inquiry latency, computing a composite score (0..100) and spec grade.
- **Offline Troubleshooting Knowledge Base:** 10 curated deep-dive engineering articles with symptom-cause-remediation guides, category filtering, and direct links to Safe Repair actions.
- **Enterprise SaaS Fleet Telemetry Sync:** Opt-in JSON Schema v1.0 telemetry client allowing IT organizations to aggregate diagnostic assessments, hardware inventories, driver versions, and failure signatures to central fleet endpoints.

### Module 15: Audio Diagnostics, Driver Matrix & HCI Packet Tracer (Phase 9)
- **Audio Stream & Profile Contention Analyzer:** Identifies whether Bluetooth audio endpoints operate in high-definition A2DP stereo mode or are throttled into mono telephony mode (HFP/HSP) due to active communications microphones (Discord, Teams, Zoom).
- **Driver Version Comparison & Regression Matrix:** Cross-checks installed driver versions against confirmed stable OEM baselines and flags known crash regressions (e.g. S3 sleep wake lockup, high-traffic HCI timeouts).
- **HCI Packet Trace Assistant:** Built-in integration with Windows native ETW Bluetooth provider (`netsh trace start scenario=Bluetooth`) capturing raw HCI packets with an integrated Wireshark conversion guide.
- **Real-Time System Tray Guard:** Background health probe monitoring adapter connectivity, SCM service states, and Configuration Manager status, delivering instant desktop alerts upon controller disconnection or driver crashes.

### Module 16: SaaS Fleet Cloud Portal & REST API Gateway (Phase 10)
- **Centralized SaaS Fleet Dashboard:** Multi-endpoint enterprise monitoring portal visualizing real-time health states, benchmark scores, driver versions, and failure signatures across managed endpoints.
- **Embedded Mock SaaS Cloud REST API Gateway:** Self-contained HTTP listener (`MockSaasCloudApiGateway`) providing authenticated `/v1/telemetry`, `/v1/fleet/metrics`, and `/v1/fleet/machines` endpoints for staging, testing, and offline enterprise demonstrations.
- **Automated Background Telemetry Poller:** Autonomous scheduler (`SaasTelemetryPollerService`) transmitting diagnostic heartbeats to cloud endpoints on admin-configurable intervals without disrupting user workflow.
- **Enterprise Policy Compliance Engine:** Automated checks identifying non-compliant driver revisions (e.g. Generic Microsoft or regression-prone builds) and service misconfigurations across corporate endpoints.

---

## 3. Hardware Compatibility & Verification Matrix

BlueDoc v1.0.0 has been validated against physical and simulated controllers across major chipset vendors:

| Chipset Vendor | Controller Series / VID | Status | Notes |
| :--- | :--- | :--- | :--- |
| **Realtek** | RTL8852BE, RTL8761B, RTL8821CE (`0x0BDA`) | **Verified** | Validated on physical host machine |
| **Intel** | AX200, AX201, AX210, BE200 (`0x8087`) | **Verified** | Hardware ID & Driver parsing validated |
| **Broadcom** | BCM20702, BCM43142 (`0x0A5C`) | **Verified** | Broadcom vendor mapping & legacy checks validated |
| **Qualcomm / Atheros** | QCA6174, QCA9377, FastConnect (`0x0CF3`) | **Verified** | SetupAPI parsing & power management validated |
| **MediaTek** | MT7921, MT7922 (`0x0E8D`) | **Verified** | Wi-Fi 6/6E Bluetooth combination chips validated |
| **Texas Instruments** | CC2564, CC2650 (`0x0451`) | **Verified** | Industrial BLE transceivers validated |
| **CSR / Cambridge** | CSR8510 A10 (`0x0A12`) | **Verified** | Classic 4.0 USB dongle heuristics verified |

---

## 4. Distribution Packages

1. **Portable Edition (`BlueDoc_v1.0.0_Portable.zip`):**
   - Zero-installation package for system administrators and field technicians.
   - Contains standalone executable, core libraries, native SQLite binaries (`x86`/`x64`), and documentation.
   - Run `BlueDoc.exe` directly from any USB flash drive or directory.
2. **Windows Installer (`installer\bluedoc_setup.iss`):**
   - Inno Setup 6 script providing automated installation to `%ProgramFiles%\UNLEFT\BlueDoc`.
   - Start Menu and Desktop shortcut creation.
   - Registry-based .NET Framework 4.7.1+ pre-flight detection.
   - Clean, complete uninstallation without leaving residual registry entries.

---

## 5. Credits and Acknowledgments

- **Company:** UNLEFT LLC (https://unleft.com)
- **Lead Architect & Developer:** Golam Morshed Eashan
- **Built With:** Microsoft .NET Framework 4.7.1 / 4.8, Windows Presentation Foundation (WPF), System.Data.SQLite, Windows SetupAPI, Configuration Manager (CfgMgr32), Win32 Bluetooth APIs, and Windows Runtime (WinRT).
