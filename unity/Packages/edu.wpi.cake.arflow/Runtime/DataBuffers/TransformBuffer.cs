using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using Google.Protobuf.WellKnownTypes;
using UnityEngine;

namespace CakeLab.ARFlow.DataBuffers
{
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
        private readonly List<RawTransformFrame> m_Buffer;

        public IReadOnlyList<RawTransformFrame> Buffer => m_Buffer;

        public TransformBuffer(
            int initialBufferSize,
            Camera mainCamera,
            IClock clock,
            float samplingIntervalMs = 50
        )
        {
            m_Buffer = new List<RawTransformFrame>(initialBufferSize);
            m_MainCamera = mainCamera;
            m_Clock = clock;
            m_SamplingIntervalMs = samplingIntervalMs;
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
            var newFrame = new RawTransformFrame
            {
                DeviceTimestamp = deviceTimestampAtCapture,
                Data = cameraTransformBytes,
            };
            m_Buffer.Add(newFrame);
        }

        public void ClearBuffer()
        {
            m_Buffer.Clear();
        }

        public RawTransformFrame TryAcquireLatestFrame()
        {
            return m_Buffer.LastOrDefault();
        }

        public ARFrame[] GetARFramesFromBuffer()
        {
            return m_Buffer.Select(frame => (ARFrame)frame).ToArray();
        }

        public void Dispose()
        {
            StopCapture();
            ClearBuffer();
        }
    }
}
