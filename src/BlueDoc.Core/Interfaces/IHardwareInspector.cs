using System.Collections.Generic;
using BlueDoc.Core.Models;

namespace BlueDoc.Core.Interfaces
{
    public interface IHardwareInspector
    {
        IReadOnlyList<BluetoothAdapterInfo> EnumerateAdapters();
        BluetoothAdapterInfo GetPrimaryAdapter();
    }
}

