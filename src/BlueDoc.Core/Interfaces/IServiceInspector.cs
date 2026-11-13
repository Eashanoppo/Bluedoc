using System.Collections.Generic;
using BlueDoc.Core.Models;

namespace BlueDoc.Core.Interfaces
{
    public interface IServiceInspector
    {
        ServiceInfo GetServiceInfo(string serviceName);
        IReadOnlyList<ServiceInfo> GetAllBluetoothServices();
    }
}

