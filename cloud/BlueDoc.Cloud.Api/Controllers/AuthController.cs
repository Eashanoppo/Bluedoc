using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using BlueDoc.Cloud.Api.Services;

namespace BlueDoc.Cloud.Api.Controllers
{
    [ApiController]
    [Route("v1/auth")]
    public class AuthController : ControllerBase
    {
        private readonly TenantService _tenantService;

        public AuthController(TenantService tenantService)
        {
            _tenantService = tenantService;
        }

        [HttpPost("register-tenant")]
        public async Task<IActionResult> RegisterTenant([FromBody] RegisterTenantRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.OrganizationName) || string.IsNullOrWhiteSpace(request.AdminEmail))
            {
                return BadRequest(new { error = "OrganizationName and AdminEmail are required." });
            }

            try
            {
                var result = await _tenantService.RegisterTenantAsync(request);
                return StatusCode(StatusCodes.Status201Created, result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
            }
        }

        [HttpPost("rotate-key")]
        public async Task<IActionResult> RotateKey()
        {
            if (!HttpContext.Items.TryGetValue("TenantId", out var tidObj) || tidObj == null || !Guid.TryParse(tidObj.ToString(), out var tenantId))
            {
                return Unauthorized(new { error = "Valid tenant authentication required." });
            }

            try
            {
                var newKey = await _tenantService.RotateApiKeyAsync(tenantId);
                return Ok(new
                {
                    newApiKey = newKey,
                    message = "API key rotated successfully. Store this new key immediately."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}

