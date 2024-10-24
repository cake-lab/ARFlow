using UnityEngine;
using ARFlow;
using Google.Protobuf;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Unity.Collections;
using static ARFlow.ProcessFrameRequest.Types.Mesh;

using static ARFlow.OtherUtils;

/// <summary>
/// Class for sending mock data to the server.
/// Used in the MockData scene.
/// </summary>
public class ARFlowMockDataSample : MonoBehaviour
{
    public TMP_InputField addressInput;
    public Button connectButton;
    public Button sendButton;

    public GameObject testBunny;

    private ARFlowClient _client;
    /// <summary>
    /// Size of mock data generated to send to server, in width (x) and length (y).
    /// </summary>
    private Vector2Int _sampleSize;
    private System.Random _rnd = new System.Random();

    // Start is called before the first frame update
    void Start()
    {
        connectButton.onClick.AddListener(OnConnectButtonClick);
        sendButton.onClick.AddListener(OnSendButtonClick);
    }

    /// <summary>
    /// On connection, send register request with mock camera's register data.
    /// For the mock sample, we are only sending color data.
    /// </summary>
    private void OnConnectButtonClick()
    {
        string serverURL = addressInput.text;
        _client = new ARFlowClient($"http://{serverURL}");

        _sampleSize = new Vector2Int(256, 192);

        _client.Connect(new RegisterClientRequest()
        {
            DeviceName = "MockDataTestbed",
            CameraIntrinsics = new RegisterClientRequest.Types.CameraIntrinsics()
            {
                FocalLengthX = 128,
                FocalLengthY = 96,
                ResolutionX = 256,
                ResolutionY = 192,
                PrincipalPointX = 128,
                PrincipalPointY = 96
            },
            CameraColor = new RegisterClientRequest.Types.CameraColor()
            {
                Enabled = true,
                DataType = "YCbCr420",
                ResizeFactorX = 1.0f,
                ResizeFactorY = 1.0f,
            },
            CameraDepth = new RegisterClientRequest.Types.CameraDepth()
            {
                Enabled = false,
            },
            CameraTransform = new RegisterClientRequest.Types.CameraTransform()
            {
                Enabled = false
            },
            Gyroscope = new RegisterClientRequest.Types.Gyroscope()
            {
                Enabled = true,
            },
            Meshing = new RegisterClientRequest.Types.Meshing()
            {
                Enabled = true,
            },
            CameraPlaneDetection = new RegisterClientRequest.Types.CameraPlaneDetection()
            {
                Enabled = true,
            }
        });
    }

    /// <summary>
    /// On pressing send, 1 frame of mock data in bytes is generated from System.Random and sended.
    /// </summary>
    private void OnSendButtonClick()
    {
        var size = _sampleSize.x * _sampleSize.y + 2 * (_sampleSize.x / 2 * _sampleSize.y / 2);

        // Generate random bytes as the image data.
        var colorBytes = new byte[size];
        _rnd.NextBytes(colorBytes);

        var dataFrame = new ProcessFrameRequest()
        {
            Color = ByteString.CopyFrom(colorBytes)
        };

        dataFrame.Gyroscope = new ProcessFrameRequest.Types.GyroscopeData();
        Quaternion attitude = Input.gyro.attitude;
        Vector3 rotation_rate = Input.gyro.rotationRateUnbiased;
        Vector3 gravity = Input.gyro.gravity;
        Vector3 acceleration = Input.gyro.userAcceleration;

        dataFrame.Gyroscope.Attitude = unityQuaternionToProto(attitude);
        dataFrame.Gyroscope.RotationRate = unityVector3ToProto(rotation_rate);
        dataFrame.Gyroscope.Gravity = unityVector3ToProto(gravity);
        dataFrame.Gyroscope.Acceleration = unityVector3ToProto(acceleration);

        // Test meshing data encode + test server handling
        Mesh meshdata = testBunny.GetComponent<MeshFilter>().sharedMesh;

        var meshEncoder = new MeshEncoder();
        List<NativeArray<byte>> encodedMesh = meshEncoder.EncodeMesh(meshdata);
        for (int i = 0; i < 20; i++)
        {
            foreach (var meshElement in encodedMesh)
            {
                var meshProto = new ProcessFrameRequest.Types.Mesh();
                meshProto.Data = ByteString.CopyFrom(meshElement);

                dataFrame.Meshes.Add(meshProto);
            }
        }

        // Test plane
        var plane = new ProcessFrameRequest.Types.Plane();
        plane.Center = unityVector3ToProto(new Vector3(1, 2, 3));
        plane.Normal = unityVector3ToProto(new Vector3(0, 2, 5));
        plane.Size = unityVector2ToProto(new Vector2(5, 5));
        plane.BoundaryPoints.Add(new[]
            { unityVector2ToProto(new Vector2(0, 2)),
            unityVector2ToProto(new Vector2(1, 3)),
            unityVector2ToProto(new Vector2(2, 4)),
            unityVector2ToProto(new Vector2(1, 5)),
            unityVector2ToProto(new Vector2(2, 1)) }
        );
        dataFrame.PlaneDetection.Add(plane);

        _client.SendFrame(dataFrame);
    }

    ProcessFrameRequest.Types.Vector3 unityVector3ToProto(Vector3 a)
    {
        return new ProcessFrameRequest.Types.Vector3()
        {
            X = a.x,
            Y = a.y,
            Z = a.z
        };
    }

    ProcessFrameRequest.Types.Quaternion unityQuaternionToProto(Quaternion a)
    {
        return new ProcessFrameRequest.Types.Quaternion()
        {
            X = a.x,
            Y = a.y,
            Z = a.z,
            W = a.w
        };
    }

    ProcessFrameRequest.Types.Vector2 unityVector2ToProto(Vector2 a)
    {
        return new ProcessFrameRequest.Types.Vector2()
        {
            X = a.x,
            Y = a.y,
        };
    }

    // Update is called once per frame
    void Update()
    {

    }
}
