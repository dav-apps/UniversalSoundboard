using Microsoft.Toolkit.Uwp.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using UniversalSoundboard.Common;
using UniversalSoundboard.Models;
using UniversalSoundboard.Pages;
using UniversalSoundBoard.Common;
using UniversalSoundBoard.DataAccess;
using UniversalSoundBoard.Models;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Resources;
using Windows.Foundation.Collections;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;

namespace UniversalSoundBoard.Pages
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
        private int getContainerHeightCount = 0;
        AdvancedCollectionView reversedPlayingSounds;
        Visibility startMessageVisibility = Visibility.Collapsed;
        Visibility emptyCategoryMessageVisibility = Visibility.Collapsed;
        
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

            FileManager.itemViewHolder.Sounds.CollectionChanged += ItemViewHolder_Sounds_CollectionChanged;
            FileManager.itemViewHolder.FavouriteSounds.CollectionChanged += ItemViewHolder_FavouriteSounds_CollectionChanged;

            reversedPlayingSounds.VectorChanged += ReversedPlayingSounds_VectorChanged;
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
            if (FileManager.itemViewHolder.PlayingSounds.Count == 1)
            {
                // Start the animation for hiding the BottomPlayingSoundsBar background / BottomPseudoContentGrid
                GridSplitterGridBottomRowDef.MinHeight = 0;
                StartSnapBottomPlayingSoundsBarAnimation(GridSplitterGridBottomRowDef.ActualHeight, 0);
            }
        }

        private async void ItemViewHolder_RemovePlayingSoundItemEnded(object sender, PlayingSoundItemEventArgs e)
        {
            // Snap the BottomPlayingSoundsBar; for the case that the height of the removed PlayingSound was different
            await UpdateGridSplitterRange();
            SnapBottomPlayingSoundsBar();
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
                
                if (
                    (FileManager.itemViewHolder.PlayingSoundsListVisible && !FileManager.itemViewHolder.OpenMultipleSounds)
                    || Window.Current.Bounds.Width < FileManager.mobileMaxWidth
                )
                {
                    // Hide the PlayingSoundsBar and the GridSplitter
                    PlayingSoundsBarColDef.Width = new GridLength(0);
                    PlayingSoundsBarColDef.MinWidth = 0;
                    GridSplitterColDef.Width = new GridLength(0);

                    // Update the visibility of the BottomPlayingSoundsBar
                    if(reversedPlayingSounds.Count == 0)
                    {
                        BottomPlayingSoundsBar.Visibility = Visibility.Collapsed;
                        GridSplitterGrid.Visibility = Visibility.Collapsed;
                    }
                    else if(reversedPlayingSounds.Count == 1)
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
                        BottomPlayingSoundsBarGridSplitter.Visibility = Visibility.Visible;
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

                    // The user reordered the sounds as the item was first removed and now added again
                    if (reorderedItem.Equals(addedSound.Uuid))
                        await UpdateSoundOrder(favourites);

                    reorderedItem = Guid.Empty;
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

        public static async Task PlaySoundAsync(Sound sound)
        {
            List<Sound> soundList = new List<Sound> { sound };

            var createMediaPlayerResult = FileManager.CreateMediaPlayer(soundList, 0);
            MediaPlayer player = createMediaPlayerResult.Item1;
            List<Sound> newSounds = createMediaPlayerResult.Item2;
            if (player == null || newSounds == null) return;

            // If OpenMultipleSounds is false, remove all sounds from PlayingSounds List
            if (!FileManager.itemViewHolder.OpenMultipleSounds)
            {
                List<PlayingSound> removedPlayingSounds = new List<PlayingSound>();
                foreach (PlayingSound pSound in FileManager.itemViewHolder.PlayingSounds)
                    removedPlayingSounds.Add(pSound);

                await RemoveSoundsFromPlayingSoundsListAsync(removedPlayingSounds);
            }

            PlayingSound playingSound = new PlayingSound(player, sound)
            {
                Uuid = await FileManager.CreatePlayingSoundAsync(null, newSounds, 0, 0, false, sound.DefaultVolume, sound.DefaultMuted),
                Volume = sound.DefaultVolume,
                Muted = sound.DefaultMuted,
                StartPlaying = true
            };
            FileManager.itemViewHolder.PlayingSounds.Add(playingSound);
            playingSound.MediaPlayer.TimelineController.Start();
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

            // If OpenMultipleSounds is false, remove all sounds from PlayingSounds List
            if (!FileManager.itemViewHolder.OpenMultipleSounds)
            {
                List<PlayingSound> removedPlayingSounds = new List<PlayingSound>();
                foreach (PlayingSound pSound in FileManager.itemViewHolder.PlayingSounds)
                    removedPlayingSounds.Add(pSound);

                await RemoveSoundsFromPlayingSoundsListAsync(removedPlayingSounds);
            }

            PlayingSound playingSound = new PlayingSound(
                await FileManager.CreatePlayingSoundAsync(null, newSounds, 0, repetitions, randomly, null, null),
                player,
                newSounds,
                0,
                repetitions,
                randomly
            );
            playingSound.StartPlaying = true;
            FileManager.itemViewHolder.PlayingSounds.Add(playingSound);
            playingSound.MediaPlayer.TimelineController.Start();
        }

        public static async Task RemovePlayingSoundAsync(PlayingSound playingSound)
        {
            await FileManager.DeletePlayingSoundAsync(playingSound.Uuid);
            FileManager.itemViewHolder.PlayingSounds.Remove(playingSound);
        }

        private static async Task RemoveSoundsFromPlayingSoundsListAsync(List<PlayingSound> removedPlayingSounds)
        {
            for (int i = 0; i < removedPlayingSounds.Count; i++)
            {
                removedPlayingSounds[i].MediaPlayer.TimelineController.Pause();
                removedPlayingSounds[i].MediaPlayer = null;
                await RemovePlayingSoundAsync(removedPlayingSounds[i]);
            }
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

            if(item == null)
            {
                if(getContainerHeightCount > 15)
                {
                    getContainerHeightCount = 0;

                    // Return the default item height
                    return 88;
                }
                else
                {
                    getContainerHeightCount++;

                    // Call this methods again after some time
                    await Task.Delay(15);
                    return await GetPlayingSoundItemContainerHeight(index);
                }
            }

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
            await MainPage.PickSounds();
        }

        private async void StartMessageLoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (await AccountPage.Login() && FileManager.itemViewHolder.AppState == FileManager.AppState.Empty)
                FileManager.itemViewHolder.AppState = FileManager.AppState.InitialSync;
        }

        private async void StartMessageImportButton_Click(object sender, RoutedEventArgs e)
        {
            Style buttonRevealStyle = Resources["ButtonRevealStyle"] as Style;
            var startMessageImportDataContentDialog = ContentDialogs.CreateStartMessageImportDataContentDialog(buttonRevealStyle);
            startMessageImportDataContentDialog.PrimaryButtonClick += StartMessageImportDataContentDialog_PrimaryButtonClick;
            await startMessageImportDataContentDialog.ShowAsync();
        }

        private async void StartMessageImportDataContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            FileManager.itemViewHolder.AppState = FileManager.AppState.Normal;
            await FileManager.ImportDataAsync(ContentDialogs.ImportFile, true);
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

            bool fileTypesSupported = true;

            // Check if the file types are supported
            foreach (var storageItem in items)
            {
                if (!storageItem.IsOfType(StorageItemTypes.File) || !FileManager.allowedFileTypes.Contains((storageItem as StorageFile).FileType))
                {
                    fileTypesSupported = false;
                    break;
                }
            }

            if (!fileTypesSupported) return;

            FileManager.itemViewHolder.LoadingScreenMessage = loader.GetString("AddSoundsMessage");
            FileManager.itemViewHolder.LoadingScreenVisible = true;

            Guid? selectedCategory = null;
            if (!Equals(FileManager.itemViewHolder.SelectedCategory, Guid.Empty))
                selectedCategory = FileManager.itemViewHolder.SelectedCategory;

            // Add all files
            foreach (StorageFile soundFile in items)
            {
                if (!FileManager.allowedFileTypes.Contains(soundFile.FileType)) continue;

                // Add the sound to the sound lists
                await FileManager.AddSound(await FileManager.CreateSoundAsync(null, soundFile.DisplayName, selectedCategory, soundFile));
            }

            FileManager.itemViewHolder.LoadingScreenVisible = false;
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
            double innerWidth = gridView.ActualWidth - 10;  // Left margin = 10, right margin = (columns * (12 - columns))
            int columns = Convert.ToInt32(innerWidth / desiredWidth);

            FileManager.itemViewHolder.SoundTileWidth = (innerWidth - columns * (12 - columns)) / columns;
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
