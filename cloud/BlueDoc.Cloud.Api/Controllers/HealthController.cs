using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BlueDoc.Cloud.Api.Data;

namespace BlueDoc.Cloud.Api.Controllers
{
    [ApiController]
    [Route("health")]
    public class HealthController : ControllerBase
    {
        private readonly BluedocCloudDbContext _db;

        public HealthController(BluedocCloudDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetHealth()
        {
            bool dbConnected = false;
            int machineCount = 0;

            try
            {
                dbConnected = await _db.Database.CanConnectAsync();
                if (dbConnected)
                {
                    machineCount = await _db.Machines.CountAsync();
                }
            }
            catch
            {
                dbConnected = false;
            }

            return Ok(new
            {
                status = dbConnected ? "Healthy" : "Degraded",
                database = dbConnected ? "Connected" : "Disconnected",
                totalMachinesTracked = machineCount,
                timestampUtc = DateTime.UtcNow
            });
        }
    }
}

