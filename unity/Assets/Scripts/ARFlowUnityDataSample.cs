using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ARFlow;
using Google.Protobuf;
using UnityEngine.UI;
using TMPro;
using Unity.Profiling;

public class ARFlowUnityDataSample : MonoBehaviour
{
    private bool _enabled;

    public TMP_InputField addressInput;
    public Button connectButton;
    public Button startPauseButton;

    private ARFlowClient _client;
    private Vector2Int _sampleSize;
    private System.Random _rnd = new System.Random();
    private Camera _captureCamera;
    private Shader _depthShader;
    private Texture2D _colorTexture;
    private RenderTexture _colorRenderTexture;
    private Texture2D _depthTexture;
    private RenderTexture _depthRenderTexture;

    // Start is called before the first frame update
    void Start()
    {
        string serverURL = addressInput.text;
        _client = new ARFlowClient($"http://{serverURL}");

        connectButton.onClick.AddListener(OnConnectButtonClick);
        startPauseButton.onClick.AddListener(OnStartPauseButtonClick);

        _captureCamera = Camera.main;
        _depthShader = Shader.Find("Custom/CameraDepth");

        _colorRenderTexture = new RenderTexture(_captureCamera.pixelWidth, _captureCamera.pixelHeight, 24, RenderTextureFormat.ARGB32);
        _depthRenderTexture = new RenderTexture(_captureCamera.pixelWidth, _captureCamera.pixelHeight, 24, RenderTextureFormat.R8);
        _colorTexture = new Texture2D(_captureCamera.pixelWidth, _captureCamera.pixelHeight, TextureFormat.RGB24, false);
        _depthTexture = new Texture2D(_captureCamera.pixelWidth, _captureCamera.pixelHeight, TextureFormat.RFloat, false);

        Application.targetFrameRate = 60;
    }

    private void OnConnectButtonClick()
    {
        Matrix4x4 projectionMatrix = _captureCamera.projectionMatrix;
        int screenWidth = Screen.width;
        int screenHeight = Screen.height;
        _sampleSize.x = screenWidth;
        _sampleSize.y = screenHeight;

        // Calculate focal length and principal points in pixels
        float focalLengthX = projectionMatrix[0, 0] * screenWidth;
        float focalLengthY = projectionMatrix[1, 1] * screenHeight;
        float principalPointX = projectionMatrix[0, 2] * screenWidth;
        float principalPointY = projectionMatrix[1, 2] * screenHeight;

        // _sampleSize = new Vector2Int(screenWidth, screenHeight);

        _client.Connect(new RegisterRequest()
        {
            DeviceName = "UnityDataTestbed",
            CameraIntrinsics = new RegisterRequest.Types.CameraIntrinsics()
            {
                FocalLengthX = focalLengthX,
                FocalLengthY = focalLengthY,
                ResolutionX = screenWidth,
                ResolutionY = screenHeight,
                PrincipalPointX = principalPointX,
                PrincipalPointY = principalPointY
            },
            CameraColor = new RegisterRequest.Types.CameraColor()
            {
                Enabled = true,
                DataType = "RGB24",
                ResizeFactorX = 1.0f,
                ResizeFactorY = 1.0f,
            },
            CameraDepth = new RegisterRequest.Types.CameraDepth()
            {
                Enabled = true,
                DataType = "f32",
                ResolutionX = screenWidth,
                ResolutionY = screenHeight
            },
            CameraTransform = new RegisterRequest.Types.CameraTransform()
            {
                Enabled = false
            }
        });
    }

    private void OnStartPauseButtonClick()
    {
        _enabled = !_enabled;
        startPauseButton.GetComponentInChildren<TMP_Text>().text = _enabled ? "Pause" : "Start";
    }

    private void UploadFrame()
    {
        // Render RGB.
        _captureCamera.targetTexture = _colorRenderTexture;
        _captureCamera.Render();
        RenderTexture.active = _colorRenderTexture;
        _colorTexture.ReadPixels(new Rect(0, 0, _captureCamera.pixelWidth, _captureCamera.pixelHeight), 0, 0, false);
        _colorTexture.Apply();
        var pixelBytes = _colorTexture.GetRawTextureData();

        // Render depth.
        _captureCamera.targetTexture = _depthRenderTexture;
        Shader.SetGlobalFloat("_CameraZeroDis", 0);
        Shader.SetGlobalFloat("_CameraOneDis", 100);
        _captureCamera.RenderWithShader(_depthShader, "");
        RenderTexture.active = _depthRenderTexture;
        _depthTexture.ReadPixels(new Rect(0, 0, _captureCamera.pixelWidth, _captureCamera.pixelHeight), 0, 0, false);
        _depthTexture.Apply();
        var depthBytes = _depthTexture.GetRawTextureData();

        Debug.Log($"pixelBytes length: {pixelBytes.Length}, depthBytes length: {depthBytes.Length}");
        _client.SendFrame(new DataFrameRequest()
        {
            Color = ByteString.CopyFrom(pixelBytes),
            Depth = ByteString.CopyFrom(depthBytes)
        });

        _captureCamera.targetTexture = null;
    }

    // Update is called once per frame
    void Update()
    {
        if (!_enabled) return;
        UploadFrame();
    }
}
