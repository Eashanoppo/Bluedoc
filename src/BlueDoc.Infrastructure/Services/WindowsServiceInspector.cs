using System;
using System.Collections.Generic;
using System.ServiceProcess;
using BlueDoc.Core.Interfaces;
using BlueDoc.Core.Models;

namespace BlueDoc.Infrastructure.Services
{
    public class WindowsServiceInspector : IServiceInspector
    {
        private static readonly string[] RelevantServices = new[]
        {
            "bthserv",    // Bluetooth Support Service (Core)
            "BthAvctpSvc", // Bluetooth Audio Gateway / AVCTP Service
            "BTAGService", // Bluetooth Audio Gateway Service
            "BluetoothUserService" // Modern Per-User Bluetooth Service on Win10/11
        };

        public ServiceInfo GetServiceInfo(string serviceName)
        {
            var info = new ServiceInfo
            {
                ServiceName = serviceName,
                DisplayName = serviceName
            };

            try
            {
                using (var sc = new ServiceController(serviceName))
                {
                    info.DisplayName = sc.DisplayName;
                    info.Status = sc.Status.ToString();
                    info.StartupType = sc.StartType.ToString();
                    info.Exists = true;
                }
            }
            catch (InvalidOperationException)
            {
                // Service does not exist
                info.Exists = false;
                info.Status = "NotInstalled";
                info.ErrorMessage = $"Service '{serviceName}' is not installed on this system.";
            }
            catch (Exception ex)
            {
                info.Exists = false;
                info.Status = "Error";
                info.ErrorMessage = ex.Message;
            }

            return info;
        }

        public IReadOnlyList<ServiceInfo> GetAllBluetoothServices()
        {
            var list = new List<ServiceInfo>();
            foreach (var svcName in RelevantServices)
            {
                var info = GetServiceInfo(svcName);
                if (info.Exists)
                {
                    list.Add(info);
                }
            }

            // Always ensure bthserv is reported even if not installed
            if (!list.Exists(s => string.Equals(s.ServiceName, "bthserv", StringComparison.OrdinalIgnoreCase)))
            {
                list.Insert(0, GetServiceInfo("bthserv"));
            }

            return list;
        }
    }
}
