using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf.WellKnownTypes;
using Unity.XR.CoreUtils.Collections;
using UnityEngine.XR.ARFoundation;

namespace CakeLab.ARFlow.DataBuffers
{
    using Grpc.V1;
    using GrpcARTrackable = Grpc.V1.ARTrackable;
    using GrpcARPlane = Grpc.V1.ARPlane;
    using GrpcPose = Grpc.V1.Pose;
    using GrpcVector2 = Grpc.V1.Vector2;
    using GrpcVector3 = Grpc.V1.Vector3;
    using GrpcQuaternion = Grpc.V1.Quaternion;
    using UnityPose = UnityEngine.Pose;
    using UnityARTrackableId = UnityEngine.XR.ARSubsystems.TrackableId;
    using UnityARTrackingState = UnityEngine.XR.ARSubsystems.TrackingState;
    using UnityARPlane = UnityEngine.XR.ARFoundation.ARPlane;
    using UnityVector2 = UnityEngine.Vector2;
    using UnityVector3 = UnityEngine.Vector3;

    public enum PlaneDetectionState
    {
        Unspecified,
        Added,
        Updated,
        Removed,
    }

    public struct RawPlaneDetectionFrame
    {
        public PlaneDetectionState State;
        public DateTime DeviceTimestamp;
        public UnityPose Pose;
        public UnityARTrackableId TrackableId;
        public UnityARTrackingState TrackingState;
        public UnityVector2[] Boundary;
        public UnityVector3 Center;
        public UnityVector3 Normal;
        public UnityVector2 Size;
        public UnityARTrackableId SubsumedById;

        public static explicit operator Grpc.V1.PlaneDetectionFrame(RawPlaneDetectionFrame rawFrame)
        {
            var trackingState = rawFrame.TrackingState switch
            {
                UnityARTrackingState.Limited => ARTrackable.Types.TrackingState.Limited,
                UnityARTrackingState.None => ARTrackable.Types.TrackingState.None,
                UnityARTrackingState.Tracking => ARTrackable.Types.TrackingState.Tracking,
                _ => ARTrackable.Types.TrackingState.Unspecified,
            };
            var planeGrpc = new GrpcARPlane
            {
                Trackable = new GrpcARTrackable
                {
                    Pose = new GrpcPose
                    {
                        Forward = new GrpcVector3 { X = rawFrame.Pose.forward.x, Y = rawFrame.Pose.forward.y, Z = rawFrame.Pose.forward.z },
                        Right = new GrpcVector3 { X = rawFrame.Pose.right.x, Y = rawFrame.Pose.right.y, Z = rawFrame.Pose.right.z },
                        Rotation = new GrpcQuaternion { X = rawFrame.Pose.rotation.x, Y = rawFrame.Pose.rotation.y, Z = rawFrame.Pose.rotation.z },
                        Up = new GrpcVector3 { X = rawFrame.Pose.up.x, Y = rawFrame.Pose.up.y, Z = rawFrame.Pose.up.z },
                    },
                    TrackableId = new GrpcARTrackable.Types.TrackableId
                    {
                        SubId1 = rawFrame.TrackableId.subId1,
                        SubId2 = rawFrame.TrackableId.subId2,
                    },
                    TrackingState = trackingState,
                },
                // We null check here because in Added and Update states, Boundary, Center, Normal, and Size are not null. In Removed state, they are, but SubsumedById can have a value (still can be null though).
                Center = rawFrame.Center == null ? null : new GrpcVector3
                {
                    X = rawFrame.Center.x,
                    Y = rawFrame.Center.y,
                    Z = rawFrame.Center.z,
                },
                Normal = rawFrame.Normal == null ? null : new GrpcVector3
                {
                    X = rawFrame.Normal.x,
                    Y = rawFrame.Normal.y,
                    Z = rawFrame.Normal.z,
                },
                Size = rawFrame.Size == null ? null : new GrpcVector2 { X = rawFrame.Size.x, Y = rawFrame.Size.y },
                SubsumedById = rawFrame.SubsumedById == null ? null : new GrpcARTrackable.Types.TrackableId
                {
                    SubId1 = rawFrame.SubsumedById.subId1,
                    SubId2 = rawFrame.SubsumedById.subId2,
                },
            };
            if (rawFrame.Boundary != null)
            {
                planeGrpc.Boundary.AddRange(
                    rawFrame.Boundary.Select(v => new GrpcVector2 { X = v.x, Y = v.y })
                );
            }
            var planeDetectionFrameGrpc = new Grpc.V1.PlaneDetectionFrame
            {
                State = (PlaneDetectionFrame.Types.State)rawFrame.State,
                DeviceTimestamp = Timestamp.FromDateTime(rawFrame.DeviceTimestamp),
                Plane = planeGrpc,
            };
            return planeDetectionFrameGrpc;
        }

        public static explicit operator Grpc.V1.ARFrame(RawPlaneDetectionFrame rawFrame)
        {
            var arFrame = new Grpc.V1.ARFrame { PlaneDetectionFrame = (Grpc.V1.PlaneDetectionFrame)rawFrame };
            return arFrame;
        }
    }

    public class PlaneDetectionBuffer : IDataBuffer<RawPlaneDetectionFrame>
    {
        ARPlaneManager m_PlaneManager;

        public ARPlaneManager PlaneManager
        {
            get => m_PlaneManager;
            set => m_PlaneManager = value;
        }

        private readonly List<RawPlaneDetectionFrame> m_Buffer;

        public IReadOnlyList<RawPlaneDetectionFrame> Buffer => m_Buffer;

        public PlaneDetectionBuffer(int initialBufferSize, ARPlaneManager planeManager)
        {
            m_Buffer = new List<RawPlaneDetectionFrame>(initialBufferSize);
            m_PlaneManager = planeManager;
        }

        public void StartCapture()
        {
            m_PlaneManager.trackablesChanged.AddListener(OnPlaneDetectionChanged);
        }

        public void StopCapture()
        {
            m_PlaneManager.trackablesChanged.RemoveListener(OnPlaneDetectionChanged);
        }

        private void OnPlaneDetectionChanged(ARTrackablesChangedEventArgs<UnityARPlane> changes)
        {
            var deviceTime = DateTime.UtcNow;
            AddToBuffer(changes.added, deviceTime, PlaneDetectionState.Added);
            AddToBuffer(changes.updated, deviceTime, PlaneDetectionState.Updated);
            AddToBuffer(changes.removed, deviceTime);
        }

        private void AddToBuffer(ReadOnlyList<UnityARPlane> planes, DateTime deviceTimestampAtCapture, PlaneDetectionState state)
        {
            m_Buffer.AddRange(planes.Select(plane => new RawPlaneDetectionFrame
            {
                State = state,
                Pose = plane.pose,
                TrackableId = plane.trackableId,
                TrackingState = plane.trackingState,
                DeviceTimestamp = deviceTimestampAtCapture,
                Boundary = plane.boundary.ToArray(),
                Center = plane.center,
                Normal = plane.normal,
                Size = plane.size,
            }));
        }

        private void AddToBuffer(ReadOnlyList<KeyValuePair<UnityARTrackableId, UnityARPlane>> planes, DateTime deviceTimestampAtCapture)
        {
            m_Buffer.AddRange(planes.Select(plane => new RawPlaneDetectionFrame
            {
                State = PlaneDetectionState.Removed,
                Pose = plane.Value.pose,
                TrackableId = plane.Key,
                TrackingState = plane.Value.trackingState,
                DeviceTimestamp = deviceTimestampAtCapture,
                SubsumedById = plane.Value.subsumedBy.trackableId,
            }));
        }

        public void ClearBuffer()
        {
            m_Buffer.Clear();
        }

        public RawPlaneDetectionFrame TryAcquireLatestFrame()
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
