using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using NativeWebSocket;
using MessagePack;

namespace ARFlow
{
    public class ARFlowClient
    {
        private WebSocket _websocket;
        private string _sessionId;
        private string _serverAddress;

        // Callback event when Session UUID is retrieved from Server
        public event Action<string> OnSessionConnected;

        public ARFlowClient(string address)
        {
            // Replace http:// with ws:// if necessary
            if (address.StartsWith("http://"))
            {
                address = address.Replace("http://", "ws://");
            }
            else if (address.StartsWith("https://"))
            {
                address = address.Replace("https://", "wss://");
            }
            
            _serverAddress = address;
            Debug.Log("Initializing WebSocket client for " + _serverAddress);
        }

        ~ARFlowClient()
        {
            DisconnectAsync();
        }

        public async void Connect(CreateSessionRequestMsg requestData)
        {
            _websocket = new WebSocket(_serverAddress);

            _websocket.OnOpen += () =>
            {
                Debug.Log("WebSocket Connection Open!");
                // 1. Connection opened, immediately send CreateSession Request
                SendSessionRequest(requestData);
            };

            _websocket.OnError += (e) =>
            {
                Debug.LogError("WebSocket Error: " + e);
            };

            _websocket.OnClose += (e) =>
            {
                Debug.Log("WebSocket Connection Closed!");
            };

            _websocket.OnMessage += (bytes) =>
            {
                // Handle receiving byte arrays from the Server
                ProcessServerMessage(bytes);
            };

            // Connect to the server
            await _websocket.Connect();
        }

        public void Update()
        {
            // NativeWebSocket needs this in Unity's Update() to dispatch callbacks
#if !UNITY_WEBGL || UNITY_EDITOR
            _websocket?.DispatchMessageQueue();
#endif
        }

        public async void DisconnectAsync()
        {
            if (_websocket != null && _websocket.State == WebSocketState.Open)
            {
                await _websocket.Close();
            }
        }

        private async void SendSessionRequest(CreateSessionRequestMsg request)
        {
            try
            {
                byte[] buffer = SensorFlex.Encode(request, MessageTypeId.CreateSessionRequestMsg);
                await _websocket.Send(buffer);
            }
            catch (Exception e)
            {
                Debug.LogError("Error Sending Session Request: " + e.Message);
            }
        }

        private void ProcessServerMessage(byte[] bytes)
        {
            try
            {
                // Unpack Server Response (Assume 9-byte SF header)
                if (bytes.Length > 9 && bytes[0] == 'S' && bytes[1] == 'F')
                {
                    // Read Type ID (Big Endian at bytes 3 and 4)
                    ushort typeId = (ushort)((bytes[3] << 8) | bytes[4]);

                    // Extract actual MessagePack Payload
                    byte[] payload = new byte[bytes.Length - 9];
                    Buffer.BlockCopy(bytes, 9, payload, 0, payload.Length);

                    if (typeId == (ushort)MessageTypeId.CreateSessionResponseMsg)
                    {
                        var response = MessagePackSerializer.Deserialize<CreateSessionResponseMsg>(payload);
                        _sessionId = response.Session.Id.Value;
                        Debug.Log("Successfully created session: " + _sessionId);
                        OnSessionConnected?.Invoke(_sessionId);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to deserialize Server Message: " + e);
            }
        }

        public async void SendColorFrame(ColorFrameMsg frame)
        {
            if (_websocket == null || _websocket.State != WebSocketState.Open) return;

            try
            {
                byte[] buffer = SensorFlex.Encode(frame, MessageTypeId.ColorFrameMsg);
                await _websocket.Send(buffer);
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to send Color Frame: " + e.Message);
            }
        }

        public async void SendDepthFrame(DepthFrameMsg frame)
        {
            if (_websocket == null || _websocket.State != WebSocketState.Open) return;

            try
            {
                byte[] buffer = SensorFlex.Encode(frame, MessageTypeId.DepthFrameMsg);
                await _websocket.Send(buffer);
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to send Depth Frame: " + e.Message);
            }
        }
    }
}
