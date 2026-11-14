# 05. Bluetooth Backend & Windows API Forensic Audit — BlueDoc

**Product:** BlueDoc  
**Company:** UNLEFT LLC  
**Lead Developer:** Golam Morshed Eashan  
**Audit Date:** November 14, 2026  
**Auditor:** Senior Forensic Audit Consortium  

---

## 1. Executive Summary: Internal Operations vs. External Windows Redirection

A primary architectural requirement of BlueDoc is that it must operate as a genuine, independent Bluetooth diagnostic and management application rather than functioning as a "launcher" that deflects operations to Windows Settings (`ms-settings:bluetooth`) or Control Panel.

### Key Audit Finding
**BlueDoc executes virtually all core Bluetooth operations internally via direct Windows native APIs:**
- **Adapter Detection & Hardware Profiling:** 100% Internal via SetupAPI (`setupapi.dll`) and Configuration Manager (`cfgmgr32.dll`).
- **Radio Enumeration & MAC Address Retrieval:** 100% Internal via Win32 `BluetoothFindFirstRadio` and `BluetoothGetRadioInfo` (`bthprops.cpl`).
- **Device Discovery (Classic):** 100% Internal via Win32 `BluetoothFindFirstDevice` with `fIssueInquiry = true` (`bthprops.cpl`).
- **Device Discovery (BLE):** 100% Internal via Windows Runtime `BluetoothLEAdvertisementWatcher` (`Windows.Devices.Bluetooth.Advertisement`).
- **Device Pairing (Classic):** 100% Internal via Win32 `BluetoothAuthenticateDevice` with in-app PIN entry modal.
- **Device Pairing (BLE):** 100% Internal via WinRT `DeviceInformationCustomPairing` with in-app numeric comparison and PIN prompt.
- **Connection Management (Audio / HID / Serial):** 100% Internal via Win32 `BluetoothSetServiceState` (`bthprops.cpl`).
- **Device Unpairing:** 100% Internal via Win32 `BluetoothRemoveDevice` (`bthprops.cpl`).
- **Radio Transceiver Power Control:** 100% Internal via Configuration Manager (`CM_Enable_DevNode` / `CM_Disable_DevNode`) and SetupAPI `DIF_PROPERTYCHANGE`.

### Where External Applications Are Launched
External applications (`devmgmt.msc`, `ms-settings:bluetooth`, `msdt.exe`, browser URLs) are invoked **only** in specific auxiliary troubleshooting scenarios:
1. When the user explicitly requests to open Device Manager from Driver Assistant or Safe Repair Center.
2. In `SafeRepairEngine.LaunchTroubleshooter` to launch the native Windows Troubleshooter.
3. In `SafeRepairEngine.OpenBluetoothSettings` as an optional user-selected maintenance shortcut.
4. In `DriverAssistant` to open official manufacturer download websites in the default web browser.
5. In `DeviceManagementService.LaunchWindowsPairingWizard` which exists as a fallback method but is **orphaned** (not called by the primary UI).

---

## 2. Comprehensive Operation & API Implementation Table

| Bluetooth Feature | Current Implementation Behavior | Internal API Used | External Launch Invocation | Expected Independent Behavior | Defect / Finding Classification |
|---|---|---|---|---|---|
| **Adapter Enumeration** | Queries PnP device tree for Bluetooth class devices | SetupAPI: `SetupDiGetClassDevs`<br/>`SetupDiEnumDeviceInfo` | None | Enumerate internal and external adapters directly | **VERIFIED INTERNAL (100% Native)** |
| **Adapter Problem Codes** | Queries devnode status flags and error numbers | CfgMgr32: `CM_Get_DevNode_Status`<br/>(Code 0, 10, 22, 43) | None | Extract real-time Configuration Manager error codes | **VERIFIED INTERNAL (100% Native)** |
| **Adapter Radio MAC** | Queries radio controller handle and transceivers | Win32: `BluetoothFindFirstRadio`<br/>`BluetoothGetRadioInfo` | None | Retrieve 48-bit MAC address of radio controller | **VERIFIED INTERNAL (100% Native)** |
| **Radio Power Toggle** | Powers controller transceiver on/off via PnP | CfgMgr32: `CM_Enable_DevNode`<br/>`CM_Disable_DevNode`<br/>SetupAPI: `DIF_PROPERTYCHANGE` | None | Toggle radio transceiver directly without external settings | **VERIFIED INTERNAL (100% Native)** |
| **Classic Device Discovery** | Issues 2.4 GHz radio inquiry burst for discoverable devices | Win32: `BluetoothFindFirstDevice`<br/>`BluetoothFindNextDevice`<br/>(`fIssueInquiry = true`) | None | Perform radio inquiry internally and report nearby devices | **VERIFIED INTERNAL (100% Native)** |
| **BLE Advertising Discovery** | Listens for BLE advertisement packets | WinRT: `BluetoothLEAdvertisementWatcher`<br/>(`Active` scanning mode) | None | Intercept BLE advertising payloads and signal RSSI | **VERIFIED INTERNAL (100% Native)** |
| **Paired Devices Query** | Enumerates remembered devices via PnP and cache | PnP: `SetupDiGetClassDevs`<br/>Win32: `BluetoothFindFirstDevice` | None | Enumerate paired devices without launching external settings | **VERIFIED INTERNAL (100% Native)** |
| **Device Class Decoding** | Decodes 24-bit CoD integer into major, minor, icon | Domain bitmask algorithm (`DeviceClassDecoder.cs`) | None | Classify peripheral type internally | **VERIFIED INTERNAL (100% Native)** |
| **Classic Device Pairing** | Authenticates peripheral with optional PIN | Win32: `BluetoothAuthenticateDevice`<br/>(`bthprops.cpl`) | None | Execute native PIN pairing within application modal | **VERIFIED INTERNAL (100% Native)** |
| **BLE Device Pairing** | Pairs BLE peripheral with custom pairing handler | WinRT: `BluetoothLEDevice.FromBluetoothAddressAsync`<br/>`DeviceInformationCustomPairing` | None | Handle PIN and Numeric Comparison in-app | **VERIFIED INTERNAL (100% Native)** |
| **Device Unpairing** | Removes cryptographic bond from Windows registry | Win32: `BluetoothRemoveDevice`<br/>(`bthprops.cpl`) | None | Remove paired peripheral bond internally | **VERIFIED INTERNAL (100% Native)** |
| **Audio Connection (A2DP/HFP)**| Binds Audio Sink, Handsfree, and AVRCP profiles | Win32: `BluetoothSetServiceState`<br/>(`BLUETOOTH_SERVICE_ENABLE`) | None | Connect audio endpoints via Windows Bluetooth stack | **VERIFIED INTERNAL (100% Native)** |
| **HID Connection (Input)** | Binds Human Interface Device profile | Win32: `BluetoothSetServiceState`<br/>(`BLUETOOTH_SERVICE_ENABLE`) | None | Bind mouse/keyboard driver to input subsystem | **VERIFIED INTERNAL (100% Native)** |
| **RFCOMM / Serial Connection**| Binds Serial Port Profile (SPP) | Win32: `BluetoothSetServiceState`<br/>(`BLUETOOTH_SERVICE_ENABLE`) | None | Bind virtual COM/RFCOMM channel | **VERIFIED INTERNAL (100% Native)** |
| **BLE GATT Connection** | Simulated connection in Orchestrator | None (Assigns `device.IsConnected = true`) | None | Establish genuine GATT session via WinRT | **DEFECT: BD-HIGH-002 (Mocked Connection)** |
| **GATT Service Discovery** | Loads static profile catalog (`GattProfileCatalog`) | None (Uses hardcoded catalog) | None | Query live GATT services via `GetGattServicesAsync` | **DEFECT: BD-HIGH-001 (Mocked GATT)** |
| **GATT Characteristic Read** | Returns fake success or device name | None (Synthetic string formatting) | None | Read live bytes via `ReadValueAsync()` | **DEFECT: BD-HIGH-001 (Mocked GATT)** |
| **GATT Characteristic Write** | Shows confirmation dialog, logs fake success | None (No byte buffer sent) | None | Write buffer via `WriteValueWithResultAsync()` | **DEFECT: BD-HIGH-001 (Mocked GATT)** |
| **Audio Subsystem Inspection** | Queries sound endpoints and detects HFP contention | WMI: `SELECT * FROM Win32_SoundDevice` | None | Inspect Windows audio routing internally | **VERIFIED INTERNAL (100% Native)** |
| **Packet Capture (HCI)** | Records ETW Bluetooth trace | Command: `netsh.exe trace start scenario=Bluetooth` | Background `netsh.exe` | Capture ETL trace without launching Wireshark | **VERIFIED CLI INTEGRATION** |
| **Service Restart** | Stops and restarts `bthserv` / `BTAGService` | SCM: `System.ServiceProcess.ServiceController` | None | Restart Windows services internally with UAC | **VERIFIED INTERNAL (100% Native)** |
| **Radio Cycle Action** | Restarts `bthserv` instead of cycling radio | SCM: `ServiceController` | None | Power-cycle devnode via `HardwareRadioManager` | **DEFECT: BD-HIGH-004 (Inaccurate Action)** |
| **Device Manager Launch** | Opens `devmgmt.msc` for manual driver operations | `Process.Start("devmgmt.msc")` | MMC (`devmgmt.msc`) | User-requested external tool launch | **EXPECTED BEHAVIOR** |
| **Windows Settings Launch** | Opens Settings page when requested by user | `Process.Start("ms-settings:bluetooth")` | Windows Settings | User-requested external shortcut | **EXPECTED BEHAVIOR** |
| **Troubleshooter Launch** | Invokes deprecated `msdt.exe` | `Process.Start("msdt.exe /id BluetoothDiagnostic")` | Windows MSDT / Settings | Launch troubleshooter on user request | **DEFECT: BD-MED-006 (MSDT Deprecation)** |

---

## 3. Deep Dive into Specific Bluetooth Capabilities

### 3.1 Adapter Management & Hardware Inspection
- **Win32 APIs Used:** `SetupDiGetClassDevs(GUID_DEVCLASS_BLUETOOTH)`, `SetupDiEnumDeviceInfo`, `SetupDiGetDeviceRegistryProperty`, `CM_Get_DevNode_Status`.
- **Operating System Reality:** Windows does not provide a single unified API that reports all adapter attributes. SetupAPI must be combined with Configuration Manager (`cfgmgr32.dll`) to distinguish physically connected adapters from phantom/disconnected device nodes.
- **BlueDoc Implementation:** Implemented with high technical precision in `SetupApiHardwareInspector.cs`. Handles multi-adapter setups, separates USB dongles from PCI internal cards, extracts Vendor ID and Product ID, maps them to known manufacturer databases (Realtek, Intel, Broadcom, MediaTek, Qualcomm), and reads driver version and date from the registry.

### 3.2 Bluetooth Radio Diagnostics & Power Management
- **Win32 APIs Used:** `BluetoothFindFirstRadio`, `BluetoothGetRadioInfo`, `BluetoothFindRadioClose`, `CloseHandle`.
- **Operating System Reality:** Windows 7 SP1 has no concept of a software "Radio Toggle" API in user mode. Windows 8.1 and 10 introduced `Windows.Devices.Radios.Radio`, but this API requires specific package identity or UWP capabilities to control radio state from desktop applications.
- **BlueDoc Implementation:** BlueDoc solves this cross-platform challenge brilliantly in `HardwareRadioManager.cs` by controlling the hardware device node directly via `CfgMgr32.CM_Disable_DevNode` / `CM_Enable_DevNode` and SetupAPI `DIF_PROPERTYCHANGE`. This works universally across Windows 7, 10, and 11 when running with Administrator privileges.

### 3.3 Device Discovery (Classic vs. Low Energy)
- **Classic Bluetooth:**
  - Uses `BluetoothFindFirstDevice` and `BluetoothFindNextDevice` with `searchParams.fIssueInquiry = true`.
  - Configures `cTimeoutMultiplier` to control inquiry duration (each unit = 1.28 seconds).
  - Handles timeout gracefully and reports discovered devices asynchronously via `IProgress<BluetoothDeviceInfo>`.
- **Bluetooth Low Energy (BLE):**
  - Uses `Windows.Devices.Bluetooth.Advertisement.BluetoothLEAdvertisementWatcher`.
  - Configured with `ScanningMode = BluetoothLEScanningMode.Active` to request Scan Response packets (Local Name).
  - Correctly extracts RSSI in dBm from `args.RawSignalStrengthInDBm`.
  - Handles deduplication and in-place property updates on existing device items.

### 3.4 Device Pairing & Authentication Handshake
- **Classic Bluetooth Pairing:**
  - Implemented in `DeviceManagementService.PairDeviceAsync` lines 220-270.
  - Calls `BluetoothNative.BluetoothAuthenticateDevice(IntPtr.Zero, hRadio, ref devInfo, pin, pinLen)`.
  - Handles standard PIN authentication and passkey workflows.
  - Successfully decodes Win32 error returns: `1244` (Authentication Rejected / PIN Mismatch), `1223` (Operation Cancelled by user).
- **BLE Custom Pairing:**
  - Implemented in `BlePairingProvider.PairBleDeviceAsync` lines 20-130.
  - Uses WinRT `BluetoothLEDevice.FromBluetoothAddressAsync()` and `DeviceInformationCustomPairing`.
  - Hooks `PairingRequested` event with deferral support.
  - Supports `ConfirmOnly`, `ProvidePin`, and `ConfirmPinMatch` (numeric comparison).
  - Tested and verified in automated unit test suite (`FixConnectionDoctorAndPairingTests.cs`).

### 3.5 Connection Management (Profiles vs. Universal Connection)
- **The Windows Platform Reality:** Windows does NOT have a universal "Connect Bluetooth Device" API. Connecting a Bluetooth device requires binding specific service profiles at the driver layer:
  - Audio: A2DP Sink (`0000110b...`), HFP Handsfree (`0000111e...`), HSP Headset (`00001108...`), AVRCP (`0000110e...`).
  - Input: Human Interface Device (HID) (`00001124...`).
  - Serial: RFCOMM / SPP (`00001101...`).
- **BlueDoc Implementation:**
  - `DeviceManagementService.ConnectDeviceAsync` calls `BluetoothNative.BluetoothSetServiceState(hRadio, ref devInfo, ref guid, BLUETOOTH_SERVICE_ENABLE)`.
  - `BluetoothConnectionOrchestrator.cs` accurately differentiates protocol paths based on peripheral classification.
  - For audio devices, it binds all 4 audio GUIDs and analyzes Windows sound endpoints via `BluetoothAudioInspector.cs`.
  - **Defect Found (BD-HIGH-002):** In `BluetoothConnectionOrchestrator.ConnectBlePeripheralAsync` (lines 244-261), it simulates success for BLE connections without calling WinRT APIs.

### 3.6 Advanced BLE GATT Explorer (Critical Defect Analysis)
- **PRD Requirement (FR-16):** Discover BLE services, enumerate GATT characteristics and descriptors, read supported characteristics, write characteristics with confirmation, subscribe to notifications.
- **Actual Codebase Implementation:**
  - `BleExplorerViewModel.cs` line 164 loads a **hardcoded static catalog** (`GattProfileCatalog.GetStandardPeripheralProfile`) based on device category.
  - When the user clicks "Read", `ReadCharacteristic()` (lines 200-232) inspects if the UUID contains "2A00" or "2A01" and returns the device name or category, or outputs a fake string: `Verified GATT Read at {timestamp}: [GattCommunicationStatus.Success]`. It NEVER reads bytes from the device.
  - When the user clicks "Write", `PerformSafeWrite()` (lines 267-297) shows a confirmation dialog and then logs a fake success message without sending any byte buffer.
  - When the user clicks "Subscribe", `ToggleNotification()` (lines 234-265) toggles a local boolean without writing to the peripheral's Client Characteristic Configuration Descriptor (CCCD).
- **Audit Verdict:** **CONFIRMED DEFECT (BD-HIGH-001).** The BLE Explorer is a visual prototype/placeholder that does not execute genuine GATT operations against live hardware.

---

## 4. Platform Limitations & Windows OS Boundaries

| Capability | Windows 7 SP1 | Windows 10 | Windows 11 | Technical Explanation |
|---|---|---|---|---|
| **Classic Bluetooth Inquiry** | Supported | Supported | Supported | Available via `bthprops.cpl!BluetoothFindFirstDevice`. |
| **Win32 Classic Pairing** | Supported | Supported | Supported | Available via `bthprops.cpl!BluetoothAuthenticateDevice`. |
| **PnP Profile Binding** | Supported | Supported | Supported | Available via `bthprops.cpl!BluetoothSetServiceState`. |
| **WinRT BLE Watcher** | **UNSUPPORTED** | Supported | Supported | Requires `Windows.Devices.Bluetooth.Advertisement` (introduced in Win10 Build 10240). |
| **WinRT BLE Custom Pairing** | **UNSUPPORTED** | Supported | Supported | Requires `DeviceInformationCustomPairing` (introduced in Win10 Build 10586). |
| **WinRT GATT Client** | **UNSUPPORTED** | Supported | Supported | Requires `Windows.Devices.Bluetooth.GenericAttributeProfile` (Win10 Build 15063+). |
| **Hardware DevNode Power Cycle**| Supported | Supported | Supported | Works via `cfgmgr32.dll` and SetupAPI on all versions when elevated. |
| **MSDT Diagnostic Troubleshooter**| Supported | Supported | **DEPRECATED / REMOVED** | Microsoft has removed `msdt.exe` in Windows 11 23H2/24H2. |

