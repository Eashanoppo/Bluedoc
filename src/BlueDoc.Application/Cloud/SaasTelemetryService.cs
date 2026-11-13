using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BlueDoc.Core.Enums;
using BlueDoc.Core.Models;
using BlueDoc.Diagnostics.Benchmark;
using BlueDoc.Infrastructure.Platform;
using Microsoft.Win32;

namespace BlueDoc.Application.Cloud
{
    public class SaasTelemetryService : ISaasTelemetryService
    {
        public Task<FleetTelemetryPayload> BuildPayloadAsync(
            BluetoothAdapterInfo adapter,
            DiagnosticSession session,
            BenchmarkResult benchmark)
        {
            return Task.Run(() =>
            {
                var os = OperatingSystemInspector.GetOperatingSystemInfo();
                var payload = new FleetTelemetryPayload
                {
                    Hostname = Environment.MachineName,
                    MachineGuid = GetMachineGuid(),
                    OperatingSystem = os.OperatingSystemName,
                    OsBuild = os.BuildNumber.ToString(),
                    Architecture = Environment.Is64BitOperatingSystem ? "x64" : "x86"
                };

                if (adapter != null)
                {
                    payload.AdapterName = adapter.FriendlyName;
                    payload.Manufacturer = !string.IsNullOrEmpty(adapter.Manufacturer) 
                        ? adapter.Manufacturer 
                        : adapter.HardwareDetails?.IdentifiedManufacturer ?? "Unknown";
                    payload.HardwareId = adapter.HardwareId;
                    payload.DriverVersion = adapter.Driver?.DriverVersion ?? "None";
                }

                if (session != null)
                {
                    payload.OverallHealthStatus = session.OverallStatus.ToString();
                    payload.TotalTests = session.Results.Count;
                    payload.PassedTests = session.Results.Count(r => r.Status == DiagnosticStatus.Passed);
                    payload.WarningTests = session.Results.Count(r => r.Status == DiagnosticStatus.Warning);
                    payload.FailedTests = session.Results.Count(r => r.Status == DiagnosticStatus.Failed);

                    payload.FailedRuleSummaries = session.Results
                        .Where(r => r.Status == DiagnosticStatus.Failed || r.Status == DiagnosticStatus.Warning)
                        .Select(r => $"[{r.Status}] {r.TestName}: {r.Summary}")
                        .ToList();
                }

                if (benchmark != null)
                {
                    payload.BenchmarkScore = benchmark.OverallScore;
                    payload.BenchmarkRating = benchmark.Rating;
                }

                return payload;
            });
        }

        public string ExportPayloadJson(FleetTelemetryPayload payload)
        {
            if (payload == null) return "{}";

            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"payloadId\": \"{Escape(payload.PayloadId)}\",");
            sb.AppendLine($"  \"generatedAtUtc\": \"{payload.GeneratedAtUtc:O}\",");
            sb.AppendLine($"  \"hostname\": \"{Escape(payload.Hostname)}\",");
            sb.AppendLine($"  \"machineGuid\": \"{Escape(payload.MachineGuid)}\",");
            sb.AppendLine($"  \"operatingSystem\": \"{Escape(payload.OperatingSystem)}\",");
            sb.AppendLine($"  \"osBuild\": \"{Escape(payload.OsBuild)}\",");
            sb.AppendLine($"  \"architecture\": \"{Escape(payload.Architecture)}\",");
            sb.AppendLine($"  \"adapterName\": \"{Escape(payload.AdapterName)}\",");
            sb.AppendLine($"  \"manufacturer\": \"{Escape(payload.Manufacturer)}\",");
            sb.AppendLine($"  \"hardwareId\": \"{Escape(payload.HardwareId)}\",");
            sb.AppendLine($"  \"driverVersion\": \"{Escape(payload.DriverVersion)}\",");
            sb.AppendLine($"  \"overallHealthStatus\": \"{Escape(payload.OverallHealthStatus)}\",");
            sb.AppendLine($"  \"totalTests\": {payload.TotalTests},");
            sb.AppendLine($"  \"passedTests\": {payload.PassedTests},");
            sb.AppendLine($"  \"warningTests\": {payload.WarningTests},");
            sb.AppendLine($"  \"failedTests\": {payload.FailedTests},");
            sb.AppendLine($"  \"benchmarkScore\": {(payload.BenchmarkScore.HasValue ? payload.BenchmarkScore.Value.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) : "null")},");
            sb.AppendLine($"  \"benchmarkRating\": \"{Escape(payload.BenchmarkRating)}\",");
            sb.AppendLine("  \"failedRuleSummaries\": [");
            for (int i = 0; i < payload.FailedRuleSummaries.Count; i++)
            {
                var comma = (i < payload.FailedRuleSummaries.Count - 1) ? "," : "";
                sb.AppendLine($"    \"{Escape(payload.FailedRuleSummaries[i])}\"{comma}");
            }
            sb.AppendLine("  ]");
            sb.AppendLine("}");

            return sb.ToString();
        }

        public async Task<SaasSyncResult> SyncTelemetryAsync(
            FleetTelemetryPayload payload,
            string endpointUrl,
            string tenantId,
            string apiKey,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = new SaasSyncResult();

            if (string.IsNullOrWhiteSpace(endpointUrl))
            {
                result.Success = false;
                result.Message = "Cloud endpoint URL is not configured.";
                return result;
            }

            if (!Uri.TryCreate(endpointUrl, UriKind.Absolute, out Uri targetUri))
            {
                result.Success = false;
                result.Message = "Invalid cloud endpoint URL format.";
                return result;
            }

            try
            {
                var jsonBody = ExportPayloadJson(payload);
                var byteData = Encoding.UTF8.GetBytes(jsonBody);

                var request = (HttpWebRequest)WebRequest.Create(targetUri);
                request.Method = "POST";
                request.ContentType = "application/json";
                request.ContentLength = byteData.Length;
                request.Timeout = 10000; // 10 seconds timeout

                if (!string.IsNullOrWhiteSpace(tenantId))
                {
                    request.Headers.Add("X-Tenant-Id", tenantId);
                }

                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    request.Headers.Add("Authorization", $"Bearer {apiKey}");
                }

                using (var stream = await request.GetRequestStreamAsync())
                {
                    await stream.WriteAsync(byteData, 0, byteData.Length);
                }

                using (var response = (HttpWebResponse)await request.GetResponseAsync())
                {
                    result.HttpStatusCode = (int)response.StatusCode;
                    result.Success = (result.HttpStatusCode >= 200 && result.HttpStatusCode < 300);
                    using (var reader = new StreamReader(response.GetResponseStream()))
                    {
                        var responseText = await reader.ReadToEndAsync();
                        result.Message = $"Telemetry synchronized successfully (HTTP {result.HttpStatusCode}). Server ack: {responseText}";
                    }
                }
            }
            catch (WebException wex)
            {
                if (wex.Response is HttpWebResponse errorResponse)
                {
                    result.HttpStatusCode = (int)errorResponse.StatusCode;
                    result.Message = $"SaaS Cloud rejected payload with HTTP {result.HttpStatusCode}: {errorResponse.StatusDescription}";
                }
                else
                {
                    result.HttpStatusCode = 0;
                    result.Message = $"Network transport failure: {wex.Message}";
                }
                result.Success = false;
            }
            catch (Exception ex)
            {
                result.HttpStatusCode = 0;
                result.Success = false;
                result.Message = $"Telemetry synchronization error: {ex.Message}";
            }

            return result;
        }

        private static string GetMachineGuid()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography"))
                {
                    if (key != null)
                    {
                        var val = key.GetValue("MachineGuid");
                        if (val != null) return val.ToString();
                    }
                }
            }
            catch { }

            return Guid.NewGuid().ToString();
        }

        private static string Escape(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return text.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "").Replace("\n", "\\n");
        }
    }
}
