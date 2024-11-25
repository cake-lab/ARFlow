using System;
using System.Collections.Generic;
using Google.Protobuf.WellKnownTypes;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine;

namespace CakeLab.ARFlow.DataBuffers
{
    using Grpc.V1;
    using Utilities;

    /// <remarks>
    /// We don't keep a buffer of XRCpuImage here because these are native resources that need to be disposed.
    /// </remarks>
    public struct RawCameraFrame
    {
        public DateTime DeviceTimestamp;

        public double ImageTimestamp;
        public XRCpuImage.Format Format;
        public XRCpuImage.Plane[] Planes;
        public XRCameraIntrinsics Intrinsics;
        public byte[] Data;

#pragma warning disable IDE0001 // Name can be simplified
        public static explicit operator Grpc.V1.ARFrame(RawCameraFrame rawFrame)
        {
            var cameraFrameGrpc = new Grpc.V1.CameraFrame
            {
                DeviceTimestamp = Timestamp.FromDateTime(rawFrame.DeviceTimestamp),
                ImageTimestamp = rawFrame.ImageTimestamp,
                Format = (CameraFrame.Types.Format)rawFrame.Format,
                Intrinsics = new Grpc.V1.CameraFrame.Types.Intrinsics
                {
                    FocalLength = new Vector2
                    {
                        X = rawFrame.Intrinsics.focalLength.x,
                        Y = rawFrame.Intrinsics.focalLength.y,
                    },
                    PrincipalPoint = new Vector2
                    {
                        X = rawFrame.Intrinsics.principalPoint.x,
                        Y = rawFrame.Intrinsics.principalPoint.y,
                    },
                    Resolution = new Vector2Int
                    {
                        X = rawFrame.Intrinsics.resolution.x,
                        Y = rawFrame.Intrinsics.resolution.y,
                    },
                },
                Data = Google.Protobuf.ByteString.CopyFrom(rawFrame.Data),
            };
            // rawCameraFrame.Planes.AddRange(
            //     (rawFrame.Planes ?? Array.Empty<XRCpuImage.Plane>()).Select(plane => new Grpc.V1.CameraFrame.Types.Plane
            //     {
            //         RowStride = plane.rowStride,
            //         PixelStride = plane.pixelStride,
            //         Data = Google.Protobuf.ByteString.CopyFrom(plane.data.ToArray()),
            //     })
            // );
            var arFrame = new Grpc.V1.ARFrame
            {
                CameraFrame = cameraFrameGrpc,
            };
            return arFrame;
        }
#pragma warning restore IDE0001 // Name can be simplified
    }

    public class CameraBuffer : IDataBuffer<RawCameraFrame>
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

        private readonly List<RawCameraFrame> m_Buffer;

        public IReadOnlyList<RawCameraFrame> Buffer => m_Buffer;

        /// <summary>
        /// Typically this should be called at Awake of the MonoBehaviour that contains the ARCameraManager.
        /// </summary>
        /// <remarks>
        /// See <a href="https://github.com/Unity-Technologies/arfoundation-samples/blob/main/Assets/Scenes/FaceTracking/ToggleCameraFacingDirectionOnAction.cs">ToggleCameraFacingDirectionOnAction.cs</a> for an example of how to use this class.
        /// </remarks>
        public CameraBuffer(int initialBufferSize, ARCameraManager cameraManager)
        {
            m_Buffer = new List<RawCameraFrame>(initialBufferSize);
            m_CameraManager = cameraManager;
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
            if (
                !m_CameraManager.TryAcquireLatestCpuImage(out XRCpuImage image)
                || !m_CameraManager.TryGetIntrinsics(out XRCameraIntrinsics intrinsics)
            )
            {
                InternalDebug.Log("Failed to acquire latest CPU image or camera intrinsics.");
                return;
            }

            AddToBufferAsync(image, intrinsics);
        }

        private async void AddToBufferAsync(XRCpuImage image, XRCameraIntrinsics intrinsics)
        {
            using (image)
            {
                var format = TextureFormat.RGBA32;
                var conversionParams = new XRCpuImage.ConversionParams(image, format);
                var conversion = image.ConvertAsync(conversionParams);
                using (conversion)
                {
                    while (conversion.status == XRCpuImage.AsyncConversionStatus.Pending || conversion.status == XRCpuImage.AsyncConversionStatus.Processing)
                    {
                        await Awaitable.NextFrameAsync(); // Potential point of contention
                    }
                    if (conversion.status != XRCpuImage.AsyncConversionStatus.Ready)
                    {
                        Debug.LogError("Failed to convert camera image to RGBA32");
                        return;
                    }
                    var newFrame = new RawCameraFrame
                    {
                        DeviceTimestamp = DateTime.UtcNow,
                        ImageTimestamp = image.timestamp,
                        Intrinsics = intrinsics,
                        Data = conversion.GetData<byte>().ToArray(),
                    };
                    m_Buffer.Add(newFrame);

                    InternalDebug.Log(
                        $"Device time: {newFrame.DeviceTimestamp}\nImage timestamp: {newFrame.ImageTimestamp}\nImage format: {newFrame.Format}\nImage plane count: {image.planeCount}\nIntrinsics: {newFrame.Intrinsics}"
                    );
                }
            }
        }

        public void ClearBuffer()
        {
            m_Buffer.Clear();
        }

        public void Dispose()
        {
            StopCapture();
            ClearBuffer();
        }

        /// <remarks>
        /// Convert is reportedly <a href="https://github.com/Unity-Technologies/arfoundation-samples/issues/1113#issuecomment-1876327727">more performant</a> than ConvertAsync so we're using that here.
        /// </remarks>
        // private void AddToBuffer(XRCpuImage image, XRCameraIntrinsics intrinsics)
        // {
        //     // using (image)
        //     // {
        //     //     var newFrame = new RawCameraFrame
        //     //     {
        //     //         DeviceTimestamp = DateTime.UtcNow,
        //     //         ImageTimestamp = image.timestamp,
        //     //         Format = image.format,
        //     //         Planes = Enumerable
        //     //             .Range(0, image.planeCount)
        //     //             .Select(i => image.GetPlane(i))
        //     //             .ToArray(),
        //     //         Intrinsics = intrinsics,
        //     //     };
        //     //     m_Buffer.Add(newFrame);

        //     //     InternalDebug.Log(
        //     //         $"Device time: {newFrame.DeviceTimestamp}\nImage timestamp: {newFrame.ImageTimestamp}\nImage format: {newFrame.Format}\nImage plane count: {image.planeCount}\nIntrinsics: {intrinsics}"
        //     //     );
        //     // }

        //     using (image)
        //     {
        //         var format = TextureFormat.RGBA32;
        //         // TODO(perf): When converting the image with XRCpuImage.Convert, provide XRCpuImage.ConversionParams with smaller outputDimensions.
        //         var conversionParams = new XRCpuImage.ConversionParams(image, format);
        //         try
        //         {
        //             image.Convert(conversionParams, )
        //         }
        //     }
        // }
    }
}
