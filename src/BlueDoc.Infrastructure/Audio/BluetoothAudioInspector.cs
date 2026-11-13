using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;

namespace BlueDoc.Infrastructure.Audio
{
    public class BluetoothAudioInspector
    {
        public BluetoothAudioAnalysisResult AnalyzeAudioSubsystem()
        {
            var result = new BluetoothAudioAnalysisResult();
            var endpoints = new List<BluetoothAudioEndpoint>();

            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT DeviceID, Name, Manufacturer, Status, PNPDeviceID FROM Win32_SoundDevice"))
                using (var collection = searcher.Get())
                {
                    foreach (ManagementObject obj in collection)
                    {
                        var name = obj["Name"]?.ToString() ?? string.Empty;
                        var pnpId = obj["PNPDeviceID"]?.ToString() ?? string.Empty;
                        var mfg = obj["Manufacturer"]?.ToString() ?? string.Empty;
                        var devId = obj["DeviceID"]?.ToString() ?? string.Empty;
                        var status = obj["Status"]?.ToString() ?? "OK";

                        // Check if this sound device is connected via Bluetooth
                        bool isBluetooth = pnpId.StartsWith("BTH", StringComparison.OrdinalIgnoreCase) ||
                                           pnpId.Contains("BTHENUM") ||
                                           pnpId.Contains("BTHHFENUM") ||
                                           name.IndexOf("Bluetooth", StringComparison.OrdinalIgnoreCase) >= 0;

                        if (isBluetooth)
                        {
                            var endpoint = new BluetoothAudioEndpoint
                            {
                                DeviceId = devId,
                                Name = name,
                                Manufacturer = mfg,
                                HardwareId = pnpId,
                                Status = status,
                                IsConnected = string.Equals(status, "OK", StringComparison.OrdinalIgnoreCase)
                            };

                            bool isHandsFree = name.IndexOf("Hands-Free", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                               name.IndexOf("HFP", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                               pnpId.IndexOf("BTHHFENUM", StringComparison.OrdinalIgnoreCase) >= 0;

                            if (isHandsFree)
                            {
                                endpoint.ActiveProfile = "Hands-Free (HFP/HSP Voice)";
                                endpoint.QualityRating = "Voice Quality (8/16kHz Mono)";
                                endpoint.Direction = name.IndexOf("Mic", StringComparison.OrdinalIgnoreCase) >= 0 ? "Recording" : "Playback";
                            }
                            else
                            {
                                endpoint.ActiveProfile = "A2DP Stereo (High Definition)";
                                endpoint.QualityRating = "High Fidelity (44.1/48kHz Stereo)";
                                endpoint.Direction = "Playback";
                            }

                            endpoints.Add(endpoint);
                        }
                    }
                }
            }
            catch
            {
                // Fallback graceful handling if WMI service is unavailable
            }

            result.Endpoints = endpoints;
            result.HasActiveHfpMicrophone = endpoints.Any(e => e.ActiveProfile.Contains("Hands-Free") && e.Direction == "Recording" && e.IsConnected);

            // Determine if High-Def Playback is constrained by simultaneous Hands-Free communication
            if (result.HasActiveHfpMicrophone)
            {
                result.IsHighDefPlaybackThrottled = true;
                result.CoexistenceNotice = "Hands-Free Telephony (HFP) profile is actively engaged. Windows bandwidth constraints force headset audio into mono voice mode (16 kHz mSBC/CVSD codec), suppressing stereo A2DP playback.";
                result.Recommendation = "To restore high-definition A2DP stereo sound, configure Discord, Teams, or Zoom to use a USB desk microphone or internal laptop microphone for voice input while keeping your Bluetooth headphones strictly for audio output.";

                foreach (var ep in endpoints.Where(e => e.Direction == "Playback"))
                {
                    ep.IsProfileConstrained = true;
                    ep.ContentionNotice = "Profile throttled to mono voice while microphone is active in telephony app.";
                }
            }
            else
            {
                result.IsHighDefPlaybackThrottled = false;
                result.CoexistenceNotice = "No active telephony mic contention detected. Connected Bluetooth audio devices operate in unconstrained high-definition A2DP stereo mode.";
                result.Recommendation = "Audio subsystem is optimal. If stutter occurs during network traffic, check for 2.4 GHz Wi-Fi antenna coexistence interference.";
            }

            return result;
        }
    }
}

