using System;
using System.Threading;
using UnityEngine;

namespace CakeLab.ARFlow.Utilities
{
    public class NtpDateTimeManager : MonoBehaviour
    {
        [SerializeField]
        private string ntpServerUrl = "pool.ntp.org";

        [SerializeField]
        private int requestTimeoutInSeconds = 3;

        private NtpDateTime m_NtpDateTime;
        private CancellationTokenSource m_Cts;

        public bool IsSynchronized => m_NtpDateTime?.DateSynchronized ?? false;
        public DateTime Now => m_NtpDateTime?.Now ?? DateTime.Now;
        public DateTime UtcNow => m_NtpDateTime?.UtcNow ?? DateTime.UtcNow;

        private void Start()
        {
            m_NtpDateTime = new NtpDateTime(ntpServerUrl, requestTimeoutInSeconds);
            m_Cts = new CancellationTokenSource();
            SynchronizeTime();
        }

        private async void SynchronizeTime()
        {
            if (m_NtpDateTime == null)
                return;

            try
            {
                await m_NtpDateTime.SynchronizeAsync(m_Cts.Token);
                InternalDebug.Log($"Time synchronized: {Now}");
            }
            catch (Exception e)
            {
                InternalDebug.LogError($"NTP synchronization failed: {e.Message}");
            }
        }

        private void OnDestroy()
        {
            m_Cts?.Cancel();
            m_Cts?.Dispose();
            m_NtpDateTime?.Dispose();
        }
    }
}
