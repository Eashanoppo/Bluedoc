using System;
using System.Collections.Generic;
using System.Linq;

namespace BlueDoc.Application.KnowledgeBase
{
    public class OfflineKnowledgeBaseService
    {
        private readonly List<KnowledgeBaseArticle> _articles;

        public OfflineKnowledgeBaseService()
        {
            _articles = InitializeArticles();
        }

        public IReadOnlyList<KnowledgeBaseArticle> GetAllArticles() => _articles.AsReadOnly();

        public IEnumerable<string> GetCategories() =>
            _articles.Select(a => a.Category).Distinct().OrderBy(c => c);

        public IEnumerable<KnowledgeBaseArticle> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return _articles;

            var q = query.Trim().ToLowerInvariant();
            return _articles.Where(a =>
                a.Id.ToLowerInvariant().Contains(q) ||
                a.Title.ToLowerInvariant().Contains(q) ||
                a.Category.ToLowerInvariant().Contains(q) ||
                a.TechnicalExplanation.ToLowerInvariant().Contains(q) ||
                a.Symptoms.Any(s => s.ToLowerInvariant().Contains(q)) ||
                a.AffectedHardware.Any(h => h.ToLowerInvariant().Contains(q)) ||
                a.RelatedRuleIds.Any(r => r.ToLowerInvariant().Contains(q))
            );
        }

        public IEnumerable<KnowledgeBaseArticle> GetByCategory(string category)
        {
            if (string.IsNullOrWhiteSpace(category) || category.Equals("All", StringComparison.OrdinalIgnoreCase))
                return _articles;

            return _articles.Where(a => string.Equals(a.Category, category, StringComparison.OrdinalIgnoreCase));
        }

        public IEnumerable<KnowledgeBaseArticle> GetByRuleId(string ruleId)
        {
            if (string.IsNullOrWhiteSpace(ruleId))
                return Enumerable.Empty<KnowledgeBaseArticle>();

            return _articles.Where(a => a.RelatedRuleIds.Any(r => string.Equals(r, ruleId, StringComparison.OrdinalIgnoreCase)));
        }

        private static List<KnowledgeBaseArticle> InitializeArticles()
        {
            return new List<KnowledgeBaseArticle>
            {
                new KnowledgeBaseArticle
                {
                    Id = "KB-BT-001",
                    Title = "Code 43 Controller Firmware Lockup & Power State Desync",
                    Category = "Hardware & Bus",
                    Severity = "Critical",
                    Symptoms = new List<string>
                    {
                        "Windows Device Manager displays yellow warning triangle on Bluetooth adapter",
                        "Device status indicates 'Windows has stopped this device because it has reported problems. (Code 43)'",
                        "Bluetooth toggle disappears completely from Windows Action Center / Settings"
                    },
                    AffectedHardware = new List<string> { "Intel Wireless-AC / AX", "Realtek RTL8822CE / RTL8852BE", "MediaTek MT7921" },
                    TechnicalExplanation = "Configuration Manager assigns CM_PROB_FAILED_POST_START (Code 43) when the device driver fails during post-start initialization or when the controller firmware becomes unresponsive following a low-power D3 state transition without clean ACPI wake.",
                    StepByStepRemediation = new List<string>
                    {
                        "1. Perform a complete electrical drain: Shut down the PC, disconnect the AC power cord, and hold the physical power button for 30 seconds to discharge motherboard capacitors.",
                        "2. In Safe Repair Center, execute 'Cycle Bluetooth Radio' or 'Open Device Manager' to perform a hardware scan.",
                        "3. In Device Manager, right-click the Bluetooth adapter, choose 'Uninstall device' (do not delete driver files), and select Action > Scan for hardware changes."
                    },
                    RelatedRuleIds = new List<string> { "RULE_HW_STATUS", "RULE_ADAPTER_DETECT" },
                    RecommendedRepairActionId = "CycleBluetoothRadio"
                },
                new KnowledgeBaseArticle
                {
                    Id = "KB-BT-002",
                    Title = "Bluetooth Adapter Disappears After S3/S4 Sleep (Selective Suspend)",
                    Category = "Power Management & Sleep",
                    Severity = "Warning",
                    Symptoms = new List<string>
                    {
                        "Bluetooth functions normally upon boot, but stops working after the laptop resumes from sleep",
                        "Bluetooth switch disappears from Windows Settings until a full reboot"
                    },
                    AffectedHardware = new List<string> { "USB-attached internal Bluetooth adapters", "Realtek RTL8761B", "Intel AX200 / AX201" },
                    TechnicalExplanation = "Windows Power Management enables USB Selective Suspend on internal USB-attached Bluetooth controllers. If the controller firmware does not properly handle the resume wake packet, the USB root hub resets the port, causing the device to drop from the PnP tree.",
                    StepByStepRemediation = new List<string>
                    {
                        "1. Open Device Manager (Safe Repair Center > Open Device Manager).",
                        "2. Expand 'Bluetooth', right-click your adapter, and open Properties.",
                        "3. Navigate to the 'Power Management' tab and uncheck 'Allow the computer to turn off this device to save power'.",
                        "4. In Windows Power Options, disable 'USB selective suspend setting' under Advanced Plan Settings."
                    },
                    RelatedRuleIds = new List<string> { "RULE_HW_STATUS", "RULE_RADIO_AVAIL" },
                    RecommendedRepairActionId = "OpenDeviceManager"
                },
                new KnowledgeBaseArticle
                {
                    Id = "KB-BT-003",
                    Title = "2.4 GHz Wi-Fi and Bluetooth Audio Stuttering (Antenna Coexistence)",
                    Category = "Audio & Coexistence",
                    Severity = "Warning",
                    Symptoms = new List<string>
                    {
                        "Bluetooth headphones experience audio stutter, dropouts, or robotic distortion",
                        "Audio degradation worsens when downloading files over Wi-Fi"
                    },
                    AffectedHardware = new List<string> { "Combo Wi-Fi + Bluetooth single-antenna cards", "Realtek", "Broadcom", "Qualcomm Atheros" },
                    TechnicalExplanation = "Bluetooth and 2.4 GHz Wi-Fi share the same industrial, scientific, and medical (ISM) 2.400 - 2.4835 GHz frequency band. On budget laptops sharing a single physical antenna, time-division multiplexing (TDM) packet collisions occur, delaying A2DP audio packets beyond buffer limits.",
                    StepByStepRemediation = new List<string>
                    {
                        "1. Switch your Wi-Fi router connection to the 5 GHz or 6 GHz Wi-Fi band to eliminate frequency contention.",
                        "2. In Device Manager > Network Adapters > Properties > Advanced, set 'Bluetooth Collaboration' or 'Coexistence Mode' to Enabled.",
                        "3. Move the computer away from high-noise RF sources (USB 3.0 hubs, microwave ovens, wireless chargers)."
                    },
                    RelatedRuleIds = new List<string> { "RULE_AUDIO_SERVICE", "RULE_DISCOVERY_READY" },
                    RecommendedRepairActionId = "RestartAudioGateway"
                },
                new KnowledgeBaseArticle
                {
                    Id = "KB-BT-004",
                    Title = "Windows Update Overwrote OEM Driver with Generic Bluetooth Adapter",
                    Category = "Driver & Firmware",
                    Severity = "Critical",
                    Symptoms = new List<string>
                    {
                        "Adapter name changes from manufacturer branding (e.g. Intel/Realtek) to 'Generic Bluetooth Adapter'",
                        "BLE features stop working, or pairing fails with timeout error"
                    },
                    AffectedHardware = new List<string> { "Intel", "Realtek", "Broadcom", "Qualcomm" },
                    TechnicalExplanation = "Windows Update occasionally pushes generic Microsoft class drivers (bth.inf) that lack manufacturer-specific vendor HCI commands and firmware patch download mechanisms (RAM code patch).",
                    StepByStepRemediation = new List<string>
                    {
                        "1. Launch the Safe Repair Center and select 'Guide Driver Rollback'.",
                        "2. In Device Manager, right-click 'Generic Bluetooth Adapter' > Properties > Driver tab > 'Roll Back Driver'.",
                        "3. If rollback is unavailable, open BlueDoc Driver Assistant and click the official OEM / manufacturer download link to reinstall the proprietary signed driver package."
                    },
                    RelatedRuleIds = new List<string> { "RULE_DRIVER_HEALTH", "RULE_BLE_AVAIL" },
                    RecommendedRepairActionId = "GuideDriverRollback"
                },
                new KnowledgeBaseArticle
                {
                    Id = "KB-BT-005",
                    Title = "Bluetooth Support Service (bthserv) Fails to Start (Error 1079 / 1068)",
                    Category = "Services & Registry",
                    Severity = "Critical",
                    Symptoms = new List<string>
                    {
                        "Attempting to start bthserv returns Error 1079: 'The account specified for this service is different from the account specified for other services'",
                        "Bluetooth discovery cannot start; pairing hangs indefinitely"
                    },
                    AffectedHardware = new List<string> { "All Windows Systems" },
                    TechnicalExplanation = "The Windows Bluetooth Support Service must run under the 'NT AUTHORITY\\LocalService' logon account. If security policy or registry corruption changes this account to 'LocalSystem', SCM rejects the startup request with Error 1079.",
                    StepByStepRemediation = new List<string>
                    {
                        "1. In BlueDoc Safe Repair Center, click 'Restart Bluetooth Services' to attempt an elevated automated restart.",
                        "2. If permission fails, open services.msc as Administrator, locate 'Bluetooth Support Service', open Properties > Log On tab.",
                        "3. Choose 'This account', enter 'NT AUTHORITY\\LocalService', leave both password fields blank, click Apply, and start the service."
                    },
                    RelatedRuleIds = new List<string> { "RULE_SERVICE_STATUS", "RULE_PAIRING_READY" },
                    RecommendedRepairActionId = "RestartBthServ"
                },
                new KnowledgeBaseArticle
                {
                    Id = "KB-BT-006",
                    Title = "Hands-Free Telephony (HFP) Degrades High-Definition Audio (A2DP)",
                    Category = "Audio & Coexistence",
                    Severity = "Warning",
                    Symptoms = new List<string>
                    {
                        "Headphone audio quality drops to mono/telephone-like low fidelity whenever Discord, Teams, or Zoom is opened",
                        "Microphone input sounds muffled and tinny"
                    },
                    AffectedHardware = new List<string> { "Bluetooth Wireless Headsets & Earbuds" },
                    TechnicalExplanation = "Bluetooth Classic bandwidth limitations prevent simultaneous two-way high-fidelity stereo audio. When the microphone is opened, the operating system shifts the Bluetooth profile from Advanced Audio Distribution Profile (A2DP, stereo high-bitrate) to Hands-Free Profile (HFP, 8 kHz or 16 kHz mono mSBC).",
                    StepByStepRemediation = new List<string>
                    {
                        "1. In your VoIP application (Discord, Teams, Zoom), configure an external USB microphone or laptop built-in mic for audio input.",
                        "2. Keep the Bluetooth headset selected strictly for Output Device (Stereo / Headphones).",
                        "3. This prevents Windows from switching the Bluetooth connection to the low-bandwidth Hands-Free telephony profile."
                    },
                    RelatedRuleIds = new List<string> { "RULE_AUDIO_SERVICE" },
                    RecommendedRepairActionId = "RestartAudioGateway"
                },
                new KnowledgeBaseArticle
                {
                    Id = "KB-BT-007",
                    Title = "BTHUSB Event ID 3: HCI Command Timeout Detected in Event Log",
                    Category = "Driver & Firmware",
                    Severity = "Critical",
                    Symptoms = new List<string>
                    {
                        "Bluetooth peripherals disconnect abruptly during active use",
                        "System event log records frequent BTHUSB Event 3: 'A command sent to the adapter has timed out.'"
                    },
                    AffectedHardware = new List<string> { "USB Bluetooth dongles", "Realtek RTL8761B", "Broadcom BCM20702" },
                    TechnicalExplanation = "BTHUSB Event 3 indicates that the host Windows driver dispatched an HCI packet across the USB endpoint, but the Bluetooth controller microcontroller did not respond within the HCI timeout window (typically 2000 ms). This indicates firmware lockup, USB bus brownout, or transceiver desync.",
                    StepByStepRemediation = new List<string>
                    {
                        "1. Disconnect any high-power unpowered USB hubs and plug the Bluetooth dongle directly into a native USB 2.0 port on the motherboard.",
                        "2. In BlueDoc Safe Repair Center, execute 'Cycle Bluetooth Radio' to power-cycle the controller transceiver.",
                        "3. Inspect BlueDoc Bluetooth Event Analyzer to verify if timeouts correlate with sleep or specific peripheral connections."
                    },
                    RelatedRuleIds = new List<string> { "RULE_EVENT_LOG_HEALTH", "RULE_HW_STATUS" },
                    RecommendedRepairActionId = "CycleBluetoothRadio"
                },
                new KnowledgeBaseArticle
                {
                    Id = "KB-BT-008",
                    Title = "Inability to Pair Due to Stale Bluetooth Device Cache Bloat",
                    Category = "Pairing & GATT",
                    Severity = "Warning",
                    Symptoms = new List<string>
                    {
                        "Attempting to pair a device fails with 'Couldn't connect' or 'Device already exists'",
                        "Device was previously removed from Settings but discovery still fails to find it"
                    },
                    AffectedHardware = new List<string> { "All Windows Systems" },
                    TechnicalExplanation = "Windows caches Bluetooth pairing keys and GATT attribute tables under HKLM\\SYSTEM\\CurrentControlSet\\Services\\BTHPORT\\Parameters\\Devices. When a peripheral is ungracefully unpaired or firmware is updated, ghost records in the registry prevent negotiation of new security link keys.",
                    StepByStepRemediation = new List<string>
                    {
                        "1. Open BlueDoc Safe Repair Center and execute 'Clear Device Cache' to purge local cache records.",
                        "2. In Windows Settings > Bluetooth, ensure the peripheral is removed from 'Other Devices'.",
                        "3. Put the peripheral device into fresh pairing mode (holding pairing button until LED flashes rapidly) and initiate discovery."
                    },
                    RelatedRuleIds = new List<string> { "RULE_PAIRING_READY", "RULE_DISCOVERY_READY" },
                    RecommendedRepairActionId = "ClearDeviceCache"
                },
                new KnowledgeBaseArticle
                {
                    Id = "KB-BT-009",
                    Title = "USB 3.0 Radio Frequency Interference (RFI) Degradation",
                    Category = "Hardware & Bus",
                    Severity = "Warning",
                    Symptoms = new List<string>
                    {
                        "Bluetooth mouse cursor jumps, skips, or exhibits severe lag",
                        "Issue occurs when external USB 3.0 hard drives or flash drives are connected"
                    },
                    AffectedHardware = new List<string> { "USB 3.0 ports adjacent to Bluetooth dongles or internal antennas" },
                    TechnicalExplanation = "USB 3.0 (SuperSpeed) data signaling generates wideband broadband electrical noise in the 2.4 GHz - 2.5 GHz spectrum. Poorly shielded cables or unshielded USB 3.0 connectors act as antennas, broadcasting noise directly into the Bluetooth receiver front-end, dropping signal-to-noise ratio (SNR) precipitously.",
                    StepByStepRemediation = new List<string>
                    {
                        "1. Move the Bluetooth dongle or peripheral receiver to a dedicated USB 2.0 port on the opposite side of the computer.",
                        "2. Use a short 1-meter shielded USB extension cable to physically separate the Bluetooth transceiver from USB 3.0 hard drives.",
                        "3. Replace unshielded USB 3.0 cables with certified, high-grade shielded cables."
                    },
                    RelatedRuleIds = new List<string> { "RULE_DISCOVERY_READY", "RULE_RADIO_AVAIL" },
                    RecommendedRepairActionId = "OpenDeviceManager"
                },
                new KnowledgeBaseArticle
                {
                    Id = "KB-BT-010",
                    Title = "Code 22: Bluetooth Adapter Disabled in Windows Device Manager",
                    Category = "Hardware & Bus",
                    Severity = "Warning",
                    Symptoms = new List<string>
                    {
                        "Bluetooth does not work at all; no Bluetooth icon in system tray",
                        "Device Manager displays a small down-arrow overlay icon on the Bluetooth adapter"
                    },
                    AffectedHardware = new List<string> { "All Windows Systems" },
                    TechnicalExplanation = "Configuration Manager reports CM_PROB_DISABLED (Code 22), meaning the PnP manager has marked the device as administratively disabled. The hardware is physically intact and detected, but Windows driver loading has been halted by configuration.",
                    StepByStepRemediation = new List<string>
                    {
                        "1. In BlueDoc Safe Repair Center, click 'Enable Bluetooth Controller (Clear Code 22)' or toggle the radio switch on the Dashboard.",
                        "2. BlueDoc automatically invokes SetupAPI and PnPUtil to re-activate the devnode and power ON the 2.4 GHz radio.",
                        "3. Alternatively, open Device Manager (devmgmt.msc), right-click the disabled adapter, and select 'Enable device'."
                    },
                    RelatedRuleIds = new List<string> { "RULE_HW_STATUS", "RULE_ADAPTER_DETECT" },
                    RecommendedRepairActionId = "EnableBluetoothController"
                }
            };
        }
    }
}

