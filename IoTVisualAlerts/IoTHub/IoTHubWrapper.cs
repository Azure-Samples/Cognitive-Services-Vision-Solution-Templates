using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTVisualAlerts.IoTHub
{
    class IoTHubWrapper
    {
        public event EventHandler<int> UploadTrainingImagesRequested;
        public event EventHandler DeleteCurrentModelRequested;
        public bool IsInitialized { get; private set; } = false;

        public static IoTHubWrapper Instance { get; } = new IoTHubWrapper();

        private DeviceClient _deviceClient;

        // The device connection string to authenticate the device with your IoT hub.
        // Using the Azure portal, load up your IoT Hub instance, click on IoT Devices under Explorer and find the connection string.
        // The format should be similar to: HostName={your iot hub name}.azure-devices.net;DeviceId={your device id};SharedAccessKey={your access key}
        private readonly static string s_connectionString = "Enter your device connection string here";

        private IoTHubWrapper() { }

        public async Task InitializeIoTHubConnectionAsync()
        {
            if (IsInitialized)
            {
                return;
            }

            // Connect to the IoT hub using the MQTT protocol
            Instance._deviceClient = DeviceClient.CreateFromConnectionString(s_connectionString, TransportType.Mqtt);
            Instance._deviceClient.OperationTimeoutInMilliseconds = 1000;

            // Create a handler for the direct method calls
            await Instance._deviceClient.SetMethodHandlerAsync("EnterLearningMode", Instance.UploadTrainingImages, null);
            await Instance._deviceClient.SetMethodHandlerAsync("DeleteCurrentModel", Instance.DeleteCurrentModel, null);
            await Instance._deviceClient.SetMethodHandlerAsync("GetIpAddress", Instance.GetIpAddress, null);

            IsInitialized = true;
        }

        private Task<MethodResponse> GetIpAddress(MethodRequest methodRequest, object userContext)
        {
            string result = "{\"IpAddress\":\"" + Util.GetIpAddress() + "\"}";
            return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 200));
        }

        // Handle the direct method call
        private Task<MethodResponse> UploadTrainingImages(MethodRequest methodRequest, object userContext)
        {
            var data = Encoding.UTF8.GetString(methodRequest.Data);

            // Payload can have a single integer value for num of images requested
            int numImagesRequested;
            Int32.TryParse(data, out numImagesRequested);

            Instance.UploadTrainingImagesRequested?.Invoke(Instance, numImagesRequested);

            // Acknowledge the direct method call with a 200 success message
            string result = "{\"result\":\"Executed direct method: " + methodRequest.Name + "\"}";
            return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 200));
        }

        // Handle the direct method call
        private Task<MethodResponse> DeleteCurrentModel(MethodRequest methodRequest, object userContext)
        {
            Instance.DeleteCurrentModelRequested?.Invoke(Instance, EventArgs.Empty);

            // Acknowledge the direct method call with a 200 success message
            string result = "{\"result\":\"Executed direct method: " + methodRequest.Name + "\"}";
            return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 200));
        }

        public async Task SendDetectedClassAlertToCloudAsync(string label, double confidence)
        {
            if (!IsInitialized)
            {
                // skip the call until we can establish a connection with IoT Hub
                return;
            }

            var detectedClassDataPoint = new
            {
                label,
                confidence
            };
            var messageString = JsonConvert.SerializeObject(detectedClassDataPoint);
            var message = new Message(Encoding.ASCII.GetBytes(messageString));

            // Add a custom application property to the message.
            // An IoT hub can filter on these properties without access to the message body.
            message.Properties.Add("detectedClassAlert", "true");

            try
            {
                await _deviceClient.SendEventAsync(message);
            }
            catch
            {
                // we will start hitting errors if our IoT Hub connection goes invalid. In that case, reset the 
                // status so we initialize it again.
                IsInitialized = false;
            }
        }

        public async Task SendStatusMessageToCloudAsync(string status)
        {
            if (!IsInitialized)
            {
                // skip the call until we can establish a connection with IoT Hub
                return;
            }

            // Create JSON message
            var dataPoint = new
            {
                status
            };
            var messageString = JsonConvert.SerializeObject(dataPoint);
            var message = new Message(Encoding.ASCII.GetBytes(messageString));

            // Send the telemetry message
            try
            {
                await _deviceClient.SendEventAsync(message);
            }
            catch
            {
                // we will start hitting errors if our IoT Hub connection goes invalid. In that case, reset the 
                // status so we initialize it again.
                IsInitialized = false;
            }
        }
    }
}
