using System;
using System.Linq;
using System.Threading.Tasks;
using UniversalSoundboard.Common;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Models;
using UniversalSoundboard.Pages;
using Windows.ApplicationModel.Resources;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

namespace UniversalSoundboard.Components
{
    public sealed partial class PlayingSoundItemTemplate : UserControl
    {
        private PlayingSound _playingSound;
        PlayingSound PlayingSound {
            get
            {
                if (PlayingSoundItem == null) return null;
                return PlayingSoundItem.PlayingSound;
            }
            set
            {
                _playingSound = value;
            }
        }
        PlayingSoundItem PlayingSoundItem;

        private readonly ResourceLoader loader = new ResourceLoader();
        PlayingSoundItemLayoutType layoutType = PlayingSoundItemLayoutType.Small;
        Guid selectedSoundUuid;
        private bool skipSoundsListViewSelectionChanged;
        private bool skipProgressSliderValueChanged = false;
        private bool inBottomPlayingSoundsBar = false;
        private bool playingSoundItemVisible = false;

        public PlayingSoundItemTemplate()
        {
            InitializeComponent();

            ContentRoot.DataContext = FileManager.itemViewHolder;
            DataContextChanged += PlayingSoundTemplate_DataContextChanged;
        }

        private void Init()
        {
            if (_playingSound == null || _playingSound.MediaPlayer == null) return;

            // Check if the PlayingSound still exists
            int j = FileManager.itemViewHolder.PlayingSounds.ToList().FindIndex(ps => ps.Uuid.Equals(_playingSound.Uuid));
            if (j == -1) return;

            inBottomPlayingSoundsBar = Tag != null;

            if(!inBottomPlayingSoundsBar)
                PlayingSoundItemTemplateUserControl.Height = double.NaN;

            // Subscribe to the appropriate PlayingSoundItem
            int i = FileManager.itemViewHolder.PlayingSoundItems.FindIndex(item => item.Uuid.Equals(_playingSound.Uuid));

            if (i == -1)
            {
                // Create a new PlayingSoundItem
                PlayingSoundItem = new PlayingSoundItem(_playingSound, CoreWindow.GetForCurrentThread().Dispatcher);
                FileManager.itemViewHolder.PlayingSoundItems.Add(PlayingSoundItem);
            }
            else
            {
                PlayingSoundItem = FileManager.itemViewHolder.PlayingSoundItems.ElementAt(i);
                if(PlayingSoundItem.CurrentSoundIsDownloading)
                    ShowIndetermindateProgressBar();
            }
            _playingSound = null;

            PlayingSoundItem.PlaybackStateChanged -= PlayingSoundItem_PlaybackStateChanged;
            PlayingSoundItem.PlaybackStateChanged += PlayingSoundItem_PlaybackStateChanged;
            PlayingSoundItem.PositionChanged -= PlayingSoundItem_PositionChanged;
            PlayingSoundItem.PositionChanged += PlayingSoundItem_PositionChanged;
            PlayingSoundItem.DurationChanged -= PlayingSoundItem_DurationChanged;
            PlayingSoundItem.DurationChanged += PlayingSoundItem_DurationChanged;
            PlayingSoundItem.ButtonVisibilityChanged -= PlayingSoundItem_ButtonVisibilityChanged;
            PlayingSoundItem.ButtonVisibilityChanged += PlayingSoundItem_ButtonVisibilityChanged;
            PlayingSoundItem.CurrentSoundChanged -= PlayingSoundItem_CurrentSoundChanged;
            PlayingSoundItem.CurrentSoundChanged += PlayingSoundItem_CurrentSoundChanged;
            PlayingSoundItem.ExpandButtonContentChanged -= PlayingSoundItem_ExpandButtonContentChanged;
            PlayingSoundItem.ExpandButtonContentChanged += PlayingSoundItem_ExpandButtonContentChanged;
            PlayingSoundItem.ShowSoundsList -= PlayingSoundItem_ShowSoundsList;
            PlayingSoundItem.ShowSoundsList += PlayingSoundItem_ShowSoundsList;
            PlayingSoundItem.HideSoundsList -= PlayingSoundItem_HideSoundsList;
            PlayingSoundItem.HideSoundsList += PlayingSoundItem_HideSoundsList;
            PlayingSoundItem.FavouriteChanged -= PlayingSoundItem_FavouriteChanged;
            PlayingSoundItem.FavouriteChanged += PlayingSoundItem_FavouriteChanged;
            PlayingSoundItem.VolumeChanged -= PlayingSoundItem_VolumeChanged;
            PlayingSoundItem.VolumeChanged += PlayingSoundItem_VolumeChanged;
            PlayingSoundItem.MutedChanged -= PlayingSoundItem_MutedChanged;
            PlayingSoundItem.MutedChanged += PlayingSoundItem_MutedChanged;
            PlayingSoundItem.ShowPlayingSound -= PlayingSoundItem_ShowPlayingSound;
            PlayingSoundItem.ShowPlayingSound += PlayingSoundItem_ShowPlayingSound;
            PlayingSoundItem.RemovePlayingSound -= PlayingSoundItem_RemovePlayingSound;
            PlayingSoundItem.RemovePlayingSound += PlayingSoundItem_RemovePlayingSound;
            PlayingSoundItem.DownloadStatusChanged -= PlayingSoundItem_DownloadStatusChanged;
            PlayingSoundItem.DownloadStatusChanged += PlayingSoundItem_DownloadStatusChanged;
            PlayingSoundItem.Init();

            SoundsListView.ItemsSource = PlayingSound.Sounds;
            UpdateUI();
        }

        #region UserControl event handlers
        private void PlayingSoundTemplate_Loaded(object sender, RoutedEventArgs eventArgs)
        {
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

            _playingSound = DataContext as PlayingSound;
            Init();
        }
        #endregion

        #region PlayingSoundItem events
        private void PlayingSoundItem_PlaybackStateChanged(object sender, PlaybackStateChangedEventArgs e)
        {
            UpdatePlayPauseButton(e.IsPlaying);
        }

        private void PlayingSoundItem_PositionChanged(object sender, PositionChangedEventArgs e)
        {
            if(e.Position.Hours == 0)
                RemainingTimeElement.Text = $"{e.Position.Minutes:D2}:{e.Position.Seconds:D2}";
            else
                RemainingTimeElement.Text = $"{e.Position.Hours:D2}:{e.Position.Minutes:D2}:{e.Position.Seconds:D2}";

            skipProgressSliderValueChanged = true;
            ProgressSlider.Value = e.Position.TotalSeconds;
            skipProgressSliderValueChanged = false;
        }

        private void PlayingSoundItem_DurationChanged(object sender, DurationChangedEventArgs e)
        {
            SetTotalDuration();
        }

        private void PlayingSoundItem_CurrentSoundChanged(object sender, CurrentSoundChangedEventArgs e)
        {
            UpdateUI();
        }

        private void PlayingSoundItem_ButtonVisibilityChanged(object sender, ButtonVisibilityChangedEventArgs e)
        {
            PreviousButton.Visibility = e.PreviousButtonVisibility;
            NextButton.Visibility = e.NextButtonVisibility;
            ExpandButton.Visibility = e.ExpandButtonVisibility;

            UpdatePlayPauseButtonMargin();
        }

        private void PlayingSoundItem_ExpandButtonContentChanged(object sender, ExpandButtonContentChangedEventArgs e)
        {
            UpdateExpandButtonUI();
        }

        private void PlayingSoundItem_ShowSoundsList(object sender, EventArgs e)
        {
            // Start the animation
            ShowSoundsListViewStoryboardAnimation.To = SoundsListView.ActualHeight;
            ShowSoundsListViewStoryboard.Begin();
        }

        private void PlayingSoundItem_HideSoundsList(object sender, EventArgs e)
        {
            // Start the animation
            HideSoundsListViewStoryboard.Begin();
        }

        private void PlayingSoundItem_FavouriteChanged(object sender, FavouriteChangedEventArgs e)
        {
            SetFavouriteFlyoutItemText(e.Favourite);
        }

        private void PlayingSoundItem_VolumeChanged(object sender, VolumeChangedEventArgs e)
        {
            VolumeControl2.Value = e.Volume;
        }

        private void PlayingSoundItem_MutedChanged(object sender, MutedChangedEventArgs e)
        {
            VolumeControl2.Muted = e.Muted;
        }

        private async void PlayingSoundItem_ShowPlayingSound(object sender, EventArgs e)
        {
            if (playingSoundItemVisible) return;
            playingSoundItemVisible = true;

            // Show the playing sound on the top of the extended list only if the PlayingSoundsBarPosition is top and the playing sounds are already loaded and visible / this is not a saved plaiyng sound
            bool addPlayingSoundOnTopOfBottomPlayingSoundsBar = (
                inBottomPlayingSoundsBar
                && (
                    SoundPage.bottomPlayingSoundsBarPosition == VerticalPosition.Top
                    || !SoundPage.playingSoundsRendered
                )
            );

            await Task.Delay(5);

            // (88 = standard height of PlayingSoundItem with one row of text)
            double contentHeight = ContentRoot.ActualHeight > 0 ? ContentRoot.ActualHeight + ContentRoot.Margin.Top + ContentRoot.Margin.Bottom : 88;

            if (addPlayingSoundOnTopOfBottomPlayingSoundsBar && FileManager.itemViewHolder.PlayingSounds.Count != 2)
            {
                // Start playing the animation for appearing PlayingSoundItem
                FileManager.itemViewHolder.TriggerShowPlayingSoundItemStartedEvent(
                    this,
                    new PlayingSoundItemEventArgs(
                        PlayingSound.Uuid,
                        contentHeight
                    )
                );
            }

            ShowPlayingSoundItemStoryboardAnimation.To = contentHeight;
            ShowPlayingSoundItemStoryboard.Begin();
        }

        private void PlayingSoundItem_RemovePlayingSound(object sender, EventArgs e)
        {
            // Start the animation for hiding the PlayingSoundItem
            HidePlayingSoundItemStoryboardAnimation.From = PlayingSoundItemTemplateUserControl.ActualHeight;
            HidePlayingSoundItemStoryboard.Begin();
        }

        private void PlayingSoundItem_DownloadStatusChanged(object sender, DownloadStatusChangedEventArgs e)
        {
            if (e.IsDownloading)
            {
                if (e.DownloadProgress < 0)
                {
                    // Show the indeterminate progress bar
                    ShowIndetermindateProgressBar();
                }
                else if (e.DownloadProgress > 100)
                {
                    // Show the progress slider
                    ProgressSlider.Visibility = Visibility.Visible;
                    DownloadProgressBar.Visibility = Visibility.Collapsed;
                }
                else
                {
                    // Show the progress bar with the current progress
                    ProgressSlider.Visibility = Visibility.Collapsed;
                    DownloadProgressBar.Visibility = Visibility.Visible;
                    DownloadProgressBar.IsIndeterminate = false;
                    DownloadProgressBar.Value = e.DownloadProgress;
                }
            }
            else
            {
                // Show the progress slider
                ProgressSlider.Visibility = Visibility.Visible;
                DownloadProgressBar.Visibility = Visibility.Collapsed;
            }
        }
        #endregion

        #region Event handlers
        private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            PlayingSoundItem.TogglePlayPause();
        }

        private async void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            await PlayingSoundItem.MoveToPrevious();
        }

        private async void NextButton_Click(object sender, RoutedEventArgs e)
        {
            await PlayingSoundItem.MoveToNext();
        }

        private void ExpandButton_Click(object sender, RoutedEventArgs e)
        {
            if (PlayingSoundItem.SoundsListVisible)
                PlayingSoundItem.CollapseSoundsList(ContentRoot.ActualHeight + ContentRoot.Margin.Top + ContentRoot.Margin.Bottom - SoundsListView.ActualHeight);
            else
                PlayingSoundItem.ExpandSoundsList(SoundsListView.ActualHeight);
        }

        private void VolumeButton_Click(object sender, RoutedEventArgs e)
        {
            // Set the value of the volume slider
            VolumeControl.Value = PlayingSound.Volume;
            VolumeControl.Muted = PlayingSound.Muted;
        }

        private void VolumeControl_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            double value;
            if (layoutType == PlayingSoundItemLayoutType.Large)
                value = VolumeControl.Value;
            else if (layoutType == PlayingSoundItemLayoutType.SingleSoundLarge)
                value = VolumeControl2.Value;
            else
                value = MoreButtonVolumeFlyoutItem.VolumeControlValue;

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
            double value;
            if (layoutType == PlayingSoundItemLayoutType.Large)
                value = VolumeControl.Value;
            else if (layoutType == PlayingSoundItemLayoutType.SingleSoundLarge)
                value = VolumeControl2.Value;
            else
                value = MoreButtonVolumeFlyoutItem.VolumeControlValue;

            int volume = Convert.ToInt32(value);
            await PlayingSoundItem.SetVolume(volume);
        }

        private async void VolumeControl_MuteChanged(object sender, bool muted)
        {
            await PlayingSoundItem.SetMuted(muted);
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            PlayingSoundItem.TriggerRemove();
        }

        private void ProgressSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (skipProgressSliderValueChanged) return;

            double diff = e.NewValue - e.OldValue;
            if (diff > 0.6 || diff < -0.6)
                PlayingSoundItem.SetPosition(Convert.ToInt32(e.NewValue));
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
            await PlayingSoundItem.SetRepetitions(2);
        }

        private async void MoreButton_Repeat_2x_Click(object sender, RoutedEventArgs e)
        {
            await PlayingSoundItem.SetRepetitions(3);
        }

        private async void MoreButton_Repeat_5x_Click(object sender, RoutedEventArgs e)
        {
            await PlayingSoundItem.SetRepetitions(6);
        }

        private async void MoreButton_Repeat_10x_Click(object sender, RoutedEventArgs e)
        {
            await PlayingSoundItem.SetRepetitions(11);
        }

        private async void MoreButton_Repeat_endless_Click(object sender, RoutedEventArgs e)
        {
            await PlayingSoundItem.SetRepetitions(int.MaxValue);
        }

        private async void MoreButtonFavouriteItem_Click(object sender, RoutedEventArgs e)
        {
            if (PlayingSound == null || PlayingSound.MediaPlayer == null) return;
            await PlayingSoundItem.ToggleFavourite();
        }

        private void SoundsListViewRemoveSwipeItem_Invoked(SwipeItem sender, SwipeItemInvokedEventArgs args)
        {
            if (PlayingSound.Sounds.Count > 1)
                PlayingSoundItem.RemoveSound((Guid)args.SwipeControl.Tag);
            else
                PlayingSoundItem.TriggerRemove();
        }

        private async void SoundsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (skipSoundsListViewSelectionChanged) return;
            await PlayingSoundItem.MoveToSound(SoundsListView.SelectedIndex);
        }

        private void SwipeControl_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            MenuFlyout flyout = new MenuFlyout();
            selectedSoundUuid = (Guid)(sender as SwipeControl).Tag;

            if (PlayingSound.Sounds.Count > 1)
            {
                MenuFlyoutItem removeFlyoutItem = new MenuFlyoutItem
                {
                    Text = loader.GetString("Remove"),
                    Icon = new FontIcon { Glyph = "\uE106" }
                };
                removeFlyoutItem.Click += RemoveFlyoutItem_Click;
                flyout.Items.Add(removeFlyoutItem);
            }

            if (flyout.Items.Count > 0)
                flyout.ShowAt(sender as UIElement, e.GetPosition(sender as UIElement));
        }

        private void RemoveFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            // Remove the selected sound
            PlayingSoundItem.RemoveSound(selectedSoundUuid);
        }
        #endregion

        #region Functionality
        private void SetTotalDuration()
        {
            var totalDuration = PlayingSoundItem.CurrentSoundTotalDuration;

            // Set the total duration text
            if (totalDuration.Hours == 0)
                TotalTimeElement.Text = $"{totalDuration.Minutes:D2}:{totalDuration.Seconds:D2}";
            else
                TotalTimeElement.Text = $"{totalDuration.Hours:D2}:{totalDuration.Minutes:D2}:{totalDuration.Seconds:D2}";

            // Set the maximum of the slider
            ProgressSlider.Maximum = totalDuration.TotalSeconds;
        }
        #endregion

        #region UI methods
        private void AdjustLayout()
        {
            if (PlayingSound == null || PlayingSound.MediaPlayer == null) return;

            // Set the appropriate layout for the PlayingSoundItem
            double windowWidth = Window.Current.Bounds.Width;
            double itemWidth = ContentRoot.ActualWidth;

            if (FileManager.itemViewHolder.PlayingSoundsListVisible && !FileManager.itemViewHolder.OpenMultipleSounds)
            {
                if (windowWidth <= 900)
                    layoutType = PlayingSoundItemLayoutType.SingleSoundSmall;
                else
                    layoutType = PlayingSoundItemLayoutType.SingleSoundLarge;
            }
            else if (windowWidth < FileManager.mobileMaxWidth)
                layoutType = PlayingSoundItemLayoutType.Compact;
            else if (itemWidth <= 210)
                layoutType = PlayingSoundItemLayoutType.Mini;
            else if (itemWidth <= 300)
                layoutType = PlayingSoundItemLayoutType.Small;
            else
                layoutType = PlayingSoundItemLayoutType.Large;

            switch (layoutType)
            {
                case PlayingSoundItemLayoutType.SingleSoundSmall:
                    VisualStateManager.GoToState(this, "LayoutSizeSingleSoundSmall", false);
                    break;
                case PlayingSoundItemLayoutType.SingleSoundLarge:
                    VisualStateManager.GoToState(this, "LayoutSizeSingleSoundLarge", false);
                    break;
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
            SetTimelineLayout(
                layoutType == PlayingSoundItemLayoutType.SingleSoundSmall || layoutType == PlayingSoundItemLayoutType.SingleSoundLarge,
                layoutType != PlayingSoundItemLayoutType.Compact
            );

            // Remove the bottom margin in Single sound mode
            if(layoutType == PlayingSoundItemLayoutType.SingleSoundSmall || layoutType == PlayingSoundItemLayoutType.SingleSoundLarge)
                ContentRoot.Margin = new Thickness(0, 6, 0, 0);
            else
                ContentRoot.Margin = new Thickness(0, 6, 0, 6);
        }

        private void UpdateUI()
        {
            if (
                PlayingSound == null
                || PlayingSound.Sounds.Count == 0
            ) return;

            // Correct PlayingSound.Current if necessary
            if (PlayingSound.Current < 0)
                PlayingSound.Current = 0;
            else if (PlayingSound.Current >= PlayingSound.Sounds.Count)
                PlayingSound.Current = PlayingSound.Sounds.Count - 1;

            // Set the name of the current sound and set the favourite flyout item
            var currentSound = PlayingSound.Sounds.ElementAt(PlayingSound.Current);
            PlayingSoundNameTextBlock.Text = currentSound.Name;
            SetFavouriteFlyoutItemText(currentSound.Favourite);

            // Set the selected item of the sounds list
            skipSoundsListViewSelectionChanged = true;
            SoundsListView.SelectedIndex = PlayingSound.Current;
            skipSoundsListViewSelectionChanged = false;

            // Set the volume icon
            if (layoutType == PlayingSoundItemLayoutType.Large)
                VolumeButton.Content = VolumeControl.GetVolumeIcon(PlayingSound.Volume, PlayingSound.Muted);

            // Set the total duration text
            SetTotalDuration();
        }

        private void SetTimelineLayout(bool compact, bool timesVisible)
        {
            if (compact)
            {
                // ProgressSlider
                RelativePanel.SetAlignLeftWithPanel(ProgressSlider, false);
                RelativePanel.SetAlignRightWithPanel(ProgressSlider, false);

                RelativePanel.SetRightOf(ProgressSlider, RemainingTimeElement);
                RelativePanel.SetLeftOf(ProgressSlider, TotalTimeElement);

                ProgressSlider.Margin = new Thickness(16, -7, 16, 0);
                ProgressSlider.Height = 37;

                // BufferingProgressBar
                RelativePanel.SetAlignLeftWithPanel(DownloadProgressBar, false);
                RelativePanel.SetAlignRightWithPanel(DownloadProgressBar, false);
                RelativePanel.SetAlignTopWithPanel(DownloadProgressBar, false);

                RelativePanel.SetRightOf(DownloadProgressBar, RemainingTimeElement);
                RelativePanel.SetLeftOf(DownloadProgressBar, TotalTimeElement);
                RelativePanel.SetAlignVerticalCenterWith(DownloadProgressBar, RemainingTimeElement);

                DownloadProgressBar.Margin = new Thickness(16, -7, 16, 0);

                // TimeElapsedElement
                RelativePanel.SetAlignBottomWith(RemainingTimeElement, null);
                RelativePanel.SetAlignLeftWithPanel(RemainingTimeElement, true);
                RelativePanel.SetAlignVerticalCenterWithPanel(RemainingTimeElement, true);

                RemainingTimeElement.Margin = new Thickness(1, 2, 0, 10);

                // TimeRemainingElement
                RelativePanel.SetAlignBottomWith(TotalTimeElement, null);
                RelativePanel.SetAlignRightWithPanel(TotalTimeElement, true);
                RelativePanel.SetAlignVerticalCenterWithPanel(TotalTimeElement, true);

                TotalTimeElement.Margin = new Thickness(0, 2, 1, 10);
            }
            else
            {
                // ProgressSlider
                RelativePanel.SetAlignLeftWithPanel(ProgressSlider, true);
                RelativePanel.SetAlignRightWithPanel(ProgressSlider, true);

                RelativePanel.SetRightOf(ProgressSlider, null);
                RelativePanel.SetLeftOf(ProgressSlider, null);

                ProgressSlider.Margin = new Thickness(0, timesVisible ? 0 : 1, 0, timesVisible ? 14 : 0);
                ProgressSlider.Height = 33;

                // BufferingProgressBar
                RelativePanel.SetAlignLeftWithPanel(DownloadProgressBar, true);
                RelativePanel.SetAlignRightWithPanel(DownloadProgressBar, true);
                RelativePanel.SetAlignTopWithPanel(DownloadProgressBar, true);

                RelativePanel.SetRightOf(DownloadProgressBar, null);
                RelativePanel.SetLeftOf(DownloadProgressBar, null);
                RelativePanel.SetAlignVerticalCenterWith(DownloadProgressBar, null);

                DownloadProgressBar.Margin = new Thickness(0, 18, 0, timesVisible ? 14 : 0);
                DownloadProgressBar.Padding = new Thickness(0);

                // TimeElapsedElement
                RelativePanel.SetAlignBottomWith(RemainingTimeElement, ProgressSlider);
                RelativePanel.SetAlignLeftWithPanel(RemainingTimeElement, true);
                RelativePanel.SetAlignVerticalCenterWithPanel(RemainingTimeElement, false);

                RemainingTimeElement.Margin = new Thickness(0);

                // TimeRemainingElement
                RelativePanel.SetAlignBottomWith(TotalTimeElement, ProgressSlider);
                RelativePanel.SetAlignRightWithPanel(TotalTimeElement, true);
                RelativePanel.SetAlignVerticalCenterWithPanel(TotalTimeElement, false);

                TotalTimeElement.Margin = new Thickness(0);
            }

            RemainingTimeElement.Visibility = timesVisible ? Visibility.Visible : Visibility.Collapsed;
            TotalTimeElement.Visibility = timesVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdatePlayPauseButton(bool isPlaying)
        {
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

        private void UpdatePlayPauseButtonMargin()
        {
            // If a single sound is allowed, move the PlayPauseButton to the right so that the buttons don't move when the Previous or Next button disappear
            if (
                layoutType == PlayingSoundItemLayoutType.SingleSoundSmall
                && PreviousButton.Visibility == Visibility.Collapsed
            ) PlayPauseButton.Margin = new Thickness(52, 0, 10, 0);
            else if (layoutType == PlayingSoundItemLayoutType.SingleSoundSmall)
                PlayPauseButton.Margin = new Thickness(10, 0, 10, 0);
            else
                PlayPauseButton.Margin = new Thickness(1, 0, 1, 0);
        }

        private void UpdateExpandButtonUI()
        {
            if (PlayingSoundItem.SoundsListVisible)
            {
                // Set the icon for the expand button
                ExpandButton.Content = "\uE098";
                ExpandButtonToolTip.Text = loader.GetString("CollapseButtonTooltip");

                // Show the sound list
                SoundsListViewStackPanel.Height = SoundsListView.ActualHeight;
            }
            else
            {
                // Set the icon for the expand button
                ExpandButton.Content = "\uE099";
                ExpandButtonToolTip.Text = loader.GetString("ExpandButtonTooltip");

                // Hide the sound list
                SoundsListViewStackPanel.Height = 0;
            }
        }

        private void SetFavouriteFlyoutItemText(bool fav)
        {
            MoreButtonFavouriteFlyoutItem.Text = loader.GetString(fav ? "SoundItemOptionsFlyout-UnsetFavourite" : "SoundItemOptionsFlyout-SetFavourite");
            MoreButtonFavouriteFlyoutItem.Icon = new FontIcon { Glyph = fav ? "\uE195" : "\uE113" };
        }

        private void ShowIndetermindateProgressBar()
        {
            ProgressSlider.Visibility = Visibility.Collapsed;
            DownloadProgressBar.Visibility = Visibility.Visible;
            DownloadProgressBar.IsIndeterminate = true;
        }
        #endregion

        #region Storyboard event handlers
        private void ShowSoundsListViewStoryboard_Completed(object sender, object e)
        {
            FileManager.itemViewHolder.TriggerPlayingSoundItemShowSoundsListAnimationEndedEvent(this, new PlayingSoundItemEventArgs(PlayingSound.Uuid));
        }

        private void HideSoundsListViewStoryboard_Completed(object sender, object e)
        {
            FileManager.itemViewHolder.TriggerPlayingSoundItemHideSoundsListAnimationEndedEvent(this, new PlayingSoundItemEventArgs(PlayingSound.Uuid));
        }

        private void ShowPlayingSoundItemStoryboard_Completed(object sender, object e)
        {
            PlayingSoundItemTemplateUserControl.Height = double.NaN;
            FileManager.itemViewHolder.TriggerShowPlayingSoundItemEndedEvent(this, new PlayingSoundItemEventArgs(PlayingSound.Uuid));
        }

        private async void HidePlayingSoundItemStoryboard_Completed(object sender, object e)
        {
            playingSoundItemVisible = false;
            await PlayingSoundItem.Remove();
        }
        #endregion
    }

    enum PlayingSoundItemLayoutType
    {
        SingleSoundSmall,
        SingleSoundLarge,
        Compact,
        Mini,
        Small,
        Large
    }
}
