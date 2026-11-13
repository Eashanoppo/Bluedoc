using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using BlueDoc.Core.Models;
using BlueDoc.Infrastructure.Platform;

namespace BlueDoc.Bluetooth.BLE
{
    public class BlePairingProvider
    {
        public bool IsSupported => OperatingSystemInspector.IsWindows10OrGreater();

        public async Task<DeviceOperationResult> PairBleDeviceAsync(
            ulong rawAddress, 
            string preferredPin = null, 
            Func<string, Task<bool>> confirmPinMatchCallback = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!IsSupported)
            {
                return DeviceOperationResult.Fail("Windows BLE pairing API requires Windows 10 or Windows 11.");
            }

            try
            {
                using (var bleDevice = await BluetoothLEDevice.FromBluetoothAddressAsync(rawAddress))
                {
                    if (bleDevice == null)
                    {
                        return DeviceOperationResult.Fail(
                            "Could not establish radio link with the peripheral. Ensure the device is powered on and in pairing mode.",
                            "FromBluetoothAddressAsync returned null.");
                    }

                    var pairing = bleDevice.DeviceInformation.Pairing;
                    if (pairing.IsPaired)
                    {
                        return DeviceOperationResult.Ok($"Device '{bleDevice.Name}' is already paired in Windows.");
                    }

                    if (!pairing.CanPair)
                    {
                        return DeviceOperationResult.Fail(
                            $"Device '{bleDevice.Name}' indicates it cannot be paired in its current state.",
                            "Pairing.CanPair is false. The device may require physical button press or reset.");
                    }

                    // Use Custom Pairing to handle PIN, Numeric Comparison, and User Confirmation
                    var customPairing = pairing.Custom;

                    TypedEventHandler<DeviceInformationCustomPairing, DevicePairingRequestedEventArgs> handler = null;
                    handler = async (sender, args) =>
                    {
                        var deferral = args.GetDeferral();
                        try
                        {
                            switch (args.PairingKind)
                            {
                                case DevicePairingKinds.ConfirmOnly:
                                    args.Accept();
                                    break;

                                case DevicePairingKinds.ProvidePin:
                                    string pinToProvide = !string.IsNullOrWhiteSpace(preferredPin) ? preferredPin : "0000";
                                    args.Accept(pinToProvide);
                                    break;

                                case DevicePairingKinds.ConfirmPinMatch:
                                    bool matches = true;
                                    if (confirmPinMatchCallback != null)
                                    {
                                        matches = await confirmPinMatchCallback(args.Pin);
                                    }
                                    if (matches)
                                    {
                                        args.Accept();
                                    }
                                    break;

                                case DevicePairingKinds.DisplayPin:
                                    // Displayed on device, accept pairing flow
                                    args.Accept();
                                    break;

                                default:
                                    args.Accept();
                                    break;
                            }
                        }
                        catch
                        {
                            // In case of cancellation or exception
                        }
                        finally
                        {
                            deferral.Complete();
                        }
                    };

                    customPairing.PairingRequested += handler;

                    try
                    {
                        var pairTask = customPairing.PairAsync(
                            DevicePairingKinds.ConfirmOnly | 
                            DevicePairingKinds.ProvidePin | 
                            DevicePairingKinds.ConfirmPinMatch | 
                            DevicePairingKinds.DisplayPin, 
                            DevicePairingProtectionLevel.None).AsTask(cancellationToken);

                        var result = await pairTask;

                        switch (result.Status)
                        {
                            case DevicePairingResultStatus.Paired:
                                return DeviceOperationResult.Ok($"Device '{bleDevice.Name}' paired successfully with Windows.");

                            case DevicePairingResultStatus.AlreadyPaired:
                                return DeviceOperationResult.Ok($"Device '{bleDevice.Name}' is already paired.");

                            case DevicePairingResultStatus.ConnectionRejected:
                                return DeviceOperationResult.Fail(
                                    $"Pairing was rejected by '{bleDevice.Name}'. Ensure device is actively in discoverable mode.",
                                    result.Status.ToString());

                            case DevicePairingResultStatus.AuthenticationTimeout:
                                return DeviceOperationResult.Fail(
                                    "Pairing authentication timed out. The peripheral did not respond in time.",
                                    result.Status.ToString());

                            case DevicePairingResultStatus.AuthenticationFailure:
                                return DeviceOperationResult.Fail(
                                    "Pairing authentication failed. The entered PIN was incorrect or the security key was rejected.",
                                    result.Status.ToString());

                            case DevicePairingResultStatus.PairingCanceled:
                                return DeviceOperationResult.Fail(
                                    "Pairing operation was cancelled by the user or peripheral.",
                                    result.Status.ToString());

                            case DevicePairingResultStatus.AccessDenied:
                                return DeviceOperationResult.Fail(
                                    "Windows access denied during pairing. Administrative rights may be required.",
                                    result.Status.ToString());

                            case DevicePairingResultStatus.NoSupportedProfiles:
                                return DeviceOperationResult.Fail(
                                    "No supported Bluetooth profiles were found on this peripheral.",
                                    result.Status.ToString());

                            default:
                                return DeviceOperationResult.Fail(
                                    $"Windows pairing failed with status: {result.Status}.",
                                    result.Status.ToString());
                        }
                    }
                    finally
                    {
                        customPairing.PairingRequested -= handler;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                return DeviceOperationResult.Fail("Pairing cancelled by user.");
            }
            catch (Exception ex)
            {
                return DeviceOperationResult.Fail($"Pairing exception: {ex.Message}");
            }
        }
    }
}
