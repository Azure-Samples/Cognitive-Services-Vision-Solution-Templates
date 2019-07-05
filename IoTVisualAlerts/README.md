# IoT Visual Alert
This sample illustrates how to leverage Microsoft Custom Vision Service to train a device with a camera to detect pre-defined visual states. 
A visual state could be something like an empty room, or a room with people, or a empty driveway or a driveway with a truck in it. 

This demo runs in a continuous state machine loop with 4 stages:
* No Model: A no-op state, meaning nothing happens while at this stage. It will just sleep for 1 second and check again.
* Capturing Training Images: While at this stage, a picture is captured and uploaded as training image to the target Custom Vision project. It will then sleep 500ms and do it again.
* Waiting For Trained Model: While at this stage, the status of the target Custom Vision project is checked every second to see whether it has a trained iteration. Once it finds one, it will export the model to a local file and switch to the Scoring state.
* Scoring: While at this stage, a frame from the camera is evaluated using Windows ML and the exported ONNX model. The result is displayed in the screen and sent as a message to IoT Hub. 

## Prerequisites

- PC with Windows 10 version 17763 or higher
- [Visual Studio 2015](https://visualstudio.microsoft.com/) or higher
- Azure Subscription (you will IoT Hub and Custom Vision resources)
- (Optional) IoT device running Windows 10 IoT Core version 17763 or higher

## Setup

1. Clone or download this repository
2. Open the solution IoTVisualAlerts.sln in Visual Studio
3. **Custom Vision setup**:
    * In CustomVision\CustomVisionServiceWrapper.cs, update ```ApiKey = "{The training key for your Custom Vision Service instance}"``` with your api key.
    * In CustomVision\CustomVisionServiceWrapper.cs, update ```Endpoint = "https://westus2.api.cognitive.microsoft.com"``` with the corresponding endpoint for your key.
    * In CustomVision\CustomVisionServiceWrapper.cs, update ```targetCVSProjectGuid = "{Your Custom Vision Service target project id}"``` with the corresponding Guid for the Custom Vision project that should be used by the app during the visual state learning workflow. **Important:** This needs to be a Compact domain project, since we will be exporting the model to ONNX later.
4. **IoT Hub setup**:
    * In IoTHub\IotHubWrapper.cs, update ```s_connectionString = "Enter your device connection string here"``` with the proper connection string for your device. Using the Azure portal, load up your IoT Hub instance, click on IoT devices under Explorers, click on your target device (or create one if needed), and find the connection string under Primary Connection String. The format should be similar to ```HostName={your iot hub name}.azure-devices.net;DeviceId={your device id};SharedAccessKey={your access key}```

## Running the app

If you are running the app in your own development PC, just hit F5 in Visual Studio to start. The app should start and show the live 
feed from the camera, as well as a status message. **Note**: If deploying to a IoT device, you will need to select the Remote Machine target option in Visual Studio and provide the Ip address of your device (it must be on the same network). You can get the Ip address from the Windows IoT default app once you boot into the device and connect to the internet.

### Learning new visual states
When running for the first time, you will notice the app doesn't have any knowledge of any visual states. As a result it won't be doing much, and simply display a status message that there is no model available. 

This is where Custom Vision comes into play. We will trigger a process in the app that will send images to the target Custom Vision Service, and then wait until there is a trained iteration. At that point, 
it will export it to ONNX, download the model and start scoring images in real-time against that model. 

You can trigger Learning Mode in two ways:
  * Via the button on the top right corner of the UI
  * Via a Direct Method call to the device via IoTHub. The method for this is EnterLearningMode, and you can send it via the device entry in the IoT Hub blade in Azure, or via a tool such as the IoT Hub Device Explorer.
 


