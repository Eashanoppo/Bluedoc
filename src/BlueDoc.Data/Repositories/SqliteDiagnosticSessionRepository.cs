using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Threading.Tasks;
using BlueDoc.Core.Enums;
using BlueDoc.Core.Interfaces;
using BlueDoc.Core.Models;

namespace BlueDoc.Data.Repositories
{
    public class SqliteDiagnosticSessionRepository : IDiagnosticSessionRepository
    {
        private readonly SqliteDatabaseManager _db;

        public SqliteDiagnosticSessionRepository(SqliteDatabaseManager db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public Task SaveSessionAsync(DiagnosticSession session)
        {
            return Task.Run(() =>
            {
                using (var conn = _db.CreateConnection())
                using (var trans = conn.BeginTransaction())
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
                            INSERT OR REPLACE INTO DiagnosticSessions 
                            (SessionId, StartedAtUtc, CompletedAtUtc, OverallStatus, OsName, OsVersion, OsBuild, AdapterName, AdapterHwId, ServiceName, ServiceStatus)
                            VALUES (@id, @start, @comp, @status, @osname, @osver, @osbld, @aname, @ahwid, @sname, @sstatus);";

                        cmd.Parameters.AddWithValue("@id", session.SessionId.ToString());
                        cmd.Parameters.AddWithValue("@start", session.StartedAt.ToString("o"));
                        cmd.Parameters.AddWithValue("@comp", session.CompletedAt.HasValue ? session.CompletedAt.Value.ToString("o") : (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@status", session.OverallStatus.ToString());
                        cmd.Parameters.AddWithValue("@osname", session.OsVersion != null ? session.OsVersion.OperatingSystemName : (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@osver", session.OsVersion != null ? $"{session.OsVersion.MajorVersion}.{session.OsVersion.MinorVersion}" : (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@osbld", session.OsVersion != null ? session.OsVersion.BuildNumber.ToString() : (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@aname", session.AdapterInfo != null ? session.AdapterInfo.FriendlyName : (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@ahwid", session.AdapterInfo != null ? session.AdapterInfo.HardwareId : (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@sname", session.BluetoothService != null ? session.BluetoothService.ServiceName : (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@sstatus", session.BluetoothService != null ? session.BluetoothService.Status : (object)DBNull.Value);

                        cmd.ExecuteNonQuery();
                    }

                    if (session.Results != null)
                    {
                        foreach (var res in session.Results)
                        {
                            using (var rCmd = conn.CreateCommand())
                            {
                                rCmd.CommandText = @"
                                    INSERT INTO DiagnosticResults 
                                    (SessionId, RuleId, TestName, Category, Status, Summary, TechnicalDetails, Evidence, PossibleCause, RecommendedAction, Confidence, ExecutedAtUtc)
                                    VALUES (@sid, @rid, @tname, @cat, @st, @sum, @tech, @ev, @cause, @rec, @conf, @ex);";

                                rCmd.Parameters.AddWithValue("@sid", session.SessionId.ToString());
                                rCmd.Parameters.AddWithValue("@rid", res.RuleId ?? string.Empty);
                                rCmd.Parameters.AddWithValue("@tname", res.TestName ?? string.Empty);
                                rCmd.Parameters.AddWithValue("@cat", res.Category ?? string.Empty);
                                rCmd.Parameters.AddWithValue("@st", res.Status.ToString());
                                rCmd.Parameters.AddWithValue("@sum", res.Summary ?? string.Empty);
                                rCmd.Parameters.AddWithValue("@tech", res.TechnicalDetails ?? string.Empty);
                                rCmd.Parameters.AddWithValue("@ev", res.Evidence ?? string.Empty);
                                rCmd.Parameters.AddWithValue("@cause", res.PossibleCause ?? string.Empty);
                                rCmd.Parameters.AddWithValue("@rec", res.RecommendedAction ?? string.Empty);
                                rCmd.Parameters.AddWithValue("@conf", res.Confidence ?? "High");
                                rCmd.Parameters.AddWithValue("@ex", res.ExecutedAt.ToString("o"));

                                rCmd.ExecuteNonQuery();
                            }
                        }
                    }

                    trans.Commit();
                }
            });
        }

        public Task<List<DiagnosticSession>> GetRecentSessionsAsync(int limit = 20)
        {
            return Task.Run(() =>
            {
                var sessions = new List<DiagnosticSession>();

                using (var conn = _db.CreateConnection())
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT * FROM DiagnosticSessions ORDER BY StartedAtUtc DESC LIMIT @lim;";
                        cmd.Parameters.AddWithValue("@lim", limit);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var sess = ReadSessionSummary(reader);
                                sessions.Add(sess);
                            }
                        }
                    }

                    // Populate results for these sessions
                    foreach (var sess in sessions)
                    {
                        sess.Results = LoadResultsForSession(conn, sess.SessionId);
                    }
                }

                return sessions;
            });
        }

        public Task<DiagnosticSession> GetSessionByIdAsync(Guid sessionId)
        {
            return Task.Run(() =>
            {
                using (var conn = _db.CreateConnection())
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT * FROM DiagnosticSessions WHERE SessionId = @id;";
                        cmd.Parameters.AddWithValue("@id", sessionId.ToString());

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var sess = ReadSessionSummary(reader);
                                sess.Results = LoadResultsForSession(conn, sess.SessionId);
                                return sess;
                            }
                        }
                    }
                }
                return null;
            });
        }

        public Task ClearAllSessionsAsync()
        {
            return Task.Run(() =>
            {
                using (var conn = _db.CreateConnection())
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "DELETE FROM DiagnosticResults; DELETE FROM DiagnosticSessions;";
                        cmd.ExecuteNonQuery();
                    }
                }
            });
        }

        private DiagnosticSession ReadSessionSummary(SQLiteDataReader reader)
        {
            var sess = new DiagnosticSession
            {
                SessionId = Guid.Parse(reader["SessionId"].ToString()),
                StartedAt = DateTime.Parse(reader["StartedAtUtc"].ToString(), null, System.Globalization.DateTimeStyles.RoundtripKind)
            };

            if (reader["CompletedAtUtc"] != DBNull.Value)
            {
                sess.CompletedAt = DateTime.Parse(reader["CompletedAtUtc"].ToString(), null, System.Globalization.DateTimeStyles.RoundtripKind);
            }

            if (reader["AdapterName"] != DBNull.Value || reader["AdapterHwId"] != DBNull.Value)
            {
                sess.AdapterInfo = new BluetoothAdapterInfo
                {
                    FriendlyName = reader["AdapterName"] != DBNull.Value ? reader["AdapterName"].ToString() : string.Empty,
                    HardwareId = reader["AdapterHwId"] != DBNull.Value ? reader["AdapterHwId"].ToString() : string.Empty
                };
            }

            if (reader["ServiceName"] != DBNull.Value)
            {
                sess.BluetoothService = new ServiceInfo
                {
                    ServiceName = reader["ServiceName"].ToString(),
                    Status = reader["ServiceStatus"] != DBNull.Value ? reader["ServiceStatus"].ToString() : string.Empty
                };
            }

            if (reader["OsName"] != DBNull.Value)
            {
                sess.OsVersion = new OsVersionInfo
                {
                    OperatingSystemName = reader["OsName"].ToString()
                };
            }

            return sess;
        }

        private List<DiagnosticResult> LoadResultsForSession(SQLiteConnection conn, Guid sessionId)
        {
            var results = new List<DiagnosticResult>();

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM DiagnosticResults WHERE SessionId = @sid ORDER BY Id ASC;";
                cmd.Parameters.AddWithValue("@sid", sessionId.ToString());

                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        Enum.TryParse(r["Status"].ToString(), out DiagnosticStatus st);

                        results.Add(new DiagnosticResult
                        {
                            RuleId = r["RuleId"]?.ToString(),
                            TestName = r["TestName"]?.ToString(),
                            Category = r["Category"]?.ToString(),
                            Status = st,
                            Summary = r["Summary"]?.ToString(),
                            TechnicalDetails = r["TechnicalDetails"]?.ToString(),
                            Evidence = r["Evidence"]?.ToString(),
                            PossibleCause = r["PossibleCause"]?.ToString(),
                            RecommendedAction = r["RecommendedAction"]?.ToString(),
                            Confidence = r["Confidence"]?.ToString(),
                            ExecutedAt = DateTime.Parse(r["ExecutedAtUtc"].ToString(), null, System.Globalization.DateTimeStyles.RoundtripKind)
                        });
                    }
                }
            }

            return results;
        }
    }
}
