using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BlueDoc.Application.Cloud
{
    public class MockSaasCloudApiGateway : IDisposable
    {
        private HttpListener _listener;
        private CancellationTokenSource _cts;
        private readonly ConcurrentDictionary<string, FleetMachineRecord> _fleetMachines;
        private int _port;
        private bool _isRunning;
        private readonly string _expectedApiKey;

        public bool IsRunning => _isRunning;
        public int Port => _port;
        public string BaseUrl => $"http://localhost:{_port}/";

        public event EventHandler<FleetMachineRecord> TelemetryIngested;
        public event EventHandler<string> GatewayLogEmitted;

        public MockSaasCloudApiGateway(int port = 48821, string expectedApiKey = "unleft_live_token_2026")
        {
            _port = port;
            _expectedApiKey = expectedApiKey;
            _fleetMachines = new ConcurrentDictionary<string, FleetMachineRecord>(StringComparer.OrdinalIgnoreCase);

            SeedDefaultFleet();
        }

        public IReadOnlyList<FleetMachineRecord> GetAllMachines() =>
            _fleetMachines.Values.OrderByDescending(m => m.LastHeartbeatUtc).ToList();

        public FleetSummaryMetrics GetFleetMetrics()
        {
            var list = GetAllMachines();
            var total = list.Count;
            var healthy = list.Count(m => m.HealthStatus == "Healthy");
            var warning = list.Count(m => m.HealthStatus == "Warning");
            var critical = list.Count(m => m.HealthStatus == "Critical");
            var avgScore = total > 0 ? Math.Round(list.Average(m => m.BenchmarkScore), 1) : 0;
            var compliant = total > 0 ? Math.Round((double)list.Count(m => m.IsCompliant) / total * 100.0, 1) : 100.0;

            return new FleetSummaryMetrics
            {
                TotalEndpoints = total,
                HealthyCount = healthy,
                WarningCount = warning,
                CriticalCount = critical,
                FleetAverageScore = avgScore,
                CompliancePercentage = compliant,
                Machines = list.ToList()
            };
        }

        public Task<bool> StartAsync()
        {
            return Task.Run(() =>
            {
                if (_isRunning) return true;

                try
                {
                    _listener = new HttpListener();
                    _listener.Prefixes.Add($"http://localhost:{_port}/");
                    _listener.Start();

                    _cts = new CancellationTokenSource();
                    _isRunning = true;

                    Task.Run(() => ListenLoopAsync(_cts.Token));
                    GatewayLogEmitted?.Invoke(this, $"[SaaS Gateway] REST API server started on http://localhost:{_port}/");
                    return true;
                }
                catch (Exception ex)
                {
                    _isRunning = false;
                    GatewayLogEmitted?.Invoke(this, $"[SaaS Gateway ERROR] Failed to start listener: {ex.Message}");
                    return false;
                }
            });
        }

        public Task StopAsync()
        {
            return Task.Run(() =>
            {
                if (!_isRunning) return;

                _isRunning = false;
                _cts?.Cancel();
                try
                {
                    _listener?.Stop();
                    _listener?.Close();
                }
                catch { }
                _listener = null;

                GatewayLogEmitted?.Invoke(this, "[SaaS Gateway] REST API server stopped.");
            });
        }

        private async Task ListenLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested && _listener != null && _listener.IsListening)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    _ = ProcessRequestAsync(context);
                }
                catch (HttpListenerException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (token.IsCancellationRequested) break;
                    GatewayLogEmitted?.Invoke(this, $"[SaaS Gateway Loop Exception] {ex.Message}");
                }
            }
        }

        private async Task ProcessRequestAsync(HttpListenerContext context)
        {
            var req = context.Request;
            var res = context.Response;

            // CORS headers for modern web interoperability
            res.Headers.Add("Access-Control-Allow-Origin", "*");
            res.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization, X-Tenant-Id");
            res.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");

            if (req.HttpMethod == "OPTIONS")
            {
                res.StatusCode = 200;
                res.Close();
                return;
            }

            var path = req.Url.AbsolutePath.ToLowerInvariant().TrimEnd('/');

            try
            {
                // Health Check
                if (req.HttpMethod == "GET" && path == "/v1/health")
                {
                    await WriteJsonResponseAsync(res, 200, new
                    {
                        status = "UP",
                        service = "BlueDoc Enterprise SaaS Gateway",
                        version = "1.0.0",
                        activeMachines = _fleetMachines.Count
                    });
                    return;
                }

                // Authentication Check
                var authHeader = req.Headers["Authorization"] ?? string.Empty;
                var hasValidBearer = string.IsNullOrEmpty(_expectedApiKey) ||
                                     authHeader.Equals($"Bearer {_expectedApiKey}", StringComparison.OrdinalIgnoreCase) ||
                                     authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase);

                if (!hasValidBearer)
                {
                    await WriteJsonResponseAsync(res, 401, new
                    {
                        error = "Unauthorized",
                        message = "Valid Bearer API Token required in Authorization header."
                    });
                    return;
                }

                // Route: GET /v1/fleet/metrics
                if (req.HttpMethod == "GET" && path == "/v1/fleet/metrics")
                {
                    var metrics = GetFleetMetrics();
                    await WriteJsonResponseAsync(res, 200, metrics);
                    return;
                }

                // Route: GET /v1/fleet/machines
                if (req.HttpMethod == "GET" && path == "/v1/fleet/machines")
                {
                    var list = GetAllMachines();
                    await WriteJsonResponseAsync(res, 200, list);
                    return;
                }

                // Route: POST /v1/telemetry
                if (req.HttpMethod == "POST" && path == "/v1/telemetry")
                {
                    string body;
                    using (var reader = new StreamReader(req.InputStream, req.ContentEncoding))
                    {
                        body = await reader.ReadToEndAsync();
                    }

                    var payload = JsonConvert.DeserializeObject<FleetTelemetryPayload>(body);
                    if (payload == null)
                    {
                        await WriteJsonResponseAsync(res, 400, new { error = "Invalid JSON body format." });
                        return;
                    }

                    var tenant = req.Headers["X-Tenant-Id"] ?? "UNLEFT-DEFAULT";

                    var record = new FleetMachineRecord
                    {
                        MachineId = !string.IsNullOrEmpty(payload.MachineGuid) ? payload.MachineGuid : payload.Hostname,
                        Hostname = payload.Hostname,
                        OrganizationId = tenant,
                        Department = "Corporate Endpoint",
                        OperatingSystem = payload.OperatingSystem,
                        PrimaryAdapter = payload.AdapterName,
                        ChipsetManufacturer = payload.Manufacturer,
                        DriverVersion = payload.DriverVersion,
                        HealthStatus = payload.OverallHealthStatus == "Passed" ? "Healthy" : (payload.OverallHealthStatus == "Warning" ? "Warning" : "Critical"),
                        BenchmarkScore = payload.BenchmarkScore ?? 85.0,
                        LastHeartbeatUtc = DateTime.UtcNow,
                        FailureAlerts = payload.FailedRuleSummaries ?? new List<string>(),
                        IsCompliant = payload.OverallHealthStatus != "Failed",
                        ComplianceIssue = payload.OverallHealthStatus == "Failed" ? "Diagnostic Failure Detected" : "Compliant"
                    };

                    _fleetMachines[record.MachineId] = record;
                    TelemetryIngested?.Invoke(this, record);
                    GatewayLogEmitted?.Invoke(this, $"[SaaS Ingest] Received heartbeat from '{record.Hostname}' ({record.HealthStatus})");

                    await WriteJsonResponseAsync(res, 200, new
                    {
                        status = "success",
                        message = "Diagnostic telemetry processed and recorded.",
                        machineId = record.MachineId,
                        receivedAtUtc = DateTime.UtcNow
                    });
                    return;
                }

                // Not Found
                await WriteJsonResponseAsync(res, 404, new { error = "Endpoint route not found." });
            }
            catch (Exception ex)
            {
                await WriteJsonResponseAsync(res, 500, new { error = "Internal server error", details = ex.Message });
            }
        }

        private static async Task WriteJsonResponseAsync(HttpListenerResponse res, int statusCode, object data)
        {
            var json = JsonConvert.SerializeObject(data, Formatting.Indented);
            var buffer = Encoding.UTF8.GetBytes(json);

            res.StatusCode = statusCode;
            res.ContentType = "application/json; charset=utf-8";
            res.ContentLength64 = buffer.Length;

            using (var stream = res.OutputStream)
            {
                await stream.WriteAsync(buffer, 0, buffer.Length);
            }
        }

        private void SeedDefaultFleet()
        {
            var seedData = new List<FleetMachineRecord>
            {
                new FleetMachineRecord
                {
                    MachineId = "CORP-W11-ENG01",
                    Hostname = "CORP-DEV-LAPTOP",
                    Department = "Engineering",
                    OperatingSystem = "Windows 11 (Build 26200)",
                    PrimaryAdapter = "Intel Wi-Fi 6E AX210 Bluetooth",
                    ChipsetManufacturer = "Intel Corporation",
                    DriverVersion = "23.40.0.4",
                    HealthStatus = "Healthy",
                    BenchmarkScore = 95.0,
                    LastHeartbeatUtc = DateTime.UtcNow.AddMinutes(-5),
                    IsCompliant = true,
                    ComplianceIssue = "Optimal Status"
                },
                new FleetMachineRecord
                {
                    MachineId = "CORP-W10-SALES02",
                    Hostname = "SALES-THINKPAD-02",
                    Department = "Sales",
                    OperatingSystem = "Windows 10 (Build 19045)",
                    PrimaryAdapter = "Realtek RTL8822CE Bluetooth",
                    ChipsetManufacturer = "Realtek Semiconductor Corp.",
                    DriverVersion = "1.1072.2312.2501",
                    HealthStatus = "Healthy",
                    BenchmarkScore = 88.0,
                    LastHeartbeatUtc = DateTime.UtcNow.AddMinutes(-12),
                    IsCompliant = true,
                    ComplianceIssue = "Optimal Status"
                },
                new FleetMachineRecord
                {
                    MachineId = "CORP-W11-OPS03",
                    Hostname = "OPS-DESKTOP-03",
                    Department = "Operations",
                    OperatingSystem = "Windows 11 (Build 22631)",
                    PrimaryAdapter = "MediaTek MT7921 Bluetooth Adapter",
                    ChipsetManufacturer = "MediaTek Inc.",
                    DriverVersion = "1.3.14.120",
                    HealthStatus = "Warning",
                    BenchmarkScore = 64.0,
                    LastHeartbeatUtc = DateTime.UtcNow.AddMinutes(-20),
                    FailureAlerts = new List<string> { "RULE_DRIVER_HEALTH: Known driver regression detected (MT7921 coexistence drop)" },
                    IsCompliant = false,
                    ComplianceIssue = "Outdated driver revision with known regression"
                },
                new FleetMachineRecord
                {
                    MachineId = "CORP-W7-LEGACY04",
                    Hostname = "FACTORY-STATION-04",
                    Department = "Manufacturing",
                    OperatingSystem = "Windows 7 SP1 (Build 7601)",
                    PrimaryAdapter = "Broadcom BCM20702 USB Dongle",
                    ChipsetManufacturer = "Broadcom Inc.",
                    DriverVersion = "12.0.1.940",
                    HealthStatus = "Warning",
                    BenchmarkScore = 72.0,
                    LastHeartbeatUtc = DateTime.UtcNow.AddMinutes(-45),
                    FailureAlerts = new List<string> { "RULE_BLE_AVAIL: WinRT BLE not supported on Windows 7 (Classic Mode only)" },
                    IsCompliant = true,
                    ComplianceIssue = "Legacy OS (Classic Bluetooth only)"
                },
                new FleetMachineRecord
                {
                    MachineId = "CORP-W10-FIN05",
                    Hostname = "FINANCE-SECURE-05",
                    Department = "Finance",
                    OperatingSystem = "Windows 10 (Build 19045)",
                    PrimaryAdapter = "Generic Bluetooth Adapter",
                    ChipsetManufacturer = "Microsoft",
                    DriverVersion = "10.0.19041.1",
                    HealthStatus = "Critical",
                    BenchmarkScore = 35.0,
                    LastHeartbeatUtc = DateTime.UtcNow.AddMinutes(-2),
                    FailureAlerts = new List<string>
                    {
                        "RULE_HW_STATUS: Hardware reports CM_PROB_FAILED_POST_START (Code 43)",
                        "RULE_SERVICE_STATUS: Bluetooth Support Service stopped"
                    },
                    IsCompliant = false,
                    ComplianceIssue = "Controller Lockup (Code 43) & Stopped bthserv"
                }
            };

            foreach (var m in seedData)
            {
                _fleetMachines[m.MachineId] = m;
            }
        }

        public void Dispose()
        {
            StopAsync().GetAwaiter().GetResult();
        }
    }
}

