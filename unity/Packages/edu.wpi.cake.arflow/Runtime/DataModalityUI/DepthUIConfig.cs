using UnityEngine;
using CakeLab.ARFlow.DataBuffers;

using TMPro;
using UnityEngine.UI;

using System.Collections.Generic;
using System;
using UnityEditor;

using UnityEngine.XR.ARFoundation;

using static CakeLab.ARFlow.DataModalityUIConfig.DefaultValues;
using CakeLab.ARFlow.Clock;
using CakeLab.ARFlow.DataModalityUIConfig;

namespace CakeLab.ARFlow.DataModalityUIConfig
{
    /// <inheritdoc/>
    public class DepthUIConfig : BaseDataModalityUIConfig
    {
        // toggle for buffer is a special case - toggling turns config off and on
        private GameObject toggle;

        private List<GameObject> m_UIConfigElements = new();
        // Configs
        private const string MODALITY_NAME = "Camera Depth";
        private TMP_InputField delayField;

        private AROcclusionManager m_Manager;

        private bool m_IsBufferAvailable;
        private bool m_IsModalityActive = false;
        public override bool isModalityActive => m_IsModalityActive;
        private IClock m_Clock;
        public DepthUIConfig(AROcclusionManager manager, IClock clock, bool isBufferAvailable = true)
        {
            m_IsBufferAvailable = isBufferAvailable;
            m_Clock = clock; m_Manager = manager;
        }

        public override void InitializeConfig(GameObject parent, DataModalityUIConfigPrefabs prefabs, Action<bool> onToggleModality)
        {
            //Name
            InstantiateGameObject.InstantiateHeaderText(parent, prefabs.headerTextPrefab, MODALITY_NAME, out var bufferNameObject);
            if (!m_IsBufferAvailable)
            {
                //If buffer is not available, don't show the rest of the UI
                GameObject bufferNotAvailableText = GameObject.Instantiate(prefabs.bodyTextPrefab, parent.transform);
                bufferNotAvailableText.GetComponent<Text>().text = UNAVAILABLE_MESSAGE;
                return;
            }

            //Buffer toggle (on or off)
            InstantiateGameObject.InstantiateToggle(parent, prefabs.togglePrefab, ENABLE_NAME, new Action<bool>[] { onToggleModality, ToggleConfig }, out toggle, out _);

            //Delay
            InstantiateGameObject.InstantiateInputField(parent, prefabs.textFieldPrefab, DELAY_NAME, DELAY_DEFAULT, out var delayObject, out delayField);
            delayField.contentType = TMP_InputField.ContentType.DecimalNumber;
            m_UIConfigElements.Add(delayObject);

            ToggleConfig(m_IsModalityActive);
        }


        public override void TurnOffConfig()
        {

            foreach (GameObject element in m_UIConfigElements)
            {
                element.SetActive(false);
            }
            // Keep toggle and name active

            m_IsModalityActive = false;
            // if (m_Manager) m_Manager.enabled = false;

        }

        public override void TurnOnConfig()
        {
            foreach (GameObject element in m_UIConfigElements)
            {
                element.SetActive(true);
            }
            m_IsModalityActive = true;
            // if (m_Manager) m_Manager.enabled = true;
        }


        /// <summary>
        /// Get the current delay value, set by the user
        /// </summary>
        /// <returns></returns>
        public override float GetDelay()
        {
            return float.Parse(delayField.text);
        }
        public DepthBuffer GetBufferFromConfig()
        {
            return new DepthBuffer(m_Manager, m_Clock);
        }

        public override IARFrameBuffer GetGenericBuffer()
        {
            return GetBufferFromConfig();
        }

        public override void Dispose()
        {
            foreach (GameObject element in m_UIConfigElements)
            {
                GameObject.Destroy(element);
            }
        }
    }
}