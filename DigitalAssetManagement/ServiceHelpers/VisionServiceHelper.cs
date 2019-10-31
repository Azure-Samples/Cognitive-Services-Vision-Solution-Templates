using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceHelpers
{
    public static class VisionServiceHelper
    {
        private const int NumberOfCharsInOperationId = 36;
        private const int MaxRetriesOnTextRecognition = 10;
        private const int DelayOnTextRecognition = 1000;
        private const int RetryCountOnQuotaLimitError = 6;
        private const int RetryDelayOnQuotaLimitError = 500;

        private static ComputerVisionClient client { get; set; }

        static VisionServiceHelper()
        {
            InitializeVisionService();
        }

        public static Action Throttled;

        private static string apiKey;
        public static string ApiKey
        {
            get
            {
                return apiKey;
            }

            set
            {
                var changed = apiKey != value;
                apiKey = value;
                if (changed)
                {
                    InitializeVisionService();
                }
            }
        }

        private static string apiEndpoint;
        public static string ApiEndpoint
        {
            get { return apiEndpoint; }
            set
            {
                var changed = apiEndpoint != value;
                apiEndpoint = value;
                if (changed)
                {
                    InitializeVisionService();
                }
            }
        }

        private static void InitializeVisionService()
        {
            bool hasEndpoint = !string.IsNullOrEmpty(ApiEndpoint) ? Uri.IsWellFormedUriString(ApiEndpoint, UriKind.Absolute) : false;
            client = !hasEndpoint
                ? new ComputerVisionClient(new ApiKeyServiceClientCredentials(ApiKey))
                : new ComputerVisionClient(new ApiKeyServiceClientCredentials(ApiKey))
                {
                    Endpoint = ApiEndpoint
                };
        }

        private static async Task<TResponse> RunTaskWithAutoRetryOnQuotaLimitExceededError<TResponse>(Func<Task<TResponse>> action)
        {
            int retriesLeft = RetryCountOnQuotaLimitError;
            int delay = RetryDelayOnQuotaLimitError;

            TResponse response = default(TResponse);

            while (true)
            {
                try
                {
                    response = await action();
                    break;
                }
                catch (ComputerVisionErrorException exception) when (exception.Response?.StatusCode == (System.Net.HttpStatusCode)429 && retriesLeft > 0)
                {
                    ErrorTrackingHelper.TrackException(exception, "Vision API throttling error");
                    if (retriesLeft == 1 && Throttled != null)
                    {
                        Throttled();
                    }

                    await Task.Delay(delay);
                    retriesLeft--;
                    delay *= 2;
                    continue;
                }
            }

            return response;
        }

        private static async Task RunTaskWithAutoRetryOnQuotaLimitExceededError(Func<Task> action)
        {
            await RunTaskWithAutoRetryOnQuotaLimitExceededError<object>(async () => { await action(); return null; });
        }

        public static async Task<ImageDescription> DescribeAsync(Func<Task<Stream>> imageStreamCallback)
        {
            return await RunTaskWithAutoRetryOnQuotaLimitExceededError(async () => await client.DescribeImageInStreamAsync(await imageStreamCallback()));
        }

        public static async Task<ImageAnalysis> AnalyzeImageAsync(string imageUrl, IList<VisualFeatureTypes> visualFeatures = null, IList<Details> details = null)
        {
            return await RunTaskWithAutoRetryOnQuotaLimitExceededError(() => client.AnalyzeImageAsync(imageUrl, visualFeatures, details));
        }

        public static async Task<ImageAnalysis> AnalyzeImageAsync(Func<Task<Stream>> imageStreamCallback, IList<VisualFeatureTypes> visualFeatures = null, IList<Details> details = null)
        {
            return await RunTaskWithAutoRetryOnQuotaLimitExceededError(async () => await client.AnalyzeImageInStreamAsync(await imageStreamCallback(), visualFeatures, details));
        }

        public static async Task<ImageDescription> DescribeAsync(string imageUrl)
        {
            return await RunTaskWithAutoRetryOnQuotaLimitExceededError(() => client.DescribeImageAsync(imageUrl));
        }

        public static async Task<ReadOperationResult> BatchReadFileAsync(string imageUrl)
        {
            var textHeaders = await client.BatchReadFileAsync(imageUrl);
            return await GetReadResultAsync(client, textHeaders.OperationLocation);
        }

        public static async Task<ReadOperationResult> BatchReadFileAsync(Func<Task<Stream>> imageStreamCallback)
        {
            var textHeaders = await client.BatchReadFileInStreamAsync(await imageStreamCallback());
            return await GetReadResultAsync(client, textHeaders.OperationLocation);
        }

        public static async Task<DetectResult> DetectObjectsInStreamAsync(Func<Task<Stream>> imageStreamCallback)
        {
            return await RunTaskWithAutoRetryOnQuotaLimitExceededError(async () => await client.DetectObjectsInStreamAsync(await imageStreamCallback()));
        }

        public static async Task<DetectResult> DetectObjectsAsync(string imageUrl)
        {
            return await RunTaskWithAutoRetryOnQuotaLimitExceededError(() => client.DetectObjectsAsync(imageUrl));
        }

        private static async Task<ReadOperationResult> GetReadResultAsync(ComputerVisionClient computerVision, string operationLocation)
        {
            // Retrieve the URI where the recognized text will be stored from the Operation-Location header
            string operationId = operationLocation.Substring(operationLocation.Length - NumberOfCharsInOperationId);
            var result = await computerVision.GetReadOperationResultAsync(operationId);

            // Wait for the operation to complete
            int i = 0;
            while ((result.Status == TextOperationStatusCodes.Running || result.Status == TextOperationStatusCodes.NotStarted) &&
                i++ < MaxRetriesOnTextRecognition)
            {
                await Task.Delay(DelayOnTextRecognition);
                result = await computerVision.GetReadOperationResultAsync(operationId);
            }

            return result;
        }

        public static TextOperationResult ConvertToTextOperationResult(this ReadOperationResult entity)
        {
            return new TextOperationResult() { Status = entity.Status, RecognitionResult = entity.RecognitionResults?.FirstOrDefault() };
        }
    }
}
