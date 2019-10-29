using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace DigitalAssetManagementTemplate
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AppShell : Page
    {
        public Frame AppFrame { get { return this.frame; } }

        public AppShell()
        {
            this.InitializeComponent();

            // Set the custom TitleBar colors
            var appView = ApplicationView.GetForCurrentView();
            var titleBar = appView.TitleBar;
            titleBar.BackgroundColor =
                titleBar.InactiveBackgroundColor =
                titleBar.ButtonBackgroundColor =
                titleBar.ButtonInactiveBackgroundColor =
                ((SolidColorBrush)Application.Current.Resources["TitleBarButtonBackgroundBrush"]).Color;
            titleBar.ForegroundColor =
                titleBar.InactiveForegroundColor =
                titleBar.ButtonForegroundColor =
                titleBar.ButtonInactiveForegroundColor =
                titleBar.ButtonHoverForegroundColor =
                titleBar.ButtonPressedForegroundColor =
                ((SolidColorBrush)Application.Current.Resources["TitleBarButtonForegroundBrush"]).Color;
            titleBar.ButtonHoverBackgroundColor = ((SolidColorBrush)Application.Current.Resources["TitleBarButtonHoverBrush"]).Color;
            titleBar.ButtonPressedBackgroundColor = ((SolidColorBrush)Application.Current.Resources["TitleBarButtonPressedBrush"]).Color;
        }

        private async void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            //show settings
            await new SettingsDialog().ShowAsync();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //check setting
            if (string.IsNullOrEmpty(SettingsHelper.Instance.FaceApiKey) ||
            string.IsNullOrEmpty(SettingsHelper.Instance.VisionApiKey))
            {
                await new SettingsDialog().ShowAsync();
            }
        }
    }
}
