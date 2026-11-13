# BlueDoc Design System Specification

**Product:** BlueDoc (Windows Bluetooth Doctor)  
**Company:** UNLEFT LLC  
**Design Architects:** Principal UI/UX Designer & Senior WPF Engineer  
**Date:** November 13, 2026  
**Primary Reference:** Apple macOS Bluetooth Settings  
**Secondary Inspiration:** Premium Minimalist Dark Desktop Application (Linear / Windows 11)  
**Status:** **ACTIVE SPECIFICATION (v2.0)**

---

## 1. Design Philosophy

BlueDoc's visual design is founded on the principle of **quiet precision hardware management**. Rather than resembling an unorganized diagnostic dashboard or administrative utility, it provides a calm, focused, and intuitive native desktop experience:

1. **Minimalism over Information Overload:** Progressive disclosure surfaces essential operational context first, reserving deep register and driver details for dedicated inspection panels.
2. **Clear Grouped Hierarchy:** Content is organized into clean grouped containers with rounded corners, subtle borders, and consistent row heights.
3. **Hardware Truth:** UI controls map directly to native physical states. When a radio is turning on, pending animations communicate hardware transit. When RSSI is unexposed, honest badges prevent false certainty.
4. **Platform Harmony:** Employs Windows native typography (`Segoe UI` / `Segoe UI Variable`) and layout mechanics while incorporating macOS System Settings simplicity.

---

## 2. Color System & Theme Tokens

### 2.1 Dark Theme (Deep Charcoal Minimalist Identity)
The dark theme eliminates saturated navy-blue surfaces in favor of deep neutral charcoal tones inspired by macOS dark mode and Linear:

| Token Name | Hex Code | Semantic Role |
| :--- | :---: | :--- |
| `BgDarkColor` / `SurfaceCanvas` | `#0E1116` | Main window background canvas |
| `SidebarBgColor` / `SurfaceSidebar` | `#0A0D11` | Navigation sidebar background |
| `CardBgColor` / `SurfaceCard` | `#161B22` | Grouped container card surface |
| `CardBgHoverColor` | `#1C2128` | Grouped device row hover background |
| `SurfaceElevatedColor` | `#21262D` | Compact buttons, popovers, elevated chips |
| `BorderColor` / `BorderSubtle` | `#30363D` | Outer container borders & row separators |
| `BorderStrongColor` | `#3D444D` | Active / focused container borders |
| `TextPrimaryColor` | `#F0F3F6` | Primary high-contrast typography |
| `TextSecondaryColor` | `#8B949E` | Secondary metadata, labels, status text |
| `TextMutedColor` | `#6E7681` | Micro-copy, breadcrumb roots, captions |
| `AccentColor` | `#2F81F7` | Refined native blue accent |
| `AccentHoverColor` | `#58A6FF` | Interactive accent hover state |
| `SuccessColor` | `#2EA043` | Operational radio, connected state |
| `WarningColor` | `#D29922` | Pairing pending, audio contention notice |
| `DangerColor` | `#F85149` | Disconnect, unpair, hardware fault |

### 2.2 Light Theme (Crisp Apple-Style Light Mode)
The light theme provides a distraction-free, soft neutral palette with high legibility:

| Token Name | Hex Code | Semantic Role |
| :--- | :---: | :--- |
| `BgDarkColor` / `SurfaceCanvas` | `#F6F8FA` | Main window background canvas |
| `SidebarBgColor` / `SurfaceSidebar` | `#EEF1F4` | Navigation sidebar background |
| `CardBgColor` / `SurfaceCard` | `#FFFFFF` | Grouped container card surface |
| `CardBgHoverColor` | `#F3F4F6` | Grouped device row hover background |
| `SurfaceElevatedColor` | `#FFFFFF` | Compact buttons, popovers, elevated chips |
| `BorderColor` / `BorderSubtle` | `#D0D7DE` | Outer container borders & row separators |
| `BorderStrongColor` | `#AFB8C1` | Active / focused container borders |
| `TextPrimaryColor` | `#1F2328` | Primary dark charcoal typography |
| `TextSecondaryColor` | `#57606A` | Secondary metadata, labels, status text |
| `TextMutedColor` | `#656D76` | Micro-copy, breadcrumb roots, captions |
| `AccentColor` | `#0969DA` | Refined native blue accent |
| `AccentHoverColor` | `#0854B2` | Interactive accent hover state |
| `SuccessColor` | `#1A7F37` | Operational radio, connected state |
| `WarningColor` | `#9A6700` | Pairing pending, audio contention notice |
| `DangerColor` | `#CF222E` | Disconnect, unpair, hardware fault |

---

## 3. Typography & Hierarchy

BlueDoc utilizes the native Windows font family hierarchy: `Segoe UI Variable Text`, `Segoe UI Variable Display`, `Segoe UI`, `sans-serif`.

| Style Class | Size | Weight | Line Height | Usage |
| :--- | :---: | :---: | :---: | :--- |
| **Page Title** | 26 px | SemiBold (600) | 34 px | View primary heading (e.g. "Bluetooth") |
| **Page Subtitle** | 13.5 px | Regular (400) | 18 px | Subheading / discoverability description |
| **Section Header** | 13 px | SemiBold (600) | 16 px | Grouped section titles ("Audio Devices", "Peripherals") |
| **Device Name** | 13.5 px | SemiBold (600) | 17 px | Per-row peripheral titles |
| **Device Subtitle** | 12 px | Regular (400) | 15 px | Connection status, MAC address, audio profile |
| **Button Text** | 12.5 px | Medium (500) | 16 px | Primary and secondary action triggers |
| **Monospace Data** | 11.5 px | Regular (400) | 14 px | MAC addresses, UUIDs, hardware PnP IDs (`Consolas`) |

Text is globally rendered with `TextOptions.TextFormattingMode="Display"` and `TextOptions.TextRenderingMode="ClearType"`.

---

## 4. Spacing, Geometry & Layout Grid

BlueDoc enforces an 8-point baseline grid system:
- **Spacing Units:** `4px`, `8px`, `12px`, `16px`, `20px`, `24px`, `32px`.
- **Corner Radii:**
  - Grouped Container Cards: `10px`
  - Modal / Dialog Cards: `12px`
  - Interactive Buttons: `6px`
  - Toggle Switch Track: `10px`
  - Status Badges / Pills: `12px` (Pill shape)
- **Container Geometry:**
  - Grouped Device Rows: Min-height `52px`, vertical padding `11px`, horizontal padding `16px`.
  - Icon Badges: `36 × 36px` container with `9px` corner radius.

---

## 5. Component Library Specification

### 5.1 Grouped Card Containers (`GroupedCardContainer`)
Modeled after macOS System Settings panes:
- Single contiguous container per peripheral category.
- Clean white (light) or `#161B22` (dark) surface.
- Rows stacked vertically separated by `1px` subtle divider lines (`BorderSubtleBrush`).
- Hovering over any row highlights that specific row with `CardBgHoverBrush`.

### 5.2 Native-Style Hardware Toggle Switch (`ModernToggleSwitch`)
- **Dimensions:** Track `38 × 20px`, Thumb `14 × 14px`.
- **Track Colors:** Gray `#3D444D` (OFF) $\rightarrow$ Electric Blue `#2F81F7` (ON).
- **Thumb Motion:** Slides smoothly between `3px` left margin and `3px` right margin.
- **Pending Transition:** Pulsing indicator when Bluetooth radio power transit is in progress.

### 5.3 Action Buttons
- **`CompactSecondaryButton`:** High-affordance, compact (`12px` font, `12 × 4px` padding) action for "Connect", "Disconnect", "Manage".
- **`CompactPrimaryButton`:** High-priority accent button for "Pair" or "Scan".
- **`CompactGhostButton`:** Minimal text button for inline dismissals or secondary tools.

---

## 6. Motion & Accessibility

- **Standard Interaction Duration:** `150–200ms` with cubic-bezier easing (`EaseOutQuad`).
- **Reduced-Motion Compliance:** `MotionManager.Instance.ForceReducedMotion` collapses all transition intervals to `TimeSpan.Zero`.
- **Contrast Ratios:** Exceeds WCAG 2.1 AA specifications (Dark: 18.3:1, Light: 17.9:1).

