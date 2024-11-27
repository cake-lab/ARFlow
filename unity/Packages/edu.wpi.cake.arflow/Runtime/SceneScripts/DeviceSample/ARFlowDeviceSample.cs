using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using System.Collections.Generic;
using System.Collections;

using EasyUI.Toast;
using System;
using System.Threading.Tasks;
using System.Net;
using System.Security.Cryptography;

// using static ARFlow.OtherUtils;
using UnityEngine.XR.ARSubsystems;
using Unity.Collections;
using System.Net.Http;

using TMPro;
using System.Text.RegularExpressions;

using CakeLab.ARFlow.Grpc;
using CakeLab.ARFlow.Grpc.V1;
using CakeLab.ARFlow.Utilities;

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

    /// <summary>
    /// Plane detection
    /// </summary>
    public ARPlaneManager planeManager;

    /// <summary>
    /// Plane detection
    /// </summary>
    public ARMeshManager meshManager;

    public IGrpcClient grpcClient;

    public Button startPauseButton;

    private bool _enabled = false;

    // private ARFlowClientManager _clientManager;

    public GameObject OptionsContainer;

    private readonly Dictionary<string, GameObject> _optionObjects = new();

    private readonly string _defaultConnection = "http://192.168.1.219:8500";

    private bool _isConnected = false;
    private Task connectTask = null;

    public Camera camera;


    [Serializable]
    public class ButtonOptionHandler
    {
        public List<GameObject> options;
        public Button toggleButton;

        private int currentOption = 0;

        public void toggleOption()
        {
            options[currentOption].SetActive(false);
            currentOption = (currentOption + 1) % options.Count;
            options[currentOption].SetActive(true);
        }

        public void disable()
        {
            options[currentOption].SetActive(false);
            options[0].SetActive(true);
            currentOption = 0;

            toggleButton.interactable = false;
        }

        public void enable()
        {
            toggleButton.interactable = true;
        }
    }
    public ButtonOptionHandler buttonOptionHandler;


    [Serializable]
    public class FindServerWindow
    {
        public TMP_InputField ipField;
        public TMP_InputField portField;
        public Button findServerButton;
        public Button connectButton;

        private const string _defaultIp = "127.0.0.1";
        private const string _defaultPort = "8500";

        public string ipText
        {
            set { }
            get { return IsIpValid(ipField.text) ? ipField.text : _defaultIp; }
        }

        public string portText
        {
            set { }
            get { return IsIpValid(portField.text) ? portField.text : _defaultPort; }
        }

        public bool IsIpValid(string ipField)
        {
            return Regex.IsMatch(ipField, @"(\d){1,3}\.(\d){1,3}\.(\d){1,3}\.(\d){1,3}");
        }

        public bool IsPortValid(string portField)
        {
            return Regex.IsMatch(portField, @"(\d){1,5}");
        }
    }

    public FindServerWindow findServerWindow;
    public void OnConnectToServer()
    {
        string ip = findServerWindow.ipText;
        string port = findServerWindow.portText;

        string serverUrl = $"http://{ip}:{port}";

        grpcClient = new GrpcClient(serverUrl);
    }

    [Serializable]
    public class SessionsWindow
    {
        public GameObject loadingIndicator;

        public GameObject sessionElementPrefab;
        public GameObject sessionListContent;
        public Button refreshButton;
        public Button createSessionButton;
        public Button chooseSessionButton;

        private List<SessionElement> _sessionElements = new();

        public void AddSession(Session session)
        {
            GameObject sessionElement = Instantiate(sessionElementPrefab, sessionListContent.transform);
            SessionElement sessionElementScript = sessionElement.GetComponent<SessionElement>();
            sessionElementScript.SetSession(session);
            _sessionElements.Add(sessionElementScript);

            //TODO: what do we do when we choose a session?
        }

        public void ClearSessions()
        {
            foreach (var sessionElement in _sessionElements)
            {
                Destroy(sessionElement.gameObject);
            }
            _sessionElements.Clear();
        }

        public void setLoading(bool loading)
        {
            loadingIndicator.SetActive(loading);
        }
    }
    public SessionsWindow sessionsWindow;



    void Start()
    {
        findServerWindow.connectButton.onClick.AddListener(OnConnectToServer);

        sessionsWindow.refreshButton.onClick.AddListener(searchForSession);
        searchForSession();
        sessionsWindow.createSessionButton.onClick.AddListener(() => { });


        // startPauseButton.onClick.AddListener(OnStartPauseButtonClick);

        // // _clientManager = new ARFlowClientManager(
        // //     cameraManager: cameraManager,
        // //     occlusionManager: occlusionManager,
        // //     planeManager: planeManager,
        // //     meshManager: meshManager
        // //  );

        // AddModalityOptionsToConfig();


        // // OnConnectButtonClick();

        // // The following suppose to limit the fps to 30, but it doesn't work.
        // // QualitySettings.vSyncCount = 0;
        // // Application.targetFrameRate = 30;

        // buttonOptionHandler.toggleButton.onClick.AddListener(
        //     buttonOptionHandler.toggleOption);
    }

    /// <summary>
    /// Search for available sessions and display them in the UI asynchronously
    /// </summary>
    async void searchForSession()
    {
        if (grpcClient != null)
        {
            await Awaitable.MainThreadAsync();
            sessionsWindow.setLoading(true);

            // Do we ned to move to background thread?
            await Awaitable.BackgroundThreadAsync();
            var res = await grpcClient.ListSessionsAsync();

            await Awaitable.MainThreadAsync();
            sessionsWindow.setLoading(false);

            sessionsWindow.ClearSessions();
            foreach (var session in res.Sessions)
            {
                sessionsWindow.AddSession(session);
            }
        }
        else
        {
            InternalDebug.Log("GrpcClient is null");
        }
    }

    async void createSessionButton()
    {

    }

    // void AddModalityOptionsToConfig()
    // {
    //     // Get first child, WITH THE ASSUMPTION that it's a checkbox
    //     GameObject firstChild = OptionsContainer.transform.GetChild(0).gameObject;

    //     foreach (string modality in ARFlowClientManager.MODALITIES)
    //     {
    //         GameObject newOption = Instantiate(
    //             firstChild,
    //             parent: OptionsContainer.transform
    //         );
    //         newOption.GetComponent<Text>().text = SplitByCapital(modality);

    //         _optionObjects.Add(modality, newOption);
    //     }
    // }

    // string SplitByCapital(string s)
    // {
    //     return Regex.Replace(s, "([a-z])([A-Z])", "$1 $2");
    // }

    // Dictionary<string, bool> GetModalityOptions()
    // {
    //     Dictionary<string, bool> res = new();
    //     foreach (var option in _optionObjects)
    //     {
    //         var optionName = option.Key;
    //         var optionObject = option.Value;

    //         var slider = optionObject.transform.Find("Slider");
    //         if (slider != null)
    //         {
    //             var sliderVal = slider.GetComponent<Slider>().value;
    //             res.Add(optionName, sliderVal != 0);
    //         }
    //     }

    //     return res;
    // }

    // bool IsIpValid(string ipField)
    // {
    //     return Regex.IsMatch(ipField, @"(\d){1,3}\.(\d){1,3}\.(\d){1,3}\.(\d){1,3}");
    // }

    // bool IsPortValid(string portField)
    // {
    //     return Regex.IsMatch(portField, @"(\d){1,5}");
    // }

    // /// <summary>
    // /// Get register request data from camera and send to server.
    // /// Image and depth info is acquired once to get information for the request, and is disposed afterwards.
    // /// </summary>
    // private void OnConnectButtonClick()
    // {

    //     var serverURL = _defaultConnection;
    //     if (IsIpValid(ipField.text) && IsPortValid(portField.text))
    //     {
    //         serverURL = "http://" + ipField.text + ":" + portField.text;
    //     }

    //     // To update status of task to user
    //     Toast.Show($"Connecting to {serverURL}", 3f, ToastColor.Yellow);

    //     // Since toast can only be called from main thread (we cannot use the hook to display toast)
    //     // these flags are updated and signals connection result to display to user.
    //     connectTask = _clientManager.ConnectTask(
    //         serverURL,
    //         GetModalityOptions(),
    //         t =>
    //         {
    //         });
    //     _isConnected = false;
    // }

    // private void UpdateConnectionStatus()
    // {
    //     if (connectTask is not null && connectTask.IsCompleted)
    //     {
    //         if (connectTask.IsFaulted)
    //         {
    //             PrintDebug(connectTask.Exception);
    //             connectTask = null;
    //             Toast.Show("Connection failed.", ToastColor.Red);
    //         }
    //         else if (connectTask.IsCompletedSuccessfully)
    //         {
    //             _isConnected = true;
    //             Toast.Show("Connected successfully.", ToastColor.Green);
    //         }
    //     }
    // }



    // /// <summary>
    // /// On pause, pressing the button changes the _eabled flag to true  (and text display) and data starts sending in Update()
    // /// On start, pressing the button changes the _enabled flag to false and data stops sending
    // /// </summary>
    // private void OnStartPauseButtonClick()
    // {
    //     PrintDebug($"Current framerate: {Application.targetFrameRate}");
    //     if (enabled)
    //     {
    //         if (!_isConnected)
    //         {
    //             Toast.Show("Connnection not established. Cannot send dataframe.");
    //             _enabled = false;
    //             return;
    //         }

    //         _clientManager.StartDataStreaming();
    //     }
    //     else
    //     {
    //         _clientManager.StopDataStreaming();
    //     }

    //     _enabled = !_enabled;
    //     startPauseButton.GetComponentInChildren<TMP_Text>().text = _enabled ? "Pause" : "Start";
    // }

    // // Button event handlers
    // public void ShowQR()
    // {
    //     var uid = _clientManager.getSessionId();
    //     if (string.IsNullOrWhiteSpace(uid))
    //     {
    //         Toast.Show("To share Uid, first conenct to a session", ToastColor.Red);
    //         return;
    //     }
    //     Texture2D tex = QRManager.encode(_clientManager.getSessionId());

    //     QR.rawQR.texture = tex;
    //     QR.window.SetActive(true);
    // }

    // public void CopyUID()
    // {
    //     var uid = _clientManager.getSessionId();
    //     if (string.IsNullOrWhiteSpace(uid))
    //     {
    //         Toast.Show("To share Uid, first conenct to a session", ToastColor.Red);
    //         return;
    //     }
    //     GUIUtility.systemCopyBuffer = uid;
    //     Toast.Show("Copied to clipboard", ToastColor.Green);
    // }

    // /// <summary>
    // /// Get texture from camera in RGBA32 format
    // /// </summary>
    // /// <returns></returns>
    // //private Texture2D getCameraTexture()
    // //{
    // //    int width = camera.pixelWidth;
    // //    int height = camera.pixelHeight;


    // //    RenderTexture lastRT = RenderTexture.active;

    // //    RenderTexture.active = camera.targetTexture;

    // //    camera.Render();

    // //    Texture2D capture = new Texture2D(width, height, TextureFormat.RGBA32, false);
    // //    capture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
    // //    capture.Apply();

    // //    RenderTexture.active = lastRT;

    // //    return capture;
    // //}


    // private byte[] GetCpuImage(out int width, out int height)
    // {
    //     try
    //     {
    //         cameraManager.TryAcquireLatestCpuImage(out var cpuImg);
    //         width = cpuImg.width; height = cpuImg.height;

    //         var conversionParams = new XRCpuImage.ConversionParams(
    //             cpuImg,
    //             TextureFormat.RGBA32
    //         );

    //         int conversionSize = cpuImg.GetConvertedDataSize(conversionParams);
    //         var nativeBytes = new NativeArray<byte>(new byte[conversionSize], Allocator.Temp);
    //         var nativeSlice = new NativeSlice<byte>(nativeBytes);

    //         cpuImg.Convert(conversionParams, nativeSlice);
    //         var byteArr = nativeSlice.ToArray();
    //         return byteArr;
    //     }
    //     catch (Exception e)
    //     {
    //         PrintDebug(e);
    //     }
    //     width = 0;
    //     height = 0;
    //     return null;
    // }

    // public void ConnectByQr()
    // {
    //     var img = GetCpuImage(out var width, out var height);
    //     //var img = getCameraTexture().GetPixels32();
    //     var uid = QRManager.readQRCode(img, width, height);

    //     if (!string.IsNullOrEmpty(uid))
    //     {
    //         connectTask = _clientManager.JoinSessionTask(uid);
    //         _isConnected = false;
    //         Toast.Show($"Joining session with UID {uid}", ToastColor.Green);
    //     }
    //     else
    //     {
    //         Toast.Show("No QR Code found", ToastColor.Red);
    //     }
    // }

    // public void ConnectFromClipboard()
    // {
    //     string uid = GUIUtility.systemCopyBuffer;
    //     if (!string.IsNullOrEmpty(uid))
    //     {
    //         connectTask = _clientManager.JoinSessionTask(uid);
    //         _isConnected = false;
    //         Toast.Show($"Joining session with UID {uid}", ToastColor.Green);
    //     }
    //     else
    //     {
    //         Toast.Show("No QR Code found", ToastColor.Red);
    //     }
    // }

    // // Update is called once per frame
    // void FixedUpdate()
    // {
    //     if (!_isConnected)
    //     {
    //         UpdateConnectionStatus();
    //     }
    //     if (!_enabled) return;
    //     _clientManager.GetAndSendFrameTask();
    // }

}
