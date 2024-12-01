using UnityEngine;
using CakeLab.ARFlow.DataBuffers;

using TMPro;
using UnityEngine.UI;

using System.Collections.Generic;
using UnityEditor;

using UnityEngine.XR.ARFoundation;

namespace CakeLab.ARFlow.DataBuffers.DataBuffersUIConfig
{
    public class ColorBufferUIConfig : IDataBufferUIConfig
    {
        private List<GameObject> uiElements = new();

        // Configs
        private const string BUFFER_NAME = "Camera Buffer";
        private const string BUFFER_SIZE_NAME = "Buffer Size";
        private const string DEFAULT_BUFFER_SIZE = "60";
        private TMP_InputField bufferSizeInputField;
        private const string DELAY_NAME = "Delay (s)";
        private const string DELAY_DEFAULT = "0.01";
        private TMP_InputField delayField;

        public ColorBufferUIConfig(GameObject parent, GameObject textPrefab, GameObject textFieldPrefab, GameObject dropDownPrefab, GameObject togglePrefab)
        {
            //TODO: add check for ARCameraManager - if null then dont instantiate UI

            //Name
            GameObject bufferNameObject = GameObject.Instantiate(textPrefab, parent.transform);
            bufferNameObject.GetComponent<Text>().text = BUFFER_NAME;

            uiElements.Add(bufferNameObject);

            //Buffer toggle (on or off)
            GameObject bufferToggle = GameObject.Instantiate(togglePrefab, parent.transform);
            bufferNameObject.GetComponent<Text>().text = "Enable buffer";

            uiElements.Add(bufferNameObject);

            //Buffer Size
            GameObject bufferSizeObject = GameObject.Instantiate(textFieldPrefab, parent.transform);
            bufferSizeObject.GetComponent<Text>().text = BUFFER_SIZE_NAME;

            GameObject bufferSizeTextField = bufferSizeObject.transform.GetChild(0).gameObject;
            bufferSizeInputField = bufferSizeTextField.GetComponent<TMP_InputField>();
            bufferSizeInputField.contentType = TMP_InputField.ContentType.IntegerNumber;
            bufferSizeInputField.text = DEFAULT_BUFFER_SIZE;
            uiElements.Add(bufferSizeObject);

            //Delay
            GameObject delayObject = GameObject.Instantiate(textFieldPrefab, parent.transform);
            delayObject.GetComponent<Text>().text = DELAY_NAME;

            GameObject inputFieldObj = bufferSizeObject.transform.GetChild(0).gameObject;
            delayField = inputFieldObj.GetComponent<TMP_InputField>();
            delayField.contentType = TMP_InputField.ContentType.DecimalNumber;
            delayField.text = DELAY_DEFAULT;
            uiElements.Add(delayObject);
        }

        // public void createBufferUI(GameObject parent, GameObject textPrefab, GameObject textFieldPrefab, GameObject dropDownPrefab, GameObject togglePrefab)
        // {


        // }



        public void turnOffConfig()
        {
            foreach (GameObject element in uiElements)
            {
                element.SetActive(false);
            }
        }

        public void turnOnConfig()
        {
            foreach (GameObject element in uiElements)
            {
                element.SetActive(true);
            }
        }

        /// <summary>
        /// Gets the delay through TMP_Text to update preriodically. 
        /// </summary>
        /// <returns></returns>
        public TMP_InputField getDelayField()
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
            foreach (GameObject element in uiElements)
            {
                GameObject.Destroy(element);
            }
        }
    }
}