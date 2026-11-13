using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlueDoc.Core.Models;

namespace BlueDoc.Core.Interfaces
{
    public interface IRepairHistoryRepository
    {
        Task RecordRepairAsync(RepairHistoryRecord record);
        Task<List<RepairHistoryRecord>> GetRepairHistoryAsync(int limit = 50);
        Task ClearRepairHistoryAsync();
    }
}
