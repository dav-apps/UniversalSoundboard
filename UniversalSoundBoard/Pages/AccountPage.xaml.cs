using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using UniversalSoundBoard;
using UniversalSoundBoard.Common;
using UniversalSoundBoard.DataAccess;
using Windows.Security.Authentication.Web;
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
            Color appThemeColor = FileManager.GetApplicationThemeColor();
            Color buttonColor = (App.Current as App).RequestedTheme == ApplicationTheme.Dark ? Colors.DimGray : Colors.LightGray;

            LoginButton.Background = new SolidColorBrush(buttonColor);
            SignupButton.Background = new SolidColorBrush(buttonColor);
            ContentRoot.Background = new SolidColorBrush(appThemeColor);
        }

        private void UpdateUserLayout()
        {
            ShowLoggedInContent();

            if ((App.Current as App)._itemViewHolder.User.IsLoggedIn)
            {
                SetUsedStorageTextBlock();
                (App.Current as App)._itemViewHolder.LoginMenuItemVisibility = false;
            }
            else
                (App.Current as App)._itemViewHolder.LoginMenuItemVisibility = true;
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

                            // Log the user in with the jwt
                            davClassLibrary.Models.DavUser user = new davClassLibrary.Models.DavUser();
                            await user.LoginAsync(jwt);
                            (App.Current as App)._itemViewHolder.User = user;
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
                }
            }
            else
            {
                Debug.WriteLine("No internet connection");
            }
        }

        private void SetUsedStorageTextBlock()
        {
            if((App.Current as App)._itemViewHolder.User.TotalStorage > 0)
            {
                string message = new Windows.ApplicationModel.Resources.ResourceLoader().GetString("Account-UsedStorage");

                double usedStorageGB = Math.Round((App.Current as App)._itemViewHolder.User.UsedStorage / 1000000000.0, 1);
                double totalStorageGB = Math.Round((App.Current as App)._itemViewHolder.User.TotalStorage / 1000000000.0, 1);

                double percentage = usedStorageGB / totalStorageGB * 100;

                StorageProgressBar.Value = percentage;
                StorageTextBlock.Text = string.Format(message, usedStorageGB.ToString(), totalStorageGB.ToString());

                if (totalStorageGB < 50)
                    UpgradeLink.Visibility = Visibility.Visible;
            }
        }
        
        private void ShowLoggedInContent()
        {
            if ((App.Current as App)._itemViewHolder.User.IsLoggedIn)
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

        private async void LogoutContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            (App.Current as App)._itemViewHolder.User.Logout();
            UpdateUserLayout();

            // Remove the sounds that are not saved locally
            await FileManager.RemoveNotLocallySavedSoundsAsync();
        }

        private async void SignupButton_Click(object sender, RoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("https://dav-apps.tech/signup"));
        }
    }
}
