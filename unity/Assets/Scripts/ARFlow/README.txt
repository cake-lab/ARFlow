# ARFlow Binary Data Format Schema


ARFlowDataStorage saves AR data frames as binary files (.bin) containing both metadata and frame data

## File Naming

Each file follows this pattern: frame_{COUNTER}_{TIMESTAMP}.bin

* COUNTER - 6-digit frame number (e.g., 000001, 000042)
* TIMESTAMP - Date and time: yyyy-MM-dd_HH-mm-ss-fff

Example: frame_000042_2024-03-15_14-30-45-123.bin


# File Structure

Each binary file has three sections:


  Metadata Size (4 bytes)
  Metadata (variable)
  Frame Data (variable)



# Section Details

  Metadata Size (First 4 Bytes)



Format: 32-bit integer (Int32)

Example:
* If metadata is 256 bytes: [0x00, 0x01, 0x00, 0x00]
* If metadata is 128 bytes: [0x80, 0x00, 0x00, 0x00]


### Metadata Section
    
    Information about when and how the frame was captured

Format: UTF-8 encoded JSON string

Example:
    json
{
  "deviceId": "Android-ABC123",
  "captureTime": "2024-03-15T14:30:45.123Z",
  "frameIndex": "42",
  "sessionId": "xyz789"
}


#Frame Data Section

 The actual AR sensor data

Format: Protocol Buffer (protobuf) serialized as DataFrameRequest

Contains:
 Color Image - YCbCr420 format
 Depth Image - Different formats per platform:
   Android: 16-bit unsigned integers (u16)

* Camera Transform - 3x4 matrix of position/rotation data


## Camera Transform Matrix Layout

The transform is stored as 48 bytes (12 floats x 4 bytes each):


[m00, m01, m02, m03,    // First row
 m10, m11, m12, m13,    // Second row
 m20, m21, m22, m23]    // Third row


each float uses IEEE 754 format.


#Binary Example


Offset  Size    Raw Bytes          What It Represents
0x0000  4       80 00 00 00        Metadata size = 128 bytes
0x0004  128     7B 22 64 65...     JSON: {"deviceId":"Android-123"...}
0x0084  10,240  [binary data]      Protobuf frame data



## How to Read a File

 Open the file in binary mode
 Read 4 bytes -> Convert to Int32 for metadata size
 Read N bytes (where N = metadata size) -> Decode as UTF-8 string
 Parse JSON string into a dictionary
 Read remaining bytes -> Parse as protobuf DataFrameRequest


## File Storage 

Location: {Application.persistentDataPath}/ARFlowData/

Limits:
* Maximum files: 2,000 (configurable)
* Oldest files auto-delete when limit reached
* Files managed as FIFO (first in, first out)

Paths:
* Android: /storage/emulated/0/Android/data/[package]/files/ARFlowData/


#Data Size

Typical frame sizes:
* Color data: ~200-500 KB (depends on resolution)
* Depth data: ~100-300 KB (depends on resolution)
* Transform: 48 bytes (fixed)
* Metadata: 100-500 bytes
* Total per frame: ~300-800 KB

At maximum capacity (2,000 frames), storage uses approximately 600 MB - 1.6 GB.
   **2000 frames can be changed in the ARFlowDataStorage.cs file**