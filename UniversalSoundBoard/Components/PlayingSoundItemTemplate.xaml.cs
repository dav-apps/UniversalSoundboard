﻿using System;
using System.Linq;
using System.Threading.Tasks;
using UniversalSoundboard.Models;
using UniversalSoundBoard.DataAccess;
using Windows.ApplicationModel.Resources;
using Windows.Media.Playback;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UniversalSoundBoard.Components
{
    public sealed partial class PlayingSoundItemTemplate : UserControl
    {
        public PlayingSound PlayingSound { get; set; }
        private readonly ResourceLoader loader = new ResourceLoader();

        public PlayingSoundItemTemplate()
        {
            InitializeComponent();

            ContentRoot.DataContext = FileManager.itemViewHolder;
            DataContextChanged += PlayingSoundTemplate_DataContextChanged;
        }

        private void PlayingSoundTemplate_Loaded(object sender, RoutedEventArgs eventArgs)
        {
            Init();
            AdjustLayout();
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            AdjustLayout();
        }

        private void PlayingSoundTemplate_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (DataContext == null) return;
            
            PlayingSound = DataContext as PlayingSound;
            Init();
        }

        private void Init()
        {
            if (PlayingSound == null || PlayingSound.MediaPlayer == null) return;

            // Set the media player for the media player element
            MediaPlayerElement.SetMediaPlayer(PlayingSound.MediaPlayer);

            // Set the name of the current sound
            PlayingSoundNameTextBlock.Text = PlayingSound.Sounds.ElementAt(PlayingSound.Current).Name;

            UpdatePlayPauseButton();
        }

        #region MediaControlButton events
        private void VolumeButton_Click(object sender, RoutedEventArgs e)
        {
            // Set the value of the volume slider
            VolumeSlider.Value = Convert.ToInt32(PlayingSound.MediaPlayer.Volume * 100);
        }

        private void VolumeSlider_ValueChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            // Apply the new volume
            PlayingSound.MediaPlayer.Volume = VolumeSlider.Value / 100;
        }

        private async void VolumeSlider_LostFocus(object sender, RoutedEventArgs e)
        {
            // Save the new volume
            await FileManager.SetVolumeOfPlayingSoundAsync(PlayingSound.Uuid, VolumeSlider.Value / 100);
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            TogglePlayPause();
            await Task.Delay(25);
            UpdatePlayPauseButton();
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            // Delete the PlayingSound and remove it from the list
            await FileManager.DeletePlayingSoundAsync(PlayingSound.Uuid);
            FileManager.itemViewHolder.PlayingSounds.Remove(PlayingSound);

            // Disable the MediaPlayer
            if (PlayingSound.MediaPlayer != null)
            {
                PlayingSound.MediaPlayer.Pause();
                MediaPlayerElement.SetMediaPlayer(null);
                PlayingSound.MediaPlayer.SystemMediaTransportControls.IsEnabled = false;
                PlayingSound.MediaPlayer = null;
            }
        }

        private void MoreButton_Click(object sender, RoutedEventArgs e)
        {

        }
        #endregion

        #region UI methods
        private void AdjustLayout()
        {

        }

        private void TogglePlayPause()
        {
            if (PlayingSound.MediaPlayer == null) return;
            if (PlayingSound.MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
                PlayingSound.MediaPlayer.Pause();
            else
                PlayingSound.MediaPlayer.Play();
        }

        private void UpdatePlayPauseButton()
        {
            if (PlayingSound.MediaPlayer == null) return;
            if (PlayingSound.MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
            {
                PlayPauseButton.Content = "\uE103";
                PlayPauseButtonToolTip.Text = loader.GetString("PauseButtonToolTip");
            }
            else
            {
                PlayPauseButton.Content = "\uE102";
                PlayPauseButtonToolTip.Text = loader.GetString("PlayButtonToolTip");
            }
        }
        #endregion
    }
}
