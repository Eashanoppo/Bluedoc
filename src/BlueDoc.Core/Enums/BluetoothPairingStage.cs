namespace BlueDoc.Core.Enums
{
    public enum BluetoothPairingStage
    {
        Idle = 0,
        Discovering = 1,
        PairingRequested = 2,
        WaitingForAuthentication = 3,
        Authenticating = 4,
        PairingCompleted = 5,
        PairingFailed = 6
    }
}
