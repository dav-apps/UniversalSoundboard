using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using UniversalSoundboard.Components;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Models;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace UniversalSoundboard.Pages
{
    public sealed partial class StoreSearchPage : Page
    {
        ObservableCollection<SoundResponse> sounds = new ObservableCollection<SoundResponse>();
        MediaPlayer mediaPlayer;
        StoreSoundTileTemplate currentSoundItemTemplate;

        public StoreSearchPage()
        {
            InitializeComponent();
            mediaPlayer = new MediaPlayer();
            mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
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

        private async void MediaPlayer_MediaEnded(MediaPlayer sender, object args)
        {
            await MainPage.dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                currentSoundItemTemplate.PlaybackStopped();
            });
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
            var item = e.ClickedItem as SoundResponse;

            if (item.AudioFileUrl != null)
            {
                MainPage.NavigateToPage(
                    typeof(StoreSoundPage),
                    item,
                    new DrillInNavigationTransitionInfo()
                );
            }
        }

        private void StoreSoundTileTemplate_Play(object sender, EventArgs e)
        {
            if (currentSoundItemTemplate != null)
                currentSoundItemTemplate.PlaybackStopped();

            currentSoundItemTemplate = sender as StoreSoundTileTemplate;
            mediaPlayer.Pause();
            mediaPlayer.Source = MediaSource.CreateFromUri(new Uri(currentSoundItemTemplate.SoundItem.AudioFileUrl));
            mediaPlayer.Play();
        }

        private void StoreSoundTileTemplate_Pause(object sender, EventArgs e)
        {
            mediaPlayer.Pause();
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
