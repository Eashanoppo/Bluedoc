namespace BlueDoc.Core.Models
{
    public class ServiceInfo
    {
        public string ServiceName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // Running, Stopped, Paused, etc.
        public string StartupType { get; set; } = string.Empty; // Automatic, Manual, Disabled
        public bool IsRunning => string.Equals(Status, "Running", System.StringComparison.OrdinalIgnoreCase);
        public bool Exists { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }
}

