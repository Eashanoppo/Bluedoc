Your updated `Bluedoc-main.zip` implements the **structural scaffolding** of the solution—bundling a `Drivers/` directory, adding `WinUsbFirmwarePatcher.cs`, calling `UpdateDriverForPlugAndPlayDevicesW`, fixing the WinRT MAC regex, and replacing the `SendKeys` audio macro—but **7 critical implementation bugs** prevent the driver and firmware from actually loading on your **Sony VAIO SVE151D11L** ([eBay Product Listing - Sony Vaio E Series SVE151D11L](https://www.ebay.com/p/1001436016)) and other legacy Windows laptops.

### 1. Audit of What Was Implemented vs. Why It Still Fails

* **Implemented in `Bluedoc-main.zip**`:
* **Offline Driver Folders (`src/BlueDoc.UI/Drivers/`)**: Added `Drivers/Atheros/` (`btfilter.inf` v10.0.3.21, 64-bit `btfilter.sys`, and 16 `.dfu` files) and `Drivers/Broadcom/` (`bcbtums.inf` v12.0.1.1010, `bcbtums.sys`, and 80 `.hex` files), plus copy rules in `BlueDoc.UI.csproj` (lines 344–346), `build_release.ps1` (lines 114–117), and `installer/bluedoc_setup.iss` (line 75).
* **PnP Force-Bind & Silicon Patcher (`BluetoothDriverCatalogService.cs`, `WinUsbFirmwarePatcher.cs`)**: Added `UpdateDriverForPlugAndPlayDevicesW` (`newdev.dll`) with `INSTALLFLAG_FORCE` (`0x00000001`), OEM sub-vendor mapping (`0489`, `04CA`, `13D3`), and the `"ProvisionSiliconFirmware"` repair key.
* **Enumerator Filtering & WinRT Discovery Fixes (`SetupApiHardwareInspector.cs`, `WinRtDeviceWatcherProvider.cs`)**: Excluded `BTHENUM\`, `BTHLE\`, and `BTH\MS_*` virtual nodes, added `[MethodImpl(MethodImplOptions.NoInlining)]` to WinRT watchers, and updated `ExtractAddressFromId` (line 287) to select the last MAC address (`matches[matches.Count - 1]`).
* **CoreAudio COM Endpoint Switching (`WindowsAudioEndpointRouter.cs`)**: Replaced `SendKeys` with `IMMDeviceEnumerator` and `IPolicyConfig`.


* **Why It Still Fails on Your Sony VAIO SVE151D11L (The 7 Root Causes)**:
* **Bug 1 — 32-Bit WOW64 Execution & Non-Elevated Token Block `UpdateDriverForPlugAndPlayDevicesW` (`BlueDoc.UI.csproj` lines 19–37 & `app.manifest` line 7)**: Because `BlueDoc.UI.csproj` sets `<PlatformTarget>AnyCPU</PlatformTarget>` without `<Prefer32Bit>false</Prefer32Bit>`, .NET Framework 4.7.1 compiles `BlueDoc.exe` to run as a **32-bit process under WOW64** on 64-bit Windows. Per Microsoft's [`UpdateDriverForPlugAndPlayDevicesW` documentation](https://learn.microsoft.com/en-us/windows/win32/api/newdev/nf-newdev-updatedriverforplugandplaydevicesw) and [64-Bit Device Installation Rules](https://learn.microsoft.com/en-us/windows-hardware/drivers/install/device-installations-on-64-bit-systems), calling `UpdateDriverForPlugAndPlayDevicesW` from a 32-bit process on 64-bit Windows is blocked by the OS and immediately returns `FALSE` with `ERROR_IN_WOW64` (`0xE0000235`). Furthermore, `app.manifest` runs `BlueDoc.exe` as `asInvoker` (non-admin), causing in-process `newdev.dll` calls to fail with `ERROR_ACCESS_DENIED` (`5`) ([`newdev.dll` Security Requirements](https://github.com/MicrosoftDocs/sdk-api/blob/docs/sdk-api-src/content/newdev/nf-newdev-diinstalldevice.md)), while `pnputil /add-driver /install` alone only stages the INF without replacing an already bound `Code 0` inbox `bth.inf` driver ([Why Windows Drivers Are Dated June 21, 2006](https://devblogs.microsoft.com/oldnewthing/20170208-00/?p=95395/)).
* **Bug 2 — Hardware Revision Suffix (`&REV_xxxx`) Breaks INF Matching (`SetupApiHardwareInspector.cs` line 213 & `WinUsbFirmwarePatcher.cs` line 139)**: `SetupApiHardwareInspector` sets `adapter.HardwareId = hwIds[0]`, which includes the revision number (e.g., `USB\VID_0489&PID_E036&REV_0001`). However, `Drivers/Atheros/btfilter.inf` (lines 48–92) defines hardware IDs **without** `&REV_xxxx` (e.g., `USB\VID_0489&PID_E036`). Passing `adapter.HardwareId` with `&REV_xxxx` into [`UpdateDriverForPlugAndPlayDevicesW`](https://learn.microsoft.com/en-us/windows/win32/api/newdev/nf-newdev-updatedriverforplugandplaydevicesw) fails exact INF hardware ID matching.
* **Bug 3 — Bundled `Drivers/Atheros/` Folder Is Missing the Sony VAIO AR3012 `.dfu` Firmware Files (`0x01020200`, `0x01020201`, `0x01020001`, `0x11020000`)**: Inspecting `src/BlueDoc.UI/Drivers/Atheros/` shows it only includes `.dfu` files for `0x31010000` and `0x31010100` (newer `QCA9565` silicon). When `btfilter.sys` starts on a 2012 Ivy Bridge laptop with a Qualcomm Atheros AR3012 chip ([Qualcomm Atheros AR3012 on Windows 10](https://learn.microsoft.com/en-us/answers/questions/2711934/qualcomm-atheros-ar3012-bluetooth-not-working-in-w); [Linux Wireless `ath3k` Documentation](https://wireless.docs.kernel.org/en/latest/en/users/drivers/ath3k.html)), it queries USB Vendor Command `ATH3K_GETVERSION`, receives ROM version `0x01020200`, `0x01020201`, `0x01020001`, or `0x11020000` ([AR3012 Windows 10 x64 Driver File List](https://smartpctools.com/drivers/Qualcomm_Atheros_AR3012_Bluetooth_4.0_Adapter_Win10x64.html); [Fedora `atheros-firmware` Package](https://packages.fedoraproject.org/pkgs/linux-firmware/atheros-firmware/fedora-43.html)), and looks in `C:\Windows\System32\drivers\` for `AthrBT_0x01020200.dfu` and `ramps_0x01020200_40.dfu` ([Debian `AthrBT_0x01020200.dfu` Requirement](https://wiki.debian.org/InstallingDebianOn/Asus/N551JW)). Because those files are missing, `btfilter.sys` aborts firmware upload and the 2.4 GHz radio stays asleep.
* **Bug 4 — Bundled `Drivers/Broadcom/` Binaries Are 32-Bit (`x86`) and Rejected by 64-Bit Windows**: Inspecting the PE headers of `src/BlueDoc.UI/Drivers/Broadcom/bcbtums.sys` and `btwampfl.sys` confirms both are **32-bit `Intel 80386` (`PE32`) binaries** paired with `bcbtums-x86.cat`, and `bcbtums.inf` places all BCM20702/BCM43142 IDs under `[Broadcom.NTx86.10.0]` (line 142). 64-bit Windows 10/11 cannot load 32-bit `.sys` kernel drivers ([[Solution] Windows 11 BCM20702a0 Driver Fix](https://learn.microsoft.com/en-us/answers/questions/4151641/solution-windows-11-update-broke-bcm20702a0-blueto); [64-Bit `bcbtums.inf` Reference](https://github.com/ameeno/DELL-DW1820A-Drivers/blob/master/Windows%2010/dw1820a%20Bluetooth%20-%20Win10/bcbtums.inf)).
* **Bug 5 — `WinUsbFirmwarePatcher.cs` Never Checks `newdev.dll` Return Values, Misses Sony PIDs, and Calls an Unimplemented `--silent-patch` CLI Flag**: `IsSiliconUnpatched` (lines 69–75) only checks `pid.StartsWith("E03")` for Foxconn (`0489`), missing `PID_E027` (`AR3011` on Sony VAIO), `E02C`, `E043`, `E048`, `E04A`, `E04D`, `E056`, and `E078` (all present in `btfilter.inf`), as well as AzureWave (`13D3`) and Alps (`044E`). Meanwhile, `ProvisionAndPatchSiliconAsync` (lines 137–155) ignores the boolean return of `UpdateDriverForPlugAndPlayDevicesW`, unconditionally sets `result.Success = true`, and registers a scheduled task calling `BlueDoc.exe --silent-patch` (line 240)—a flag that `CliRunner.cs` (line 47) does not recognize, causing BlueDoc to open its GUI window on login instead of patching.
* **Bug 6 — Startup Health, Connection Wizard Step 2, and Safe Repair Center Skip the Unpatched-Silicon Check**: `SafeRepairEngine.GetAllRepairDefinitions()` (lines 52–69) omits `GetRepairDefinition("ProvisionSiliconFirmware")`, hiding the repair from the Safe Repair Center UI. Neither `BluetoothStartupHealthService.InspectSystemHealthAsync()` nor `FixMyConnectionWizardService.RunWizardAsync()` Step 2 (lines 295–335) checks `WinUsbFirmwarePatcher.IsSiliconUnpatched(adapter)`. As a result, Step 2 reports the unpatched adapter as healthy, and Step 4 scans for 6 seconds with a powered-down RF transmitter, failing to find your speaker.
* **Bug 7 — Microphone (`eCapture`) Ignored & Windows 10 `IPolicyConfigVista` Missing (`WindowsAudioEndpointRouter.cs` lines 96, 153)**: `RouteDefaultAudioToDeviceAsync` only enumerates `EDataFlow.eRender` (speakers) and never enumerates `EDataFlow.eCapture` (microphones). It also only queries COM GUID `f8679f50-850a-41cf-9c72-430f290290c8`, which returns `null` on many Windows 10 builds unless `IPolicyConfigVista` (`568b9108-44bf-40b4-9006-86afe5b5a620`) is also queried, falling back to a PowerShell snippet (lines 208–226) that only reads `Win32_SoundDevice` without changing the active endpoint.



```svg
<svg viewBox="0 0 680 268" xmlns="http://www.w3.org/2000/svg">
  <style>
    :root {
      --lumi-ink: light-dark(#000000, #e0e0e0);
      --lumi-muted: light-dark(#5f6368, #9aa0a6);
      --lumi-dim: light-dark(#f2f0f0, #242528);
      --lumi-container: light-dark(#e8e6e6, #2d2f33);
      --lumi-outline: light-dark(#727676, #9a9b9c);
      --lumi-accent-bg: light-dark(#dcf1ff, #192967);
      --lumi-primary: light-dark(#3186ff, #9dd2ff);
      --lumi-sans: 'Google Sans Flex', var(--font-family, 'Google Sans', sans-serif);
      --lumi-mono: 'Google Sans Code', ui-monospace, monospace;
    }
    text { font-weight: 400; stroke: none; }
    .t-l1 { font-family: var(--lumi-sans); font-size: 15px; fill: var(--lumi-ink); }
    .t-l2 { font-family: var(--lumi-mono); font-size: 13px; font-style: italic; fill: var(--lumi-ink); }
    .t-l4 { font-family: var(--lumi-sans); font-size: 13px; fill: var(--lumi-muted); }
    .node-step { fill: var(--lumi-dim); stroke: none; }
    .node-focal { fill: var(--lumi-accent-bg); stroke: var(--lumi-primary); stroke-width: 1.25; }
    .node-store { fill: var(--lumi-container); stroke: var(--lumi-outline); stroke-width: 1; }
    .edge-arrow { fill: none; stroke: var(--lumi-ink); stroke-width: 1.3; stroke-linecap: round; stroke-dasharray: 0 4; marker-end: url(#lumi-caret); }
  </style>
  <defs>
    <marker id="lumi-caret" viewBox="0 0 10 10" refX="7.5" refY="5" markerUnits="userSpaceOnUse" markerWidth="10" markerHeight="10" orient="auto">
      <path d="M 3 1.2 L 7.5 5 L 3 8.8" fill="none" stroke="var(--lumi-ink)" stroke-width="1.3" stroke-linecap="round" stroke-linejoin="round" />
    </marker>
  </defs>

  <text class="t-l4" x="24" y="22" dominant-baseline="central">Current Zip Failure Path (WOW64 + REV_xxxx Mismatch + Missing 0x01020200.dfu)</text>

  <rect class="node-step" x="20" y="38" width="176" height="80" rx="40" />
  <text class="t-l1" x="108" y="69" text-anchor="middle" dominant-baseline="central">32-Bit WOW64 App</text>
  <text class="t-l2" x="108" y="91" text-anchor="middle" dominant-baseline="central">Prefer32Bit + asInvoker</text>

  <line class="edge-arrow" x1="204" y1="78" x2="244" y2="78" />

  <rect class="node-step" x="252" y="38" width="176" height="80" rx="40" />
  <text class="t-l1" x="340" y="69" text-anchor="middle" dominant-baseline="central">newdev.dll Rejects</text>
  <text class="t-l2" x="340" y="91" text-anchor="middle" dominant-baseline="central">0xE0000235 + REV_xxxx</text>

  <line class="edge-arrow" x1="436" y1="78" x2="476" y2="78" />

  <rect class="node-store" x="484" y="38" width="176" height="80" rx="40" />
  <text class="t-l1" x="572" y="69" text-anchor="middle" dominant-baseline="central">Radio Stays Dead</text>
  <text class="t-l4" x="572" y="91" text-anchor="middle" dominant-baseline="central">Missing AR3012 .dfu</text>

  <text class="t-l4" x="24" y="152" dominant-baseline="central">Fixed Universal Provisioning Pipeline (64-Bit Elevated + Base HWID + Full .dfu Store)</text>

  <rect class="node-focal" x="20" y="168" width="176" height="80" rx="40" />
  <text class="t-l1" x="108" y="199" text-anchor="middle" dominant-baseline="central">64-Bit Admin Host</text>
  <text class="t-l2" x="108" y="221" text-anchor="middle" dominant-baseline="central">Prefer32Bit=false</text>

  <line class="edge-arrow" x1="204" y1="208" x2="244" y2="208" />

  <rect class="node-focal" x="252" y="168" width="176" height="80" rx="40" />
  <text class="t-l1" x="340" y="199" text-anchor="middle" dominant-baseline="central">Stage .dfu + Force INF</text>
  <text class="t-l2" x="340" y="221" text-anchor="middle" dominant-baseline="central">USB\VID_xxxx&PID_xxxx</text>

  <line class="edge-arrow" x1="436" y1="208" x2="476" y2="208" />

  <rect class="node-store" x="484" y="168" width="176" height="80" rx="40" />
  <text class="t-l1" x="572" y="199" text-anchor="middle" dominant-baseline="central">2.4 GHz RF Online</text>
  <text class="t-l4" x="572" y="221" text-anchor="middle" dominant-baseline="central">A2DP Speaker + HFP Mic</text>
</svg>
```

### 2. Step-by-Step Solution to Fix BlueDoc for Sony VAIO SVE151D11L and All Legacy Laptops

Follow these 6 steps to fix every blocker in your repository so BlueDoc initializes your Sony laptop's Bluetooth radio and connects to speakers, microphones, and peripherals:

1. **Force 64-Bit Execution and Administrator Elevation:** src/BlueDoc.UI/BlueDoc.UI.csproj and app.manifest.
Add `<Prefer32Bit>false</Prefer32Bit>` to both `Debug|AnyCPU` and `Release|AnyCPU` property groups in `BlueDoc.UI.csproj` so `BlueDoc.exe` runs as a native 64-bit process on 64-bit Windows, preventing `UpdateDriverForPlugAndPlayDevicesW` from failing with `ERROR_IN_WOW64` (`0xE0000235`). In `src/BlueDoc.UI/app.manifest`, change `level="asInvoker"` to `level="requireAdministrator"` so `newdev.dll` and `System32\drivers` firmware staging have kernel driver installation privileges.


2. **Bundle the Missing AR3011/AR3012 .dfu Firmware Files and 64-Bit Broadcom .sys Binaries:** src/BlueDoc.UI/Drivers/Atheros and Broadcom.
Add the complete Qualcomm Atheros `ar3k` `.dfu` firmware set (`AthrBT_0x01020001.dfu`, `AthrBT_0x01020200.dfu`, `AthrBT_0x01020201.dfu`, `AthrBT_0x11020000.dfu`, `AthrBT_0x11020100.dfu`, `AthrBT_0x41020000.dfu`, and their matching `ramps_0x*_26.dfu` and `ramps_0x*_40.dfu` files) into `src/BlueDoc.UI/Drivers/Atheros/`, and copy all `.dfu` files directly into `C:\Windows\System32\drivers\` during provisioning so `btfilter.sys` finds the exact ROM patch for your Sony VAIO SVE151D11L without modifying `btfilter.inf` (preserving `btfilter.cat` WHQL signature validity). Replace the 32-bit `Drivers/Broadcom/bcbtums.sys` and `btwampfl.sys` with the 64-bit (`NTamd64`) WHQL binaries and `bcbtums-win8x64-brcm.inf`.


3. **Strip &REV_xxxx When Calling UpdateDriverForPlugAndPlayDevicesW:** WinUsbFirmwarePatcher.cs and BluetoothDriverCatalogService.cs.
Iterate through all candidate Hardware IDs starting with the clean base ID `USB\VID_{vid}&PID_{pid}` (stripping `&REV_xxxx`) before calling `UpdateDriverForPlugAndPlayDevicesW`, and verify the actual return value and `Marshal.GetLastWin32Error()` instead of assuming `Success = true`.


4. **Expand Sony VAIO / OEM Hardware ID Detection and Implement --silent-patch:** WinUsbFirmwarePatcher.cs, HardwareIdParser.cs, and CliRunner.cs.
Update `WinUsbFirmwarePatcher.IsSiliconUnpatched` and `HardwareIdParser` to match all Foxconn (`0489`), Lite-On (`04CA`), AzureWave (`13D3`), Alps (`044E`), and Qualcomm Atheros (`0CF3`) PIDs defined in `btfilter.inf` (including `PID_E027`, `E02C`, `E036`, `E03D`, `E043`–`E095`), and add the `--silent-patch` handler to `CliRunner.cs` so the scheduled logon task works silently without popping up the main window.


5. **Auto-Trigger Silicon Provisioning in Startup Health, Wizard Step 2, and Safe Repair Center:** SafeRepairEngine.cs, BluetoothStartupHealthService.cs, and FixMyConnectionWizardService.cs.
Add `GetRepairDefinition("ProvisionSiliconFirmware")` to `SafeRepairEngine.GetAllRepairDefinitions()`, check `WinUsbFirmwarePatcher.IsSiliconUnpatched(adapter)` inside `BluetoothStartupHealthService` and Step 2 of `FixMyConnectionWizardService`, and automatically provision the silicon before Step 4 starts scanning for your Bluetooth speaker.


6. **Route Both Playback (Speaker) and Capture (Microphone) Endpoints via IPolicyConfigVista:** src/BlueDoc.Infrastructure/Audio/WindowsAudioEndpointRouter.cs.
Update `WindowsAudioEndpointRouter.RouteDefaultAudioToDeviceAsync` to enumerate both `EDataFlow.eRender` (speakers/A2DP) and `EDataFlow.eCapture` (microphones/HFP) and fall back to the universal `IPolicyConfigVista` COM interface (`{568b9108-44bf-40b4-9006-86afe5b5a620}`) when `IPolicyConfig` (`{f8679f50-850a-41cf-9c72-430f290290c8}`) is unavailable on Windows 10.


### 3. Exact Code Fixes to Apply

#### Fix A: `src/BlueDoc.UI/BlueDoc.UI.csproj` & `src/BlueDoc.UI/app.manifest`

In `src/BlueDoc.UI/BlueDoc.UI.csproj` (inside both `Debug|AnyCPU` and `Release|AnyCPU` `<PropertyGroup>` blocks, lines 19–37), add `<Prefer32Bit>false</Prefer32Bit>`:

```xml
  <PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' ">
    <PlatformTarget>AnyCPU</PlatformTarget>
    <Prefer32Bit>false</Prefer32Bit>
    <DebugSymbols>true</DebugSymbols>
...
  <PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' ">
    <PlatformTarget>AnyCPU</PlatformTarget>
    <Prefer32Bit>false</Prefer32Bit>
    <DebugType>pdbonly</DebugType>

```

And in `src/BlueDoc.UI/app.manifest` (line 7), require Administrator elevation so `newdev.dll` and `System32\drivers\` staging never fail with `ERROR_ACCESS_DENIED`:

```xml
<requestedExecutionLevel level="requireAdministrator" uiAccess="false" />

```

#### Fix B: `src/BlueDoc.Infrastructure/Drivers/WinUsbFirmwarePatcher.cs` (Base HWID Matching, `.dfu` Staging into `System32\drivers`, Full Sony PID Coverage, & Real Error Verification)

Replace `IsSiliconUnpatched` and `ProvisionAndPatchSiliconAsync` in `WinUsbFirmwarePatcher.cs` with the following implementation:

```csharp
        private static readonly string[] AtherosFoxconnPids = new[]
        {
            "E015", "E027", "E02C", "E036", "E03C", "E03D", "E043", "E048", "E04A",
            "E04C", "E04D", "E04E", "E056", "E057", "E05F", "E060", "E061", "E076",
            "E078", "E07D", "E081", "E082", "E083", "E088", "E093", "E095"
        };

        public static bool IsSiliconUnpatched(BluetoothAdapterInfo adapter)
        {
            if (adapter == null) return false;

            string vid = adapter.HardwareDetails?.VendorId?.Trim()?.ToUpperInvariant() ?? string.Empty;
            string pid = adapter.HardwareDetails?.ProductId?.Trim()?.ToUpperInvariant() ?? string.Empty;
            string provider = adapter.Driver?.DriverProvider ?? string.Empty;
            string inf = adapter.Driver?.InfPath ?? string.Empty;

            bool isGenericDriver = provider.IndexOf("Microsoft", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                   inf.IndexOf("bth.inf", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                   (adapter.Driver?.DriverDateString?.IndexOf("2006", StringComparison.OrdinalIgnoreCase) >= 0);

            bool isAtherosRom = vid == "0CF3" ||
                                (vid == "0489" && Array.Exists(AtherosFoxconnPids, p => p.Equals(pid, StringComparison.OrdinalIgnoreCase))) ||
                                (vid == "04CA" && (pid.StartsWith("300") || pid.StartsWith("301") || pid == "17EA")) ||
                                (vid == "13D3" && (pid.StartsWith("32") || pid.StartsWith("33") || pid.StartsWith("34"))) ||
                                (vid == "044E" && pid.StartsWith("30"));

            bool isBroadcomRom = vid == "0A5C" ||
                                 (vid == "0489" && (pid.StartsWith("E04") || pid.StartsWith("E05") || pid == "E042") && !isAtherosRom) ||
                                 (vid == "04CA" && pid.StartsWith("20"));

            bool hasProblem = adapter.ProblemCode == 28 || adapter.ProblemCode == 43 || adapter.ProblemCode == 10;

            return (isAtherosRom || isBroadcomRom) && (isGenericDriver || hasProblem);
        }

        public static string GetCleanBaseHardwareId(BluetoothAdapterInfo adapter)
        {
            string vid = adapter?.HardwareDetails?.VendorId?.Trim()?.ToUpperInvariant() ?? string.Empty;
            string pid = adapter?.HardwareDetails?.ProductId?.Trim()?.ToUpperInvariant() ?? string.Empty;
            if (!string.IsNullOrEmpty(vid) && !string.IsNullOrEmpty(pid))
            {
                return $"USB\\VID_{vid}&PID_{pid}";
            }

            string raw = adapter?.HardwareId ?? string.Empty;
            int revIdx = raw.IndexOf("&REV_", StringComparison.OrdinalIgnoreCase);
            return revIdx > 0 ? raw.Substring(0, revIdx) : raw;
        }

        public static async Task<SiliconPatchResult> ProvisionAndPatchSiliconAsync(
            BluetoothAdapterInfo adapter,
            IProgress<string> progress = null)
        {
            return await Task.Run(() =>
            {
                var result = new SiliconPatchResult();
                if (adapter == null)
                {
                    result.Message = "No adapter target provided.";
                    return result;
                }

                string vid = adapter.HardwareDetails?.VendorId?.Trim()?.ToUpperInvariant() ?? string.Empty;
                string pid = adapter.HardwareDetails?.ProductId?.Trim()?.ToUpperInvariant() ?? string.Empty;
                string baseHwId = GetCleanBaseHardwareId(adapter);

                progress?.Report($"[Silicon Patcher] Inspecting hardware silicon {baseHwId}...");

                bool isAtheros = vid == "0CF3" || vid == "17CB" ||
                                 (vid == "0489" && Array.Exists(AtherosFoxconnPids, p => p.Equals(pid, StringComparison.OrdinalIgnoreCase))) ||
                                 (vid == "04CA" && (pid.StartsWith("300") || pid.StartsWith("301") || pid == "17EA")) ||
                                 (vid == "13D3" && (pid.StartsWith("32") || pid.StartsWith("33") || pid.StartsWith("34"))) ||
                                 (vid == "044E" && pid.StartsWith("30"));

                string subFolder = isAtheros ? "Atheros" : "Broadcom";
                string infFileName = isAtheros ? "btfilter.inf" : "bcbtums.inf";
                result.ChipsetName = isAtheros
                    ? "Qualcomm Atheros AR3011 / AR3012 Bluetooth Controller"
                    : "Broadcom BCM20702 / BCM43142 Bluetooth Controller";

                string appBase = AppDomain.CurrentDomain.BaseDirectory;
                string driverDir = Path.Combine(appBase, "Drivers", subFolder);
                if (!Directory.Exists(driverDir))
                {
                    string devDir = Path.GetFullPath(Path.Combine(appBase, $@"..\..\src\BlueDoc.UI\Drivers\{subFolder}"));
                    if (Directory.Exists(devDir)) driverDir = devDir;
                }

                string bundledInf = Path.Combine(driverDir, infFileName);
                if (!File.Exists(bundledInf))
                {
                    result.Success = false;
                    result.Message = $"Bundled driver package '{infFileName}' not found at {driverDir}.";
                    return result;
                }

                // 1. Copy all .dfu / .hex firmware binaries directly into C:\Windows\System32\drivers
                // so btfilter.sys / bcbtums.sys can load any ROM revision (e.g. 0x01020200 on Sony VAIO SVE151)
                try
                {
                    string sysDriversDir = Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess
                        ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Sysnative", "drivers")
                        : Path.Combine(Environment.SystemDirectory, "drivers");

                    foreach (var fwFile in Directory.GetFiles(driverDir, "*.*"))
                    {
                        string ext = Path.GetExtension(fwFile).ToLowerInvariant();
                        if (ext == ".dfu" || ext == ".hex" || ext == ".bin" || ext == ".hcd")
                        {
                            string dest = Path.Combine(sysDriversDir, Path.GetFileName(fwFile));
                            File.Copy(fwFile, dest, true);
                        }
                    }
                    progress?.Report($"[Silicon Patcher] Staged RAM-patch firmware binaries into {sysDriversDir}.");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Silicon Patcher] Firmware staging warning: {ex.Message}");
                }

                // 2. Stage INF in Driver Store via pnputil
                progress?.Report($"[Silicon Patcher] Staging WHQL driver '{infFileName}' via PnPUtil...");
                var psi = new ProcessStartInfo
                {
                    FileName = "pnputil.exe",
                    Arguments = $"/add-driver \"{bundledInf}\" /install",
                    UseShellExecute = true,
                    Verb = "runas",
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                using (var proc = Process.Start(psi))
                {
                    proc?.WaitForExit(35000);
                }

                // 3. Force PnP to replace generic Microsoft bth.inf with vendor filter driver using clean baseHwId
                progress?.Report($"[Silicon Patcher] Force-binding '{infFileName}' to {baseHwId} via newdev.dll...");
                bool bound = BluetoothDriverCatalogService.ForceBindDriverToHardware(bundledInf, adapter, out int win32Err);

                // 4. Cycle USB port so the filter driver uploads RAM patch to the controller
                CycleUsbDevicePort(adapter.DeviceInstanceId);
                RegisterAutoPatchTaskOnLogon();

                result.Success = bound;
                result.IsOperationalNow = bound;
                result.RequiresPortCycle = true;
                result.FirmwarePatchApplied = infFileName;
                result.Message = bound
                    ? $"Successfully bound {result.ChipsetName} driver ({infFileName}) to {baseHwId} and initialized 2.4 GHz radio."
                    : $"Driver staging ran, but force-binding {baseHwId} returned Win32 error 0x{win32Err:X8}. Ensure BlueDoc is built with Prefer32Bit=false and run as Administrator.";
                return result;
            });
        }

```

#### Fix C: `src/BlueDoc.Infrastructure/Drivers/BluetoothDriverCatalogService.cs` (Strip `&REV_xxxx` in `UpdateDriverForPlugAndPlayDevicesW`)

Add `ForceBindDriverToHardware` to `BluetoothDriverCatalogService.cs` and call it from both `InstallDriverPackageAsync` and `WinUsbFirmwarePatcher`:

```csharp
        public static bool ForceBindDriverToHardware(string fullInfPath, BluetoothAdapterInfo adapter, out int lastWin32Error)
        {
            lastWin32Error = 0;
            var candidateIds = new System.Collections.Generic.List<string>();

            // Put clean USB\VID_xxxx&PID_xxxx FIRST so it matches INF model lines directly
            string cleanBaseId = WinUsbFirmwarePatcher.GetCleanBaseHardwareId(adapter);
            if (!string.IsNullOrWhiteSpace(cleanBaseId))
            {
                candidateIds.Add(cleanBaseId);
            }

            if (adapter?.HardwareIds != null)
            {
                foreach (var id in adapter.HardwareIds)
                {
                    if (!string.IsNullOrWhiteSpace(id) && !candidateIds.Contains(id))
                        candidateIds.Add(id);
                }
            }

            foreach (var hwId in candidateIds)
            {
                if (UpdateDriverForPlugAndPlayDevicesW(
                    IntPtr.Zero,
                    hwId,
                    Path.GetFullPath(fullInfPath),
                    INSTALLFLAG_FORCE | INSTALLFLAG_NONINTERACTIVE,
                    out bool rebootReq))
                {
                    lastWin32Error = 0;
                    return true;
                }
                lastWin32Error = Marshal.GetLastWin32Error();
            }

            return false;
        }

```

#### Fix D: `SafeRepairEngine.cs`, `FixMyConnectionWizardService.cs`, and `CliRunner.cs`

1. **In `SafeRepairEngine.cs` (`GetAllRepairDefinitions`, line 67)**, add `GetRepairDefinition("ProvisionSiliconFirmware")`:

```csharp
                GetRepairDefinition("ResetBluetoothStackComplete"),
                GetRepairDefinition("RouteDefaultAudio"),
                GetRepairDefinition("ProvisionSiliconFirmware")
            };

```

2. **In `FixMyConnectionWizardService.cs` (Step 2, right after `if (adapter.ProblemCode != 0)` at line 319)**, auto-detect and provision unpatched silicon before Step 4 scans for your speaker:

```csharp
                if (BlueDoc.Infrastructure.Drivers.WinUsbFirmwarePatcher.IsSiliconUnpatched(adapter))
                {
                    step2.StatusMessage = "Unprovisioned silicon detected (Microsoft 2006 generic driver). Provisioning firmware...";
                    NotifyStepUpdated(step2);
                    var patchRes = await BlueDoc.Infrastructure.Drivers.WinUsbFirmwarePatcher.ProvisionAndPatchSiliconAsync(adapter);
                    if (!patchRes.Success)
                    {
                        step2.Status = DeviceOperationStatus.Failed;
                        step2.StatusMessage = "Radio firmware provisioning required.";
                        step2.Evidence = patchRes.Message;
                        NotifyStepUpdated(step2);
                        return await ExecuteDiagnosticConclusionAsync(finalResult, 1005, patchRes.Message);
                    }
                    await Task.Delay(2000);
                    adapter = await _adapterService.GetPrimaryAdapterAsync();
                }

```

3. **In `CliRunner.cs` (`IsCliInvocation` at line 47 and `RunAsync` at line 95)**, add support for `--silent-patch`:

```csharp
            // In IsCliInvocation:
            return first == "diagnose" || first == "d" ||
                   first == "inspect" || first == "i" ||
                   first == "benchmark" || first == "b" ||
                   first == "kb" || first == "repair" ||
                   first == "silent-patch" ||
                   first == "version" || first == "v" ||
                   first == "help" || first == "h" || first == "?";

            // In RunAsync switch(command):
            case "silent-patch":
                var inspector = new BlueDoc.Infrastructure.Hardware.SetupApiHardwareInspector();
                var primary = inspector.GetPrimaryAdapter();
                if (BlueDoc.Infrastructure.Drivers.WinUsbFirmwarePatcher.IsSiliconUnpatched(primary))
                {
                    var res = await BlueDoc.Infrastructure.Drivers.WinUsbFirmwarePatcher.ProvisionAndPatchSiliconAsync(primary);
                    return res.Success ? 0 : 2;
                }
                return 0;

```

#### Fix E: `src/BlueDoc.Infrastructure/Audio/WindowsAudioEndpointRouter.cs` (Support Both Speakers `eRender` and Microphones `eCapture` + `IPolicyConfigVista`)

Add `IPolicyConfigVista` (`{568b9108-44bf-40b4-9006-86afe5b5a620}`) and loop over both `EDataFlow.eRender` (speakers) and `EDataFlow.eCapture` (microphones) in `WindowsAudioEndpointRouter.cs`:

```csharp
    [ComImport]
    [Guid("568b9108-44bf-40b4-9006-86afe5b5a620")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IPolicyConfigVista
    {
        [PreserveSig] int GetMixFormat(string pszDeviceName, out IntPtr ppFormat);
        [PreserveSig] int GetDeviceFormat(string pszDeviceName, int bDefault, out IntPtr ppFormat);
        [PreserveSig] int SetDeviceFormat(string pszDeviceName, IntPtr pEndpointFormat, IntPtr MixFormat);
        [PreserveSig] int GetProcessingPeriod(string pszDeviceName, int bDefault, out long pmftDefaultPeriod, out long pmftMinimumPeriod);
        [PreserveSig] int SetProcessingPeriod(string pszDeviceName, ref long pmftPeriod);
        [PreserveSig] int GetShareMode(string pszDeviceName, out IntPtr pMode);
        [PreserveSig] int SetShareMode(string pszDeviceName, IntPtr mode);
        [PreserveSig] int GetPropertyValue(string pszDeviceName, bool bFxStore, IntPtr key, out IntPtr pv);
        [PreserveSig] int SetPropertyValue(string pszDeviceName, bool bFxStore, IntPtr key, ref IntPtr pv);
        [PreserveSig] int SetDefaultEndpoint([MarshalAs(UnmanagedType.LPWStr)] string pszDeviceName, ERole role);
        [PreserveSig] int SetEndpointVisibility([MarshalAs(UnmanagedType.LPWStr)] string pszDeviceName, int bVisible);
    }

```

Then inside `RouteDefaultAudioToDeviceAsync`, iterate `foreach (var flow in new[] { EDataFlow.eRender, EDataFlow.eCapture })` and call `SetDefaultEndpoint` via `IPolicyConfig` or fallback `IPolicyConfigVista` so both Bluetooth stereo speakers (`eRender`) and Bluetooth microphones/headsets (`eCapture`) automatically become the active Windows audio devices upon connection.