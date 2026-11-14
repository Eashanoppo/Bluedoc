using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using BlueDoc.Application.Cloud;
using BlueDoc.Tests.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BlueDoc.Tests.Unit
{
    public static class Phase10SaasFleetAndApiGatewayTests
    {
        public static async Task RunAllTestsAsync()
        {
            Console.WriteLine("\n[TEST SUITE] Phase 10 SaaS Fleet Central, REST API Gateway & Loopback Ingestion Tests");

            await TestMockSaasGatewayAndLoopbackAsync();
            TestFleetMetricsCalculations();

            Console.WriteLine("All Phase 10 SaaS Fleet Central & API Gateway tests passed!");
        }

        private static async Task TestMockSaasGatewayAndLoopbackAsync()
        {
            int testPort = 48822;
            string testToken = "unleft_live_token_2026";
            var gateway = new MockSaasCloudApiGateway(testPort, testToken);

            try
            {
                // 1. Test Gateway Startup
                bool started = await gateway.StartAsync();
                Assert(started, "Gateway started successfully on test port 48822");
                Assert(gateway.IsRunning, "Gateway IsRunning is true");

                // 2. Test GET /v1/health (Unauthenticated public endpoint)
                var healthReq = (HttpWebRequest)WebRequest.Create($"http://localhost:{testPort}/v1/health");
                healthReq.Method = "GET";
                using (var healthRes = (HttpWebResponse)await healthReq.GetResponseAsync())
                {
                    Assert((int)healthRes.StatusCode == 200, "Health check returned HTTP 200");
                    using (var reader = new StreamReader(healthRes.GetResponseStream()))
                    {
                        var body = await reader.ReadToEndAsync();
                        var obj = JObject.Parse(body);
                        Assert((string)obj["status"] == "UP", "Service reports status UP");
                    }
                }

                // 3. Test Authentication Gate (Unauthorized Request)
                var unauthReq = (HttpWebRequest)WebRequest.Create($"http://localhost:{testPort}/v1/fleet/metrics");
                unauthReq.Method = "GET";
                try
                {
                    using (var unauthRes = (HttpWebResponse)await unauthReq.GetResponseAsync())
                    {
                        Assert(false, "Unauthenticated request should have thrown WebException");
                    }
                }
                catch (WebException wex)
                {
                    if (wex.Response is HttpWebResponse errRes)
                    {
                        Assert((int)errRes.StatusCode == 401, "Rejected unauthenticated request with HTTP 401 Unauthorized");
                    }
                    else
                    {
                        throw;
                    }
                }

                // 4. Test Authenticated Ingestion (POST /v1/telemetry)
                var telemetryPayload = new FleetTelemetryPayload
                {
                    PayloadId = Guid.NewGuid().ToString(),
                    Hostname = "QA-TEST-MACHINE-01",
                    MachineGuid = "MOCK-GUID-QA01",
                    OperatingSystem = "Windows 11 Pro",
                    AdapterName = "Realtek RTL8852BE Controller",
                    Manufacturer = "Realtek",
                    HardwareId = @"USB\VID_0BDA&PID_C822",
                    DriverVersion = "1.1072.2312.2501",
                    OverallHealthStatus = "Passed",
                    BenchmarkScore = 94.5,
                    TotalTests = 12,
                    PassedTests = 12,
                    WarningTests = 0,
                    FailedTests = 0
                };

                var postReq = (HttpWebRequest)WebRequest.Create($"http://localhost:{testPort}/v1/telemetry");
                postReq.Method = "POST";
                postReq.ContentType = "application/json";
                postReq.Headers.Add("Authorization", $"Bearer {testToken}");
                postReq.Headers.Add("X-Tenant-Id", "UNLEFT-QA-ORG");

                var jsonStr = JsonConvert.SerializeObject(telemetryPayload);
                var postBytes = Encoding.UTF8.GetBytes(jsonStr);
                postReq.ContentLength = postBytes.Length;

                using (var stream = await postReq.GetRequestStreamAsync())
                {
                    await stream.WriteAsync(postBytes, 0, postBytes.Length);
                }

                using (var postRes = (HttpWebResponse)await postReq.GetResponseAsync())
                {
                    Assert((int)postRes.StatusCode == 200, "Telemetry ingestion returned HTTP 200 OK");
                    using (var reader = new StreamReader(postRes.GetResponseStream()))
                    {
                        var resBody = await reader.ReadToEndAsync();
                        var resObj = JObject.Parse(resBody);
                        Assert((string)resObj["status"] == "success", "Response confirmed success");
                    }
                }

                // Verify the machine is now in the gateway collection
                var machines = gateway.GetAllMachines();
                var ingested = machines.FirstOrDefault(m => m.Hostname == "QA-TEST-MACHINE-01");
                Assert(ingested != null, "Ingested machine record found in gateway registry");
                Assert(ingested.HealthStatus == "Healthy", "HealthStatus mapped correctly to Healthy");
                Assert(ingested.BenchmarkScore == 94.5, "Benchmark score preserved accurately");

                Console.WriteLine("  ✓ Embedded REST API gateway startup, Bearer authentication, and loopback telemetry ingestion verified.");
            }
            finally
            {
                await gateway.StopAsync();
                Assert(!gateway.IsRunning, "Gateway stopped and cleaned up cleanly");
            }
        }

        private static void TestFleetMetricsCalculations()
        {
            var gateway = new MockSaasCloudApiGateway(48823);
            var metrics = gateway.GetFleetMetrics();

            Assert(metrics != null, "Metrics object calculated");
            Assert(metrics.TotalEndpoints >= 5, "Seed data contains at least 5 default enterprise endpoints");
            Assert(metrics.HealthyCount + metrics.WarningCount + metrics.CriticalCount == metrics.TotalEndpoints, "Sum of health states matches total endpoints");
            Assert(metrics.FleetAverageScore > 0.0 && metrics.FleetAverageScore <= 100.0, $"Fleet average score in valid range (Observed: {metrics.FleetAverageScore})");
            Assert(metrics.CompliancePercentage >= 0.0 && metrics.CompliancePercentage <= 100.0, $"Compliance percentage in valid range (Observed: {metrics.CompliancePercentage}%)");

            // Test filtering logic
            var vm = new BlueDoc.UI.ViewModels.SaasFleetDashboardViewModel();
            vm.LoadFleetMetrics(metrics);

            Assert(vm.Metrics != null, "Dashboard ViewModel loaded metrics");
            Assert(vm.FilteredMachines != null && vm.FilteredMachines.Count >= 5, "Dashboard loaded filtered machines list");

            // Filter by department
            vm.SelectedDepartment = "Engineering";
            Assert(vm.FilteredMachines.All(m => m.Department == "Engineering"), "Filtered by Engineering department");

            // Filter by status
            vm.SelectedDepartment = "All";
            vm.SelectedStatusFilter = "Critical";
            Assert(vm.FilteredMachines.All(m => m.HealthStatus == "Critical"), "Filtered by Critical status");

            // Reset filters
            vm.SelectedStatusFilter = "All";
            Assert(vm.FilteredMachines.Count >= 5, "Reset filter restored all machines");

            Console.WriteLine($"  ✓ Enterprise fleet health metrics calculation & multi-attribute filtering verified (Total: {metrics.TotalEndpoints}, Avg: {metrics.FleetAverageScore:F1}/100, Compliance: {metrics.CompliancePercentage}%).");
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

