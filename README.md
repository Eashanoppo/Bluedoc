# BlueDoc — A Bluetooth Doctor for Windows

[![Build Status](https://img.shields.io/badge/build-passing-brightgreen.svg)]()
[![Platform](https://img.shields.io/badge/platform-Windows%207%20SP1%20%7C%2010%20%7C%2011-blue.svg)]()
[![Framework](https://img.shields.io/badge/.NET%20Framework-4.7.1%20%7C%204.8-purple.svg)]()
[![License](https://img.shields.io/badge/license-UNLEFT%20Commercial-blue.svg)]()

> **"Detect the problem. Understand the cause. Guide the repair."**

**BlueDoc** is an enterprise-grade Windows desktop application engineered by **UNLEFT LLC** under the architectural direction of **Golam Morshed Eashan**. It is designed to diagnose, troubleshoot, inspect, and manage the Windows Bluetooth ecosystem across legacy and modern platforms.

Instead of generic error messages or uninformative "Bluetooth is turned off" dialogs, BlueDoc inspects kernel-level hardware identifiers, queries Configuration Manager problem codes, parses service dependencies, evaluates event log driver crashes, and guides users through verified, safe repair operations.

---

## Key Highlights

- **13 Specialized Modules:** Comprehensive coverage from executive dashboards to BLE GATT characteristic debugging.
- **Deterministic Diagnostic Engine:** 12 automated rules evaluating OS compatibility, hardware status, drivers, services, radio availability, discovery, pairing, BLE, GATT, and event stability.
- **Zero-Dependency PDF Reporting:** Native, built-in vector PDF-1.4 report generator that compiles executive diagnostic summaries without external third-party bloat.
- **Multi-Vendor Hardware Detection:** Accurate chipset identification for Realtek, Intel, Broadcom, Qualcomm, MediaTek, Texas Instruments, and CSR.
- **Safe Repair Center:** 8 non-destructive repair actions governed by explicit permission checks, user confirmation dialogs, and before-and-after state verification.
- **Zero-Fabrication Guarantee:** Strictly adheres to Section 15 prohibitions — no fake signal strength, no synthetic hardware IDs, and no silent background repairs.
- **Cross-Platform Compatibility:** Runs seamlessly on Windows 7 SP1 (Classic Mode), Windows 10, and Windows 11 (Modern BLE & WinRT GATT Mode).

---

## System Architecture

BlueDoc is built upon a modular, layered Model-View-ViewModel (MVVM) architecture with strict separation of concerns:

```
BlueDoc/
├── BlueDoc.sln
│
├── src/
│   ├── BlueDoc.Core/              # Domain models, enums, interfaces, and contracts
│   ├── BlueDoc.Infrastructure/    # SetupAPI, CfgMgr32, Win32 P/Invoke, and PDF generation
│   ├── BlueDoc.Bluetooth/         # Win32 Classic Bluetooth & WinRT BLE GATT abstraction
│   ├── BlueDoc.Diagnostics/       # 12-rule diagnostic engine, context, and evaluation rules
│   ├── BlueDoc.Data/              # SQLite database manager, repositories, and local storage
│   ├── BlueDoc.Application/       # Application workflows, repair engine, and services
│   └── BlueDoc.UI/                # WPF desktop application, MVVM views, view models, and styling
│
├── tests/
│   └── BlueDoc.Tests/             # Unit tests, scenario simulation, and live hardware tests
│
├── installer/
│   └── bluedoc_setup.iss          # Inno Setup 6 installer compilation script
│
├── build_release.ps1              # Automated build, test, and release packaging script
├── README.md                      # Primary documentation
├── RELEASE_NOTES_v1.0.md          # Version 1.0.0 release notes
└── LICENSE                        # UNLEFT LLC software license
```

---

## The 13 Functional Modules

| # | Module Name | Primary Responsibilities |
|---|---|---|
| **1** | **Dashboard** | Overall health score, adapter status tile, service state, connected device counter, and quick-action triggers. |
| **2** | **Device Scanner** | Dual-engine Classic & BLE discovery, device categorization, sanitized MAC address formatting, and honest RSSI metrics. |
| **3** | **Hardware Inspector** | Deep SetupAPI enumeration, VID/PID parsing, chipset identification, Configuration Manager problem codes (e.g. Code 10, Code 22, Code 43), and driver metadata. |
| **4** | **Diagnostic Center** | 12 automated diagnostic rules returning Confirmed Facts, Technical Evidence, Probable Causes, and Recommended Actions. |
| **5** | **Event Analyzer** | Scans Windows Event Log for driver crashes (BTHUSB Events 3, 5, 17) and correlates log anomalies with diagnostic results. |
| **6** | **Driver Assistant** | Inspects driver versions, dates, and INF files; provides direct manufacturer support links and guided rollback instructions. |
| **7** | **Connection History** | Local SQLite persistent storage tracking past device sightings, pairing events, repair audits, and diagnostic history. |
| **8** | **Diagnostic Report** | One-click report generation supporting Plain Text (`.txt`), HTML (`.html`), JSON (`.json`), and native Vector PDF (`.pdf`). |
| **9** | **Safe Repair Center** | 8 guided repair actions (bthserv restart, audio gateway restart, radio power-cycle, troubleshooter, device manager, and cache purge). |
| **10** | **Signal Monitor** | Continuous RSSI polling loop displaying live signal strength in dBm, Unicode signal bars (`▂▄▆█`), and CSV telemetry export. |
| **11** | **BLE Explorer** | GATT service discovery, characteristic enumeration, read/write operations with safety confirmation, and notification streaming. |
| **12** | **Compatibility Checker** | 8-subsystem readiness matrix evaluating OS compatibility, Win32 APIs, SCM, WinRT BLE GATT, Audio Gateway, and core HCI specs. |
| **13** | **Settings & Privacy** | Theme selection (Dark/Light), discovery timeouts, diagnostic log controls, and one-click local cache purging. |
| **14** | **Technician Toolkit** | Performance latency benchmarking (SetupAPI/SCM/Radio), 10+ offline troubleshooting knowledge base articles, and enterprise SaaS cloud fleet telemetry synchronization. |
| **15** | **Audio & Packet Tracer** | Bluetooth Core Audio profile inspection (A2DP stereo vs HFP telephony mic contention), driver version regression matrix, Windows ETW HCI packet tracing, and real-time System Tray Guard. |
| **16** | **SaaS Fleet Central** | Multi-endpoint enterprise cloud portal, live REST API Gateway (`/v1/telemetry`, `/v1/fleet/metrics`), compliance policy checks, and automated background heartbeat polling. |
| **14** | **Technician Toolkit** | Performance latency benchmarking (SetupAPI/SCM/Radio), 10+ offline troubleshooting knowledge base articles, and enterprise SaaS cloud fleet telemetry synchronization. |
| **15** | **Audio & Packet Tracer** | Bluetooth Core Audio profile inspection (A2DP stereo vs HFP telephony mic contention), driver version regression matrix, Windows ETW HCI packet tracing, and real-time System Tray Guard. |
| **16** | **SaaS Fleet Central** | Multi-endpoint enterprise cloud portal, live REST API Gateway (`/v1/telemetry`, `/v1/fleet/metrics`), compliance policy checks, and automated background heartbeat polling. |

---

## Enterprise SaaS REST API Gateway

BlueDoc features an embedded, high-performance mock SaaS Cloud REST API Gateway (`MockSaasCloudApiGateway`) allowing enterprises, MSPs, and DevOps pipelines to simulate, stage, and test centralized fleet telemetry ingestion locally.

### REST Endpoints
| Method | Route | Description | Auth Required |
| :---: | :--- | :--- | :---: |
| `GET` | `/v1/health` | Service health status and active machine count | No |
| `POST` | `/v1/telemetry` | Ingests signed `FleetTelemetryPayload` JSON | Yes (Bearer Token) |
| `GET` | `/v1/fleet/metrics` | Returns aggregated fleet health metrics & compliance % | Yes (Bearer Token) |
| `GET` | `/v1/fleet/machines` | Returns full registry of managed enterprise endpoints | Yes (Bearer Token) |

---

## Headless CLI Automation

BlueDoc includes a full-featured Command Line Interface (CLI) for system administrators, technician toolkits, and remote management agents (RMM / Intune / SCCM).

```bash
# Run full diagnostic suite with JSON output to stdout
BlueDoc.exe --diagnose

# Generate executive PDF report directly to file
BlueDoc.exe --diagnose --format pdf --output C:\Reports\diag.pdf

# Output detailed hardware adapter & driver inventory
BlueDoc.exe --inspect

# Run Bluetooth performance, latency & capability benchmark
BlueDoc.exe --benchmark

# Search offline troubleshooting knowledge base
BlueDoc.exe --kb "Code 43"

# Non-interactively execute a verified safe repair
BlueDoc.exe --repair RestartBthServ --confirm

# Display version and application credits
BlueDoc.exe --version
```

### Deterministic Exit Codes
| Exit Code | Meaning |
| :---: | :--- |
| **0** | All diagnostic tests passed / Operation completed successfully |
| **1** | Diagnostic warnings detected (non-fatal advisory) |
| **2** | Critical hardware, driver, or service failure detected |
| **3** | No Bluetooth adapter detected on system |
| **4** | CLI syntax error or missing mandatory confirmation flag |
| **5** | Elevation required (Administrator privileges missing) |

---

## Headless CLI Automation

BlueDoc includes a full-featured Command Line Interface (CLI) for system administrators, technician toolkits, and remote management agents (RMM / Intune / SCCM).

```bash
# Run full diagnostic suite with JSON output to stdout
BlueDoc.exe --diagnose

# Generate executive PDF report directly to file
BlueDoc.exe --diagnose --format pdf --output C:\Reports\diag.pdf

# Output detailed hardware adapter & driver inventory
BlueDoc.exe --inspect

# Run Bluetooth performance, latency & capability benchmark
BlueDoc.exe --benchmark

# Search offline troubleshooting knowledge base
BlueDoc.exe --kb "Code 43"

# Non-interactively execute a verified safe repair
BlueDoc.exe --repair RestartBthServ --confirm

# Display version and application credits
BlueDoc.exe --version
```

### Deterministic Exit Codes
| Exit Code | Meaning |
| :---: | :--- |
| **0** | All diagnostic tests passed / Operation completed successfully |
| **1** | Diagnostic warnings detected (non-fatal advisory) |
| **2** | Critical hardware, driver, or service failure detected |
| **3** | No Bluetooth adapter detected on system |
| **4** | CLI syntax error or missing mandatory confirmation flag |
| **5** | Elevation required (Administrator privileges missing) |

---

## Operating System Compatibility Matrix

| Operating System | SetupAPI & CfgMgr32 | Win32 Classic Bluetooth | Windows SCM Services | WinRT BLE & GATT | Diagnostic Engine |
| :--- | :---: | :---: | :---: | :---: | :---: |
| **Windows 7 SP1 (x86 & x64)** | Supported | Supported | Supported | *Not Supported* | Supported (Classic Mode) |
| **Windows 10 (Build 19041+)** | Supported | Supported | Supported | Supported | Supported (Full Modern Mode) |
| **Windows 11 (All Builds)** | Supported | Supported | Supported | Supported | Supported (Full Modern Mode) |

*Note: On Windows 7 SP1, BlueDoc gracefully degrades WinRT BLE features, labeling them as `NOT SUPPORTED` without application failure or unhandled exceptions.*

---

## Build & Development Guide

### Prerequisites
1. **Operating System:** Windows 10 or Windows 11 (Build 19041 or newer recommended for development).
2. **IDE:** Visual Studio 2022 (Community, Professional, or Enterprise) with:
   - *.NET desktop development* workload installed.
   - *.NET Framework 4.7.1 / 4.8 targeting pack*.
3. **Inno Setup 6 (Optional):** Required only if compiling the Windows Installer `.exe` from source.

### 1. Build via Visual Studio
Open `BlueDoc.sln` in Visual Studio 2022, set the solution configuration to `Release` and platform to `Any CPU`, and select **Build > Rebuild Solution**.

### 2. Build & Verify via PowerShell
Execute the automated release script from an elevated or standard PowerShell console:
```powershell
.\build_release.ps1
```
This script will:
1. Rebuild the solution in `Release` configuration.
2. Run the complete automated test harness (HardwareIdParser, Diagnostic Rules, Device Class Decoder, SQLite Storage, Heuristics, Vendor Mapping, PDF Generation, Advanced Diagnostics, and Scenario Simulations).
3. Stage the standalone binaries in `dist\BlueDoc_v1.0.0_Portable\`.
4. Generate the compressed portable distribution archive `dist\BlueDoc_v1.0.0_Portable.zip`.
5. Automatically compile `dist\BlueDoc_v1.0.0_Setup.exe` if Inno Setup 6 (`ISCC.exe`) is detected.

---

## Distribution Packages

- **Portable Edition:** `dist\BlueDoc_v1.0.0_Portable.zip`  
  *Extract and run `BlueDoc.exe` directly on any machine without installation.*
- **Installer Edition:** `dist\BlueDoc_v1.0.0_Setup.exe`  
  *Standard Windows Setup wizard installing to `%ProgramFiles%\UNLEFT\BlueDoc`.*

---

## Safety and Privacy Guarantees

- **No Data Collection:** BlueDoc operates 100% locally on the host machine. Diagnostics and device cache are saved exclusively to the local SQLite database. No analytics or reports are transmitted to external servers.
- **No Unsolicited Modifications:** Safe Repair actions are never performed in the background. Each repair displays an explanation, lists potential side effects, verifies administrator privileges, and requires explicit user confirmation.
- **No Third-Party Bloat:** All reports (including PDF) and telemetry exporters are written with native, zero-dependency C# code.

---

## Authors & License

- **Developer:** Golam Morshed Eashan
- **Company:** UNLEFT LLC ([https://unleft.com](https://unleft.com))
- **Inquiries:** support@unleft.com
- **Copyright:** Copyright © 2026 UNLEFT LLC. All Rights Reserved.
