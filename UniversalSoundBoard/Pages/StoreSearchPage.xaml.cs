using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Models;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace UniversalSoundboard.Pages
{
    public sealed partial class StoreSearchPage : Page
    {
        ObservableCollection<SoundResponse> sounds = new ObservableCollection<SoundResponse>();

        public StoreSearchPage()
        {
            InitializeComponent();
        }

        private void Page_Loading(FrameworkElement sender, object args)
        {
            SetThemeColors();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            string searchText = e.Parameter as string;

            if (searchText != null)
                SearchAutoSuggestBox.Text = searchText;
        }

        private async void SearchAutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (SearchAutoSuggestBox.Text.Length == 0)
                sounds.Clear();
            else
                await LoadSounds();
        }

        private void SoundsGridView_ItemClick(object sender, ItemClickEventArgs e)
        {

        }

        private void StoreSoundTileTemplate_Play(object sender, EventArgs e)
        {

        }

        private void StoreSoundTileTemplate_Pause(object sender, EventArgs e)
        {

        }

        private void SetThemeColors()
        {
            RequestedTheme = FileManager.GetRequestedTheme();
            SolidColorBrush appThemeColorBrush = new SolidColorBrush(FileManager.GetApplicationThemeColor());
            ContentRoot.Background = appThemeColorBrush;
        }

        private async Task LoadSounds()
        {
            ListResponse<SoundResponse> listSoundsResponse = await ApiManager.ListSounds(query: SearchAutoSuggestBox.Text);
            if (listSoundsResponse.Items == null) return;

            sounds.Clear();

            foreach (var sound in listSoundsResponse.Items)
                sounds.Add(sound);
        }
    }
}
