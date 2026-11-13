using System;

namespace BlueDoc.Infrastructure.Hardware
{
    public static class DeviceClassDecoder
    {
        public static void Decode(uint cod, out string categoryName, out string majorClass, out string icon)
        {
            // Bits 8-12: Major Device Class (5 bits)
            uint majorDeviceClass = (cod >> 8) & 0x1F;
            // Bits 2-7: Minor Device Class (6 bits)
            uint minorDeviceClass = (cod >> 2) & 0x3F;

            switch (majorDeviceClass)
            {
                case 0x01: // Computer
                    majorClass = "Computer";
                    categoryName = "Computer";
                    icon = "💻";
                    break;

                case 0x02: // Phone
                    majorClass = "Phone";
                    categoryName = "Smartphone / Phone";
                    icon = "📱";
                    break;

                case 0x03: // LAN / Network Access Point
                    majorClass = "Network";
                    categoryName = "Network Access Point";
                    icon = "🌐";
                    break;

                case 0x04: // Audio / Video
                    majorClass = "Audio/Video";
                    icon = "🎧";
                    // Minor class decoding:
                    switch (minorDeviceClass)
                    {
                        case 0x01: categoryName = "Headset (Hands-free)"; break;
                        case 0x02: categoryName = "Hands-free Unit"; break;
                        case 0x04: categoryName = "Microphone"; icon = "🎙️"; break;
                        case 0x05: categoryName = "Loudspeaker"; icon = "🔊"; break;
                        case 0x06: categoryName = "Headphones"; break;
                        case 0x07: categoryName = "Portable Audio"; break;
                        case 0x08: categoryName = "Car Audio"; icon = "🚗"; break;
                        default: categoryName = "Audio / Headphones"; break;
                    }
                    break;

                case 0x05: // Peripheral (Mouse, Keyboard, etc.)
                    majorClass = "Peripheral";
                    // Upper 2 bits of minor class (bits 6-7 of CoD, bits 4-5 of minorDeviceClass) indicate keyboard/pointing device
                    uint peripheralType = (minorDeviceClass >> 4) & 0x03;
                    if (peripheralType == 1)
                    {
                        categoryName = "Keyboard";
                        icon = "⌨️";
                    }
                    else if (peripheralType == 2)
                    {
                        categoryName = "Mouse / Pointing Device";
                        icon = "🖱️";
                    }
                    else if (peripheralType == 3)
                    {
                        categoryName = "Keyboard & Mouse Combo";
                        icon = "⌨️";
                    }
                    else
                    {
                        // Check lower 4 bits
                        uint subType = minorDeviceClass & 0x0F;
                        if (subType == 1) { categoryName = "Gamepad"; icon = "🎮"; }
                        else if (subType == 2) { categoryName = "Joystick"; icon = "🕹️"; }
                        else { categoryName = "Peripheral Device"; icon = "🖱️"; }
                    }
                    break;

                case 0x06: // Imaging
                    majorClass = "Imaging";
                    categoryName = "Printer / Scanner";
                    icon = "🖨️";
                    break;

                case 0x07: // Wearable
                    majorClass = "Wearable";
                    categoryName = "Wearable / Watch";
                    icon = "⌚";
                    break;

                case 0x08: // Toy
                    majorClass = "Toy";
                    categoryName = "Toy Device";
                    icon = "🧸";
                    break;

                case 0x09: // Health
                    majorClass = "Health";
                    categoryName = "Health / Medical Monitor";
                    icon = "🩺";
                    break;

                default:
                    majorClass = "Uncategorized";
                    categoryName = "Bluetooth Device";
                    icon = "📶";
                    break;
            }
        }

        public static void InferFromNameOrInstance(string name, string instanceId, ref string categoryName, ref string majorClass, ref string icon)
        {
            string combined = ((name ?? "") + " " + (instanceId ?? "")).ToLowerInvariant();

            if (combined.Contains("mouse"))
            {
                categoryName = "Mouse / Pointing Device";
                majorClass = "Peripheral";
                icon = "🖱️";
            }
            else if (combined.Contains("keyboard") || combined.Contains("keypad"))
            {
                categoryName = "Keyboard";
                majorClass = "Peripheral";
                icon = "⌨️";
            }
            else if (combined.Contains("headset") || combined.Contains("headphone") || combined.Contains("buds") || combined.Contains("earphone") || combined.Contains("audio") || combined.Contains("sound") || combined.Contains("speaker") || combined.Contains("vibe") || combined.Contains("hoco") || combined.Contains("oraimo") || combined.Contains("airpods"))
            {
                categoryName = "Audio / Headphones";
                majorClass = "Audio/Video";
                icon = "🎧";
            }
            else if (combined.Contains("watch") || combined.Contains("band") || combined.Contains("fitbit"))
            {
                categoryName = "Wearable / Smartwatch";
                majorClass = "Wearable";
                icon = "⌚";
            }
            else if (combined.Contains("phone") || combined.Contains("galaxy") || combined.Contains("iphone") || combined.Contains("pixel"))
            {
                categoryName = "Smartphone / Phone";
                majorClass = "Phone";
                icon = "📱";
            }
        }
    }
}

