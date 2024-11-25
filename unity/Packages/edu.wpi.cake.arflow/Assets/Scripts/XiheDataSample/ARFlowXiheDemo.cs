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

using static ARFlow.OtherUtils;

public class ARFlowXiheDemo : MonoBehaviour
{
    public TMP_InputField addressInput;
    public Button connectButton;
    public Button startPauseButton;
    public ARCameraManager cameraManager;
    public AROcclusionManager occlusionManager;
    private bool _initialized = false;
    private bool _enabled = false;

    private ARFlowClientManager _clientManager;

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
        _clientManager = new ARFlowClientManager(cameraManager, occlusionManager);
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

        _clientManager.Connect(addressInput.text);
    }

    private void OnStartPauseButtonClick()
    {
        PrintDebug($"Current framerate: {Application.targetFrameRate}");

        _enabled = !_enabled;
        startPauseButton.GetComponentInChildren<TMP_Text>().text = _enabled ? "Pause" : "Start";
    }

    private void UploadFrame()
    {

        var responseSHC = _clientManager.GetAndSendFrame();

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
