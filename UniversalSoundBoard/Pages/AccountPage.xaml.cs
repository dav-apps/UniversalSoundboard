using System;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using UniversalSoundBoard.Common;
using UniversalSoundBoard.DataAccess;
using Windows.ApplicationModel.Resources;
using Windows.Security.Authentication.Web;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace UniversalSoundboard.Pages
{
    public sealed partial class AccountPage : Page
    {
        ResourceLoader loader = new ResourceLoader();

        public AccountPage()
        {
            InitializeComponent();
            FileManager.itemViewHolder.PropertyChanged += ItemViewHolder_PropertyChanged;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ContentRoot.DataContext = FileManager.itemViewHolder;
            SetThemeColors();
            UpdateUserLayout();

            SharedShadow.Receivers.Add(TopBackgroundGrid);
            SharedShadow.Receivers.Add(BottomBackgroundGrid);
        }

        private void ItemViewHolder_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if(e.PropertyName.Equals(ItemViewHolder.CurrentThemeKey))
                SetThemeColors();
        }

        private void SetThemeColors()
        {
            RequestedTheme = FileManager.GetRequestedTheme();
            ContentRoot.Background = new SolidColorBrush(FileManager.GetApplicationThemeColor());

            // Set the dav logo
            DavLogoImage.Source = new BitmapImage(new Uri(
                RequestedTheme == ElementTheme.Dark ? "ms-appx:///Assets/Images/dav-logo-text-white.png" : "ms-appx:///Assets/Images/dav-logo-text.png"
            ));
        }

        private void UpdateUserLayout()
        {
            if (FileManager.itemViewHolder.User.IsLoggedIn)
                SetUsedStorageTextBlock();

            // Set the visibilities for the content elements
            LoggedInContent.Visibility = FileManager.itemViewHolder.User.IsLoggedIn ? Visibility.Visible : Visibility.Collapsed;
            LoggedOutContent.Visibility = FileManager.itemViewHolder.User.IsLoggedIn ? Visibility.Collapsed : Visibility.Visible;
        }

        public static async Task<bool> Login()
        {
            if (NetworkInterface.GetIsNetworkAvailable())
            {
                try
                {
                    Uri redirectUrl = WebAuthenticationBroker.GetCurrentApplicationCallbackUri();
                    Uri requestUrl = new Uri(string.Format("{0}/login_session?api_key={1}&app_id={2}&redirect_url={3}", FileManager.WebsiteBaseUrl, FileManager.ApiKey, FileManager.AppId, redirectUrl));

                    var webAuthenticationResult = await WebAuthenticationBroker.AuthenticateAsync(WebAuthenticationOptions.None, requestUrl);
                    if (webAuthenticationResult.ResponseStatus != WebAuthenticationStatus.Success) return false;

                    // Get the JWT from the response string
                    string jwt = webAuthenticationResult.ResponseData.Split(new[] { "jwt=" }, StringSplitOptions.None)[1];

                    // Log the user in with the jwt
                    davClassLibrary.Models.DavUser user = new davClassLibrary.Models.DavUser();
                    await user.LoginAsync(jwt);
                    FileManager.itemViewHolder.User = user;
                    return true;
                }
                catch { }
            }

            return false;
        }

        private void SetUsedStorageTextBlock()
        {
            if(FileManager.itemViewHolder.User.TotalStorage > 0)
            {
                string message = loader.GetString("Account-UsedStorage");

                string usedStorage = FileManager.GetFormattedSize(Convert.ToUInt64(FileManager.itemViewHolder.User.UsedStorage));
                string totalStorage = FileManager.GetFormattedSize(Convert.ToUInt64(FileManager.itemViewHolder.User.TotalStorage), true);
                double percentage = FileManager.itemViewHolder.User.UsedStorage / (double)FileManager.itemViewHolder.User.TotalStorage * 100;

                StorageProgressBar.Value = percentage;
                StorageTextBlock.Text = string.Format(message, usedStorage, totalStorage);
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
            FileManager.itemViewHolder.User.Logout();
            UpdateUserLayout();

            // Remove the sounds that are not saved locally
            await FileManager.RemoveNotLocallySavedSoundsAsync();
        }

        private async void SignupButton_Click(object sender, RoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("https://dav-apps.tech/signup"));
        }

        private async void PlusCardSelectButton_Click(object sender, RoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("https://dav-apps.tech/login?redirect=user%23plans%0A"));
        }

        private async void DavLogoImage_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("https://dav-apps.tech"));
        }

        private void DavLogoImage_PointerEntered(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Hand, 0);
        }

        private void DavLogoImage_PointerExited(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 0);
        }
    }
}
