using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BlueDoc.Cloud.Api.Data;

namespace BlueDoc.Cloud.Api.Controllers
{
    [ApiController]
    [Route("v1/alerts")]
    public class AlertController : ControllerBase
    {
        private readonly BluedocCloudDbContext _db;

        public AlertController(BluedocCloudDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetAlerts([FromQuery] string? severity = null, [FromQuery] Guid? machineId = null)
        {
            if (!HttpContext.Items.TryGetValue("TenantId", out var tidObj) || tidObj == null || !Guid.TryParse(tidObj.ToString(), out var tenantId))
            {
                return Unauthorized(new { error = "Unauthorized tenant context." });
            }

            var query = _db.Alerts
                .Include(a => a.Machine)
                .Where(a => a.TenantId == tenantId);

            if (!string.IsNullOrWhiteSpace(severity))
            {
                query = query.Where(a => a.Severity.ToLower() == severity.ToLower());
            }

            if (machineId.HasValue)
            {
                query = query.Where(a => a.MachineId == machineId.Value);
            }

            var all = await query.OrderByDescending(a => a.CreatedAt).ToListAsync();
            var unackCount = all.Count(a => !a.Acknowledged);

            var alertDtos = all.Select(a => new
            {
                alertId = a.AlertId.ToString(),
                machineId = a.MachineId?.ToString() ?? string.Empty,
                hostname = a.Machine?.Hostname ?? "Unknown Host",
                severity = a.Severity,
                ruleId = a.RuleId ?? string.Empty,
                title = a.Title,
                detail = a.Detail ?? string.Empty,
                createdAt = a.CreatedAt,
                acknowledged = a.Acknowledged
            }).ToList();

            return Ok(new
            {
                totalCount = alertDtos.Count,
                unacknowledgedCount = unackCount,
                alerts = alertDtos
            });
        }

        [HttpPost("{alertId}/acknowledge")]
        public async Task<IActionResult> AcknowledgeAlert(Guid alertId)
        {
            if (!HttpContext.Items.TryGetValue("TenantId", out var tidObj) || tidObj == null || !Guid.TryParse(tidObj.ToString(), out var tenantId))
            {
                return Unauthorized(new { error = "Unauthorized tenant context." });
            }

            var alert = await _db.Alerts.FirstOrDefaultAsync(a => a.AlertId == alertId && a.TenantId == tenantId);
            if (alert == null)
            {
                return NotFound(new { error = "Alert not found." });
            }

            alert.Acknowledged = true;
            alert.AckAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(new { success = true, alertId = alertId.ToString(), acknowledgedAt = alert.AckAt });
        }
    }
}

