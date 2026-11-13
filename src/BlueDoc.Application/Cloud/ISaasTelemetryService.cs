using System.Threading;
using System.Threading.Tasks;
using BlueDoc.Core.Models;
using BlueDoc.Diagnostics.Benchmark;

namespace BlueDoc.Application.Cloud
{
    public interface ISaasTelemetryService
    {
        Task<FleetTelemetryPayload> BuildPayloadAsync(
            BluetoothAdapterInfo adapter,
            DiagnosticSession session,
            BenchmarkResult benchmark);

        string ExportPayloadJson(FleetTelemetryPayload payload);

        Task<SaasSyncResult> SyncTelemetryAsync(
            FleetTelemetryPayload payload,
            string endpointUrl,
            string tenantId,
            string apiKey,
            CancellationToken cancellationToken = default(CancellationToken));
    }
}

