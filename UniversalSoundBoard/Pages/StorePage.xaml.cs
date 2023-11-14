using System;
using System.Collections.Generic;
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

namespace UniversalSoundboard.Pages
{
    public sealed partial class StorePage : Page
    {
        List<SoundResponse> soundItems = new List<SoundResponse>();
        MediaPlayer mediaPlayer;
        StoreSoundTileTemplate currentSoundItemTemplate;

        public StorePage()
        {
            InitializeComponent();
            mediaPlayer = new MediaPlayer();
            mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            SetThemeColors();
            await LoadSounds();
        }

        private async void MediaPlayer_MediaEnded(MediaPlayer sender, object args)
        {
            await MainPage.dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                currentSoundItemTemplate.PlaybackStopped();
            });
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

        private void StoreSoundTileTemplate_Play(object sender, EventArgs e)
        {
            if (currentSoundItemTemplate != null)
                currentSoundItemTemplate.PlaybackStopped();

            currentSoundItemTemplate = (sender as StoreSoundTileTemplate);
            mediaPlayer.Pause();
            mediaPlayer.Source = MediaSource.CreateFromUri(new Uri(currentSoundItemTemplate.SoundItem.AudioFileUrl));
            mediaPlayer.Play();
        }

        private void StoreSoundTileTemplate_Pause(object sender, EventArgs e)
        {
            mediaPlayer.Pause();
        }

        private void SoundsGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var clickedItem = e.ClickedItem as SoundResponse;
            MainPage.NavigateToPage(typeof(StoreSoundPage), clickedItem, new DrillInNavigationTransitionInfo());
        }
    }
}
