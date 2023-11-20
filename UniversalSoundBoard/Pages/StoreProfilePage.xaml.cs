using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UniversalSoundboard.Components;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Dialogs;
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
    public sealed partial class StoreProfilePage : Page
    {
        List<SoundResponse> sounds = new List<SoundResponse>();
        private string numberOfSoundsText = "0 sounds";
        MediaPlayer mediaPlayer;
        StoreSoundTileTemplate currentSoundItemTemplate;

        public StoreProfilePage()
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
            var listSoundsResult = await ApiManager.ListSounds(mine: true);
            if (listSoundsResult.Items == null) return;

            sounds = listSoundsResult.Items;
            numberOfSoundsText = listSoundsResult.Total.ToString() + " sounds";
            Bindings.Update();
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

        private void StoreSoundTileTemplate_Pause(object sender, System.EventArgs e)
        {
            mediaPlayer.Pause();
        }

        private void PublishSoundButton_Click(object sender, RoutedEventArgs e)
        {
            MainPage.NavigateToPage(typeof(PublishSoundPage), new DrillInNavigationTransitionInfo());
        }
    }
}
