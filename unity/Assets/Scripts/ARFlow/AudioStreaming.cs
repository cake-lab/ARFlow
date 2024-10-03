using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Pv.Unity;

namespace ARFlow
{
    public class AudioStreaming
    {

        private List<float> _unsentFrames;

        public List<float> UnsentFrames
        {
            get { return _unsentFrames; }
        }

        public AudioStreaming()
        {
            _unsentFrames = new List<float>();
        }

        /// <summary>
        /// Since Unity's microphone implementation requires calling "start" and "end", we need
        /// a call to start microphone recording
        /// </summary>
        public void initializeAudioRecording(int sampleRate, int frameLength)
        {
            VoiceProcessor.Instance.StartRecording(frameLength, sampleRate);
            VoiceProcessor.Instance.AddFrameListener(updateCurrentAudioFrame);
        }

        /// <summary>
        /// Our point is that we only want to send the current frame, not all frames that might be sent.
        /// </summary>
        /// <param name="frame"></param>
        private void updateCurrentAudioFrame(float[] frame)
        {
            _unsentFrames.AddRange(frame);
        }

        public void clearFrameList()
        {
            _unsentFrames.Clear();
        }

        public void disposeAudioRecording()
        {
            VoiceProcessor.Instance.StopRecording();
        }
    }

}
