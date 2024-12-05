using UnityEngine;
using CakeLab.ARFlow.DataBuffers;

using TMPro;
using UnityEngine.UI;

using System.Collections.Generic;
using System;
using UnityEditor;

using UnityEngine.XR.ARFoundation;

using static CakeLab.ARFlow.DataModalityUIConfig.DefaultValues;
using CakeLab.ARFlow.DataModalityUIConfig;
using CakeLab.ARFlow.Clock;

namespace CakeLab.ARFlow.DataModalityUIConfig
{
    /// <inheritdoc/>
    public class PointCloudDetectionUIConfig : BaseDataModalityUIConfig
    {
        // toggle for buffer is a special case - toggling turns config off and on
        private GameObject toggle;

        private List<GameObject> m_UiConfigElements = new();

        // Configs
        private const string MODALITY_NAME = "Point Cloud Detection";
        private TMP_InputField bufferSizeField;
        private TMP_InputField delayField;

        private ARPointCloudManager m_manager;

        private bool m_IsBufferAvailable = true;
        private bool m_IsModalityActive = false;
        public override bool isModalityActive => m_IsModalityActive;
        private IClock m_Clock;
        public PointCloudDetectionUIConfig(ARPointCloudManager manager, IClock clock, bool isBufferAvailable = true)
        {
            m_IsBufferAvailable = isBufferAvailable;
            m_Clock = clock; m_manager = manager;
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

            //Buffer Size
            InstantiateGameObject.InstantiateInputField(parent, prefabs.textFieldPrefab, BUFFER_SIZE_NAME, DEFAULT_BUFFER_SIZE, out var bufferSizeObject, out bufferSizeField);
            bufferSizeField.contentType = TMP_InputField.ContentType.IntegerNumber;
            m_UiConfigElements.Add(bufferSizeObject);

            //Delay
            InstantiateGameObject.InstantiateInputField(parent, prefabs.textFieldPrefab, DELAY_NAME, DELAY_DEFAULT, out var delayObject, out delayField);
            delayField.contentType = TMP_InputField.ContentType.DecimalNumber;
            m_UiConfigElements.Add(delayObject);

            ToggleConfig(m_IsModalityActive);
        }


        public override void TurnOffConfig()
        {

            foreach (GameObject element in m_UiConfigElements)
            {
                element.SetActive(false);
            }
            // Keep toggle and name active

            m_IsModalityActive = false;
            m_manager.enabled = false;

        }

        public override void TurnOnConfig()
        {
            foreach (GameObject element in m_UiConfigElements)
            {
                element.SetActive(true);
            }
            m_IsModalityActive = true;
            m_manager.enabled = true;
        }

        public override void ToggleConfig(bool isOn)
        {
            base.ToggleConfig(isOn);
            m_manager.enabled = isOn;
        }


        /// <summary>
        /// Get the current delay value, set by the user
        /// </summary>
        /// <returns></returns>
        public override float GetDelay()
        {
            return float.Parse(delayField.text);
        }
        public PointCloudDetectionBuffer GetBufferFromConfig()
        {
            //TODO: validate
            return new PointCloudDetectionBuffer(int.Parse(bufferSizeField.text), m_manager, m_Clock);
        }

        public override IARFrameBuffer GetGenericBuffer()
        {
            return GetBufferFromConfig();
        }

        public override void Dispose()
        {
            foreach (GameObject element in m_UiConfigElements)
            {
                GameObject.Destroy(element);
            }
        }
    }
}