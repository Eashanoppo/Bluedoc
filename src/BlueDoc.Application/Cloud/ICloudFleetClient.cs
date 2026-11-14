using System.Threading;
using System.Threading.Tasks;

namespace BlueDoc.Application.Cloud
{
    /// <summary>
    /// Production cloud fleet client. Communicates with BlueDoc.Cloud.Api.
    /// No mock data, no in-process server, no hardcoded secrets.
    /// </summary>
    public interface ICloudFleetClient
    {
        /// <summary>Push telemetry payload to cloud. Queues offline if unreachable.</summary>
        Task<SaasSyncResult> PushTelemetryAsync(FleetTelemetryPayload payload, CancellationToken ct = default);

        /// <summary>Pull fleet machine list from cloud for the authenticated tenant.</summary>
        Task<FleetSummaryMetrics> GetFleetMetricsAsync(string statusFilter = "all", string search = "", CancellationToken ct = default);

        /// <summary>Pull unacknowledged alerts for this tenant.</summary>
        Task<CloudAlertList> GetAlertsAsync(CancellationToken ct = default);

        /// <summary>Acknowledge a specific alert by its server-side ID.</summary>
        Task<bool> AcknowledgeAlertAsync(string alertId, CancellationToken ct = default);

        /// <summary>Register a new tenant and obtain the initial API key.</summary>
        Task<TenantRegistrationResult> RegisterTenantAsync(string orgName, string adminEmail, string plan, CancellationToken ct = default);

        bool IsConfigured { get; }
        string EndpointUrl { get; set; }
        string ApiKey { get; set; }
        string TenantId { get; set; }
    }
}

