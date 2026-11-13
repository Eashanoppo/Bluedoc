using System;
using System.Data.SQLite;
using System.Threading.Tasks;
using BlueDoc.Core.Interfaces;

namespace BlueDoc.Data.Repositories
{
    public class SqliteSettingsRepository : ISettingsRepository
    {
        private readonly SqliteDatabaseManager _db;

        public SqliteSettingsRepository(SqliteDatabaseManager db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public Task<string> GetSettingAsync(string key, string defaultValue = "")
        {
            return Task.Run(() =>
            {
                using (var conn = _db.CreateConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT Value FROM ApplicationSettings WHERE Key = @k;";
                    cmd.Parameters.AddWithValue("@k", key);

                    var res = cmd.ExecuteScalar();
                    if (res != null && res != DBNull.Value)
                    {
                        return res.ToString();
                    }
                    return defaultValue;
                }
            });
        }

        public Task SetSettingAsync(string key, string value)
        {
            return Task.Run(() =>
            {
                using (var conn = _db.CreateConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        INSERT INTO ApplicationSettings (Key, Value, UpdatedUtc)
                        VALUES (@k, @v, @u)
                        ON CONFLICT(Key) DO UPDATE SET Value = excluded.Value, UpdatedUtc = excluded.UpdatedUtc;";

                    cmd.Parameters.AddWithValue("@k", key);
                    cmd.Parameters.AddWithValue("@v", value ?? string.Empty);
                    cmd.Parameters.AddWithValue("@u", DateTime.UtcNow.ToString("o"));

                    cmd.ExecuteNonQuery();
                }
            });
        }

        public async Task<bool> GetBoolSettingAsync(string key, bool defaultValue = false)
        {
            string val = await GetSettingAsync(key, defaultValue ? "true" : "false");
            if (bool.TryParse(val, out bool result))
            {
                return result;
            }
            return defaultValue;
        }

        public async Task SetBoolSettingAsync(string key, bool value)
        {
            await SetSettingAsync(key, value ? "true" : "false");
        }
    }
}
