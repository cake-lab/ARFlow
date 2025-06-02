using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf.WellKnownTypes;
using UnityEngine;

namespace CakeLab.ARFlow.DataBuffers
{
    using CakeLab.ARFlow.Utilities;
    using Clock;
    using Grpc.V1;

    public struct RawTransformFrame
    {
        public DateTime DeviceTimestamp;
        public byte[] Data;

        public static explicit operator Grpc.V1.TransformFrame(RawTransformFrame rawFrame)
        {
            var transformFrameGrpc = new Grpc.V1.TransformFrame
            {
                DeviceTimestamp = Timestamp.FromDateTime(rawFrame.DeviceTimestamp),
                Data = Google.Protobuf.ByteString.CopyFrom(rawFrame.Data),
            };
            return transformFrameGrpc;
        }

        public static explicit operator Grpc.V1.ARFrame(RawTransformFrame rawFrame)
        {
            var arFrame = new Grpc.V1.ARFrame { TransformFrame = (Grpc.V1.TransformFrame)rawFrame };
            return arFrame;
        }
    }

    public class TransformBuffer : IARFrameBuffer<RawTransformFrame>
    {
        Camera m_MainCamera;

        public Camera MainCameraManager
        {
            get => m_MainCamera;
            set => m_MainCamera = value;
        }

        IClock m_Clock;

        public IClock Clock
        {
            get => m_Clock;
            set => m_Clock = value;
        }

        private const int m_TransformDataSize = 3 * 4 * sizeof(float);
        private float m_SamplingIntervalMs;
        private bool m_IsCapturing;
        private ConcurrentQueue<RawTransformFrame> m_Buffer;

        public ConcurrentQueue<RawTransformFrame> Buffer => m_Buffer;

        public TransformBuffer(Camera mainCamera, IClock clock, float samplingRateHz = 60)
        {
            m_Buffer = new ConcurrentQueue<RawTransformFrame>();
            m_MainCamera = mainCamera;
            m_Clock = clock;
            m_SamplingIntervalMs = (float)(1000.0 / samplingRateHz);
        }

        public void StartCapture()
        {
            if (m_IsCapturing)
            {
                return;
            }
            m_IsCapturing = true;
            CaptureTransformAsync();
        }

        public void StopCapture()
        {
            if (!m_IsCapturing)
            {
                return;
            }
            m_IsCapturing = false;
        }

        private async void CaptureTransformAsync()
        {
            while (m_IsCapturing)
            {
                await Awaitable.WaitForSecondsAsync(m_SamplingIntervalMs / 1000);
                AddToBuffer(m_Clock.UtcNow);
            }
        }

        private void AddToBuffer(DateTime deviceTimestampAtCapture)
        {
            var m = m_MainCamera.transform.localToWorldMatrix;
            var cameraTransformBytes = new byte[m_TransformDataSize];
            System.Buffer.BlockCopy(
                new[]
                {
                    m.m00,
                    m.m01,
                    m.m02,
                    m.m03,
                    m.m10,
                    m.m11,
                    m.m12,
                    m.m13,
                    m.m20,
                    m.m21,
                    m.m22,
                    m.m23,
                },
                0,
                cameraTransformBytes,
                0,
                m_TransformDataSize
            );
            InternalDebug.Log($"Camera transform: {m}");
            var newFrame = new RawTransformFrame
            {
                DeviceTimestamp = deviceTimestampAtCapture,
                Data = cameraTransformBytes,
            };
            m_Buffer.Enqueue(newFrame);
        }

        public RawTransformFrame TryAcquireLatestFrame()
        {
            return m_Buffer.LastOrDefault();
        }

        public IEnumerable<ARFrame> TakeARFrames()
        {
            ConcurrentQueue<RawTransformFrame> oldFrames;
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
