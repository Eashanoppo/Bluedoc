using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Threading.Tasks;
using BlueDoc.Core.Interfaces;
using BlueDoc.Core.Models;

namespace BlueDoc.Data.Repositories
{
    public class SqliteRepairHistoryRepository : IRepairHistoryRepository
    {
        private readonly SqliteDatabaseManager _db;

        public SqliteRepairHistoryRepository(SqliteDatabaseManager db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public Task RecordRepairAsync(RepairHistoryRecord record)
        {
            return Task.Run(() =>
            {
                using (var conn = _db.CreateConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        INSERT INTO RepairHistory 
                        (RepairKey, ActionTitle, Description, TargetComponent, TimestampUtc, RequiredAdmin, Status, VerificationResult, ErrorMessage)
                        VALUES (@rkey, @act, @desc, @comp, @time, @admin, @st, @vr, @err);";

                    cmd.Parameters.AddWithValue("@rkey", record.RepairKey ?? string.Empty);
                    cmd.Parameters.AddWithValue("@act", record.ActionTitle ?? string.Empty);
                    cmd.Parameters.AddWithValue("@desc", record.Description ?? string.Empty);
                    cmd.Parameters.AddWithValue("@comp", record.TargetComponent ?? string.Empty);
                    cmd.Parameters.AddWithValue("@time", record.TimestampUtc.ToString("o"));
                    cmd.Parameters.AddWithValue("@admin", record.RequiredAdmin ? 1 : 0);
                    cmd.Parameters.AddWithValue("@st", record.Status ?? "Pending");
                    cmd.Parameters.AddWithValue("@vr", record.VerificationResult ?? string.Empty);
                    cmd.Parameters.AddWithValue("@err", record.ErrorMessage ?? string.Empty);

                    cmd.ExecuteNonQuery();
                }
            });
        }

        public Task<List<RepairHistoryRecord>> GetRepairHistoryAsync(int limit = 50)
        {
            return Task.Run(() =>
            {
                var list = new List<RepairHistoryRecord>();

                using (var conn = _db.CreateConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM RepairHistory ORDER BY TimestampUtc DESC LIMIT @lim;";
                    cmd.Parameters.AddWithValue("@lim", limit);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var rec = new RepairHistoryRecord
                            {
                                Id = Convert.ToInt64(reader["Id"]),
                                RepairKey = reader["RepairKey"]?.ToString(),
                                ActionTitle = reader["ActionTitle"]?.ToString(),
                                Description = reader["Description"]?.ToString(),
                                TargetComponent = reader["TargetComponent"]?.ToString(),
                                TimestampUtc = DateTime.Parse(reader["TimestampUtc"].ToString(), null, System.Globalization.DateTimeStyles.RoundtripKind),
                                RequiredAdmin = Convert.ToInt32(reader["RequiredAdmin"]) == 1,
                                Status = reader["Status"]?.ToString(),
                                VerificationResult = reader["VerificationResult"]?.ToString(),
                                ErrorMessage = reader["ErrorMessage"]?.ToString()
                            };

                            list.Add(rec);
                        }
                    }
                }

                return list;
            });
        }

        public Task ClearRepairHistoryAsync()
        {
            return Task.Run(() =>
            {
                using (var conn = _db.CreateConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM RepairHistory;";
                    cmd.ExecuteNonQuery();
                }
            });
        }
    }
}
