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

    private ARFlowClient _client;
    private Vector2Int _sampleSize;
    private bool _enabled = false;

    public TMP_InputField ipField;
    public TMP_InputField portField;

    // Start is called before the first frame update
    void Start()
    {
        const string initServerURL = "192.168.1.219";
        const string initPort= "8500";
        ipField.text = initServerURL;
        portField.text = initPort;

        connectButton.onClick.AddListener(OnConnectButtonClick);
        startPauseButton.onClick.AddListener(OnStartPauseButtonClick);

        // OnConnectButtonClick();

        // The following suppose to limit the fps to 30, but it doesn't work.
        // QualitySettings.vSyncCount = 0;
        // Application.targetFrameRate = 30;
    }

    /// <summary>
    /// Get register request data from camera and send to server.
    /// Image and depth info is acquired once to get information for the request, and is disposed afterwards.
    /// </summary>
    private void OnConnectButtonClick()
    {
        var serverURL = "http://" + ipField.text + ":" +portField;
        serverURL = Regex.Replace(serverURL, @"\s+", "");
        // destructor dispose old client when we reconnect
        _client = new ARFlowClient(serverURL);

        try
        {
            cameraManager.TryGetIntrinsics(out var k);
            cameraManager.TryAcquireLatestCpuImage(out var colorImage);
            occlusionManager.TryAcquireEnvironmentDepthCpuImage(out var depthImage);

            _sampleSize = depthImage.dimensions;

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
                    ResizeFactorX = depthImage.dimensions.x / (float)colorImage.dimensions.x,
                    ResizeFactorY = depthImage.dimensions.y / (float)colorImage.dimensions.y,
                },
                CameraDepth = new RegisterRequest.Types.CameraDepth()
                {
                    Enabled = true,
                    DataType = "u16", // f32 for iOS, u16 for Android
                    ConfidenceFilteringLevel = 0,
                    ResolutionX = depthImage.dimensions.x,
                    ResolutionY = depthImage.dimensions.y
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
            colorImage.Dispose();
            depthImage.Dispose();

            _client.Connect(requestData);

            // OnStartPauseButtonClick();
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


        _client.SendFrame(new DataFrameRequest()
        {
            Color = ByteString.CopyFrom(colorImage.Encode()),
            Depth = ByteString.CopyFrom(depthImage.Encode()),
            Transform = ByteString.CopyFrom(cameraTransformBytes)
        });

        colorImage.Dispose();
        depthImage.Dispose();
    }
}
