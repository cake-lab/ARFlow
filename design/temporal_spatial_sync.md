# Temporal and spatial synchronization

A design proposal for aligning/synchronizing sensor data across multiple ARFlow
clients, both temporally and spatially.

## Why is this important?

<!-- TODO(felixngfender) -->

## Status quo

This section describes the current state of ARFlow as of November 15, 2024.

The typical ARFlow setup consists of multiple clients, which are devices with a
Unity application installed, that communicate with a central Python server via
gRPC. For data capture, the client collects data from multiple sensors (e.g.,
RGB image, depth data, audio). The multimodal data is then packed into a data
frame marked with a timestamp issued by the system clock. This data frame is
then sent to the server for processing and storage.

On the server side, the gRPC server maintains a registry of connected clients ,
their device's current configuration, and a handle to their recording stream.
You could think of this handle as a pointer to a file on disk that stores the
data received from that specific client. Moreover, a client can join another
client's session and stream to the same file using the former's ID.

### Problems

As for why the current design is a major blocker to achieving granular temporal
and spatial sync and a major redesign is needed, consider the following:

#### Single-device timestamping

At the hardware level, compared to something like
[Project Aria glasses and their hardware configuration for timestamping](https://facebookresearch.github.io/projectaria_tools/docs/tech_insights/device_timestamping#aria-device-hardware),
smartphones lack the dedicated microcontroller unit (MCU) used by Aria for
synchronized timestamping, leading to less precise alignment across sensor data.
Smartphones also have higher latency and jitter due to sensor timestamps being
managed by the main processor, introducing potential delays compared to Aria’s
immediate MCU-based timestamps. Moreover, unlike Aria's global shutter cameras,
smartphone cameras have rolling shutters, resulting in slight timing offsets in
images.

At the software level, we are using Unity's
[FixedUpdate](https://docs.unity3d.com/ScriptReference/MonoBehaviour.FixedUpdate.html)
callback at a rate of 10 times per second to collect the data. This is the same
across sensors and cannot be configured at runtime. In reality, different
sensors can have varying sampling rates and users should be able to utilize that
and configure different sampling rates for each sensor when they are using the
application.

#### Multi-device timestamping

On the server side, we are treating the timestamps of every client equally, as
in they are all coming from an external shared clock. However, this is not true
as the current timestamping mechanism relies on the system clock of each client
device. This can lead to discrepancies due to clock drift, network latency, and
varying processing times. As a result, the temporal alignment of sensor data
across clients may not be precise.

## Proposal

### Temporal synchronization

We will address the [stated problems](#problems) one-by-one, starting from the
lowest level of granularity:

For
[single-device, hardware level timestamping issues](#single-device-timestamping),
since we're not experts on hardware, we choose to focus on the software side of
things while accepting the minor timing differences at the hardware level of
smartphones.

For
[single-device, software level timestamping issues](#single-device-timestamping),
here's a more robust timestamping mechanism and redesign of the data collection
process to accommodate varying sensor sampling rates:

1. Separate sensor loops for variable rates

   Independent
   [coroutines](https://docs.unity3d.com/ScriptReference/Coroutine.html):
   Implement separate coroutines for each sensor type to allow for individual
   control over sampling frequency. This can be done by creating a coroutine for
   each sensor, each with its own
   [`WaitForSeconds`](https://docs.unity3d.com/ScriptReference/WaitForSeconds.html)
   delay to match the desired rate (e.g., 10ms for 100Hz).

   Adaptive timing: Alternatively, track the actual elapsed time between frames
   to dynamically adjust collection intervals, especially if slight timing
   adjustments are needed based on system load or frame drops.

2. Buffering and aggregating data

   To avoid overwhelming the network, create a buffer for each sensor. Sensors
   can write data to these buffers at their respective rates, and another
   process (like a coroutine or a lower-frequency
   [`Update`](https://docs.unity3d.com/ScriptReference/MonoBehaviour.Update.html)
   or
   [`FixedUpdate`](https://docs.unity3d.com/ScriptReference/MonoBehaviour.FixedUpdate.html))
   can periodically aggregate data from the buffers and batch-send them. Reuse
   and modify the existing `DataFrame` structure to accommodate a list of sensor
   data entries rather than a single “frame” per call. This lets the system
   batch multiple readings for efficient transmission.

3. Time synchronization

   Ensure each sensor readout includes an accurate timestamp sourced from the
   system clock.

And here's the Unity pseudo-code to illustrate the proposed design:

```cs
Dictionary<string, List<SensorData>> sensorBuffers = new Dictionary<string, List<SensorData>>();
Coroutine gyroCoroutine, accelCoroutine;

void Start() {
    sensorBuffers["gyroscope"] = new List<SensorData>();
    sensorBuffers["accelerometer"] = new List<SensorData>();

    gyroCoroutine = StartCoroutine(CollectGyroscopeData(0.02f));  // 50Hz
    accelCoroutine = StartCoroutine(CollectAccelerometerData(0.1f));  // 10Hz

    StartCoroutine(SendDataCoroutine());
}

IEnumerator CollectGyroscopeData(float interval) {
    while (true) {
        SensorData gyroData = GetGyroscopeReading();
        sensorBuffers["gyroscope"].Add(gyroData);
        yield return new WaitForSeconds(interval);
    }
}

IEnumerator CollectAccelerometerData(float interval) {
    while (true) {
        SensorData accelData = GetAccelerometerReading();
        sensorBuffers["accelerometer"].Add(accelData);
        yield return new WaitForSeconds(interval);
    }
}

IEnumerator SendDataCoroutine() {
    while (true) {
        if (sensorBuffers["gyroscope"].Count > 0 || sensorBuffers["accelerometer"].Count > 0) {
            DataFrame dataFrame = new DataFrame();

            foreach (var buffer in sensorBuffers) {
                dataFrame.AddData(buffer.Key, buffer.Value);
                buffer.Value.Clear();
            }

            SendDataToServer(dataFrame);
        }

        yield return new WaitForSeconds(0.5f);
    }
}

SensorData GetGyroscopeReading() {
    return new SensorData("gyroscope", GetTimestamp(), /* sensor values */);
}

SensorData GetAccelerometerReading() {
    return new SensorData("accelerometer", GetTimestamp(), /* sensor values */);
}

void SendDataToServer(DataFrame dataFrame) {
    grpcClient.Send(dataFrame);
}
```

Of course, some adjustments to the gRPC protocol and how the server processes
the data frame would also be needed. Overall, this setup provides the
flexibility for each sensor to have its own collection rate, while managing
network load through batched transmission.

For [multi-device timestamping issues](#multi-device-timestamping), since
Project Aria achieves better-than-1ms precision after a ~45-second warmup using
[TICSync](https://facebookresearch.github.io/projectaria_tools/docs/ARK/sdk/ticsync#overview),
we aim for similar alignment while balancing development effort with practical
gains.

1. Leverage smartphone NTP synchronization: Smartphones already synchronize with
   Internet-based NTP servers as part of their operating systems. This ensures
   reasonable timestamp alignment within tens of milliseconds on average and can
   achieve better precision in stable networks.

2. Client-side timestamps as the source of truth: Each data frame includes an
   NTP-synchronized timestamp from the client’s local system clock. These
   timestamps are treated as authoritative across all devices.

This approach keeps the system simple and efficient by relying solely on
NTP-synced client-side timestamps, achieving reliable alignment with minimal
additional development effort.

<!-- TODO(felixngfender): Write-up comparing simple iterator-based coroutine, .NET
Task, .NET ValueTask, and Unity Awaitable -->

### Spatial synchronization
