using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UniversalSoundboard.Components;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Models;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace UniversalSoundboard.Pages
{
    public sealed partial class StorePage : Page
    {
        List<SoundResponse> soundItems = new List<SoundResponse>();
        MediaPlayer mediaPlayer;

        public StorePage()
        {
            InitializeComponent();
            mediaPlayer = new MediaPlayer();
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

        private void StoreSoundTileTemplate_Play(object sender, EventArgs e)
        {
            SoundResponse soundItem = (sender as StoreSoundTileTemplate).SoundItem;
            mediaPlayer.Pause();
            mediaPlayer.Source = MediaSource.CreateFromUri(new Uri(soundItem.AudioFileUrl));
            mediaPlayer.Play();
        }

        private void StoreSoundTileTemplate_Pause(object sender, EventArgs e)
        {
            mediaPlayer.Pause();
        }
    }
}
