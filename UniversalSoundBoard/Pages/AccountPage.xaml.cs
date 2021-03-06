﻿using davClassLibrary;
using System;
using System.Threading.Tasks;
using UniversalSoundboard.Common;
using UniversalSoundboard.DataAccess;
using Windows.ApplicationModel.Resources;
using Windows.Security.Authentication.Web;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace UniversalSoundboard.Pages
{
    public sealed partial class AccountPage : Page
    {
        ResourceLoader loader = new ResourceLoader();

        public AccountPage()
        {
            InitializeComponent();
            FileManager.itemViewHolder.PropertyChanged += ItemViewHolder_PropertyChanged;
            FileManager.itemViewHolder.UserSyncFinished += ItemViewHolder_UserSyncFinished;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ContentRoot.DataContext = FileManager.itemViewHolder;
            SetThemeColors();
            UpdateUserLayout();

            SharedShadow.Receivers.Add(TopBackgroundGrid);
            SharedShadow.Receivers.Add(BottomBackgroundGrid);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            FileManager.itemViewHolder.PropertyChanged -= ItemViewHolder_PropertyChanged;
            FileManager.itemViewHolder.UserSyncFinished -= ItemViewHolder_UserSyncFinished;

            base.OnNavigatedFrom(e);
        }

        private void ItemViewHolder_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if(e.PropertyName.Equals(ItemViewHolder.CurrentThemeKey))
                SetThemeColors();
        }

        private void ItemViewHolder_UserSyncFinished(object sender, EventArgs e)
        {
            Bindings.Update();
            UpdateUserLayout();
        }

        private void SetThemeColors()
        {
            RequestedTheme = FileManager.GetRequestedTheme();
            ContentRoot.Background = new SolidColorBrush(FileManager.GetApplicationThemeColor());

            // Set the dav logo
            DavLogoImage.Source = new BitmapImage(new Uri(
                RequestedTheme == ElementTheme.Dark ? "ms-appx:///Assets/Images/dav-logo-text-white.png" : "ms-appx:///Assets/Images/dav-logo-text.png"
            ));

            // Set the colors for the dav Plus card
            if (RequestedTheme == ElementTheme.Dark)
            {
                var backgroundColorBrush = new SolidColorBrush((Color)Application.Current.Resources["DarkThemeSecondaryBackgroundColor"]);
                PlusCardStackPanel.Background = backgroundColorBrush;

                var borderBrush = new SolidColorBrush((Color)Application.Current.Resources["DarkThemeBorderColor"]);
                PlusCardStackPanel.BorderBrush = backgroundColorBrush;
                PlusCardBorder1.BorderBrush = borderBrush;
                PlusCardBorder2.BorderBrush = borderBrush;
                PlusCardBorder3.BorderBrush = borderBrush;
            }
            else
            {
                PlusCardStackPanel.Background = new SolidColorBrush(FileManager.GetApplicationThemeColor());

                var borderBrush = new SolidColorBrush((Color)Application.Current.Resources["LightThemeBorderColor"]);
                PlusCardStackPanel.BorderBrush = borderBrush;
                PlusCardBorder1.BorderBrush = borderBrush;
                PlusCardBorder2.BorderBrush = borderBrush;
                PlusCardBorder3.BorderBrush = borderBrush;
            }
        }

        private void UpdateUserLayout()
        {
            if (Dav.IsLoggedIn)
                SetUsedStorageTextBlock();

            // Set the visibilities for the content elements
            LoggedInContent.Visibility = Dav.IsLoggedIn ? Visibility.Visible : Visibility.Collapsed;
            LoggedOutContent.Visibility = Dav.IsLoggedIn ? Visibility.Collapsed : Visibility.Visible;
        }

        public static async Task<bool> ShowLoginPage(bool showSignup = false)
        {
            try
            {
                Uri redirectUrl = WebAuthenticationBroker.GetCurrentApplicationCallbackUri();
                string action = showSignup ? "signup" : "login";
                Uri requestUrl = new Uri(string.Format("{0}/{1}?appId={2}&apiKey={3}&redirectUrl={4}", FileManager.WebsiteBaseUrl, action, FileManager.AppId, FileManager.ApiKey, redirectUrl));

                var webAuthenticationResult = await WebAuthenticationBroker.AuthenticateAsync(WebAuthenticationOptions.None, requestUrl);
                if (webAuthenticationResult.ResponseStatus != WebAuthenticationStatus.Success) return false;

                // Get the access token from the response string
                string accessToken = webAuthenticationResult.ResponseData.Split(new[] { "accessToken=" }, StringSplitOptions.None)[1];

                // Log the user in with the access token
                Dav.Login(accessToken);
                return true;
            }
            catch { }

            return false;
        }

        private void SetUsedStorageTextBlock()
        {
            if(Dav.User.TotalStorage > 0)
            {
                string message = loader.GetString("Account-UsedStorage");

                string usedStorage = FileManager.GetFormattedSize(Convert.ToUInt64(Dav.User.UsedStorage));
                string totalStorage = FileManager.GetFormattedSize(Convert.ToUInt64(Dav.User.TotalStorage), true);
                double percentage = Dav.User.UsedStorage / (double)Dav.User.TotalStorage * 100;

                StorageProgressBar.Value = percentage;
                StorageTextBlock.Text = string.Format(message, usedStorage, totalStorage);
            }
        }
        
        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            await ShowLoginPage();
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
            Dav.Logout();
            UpdateUserLayout();

            // Remove the sounds that are not saved locally
            await FileManager.RemoveNotLocallySavedSoundsAsync();
        }

        private async void SignupButton_Click(object sender, RoutedEventArgs e)
        {
            await ShowLoginPage(true);
            UpdateUserLayout();
        }

        private async void DavLogoImage_Tapped(object sender, TappedRoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri("https://dav-apps.tech"));
        }

        private async void DropShadowPanel_Tapped(object sender, TappedRoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri("https://dav-apps.tech/login?redirect=user"));
        }

        private async void PlusCardSelectButton_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri("https://dav-apps.tech/login?redirect=user%23plans%0A"));
        }

        private void Image_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Hand, 0);
        }

        private void Image_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 0);
        }
    }
}
