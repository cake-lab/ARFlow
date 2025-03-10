using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf.WellKnownTypes;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CakeLab.ARFlow.DataBuffers
{
    using CakeLab.ARFlow.Utilities;
    using Clock;
    using Grpc.V1;

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
        private ConcurrentQueue<RawGyroscopeFrame> m_Buffer;
        private float m_SamplingIntervalMs;
        private bool m_IsCapturing;

        IClock m_Clock;

        public IClock Clock
        {
            get => m_Clock;
            set => m_Clock = value;
        }
        /*
               m_Device = GetDeviceInfo.GetDevice();

                if (UnityEngine.InputSystem.Gyroscope.current != null)
                {
                    InputSystem.EnableDevice(UnityEngine.InputSystem.Gyroscope.current);
                }
                if (AttitudeSensor.current != null)
                {
                    InputSystem.EnableDevice(AttitudeSensor.current);
                }
                if (Accelerometer.current != null)
                {
                    InputSystem.EnableDevice(Accelerometer.current);
                }
                if (GravitySensor.current != null)
                {
                    InputSystem.EnableDevice(GravitySensor.current);
                }

        */
        public ConcurrentQueue<RawGyroscopeFrame> Buffer => m_Buffer;

        public GyroscopeBuffer(IClock clock, float samplingIntervalMs = 50)
        {
            m_Buffer = new ConcurrentQueue<RawGyroscopeFrame>();
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
            CaptureGyroscopeAsync();
        }

        public void StopCapture()
        {
            if (!m_IsCapturing)
            {
                return;
            }
            m_IsCapturing = false;
        }

        private async void CaptureGyroscopeAsync()
        {
            while (m_IsCapturing)
            {
                await Awaitable.WaitForSecondsAsync(m_SamplingIntervalMs / 1000);
                AddToBuffer(m_Clock.UtcNow);
            }
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
            m_Buffer.Enqueue(newFrame);
        }

        public RawGyroscopeFrame TryAcquireLatestFrame()
        {
            return m_Buffer.LastOrDefault();
        }

        public IEnumerable<ARFrame> TakeARFrames()
        {
            ConcurrentQueue<RawGyroscopeFrame> oldFrames;
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
