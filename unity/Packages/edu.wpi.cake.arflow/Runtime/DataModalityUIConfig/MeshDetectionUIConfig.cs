using System;
using System.Collections.Generic;
using CakeLab.ARFlow.DataBuffers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

namespace CakeLab.ARFlow.DataModalityUIConfig
{
    using Utilities;
    using static DefaultValues;

    public class MeshDetectionUIConfig : IDataModalityUIConfig
    {
        // toggle for buffer is a special case - toggling turns config off and on
        private GameObject toggle;

        private List<GameObject> uiConfigElements = new();

        // Configs
        private const string MODALITY_NAME = "Mesh Detection";
        private TMP_InputField bufferSizeField;

        private TMP_InputField delayField;

        public MeshDetectionUIConfig(
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

        public MeshDetectionBuffer getBufferFromConfig(
            ARMeshManager meshManager,
            NtpDateTimeManager ntpManager
        )
        {
            return new MeshDetectionBuffer(
                int.Parse(bufferSizeField.text),
                meshManager,
                ntpManager
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

