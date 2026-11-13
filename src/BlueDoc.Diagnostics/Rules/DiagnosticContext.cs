using System.Collections.Generic;
using BlueDoc.Core.Models;

namespace BlueDoc.Diagnostics.Rules
{
    public class DiagnosticContext
    {
        public OsVersionInfo OsInfo { get; set; }
        public IReadOnlyList<BluetoothAdapterInfo> Adapters { get; set; }
        public BluetoothAdapterInfo PrimaryAdapter { get; set; }
        public IReadOnlyList<ServiceInfo> Services { get; set; }
        public ServiceInfo CoreService { get; set; }
        public ServiceInfo AudioGatewayService { get; set; }
        public ServiceInfo AvctpService { get; set; }
        public bool? IsRadioEnabled { get; set; }
        public IReadOnlyList<BluetoothDeviceInfo> PairedDevices { get; set; }
        public IReadOnlyList<BluetoothEventRecord> RecentEvents { get; set; }
        public List<DiagnosticResult> PreviousResults { get; } = new List<DiagnosticResult>();
    }
}

