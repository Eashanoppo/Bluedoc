using System;
using BlueDoc.Core.Enums;

namespace BlueDoc.Core.Models
{
    public class DeviceOperationResult
    {
        public bool Success { get; set; }
        public DeviceOperationStatus Status { get; set; } = DeviceOperationStatus.Succeeded;
        public string Message { get; set; } = string.Empty;
        public string ErrorDetails { get; set; } = string.Empty;
        public uint NativeErrorCode { get; set; }

        public static DeviceOperationResult Ok(string message)
        {
            return new DeviceOperationResult
            {
                Success = true,
                Status = DeviceOperationStatus.Succeeded,
                Message = message
            };
        }

        public static DeviceOperationResult InProgress(string message)
        {
            return new DeviceOperationResult
            {
                Success = true,
                Status = DeviceOperationStatus.InProgress,
                Message = message
            };
        }

        public static DeviceOperationResult Fail(string message, string details = "", uint errorCode = 0)
        {
            return new DeviceOperationResult
            {
                Success = false,
                Status = DeviceOperationStatus.Failed,
                Message = message,
                ErrorDetails = details,
                NativeErrorCode = errorCode
            };
        }

        public static DeviceOperationResult Unsupported(string message, string details = "")
        {
            return new DeviceOperationResult
            {
                Success = false,
                Status = DeviceOperationStatus.Unsupported,
                Message = message,
                ErrorDetails = details
            };
        }

        public static DeviceOperationResult RequiresInteraction(string message, string details = "")
        {
            return new DeviceOperationResult
            {
                Success = false,
                Status = DeviceOperationStatus.RequiresUserInteraction,
                Message = message,
                ErrorDetails = details
            };
        }
    }
}
