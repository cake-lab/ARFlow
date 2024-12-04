using System;
using TMPro;
using UnityEngine;

namespace CakeLab.ARFlow
{
    using Utilities;

    public class NtpDemo : MonoBehaviour
    {
        public TMP_Text LocalValue;
        public TMP_Text NtpValue;
        public TMP_Text NtpUtcValue;

        private void Update()
        {
            var dateTimeNow = DateTime.Now;
            LocalValue.text = string.Format(
                "Local value: {0} {1}",
                dateTimeNow.ToShortDateString(),
                dateTimeNow.ToLongTimeString()
            );

            if (NtpDateTime.Instance.DateSynchronized)
            {
                var ntpDateTimeNow = NtpDateTime.Instance.Now;
                NtpValue.text = string.Format(
                    "NTP value: {0} {1}",
                    ntpDateTimeNow.ToShortDateString(),
                    ntpDateTimeNow.ToLongTimeString()
                );
                var ntpUtcDateTimeNow = NtpDateTime.Instance.UtcNow;
                NtpUtcValue.text = string.Format(
                    "NTP UTC value: {0} {1}",
                    ntpUtcDateTimeNow.ToShortDateString(),
                    ntpUtcDateTimeNow.ToLongTimeString()
                );
            }
            else
            {
                NtpValue.text = "Synchronization in progress...";
                NtpUtcValue.text = "Synchronization in progress...";
            }
        }
    }
}
