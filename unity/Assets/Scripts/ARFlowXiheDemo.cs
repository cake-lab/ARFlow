using System;
using System.Collections.Generic;
using Google.Protobuf;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using ARFlow;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.Rendering;

public class ARFlowXiheDemo : MonoBehaviour
{
    public TMP_InputField addressInput;
    public Button connectButton;
    public Button startPauseButton;
    public ARCameraManager cameraManager;
    public AROcclusionManager occlusionManager;
    private ARFlowClient _client;
    private Vector2Int _sampleSize;
    private bool _initialized = false;
    private bool _enabled = false;

    public GameObject objectToPlace;
    public GameObject placementIndicator;
    public ARRaycastManager raycastManager;

    private Pose placementPose;
    private bool placementPoseIsValid = false;

    // Start is called before the first frame update
    void Start()
    {
        connectButton.onClick.AddListener(OnConnectButtonClick);
        startPauseButton.onClick.AddListener(OnStartPauseButtonClick);
    }

    // Update is called once per frame
    void Update()
    {
        if (!_initialized)
        {
            UpdatePlacementPose();
            UpdatePlacementIndicator();

            if (placementPoseIsValid && Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                PlaceObject();
            }
        }
        else
        {
            if (!_enabled) return;
            UploadFrame();
        }
    }

    private void PlaceObject()
    {
        Instantiate(objectToPlace, placementPose.position, placementPose.rotation);
        _initialized = true;
        placementIndicator.SetActive(false);
    }

    private void UpdatePlacementIndicator()
    {
        if (placementPoseIsValid)
        {
            placementIndicator.SetActive(true);
            placementIndicator.transform.SetPositionAndRotation(placementPose.position, placementPose.rotation);
        }
        else
        {
            placementIndicator.SetActive(false);
        }
    }

    private void UpdatePlacementPose()
    {
        print(Camera.current);
        var screenCenter = Camera.current.ViewportToScreenPoint(new Vector3(0.5f, 0.5f));
        var hits = new List<ARRaycastHit>();
        raycastManager.Raycast(screenCenter, hits, TrackableType.AllTypes);

        placementPoseIsValid = hits.Count > 0;
        if (placementPoseIsValid)
        {
            placementPose = hits[0].pose;

            var cameraForward = Camera.current.transform.forward;
            var cameraBearing = new Vector3(cameraForward.x, 0, cameraForward.z).normalized;
            placementPose.rotation = Quaternion.LookRotation(cameraBearing);
        }
    }

    private void OnConnectButtonClick()
    {
        if (!_initialized)
        {
            PlaceObject();
        }

        try
        {
            _client = new ARFlowClient(addressInput.text);

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
#if UNITY_ANDROID
                    DataType = "u16", // f32 for iOS, u16 for Android
#endif
#if UNITY_IPHONE
                    DataType = "f32",
#endif
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

    private void OnStartPauseButtonClick()
    {
        Debug.Log($"Current framerate: {Application.targetFrameRate}");

        _enabled = !_enabled;
        startPauseButton.GetComponentInChildren<TMP_Text>().text = _enabled ? "Pause" : "Start";
    }

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


        var responseSHC = _client.SendFrame(new DataFrameRequest()
        {
            Color = ByteString.CopyFrom(colorImage.Encode()),
            Depth = ByteString.CopyFrom(depthImage.Encode()),
            Transform = ByteString.CopyFrom(cameraTransformBytes)
        });

        colorImage.Dispose();
        depthImage.Dispose();

        if (responseSHC.Length > 0)
        {
            var coefficients = new float[27];
            var responseSHCArray = responseSHC.Split(",");
            for (var i = 0; i < 27; i++)
            {
                coefficients[i] = float.Parse(responseSHCArray[i]);
            }

            var bakedProbes = LightmapSettings.lightProbes.bakedProbes;

            for (var i = 0; i < LightmapSettings.lightProbes.count; i++)
            {
                for (var c = 0; c < 3; c++)
                {
                    for (var b = 0; b < 9; b++)
                    {
                        bakedProbes[i][c, b] = coefficients[c * 9 + b];
                    }
                }
            }

            LightmapSettings.lightProbes.bakedProbes = bakedProbes;
        }
    }
}
