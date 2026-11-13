using System;
using System.Diagnostics;
using BlueDoc.Core.Models;

namespace BlueDoc.Infrastructure.Drivers
{
    public class DriverAssistant
    {
        public string GetManufacturerSupportUrl(HardwareIdDetails hwDetails)
        {
            if (hwDetails == null || string.IsNullOrWhiteSpace(hwDetails.VendorId)) 
                return "https://support.microsoft.com/en-us/windows/fix-bluetooth-problems-in-windows-723e05f8-0376-46a4-73ac-eeb2d8442a66";

            string vid = hwDetails.VendorId.Trim().ToUpperInvariant();
            switch (vid)
            {
                case "0BDA": // Realtek
                    return "https://www.realtek.com/en/component/zoo/category/pci-wireless-lan-pci-express-software";
                case "8087": // Intel
                    return "https://www.intel.com/content/www/us/en/support/articles/000005489/wireless.html";
                case "0A5C": // Broadcom
                    return "https://www.broadcom.com/support";
                case "0CF3": // Atheros / Qualcomm
                case "17CB": // Qualcomm
                    return "https://www.qualcomm.com/products/features/bluetooth";
                case "0E8D": // MediaTek
                    return "https://www.mediatek.com/";
                case "0451": // Texas Instruments
                    return "https://www.ti.com/wireless-connectivity/bluetooth/overview.html";
                default:
                    return "https://support.microsoft.com/en-us/windows/fix-bluetooth-problems-in-windows-723e05f8-0376-46a4-73ac-eeb2d8442a66";
            }
        }

        public string ExplainProblemCode(int problemCode)
        {
            switch (problemCode)
            {
                case 0:
                    return "Operational: The device is loaded, recognized, and working properly without reported driver errors.";
                case 10: // CM_PROB_FAILED_START
                    return "Device Start Failure (Code 10): This device cannot start. Common causes include a corrupted driver, missing firmware file, or conflicting USB filters. Recommended action: Update driver or reinstall using official manufacturer package.";
                case 14: // CM_PROB_NEED_RESTART
                    return "Restart Required (Code 14): This device requires a system restart before it can function properly. Recommended action: Restart Windows.";
                case 22: // CM_PROB_DISABLED
                    return "Device Disabled (Code 22): The device is currently disabled in Windows. Recommended action: Enable the adapter using BlueDoc Safe Repair Center or Windows Device Manager.";
                case 28: // CM_PROB_FAILED_INSTALL
                    return "Drivers Not Installed (Code 28): The drivers for this device are not installed. Recommended action: Install the official driver from the manufacturer's support portal.";
                case 43: // CM_PROB_FAILED_POST_START
                    return "Device Stopped Due to Errors (Code 43): Windows stopped this device because it reported a communication fault or controller crash. Recommended action: Unplug/replug USB adapter or power cycle computer.";
                default:
                    return $"Windows Configuration Manager reported error code {problemCode}. Inspect Device Manager for additional error strings.";
            }
        }

        public string GenerateRollbackInstructions(string adapterName)
        {
            string name = string.IsNullOrWhiteSpace(adapterName) ? "your Bluetooth adapter" : $"\"{adapterName}\"";
            return $@"Step-by-Step Driver Rollback Guide:
1. Open Windows Device Manager (you can click the 'Open Device Manager' button below).
2. Expand the 'Bluetooth' category.
3. Right-click on {name} and select 'Properties'.
4. In the Properties dialog, switch to the 'Driver' tab.
5. If the 'Roll Back Driver' button is active, click it.
   (If the button is grayed out, Windows has no previous driver version backed up).
6. Select a reason when prompted and click 'Yes' to confirm.
7. Restart your computer if requested to apply the changes.";
        }

        public string GenerateReinstallInstructions(string adapterName, string manufacturerSupportUrl)
        {
            string name = string.IsNullOrWhiteSpace(adapterName) ? "your Bluetooth adapter" : $"\"{adapterName}\"";
            return $@"Step-by-Step Clean Reinstallation Guide:
1. Visit the official manufacturer support page ({manufacturerSupportUrl}) and download the latest installer for your adapter model.
2. Open Windows Device Manager.
3. Locate {name} under the 'Bluetooth' category.
4. Right-click the adapter and select 'Uninstall device'.
   NOTE: Do NOT check 'Delete the driver software for this device' unless you have the new installer downloaded and ready.
5. Click 'Uninstall' to remove the current device registration.
6. Run the manufacturer's installer package downloaded in Step 1, OR in Device Manager, click 'Action' > 'Scan for hardware changes'.
7. Windows will re-detect the hardware and bind the fresh driver.
8. Restart your computer to complete initialization.";
        }

        public bool OpenDeviceManager()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "devmgmt.msc",
                    UseShellExecute = true
                });
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool OpenWindowsBluetoothSettings()
        {
            try
            {
                // Modern Windows Settings URI
                Process.Start(new ProcessStartInfo
                {
                    FileName = "ms-settings:bluetooth",
                    UseShellExecute = true
                });
                return true;
            }
            catch
            {
                // Fallback for Windows 7 / legacy: open classic Control Panel Bluetooth Devices
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "control.exe",
                        Arguments = "bthprops.cpl",
                        UseShellExecute = true
                    });
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }
    }
}
