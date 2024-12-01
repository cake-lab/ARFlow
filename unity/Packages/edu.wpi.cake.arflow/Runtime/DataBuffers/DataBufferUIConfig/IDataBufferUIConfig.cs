using UnityEngine;
using TMPro;
using System;

namespace CakeLab.ARFlow.DataBuffers.DataBuffersUIConfig
{
    public interface IDataBufferUIConfig : IDisposable
    {
        /// <summary>
        /// Gets the buffer size through TMP_Text to update preriodically.
        /// </summary>
        /// <returns></returns>
        public TMP_InputField getDelayField();

        public void turnOnConfig();
        public void turnOffConfig();
    }

}

