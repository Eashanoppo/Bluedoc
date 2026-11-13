using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlueDoc.Core.Interfaces;
using BlueDoc.Core.Models;
using BlueDoc.Infrastructure.Repairs;

namespace BlueDoc.Application.Services
{
    public class RepairService
    {
        private readonly SafeRepairEngine _engine;
        private readonly IRepairHistoryRepository _repo;

        public RepairService(IRepairHistoryRepository repo = null, IDeviceCacheRepository deviceCacheRepo = null)
        {
            _repo = repo;
            _engine = new SafeRepairEngine(repo, deviceCacheRepo);
        }

        public bool IsRunningAsAdmin => _engine.IsRunningAsAdministrator();

        public List<RepairActionDefinition> GetAllRepairDefinitions()
        {
            return _engine.GetAllRepairDefinitions();
        }

        public RepairActionDefinition GetRepairInfo(string key)
        {
            return _engine.GetRepairDefinition(key);
        }

        public Task<RepairHistoryRecord> ExecuteRepairAsync(string key)
        {
            return _engine.ExecuteRepairAsync(key);
        }

        public Task<List<RepairHistoryRecord>> GetRepairHistoryAsync(int limit = 50)
        {
            if (_repo == null) return Task.FromResult(new List<RepairHistoryRecord>());
            return _repo.GetRepairHistoryAsync(limit);
        }

        public Task ClearRepairHistoryAsync()
        {
            if (_repo == null) return Task.CompletedTask;
            return _repo.ClearRepairHistoryAsync();
        }
    }
}
