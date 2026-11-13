using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlueDoc.Core.Models;

namespace BlueDoc.Core.Interfaces
{
    public interface IConnectionHistoryRepository
    {
        Task RecordEventAsync(ConnectionHistoryRecord record);
        Task<List<ConnectionHistoryRecord>> GetHistoryAsync(int limit = 100, string search = null);
        Task ClearHistoryAsync();
    }
}
