using System;
using Cysharp.Net.Http;
using Grpc.Net.Client;
using UnityEngine;

namespace ARFlow
{
    public class ARFlowClient
    {
        private readonly GrpcChannel _channel;
        private readonly ARFlowService.ARFlowServiceClient _client;
        private string _sessionId;

        public ARFlowClient(string address)
        {
            var handler = new YetAnotherHttpHandler() {Http2Only = true};
            _channel = GrpcChannel.ForAddress(address, new GrpcChannelOptions()
            {
                HttpHandler = handler,
                MaxReceiveMessageSize = 4194304 * 10
            });
            _client = new ARFlowService.ARFlowServiceClient(_channel);
        }

        ~ARFlowClient()
        {
            _channel.Dispose();
        }
        
        public void Connect(RegisterRequest requestData)
        {
            try
            {
                var response = _client.register(requestData);
                _sessionId = response.Message;

                Debug.Log(response.Message);
            }
            catch (Exception e)
            {
                // Try to catch any exceptions.
                // Network, device image, camera intrinsics
                Debug.LogError(e);
            }
        }

        public void SendFrame(DataFrameRequest frameData)
        {
            frameData.Uid = _sessionId;
            try
            {
                var response = _client.data_frame(frameData);
                Debug.Log(response);
            }
            catch (Exception e)
            {
                // Try to catch any exceptions.
                // Network, device image, camera intrinsics
                Debug.LogError(e);
            }
        }
    }
}
