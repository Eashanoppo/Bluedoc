using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using BlueDoc.Cloud.Api.Data;

namespace BlueDoc.Cloud.Api.Middleware
{
    public class ApiKeyAuthMiddleware
    {
        private readonly RequestDelegate _next;

        public ApiKeyAuthMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext ctx, BluedocCloudDbContext db)
        {
            // Public endpoints (tenant registration, health check, swagger docs)
            var path = ctx.Request.Path;
            if (path.StartsWithSegments("/v1/auth/register-tenant") ||
                path.StartsWithSegments("/health") ||
                path.StartsWithSegments("/swagger") ||
                path.Value == "/")
            {
                await _next(ctx);
                return;
            }

            string? raw = ctx.Request.Headers["Authorization"].FirstOrDefault()
                         ?? ctx.Request.Headers["X-Api-Key"].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(raw))
            {
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await ctx.Response.WriteAsJsonAsync(new { error = "API key required." });
                return;
            }

            // Strip "Bearer " prefix if present
            if (raw.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                raw = raw.Substring("Bearer ".Length).Trim();
            }
            else
            {
                raw = raw.Trim();
            }

            // Hash and lookup
            var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
            var hash = Convert.ToHexString(hashBytes).ToLowerInvariant();

            var apiKey = await db.ApiKeys
                .Include(k => k.Tenant)
                .FirstOrDefaultAsync(k => k.KeyHash == hash && k.IsActive && k.Tenant != null && k.Tenant.IsActive);

            if (apiKey == null)
            {
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await ctx.Response.WriteAsJsonAsync(new { error = "Invalid or revoked API key." });
                return;
            }

            // Stamp last used
            apiKey.LastUsedAt = DateTime.UtcNow;
            try
            {
                await db.SaveChangesAsync();
            }
            catch
            {
                // Non-blocking if concurrency issue
            }

            // Set identity in HttpContext so controllers can use it
            ctx.Items["TenantId"] = apiKey.TenantId.ToString();
            ctx.Items["Tenant"] = apiKey.Tenant;

            await _next(ctx);
        }
    }
}

