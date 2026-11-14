using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BlueDoc.Application.Cloud
{
    public class CloudFleetClient : ICloudFleetClient
    {
        private static readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        private readonly IOfflineTelemetryQueue _queue;

        public string EndpointUrl { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(EndpointUrl) &&
            !string.IsNullOrWhiteSpace(ApiKey);

        public CloudFleetClient(IOfflineTelemetryQueue queue = null)
        {
            _queue = queue ?? new SqliteOfflineTelemetryQueue();
        }

        // ── Telemetry Push ─────────────────────────────────────────────────
        public async Task<SaasSyncResult> PushTelemetryAsync(
            FleetTelemetryPayload payload, CancellationToken ct = default)
        {
            if (!IsConfigured)
            {
                return Fail("Cloud endpoint not configured.");
            }

            // Drain any previously queued payloads (non-blocking best-effort)
            _ = DrainQueueAsync(ct);

            return await PostPayloadAsync(payload, ct);
        }

        private async Task<SaasSyncResult> PostPayloadAsync(
            FleetTelemetryPayload payload, CancellationToken ct)
        {
            try
            {
                var json = JsonConvert.SerializeObject(payload);
                var url = $"{EndpointUrl.TrimEnd('/')}/v1/telemetry";

                using (var req = new HttpRequestMessage(HttpMethod.Post, url))
                {
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);

                    if (!string.IsNullOrWhiteSpace(TenantId))
                    {
                        req.Headers.Add("X-Tenant-Id", TenantId);
                    }

                    req.Content = new StringContent(json, Encoding.UTF8, "application/json");

                    using (var resp = await _http.SendAsync(req, ct))
                    {
                        var body = await resp.Content.ReadAsStringAsync();

                        if (resp.IsSuccessStatusCode)
                        {
                            return new SaasSyncResult
                            {
                                Success = true,
                                HttpStatusCode = (int)resp.StatusCode,
                                Message = $"Accepted. Server: {body}"
                            };
                        }

                        // Server-side error — queue for retry
                        await _queue.EnqueueAsync(payload);
                        return Fail($"HTTP {(int)resp.StatusCode}: {body}");
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                // Network unreachable — queue for retry
                await _queue.EnqueueAsync(payload);
                return Fail($"Network error (queued for retry): {ex.Message}");
            }
            catch (TaskCanceledException)
            {
                await _queue.EnqueueAsync(payload);
                return Fail("Request timed out (queued for retry).");
            }
            catch (Exception ex)
            {
                await _queue.EnqueueAsync(payload);
                return Fail($"Transmission error (queued for retry): {ex.Message}");
            }
        }

        private async Task DrainQueueAsync(CancellationToken ct)
        {
            try
            {
                var pending = await _queue.DequeueAllAsync();
                foreach (var old in pending)
                {
                    var r = await PostPayloadAsync(old, ct);
                    if (!r.Success)
                    {
                        break; // Connection is still failing, leave rest in queue
                    }
                    else
                    {
                        await _queue.MarkSentAsync(old.PayloadId);
                    }
                }
            }
            catch
            {
                // Queue drain is best-effort background operation
            }
        }

        // ── Fleet Metrics ───────────────────────────────────────────────────
        public async Task<FleetSummaryMetrics> GetFleetMetricsAsync(
            string statusFilter = "all", string search = "", CancellationToken ct = default)
        {
            if (!IsConfigured) return new FleetSummaryMetrics();

            try
            {
                var url = $"{EndpointUrl.TrimEnd('/')}/v1/fleet/metrics?status={Uri.EscapeDataString(statusFilter)}&search={Uri.EscapeDataString(search)}";
                using (var req = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);

                    using (var resp = await _http.SendAsync(req, ct))
                    {
                        resp.EnsureSuccessStatusCode();
                        var json = await resp.Content.ReadAsStringAsync();
                        return JsonConvert.DeserializeObject<FleetSummaryMetrics>(json) ?? new FleetSummaryMetrics();
                    }
                }
            }
            catch
            {
                return new FleetSummaryMetrics();
            }
        }

        // ── Alert Retrieval ──────────────────────────────────────────────────
        public async Task<CloudAlertList> GetAlertsAsync(CancellationToken ct = default)
        {
            if (!IsConfigured) return new CloudAlertList();

            try
            {
                var url = $"{EndpointUrl.TrimEnd('/')}/v1/alerts";
                using (var req = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);

                    using (var resp = await _http.SendAsync(req, ct))
                    {
                        resp.EnsureSuccessStatusCode();
                        var json = await resp.Content.ReadAsStringAsync();
                        return JsonConvert.DeserializeObject<CloudAlertList>(json) ?? new CloudAlertList();
                    }
                }
            }
            catch
            {
                return new CloudAlertList();
            }
        }

        // ── Alert Acknowledgement ────────────────────────────────────────────
        public async Task<bool> AcknowledgeAlertAsync(string alertId, CancellationToken ct = default)
        {
            if (!IsConfigured || string.IsNullOrWhiteSpace(alertId)) return false;

            try
            {
                var url = $"{EndpointUrl.TrimEnd('/')}/v1/alerts/{Uri.EscapeDataString(alertId)}/acknowledge";
                using (var req = new HttpRequestMessage(HttpMethod.Post, url))
                {
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);
                    req.Content = new StringContent("{}", Encoding.UTF8, "application/json");

                    using (var resp = await _http.SendAsync(req, ct))
                    {
                        return resp.IsSuccessStatusCode;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        // ── Tenant Registration ──────────────────────────────────────────────
        public async Task<TenantRegistrationResult> RegisterTenantAsync(
            string orgName, string adminEmail, string plan, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(EndpointUrl))
            {
                return new TenantRegistrationResult
                {
                    Success = false,
                    ErrorMessage = "Endpoint URL must be specified before registering."
                };
            }

            try
            {
                var payload = new
                {
                    organizationName = orgName,
                    adminEmail = adminEmail,
                    plan = plan
                };

                var url = $"{EndpointUrl.TrimEnd('/')}/v1/auth/register-tenant";
                using (var req = new HttpRequestMessage(HttpMethod.Post, url))
                {
                    req.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

                    using (var resp = await _http.SendAsync(req, ct))
                    {
                        var json = await resp.Content.ReadAsStringAsync();
                        if (!resp.IsSuccessStatusCode)
                        {
                            return new TenantRegistrationResult
                            {
                                Success = false,
                                ErrorMessage = $"Registration failed (HTTP {(int)resp.StatusCode}): {json}"
                            };
                        }

                        var result = JsonConvert.DeserializeObject<TenantRegistrationResult>(json);
                        if (result != null)
                        {
                            result.Success = true;
                            return result;
                        }

                        return new TenantRegistrationResult
                        {
                            Success = false,
                            ErrorMessage = "Server returned empty registration response."
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new TenantRegistrationResult
                {
                    Success = false,
                    ErrorMessage = $"Connection failure during registration: {ex.Message}"
                };
            }
        }

        private static SaasSyncResult Fail(string msg) =>
            new SaasSyncResult { Success = false, Message = msg };
    }
}

