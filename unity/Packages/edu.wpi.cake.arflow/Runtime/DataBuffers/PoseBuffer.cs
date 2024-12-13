using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf.WellKnownTypes;
using Unity.Collections;
using UnityEngine.XR.ARFoundation;
using UnityEngine;

namespace CakeLab.ARFlow.DataBuffers
{
    using Clock;
    using Grpc.V1;
    using Utilities;
    using GrpcVector2Int = Grpc.V1.Vector2Int;
    using GrpcXRCpuImage = Grpc.V1.XRCpuImage;
    using UnityVector2Int = UnityEngine.Vector2Int;
    using UnityXRCpuImage = UnityEngine.XR.ARSubsystems.XRCpuImage;

    /// <remarks>
    /// We don't keep a buffer of XRCpuImage here because these are native resources that need to be disposed.
    /// </remarks>
    public struct RawPoseFrame
    {
        public DateTime DeviceTimestamp;
        public UnityEngine.Vector3 forward;
        public UnityEngine.Vector3 position;
        public UnityEngine.Vector3 right;
        public UnityEngine.Quaternion rotation;
        public UnityEngine.Vector3 up;

        public static explicit operator Grpc.V1.PoseFrame(RawPoseFrame rawFrame)
        {
            var poseFrameGrpc = new Grpc.V1.PoseFrame
            {
                DeviceTimestamp = Timestamp.FromDateTime(rawFrame.DeviceTimestamp),
                Pose = new Grpc.V1.Pose
                {
                    Forward = new Grpc.V1.Vector3
                    {
                        X = rawFrame.forward.x,
                        Y = rawFrame.forward.y,
                        Z = rawFrame.forward.z,
                    },
                    Position = new Grpc.V1.Vector3
                    {
                        X = rawFrame.position.x,
                        Y = rawFrame.position.y,
                        Z = rawFrame.position.z,
                    },
                    Right = new Grpc.V1.Vector3
                    {
                        X = rawFrame.right.x,
                        Y = rawFrame.right.y,
                        Z = rawFrame.right.z,
                    },
                    Rotation = new Grpc.V1.Quaternion
                    {
                        X = rawFrame.rotation.x,
                        Y = rawFrame.rotation.y,
                        Z = rawFrame.rotation.z,
                        W = rawFrame.rotation.w,
                    },
                    Up = new Grpc.V1.Vector3
                    {
                        X = rawFrame.up.x,
                        Y = rawFrame.up.y,
                        Z = rawFrame.up.z,
                    },
                },
            };

            return poseFrameGrpc;
        }

        public static explicit operator Grpc.V1.ARFrame(RawPoseFrame rawFrame)
        {
            var arFrame = new Grpc.V1.ARFrame { PoseFrame = (Grpc.V1.PoseFrame)rawFrame };
            return arFrame;
        }
    }

    public class PoseBuffer : IARFrameBuffer<RawPoseFrame>
    {
        Transform m_ObjTransform;

        /// <summary>
        /// The ARCameraManager which will produce frame events.
        /// </summary>
        public Transform ObjTransform
        {
            get => m_ObjTransform;
            set => m_ObjTransform = value;
        }

        private float m_SamplingIntervalMs;
        private bool m_IsCapturing;

        IClock m_Clock;

        public IClock Clock
        {
            get => m_Clock;
            set => m_Clock = value;
        }

        private readonly List<RawPoseFrame> m_Buffer;

        public IReadOnlyList<RawPoseFrame> Buffer => m_Buffer;

        /// <summary>
        /// Typically this should be called at Awake of the MonoBehaviour that contains the ARCameraManager.
        /// </summary>
        /// <remarks>
        /// See <a href="https://github.com/Unity-Technologies/arfoundation-samples/blob/main/Assets/Scenes/FaceTracking/ToggleCameraFacingDirectionOnAction.cs">ToggleCameraFacingDirectionOnAction.cs</a> for an example of how to use this class.
        /// </remarks>
        public PoseBuffer(int initialBufferSize, Transform objTransform, IClock clock, float samplingIntervalMs = 50)
        {
            m_Buffer = new List<RawPoseFrame>(initialBufferSize);
            m_ObjTransform = objTransform;
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
            CaptureAsync();
        }

        public void StopCapture()
        {
            if (!m_IsCapturing)
            {
                return;
            }
            m_IsCapturing = false;
        }

        private async void CaptureAsync()
        {
            while (m_IsCapturing)
            {
                await Awaitable.WaitForSecondsAsync(m_SamplingIntervalMs / 1000);
                AddToBuffer(m_Clock.UtcNow);
            }
        }

        private void AddToBuffer(DateTime deviceTimestampAtCapture)
        {
            var pose = new UnityEngine.Pose(m_ObjTransform.position, m_ObjTransform.rotation);
            var newFrame = new RawPoseFrame
            {
                DeviceTimestamp = deviceTimestampAtCapture,
                forward = pose.forward,
                position = pose.position,
                right = pose.right,
                rotation = pose.rotation,
                up = pose.up,
            };
            m_Buffer.Add(newFrame);
        }

        public void ClearBuffer()
        {
            m_Buffer.Clear();
        }

        public RawPoseFrame TryAcquireLatestFrame()
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
