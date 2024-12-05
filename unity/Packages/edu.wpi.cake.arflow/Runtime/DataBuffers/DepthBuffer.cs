using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf.WellKnownTypes;
using UnityEngine.XR.ARFoundation;

namespace CakeLab.ARFlow.DataBuffers
{
    using Grpc.V1;
    using Utilities;
    using GrpcVector2Int = Grpc.V1.Vector2Int;
    using GrpcXRCpuImage = Grpc.V1.XRCpuImage;
    using UnityVector2Int = UnityEngine.Vector2Int;
    using UnityXRCpuImage = UnityEngine.XR.ARSubsystems.XRCpuImage;

    public struct RawDepthFrame
    {
        public DateTime DeviceTimestamp;
        public bool EnvironmentDepthTemporalSmoothingEnabled;
        public UnityVector2Int Dimensions;
        public UnityXRCpuImage.Format Format;
        public double ImageTimestamp;
        public UnityXRCpuImage.Plane[] Planes;

        public static explicit operator Grpc.V1.DepthFrame(RawDepthFrame rawFrame)
        {
            var xrCpuImageGrpc = new GrpcXRCpuImage
            {
                Dimensions = new GrpcVector2Int
                {
                    X = rawFrame.Dimensions.x,
                    Y = rawFrame.Dimensions.y,
                },
                Format = (XRCpuImage.Types.Format)rawFrame.Format,
                Timestamp = rawFrame.ImageTimestamp,
            };
            xrCpuImageGrpc.Planes.AddRange(
                (rawFrame.Planes ?? Array.Empty<UnityXRCpuImage.Plane>()).Select(
                    plane => new Grpc.V1.XRCpuImage.Types.Plane
                    {
                        RowStride = plane.rowStride,
                        PixelStride = plane.pixelStride,
                        Data = Google.Protobuf.ByteString.CopyFrom(plane.data.ToArray()),
                    }
                )
            );
            var depthFrameGrpc = new Grpc.V1.DepthFrame
            {
                DeviceTimestamp = Timestamp.FromDateTime(rawFrame.DeviceTimestamp),
                Image = xrCpuImageGrpc,
            };
            return depthFrameGrpc;
        }

        public static explicit operator Grpc.V1.ARFrame(RawDepthFrame rawFrame)
        {
            var arFrame = new Grpc.V1.ARFrame { DepthFrame = (Grpc.V1.DepthFrame)rawFrame };
            return arFrame;
        }
    }

    public class DepthBuffer : IDataBuffer<RawDepthFrame>
    {
        AROcclusionManager m_OcclusionManager;

        /// <summary>
        /// The AROcclusionManager that will provide the depth frames.
        /// </summary>
        public AROcclusionManager OcclusionManager
        {
            get => m_OcclusionManager;
            set => m_OcclusionManager = value;
        }

        NtpDateTimeManager m_NtpManager;

        public NtpDateTimeManager NtpManager
        {
            get => m_NtpManager;
            set => m_NtpManager = value;
        }

        private readonly List<RawDepthFrame> m_Buffer;

        public IReadOnlyList<RawDepthFrame> Buffer => m_Buffer;

        public DepthBuffer(
            int initialBufferSize,
            AROcclusionManager occlusionManager,
            NtpDateTimeManager ntpManager
        )
        {
            m_Buffer = new List<RawDepthFrame>(initialBufferSize);
            m_OcclusionManager = occlusionManager;
            m_NtpManager = ntpManager;
        }

        public void StartCapture()
        {
            m_OcclusionManager.frameReceived += OnOcclusionFrameReceived;
        }

        public void StopCapture()
        {
            m_OcclusionManager.frameReceived -= OnOcclusionFrameReceived;
        }

        private void OnOcclusionFrameReceived(AROcclusionFrameEventArgs _)
        {
            if (!m_OcclusionManager.TryAcquireEnvironmentDepthCpuImage(out UnityXRCpuImage image))
            {
                InternalDebug.Log("Failed to acquire occlusion frame data or intrinsics.");
                return;
            }

            AddToBuffer(image, m_NtpManager.UtcNow);
            image.Dispose();
        }

        private void AddToBuffer(UnityXRCpuImage image, DateTime deviceTimestampAtCapture)
        {
            var newFrame = new RawDepthFrame
            {
                DeviceTimestamp = deviceTimestampAtCapture,
                EnvironmentDepthTemporalSmoothingEnabled =
                    m_OcclusionManager.environmentDepthTemporalSmoothingEnabled,
                Dimensions = image.dimensions,
                Format = image.format,
                ImageTimestamp = image.timestamp,
                Planes = Enumerable
                    .Range(0, image.planeCount)
                    .Select(i =>
                    {
                        // Make a deep copy to decouple lifetime of the image from the buffer
                        var plane = image.GetPlane(i);
                        return new UnityXRCpuImage.Plane(
                            plane.rowStride,
                            plane.pixelStride,
                            plane.data
                        );
                    })
                    .ToArray(),
            };
            m_Buffer.Add(newFrame);
        }

        public void ClearBuffer()
        {
            m_Buffer.Clear();
        }

        public RawDepthFrame TryAcquireLatestFrame()
        {
            return m_Buffer.LastOrDefault();
        }

        public void Dispose()
        {
            StopCapture();
            ClearBuffer();
        }
    }
}
