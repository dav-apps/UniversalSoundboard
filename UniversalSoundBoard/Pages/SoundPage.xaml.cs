using Microsoft.Toolkit.Uwp.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using UniversalSoundboard.Common;
using UniversalSoundboard.Components;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Models;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Resources;
using Windows.Foundation.Collections;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using WinUI = Microsoft.UI.Xaml.Controls;

namespace UniversalSoundboard.Pages
{
    public sealed partial class SoundPage : Page
    {
        ResourceLoader loader = new ResourceLoader();
        public static bool soundsPivotSelected = true;
        private bool skipSoundListSelectionChangedEvent = false;
        Guid reorderedItem = Guid.Empty;
        public static VerticalPosition bottomPlayingSoundsBarPosition = VerticalPosition.Bottom;
        private static bool playingSoundsLoaded = false;
        public static bool playingSoundsRendered = false;
        private double maxBottomPlayingSoundsBarHeight = 500;
        private bool isManipulatingBottomPlayingSoundsBar = false;
        private bool snapBottomPlayingSoundsBarAnimationRunning = false;
        private static PlayingSound nextSinglePlayingSoundToOpen;
        private int getContainerHeightCount = 0;
        AdvancedCollectionView reversedPlayingSounds;
        Visibility startMessageVisibility = Visibility.Collapsed;
        Visibility emptyCategoryMessageVisibility = Visibility.Collapsed;
        WinUI.ProgressRing inAppNotificationProgressRing = new WinUI.ProgressRing();
        TextBlock inAppNotificationMessageTextBlock = new TextBlock();
        
        public SoundPage()
        {
            InitializeComponent();
            ContentRoot.DataContext = FileManager.itemViewHolder;

            // Initialize the reversedPlayingSounds list
            reversedPlayingSounds = new AdvancedCollectionView(FileManager.itemViewHolder.PlayingSounds);
            reversedPlayingSounds.SortDescriptions.Add(new SortDescription(SortDirection.Ascending, new ReverserClass()));

            reversedPlayingSounds.Filter = item =>
            {
                if (!playingSoundsLoaded) return false;
                if (FileManager.itemViewHolder.PlayingSounds.Count == 0) return true;
                return FileManager.itemViewHolder.OpenMultipleSounds || ((PlayingSound)item).Uuid.Equals(FileManager.itemViewHolder.PlayingSounds.Last().Uuid);
            };

            // Subscribe to events
            FileManager.itemViewHolder.PropertyChanged += ItemViewHolder_PropertyChanged;
            FileManager.itemViewHolder.PlayingSoundsLoaded += ItemViewHolder_PlayingSoundsLoaded;
            FileManager.itemViewHolder.SelectAllSounds += ItemViewHolder_SelectAllSounds;
            FileManager.itemViewHolder.PlayingSoundItemShowSoundsListAnimationStarted += ItemViewHolder_PlayingSoundItemShowSoundsListAnimationStarted;
            FileManager.itemViewHolder.PlayingSoundItemShowSoundsListAnimationEnded += ItemViewHolder_PlayingSoundItemShowSoundsListAnimationEnded;
            FileManager.itemViewHolder.PlayingSoundItemHideSoundsListAnimationStarted += ItemViewHolder_PlayingSoundItemHideSoundsListAnimationStarted;
            FileManager.itemViewHolder.PlayingSoundItemHideSoundsListAnimationEnded += ItemViewHolder_PlayingSoundItemHideSoundsListAnimationEnded;
            FileManager.itemViewHolder.ShowPlayingSoundItemStarted += ItemViewHolder_ShowPlayingSoundItemStarted;
            FileManager.itemViewHolder.ShowPlayingSoundItemEnded += ItemViewHolder_ShowPlayingSoundItemEnded;
            FileManager.itemViewHolder.RemovePlayingSoundItemStarted += ItemViewHolder_RemovePlayingSoundItemStarted;
            FileManager.itemViewHolder.RemovePlayingSoundItemEnded += ItemViewHolder_RemovePlayingSoundItemEnded;
            FileManager.itemViewHolder.ShowInAppNotification += ItemViewHolder_ShowInAppNotification;
            FileManager.itemViewHolder.SetInAppNotificationMessage += ItemViewHolder_SetInAppNotificationMessage;
            FileManager.itemViewHolder.SetInAppNotificationProgress += ItemViewHolder_SetInAppNotificationProgress;
            FileManager.itemViewHolder.DismissInAppNotification += ItemViewHolder_DismissInAppNotification;

            FileManager.itemViewHolder.Sounds.CollectionChanged += ItemViewHolder_Sounds_CollectionChanged;
            FileManager.itemViewHolder.FavouriteSounds.CollectionChanged += ItemViewHolder_FavouriteSounds_CollectionChanged;

            reversedPlayingSounds.VectorChanged += ReversedPlayingSounds_VectorChanged;

            // Check if there is a InAppNotification to display
            var lastArgs = FileManager.GetLastInAppNotificationEventArgs();
            if (lastArgs != null) ShowInAppNotification(lastArgs);

            // Enable or disable StartMessage buttons
            StartMessageAddFirstSoundButton.IsEnabled = !FileManager.itemViewHolder.Importing;
            StartMessageLoginButton.IsEnabled = !FileManager.itemViewHolder.Importing;
            StartMessageImportButton.IsEnabled = !FileManager.itemViewHolder.Importing;
        }

        #region Page event handlers
        async void SoundPage_Loaded(object sender, RoutedEventArgs e)
        {
            soundsPivotSelected = true;

            // Set the height of the BottomPlayingSoundsBar if the user is navigating from another page
            if(playingSoundsLoaded)
                await InitBottomPlayingSoundsBarHeight();

            await UpdatePlayingSoundsListAsync();
            await UpdateGridSplitterRange();
        }

        private async void SoundPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            maxBottomPlayingSoundsBarHeight = Window.Current.Bounds.Height * 0.6;
            await UpdatePlayingSoundsListAsync();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            FileManager.itemViewHolder.PropertyChanged -= ItemViewHolder_PropertyChanged;
            FileManager.itemViewHolder.PlayingSoundsLoaded -= ItemViewHolder_PlayingSoundsLoaded;
            FileManager.itemViewHolder.SelectAllSounds -= ItemViewHolder_SelectAllSounds;
            FileManager.itemViewHolder.PlayingSoundItemShowSoundsListAnimationStarted -= ItemViewHolder_PlayingSoundItemShowSoundsListAnimationStarted;
            FileManager.itemViewHolder.PlayingSoundItemShowSoundsListAnimationEnded -= ItemViewHolder_PlayingSoundItemShowSoundsListAnimationEnded;
            FileManager.itemViewHolder.PlayingSoundItemHideSoundsListAnimationStarted -= ItemViewHolder_PlayingSoundItemHideSoundsListAnimationStarted;
            FileManager.itemViewHolder.PlayingSoundItemHideSoundsListAnimationEnded -= ItemViewHolder_PlayingSoundItemHideSoundsListAnimationEnded;
            FileManager.itemViewHolder.ShowPlayingSoundItemStarted -= ItemViewHolder_ShowPlayingSoundItemStarted;
            FileManager.itemViewHolder.ShowPlayingSoundItemEnded -= ItemViewHolder_ShowPlayingSoundItemEnded;
            FileManager.itemViewHolder.RemovePlayingSoundItemStarted -= ItemViewHolder_RemovePlayingSoundItemStarted;
            FileManager.itemViewHolder.RemovePlayingSoundItemEnded -= ItemViewHolder_RemovePlayingSoundItemEnded;
            FileManager.itemViewHolder.ShowInAppNotification -= ItemViewHolder_ShowInAppNotification;
            FileManager.itemViewHolder.SetInAppNotificationMessage -= ItemViewHolder_SetInAppNotificationMessage;
            FileManager.itemViewHolder.SetInAppNotificationProgress -= ItemViewHolder_SetInAppNotificationProgress;
            FileManager.itemViewHolder.DismissInAppNotification -= ItemViewHolder_DismissInAppNotification;

            FileManager.itemViewHolder.Sounds.CollectionChanged -= ItemViewHolder_Sounds_CollectionChanged;
            FileManager.itemViewHolder.FavouriteSounds.CollectionChanged -= ItemViewHolder_FavouriteSounds_CollectionChanged;

            reversedPlayingSounds.VectorChanged -= ReversedPlayingSounds_VectorChanged;

            base.OnNavigatedFrom(e);
        }

        private void ItemViewHolder_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if(e.PropertyName.Equals(ItemViewHolder.CurrentThemeKey))
                RequestedTheme = FileManager.GetRequestedTheme();
            else if (e.PropertyName.Equals(ItemViewHolder.AppStateKey) || e.PropertyName.Equals(ItemViewHolder.SelectedCategoryKey))
                UpdateMessagesVisibilities();
        }

        private async void ItemViewHolder_PlayingSoundsLoaded(object sender, EventArgs e)
        {
            playingSoundsLoaded = true;

            // Load the reversed playing sounds list
            reversedPlayingSounds.RefreshFilter();

            await InitBottomPlayingSoundsBarHeight();
            playingSoundsRendered = true;
        }

        private void ItemViewHolder_SelectAllSounds(object sender, RoutedEventArgs e)
        {
            skipSoundListSelectionChangedEvent = true;

            if (FileManager.itemViewHolder.ShowListView)
            {
                // Get the visible ListView
                ListView listView = GetVisibleListView();
                FileManager.itemViewHolder.SelectedSounds.Clear();

                if(listView.SelectedItems.Count == listView.Items.Count)
                {
                    // All items are selected, deselect all items
                    listView.DeselectRange(new ItemIndexRange(0, (uint)listView.Items.Count));
                }
                else
                {
                    // Select all items
                    listView.SelectAll();

                    // Add all sounds to the selected sounds
                    foreach (var sound in listView.Items)
                        FileManager.itemViewHolder.SelectedSounds.Add(sound as Sound);
                }
            }
            else
            {
                // Get the visible GridView
                GridView gridView = GetVisibleGridView();
                FileManager.itemViewHolder.SelectedSounds.Clear();

                if (gridView.SelectedItems.Count == gridView.Items.Count)
                {
                    // All items are selected, deselect all items
                    gridView.DeselectRange(new ItemIndexRange(0, (uint)gridView.Items.Count));
                }
                else
                {
                    // Select all items
                    gridView.SelectAll();

                    // Add all sounds to the selected sounds
                    foreach (var sound in gridView.Items)
                        FileManager.itemViewHolder.SelectedSounds.Add(sound as Sound);
                }
            }
            
            skipSoundListSelectionChangedEvent = false;
            UpdateSelectAllFlyoutText();
        }

        private void ItemViewHolder_PlayingSoundItemShowSoundsListAnimationStarted(object sender, PlayingSoundItemEventArgs args)
        {
            // Update the max height for the GridSplitter with the new height
            double newMaxHeight = BottomPlayingSoundsBarListView.ActualHeight + args.HeightDifference;
            if (newMaxHeight >= maxBottomPlayingSoundsBarHeight) newMaxHeight = maxBottomPlayingSoundsBarHeight;

            GridSplitterGridBottomRowDef.MaxHeight = newMaxHeight;

            if (bottomPlayingSoundsBarPosition == VerticalPosition.Top)
            {
                // Update the BottomPlayingSoundsBar height and the bottom row def max height to the new height, so that the animation is able to play
                double newHeight = BottomPlayingSoundsBarListView.ActualHeight + args.HeightDifference;
                if (newHeight >= maxBottomPlayingSoundsBarHeight) newHeight = maxBottomPlayingSoundsBarHeight;

                BottomPlayingSoundsBar.Height = newHeight;
            }
            else
            {
                // Setup and start the animation for increasing the height of the BottomPlayingSoundsBar
                double newHeight = BottomPlayingSoundsBarListView.ActualHeight + args.HeightDifference;
                if (newHeight >= maxBottomPlayingSoundsBarHeight) newHeight = maxBottomPlayingSoundsBarHeight;

                ShowSoundsListViewStoryboardAnimation.From = BottomPlayingSoundsBarListView.ActualHeight;
                ShowSoundsListViewStoryboardAnimation.To = newHeight;
                ShowSoundsListViewStoryboard.Begin();
            }

            // Trigger the animation start
            FileManager.itemViewHolder.TriggerPlayingSoundItemStartSoundsListAnimationEvent(this);
        }

        private async void ItemViewHolder_PlayingSoundItemShowSoundsListAnimationEnded(object sender, PlayingSoundItemEventArgs args)
        {
            // Update the min and max height of the bottom row def
            await UpdateGridSplitterRange();
        }

        private void ItemViewHolder_PlayingSoundItemHideSoundsListAnimationStarted(object sender, PlayingSoundItemEventArgs args)
        {
            // Set the min height of the splitter to 0, so that the animation is able to play
            GridSplitterGridBottomRowDef.MinHeight = 0;

            if (bottomPlayingSoundsBarPosition == VerticalPosition.Bottom)
            {
                // Setup and start the animation for decreasing the height of the BottomPlayingSoundsBar
                HideSoundsListViewStoryboardAnimation.From = BottomPlayingSoundsBarListView.ActualHeight;
                HideSoundsListViewStoryboardAnimation.To = args.HeightDifference;
                HideSoundsListViewStoryboard.Begin();
            }

            // Trigger the animation start
            FileManager.itemViewHolder.TriggerPlayingSoundItemStartSoundsListAnimationEvent(this);
        }

        private async void ItemViewHolder_PlayingSoundItemHideSoundsListAnimationEnded(object sender, PlayingSoundItemEventArgs args)
        {
            // Update the min and max height of the bottom row def
            await UpdateGridSplitterRange();
        }

        private void ItemViewHolder_ShowPlayingSoundItemStarted(object sender, PlayingSoundItemEventArgs args)
        {
            // Update the animation with the actual PlayingSoundItem height
            AnimateIncreasingBottomPlayingSoundBar(args.HeightDifference);
        }

        private async void ItemViewHolder_ShowPlayingSoundItemEnded(object sender, PlayingSoundItemEventArgs e)
        {
            await UpdateGridSplitterRange();
            SnapBottomPlayingSoundsBar();
        }

        private void ItemViewHolder_RemovePlayingSoundItemStarted(object sender, PlayingSoundItemEventArgs args)
        {
            if (!FileManager.itemViewHolder.OpenMultipleSounds || FileManager.itemViewHolder.PlayingSounds.Count == 1)
            {
                // Start the animation for hiding the BottomPlayingSoundsBar background / BottomPseudoContentGrid
                GridSplitterGridBottomRowDef.MinHeight = 0;
                StartSnapBottomPlayingSoundsBarAnimation(GridSplitterGridBottomRowDef.ActualHeight, 0);
            }
        }

        private async void ItemViewHolder_RemovePlayingSoundItemEnded(object sender, PlayingSoundItemEventArgs e)
        {
            if (nextSinglePlayingSoundToOpen != null)
            {
                FileManager.itemViewHolder.PlayingSounds.Add(nextSinglePlayingSoundToOpen);
                nextSinglePlayingSoundToOpen = null;
            }
            else if(FileManager.itemViewHolder.OpenMultipleSounds)
            {
                // Snap the BottomPlayingSoundsBar; for the case that the height of the removed PlayingSound was different
                await UpdateGridSplitterRange();
                SnapBottomPlayingSoundsBar();
            }
        }

        private void ItemViewHolder_ShowInAppNotification(object sender, ShowInAppNotificationEventArgs args)
        {
            ShowInAppNotification(args);
        }

        private void ItemViewHolder_SetInAppNotificationMessage(object sender, SetInAppNotificationMessageEventArgs e)
        {
            inAppNotificationMessageTextBlock.Text = e.Message;
        }

        private void ItemViewHolder_SetInAppNotificationProgress(object sender, SetInAppNotificationProgressEventArgs e)
        {
            if (e.IsIndeterminate)
                inAppNotificationProgressRing.IsIndeterminate = true;
            else
            {
                inAppNotificationProgressRing.IsIndeterminate = false;

                int progress = e.Progress;
                if (progress > 100) progress = 100;
                else if (progress < 0) progress = 0;

                inAppNotificationProgressRing.Value = progress;
            }
        }

        private void ItemViewHolder_DismissInAppNotification(object sender, EventArgs e)
        {
            InAppNotification.Dismiss();
        }

        private async void ItemViewHolder_Sounds_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            await HandleSoundsCollectionChanged(e, false);
            UpdateMessagesVisibilities();
        }

        private async void ItemViewHolder_FavouriteSounds_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            await HandleSoundsCollectionChanged(e, true);
        }

        private async void ReversedPlayingSounds_VectorChanged(IObservableVector<object> sender, IVectorChangedEventArgs args)
        {
            await Task.Delay(5);
            await UpdatePlayingSoundsListAsync();
            await UpdateGridSplitterRange();
        }
        #endregion

        #region Helper methods
        private GridView GetVisibleGridView()
        {
            if (!FileManager.itemViewHolder.ShowSoundsPivot)
                return SoundGridView2;
            else if (SoundGridViewPivot.SelectedIndex == 1)
                return FavouriteSoundGridView;
            else
                return SoundGridView;
        }

        private ListView GetVisibleListView()
        {
            if (!FileManager.itemViewHolder.ShowSoundsPivot)
                return SoundListView2;
            else if (SoundListViewPivot.SelectedIndex == 1)
                return FavouriteSoundListView;
            else
                return SoundListView;
        }

        private void UpdateSelectAllFlyoutText()
        {
            int itemsCount = FileManager.itemViewHolder.ShowListView ? GetVisibleListView().Items.Count : GetVisibleGridView().Items.Count;

            if (itemsCount == FileManager.itemViewHolder.SelectedSounds.Count && itemsCount != 0)
            {
                FileManager.itemViewHolder.SelectAllFlyoutText = loader.GetString("MoreButton_SelectAllFlyout-DeselectAll");
                FileManager.itemViewHolder.SelectAllFlyoutIcon = new SymbolIcon(Symbol.ClearSelection);
            }
            else
            {
                FileManager.itemViewHolder.SelectAllFlyoutText = loader.GetString("MoreButton_SelectAllFlyout-SelectAll");
                FileManager.itemViewHolder.SelectAllFlyoutIcon = new SymbolIcon(Symbol.SelectAll);
            }
        }

        private async Task UpdatePlayingSoundsListAsync()
        {
            if (FileManager.itemViewHolder.PlayingSoundsListVisible)
            {
                // Set the max width of the sounds list and playing sounds list columns
                PlayingSoundsBarColDef.MaxWidth = ContentRoot.ActualWidth / 2;
                
                if (!FileManager.itemViewHolder.OpenMultipleSounds || Window.Current.Bounds.Width < FileManager.mobileMaxWidth)
                {
                    // Hide the PlayingSoundsBar and the GridSplitter
                    PlayingSoundsBarColDef.Width = new GridLength(0);
                    PlayingSoundsBarColDef.MinWidth = 0;
                    GridSplitterColDef.Width = new GridLength(0);

                    // Update the visibility of the BottomPlayingSoundsBar
                    if (reversedPlayingSounds.Count == 0)
                    {
                        BottomPlayingSoundsBar.Visibility = Visibility.Collapsed;
                        GridSplitterGrid.Visibility = Visibility.Collapsed;
                    }
                    else if (reversedPlayingSounds.Count == 1)
                    {
                        BottomPlayingSoundsBar.Visibility = Visibility.Visible;
                        GridSplitterGrid.Visibility = Visibility.Visible;

                        // Set the height of the bottom row def, but hide the GridSplitter
                        BottomPlayingSoundsBarGridSplitter.Visibility = Visibility.Collapsed;
                        GridSplitterGridBottomRowDef.Height = new GridLength(await GetPlayingSoundItemContainerHeight(0));
                    }
                    else
                    {
                        BottomPlayingSoundsBar.Visibility = Visibility.Visible;
                        GridSplitterGrid.Visibility = Visibility.Visible;
                        BottomPlayingSoundsBarGridSplitter.Visibility = FileManager.itemViewHolder.OpenMultipleSounds ? Visibility.Visible : Visibility.Collapsed;
                    }
                }
                else
                {
                    // Show the PlayingSoundsBar and the GridSplitter
                    PlayingSoundsBarColDef.Width = new GridLength(ContentRoot.ActualWidth * FileManager.itemViewHolder.PlayingSoundsBarWidth);
                    PlayingSoundsBarColDef.MinWidth = ContentRoot.ActualWidth / 3.8;
                    GridSplitterColDef.Width = new GridLength(12);

                    // Hide the BottomPlayingSoundsBar
                    BottomPlayingSoundsBar.Visibility = Visibility.Collapsed;
                    GridSplitterGrid.Visibility = Visibility.Collapsed;
                }

                AdaptSoundListScrollViewerForBottomPlayingSoundsBar();
            }
            else
            {
                // Hide the PlayingSoundsBar and the GridSplitter
                PlayingSoundsBarColDef.Width = new GridLength(0);
                PlayingSoundsBarColDef.MinWidth = 0;
                GridSplitterColDef.Width = new GridLength(0);

                // Hide the BottomPlayingSoundsBar
                BottomPlayingSoundsBar.Visibility = Visibility.Collapsed;
                GridSplitterGrid.Visibility = Visibility.Collapsed;
                AdaptSoundListScrollViewerForBottomPlayingSoundsBar();
            }
        }
        #endregion

        #region Functionality
        private async Task HandleSoundsCollectionChanged(NotifyCollectionChangedEventArgs e, bool favourites)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Remove:
                    reorderedItem = (e.OldItems[0] as Sound).Uuid;
                    break;
                case NotifyCollectionChangedAction.Add:
                    Sound addedSound = e.NewItems[0] as Sound;
                    bool updateOrder = reorderedItem.Equals(addedSound.Uuid);
                    reorderedItem = Guid.Empty;

                    // The user reordered the sounds as the item was first removed and now added again
                    if (updateOrder) await UpdateSoundOrder(favourites);
                    break;
            }
        }

        private async Task UpdateSoundOrder(bool showFavourites)
        {
            // Get the current category uuid
            Guid currentCategoryUuid = FileManager.itemViewHolder.SelectedCategory;

            // Get the uuids of the sounds
            List<Guid> uuids = new List<Guid>();
            foreach (var sound in showFavourites ? FileManager.itemViewHolder.FavouriteSounds : FileManager.itemViewHolder.Sounds)
                uuids.Add(sound.Uuid);

            await DatabaseOperations.SetSoundOrderAsync(currentCategoryUuid, showFavourites, uuids);
            FileManager.UpdateCustomSoundOrder(currentCategoryUuid, showFavourites, uuids);
        }

        public static async Task PlaySoundAsync(Sound sound, bool startPlaying = true, int? volume = null, bool? muted = null, int? playbackSpeed = null, TimeSpan? position = null)
        {
            List<Sound> soundList = new List<Sound> { sound };

            var createMediaPlayerResult = FileManager.CreateMediaPlayer(soundList, 0);
            MediaPlayer player = createMediaPlayerResult.Item1;
            List<Sound> newSounds = createMediaPlayerResult.Item2;
            if (player == null || newSounds == null) return;
            if (position.HasValue) player.PlaybackSession.Position = position.Value;

            int v = volume.HasValue ? volume.Value : sound.DefaultVolume;
            bool m = muted.HasValue ? muted.Value : sound.DefaultMuted;
            int ps = playbackSpeed.HasValue ? playbackSpeed.Value : sound.DefaultPlaybackSpeed;

            double appVolume = ((double)FileManager.itemViewHolder.Volume) / 100;
            player.Volume = appVolume * ((double)v / 100);
            player.IsMuted = m;
            player.PlaybackSession.PlaybackRate = (double)ps / 100;

            PlayingSound playingSound = new PlayingSound(player, sound)
            {
                Uuid = await FileManager.CreatePlayingSoundAsync(null, newSounds, 0, 0, false, v, m),
                Volume = v,
                Muted = m,
                PlaybackSpeed = ps,
                StartPlaying = startPlaying,
                StartPosition = position
            };

            if (FileManager.itemViewHolder.OpenMultipleSounds || FileManager.itemViewHolder.PlayingSoundItems.Count == 0)
            {
                FileManager.itemViewHolder.PlayingSounds.Add(playingSound);
            }
            else
            {
                nextSinglePlayingSoundToOpen = playingSound;

                foreach (PlayingSoundItem playingSoundItem in FileManager.itemViewHolder.PlayingSoundItems)
                    playingSoundItem.TriggerRemove();
            }
        }

        public static async Task PlaySoundsAsync(List<Sound> sounds, int repetitions, bool randomly)
        {
            // If randomly is true, shuffle sounds
            if (randomly)
            {
                Random random = new Random();
                sounds = sounds.OrderBy(a => random.Next()).ToList();
            }

            var createMediaPlayerResult = FileManager.CreateMediaPlayer(sounds, 0);
            MediaPlayer player = createMediaPlayerResult.Item1;
            List<Sound> newSounds = createMediaPlayerResult.Item2;
            if (player == null || newSounds == null) return;

            PlayingSound playingSound = new PlayingSound(
                await FileManager.CreatePlayingSoundAsync(null, newSounds, 0, repetitions, randomly, null, null),
                player,
                newSounds,
                0,
                repetitions,
                randomly
            );
            playingSound.StartPlaying = true;

            if (FileManager.itemViewHolder.OpenMultipleSounds || FileManager.itemViewHolder.PlayingSoundItems.Count == 0)
            {
                FileManager.itemViewHolder.PlayingSounds.Add(playingSound);
            }
            else
            {
                nextSinglePlayingSoundToOpen = playingSound;

                foreach (PlayingSoundItem playingSoundItem in FileManager.itemViewHolder.PlayingSoundItems)
                    playingSoundItem.TriggerRemove();
            }
        }

        public static void PlayLocalSound(StorageFile file)
        {
            Sound sound = new Sound(Guid.NewGuid(), file.DisplayName)
            {
                AudioFile = file
            };

            MediaPlayer player = FileManager.CreateMediaPlayerForLocalSound(sound);
            if (player == null) return;

            PlayingSound playingSound = new PlayingSound(Guid.NewGuid(), player, new List<Sound> { sound }, 0, 0, false);
            playingSound.LocalFile = true;
            playingSound.StartPlaying = true;

            if (FileManager.itemViewHolder.OpenMultipleSounds || FileManager.itemViewHolder.PlayingSoundItems.Count == 0)
            {
                FileManager.itemViewHolder.PlayingSounds.Add(playingSound);
            }
            else
            {
                nextSinglePlayingSoundToOpen = playingSound;

                foreach (PlayingSoundItem playingSoundItem in FileManager.itemViewHolder.PlayingSoundItems)
                    playingSoundItem.TriggerRemove();
            }
        }

        public static async Task PlaySoundAfterPlayingSoundsLoadedAsync(Sound sound)
        {
            if (!playingSoundsLoaded)
            {
                await Task.Delay(10);
                await PlaySoundAfterPlayingSoundsLoadedAsync(sound);
                return;
            }

            await PlaySoundAsync(sound);
        }

        public static async Task PlayLocalSoundAfterPlayingSoundsLoadedAsync(StorageFile file)
        {
            if (!playingSoundsLoaded)
            {
                await Task.Delay(10);
                await PlayLocalSoundAfterPlayingSoundsLoadedAsync(file);
                return;
            }

            PlayLocalSound(file);
        }

        private void ShowInAppNotification(ShowInAppNotificationEventArgs args)
        {
            InAppNotification.ShowDismissButton = args.Dismissable;

            Grid rootGrid = new Grid();
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            rootGrid.ColumnDefinitions.Add(new ColumnDefinition());
            rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            inAppNotificationProgressRing = new WinUI.ProgressRing
            {
                Width = 20,
                Height = 20,
                Margin = new Thickness(0, 0, 10, 0),
                IsActive = true
            };

            inAppNotificationMessageTextBlock = new TextBlock
            {
                Text = args.Message,
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.WrapWholeWords
            };
            Grid.SetColumn(inAppNotificationMessageTextBlock, 1);

            StackPanel buttonStackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(buttonStackPanel, 2);

            if (args.PrimaryButtonText != null)
            {
                Button primaryButton = new Button
                {
                    Content = args.PrimaryButtonText,
                    Height = 32
                };
                primaryButton.Click += (object s, RoutedEventArgs e) => args.TriggerPrimaryButtonClickEvent(s, e);

                buttonStackPanel.Children.Add(primaryButton);
            }

            if (args.SecondaryButtonText != null)
            {
                Button secondaryButton = new Button
                {
                    Content = args.SecondaryButtonText,
                    Height = 32,
                    Margin = new Thickness(10, 0, 0, 0)
                };
                secondaryButton.Click += (object s, RoutedEventArgs e) => args.TriggerSecondaryButtonClickEvent(s, e);

                buttonStackPanel.Children.Add(secondaryButton);
            }

            if (args.ShowProgressRing) rootGrid.Children.Add(inAppNotificationProgressRing);
            rootGrid.Children.Add(inAppNotificationMessageTextBlock);
            rootGrid.Children.Add(buttonStackPanel);

            InAppNotification.Show(rootGrid, args.Duration);
        }
        #endregion

        #region UI
        private async Task InitBottomPlayingSoundsBarHeight()
        {
            // Update the min and max height of the bottom row def
            await UpdateGridSplitterRange();

            if (BottomPlayingSoundsBarListView.Items.Count > 0)
            {
                // Get the height of the first PlayingSound item and set the height of the BottomPlayingSoundsBar
                double firstItemHeight = await GetPlayingSoundItemContainerHeight(0);
                BottomPlayingSoundsBar.Height = firstItemHeight;
                GridSplitterGridBottomRowDef.MinHeight = firstItemHeight;
                GridSplitterGridBottomRowDef.Height = new GridLength(firstItemHeight);
            }
        }

        private void SnapBottomPlayingSoundsBar()
        {
            double currentPosition = BottomPlayingSoundsBar.ActualHeight - GridSplitterGridBottomRowDef.MinHeight;
            double maxPosition = GridSplitterGridBottomRowDef.MaxHeight - GridSplitterGridBottomRowDef.MinHeight;

            if (currentPosition < maxPosition / 2)
            {
                StartSnapBottomPlayingSoundsBarAnimation(BottomPlayingSoundsBar.ActualHeight, GridSplitterGridBottomRowDef.MinHeight);
                bottomPlayingSoundsBarPosition = VerticalPosition.Bottom;
            }
            else
            {
                StartSnapBottomPlayingSoundsBarAnimation(BottomPlayingSoundsBar.ActualHeight, GridSplitterGridBottomRowDef.MaxHeight);
                bottomPlayingSoundsBarPosition = VerticalPosition.Top;
            }
        }

        private void StartSnapBottomPlayingSoundsBarAnimation(double start, double end)
        {
            if (end >= maxBottomPlayingSoundsBarHeight) end = maxBottomPlayingSoundsBarHeight;
            
            if (snapBottomPlayingSoundsBarAnimationRunning)
            {
                SnapBottomPlayingSoundsBarStoryboardAnimation.To = end;
            }
            else
            {
                SnapBottomPlayingSoundsBarStoryboardAnimation.From = start;
                SnapBottomPlayingSoundsBarStoryboardAnimation.To = end;
                SnapBottomPlayingSoundsBarStoryboard.Begin();
                snapBottomPlayingSoundsBarAnimationRunning = true;
            }
        }

        private void AnimateIncreasingBottomPlayingSoundBar(double addedHeight)
        {
            GridSplitterGridBottomRowDef.MaxHeight = BottomPlayingSoundsBar.ActualHeight + addedHeight;
            StartSnapBottomPlayingSoundsBarAnimation(BottomPlayingSoundsBar.ActualHeight, BottomPlayingSoundsBar.ActualHeight + addedHeight);
        }

        private void AdaptSoundListScrollViewerForBottomPlayingSoundsBar()
        {
            double bottomPlayingSoundsBarHeight = 0;

            if(BottomPlayingSoundsBar.Visibility == Visibility.Visible)
                bottomPlayingSoundsBarHeight = GridSplitterGridBottomRowDef.ActualHeight + (FileManager.itemViewHolder.PlayingSounds.Count == 1 ? 0 : 16);

            // Set the padding of the sound GridViews and ListViews, so that the ScrollViewer ends at the bottom bar and the list continues behind the bottom bar
            SoundGridView.Padding = new Thickness(
                SoundGridView.Padding.Left,
                SoundGridView.Padding.Top,
                SoundGridView.Padding.Right,
                bottomPlayingSoundsBarHeight
            );
            FavouriteSoundGridView.Padding = new Thickness(
                FavouriteSoundGridView.Padding.Left,
                FavouriteSoundGridView.Padding.Top,
                FavouriteSoundGridView.Padding.Right,
                bottomPlayingSoundsBarHeight
            );
            SoundListView.Padding = new Thickness(
                SoundListView.Padding.Left,
                SoundListView.Padding.Top,
                SoundListView.Padding.Right,
                bottomPlayingSoundsBarHeight
            );
            FavouriteSoundListView.Padding = new Thickness(
                FavouriteSoundListView.Padding.Left,
                FavouriteSoundListView.Padding.Top,
                FavouriteSoundListView.Padding.Right,
                bottomPlayingSoundsBarHeight
            );
            SoundGridView2.Padding = new Thickness(
                SoundGridView2.Padding.Left,
                SoundGridView2.Padding.Top,
                SoundGridView2.Padding.Right,
                bottomPlayingSoundsBarHeight
            );
            SoundListView2.Padding = new Thickness(
                SoundListView2.Padding.Left,
                SoundListView2.Padding.Top,
                SoundListView2.Padding.Right,
                bottomPlayingSoundsBarHeight
            );
        }

        private async Task UpdateGridSplitterRange()
        {
            if (BottomPlayingSoundsBarListView.Items.Count == 0) return;

            // Set the max height of the bottom row def
            double totalHeight = await GetTotalPlayingSoundListContentHeight();
            if (totalHeight >= maxBottomPlayingSoundsBarHeight) totalHeight = maxBottomPlayingSoundsBarHeight;

            GridSplitterGridBottomRowDef.MaxHeight = totalHeight;

            // Set the min height of the bottom row def
            double firstItemHeight = await GetPlayingSoundItemContainerHeight(0);
            if (firstItemHeight >= maxBottomPlayingSoundsBarHeight) firstItemHeight = GridSplitterGridBottomRowDef.ActualHeight;

            GridSplitterGridBottomRowDef.MinHeight = firstItemHeight;
        }

        private async Task<double> GetTotalPlayingSoundListContentHeight()
        {
            double totalHeight = 0;

            for (int i = 0; i < BottomPlayingSoundsBarListView.Items.Count; i++)
                totalHeight += await GetPlayingSoundItemContainerHeight(i);

            return totalHeight;
        }

        private async Task<double> GetPlayingSoundItemContainerHeight(int index)
        {
            ListViewItem item = BottomPlayingSoundsBarListView.ContainerFromIndex(index) as ListViewItem;

            if(item == null || !item.IsLoaded || item.ActualHeight == 0)
            {
                if (getContainerHeightCount > 20)
                {
                    getContainerHeightCount = 0;

                    // Return the default item height
                    return 88;
                }
                else
                {
                    getContainerHeightCount++;

                    // Call this methods again after some time
                    await Task.Delay(20);
                    return await GetPlayingSoundItemContainerHeight(index);
                }
            }

            getContainerHeightCount = 0;
            return item.ActualHeight;
        }

        private void UpdateMessagesVisibilities()
        {
            startMessageVisibility = (
                    FileManager.itemViewHolder.AppState == FileManager.AppState.Empty
                    && FileManager.itemViewHolder.SelectedCategory.Equals(Guid.Empty)
                ) ? Visibility.Visible : Visibility.Collapsed;

            if(FileManager.itemViewHolder.AllSounds.Count > 0)
                EmptyCategoryMessageRelativePanel.Margin = new Thickness(0, 220, 0, 25);
            else
                EmptyCategoryMessageRelativePanel.Margin = new Thickness(0, 110, 0, 25);

            emptyCategoryMessageVisibility = (
                !FileManager.itemViewHolder.SelectedCategory.Equals(Guid.Empty)
                && FileManager.itemViewHolder.Sounds.Count == 0
                && FileManager.itemViewHolder.AppState != FileManager.AppState.Loading
            ) ? Visibility.Visible : Visibility.Collapsed;

            Bindings.Update();
        }
        #endregion

        #region Event handlers
        #region Start message event handlers
        private async void StartMessageAddFirstSoundButton_Click(object sender, RoutedEventArgs e)
        {
            // Show file picker for new sounds
            var files = await MainPage.PickFilesForAddSoundsContentDialog();
            if (files.Count == 0) return;

            // Show the dialog for adding the sounds
            var template = (DataTemplate)Resources["SoundFileItemTemplate"];

            ContentDialog addSoundsContentDialog = ContentDialogs.CreateAddSoundsContentDialog(template, files);
            addSoundsContentDialog.PrimaryButtonClick += AddSoundsContentDialog_PrimaryButtonClick;
            await addSoundsContentDialog.ShowAsync();
        }

        private async void AddSoundsContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            await MainPage.AddSelectedSoundFiles();
        }

        private async void StartMessageLoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (await AccountPage.ShowLoginPage() && FileManager.itemViewHolder.AppState == FileManager.AppState.Empty)
                FileManager.itemViewHolder.AppState = FileManager.AppState.InitialSync;
        }

        private async void StartMessageImportButton_Click(object sender, RoutedEventArgs e)
        {
            var startMessageImportDataContentDialog = ContentDialogs.CreateStartMessageImportDataContentDialog();
            startMessageImportDataContentDialog.PrimaryButtonClick += StartMessageImportDataContentDialog_PrimaryButtonClick;
            await startMessageImportDataContentDialog.ShowAsync();
        }

        private async void StartMessageImportDataContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            FileManager.itemViewHolder.AppState = FileManager.AppState.Normal;
            await FileManager.ImportDataAsync(ContentDialogs.ImportFile, true);
            FileManager.UpdatePlayAllButtonVisibility();
        }
        #endregion

        private async void SoundGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (FileManager.itemViewHolder.MultiSelectionEnabled) return;
            await PlaySoundAsync((Sound)e.ClickedItem);
        }

        private async void SoundContentGrid_DragOver(object sender, DragEventArgs e)
        {
            if (!e.DataView.Contains("FileName")) return;

            var deferral = e.GetDeferral();
            e.AcceptedOperation = DataPackageOperation.Copy;

            e.DragUIOverride.IsCaptionVisible = true;
            e.DragUIOverride.IsContentVisible = true;
            e.DragUIOverride.IsGlyphVisible = true;

            // If the file types of all items are not supported, update the Caption
            bool fileTypesSupported = true;

            var storageItems = await e.DataView.GetStorageItemsAsync();
            foreach (var item in storageItems)
            {
                if (!item.IsOfType(StorageItemTypes.File) || !FileManager.allowedFileTypes.Contains((item as StorageFile).FileType))
                {
                    fileTypesSupported = false;
                    break;
                }
            }

            if (fileTypesSupported)
                e.DragUIOverride.Caption = loader.GetString("Drop");
            else
                e.DragUIOverride.Caption = loader.GetString("Drop-FileTypeNotSupported");

            deferral.Complete();
        }

        private async void SoundContentGrid_Drop(object sender, DragEventArgs e)
        {
            if (!e.DataView.Contains(StandardDataFormats.StorageItems)) return;

            var items = await e.DataView.GetStorageItemsAsync();
            if (!items.Any()) return;

            // Check if the file types are supported
            foreach (var storageItem in items)
            {
                if (
                    !storageItem.IsOfType(StorageItemTypes.File)
                    || !FileManager.allowedFileTypes.Contains((storageItem as StorageFile).FileType)
                ) return;
            }

            FileManager.itemViewHolder.LoadingScreenMessage = loader.GetString("AddSoundsMessage");
            FileManager.itemViewHolder.LoadingScreenVisible = true;

            List<string> notAddedSounds = new List<string>();
            Guid? selectedCategory = null;
            if (!Equals(FileManager.itemViewHolder.SelectedCategory, Guid.Empty))
                selectedCategory = FileManager.itemViewHolder.SelectedCategory;

            // Add all files
            foreach (StorageFile soundFile in items)
            {
                if (!FileManager.allowedFileTypes.Contains(soundFile.FileType)) continue;

                // Add the sound to the sound lists
                Guid soundUuid = await FileManager.CreateSoundAsync(null, soundFile.DisplayName, selectedCategory, soundFile);

                if (soundUuid.Equals(Guid.Empty))
                    notAddedSounds.Add(soundFile.Name);
                else
                    await FileManager.AddSound(soundUuid);
            }

            FileManager.itemViewHolder.LoadingScreenVisible = false;

            if(notAddedSounds.Count > 0)
            {
                if(items.Count == 1)
                {
                    var addSoundErrorContentDialog = ContentDialogs.CreateAddSoundErrorContentDialog();
                    await addSoundErrorContentDialog.ShowAsync();
                }
                else
                {
                    var addSoundsErrorContentDialog = ContentDialogs.CreateAddSoundsErrorContentDialog(notAddedSounds);
                    await addSoundsErrorContentDialog.ShowAsync();
                }
            }
        }

        private void SoundGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (skipSoundListSelectionChangedEvent) return;

            // Add new items to selectedSounds list
            if(e.AddedItems.Count > 0)
            {
                // Add each item to SelectedSounds
                foreach(var item in e.AddedItems)
                    FileManager.itemViewHolder.SelectedSounds.Add(item as Sound);
            }
            else if(e.RemovedItems.Count > 0)
            {
                // Remove each item from SelectedSounds
                foreach (var item in e.RemovedItems)
                    FileManager.itemViewHolder.SelectedSounds.Remove(item as Sound);
            }

            UpdateSelectAllFlyoutText();
        }

        private void SoundGridViewPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Pivot pivot = (Pivot)sender;

            // Deselect all items in both GridViews
            SoundGridView.DeselectRange(new ItemIndexRange(0, (uint)SoundGridView.Items.Count));
            FavouriteSoundGridView.DeselectRange(new ItemIndexRange(0, (uint)FavouriteSoundGridView.Items.Count));

            FileManager.itemViewHolder.SelectedSounds.Clear();
            soundsPivotSelected = pivot.SelectedIndex == 0;
        }

        private void SoundListViewPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Pivot pivot = (Pivot)sender;

            // Deselect all items in both ListViews
            SoundListView.DeselectRange(new ItemIndexRange(0, (uint)SoundListView.Items.Count));
            FavouriteSoundListView.DeselectRange(new ItemIndexRange(0, (uint)FavouriteSoundListView.Items.Count));

            FileManager.itemViewHolder.SelectedSounds.Clear();
            soundsPivotSelected = pivot.SelectedIndex == 0;
        }

        private void SoundGridView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            GridView gridView = (GridView)sender;
            double desiredWidth = 200;
            double innerWidth = gridView.ActualWidth - 10;  // Left margin = 10, right margin = (innerWidth - (columns * [sound tile margin])) / columns
            int columns = Convert.ToInt32(innerWidth / desiredWidth);

            FileManager.itemViewHolder.SoundTileWidth = (innerWidth - (columns * 10)) / columns;
            FileManager.itemViewHolder.TriggerSoundTileSizeChangedEvent(gridView, e);
        }

        private void PlayingSoundsBarGridSplitter_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            // Calculate the width of the PlayingSoundsBar in percent
            FileManager.itemViewHolder.PlayingSoundsBarWidth = PlayingSoundsBar.ActualWidth / ContentRoot.ActualWidth;
        }

        private void BottomPlayingSoundsBarGridSplitter_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            isManipulatingBottomPlayingSoundsBar = true;
        }

        private void BottomPlayingSoundsBarGridSplitter_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            BottomPlayingSoundsBar.Height = GridSplitterGridBottomRowDef.ActualHeight;
        }

        private void BottomPlayingSoundsBarGridSplitter_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            isManipulatingBottomPlayingSoundsBar = false;
            SnapBottomPlayingSoundsBar();
        }

        private void BottomPseudoContentGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Update the Paddings of the GridViews and ListViews
            AdaptSoundListScrollViewerForBottomPlayingSoundsBar();
        }

        private void BottomPlayingSoundsBarListView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (isManipulatingBottomPlayingSoundsBar) return;
            GridSplitterGridBottomRowDef.Height = new GridLength(BottomPlayingSoundsBarListView.ActualHeight);
        }

        private async void SnapBottomPlayingSoundsBarStoryboard_Completed(object sender, object e)
        {
            snapBottomPlayingSoundsBarAnimationRunning = false;
            await UpdateGridSplitterRange();
        }
        #endregion
    }

    public enum VerticalPosition
    {
        Top,
        Bottom
    }

    public class ReverserClass : IComparer
    {
        // Reverses the order without comparison
        int IComparer.Compare(object x, object y)
        {
            return 1;
        }
    }
}
