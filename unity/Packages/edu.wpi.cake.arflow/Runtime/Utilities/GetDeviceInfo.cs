using UnityEngine;

using CakeLab.ARFlow.Grpc;
using CakeLab.ARFlow.Grpc.V1;
using CakeLab.ARFlow.Utilities;

namespace CakeLab.ARFlow.Utilities
{
    public static class GetDeviceInfo
    {
        public static Device GetDevice()
        {
            return new Device
            {
                Model = SystemInfo.deviceModel,
                Name = SystemInfo.deviceName,
                Type = (Device.Types.Type)SystemInfo.deviceType,
                Uid = SystemInfo.deviceUniqueIdentifier,
            };
        }
    }
}
