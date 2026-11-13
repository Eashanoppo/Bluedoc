using System;
using BlueDoc.Infrastructure.Hardware;

namespace BlueDoc.Tests.Unit
{
    public static class DeviceClassDecoderTests
    {
        public static void Run()
        {
            Console.WriteLine("[TEST SUITE] DeviceClassDecoder Tests");

            // Test 1: Computer (Major 0x01)
            // CoD: 0x000104 (Major 0x01, Minor 0x01 = Desktop)
            DeviceClassDecoder.Decode(0x000104, out var cat1, out var maj1, out var ico1);
            Assert(maj1 == "Computer", "Test 1.1: Major is Computer");
            Assert(ico1 == "💻", "Test 1.2: Icon is Laptop/Computer");

            // Test 2: Phone (Major 0x02)
            // CoD: 0x000204 (Smartphone)
            DeviceClassDecoder.Decode(0x000204, out var cat2, out var maj2, out var ico2);
            Assert(maj2 == "Phone", "Test 2.1: Major is Phone");
            Assert(ico2 == "📱", "Test 2.2: Icon is Phone");

            // Test 3: Audio Headphones (Major 0x04, Minor 0x06 -> CoD: 0x200418)
            // Minor bits: 0x06 << 2 = 0x18. Major: 0x04 << 8 = 0x0400 -> CoD: 0x0418
            DeviceClassDecoder.Decode(0x000418, out var cat3, out var maj3, out var ico3);
            Assert(maj3 == "Audio/Video", "Test 3.1: Major is Audio/Video");
            Assert(cat3 == "Headphones", "Test 3.2: Category is Headphones");
            Assert(ico3 == "🎧", "Test 3.3: Icon is Headphones");

            // Test 4: Peripheral Mouse (Major 0x05, Minor peripheralType 2 -> CoD: 0x002580)
            // Minor bits: 0x20 << 2 = 0x80. Major: 0x05 << 8 = 0x0500 -> CoD: 0x0580
            DeviceClassDecoder.Decode(0x000580, out var cat4, out var maj4, out var ico4);
            Assert(maj4 == "Peripheral", "Test 4.1: Major is Peripheral");
            Assert(cat4.Contains("Mouse"), "Test 4.2: Category is Mouse");
            Assert(ico4 == "🖱️", "Test 4.3: Icon is Mouse");

            // Test 5: Name/Instance Heuristic Inference
            string c5 = "", m5 = "", i5 = "";
            DeviceClassDecoder.InferFromNameOrInstance("BT5.4 Mouse", "BTHLE\\DEV_499B95523E49", ref c5, ref m5, ref i5);
            Assert(m5 == "Peripheral", "Test 5.1: Inferred Mouse as Peripheral");
            Assert(i5 == "🖱️", "Test 5.2: Inferred Mouse Icon");

            string c6 = "", m6 = "", i6 = "";
            DeviceClassDecoder.InferFromNameOrInstance("oraimo SpaceBuds Lite", "BTHENUM\\DEV_2852E031E284", ref c6, ref m6, ref i6);
            Assert(m6 == "Audio/Video", "Test 5.3: Inferred Earbuds as Audio/Video");
            Assert(i6 == "🎧", "Test 5.4: Inferred Earbuds Icon");

            Console.WriteLine("All DeviceClassDecoder tests passed!\n");
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

