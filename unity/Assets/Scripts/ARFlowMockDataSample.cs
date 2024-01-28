using UnityEngine;
using ARFlow;
using Google.Protobuf;
using UnityEngine.UI;
using TMPro;

public class ARFlowMockDataSample : MonoBehaviour
{
    public TMP_InputField addressInput;
    public Button connectButton;
    public Button sendButton;

    private ARFlowClient _client;
    private Vector2Int _sampleSize;
    private System.Random _rnd = new System.Random();

    // Start is called before the first frame update
    void Start()
    {
        string serverURL = addressInput.text;
        _client = new ARFlowClient(serverURL);

        connectButton.onClick.AddListener(OnConnectButtonClick);
        sendButton.onClick.AddListener(OnSendButtonClick);
    }

    private void OnConnectButtonClick()
    {
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
                ResizeFactorX = 1.0f,
                ResizeFactorY = 1.0f,
            },
            CameraDepth = new RegisterRequest.Types.CameraDepth()
            {
                Enabled = false,
                DataType = "f32",
            },
            CameraTransform = new RegisterRequest.Types.CameraTransform()
            {
                Enabled = false
            }
        });
    }

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
