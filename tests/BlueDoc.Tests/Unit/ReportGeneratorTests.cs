using System;
using System.Collections.Generic;
using BlueDoc.Core.Enums;
using BlueDoc.Core.Models;
using BlueDoc.Infrastructure.Reporting;

namespace BlueDoc.Tests.Unit
{
    public class ReportGeneratorTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("[TEST SUITE] Diagnostic Report Generator Tests");

            var generator = new ReportGenerator();
            var report = new DiagnosticReport
            {
                ReportId = Guid.NewGuid(),
                GeneratedAtUtc = DateTime.UtcNow,
                ApplicationVersion = "1.0.0",
                MachineName = "TEST-PC",
                OsVersion = new OsVersionInfo { OperatingSystemName = "Windows 11 Pro", BuildNumber = 26200 },
                Adapter = new BluetoothAdapterInfo
                {
                    FriendlyName = "Realtek Bluetooth Adapter",
                    Manufacturer = "Realtek Semiconductor Corp.",
                    HardwareId = "USB\\VID_0BDA&PID_C822",
                    HardwareDetails = new HardwareIdDetails { VendorId = "0BDA", ProductId = "C822" },
                    Driver = new DriverInfo { DriverVersion = "1.1072.2312.2501", DriverProvider = "Realtek" }
                },
                Service = new ServiceInfo { ServiceName = "bthserv", DisplayName = "Bluetooth Support Service", Status = "Running", StartupType = "Manual" },
                Session = new DiagnosticSession
                {
                    StartedAt = DateTime.UtcNow.AddSeconds(-30),
                    CompletedAt = DateTime.UtcNow,
                    Results = new List<DiagnosticResult>
                    {
                        new DiagnosticResult
                        {
                            RuleId = "TEST-01",
                            TestName = "Bluetooth Service Status",
                            Status = DiagnosticStatus.Passed,
                            Summary = "bthserv is running.",
                            RecommendedAction = "No action required."
                        }
                    }
                },
                RelevantEvents = new List<BluetoothEventRecord>
                {
                    new BluetoothEventRecord
                    {
                        EventId = 3,
                        ProviderName = "BTHUSB",
                        LevelDisplayName = "Warning",
                        HumanAnalysis = "Command timed out."
                    }
                }
            };

            // Test TXT Generation
            string txt = generator.GeneratePlainText(report);
            if (string.IsNullOrWhiteSpace(txt) || !txt.Contains("BLUEDOC DIAGNOSTIC REPORT") || !txt.Contains("Realtek Bluetooth Adapter"))
                throw new Exception("Plain text report generation failed.");
            Console.WriteLine("  ✓ Plain text diagnostic report generated.");

            // Test HTML Generation
            string html = generator.GenerateHtml(report);
            if (string.IsNullOrWhiteSpace(html) || !html.Contains("<!DOCTYPE html>") || !html.Contains("Realtek Bluetooth Adapter"))
                throw new Exception("HTML report generation failed.");
            Console.WriteLine("  ✓ Styled HTML diagnostic report generated.");

            // Test JSON Generation
            string json = generator.GenerateJson(report);
            if (string.IsNullOrWhiteSpace(json) || !json.Contains("\"ReportId\":") || !json.Contains("USB\\\\VID_0BDA&PID_C822"))
                throw new Exception("JSON report generation failed.");
            Console.WriteLine("  ✓ Schema-compliant JSON diagnostic report generated.");
        }
    }
}
