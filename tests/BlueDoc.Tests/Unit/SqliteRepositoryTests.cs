using System;
using System.IO;
using System.Threading.Tasks;
using BlueDoc.Core.Enums;
using BlueDoc.Core.Models;
using BlueDoc.Data;
using BlueDoc.Data.Repositories;

namespace BlueDoc.Tests.Unit
{
    public class SqliteRepositoryTests
    {
        public static async Task RunAllTests()
        {
            Console.WriteLine("[TEST SUITE] SQLite Local Storage & Repository Tests");
            string tempDb = Path.Combine(Path.GetTempPath(), $"bluedoc_test_{Guid.NewGuid():N}.db");

            try
            {
                var db = new SqliteDatabaseManager(tempDb);
                db.InitializeDatabase();
                Console.WriteLine("  ✓ SQLite Database initialized schema successfully.");

                await TestDiagnosticSessionRepository(db);
                await TestConnectionHistoryRepository(db);
                await TestRepairHistoryRepository(db);
                await TestDeviceCacheRepository(db);
                await TestSettingsRepository(db);
            }
            finally
            {
                if (File.Exists(tempDb))
                {
                    try { File.Delete(tempDb); } catch { }
                }
            }
        }

        private static async Task TestDiagnosticSessionRepository(SqliteDatabaseManager db)
        {
            var repo = new SqliteDiagnosticSessionRepository(db);
            var session = new DiagnosticSession
            {
                SessionId = Guid.NewGuid(),
                StartedAt = DateTime.UtcNow.AddMinutes(-5),
                CompletedAt = DateTime.UtcNow,
                AdapterInfo = new BluetoothAdapterInfo
                {
                    FriendlyName = "Realtek Bluetooth Adapter Test",
                    HardwareId = "USB\\VID_0BDA&PID_C822"
                },
                BluetoothService = new ServiceInfo
                {
                    ServiceName = "bthserv",
                    Status = "Running"
                },
                OsVersion = new OsVersionInfo
                {
                    OperatingSystemName = "Windows 11 Test Environment"
                }
            };

            session.Results.Add(new DiagnosticResult
            {
                RuleId = "TEST-01",
                TestName = "Adapter Detection",
                Category = "Hardware",
                Status = DiagnosticStatus.Passed,
                Summary = "Adapter operational",
                ExecutedAt = DateTime.UtcNow
            });

            await repo.SaveSessionAsync(session);

            var retrieved = await repo.GetSessionByIdAsync(session.SessionId);
            if (retrieved == null) throw new Exception("Failed to retrieve saved session by ID.");
            if (retrieved.Results.Count != 1) throw new Exception($"Expected 1 result, got {retrieved.Results.Count}");
            if (retrieved.Results[0].Status != DiagnosticStatus.Passed) throw new Exception("Result status mismatch.");
            Console.WriteLine("  ✓ DiagnosticSessionRepository save & load passed.");
        }

        private static async Task TestConnectionHistoryRepository(SqliteDatabaseManager db)
        {
            var repo = new SqliteConnectionHistoryRepository(db);
            await repo.RecordEventAsync(new ConnectionHistoryRecord
            {
                DeviceAddress = "00:11:22:33:44:55",
                DeviceName = "Test Bluetooth Headphones",
                DeviceType = "AudioVideo",
                EventType = "Connected",
                TimestampUtc = DateTime.UtcNow,
                Details = "Signal -62 dBm",
                IsSuccess = true
            });

            var list = await repo.GetHistoryAsync(10);
            if (list.Count != 1) throw new Exception($"Expected 1 history record, got {list.Count}");
            if (list[0].DeviceName != "Test Bluetooth Headphones") throw new Exception("DeviceName mismatch in history.");
            Console.WriteLine("  ✓ ConnectionHistoryRepository record & query passed.");
        }

        private static async Task TestRepairHistoryRepository(SqliteDatabaseManager db)
        {
            var repo = new SqliteRepairHistoryRepository(db);
            await repo.RecordRepairAsync(new RepairHistoryRecord
            {
                RepairKey = "RestartBthServ",
                ActionTitle = "Restart Bluetooth Support Service",
                Description = "Service restarted test",
                TargetComponent = "bthserv",
                RequiredAdmin = true,
                Status = "Success",
                VerificationResult = "Verified Running"
            });

            var list = await repo.GetRepairHistoryAsync(10);
            if (list.Count != 1) throw new Exception($"Expected 1 repair record, got {list.Count}");
            if (list[0].Status != "Success") throw new Exception("Repair status mismatch.");
            Console.WriteLine("  ✓ RepairHistoryRepository record & query passed.");
        }

        private static async Task TestDeviceCacheRepository(SqliteDatabaseManager db)
        {
            var repo = new SqliteDeviceCacheRepository(db);
            var dev = new BluetoothDeviceInfo
            {
                BluetoothAddress = "AA:BB:CC:DD:EE:FF",
                DeviceName = "Cached Test Mouse",
                DeviceType = DeviceType.Classic,
                IsPaired = true,
                IsConnected = true,
                Rssi = -55,
                CategoryName = "Mouse",
                FirstDiscovered = DateTime.UtcNow,
                LastSeen = DateTime.UtcNow
            };

            await repo.UpsertDiscoveredDeviceAsync(dev);
            var devices = await repo.GetDiscoveredDevicesAsync();
            if (devices.Count != 1) throw new Exception($"Expected 1 cached device, got {devices.Count}");
            if (devices[0].BluetoothAddress != "AA:BB:CC:DD:EE:FF") throw new Exception("Device MAC mismatch in cache.");
            Console.WriteLine("  ✓ DeviceCacheRepository upsert & retrieve passed.");
        }

        private static async Task TestSettingsRepository(SqliteDatabaseManager db)
        {
            var repo = new SqliteSettingsRepository(db);
            await repo.SetSettingAsync("TestKey", "TestValue");
            string val = await repo.GetSettingAsync("TestKey");
            if (val != "TestValue") throw new Exception("Settings string mismatch.");

            await repo.SetBoolSettingAsync("BoolKey", true);
            bool bVal = await repo.GetBoolSettingAsync("BoolKey");
            if (!bVal) throw new Exception("Settings bool mismatch.");
            Console.WriteLine("  ✓ SettingsRepository set & get passed.");
        }
    }
}
