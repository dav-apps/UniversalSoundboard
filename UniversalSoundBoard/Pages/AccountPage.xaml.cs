﻿using davClassLibrary;
using davClassLibrary.Controllers;
using Sentry;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UniversalSoundboard.Common;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Dialogs;
using Windows.Foundation.Metadata;
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
        bool outputDevicesFeatureVisible = false;
        bool davPlusHotkeyFeatureVisible = false;

        public AccountPage()
        {
            InitializeComponent();
            FileManager.itemViewHolder.PropertyChanged += ItemViewHolder_PropertyChanged;
            FileManager.itemViewHolder.UserSyncFinished += ItemViewHolder_UserSyncFinished;
            FileManager.itemViewHolder.UserPlanChanged += ItemViewHolder_UserPlanChanged;
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
            FileManager.itemViewHolder.UserPlanChanged -= ItemViewHolder_UserPlanChanged;

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

        private void ItemViewHolder_UserPlanChanged(object sender, EventArgs e)
        {
            UpdateUserLayout();
        }

        private void SetThemeColors()
        {
            RequestedTheme = FileManager.GetRequestedTheme();
            ContentRoot.Background = new SolidColorBrush(FileManager.GetApplicationThemeColor());

            // Set the dav logo
            var davLogoSource = new BitmapImage(new Uri(
                RequestedTheme == ElementTheme.Dark ? "ms-appx:///Assets/Images/dav-logo-text-white.png" : "ms-appx:///Assets/Images/dav-logo-text.png"
            ));

            DavLogoImage.Source = davLogoSource;
            DavLogoImage2.Source = davLogoSource;

            // Set the colors for the dav Plus card and logo background
            if (RequestedTheme == ElementTheme.Dark)
            {
                var backgroundColorBrush = new SolidColorBrush((Color)Application.Current.Resources["DarkThemeSecondaryBackgroundColor"]);
                PlusCardStackPanel.Background = backgroundColorBrush;

                var borderBrush = new SolidColorBrush((Color)Application.Current.Resources["DarkThemeBorderColor"]);
                PlusCardStackPanel.BorderBrush = backgroundColorBrush;
                PlusCardBorder1.BorderBrush = borderBrush;
                PlusCardBorder2.BorderBrush = borderBrush;
                PlusCardBorder3.BorderBrush = borderBrush;
                PlusCardBorder4.BorderBrush = borderBrush;
                PlusCardBorder5.BorderBrush = borderBrush;
            }
            else
            {
                PlusCardStackPanel.Background = new SolidColorBrush(FileManager.GetApplicationThemeColor());

                var borderBrush = new SolidColorBrush((Color)Application.Current.Resources["LightThemeBorderColor"]);
                PlusCardStackPanel.BorderBrush = borderBrush;
                PlusCardBorder1.BorderBrush = borderBrush;
                PlusCardBorder2.BorderBrush = borderBrush;
                PlusCardBorder3.BorderBrush = borderBrush;
                PlusCardBorder4.BorderBrush = borderBrush;
                PlusCardBorder5.BorderBrush = borderBrush;
            }
        }

        private void UpdateUserLayout()
        {
            if (Dav.IsLoggedIn)
                SetUsedStorageTextBlock();

            // Set the visibilities for the content elements
            LoggedInContent.Visibility = Dav.IsLoggedIn ? Visibility.Visible : Visibility.Collapsed;
            LoggedOutContent.Visibility = Dav.IsLoggedIn ? Visibility.Collapsed : Visibility.Visible;
            ManageSubscriptionButton.Visibility = Dav.IsLoggedIn && Dav.User.Plan > 0 ? Visibility.Visible : Visibility.Collapsed;

            outputDevicesFeatureVisible = !FileManager.itemViewHolder.PlusPurchased;
            davPlusHotkeyFeatureVisible = ApiInformation.IsApiContractPresent("Windows.ApplicationModel.FullTrustAppContract", 1, 0) && !FileManager.itemViewHolder.PlusPurchased;
            Bindings.Update();
        }

        public static async Task<bool> ShowLoginPage(bool showSignup = false)
        {
            try
            {
                Uri redirectUrl = WebAuthenticationBroker.GetCurrentApplicationCallbackUri();
                string action = showSignup ? "signup" : "login";
                Uri requestUrl = new Uri(string.Format("{0}/{1}?appId={2}&apiKey={3}&redirectUrl={4}", Constants.WebsiteBaseUrl, action, Constants.AppId, Constants.ApiKey, redirectUrl));

                var webAuthenticationResult = await WebAuthenticationBroker.AuthenticateAsync(WebAuthenticationOptions.None, requestUrl);
                if (webAuthenticationResult.ResponseStatus != WebAuthenticationStatus.Success) return false;

                // Get the access token from the response string
                string accessToken = webAuthenticationResult.ResponseData.Split(new[] { "accessToken=" }, StringSplitOptions.None)[1];

                // Log the user in with the access token
                Dav.Login(accessToken);
                ApiManager.ReloadClients(accessToken);

                // Show InAppNotification for sync
                FileManager.itemViewHolder.TriggerShowInAppNotificationEvent(
                    null,
                    new ShowInAppNotificationEventArgs(
                        InAppNotificationType.Sync,
                        FileManager.loader.GetString("InAppNotification-Sync"),
                        0,
                        true
                    )
                );

                if (FileManager.itemViewHolder.AllSounds.Count == 0)
                    FileManager.itemViewHolder.AppState = AppState.InitialSync;

                return true;
            }
            catch { }

            return false;
        }

        private void SetUsedStorageTextBlock()
        {
            if(Dav.User.TotalStorage > 0)
            {
                string message = FileManager.loader.GetString("Account-UsedStorage");

                string usedStorage = FileManager.GetFormattedSize(Convert.ToUInt64(Dav.User.UsedStorage));
                string totalStorage = FileManager.GetFormattedSize(Convert.ToUInt64(Dav.User.TotalStorage), true);
                double percentage = Dav.User.UsedStorage / (double)Dav.User.TotalStorage * 100;

                StorageProgressBar.Value = percentage;
                StorageTextBlock.Text = string.Format(message, usedStorage, totalStorage);
            }
        }
        
        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            bool result = await ShowLoginPage();
            UpdateUserLayout();

            SentrySdk.CaptureMessage("LoginButtonClick", scope =>
            {
                scope.SetTags(new Dictionary<string, string>
                {
                    { "Context", "AccountPage" },
                    { "Result", result.ToString() }
                });
            });
        }

        private async void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var logoutDialog = new LogoutDialog();
            logoutDialog.PrimaryButtonClick += LogoutContentDialog_PrimaryButtonClick;
            await logoutDialog.ShowAsync();
        }

        private async void LogoutContentDialog_PrimaryButtonClick(Dialog sender, ContentDialogButtonClickEventArgs args)
        {
            Dav.Logout();
            UpdateUserLayout();

            // Remove the sounds that are not saved locally
            await FileManager.RemoveNotLocallySavedSoundsAsync();

            SentrySdk.CaptureMessage("AccountPage-Logout");
        }

        private async void SignupButton_Click(object sender, RoutedEventArgs e)
        {
            bool result = await ShowLoginPage(true);
            UpdateUserLayout();

            SentrySdk.CaptureMessage("AccountPage-SignupButtonClick", scope =>
            {
                scope.SetTag("Result", result.ToString());
            });
        }

        private async void ManageSubscriptionButton_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri("https://dav-apps.tech/user#plans"));
        }

        private async void DavLogoImage_Tapped(object sender, TappedRoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri("https://dav-apps.tech"));
        }

        private async void UsernameTextBlock_Tapped(object sender, TappedRoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri("https://dav-apps.tech/user"));
        }

        private async void PlusCardSelectButton_Click(object sender, RoutedEventArgs e)
        {
            PlusCardSelectButton.IsEnabled = false;
            PlusCardSelectButton.Margin = new Thickness(32, 0, 0, 0);
            PlusCardSelectButtonProgressRing.Visibility = Visibility.Visible;

            SentrySdk.CaptureMessage("AccountPage-PlusCardSelectButtonClick");

            var createCheckoutSessionResponse = await CheckoutSessionsController.CreateCheckoutSession(
                1,
                Constants.CreateCheckoutSessionSuccessUrl,
                Constants.CreateCheckoutSessionCancelUrl
            );

            PlusCardSelectButton.IsEnabled = true;
            PlusCardSelectButton.Margin = new Thickness(0, 0, 0, 0);
            PlusCardSelectButtonProgressRing.Visibility = Visibility.Collapsed;

            if (createCheckoutSessionResponse.Success)
            {
                await Launcher.LaunchUriAsync(new Uri(createCheckoutSessionResponse.Data.SessionUrl));
            }
            else
            {
                if (createCheckoutSessionResponse.Errors.Length > 0)
                {
                    SentrySdk.CaptureMessage("AccountPage-PlusCardSelectButtonClick-Error", scope =>
                    {
                        scope.SetTag("ErrorCode", createCheckoutSessionResponse.Errors[0].Code.ToString());
                    });
                }

                // Show dialog for error
                await new UpgradeErrorDialog().ShowAsync();
            }
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
