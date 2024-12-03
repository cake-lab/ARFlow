using UnityEngine;
using TMPro;
using System;

namespace CakeLab.ARFlow.DataModalityUIConfig
{
    public interface IDataModalityUIConfig : IDisposable
    {
        public float GetDelay();
        public void TurnOnConfig();
        public void TurnOffConfig();
    }

}

