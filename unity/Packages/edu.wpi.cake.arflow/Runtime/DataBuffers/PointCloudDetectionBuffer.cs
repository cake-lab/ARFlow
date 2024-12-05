using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf.WellKnownTypes;
using Unity.XR.CoreUtils.Collections;
using UnityEngine.XR.ARFoundation;

namespace CakeLab.ARFlow.DataBuffers
{
    using Grpc.V1;
    using Utilities;
    using GrpcARPointCloud = Grpc.V1.ARPointCloud;
    using GrpcARTrackable = Grpc.V1.ARTrackable;
    using GrpcPose = Grpc.V1.Pose;
    using GrpcQuaternion = Grpc.V1.Quaternion;
    using GrpcVector3 = Grpc.V1.Vector3;
    using UnityARPointCloud = UnityEngine.XR.ARFoundation.ARPointCloud;
    using UnityARTrackableId = UnityEngine.XR.ARSubsystems.TrackableId;
    using UnityARTrackingState = UnityEngine.XR.ARSubsystems.TrackingState;
    using UnityPose = UnityEngine.Pose;
    using UnityVector3 = UnityEngine.Vector3;

    public enum PointCloudDetectionState
    {
        Unspecified,
        Added,
        Updated,
        Removed,
    }

    public struct RawPointCloudDetectionFrame
    {
        public UnityPose Pose;
        public UnityARTrackableId TrackableId;
        public UnityARTrackingState TrackingState;
        public PointCloudDetectionState State;
        public DateTime DeviceTimestamp;
        public float[] ConfidenceValues;
        public ulong[] Identifiers;
        public UnityVector3[] Positions;

        public static explicit operator Grpc.V1.PointCloudDetectionFrame(
            RawPointCloudDetectionFrame rawFrame
        )
        {
            var trackingState = rawFrame.TrackingState switch
            {
                UnityARTrackingState.Limited => ARTrackable.Types.TrackingState.Limited,
                UnityARTrackingState.None => ARTrackable.Types.TrackingState.None,
                UnityARTrackingState.Tracking => ARTrackable.Types.TrackingState.Tracking,
                _ => ARTrackable.Types.TrackingState.Unspecified,
            };
            var pointCloudGrpc = new GrpcARPointCloud
            {
                Trackable = new GrpcARTrackable
                {
                    Pose = new GrpcPose
                    {
                        Forward = new GrpcVector3
                        {
                            X = rawFrame.Pose.forward.x,
                            Y = rawFrame.Pose.forward.y,
                            Z = rawFrame.Pose.forward.z,
                        },
                        Right = new GrpcVector3
                        {
                            X = rawFrame.Pose.right.x,
                            Y = rawFrame.Pose.right.y,
                            Z = rawFrame.Pose.right.z,
                        },
                        Rotation = new GrpcQuaternion
                        {
                            X = rawFrame.Pose.rotation.x,
                            Y = rawFrame.Pose.rotation.y,
                            Z = rawFrame.Pose.rotation.z,
                        },
                        Up = new GrpcVector3
                        {
                            X = rawFrame.Pose.up.x,
                            Y = rawFrame.Pose.up.y,
                            Z = rawFrame.Pose.up.z,
                        },
                    },
                    TrackableId = new GrpcARTrackable.Types.TrackableId
                    {
                        SubId1 = rawFrame.TrackableId.subId1,
                        SubId2 = rawFrame.TrackableId.subId2,
                    },
                    TrackingState = trackingState,
                },
            };
            pointCloudGrpc.ConfidenceValues.AddRange(rawFrame.ConfidenceValues);
            pointCloudGrpc.Identifiers.AddRange(rawFrame.Identifiers);
            pointCloudGrpc.Positions.AddRange(
                rawFrame.Positions.Select(v => new GrpcVector3
                {
                    X = v.x,
                    Y = v.y,
                    Z = v.z,
                })
            );
            var pointCloudDetectionFrameGrpc = new Grpc.V1.PointCloudDetectionFrame
            {
                State = (PointCloudDetectionFrame.Types.State)rawFrame.State,
                DeviceTimestamp = Timestamp.FromDateTime(rawFrame.DeviceTimestamp),
                PointCloud = pointCloudGrpc,
            };
            return pointCloudDetectionFrameGrpc;
        }

        public static explicit operator Grpc.V1.ARFrame(RawPointCloudDetectionFrame rawFrame)
        {
            var arFrame = new Grpc.V1.ARFrame
            {
                PointCloudDetectionFrame = (Grpc.V1.PointCloudDetectionFrame)rawFrame,
            };
            return arFrame;
        }
    }

    public class PointCloudDetectionBuffer : IDataBuffer<RawPointCloudDetectionFrame>
    {
        ARPointCloudManager m_PointCloudManager;

        public ARPointCloudManager PointCloudManager
        {
            get => m_PointCloudManager;
            set => m_PointCloudManager = value;
        }

        NtpDateTimeManager m_NtpManager;

        public NtpDateTimeManager NtpManager
        {
            get => m_NtpManager;
            set => m_NtpManager = value;
        }

        private readonly List<RawPointCloudDetectionFrame> m_Buffer;

        public IReadOnlyList<RawPointCloudDetectionFrame> Buffer => m_Buffer;

        public PointCloudDetectionBuffer(
            int initialBufferSize,
            ARPointCloudManager pointCloudManager,
            NtpDateTimeManager ntpManager
        )
        {
            m_Buffer = new List<RawPointCloudDetectionFrame>(initialBufferSize);
            m_PointCloudManager = pointCloudManager;
            m_NtpManager = ntpManager;
        }

        public void StartCapture()
        {
            m_PointCloudManager.trackablesChanged.AddListener(OnPointCloudDetectionChanged);
        }

        public void StopCapture()
        {
            m_PointCloudManager.trackablesChanged.RemoveListener(OnPointCloudDetectionChanged);
        }

        private void OnPointCloudDetectionChanged(
            ARTrackablesChangedEventArgs<UnityARPointCloud> changes
        )
        {
            var deviceTime = m_NtpManager.UtcNow;
            AddToBuffer(changes.added, deviceTime, PointCloudDetectionState.Added);
            AddToBuffer(changes.updated, deviceTime, PointCloudDetectionState.Updated);
            AddToBuffer(changes.removed, deviceTime);
        }

        private void AddToBuffer(
            ReadOnlyList<UnityARPointCloud> pointClouds,
            DateTime deviceTimestampAtCapture,
            PointCloudDetectionState state
        )
        {
            m_Buffer.AddRange(
                pointClouds.Select(pointCloud => new RawPointCloudDetectionFrame
                {
                    State = state,
                    Pose = pointCloud.pose,
                    TrackableId = pointCloud.trackableId,
                    TrackingState = pointCloud.trackingState,
                    DeviceTimestamp = deviceTimestampAtCapture,
                    ConfidenceValues =
                        pointCloud.confidenceValues?.ToArray() ?? Array.Empty<float>(),
                    Identifiers = pointCloud.identifiers?.ToArray() ?? Array.Empty<ulong>(),
                    Positions = pointCloud.positions?.ToArray() ?? Array.Empty<UnityVector3>(),
                })
            );
        }

        private void AddToBuffer(
            ReadOnlyList<KeyValuePair<UnityARTrackableId, UnityARPointCloud>> pointClouds,
            DateTime deviceTimestampAtCapture
        )
        {
            m_Buffer.AddRange(
                pointClouds.Select(pointCloud => new RawPointCloudDetectionFrame
                {
                    State = PointCloudDetectionState.Removed,
                    Pose = pointCloud.Value.pose,
                    TrackableId = pointCloud.Key,
                    TrackingState = pointCloud.Value.trackingState,
                    DeviceTimestamp = deviceTimestampAtCapture,
                    ConfidenceValues = Array.Empty<float>(),
                    Identifiers = Array.Empty<ulong>(),
                    Positions = Array.Empty<UnityVector3>(),
                })
            );
        }

        public void ClearBuffer()
        {
            m_Buffer.Clear();
        }

        public RawPointCloudDetectionFrame TryAcquireLatestFrame()
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
