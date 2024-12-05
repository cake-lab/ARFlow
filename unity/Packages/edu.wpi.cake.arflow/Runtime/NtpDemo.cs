using UnityEngine;

namespace CakeLab.ARFlow
{
    using Utilities;

    public class NtpDemo : MonoBehaviour
    {
        private NtpDateTimeManager ntpManager;

        private void Start()
        {
            ntpManager = FindFirstObjectByType<NtpDateTimeManager>();

            if (ntpManager == null)
            {
                InternalDebug.LogError("NtpDateTimeManager not found in the scene.");
            }
        }

        private void Update()
        {
            if (ntpManager != null && ntpManager.IsSynchronized)
            {
                InternalDebug.Log($"Synchronized Time: {ntpManager.Now}");
                InternalDebug.Log($"Synchronized UtcTime: {ntpManager.UtcNow}");
            }
            else
            {
                InternalDebug.Log("Time not yet synchronized.");
            }
        }
    }
}
