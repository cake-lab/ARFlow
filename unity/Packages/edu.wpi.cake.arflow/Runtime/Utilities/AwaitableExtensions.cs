using System.Threading.Tasks;
using UnityEngine;

namespace CakeLab.ARFlow.Utilities
{
    /// <summary>
    /// Provides extension methods for converting <see cref="Awaitable"/> and <see cref="Awaitable{T}"/> to <see cref="Task"/> and <see cref="Task{T}"/> respectively.
    /// </summary>
    /// <remarks>
    /// These extension methods allow you to use Unity's awaitable types with the standard .NET Task-based asynchronous programming model.
    /// See <a href="https://docs.unity3d.com/6000.0/Documentation/Manual/async-awaitable-examples.html#awaitable-as-task">Unity Documentation</a> for more details.
    /// </remarks>
    public static class AwaitableExtensions
    {
        /// <summary>
        /// Converts an <see cref="Awaitable"/> to a <see cref="Task"/>.
        /// </summary>
        /// <param name="a">The <see cref="Awaitable"/> to convert.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
        public static async Task AsTask(this Awaitable a)
        {
            await a;
        }

        /// <summary>
        /// Converts an <see cref="Awaitable{T}"/> to a <see cref="Task{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of the result produced by the <see cref="Awaitable{T}"/>.</typeparam>
        /// <param name="a">The <see cref="Awaitable{T}"/> to convert.</param>
        /// <returns>A <see cref="Task{T}"/> that represents the asynchronous operation and produces a result of type <typeparamref name="T"/>.</returns>
        public static async Task<T> AsTask<T>(this Awaitable<T> a)
        {
            return await a;
        }
    }
}