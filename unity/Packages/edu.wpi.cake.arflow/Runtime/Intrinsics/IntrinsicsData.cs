using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf.WellKnownTypes;
using Unity.Collections;
using UnityEngine.XR.ARFoundation;
using UnityEngine;

namespace CakeLab.ARFlow
{
    using Clock;
    using Grpc.V1;
    using Utilities;
    using GrpcVector2Int = Grpc.V1.Vector2Int;
    using GrpcVector2 = Grpc.V1.Vector2;
    using GrpcXRCpuImage = Grpc.V1.XRCpuImage;
    using UnityVector2Int = UnityEngine.Vector2Int;
    using UnityXRCpuImage = UnityEngine.XR.ARSubsystems.XRCpuImage;

    public class IntrinsicsData
    {
        private ARCameraManager m_CameraManager;

        private IClock m_Clock;

        /// <summary>
        /// Typically this should be called at Awake of the MonoBehaviour that contains the ARCameraManager.
        /// </summary>
        /// <remarks>
        /// See <a href="https://github.com/Unity-Technologies/arfoundation-samples/blob/main/Assets/Scenes/FaceTracking/ToggleCameraFacingDirectionOnAction.cs">ToggleCameraFacingDirectionOnAction.cs</a> for an example of how to use this class.
        /// </remarks>
        public IntrinsicsData(ARCameraManager cameraManager, IClock clock)
        {
            m_CameraManager = cameraManager;
            m_Clock = clock;
        }

        public void GetIntrinsic(out Intrinsics intrinsic, out Timestamp timestamp)
        {
            m_CameraManager.TryGetIntrinsics(out var cameraIntrinsics);
            intrinsic = new Intrinsics
            {
                FocalLength = new GrpcVector2
                {
                    X = cameraIntrinsics.focalLength.x,
                    Y = cameraIntrinsics.focalLength.y
                },
                PrincipalPoint = new GrpcVector2
                {
                    X = cameraIntrinsics.principalPoint.x,
                    Y = cameraIntrinsics.principalPoint.y
                },
                Resolution = new GrpcVector2Int
                {
                    X = cameraIntrinsics.resolution.x,
                    Y = cameraIntrinsics.resolution.y
                }
            };
            timestamp = Timestamp.FromDateTime(m_Clock.UtcNow);
        }
    }
}
