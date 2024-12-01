using System;
using System.Collections.Generic;

namespace CakeLab.ARFlow.DataBuffers
{
    /// <summary>
    /// Interface for a data buffer. This interface does not provide access to the data buffer, and is only used to create list of different typed-buffers.
    /// </summary>
    interface IDataBuffer : IDisposable
    {
        void StartCapture();
        void StopCapture();
        void ClearBuffer();
    }

    /// <summary>
    /// Interface for a data buffer that stores data of type T. Read-only, ordered access to the buffer is provided.
    /// </summary>
    interface IDataBuffer<T> : IDisposable, IDataBuffer
    {
        IReadOnlyList<T> Buffer { get; }
        /// <summary>
        /// Tries to acquire the latest frame from the buffer. If the buffer is empty, returns the default.
        /// </summary>
        T TryAcquireLatestFrame();
    }
}
