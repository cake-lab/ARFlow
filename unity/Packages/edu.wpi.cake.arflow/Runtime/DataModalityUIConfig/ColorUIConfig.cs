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

namespace CakeLab.ARFlow.DataBuffers.DataModalityUIConfig
{
    public class ColorUIConfig : IDataModalityUIConfig
    {
        // toggle for buffer is a special case - toggling turns config off and on
        private GameObject bufferToggle;

        private List<GameObject> uiConfigElements = new();

        // Configs
        private const string BUFFER_NAME = "Camera Buffer";
        private TMP_InputField bufferSizeInputField;
        private TMP_InputField delayField;

        public ColorUIConfig(GameObject parent, DataBufferUIConfigPrefabs prefabs, Action<bool> onToggleModality, bool isBufferAvailable = false)
        {
            //Name
            GameObject bufferNameObject = GameObject.Instantiate(prefabs.headerTextPrefab, parent.transform);
            bufferNameObject.GetComponent<Text>().text = BUFFER_NAME;

            if (!isBufferAvailable)
            {
                //If buffer is not available, don't show the rest of the UI
                GameObject bufferNotAvailableText = GameObject.Instantiate(prefabs.bodyTextPrefab, parent.transform);
                bufferNotAvailableText.GetComponent<Text>().text = UNAVAILABLE_MESSAGE;

            }

            //Buffer toggle (on or off)
            bufferToggle = GameObject.Instantiate(prefabs.togglePrefab, parent.transform);
            bufferToggle.GetComponent<Text>().text = ENABLE_NAME;

            bufferToggle.GetComponentInChildren<DebugSlider>().onValueChanged.AddListener((float arg) => onToggleModality(arg == 1));

            //Buffer Size
            GameObject bufferSizeObject = GameObject.Instantiate(prefabs.textFieldPrefab, parent.transform);
            bufferSizeObject.GetComponent<Text>().text = BUFFER_SIZE_NAME;

            bufferSizeInputField = bufferSizeObject.GetComponentInChildren<TMP_InputField>();
            bufferSizeInputField.contentType = TMP_InputField.ContentType.IntegerNumber;
            bufferSizeInputField.text = DEFAULT_BUFFER_SIZE;
            uiConfigElements.Add(bufferSizeObject);

            //Delay
            GameObject delayObject = GameObject.Instantiate(prefabs.textFieldPrefab, parent.transform);
            delayObject.GetComponent<Text>().text = DELAY_NAME;

            delayField = delayObject.GetComponentInChildren<TMP_InputField>();
            delayField.contentType = TMP_InputField.ContentType.DecimalNumber;
            delayField.text = DELAY_DEFAULT;
            uiConfigElements.Add(delayObject);
        }


        public void TurnOffConfig()
        {

            foreach (GameObject element in uiConfigElements)
            {
                element.SetActive(false);
            }
            // Keep toggle and name active
            bufferToggle.SetActive(true);
        }

        public void TurnOnConfig()
        {
            foreach (GameObject element in uiConfigElements)
            {
                element.SetActive(true);
            }
        }

        /// <summary>
        /// Gets the delay through TMP_Text to update preriodically. 
        /// </summary>
        /// <returns></returns>
        public TMP_InputField GetDelayField()
        {
            return delayField;
        }
        public ColorBuffer getBufferFromConfig(ARCameraManager cameraManager)
        {
            //TODO: validate
            return new ColorBuffer(int.Parse(bufferSizeInputField.text), cameraManager);
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