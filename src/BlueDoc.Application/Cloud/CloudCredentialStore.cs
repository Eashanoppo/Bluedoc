using System;
using System.Threading.Tasks;
using BlueDoc.Core.Interfaces;
using BlueDoc.Data;
using BlueDoc.Data.Repositories;

namespace BlueDoc.Application.Cloud
{
    /// <summary>
    /// Stores cloud credentials and configuration in the local ApplicationSettings SQLite database.
    /// Provides persistent settings for the enterprise cloud agent.
    /// </summary>
    public class CloudCredentialStore
    {
        private readonly ISettingsRepository _settings;

        private const string KEY_ENDPOINT = "cloud.endpointUrl";
        private const string KEY_TENANT_ID = "cloud.tenantId";
        private const string KEY_API_KEY = "cloud.apiKey";
        private const string KEY_AUTO_SYNC = "cloud.autoSync";
        private const string KEY_INTERVAL = "cloud.syncIntervalMinutes";
        private const string KEY_ENABLED = "cloud.enabled";

        public CloudCredentialStore(ISettingsRepository settings = null)
        {
            _settings = settings ?? new SqliteSettingsRepository(new SqliteDatabaseManager());
        }

        public Task<string> GetEndpointUrlAsync() => _settings.GetSettingAsync(KEY_ENDPOINT, string.Empty);
        public Task<string> GetTenantIdAsync() => _settings.GetSettingAsync(KEY_TENANT_ID, string.Empty);
        public Task<string> GetApiKeyAsync() => _settings.GetSettingAsync(KEY_API_KEY, string.Empty);
        public Task<bool> GetAutoSyncAsync() => _settings.GetBoolSettingAsync(KEY_AUTO_SYNC, false);
        public Task<bool> GetEnabledAsync() => _settings.GetBoolSettingAsync(KEY_ENABLED, false);
        public Task<int> GetIntervalMinutesAsync() =>
            _settings.GetSettingAsync(KEY_INTERVAL, "30")
                     .ContinueWith(t => int.TryParse(t.Result, out int v) ? v : 30);

        public Task SetEndpointUrlAsync(string v) => _settings.SetSettingAsync(KEY_ENDPOINT, v ?? string.Empty);
        public Task SetTenantIdAsync(string v) => _settings.SetSettingAsync(KEY_TENANT_ID, v ?? string.Empty);
        public Task SetApiKeyAsync(string v) => _settings.SetSettingAsync(KEY_API_KEY, v ?? string.Empty);
        public Task SetAutoSyncAsync(bool v) => _settings.SetBoolSettingAsync(KEY_AUTO_SYNC, v);
        public Task SetEnabledAsync(bool v) => _settings.SetBoolSettingAsync(KEY_ENABLED, v);
        public Task SetIntervalMinutesAsync(int v) => _settings.SetSettingAsync(KEY_INTERVAL, v.ToString());
    }
}

