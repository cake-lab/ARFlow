// Vendored from https://github.com/disas69/Unity-NTPTimeSync-Asset/blob/master/Assets/Scripts/NtpDateTime.cs
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace CakeLab.ARFlow.Utilities
{
    public class NtpDateTime : IDisposable
    {
        private DateTime m_NtpTime;
        private float m_ResponseReceivedTime;
        private string m_NtpServerUrl;
        private int m_RequestTimeoutInS;
        private CancellationTokenSource m_Cts;

        public bool DateSynchronized { get; private set; }

        public DateTime Now =>
            DateSynchronized
                ? m_NtpTime.AddSeconds(Time.realtimeSinceStartup - m_ResponseReceivedTime)
                : DateTime.Now;

        public DateTime UtcNow =>
            DateSynchronized
                ? m_NtpTime
                    .ToUniversalTime()
                    .AddSeconds(Time.realtimeSinceStartup - m_ResponseReceivedTime)
                : DateTime.UtcNow;

        public NtpDateTime(string ntpServerUrl, int requestTimeoutInS = 3)
        {
            m_NtpServerUrl = ntpServerUrl;
            m_RequestTimeoutInS = requestTimeoutInS;
            DateSynchronized = false;
        }

        public void Dispose()
        {
            m_Cts?.Cancel();
            m_Cts?.Dispose();
        }

        public async Task SynchronizeAsync(CancellationToken cancellationToken = default)
        {
            m_Cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            try
            {
                while (!cancellationToken.IsCancellationRequested && !DateSynchronized)
                {
                    if (ConnectionEnabled())
                    {
                        await SynchronizeDateAsync(m_Cts.Token);
                    }
                    // await Awaitable.WaitForSecondsAsync(m_RequestTimeoutInS, cancellationToken);
                    await Task.Delay(TimeSpan.FromSeconds(m_RequestTimeoutInS), m_Cts.Token);
                }
            }
            catch (TaskCanceledException)
            {
                InternalDebug.Log("NTP synchronization cancelled.");
            }
        }

        private async Task SynchronizeDateAsync(CancellationToken cancellationToken)
        {
            try
            {
                var ntpData = new byte[48];
                ntpData[0] = 0x1B; // NTP request header

                var addresses = await Dns.GetHostAddressesAsync(m_NtpServerUrl);
                var ipEndPoint = new IPEndPoint(addresses[0], 123);

                using (
                    var socket = new Socket(
                        AddressFamily.InterNetwork,
                        SocketType.Dgram,
                        ProtocolType.Udp
                    )
                )
                {
                    var timeoutTask = Task.Delay(m_RequestTimeoutInS * 1000, cancellationToken);

                    socket.Connect(ipEndPoint);

                    // Send request
                    var sendTask = socket.SendAsync(
                        new ArraySegment<byte>(ntpData),
                        SocketFlags.None
                    );
                    if (await Task.WhenAny(sendTask, timeoutTask) == timeoutTask)
                    {
                        throw new TimeoutException("NTP request timed out.");
                    }

                    // Receive response
                    var receiveTask = socket.ReceiveAsync(
                        new ArraySegment<byte>(ntpData),
                        SocketFlags.None
                    );
                    if (await Task.WhenAny(receiveTask, timeoutTask) == timeoutTask)
                    {
                        throw new TimeoutException("NTP response timed out.");
                    }

                    ParseNtpResponse(ntpData);
                }
            }
            catch (Exception e)
            {
                InternalDebug.LogError($"NTP synchronization failed: {e.Message}");
                DateSynchronized = false;
            }
        }

        private void ParseNtpResponse(byte[] m_ReceivedNtpData)
        {
            m_ResponseReceivedTime = Time.realtimeSinceStartup;

            var intPart =
                ((ulong)m_ReceivedNtpData[40] << 24)
                | ((ulong)m_ReceivedNtpData[41] << 16)
                | ((ulong)m_ReceivedNtpData[42] << 8)
                | m_ReceivedNtpData[43];
            var fractPart =
                ((ulong)m_ReceivedNtpData[44] << 24)
                | ((ulong)m_ReceivedNtpData[45] << 16)
                | ((ulong)m_ReceivedNtpData[46] << 8)
                | m_ReceivedNtpData[47];

            var milliseconds = intPart * 1000 + fractPart * 1000 / 0x100000000L;
            m_NtpTime = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                .AddMilliseconds((long)milliseconds)
                .ToLocalTime();

            DateSynchronized = true;
            InternalDebug.Log($"Date synchronized: {Now}");
        }

        private static bool ConnectionEnabled()
        {
            return Application.internetReachability != NetworkReachability.NotReachable;
        }
    }
}
