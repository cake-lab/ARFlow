 C# client that replays `.bin` frame files to an ARFlow gRPC server.

How It Works

- Connects to the ARFlow server (default: http://localhost:8500)
- Registers as a virtual device
- Loads `.bin` frame files from a folder
- Sends them to the server at ~30 FPS

Requirements

- .NET 6 or later
- ARFlow server running
- `.bin` frame files in this format:
  [4 bytes] metadata length
  [n bytes] metadata (JSON)
  [m bytes] DataFrameRequest (protobuf)

How to Run

1. Place `.bin` files in:
   C:\Users\alexr\Desktop\ARFlowData (or whereever you are storing them)

2. Build and run:
   dotnet build
   dotnet run


Output

- Shows how many frames were sent
- Shows any errors
- Prints a summary at the end
