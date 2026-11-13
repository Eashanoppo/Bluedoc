using System;
using BlueDoc.Core.Models;
using BlueDoc.Infrastructure.Drivers;

namespace BlueDoc.Tests.Unit
{
    public class DriverAssistantTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("[TEST SUITE] Driver Assistant & Vendor Mapping Tests");

            var assistant = new DriverAssistant();

            // Realtek 0x0BDA
            var realtekHw = new HardwareIdDetails { VendorId = "0BDA", ProductId = "C822" };
            string realtekUrl = assistant.GetManufacturerSupportUrl(realtekHw);
            if (!realtekUrl.Contains("realtek.com"))
                throw new Exception($"Expected Realtek URL, got: {realtekUrl}");
            Console.WriteLine("  ✓ Realtek (0x0BDA) support URL generated.");

            // Intel 0x8087
            var intelHw = new HardwareIdDetails { VendorId = "8087", ProductId = "0032" };
            string intelUrl = assistant.GetManufacturerSupportUrl(intelHw);
            if (!intelUrl.Contains("intel.com"))
                throw new Exception($"Expected Intel URL, got: {intelUrl}");
            Console.WriteLine("  ✓ Intel (0x8087) support URL generated.");

            // Problem codes
            string code0 = assistant.ExplainProblemCode(0);
            if (!code0.Contains("Operational")) throw new Exception("Code 0 explanation failed.");

            string code10 = assistant.ExplainProblemCode(10);
            if (!code10.Contains("Device Start Failure")) throw new Exception("Code 10 explanation failed.");

            string code43 = assistant.ExplainProblemCode(43);
            if (!code43.Contains("Device Stopped Due to Errors")) throw new Exception("Code 43 explanation failed.");

            Console.WriteLine("  ✓ Windows Configuration Manager problem codes (Code 0, 10, 43) decoded accurately.");
        }
    }
}
