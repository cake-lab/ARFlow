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

        _client.Connect(new RegisterRequest()
        {
            DeviceName = "MockDataTestbed",
            CameraIntrinsics = new RegisterRequest.Types.CameraIntrinsics()
            {
                FocalLengthX = 128,
                FocalLengthY = 96,
                ResolutionX = 256,
                ResolutionY = 192,
                PrincipalPointX = 128,
                PrincipalPointY = 96
            },
            CameraColor = new RegisterRequest.Types.CameraColor()
            {
                Enabled = true,
                DataType = "YCbCr420",
                ResizeFactorX = 1.0f,
                ResizeFactorY = 1.0f,
            },
            CameraDepth = new RegisterRequest.Types.CameraDepth()
            {
                Enabled = false,
            },
            CameraTransform = new RegisterRequest.Types.CameraTransform()
            {
                Enabled = false
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

        _client.SendFrame(new DataFrameRequest()
        {
            Color = ByteString.CopyFrom(colorBytes)
        });
    }

    // Update is called once per frame
    void Update()
    {

    }
}
