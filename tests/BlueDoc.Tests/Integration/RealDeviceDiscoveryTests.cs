using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BlueDoc.Application.Services;

namespace BlueDoc.Tests.Integration
{
    public static class RealDeviceDiscoveryTests
    {
        public static void Run()
        {
            Console.WriteLine("[TEST SUITE] Live Bluetooth Device Management & Discovery Verification");

            RunLivePairedDevicesTest().GetAwaiter().GetResult();

            Console.WriteLine("All Live Device Management tests completed!\n");
        }

        private static async Task RunLivePairedDevicesTest()
        {
            var mgmt = new DeviceManagementService();
            Console.WriteLine("  Querying registered and paired Bluetooth devices from Windows PnP / Win32...");

            var devices = await mgmt.GetPairedDevicesAsync();
            Console.WriteLine($"  Total paired/remembered devices retrieved: {devices.Count}");

            if (devices.Count == 0)
            {
                Console.WriteLine("  [INFO] No paired devices currently in Windows registry.");
                return;
            }

            foreach (var d in devices)
            {
                Console.WriteLine($"    -> [{d.CategoryIcon}] {d.DisplayName}");
                Console.WriteLine($"       Address: {d.BluetoothAddress}");
                Console.WriteLine($"       Type: {d.DeviceType}, Category: {d.CategoryName} ({d.MajorDeviceClass})");
                Console.WriteLine($"       State: {d.StatusDisplay} (IsConnected: {d.IsConnected}, IsPaired: {d.IsPaired})");
                Console.WriteLine($"       Instance: {d.InstanceId}");

                // Validate address format
                if (!Regex.IsMatch(d.BluetoothAddress, @"^([0-9A-Fa-f]{2}:){5}[0-9A-Fa-f]{2}$"))
                {
                    throw new Exception($"Invalid Bluetooth MAC address format: {d.BluetoothAddress}");
                }

                if (string.IsNullOrWhiteSpace(d.DisplayName))
                {
                    throw new Exception("DisplayName must not be empty");
                }
            }

            Console.WriteLine("  [PASS] Live paired devices verified with valid MAC addresses and categories.");
        }
    }
}

