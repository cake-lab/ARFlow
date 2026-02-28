using System;
using System.Text;
using System.Collections.Generic;
using MessagePack;

namespace ARFlow
{
    // ==========================================
    // SensorFlex Protocol Encoders
    // ==========================================

    public enum MessageTypeId : ushort
    {
        CreateSessionRequestMsg = 0,
        CreateSessionResponseMsg = 1,
        ColorFrameMsg = 2,
        DepthFrameMsg = 3
    }

    public static class SensorFlex
    {
        private static readonly byte[] MagicBytes = Encoding.ASCII.GetBytes("SF");
        private const byte Version = 1;

        public static byte[] Encode<T>(T payload, MessageTypeId typeId)
        {
            byte[] msgPackData = MessagePackSerializer.Serialize(payload);
            
            // Header: Magic(2) + Version(1) + TypeID(2) + Length(4) = 9 bytes
            byte[] buffer = new byte[9 + msgPackData.Length];
            
            // 1. Magic "SF"
            buffer[0] = MagicBytes[0];
            buffer[1] = MagicBytes[1];
            
            // 2. Version
            buffer[2] = Version;
            
            // 3. TypeID (Big Endian)
            ushort id = (ushort)typeId;
            buffer[3] = (byte)(id >> 8);
            buffer[4] = (byte)(id & 0xFF);
            
            // 4. Length (Big Endian)
            uint length = (uint)msgPackData.Length;
            buffer[5] = (byte)((length >> 24) & 0xFF);
            buffer[6] = (byte)((length >> 16) & 0xFF);
            buffer[7] = (byte)((length >> 8) & 0xFF);
            buffer[8] = (byte)(length & 0xFF);
            
            // 5. Payload
            Buffer.BlockCopy(msgPackData, 0, buffer, 9, msgPackData.Length);
            
            return buffer;
        }
    }

    // ==========================================
    // Data Models
    // ==========================================

    [MessagePackObject]
    public class DeviceMsg
    {
        [Key("device_id")] public string DeviceId { get; set; }
        [Key("device_name")] public string DeviceName { get; set; }
        [Key("os_version")] public string OsVersion { get; set; }
    }

    [MessagePackObject]
    public class SessionUuidMsg
    {
        [Key("value")] public string Value { get; set; }
    }

    [MessagePackObject]
    public class SessionMetadataMsg
    {
        [Key("save_path")] public string SavePath { get; set; }
    }

    [MessagePackObject]
    public class SessionMsg
    {
        [Key("id")] public SessionUuidMsg Id { get; set; }
        [Key("metadata")] public SessionMetadataMsg Metadata { get; set; }
        [Key("devices")] public List<DeviceMsg> Devices { get; set; }
    }

    [MessagePackObject]
    public class CreateSessionRequestMsg
    {
        [Key("session_metadata")] public SessionMetadataMsg SessionMetadata { get; set; }
        [Key("device")] public DeviceMsg Device { get; set; }
    }

    [MessagePackObject]
    public class CreateSessionResponseMsg
    {
        [Key("session")] public SessionMsg Session { get; set; }
    }

    [MessagePackObject]
    public class Vector2IntMsg
    {
        [Key("x")] public int X { get; set; }
        [Key("y")] public int Y { get; set; }
    }

    [MessagePackObject]
    public class PlaneMsg
    {
        [Key("row_stride")] public int RowStride { get; set; }
        [Key("pixel_stride")] public int PixelStride { get; set; }
        [Key("data")] public byte[] Data { get; set; }
    }

    [MessagePackObject]
    public class XRCpuImageMsg
    {
        [Key("dimensions")] public Vector2IntMsg Dimensions { get; set; }
        [Key("format")] public int Format { get; set; }
        [Key("timestamp")] public double Timestamp { get; set; }
        [Key("planes")] public List<PlaneMsg> Planes { get; set; }
    }

    [MessagePackObject]
    public class ColorFrameMsg
    {
        [Key("device_timestamp")] public double DeviceTimestamp { get; set; }
        [Key("image")] public XRCpuImageMsg Image { get; set; }
    }

    [MessagePackObject]
    public class DepthFrameMsg
    {
        [Key("device_timestamp")] public double DeviceTimestamp { get; set; }
        [Key("image")] public XRCpuImageMsg Image { get; set; }
    }
}
