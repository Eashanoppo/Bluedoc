using System;
using System.Diagnostics;
using BlueDoc.Core.Models;
using BlueDoc.Infrastructure.Drivers;

namespace BlueDoc.Application.Services
{
    public class DriverService
    {
        private readonly DriverAssistant _assistant;

        public DriverService(DriverAssistant assistant = null)
        {
            _assistant = assistant ?? new DriverAssistant();
        }

        public string GetManufacturerSupportLink(HardwareIdDetails hwDetails)
        {
            return _assistant.GetManufacturerSupportUrl(hwDetails);
        }

        public string ExplainProblemCode(int problemCode)
        {
            return _assistant.ExplainProblemCode(problemCode);
        }

        public string GetRollbackGuide(string adapterName)
        {
            return _assistant.GenerateRollbackInstructions(adapterName);
        }

        public string GetReinstallationGuide(string adapterName, string supportUrl)
        {
            return _assistant.GenerateReinstallInstructions(adapterName, supportUrl);
        }

        public bool OpenDeviceManager()
        {
            return _assistant.OpenDeviceManager();
        }

        public bool OpenWindowsBluetoothSettings()
        {
            return _assistant.OpenWindowsBluetoothSettings();
        }
    }
}
