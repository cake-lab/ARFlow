using System.Threading;
using UnityEngine;

namespace CakeLab.ARFlow
{
    using Clock;
    using Utilities;

    public class NtpDemo : MonoBehaviour
    {
        public NtpClock clock;
        public string ntpServerUrl = "pool.ntp.org";
        public int requestTimeoutInS = 3;
        private CancellationTokenSource m_Cts;

        async void Start()
        {
            clock = new NtpClock(ntpServerUrl, requestTimeoutInS);
            m_Cts = new CancellationTokenSource();
            await clock.SynchronizeAsync(m_Cts.Token);
        }

        void Update()
        {
            if (!clock.TimeSynchronized)
            {
                InternalDebug.Log("Time not yet synchronized.");
            }
            InternalDebug.Log($"Synchronized Time: {clock.Now}");
            InternalDebug.Log($"Synchronized UtcTime: {clock.UtcNow}");
        }
    }
}
