using UnityEngine;
using ARFlow;
using Google.Protobuf;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Class for sending mock data to the server.
/// Used in the MockData scene.
/// </summary>
public class ARFlowMockDataSample : MonoBehaviour
{
    public TMP_InputField addressInput;
    public Button connectButton;
    public Button sendButton;

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

        _client.Connect(new ClientConfiguration()
        {
            DeviceName = "MockDataTestbed",
            CameraIntrinsics = new ClientConfiguration.Types.CameraIntrinsics()
            {
                FocalLengthX = 128,
                FocalLengthY = 96,
                ResolutionX = 256,
                ResolutionY = 192,
                PrincipalPointX = 128,
                PrincipalPointY = 96
            },
            CameraColor = new ClientConfiguration.Types.CameraColor()
            {
                Enabled = true,
                DataType = "YCbCr420",
                ResizeFactorX = 1.0f,
                ResizeFactorY = 1.0f,
            },
            CameraDepth = new ClientConfiguration.Types.CameraDepth()
            {
                Enabled = false,
            },
            CameraTransform = new ClientConfiguration.Types.CameraTransform()
            {
                Enabled = false
            },
            Gyroscope = new RegisterRequest.Types.Gyroscope()
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

        var dataFrameRequest = new DataFrameRequest()
        {
            Color = ByteString.CopyFrom(colorBytes)
        };

        dataFrameRequest.Gyroscope = new DataFrameRequest.Types.gyroscope_data();
        Quaternion attitude = Input.gyro.attitude;
        Vector3 rotation_rate = Input.gyro.rotationRateUnbiased;
        Vector3 gravity = Input.gyro.gravity;
        Vector3 acceleration = Input.gyro.userAcceleration;

        dataFrameRequest.Gyroscope.Attitude = unityQuaternionToProto(attitude);
        dataFrameRequest.Gyroscope.RotationRate = unityVector3ToProto(rotation_rate);
        dataFrameRequest.Gyroscope.Gravity = unityVector3ToProto(gravity);
        dataFrameRequest.Gyroscope.Acceleration = unityVector3ToProto(acceleration);

        _client.SendFrame(new DataFrameRequest()
        {
            Color = ByteString.CopyFrom(colorBytes)

        });
    }

    DataFrameRequest.Types.Vector3 unityVector3ToProto(Vector3 a)
    {
        return new DataFrameRequest.Types.Vector3()
        {
            X = a.x,
            Y = a.y,
            Z = a.z
        };
    }

    DataFrameRequest.Types.Quaternion unityQuaternionToProto(Quaternion a)
    {
        return new DataFrameRequest.Types.Quaternion()
        {
            X = a.x,
            Y = a.y,
            Z = a.z,
            W = a.w
        };
    }

    // Update is called once per frame
    void Update()
    {

    }
}
