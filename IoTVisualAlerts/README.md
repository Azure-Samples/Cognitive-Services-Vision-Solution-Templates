# IoT Visual Alert
This is a sample UWP application that illustrates how to leverage Microsoft Custom Vision Service 
to train a Windows 10 device with a camera to detect visual states and raise IoT Hub alerts when it happens. 
A visual state could be something like an empty room, or a room with people, or a empty driveway or a driveway with a truck in it. 

## Prerequisites

- PC with Windows 10 version 17763 or higher
- [Visual Studio 2015](https://visualstudio.microsoft.com/) or higher
- Azure Subscription (you will need IoT Hub and Custom Vision resources)
- IoT device running Windows 10 IoT Core version 17763 or higher

## Setup

1. Clone or download this repository
2. Open the solution IoTVisualAlerts.sln in Visual Studio
3. Enter your Custom Vision info in 'CustomVision\CustomVisionServiceWrapper.cs':
  * Update line ```ApiKey = "{The training key for your Custom Vision Service instance}"``` with your key
  * Update line ```Endpoint = "https://westus2.api.cognitive.microsoft.com"``` with the corresponding endpoint for your key
  * Update line ```targetCVSProjectGuid = "{Your Custom Vision Service target project id}"``` with the corresponding Guid for the Custom Vision project that should be used by the app during the visual state learning workflow. Notice this needs to be a Compact domain project, since we will be exporting it to ONNX later.
4. Enter your IoT Hub connection string in 'IoTHub\IotHubWrapper.cs':
  * Update line ```s_connectionString = "Enter your device connection string here"``` with the proper connection string for your device. Using the Azure portal, load up your IoT Hub instance, click on IoT devices under Explorers, click on your target device (or create one if needed), and find the connection string under Primary Connection String. The format should be similar to ```HostName={your iot hub name}.azure-devices.net;DeviceId={your device id};SharedAccessKey={your access key}```

## Running the app

If you are running the app in your own development PC, just hit F5 in Visual Studio to start. The app should start and show the live 
feed from the camera, as well as a status message. Note: If deploying to a IoT device, you will need to select the Remote Machine option in Visual Studio and provide the IP address of the device.

### Learning new visual states
When running for the first time, you will notice the app doesn't have any knowledge of any visual states. As a result it won't be doing much, and simply display a status message that there is no model available. 

This is where Custom Vision comes into play. We will trigger a process in the app that will send images to the target Custom Vision Service, and then wait until there is a trained iteration. At that point, 
it will export it to ONNX, download the model and start scoring images in real-time against that model. 

You can trigger Learning Mode in two ways:
  * Via the button on the top right corner of the UI
  * Via a Direct Method call to the device via IoTHub. The method for this is EnterLearningMode, and you can send it via the device entry in the IoT Hub blade in Azure, or via a tool such as the IoT Hub Device Explorer.
 


