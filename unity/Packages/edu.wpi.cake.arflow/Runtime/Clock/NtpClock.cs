// Vendored from https://github.com/disas69/Unity-NTPTimeSync-Asset/blob/master/Assets/Scripts/NtpDateTime.cs
using System;
using System.Threading;
using UnityEngine;

namespace CakeLab.ARFlow.Clock
{
    using Ntp;
    using Utilities;

    /// <summary>
    /// Clock that syncs with an NTP server. Need to call SynchronizeAsync to start the synchronization, else the clock will fallback to the system clock. Check if time is synchronized with TimeSynchronized property.
    /// </summary>
    public class NtpClock : IClock, IDisposable
    {
        private readonly NtpClient m_NtpClient;
        private CancellationTokenSource m_Cts;
        private DateTime m_NtpTime;
        private float m_ResponseReceivedTime;

        public bool TimeSynchronized { get; private set; }

        public DateTime Now =>
            TimeSynchronized
                ? m_NtpTime.AddSeconds(Time.realtimeSinceStartup - m_ResponseReceivedTime)
                : DateTime.Now;

        public DateTime UtcNow =>
            TimeSynchronized
                ? m_NtpTime
                    .ToUniversalTime()
                    .AddSeconds(Time.realtimeSinceStartup - m_ResponseReceivedTime)
                : DateTime.UtcNow;

        public NtpClock(string ntpServerUrl, int requestTimeoutInS = 3)
        {
            m_NtpClient = new NtpClient(ntpServerUrl, 123, requestTimeoutInS * 1000);
            TimeSynchronized = false;
        }

        public void Dispose()
        {
            m_Cts?.Cancel();
            m_Cts?.Dispose();
        }

        public async Awaitable SynchronizeAsync(CancellationToken cancellationToken = default)
        {
            m_Cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            while (!cancellationToken.IsCancellationRequested && !TimeSynchronized)
            {
                if (ConnectionEnabled())
                {
                    await SynchronizeDateAsync(m_Cts.Token);
                }
                await Awaitable.WaitForSecondsAsync(m_NtpClient.TimeoutInMs / 1000, m_Cts.Token);
            }
        }

        private async Awaitable SynchronizeDateAsync(CancellationToken cancellationToken)
        {
            try
            {
                m_NtpTime = await m_NtpClient.GetCurrentTimeAsync(cancellationToken);
                m_ResponseReceivedTime = Time.realtimeSinceStartup;
                TimeSynchronized = true;
                InternalDebug.Log($"Time synchronized with NTP server: {m_NtpTime}");
            }
            catch (Exception e)
            {
                InternalDebug.LogWarning(
                    $"Failed to synchronize time with NTP server: {e.Message}"
                );
                TimeSynchronized = false;
            }
        }

        private static bool ConnectionEnabled()
        {
            return Application.internetReachability != NetworkReachability.NotReachable;
        }
    }
}
