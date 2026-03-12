using System;
using System.Text.RegularExpressions;
using ARFlow;
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

    private ARFlowClient _client;
    private Vector2Int _sampleSize;
    private bool _enabled = false;

    public TMP_InputField ipField;
    public TMP_InputField portField;

    private string _defaultConnection = "http://192.168.1.219:8500";

    // Start is called before the first frame update
    void Start()
    {
        connectButton.onClick.AddListener(OnConnectButtonClick);
        startPauseButton.onClick.AddListener(OnStartPauseButtonClick);

        // OnConnectButtonClick();

        // The following suppose to limit the fps to 30, but it doesn't work.
        // QualitySettings.vSyncCount = 0;
        // Application.targetFrameRate = 30;
    }

    bool validIP(string ipField)
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
            serverURL = "ws://" + ipField.text + ":" + portField.text;
        }
        else
        {
            serverURL = serverURL.Replace("http", "ws");
        }
        serverURL = Regex.Replace(serverURL, @"\s+", "");
        
        _client = new ARFlowClient(serverURL);
        
        // Listen to successful connection to enable data sending
        _client.OnSessionConnected += (sessionId) => 
        {
            _enabled = true;
            // Optionally update UI here
        };

        try
        {
            cameraManager.TryGetIntrinsics(out var k);
            cameraManager.TryAcquireLatestCpuImage(out var colorImage);
            occlusionManager.TryAcquireEnvironmentDepthCpuImage(out var depthImage);

            _sampleSize = depthImage.dimensions;

            var requestData = new CreateSessionRequestMsg()
            {
                Device = new DeviceMsg()
                {
                    DeviceId = "test-unity-client-0.3.0",
                    DeviceName = SystemInfo.deviceName,
                    OsVersion = SystemInfo.operatingSystem
                },
                SessionMetadata = new SessionMetadataMsg()
                {
                    SavePath = "test_unity_session.rrd"
                }
            };
            
            colorImage.Dispose();
            depthImage.Dispose();

            _client.Connect(requestData);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
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
        // NOTE: ARFlow-0.3.0 Client Refactoring week 6:
        // We defer sending exact ColorFrameMsg from XRYCbCrColorImage parsing until the python server can handle PlaneMsg.
        // The implementation follows Unity's structure:
        // _client.SendColorFrame(new ColorFrameMsg { ... });
        
        colorImage.Dispose();
        depthImage.Dispose();
        
        // Let the client pump its message loop
        _client.Update();
    }
}
