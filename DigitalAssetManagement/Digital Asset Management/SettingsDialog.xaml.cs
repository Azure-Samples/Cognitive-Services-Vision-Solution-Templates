using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace DigitalAssetManagementTemplate
{
    public sealed partial class SettingsDialog : ContentDialog
    {
        public SettingsHelper SettingsHelper { get; } = SettingsHelper.Instance;

        public SettingsDialog()
        {
            this.InitializeComponent();
        }

        async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            args.Cancel = true;
            var hasError = false;
            //validate key
            try
            {
                //face
                var faceClient = new FaceClient(new Microsoft.Azure.CognitiveServices.Vision.ComputerVision.ApiKeyServiceClientCredentials(CognitiveApi.Password)) { Endpoint = ApiEndpoint.Text };
                await faceClient.PersonGroup.ListAsync();

                //vision
                var visionClient = new ComputerVisionClient(new Microsoft.Azure.CognitiveServices.Vision.ComputerVision.ApiKeyServiceClientCredentials(CognitiveApi.Password)) { Endpoint = ApiEndpoint.Text };
                await visionClient.ListModelsAsync();

                CognitiveApiError.Visibility = Visibility.Collapsed;
            }
            catch(Exception ex)
            {
                CognitiveApiError.Text = Util.GetMessageFromException(ex);
                CognitiveApiError.Visibility = Visibility.Visible;
                hasError = true;
            }
            if (!string.IsNullOrWhiteSpace(CustomVisionTrainingApi.Password) && !string.IsNullOrWhiteSpace(CustomVisionEndpoint.Text))
            {
                try
                {
                    //custom vision training
                    var client = new CustomVisionTrainingClient { Endpoint = CustomVisionEndpoint.Text, ApiKey = CustomVisionTrainingApi.Password };
                    await client.GetDomainsAsync();

                    CustomVisionTrainingApiError.Visibility = Visibility.Collapsed;
                }
                catch (Exception ex)
                {
                    CustomVisionTrainingApiError.Text = Util.GetMessageFromException(ex);
                    CustomVisionTrainingApiError.Visibility = Visibility.Visible;
                    hasError = true;
                }
            }

            //save settings
            if (!hasError)
            {
                SettingsHelper.CognitiveServiceApiKey = CognitiveApi.Password;
                SettingsHelper.CognitiveServiceEndpoint = ApiEndpoint.Text;
                SettingsHelper.ShowAgeAndGender = Age.IsChecked.GetValueOrDefault();
                SettingsHelper.CustomVisionTrainingApiKey = CustomVisionTrainingApi.Password;
                SettingsHelper.CustomVisionPredictionApiKey = CustomVisionPredictionApi.Password;
                SettingsHelper.CustomVisionApiKeyEndpoint = CustomVisionEndpoint.Text;
                await SettingsHelper.PushSettingsToServices();

                args.Cancel = false;
                Hide();
            }
        }

    }
}
