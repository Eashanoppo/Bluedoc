using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using BlueDoc.Application.Services;
using BlueDoc.Core.Enums;
using BlueDoc.Core.Models;
using BlueDoc.Data.Repositories;
using BlueDoc.Infrastructure.Platform;
using BlueDoc.Infrastructure.Repairs;
using BlueDoc.Infrastructure.Reporting;
using BlueDoc.Infrastructure.Services;

namespace BlueDoc.Tests.Unit
{
    public static class Phase6RepairAndReportingTests
    {
        public static async Task RunAllTestsAsync()
        {
            Console.WriteLine("[TEST SUITE] Phase 6 Repair & Reporting Tests (PDF, Multi-Format Reports, Safe Repairs)");

            TestPdfReportGenerationAsync();
            await TestMultiFormatReportPersistenceAsync();
            await TestSafeRepairEngineOperationsAsync();

            Console.WriteLine("All Phase 6 Repair & Reporting tests passed!\n");
        }

        private static void TestPdfReportGenerationAsync()
        {
            var pdfGen = new PdfReportGenerator();
            var report = CreateSampleReport();

            byte[] pdfBytes = pdfGen.GeneratePdf(report);

            Assert(pdfBytes != null && pdfBytes.Length > 500, "pdfBytes must not be empty and > 500 bytes");

            string asciiHeader = Encoding.ASCII.GetString(pdfBytes, 0, 8);
            Assert(asciiHeader.StartsWith("%PDF-1.4"), "PDF must start with %PDF-1.4 header");

            string fullAscii = Encoding.ASCII.GetString(pdfBytes);
            Assert(fullAscii.Contains("%%EOF"), "PDF must contain %%EOF trailer");
            Assert(fullAscii.Contains("startxref"), "PDF must contain startxref table offset");
            Assert(fullAscii.Contains("/Type /Catalog"), "PDF must contain Catalog dictionary");
            Assert(fullAscii.Contains("/Type /Pages"), "PDF must contain Pages root");
            Assert(fullAscii.Contains("/Type /Page"), "PDF must contain at least one Page");
            Assert(fullAscii.Contains("BlueDoc Diagnostic Report"), "PDF text stream must contain Title");

            Console.WriteLine($"  ✓ Zero-dependency PDF generation verified (Generated valid PDF-1.4 document of {pdfBytes.Length} bytes).");
        }

        private static async Task TestMultiFormatReportPersistenceAsync()
        {
            var reportService = new ReportService();
            var report = CreateSampleReport();
            string testDir = Path.Combine(Path.GetTempPath(), "BlueDoc_Test_Reports_" + Guid.NewGuid().ToString("N"));

            try
            {
                Directory.CreateDirectory(testDir);

                // 1. Text format
                string txtPath = Path.Combine(testDir, "report.txt");
                await reportService.SaveReportToFileAsync(report, txtPath, "txt");
                Assert(File.Exists(txtPath), "report.txt must exist");
                string txtContent = File.ReadAllText(txtPath);
                Assert(txtContent.Contains("BLUEDOC DIAGNOSTIC REPORT"), "TXT contains header");
                Assert(txtContent.Contains("Realtek Bluetooth Adapter"), "TXT contains adapter name");

                // 2. HTML format
                string htmlPath = Path.Combine(testDir, "report.html");
                await reportService.SaveReportToFileAsync(report, htmlPath, "html");
                Assert(File.Exists(htmlPath), "report.html must exist");
                string htmlContent = File.ReadAllText(htmlPath);
                Assert(htmlContent.Contains("<!DOCTYPE html>"), "HTML contains doctype");
                Assert(htmlContent.Contains("badge-passed"), "HTML contains passed status badge");

                // 3. JSON format
                string jsonPath = Path.Combine(testDir, "report.json");
                await reportService.SaveReportToFileAsync(report, jsonPath, "json");
                Assert(File.Exists(jsonPath), "report.json must exist");
                string jsonContent = File.ReadAllText(jsonPath);
                Assert(jsonContent.Contains("\"ReportId\""), "JSON contains ReportId");
                Assert(jsonContent.Contains("\"OverallHealthSummary\""), "JSON contains OverallHealthSummary");

                // 4. PDF format
                string pdfPath = Path.Combine(testDir, "report.pdf");
                await reportService.SaveReportToFileAsync(report, pdfPath, "pdf");
                Assert(File.Exists(pdfPath), "report.pdf must exist");
                byte[] savedPdfBytes = File.ReadAllBytes(pdfPath);
                Assert(savedPdfBytes.Length > 500, "Saved PDF size > 500 bytes");

                Console.WriteLine("  ✓ End-to-end multi-format report persistence (TXT, HTML, JSON, PDF) verified.");
            }
            finally
            {
                if (Directory.Exists(testDir))
                {
                    try { Directory.Delete(testDir, true); } catch { }
                }
            }
        }

        private static async Task TestSafeRepairEngineOperationsAsync()
        {
            var dbPath = Path.Combine(Path.GetTempPath(), $"bluedoc_repair_test_{Guid.NewGuid():N}.db");
            var db = new BlueDoc.Data.SqliteDatabaseManager(dbPath);
            db.InitializeDatabase();

            var connRepo = new SqliteConnectionHistoryRepository(db);
            var repairRepo = new SqliteRepairHistoryRepository(db);
            var cacheRepo = new SqliteDeviceCacheRepository(db);

            var engine = new SafeRepairEngine(repairRepo, cacheRepo);

            // Test 1: Verify all repair actions exist and are properly defined
            var allRepairs = engine.GetAllRepairDefinitions();
            Assert(allRepairs.Count >= 8, $"Expected at least 8 repairs, found {allRepairs.Count}");

            var restartBthServ = engine.GetRepairDefinition("RestartBthServ");
            Assert(restartBthServ.RequiresAdmin == true, "RestartBthServ must require administrator permissions");

            var openSettings = engine.GetRepairDefinition("OpenBluetoothSettings");
            Assert(openSettings.RequiresAdmin == false, "OpenBluetoothSettings does not require admin");

            // Test 2: If running without admin, admin-gated repairs must fail safely with permission notice
            if (!engine.IsRunningAsAdministrator())
            {
                var record = await engine.ExecuteRepairAsync("RestartBthServ");
                Assert(record.Status == "Failed", "Admin repair executed as non-admin must fail safely");
                Assert(record.ErrorMessage.Contains("Administrative privileges required"), "Clear error message on missing admin");
                Assert(record.VerificationResult.Contains("Permission Denied"), "Verification records Permission Denied");

                var history = await repairRepo.GetRepairHistoryAsync(5);
                Assert(history.Count > 0, "Failed repair attempt must be logged in repair history");
            }

            // Test 3: Clear device cache repair (safe non-admin repair)
            await cacheRepo.UpsertDiscoveredDeviceAsync(new BluetoothDeviceInfo
            {
                DeviceName = "Stale Test Beacon",
                BluetoothAddress = "11:22:33:44:55:66"
            });
            var cachedBefore = await cacheRepo.GetDiscoveredDevicesAsync();
            Assert(cachedBefore.Count == 1, "Cache has 1 device before clear");

            var clearResult = await engine.ExecuteRepairAsync("ClearDeviceCache");
            Assert(clearResult.Status == "Success", "ClearDeviceCache must succeed");

            var cachedAfter = await cacheRepo.GetDiscoveredDevicesAsync();
            Assert(cachedAfter.Count == 0, "Cache has 0 devices after clear");

            // Verify logged in history
            var repairHistory = await repairRepo.GetRepairHistoryAsync(10);
            Assert(repairHistory.Exists(r => r.RepairKey == "ClearDeviceCache" && r.Status == "Success"), "ClearDeviceCache logged in audit history");

            // Test 4: Clear repair history
            await repairRepo.ClearRepairHistoryAsync();
            var clearedHistory = await repairRepo.GetRepairHistoryAsync(10);
            Assert(clearedHistory.Count == 0, "Repair history must be 0 after clear");

            Console.WriteLine("  ✓ Safe Repair workflow (permission check, execution, verification, and audit trail) verified.");
        }

        private static DiagnosticReport CreateSampleReport()
        {
            var os = OperatingSystemInspector.GetOperatingSystemInfo();
            var report = new DiagnosticReport
            {
                ReportId = Guid.NewGuid(),
                GeneratedAtUtc = DateTime.UtcNow,
                ApplicationVersion = "1.0.0 (Production Release)",
                MachineName = Environment.MachineName,
                OsVersion = os,
                OverallHealthSummary = "Passed",
                Adapter = new BluetoothAdapterInfo
                {
                    FriendlyName = "Realtek Bluetooth Adapter",
                    Manufacturer = "Realtek Semiconductor Corp.",
                    HardwareId = @"USB\VID_0BDA&PID_C822",
                    HardwareDetails = new HardwareIdDetails
                    {
                        VendorId = "0BDA",
                        ProductId = "C822"
                    },
                    Driver = new DriverInfo
                    {
                        DriverProvider = "Realtek Semiconductor Corp.",
                        DriverVersion = "1.1072.2312.2501",
                        DriverDate = new DateTime(2023, 12, 25)
                    }
                },
                Service = new ServiceInfo
                {
                    ServiceName = "bthserv",
                    DisplayName = "Bluetooth Support Service",
                    Status = "Running",
                    StartupType = "Manual"
                },
                Session = new DiagnosticSession
                {
                    SessionId = Guid.NewGuid(),
                    Results = new List<DiagnosticResult>
                    {
                        new DiagnosticResult { RuleId = "BTH-001", TestName = "Operating System Compatibility", Status = DiagnosticStatus.Passed, Summary = "Supported OS kernel", RecommendedAction = "No action required." },
                        new DiagnosticResult { RuleId = "BTH-002", TestName = "Bluetooth Adapter Detection", Status = DiagnosticStatus.Passed, Summary = "Realtek Adapter found", RecommendedAction = "No action required." },
                        new DiagnosticResult { RuleId = "BTH-003", TestName = "Adapter Hardware Status", Status = DiagnosticStatus.Passed, Summary = "Operational", RecommendedAction = "No action required." },
                        new DiagnosticResult { RuleId = "BTH-004", TestName = "Bluetooth Driver Health", Status = DiagnosticStatus.Passed, Summary = "Driver installed and active", RecommendedAction = "No action required." },
                        new DiagnosticResult { RuleId = "BTH-005", TestName = "Bluetooth Support Service Status", Status = DiagnosticStatus.Passed, Summary = "bthserv is running", RecommendedAction = "No action required." },
                        new DiagnosticResult { RuleId = "BTH-006", TestName = "Bluetooth Audio Services Status", Status = DiagnosticStatus.Passed, Summary = "Audio Gateway services operational", RecommendedAction = "No action required." },
                        new DiagnosticResult { RuleId = "BTH-007", TestName = "Bluetooth Radio Availability", Status = DiagnosticStatus.Passed, Summary = "Radio is responsive", RecommendedAction = "No action required." },
                        new DiagnosticResult { RuleId = "BTH-008", TestName = "Device Discovery Readiness", Status = DiagnosticStatus.Passed, Summary = "Inquiry scanner ready", RecommendedAction = "No action required." },
                        new DiagnosticResult { RuleId = "BTH-009", TestName = "Pairing & Security Subsystem Readiness", Status = DiagnosticStatus.Passed, Summary = "Pairing subsystem ready", RecommendedAction = "No action required." },
                        new DiagnosticResult { RuleId = "BTH-010", TestName = "Bluetooth Low Energy (BLE) Availability", Status = DiagnosticStatus.Passed, Summary = "BLE 4.0+ supported", RecommendedAction = "No action required." },
                        new DiagnosticResult { RuleId = "BTH-011", TestName = "BLE GATT Client & Protocol Support", Status = DiagnosticStatus.Passed, Summary = "GATT Client APIs supported", RecommendedAction = "No action required." },
                        new DiagnosticResult { RuleId = "BTH-012", TestName = "Bluetooth Event Log Health & Stability", Status = DiagnosticStatus.Passed, Summary = "System log normal", RecommendedAction = "No action required." }
                    }
                },
                RelevantEvents = new List<BluetoothEventRecord>
                {
                    new BluetoothEventRecord
                    {
                        ProviderName = "bthserv",
                        EventId = 1,
                        LevelDisplayName = "Information",
                        HumanAnalysis = "Bluetooth Support Service started successfully.",
                        TimeCreated = DateTime.UtcNow.AddHours(-1)
                    }
                },
                Devices = new List<BluetoothDeviceInfo>
                {
                    new BluetoothDeviceInfo
                    {
                        DeviceName = "BT5.4 Mouse",
                        BluetoothAddress = "49:9B:95:52:3E:49",
                        CategoryName = "Mouse / Pointing Device",
                        IsPaired = true
                    }
                }
            };

            return report;
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception($"[TEST FAILURE] Assertion failed: {message}");
            }
        }
    }
}
