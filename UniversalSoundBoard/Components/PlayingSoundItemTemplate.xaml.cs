using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

namespace UniversalSoundBoard.Components
{
    public sealed partial class PlayingSoundItemTemplate : UserControl
    {
        public PlayingSound PlayingSound { get; set; }
        private readonly ResourceLoader loader = new ResourceLoader();
        CoreDispatcher dispatcher;
        PlayingSoundItemLayoutType layoutType = PlayingSoundItemLayoutType.Small;
        private bool skipSoundsCollectionChanged = false;
        Guid selectedSoundUuid;

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
            UpdateUI();
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

            // Subscribe to MediaPlayer events
            PlayingSound.MediaPlayer.MediaEnded -= MediaPlayer_MediaEnded;
            PlayingSound.MediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
            PlayingSound.MediaPlayer.PlaybackSession.PlaybackStateChanged -= PlaybackSession_PlaybackStateChanged;
            PlayingSound.MediaPlayer.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;
            ((MediaPlaybackList)PlayingSound.MediaPlayer.Source).CurrentItemChanged -= PlayingSoundItemTemplate_CurrentItemChanged;
            ((MediaPlaybackList)PlayingSound.MediaPlayer.Source).CurrentItemChanged += PlayingSoundItemTemplate_CurrentItemChanged;
            PlayingSound.MediaPlayer.CommandManager.PreviousReceived -= CommandManager_PreviousReceived;
            PlayingSound.MediaPlayer.CommandManager.PreviousReceived += CommandManager_PreviousReceived;

            // Subscribe to other events
            FileManager.itemViewHolder.PropertyChanged += ItemViewHolder_PropertyChanged;
            PlayingSound.Sounds.CollectionChanged -= Sounds_CollectionChanged;
            PlayingSound.Sounds.CollectionChanged += Sounds_CollectionChanged;

            SoundsListView.ItemsSource = PlayingSound.Sounds;

            UpdateUI();
        }

        #region Button events
        private void ExpandButton_Click(object sender, RoutedEventArgs e)
        {
            if(SoundsListView.Visibility == Visibility.Collapsed)
            {
                SoundsListView.Visibility = Visibility.Visible;
                ExpandButton.Content = "\uE098";
            }
            else
            {
                SoundsListView.Visibility = Visibility.Collapsed;
                ExpandButton.Content = "\uE099";
            }
        }

        private void VolumeButton_Click(object sender, RoutedEventArgs e)
        {
            // Set the value of the volume slider
            VolumeControl.Value = PlayingSound.Volume;
            VolumeControl.Muted = PlayingSound.Muted;
        }

        private void VolumeControl_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            double value = layoutType == PlayingSoundItemLayoutType.Large ? VolumeControl.Value : MoreButtonVolumeFlyoutItem.VolumeControlValue;

            // Apply the new volume
            PlayingSound.MediaPlayer.Volume = value / 100 * FileManager.itemViewHolder.Volume / 100;
        }

        private void VolumeControl_IconChanged(object sender, string newIcon)
        {
            // Update the icon of the Volume button
            VolumeButton.Content = newIcon;
        }

        private async void VolumeControl_LostFocus(object sender, RoutedEventArgs e)
        {
            double value = layoutType == PlayingSoundItemLayoutType.Large ? VolumeControl.Value : MoreButtonVolumeFlyoutItem.VolumeControlValue;
            int volume = Convert.ToInt32(value);

            // Save new Volume
            PlayingSound.Volume = volume;
            await FileManager.SetVolumeOfPlayingSoundAsync(PlayingSound.Uuid, volume);
        }

        private async void VolumeControl_MuteChanged(object sender, bool muted)
        {
            PlayingSound.MediaPlayer.IsMuted = muted || FileManager.itemViewHolder.Muted;

            // Save new Muted
            PlayingSound.Muted = muted;
            await FileManager.SetMutedOfPlayingSoundAsync(PlayingSound.Uuid, muted);
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

        private void MenuFlyout_Opened(object sender, object e)
        {
            if (layoutType != PlayingSoundItemLayoutType.Large)
            {
                // Set the value of the VolumeMenuFlyoutItem
                MoreButtonVolumeFlyoutItem.VolumeControlValue = PlayingSound.Volume;
                MoreButtonVolumeFlyoutItem.VolumeControlMuted = PlayingSound.Muted;
            }
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

        private async void MoreButtonFavouriteItem_Click(object sender, RoutedEventArgs e)
        {
            if (PlayingSound == null || PlayingSound.MediaPlayer == null) return;

            Sound currentSound = PlayingSound.Sounds.ElementAt(PlayingSound.Current);
            currentSound.Favourite = !currentSound.Favourite;

            // Update the text of the MenuFlyoutItem
            SetFavouriteFlyoutItemText(currentSound.Favourite);

            // Save the new favourite and reload the sound
            await FileManager.SetSoundAsFavouriteAsync(currentSound.Uuid, currentSound.Favourite);
            await FileManager.ReloadSound(currentSound.Uuid);
        }

        private async void SoundsListViewRemoveSwipeItem_Invoked(SwipeItem sender, SwipeItemInvokedEventArgs args)
        {
            if (PlayingSound.Sounds.Count > 1)
                RemoveSound((Guid)args.SwipeControl.Tag);
            else
                await RemovePlayingSound();
        }
        
        private void SwipeControl_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            MenuFlyout flyout = new MenuFlyout();
            selectedSoundUuid = (Guid)(sender as SwipeControl).Tag;

            if(PlayingSound.Sounds.Count > 1)
            {
                MenuFlyoutItem removeFlyoutItem = new MenuFlyoutItem
                {
                    Text = loader.GetString("Remove"),
                    Icon = new FontIcon { Glyph = "\uE106" }
                };
                removeFlyoutItem.Click += RemoveFlyoutItem_Click;
                flyout.Items.Add(removeFlyoutItem);
            }

            if(flyout.Items.Count > 0)
                flyout.ShowAt(sender as UIElement, e.GetPosition(sender as UIElement));
        }

        private void RemoveFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            // Remove the selected sound
            RemoveSound(selectedSoundUuid);
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

        private async Task MoveToSound(int index)
        {
            if (
                PlayingSound.Sounds.Count <= 1
                || PlayingSound.Current == index
                || index >= PlayingSound.Sounds.Count
                || index < 0
            ) return;

            // Update PlayingSound.Current
            PlayingSound.Current = index;

            // Move to the selected sound
            if (((MediaPlaybackList)PlayingSound.MediaPlayer.Source).CurrentItemIndex != index)
                ((MediaPlaybackList)PlayingSound.MediaPlayer.Source).MoveTo(Convert.ToUInt32(index));

            // Update the visibility of the Next/Previous buttons and show the name of the new sound and 
            AdjustLayout();
            UpdateUI();

            // Save the new Current
            await FileManager.SetCurrentOfPlayingSoundAsync(PlayingSound.Uuid, index);
        }

        private void RemoveSound(Guid uuid)
        {
            int index = PlayingSound.Sounds.ToList().FindIndex(s => s.Uuid.Equals(uuid));
            if (index == -1) return;

            PlayingSound.Sounds.RemoveAt(index);
        }
        #endregion

        #region UI methods
        private void AdjustLayout()
        {
            if (PlayingSound == null || PlayingSound.MediaPlayer == null) return;

            // Set the visibility of the Previous and Next buttons
            PreviousButton.Visibility = PlayingSound.Current > 0 ? Visibility.Visible : Visibility.Collapsed;
            NextButton.Visibility = PlayingSound.Current != PlayingSound.Sounds.Count - 1 ? Visibility.Visible : Visibility.Collapsed;

            // Set the appropriate layout for the PlayingSoundItem
            double windowWidth = Window.Current.Bounds.Width;
            double itemWidth = ContentRoot.ActualWidth;

            if (windowWidth <= FileManager.mobileMaxWidth)
                layoutType = PlayingSoundItemLayoutType.Compact;
            else if (itemWidth <= 210)
                layoutType = PlayingSoundItemLayoutType.Mini;
            else if (itemWidth <= 300)
                layoutType = PlayingSoundItemLayoutType.Small;
            else
                layoutType = PlayingSoundItemLayoutType.Large;

            switch (layoutType)
            {
                case PlayingSoundItemLayoutType.Compact:
                    VisualStateManager.GoToState(this, "LayoutSizeCompact", false);
                    break;
                case PlayingSoundItemLayoutType.Mini:
                    VisualStateManager.GoToState(this, "LayoutSizeMini", false);
                    break;
                case PlayingSoundItemLayoutType.Small:
                    VisualStateManager.GoToState(this, "LayoutSizeSmall", false);
                    break;
                case PlayingSoundItemLayoutType.Large:
                    VisualStateManager.GoToState(this, "LayoutSizeLarge", false);
                    break;
            }

            // Set the visibility of the time texts in the TransportControls
            BasicMediaTransportControls.TimesVisible = layoutType != PlayingSoundItemLayoutType.Compact;
        }

        private void UpdateUI()
        {
            // Set the name of the current sound and set the favourite flyout item
            var currentSound = PlayingSound.Sounds.ElementAt(PlayingSound.Current);
            PlayingSoundNameTextBlock.Text = currentSound.Name;
            SetFavouriteFlyoutItemText(currentSound.Favourite);

            // Set the selected item of the sounds list
            SoundsListView.SelectedIndex = PlayingSound.Current;

            // Set the volume icon
            if (layoutType == PlayingSoundItemLayoutType.Large)
                VolumeButton.Content = UniversalSoundboard.Components.VolumeControl.GetVolumeIcon(PlayingSound.Volume, PlayingSound.Muted);
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

        private void SetFavouriteFlyoutItemText(bool fav)
        {
            MoreButtonFavouriteFlyoutItem.Text = loader.GetString(fav ? "SoundItemOptionsFlyout-UnsetFavourite" : "SoundItemOptionsFlyout-SetFavourite");
            MoreButtonFavouriteFlyoutItem.Icon = new FontIcon { Glyph = fav ? "\uE195" : "\uE113" };
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
                        skipSoundsCollectionChanged = true;
                        int soundsCount = PlayingSound.Sounds.Count;

                        // Copy the sounds
                        List<Sound> sounds = new List<Sound>();
                        foreach (var sound in PlayingSound.Sounds)
                            sounds.Add(sound);

                        // Copy the MediaPlaybackList
                        List<MediaPlaybackItem> mediaPlaybackItems = new List<MediaPlaybackItem>();
                        foreach (var item in ((MediaPlaybackList)PlayingSound.MediaPlayer.Source).Items)
                            mediaPlaybackItems.Add(item);

                        PlayingSound.Sounds.Clear();
                        ((MediaPlaybackList)PlayingSound.MediaPlayer.Source).Items.Clear();

                        // Add the sounds in random order
                        for (int i = 0; i < soundsCount; i++)
                        {
                            // Take a random sound from the sounds list and add it to the original sounds list
                            int randomIndex = random.Next(sounds.Count);

                            PlayingSound.Sounds.Add(sounds.ElementAt(randomIndex));
                            sounds.RemoveAt(randomIndex);

                            ((MediaPlaybackList)PlayingSound.MediaPlayer.Source).Items.Add(mediaPlaybackItems.ElementAt(randomIndex));
                            mediaPlaybackItems.RemoveAt(randomIndex);
                        }

                        // Set the new sound order
                        await FileManager.SetSoundsListOfPlayingSoundAsync(PlayingSound.Uuid, PlayingSound.Sounds.ToList());

                        skipSoundsCollectionChanged = false;
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
                await MoveToSound((int)sender.CurrentItemIndex);
            });
        }

        private void CommandManager_PreviousReceived(MediaPlaybackCommandManager sender, MediaPlaybackCommandManagerPreviousReceivedEventArgs args)
        {
            args.Handled = true;
            MoveToPrevious();
        }
        #endregion

        #region Other event handlers
        private void ItemViewHolder_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Volume")
                PlayingSound.MediaPlayer.Volume = (double)PlayingSound.Volume / 100 * FileManager.itemViewHolder.Volume / 100;
            else if (e.PropertyName == "Muted")
                PlayingSound.MediaPlayer.IsMuted = PlayingSound.Muted || FileManager.itemViewHolder.Muted;
        }

        private async void Sounds_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (PlayingSound.MediaPlayer == null || skipSoundsCollectionChanged) return;

            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                // Remove the item at the start position of the removed items
                ((MediaPlaybackList)PlayingSound.MediaPlayer.Source).Items.RemoveAt(e.OldStartingIndex);

                if (e.OldStartingIndex <= PlayingSound.Current && PlayingSound.Current > 0)
                {
                    PlayingSound.Current--;
                    await FileManager.SetCurrentOfPlayingSoundAsync(PlayingSound.Uuid, PlayingSound.Current);
                }

                // Update the PlayingSound
                await FileManager.SetSoundsListOfPlayingSoundAsync(PlayingSound.Uuid, PlayingSound.Sounds.ToList());
            }

            UpdateUI();
        }

        private async void SoundsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            await MoveToSound(SoundsListView.SelectedIndex);
        }
        #endregion
    }

    enum PlayingSoundItemLayoutType
    {
        Compact,
        Mini,
        Small,
        Large
    }
}
