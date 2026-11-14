using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BlueDoc.Application.Cloud
{
    /// <summary>
    /// Persists unsent telemetry payloads to a local SQLite queue so they survive
    /// network outages and are retransmitted on the next successful connection.
    /// </summary>
    public class SqliteOfflineTelemetryQueue : IOfflineTelemetryQueue
    {
        private readonly string _dbPath;

        public SqliteOfflineTelemetryQueue()
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "UNLEFT",
                "BlueDoc");

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            _dbPath = Path.Combine(dir, "offline_queue.db");
            EnsureTable();
        }

        private SQLiteConnection Open()
        {
            return new SQLiteConnection($"Data Source={_dbPath};Version=3;Journal Mode=WAL;");
        }

        private void EnsureTable()
        {
            using (var conn = Open())
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        CREATE TABLE IF NOT EXISTS OfflineQueue (
                            PayloadId   TEXT PRIMARY KEY,
                            PayloadJson TEXT NOT NULL,
                            QueuedAt    TEXT NOT NULL,
                            Sent        INTEGER NOT NULL DEFAULT 0
                        );";
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public Task EnqueueAsync(FleetTelemetryPayload payload)
        {
            return Task.Run(() =>
            {
                using (var conn = Open())
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
                            INSERT OR IGNORE INTO OfflineQueue (PayloadId, PayloadJson, QueuedAt, Sent)
                            VALUES (@id, @json, @at, 0);";
                        cmd.Parameters.AddWithValue("@id", payload.PayloadId);
                        cmd.Parameters.AddWithValue("@json", JsonConvert.SerializeObject(payload));
                        cmd.Parameters.AddWithValue("@at", DateTime.UtcNow.ToString("O"));
                        cmd.ExecuteNonQuery();
                    }
                }
            });
        }

        public Task<IReadOnlyList<FleetTelemetryPayload>> DequeueAllAsync()
        {
            return Task.Run<IReadOnlyList<FleetTelemetryPayload>>(() =>
            {
                var result = new List<FleetTelemetryPayload>();
                using (var conn = Open())
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT PayloadJson FROM OfflineQueue WHERE Sent = 0 ORDER BY QueuedAt LIMIT 50;";
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var json = reader.GetString(0);
                                var payload = JsonConvert.DeserializeObject<FleetTelemetryPayload>(json);
                                if (payload != null)
                                {
                                    result.Add(payload);
                                }
                            }
                        }
                    }
                }
                return result;
            });
        }

        public Task MarkSentAsync(string payloadId)
        {
            return Task.Run(() =>
            {
                using (var conn = Open())
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "UPDATE OfflineQueue SET Sent = 1 WHERE PayloadId = @id;";
                        cmd.Parameters.AddWithValue("@id", payloadId);
                        cmd.ExecuteNonQuery();
                    }
                }
            });
        }

        public Task<int> GetPendingCountAsync()
        {
            return Task.Run(() =>
            {
                using (var conn = Open())
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT COUNT(*) FROM OfflineQueue WHERE Sent = 0;";
                        var count = cmd.ExecuteScalar();
                        return Convert.ToInt32(count);
                    }
                }
            });
        }
    }
}

