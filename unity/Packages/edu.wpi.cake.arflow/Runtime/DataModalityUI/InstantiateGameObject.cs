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

namespace CakeLab.ARFlow.DataModalityUIConfig
{
    public class InstantiateGameObject
    {
        public static void InstantiateInputField(GameObject parent, GameObject prefab, string labelText, string defaultText, out GameObject inputFieldObject, out TMP_InputField inputField)
        {
            inputFieldObject = GameObject.Instantiate(prefab, parent.transform);
            inputFieldObject.GetComponent<Text>().text = labelText;

            inputField = inputFieldObject.GetComponentInChildren<TMP_InputField>();
            inputField.text = defaultText;
        }

        public static void InstantiateToggle(GameObject parent, GameObject prefab, string labelText, Action<bool>[] onToggleModality, out GameObject toggleObject, out DebugSlider debugSlider)
        {
            toggleObject = GameObject.Instantiate(prefab, parent.transform);
            toggleObject.GetComponent<Text>().text = labelText;
            debugSlider = toggleObject.GetComponentInChildren<DebugSlider>();
            foreach (var action in onToggleModality)
            {
                debugSlider.onValueChanged.AddListener((float arg) => action(arg == 1));
            }
        }

        public static void InstantiateHeaderText(GameObject parent, GameObject prefab, string text, out GameObject headerTextObject)
        {
            headerTextObject = GameObject.Instantiate(prefab, parent.transform);
            headerTextObject.GetComponent<Text>().text = text;
        }

        public static void InstantiateBodyText(GameObject parent, GameObject prefab, string text, out GameObject bodyTextObject)
        {
            bodyTextObject = GameObject.Instantiate(prefab, parent.transform);
            bodyTextObject.GetComponent<Text>().text = text;
        }

        public static void InstantiateDropdown(GameObject parent, GameObject prefab, string labelText, List<string> options, Action<int> onDropdownChanged, out GameObject dropdownObject, out TMP_Dropdown dropdown)
        {
            dropdownObject = GameObject.Instantiate(prefab, parent.transform);
            dropdownObject.GetComponent<Text>().text = labelText;

            dropdown = dropdownObject.GetComponentInChildren<TMP_Dropdown>();
            dropdown.ClearOptions();
            dropdown.AddOptions(options);
            dropdown.onValueChanged.AddListener((i) => onDropdownChanged(i));
        }
    }
}