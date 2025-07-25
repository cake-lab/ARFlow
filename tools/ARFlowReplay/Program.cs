using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Grpc.Net.Client;
using Google.Protobuf;
using ARFlow;

namespace ARFlowReplay
{
    class MultiFrameReplay
    {
        static async Task Main(string[] args)
        {
            // Configuration
            string serverUrl = "http://localhost:8500";
            string frameDirectory = @"C:\Users\alexr\Desktop\ARFlowData"; // Directory containing frame files
            int delayBetweenFrames = 33; // milliseconds (30 FPS)

            Console.WriteLine("ARFlow Multi-Frame Replay Client");
            Console.WriteLine("================================");

            try
            {
                // Get all frame files
                var frameFiles = Directory.GetFiles(frameDirectory, "frame_*.bin")
                                        .OrderBy(f => f)
                                        .ToList();

                Console.WriteLine($"Found {frameFiles.Count} frame files in {frameDirectory}");

                if (frameFiles.Count == 0)
                {
                    Console.WriteLine("No frame files found!");
                    return;
                }

                // Create gRPC channel and client
                AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                var channel = GrpcChannel.ForAddress(serverUrl);
                var client = new ARFlowService.ARFlowServiceClient(channel);
                Console.WriteLine($"Connected to server at {serverUrl}");

                // Register with the server
                var registerRequest = CreateMockRegisterRequest();
                var registerResponse = client.register(registerRequest);
                string sessionId = registerResponse.Uid;
                Console.WriteLine($"Registered with server. Session ID: {sessionId}");

                // Send frames
                int successCount = 0;
                int errorCount = 0;

                Console.WriteLine($"\nStarting replay of {frameFiles.Count} frames...");
                Console.WriteLine("Press Ctrl+C to stop\n");

                foreach (var frameFile in frameFiles)
                {
                    try
                    {
                        // Load frame
                        var (success, frameData, metadata) = await LoadFrameFromFile(frameFile);
                        if (!success)
                        {
                            Console.WriteLine($"Failed to load: {Path.GetFileName(frameFile)}");
                            errorCount++;
                            continue;
                        }

                        // Update session ID
                        frameData.Uid = sessionId;

                        // Send frame
                        var response = client.data_frame(frameData);
                        successCount++;

                        // Progress indicator
                        Console.Write($"\rSent: {successCount}/{frameFiles.Count} frames (Errors: {errorCount})");

                        // Delay between frames
                        await Task.Delay(delayBetweenFrames);
                    }
                    catch (Exception e)
                    {
                        errorCount++;
                        Console.WriteLine($"\nError sending {Path.GetFileName(frameFile)}: {e.Message}");
                    }
                }

                Console.WriteLine($"\n\nReplay complete! Sent {successCount} frames successfully.");

                // Cleanup
                await channel.ShutdownAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                Console.WriteLine($"Stack trace: {e.StackTrace}");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        static RegisterRequest CreateMockRegisterRequest()
        {
            return new RegisterRequest
            {
                DeviceName = "Replay Client - Galaxy S9+",
                CameraIntrinsics = new RegisterRequest.Types.CameraIntrinsics
                {
                    FocalLengthX = 615.0f,
                    FocalLengthY = 615.0f,
                    ResolutionX = 640,
                    ResolutionY = 480,
                    PrincipalPointX = 320.0f,
                    PrincipalPointY = 240.0f,
                },
                CameraColor = new RegisterRequest.Types.CameraColor
                {
                    Enabled = true,
                    DataType = "YCbCr420",
                    ResizeFactorX = 0.25f,
                    ResizeFactorY = 0.25f,
                },
                CameraDepth = new RegisterRequest.Types.CameraDepth
                {
                    Enabled = true,
                    DataType = "u16",
                    ConfidenceFilteringLevel = 0,
                    ResolutionX = 160,
                    ResolutionY = 120
                },
                CameraTransform = new RegisterRequest.Types.CameraTransform
                {
                    Enabled = true
                },
                CameraPointCloud = new RegisterRequest.Types.CameraPointCloud
                {
                    Enabled = true,
                    DepthUpscaleFactor = 1.0f,
                }
            };
        }

        static async Task<(bool success, DataFrameRequest frameData, Dictionary<string, object> metadata)>
            LoadFrameFromFile(string filePath)
        {
            try
            {
                using var fileStream = File.OpenRead(filePath);

                // Read metadata size
                var metadataSizeBytes = new byte[4];
                await fileStream.ReadAsync(metadataSizeBytes, 0, 4);
                var metadataSize = BitConverter.ToInt32(metadataSizeBytes, 0);

                // Read metadata
                var metadataBytes = new byte[metadataSize];
                await fileStream.ReadAsync(metadataBytes, 0, metadataSize);
                var metadataJson = System.Text.Encoding.UTF8.GetString(metadataBytes);

                var metadata = new Dictionary<string, object> { ["raw"] = metadataJson };

                // Read frame data
                var frameDataLength = (int)(fileStream.Length - fileStream.Position);
                var frameBytes = new byte[frameDataLength];
                await fileStream.ReadAsync(frameBytes, 0, frameDataLength);

                var frameData = DataFrameRequest.Parser.ParseFrom(frameBytes);

                return (true, frameData, metadata);
            }
            catch (Exception e)
            {
                return (false, null, null);
            }
        }
    }
}
