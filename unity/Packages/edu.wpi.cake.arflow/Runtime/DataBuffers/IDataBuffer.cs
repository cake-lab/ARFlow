using System;
using System.Collections.Generic;

namespace CakeLab.ARFlow.DataBuffers
{
    /// <summary>
    /// Interface for a data buffer that stores data of type T. Read-only, ordered access to the buffer is provided.
    /// </summary>
    interface IDataBuffer<T> : IDisposable
    {
        IReadOnlyList<T> Buffer { get; }
        void StartCapture();
        void StopCapture();
        void ClearBuffer();
    }
}
