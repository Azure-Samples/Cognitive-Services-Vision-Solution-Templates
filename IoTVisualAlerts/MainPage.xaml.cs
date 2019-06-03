using Emmellsoft.IoT.Rpi.SenseHat;
using IoTVisualAlerts.CustomVision;
using IoTVisualAlerts.IoTHub;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace IoTVisualAlerts
{

    enum State
    {
        NoModel,
        Scoring,
        WaitingForTrainedModel,
        CapturingTrainingImages
    }

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private ISenseHat _senseHat;

        private MediaCapture _mediaCapture;
        private int _cameraResolutionWidth;
        private int _cameraResolutionHeight;

        private CustomVisionONNXModel _customVisionONNXModel;
        private bool _isModelLoadedSuccessfully = false;
        private const float MinProbabilityValue = 0.3f;
        private string _lastMatchLabel;

        private Task _processingLoopTask;
        private State _currentState;

        private int _imageUploadCount;
        private int _numTrainingImagesRequested = 30;
        private DateTime _timeOfLastImageUpload = DateTime.MinValue;

        CustomVisionServiceWrapper _cvsWrapper = new CustomVisionServiceWrapper();

        public MainPage()
        {
            this.InitializeComponent();
        }

        #region Initialization and handlers for UI buttons

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            //Try to get a reference to a SenseHat in case we are running on a Raspberry Pi device
            //that has a SenseHat attached.
            try
            {
                _senseHat = await SenseHatFactory.GetSenseHat();

                _senseHat.Display.Clear();
                _senseHat.Display.Update();
            }
            catch (Exception)
            {
            }

            deviceInfoTextBlock.Text = $"Device IP Address: {Util.GetIpAddress()}";

            // Trigger IoT Hub initialization. It might potentially fail right away (e.g. no internet connection), so we will also
            // keep checking for it in the processing loop
            await CheckIoTHubConnectionAsync();

            IoTHubWrapper.Instance.UploadTrainingImagesRequested += UploadTrainingImagesRequested;
            IoTHubWrapper.Instance.DeleteCurrentModelRequested += DeleteCurrentModelRequested;

            await LoadONNXModelAsync();
            await InitCameraAsync();

            // start processing loop
            this._processingLoopTask = Task.Run(() => this.ProcessingLoop());

            base.OnNavigatedTo(e);
        }

        private async void OnEnterLearningModeButtonClicked(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            await StartUploadingTrainingImagesAsync(0 /* will result in uploading the default number of images */);
        }

        private async void OnDeleteCurrentModelClicked(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            await DeleteCurrentONNXModelAsync();
        }

        private async Task CheckIoTHubConnectionAsync()
        {
            // Trigger check for IoT Hub initialization so we eventually get it done and can start exchanging data with it 
            try
            {
                if (IoTHubWrapper.Instance.IsInitialized)
                {
                    await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        iotHubStatusTextBlock.Text = "IoT Hub Status: Connected";
                    });

                    return;
                }
                else
                {
                    // Try initialize. If we fail, we will update the UI with the error message in the catch block, and if we pass, 
                    // we will eventually update it to show the success state the next time this method is called by the processing loop
                    await IoTHubWrapper.Instance.InitializeIoTHubConnectionAsync();
                }
            }
            catch (Exception ex)
            {
                await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    iotHubStatusTextBlock.Text = $"IoT Hub Status: Failure. {ex.Message}";
                });
            }
        }

        #endregion

        #region Processing loop and status update

        private async void ProcessingLoop()
        {
            while (true)
            {
                await CheckIoTHubConnectionAsync();

                switch (_currentState)
                {
                    case State.Scoring:
                        await ScoreFrame(await CaptureFrameAsync());
                        // Remove or change the delay depending on how often scoring should happen
                        await Task.Delay(1000);
                        break;

                    case State.WaitingForTrainedModel:
                        try
                        {
                            await UpdateStatus("Waiting", "Waiting for trained iteration...");
                            Export onnxExport = await _cvsWrapper.GetTrainedONNXExportIfAvailableAsync(_timeOfLastImageUpload);
                            if (onnxExport == null)
                            {
                                // sleep a little until a trained model is available
                                await Task.Delay(1000);
                                continue;
                            }
                            else if (onnxExport?.DownloadUri != null)
                            {
                                await UpdateStatus("Waiting", "Downloading ONNX model...");
                                BackgroundDownloader downloader = new BackgroundDownloader();
                                DownloadOperation download = downloader.CreateDownload(new Uri(onnxExport.DownloadUri), 
                                    await ApplicationData.Current.LocalFolder.CreateFileAsync("model.onnx", CreationCollisionOption.ReplaceExisting));
                                await download.StartAsync();

                                // load the new model and switch to scoring mode
                                await UpdateStatus("Waiting", "Loading ONNX model...");
                                await LoadONNXModelAsync();

                                _currentState = State.Scoring;
                            }
                        }catch (Exception ex)
                        {
                            await UpdateStatus("Error", $"Failure while setting up new trained model: {ex.Message}");
                        }
                        break;

                    case State.CapturingTrainingImages:
                        await UploadTrainingImageAsync();
                        await Task.Delay(500);
                        break;

                    case State.NoModel:
                        await UpdateStatus("No model available", "Nothing detected");
                        await Task.Delay(1000);
                        break;

                    default:
                        break;
                }
            }
        }

        private void UpdateSenseHatLights(bool lightsOn)
        {
            if (_senseHat != null)
            {
                if (lightsOn)
                {
                    _senseHat.Display.Fill(Colors.Red);
                }
                else
                {
                    _senseHat.Display.Clear();
                }
                _senseHat.Display.Update();
            }
        }

        private async Task UpdateStatus(string state, string status, string details = "", bool sendMessageToIoTHub = true)
        {
            if (sendMessageToIoTHub)
            {
                await IoTHubWrapper.Instance.SendStatusMessageToCloudAsync($"[{state}] {status}");
            }

            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                currentStateTextBlock.Text = $"[{state}]";
                statusTextBlock.Text = status;
                statusDetailsTextBlock.Text = details;
            });
        }

        #endregion 

        #region Training image upload 

        private async void UploadTrainingImagesRequested(object sender, int numImgs)
        {
            await StartUploadingTrainingImagesAsync(numImgs);
        }

        private async Task StartUploadingTrainingImagesAsync(int numImgs)
        {
            try
            {
                _numTrainingImagesRequested = numImgs == 0 ? 30 : numImgs;
                await _cvsWrapper.PrepTargetProjectForTrainingAsync();
            }
            catch
            {
                await UpdateStatus("Error", "Couldn't clear project before adding new images. Ignoring error and sending new images.");
            }
            finally
            {
                _currentState = State.CapturingTrainingImages;
                _lastMatchLabel = null;
            }
        }

        private async Task UploadTrainingImageAsync()
        {
            if (_imageUploadCount == _numTrainingImagesRequested)
            {
                _imageUploadCount = 0;
                _currentState = State.WaitingForTrainedModel;
                return;
            }

            try
            {
                var stream = await GetStreamFromVideoFrameAsync(await CaptureFrameAsync());
                await _cvsWrapper.UploadTrainingImageAsync(stream);
                _timeOfLastImageUpload = DateTime.UtcNow;
                await UpdateStatus("Uploading images", $"{++_imageUploadCount} uploaded");
            }
            catch (Exception ex)
            {
                await UpdateStatus("Error", "Couldn't add training image: " + ex.Message);
            }
        }

        #endregion

        #region ONNX model handling

        public async Task ScoreFrame(VideoFrame videoFrame)
        {
            if (!_isModelLoadedSuccessfully || videoFrame == null)
            {
                return;
            }

            try
            {
                using (SoftwareBitmap bitmapBuffer = new SoftwareBitmap(BitmapPixelFormat.Bgra8,
                    _customVisionONNXModel.InputImageWidth, _customVisionONNXModel.InputImageHeight, BitmapAlphaMode.Ignore))
                {
                    using (VideoFrame buffer = VideoFrame.CreateWithSoftwareBitmap(bitmapBuffer))
                    {
                        await videoFrame.CopyToAsync(buffer);
                        var input = new CustomVisionModelInput() { data = buffer };

                        // Perform prediction using ONNX model
                        DateTime start = DateTime.Now;
                        CustomVisionModelOutput output = await this._customVisionONNXModel.EvaluateAsync(input);

                        await ShowPredictionResults(output, Math.Round((DateTime.Now - start).TotalMilliseconds));
                    }
                }
            }
            catch (Exception ex)
            {
                this._isModelLoadedSuccessfully = false;
                await UpdateStatus("Error", $"Failure scoring camera frame: {ex.Message}");
            }
        }

        private async Task ShowPredictionResults(CustomVisionModelOutput output, double latency)
        {
            List<Tuple<string, float>> result = output.GetPredictionResult();
            Tuple<string, float> topMatch = result?.Where(x => x.Item2 > MinProbabilityValue)?.OrderByDescending(x => x.Item2).FirstOrDefault();

            // Update tags in the result panel
            if (topMatch != null && topMatch.Item1 != "Negative")
            {
                if (_lastMatchLabel != topMatch.Item1)
                {
                    _lastMatchLabel = topMatch.Item1;
                    // if we want to only send  alerts when a detected class changes, use this line instead
                    //IoTHubHelper.Instance.SendDetectedClassAlertToCloudsync(topMatch.Item1, topMatch.Item2);
                    UpdateSenseHatLights(lightsOn: true);
                }
                await IoTHubWrapper.Instance.SendDetectedClassAlertToCloudAsync(topMatch.Item1, Math.Round(topMatch.Item2, 2));
                await UpdateStatus("Scoring", $"{topMatch.Item1} ({Math.Round(topMatch.Item2 * 100)}%)", $"{Math.Round(1000 / latency)}fps", sendMessageToIoTHub: false);
            }
            else
            {
                _lastMatchLabel = null;
                UpdateSenseHatLights(lightsOn: false);
                await UpdateStatus("Scoring", "Nothing detected", $"{Math.Round(1000 / latency)}fps");
            }
        }

        private async Task LoadONNXModelAsync()
        {
            try
            {                
                StorageFile modelFile = await ApplicationData.Current.LocalFolder.TryGetItemAsync("model.onnx") as StorageFile;
                if (modelFile == null)
                {
                    _currentState = State.NoModel;
                }
                else
                {
                    _customVisionONNXModel = await CustomVisionONNXModel.CreateONNXModel(modelFile);
                    _isModelLoadedSuccessfully = true;
                    _currentState = State.Scoring;
                }
            }
            catch (Exception ex)
            {
                _isModelLoadedSuccessfully = false;
                await UpdateStatus("Error", "Failure loading ONNX model: " + ex.Message);
            }
        }

        private async void DeleteCurrentModelRequested(object sender, EventArgs e)
        {
            await DeleteCurrentONNXModelAsync();
        }

        private async Task DeleteCurrentONNXModelAsync()
        {
            StorageFile modelFile = await ApplicationData.Current.LocalFolder.TryGetItemAsync("model.onnx") as StorageFile;
            if (modelFile != null)
            {
                try
                {
                    await modelFile.DeleteAsync();
                    _currentState = State.NoModel;
                    await UpdateStatus("No model", "Nothing detected");
                }
                catch
                {
                    await UpdateStatus("Error", "Couldn't delete current model");
                }
            }
        }

        #endregion

        #region Camera processing 

        async Task InitCameraAsync()
        {
            try
            {
                _mediaCapture = new MediaCapture();
                await _mediaCapture.InitializeAsync();

                captureElement.Source = _mediaCapture;
                await _mediaCapture.StartPreviewAsync();

                _cameraResolutionHeight = (int)((VideoEncodingProperties)_mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview)).Height;
                _cameraResolutionWidth = (int)((VideoEncodingProperties)_mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview)).Width;
            }
            catch
            {
                await UpdateStatus("Error", "No camera detected. Connect a camera and restart.");
            }
        }

        public async Task<VideoFrame> CaptureFrameAsync()
        {
            try
            {
                // Capture a frame from the preview stream
                var videoFrame = new VideoFrame(BitmapPixelFormat.Bgra8, _cameraResolutionWidth, _cameraResolutionHeight);
                return await _mediaCapture.GetPreviewFrameAsync(videoFrame);
            }
            catch
            {
                // return null if we can't grab a frame
            }

            return null;
        }

        internal static async Task<Stream> GetStreamFromVideoFrameAsync(VideoFrame frame)
        {
            InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream();
            BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);
            encoder.SetSoftwareBitmap(frame.SoftwareBitmap);
            await encoder.FlushAsync();
            return stream.AsStreamForRead();
        }

        #endregion
    }
}