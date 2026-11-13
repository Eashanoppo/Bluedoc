using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlueDoc.Application.Cloud;
using BlueDoc.Application.KnowledgeBase;
using BlueDoc.Core.Enums;
using BlueDoc.Core.Models;
using BlueDoc.Diagnostics.Benchmark;
using BlueDoc.UI.Cli;

namespace BlueDoc.Tests.Unit
{
    public static class Phase8TechnicianAndSaaSIntegrationTests
    {
        public static async Task RunAllTestsAsync()
        {
            Console.WriteLine("\n[TEST SUITE] Phase 8 Technician Toolkit, Benchmark, Knowledge Base & SaaS Telemetry Tests");

            await TestBenchmarkEngineAsync();
            TestOfflineKnowledgeBase();
            await TestSaaSCloudTelemetryAsync();
            TestCliRunnerParameterParsing();

            Console.WriteLine("All Phase 8 Technician Toolkit & SaaS Integration tests passed!");
        }

        private static async Task TestBenchmarkEngineAsync()
        {
            var engine = new BluetoothBenchmarkEngine();
            var bench = await engine.RunBenchmarkAsync();

            Assert(bench != null, "Benchmark execution returned non-null result");
            Assert(bench.OverallScore >= 0.0 && bench.OverallScore <= 100.0, $"Overall score in valid range 0..100 (Observed: {bench.OverallScore})");
            Assert(!string.IsNullOrEmpty(bench.Rating), $"Rating assigned (Observed: {bench.Rating})");
            Assert(!string.IsNullOrEmpty(bench.HardwareSpecGrade), $"Hardware spec grade assigned (Observed: {bench.HardwareSpecGrade})");
            Assert(bench.Metrics != null && bench.Metrics.Count >= 5, $"Generated at least 5 benchmark metrics (Count: {bench.Metrics?.Count})");

            var enumMetric = bench.Metrics.FirstOrDefault(m => m.Name.Contains("Enumeration"));
            Assert(enumMetric != null, "SetupAPI Enumeration metric present");
            Assert(enumMetric.TargetThreshold == "< 250 ms", "SetupAPI target threshold is < 250 ms");

            var scmMetric = bench.Metrics.FirstOrDefault(m => m.Name.Contains("SCM Services"));
            Assert(scmMetric != null, "SCM Services Query metric present");

            var hwMetric = bench.Metrics.FirstOrDefault(m => m.Name.Contains("Hardware Controller"));
            Assert(hwMetric != null, "Hardware Controller status metric present");

            Console.WriteLine($"  ✓ Bluetooth benchmark engine execution verified (Score: {bench.OverallScore:F1}/100 [{bench.Rating}], Latency: {bench.EnumerationLatencyMs}ms).");
        }

        private static void TestOfflineKnowledgeBase()
        {
            var kb = new OfflineKnowledgeBaseService();
            var all = kb.GetAllArticles();

            Assert(all != null && all.Count >= 10, $"Knowledge base initialized with at least 10 articles (Found: {all?.Count})");

            // Test 1: Search by Symptom / Keyword
            var code43Results = kb.Search("Code 43").ToList();
            Assert(code43Results.Count >= 1, "Searching 'Code 43' returns matching article");
            var code43Article = code43Results.First();
            Assert(code43Article.Id == "KB-BT-001", "Article KB-BT-001 matches Code 43");
            Assert(code43Article.Severity == "Critical", "Code 43 marked Critical severity");
            Assert(code43Article.StepByStepRemediation.Count >= 3, "Contains structured step-by-step remediation");
            Assert(code43Article.RecommendedRepairActionId == "CycleBluetoothRadio", "Linked to CycleBluetoothRadio Safe Repair");

            // Test 2: Search by RF Coexistence
            var wifiResults = kb.Search("2.4 GHz").ToList();
            Assert(wifiResults.Count >= 1, "Searching '2.4 GHz' returns Wi-Fi coexistence article");
            Assert(wifiResults.First().Id == "KB-BT-003", "Article KB-BT-003 matches 2.4GHz coexistence");

            // Test 3: Category Filtering
            var categories = kb.GetCategories().ToList();
            Assert(categories.Contains("Hardware & Bus"), "Categories contain 'Hardware & Bus'");
            Assert(categories.Contains("Audio & Coexistence"), "Categories contain 'Audio & Coexistence'");
            Assert(categories.Contains("Power Management & Sleep"), "Categories contain 'Power Management & Sleep'");

            var powerArticles = kb.GetByCategory("Power Management & Sleep").ToList();
            Assert(powerArticles.Count >= 1, "Category filter returns power management articles");

            // Test 4: Diagnostic Rule Correlation
            var hwRuleArticles = kb.GetByRuleId("RULE_HW_STATUS").ToList();
            Assert(hwRuleArticles.Count >= 1, "Rule ID correlation returns articles for RULE_HW_STATUS");

            var svcRuleArticles = kb.GetByRuleId("RULE_SERVICE_STATUS").ToList();
            Assert(svcRuleArticles.Count >= 1, "Rule ID correlation returns articles for RULE_SERVICE_STATUS");

            Console.WriteLine($"  ✓ Offline troubleshooting knowledge base verified ({all.Count} articles, full search, categories, and rule mapping).");
        }

        private static async Task TestSaaSCloudTelemetryAsync()
        {
            var saasService = new SaasTelemetryService();

            var adapter = new BluetoothAdapterInfo
            {
                FriendlyName = "Mock Fleet Adapter",
                Manufacturer = "Realtek Semiconductor Corp.",
                HardwareId = @"USB\VID_0BDA&PID_C822",
                Driver = new DriverInfo
                {
                    DriverVersion = "1.1072.2312.2501"
                }
            };

            var session = new DiagnosticSession();
            session.Results.Add(new DiagnosticResult
            {
                RuleId = "RULE_OS_COMPAT",
                TestName = "OS Compatibility",
                Status = DiagnosticStatus.Passed,
                Summary = "Supported OS"
            });
            session.Results.Add(new DiagnosticResult
            {
                RuleId = "RULE_HW_STATUS",
                TestName = "Hardware Status",
                Status = DiagnosticStatus.Warning,
                Summary = "Code 22 Disabled in Device Manager"
            });
            session.Results.Add(new DiagnosticResult
            {
                RuleId = "RULE_RADIO_AVAIL",
                TestName = "Radio Availability",
                Status = DiagnosticStatus.Failed,
                Summary = "Radio unpowered"
            });

            var benchmark = new BenchmarkResult
            {
                OverallScore = 78.5,
                Rating = "Adequate"
            };

            var payload = await saasService.BuildPayloadAsync(adapter, session, benchmark);

            Assert(!string.IsNullOrEmpty(payload.PayloadId), "PayloadId generated");
            Assert(!string.IsNullOrEmpty(payload.Hostname), "Hostname populated");
            Assert(!string.IsNullOrEmpty(payload.Architecture), "Architecture populated");
            Assert(payload.AdapterName == "Mock Fleet Adapter", "Adapter name accurately captured");
            Assert(payload.Manufacturer.Contains("Realtek"), "Manufacturer accurately captured");
            Assert(payload.HardwareId == @"USB\VID_0BDA&PID_C822", "Hardware ID accurately captured");
            Assert(payload.TotalTests == 3, "Total tests count matches");
            Assert(payload.PassedTests == 1, "Passed tests count matches");
            Assert(payload.WarningTests == 1, "Warning tests count matches");
            Assert(payload.FailedTests == 1, "Failed tests count matches");
            Assert(payload.BenchmarkScore == 78.5, "Benchmark score matches");
            Assert(payload.BenchmarkRating == "Adequate", "Benchmark rating matches");
            Assert(payload.FailedRuleSummaries.Count == 2, "Failed & Warning rule summaries recorded");

            // JSON Serialization validation
            var json = saasService.ExportPayloadJson(payload);
            Assert(json.Contains("\"payloadId\""), "JSON contains payloadId");
            Assert(json.Contains("\"mock fleet adapter\"") || json.Contains("\"Mock Fleet Adapter\""), "JSON contains adapter name");
            Assert(json.Contains("\"benchmarkScore\": 78.5"), "JSON contains benchmark score");
            Assert(json.Contains("\"failedRuleSummaries\""), "JSON contains failure summaries array");

            // Privacy and Safety: Empty or Invalid Endpoint Handling
            var emptyResult = await saasService.SyncTelemetryAsync(payload, "", "TENANT-1", "TOKEN");
            Assert(!emptyResult.Success, "Fails safely with empty endpoint");
            Assert(emptyResult.Message.Contains("not configured"), "Identifies unconfigured endpoint");

            var invalidResult = await saasService.SyncTelemetryAsync(payload, "invalid-url", "TENANT-1", "TOKEN");
            Assert(!invalidResult.Success, "Fails safely with invalid URL");

            Console.WriteLine("  ✓ Enterprise SaaS fleet telemetry payload assembly, JSON serialization & safety checks verified.");
        }

        private static void TestCliRunnerParameterParsing()
        {
            Assert(CliRunner.IsCliInvocation(new[] { "--diagnose" }), "'--diagnose' identified as CLI");
            Assert(CliRunner.IsCliInvocation(new[] { "-d" }), "'-d' identified as CLI");
            Assert(CliRunner.IsCliInvocation(new[] { "/inspect" }), "'/inspect' identified as CLI");
            Assert(CliRunner.IsCliInvocation(new[] { "--benchmark" }), "'--benchmark' identified as CLI");
            Assert(CliRunner.IsCliInvocation(new[] { "--kb", "Code 43" }), "'--kb' identified as CLI");
            Assert(CliRunner.IsCliInvocation(new[] { "--version" }), "'--version' identified as CLI");
            Assert(!CliRunner.IsCliInvocation(new string[0]), "Empty args identified as GUI launch");
            Assert(!CliRunner.IsCliInvocation(null), "Null args identified as GUI launch");

            Console.WriteLine("  ✓ Headless CLI runner parameter recognition and invocation logic verified.");
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

