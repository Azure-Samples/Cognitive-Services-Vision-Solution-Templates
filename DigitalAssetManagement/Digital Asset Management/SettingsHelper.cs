using ServiceHelpers;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml.Controls;

namespace DigitalAssetManagementTemplate
{
    public class SettingsHelper
    {
        ApplicationDataContainer _settings = ApplicationData.Current.LocalSettings;

        public static SettingsHelper Instance { get; private set; }
        public static Func<Task> InitCustomVisionHandler { get; set; }

        static SettingsHelper()
        {
            Instance = new SettingsHelper();
        }

        T Get<T>(T defaultValue, [CallerMemberName] string key = null)
        {
            var value = _settings.Values[key];
            return value is T ? (T)value : defaultValue;
        }

        void Set<T>(T value, [CallerMemberName] string key = null)
        {
            _settings.Values[key] = value;
        }

        public string FaceApiKey { get => CognitiveServiceApiKey; }

        public string VisionApiKey { get => CognitiveServiceApiKey; }

        public string CognitiveServiceApiKey
        {
            get => Get<string>(null);
            set => Set(value);
        }

        public string CognitiveServiceEndpoint
        {
            get => Get<string>("https://westus2.api.cognitive.microsoft.com");
            set => Set(value);
        }

        public bool ShowAgeAndGender
        {
            get => Get<bool>(true);
            set => Set(value);
        }

        public string CustomVisionTrainingApiKey
        {
            get => Get<string>(null);
            set => Set(value);
        }

        public string CustomVisionPredictionApiKey
        {
            get => Get<string>(null);
            set => Set(value);
        }

        public string CustomVisionApiKeyEndpoint
        {
            get => Get<string>("https://westus2.api.cognitive.microsoft.com");
            set => Set(value);
        }

        public string CustomVisionTrainingApiKeyEndpoint { get => CustomVisionApiKeyEndpoint; }

        public string CustomVisionPredictionApiKeyEndpoint { get => CustomVisionApiKeyEndpoint; }

        public async Task PushSettingsToServices()
        {
            //face API
            FaceServiceHelper.ApiKey = FaceApiKey;
            FaceServiceHelper.ApiEndpoint = CognitiveServiceEndpoint;

            //vision API
            VisionServiceHelper.ApiKey = VisionApiKey;
            VisionServiceHelper.ApiEndpoint = CognitiveServiceEndpoint;

            //custom vision API
            await (InitCustomVisionHandler?.Invoke() ?? Task.CompletedTask);
        }
    }
}