namespace BlueDoc.Core.Enums
{
    public enum BluetoothConnectionStage
    {
        Idle = 0,
        PreparingConnection = 1,
        Connecting = 2,
        VerifyingState = 3,
        Connected = 4,
        ConnectionFailed = 5
    }
}
