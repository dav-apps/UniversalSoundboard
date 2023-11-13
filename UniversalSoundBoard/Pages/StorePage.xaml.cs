using System.Collections.Generic;
using System.Threading.Tasks;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Models;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace UniversalSoundboard.Pages
{
    public sealed partial class StorePage : Page
    {
        List<SoundResponse> soundItems = new List<SoundResponse>();

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
            var listSoundsResult = await ApiManager.ListSounds();
            if (listSoundsResult.Items == null) return;

            soundItems = listSoundsResult.Items;
            Bindings.Update();
        }
    }
}
