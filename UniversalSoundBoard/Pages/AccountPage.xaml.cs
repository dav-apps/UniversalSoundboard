using System;
using System.Diagnostics;
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
        string jwt = "";

        public AccountPage()
        {
            InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            SetDataContext();
            SetDarkThemeLayout();
            await UpdateUserLayout();
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

        private async Task UpdateUserLayout()
        {
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
                    (App.Current as App)._itemViewHolder.loginMenuItemVisibility = false;
                }
                else
                {
                    (App.Current as App)._itemViewHolder.loginMenuItemVisibility = true;
                }
            }
            else
            {
                (App.Current as App)._itemViewHolder.loginMenuItemVisibility = true;
            }
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
        
        private void ShowLoggedInContent()
        {
            if (!String.IsNullOrEmpty(jwt))
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
            await ApiManager.Login();
            await UpdateUserLayout();
        }

        private async void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var logoutContentDialog = ContentDialogs.CreateLogoutContentDialog();
            logoutContentDialog.PrimaryButtonClick += LogoutContentDialog_PrimaryButtonClick;
            await logoutContentDialog.ShowAsync();
        }

        private async void LogoutContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            ApiManager.Logout();
            await UpdateUserLayout();
        }

        private async void SignupButton_Click(object sender, RoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("https://dav-apps.tech/signup"));
        }
    }
}
