using System;
using System.Collections.Generic;
using CakeLab.ARFlow.DataBuffers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CakeLab.ARFlow.DataModalityUIConfig
{
    using Clock;
    using static DefaultValues;

    public class AudioUIConfig : IDataModalityUIConfig
    {
        // toggle for buffer is a special case - toggling turns config off and on
        private GameObject toggle;

        private List<GameObject> uiConfigElements = new();

        // Configs
        private const string MODALITY_NAME = "Audio";
        private TMP_InputField bufferSizeField;

        private const string SAMPLE_RATE_NAME = "Sample Rate";
        private TMP_InputField sampleRateField;
        private const string DEFAULT_SAMPLE_RATE = "16000";

        private const string FRAME_LENGTH_NAME = "Frame Length";
        private TMP_InputField frameLengthField;
        private const string DEFAULT_FRAME_LENGTH = "512";

        private TMP_InputField delayField;

        public AudioUIConfig(
            GameObject parent,
            DataModalityUIConfigPrefabs prefabs,
            Action<bool> onToggleModality,
            bool isBufferAvailable = false
        )
        {
            //Name
            InstantiateGameObject.InstantiateHeaderText(
                parent,
                prefabs.headerTextPrefab,
                MODALITY_NAME,
                out var bufferNameObject
            );
            if (!isBufferAvailable)
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
                onToggleModality,
                out toggle,
                out _
            );

            //Buffer Size
            InstantiateGameObject.InstantiateInputField(
                parent,
                prefabs.textFieldPrefab,
                BUFFER_SIZE_NAME,
                DEFAULT_BUFFER_SIZE,
                out var bufferSizeObject,
                out bufferSizeField
            );
            bufferSizeField.contentType = TMP_InputField.ContentType.IntegerNumber;
            uiConfigElements.Add(bufferSizeObject);

            //Sample Rate
            InstantiateGameObject.InstantiateInputField(
                parent,
                prefabs.textFieldPrefab,
                SAMPLE_RATE_NAME,
                DEFAULT_SAMPLE_RATE,
                out var sampleRateObject,
                out sampleRateField
            );
            sampleRateField.contentType = TMP_InputField.ContentType.IntegerNumber;
            uiConfigElements.Add(sampleRateObject);

            //Frame length
            InstantiateGameObject.InstantiateInputField(
                parent,
                prefabs.textFieldPrefab,
                FRAME_LENGTH_NAME,
                DEFAULT_FRAME_LENGTH,
                out var frameLengthObj,
                out frameLengthField
            );
            frameLengthField.contentType = TMP_InputField.ContentType.IntegerNumber;
            uiConfigElements.Add(frameLengthObj);

            //Delay
            InstantiateGameObject.InstantiateInputField(
                parent,
                prefabs.textFieldPrefab,
                DELAY_NAME,
                DELAY_DEFAULT,
                out var delayObject,
                out delayField
            );
            delayField.contentType = TMP_InputField.ContentType.DecimalNumber;
            uiConfigElements.Add(delayObject);
        }

        public void TurnOffConfig()
        {
            foreach (GameObject element in uiConfigElements)
            {
                element.SetActive(false);
            }
            // Keep toggle and name active
            toggle.SetActive(true);
        }

        public void TurnOnConfig()
        {
            foreach (GameObject element in uiConfigElements)
            {
                element.SetActive(true);
            }
        }

        /// <summary>
        /// Get the current delay value, set by the user
        /// </summary>
        /// <returns></returns>
        public float GetDelay()
        {
            return float.Parse(delayField.text);
        }

        public AudioBuffer getBufferFromConfig(IClock clock)
        {
            return new AudioBuffer(
                int.Parse(bufferSizeField.text),
                clock,
                int.Parse(sampleRateField.text),
                int.Parse(frameLengthField.text)
            );
        }

        public void Dispose()
        {
            foreach (GameObject element in uiConfigElements)
            {
                GameObject.Destroy(element);
            }
        }
    }
}
