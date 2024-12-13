using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Net.Http;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using UnityEngine;
using Grpc.Core;

namespace CakeLab.ARFlow.Grpc
{
    using Grpc.V1;
    using Utilities;

    public interface IGrpcClient : IDisposable
    {
        Awaitable<CreateSessionResponse> CreateSessionAsync(
            SessionMetadata sessionMetadata,
            Device device,
            string savePath = "",
            CancellationToken cancellationToken = default
        );
        Awaitable<DeleteSessionResponse> DeleteSessionAsync(
            SessionUuid sessionId,
            CancellationToken cancellationToken = default
        );
        Awaitable<GetSessionResponse> GetSessionAsync(
            SessionUuid sessionId,
            CancellationToken cancellationToken = default
        );
        Awaitable<ListSessionsResponse> ListSessionsAsync(
            CancellationToken cancellationToken = default
        );
        Awaitable<JoinSessionResponse> JoinSessionAsync(
            SessionUuid sessionId,
            Device device,
            CancellationToken cancellationToken = default
        );
        Awaitable<LeaveSessionResponse> LeaveSessionAsync(
            SessionUuid sessionId,
            Device device,
            CancellationToken cancellationToken = default
        );
        Awaitable<SaveARFramesResponse> SaveARFramesAsync(
            SessionUuid sessionId,
            IEnumerable<ARFrame> arFrames,
            Device device,
            CancellationToken cancellationToken = default
        );
        Awaitable<RegisterIntrinsicsResponse> RegisterIntrinsicsAsync(
            SessionUuid sessionId,
            Device device,
            Google.Protobuf.WellKnownTypes.Timestamp timestamp,
            Intrinsics intrinsics,
            CancellationToken cancellationToken = default
        );
    }

    /// <summary>
    /// gRPC client that reuses the channel for multiple requests, but will create a new client for each request. This is more performant than creating a new channel for each request or even reusing both the channel and the client for each request.
    /// </summary>
    public class GrpcClient : IGrpcClient
    {
        private readonly GrpcChannel m_Channel;
        private readonly CallInvoker m_Invoker;

        public GrpcClient(string serverUrl)
        {
            InternalDebug.Log($"Connecting to {serverUrl}");
            m_Channel = GrpcChannel.ForAddress(
                serverUrl,
                new GrpcChannelOptions()
                {
                    HttpHandler = new YetAnotherHttpHandler()
                    {
                        // https://scientificprogrammer.net/2022/06/05/performance-best-practices-for-using-grpc-on-net/
                        PoolIdleTimeout = Timeout.InfiniteTimeSpan,
                        Http2KeepAliveInterval = TimeSpan.FromSeconds(60),
                        Http2KeepAliveTimeout = TimeSpan.FromSeconds(30),
                        // https://github.com/Cysharp/yetanotherhttphandler?tab=readme-ov-file#using-http2-over-cleartext-h2c
                        Http2Only = true,
                        // Http2MaxSendBufferSize = 8192,
                    },
                    // https://github.com/Cysharp/yetanotherhttphandler?tab=readme-ov-file#using-grpcchannel-with-yetanotherhttphandler
                    DisposeHttpClient = true,
                }
            );
            m_Invoker = m_Channel.Intercept(new ClientLoggerInterceptor());
        }

        public void Dispose()
        {
            m_Channel.Dispose();
        }

        public async Awaitable<CreateSessionResponse> CreateSessionAsync(SessionMetadata sessionMetadata, Device device, string savePath = "", CancellationToken cancellationToken = default)
        {
            var client = new ARFlowService.ARFlowServiceClient(m_Invoker);
            var request = new CreateSessionRequest
            {
                SessionMetadata = sessionMetadata,
                Device = device,
            };
            if (!string.IsNullOrEmpty(savePath))
            {
                request.SessionMetadata.SavePath = savePath;
            }
            var response = await client.CreateSessionAsync(
                request,
                cancellationToken: cancellationToken
            );
            return response;
        }

        public async Awaitable<DeleteSessionResponse> DeleteSessionAsync(SessionUuid sessionId, CancellationToken cancellationToken = default)
        {
            var client = new ARFlowService.ARFlowServiceClient(m_Invoker);
            var request = new DeleteSessionRequest
            {
                SessionId = sessionId,
            };
            var response = await client.DeleteSessionAsync(
                request,
                cancellationToken: cancellationToken
            );
            return response;
        }

        public async Awaitable<GetSessionResponse> GetSessionAsync(SessionUuid sessionId, CancellationToken cancellationToken = default)
        {
            var client = new ARFlowService.ARFlowServiceClient(m_Invoker);
            var request = new GetSessionRequest
            {
                SessionId = sessionId,
            };
            var response = await client.GetSessionAsync(
                request,
                cancellationToken: cancellationToken
            );
            return response;
        }

        public async Awaitable<JoinSessionResponse> JoinSessionAsync(SessionUuid sessionId, Device device, CancellationToken cancellationToken = default)
        {
            var client = new ARFlowService.ARFlowServiceClient(m_Invoker);
            var request = new JoinSessionRequest
            {
                SessionId = sessionId,
                Device = device,
            };
            var response = await client.JoinSessionAsync(
                request,
                cancellationToken: cancellationToken
            );
            return response;
        }

        public async Awaitable<LeaveSessionResponse> LeaveSessionAsync(SessionUuid sessionId, Device device, CancellationToken cancellationToken = default)
        {
            var client = new ARFlowService.ARFlowServiceClient(m_Invoker);
            var request = new LeaveSessionRequest
            {
                SessionId = sessionId,
                Device = device,
            };
            var response = await client.LeaveSessionAsync(
                request,
                cancellationToken: cancellationToken
            );
            return response;
        }

        public async Awaitable<ListSessionsResponse> ListSessionsAsync(CancellationToken cancellationToken = default)
        {
            var client = new ARFlowService.ARFlowServiceClient(m_Invoker);
            var request = new ListSessionsRequest();
            var response = await client.ListSessionsAsync(
                request,
                cancellationToken: cancellationToken
            );
            return response;
        }

        public async Awaitable<SaveARFramesResponse> SaveARFramesAsync(
            SessionUuid sessionId,
            IEnumerable<ARFrame> arFrames,
            Device device,
            CancellationToken cancellationToken = default
        )
        {
            var client = new ARFlowService.ARFlowServiceClient(m_Invoker);
            var request = new SaveARFramesRequest
            {
                SessionId = sessionId,
                Device = device,
            };
            request.Frames.AddRange(arFrames);
            var response = await client.SaveARFramesAsync(
                request,
                cancellationToken: cancellationToken
            );
            return response;
        }

        public async Awaitable<RegisterIntrinsicsResponse> RegisterIntrinsicsAsync(
            SessionUuid sessionId,
            Device device,
            Google.Protobuf.WellKnownTypes.Timestamp timestamp,
            Intrinsics intrinsics,
            CancellationToken cancellationToken = default
        )
        {
            var client = new ARFlowService.ARFlowServiceClient(m_Invoker);
            var request = new RegisterIntrinsicsRequest
            {
                SessionId = sessionId,
                Device = device,
                DeviceTimestamp = timestamp,
                Intrinsics = intrinsics,
            };
            var response = await client.RegisterIntrinsicsAsync(
                request,
                cancellationToken: cancellationToken
            );
            return response;
        }
    }
}