using System.Collections.Generic;
using System.Threading.Tasks;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Models;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace UniversalSoundboard.Pages
{
    public sealed partial class StoreProfilePage : Page
    {
        List<SoundResponse> sounds = new List<SoundResponse>();
        private string numberOfSoundsText = "0 sounds";

        public StoreProfilePage()
        {
            InitializeComponent();
        }

        private async void Page_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
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
            var listSoundsResult = await ApiManager.ListSounds(mine: true);
            if (listSoundsResult.Items == null) return;

            sounds = listSoundsResult.Items;
            numberOfSoundsText = listSoundsResult.Total.ToString() + " sounds";
            Bindings.Update();
        }

        private void SoundsGridView_ItemClick(object sender, ItemClickEventArgs e)
        {

        }

        private void StoreSoundTileTemplate_Play(object sender, System.EventArgs e)
        {

        }

        private void StoreSoundTileTemplate_Pause(object sender, System.EventArgs e)
        {

        }
    }
}
