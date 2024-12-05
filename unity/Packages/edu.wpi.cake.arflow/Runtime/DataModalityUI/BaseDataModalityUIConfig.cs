using UnityEngine;
using TMPro;
using System;
using CakeLab.ARFlow.DataBuffers;


namespace CakeLab.ARFlow.DataModalityUIConfig
{
    /// <summary>
    /// Data modality's UI configurations. This class spawns UI elements for the modality and controls the modality's life cycle (for the current implementation).
    /// </summary>
    public abstract class BaseDataModalityUIConfig : IDisposable
    {
        public abstract float GetDelay();
        public abstract void TurnOnConfig();
        public abstract void TurnOffConfig();
        public abstract void InitializeConfig(GameObject parent, DataModalityUIConfigPrefabs prefabs, Action<bool> onToggleModality);
        public abstract bool isModalityActive { get; }
        public virtual void ToggleConfig(bool isOn)
        {
            if (isOn)
            {
                TurnOnConfig();
            }
            else
            {
                TurnOffConfig();
            }
        }
        public abstract IARFrameBuffer GetGenericBuffer();
        public abstract void Dispose();
    }
}

