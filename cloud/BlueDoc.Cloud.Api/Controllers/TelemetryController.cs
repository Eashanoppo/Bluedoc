using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using BlueDoc.Cloud.Api.Models;
using BlueDoc.Cloud.Api.Services;

namespace BlueDoc.Cloud.Api.Controllers
{
    [ApiController]
    [Route("v1/telemetry")]
    public class TelemetryController : ControllerBase
    {
        private readonly TelemetryIngestionService _ingestion;
        private readonly AlertEvaluationService _alerter;

        public TelemetryController(TelemetryIngestionService ingestion, AlertEvaluationService alerter)
        {
            _ingestion = ingestion;
            _alerter = alerter;
        }

        [HttpPost]
        public async Task<IActionResult> Ingest([FromBody] FleetTelemetryPayload payload)
        {
            if (payload == null)
            {
                return BadRequest(new { error = "Empty telemetry payload." });
            }

            if (!HttpContext.Items.TryGetValue("TenantId", out var tidObj) || tidObj == null || !Guid.TryParse(tidObj.ToString(), out var tenantId))
            {
                return Unauthorized(new { error = "Unauthorized tenant context." });
            }

            // 1. Upsert machine record (create if new, update if exists)
            var machineId = await _ingestion.UpsertMachineAsync(tenantId, payload);

            // 2. Write immutable telemetry record
            var recordId = await _ingestion.RecordTelemetryAsync(tenantId, machineId, payload);

            // 3. Evaluate alert rules
            int alertsRaised = await _alerter.EvaluateAsync(tenantId, machineId, payload);

            return Ok(new
            {
                accepted = true,
                machineId = machineId.ToString(),
                recordId = recordId.ToString(),
                alertsRaised
            });
        }
    }
}

