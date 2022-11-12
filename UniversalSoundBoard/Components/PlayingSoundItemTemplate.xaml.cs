using davClassLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using UniversalSoundboard.Common;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Dialogs;
using UniversalSoundboard.Models;
using UniversalSoundboard.Pages;
using Windows.Devices.Enumeration;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;

namespace UniversalSoundboard.Components
{
    public sealed partial class PlayingSoundItemTemplate : UserControl
    {
        PlayingSoundItemContainer PlayingSoundItemContainer;
        PlayingSoundItem PlayingSoundItem;
        PlayingSound PlayingSound
        {
            get
            {
                if (PlayingSoundItemContainer == null) return null;
                return PlayingSoundItemContainer.PlayingSound;
            }
        }
        public List<Sound> Sounds
        {
            get => PlayingSound?.Sounds.ToList();
        }

        private bool initialized = false;
        PlayingSoundItemLayoutType layoutType = PlayingSoundItemLayoutType.Small;
        Guid selectedSoundUuid;
        Thickness singlePlayingSoundTitleMargin = new Thickness(0);
        private bool skipSoundsListViewSelectionChanged;
        private bool skipProgressSliderValueChanged = false;
        private bool isSoundsListVisible = false;

        private const string MoreButtonOutputDeviceFlyoutSubItemName = "MoreButtonOutputDeviceFlyoutSubItem";
        private const string MoreButtonPlaybackSpeedFlyoutSubItemName = "MoreButtonPlaybackSpeedFlyoutSubItemName";

        public event EventHandler<EventArgs> Expand;
        public event EventHandler<EventArgs> Collapse;

        public PlayingSoundItemTemplate()
        {
            InitializeComponent();

            ContentRoot.DataContext = FileManager.itemViewHolder;
            DataContextChanged += PlayingSoundTemplate_DataContextChanged;

            Opacity = 0;
            Translation = new Vector3(0, -300, 0);
        }
        
        private void Init()
        {
            if (
                PlayingSoundItemContainer == null
                || PlayingSound == null
                || PlayingSound.AudioPlayer == null
                || initialized
            ) return;

            // Hide this item if it was removed before initialization
            if (!PlayingSoundItemContainer.IsVisible)
            {
                Content = null;
                return;
            }

            initialized = true;

            // Check if the PlayingSound exists
            int j = FileManager.itemViewHolder.PlayingSounds.ToList().FindIndex(ps => ps.Uuid.Equals(PlayingSound.Uuid));
            if (j == -1) return;

            PlayingSoundItem = PlayingSoundItem.Subscribe(PlayingSound);

            PlayingSoundItem.PlaybackStateChanged -= PlayingSoundItem_PlaybackStateChanged;
            PlayingSoundItem.PlaybackStateChanged += PlayingSoundItem_PlaybackStateChanged;
            PlayingSoundItem.PositionChanged -= PlayingSoundItem_PositionChanged;
            PlayingSoundItem.PositionChanged += PlayingSoundItem_PositionChanged;
            PlayingSoundItem.DurationChanged -= PlayingSoundItem_DurationChanged;
            PlayingSoundItem.DurationChanged += PlayingSoundItem_DurationChanged;
            PlayingSoundItem.CurrentSoundChanged -= PlayingSoundItem_CurrentSoundChanged;
            PlayingSoundItem.CurrentSoundChanged += PlayingSoundItem_CurrentSoundChanged;
            PlayingSoundItem.ButtonVisibilityChanged -= PlayingSoundItem_ButtonVisibilityChanged;
            PlayingSoundItem.ButtonVisibilityChanged += PlayingSoundItem_ButtonVisibilityChanged;
            PlayingSoundItem.LocalFileButtonVisibilityChanged -= PlayingSoundItem_LocalFileButtonVisibilityChanged;
            PlayingSoundItem.LocalFileButtonVisibilityChanged += PlayingSoundItem_LocalFileButtonVisibilityChanged;
            PlayingSoundItem.OutputDeviceButtonVisibilityChanged -= PlayingSoundItem_OutputDeviceButtonVisibilityChanged;
            PlayingSoundItem.OutputDeviceButtonVisibilityChanged += PlayingSoundItem_OutputDeviceButtonVisibilityChanged;
            PlayingSoundItem.RepetitionsChanged -= PlayingSoundItem_RepetitionsChanged;
            PlayingSoundItem.RepetitionsChanged += PlayingSoundItem_RepetitionsChanged;
            PlayingSoundItem.FavouriteChanged -= PlayingSoundItem_FavouriteChanged;
            PlayingSoundItem.FavouriteChanged += PlayingSoundItem_FavouriteChanged;
            PlayingSoundItem.VolumeChanged -= PlayingSoundItem_VolumeChanged;
            PlayingSoundItem.VolumeChanged += PlayingSoundItem_VolumeChanged;
            PlayingSoundItem.MutedChanged -= PlayingSoundItem_MutedChanged;
            PlayingSoundItem.MutedChanged += PlayingSoundItem_MutedChanged;
            PlayingSoundItem.PlaybackSpeedChanged -= PlayingSoundItem_PlaybackSpeedChanged;
            PlayingSoundItem.PlaybackSpeedChanged += PlayingSoundItem_PlaybackSpeedChanged;
            PlayingSoundItem.RemovePlayingSound -= PlayingSoundItem_RemovePlayingSound;
            PlayingSoundItem.RemovePlayingSound += PlayingSoundItem_RemovePlayingSound;
            PlayingSoundItem.DownloadStatusChanged -= PlayingSoundItem_DownloadStatusChanged;
            PlayingSoundItem.DownloadStatusChanged += PlayingSoundItem_DownloadStatusChanged;

            FileManager.itemViewHolder.RemovePlayingSoundItem -= ItemViewHolder_RemovePlayingSoundItem;
            FileManager.itemViewHolder.RemovePlayingSoundItem += ItemViewHolder_RemovePlayingSoundItem;

            PlayingSoundItem.Init();

            SoundsListView.ItemsSource = PlayingSound.Sounds;
            UpdateUI();
        }

        #region UserControl event handlers
        private void PlayingSoundItemTemplateUserControl_Loaded(object sender, RoutedEventArgs eventArgs)
        {
            AdjustLayout();
            PlayingSoundItemContainer?.TriggerLoadedEvent(EventArgs.Empty);
        }

        private void PlayingSoundItemTemplateUserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            AdjustLayout();
            UpdateUI();
        }

        private void PlayingSoundTemplate_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (DataContext == null) return;

            var playingSoundItemContainer = DataContext as PlayingSoundItemContainer;

            if (playingSoundItemContainer == null || initialized) return;

            PlayingSoundItemContainer = playingSoundItemContainer;
            PlayingSoundItemContainer.PlayingSoundItemTemplate = this;

            Init();
        }
        #endregion

        #region PlayingSoundItem event handlers
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
        }

        private void PlayingSoundItem_LocalFileButtonVisibilityChanged(object sender, LocalFileButtonVisibilityEventArgs e)
        {
            LocalFileButton.Visibility = e.LocalFileButtonVisibility;
            MoreButtonFavouriteFlyoutItem.Visibility = e.LocalFileButtonVisibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }

        private void PlayingSoundItem_OutputDeviceButtonVisibilityChanged(object sender, OutputDeviceButtonVisibilityEventArgs e)
        {
            OutputDeviceButton.Visibility = e.OutputDeviceButtonVisibility;
        }

        private void PlayingSoundItem_RepetitionsChanged(object sender, RepetitionsChangedEventArgs e)
        {
            SetRepeatFlyoutItemText(e.Repetitions);
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

        private void PlayingSoundItem_PlaybackSpeedChanged(object sender, PlaybackSpeedChangedEventArgs e)
        {
            switch (e.PlaybackSpeed)
            {
                case 25:
                    PlaybackSpeedButton.Content = "0.25×";
                    PlaybackSpeedButton.FontSize = 11;
                    break;
                case 50:
                    PlaybackSpeedButton.Content = "0.5×";
                    PlaybackSpeedButton.FontSize = 12;
                    break;
                case 75:
                    PlaybackSpeedButton.Content = "0.75×";
                    PlaybackSpeedButton.FontSize = 11;
                    break;
                case 125:
                    PlaybackSpeedButton.Content = "1.25×";
                    PlaybackSpeedButton.FontSize = 11;
                    break;
                case 150:
                    PlaybackSpeedButton.Content = "1.5×";
                    PlaybackSpeedButton.FontSize = 12;
                    break;
                case 175:
                    PlaybackSpeedButton.Content = "1.75×";
                    PlaybackSpeedButton.FontSize = 11;
                    break;
                case 200:
                    PlaybackSpeedButton.Content = "2×";
                    PlaybackSpeedButton.FontSize = 12;
                    break;
                default:
                    PlaybackSpeedButton.Visibility = Visibility.Collapsed;
                    return;
            }

            string playbackSpeedText = FileManager.loader.GetString("PlaybackSpeedButtonToolTip").Replace("{0}", ((double)e.PlaybackSpeed / 100).ToString());
            PlaybackSpeedButtonToolTip.Text = playbackSpeedText;
            PlaybackSpeedFlyoutText.Text = playbackSpeedText;
            PlaybackSpeedButton.Visibility = Visibility.Visible;
        }

        private void PlayingSoundItem_RemovePlayingSound(object sender, EventArgs e)
        {
            PlayingSoundItemContainer.TriggerHideEvent(EventArgs.Empty);
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

        #region ItemViewHolder event handlers
        private void ItemViewHolder_RemovePlayingSoundItem(object sender, RemovePlayingSoundItemEventArgs args)
        {
            if (
                PlayingSoundItemContainer.IsInBottomPlayingSoundsBar
                && args.Uuid.Equals(PlayingSoundItem.Uuid)
                && Window.Current.Bounds.Width >= FileManager.mobileMaxWidth
            ) Content = null;
        }
        #endregion

        #region Event handlers
        private async void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            await PlayingSoundItem.TogglePlayPause();
        }

        private async void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            await PlayingSoundItem.MoveToPrevious();
        }

        private async void NextButton_Click(object sender, RoutedEventArgs e)
        {
            await PlayingSoundItem.MoveToNext();
        }

        private void TopButtonsStackPanel_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (
                layoutType == PlayingSoundItemLayoutType.SingleSoundSmall
                || layoutType == PlayingSoundItemLayoutType.SingleSoundLarge
            )
            {
                singlePlayingSoundTitleMargin.Left = TopButtonsStackPanel.ActualWidth + 8;
                singlePlayingSoundTitleMargin.Right = TopButtonsStackPanel.ActualWidth + 8;
            }
            else
            {
                singlePlayingSoundTitleMargin.Left = 0;
                singlePlayingSoundTitleMargin.Right = TopButtonsStackPanel.ActualWidth + 8;
            }

            Bindings.Update();
        }

        private async void LocalFileFlyoutAddButton_Click(object sender, RoutedEventArgs e)
        {
            LocalFileFlyout.Hide();

            // Add the sound to the soundboard
            await PlayingSoundItem.AddSoundToSoundboard();
        }

        private void PlaybackSpeedFlyoutResetButton_Click(object sender, RoutedEventArgs e)
        {
            PlayingSoundItem.SetPlaybackSpeed(100);
            PlaybackSpeedFlyout.Hide();
        }

        private async void OutputDeviceButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(PlayingSoundItem.PlayingSound.OutputDevice)) return;

            // Get the name of the current output device
            DeviceInformation deviceInfo = await FileManager.GetDeviceInformationById(PlayingSoundItem.PlayingSound.OutputDevice);
            if (deviceInfo == null || !deviceInfo.IsEnabled) return;

            OutputDeviceFlyoutDeviceName.Text = deviceInfo.Name;
        }

        private async void OutputDeviceFlyoutResetButton_Click(object sender, RoutedEventArgs e)
        {
            // Hide the flyout
            OutputDeviceFlyout.Hide();

            // Reset the output device
            await SetOutputDevice(null);
        }

        private void ExpandButton_Click(object sender, RoutedEventArgs e)
        {
            if (isSoundsListVisible)
            {
                isSoundsListVisible = false;

                if (PlayingSoundItemContainer.IsInBottomPlayingSoundsBar)
                    Collapse?.Invoke(this, EventArgs.Empty);
                else
                    PlayingSoundItemContainer.TriggerCollapseSoundsListEvent(new PlayingSoundSoundsListEventArgs(SoundsListViewStackPanel));

                // Set the icon for the expand button
                ExpandButton.Content = "\uE099";
                ExpandButtonToolTip.Text = FileManager.loader.GetString("ExpandButtonTooltip");
            }
            else
            {
                isSoundsListVisible = true;

                if (PlayingSoundItemContainer.IsInBottomPlayingSoundsBar)
                    Expand?.Invoke(this, EventArgs.Empty);
                else
                    PlayingSoundItemContainer.TriggerExpandSoundsListEvent(new PlayingSoundSoundsListEventArgs(SoundsListViewStackPanel));

                // Set the icon for the expand button
                ExpandButton.Content = "\uE098";
                ExpandButtonToolTip.Text = FileManager.loader.GetString("CollapseButtonTooltip");
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
            double value;
            if (layoutType == PlayingSoundItemLayoutType.Large)
                value = VolumeControl.Value;
            else if (layoutType == PlayingSoundItemLayoutType.SingleSoundLarge)
                value = VolumeControl2.Value;
            else
                value = MoreButtonVolumeFlyoutItem.VolumeControlValue;

            // Apply the new volume
            PlayingSound.AudioPlayer.Volume = value / 100 * FileManager.itemViewHolder.Volume / 100;
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

        private async void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            await PlayingSoundItem.TriggerRemove();
        }

        private void ProgressSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (skipProgressSliderValueChanged) return;

            double diff = e.NewValue - e.OldValue;
            if (diff > 0.6 || diff < -0.6)
                PlayingSoundItem.SetPosition(Convert.ToInt32(e.NewValue));
        }

        private async void MenuFlyout_Opening(object sender, object e)
        {
            // First insert the items
            int index = MoreButtonMenuFlyout.Items.ToList().FindIndex(item => item.Name == MoreButtonOutputDeviceFlyoutSubItemName);
            if (index != -1) MoreButtonMenuFlyout.Items.RemoveAt(index);

            MenuFlyoutSubItem outputDeviceFlyoutItem = new MenuFlyoutSubItem
            {
                Name = MoreButtonOutputDeviceFlyoutSubItemName,
                Text = FileManager.loader.GetString("MoreButton-OutputDevice"),
                Icon = new FontIcon
                {
                    FontFamily = new FontFamily(FileManager.FluentIconsFontFamily),
                    Glyph = "\uE7F5"
                }
            };

            MoreButtonMenuFlyout.Items.Insert(1, outputDeviceFlyoutItem);

            index = MoreButtonMenuFlyout.Items.ToList().FindIndex(item => item.Name == MoreButtonPlaybackSpeedFlyoutSubItemName);
            if (index != -1) MoreButtonMenuFlyout.Items.RemoveAt(index);

            MenuFlyoutSubItem playbackSpeedFlyoutItem = new MenuFlyoutSubItem
            {
                Name = MoreButtonPlaybackSpeedFlyoutSubItemName,
                Text = FileManager.loader.GetString("PlaybackSpeed"),
                Icon = new FontIcon
                {
                    FontFamily = new FontFamily(FileManager.FluentIconsFontFamily),
                    Glyph = "\uEC58"
                }
            };

            MoreButtonMenuFlyout.Items.Insert(2, playbackSpeedFlyoutItem);

            #region OutputDevice
            // Update the list of possible output devices
            DeviceInformationCollection devices = await DeviceInformation.FindAllAsync(DeviceClass.AudioRender);
            string selectedOutputDevice = PlayingSoundItem.PlayingSound.OutputDevice;

            ToggleMenuFlyoutItem standardItem = new ToggleMenuFlyoutItem
            {
                Text = FileManager.loader.GetString("StandardOutputDevice"),
                IsChecked = true
            };
            standardItem.Click += MoreButton_OutputDevice_Click;
            outputDeviceFlyoutItem.Items.Add(standardItem);

            foreach (DeviceInformation deviceInfo in devices)
            {
                ToggleMenuFlyoutItem deviceItem = new ToggleMenuFlyoutItem
                {
                    Text = deviceInfo.Name,
                    Tag = deviceInfo.Id,
                    IsChecked = selectedOutputDevice == deviceInfo.Id
                };
                deviceItem.Click += MoreButton_OutputDevice_Click;
                outputDeviceFlyoutItem.Items.Add(deviceItem);

                if (deviceItem.IsChecked)
                    standardItem.IsChecked = false;
            }
            #endregion

            #region Playback speed
            ToggleMenuFlyoutItem playbackSpeedItem_0_25 = new ToggleMenuFlyoutItem { Text = "0.25×", IsChecked = PlayingSound.PlaybackSpeed == 25 };
            playbackSpeedItem_0_25.Click += MoreButton_PlaybackSpeed_0_25_Click;
            playbackSpeedFlyoutItem.Items.Add(playbackSpeedItem_0_25);

            ToggleMenuFlyoutItem playbackSpeedItem_0_5 = new ToggleMenuFlyoutItem { Text = "0.5×", IsChecked = PlayingSound.PlaybackSpeed == 50 };
            playbackSpeedItem_0_5.Click += MoreButton_PlaybackSpeed_0_5x_Click;
            playbackSpeedFlyoutItem.Items.Add(playbackSpeedItem_0_5);

            ToggleMenuFlyoutItem playbackSpeedItem_0_75 = new ToggleMenuFlyoutItem { Text = "0.75×", IsChecked = PlayingSound.PlaybackSpeed == 75 };
            playbackSpeedItem_0_75.Click += MoreButton_PlaybackSpeed_0_75x_Click;
            playbackSpeedFlyoutItem.Items.Add(playbackSpeedItem_0_75);

            ToggleMenuFlyoutItem playbackSpeedItem_1_0 = new ToggleMenuFlyoutItem { Text = "1.0×", IsChecked = PlayingSound.PlaybackSpeed == 100 };
            playbackSpeedItem_1_0.Click += MoreButton_PlaybackSpeed_1_0x_Click;
            playbackSpeedFlyoutItem.Items.Add(playbackSpeedItem_1_0);

            ToggleMenuFlyoutItem playbackSpeedItem_1_25 = new ToggleMenuFlyoutItem { Text = "1.25×", IsChecked = PlayingSound.PlaybackSpeed == 125 };
            playbackSpeedItem_1_25.Click += MoreButton_PlaybackSpeed_1_25x_Click;
            playbackSpeedFlyoutItem.Items.Add(playbackSpeedItem_1_25);
            
            ToggleMenuFlyoutItem playbackSpeedItem_1_5 = new ToggleMenuFlyoutItem { Text = "1.5×", IsChecked = PlayingSound.PlaybackSpeed == 150 };
            playbackSpeedItem_1_5.Click += MoreButton_PlaybackSpeed_1_5x_Click;
            playbackSpeedFlyoutItem.Items.Add(playbackSpeedItem_1_5);

            ToggleMenuFlyoutItem playbackSpeedItem_1_75 = new ToggleMenuFlyoutItem { Text = "1.75×", IsChecked = PlayingSound.PlaybackSpeed == 175 };
            playbackSpeedItem_1_75.Click += MoreButton_PlaybackSpeed_1_75x_Click;
            playbackSpeedFlyoutItem.Items.Add(playbackSpeedItem_1_75);

            ToggleMenuFlyoutItem playbackSpeedItem_2_0 = new ToggleMenuFlyoutItem { Text = "2.0×", IsChecked = PlayingSound.PlaybackSpeed == 200 };
            playbackSpeedItem_2_0.Click += MoreButton_PlaybackSpeed_2_0x_Click;
            playbackSpeedFlyoutItem.Items.Add(playbackSpeedItem_2_0);
            #endregion
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

        private async void MoreButton_OutputDevice_Click(object sender, RoutedEventArgs e)
        {
            if (Dav.IsLoggedIn && Dav.User.Plan > 0)
            {
                await SetOutputDevice((string)(sender as ToggleMenuFlyoutItem).Tag);
            }
            else
            {
                // Show dialog which explains that this feature is only for Plus users
                var davPlusOutputDeviceDialog = new DavPlusOutputDeviceDialog();
                davPlusOutputDeviceDialog.PrimaryButtonClick += DavPlusOutputDeviceContentDialog_PrimaryButtonClick;
                await davPlusOutputDeviceDialog.ShowAsync();
            }
        }

        private void DavPlusOutputDeviceContentDialog_PrimaryButtonClick(Dialog sender, ContentDialogButtonClickEventArgs args)
        {
            // Navigate to the Account page
            FileManager.NavigateToAccountPage();
        }

        private async void MoreButton_Repeat_0x_Click(object sender, RoutedEventArgs e)
        {
            await PlayingSoundItem.SetRepetitions(0);
        }

        private async void MoreButton_Repeat_1x_Click(object sender, RoutedEventArgs e)
        {
            await PlayingSoundItem.SetRepetitions(1);
        }

        private async void MoreButton_Repeat_2x_Click(object sender, RoutedEventArgs e)
        {
            await PlayingSoundItem.SetRepetitions(2);
        }

        private async void MoreButton_Repeat_5x_Click(object sender, RoutedEventArgs e)
        {
            await PlayingSoundItem.SetRepetitions(5);
        }

        private async void MoreButton_Repeat_10x_Click(object sender, RoutedEventArgs e)
        {
            await PlayingSoundItem.SetRepetitions(10);
        }

        private async void MoreButton_Repeat_endless_Click(object sender, RoutedEventArgs e)
        {
            await PlayingSoundItem.SetRepetitions(int.MaxValue);
        }

        private void MoreButton_PlaybackSpeed_0_25_Click(object sender, RoutedEventArgs e)
        {
            PlayingSoundItem.SetPlaybackSpeed(25);
        }

        private void MoreButton_PlaybackSpeed_0_5x_Click(object sender, RoutedEventArgs e)
        {
            PlayingSoundItem.SetPlaybackSpeed(50);
        }

        private void MoreButton_PlaybackSpeed_0_75x_Click(object sender, RoutedEventArgs e)
        {
            PlayingSoundItem.SetPlaybackSpeed(75);
        }

        private void MoreButton_PlaybackSpeed_1_0x_Click(object sender, RoutedEventArgs e)
        {
            PlayingSoundItem.SetPlaybackSpeed(100);
        }

        private void MoreButton_PlaybackSpeed_1_25x_Click(object sender, RoutedEventArgs e)
        {
            PlayingSoundItem.SetPlaybackSpeed(125);
        }

        private void MoreButton_PlaybackSpeed_1_5x_Click(object sender, RoutedEventArgs e)
        {
            PlayingSoundItem.SetPlaybackSpeed(150);
        }

        private void MoreButton_PlaybackSpeed_1_75x_Click(object sender, RoutedEventArgs e)
        {
            PlayingSoundItem.SetPlaybackSpeed(175);
        }

        private void MoreButton_PlaybackSpeed_2_0x_Click(object sender, RoutedEventArgs e)
        {
            PlayingSoundItem.SetPlaybackSpeed(200);
        }

        private async void MoreButton_OpenSoundSeparatelyFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            if (PlayingSound == null || PlayingSound.AudioPlayer == null) return;
            if (PlayingSound.Current >= PlayingSound.Sounds.Count) return;

            // Get the current sound
            Sound sound = PlayingSound.Sounds.ElementAt(PlayingSound.Current);

            // Open the sound
            await SoundPage.PlaySoundAsync(
                sound,
                PlayingSound.AudioPlayer.IsPlaying,
                PlayingSound.Volume,
                PlayingSound.Muted,
                PlayingSound.PlaybackSpeed,
                PlayingSound.AudioPlayer.IsPlaying ? PlayingSound.AudioPlayer.Position : TimeSpan.Zero
            );

            // Pause this PlayingSound
            await PlayingSoundItem.SetPlayPause(false);
        }

        private async void MoreButton_FavouriteItem_Click(object sender, RoutedEventArgs e)
        {
            if (PlayingSound == null || PlayingSound.AudioPlayer == null) return;
            await PlayingSoundItem.ToggleFavourite();
        }

        private async void PlayingSoundItemSoundItemTemplate_Remove(object sender, EventArgs args)
        {
            PlayingSoundItemSoundItemTemplate itemTemplate = sender as PlayingSoundItemSoundItemTemplate;

            PlayingSoundItem.RemoveSound(itemTemplate.Sound.Uuid);

            if (PlayingSound.Sounds.Count == 0)
            {
                await Task.Delay(200);
                await PlayingSoundItem.TriggerRemove();
            }
        }

        private async void SoundsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (skipSoundsListViewSelectionChanged) return;
            await PlayingSoundItem.MoveToSound(SoundsListView.SelectedIndex);
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
            if (PlayingSoundItem == null) return;

            var totalDuration = PlayingSoundItem.CurrentSoundTotalDuration;

            // Set the total duration text
            if (totalDuration.Hours == 0)
                TotalTimeElement.Text = $"{totalDuration.Minutes:D2}:{totalDuration.Seconds:D2}";
            else
                TotalTimeElement.Text = $"{totalDuration.Hours:D2}:{totalDuration.Minutes:D2}:{totalDuration.Seconds:D2}";

            // Set the maximum of the slider
            ProgressSlider.Maximum = totalDuration.TotalSeconds;
        }

        private async Task SetOutputDevice(string outputDevice)
        {
            PlayingSoundItem.PlayingSound.OutputDevice = outputDevice;

            // Update the audio device of the PlayingSound
            await PlayingSoundItem.UpdateOutputDevice();

            // Save the selected device
            await FileManager.SetOutputDeviceOfPlayingSoundAsync(PlayingSoundItem.PlayingSound.Uuid, outputDevice == null ? "" : outputDevice);
        }
        #endregion

        #region UI methods
        private void AdjustLayout()
        {
            if (
                PlayingSound == null
                || PlayingSound.AudioPlayer == null
                || ContentRoot == null
            ) return;

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
            if (layoutType == PlayingSoundItemLayoutType.SingleSoundSmall || layoutType == PlayingSoundItemLayoutType.SingleSoundLarge)
                ContentRoot.Margin = new Thickness(0, 6, 0, 0);
            else
                ContentRoot.Margin = new Thickness(0, 6, 0, 6);

            // Update the ContentHeight property in PlayingSoundItemContainer
            if (PlayingSoundItemContainer.IsVisible)
                PlayingSoundItemContainer.ContentHeight = ActualHeight;
            else
                PlayingSoundItemContainer.ContentHeight = 0;
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

            // Set the Repeat flyout item text
            SetRepeatFlyoutItemText(PlayingSound.Repetitions);

            // Show or hide the OpenSoundSeparately flyout item
            MoreButtonOpenSoundSeparatelyFlyoutItem.Visibility = (FileManager.itemViewHolder.OpenMultipleSounds && PlayingSound.Sounds.Count > 1) ? Visibility.Visible : Visibility.Collapsed;

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
                PlayPauseButtonToolTip.Text = FileManager.loader.GetString("PauseButtonToolTip");
            }
            else
            {
                PlayPauseButton.Content = "\uE102";
                PlayPauseButtonToolTip.Text = FileManager.loader.GetString("PlayButtonToolTip");
            }
        }

        private void SetRepeatFlyoutItemText(int repetitions)
        {
            // Set the text of the repeat flyout item
            if (repetitions == 0)
                MoreButtonRepeatFlyoutSubItem.Text = FileManager.loader.GetString("MoreButton-Repeat");
            else if (repetitions == int.MaxValue)
                MoreButtonRepeatFlyoutSubItem.Text = string.Format(FileManager.loader.GetString("MoreButton-RepeatWithNumber"), "∞");
            else if (repetitions > 100)
                MoreButtonRepeatFlyoutSubItem.Text = string.Format(FileManager.loader.GetString("MoreButton-RepeatWithNumber"), ">100");
            else
                MoreButtonRepeatFlyoutSubItem.Text = string.Format(FileManager.loader.GetString("MoreButton-RepeatWithNumber"), repetitions);
        }

        private void SetFavouriteFlyoutItemText(bool fav)
        {
            MoreButtonFavouriteFlyoutItem.Text = FileManager.loader.GetString(fav ? "SoundItemOptionsFlyout-UnsetFavourite" : "SoundItemOptionsFlyout-SetFavourite");
            MoreButtonFavouriteFlyoutItem.Icon = new FontIcon { Glyph = fav ? "\uE195" : "\uE113" };
            MoreButtonFavouriteFlyoutItem.Visibility = PlayingSound.LocalFile ? Visibility.Collapsed : Visibility.Visible;
        }

        private void ShowIndetermindateProgressBar()
        {
            ProgressSlider.Visibility = Visibility.Collapsed;
            DownloadProgressBar.Visibility = Visibility.Visible;
            DownloadProgressBar.IsIndeterminate = true;
        }
        #endregion
    }
}
