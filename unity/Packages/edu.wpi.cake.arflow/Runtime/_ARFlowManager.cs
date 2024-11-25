// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Threading.Tasks;
// using CakeLab.ARFlow.Utilities;
// using CakeLab.ARFlow.Grpc.V1;
// using Google.Protobuf;
// using Google.Protobuf.WellKnownTypes;
// using Unity.Collections;
// using UnityEngine;
// using UnityEngine.Android;
// using UnityEngine.InputSystem;
// using UnityEngine.XR.ARFoundation;
// using UnityEngine.XR.ARSubsystems;
// using System.Collections;
// using CakeLab.ARFlow.Grpc;

// namespace CakeLab.ARFlow.DataBuffers
// {
//     public class _ARFlowManager
//     {

//         /// <summary>
//         /// Controls the lifecycle of all AR features by enabling or disabling AR on the target platform.
//         /// </summary>
//         /// <remarks>
//         /// At most one AR session per scene.
//         /// </remarks>
//         [SerializeField]
//         ARSession arSession;

//         [SerializeField]
//         [Tooltip("The ARCameraManager which will produce frame events.")]
//         ARCameraManager cameraManager;

//         /// <summary>
//         /// Get or set the <see cref"ARCameraManager"/>.
//         /// </summary>
//         public ARCameraManager CameraManager
//         {
//             get => cameraManager;
//             set => cameraManager = value;
//         }

//         [SerializeField]
//         [Tooltip("The AROcclusionManager which will produce depth textures.")]
//         AROcclusionManager occlusionManager;

//         /// <summary>
//         /// Get or set the <see cref"AROcclusionManager"/>.
//         /// </summary>
//         public AROcclusionManager OcclusionManager
//         {
//             get => occlusionManager;
//             set => occlusionManager = value;
//         }

//         [SerializeField]
//         [Tooltip("The ARMeshManager which will produce mesh data.")]
//         ARMeshManager meshManager;

//         /// <summary>
//         /// Get or set the <see cref"ARMeshManager"/>.
//         /// </summary>
//         public ARMeshManager MeshManager
//         {
//             get => meshManager;
//             set => meshManager = value;
//         }

//         [SerializeField]
//         [Tooltip("The ARPlaneManager which will produce plane data.")]
//         ARPlaneManager planeManager;

//         /// <summary>
//         /// Get or set the <see cref"ARPlaneManager"/>.
//         /// </summary>
//         public ARPlaneManager PlaneManager
//         {
//             get => planeManager;
//             set => planeManager = value;
//         }

//         [SerializeField]
//         [Tooltip("The AudioManager which will produce audio data.")]
//         AudioManager audioManager;

//         /// <summary>
//         /// Get or set the <see cref"AudioManager"/>.
//         /// </summary>
//         public AudioManager ARFlowAudioManager
//         {
//             get => audioManager;
//             set => audioManager = value;
//         }
//         private MeshEncoder _meshEncoder;
//         private Vector2Int _sampleSize;
//         private Dictionary<string, bool> _activatedDataModalities;

//         private Task oldTask = null;

//         private bool _isStreaming = false;

//         private string _currentUid = null;

//         IEnumerator Start()
//         {
//             yield return CheckARSupport();
//             StartARSession();
//             CheckCameraSupport();
//         }

//         /// <summary>
//         /// Check if the platform supports AR.
//         /// </summary>
//         /// <remarks>
//         /// See <a href="https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@6.0/manual/features/session.html#check-for-device-support">AR Device Support</a> for more details
//         /// </remarks>
//         private IEnumerator CheckARSupport()
//         {
//             if (ARSession.state == ARSessionState.None || ARSession.state == ARSessionState.CheckingAvailability)
//             {
//                 yield return ARSession.CheckAvailability();
//             }
//         }

//         /// <summary>
//         /// Start the AR session if the platform supports AR. Otherwise, start a fallback experience.
//         /// </summary>
//         private void StartARSession()
//         {
//             if (ARSession.state == ARSessionState.Unsupported)
//             {
//                 // TODO: Start some fallback experience for unsupported devices
//                 InternalDebug.Log("This device does not support AR");
//                 return;
//             }
//             // Start the AR session
//             arSession.enabled = true;
//         }

//         /// <summary>
//         /// Check and set the camera support status. This assumes that the app has already initialized XR.
//         /// </summary>
//         /// <remarks>
//         /// See <a href="https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@6.0/manual/features/camera/platform-support.html#check-for-camera-support">Camera Platform Support</a> for more details.
//         /// </remarks>
//         private void CheckCameraSupport()
//         {
//             InternalDebug.Assert(arSession != null, "ARSession must be not null");
//             var loader = LoaderUtility.GetActiveLoader();
//             if (loader == null || loader.GetLoadedSubsystem<XRCameraSubsystem>() == null)
//             {
//                 InternalDebug.Log("This device does not support the camera subsystem");
//                 return;
//             }
//         }

//         private readonly Dictionary<string, bool> DEFAULT_MODALITIES =
//             new()
//             {
//                 ["CameraColor"] = false,
//                 ["CameraDepth"] = false,
//                 ["CameraTransform"] = false,
//                 ["CameraPointCloud"] = false,
//                 ["PlaneDetection"] = false,
//                 ["Gyroscope"] = false,
//                 ["Audio"] = false,
//                 ["Meshing"] = false,
//             };

//         public static readonly List<string> MODALITIES =
//             new()
//             {
//                 "CameraColor",
//                 "CameraDepth",
//                 "CameraTransform",
//                 "CameraPointCloud",
//                 "PlaneDetection",
//                 "Gyroscope",
//                 "Audio",
//                 "Meshing",
//             };

//         public ARFlowManager(
//             ARCameraManager cameraManager = null,
//             AROcclusionManager occlusionManager = null,
//             ARPlaneManager planeManager = null,
//             ARMeshManager meshManager = null
//         )
//         {
//             if (UnityEngine.InputSystem.Gyroscope.current != null)
//             {
//                 InputSystem.EnableDevice(UnityEngine.InputSystem.Gyroscope.current);
//             }
//             if (AttitudeSensor.current != null)
//             {
//                 InputSystem.EnableDevice(AttitudeSensor.current);
//             }
//             if (Accelerometer.current != null)
//             {
//                 InputSystem.EnableDevice(Accelerometer.current);
//             }
//             if (GravitySensor.current != null)
//             {
//                 InputSystem.EnableDevice(GravitySensor.current);
//             }
//             cameraManager = cameraManager;
//             _occlusionManager = occlusionManager;

//             PlaneManager = planeManager;
//             meshManager = meshManager;

//             ARFlowAudioManager = new AudioStreaming();
//             _meshEncoder = new MeshEncoder();

// #if UNITY_ANDROID
//             if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
//             {
//                 Permission.RequestUserPermission(Permission.Microphone);
//             }
// #endif
// #if UNITY_IOS
//             if (Application.HasUserAuthorization(UserAuthorization.Microphone))
//             {
//                 Application.RequestUserAuthorization(UserAuthorization.Microphone);
//             }
// #endif
//         }

//         /// <summary>
//         /// A Task wrapper for the connect method, to avoid blocking the main thread.
//         /// <para /> Only the connect method is sent to another thread.
//         /// The methods to collect client configurations is required to be in the main thread.
//         ///
//         /// <para /> The method will spawn a new task, so the usage of this is only calling "ConnectTask(args)"
//         /// </summary>
//         /// <param name="address">Server address</param>
//         /// <param name="activatedDataModalities">Dictionary of all data modalities, either activated or not</param>
//         public Task ConnectTask(
//             string address,
//             Dictionary<string, bool> activatedDataModalities = null,
//             Action<Task> taskFinishedHook = null
//         )
//         {
//             ResetState();
//             _grpc_client = new GrpcClient(address);

//             _activatedDataModalities = activatedDataModalities;
//             if (activatedDataModalities == null)
//                 _activatedDataModalities = DEFAULT_MODALITIES;

//             // To avoid old method calls to log message
//             oldTask?.ContinueWith(t => { });

//             var requestData = GetClientConfiguration();
//             var task = Task.Run(() =>
//             {
//                 _grpc_client.Connect(requestData);
//                 _currentUid = _grpc_client.sessionId;
//             });
//             if (taskFinishedHook is not null)
//                 task.ContinueWith(taskFinishedHook);

//             oldTask = task;

//             return task;
//         }

//         /// <summary>
//         /// Connect to the server at an address, and with data modalities activated or not.
//         /// </summary>
//         /// <param name="address">Server address</param>
//         /// <param name="activatedDataModalities">Dictionary of all data modalities, either activated or not</param>
//         public void Connect(string address, Dictionary<string, bool> activatedDataModalities = null)
//         {
//             ResetState();
//             _grpc_client = new GrpcClient(address);

//             _activatedDataModalities = activatedDataModalities;
//             if (activatedDataModalities == null)
//                 _activatedDataModalities = DEFAULT_MODALITIES;

//             try
//             {
//                 var requestData = GetClientConfiguration();
//                 _grpc_client.Connect(requestData);

//                 _currentUid = _grpc_client.sessionId;
//             }
//             catch (Exception e)
//             {
//                 InternalDebug.Log(e.Message);
//             }
//         }

//         private void ResetState()
//         {
//             if (_isStreaming)
//             {
//                 StopDataStreaming();
//             }
//         }

//         private RegisterClientRequest GetClientConfiguration()
//         {
//             cameraManager.TryGetIntrinsics(out var k);
//             cameraManager.TryAcquireLatestCpuImage(out var colorImage);
//             occlusionManager.TryAcquireEnvironmentDepthCpuImage(out var depthImage);

//             _sampleSize = depthImage.dimensions;

//             var requestData = new RegisterClientRequest()
//             {
//                 DeviceName = SystemInfo.deviceName,
//                 CameraIntrinsics = new RegisterClientRequest.Types.CameraIntrinsics()
//                 {
//                     FocalLengthX = k.focalLength.x,
//                     FocalLengthY = k.focalLength.y,
//                     ResolutionX = k.resolution.x,
//                     ResolutionY = k.resolution.y,
//                     PrincipalPointX = k.principalPoint.x,
//                     PrincipalPointY = k.principalPoint.y,
//                 },
//             };
//             if (_currentUid != null)
//             {
//                 requestData.InitUid = _currentUid;
//             }
//             if (_activatedDataModalities["CameraColor"])
//             {
//                 var CameraColor = new RegisterClientRequest.Types.CameraColor()
//                 {
//                     Enabled = true,
//                     DataType = "YCbCr420",
//                     ResizeFactorX = depthImage.dimensions.x / (float)colorImage.dimensions.x,
//                     ResizeFactorY = depthImage.dimensions.y / (float)colorImage.dimensions.y,
//                 };
//                 requestData.CameraColor = CameraColor;
//             }
//             if (_activatedDataModalities["CameraDepth"])
//             {
//                 var CameraDepth = new RegisterClientRequest.Types.CameraDepth()
//                 {
//                     Enabled = true,
// #if UNITY_ANDROID
//                     DataType = "u16", // f32 for iOS, u16 for Android
// #endif
// #if UNITY_IOS
//                     DataType = "f32",
// #endif
//                     ConfidenceFilteringLevel = 0,
//                     ResolutionX = depthImage.dimensions.x,
//                     ResolutionY = depthImage.dimensions.y,
//                 };
//                 requestData.CameraDepth = CameraDepth;
//             }

//             if (_activatedDataModalities["CameraTransform"])
//             {
//                 var CameraTransform = new RegisterClientRequest.Types.CameraTransform()
//                 {
//                     Enabled = true,
//                 };
//                 requestData.CameraTransform = CameraTransform;
//             }

//             if (_activatedDataModalities["CameraPointCloud"])
//             {
//                 var CameraPointCloud = new RegisterClientRequest.Types.CameraPointCloud()
//                 {
//                     Enabled = true,
//                     DepthUpscaleFactor = 1.0f,
//                 };
//                 requestData.CameraPointCloud = CameraPointCloud;
//             }
//             ;

//             if (_activatedDataModalities["PlaneDetection"])
//             {
//                 var CameraPlaneDetection = new RegisterClientRequest.Types.CameraPlaneDetection()
//                 {
//                     Enabled = true,
//                 };
//                 requestData.CameraPlaneDetection = CameraPlaneDetection;
//             }

//             if (_activatedDataModalities["Gyroscope"])
//             {
//                 var Gyroscope = new RegisterClientRequest.Types.Gyroscope() { Enabled = true };
//                 requestData.Gyroscope = Gyroscope;
//             }

//             if (_activatedDataModalities["Audio"])
//             {
//                 var Audio = new RegisterClientRequest.Types.Audio() { Enabled = true };
//                 requestData.Audio = Audio;
//             }

//             if (_activatedDataModalities["Meshing"])
//             {
//                 var Meshing = new RegisterClientRequest.Types.Meshing() { Enabled = true };
//                 requestData.Meshing = Meshing;
//             }

//             colorImage.Dispose();
//             depthImage.Dispose();

//             return requestData;
//         }

//         /// <summary>
//         /// Join multiplayer session async
//         /// </summary>
//         /// <param name="sessionId">Session ID to join</param>
//         /// <param name="taskFinishedHook">Hook to run after task completes</param>
//         /// <returns></returns>
//         public Task<string> JoinSessionTask(
//             string sessionId,
//             Dictionary<string, bool> activatedDataModalities = null,
//             Action<Task> taskFinishedHook = null
//         )
//         {
//             _activatedDataModalities = activatedDataModalities;
//             if (activatedDataModalities == null)
//                 _activatedDataModalities = DEFAULT_MODALITIES;

//             JoinSessionRequest joinSessionRequest = new JoinSessionRequest();
//             joinSessionRequest.SessionUid = sessionId;
//             joinSessionRequest.ClientConfig = GetClientConfiguration();
//             var task = Task.Run(() =>
//             {
//                 var res = _grpc_client.JoinSession(joinSessionRequest);
//                 _currentUid = sessionId;
//                 return res;
//             });

//             //var task = Task.Run(() =>
//             //     _client.JoinSession(joinSessionRequest));
//             if (taskFinishedHook is not null)
//                 task.ContinueWith(taskFinishedHook);

//             oldTask = task;
//             return task;
//         }

//         /// <summary>
//         /// Join multiplayer session
//         /// </summary>
//         /// <param name="sessionId">Session ID to join</param>
//         /// <returns></returns>
//         public string JoinSession(
//             string sessionId,
//             Dictionary<string, bool> activatedDataModalities = null
//         )
//         {
//             _activatedDataModalities = activatedDataModalities;
//             if (activatedDataModalities == null)
//                 _activatedDataModalities = DEFAULT_MODALITIES;

//             JoinSessionRequest joinSessionRequest = new JoinSessionRequest();
//             joinSessionRequest.SessionUid = sessionId;
//             joinSessionRequest.ClientConfig = GetClientConfiguration();
//             var res = _grpc_client.JoinSession(joinSessionRequest);

//             _currentUid = sessionId;

//             return res;
//         }

//         /// <summary>
//         /// Helper function to convert from unity data types to custom proto types
//         /// </summary>
//         /// <param name="v"></param>
//         /// <returns></returns>
//         ProcessFrameRequest.Types.Vector3 UnityVector3ToProto(Vector3 a)
//         {
//             return new ProcessFrameRequest.Types.Vector3()
//             {
//                 X = a.x,
//                 Y = a.y,
//                 Z = a.z,
//             };
//         }

//         ProcessFrameRequest.Types.Vector2 UnityVector2ToProto(Vector2 a)
//         {
//             return new ProcessFrameRequest.Types.Vector2() { X = a.x, Y = a.y };
//         }

//         ProcessFrameRequest.Types.Quaternion UnityQuaternionToProto(Quaternion a)
//         {
//             return new ProcessFrameRequest.Types.Quaternion()
//             {
//                 X = a.x,
//                 Y = a.y,
//                 Z = a.z,
//                 W = a.w,
//             };
//         }

//         /// <summary>
//         /// For streaming data: start streaming allow data to be sent periodically until stop streaming.
//         /// </summary>
//         public void StartDataStreaming()
//         {
//             _isStreaming = true;
//             if (_activatedDataModalities["Audio"])
//             {
//                 ARFlowAudioManager.InitializeAudioRecording(DEFAULT_SAMPLE_RATE, DEFAULT_FRAME_LENGTH);
//             }
//         }

//         /// <summary>
//         /// For streaming data: stop streaming data so that we don't consume more
//         /// resource after this point.
//         /// </summary>
//         public void StopDataStreaming()
//         {
//             _isStreaming = false;
//             if (_activatedDataModalities["Audio"])
//             {
//                 ARFlowAudioManager.DisposeAudioRecording();
//             }
//         }

//         /// <summary>
//         /// Collect data frame's data for sending to server
//         /// </summary>
//         /// <returns></returns>
//         public ProcessFrameRequest CollectDataFrame()
//         {
//             var dataFrame = new ProcessFrameRequest();

//             //if (!_activatedDataModalities["CameraColor"] || !_activatedDataModalities["CameraDepth"])
//             //    dataFrame.Timestamp = Timestamp.FromDateTime(System.DateTime.UtcNow);

//             if (_activatedDataModalities["CameraColor"])
//             {
//                 var colorImage = new XRYCbCrColorImage(cameraManager, _sampleSize);
//                 dataFrame.Color = ByteString.CopyFrom(colorImage.Encode());
//                 colorImage.Dispose();
//             }

//             if (_activatedDataModalities["CameraDepth"])
//             {
//                 occlusionManager.TryAcquireEnvironmentDepthConfidenceCpuImage(out depthImage);
//                 _occlusionManager.TryAcquireTemporalSmoothedDepthCpuImage(out depthImage);
//                 var depthImage = new XRConfidenceFilteredDepthImage(_occlusionManager, 0);
//                 dataFrame.Depth = ByteString.CopyFrom(depthImage.Encode());
//                 depthImage.Dispose();
//             }

//             if (_activatedDataModalities["CameraTransform"])
//             {
//                 const int transformLength = 3 * 4 * sizeof(float);
//                 var m = Camera.main!.transform.localToWorldMatrix;
//                 var cameraTransformBytes = new byte[transformLength];

//                 Buffer.BlockCopy(
//                     new[]
//                     {
//                         m.m00,
//                         m.m01,
//                         m.m02,
//                         m.m03,
//                         m.m10,
//                         m.m11,
//                         m.m12,
//                         m.m13,
//                         m.m20,
//                         m.m21,
//                         m.m22,
//                         m.m23,
//                     },
//                     0,
//                     cameraTransformBytes,
//                     0,
//                     transformLength
//                 );

//                 dataFrame.Transform = ByteString.CopyFrom(cameraTransformBytes);
//             }

//             if (_activatedDataModalities["PlaneDetection"])
//             {
//                 foreach (ARPlane plane in PlaneManager.trackables)
//                 {
//                     var protoPlane = new ProcessFrameRequest.Types.Plane();
//                     protoPlane.Center = UnityVector3ToProto(plane.center);
//                     protoPlane.Normal = UnityVector3ToProto(plane.normal);
//                     protoPlane.Size = UnityVector2ToProto(plane.size);
//                     protoPlane.BoundaryPoints.Add(
//                         plane.boundary.Select(point => UnityVector2ToProto(point))
//                     );

//                     dataFrame.PlaneDetection.Add(protoPlane);
//                 }
//             }

//             if (_activatedDataModalities["Gyroscope"])
//             {
//                 dataFrame.Gyroscope = new ProcessFrameRequest.Types.GyroscopeData();
//                 Quaternion attitude = AttitudeSensor.current.attitude.ReadValue();
//                 Vector3 rotation_rate =
//                     UnityEngine.InputSystem.Gyroscope.current.angularVelocity.ReadValue();
//                 Vector3 gravity = GravitySensor.current.gravity.ReadValue();
//                 Vector3 acceleration = Accelerometer.current.acceleration.ReadValue();

//                 dataFrame.Gyroscope.Attitude = UnityQuaternionToProto(attitude);
//                 dataFrame.Gyroscope.RotationRate = UnityVector3ToProto(rotation_rate);
//                 dataFrame.Gyroscope.Gravity = UnityVector3ToProto(gravity);
//                 dataFrame.Gyroscope.Acceleration = UnityVector3ToProto(acceleration);
//             }

//             if (_activatedDataModalities["Audio"])
//             {
//                 InternalDebug.Log("audio");
//                 dataFrame.AudioData.Add(ARFlowAudioManager.GetFrames());
//                 ARFlowAudioManager.ClearFrameList();
//             }

//             if (_activatedDataModalities["Meshing"])
//             {
//                 IList<MeshFilter> meshFilters = meshManager.meshes;
//                 InternalDebug.Log($"Number of mesh filters: {meshFilters.Count}");
//                 foreach (MeshFilter meshFilter in meshFilters)
//                 {
//                     Mesh mesh = meshFilter.sharedMesh;
//                     List<NativeArray<byte>> encodedMesh = _meshEncoder.EncodeMesh(mesh);

//                     foreach (var meshElement in encodedMesh)
//                     {
//                         var meshProto = new ProcessFrameRequest.Types.Mesh();
//                         meshProto.Data = ByteString.CopyFrom(meshElement);

//                         dataFrame.Meshes.Add(meshProto);
//                     }
//                 }
//             }

//             return dataFrame;
//         }

//         /// <summary>
//         /// This is a Task wrapper for GetAndSendFrame, to avoid blocking in the main thread.
//         /// <para /> The method will spawn a new task, so the usage of this is only calling "GetAndSendFrameTask()"
//         /// </summary>
//         /// <returns></returns>
//         public Task<string> GetAndSendFrameTask()
//         {
//             var dataFrame = CollectDataFrame();

//             return Task.Run(() => _grpc_client.SendFrame(dataFrame));
//         }

//         /// <summary>
//         /// Send a data of a frame to the server.
//         /// </summary>
//         /// <param name="frameData">Data of the frame. The typing of this is generated by Protobuf.</param>
//         /// <returns>A message from the server.</returns>
//         public string GetAndSendFrame()
//         {
//             var dataFrame = CollectDataFrame();

//             string serverMessage = _grpc_client.SendFrame(dataFrame);
//             return serverMessage;
//         }

//         public string getSessionId()
//         {
//             return _currentUid;
//         }
//     }
// }
