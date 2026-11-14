To make BlueDoc solve your Sony VAIO SVE151D11L problem automatically—and work universally on any Windows laptop or desktop—BlueDoc must stop relying on Windows' generic `bth.inf` fallback driver (`v10.0.19041.5848`, `06-21-2006`) and instead **become the firmware and driver provisioning layer itself**.

On Windows, Microsoft's built-in [`BthPort.sys` / `BthUsb.sys](https://learn.microsoft.com/en-us/windows-hardware/drivers/bluetooth/bluetooth-driver-stack)` already acts as a universal Bluetooth HCI driver **after** the chip's silicon is initialized. What is missing on your Sony SVE151D11L (and most older laptops) is the **vendor RAM-patch firmware** (`.dfu` for Qualcomm Atheros or `.hcd` for Broadcom) that must be uploaded to the chip every time Windows boots or wakes from sleep.

### How BlueDoc Can Solve This Universally (Two-Tier Engine)

```svg
<svg viewBox="0 0 680 288" xmlns="http://www.w3.org/2000/svg">
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
    .edge-seg { fill: none; stroke: var(--lumi-ink); stroke-width: 1.3; stroke-linecap: round; stroke-dasharray: 0 4; }
    .edge-arrow { fill: none; stroke: var(--lumi-ink); stroke-width: 1.3; stroke-linecap: round; stroke-dasharray: 0 4; marker-end: url(#lumi-caret); }
  </style>
  <defs>
    <marker id="lumi-caret" viewBox="0 0 10 10" refX="7.5" refY="5" markerUnits="userSpaceOnUse" markerWidth="10" markerHeight="10" orient="auto">
      <path d="M 3 1.2 L 7.5 5 L 3 8.8" fill="none" stroke="var(--lumi-ink)" stroke-width="1.3" stroke-linecap="round" stroke-linejoin="round" />
    </marker>
  </defs>

  <rect class="node-step" x="20" y="24" width="172" height="80" rx="40" />
  <text class="t-l1" x="106" y="55" text-anchor="middle" dominant-baseline="central">1. Scan All Buses</text>
  <text class="t-l2" x="106" y="77" text-anchor="middle" dominant-baseline="central">USB Class E0 + VID</text>

  <line class="edge-arrow" x1="200" y1="64" x2="246" y2="64" />

  <rect class="node-focal" x="254" y="24" width="172" height="80" rx="40" />
  <text class="t-l1" x="340" y="55" text-anchor="middle" dominant-baseline="central">2. Detect Unpatched</text>
  <text class="t-l4" x="340" y="77" text-anchor="middle" dominant-baseline="central">bth.inf or Code 28</text>

  <line class="edge-seg" x1="434" y1="64" x2="442" y2="64" />
  <rect x="446" y="54" width="44" height="20" rx="10" fill="var(--lumi-dim)" />
  <text class="t-l2" x="468" y="64" text-anchor="middle" dominant-baseline="central">WHQL</text>
  <line class="edge-arrow" x1="494" y1="64" x2="504" y2="64" />

  <rect class="node-store" x="512" y="24" width="148" height="80" rx="40" />
  <text class="t-l1" x="586" y="55" text-anchor="middle" dominant-baseline="central">3A. Install .INF</text>
  <text class="t-l2" x="586" y="77" text-anchor="middle" dominant-baseline="central">newdev.dll Force</text>

  <line class="edge-seg" x1="340" y1="112" x2="340" y2="128" />
  <rect x="274" y="136" width="132" height="20" rx="10" fill="var(--lumi-dim)" />
  <text class="t-l2" x="340" y="146" text-anchor="middle" dominant-baseline="central">No OEM Driver</text>
  <line class="edge-arrow" x1="340" y1="164" x2="340" y2="180" />

  <rect class="node-store" x="254" y="188" width="172" height="80" rx="40" />
  <text class="t-l1" x="340" y="219" text-anchor="middle" dominant-baseline="central">3B. WinUSB Patch</text>
  <text class="t-l4" x="340" y="241" text-anchor="middle" dominant-baseline="central">Upload .dfu / .hcd</text>

  <line class="edge-arrow" x1="434" y1="228" x2="504" y2="228" />

  <rect class="node-step" x="512" y="188" width="148" height="80" rx="40" />
  <text class="t-l1" x="586" y="219" text-anchor="middle" dominant-baseline="central">4. Reset & Bind</text>
  <text class="t-l2" x="586" y="241" text-anchor="middle" dominant-baseline="central">BthUsb.sys Active</text>
</svg>
```

1. **Tier A — Automated VID/PID Driver Store Provisioning (Recommended Primary Path):**
BlueDoc inspects the exact `USB\VID_XXXX&PID_XXXX` of the chip, fetches or extracts the matching Microsoft-signed vendor filter driver + firmware package (from an embedded/CDN `.cab` repository), and forces Windows PnP to replace `bth.inf` using [`UpdateDriverForPlugAndPlayDevicesW`](https://learn.microsoft.com/en-us/windows/win32/api/newdev/nf-newdev-updatedriverforplugandplaydevicesw) (`newdev.dll`) with `INSTALLFLAG_FORCE`.
2. **Tier B — Universal User-Mode Firmware Injector (When Zero Signed Drivers Exist):**
For legacy chips that have no Windows 10/11 signed driver at all, BlueDoc uses [`libwdi`](https://github.com/pbatard/libwdi/wiki/Zadig/3b10a7560f35f6744eaf48630eb0075145001bf2) to temporarily bind Microsoft's built-in `WinUSB.sys` to the USB radio, uploads the chip's firmware patch (`.dfu`, `.hcd`, `.sfi`, `.bin`) over raw USB Control/Bulk endpoints, cycles the USB port, and hands the initialized HCI chip back to Microsoft's inbox `BthUsb.sys`.

---

### Exact Engineering Changes Required in BlueDoc

1. **Detect Unconfigured & Generic Adapters Across All Buses:** src/BlueDoc.Infrastructure/Hardware/SetupApiHardwareInspector.cs.
Change `SetupDiGetClassDevs` to also scan `DIGCF_ALLCLASSES` for USB Class `E0` (`USB\Class_E0&SubClass_01&Prot_01`) and devices with `CM_PROB_FAILED_INSTALL` (Code 28) or `CM_PROB_FAILED_POST_START` (Code 43), including OEM Sony/laptop sub-vendor VIDs (`0489` Foxconn, `04CA` Lite-On, `13D3` AzureWave, `044E` Alps, `0CF3` Atheros, `0A5C` Broadcom, `8087` Intel, `0BDA` Realtek, `0A12` CSR, `0E8D` MediaTek).


2. **Flag Generic Microsoft Drivers on RAM-Patch Silicon:** src/BlueDoc.Diagnostics/Rules/DriverHealthRule.cs.
Update `DriverHealthRule` so that if `DriverProvider == "Microsoft"` and `InfPath == "bth.inf"` on Atheros (`0CF3`/`0489`), Broadcom (`0A5C`/`04CA`), Realtek (`0BDA`), or Intel (`8087`) chips, BlueDoc reports **Failed: Missing Vendor Firmware Patch** and triggers the 1-click Auto-Provisioner instead of marking the driver healthy.


3. **Build a Real VID/PID-to-CAB Installer Using newdev.dll:** src/BlueDoc.Infrastructure/Drivers/BluetoothDriverCatalogService.cs.
Replace the broken HTML portal links with a real JSON catalog mapping `VID` + `PID` to direct Microsoft Update Catalog `.cab` packages (or an offline bundled `drivers/` folder inside `BlueDoc_v1.0.0_Setup.exe`). Extract the `.cab` via `expand.exe`, stage it with `pnputil.exe /add-driver`, and call [`UpdateDriverForPlugAndPlayDevicesW`](https://learn.microsoft.com/en-us/windows/win32/api/newdev/nf-newdev-updatedriverforplugandplaydevicesw) with `INSTALLFLAG_FORCE` to overwrite `bth.inf`.


4. **Add a WinUSB Firmware Patcher + Boot Service for Legacy Chips:** New module in src/BlueDoc.Infrastructure/Drivers/.
Integrate `libwdi` + `LibUsbDotNet` (or `WinUSB` P/Invoke) with open-source firmware blobs (`ar3k/*.dfu` for Atheros AR3011/AR3012, `brcm/*.hcd` for Broadcom, `rtl_bt/*.bin` for Realtek). When no signed `.inf` exists for a chip's `VID/PID`, BlueDoc binds `WinUSB`, sends the vendor HCI firmware upload sequence, resets the USB port so `BthUsb.sys` takes over, and registers a lightweight Windows Service so the patch re-runs automatically after reboot or sleep.


5. **Fix Discovery MAC Parsing & Audio Routing:** src/BlueDoc.Bluetooth/WinRT/WinRtDeviceWatcherProvider.cs & WindowsAudioEndpointRouter.cs.
Fix Line 280 in `WinRtDeviceWatcherProvider.cs` to use `Regex.Matches` and take the **last** MAC address (`Bluetooth#<AdapterMAC>-<SpeakerMAC>`) so discovered speakers don't get overwritten by your laptop's own MAC, and update `WindowsAudioEndpointRouter.cs` to call CoreAudio `IPolicyConfig::SetDefaultEndpoint` so connected speakers actually receive Windows audio.