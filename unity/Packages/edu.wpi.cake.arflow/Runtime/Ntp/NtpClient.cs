using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace CakeLab.ARFlow.Ntp
{
    using Utilities;

    public class NtpClient
    {
        public string ServerUrl { get; private set; }
        public int Port { get; private set; }
        public int TimeoutInMs { get; private set; }

        public NtpClient(string ntpServerUrl, int port = 123, int timeoutInMs = 3000)
        {
            ServerUrl = ntpServerUrl;
            Port = port;
            TimeoutInMs = timeoutInMs;
        }

        public async Awaitable<DateTime> GetCurrentTimeAsync(
            CancellationToken cancellationToken = default
        )
        {
            var ntpData = new byte[48];
            ntpData[0] = 0x1B; // NTP request header

            var addresses = await Dns.GetHostAddressesAsync(ServerUrl);
            var ipEndPoint = new IPEndPoint(addresses[0], Port);

            using var socket = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Dgram,
                ProtocolType.Udp
            );

            await socket.ConnectAsync(ipEndPoint);
            socket.ReceiveTimeout = TimeoutInMs;

            await socket.SendToAsync(new ArraySegment<byte>(ntpData), SocketFlags.None, ipEndPoint);

            var receiveBuffer = new byte[48];
            var response = await socket.ReceiveAsync(
                new ArraySegment<byte>(receiveBuffer),
                SocketFlags.None
            );
            if (response != 48) // Expected size of NTP response
                throw new Exception("Unexpected response size from NTP server.");
            return ParseNtpResponse(receiveBuffer);
        }

        /// <summary>
        /// Probes the specified NTP port to check if it's open.
        /// </summary>
        public async Awaitable<bool> ProbePortAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var addresses = await Dns.GetHostAddressesAsync(ServerUrl);
                var ipEndPoint = new IPEndPoint(addresses[0], Port);

                using (
                    var socket = new Socket(
                        AddressFamily.InterNetwork,
                        SocketType.Dgram,
                        ProtocolType.Udp
                    )
                )
                {
                    // Connect and check if port is reachable
                    var connectTask = socket.ConnectAsync(ipEndPoint);
                    if (
                        await Task.WhenAny(
                            connectTask,
                            Awaitable
                                .WaitForSecondsAsync(TimeoutInMs / 1000, cancellationToken)
                                .AsTask()
                        ) == connectTask
                    )
                    {
                        return true;
                    }

                    return false;
                }
            }
            catch
            {
                return false; // Assume port is closed or unreachable on exception
            }
        }

        private DateTime ParseNtpResponse(byte[] ntpData)
        {
            var intPart =
                ((ulong)ntpData[40] << 24)
                | ((ulong)ntpData[41] << 16)
                | ((ulong)ntpData[42] << 8)
                | ntpData[43];
            var fractPart =
                ((ulong)ntpData[44] << 24)
                | ((ulong)ntpData[45] << 16)
                | ((ulong)ntpData[46] << 8)
                | ntpData[47];

            var milliseconds = intPart * 1000 + fractPart * 1000 / 0x100000000L;
            return new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                .AddMilliseconds((long)milliseconds)
                .ToLocalTime();
        }
    }
}
