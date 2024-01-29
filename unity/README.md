# ARFlow Client

The ARFlow client is responsible for on-device AR data collection and high-performance AR data streaming. We implement the ARFlow client as a Unity application that can be easily ported to different platforms and devices.

The core functions are implemented in `unity/Assets/Scripts/ARFlow`. We show three example ARFlow integration of three different data sources:

- Mock data: [unity/Assets/Scripts/ARFlowMockDataSample.cs](./Assets/Scripts/ARFlowMockDataSample.cs)
- ARFoundation device data: [unity/Assets/Scripts/ARFlowDeviceSample.cs](./Assets/Scripts/ARFlowDeviceSample.cs)
- Unity scene data: [unity/Assets/Scripts/ARFlowUnityDataSample.cs](./Assets/Scripts/ARFlowUnityDataSample.cs)

To use ARFlow with your own device, you should directly deploy our client code to your AR device.
Please compile the Unity code for your target deployment platform and install the compiled application.

Currently, we support the following platforms:

- iOS (iPhone, iPad)
- Android (Android Phone)

<!-- TODO: client side address input and screenshot. -->
