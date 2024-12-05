using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using Google.Protobuf.WellKnownTypes;
using UnityEngine.InputSystem;

namespace CakeLab.ARFlow.DataBuffers
{
    using System.Diagnostics;
    using System.Text.RegularExpressions;
    using CakeLab.ARFlow.Utilities;
    using Clock;
    using Grpc.V1;
    using UnityEngine.Apple;

    public struct RawGyroscopeFrame
    {
        public DateTime DeviceTimestamp;
        public UnityEngine.Quaternion Attitude;
        public UnityEngine.Vector3 RotationRate;
        public UnityEngine.Vector3 Gravity;
        public UnityEngine.Vector3 Acceleration;

        public static explicit operator Grpc.V1.GyroscopeFrame(RawGyroscopeFrame rawFrame)
        {
            var gyroscopeFrameGrpc = new Grpc.V1.GyroscopeFrame
            {
                DeviceTimestamp = Timestamp.FromDateTime(rawFrame.DeviceTimestamp),
                Attitude = new Grpc.V1.Quaternion
                {
                    X = rawFrame.Attitude.x,
                    Y = rawFrame.Attitude.y,
                    Z = rawFrame.Attitude.z,
                    W = rawFrame.Attitude.w,
                },
                RotationRate = new Grpc.V1.Vector3
                {
                    X = rawFrame.RotationRate.x,
                    Y = rawFrame.RotationRate.y,
                    Z = rawFrame.RotationRate.z,
                },
                Gravity = new Grpc.V1.Vector3
                {
                    X = rawFrame.Gravity.x,
                    Y = rawFrame.Gravity.y,
                    Z = rawFrame.Gravity.z,
                },
                Acceleration = new Grpc.V1.Vector3
                {
                    X = rawFrame.Acceleration.x,
                    Y = rawFrame.Acceleration.y,
                    Z = rawFrame.Acceleration.z,
                },
            };
            return gyroscopeFrameGrpc;
        }

        public static explicit operator Grpc.V1.ARFrame(RawGyroscopeFrame rawFrame)
        {
            var arFrame = new Grpc.V1.ARFrame { GyroscopeFrame = (Grpc.V1.GyroscopeFrame)rawFrame };
            return arFrame;
        }
    }

    public class GyroscopeBuffer : IARFrameBuffer<RawGyroscopeFrame>
    {
        private readonly List<RawGyroscopeFrame> m_Buffer;
        private readonly Timer m_SamplingTimer;
        private bool m_IsCapturing;

        IClock m_Clock;

        public IClock Clock
        {
            get => m_Clock;
            set => m_Clock = value;
        }

        public IReadOnlyList<RawGyroscopeFrame> Buffer => m_Buffer;

        public GyroscopeBuffer(int initialBufferSize, IClock clock, double samplingIntervalMs = 50)
        {
            InternalDebug.Log($"Start Gyroscope Buffer with {initialBufferSize} frames");

            m_Buffer = new List<RawGyroscopeFrame>(initialBufferSize);
            m_Clock = clock;
            m_SamplingTimer = new Timer(samplingIntervalMs);
            m_SamplingTimer.Elapsed += OnSamplingTimerElapsed;
        }

        public void StartCapture()
        {
            InternalDebug.Log($"Start Gyroscope Capture {m_IsCapturing}");
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
            AddToBuffer(m_Clock.UtcNow);
        }

        private void AddToBuffer(DateTime deviceTimestampAtCapture)
        {
            var newFrame = new RawGyroscopeFrame
            {
                DeviceTimestamp = deviceTimestampAtCapture,
                Attitude = AttitudeSensor.current.attitude.ReadValue(),
                RotationRate =
                    UnityEngine.InputSystem.Gyroscope.current.angularVelocity.ReadValue(),
                Gravity = GravitySensor.current.gravity.ReadValue(),
                Acceleration = Accelerometer.current.acceleration.ReadValue(),
            };
            m_Buffer.Add(newFrame);
        }

        public void ClearBuffer()
        {
            m_Buffer.Clear();
        }

        public RawGyroscopeFrame TryAcquireLatestFrame()
        {
            return m_Buffer.LastOrDefault();
        }

        public ARFrame[] GetARFramesFromBuffer()
        {
            InternalDebug.Log($"Get Gyroscope {m_Buffer.Count} frames from buffer");
            return m_Buffer.Select(frame => (ARFrame)frame).ToArray();
        }


        public void Dispose()
        {
            StopCapture();
            ClearBuffer();
            m_SamplingTimer.Dispose();
        }
    }
}
