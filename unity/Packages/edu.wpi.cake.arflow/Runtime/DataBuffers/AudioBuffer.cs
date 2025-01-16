using System;
using System.Collections.Generic;
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
        private readonly List<RawAudioFrame> m_Buffer;
        private readonly int m_SampleRate;
        private readonly int m_FrameLength;

        IClock m_Clock;

        public IClock Clock
        {
            get => m_Clock;
            set => m_Clock = value;
        }

        public IReadOnlyList<RawAudioFrame> Buffer => m_Buffer;

        public AudioBuffer(
            int initialBufferSize,
            IClock clock,
            int sampleRate = 16000,
            int frameLength = 512
        )
        {
            m_Buffer = new List<RawAudioFrame>(initialBufferSize);
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
            m_Buffer.Add(new RawAudioFrame { DeviceTimestamp = m_Clock.UtcNow, Data = frame });
        }

        public void ClearBuffer()
        {
            m_Buffer.Clear();
        }

        public RawAudioFrame TryAcquireLatestFrame()
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
