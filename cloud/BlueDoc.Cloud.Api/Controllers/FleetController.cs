using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using BlueDoc.Cloud.Api.Services;

namespace BlueDoc.Cloud.Api.Controllers
{
    [ApiController]
    [Route("v1/fleet")]
    public class FleetController : ControllerBase
    {
        private readonly FleetAggregationService _fleetService;

        public FleetController(FleetAggregationService fleetService)
        {
            _fleetService = fleetService;
        }

        [HttpGet("metrics")]
        public async Task<IActionResult> GetMetrics([FromQuery] string status = "all", [FromQuery] string search = "")
        {
            if (!HttpContext.Items.TryGetValue("TenantId", out var tidObj) || tidObj == null || !Guid.TryParse(tidObj.ToString(), out var tenantId))
            {
                return Unauthorized(new { error = "Unauthorized tenant context." });
            }

            var metrics = await _fleetService.GetFleetMetricsAsync(tenantId, status, search);
            return Ok(metrics);
        }

        [HttpGet("machines")]
        public async Task<IActionResult> GetMachines([FromQuery] string status = "all", [FromQuery] string search = "")
        {
            if (!HttpContext.Items.TryGetValue("TenantId", out var tidObj) || tidObj == null || !Guid.TryParse(tidObj.ToString(), out var tenantId))
            {
                return Unauthorized(new { error = "Unauthorized tenant context." });
            }

            var metrics = await _fleetService.GetFleetMetricsAsync(tenantId, status, search);
            return Ok(new
            {
                totalCount = metrics.Machines.Count,
                machines = metrics.Machines
            });
        }
    }
}

