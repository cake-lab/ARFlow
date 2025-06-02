using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using CakeLab.ARFlow.Clock;
using CakeLab.ARFlow.DataBuffers;
using CakeLab.ARFlow.DataModalityUIConfig;
using CakeLab.ARFlow.Grpc;
using CakeLab.ARFlow.Grpc.V1;
using CakeLab.ARFlow.Utilities;
using EasyUI.Toast;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARFlowDeviceSample : MonoBehaviour
{
    public GameObject XROrigin;

    [Space(20)]
    [Header("AR Managers & Data collectors")]
    // AR Managers for AR data collection
    [Tooltip("Camera image data's manager from the device camera")]
    public ARCameraManager cameraManager;

    [Tooltip("Depth data's manager from the device camera")]
    public AROcclusionManager occlusionManager;

    public ARPlaneManager planeManager;

    public ARMeshManager meshManager;

    public ARPointCloudManager pointCloudManager;

    [Tooltip(
        "Pose will be obtained from this object. This object should contain a TrackedPoseDriver component."
    )]

    // Variables for state of the ARFlow device sample
    public IGrpcClient grpcClient;

    public IClock clock;

    private Session m_ActiveSession;
    private Device m_Device;

    private bool m_isSending = false;

    // Data buffers and UI configs

    /// <summary>
    /// Handles the lifecycle and sending of frames for all data modalities that support conversion to ARFrame.
    /// The manager's enabled state is also managed by the UIConfig class.
    ///
    /// This is a proposal for a way to manage the lifecycle of the buffer and sending of frames through UIConfig.
    /// </summary>
    public class BufferControl : IDisposable
    {
        public BaseDataModalityUIConfig config;
        public IARFrameBuffer buffer;
        public CancellationTokenSource cts = new CancellationTokenSource();

        public BufferControl(BaseDataModalityUIConfig config)
        {
            this.config = config;
        }

        public Action<bool> OnToggle(Action<BufferControl> sendFrame)
        {
            return (bool isOn) =>
            {
                if (isOn)
                {
                    buffer = config.GetGenericBuffer();
                    InternalDebug.Log($"Enable Manager {buffer.GetType().Name}");
                    sendFrame(this);
                }
                else
                {
                    InternalDebug.Log("Disable Manager");
                    buffer?.StopCapture();
                    buffer?.Dispose();
                    cts.Cancel();
                    cts = new CancellationTokenSource();
                }
            };
        }

        public void Dispose()
        {
            config.Dispose();
            buffer?.Dispose();
            cts?.Dispose();
        }
    }

    public async void SendFrame(BufferControl control)
    {
        if (!m_isSending)
        {
            return;
        }
        control.buffer.StartCapture();

        try
        {
            while (!control.cts.Token.IsCancellationRequested)
            {
                float currentSendIntervalS = control.config.GetSendIntervalS();
                // OperationCanceledException is thrown when the token is cancelled, this is expected
                // For more details, see https://blog.stephencleary.com/2022/02/cancellation-1-overview.html
                await Awaitable.WaitForSecondsAsync(currentSendIntervalS, control.cts.Token);

                IEnumerable<ARFrame> arFrames = control.buffer.TakeARFrames();

                if (arFrames == null || !arFrames.Any())
                {
                    InternalDebug.Log("No frames to send.");
                    continue;
                }
                // NOTE: Count() is expensive so only uncomment for debugging, the compiler not smart enough to
                // strip out unecessary call during production build
                // InternalDebug.Log($"Sending {arFrames.Count()} frames");

                var _ = await grpcClient.SaveARFramesAsync(
                    m_ActiveSession.Id,
                    arFrames,
                    m_Device,
                    control.cts.Token
                );
            }
        }
        catch (OperationCanceledException e)
        {
            InternalDebug.Log($"Operation cancelled {e}");
        }
    }

    private AudioUIConfig m_AudioUIConfig;
    BufferControl audioBufferControl;

    private ColorUIConfig m_ColorUIConfig;
    BufferControl colorBufferControl;

    private DepthUIConfig m_depthUIConfig;

    BufferControl depthBufferControl;

    private GyroscopeUIConfig m_GyroscopeUIConfig;
    BufferControl gyroscopeBufferControl;

    private MeshDetectionUIConfig m_MeshDetectionUIConfig;
    BufferControl meshDetectionControl;

    private PlaneDetectionUIConfig m_PlaneDetectionUIConfig;
    BufferControl planeDetectionControl;

    private PointCloudDetectionUIConfig m_PointCloudDetectionUIConfig;
    BufferControl pointCloudDetectionControl;

    private TransformUIConfig m_TransformUIConfig;
    BufferControl transformBufferControl;

    private List<IARFrameBuffer> m_DataBuffers;
    private List<BufferControl> m_BufferControls = new();

    private List<BaseDataModalityUIConfig> m_dataModalityUIConfigs = new();
    private List<CancellationTokenSource> m_ctsList = new List<CancellationTokenSource>();

    // UI Windows

    [Serializable]
    public class FindServerWindow
    {
        public GameObject windowGameObject;
        public TMP_InputField ipServerField;
        public TMP_InputField portServerField;

        public Toggle NTPSameServerToggle;
        public TMP_InputField ipNTPField;
        public Button findServerButton;
        public Button connectButton;

        private const string _defaultIp = "127.0.0.1";
        private const string _defaultPort = "8500";
        public string defaultAddress => $"http://{_defaultIp}:{_defaultPort}";

        public string serverIP => ipServerField.text;

        public string serverPort => portServerField.text;
        public string serverAddress => $"http://{serverIP}:{serverPort}";

        public bool isToggleOn => NTPSameServerToggle.isOn;

        public string ipNTPText => isToggleOn ? serverIP : ipNTPField.text;
        public string ntpAddress => ipNTPText;
        public string defaultNTP => "pool.ntp.org";

        public bool IsIpValid(string ipField, string portField)
        {
            return Regex.IsMatch(ipField, @"(\d){1,3}\.(\d){1,3}\.(\d){1,3}\.(\d){1,3}");
        }

        /// <summary>
        /// To be called outside because this runs before the variables get filled in by Unity
        /// </summary>
        public void initFindServerWindow()
        {
            ipServerField.text = _defaultIp;
            portServerField.text = _defaultPort;
            NTPSameServerToggle.onValueChanged.AddListener(
                (bool value) =>
                {
                    ipNTPField.interactable = !value;
                }
            );
        }
    }

    [Header("UI Windows")]
    [Tooltip("UI Window for finding server")]
    public FindServerWindow findServerWindow;

    public async void OnConnectToServer()
    {
        try
        {
            string serverURL = Uri.IsWellFormedUriString(
                findServerWindow.serverAddress,
                UriKind.RelativeOrAbsolute
            )
                ? findServerWindow.serverAddress
                : findServerWindow.defaultAddress;

            string ntpURL = findServerWindow.ntpAddress;

            grpcClient = new GrpcClient(serverURL);

            Toast.Show("Connection in progress.", 1f, ToastColor.Yellow);

            // Search for session also
            await SearchForSession();

            InternalDebug.Log("connecting to NTP server");

            NtpClock ntpClock = new NtpClock(ntpURL);
            clock = ntpClock;

            InitUIConfig();
            findServerWindow.windowGameObject.SetActive(false);
            sessionsWindow.windowGameObject.SetActive(true);
            // If "NTP server same as server" is not checked and field is left empty, do not sync. NTP clock will fallback to system clock
            if (ntpURL.Length > 0)
            {
                await ntpClock.SynchronizeAsync();
            }
            // NOTE: enable only if you want to evaluate temporal sync. Don't enable to release builds
            // CaptureTimeMetrics(1f);
        }
        catch (Exception e)
        {
            InternalDebug.Log($"Error connecting to server: {e}");
            Toast.Show(
                "Error connecting to server. Make sure host and port is correct.",
                ToastColor.Red
            );
        }
    }

    [Serializable]
    public class TimeLogEntry
    {
        public string ntp_time;
        public string device_time;
    }

    private async void CaptureTimeMetrics(float interval_s)
    {
        while (true)
        {
            var ntpTime = clock.UtcNow;
            var deviceTime = DateTime.UtcNow;
            var logEntry = new TimeLogEntry
            {
                ntp_time = ntpTime.ToString("o"),
                device_time = deviceTime.ToString("o"),
            };
            Debug.Log($"{JsonUtility.ToJson(logEntry)}");
            await Awaitable.WaitForSecondsAsync(interval_s);
        }
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
        public CancellationTokenSource cts
        {
            get { return m_cts; }
        }
    }

    void OnCancelCreateSession()
    {
        sessionsWindow.createSessionWindow.windowGameObject.SetActive(false);
        sessionsWindow.windowGameObject.SetActive(true);
        sessionsWindow.createSessionWindow.cts.Cancel();
    }

    async void OnCreateSession()
    {
        InternalDebug.Log("Create session button pressed");
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
            InternalDebug.Log("Session name cannot be empty");
            Toast.Show("Session name cannot be empty", ToastColor.Red);
            return;
        }
        var res = new SessionMetadata
        {
            Name = sessionName,
            SavePath = string.IsNullOrEmpty(sessionSavePath) ? "" : sessionSavePath,
        };

        var createSessionRes = await grpcClient.CreateSessionAsync(
            res,
            GetDeviceInfo.GetDevice(),
            sessionSavePath,
            sessionsWindow.createSessionWindow.cts.Token
        );

        if (createSessionRes is not null)
        {
            InternalDebug.Log("Session created successfully");
            Toast.Show("Session created successfully", ToastColor.Green);
            m_ActiveSession = createSessionRes.Session;

            // Go to ARView window
            sessionsWindow.createSessionWindow.windowGameObject.SetActive(false);
            arViewWindow.windowGameObject.SetActive(true);
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
        public Button goBackButton;
        public GameObject loadingIndicator;
        public GameObject noSessionFoundText;

        public GameObject sessionElementPrefab;
        public GameObject sessionListContent;
        public Button refreshButton;
        public Button createSessionButton;
        public Button deleteSessionButton;
        public Button joinSessionButton;

        public CreateSessionWindow createSessionWindow;

        private List<SessionElement> m_sessionElements = new();

        private CancellationTokenSource m_cts = new CancellationTokenSource();
        public CancellationTokenSource cts
        {
            get { return m_cts; }
        }

        // Do not serialize this to avoid Unity reflection
        private SessionElement m_selectedSessionElement;

        [Tooltip("UI Window for creating a new session")]
        public SessionElement selectedSessionElement
        {
            get { return m_selectedSessionElement; }
            set { m_selectedSessionElement = value; }
        }

        private void OnSelectSession(SessionElement sessionElement)
        {
            m_selectedSessionElement = sessionElement;
            InternalDebug.Log($"Session selected: {sessionElement.session.Metadata.Name}");
        }

        public void AddSession(Session session)
        {
            GameObject sessionElementObject = Instantiate(
                sessionElementPrefab,
                sessionListContent.transform
            );
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
            if (loading)
            {
                noSessionFoundText.SetActive(false);
                ClearSessions();
            }

            loadingIndicator.SetActive(loading);
        }
    }

    [Tooltip("UI Window for managing sessions")]
    public SessionsWindow sessionsWindow;

    private void OnGoBackFromSessionsWindow()
    {
        sessionsWindow.ClearSessions();

        grpcClient = null;

        sessionsWindow.windowGameObject.SetActive(false);
        findServerWindow.windowGameObject.SetActive(true);
        DisposeUIConfig();
    }

    /// <summary>
    /// Search for available sessions and display them in the UI asynchronously
    /// </summary>
    async Awaitable SearchForSession()
    {
        if (grpcClient != null)
        {
            Toast.Dismiss();

            // TODO: Could race condition happen here?
            sessionsWindow.selectedSessionElement = null;
            sessionsWindow.setLoading(true);

            // Do we need to move to background thread?
            var res = await grpcClient.ListSessionsAsync();

            sessionsWindow.setLoading(false);

            if (res.Sessions.Count == 0)
            {
                InternalDebug.Log("No sessions found");
                sessionsWindow.noSessionFoundText.SetActive(true);
                return;
            }
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
            await grpcClient.DeleteSessionAsync(session.Id, sessionsWindow.cts.Token);
            await SearchForSession();
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
            InternalDebug.Log($"Joining session: {session.Metadata.Name}");
            var res = await grpcClient.JoinSessionAsync(
                session.Id,
                GetDeviceInfo.GetDevice(),
                sessionsWindow.cts.Token
            );

            m_ActiveSession = res.Session;

            //joining completeted --> switch to the next window
            sessionsWindow.windowGameObject.SetActive(false);
            arViewWindow.windowGameObject.SetActive(true);
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
        public GameObject configurationsContainer;
        public Button startPauseButton;
        public Button goBackButton;

        public Button arucoSyncButton;

        public GameObject headerTextPrefab;
        public GameObject bodyTextPrefab;
        public GameObject textInputPrefab;
        public GameObject dropdownPrefab;
        public GameObject togglePrefab;
        public DataModalityUIConfigPrefabs ConfigPrefabs =>
            new(headerTextPrefab, bodyTextPrefab, textInputPrefab, dropdownPrefab, togglePrefab);
    }

    [Tooltip("UI Window sending AR data")]
    public ARViewWindow arViewWindow;

    private void OnStartPauseButton()
    {
        m_isSending = !m_isSending;
        if (m_isSending)
        {
            arViewWindow.startPauseButton.GetComponentInChildren<TMP_Text>().text = "Pause";
            foreach (var control in m_BufferControls)
            {
                bool isModalityActive = control.config.isModalityActive;
                control.OnToggle(SendFrame)(isModalityActive);
            }
        }
        else
        {
            arViewWindow.startPauseButton.GetComponentInChildren<TMP_Text>().text = "Start";
            foreach (var control in m_BufferControls)
            {
                control.OnToggle(SendFrame)(false);
            }
        }
    }

    private async void OnGoBackFromARView()
    {
        m_isSending = false;
        foreach (var control in m_BufferControls)
        {
            control.OnToggle(SendFrame)(false);
        }

        await grpcClient.LeaveSessionAsync(m_ActiveSession.Id, m_Device);
        m_ActiveSession = null;
        arViewWindow.windowGameObject.SetActive(false);
        sessionsWindow.windowGameObject.SetActive(true);
        await SearchForSession();
    }

    // //Aruco windows
    // [Serializable]
    // public class ArucoWindow
    // {
    //     public ARFoundationCameraArUco cameraArUco;
    //     public GameObject configWindow;
    //     public Button startScanButton;
    //     public Button goBackButton;

    //     public GameObject scanWindow;
    //     public Button stopScanButton;
    //     public Button finishScanButton;

    //     public TMP_Text statusText;
    //     public TMP_Text positionText;
    //     public TMP_Text rotationText;
    // }

    // public ArucoWindow arucoWindow;

    /// <summary>
    /// The object that will be synced with the ARuco marker
    /// This object will be a helper for setting the xrorigin, since setting the position of xrorigin while syncing give undefine behaviors
    /// </summary>
    public GameObject arucoSyncObj;

    void ResetOriginPosition()
    {
        XROrigin.transform.position = UnityEngine.Vector3.zero;
        XROrigin.transform.rotation = UnityEngine.Quaternion.identity;
    }

    // void onStartScan()
    // {
    //     ResetOriginPosition();
    //     arucoWindow.cameraArUco.OnStartScanning();
    //     arucoWindow.configWindow.SetActive(false);
    //     arucoWindow.scanWindow.SetActive(true);
    //     arucoWindow.finishScanButton.interactable = false;

    //     arucoWindow.statusText.text = "To sync, scan an ArUco Marker of the type specified.";
    //     arucoWindow.positionText.text = $"";
    //     arucoWindow.rotationText.text = $"";
    // }

    // void onStopScan()
    // {
    //     arucoWindow.cameraArUco.OnStopScanning();
    //     arucoWindow.scanWindow.SetActive(false);
    //     arucoWindow.configWindow.SetActive(true);
    // }

    // void onGoBackFromAruco()
    // {
    //     arucoWindow.cameraArUco.OnStopScanning();
    //     arucoWindow.scanWindow.SetActive(false);
    //     arucoWindow.configWindow.SetActive(false);

    //     arViewWindow.windowGameObject.SetActive(true);
    // }

    // void onGoToArucoWindow()
    // {
    //     arViewWindow.windowGameObject.SetActive(false);
    //     arucoWindow.configWindow.SetActive(true);
    //     arucoWindow.scanWindow.SetActive(false);
    // }

    // void OnSpaceSynced()
    // {
    //     InternalDebug.Log($"Space synced successfully {arucoSyncObj.transform.position}");
    //     arucoWindow.finishScanButton.interactable = true;
    //     arucoWindow.statusText.text =
    //         "ArUco marker scanned. To finish syncing, press the finish button";
    //     arucoWindow.positionText.text = $"Position: {arucoSyncObj.transform.position}";
    //     arucoWindow.rotationText.text = $"Rotation: {arucoSyncObj.transform.rotation.eulerAngles}";
    // }

    // void OnFinishScan()
    // {
    //     XROrigin.transform.position = arucoSyncObj.transform.position;
    //     XROrigin.transform.rotation = arucoSyncObj.transform.rotation;

    //     onGoBackFromAruco();
    // }

    void OnApplicationQuit()
    {
        if (m_ActiveSession != null)
        {
            grpcClient.LeaveSessionAsync(m_ActiveSession.Id, m_Device);
        }
    }

    bool CheckSubsystemAvailability<T>()
        where T : class, ISubsystem
    {
        return LoaderUtility.GetActiveLoader()?.GetLoadedSubsystem<T>() != null;
    }

    private void DisposeUIConfig()
    {
        foreach (var control in m_BufferControls)
        {
            control.Dispose();
        }

        //TODO: replace this (placeholder workaround)
        foreach (Transform children in arViewWindow.configurationsContainer.transform)
        {
            Destroy(children.gameObject);
        }
        // if (m_BufferControls != null) m_BufferControls.Clear();
        // if (m_DataBuffers != null) m_DataBuffers.Clear();
    }

    /// <summary>
    /// To be called after clock has been initialized
    /// </summary>
    private void InitUIConfig()
    {
        m_AudioUIConfig = new AudioUIConfig(clock, Microphone.devices.Count() > 0);
        m_ColorUIConfig = new ColorUIConfig(cameraManager, clock);
        m_depthUIConfig = new DepthUIConfig(occlusionManager, clock);
        m_GyroscopeUIConfig = new GyroscopeUIConfig(clock, SystemInfo.supportsGyroscope);
        m_MeshDetectionUIConfig = new MeshDetectionUIConfig(meshManager, clock);
        m_PlaneDetectionUIConfig = new PlaneDetectionUIConfig(planeManager, clock);
        m_PointCloudDetectionUIConfig = new PointCloudDetectionUIConfig(pointCloudManager, clock);
        m_TransformUIConfig = new TransformUIConfig(Camera.main, clock);

        audioBufferControl = new BufferControl(m_AudioUIConfig);
        colorBufferControl = new BufferControl(m_ColorUIConfig);
        depthBufferControl = new BufferControl(m_depthUIConfig);
        gyroscopeBufferControl = new BufferControl(m_GyroscopeUIConfig);
        meshDetectionControl = new BufferControl(m_MeshDetectionUIConfig);
        planeDetectionControl = new BufferControl(m_PlaneDetectionUIConfig);
        pointCloudDetectionControl = new BufferControl(m_PointCloudDetectionUIConfig);
        transformBufferControl = new BufferControl(m_TransformUIConfig);

        m_dataModalityUIConfigs = new List<BaseDataModalityUIConfig>()
        {
            m_AudioUIConfig,
            m_ColorUIConfig,
            m_depthUIConfig,
            m_GyroscopeUIConfig,
            m_MeshDetectionUIConfig,
            m_PlaneDetectionUIConfig,
            m_PointCloudDetectionUIConfig,
            m_TransformUIConfig,
        };

        m_BufferControls = new List<BufferControl>()
        {
            audioBufferControl,
            colorBufferControl,
            depthBufferControl,
            gyroscopeBufferControl,
            meshDetectionControl,
            planeDetectionControl,
            pointCloudDetectionControl,
            transformBufferControl,
        };

        foreach (var control in m_BufferControls)
        {
            control.config.InitializeConfig(
                arViewWindow.configurationsContainer,
                arViewWindow.ConfigPrefabs,
                control.OnToggle(SendFrame)
            );
        }
    }

    void Start()
    {
        if (UnityEngine.InputSystem.Gyroscope.current != null)
        {
            InputSystem.EnableDevice(UnityEngine.InputSystem.Gyroscope.current);
        }
        if (AttitudeSensor.current != null)
        {
            InputSystem.EnableDevice(AttitudeSensor.current);
        }
        if (Accelerometer.current != null)
        {
            InputSystem.EnableDevice(Accelerometer.current);
        }
        if (GravitySensor.current != null)
        {
            InputSystem.EnableDevice(GravitySensor.current);
        }

        m_Device = GetDeviceInfo.GetDevice();

        // Initialize find server window
        findServerWindow.initFindServerWindow();
        findServerWindow.connectButton.onClick.AddListener(OnConnectToServer);

        // Initialize sessions window
        sessionsWindow.refreshButton.onClick.AddListener(async () => await SearchForSession());
        sessionsWindow.createSessionButton.onClick.AddListener(OnPressCreateSession);
        sessionsWindow.deleteSessionButton.onClick.AddListener(OnDeleteSession);
        sessionsWindow.joinSessionButton.onClick.AddListener(OnJoinSession);
        sessionsWindow.goBackButton.onClick.AddListener(OnGoBackFromSessionsWindow);

        //Inititalize create sessions window
        sessionsWindow.createSessionWindow.cancelSessionButton.onClick.AddListener(
            OnCancelCreateSession
        );
        sessionsWindow.createSessionWindow.createSessionButton.onClick.AddListener(OnCreateSession);

        // Initialize AR view window
        arViewWindow.startPauseButton.onClick.AddListener(OnStartPauseButton);
        arViewWindow.goBackButton.onClick.AddListener(OnGoBackFromARView);
        // arViewWindow.arucoSyncButton.onClick.AddListener(onGoToArucoWindow);

        // Initialize Aruco window
        // arucoWindow.startScanButton.onClick.AddListener(onStartScan);
        // arucoWindow.goBackButton.onClick.AddListener(onGoBackFromAruco);
        // arucoWindow.stopScanButton.onClick.AddListener(onStopScan);
        // arucoWindow.cameraArUco.OnSpaceSynced += OnSpaceSynced;
        // arucoWindow.finishScanButton.onClick.AddListener(OnFinishScan);
    }
}
