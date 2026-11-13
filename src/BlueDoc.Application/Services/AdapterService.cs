using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BlueDoc.Core.Interfaces;
using BlueDoc.Core.Models;
using BlueDoc.Infrastructure.Hardware;
using BlueDoc.Infrastructure.Services;

namespace BlueDoc.Application.Services
{
    public class AdapterService
    {
        private readonly IHardwareInspector _hardwareInspector;
        private readonly IServiceInspector _serviceInspector;

        public AdapterService(IHardwareInspector hardwareInspector = null, IServiceInspector serviceInspector = null)
        {
            _hardwareInspector = hardwareInspector ?? new SetupApiHardwareInspector();
            _serviceInspector = serviceInspector ?? new WindowsServiceInspector();
        }

        public Task<IReadOnlyList<BluetoothAdapterInfo>> GetAdaptersAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.Run(() => _hardwareInspector.EnumerateAdapters(), cancellationToken);
        }

        public Task<BluetoothAdapterInfo> GetPrimaryAdapterAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.Run(() => _hardwareInspector.GetPrimaryAdapter(), cancellationToken);
        }

        public Task<IReadOnlyList<ServiceInfo>> GetServicesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.Run(() => _serviceInspector.GetAllBluetoothServices(), cancellationToken);
        }
    }
}
