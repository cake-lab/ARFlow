using System;
using System.Text.RegularExpressions;
using ARFlow;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

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

    public Button connectButton;
    public Button startPauseButton;

    private bool _enabled = false;

    private ARFlowClientManager _clientManager;

    public TMP_InputField ipField;
    public TMP_InputField portField;

    private string _defaultConnection = "http://192.168.1.219:8500";

    // Start is called before the first frame update
    void Start()
    {
        connectButton.onClick.AddListener(OnConnectButtonClick);
        startPauseButton.onClick.AddListener(OnStartPauseButtonClick);
        _clientManager = new ARFlowClientManager(cameraManager, occlusionManager);

        // OnConnectButtonClick();

        // The following suppose to limit the fps to 30, but it doesn't work.
        // QualitySettings.vSyncCount = 0;
        // Application.targetFrameRate = 30;
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
        _clientManager.Connect(serverURL);
    }

    /// <summary>
    /// On pause, pressing the button changes the _enabled flag to true  (and text display) and data starts sending in Update()
    /// On start, pressing the button changes the _enabled flag to false and data stops sending
    /// </summary>
    private void OnStartPauseButtonClick()
    {
        Debug.Log($"Current framerate: {Application.targetFrameRate}");

        _enabled = !_enabled;
        startPauseButton.GetComponentInChildren<TMP_Text>().text = _enabled ? "Pause" : "Start";
    }

    // Update is called once per frame
    void Update()
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
