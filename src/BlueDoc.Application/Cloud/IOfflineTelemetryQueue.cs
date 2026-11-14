using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlueDoc.Application.Cloud
{
    public interface IOfflineTelemetryQueue
    {
        Task EnqueueAsync(FleetTelemetryPayload payload);
        Task<IReadOnlyList<FleetTelemetryPayload>> DequeueAllAsync();
        Task MarkSentAsync(string payloadId);
        Task<int> GetPendingCountAsync();
    }
}

