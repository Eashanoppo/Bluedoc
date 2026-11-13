using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Threading.Tasks;
using BlueDoc.Core.Interfaces;
using BlueDoc.Core.Models;

namespace BlueDoc.Data.Repositories
{
    public class SqliteConnectionHistoryRepository : IConnectionHistoryRepository
    {
        private readonly SqliteDatabaseManager _db;

        public SqliteConnectionHistoryRepository(SqliteDatabaseManager db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public Task RecordEventAsync(ConnectionHistoryRecord record)
        {
            return Task.Run(() =>
            {
                using (var conn = _db.CreateConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        INSERT INTO ConnectionHistory 
                        (DeviceAddress, DeviceName, DeviceType, EventType, TimestampUtc, Details, IsSuccess, DiagnosticSessionId)
                        VALUES (@addr, @name, @type, @evt, @time, @det, @succ, @sid);";

                    cmd.Parameters.AddWithValue("@addr", record.DeviceAddress ?? string.Empty);
                    cmd.Parameters.AddWithValue("@name", record.DeviceName ?? string.Empty);
                    cmd.Parameters.AddWithValue("@type", record.DeviceType ?? "Unknown");
                    cmd.Parameters.AddWithValue("@evt", record.EventType ?? "Discovered");
                    cmd.Parameters.AddWithValue("@time", record.TimestampUtc.ToString("o"));
                    cmd.Parameters.AddWithValue("@det", record.Details ?? string.Empty);
                    cmd.Parameters.AddWithValue("@succ", record.IsSuccess ? 1 : 0);
                    cmd.Parameters.AddWithValue("@sid", record.DiagnosticSessionId.HasValue ? record.DiagnosticSessionId.Value.ToString() : (object)DBNull.Value);

                    cmd.ExecuteNonQuery();
                }
            });
        }

        public Task<List<ConnectionHistoryRecord>> GetHistoryAsync(int limit = 100, string search = null)
        {
            return Task.Run(() =>
            {
                var list = new List<ConnectionHistoryRecord>();

                using (var conn = _db.CreateConnection())
                using (var cmd = conn.CreateCommand())
                {
                    if (string.IsNullOrWhiteSpace(search))
                    {
                        cmd.CommandText = "SELECT * FROM ConnectionHistory ORDER BY TimestampUtc DESC LIMIT @lim;";
                    }
                    else
                    {
                        cmd.CommandText = @"
                            SELECT * FROM ConnectionHistory 
                            WHERE DeviceName LIKE @s OR DeviceAddress LIKE @s OR EventType LIKE @s
                            ORDER BY TimestampUtc DESC LIMIT @lim;";
                        cmd.Parameters.AddWithValue("@s", $"%{search}%");
                    }
                    cmd.Parameters.AddWithValue("@lim", limit);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var rec = new ConnectionHistoryRecord
                            {
                                Id = Convert.ToInt64(reader["Id"]),
                                DeviceAddress = reader["DeviceAddress"]?.ToString(),
                                DeviceName = reader["DeviceName"]?.ToString(),
                                DeviceType = reader["DeviceType"]?.ToString(),
                                EventType = reader["EventType"]?.ToString(),
                                TimestampUtc = DateTime.Parse(reader["TimestampUtc"].ToString(), null, System.Globalization.DateTimeStyles.RoundtripKind),
                                Details = reader["Details"]?.ToString(),
                                IsSuccess = Convert.ToInt32(reader["IsSuccess"]) == 1
                            };

                            if (reader["DiagnosticSessionId"] != DBNull.Value && Guid.TryParse(reader["DiagnosticSessionId"].ToString(), out Guid sid))
                            {
                                rec.DiagnosticSessionId = sid;
                            }

                            list.Add(rec);
                        }
                    }
                }

                return list;
            });
        }

        public Task ClearHistoryAsync()
        {
            return Task.Run(() =>
            {
                using (var conn = _db.CreateConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM ConnectionHistory;";
                    cmd.ExecuteNonQuery();
                }
            });
        }
    }
}
