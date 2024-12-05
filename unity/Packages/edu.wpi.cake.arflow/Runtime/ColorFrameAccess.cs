using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

namespace CakeLab.ARFlow.Samples
{
    using Clock;
    using DataBuffers;
    using Grpc;
    using Grpc.V1;
    using Utilities;

    public class ColorFrameAccess : MonoBehaviour
    {
        public Button CreateSessionButton;
        public Button DeleteSessionButton;
        public Button RefreshSessionsButton; // ListSessions
        public Button JoinSessionButton;
        public Button LeaveSessionButton;
        public Button StartButton;
        public Button StopButton;
        public TMP_InputField AddressInputField;
        public TMP_InputField DelayInputField;
        public TMP_Text StatusText;

        ColorBuffer m_CameraBuffer;

        private Device m_Device;
        private List<Session> m_Sessions;
        private Session m_ActiveSession;
        private Uri m_Address;
        private IGrpcClient m_GrpcClient;
        private float m_DelayInS;
        private CancellationTokenSource m_Cts;

        [SerializeField]
        [Tooltip("The AR camera manager that is used to produce frame events")]
        ARCameraManager m_CameraManager;

        [SerializeField]
        IClock m_Clock;

        public ARCameraManager CameraManager
        {
            // Proxy to underlying ARCameraManager
            get => m_CameraBuffer.CameraManager;
            set => m_CameraBuffer.CameraManager = value;
        }

        /// <summary>
        /// Awake is called when the script instance is being loaded, before any Start.
        /// </summary>
        void Awake()
        {
            m_CameraBuffer = new ColorBuffer(64, m_CameraManager, m_Clock);

            // Initialize default values (if they aren't dynamic or coming from other scripts)
            m_Address = new("http://192.168.1.50:8500");
            m_DelayInS = 0.5f;

            // Set initial state for UI components (safe to assign references)
            StartButton.interactable = false;
            StopButton.interactable = false;
            AddressInputField.interactable = false;
            DelayInputField.interactable = false;
        }

        /// <summary>
        /// Start is called before the first frame update
        /// </summary>
        void Start()
        {
            m_Device = new()
            {
                Model = SystemInfo.deviceModel,
                Name = SystemInfo.deviceName,
                Type = (Device.Types.Type)SystemInfo.deviceType,
                Uid = SystemInfo.deviceUniqueIdentifier,
            };

            // Set up initial state of UI (now interactable)
            StartButton.interactable = true;
            StopButton.interactable = false;
            AddressInputField.interactable = true;
            DelayInputField.interactable = true;

            // Populate text fields with initial values
            AddressInputField.text = m_Address.ToString();
            DelayInputField.text = m_DelayInS.ToString();
            StatusText.text = "Disconnected";

            // Add UI listeners
            StartButton.onClick.AddListener(OnStartButtonClicked);
            StopButton.onClick.AddListener(OnStopButtonClicked);
            AddressInputField.onValueChanged.AddListener(OnAddressChange);
            DelayInputField.onValueChanged.AddListener(OnDelayChange);
        }

        private void OnStartButtonClicked()
        {
            m_GrpcClient = new GrpcClient(m_Address.ToString());
            m_CameraBuffer.StartCapture();
            m_Cts = new CancellationTokenSource();
            SendFramesAsync();

            StartButton.interactable = false;
            StopButton.interactable = true;
            AddressInputField.interactable = false;
            DelayInputField.interactable = false;

            StatusText.text = $"Sending frames every {m_DelayInS}s to {m_Address}";
        }

        private void OnStopButtonClicked()
        {
            m_Cts.Cancel();
            m_CameraBuffer.StopCapture();
            m_CameraBuffer.ClearBuffer();
            m_GrpcClient?.Dispose();
            m_GrpcClient = null; // to reconnect if needed

            StartButton.interactable = true;
            StopButton.interactable = false;
            AddressInputField.interactable = true;
            DelayInputField.interactable = true;

            StatusText.text = "Disconnected";
        }

        private async void SendFramesAsync()
        {
            while (!m_Cts.IsCancellationRequested)
            {
                // OperationCanceledException is thrown when the token is cancelled, this is expected
                // For more details, see https://blog.stephencleary.com/2022/02/cancellation-1-overview.html
                await Awaitable.WaitForSecondsAsync(m_DelayInS, m_Cts.Token);

                ARFrame[] arFrames = m_CameraBuffer
                    .Buffer
                    // This works because we have an explicit conversion operator defined for RawCameraFrame
                    .Select(frame => (ARFrame)frame)
                    .ToArray();

                if (arFrames.Length == 0)
                {
                    InternalDebug.Log("No frames to send.");
                    continue;
                }

                var _ = await m_GrpcClient.SaveARFramesAsync(
                    m_ActiveSession.Id,
                    arFrames,
                    m_Device,
                    m_Cts.Token
                );
                m_CameraBuffer.ClearBuffer();
            }
        }

        private void OnAddressChange(string value)
        {
            if (Uri.TryCreate(value, UriKind.Absolute, out Uri newAddress))
            {
                m_Address = newAddress;
            }
        }

        private void OnDelayChange(string value)
        {
            if (float.TryParse(value, out float newDelay))
            {
                m_DelayInS = newDelay;
            }
        }

        private void OnDestroy()
        {
            m_Cts?.Cancel();
            m_Cts?.Dispose();
            m_GrpcClient?.Dispose();
        }
    }
}
