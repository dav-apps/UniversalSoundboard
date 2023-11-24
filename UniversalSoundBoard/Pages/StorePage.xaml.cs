using System;
using System.Collections.Generic;
using System.Linq;
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
        List<SoundResponse> soundsOfTheDay = new List<SoundResponse>();
        List<SoundResponse> recentlyAddedSounds = new List<SoundResponse>();
        List<string> tags = new List<string>();
        MediaPlayer mediaPlayer;
        StoreSoundTileTemplate currentSoundItemTemplate;

        public StorePage()
        {
            InitializeComponent();
            mediaPlayer = new MediaPlayer();
            mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;

            LoadTags();
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

        private void LoadTags()
        {
            Random random = new Random();
            List<string> originalTags = FileManager.GetStoreTags();

            for (int i = 0; i < originalTags.Count; i++)
            {
                int randomIndex = random.Next(originalTags.Count);

                tags.Add(originalTags.ElementAt(randomIndex));
                originalTags.RemoveAt(randomIndex);
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

        private void SoundsGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            MainPage.NavigateToPage(
                typeof(StoreSoundPage),
                e.ClickedItem as SoundResponse,
                new DrillInNavigationTransitionInfo()
            );
        }

        private void TagsGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            MainPage.NavigateToPage(
                typeof(StoreSearchPage),
                e.ClickedItem,
                new DrillInNavigationTransitionInfo()
            );
        }
    }
}
