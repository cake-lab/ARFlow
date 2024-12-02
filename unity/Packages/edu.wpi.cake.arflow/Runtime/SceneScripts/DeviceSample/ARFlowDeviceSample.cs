using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using CakeLab.ARFlow.DataBuffers;
using CakeLab.ARFlow.DataBuffers.DataModalityUIConfig;
using CakeLab.ARFlow.DataModalityUIConfig;
using CakeLab.ARFlow.Grpc;
using CakeLab.ARFlow.Grpc.V1;
using CakeLab.ARFlow.Utilities;
using EasyUI.Toast;
using TMPro;
using Unity.Android.Gradle.Manifest;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

public class ARFlowDeviceSample : MonoBehaviour
{
    [Tooltip("Camera image data's manager from the device camera")]
    public ARCameraManager cameraManager;

    [Tooltip("Depth data's manager from the device camera")]
    public AROcclusionManager occlusionManager;

    public ARPlaneManager planeManager;

    public ARMeshManager meshManager;

    public IGrpcClient grpcClient;


    private ColorBuffer m_ColorBuffer;
    private ColorUIConfig m_ColorUIConfig;
    private CancellationTokenSource m_colorCts;



    private List<IDataBuffer> m_dataBuffers;

    private List<IDataModalityUIConfig> m_dataModalityUIConfigs;
    private List<CancellationTokenSource> m_ctsList = new List<CancellationTokenSource>();

    private Session m_ActiveSession;
    private Device m_Device;

    private bool m_isSending = false;

    [Serializable]
    [Tooltip("UI Window for finding server")]
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
        findServerWindow.windowGameObject.SetActive(false);
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
            await Awaitable.MainThreadAsync();
            InternalDebug.Log("Session name cannot be empty");
            Toast.Show("Session name cannot be empty", ToastColor.Red);
            return;
        }
        var res = new SessionMetadata
        {
            Name = sessionName,
            SavePath = string.IsNullOrEmpty(sessionSavePath) ? "" : sessionSavePath,
        };

        // TODO: Do we need to move to background thread?
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
            sessionsWindow.createSessionWindow.windowGameObject.SetActive(false);

            // Go to ARView window
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
            InternalDebug.Log($"Joining session: {session.Metadata.Name}");
            await grpcClient.JoinSessionAsync(
                session.Id,
                GetDeviceInfo.GetDevice(),
                sessionsWindow.cts.Token
            );

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
        public GameObject configurationsContainer;
        public Button startPauseButton;
        public Button goBackButton;

        public GameObject headerTextPrefab;
        public GameObject bodyTextPrefab;
        public GameObject textInputPrefab;
        public GameObject dropdownPrefab;
        public GameObject togglePrefab;
        public DataBufferUIConfigPrefabs ConfigPrefabs => new(
                headerTextPrefab,
                bodyTextPrefab,
                textInputPrefab,
                dropdownPrefab,
                togglePrefab
            );
    }

    [Tooltip("UI Window sending AR data")]
    public ARViewWindow arViewWindow;

    private void onStartPauseButton()
    {
        if (m_isSending)
        {
            m_isSending = false;
            arViewWindow.startPauseButton.GetComponentInChildren<TMP_Text>().text = "Start";
            m_dataBuffers.ForEach(buffer => buffer.StopCapture());
            m_dataModalityUIConfigs.ForEach(config => config.TurnOnConfig());
            m_ctsList.ForEach(cts =>
            {
                cts.Cancel();
                cts.Dispose();
                cts = new CancellationTokenSource();
            });
        }
        else
        {
            m_isSending = true;
            arViewWindow.startPauseButton.GetComponentInChildren<TMP_Text>().text = "Pause";
            m_ctsList.ForEach(cts =>
            {
                cts = new CancellationTokenSource();
            });
            m_dataBuffers.ForEach(buffer => buffer.StartCapture());
            m_dataModalityUIConfigs.ForEach(config => config.TurnOffConfig());

            //start individual buffer sending
            SendCameraFrames();
        }
    }

    private async void SendCameraFrames()
    {
        while (!m_colorCts.Token.IsCancellationRequested)
        {
            // Get delay from the UI. Data is validated through TMP's content type settings
            float currentDelay = float.Parse(m_ColorUIConfig.GetDelayField().text);
            if (currentDelay <= 0)
            {
                currentDelay = 0.5f;
            }
            // OperationCanceledException is thrown when the token is cancelled, this is expected
            // For more details, see https://blog.stephencleary.com/2022/02/cancellation-1-overview.html
            await Awaitable.WaitForSecondsAsync(currentDelay, m_colorCts.Token);

            ARFrame[] arFrames = m_ColorBuffer
                .Buffer
                // This works because we have an explicit conversion operator defined for RawCameraFrame
                .Select(frame => (ARFrame)frame)
                .ToArray();

            if (arFrames.Length == 0)
            {
                InternalDebug.Log("No frames to send.");
                continue;
            }

            var _ = await grpcClient.SaveARFramesAsync(
                m_ActiveSession.Id,
                arFrames,
                m_Device,
                m_colorCts.Token
            );
            m_ColorBuffer.ClearBuffer();
        }
    }

    void Start()
    {
        // Initialize data buffers and sending-related vaiables
        m_ColorBuffer = new ColorBuffer(64, cameraManager);
        m_dataBuffers = new List<IDataBuffer>()
        {
            m_ColorBuffer,
            // new DepthBuffer(occlusionManager),
            // new PlaneBuffer(planeManager),
            // new MeshBuffer(meshManager)
        };

        m_colorCts = new CancellationTokenSource();
        m_ctsList.Add(m_colorCts);

        m_ColorUIConfig = new ColorUIConfig(
            arViewWindow.configurationsContainer,
            arViewWindow.ConfigPrefabs,
            (bool isOn) =>
            {
                if (isOn)
                {
                    cameraManager.enabled = false;
                    InternalDebug.Log("Enable camera manager");
                }
                else
                {
                    InternalDebug.Log("Disable camera manager");
                    m_ColorBuffer.StopCapture();
                }
            }
        );
        m_dataModalityUIConfigs = new List<IDataModalityUIConfig>()
        {
            m_ColorUIConfig,
            // new DepthBuffer(occlusionManager),
            // new PlaneBuffer(planeManager),
            // new MeshBuffer(meshManager)
        };

        // Initialize find server window
        findServerWindow.connectButton.onClick.AddListener(OnConnectToServer);

        // Initialize sessions window
        sessionsWindow.refreshButton.onClick.AddListener(SearchForSession);
        sessionsWindow.createSessionButton.onClick.AddListener(OnPressCreateSession);
        sessionsWindow.deleteSessionButton.onClick.AddListener(OnDeleteSession);
        sessionsWindow.joinSessionButton.onClick.AddListener(OnJoinSession);

        //Inititalize create sessions window
        sessionsWindow.createSessionWindow.cancelSessionButton.onClick.AddListener(
            OnCancelCreateSession
        );
        sessionsWindow.createSessionWindow.createSessionButton.onClick.AddListener(OnCreateSession);

        // Initialize AR view window
        arViewWindow.startPauseButton.onClick.AddListener(onStartPauseButton);
    }
}
