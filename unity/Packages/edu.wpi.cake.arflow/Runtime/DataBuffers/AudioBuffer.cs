using System;
using System.Collections.Generic;

namespace CakeLab.ARFlow.DataBuffers
{
    using Utilities;

    public class AudioBuffer : IDataBuffer<float>
    {
        private readonly List<float> m_Buffer;
        private int m_SampleRate;
        private int m_FrameLength;

        public IReadOnlyList<float> Buffer => m_Buffer;

        public AudioBuffer(int initialBufferSize, int sampleRate = 16000, int frameLength = 512)
        {
            m_Buffer = new List<float>(initialBufferSize);
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
            m_Buffer.AddRange(frame);
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
    }
}
