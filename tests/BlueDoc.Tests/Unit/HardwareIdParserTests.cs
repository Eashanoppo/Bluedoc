using System;
using BlueDoc.Infrastructure.Hardware;

namespace BlueDoc.Tests.Unit
{
    public static class HardwareIdParserTests
    {
        public static void Run()
        {
            Console.WriteLine("[TEST SUITE] HardwareIdParser Tests");

            // Test 1: Realtek USB Adapter
            var r1 = HardwareIdParser.Parse(@"USB\VID_0BDA&PID_C822&REV_0001");
            Assert(r1.BusType == "USB", "Test 1.1: BusType is USB");
            Assert(r1.VendorId == "0BDA", "Test 1.2: VendorId is 0BDA");
            Assert(r1.ProductId == "C822", "Test 1.3: ProductId is C822");
            Assert(r1.IdentifiedManufacturer.Contains("Realtek"), "Test 1.4: Manufacturer is Realtek");
            Assert(r1.IsValid, "Test 1.5: IsValid is true");

            // Test 2: Intel USB Adapter
            var r2 = HardwareIdParser.Parse(@"USB\VID_8087&PID_0026");
            Assert(r2.VendorId == "8087", "Test 2.1: VendorId is 8087");
            Assert(r2.ProductId == "0026", "Test 2.2: ProductId is 0026");
            Assert(r2.IdentifiedManufacturer.Contains("Intel"), "Test 2.3: Manufacturer is Intel");

            // Test 3: Broadcom USB Adapter
            var r3 = HardwareIdParser.Parse(@"USB\VID_0A5C&PID_21E8");
            Assert(r3.VendorId == "0A5C", "Test 3.1: VendorId is 0A5C");
            Assert(r3.IdentifiedManufacturer.Contains("Broadcom"), "Test 3.2: Manufacturer is Broadcom");

            // Test 4: PCI Adapter
            var r4 = HardwareIdParser.Parse(@"PCI\VEN_8086&DEV_43F0&SUBSYS_00348086");
            Assert(r4.BusType == "PCI", "Test 4.1: BusType is PCI");
            Assert(r4.VendorId == "8086", "Test 4.2: VendorId is 8086");
            Assert(r4.ProductId == "43F0", "Test 4.3: ProductId is 43F0");

            // Test 5: Malformed and empty inputs
            var r5 = HardwareIdParser.Parse(null);
            Assert(!r5.IsValid, "Test 5.1: Null input handled safely");
            var r6 = HardwareIdParser.Parse("");
            Assert(!r6.IsValid, "Test 5.2: Empty input handled safely");
            var r7 = HardwareIdParser.Parse("UNKNOWN_GARBAGE_ID");
            Assert(!r7.IsValid, "Test 5.3: Unknown string handled safely without crash");

            Console.WriteLine("All HardwareIdParser tests passed!\n");
        }

        private static void Assert(bool condition, string testName)
        {
            if (condition)
            {
                Console.WriteLine($"  [PASS] {testName}");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  [FAIL] {testName}");
                Console.ResetColor();
                throw new Exception($"Assertion failed for: {testName}");
            }
        }
    }
}

