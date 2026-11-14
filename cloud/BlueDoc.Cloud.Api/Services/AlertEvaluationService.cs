using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BlueDoc.Cloud.Api.Data;
using BlueDoc.Cloud.Api.Models;

namespace BlueDoc.Cloud.Api.Services
{
    public class AlertEvaluationService
    {
        private readonly BluedocCloudDbContext _db;

        public AlertEvaluationService(BluedocCloudDbContext db)
        {
            _db = db;
        }

        public async Task<int> EvaluateAsync(Guid tenantId, Guid machineId, FleetTelemetryPayload p)
        {
            var alerts = new List<Alert>();

            // Rule 1: Controller hardware failure (Code 10 / 43)
            if (p.FailedRuleSummaries.Any(s => s.Contains("RULE_HW_STATUS") || s.Contains("RULE_HARDWARE") || s.Contains("Code 10") || s.Contains("Code 43")))
            {
                alerts.Add(CreateAlert("Critical", "RULE_HW_STATUS",
                    $"Hardware failure on {p.Hostname}: Bluetooth controller reports malfunction",
                    string.Join("\n", p.FailedRuleSummaries.Where(s => s.Contains("HW") || s.Contains("Code")))));
            }

            // Rule 2: Bluetooth service stopped
            if (p.FailedRuleSummaries.Any(s => s.Contains("RULE_SERVICE_STATUS") || s.Contains("bthserv")))
            {
                alerts.Add(CreateAlert("Critical", "RULE_SERVICE_STATUS",
                    $"Bluetooth support service stopped on {p.Hostname}",
                    "bthserv is in Stopped state. Safe auto-repair recommended."));
            }

            // Rule 3: Driver health warning / outdated driver
            if (p.FailedRuleSummaries.Any(s => s.Contains("RULE_DRIVER")))
            {
                alerts.Add(CreateAlert("Warning", "RULE_DRIVER_HEALTH",
                    $"Outdated or problematic driver on {p.Hostname}: {p.DriverVersion}",
                    string.Join("\n", p.FailedRuleSummaries.Where(s => s.Contains("DRIVER")))));
            }

            // Rule 4: Benchmark below threshold (< 50.0)
            if (p.BenchmarkScore.HasValue && p.BenchmarkScore < 50.0)
            {
                alerts.Add(CreateAlert("Warning", "BENCHMARK_LOW",
                    $"Low Bluetooth benchmark score on {p.Hostname}: {p.BenchmarkScore:F1}/100",
                    "Score below 50 indicates significant latency or hardware throughput degradation."));
            }

            int addedCount = 0;
            // Deduplication: skip if same rule alert already unacknowledged for same machine within last 24h
            foreach (var alert in alerts)
            {
                bool exists = await _db.Alerts.AnyAsync(a =>
                    a.MachineId == machineId &&
                    a.RuleId == alert.RuleId &&
                    !a.Acknowledged &&
                    a.CreatedAt > DateTime.UtcNow.AddHours(-24));

                if (!exists)
                {
                    alert.TenantId = tenantId;
                    alert.MachineId = machineId;
                    _db.Alerts.Add(alert);
                    addedCount++;
                }
            }

            if (addedCount > 0)
            {
                await _db.SaveChangesAsync();
            }

            return addedCount;

            Alert CreateAlert(string severity, string ruleId, string title, string detail) => new()
            {
                AlertId = Guid.NewGuid(),
                Severity = severity,
                RuleId = ruleId,
                Title = title,
                Detail = detail,
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}

