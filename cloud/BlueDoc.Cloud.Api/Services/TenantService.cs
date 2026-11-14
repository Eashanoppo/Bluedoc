using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BlueDoc.Cloud.Api.Data;
using BlueDoc.Cloud.Api.Models;

namespace BlueDoc.Cloud.Api.Services
{
    public class RegisterTenantRequest
    {
        public string OrganizationName { get; set; } = string.Empty;
        public string AdminEmail { get; set; } = string.Empty;
        public string Plan { get; set; } = "free";
    }

    public class RegisterTenantResponse
    {
        public Guid TenantId { get; set; }
        public string Slug { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class TenantService
    {
        private readonly BluedocCloudDbContext _db;

        public TenantService(BluedocCloudDbContext db)
        {
            _db = db;
        }

        public async Task<RegisterTenantResponse> RegisterTenantAsync(RegisterTenantRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.OrganizationName))
                throw new ArgumentException("Organization name cannot be empty.", nameof(req.OrganizationName));

            string baseSlug = Slugify(req.OrganizationName);
            string slug = baseSlug;
            int counter = 1;

            while (await _db.Tenants.AnyAsync(t => t.Slug == slug))
            {
                slug = $"{baseSlug}-{counter++}";
            }

            var tenant = new Tenant
            {
                TenantId = Guid.NewGuid(),
                Name = req.OrganizationName.Trim(),
                Slug = slug,
                Plan = string.IsNullOrWhiteSpace(req.Plan) ? "free" : req.Plan.Trim().ToLowerInvariant(),
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _db.Tenants.Add(tenant);

            // Generate raw API key
            string rawKey = GenerateSecureApiKey();
            string keyHash = HashApiKey(rawKey);

            var apiKey = new ApiKey
            {
                KeyId = Guid.NewGuid(),
                TenantId = tenant.TenantId,
                KeyHash = keyHash,
                Label = $"Initial key ({req.AdminEmail.Trim()})",
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _db.ApiKeys.Add(apiKey);
            await _db.SaveChangesAsync();

            return new RegisterTenantResponse
            {
                TenantId = tenant.TenantId,
                Slug = tenant.Slug,
                ApiKey = rawKey,
                Message = "Store this API key securely. It cannot be retrieved again."
            };
        }

        public async Task<string> RotateApiKeyAsync(Guid tenantId, string label = "Rotated Key")
        {
            var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.TenantId == tenantId && t.IsActive);
            if (tenant == null)
                throw new InvalidOperationException("Tenant not found or inactive.");

            // Deactivate existing keys
            var oldKeys = await _db.ApiKeys.Where(k => k.TenantId == tenantId && k.IsActive).ToListAsync();
            foreach (var k in oldKeys)
            {
                k.IsActive = false;
            }

            string rawKey = GenerateSecureApiKey();
            string keyHash = HashApiKey(rawKey);

            var newKey = new ApiKey
            {
                KeyId = Guid.NewGuid(),
                TenantId = tenantId,
                KeyHash = keyHash,
                Label = label,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _db.ApiKeys.Add(newKey);
            await _db.SaveChangesAsync();

            return rawKey;
        }

        public static string GenerateSecureApiKey()
        {
            var bytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return "bd_live_" + Convert.ToHexString(bytes).ToLowerInvariant();
        }

        public static string HashApiKey(string rawKey)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawKey));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        private static string Slugify(string name)
        {
            var slug = name.Trim().ToLowerInvariant();
            slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
            slug = Regex.Replace(slug, @"\s+", "-");
            slug = Regex.Replace(slug, @"-+", "-");
            return string.IsNullOrEmpty(slug) ? "tenant" : slug;
        }
    }
}

