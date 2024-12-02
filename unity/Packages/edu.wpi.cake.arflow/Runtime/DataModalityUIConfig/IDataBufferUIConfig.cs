using UnityEngine;
using TMPro;
using System;

namespace CakeLab.ARFlow.DataModalityUIConfig
{
    public interface IDataModalityUIConfig : IDisposable
    {
        /// <summary>
        /// Gets the buffer size through TMP_Text to update preriodically.
        /// </summary>
        /// <returns></returns>
        public TMP_InputField GetDelayField();

        public void TurnOnConfig();
        public void TurnOffConfig();
    }

}

