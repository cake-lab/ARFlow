using UnityEngine;
using ARFlow;
using Google.Protobuf;
using UnityEngine.UI;

public class MockARTestbed : MonoBehaviour
{
    public Button connectButton;
    public Button sendButton;

    private ARFlowClient _client;
    private Vector2Int _sampleSize;

    // Start is called before the first frame update
    void Start()
    {
        // const string serverURL = "http://192.168.1.100:8500";
        const string serverURL = "http://0.0.0.0:8500";
        _client = new ARFlowClient(serverURL);

        connectButton.onClick.AddListener(OnConnectButtonClick);
        sendButton.onClick.AddListener(OnSendButtonClick);
    }

    private void OnConnectButtonClick()
    {
        _sampleSize = new Vector2Int(256, 128);

        _client.Connect(new RegisterRequest()
        {
            DeviceName = "MockDataTestbed",
            CameraIntrinsics = new RegisterRequest.Types.CameraIntrinsics()
            {
                FocalLengthX = 1,
                FocalLengthY = 2,
                ResolutionX = 3,
                ResolutionY = 4,
                PrincipalPointX = 0.5f,
                PrincipalPointY = 0.5f
            },
            CameraColor = new RegisterRequest.Types.CameraColor()
            {
                Enabled = true
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
        var colorBytes = new byte[size];

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
