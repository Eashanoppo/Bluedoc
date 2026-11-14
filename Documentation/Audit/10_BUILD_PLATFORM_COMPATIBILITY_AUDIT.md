# BlueDoc Build, Dependencies & Platform Compatibility Audit

**Document ID:** `BD-AUDIT-10`  
**Auditor Roles:** Principal Software Architect, Senior C# / .NET Engineer, Windows Systems Integration Engineer  
**Target Solution:** `BlueDoc.sln`  
**Audit Date:** November 2026  
**Status:** COMPLETE / EVIDENCE-BASED  

---

## 1. Executive Summary

This forensic audit investigates the compilation toolchain, target framework configurations, assembly references, package dependencies, installer packaging, and multi-version Windows compatibility across the three target operating system tiers specified in the PRD:
- **Windows 7 SP1** (Kernel 6.1)
- **Windows 10** (Kernel 10.0, Builds 10240 - 19045)
- **Windows 11** (Kernel 10.0, Builds 22000 - 26100+)

---

## 2. Compilation Toolchain & Framework Discrepancy

### 2.1 The Framework Discrepancy (BD-CRIT-001)
- **Requirement:** PRD Section 10 ("Technical Architecture") and SDD Section 2.1 mandate:
  > Target Framework: Microsoft .NET Framework 4.8
- **Actual Implementation in Codebase:**
  All eight (8) `.csproj` project files across the solution specify:
  ```xml
  <TargetFrameworkVersion>v4.7.1</TargetFrameworkVersion>
  ```
- **Inno Setup Installer Script (`installer\bluedoc_setup.iss`):**
  Lines 96-104:
  ```pascal
  // Checks Release DWORD in HKLM\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full
  if installedRelease >= 461308 then // 461308 = .NET Framework 4.7.1
      Result := true;
  ```
  The installer accepts .NET 4.7.1 while displaying a message box instructing the user to install .NET 4.8.

#### Engineering Analysis & Impact:
- **Why was .NET 4.7.1 chosen initially?** .NET Framework 4.7.1 is the minimum version that provides first-class support for `netstandard2.0` assembly consumption and early WinRT projections via `System.Runtime.WindowsRuntime.dll`.
- **The Risk:** .NET Framework 4.8 includes critical runtime fixes for Windows 10/11 High-DPI per-monitor scaling v2, accessible UI automation events, and TLS 1.3/AES-GCM security enhancements.
- **Verdict:** Solution compiles cleanly with MSBuild 17+ targeting v4.7.1, but all projects must be upgraded to `<TargetFrameworkVersion>v4.8</TargetFrameworkVersion>` to align with PRD specification and maximize platform longevity.

---

### 2.2 Platform Target: AnyCPU vs x86 vs x64
- **Configuration:** All projects specify `<PlatformTarget>AnyCPU</PlatformTarget>` with `<Prefer32Bit>false</Prefer32Bit>` in `Release` configuration.
- **Native Interop Dependency:** SQLite uses unmanaged C++ binaries for SQLite database operations:
  - `x86\SQLite.Interop.dll` (32-bit native PE)
  - `x64\SQLite.Interop.dll` (64-bit native PE)
- **Runtime Resolution:** `System.Data.SQLite.dll` automatically detects the process bitness via `Environment.Is64BitProcess` and loads the appropriate `SQLite.Interop.dll` from the corresponding subfolder.
- **Inno Setup Packaging (`bluedoc_setup.iss` lines 61-63):**
  ```pascal
  Source: "{#SourceBinPath}\x86\SQLite.Interop.dll"; DestDir: "{app}\x86"; Flags: ignoreversion
  Source: "{#SourceBinPath}\x64\SQLite.Interop.dll"; DestDir: "{app}\x64"; Flags: ignoreversion
  ```
  Both 32-bit and 64-bit native DLLs are packaged into the installer.
- **Assessment:** **PASSED**. Bit-neutral deployment operates correctly across 32-bit and 64-bit Windows installations.

---

## 3. NuGet Packages and Assembly Dependencies

| Package / Assembly | Version | Referencing Projects | Purpose | Security / License |
|---|---|---|---|---|
| `Newtonsoft.Json` | `13.0.3` | All projects | DTO serialization, config files, reports | MIT / Up to date |
| `System.Data.SQLite.Core`| `1.0.118.0` | `BlueDoc.Data`, `BlueDoc.UI` | Connection history & repair cache persistence | Public Domain / Up to date |
| `Windows.winmd` | `10.0.19041.0` | `BlueDoc.Bluetooth` | Win10 WinRT Bluetooth APIs (BLE, Radios) | MS SDK Agreement |
| `System.Runtime.WindowsRuntime` | `4.6.0` | `BlueDoc.Bluetooth` | IAsyncOperation to Task bridge (`AsTask()`) | MIT / Supported |

- **Assembly Integrity:** Zero deprecated, abandoned, or GPL-encumbered third-party packages are present.
- **Dependency Isolation:** No dynamic code emission (`Reflection.Emit`) or unverified third-party NuGet packages are used.

---

## 4. Platform Compatibility Forensic Analysis

### 4.1 Windows 7 SP1 Compatibility (The WinRT TypeLoad Hazard)

#### PRD Specification:
PRD Section 1 explicitly designates **Windows 7 SP1** as a supported target platform.

#### Native Windows 7 Subsystem Characteristics:
- Kernel version: 6.1.7601 (SP1).
- Native Bluetooth Stack: Win32 Bluetooth API (`bthprops.cpl`, `BluetoothAPIs.dll`, `Irprops.cpl`). Supports **Bluetooth 2.1 + EDR** and **Bluetooth 3.0 + HS**.
- Native Bluetooth LE (Low Energy / 4.0): **UNSUPPORTED**. Windows 7 kernel drivers (`bthport.sys`) lack native BLE GATT client/server architecture.
- Windows Runtime (WinRT): **ABSENT**. WinRT was introduced in Windows 8 (NT 6.2) and does not exist on Windows 7.

#### Critical Code Defect: BD-CRIT-002 (WinRT TypeLoad Hazard)
In `BlueDoc.Bluetooth.csproj`:
```xml
<Reference Include="Windows">
  <HintPath>C:\Program Files (x86)\Windows Kits\10\UnionMetadata\10.0.19041.0\Windows.winmd</HintPath>
</Reference>
<Reference Include="System.Runtime.WindowsRuntime">
  <HintPath>..\..\packages\System.Runtime.WindowsRuntime.4.6.0\lib\net461\System.Runtime.WindowsRuntime.dll</HintPath>
</Reference>
```
In `src/BlueDoc.Bluetooth/Services/DeviceDiscoveryService.cs` (lines 35-38):
```csharp
public class DeviceDiscoveryService : IDeviceDiscoveryService
{
    private readonly BleAdvertisementWatcherProvider _bleWatcher; // WinRT instance field!
    // ...
```
- **The CLR Failure Mechanism:**
  When the .NET Common Language Runtime (CLR) JIT-compiles `DeviceDiscoveryService` on Windows 7, it attempts to resolve the type metadata for `BleAdvertisementWatcherProvider` and its underlying WinRT types (`Windows.Devices.Bluetooth.Advertisement.BluetoothLEAdvertisementWatcher`).
  Because `Windows.winmd` is absent on Windows 7, the CLR immediately throws a fatal `TypeLoadException` or `FileNotFoundException` during class initialization, crashing the entire application before any discovery code can execute.

#### Architectural Solution for Windows 7:
1. Abstract BLE discovery behind an interface `IBleAdvertisementWatcher`.
2. Move WinRT implementations into a dedicated adapter assembly (`BlueDoc.Bluetooth.Win10.dll`).
3. Use a runtime OS check (`OperatingSystemInspector.IsWindows10OrGreater()`):
   - On Windows 10/11: Dynamically load `BlueDoc.Bluetooth.Win10.dll`.
   - On Windows 7: Load a Null/Fallback provider `NullBleWatcherProvider` that returns `IsSupported = false`.

---

### 4.2 Windows 10 Compatibility
- **Evaluation:** Complete native compatibility.
- Windows 10 provides full WinRT `Windows.Devices.Bluetooth` and `Windows.Devices.Radios` APIs.
- Win32 SetupAPI and SCM function identically to legacy specifications.
- Swift Pair notifications and Bluetooth 5.0+ LE Coded PHY are supported on builds 1809 and newer.

---

### 4.3 Windows 11 Compatibility (The MSDT Deprecation Defect)

#### Subsystem Differences in Windows 11:
- Windows 11 22H2 / 23H2 / 24H2 introduces LE Audio, LC3 codec, and Auracast broadcast support.
- Microsoft has completely overhauled Windows Settings (`ms-settings:bluetooth`).
- **Defect BD-MED-006 (MSDT Deprecation):**
  - In `SafeRepairEngine.cs` (line 374):
    ```csharp
    Process.Start("msdt.exe", "/id BluetoothDiagnostic");
    ```
  - **The Defect:** Microsoft officially deprecated the Microsoft Support Diagnostic Tool (MSDT) in 2023 due to CVE vulnerabilities (Follina). In Windows 11 23H2 and 24H2, `msdt.exe /id BluetoothDiagnostic` displays a message box stating:
    > "Microsoft Support Diagnostic Tool (MSDT) has been retired."
    or fails silently.
  - **Solution:** Detect Windows 11 via build number (`build >= 22000`). If Windows 11, execute:
    ```csharp
    Process.Start(new ProcessStartInfo("ms-settings:troubleshoot") { UseShellExecute = true });
    ```
    Only execute `msdt.exe /id BluetoothDiagnostic` on Windows 10 (Builds 10240 - 19045) and Windows 7/8.1.

---

## 5. Inno Setup Installer Script Forensic Review (`bluedoc_setup.iss`)

We audited `installer\bluedoc_setup.iss` line-by-line:

### 5.1 Positive Architectural Aspects:
- `AppId={{D37E6924-E859-4D89-A2C3-5A384A9E2190}}`: Unique GUID prevents upgrade collisions.
- `ArchitecturesInstallIn64BitMode=x64compatible`: Ensures 64-bit systems install to `C:\Program Files\BlueDoc` rather than `Program Files (x86)`.
- `PrivilegesRequired=admin`: Correctly requires administrator rights to install services, shortcuts, and write to `Program Files`.
- Includes both 32-bit and 64-bit `SQLite.Interop.dll` native binaries.
- `MinVersion=6.1sp1`: Enforces Windows 7 SP1 minimum operating system check at installer level.

### 5.2 Installer Packaging Deficiencies:
1. **Missing .NET 4.8 Prerequisite Check:**
   - In lines 96-104, `IsDotNetSupported()` checks for `installedRelease >= 461308` (.NET 4.7.1).
   - *Fix:* Update check to `installedRelease >= 528040` to enforce .NET Framework 4.8 runtime presence.
2. **Missing VC++ Runtime Check:**
   - `SQLite.Interop.dll` requires Microsoft Visual C++ 2015-2022 Redistributable (`msvcr140.dll`).
   - If a fresh Windows 7 SP1 machine lacks the VC++ runtime, SQLite will fail to load.
   - *Fix:* Bundle the statically linked SQLite binary (`System.Data.SQLite` bundled interop) or add a redistributable check in Inno Setup.

---

## 6. Build & Platform Compatibility Scorecard

| Platform / Metric | Compatibility Status | Risk Level | Primary Action Item |
|---|---|---|---|
| **.NET Framework** | Discrepancy (4.7.1 vs 4.8) | MEDIUM (BD-CRIT-001) | Retarget all 8 `.csproj` to v4.8 |
| **Windows 7 SP1** | High Risk (WinRT TypeLoad) | CRITICAL (BD-CRIT-002) | Decouple WinRT behind dynamic adapter |
| **Windows 10** | 100% Compatible | NONE | Full feature set verified |
| **Windows 11** | Compatible (MSDT Retired) | MEDIUM (BD-MED-006) | Route troubleshooter to `ms-settings:troubleshoot` |
| **Architecture** | 100% Compatible | NONE | AnyCPU + x86/x64 native SQLite DLLs verified |
| **Installer** | Fully Functional | LOW | Update .NET check to Release 528040 (.NET 4.8) |

