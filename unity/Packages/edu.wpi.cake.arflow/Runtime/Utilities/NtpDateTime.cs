// Vendored from https://github.com/disas69/Unity-NTPTimeSync-Asset/blob/master/Assets/Scripts/NtpDateTime.cs
using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace CakeLab.ARFlow.Utilities
{
    public class NtpDateTime : MonoSingleton<NtpDateTime>
    {
        private DateTime _ntpDate;
        private float _responseReceivedTime;
        private byte[] _receivedNtpData;
        private Socket _socket;
        private Thread _syncThread;
        private Coroutine _responseRoutine;
        private bool _responseReceived;

        [SerializeField]
        private string _ntpServer = "pool.ntp.org";

        [SerializeField]
        [Range(1, 10)]
        private int _requestTimeout = 3;

        public bool DateSynchronized { get; private set; }

        public DateTime Now
        {
            get
            {
                if (DateSynchronized)
                {
                    return _ntpDate.AddSeconds(Time.realtimeSinceStartup - _responseReceivedTime);
                }

                return DateTime.Now;
            }
        }

        public DateTime UtcNow
        {
            get
            {
                if (DateSynchronized)
                {
                    return _ntpDate
                        .ToUniversalTime()
                        .AddSeconds(Time.realtimeSinceStartup - _responseReceivedTime);
                }
                return DateTime.UtcNow;
            }
        }

        public void Synchronize()
        {
            DateSynchronized = false;
            StartCoroutine(SynchronizeDateAsync());
        }

        private void Start()
        {
            Synchronize();
        }

        private void OnApplicationQuit()
        {
            if (_syncThread != null)
            {
                _syncThread.Abort();
            }

            if (_socket != null)
            {
                _socket.Close();
            }
        }

        private IEnumerator SynchronizeDateAsync()
        {
            var wait = new WaitForSeconds(_requestTimeout);

            while (true)
            {
                if (!DateSynchronized)
                {
                    if (ConnectionEnabled())
                    {
                        SynchronizeDate();
                    }

                    yield return wait;
                }
                else
                {
                    yield break;
                }
            }
        }

        private void SynchronizeDate()
        {
            if (_syncThread != null)
            {
                _syncThread.Abort();
            }

            if (_socket != null)
            {
                _socket.Close();
            }

            if (_responseRoutine != null)
            {
                StopCoroutine(_responseRoutine);
            }

            _responseReceived = false;
            _syncThread = new Thread(Request);
            _syncThread.Start();

            _responseRoutine = StartCoroutine(WaitForResponse());

            InternalDebug.Log("NTP request started.");
        }

        private void Request()
        {
            var ntpData = new byte[48];
            ntpData[0] = 0x1B;

            var addresses = Dns.GetHostEntry(_ntpServer).AddressList;
            var ipEndPoint = new IPEndPoint(addresses[0], 123);

            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            try
            {
                _socket.Connect(ipEndPoint);
                _socket.ReceiveTimeout = _requestTimeout * 1000;
                _socket.Send(ntpData);
                _socket.Receive(ntpData);
            }
            catch (SocketException e)
            {
                InternalDebug.LogError("NTP sync failed : " + e.Message);
                return;
            }
            finally
            {
                _socket.Close();
                _socket = null;
            }

            _receivedNtpData = ntpData;
            _responseReceived = true;

            InternalDebug.Log("NTP response received.");
        }

        private IEnumerator WaitForResponse()
        {
            while (!_responseReceived)
            {
                yield return 0;
            }

            _responseReceivedTime = Time.realtimeSinceStartup;

            var intPart =
                ((ulong)_receivedNtpData[40] << 24)
                | ((ulong)_receivedNtpData[41] << 16)
                | ((ulong)_receivedNtpData[42] << 8)
                | _receivedNtpData[43];
            var fractPart =
                ((ulong)_receivedNtpData[44] << 24)
                | ((ulong)_receivedNtpData[45] << 16)
                | ((ulong)_receivedNtpData[46] << 8)
                | _receivedNtpData[47];

            var milliseconds = intPart * 1000 + fractPart * 1000 / 0x100000000L;
            _ntpDate = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                .AddMilliseconds((long)milliseconds)
                .ToLocalTime();

            DateSynchronized = true;
            InternalDebug.Log("Date is synchronized : " + Now);
        }

        private static bool ConnectionEnabled()
        {
            return Application.internetReachability != NetworkReachability.NotReachable;
        }
    }
}
