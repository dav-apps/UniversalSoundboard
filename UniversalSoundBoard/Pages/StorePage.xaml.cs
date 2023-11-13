using System.Threading.Tasks;
using UniversalSoundboard.DataAccess;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace UniversalSoundboard.Pages
{
    public sealed partial class StorePage : Page
    {
        public StorePage()
        {
            InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            SetThemeColors();
            await LoadSounds();
        }

        private void SetThemeColors()
        {
            RequestedTheme = FileManager.GetRequestedTheme();
            SolidColorBrush appThemeColorBrush = new SolidColorBrush(FileManager.GetApplicationThemeColor());
            ContentRoot.Background = appThemeColorBrush;
        }

        private async Task LoadSounds()
        {
            await ApiManager.ListSounds();
        }
    }
}
