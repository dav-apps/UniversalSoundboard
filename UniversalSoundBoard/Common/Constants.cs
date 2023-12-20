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
    }
}
