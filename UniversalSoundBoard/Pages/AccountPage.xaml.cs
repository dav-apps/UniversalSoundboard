using System;
using System.Diagnostics;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Models;
using UniversalSoundBoard;
using UniversalSoundBoard.DataAccess;
using Windows.Security.Authentication.Web;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UniversalSoundboard.Pages
{
    public sealed partial class AccountPage : Page
    {
        string jwt = "";

        public AccountPage()
        {
            InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            SetDataContext();

            // Get the JWT from local storage
            jwt = ApiManager.GetJwt();
            ShowLoggedInContent();

            if (jwt != null)
            {
                SetUsedStorageTextBlock();

                // Get the user information
                var newUser = await ApiManager.GetUser();

                if (newUser != null)
                {
                    (App.Current as App)._itemViewHolder.user = newUser;
                    SetUsedStorageTextBlock();
                }
                else
                {

                }
            }
        }

        private void SetDataContext()
        {
            ContentRoot.DataContext = (App.Current as App)._itemViewHolder;
        }

        private void SetUsedStorageTextBlock()
        {
            if((App.Current as App)._itemViewHolder.user.TotalStorage != 0)
            {
                string message = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("Account-UsedStorage");

                double usedStorageGB = Math.Round((App.Current as App)._itemViewHolder.user.UsedStorage / 1000.0 / 1000.0 / 1000.0, 1);
                double totalStorageGB = Math.Round((App.Current as App)._itemViewHolder.user.TotalStorage / 1000.0 / 1000.0 / 1000.0, 1);

                double percentage = usedStorageGB / totalStorageGB * 100;

                StorageProgressBar.Value = percentage;
                message = message.Replace("|", usedStorageGB.ToString());
                message = message.Replace("_", totalStorageGB.ToString());

                StorageTextBlock.Text = message;

                if (totalStorageGB < 50)
                    UpgradeLink.Visibility = Visibility.Visible;
            }
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            Uri redirectUrl = WebAuthenticationBroker.GetCurrentApplicationCallbackUri();
            string apiKey = "gHgHKRbIjdguCM4cv5481hdiF5hZGWZ4x12Ur-7v";
            Uri requestUrl = new Uri("https://dav-apps.tech/login_implicit?api_key=" + apiKey + "&redirect_url=" + redirectUrl);

            var webAuthenticationResult = await WebAuthenticationBroker.AuthenticateAsync(WebAuthenticationOptions.None, requestUrl);
            switch (webAuthenticationResult.ResponseStatus)
            {
                case WebAuthenticationStatus.Success:
                    // Get the JWT from the response string
                    string jwt = webAuthenticationResult.ResponseData.Split(new[] { "jwt=" }, StringSplitOptions.None)[1];
                    SaveJwt(jwt);
                    break;
                default:
                    Debug.WriteLine("There was an error with logging you in.");
                    break;
            }
        }
        
        private void ShowLoggedInContent()
        {
            if (!String.IsNullOrEmpty(jwt))
            {
                LoggedInContent.Visibility = Visibility.Visible;
                LoggedOutContent.Visibility = Visibility.Collapsed;
            }
            else
            {
                LoggedInContent.Visibility = Visibility.Collapsed;
                LoggedOutContent.Visibility = Visibility.Visible;
            }
        }

        private void SaveJwt(string jwt)
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values[FileManager.jwtKey] = jwt;
            this.jwt = jwt;

            ShowLoggedInContent();
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void UpgradeLink_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
