using System;
using System.Diagnostics;
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

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            Uri redirectUrl = WebAuthenticationBroker.GetCurrentApplicationCallbackUri();
            string apiKey = "gHgHKRbIjdguCM4cv5481hdiF5hZGWZ4x12Ur-7v";
            Uri requestUrl = new Uri("https://dav-apps.tech/login_implicit?api_key=" + apiKey + "&redirect_url=" + redirectUrl);

            var webAuthenticationResult = await WebAuthenticationBroker.AuthenticateAsync(WebAuthenticationOptions.None, requestUrl);
            Debug.WriteLine(webAuthenticationResult.ResponseData);
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

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // Get JWT from local storage
            var localSettings = ApplicationData.Current.LocalSettings;

            var jwtObject = localSettings.Values[FileManager.jwtKey];
            bool loggedIn = false;

            if(jwtObject != null)
            {
                jwt = jwtObject.ToString();
                if (!String.IsNullOrEmpty(jwt))
                {
                    loggedIn = true;
                }
            }

            ShowLoggedInContent();

            if (loggedIn)
            {
                // Get the user information from the server and update layout if necessary

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
    }
}
