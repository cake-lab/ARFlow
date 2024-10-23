using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARFlow
{
    /// <summary>
    /// Interface for audio streaming. 
    /// </summary>
    public interface IAudioStreaming
    {
        /// <summary>
        /// Get all audio frames.
        /// </summary>
        /// <returns>Audio data in float</returns>
        public List<float> GetFrames();
        /// <summary>
        /// Start the recording process
        /// </summary>
        /// <param name="sampleRate"></param>
        /// <param name="frameLength">Length of frame before getFrames is appended with new data.</param>
        public void InitializeAudioRecording(int sampleRate, int frameLength);

        /// <summary>
        /// Stop the recording process.
        /// </summary>
        public void DisposeAudioRecording();

        /// <summary>
        /// If we are sending all unsent audio data (instead of the newest data),
        /// clearFrameList will be called every time current audio frames are sent.
        /// </summary>
        public void ClearFrameList();
    }

}
