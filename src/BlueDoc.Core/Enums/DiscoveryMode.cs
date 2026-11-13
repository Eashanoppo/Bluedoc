namespace BlueDoc.Core.Enums
{
    public enum DiscoveryMode
    {
        DualMode = 0,    // Simultaneous Classic Inquiry + Windows BLE Advertisement Watcher
        ClassicOnly = 1, // Windows Classic Inquiry Only (Win7 SP1 Compatible)
        BleOnly = 2      // Windows BLE Advertisement Watcher Only (Win 10/11)
    }
}
