using System;
using System.Text.RegularExpressions;
using ARFlow;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using System.Collections.Generic;
using System.Collections;

public class ARFlowDeviceSample : MonoBehaviour
{
    /// <summary>
    /// Camera image data's manager from the device camera
    /// </summary>
    public ARCameraManager cameraManager;
    /// <summary>
    /// Depth data's manager from the device camera
    /// </summary>
    public AROcclusionManager occlusionManager;

    /// <summary>
    /// Plane detection
    /// </summary>
    public ARPlaneManager planeManager;

    /// <summary>
    /// Plane detection
    /// </summary>
    public ARMeshManager meshManager;

    public Button connectButton;
    public Button startPauseButton;

    private bool _enabled = false;

    private ARFlowClientManager _clientManager;

    public TMP_InputField ipField;
    public TMP_InputField portField;

    public GameObject OptionsContainer;

    private Dictionary<string, GameObject> _optionObjects = new Dictionary<string, GameObject>();

    private string _defaultConnection = "http://192.168.1.219:8500";

    // Start is called before the first frame update
    void Start()
    {
        connectButton.onClick.AddListener(OnConnectButtonClick);
        startPauseButton.onClick.AddListener(OnStartPauseButtonClick);
        _clientManager = new ARFlowClientManager(
            cameraManager: cameraManager,
            occlusionManager: occlusionManager,
            planeManager: planeManager,
            meshManager: meshManager);

        addModalityOptionsToConfig();

        // OnConnectButtonClick();

        // The following suppose to limit the fps to 30, but it doesn't work.
        // QualitySettings.vSyncCount = 0;
        // Application.targetFrameRate = 30;
    }

    void addModalityOptionsToConfig()
    {
        // Get first child, WITH THE ASSUMPTION that it's a checkbox
        GameObject firstChild = OptionsContainer.transform.GetChild(0).gameObject;

        foreach (string modality in ARFlowClientManager.MODALITIES)
        {
            GameObject newOption = Instantiate(
                firstChild, 
                parent: OptionsContainer.transform
            );
            newOption.GetComponent<Text>().text = splitByCapital(modality);

            _optionObjects.Add( modality, newOption );
        }
    }

    string splitByCapital(string s)
    {
        return Regex.Replace(s, "([a-z])([A-Z])", "$1 $2");
    }

    Dictionary<string, bool> modalityOptions()
    {
        Dictionary<string, bool> res = new Dictionary<string, bool>();
        foreach (var option in  _optionObjects)
        {
            var optionName = option.Key;
            var optionObject = option.Value;

            var slider = optionObject.transform.Find("Slider");
            if (slider != null)
            {
                var sliderVal = slider.GetComponent<Slider>().value;
                res.Add(optionName, sliderVal != 0);
            }
        }

        return res;
    }

    bool validIP (string ipField)
    {
        return Regex.IsMatch(ipField, @"(\d){1,3}\.(\d){1,3}\.(\d){1,3}\.(\d){1,3}");
    }

    bool validPort(string portField)
    {
        return Regex.IsMatch(portField, @"(\d){1,5}");
    }

    /// <summary>
    /// Get register request data from camera and send to server.
    /// Image and depth info is acquired once to get information for the request, and is disposed afterwards.
    /// </summary>
    private void OnConnectButtonClick()
    {
        var serverURL = _defaultConnection;
        if (validIP(ipField.text) && validPort(portField.text))
        {
            serverURL = "http://" + ipField.text + ":" + portField.text;
        }
        //var modalities = modalityOptions();
        //prettyPrintDictionary(modalities);

        _clientManager.Connect(serverURL, modalityOptions());
    }

    public static void prettyPrintDictionary(Dictionary<string, bool> dict)
    {
        string log = "";
        foreach (var kvp in dict)
        {
            //textBox3.Text += ("Key = {0}, Value = {1}", kvp.Key, kvp.Value);
            log += string.Format("Key = {0}, Value = {1}", kvp.Key, kvp.Value);
        }
        Debug.Log(log);
    }

    /// <summary>
    /// On pause, pressing the button changes the _enabled flag to true  (and text display) and data starts sending in Update()
    /// On start, pressing the button changes the _enabled flag to false and data stops sending
    /// </summary>
    private void OnStartPauseButtonClick()
    {
        Debug.Log($"Current framerate: {Application.targetFrameRate}");

        if (enabled)
        {
            _clientManager.startDataStreaming();
        }
        else
        {
            _clientManager.stopDataStreaming();
        }

        _enabled = !_enabled;
        startPauseButton.GetComponentInChildren<TMP_Text>().text = _enabled ? "Pause" : "Start";
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (!_enabled) return;
        UploadFrame();
    }

    /// <summary>
    /// Get color image and depth information, and copy camera's transform from float to bytes. 
    /// This data is sent over the server.
    /// </summary>
    private void UploadFrame()
    {
        _clientManager.GetAndSendFrame();
    }

}
