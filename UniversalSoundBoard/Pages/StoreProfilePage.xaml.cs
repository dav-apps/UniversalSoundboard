using UniversalSoundboard.DataAccess;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace UniversalSoundboard.Pages
{
    public sealed partial class StoreProfilePage : Page
    {
        public StoreProfilePage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            SetThemeColors();
        }

        private void SetThemeColors()
        {
            RequestedTheme = FileManager.GetRequestedTheme();
            SolidColorBrush appThemeColorBrush = new SolidColorBrush(FileManager.GetApplicationThemeColor());
            ContentRoot.Background = appThemeColorBrush;
        }
    }
}
