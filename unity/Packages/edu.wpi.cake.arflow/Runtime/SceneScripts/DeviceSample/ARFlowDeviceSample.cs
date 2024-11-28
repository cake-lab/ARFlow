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
using System.Threading;

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
        public GameObject windowGameObject;
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

        // Search for session also
        SearchForSession();
        // findServerWindow.windowGameObject.SetActive(false);
        sessionsWindow.windowGameObject.SetActive(true);
    }

    [Serializable]
    public class CreateSessionWindow
    {
        public GameObject windowGameObject;
        public TMP_InputField sessionNameInput;
        public TMP_InputField sessionSavePathInput;
        public Button createSessionButton;
        public Button cancelSessionButton;

        private CancellationTokenSource m_cts = new CancellationTokenSource();
        public CancellationTokenSource cts { get { return m_cts; } }

    }

    void OnCancelCreateSession()
    {
        sessionsWindow.createSessionWindow.windowGameObject.SetActive(false);
        sessionsWindow.windowGameObject.SetActive(true);
        sessionsWindow.createSessionWindow.cts.Cancel();
    }

    async void OnCreateSession()
    {
        string sessionName = sessionsWindow.createSessionWindow.sessionNameInput.text;
        string sessionSavePath = sessionsWindow.createSessionWindow.sessionSavePathInput.text;

        if (grpcClient == null)
        {
            InternalDebug.Log("GrpcClient is null");
            return;
        }

        // if gRPC client is not null, we can create a session
        if (sessionName.Length == 0)
        {
            await Awaitable.MainThreadAsync();
            InternalDebug.Log("Session name cannot be empty");
            Toast.Show("Session name cannot be empty", ToastColor.Red);
            return;
        }
        var res = new SessionMetadata
        {
            Name = sessionName,
            SavePath = string.IsNullOrEmpty(sessionSavePath) ? null : sessionSavePath
        };

        // TODO: Do we need to move to background thread?
        await Awaitable.BackgroundThreadAsync();
        var createSessionRes = await grpcClient.CreateSessionAsync(
            res,
            GetDeviceInfo.GetDevice(),
            sessionSavePath,
            sessionsWindow.createSessionWindow.cts.Token
            );

        await Awaitable.MainThreadAsync();
        if (createSessionRes is not null)
        {
            InternalDebug.Log("Session created successfully");
            Toast.Show("Session created successfully", ToastColor.Green);
            sessionsWindow.createSessionWindow.windowGameObject.SetActive(false);
            SearchForSession();
        }
        else
        {
            InternalDebug.Log("Session creation failed");
            Toast.Show("Session creation failed", ToastColor.Red);
        }
    }

    [Serializable]
    public class SessionsWindow
    {
        public GameObject windowGameObject;
        public GameObject anotherGameObject;
        public GameObject loadingIndicator;

        public GameObject sessionElementPrefab;
        public GameObject sessionListContent;
        public Button refreshButton;
        public Button createSessionButton;
        public Button deleteSessionButton;
        public Button joinSessionButton;

        public CreateSessionWindow createSessionWindow;

        private List<SessionElement> m_sessionElements = new();

        private CancellationTokenSource m_cts = new CancellationTokenSource();
        public CancellationTokenSource cts { get { return m_cts; } }

        // Do not serialize this to avoid Unity reflection
        private SessionElement m_selectedSessionElement;
        public SessionElement selectedSessionElement
        {
            get
            {
                return m_selectedSessionElement;
            }
            set
            {
                m_selectedSessionElement = value;
            }
        }

        private void OnSelectSession(SessionElement sessionElement)
        {
            m_selectedSessionElement = sessionElement;
        }

        public void AddSession(Session session)
        {
            GameObject sessionElementObject = Instantiate(sessionElementPrefab, sessionListContent.transform);
            SessionElement sessionElement = sessionElementObject.GetComponent<SessionElement>();
            sessionElement.session = session;
            m_sessionElements.Add(sessionElement);

            // When select session, set the selected session element
            sessionElement.selectButton.onClick.AddListener(() => OnSelectSession(sessionElement));
        }

        public void ClearSessions()
        {
            foreach (var sessionElement in m_sessionElements)
            {
                Destroy(sessionElement.gameObject);
            }
            m_sessionElements.Clear();
            m_selectedSessionElement = null;
        }

        public void setLoading(bool loading)
        {
            loadingIndicator.SetActive(loading);
        }
    }
    public SessionsWindow sessionsWindow;

    /// <summary>
    /// Search for available sessions and display them in the UI asynchronously
    /// </summary>
    async void SearchForSession()
    {

        if (grpcClient != null)
        {
            await Awaitable.MainThreadAsync();

            // TODO: Could race condition happen here?
            sessionsWindow.selectedSessionElement = null;
            sessionsWindow.setLoading(true);

            // Do we need to move to background thread?
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

    void OnPressCreateSession()
    {
        sessionsWindow.createSessionWindow.windowGameObject.SetActive(true);
        sessionsWindow.windowGameObject.SetActive(false);
    }

    async void OnDeleteSession()
    {
        if (sessionsWindow.selectedSessionElement != null)
        {
            var session = sessionsWindow.selectedSessionElement.session;
            sessionsWindow.cts.Cancel();
            await grpcClient.DeleteSessionAsync(session.Id, sessionsWindow.cts.Token);
            SearchForSession();
        }
        else
        {
            Toast.Show("No session selected", ToastColor.Red);
        }
    }

    async void OnJoinSession()
    {
        if (sessionsWindow.selectedSessionElement != null)
        {
            var session = sessionsWindow.selectedSessionElement.session;
            sessionsWindow.cts.Cancel();
            await grpcClient.JoinSessionAsync(session.Id, GetDeviceInfo.GetDevice(), sessionsWindow.cts.Token);

            //joining completeted --> switch to the next window
            await Awaitable.MainThreadAsync();

            //TODO: switch to the next window
        }
        else
        {
            Toast.Show("No session selected", ToastColor.Red);
        }
    }

    [Serializable]
    public class ARViewWindow
    {
        public GameObject windowGameObject;
        public GameObject ConfigurationsContainer;
        public Button startPauseButton;

    }



    void Start()
    {
        findServerWindow.connectButton.onClick.AddListener(OnConnectToServer);

        sessionsWindow.refreshButton.onClick.AddListener(SearchForSession);
        sessionsWindow.createSessionButton.onClick.AddListener(OnPressCreateSession);

        //Init create sessions window

        sessionsWindow.createSessionWindow.cancelSessionButton.onClick.AddListener(OnCancelCreateSession);
        sessionsWindow.createSessionWindow.createSessionButton.onClick.AddListener(OnCreateSession);
        sessionsWindow.deleteSessionButton.onClick.AddListener(OnDeleteSession);
        sessionsWindow.joinSessionButton.onClick.AddListener(OnJoinSession);

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
