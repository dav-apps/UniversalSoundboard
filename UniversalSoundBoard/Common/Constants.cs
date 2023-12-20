using Windows.UI;

namespace UniversalSoundboard.Common
{
    public class Constants
    {
        #region Design constants
        public const int mobileMaxWidth = 775;
        public const int topButtonsCollapsedMaxWidth = 1600;
        public const int sideBarCollapsedMaxWidth = 1100;
        public const int hideSearchBoxMaxWidth = 800;
        #endregion

        #region Colors for the background of PlayingSoundsBar and SideBar
        public const double sideBarAcrylicBackgroundTintOpacity = 0.6;
        public const double playingSoundsBarAcrylicBackgroundTintOpacity = 0.85;
        public const double secondaryWindowAcrylicBackgroundTintOpacity = 0.85;
        public static readonly Color sideBarLightBackgroundColor = Color.FromArgb(255, 245, 245, 245);            // #f5f5f5
        public static readonly Color sideBarDarkBackgroundColor = Color.FromArgb(255, 29, 34, 49);                // #1d2231
        public static readonly Color playingSoundsBarLightBackgroundColor = Color.FromArgb(255, 253, 253, 253);   // #fdfdfd
        public static readonly Color playingSoundsBarDarkBackgroundColor = Color.FromArgb(255, 15, 20, 35);       // #0f1423
        public static readonly Color secondaryWindowLightBackgroundColor = Color.FromArgb(255, 255, 255, 255);    // #ffffff
        public static readonly Color secondaryWindowDarkBackgroundColor = Color.FromArgb(255, 13, 18, 33);        // #0d1221
        #endregion

        #region dav Keys
        public static string ApiKey { get => Env.DavApiKey; }
        public const string WebsiteBaseUrl = "https://dav-login-7ymir.ondigitalocean.app";
        public const string UniversalSoundboardWebsiteBaseUrl = "https://universalsoundboard.dav-apps.tech";
        public const string ApiBaseUrl = "https://universalsoundboard-api-rmkdv.ondigitalocean.app/";
        public const int AppId = 1;
        public const int SoundFileTableId = 6;
        public const int ImageFileTableId = 7;
        public const int CategoryTableId = 8;
        public const int SoundTableId = 5;
        public const int PlayingSoundTableId = 9;
        public const int OrderTableId = 12;
        #endregion
    }
}
