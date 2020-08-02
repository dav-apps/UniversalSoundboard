using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using UniversalSoundboard.Models;
using UniversalSoundboard.Pages;
using UniversalSoundBoard.Common;
using UniversalSoundBoard.DataAccess;
using UniversalSoundBoard.Models;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Resources;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace UniversalSoundBoard.Pages
{
    public sealed partial class SoundPage : Page
    {
        ResourceLoader loader = new ResourceLoader();
        public static bool soundsPivotSelected = true;
        private bool skipSoundListSelectionChangedEvent = false;
        private bool isDragging = false;
        
        public SoundPage()
        {
            InitializeComponent();
            ContentRoot.DataContext = FileManager.itemViewHolder;

            // Subscribe to events
            FileManager.itemViewHolder.PropertyChanged += ItemViewHolder_PropertyChanged;
            FileManager.itemViewHolder.SoundsLoadedEvent += ItemViewHolder_SoundsLoadedEvent;
            FileManager.itemViewHolder.SelectAllSoundsEvent += ItemViewHolder_SelectAllSoundsEvent;
            FileManager.itemViewHolder.Sounds.CollectionChanged += ItemViewHolder_Sounds_CollectionChanged;
            FileManager.itemViewHolder.FavouriteSounds.CollectionChanged += ItemViewHolder_FavouriteSounds_CollectionChanged;
            FileManager.itemViewHolder.PlayingSounds.CollectionChanged += PlayingSounds_CollectionChanged;
        }

        #region Page event handlers
        async void SoundPage_Loaded(object sender, RoutedEventArgs e)
        {
            await UpdatePlayingSoundsListAsync();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            soundsPivotSelected = true;
        }

        private async void SoundPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            await UpdatePlayingSoundsListAsync();
        }

        private void ItemViewHolder_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if(e.PropertyName.Equals("CurrentTheme"))
                RequestedTheme = FileManager.GetRequestedTheme();
        }

        private void ItemViewHolder_SoundsLoadedEvent(object sender, EventArgs e)
        {
            AdaptSoundListScrollViewerForBottomPlayingSoundsBar();
        }

        private void ItemViewHolder_SelectAllSoundsEvent(object sender, RoutedEventArgs e)
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

        private async void ItemViewHolder_Sounds_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove)
                isDragging = true;

            if(
                (
                    e.Action == NotifyCollectionChangedAction.Add
                    || e.Action == NotifyCollectionChangedAction.Move
                )
                && isDragging
            )
            {
                await UpdateSoundOrder(false);
                isDragging = false;
            }
        }

        private async void ItemViewHolder_FavouriteSounds_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove) isDragging = true;

            if (
                (
                    e.Action == NotifyCollectionChangedAction.Add
                    || e.Action == NotifyCollectionChangedAction.Move
                )
                && isDragging
            )
            {
                await UpdateSoundOrder(true);
                isDragging = false;
            }
        }

        private async void PlayingSounds_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            await UpdatePlayingSoundsListAsync();
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
                // Remove unused PlayingSounds
                await RemoveUnusedSoundsAsync();

                // Set the max width of the sounds list and playing sounds list columns
                PlayingSoundsBarColDef.MaxWidth = ContentRoot.ActualWidth / 2;
                
                if (Window.Current.Bounds.Width < FileManager.mobileMaxWidth)
                {
                    // Hide the PlayingSoundsBar and the GridSplitter
                    PlayingSoundsBarColDef.Width = new GridLength(0);
                    PlayingSoundsBarColDef.MinWidth = 0;
                    GridSplitterColDef.Width = new GridLength(0);

                    // Show the mobile PlayingSounds list
                    if(FileManager.itemViewHolder.PlayingSounds.Count == 0)
                    {
                        BottomPlayingSoundsBar.Visibility = Visibility.Collapsed;
                        BottomPlayingSoundsBarGridSplitter.Visibility = Visibility.Collapsed;
                    }
                    else if(FileManager.itemViewHolder.PlayingSounds.Count == 1)
                    {
                        BottomPlayingSoundsBar.Visibility = Visibility.Visible;
                        BottomPlayingSoundsBarGridSplitter.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        BottomPlayingSoundsBar.Visibility = Visibility.Visible;
                        BottomPlayingSoundsBarGridSplitter.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    // Show the PlayingSoundsBar and the GridSplitter
                    PlayingSoundsBarColDef.Width = new GridLength(ContentRoot.ActualWidth * FileManager.itemViewHolder.PlayingSoundsBarWidth);
                    PlayingSoundsBarColDef.MinWidth = ContentRoot.ActualWidth / 3.8;
                    GridSplitterColDef.Width = new GridLength(12);

                    // Hide the mobile PlayingSounds list
                    BottomPlayingSoundsBar.Visibility = Visibility.Collapsed;
                }

                AdaptSoundListScrollViewerForBottomPlayingSoundsBar();
            }
            else
            {
                // Hide the PlayingSoundsBar and the GridSplitter
                PlayingSoundsBarColDef.Width = new GridLength(0);
                PlayingSoundsBarColDef.MinWidth = 0;
                GridSplitterColDef.Width = new GridLength(0);

                // Hide the mobile PlayingSounds list
                BottomPlayingSoundsBar.Visibility = Visibility.Collapsed;
            }
        }
        #endregion

        #region Functionality
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

            MediaPlayer player = await FileManager.CreateMediaPlayerAsync(soundList, 0);
            if (player == null) return;

            // If PlayOneSoundAtOnce is true, remove all sounds from PlayingSounds List
            if (FileManager.itemViewHolder.PlayOneSoundAtOnce)
            {
                List<PlayingSound> removedPlayingSounds = new List<PlayingSound>();
                foreach (PlayingSound pSound in FileManager.itemViewHolder.PlayingSounds)
                    removedPlayingSounds.Add(pSound);

                await RemoveSoundsFromPlayingSoundsListAsync(removedPlayingSounds);
            }

            PlayingSound playingSound = new PlayingSound(player, sound)
            {
                Uuid = await FileManager.CreatePlayingSoundAsync(null, soundList, 0, 0, false)
            };
            FileManager.itemViewHolder.PlayingSounds.Add(playingSound);
            playingSound.MediaPlayer.Play();
        }

        public static async Task PlaySoundsAsync(List<Sound> sounds, int repetitions, bool randomly)
        {
            // If randomly is true, shuffle sounds
            if (randomly)
            {
                Random random = new Random();
                sounds = sounds.OrderBy(a => random.Next()).ToList();
            }

            MediaPlayer player = await FileManager.CreateMediaPlayerAsync(sounds, 0);
            if (player == null) return;

            // If PlayOneSoundAtOnce is true, remove all sounds from PlayingSounds List
            if (FileManager.itemViewHolder.PlayOneSoundAtOnce)
            {
                List<PlayingSound> removedPlayingSounds = new List<PlayingSound>();
                foreach (PlayingSound pSound in FileManager.itemViewHolder.PlayingSounds)
                    removedPlayingSounds.Add(pSound);

                await RemoveSoundsFromPlayingSoundsListAsync(removedPlayingSounds);
            }

            PlayingSound playingSound = new PlayingSound(Guid.Empty, player, sounds, 0, repetitions, randomly)
            {
                Uuid = await FileManager.CreatePlayingSoundAsync(null, sounds, 0, repetitions, randomly)
            };
            FileManager.itemViewHolder.PlayingSounds.Add(playingSound);
            playingSound.MediaPlayer.Play();
        }

        public static async Task RemovePlayingSoundAsync(PlayingSound playingSound)
        {
            await FileManager.DeletePlayingSoundAsync(playingSound.Uuid);
            FileManager.itemViewHolder.PlayingSounds.Remove(playingSound);
        }

        private static async Task RemoveUnusedSoundsAsync()
        {
            List<PlayingSound> removedPlayingSounds = new List<PlayingSound>();
            foreach (PlayingSound playingSound in FileManager.itemViewHolder.PlayingSounds)
            {
                if (
                    playingSound.MediaPlayer.PlaybackSession.PlaybackState != MediaPlaybackState.Playing
                    && playingSound.MediaPlayer.PlaybackSession.PlaybackState != MediaPlaybackState.Paused
                    && playingSound.MediaPlayer.PlaybackSession.PlaybackState != MediaPlaybackState.Opening
                ) removedPlayingSounds.Add(playingSound);
            }

            await RemoveSoundsFromPlayingSoundsListAsync(removedPlayingSounds);
        }

        private static async Task RemoveSoundsFromPlayingSoundsListAsync(List<PlayingSound> removedPlayingSounds)
        {
            for (int i = 0; i < removedPlayingSounds.Count; i++)
            {
                removedPlayingSounds[i].MediaPlayer.Pause();
                removedPlayingSounds[i].MediaPlayer.SystemMediaTransportControls.IsEnabled = false;
                removedPlayingSounds[i].MediaPlayer = null;
                await RemovePlayingSoundAsync(removedPlayingSounds[i]);
            }
        }
        #endregion

        #region UI
        private void AdaptSoundListScrollViewerForBottomPlayingSoundsBar()
        {
            double bottomPlayingSoundsBarHeight = BottomPlayingSoundsBar.Visibility == Visibility.Visible ? Window.Current.Bounds.Height - PseudoContentGrid.ActualHeight : 0;

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

        private void AdaptBottomPlayingSoundsBarHeight()
        {
            // Set the height of the bottom bar to the height of the space below the GridSplitter
            BottomPlayingSoundsBarContent.Height = BottomPlayingSoundsBarRowDef.ActualHeight;

            // Set the MinHeight and MaxHeight values of the BottomPlayingSoundsBar
            UpdateBottomPlayingSoundsMinMaxHeight();

            // Update the ScrollViewer of the Sounds lists
            AdaptSoundListScrollViewerForBottomPlayingSoundsBar();
        }

        private void UpdateBottomPlayingSoundsMinMaxHeight()
        {
            // Calculate the height of BottomPlayingSoundsBarListView
            double totalListViewHeight = 0;
            for (int i = 0; i < BottomPlayingSoundsBarListView.Items.Count; i++)
            {
                if (i == 0)
                {
                    // Set the min height of the row definition
                    var item = BottomPlayingSoundsBarListView.ContainerFromIndex(i) as ListViewItem;
                    if (item == null) return;

                    BottomPlayingSoundsBarRowDef.MinHeight = item.ActualHeight;
                }

                totalListViewHeight += (BottomPlayingSoundsBarListView.ContainerFromIndex(i) as ListViewItem).ActualHeight;
            }

            BottomPlayingSoundsBarRowDef.MaxHeight = totalListViewHeight;
        }

        private void AnimateBottomPlayingSoundsBar(bool moveToTop)
        {
            // Calculate the height of the key frames
            double frameLength = 0.01;
            int frames = 12;

            double diff;
            if (moveToTop)
                diff = BottomPlayingSoundsBarRowDef.MaxHeight - BottomPlayingSoundsBarRowDef.ActualHeight;
            else
                diff = BottomPlayingSoundsBarRowDef.MinHeight - BottomPlayingSoundsBarRowDef.ActualHeight;

            double heightPerFrame = diff / frames;

            // Stop the current animation and remove the old key frames
            SnapBottomPlayingSoundsBarStoryboard.Stop();
            SnapBottomPlayingSoundsBarRowDefAnimation.KeyFrames.Clear();
            SnapBottomPlayingSoundsBarContentAnimation.KeyFrames.Clear();

            // Create the new key frames and add them to the storyboard
            for (int i = 0; i < frames; i++)
            {
                SnapBottomPlayingSoundsBarRowDefAnimation.KeyFrames.Add(
                    new DiscreteObjectKeyFrame
                    {
                        Value = BottomPlayingSoundsBarRowDef.ActualHeight + heightPerFrame * i,
                        KeyTime = TimeSpan.FromSeconds(frameLength * i)
                    }
                );
                SnapBottomPlayingSoundsBarContentAnimation.KeyFrames.Add(
                    new DiscreteObjectKeyFrame
                    {
                        Value = BottomPlayingSoundsBarRowDef.ActualHeight + heightPerFrame * i,
                        KeyTime = TimeSpan.FromSeconds(frameLength * i)
                    }
                );
            }

            // Start the animation
            SnapBottomPlayingSoundsBarStoryboard.Begin();
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

        private void BottomPlayingSoundsBarGridSplitter_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            AdaptBottomPlayingSoundsBarHeight();
        }

        private void BottomPlayingSoundsBarGridSplitter_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            AnimateBottomPlayingSoundsBar(BottomPlayingSoundsBarRowDef.ActualHeight > (BottomPlayingSoundsBarRowDef.MaxHeight + BottomPlayingSoundsBarRowDef.MinHeight) / 2);
        }

        private void SnapBottomPlayingSoundsBarStoryboard_Completed(object sender, object e)
        {
            AdaptSoundListScrollViewerForBottomPlayingSoundsBar();
        }
        #endregion
    }
}
