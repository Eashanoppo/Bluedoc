using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Threading.Tasks;
using BlueDoc.Core.Enums;
using BlueDoc.Core.Interfaces;
using BlueDoc.Core.Models;

namespace BlueDoc.Data.Repositories
{
    public class SqliteDeviceCacheRepository : IDeviceCacheRepository
    {
        private readonly SqliteDatabaseManager _db;

        public SqliteDeviceCacheRepository(SqliteDatabaseManager db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public Task UpsertDiscoveredDeviceAsync(BluetoothDeviceInfo device)
        {
            return Task.Run(() =>
            {
                using (var conn = _db.CreateConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        INSERT INTO DiscoveredDevices 
                        (DeviceAddress, DeviceName, DeviceType, IsPaired, IsConnected, Rssi, MajorDeviceClass, CategoryName, CategoryIcon, FirstDiscoveredUtc, LastSeenUtc)
                        VALUES (@addr, @name, @type, @pair, @conn, @rssi, @major, @cat, @icon, @first, @last)
                        ON CONFLICT(DeviceAddress) DO UPDATE SET
                            DeviceName = excluded.DeviceName,
                            DeviceType = excluded.DeviceType,
                            IsPaired = excluded.IsPaired,
                            IsConnected = excluded.IsConnected,
                            Rssi = excluded.Rssi,
                            MajorDeviceClass = excluded.MajorDeviceClass,
                            CategoryName = excluded.CategoryName,
                            CategoryIcon = excluded.CategoryIcon,
                            LastSeenUtc = excluded.LastSeenUtc;";

                    cmd.Parameters.AddWithValue("@addr", device.BluetoothAddress ?? string.Empty);
                    cmd.Parameters.AddWithValue("@name", device.DeviceName ?? string.Empty);
                    cmd.Parameters.AddWithValue("@type", device.DeviceType.ToString());
                    cmd.Parameters.AddWithValue("@pair", device.IsPaired ? 1 : 0);
                    cmd.Parameters.AddWithValue("@conn", device.IsConnected ? 1 : 0);
                    cmd.Parameters.AddWithValue("@rssi", device.Rssi.HasValue ? (object)device.Rssi.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@major", device.MajorDeviceClass ?? string.Empty);
                    cmd.Parameters.AddWithValue("@cat", device.CategoryName ?? string.Empty);
                    cmd.Parameters.AddWithValue("@icon", device.CategoryIcon ?? string.Empty);
                    cmd.Parameters.AddWithValue("@first", device.FirstDiscovered.ToString("o"));
                    cmd.Parameters.AddWithValue("@last", device.LastSeen.ToString("o"));

                    cmd.ExecuteNonQuery();
                }
            });
        }

        public Task<List<BluetoothDeviceInfo>> GetDiscoveredDevicesAsync()
        {
            return Task.Run(() =>
            {
                var list = new List<BluetoothDeviceInfo>();

                using (var conn = _db.CreateConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM DiscoveredDevices ORDER BY LastSeenUtc DESC;";

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Enum.TryParse(reader["DeviceType"].ToString(), out DeviceType dt);

                            var dev = new BluetoothDeviceInfo
                            {
                                BluetoothAddress = reader["DeviceAddress"]?.ToString(),
                                DeviceName = reader["DeviceName"]?.ToString(),
                                DeviceType = dt,
                                IsPaired = Convert.ToInt32(reader["IsPaired"]) == 1,
                                IsConnected = Convert.ToInt32(reader["IsConnected"]) == 1,
                                MajorDeviceClass = reader["MajorDeviceClass"]?.ToString(),
                                CategoryName = reader["CategoryName"]?.ToString(),
                                CategoryIcon = reader["CategoryIcon"]?.ToString(),
                                FirstDiscovered = DateTime.Parse(reader["FirstDiscoveredUtc"].ToString(), null, System.Globalization.DateTimeStyles.RoundtripKind),
                                LastSeen = DateTime.Parse(reader["LastSeenUtc"].ToString(), null, System.Globalization.DateTimeStyles.RoundtripKind),
                                IsRemembered = true
                            };

                            if (reader["Rssi"] != DBNull.Value)
                            {
                                dev.Rssi = Convert.ToInt32(reader["Rssi"]);
                            }

                            list.Add(dev);
                        }
                    }
                }

                return list;
            });
        }

        public Task ClearDevicesAsync()
        {
            return Task.Run(() =>
            {
                using (var conn = _db.CreateConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM DiscoveredDevices;";
                    cmd.ExecuteNonQuery();
                }
            });
        }
    }
}
