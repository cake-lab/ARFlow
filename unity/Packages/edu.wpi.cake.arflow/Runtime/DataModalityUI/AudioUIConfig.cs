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
    public class AudioUIConfig : BaseDataModalityUIConfig
    {
        // toggle for buffer is a special case - toggling turns config off and on
        private GameObject toggle;

        private List<GameObject> m_UIConfigElements = new();
        // Configs
        private const string MODALITY_NAME = "Audio";

        private const string SAMPLE_RATE_NAME = "Sample Rate";
        private TMP_InputField sampleRateField;
        private const string DEFAULT_SAMPLE_RATE = "16000";

        private const string FRAME_LENGTH_NAME = "Frame Length";
        private TMP_InputField frameLengthField;
        private const string DEFAULT_FRAME_LENGTH = "512";

        private TMP_InputField delayField;

        private bool m_IsBufferAvailable;

        private bool m_IsModalityActive = false;
        public override bool isModalityActive => m_IsModalityActive;
        private IClock m_Clock;
        public AudioUIConfig(IClock clock, bool isBufferAvailable = true)
        {
            m_IsBufferAvailable = isBufferAvailable;
            m_Clock = clock;
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

            //Sample Rate
            InstantiateGameObject.InstantiateInputField(parent, prefabs.textFieldPrefab, SAMPLE_RATE_NAME, DEFAULT_SAMPLE_RATE, out var sampleRateObject, out sampleRateField);
            sampleRateField.contentType = TMP_InputField.ContentType.IntegerNumber;
            m_UIConfigElements.Add(sampleRateObject);

            //Frame length
            InstantiateGameObject.InstantiateInputField(parent, prefabs.textFieldPrefab, FRAME_LENGTH_NAME, DEFAULT_FRAME_LENGTH, out var frameLengthObj, out frameLengthField);
            frameLengthField.contentType = TMP_InputField.ContentType.IntegerNumber;
            m_UIConfigElements.Add(frameLengthObj);

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
        }

        public override void TurnOnConfig()
        {
            foreach (GameObject element in m_UIConfigElements)
            {
                element.SetActive(true);

            }
            m_IsModalityActive = true;
        }

        // /// <summary>
        // /// To attach to toggle
        // /// </summary>
        // /// <param name="isOn">Is toggle on or off</param>
        // private void ToggleConfig(bool isOn)
        // {
        //     if (isOn)
        //     {
        //         TurnOnConfig();
        //     }
        //     else
        //     {
        //         TurnOffConfig();
        //     }
        // }

        /// <summary>
        /// Get the current delay value, set by the user
        /// </summary>
        /// <returns></returns>
        public override float GetDelay()
        {
            return float.Parse(delayField.text);

        }
        public AudioBuffer GetBufferFromConfig()
        {
            return new AudioBuffer(m_Clock, int.Parse(sampleRateField.text), int.Parse(frameLengthField.text));
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