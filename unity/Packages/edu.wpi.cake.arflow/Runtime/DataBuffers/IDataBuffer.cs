using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using CakeLab.ARFlow.Grpc.V1;

namespace CakeLab.ARFlow.DataBuffers
{
    /// <summary>
    /// Interface for a data buffer. This interface does not provide access to the data buffer, and is only used to create list of different typed-buffers.
    /// </summary>
    public interface IDataBuffer : IDisposable
    {
        void StartCapture();
        void StopCapture();
    }

    /// <summary>
    /// Interface for a data buffer that stores data of type T. Read-only, ordered access to the buffer is provided.
    /// </summary>
    public interface IDataBuffer<T> : IDataBuffer
    {
        ConcurrentQueue<T> Buffer { get; }
        /// <summary>
        /// Tries to acquire the latest frame from the buffer. If the buffer is empty, returns the default.
        /// </summary>
        T TryAcquireLatestFrame();
    }


    /// <summary>
    /// Interface for a data buffer that stores frames convertable to ARFrames. This interface does not provide access to the data buffer, and is only used to create list of different typed-buffers.
    /// </summary>
    public interface IARFrameBuffer : IDataBuffer
    {
        /// <summary>
        /// Atomically take and clear all frames in the buffer. This is a helper method to manage sending ARFrames more easily.
        /// </summary>
        /// <remarks>See https://stackoverflow.com/a/75331708</remarks>
        IEnumerable<ARFrame> TakeARFrames();
    }

    /// <summary>
    /// Interface for a data buffer that stores data of type T. Read-only, ordered access to the buffer is provided. The data in the buffer is convertable to ARFrames.
    /// </summary>
    public interface IARFrameBuffer<T> : IDataBuffer<T>, IARFrameBuffer
    {
    }
}
