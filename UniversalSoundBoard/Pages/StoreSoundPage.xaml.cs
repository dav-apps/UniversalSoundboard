using System;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Models;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace UniversalSoundboard.Pages
{
    public sealed partial class StoreSoundPage : Page
    {
        private SoundResponse soundItem;
        private MediaPlayer mediaPlayer;
        private Uri sourceUri;
        private bool isPlaying = false;

        public StoreSoundPage()
        {
            InitializeComponent();
            mediaPlayer = new MediaPlayer();
            mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            SetThemeColors();
            UpdatePlayPauseButtonUI();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter == null)
                return;

            soundItem = e.Parameter as SoundResponse;
            sourceUri = new Uri(soundItem.Source);
            Bindings.Update();

            mediaPlayer.Source = MediaSource.CreateFromUri(new Uri(soundItem.AudioFileUrl));
        }

        private async void MediaPlayer_MediaEnded(MediaPlayer sender, object args)
        {
            isPlaying = false;

            await MainPage.dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                UpdatePlayPauseButtonUI();
            });
        }

        private void SetThemeColors()
        {
            RequestedTheme = FileManager.GetRequestedTheme();
            SolidColorBrush appThemeColorBrush = new SolidColorBrush(FileManager.GetApplicationThemeColor());
            ContentRoot.Background = appThemeColorBrush;
        }

        private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (isPlaying)
                mediaPlayer.Pause();
            else
                mediaPlayer.Play();

            isPlaying = !isPlaying;
            UpdatePlayPauseButtonUI();
        }

        private void UpdatePlayPauseButtonUI()
        {
            if (isPlaying)
            {
                PlayPauseButton.Content = "\uE103";
                PlayPauseButtonToolTip.Text = FileManager.loader.GetString("PauseButtonToolTip");
            }
            else
            {
                PlayPauseButton.Content = "\uE102";
                PlayPauseButtonToolTip.Text = FileManager.loader.GetString("PlayButtonToolTip");
            }
        }
    }
}
