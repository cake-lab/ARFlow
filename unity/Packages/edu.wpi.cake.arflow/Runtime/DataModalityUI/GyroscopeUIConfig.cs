using System;
using System.Collections.Generic;
using CakeLab.ARFlow.Clock;
using CakeLab.ARFlow.DataBuffers;
using CakeLab.ARFlow.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static CakeLab.ARFlow.DataModalityUIConfig.DefaultValues;

namespace CakeLab.ARFlow.DataModalityUIConfig
{
    public class GyroscopeUIConfig : BaseDataModalityUIConfig
    {
        // toggle for buffer is a special case - toggling turns config off and on
        private GameObject toggle;

        private List<GameObject> m_UIConfigElements = new();

        // Configs
        private const string MODALITY_NAME = "Gyroscope";
        private const string SAMPLING_RATE_NAME = "Sampling Rate (Hz)";
        private TMP_InputField samplingRateHzField;
        private const string DEFAULT_SAMPLING_RATE_HZ = "125";

        private TMP_InputField sendIntervalField;

        private bool m_IsBufferAvailable;
        private bool m_IsModalityActive = false;
        public override bool isModalityActive => m_IsModalityActive;
        private IClock m_Clock;

        public GyroscopeUIConfig(IClock clock, bool isBufferAvailable = true)
        {
            m_IsBufferAvailable = isBufferAvailable;
            m_Clock = clock;
        }

        public override void InitializeConfig(
            GameObject parent,
            DataModalityUIConfigPrefabs prefabs,
            Action<bool> onToggleModality
        )
        {
            //Name
            InstantiateGameObject.InstantiateHeaderText(
                parent,
                prefabs.headerTextPrefab,
                MODALITY_NAME,
                out var bufferNameObject
            );
            if (!m_IsBufferAvailable)
            {
                //If buffer is not available, don't show the rest of the UI
                GameObject bufferNotAvailableText = GameObject.Instantiate(
                    prefabs.bodyTextPrefab,
                    parent.transform
                );
                bufferNotAvailableText.GetComponent<Text>().text = UNAVAILABLE_MESSAGE;
                return;
            }

            //Buffer toggle (on or off)
            InstantiateGameObject.InstantiateToggle(
                parent,
                prefabs.togglePrefab,
                ENABLE_NAME,
                new Action<bool>[] { onToggleModality, ToggleConfig },
                out toggle,
                out _
            );

            InstantiateGameObject.InstantiateInputField(
                parent,
                prefabs.textFieldPrefab,
                SAMPLING_RATE_NAME,
                DEFAULT_SAMPLING_RATE_HZ,
                out var samplingRateHzObj,
                out samplingRateHzField
            );
            samplingRateHzField.contentType = TMP_InputField.ContentType.DecimalNumber;
            m_UIConfigElements.Add(samplingRateHzObj);

            InstantiateGameObject.InstantiateInputField(
                parent,
                prefabs.textFieldPrefab,
                SEND_INTERVAL_NAME,
                LIGHT_MODALITIES_SEND_INTERVAL_DEFAULT,
                out var sendIntervalObject,
                out sendIntervalField
            );
            sendIntervalField.contentType = TMP_InputField.ContentType.DecimalNumber;
            m_UIConfigElements.Add(sendIntervalObject);

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

        /// <summary>
        /// Get the current send interval value, set by the user
        /// </summary>
        /// <returns></returns>
        public override float GetSendIntervalS()
        {
            return float.Parse(sendIntervalField.text);
        }

        public GyroscopeBuffer GetBufferFromConfig()
        {
            InternalDebug.Log("Gyroscope buffer created");
            return new GyroscopeBuffer(m_Clock, float.Parse(samplingRateHzField.text));
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
