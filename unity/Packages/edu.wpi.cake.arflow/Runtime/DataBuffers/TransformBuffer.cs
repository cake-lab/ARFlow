using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using Google.Protobuf.WellKnownTypes;
using UnityEngine;

namespace CakeLab.ARFlow.DataBuffers
{
    using Utilities;

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

    public class TransformBuffer : IDataBuffer<RawTransformFrame>
    {
        Camera m_MainCamera;

        public Camera MainCameraManager
        {
            get => m_MainCamera;
            set => m_MainCamera = value;
        }

        NtpDateTimeManager m_NtpManager;

        public NtpDateTimeManager NtpManager
        {
            get => m_NtpManager;
            set => m_NtpManager = value;
        }

        private readonly Timer m_SamplingTimer;
        private bool m_IsCapturing;
        private const int m_TransformDataSize = 3 * 4 * sizeof(float);
        private readonly List<RawTransformFrame> m_Buffer;

        public IReadOnlyList<RawTransformFrame> Buffer => m_Buffer;

        public TransformBuffer(
            int initialBufferSize,
            Camera mainCamera,
            NtpDateTimeManager ntpManager,
            double samplingIntervalMs = 50
        )
        {
            m_Buffer = new List<RawTransformFrame>(initialBufferSize);
            m_MainCamera = mainCamera;
            m_NtpManager = ntpManager;
            m_SamplingTimer = new Timer(samplingIntervalMs);
            m_SamplingTimer.Elapsed += OnSamplingTimerElapsed;
        }

        public void StartCapture()
        {
            if (m_IsCapturing)
            {
                return;
            }
            m_IsCapturing = true;
            m_SamplingTimer.Start();
        }

        public void StopCapture()
        {
            if (!m_IsCapturing)
            {
                return;
            }
            m_SamplingTimer.Stop();
            m_IsCapturing = false;
        }

        private void OnSamplingTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (!m_IsCapturing)
            {
                return;
            }
            AddToBuffer(m_NtpManager.UtcNow);
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

        public void Dispose()
        {
            StopCapture();
            ClearBuffer();
            m_SamplingTimer.Dispose();
        }
    }
}
