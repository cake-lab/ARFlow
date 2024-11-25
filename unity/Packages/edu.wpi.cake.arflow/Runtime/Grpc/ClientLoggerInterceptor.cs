// Vendored from https://github.com/grpc/grpc-dotnet/blob/master/examples/Interceptor/Client/ClientLoggerInterceptor.cs. We have made modifications to the original source code.
#region Copyright notice and license

// Copyright 2019 The gRPC Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using System;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;
using UnityEngine;

namespace CakeLab.ARFlow.Grpc
{
    using Utilities;

    public class ClientLoggerInterceptor : Interceptor
    {
        public ClientLoggerInterceptor() { }

        public override TResponse BlockingUnaryCall<TRequest, TResponse>(
            TRequest request,
            ClientInterceptorContext<TRequest, TResponse> context,
            BlockingUnaryCallContinuation<TRequest, TResponse> continuation
        )
        {
            LogCall(context.Method);
            AddCallerMetadata(ref context);

            try
            {
                return continuation(request, context);
            }
            catch (Exception ex)
            {
                LogException(ex);
                throw;
            }
        }

        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
            TRequest request,
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncUnaryCallContinuation<TRequest, TResponse> continuation
        )
        {
            LogCall(context.Method);
            AddCallerMetadata(ref context);

            try
            {
                var call = continuation(request, context);

                return new AsyncUnaryCall<TResponse>(
                    HandleResponse(call.ResponseAsync),
                    call.ResponseHeadersAsync,
                    call.GetStatus,
                    call.GetTrailers,
                    call.Dispose
                );
            }
            catch (Exception ex)
            {
                LogException(ex);
                throw;
            }
        }

        private async Task<TResponse> HandleResponse<TResponse>(Task<TResponse> t)
        {
            try
            {
                var response = await t;
                InternalDebug.Log($"Response received: {response}");
                return response;
            }
            catch (Exception ex)
            {
                LogException(ex);
                throw;
            }
        }

        public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<
            TRequest,
            TResponse
        >(
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncClientStreamingCallContinuation<TRequest, TResponse> continuation
        )
        {
            LogCall(context.Method);
            AddCallerMetadata(ref context);

            try
            {
                return continuation(context);
            }
            catch (Exception ex)
            {
                LogException(ex);
                throw;
            }
        }

        public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<
            TRequest,
            TResponse
        >(
            TRequest request,
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation
        )
        {
            LogCall(context.Method);
            AddCallerMetadata(ref context);

            try
            {
                return continuation(request, context);
            }
            catch (Exception ex)
            {
                LogException(ex);
                throw;
            }
        }

        public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<
            TRequest,
            TResponse
        >(
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncDuplexStreamingCallContinuation<TRequest, TResponse> continuation
        )
        {
            LogCall(context.Method);
            AddCallerMetadata(ref context);

            try
            {
                return continuation(context);
            }
            catch (Exception ex)
            {
                LogException(ex);
                throw;
            }
        }

        private void LogCall<TRequest, TResponse>(Method<TRequest, TResponse> method)
            where TRequest : class
            where TResponse : class
        {
            InternalDebug.Log(
                $"Starting call. Name: {method.Name}. Type: {method.Type}. Request: {typeof(TRequest)}. Response: {typeof(TResponse)}"
            );
        }

        private void AddCallerMetadata<TRequest, TResponse>(
            ref ClientInterceptorContext<TRequest, TResponse> context
        )
            where TRequest : class
            where TResponse : class
        {
            var headers = context.Options.Headers;

            // Call doesn't have a headers collection to add to.
            // Need to create a new context with headers for the call.
            if (headers == null)
            {
                headers = new Metadata();
                var options = context.Options.WithHeaders(headers);
                context = new ClientInterceptorContext<TRequest, TResponse>(
                    context.Method,
                    context.Host,
                    options
                );
            }

            // Add caller metadata to call headers
            headers.Add("caller-user", Environment.UserName);
            headers.Add("caller-machine", Environment.MachineName);
            headers.Add("caller-os", Environment.OSVersion.ToString());
        }

        private void LogException(Exception ex)
        {
            InternalDebug.LogException(ex);
        }
    }
}
