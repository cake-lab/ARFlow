using System;
using System.Collections.Concurrent;
using System.Linq;
using Google.Protobuf.WellKnownTypes;
using UnityEngine.XR.ARFoundation;

namespace CakeLab.ARFlow.DataBuffers
{
    using Clock;
    using Grpc.V1;
    using Utilities;
    using GrpcVector2Int = Grpc.V1.Vector2Int;
    using GrpcXRCpuImage = Grpc.V1.XRCpuImage;
    using UnityVector2Int = UnityEngine.Vector2Int;
    using UnityXRCameraIntrinsics = UnityEngine.XR.ARSubsystems.XRCameraIntrinsics;
    using UnityXRCpuImage = UnityEngine.XR.ARSubsystems.XRCpuImage;

    /// <remarks>
    /// We don't keep a buffer of XRCpuImage here because these are native resources that need to be disposed.
    /// </remarks>
    public struct RawColorFrame
    {
        public DateTime DeviceTimestamp;
        public UnityVector2Int Dimensions;
        public UnityXRCpuImage.Format Format;
        public double ImageTimestamp;
        public RawARPlane[] Planes;
        public UnityXRCameraIntrinsics Intrinsics;

        public static explicit operator Grpc.V1.ColorFrame(RawColorFrame rawFrame)
        {
            var xrCameraIntrinsicsGrpc = new Grpc.V1.Intrinsics
            {
                FocalLength = new Grpc.V1.Vector2
                {
                    X = rawFrame.Intrinsics.focalLength.x,
                    Y = rawFrame.Intrinsics.focalLength.y,
                },
                PrincipalPoint = new Grpc.V1.Vector2
                {
                    X = rawFrame.Intrinsics.principalPoint.x,
                    Y = rawFrame.Intrinsics.principalPoint.y,
                },
                Resolution = new Grpc.V1.Vector2Int
                {
                    X = rawFrame.Intrinsics.resolution.x,
                    Y = rawFrame.Intrinsics.resolution.y,
                },
            };
            var xrCpuImageGrpc = new GrpcXRCpuImage
            {
                Dimensions = new GrpcVector2Int
                {
                    X = rawFrame.Dimensions.x,
                    Y = rawFrame.Dimensions.y,
                },
                Format = (XRCpuImage.Types.Format)rawFrame.Format,
                Timestamp = rawFrame.ImageTimestamp,
            };
            xrCpuImageGrpc.Planes.AddRange(
                rawFrame.Planes.Select(plane => (Grpc.V1.XRCpuImage.Types.Plane)plane)
            );
            var colorFrameGrpc = new Grpc.V1.ColorFrame
            {
                DeviceTimestamp = Timestamp.FromDateTime(rawFrame.DeviceTimestamp),
                Image = xrCpuImageGrpc,
                Intrinsics = xrCameraIntrinsicsGrpc,
            };
            return colorFrameGrpc;
        }

        public static explicit operator Grpc.V1.ARFrame(RawColorFrame rawFrame)
        {
            var arFrame = new Grpc.V1.ARFrame { ColorFrame = (Grpc.V1.ColorFrame)rawFrame };
            return arFrame;
        }
    }

    public class ColorBuffer : IARFrameBuffer<RawColorFrame>
    {
        ARCameraManager m_CameraManager;

        /// <summary>
        /// The ARCameraManager which will produce frame events.
        /// </summary>
        public ARCameraManager CameraManager
        {
            get => m_CameraManager;
            set => m_CameraManager = value;
        }

        IClock m_Clock;

        public IClock Clock
        {
            get => m_Clock;
            set => m_Clock = value;
        }

        private ConcurrentQueue<RawColorFrame> m_Buffer;

        public ConcurrentQueue<RawColorFrame> Buffer => m_Buffer;

        /// <summary>
        /// Typically this should be called at Awake of the MonoBehaviour that contains the ARCameraManager.
        /// </summary>
        /// <remarks>
        /// See <a href="https://github.com/Unity-Technologies/arfoundation-samples/blob/main/Assets/Scenes/FaceTracking/ToggleCameraFacingDirectionOnAction.cs">ToggleCameraFacingDirectionOnAction.cs</a> for an example of how to use this class.
        /// </remarks>
        public ColorBuffer(ARCameraManager cameraManager, IClock clock)
        {
            m_Buffer = new ConcurrentQueue<RawColorFrame>();
            m_CameraManager = cameraManager;
            m_Clock = clock;
        }

        public void StartCapture()
        {
            m_CameraManager.frameReceived += OnCameraFrameReceived;
        }

        public void StopCapture()
        {
            m_CameraManager.frameReceived -= OnCameraFrameReceived;
        }

        private void OnCameraFrameReceived(ARCameraFrameEventArgs _)
        {
            if (!m_CameraManager.TryAcquireLatestCpuImage(out UnityXRCpuImage image))
            {
                InternalDebug.Log("Failed to acquire latest CPU image or camera intrinsics.");
                return;
            }
            if (!m_CameraManager.TryGetIntrinsics(out UnityXRCameraIntrinsics intrinsics))
            {
                InternalDebug.Log("Failed to acquire latest CPU image or camera intrinsics.");
                return;
            }

            AddToBuffer(image, intrinsics, m_Clock.UtcNow);
            image.Dispose();
        }

        private void AddToBuffer(
            UnityXRCpuImage image,
            UnityXRCameraIntrinsics intrinsics,
            DateTime deviceTimestampAtCapture
        )
        {
            using (image)
            {
                var newFrame = new RawColorFrame
                {
                    DeviceTimestamp = deviceTimestampAtCapture,
                    Dimensions = image.dimensions,
                    Format = image.format,
                    ImageTimestamp = image.timestamp,
                    Planes = Enumerable
                        .Range(0, image.planeCount)
                        .Select(i => (RawARPlane)image.GetPlane(i))
                        .ToArray(),
                    Intrinsics = intrinsics,
                };
                m_Buffer.Enqueue(newFrame);

                InternalDebug.Log(
                    $"Device time: {newFrame.DeviceTimestamp}\nImage timestamp: {newFrame.ImageTimestamp}\nImage format: {newFrame.Format}\nImage plane count: {image.planeCount}"
                );
            }
        }

        public RawColorFrame TryAcquireLatestFrame()
        {
            return m_Buffer.LastOrDefault();
        }

        public ARFrame[] TakeARFrames()
        {
            ConcurrentQueue<RawColorFrame> oldFrames;
            lock (m_Buffer)
            {
                oldFrames = m_Buffer;
                m_Buffer = new();
            }
            return oldFrames.Select(frame => (ARFrame)frame).ToArray();
        }

        public void Dispose()
        {
            StopCapture();
            m_Buffer.Clear();
        }

        /// <remarks>
        /// Convert is reportedly <a href="https://github.com/Unity-Technologies/arfoundation-samples/issues/1113#issuecomment-1876327727">more performant</a> than ConvertAsync so we're using that here.
        /// </remarks>
        // private async void AddToBufferAsync(XRCpuImage image, XRCameraIntrinsics intrinsics, DateTime deviceTimestampAtCapture)
        // {
        //     var format = m_DesiredFormat switch
        //     {
        //         CameraFrame.Types.Format.Rgb24 => TextureFormat.RGB24,
        //         CameraFrame.Types.Format.Rgba32 => TextureFormat.RGBA32,
        //         CameraFrame.Types.Format.Unspecified => throw new ArgumentException("Desired format cannot be Unspecified.", nameof(m_DesiredFormat)),
        //         _ => throw new ArgumentOutOfRangeException(nameof(m_DesiredFormat), m_DesiredFormat, "Desired format is not supported."),
        //     };
        //     var conversionParams = new XRCpuImage.ConversionParams(image, format);
        //     var conversion = image.ConvertAsync(conversionParams);
        //     while (conversion.status == XRCpuImage.AsyncConversionStatus.Pending || conversion.status == XRCpuImage.AsyncConversionStatus.Processing)
        //     {
        //         await Awaitable.NextFrameAsync(); // Potential point of contention
        //     }
        //     if (conversion.status != XRCpuImage.AsyncConversionStatus.Ready)
        //     {
        //         InternalDebug.LogErrorFormat("Image conversion failed with status {0}", conversion.status);
        //         // Dispose even if there is an error
        //         conversion.Dispose();
        //         return;
        //     }
        //     var newFrame = new RawCameraFrame
        //     {
        //         DeviceTimestamp = deviceTimestampAtCapture,
        //         ImageTimestamp = image.timestamp,
        //         Intrinsics = intrinsics,
        //         Data = conversion.GetData<byte>().ToArray(),
        //     };
        //     m_Buffer.Add(newFrame);

        //     InternalDebug.Log(
        //         $"Device time: {newFrame.DeviceTimestamp}\nImage timestamp: {newFrame.ImageTimestamp}\nImage format: {newFrame.Format}\nImage plane count: {image.planeCount}\nIntrinsics: {newFrame.Intrinsics}"
        //     );
        //     conversion.Dispose();
        // }
    }
}
