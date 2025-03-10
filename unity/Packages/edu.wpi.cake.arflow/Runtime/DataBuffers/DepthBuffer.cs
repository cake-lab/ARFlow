using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Collections.Generic;
using Google.Protobuf.WellKnownTypes;
using UnityEngine.XR.ARFoundation;

namespace CakeLab.ARFlow.DataBuffers
{
    using Clock;
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
        public RawARPlane[] Planes;

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
                rawFrame.Planes.Select(plane => (Grpc.V1.XRCpuImage.Types.Plane)plane)
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

    public class DepthBuffer : IARFrameBuffer<RawDepthFrame>
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

        IClock m_Clock;

        public IClock Clock
        {
            get => m_Clock;
            set => m_Clock = value;
        }

        private ConcurrentQueue<RawDepthFrame> m_Buffer;

        public ConcurrentQueue<RawDepthFrame> Buffer => m_Buffer;

        public DepthBuffer(AROcclusionManager occlusionManager, IClock clock)
        {
            m_Buffer = new ConcurrentQueue<RawDepthFrame>();
            m_OcclusionManager = occlusionManager;
            m_Clock = clock;
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

            AddToBuffer(image, m_Clock.UtcNow);
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
                    .Select(i => (RawARPlane)image.GetPlane(i))
                    .ToArray(),
            };
            m_Buffer.Enqueue(newFrame);
        }

        public RawDepthFrame TryAcquireLatestFrame()
        {
            return m_Buffer.LastOrDefault();
        }

        public IEnumerable<ARFrame> TakeARFrames()
        {
            ConcurrentQueue<RawDepthFrame> oldFrames;
            lock (m_Buffer)
            {
                oldFrames = m_Buffer;
                m_Buffer = new();
            }
            return oldFrames.Select(frame => (ARFrame)frame);
        }

        public void Dispose()
        {
            StopCapture();
            m_Buffer.Clear();
        }
    }
}
