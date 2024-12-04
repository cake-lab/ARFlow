using UnityEngine;

namespace CakeLab.ARFlow.Utilities
{
    using Grpc.V1;

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
