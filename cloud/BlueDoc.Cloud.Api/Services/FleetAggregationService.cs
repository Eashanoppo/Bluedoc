using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BlueDoc.Cloud.Api.Data;
using BlueDoc.Cloud.Api.Models;

namespace BlueDoc.Cloud.Api.Services
{
    public class FleetSummaryDto
    {
        public int TotalEndpoints { get; set; }
        public int HealthyCount { get; set; }
        public int WarningCount { get; set; }
        public int CriticalCount { get; set; }
        public double FleetAverageScore { get; set; }
        public double CompliancePercentage { get; set; }
        public List<FleetMachineDto> Machines { get; set; } = new();
    }

    public class FleetMachineDto
    {
        public string MachineId { get; set; } = string.Empty;
        public string Hostname { get; set; } = string.Empty;
        public string OrganizationId { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string OperatingSystem { get; set; } = string.Empty;
        public string PrimaryAdapter { get; set; } = string.Empty;
        public string ChipsetManufacturer { get; set; } = string.Empty;
        public string DriverVersion { get; set; } = string.Empty;
        public string HealthStatus { get; set; } = "Unknown";
        public double BenchmarkScore { get; set; }
        public DateTime LastHeartbeatUtc { get; set; }
        public List<string> FailureAlerts { get; set; } = new();
        public bool IsCompliant { get; set; }
        public string ComplianceIssue { get; set; } = string.Empty;
        public string LocalHeartbeatDisplay => LastHeartbeatUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
    }

    public class FleetAggregationService
    {
        private readonly BluedocCloudDbContext _db;

        public FleetAggregationService(BluedocCloudDbContext db)
        {
            _db = db;
        }

        public async Task<FleetSummaryDto> GetFleetMetricsAsync(Guid tenantId, string statusFilter = "all", string search = "")
        {
            var query = _db.Machines.Where(m => m.TenantId == tenantId);

            var allMachines = await query.ToListAsync();
            var total = allMachines.Count;
            var healthy = allMachines.Count(m => m.HealthStatus == "Healthy");
            var warning = allMachines.Count(m => m.HealthStatus == "Warning");
            var critical = allMachines.Count(m => m.HealthStatus == "Critical");
            var avgScore = total > 0 ? (double)Math.Round(allMachines.Average(m => (double)(m.BenchmarkScore ?? 0)), 1) : 0.0;
            var compliantCount = allMachines.Count(m => m.IsCompliant);
            var compliancePct = total > 0 ? Math.Round((double)compliantCount / total * 100.0, 1) : 100.0;

            // Apply filtering for the returned machine list
            var filtered = allMachines.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLowerInvariant();
                filtered = filtered.Where(m =>
                    (m.Hostname?.ToLowerInvariant().Contains(s) ?? false) ||
                    (m.AdapterName?.ToLowerInvariant().Contains(s) ?? false) ||
                    (m.Manufacturer?.ToLowerInvariant().Contains(s) ?? false) ||
                    (m.Department?.ToLowerInvariant().Contains(s) ?? false));
            }

            if (!string.IsNullOrWhiteSpace(statusFilter) && !statusFilter.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                filtered = filtered.Where(m => string.Equals(m.HealthStatus, statusFilter, StringComparison.OrdinalIgnoreCase));
            }

            var machineDtos = filtered
                .OrderByDescending(m => m.LastHeartbeat)
                .Select(m => new FleetMachineDto
                {
                    MachineId = m.MachineId.ToString(),
                    Hostname = m.Hostname,
                    OrganizationId = tenantId.ToString(),
                    Department = m.Department ?? "General",
                    OperatingSystem = m.OsName ?? "Windows",
                    PrimaryAdapter = m.AdapterName ?? "Unknown Adapter",
                    ChipsetManufacturer = m.Manufacturer ?? "Unknown",
                    DriverVersion = m.DriverVersion ?? "N/A",
                    HealthStatus = m.HealthStatus,
                    BenchmarkScore = (double)(m.BenchmarkScore ?? 0),
                    LastHeartbeatUtc = m.LastHeartbeat,
                    IsCompliant = m.IsCompliant,
                    ComplianceIssue = m.ComplianceIssue ?? (m.IsCompliant ? "Fully Compliant" : "Non-Compliant"),
                    FailureAlerts = string.IsNullOrEmpty(m.ComplianceIssue) ? new List<string>() : new List<string> { m.ComplianceIssue }
                })
                .ToList();

            return new FleetSummaryDto
            {
                TotalEndpoints = total,
                HealthyCount = healthy,
                WarningCount = warning,
                CriticalCount = critical,
                FleetAverageScore = avgScore,
                CompliancePercentage = compliancePct,
                Machines = machineDtos
            };
        }
    }
}

