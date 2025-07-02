using System;
using System.Collections.Generic;
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
   
    // Camera image data's manager from the device camera

    public ARCameraManager cameraManager;
 
    // Depth data's manager from the device camera

    public AROcclusionManager occlusionManager;

    public Button connectButton;
    public Button startPauseButton;

    private ARFlowClient _client;
    private ARFlowDataStorage _dataStorage;
    private Vector2Int _sampleSize;
    private bool _enabled = false;
    private bool _enableLocalStorage = true; // Toggle for local storage

    public TMP_InputField ipField;
    public TMP_InputField portField;

    private string _defaultConnection = "http://192.168.1.219:8500";

    // UI elements for storage status
    public TMP_Text storageStatusText; //  Add to show storage info, for futrue verisons, never got to this 
    public Toggle localStorageToggle; // Add to enable/disable storage, for futrue verisons, never got to this 

    // Start is called before the first frame update
    void Start()
    {
        connectButton.onClick.AddListener(OnConnectButtonClick);
        startPauseButton.onClick.AddListener(OnStartPauseButtonClick);

        // Initialize local storage (binary only)
        _dataStorage = new ARFlowDataStorage(maxStoredFiles: 2000);

        // Setup storage toggle, for futrue verisons, never got to this 
        if (localStorageToggle != null)
        {
            localStorageToggle.isOn = _enableLocalStorage;
            localStorageToggle.onValueChanged.AddListener((value) => _enableLocalStorage = value);
        }

        // Show initial storage info
        UpdateStorageStatusUI();
    }

    bool validIP(string ipField)
    {
        return Regex.IsMatch(ipField, @"(\d){1,3}\.(\d){1,3}\.(\d){1,3}\.(\d){1,3}");
    }

    bool validPort(string portField)
    {
        return Regex.IsMatch(portField, @"(\d){1,5}");
    }


    // Get register request data from camera and send to server.
    // Image and depth info is acquired once to get information for the request, and is disposed afterwards.

    private void OnConnectButtonClick()
    {
        var serverURL = _defaultConnection;
        if (validIP(ipField.text) && validPort(portField.text))
        {
            serverURL = "http://" + ipField.text + ":" + portField.text;
        }
        serverURL = Regex.Replace(serverURL, @"\s+", "");
        // destructor dispose old client when we reconnect
        _client = new ARFlowClient(serverURL);

        try
        {
            cameraManager.TryGetIntrinsics(out var k);
            cameraManager.TryAcquireLatestCpuImage(out var colorImage);
            occlusionManager.TryAcquireEnvironmentDepthCpuImage(out var depthImage);

            // Store dimensions before disposing
            var depthDimensions = depthImage.dimensions;
            var colorDimensions = colorImage.dimensions;

            _sampleSize = depthDimensions;

            Debug.Log($"Color dimensions: {colorDimensions.x}x{colorDimensions.y}");
            Debug.Log($"Depth dimensions: {depthDimensions.x}x{depthDimensions.y}");

            var requestData = new RegisterRequest()
            {
                DeviceName = SystemInfo.deviceName,
                CameraIntrinsics = new RegisterRequest.Types.CameraIntrinsics()
                {
                    FocalLengthX = k.focalLength.x,
                    FocalLengthY = k.focalLength.y,
                    ResolutionX = k.resolution.x,
                    ResolutionY = k.resolution.y,
                    PrincipalPointX = k.principalPoint.x,
                    PrincipalPointY = k.principalPoint.y,
                },
                CameraColor = new RegisterRequest.Types.CameraColor()
                {
                    Enabled = true,
                    DataType = "YCbCr420",
                    ResizeFactorX = depthDimensions.x / (float)colorDimensions.x,
                    ResizeFactorY = depthDimensions.y / (float)colorDimensions.y,
                },
                CameraDepth = new RegisterRequest.Types.CameraDepth()
                {
                    Enabled = true,
#if UNITY_ANDROID
                    DataType = "u16", // f32 for iOS, u16 for Android
#endif
#if (UNITY_IOS || UNITY_VISIONOS)
                    DataType = "f32",
#endif
                    ConfidenceFilteringLevel = 0,
                    ResolutionX = depthDimensions.x,  // Use stored dimensions
                    ResolutionY = depthDimensions.y   // Use stored dimensions
                },
                CameraTransform = new RegisterRequest.Types.CameraTransform()
                {
                    Enabled = true
                },
                CameraPointCloud = new RegisterRequest.Types.CameraPointCloud()
                {
                    Enabled = true,
                    DepthUpscaleFactor = 1.0f,
                },
            };

            // Dispose after we've used the dimensions
            colorImage.Dispose();
            depthImage.Dispose();

            _client.Connect(requestData);

            // Optionally start streaming immediately after connecting
            // OnStartPauseButtonClick();
        }
        catch (Exception e)
        {
            Debug.LogError($"Connection failed: {e}");
        }
    }

 
    // On pause, pressing the button changes the _enabled flag to true  (and text display) and data starts sending in Update()
    // On start, pressing the button changes the _enabled flag to false and data stops sending

    private void OnStartPauseButtonClick()
    {
        Debug.Log($"Current framerate: {Application.targetFrameRate}");

        _enabled = !_enabled;
        startPauseButton.GetComponentInChildren<TMP_Text>().text = _enabled ? "Pause" : "Start";

        if (!_enabled)
        {
            // When stopping, update storage info
            UpdateStorageStatusUI();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!_enabled) return;
        UploadFrame();
    }


    // Get color image and depth information, and copy camera's transform from float to bytes. 
    // This data is sent over the server and optionally saved locally.

    private void UploadFrame()
    {
        var colorImage = new XRYCbCrColorImage(cameraManager, _sampleSize);
        var depthImage = new XRConfidenceFilteredDepthImage(occlusionManager, 0);

        const int transformLength = 3 * 4 * sizeof(float);
        var m = Camera.main!.transform.localToWorldMatrix;
        var cameraTransformBytes = new byte[transformLength];

        Buffer.BlockCopy(new[]
        {
            m.m00, m.m01, m.m02, m.m03,
            m.m10, m.m11, m.m12, m.m13,
            m.m20, m.m21, m.m22, m.m23
        }, 0, cameraTransformBytes, 0, transformLength);

        var frameData = new DataFrameRequest()
        {
            Color = ByteString.CopyFrom(colorImage.Encode()),
            Depth = ByteString.CopyFrom(depthImage.Encode()),
            Transform = ByteString.CopyFrom(cameraTransformBytes)
        };

        // Send to server
        _client.SendFrame(frameData);

        // Save locally if enabled
        if (_enableLocalStorage && _dataStorage != null)
        {
            SaveFrameLocally(frameData);
        }

        colorImage.Dispose();
        depthImage.Dispose();
    }

  
    // Save frame data to local storage asynchronously

    private async void SaveFrameLocally(DataFrameRequest frameData)
    {
        try
        {
            // Create metadata for the frame
            var metadata = new Dictionary<string, object>
            {
                { "timestamp", DateTime.Now.ToString("O") },
                { "frameNumber", Time.frameCount },
                { "deviceName", SystemInfo.deviceName },
                { "sessionId", frameData.Uid ?? "unknown" },
                { "colorSize", frameData.Color?.Length ?? 0 },
                { "depthSize", frameData.Depth?.Length ?? 0 },
                { "transformSize", frameData.Transform?.Length ?? 0 }
            };

            // Store the frame
            var filePath = await _dataStorage.StoreFrameAsync(frameData, metadata);

            if (!string.IsNullOrEmpty(filePath))
            {
                Debug.Log($"Frame saved locally: {filePath}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save frame locally: {e.Message}");
        }
    }

 
    // Update storage status UI

    private void UpdateStorageStatusUI()
    {
        if (_dataStorage != null && storageStatusText != null)
        {
            var info = _dataStorage.GetStorageInfo();
            var sizeInMB = info.totalSizeBytes / (1024f * 1024f);
            storageStatusText.text = $"Storage: {info.fileCount} files, {sizeInMB:F2} MB";
        }
    }

    // Get list of stored frames for playback
 
    public List<string> GetStoredFrames()
    {
        return _dataStorage?.GetStoredFrames() ?? new List<string>();
    }


    // Clear all stored frames

    public void ClearStoredFrames()
    {
        _dataStorage?.ClearStorage();
        UpdateStorageStatusUI();
    }

   
    // Load a specific frame from storage

    public async void LoadFrame(string filePath)
    {
        if (_dataStorage != null)
        {
            var (frameData, metadata) = await _dataStorage.LoadFrameAsync(filePath);
            if (frameData != null)
            {
                Debug.Log($"Loaded frame from {filePath}");
                // Process loaded frame as needed
            }
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        // Stop recording when app is paused
        if (pauseStatus && _enabled)
        {
            OnStartPauseButtonClick();
        }
    }

    private void OnDestroy()
    {
        // Ensure we stop recording
        _enabled = false;
    }
}