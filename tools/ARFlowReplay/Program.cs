using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Grpc.Net.Client;
using Newtonsoft.Json;

namespace ARFlowReplay
{
    // Main replay application
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("ARFlow Data Replay Tool");
            Console.WriteLine("=======================\n");

            // Get configuration
            Console.Write("Enter the data folder path: ");
            var dataPath = Console.ReadLine();

            Console.Write("Enter server URL (e.g., http://192.168.1.219:8500): ");
            var serverUrl = Console.ReadLine();

            Console.Write("Enter replay speed (1.0 = normal, 2.0 = double speed): ");
            var speedMultiplier = double.Parse(Console.ReadLine() ?? "1.0");

            Console.Write("Loop playback? (y/n): ");
            var loop = Console.ReadLine()?.ToLower() == "y";

            // Create replay engine
            var replayEngine = new ARFlowReplayEngine(dataPath, serverUrl);

            // Start replay
            await replayEngine.StartReplay(speedMultiplier, loop);
        }
    }

    // Main replay engine
    public class ARFlowReplayEngine
    {
        private readonly string _dataPath;
        private readonly ARFlowClient _client;
        private readonly List<FrameFile> _frameFiles;
        private bool _isPlaying;
        private CancellationTokenSource _cancellationTokenSource;

        public ARFlowReplayEngine(string dataPath, string serverUrl)
        {
            _dataPath = dataPath;
            _client = new ARFlowClient(serverUrl);
            _frameFiles = new List<FrameFile>();
            LoadFrameFiles();
        }

        private void LoadFrameFiles()
        {
            if (!Directory.Exists(_dataPath))
            {
                throw new DirectoryNotFoundException($"Data path not found: {_dataPath}");
            }

            var files = Directory.GetFiles(_dataPath, "*.bin")
                .OrderBy(f => f)
                .ToList();

            Console.WriteLine($"Found {files.Count} frame files");

            foreach (var file in files)
            {
                _frameFiles.Add(new FrameFile
                {
                    FilePath = file,
                    FileName = Path.GetFileName(file)
                });
            }
        }

        public async Task StartReplay(double speedMultiplier = 1.0, bool loop = false)
        {
            if (_frameFiles.Count == 0)
            {
                Console.WriteLine("No frame files found!");
                return;
            }

            _cancellationTokenSource = new CancellationTokenSource();
            _isPlaying = true;

            // First, we need to register with the server
            // We'll read the first frame to get camera intrinsics info
            var firstFrame = await LoadFrame(_frameFiles[0].FilePath);
            if (firstFrame.frameData != null)
            {
                // Create a registration request (we'll use dummy values since we're replaying)
                var registerRequest = new RegisterRequest
                {
                    DeviceName = "ARFlow Replay Tool",
                    CameraIntrinsics = new RegisterRequest.Types.CameraIntrinsics
                    {
                        FocalLengthX = 500.0f,  // These should match original device
                        FocalLengthY = 500.0f,
                        ResolutionX = 640f,
                        ResolutionY = 480f,
                        PrincipalPointX = 320f,
                        PrincipalPointY = 240f,
                    },
                    CameraColor = new RegisterRequest.Types.CameraColor
                    {
                        Enabled = true,
                        DataType = "YCbCr420",
                        ResizeFactorX = 1.0f,
                        ResizeFactorY = 1.0f,
                    },
                    CameraDepth = new RegisterRequest.Types.CameraDepth
                    {
                        Enabled = true,
                        DataType = "u16", // or "f32" for iOS
                        ConfidenceFilteringLevel = 0,
                        ResolutionX = 640,
                        ResolutionY = 480
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

                _client.Connect(registerRequest);
                Console.WriteLine("Connected to server");
            }

            // Setup keyboard listener for controls
            var controlTask = Task.Run(() => ListenForControls(_cancellationTokenSource.Token));

            do
            {
                Console.WriteLine($"\nStarting replay of {_frameFiles.Count} frames...");
                var frameIndex = 0;
                var lastFrameTime = DateTime.Now;

                foreach (var frameFile in _frameFiles)
                {
                    if (_cancellationTokenSource.Token.IsCancellationRequested)
                        break;

                    try
                    {
                        var (success, frameData, metadata) = await LoadFrame(frameFile.FilePath);

                        if (success && frameData != null)
                        {
                            // Send frame to server
                            var response = _client.SendFrame(frameData);

                            // Display progress
                            Console.Write($"\rFrame {frameIndex + 1}/{_frameFiles.Count} - {frameFile.FileName}");

                            // Calculate delay for next frame (assuming 30 FPS by default)
                            var frameDelay = (int)(33.33 / speedMultiplier); // 33.33ms = 30fps
                            
                            // If metadata contains timing info, use it
                            if (metadata != null && metadata.ContainsKey("frameInterval"))
                            {
                                if (int.TryParse(metadata["frameInterval"].ToString(), out var interval))
                                {
                                    frameDelay = (int)(interval / speedMultiplier);
                                }
                            }

                            await Task.Delay(frameDelay);
                        }
                        else
                        {
                            Console.WriteLine($"\nFailed to load frame: {frameFile.FileName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"\nError processing frame {frameFile.FileName}: {ex.Message}");
                    }

                    frameIndex++;
                }

                Console.WriteLine("\nReplay complete!");

            } while (loop && !_cancellationTokenSource.Token.IsCancellationRequested);

            _isPlaying = false;
            Console.WriteLine("\nReplay stopped.");
        }

        private void ListenForControls(CancellationToken cancellationToken)
        {
            Console.WriteLine("\nControls: [Space] = Pause/Resume, [Q] = Quit");

            while (!cancellationToken.IsCancellationRequested)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);

                    switch (key.Key)
                    {
                        case ConsoleKey.Spacebar:
                            _isPlaying = !_isPlaying;
                            Console.WriteLine(_isPlaying ? "\nResumed" : "\nPaused");
                            break;

                        case ConsoleKey.Q:
                            Console.WriteLine("\nQuitting...");
                            _cancellationTokenSource.Cancel();
                            break;
                    }
                }

                Thread.Sleep(100);
            }
        }

        private async Task<(bool success, DataFrameRequest frameData, Dictionary<string, object> metadata)> LoadFrame(string filePath)
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
                var metadata = JsonConvert.DeserializeObject<Dictionary<string, object>>(metadataJson);

                // Read frame data
                var frameDataLength = (int)(fileStream.Length - fileStream.Position);
                var frameBytes = new byte[frameDataLength];
                await fileStream.ReadAsync(frameBytes, 0, frameDataLength);

                var frameData = DataFrameRequest.Parser.ParseFrom(frameBytes);
                return (true, frameData, metadata);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to load frame from {filePath}: {e.Message}");
                return (false, null, null);
            }
        }

        private class FrameFile
        {
            public string FilePath { get; set; }
            public string FileName { get; set; }
        }
    }

    // Simplified ARFlowClient for replay (without Unity dependencies)
    public class ARFlowClient
    {
        private readonly GrpcChannel _channel;
        private readonly ARFlowService.ARFlowServiceClient _client;
        private string _sessionId;

        public ARFlowClient(string address)
        {
            Console.WriteLine("Initialize client for " + address);
            _channel = GrpcChannel.ForAddress(address);
            _client = new ARFlowService.ARFlowServiceClient(_channel);
        }

        public void Connect(RegisterRequest requestData)
        {
            try
            {
                var response = _client.register(requestData);
                _sessionId = response.Uid;
                Console.WriteLine($"Session ID: {response.Uid}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Connection error: {e.Message}");
                throw;
            }
        }

        public string SendFrame(DataFrameRequest frameData)
        {
            frameData.Uid = _sessionId;
            try
            {
                var response = _client.data_frame(frameData);
                return response.Message;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Send frame error: {e.Message}");
                return "";
            }
        }

        public void Dispose()
        {
            _channel?.Dispose();
        }
    }
}

// You'll also need a .csproj file to build this:
/*
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net6.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Google.Protobuf" Version="3.21.12" />
    <PackageReference Include="Grpc.Net.Client" Version="2.51.0" />
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
  </ItemGroup>

  <ItemGroup>
    <Protobuf Include="protos\arflow\*.proto" GrpcServices="Client" />
  </ItemGroup>
</Project>
*/