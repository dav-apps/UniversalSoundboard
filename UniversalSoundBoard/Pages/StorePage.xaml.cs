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
        List<SoundResponse> recentlyAddedSounds = new List<SoundResponse>();
        List<SoundResponse> soundsOfTheDay = new List<SoundResponse>();
        MediaPlayer mediaPlayer;
        StoreSoundTileTemplate currentSoundItemTemplate;

        public StorePage()
        {
            InitializeComponent();
            mediaPlayer = new MediaPlayer();
            mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            SetThemeColors();
            LoadSounds();
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

        private void LoadSounds()
        {
            var a = LoadSoundsOfTheDay();
            var b = LoadRecentlyAddedSounds();
        }

        private async Task LoadSoundsOfTheDay()
        {
            var soundsOfTheDayResult = await ApiManager.ListSounds(random: true);
            if (soundsOfTheDayResult.Items == null) return;

            soundsOfTheDay = soundsOfTheDayResult.Items;
            Bindings.Update();
        }

        private async Task LoadRecentlyAddedSounds()
        {
            var recentlyAddedSoundsResult = await ApiManager.ListSounds();
            if (recentlyAddedSoundsResult.Items == null) return;

            recentlyAddedSounds = recentlyAddedSoundsResult.Items;
            Bindings.Update();
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

        private void SoundsGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            MainPage.NavigateToPage(
                typeof(StoreSoundPage),
                e.ClickedItem as SoundResponse,
                new DrillInNavigationTransitionInfo()
            );
        }
    }
}
