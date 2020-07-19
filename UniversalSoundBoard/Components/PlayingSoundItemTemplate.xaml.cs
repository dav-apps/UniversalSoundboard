using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UniversalSoundboard.Models;
using UniversalSoundBoard.DataAccess;
using UniversalSoundBoard.Models;
using Windows.ApplicationModel.Resources;
using Windows.Media.Playback;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UniversalSoundBoard.Components
{
    public sealed partial class PlayingSoundItemTemplate : UserControl
    {
        public PlayingSound PlayingSound { get; set; }
        private readonly ResourceLoader loader = new ResourceLoader();
        CoreDispatcher dispatcher;

        public PlayingSoundItemTemplate()
        {
            InitializeComponent();
            dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;

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
            PlayingSound.MediaPlayer.MediaEnded -= MediaPlayer_MediaEnded;
            PlayingSound.MediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
            PlayingSound.MediaPlayer.PlaybackSession.PlaybackStateChanged -= PlaybackSession_PlaybackStateChanged;
            PlayingSound.MediaPlayer.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;
            ((MediaPlaybackList)PlayingSound.MediaPlayer.Source).CurrentItemChanged -= PlayingSoundItemTemplate_CurrentItemChanged;
            ((MediaPlaybackList)PlayingSound.MediaPlayer.Source).CurrentItemChanged += PlayingSoundItemTemplate_CurrentItemChanged;
            PlayingSound.MediaPlayer.CommandManager.PreviousReceived -= CommandManager_PreviousReceived;
            PlayingSound.MediaPlayer.CommandManager.PreviousReceived += CommandManager_PreviousReceived;

            // Set the name of the current sound
            PlayingSoundNameTextBlock.Text = PlayingSound.Sounds.ElementAt(PlayingSound.Current).Name;
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
            MoveToPrevious();
        }

        private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            TogglePlayPause();
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (PlayingSound == null || PlayingSound.MediaPlayer == null) return;
            ((MediaPlaybackList)PlayingSound.MediaPlayer.Source).MoveNext();
        }

        private async void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            await RemovePlayingSound();
        }

        private async void MoreButton_Repeat_1x_Click(object sender, RoutedEventArgs e)
        {
            await RepeatAsync(2);
        }

        private async void MoreButton_Repeat_2x_Click(object sender, RoutedEventArgs e)
        {
            await RepeatAsync(3);
        }

        private async void MoreButton_Repeat_5x_Click(object sender, RoutedEventArgs e)
        {
            await RepeatAsync(6);
        }

        private async void MoreButton_Repeat_10x_Click(object sender, RoutedEventArgs e)
        {
            await RepeatAsync(11);
        }

        private async void MoreButton_Repeat_endless_Click(object sender, RoutedEventArgs e)
        {
            await RepeatAsync(int.MaxValue);
        }
        #endregion

        #region Functionality
        private void MoveToPrevious()
        {
            if (PlayingSound == null || PlayingSound.MediaPlayer == null) return;
            if (PlayingSound.MediaPlayer.PlaybackSession.Position.Seconds >= 5)
            {
                // Move to the start of the sound
                PlayingSound.MediaPlayer.PlaybackSession.Position = new TimeSpan(0);
            }
            else
            {
                // Move to the previous sound
                ((MediaPlaybackList)PlayingSound.MediaPlayer.Source).MovePrevious();
            }
        }

        private async Task RemovePlayingSound()
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

        private async Task RepeatAsync(int repetitions)
        {
            PlayingSound.Repetitions = repetitions;
            await FileManager.SetRepetitionsOfPlayingSoundAsync(PlayingSound.Uuid, repetitions);
        }
        #endregion

        #region UI methods
        private void AdjustLayout()
        {
            if (PlayingSound == null || PlayingSound.MediaPlayer == null) return;

            // Set the visibility of the Previous and Next buttons
            int currentItemIndex = (int)((MediaPlaybackList)PlayingSound.MediaPlayer.Source).CurrentItemIndex;
            PreviousButton.Visibility = currentItemIndex > 0 ? Visibility.Visible : Visibility.Collapsed;
            NextButton.Visibility = currentItemIndex != PlayingSound.Sounds.Count - 1 ? Visibility.Visible : Visibility.Collapsed;
        }

        /**
         * Toggles the MediaPlayer from Playing -> Paused or from Paused -> Playing
         */
        private void TogglePlayPause()
        {
            if (PlayingSound.MediaPlayer == null) return;
            if (PlayingSound.MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Opening || PlayingSound.MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
                PlayingSound.MediaPlayer.Pause();
            else
                PlayingSound.MediaPlayer.Play();
        }

        private void UpdatePlayPauseButton()
        {
            UpdatePlayPauseButton(
                PlayingSound.MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Opening
                || PlayingSound.MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing
            );
        }

        private void UpdatePlayPauseButton(bool isPlaying)
        {
            if (PlayingSound.MediaPlayer == null) return;
            if (isPlaying)
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

        #region MediaPlayer event handlers
        private async void MediaPlayer_MediaEnded(MediaPlayer sender, object args)
        {
            await dispatcher.RunAsync(CoreDispatcherPriority.Low, async () =>
            {
                PlayingSound.Repetitions--;

                if (PlayingSound.Repetitions <= 0)
                {
                    // Delete and remove the PlayingSound
                    await RemovePlayingSound();
                    return;
                }

                // Set the new repetitions
                await FileManager.SetRepetitionsOfPlayingSoundAsync(PlayingSound.Uuid, PlayingSound.Repetitions);

                if(PlayingSound.Sounds.Count > 1)
                {
                    // If randomly is true, shuffle sounds
                    if (PlayingSound.Randomly)
                    {
                        Random random = new Random();

                        // Copy the sounds
                        List<Sound> sounds = new List<Sound>();
                        foreach (var sound in PlayingSound.Sounds)
                            sounds.Add(sound);

                        // Copy the MediaPlaybackList
                        List<MediaPlaybackItem> mediaPlaybackItems = new List<MediaPlaybackItem>();
                        foreach (var item in ((MediaPlaybackList)PlayingSound.MediaPlayer.Source).Items)
                            mediaPlaybackItems.Add(item);

                        PlayingSound.Sounds.Clear();

                        // Add the sounds in random order
                        for(int i = 0; i < sounds.Count; i++)
                        {
                            // Take a random sound from the sounds list and add it to the original sounds list
                            int randomIndex = random.Next(sounds.Count);

                            PlayingSound.Sounds.Add(sounds.ElementAt(randomIndex));
                            sounds.RemoveAt(randomIndex);

                            ((MediaPlaybackList)PlayingSound.MediaPlayer.Source).Items.Add(mediaPlaybackItems.ElementAt(randomIndex));
                            mediaPlaybackItems.RemoveAt(randomIndex);
                        }

                        // Set the new sound order
                        await FileManager.SetSoundsListOfPlayingSoundAsync(PlayingSound.Uuid, PlayingSound.Sounds);
                    }

                    ((MediaPlaybackList)PlayingSound.MediaPlayer.Source).MoveTo(0);
                    await FileManager.SetCurrentOfPlayingSoundAsync(PlayingSound.Uuid, 0);
                }

                PlayingSound.MediaPlayer.Play();
            });
        }

        private async void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
        {
            await dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                if (PlayingSound == null || PlayingSound.MediaPlayer == null) return;
                UpdatePlayPauseButton();
            });
        }

        private async void PlayingSoundItemTemplate_CurrentItemChanged(MediaPlaybackList sender, CurrentMediaPlaybackItemChangedEventArgs args)
        {
            await dispatcher.RunAsync(CoreDispatcherPriority.Low, async () =>
            {
                if (PlayingSound.Sounds.Count <= 1 || sender.CurrentItemIndex >= PlayingSound.Sounds.Count) return;

                int currentItemIndex = (int)sender.CurrentItemIndex;

                // Show the name of the new sound
                PlayingSoundNameTextBlock.Text = PlayingSound.Sounds.ElementAt(currentItemIndex).Name;

                // Update PlayingSound.Current
                PlayingSound.Current = currentItemIndex;
                await FileManager.SetCurrentOfPlayingSoundAsync(PlayingSound.Uuid, currentItemIndex);

                AdjustLayout();
            });
        }

        private void CommandManager_PreviousReceived(MediaPlaybackCommandManager sender, MediaPlaybackCommandManagerPreviousReceivedEventArgs args)
        {
            args.Handled = true;
            MoveToPrevious();
        }
        #endregion
    }
}
