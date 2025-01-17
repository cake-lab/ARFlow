using System;
using System.Collections.Concurrent;
using System.Linq;
using Google.Protobuf.WellKnownTypes;

namespace CakeLab.ARFlow.DataBuffers
{
    using Clock;
    using Utilities;
    using Grpc.V1;

    public struct RawAudioFrame
    {
        public DateTime DeviceTimestamp;
        public float[] Data;

        public static explicit operator Grpc.V1.AudioFrame(RawAudioFrame rawFrame)
        {
            var audioFrameGrpc = new Grpc.V1.AudioFrame
            {
                DeviceTimestamp = Timestamp.FromDateTime(rawFrame.DeviceTimestamp),
            };
            audioFrameGrpc.Data.AddRange(rawFrame.Data);
            return audioFrameGrpc;
        }

        public static explicit operator Grpc.V1.ARFrame(RawAudioFrame rawFrame)
        {
            var arFrame = new Grpc.V1.ARFrame { AudioFrame = (Grpc.V1.AudioFrame)rawFrame };
            return arFrame;
        }
    }

    public class AudioBuffer : IARFrameBuffer<RawAudioFrame>
    {
        private ConcurrentQueue<RawAudioFrame> m_Buffer;
        private readonly int m_SampleRate;
        private readonly int m_FrameLength;

        IClock m_Clock;

        public IClock Clock
        {
            get => m_Clock;
            set => m_Clock = value;
        }

        public ConcurrentQueue<RawAudioFrame> Buffer => m_Buffer;

        public AudioBuffer(
            IClock clock,
            int sampleRate = 16000,
            int frameLength = 512
        )
        {
            m_Buffer = new ConcurrentQueue<RawAudioFrame>();
            m_Clock = clock;
            m_SampleRate = sampleRate;
            m_FrameLength = frameLength;
        }

        public void StartCapture()
        {
            // Since Unity's microphone implementation requires calling Start and End, we need
            // a call to start microphone recording
            VoiceProcessor.Instance.StartRecording(m_FrameLength, m_SampleRate);
            VoiceProcessor.Instance.AddFrameListener(OnFrameCaptured);
        }

        public void StopCapture()
        {
            VoiceProcessor.Instance.RemoveFrameListener(OnFrameCaptured);
            VoiceProcessor.Instance.StopRecording();
        }

        private void OnFrameCaptured(float[] frame)
        {
            m_Buffer.Enqueue(new RawAudioFrame { DeviceTimestamp = m_Clock.UtcNow, Data = frame });
        }

        public RawAudioFrame TryAcquireLatestFrame()
        {
            return m_Buffer.LastOrDefault();
        }

        public ARFrame[] TakeARFrames()
        {
            ConcurrentQueue<RawAudioFrame> oldFrames;
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
    }
}
