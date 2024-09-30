using System;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Cysharp.Net.Http;
using Google.Protobuf;
using Grpc.Net.Client;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using System.Collections.Generic;
using static ARFlow.RegisterRequest.Types;
using UnityEngine.InputSystem;

namespace ARFlow
{
    /// <summary>
    /// This class represent the implementation for the client manager
    /// The client manager is an abstraction layer (for hopefully cleaner code) that collects and send data to the client.
    /// The Unity Scene only needs to input AR managers and modalities options.
    /// </summary>
    public class ARFlowClientManager
    {
        private ARFlowClient _client;
        private ARCameraManager _cameraManager;
        private AROcclusionManager _occlusionManager;
        private Vector2Int _sampleSize;
        private Dictionary<string, bool> _activatedDataModalities;

        private readonly Dictionary<string, bool> DEFAULT_MODALITIES = new Dictionary<string, bool>
        {
            ["CameraColor"] = false,
            ["CameraDepth"] = false,
            ["CameraTransform"] = false,
            ["CameraPointCloud"] = false,
            ["PlaneDetection"] = false,
            ["Gyroscope"] = false,
            ["Audio"] = false,
            ["Meshing"] = false
        };

        public static readonly List<string> MODALITIES = new List<string>
            { "CameraColor", "CameraDepth", "CameraTransform", "CameraPointCloud", "PlaneDetection", "Gyroscope", "Audio", "Meshing"};

        /// <summary>
        /// Initialize the client manager
        /// </summary>
        public ARFlowClientManager(
            ARCameraManager cameraManager = null,
            AROcclusionManager occlusionManager = null,
            ARPlaneManager planeManader = null
        )
        {
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
            _cameraManager = cameraManager;
            _occlusionManager = occlusionManager;
        }



        /// <summary>
        /// Connect to the server at an address, and with data modalities activated or not.
        /// </summary>
        /// <param name="address">Server address</param>
        /// <param name="activatedDataModalities">Dictionary of all data modalities, either activated or not</param>
        public void Connect(
            string address,
            Dictionary<string, bool> activatedDataModalities = null
        )
        {
            _client = new ARFlowClient(address);
            _activatedDataModalities = activatedDataModalities;
            if (activatedDataModalities == null)
                _activatedDataModalities = DEFAULT_MODALITIES;

            try
            {
                _cameraManager.TryGetIntrinsics(out var k);
                _cameraManager.TryAcquireLatestCpuImage(out var colorImage);
                _occlusionManager.TryAcquireEnvironmentDepthCpuImage(out var depthImage);

                _sampleSize = depthImage.dimensions;

                var requestData = new RegisterRequest()
                {
                    DeviceName = SystemInfo.deviceName,
                    CameraIntrinsics = new RegisterRequest.Types.CameraIntrinsics()
                    {
                        FocalLengthX = k.focalLength.x,
                        FocalLengthY = k.focalLength.y,
                        ResolutionX = k.resolution.x,
                        ResolutionY = k.resolution.y,
                        PrincipalPointX = k.principalPoint.x,
                        PrincipalPointY = k.principalPoint.y,
                    }

                };
                if (_activatedDataModalities["CameraColor"])
                {
                    var CameraColor = new RegisterRequest.Types.CameraColor()
                    {
                        Enabled = true,
                        DataType = "YCbCr420",
                        ResizeFactorX = depthImage.dimensions.x / (float)colorImage.dimensions.x,
                        ResizeFactorY = depthImage.dimensions.y / (float)colorImage.dimensions.y,
                    };
                    requestData.CameraColor = CameraColor;
                }
                if (_activatedDataModalities["CameraDepth"])
                {
                    var CameraDepth = new RegisterRequest.Types.CameraDepth()
                    {
                        Enabled = true,
#if UNITY_ANDROID
                        DataType = "u16", // f32 for iOS, u16 for Android
#endif
#if UNITY_IPHONE
                        DataType = "f32",
#endif
                        ConfidenceFilteringLevel = 0,
                        ResolutionX = depthImage.dimensions.x,
                        ResolutionY = depthImage.dimensions.y
                    };
                    requestData.CameraDepth = CameraDepth;
                }

                if (_activatedDataModalities["CameraTransform"])
                {
                    var CameraTransform = new RegisterRequest.Types.CameraTransform()
                    {
                        Enabled = true
                    };
                    requestData.CameraTransform = CameraTransform;
                }

                if (_activatedDataModalities["CameraPointCloud"])
                {
                    var CameraPointCloud = new RegisterRequest.Types.CameraPointCloud()
                    {
                        Enabled = true,
                        DepthUpscaleFactor = 1.0f,
                    };
                    requestData.CameraPointCloud = CameraPointCloud;
                };

                if (_activatedDataModalities["PlaneDetection"])
                {
                    var CameraPlaneDetection = new RegisterRequest.Types.CameraPlaneDetection()
                    {
                        Enabled = true
                    };
                    requestData.CameraPlaneDetection = CameraPlaneDetection;
                }

                if (_activatedDataModalities["Gyroscope"])
                {
                    var Gyroscope = new RegisterRequest.Types.Gyroscope()
                    {
                        Enabled = true
                    };
                    requestData.Gyroscope = Gyroscope;
                }

                if (_activatedDataModalities["Audio"])
                {
                    var Audio = new RegisterRequest.Types.Audio()
                    {
                        Enabled = true
                    };
                    requestData.Audio = Audio;
                }

                if (_activatedDataModalities["Meshing"])
                {
                    var Meshing = new RegisterRequest.Types.Meshing()
                    {
                        Enabled = true
                    };
                    requestData.Meshing = Meshing;
                }

                colorImage.Dispose();
                depthImage.Dispose();

                _client.Connect(requestData);

            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        /// <summary>
        /// Helper function to convert from unity data types to custom proto types
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        DataFrameRequest.Types.Vector3 unityVector3ToProto(Vector3 a)
        {
            return new DataFrameRequest.Types.Vector3 ()
            {
                X = a.x,
                Y = a.y,
                Z = a.z
            };
        }

        DataFrameRequest.Types.Quaternion unityQuaternionToProto(Quaternion a)
        {
            Debug.Log("hai");
            return new DataFrameRequest.Types.Quaternion ()
            {
                X = a.x,
                Y = a.y,
                Z = a.z,
                W = a.w
            };
        }



        /// <summary>
        /// Send a data of a frame to the server.
        /// </summary>
        /// <param name="frameData">Data of the frame. The typing of this is generated by Protobuf.</param>
        public string GetAndSendFrame()
        {
            var dataFrameRequest = new DataFrameRequest();

            if (_activatedDataModalities["CameraColor"])
            {
                var colorImage = new XRYCbCrColorImage(_cameraManager, _sampleSize);
                dataFrameRequest.Color = ByteString.CopyFrom(colorImage.Encode());

                colorImage.Dispose();
            }
            if (_activatedDataModalities["CameraDepth"])
            {
                var depthImage = new XRConfidenceFilteredDepthImage(_occlusionManager, 0);
                dataFrameRequest.Depth = ByteString.CopyFrom(depthImage.Encode());

                depthImage.Dispose();
            }

            if (_activatedDataModalities["CameraTransform"])
            {
                const int transformLength = 3 * 4 * sizeof(float);
                var m = Camera.main!.transform.localToWorldMatrix;
                var cameraTransformBytes = new byte[transformLength];

                Buffer.BlockCopy(new[]
                {
                    m.m00, m.m01, m.m02, m.m03,
                    m.m10, m.m11, m.m12, m.m13,
                    m.m20, m.m21, m.m22, m.m23
                }, 0, cameraTransformBytes, 0, transformLength);

                dataFrameRequest.Transform = ByteString.CopyFrom(cameraTransformBytes);
            }

            //if (_activatedDataModalities["CameraPlaneDetection"])
            //{
            //    var CameraPlaneDetection = new RegisterRequest.Types.CameraPlaneDetection()
            //    {
            //        Enabled = true
            //    };
            //    requestData.CameraPlaneDetection = CameraPlaneDetection;
            //}

            if (_activatedDataModalities["Gyroscope"])
            {
                dataFrameRequest.Gyroscope = new DataFrameRequest.Types.gyroscope_data();
                Quaternion attitude = AttitudeSensor.current.attitude.ReadValue();
                Vector3 rotation_rate = UnityEngine.InputSystem.Gyroscope.current.angularVelocity.ReadValue();
                Vector3 gravity = GravitySensor.current.gravity.ReadValue();
                Vector3 acceleration = Accelerometer.current.acceleration.ReadValue();

                dataFrameRequest.Gyroscope.Attitude = unityQuaternionToProto(attitude);
                dataFrameRequest.Gyroscope.RotationRate = unityVector3ToProto(rotation_rate);
                dataFrameRequest.Gyroscope.Gravity = unityVector3ToProto(gravity);
                dataFrameRequest.Gyroscope.Acceleration = unityVector3ToProto(acceleration);
            }

            //if (_activatedDataModalities["Audio"])
            //{
            //    var Audio = new RegisterRequest.Types.Audio()
            //    {
            //        Enabled = true
            //    };
            //    requestData.Audio = Audio;
            //}

            //if (_activatedDataModalities["Meshing"])
            //{
            //    var Meshing = new RegisterRequest.Types.Meshing()
            //    {
            //        Enabled = true
            //    };
            //    requestData.Meshing = Meshing;
            //}



            string serverMessage = _client.SendFrame(dataFrameRequest);
            return serverMessage;
        }
    }
}

