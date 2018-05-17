using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Models;
using UniversalSoundBoard;
using UniversalSoundBoard.Common;
using UniversalSoundBoard.DataAccess;
using Windows.Security.Authentication.Web;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace UniversalSoundboard.Pages
{
    public sealed partial class AccountPage : Page
    {
        public AccountPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            SetDataContext();
            SetDarkThemeLayout();
            UpdateUserLayout();
        }

        private void SetDataContext()
        {
            ContentRoot.DataContext = (App.Current as App)._itemViewHolder;
        }

        private void SetDarkThemeLayout()
        {
            if ((App.Current as App).RequestedTheme == ApplicationTheme.Dark)
            {
                LoginButton.Background = new SolidColorBrush(Colors.DimGray);
                SignupButton.Background = new SolidColorBrush(Colors.DimGray);
            }
        }

        private void UpdateUserLayout()
        {
            ShowLoggedInContent();

            if ((App.Current as App)._itemViewHolder.user.IsLoggedIn)
            {
                SetUsedStorageTextBlock();
                (App.Current as App)._itemViewHolder.loginMenuItemVisibility = false;
            }
            else
            {
                (App.Current as App)._itemViewHolder.loginMenuItemVisibility = true;
            }
        }

        public static async Task Login()
        {
            if (NetworkInterface.GetIsNetworkAvailable())
            {
                try
                {
                    Uri redirectUrl = WebAuthenticationBroker.GetCurrentApplicationCallbackUri();
                    Uri requestUrl = new Uri(FileManager.LoginImplicitUrl + "?api_key=" + FileManager.ApiKey + "&redirect_url=" + redirectUrl);

                    var webAuthenticationResult = await WebAuthenticationBroker.AuthenticateAsync(WebAuthenticationOptions.None, requestUrl);
                    switch (webAuthenticationResult.ResponseStatus)
                    {
                        case WebAuthenticationStatus.Success:
                            // Get the JWT from the response string
                            string jwt = webAuthenticationResult.ResponseData.Split(new[] { "jwt=" }, StringSplitOptions.None)[1];
                            await (App.Current as App)._itemViewHolder.user.Login(jwt);
                            break;
                        default:
                            Debug.WriteLine("There was an error with logging you in.");
                            break;
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Can't connect to the server");
                    Debug.WriteLine(e);
                    // TODO Show error message
                }
            }
            else
            {
                Debug.WriteLine("No internet connection");
                // TODO Show error message
            }
        }

        private void SetUsedStorageTextBlock()
        {
            if((App.Current as App)._itemViewHolder.user.UsedStorage > 0 && (App.Current as App)._itemViewHolder.user.TotalStorage > 0)
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
        
        private void ShowLoggedInContent()
        {
            if ((App.Current as App)._itemViewHolder.user.IsLoggedIn)
            {
                LoggedInContent.Visibility = Visibility.Visible;
                LoggedOutContent.Visibility = Visibility.Collapsed;

                LoginButton.Visibility = Visibility.Collapsed;
                LogoutButton.Visibility = Visibility.Visible;
            }
            else
            {
                LoggedInContent.Visibility = Visibility.Collapsed;
                LoggedOutContent.Visibility = Visibility.Visible;

                LoginButton.Visibility = Visibility.Visible;
                LogoutButton.Visibility = Visibility.Collapsed;
            }
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            await Login();
            UpdateUserLayout();
        }

        private async void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var logoutContentDialog = ContentDialogs.CreateLogoutContentDialog();
            logoutContentDialog.PrimaryButtonClick += LogoutContentDialog_PrimaryButtonClick;
            await logoutContentDialog.ShowAsync();
        }

        private void LogoutContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            (App.Current as App)._itemViewHolder.user.Logout();
            UpdateUserLayout();
        }

        private async void SignupButton_Click(object sender, RoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("https://dav-apps.tech/signup"));
        }
    }
}
