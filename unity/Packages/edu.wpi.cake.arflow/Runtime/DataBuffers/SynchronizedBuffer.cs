using System;
using System.Collections.Generic;
using Google.Protobuf.WellKnownTypes;

namespace CakeLab.ARFlow.DataBuffers
{
    public struct RawSynchronizedBufferFrame
    {
        public DateTime DeviceTimestamp;
        public RawTransformFrame TransformFrame;
        public RawColorFrame ColorFrame;
        public RawDepthFrame DepthFrame;
        public RawGyroscopeFrame GyroscopeFrame;
        public RawAudioFrame AudioFrame;
        public RawPlaneDetectionFrame PlaneDetectionFrame;
        public RawPointCloudDetectionFrame PointCloudDetectionFrame;
        public RawMeshDetectionFrame MeshDetectionFrame;

        public static explicit operator Grpc.V1.SynchronizedARFrame(RawSynchronizedBufferFrame rawFrame)
        {
            var synchronizedFrameGrpc = new Grpc.V1.SynchronizedARFrame
            {
                DeviceTimestamp = Timestamp.FromDateTime(rawFrame.DeviceTimestamp),
                TransformFrame = (Grpc.V1.TransformFrame)rawFrame.TransformFrame,
                ColorFrame = (Grpc.V1.ColorFrame)rawFrame.ColorFrame,
                DepthFrame = (Grpc.V1.DepthFrame)rawFrame.DepthFrame,
                GyroscopeFrame = (Grpc.V1.GyroscopeFrame)rawFrame.GyroscopeFrame,
                AudioFrame = (Grpc.V1.AudioFrame)rawFrame.AudioFrame,
                PlaneDetectionFrame = (Grpc.V1.PlaneDetectionFrame)rawFrame.PlaneDetectionFrame,
                PointCloudDetectionFrame = (Grpc.V1.PointCloudDetectionFrame)rawFrame.PointCloudDetectionFrame,
                MeshDetectionFrame = (Grpc.V1.MeshDetectionFrame)rawFrame.MeshDetectionFrame,
            };
            return synchronizedFrameGrpc;
        }
    }

    public class SynchronizedBuffer : IDataBuffer<RawSynchronizedBufferFrame>
    {
        private readonly TransformBuffer m_TransformBuffer;
        private readonly ColorBuffer m_ColorBuffer;
        private readonly DepthBuffer m_DepthBuffer;
        private readonly GyroscopeBuffer m_GyroscopeBuffer;
        private readonly AudioBuffer m_AudioBuffer;
        private readonly PlaneDetectionBuffer m_PlaneDetectionBuffer;
        private readonly PointCloudDetectionBuffer m_PointCloudDetectionBuffer;
        private readonly MeshDetectionBuffer m_MeshDetectionBuffer;

        private readonly List<RawSynchronizedBufferFrame> m_Buffer;

        public IReadOnlyList<RawSynchronizedBufferFrame> Buffer => m_Buffer;

        public SynchronizedBuffer(int initialBufferSize, TransformBuffer transformBuffer, ColorBuffer colorBuffer, DepthBuffer depthBuffer, GyroscopeBuffer gyroscopeBuffer, AudioBuffer audioBuffer, PlaneDetectionBuffer planeDetectionBuffer, PointCloudDetectionBuffer pointCloudDetectionBuffer, MeshDetectionBuffer meshDetectionBuffer)
        {
            m_Buffer = new List<RawSynchronizedBufferFrame>(initialBufferSize);
            m_TransformBuffer = transformBuffer;
            m_ColorBuffer = colorBuffer;
            m_DepthBuffer = depthBuffer;
            m_GyroscopeBuffer = gyroscopeBuffer;
            m_AudioBuffer = audioBuffer;
            m_PlaneDetectionBuffer = planeDetectionBuffer;
            m_PointCloudDetectionBuffer = pointCloudDetectionBuffer;
            m_MeshDetectionBuffer = meshDetectionBuffer;
        }

        public void StartCapture()
        {
            m_TransformBuffer.StartCapture();
            m_ColorBuffer.StartCapture();
            m_DepthBuffer.StartCapture();
            m_GyroscopeBuffer.StartCapture();
            m_AudioBuffer.StartCapture();
            m_PlaneDetectionBuffer.StartCapture();
            m_PointCloudDetectionBuffer.StartCapture();
            m_MeshDetectionBuffer.StartCapture();
        }

        public void StopCapture()
        {
            m_TransformBuffer.StopCapture();
            m_ColorBuffer.StopCapture();
            m_DepthBuffer.StopCapture();
            m_GyroscopeBuffer.StopCapture();
            m_AudioBuffer.StopCapture();
            m_PlaneDetectionBuffer.StopCapture();
            m_PointCloudDetectionBuffer.StopCapture();
            m_MeshDetectionBuffer.StopCapture();
        }

        public void ClearBuffer()
        {
            m_Buffer.Clear();
        }

        public RawSynchronizedBufferFrame TryAcquireLatestFrame()
        {
            var transformFrame = m_TransformBuffer.TryAcquireLatestFrame();
            var colorFrame = m_ColorBuffer.TryAcquireLatestFrame();
            var depthFrame = m_DepthBuffer.TryAcquireLatestFrame();
            var gyroscopeFrame = m_GyroscopeBuffer.TryAcquireLatestFrame();
            var audioFrame = m_AudioBuffer.TryAcquireLatestFrame();
            var planeDetectionFrame = m_PlaneDetectionBuffer.TryAcquireLatestFrame();
            var pointCloudDetectionFrame = m_PointCloudDetectionBuffer.TryAcquireLatestFrame();
            var meshDetectionFrame = m_MeshDetectionBuffer.TryAcquireLatestFrame();
            return new RawSynchronizedBufferFrame
            {
                DeviceTimestamp = DateTime.UtcNow,
                TransformFrame = transformFrame,
                ColorFrame = colorFrame,
                DepthFrame = depthFrame,
                GyroscopeFrame = gyroscopeFrame,
                AudioFrame = audioFrame,
                PlaneDetectionFrame = planeDetectionFrame,
                PointCloudDetectionFrame = pointCloudDetectionFrame,
                MeshDetectionFrame = meshDetectionFrame,
            };
        }

        public void Dispose()
        {
            StopCapture();
            ClearBuffer();
        }
    }
}