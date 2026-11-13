using System;
using System.Collections.Generic;

namespace BlueDoc.Infrastructure.Audio
{
    public class BluetoothAudioEndpoint
    {
        public string DeviceId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Direction { get; set; } = "Playback"; // Playback, Recording
        public string ActiveProfile { get; set; } = "A2DP Stereo (High Definition)"; // A2DP, HFP, Unknown
        public string QualityRating { get; set; } = "High Fidelity (44.1/48kHz Stereo)";
        public bool IsProfileConstrained { get; set; }
        public string ContentionNotice { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = "Unknown";
        public string HardwareId { get; set; } = string.Empty;
        public bool IsConnected { get; set; }
        public string Status { get; set; } = "OK";
    }

    public class BluetoothAudioAnalysisResult
    {
        public List<BluetoothAudioEndpoint> Endpoints { get; set; } = new List<BluetoothAudioEndpoint>();
        public bool HasActiveHfpMicrophone { get; set; }
        public bool IsHighDefPlaybackThrottled { get; set; }
        public string CoexistenceNotice { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
    }
}

