using System;
using System.Data;
using System.Data.SQLite;
using System.IO;

namespace BlueDoc.Data
{
    public class SqliteDatabaseManager
    {
        private readonly string _databasePath;
        private readonly string _connectionString;

        public SqliteDatabaseManager(string customPath = null)
        {
            if (string.IsNullOrWhiteSpace(customPath))
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string appFolder = Path.Combine(localAppData, "UNLEFT", "BlueDoc");
                if (!Directory.Exists(appFolder))
                {
                    Directory.CreateDirectory(appFolder);
                }
                _databasePath = Path.Combine(appFolder, "bluedoc.db");
            }
            else
            {
                _databasePath = customPath;
                string dir = Path.GetDirectoryName(_databasePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }

            _connectionString = $"Data Source={_databasePath};Version=3;Journal Mode=WAL;BusyTimeout=5000;";
        }

        public string DatabasePath => _databasePath;

        public SQLiteConnection CreateConnection()
        {
            var conn = new SQLiteConnection(_connectionString);
            conn.Open();
            return conn;
        }

        public void InitializeDatabase()
        {
            using (var conn = CreateConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    PRAGMA foreign_keys = ON;

                    CREATE TABLE IF NOT EXISTS DiagnosticSessions (
                        SessionId TEXT PRIMARY KEY,
                        StartedAtUtc TEXT NOT NULL,
                        CompletedAtUtc TEXT,
                        OverallStatus TEXT NOT NULL,
                        OsName TEXT,
                        OsVersion TEXT,
                        OsBuild TEXT,
                        AdapterName TEXT,
                        AdapterHwId TEXT,
                        ServiceName TEXT,
                        ServiceStatus TEXT
                    );

                    CREATE TABLE IF NOT EXISTS DiagnosticResults (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        SessionId TEXT NOT NULL,
                        RuleId TEXT NOT NULL,
                        TestName TEXT NOT NULL,
                        Category TEXT NOT NULL,
                        Status TEXT NOT NULL,
                        Summary TEXT,
                        TechnicalDetails TEXT,
                        Evidence TEXT,
                        PossibleCause TEXT,
                        RecommendedAction TEXT,
                        Confidence TEXT,
                        ExecutedAtUtc TEXT NOT NULL,
                        FOREIGN KEY(SessionId) REFERENCES DiagnosticSessions(SessionId) ON DELETE CASCADE
                    );

                    CREATE TABLE IF NOT EXISTS DiscoveredDevices (
                        DeviceAddress TEXT PRIMARY KEY,
                        DeviceName TEXT,
                        DeviceType TEXT,
                        IsPaired INTEGER,
                        IsConnected INTEGER,
                        Rssi INTEGER,
                        MajorDeviceClass TEXT,
                        CategoryName TEXT,
                        CategoryIcon TEXT,
                        FirstDiscoveredUtc TEXT NOT NULL,
                        LastSeenUtc TEXT NOT NULL
                    );

                    CREATE TABLE IF NOT EXISTS ConnectionHistory (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        DeviceAddress TEXT NOT NULL,
                        DeviceName TEXT,
                        DeviceType TEXT,
                        EventType TEXT NOT NULL,
                        TimestampUtc TEXT NOT NULL,
                        Details TEXT,
                        IsSuccess INTEGER NOT NULL,
                        DiagnosticSessionId TEXT
                    );

                    CREATE TABLE IF NOT EXISTS RepairHistory (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        RepairKey TEXT NOT NULL,
                        ActionTitle TEXT NOT NULL,
                        Description TEXT,
                        TargetComponent TEXT,
                        TimestampUtc TEXT NOT NULL,
                        RequiredAdmin INTEGER NOT NULL,
                        Status TEXT NOT NULL,
                        VerificationResult TEXT,
                        ErrorMessage TEXT
                    );

                    CREATE TABLE IF NOT EXISTS ApplicationSettings (
                        Key TEXT PRIMARY KEY,
                        Value TEXT NOT NULL,
                        UpdatedUtc TEXT NOT NULL
                    );

                    CREATE INDEX IF NOT EXISTS IX_DiagnosticResults_SessionId ON DiagnosticResults(SessionId);
                    CREATE INDEX IF NOT EXISTS IX_ConnectionHistory_Timestamp ON ConnectionHistory(TimestampUtc);
                    CREATE INDEX IF NOT EXISTS IX_ConnectionHistory_DeviceAddress ON ConnectionHistory(DeviceAddress);
                    CREATE INDEX IF NOT EXISTS IX_RepairHistory_Timestamp ON RepairHistory(TimestampUtc);
                ";
                cmd.ExecuteNonQuery();
            }
        }
    }
}
