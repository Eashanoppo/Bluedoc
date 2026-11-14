using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BlueDoc.Cloud.Api.Data;
using BlueDoc.Cloud.Api.Models;

namespace BlueDoc.Cloud.Api.Services
{
    public class TelemetryIngestionService
    {
        private readonly BluedocCloudDbContext _db;

        public TelemetryIngestionService(BluedocCloudDbContext db)
        {
            _db = db;
        }

        public async Task<Guid> UpsertMachineAsync(Guid tenantId, FleetTelemetryPayload payload)
        {
            var health = DeriveHealthStatus(payload);
            var compliant = payload.FailedTests == 0;

            var existing = await _db.Machines
                .FirstOrDefaultAsync(m => m.TenantId == tenantId && m.MachineGuid == payload.MachineGuid);

            if (existing != null)
            {
                existing.Hostname = payload.Hostname;
                existing.OsName = payload.OperatingSystem;
                existing.OsBuild = payload.OsBuild;
                existing.Architecture = payload.Architecture;
                existing.AdapterName = payload.AdapterName;
                existing.Manufacturer = payload.Manufacturer;
                existing.HardwareId = payload.HardwareId;
                existing.DriverVersion = payload.DriverVersion;
                existing.HealthStatus = health;
                existing.BenchmarkScore = payload.BenchmarkScore.HasValue ? (decimal?)payload.BenchmarkScore.Value : null;
                existing.IsCompliant = compliant;
                existing.ComplianceIssue = compliant
                    ? "Fully Compliant"
                    : string.Join("; ", payload.FailedRuleSummaries.Take(3));
                existing.LastHeartbeat = DateTime.UtcNow;

                await _db.SaveChangesAsync();
                return existing.MachineId;
            }

            var machine = new Machine
            {
                MachineId = Guid.NewGuid(),
                TenantId = tenantId,
                MachineGuid = payload.MachineGuid,
                Hostname = payload.Hostname,
                OsName = payload.OperatingSystem,
                OsBuild = payload.OsBuild,
                Architecture = payload.Architecture,
                AdapterName = payload.AdapterName,
                Manufacturer = payload.Manufacturer,
                HardwareId = payload.HardwareId,
                DriverVersion = payload.DriverVersion,
                HealthStatus = health,
                BenchmarkScore = payload.BenchmarkScore.HasValue ? (decimal?)payload.BenchmarkScore.Value : null,
                IsCompliant = compliant,
                ComplianceIssue = compliant
                    ? "Fully Compliant"
                    : string.Join("; ", payload.FailedRuleSummaries.Take(3)),
                LastHeartbeat = DateTime.UtcNow,
                EnrolledAt = DateTime.UtcNow
            };

            _db.Machines.Add(machine);
            await _db.SaveChangesAsync();
            return machine.MachineId;
        }

        public async Task<Guid> RecordTelemetryAsync(Guid tenantId, Guid machineId, FleetTelemetryPayload payload)
        {
            var record = new TelemetryRecord
            {
                RecordId = Guid.NewGuid(),
                MachineId = machineId,
                TenantId = tenantId,
                ReceivedAt = DateTime.UtcNow,
                OverallStatus = payload.OverallHealthStatus,
                TotalTests = payload.TotalTests,
                PassedTests = payload.PassedTests,
                WarningTests = payload.WarningTests,
                FailedTests = payload.FailedTests,
                BenchmarkScore = payload.BenchmarkScore.HasValue ? (decimal?)payload.BenchmarkScore.Value : null,
                BenchmarkRating = payload.BenchmarkRating,
                FailedSummaries = JsonSerializer.Serialize(payload.FailedRuleSummaries),
                RawPayload = JsonSerializer.Serialize(payload)
            };

            _db.TelemetryRecords.Add(record);
            await _db.SaveChangesAsync();
            return record.RecordId;
        }

        private static string DeriveHealthStatus(FleetTelemetryPayload p)
        {
            if (p.FailedTests > 0) return "Critical";
            if (p.WarningTests > 0) return "Warning";
            return "Healthy";
        }
    }
}

