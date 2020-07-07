using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
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
using Windows.UI.Xaml.Media;
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
        }

        #region Events
        async void SoundPage_Loaded(object sender, RoutedEventArgs e)
        {
            await ShowPlayingSoundsListAsync();

            // Subscribe to events
            FileManager.itemViewHolder.SelectAllSoundsEvent += ItemViewHolder_SelectAllSoundsEvent;
            FileManager.itemViewHolder.Sounds.CollectionChanged += ItemViewHolder_Sounds_CollectionChanged;
            FileManager.itemViewHolder.FavouriteSounds.CollectionChanged += ItemViewHolder_FavouriteSounds_CollectionChanged;

            FileManager.itemViewHolder.PlayingSoundsBarWidth = DrawerContent.ActualWidth;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            soundsPivotSelected = true;

            bool canReorderItems = FileManager.itemViewHolder.SoundOrder == FileManager.SoundOrder.Custom;
            SetReorderItems(canReorderItems);
        }

        private async void SoundPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            await ShowPlayingSoundsListAsync();

            // Update the value of ItemViewHolder.PlayingSoundsBarWidth
            FileManager.itemViewHolder.PlayingSoundsBarWidth = DrawerContent.ActualWidth;
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
            bool canReorderItems = !(
                e.Action == NotifyCollectionChangedAction.Add
                && !string.IsNullOrEmpty(FileManager.itemViewHolder.SearchQuery)
            );

            // Enable or disable the ability to drag sounds
            SetReorderItems(canReorderItems);

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
        #endregion

        #region Helper methods
        private void SetReorderItems(bool value)
        {
            // GridViews
            SoundGridView.CanReorderItems = value;
            SoundGridView.AllowDrop = value;
            SoundGridView2.CanReorderItems = value;
            SoundGridView2.AllowDrop = value;
            FavouriteSoundGridView.CanReorderItems = value;
            FavouriteSoundGridView.AllowDrop = value;

            // ListViews
            SoundListView.CanReorderItems = value;
            SoundListView.AllowDrop = value;
            SoundListView2.CanReorderItems = value;
            SoundListView2.AllowDrop = value;
            FavouriteSoundListView.CanReorderItems = value;
            FavouriteSoundListView.AllowDrop = value;
        }

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

        private async Task ShowPlayingSoundsListAsync()
        {
            if (FileManager.itemViewHolder.PlayingSoundsListVisible)
            {
                // Remove unused PlayingSounds
                await RemoveUnusedSoundsAsync();

                if (Window.Current.Bounds.Width < FileManager.mobileMaxWidth)      // If user is on Mobile
                {
                    SecondColDef.Width = new GridLength(0);     // Set size of right PlayingSoundsList to 0
                    DrawerContentGrid.Visibility = Visibility.Visible;
                }
                else        // If user is on Tablet or Desktop
                {
                    SecondColDef.Width = new GridLength(1, GridUnitType.Star);
                    DrawerContentGrid.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                SecondColDef.Width = new GridLength(0);
                DrawerContentGrid.Visibility = Visibility.Collapsed;
            }
        }
        #endregion

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

            PlayingSound playingSound = new PlayingSound(sound, player)
            {
                Uuid = await FileManager.CreatePlayingSoundAsync(null, soundList, 0, 0, false, player.Volume)
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

            PlayingSound playingSound = new PlayingSound(Guid.Empty, sounds, player, repetitions, randomly, 0)
            {
                Uuid = await FileManager.CreatePlayingSoundAsync(null, sounds, 0, repetitions, randomly, player.Volume)
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

        #region Events
        private async void SoundGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (FileManager.itemViewHolder.MultiSelectionEnabled) return;
            await PlaySoundAsync((Sound)e.ClickedItem);
        }

        private async void ContentRoot_DragOver(object sender, DragEventArgs e)
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

        private async void ContentRoot_Drop(object sender, DragEventArgs e)
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

        private void HandleGrid_OnManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            SolidColorBrush themeBrush = Application.Current.Resources["AppBarToggleButtonBackgroundCheckedPointerOver"] as SolidColorBrush;
            if (themeBrush != null) HandleGrid.Background = themeBrush;
        }

        private void HandleGrid_OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            DrawerContentGrid.Height = DrawerContentGrid.ActualHeight + -e.Delta.Translation.Y;
            if (DrawerContentGrid.Height > ActualHeight)
                DrawerContentGrid.Height = ActualHeight;
        }

        private void HandleGrid_OnManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            SolidColorBrush themeBrush = Application.Current.Resources["AppBarBorderThemeBrush"] as SolidColorBrush;
            if (themeBrush != null) HandleGrid.Background = themeBrush;
        }

        private void HandleGrid_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (DrawerContentGrid.Height == ActualHeight)
                DrawerContentGrid.Height = HandleGrid.ActualHeight;
            else if (DrawerContentGrid.Height > ActualHeight / 2)
                DrawerContentGrid.Height = ActualHeight;
            else if (DrawerContentGrid.Height <= HandleGrid.ActualHeight)
                DrawerContentGrid.Height = ActualHeight;
            else
                DrawerContentGrid.Height = HandleGrid.ActualHeight;
        }

        private void SoundGridViewPivot_PivotItemLoaded(Pivot sender, PivotItemEventArgs args)
        {
            // Deselect all items in both GridViews
            SoundGridView.DeselectRange(new ItemIndexRange(0, (uint)SoundGridView.Items.Count));
            FavouriteSoundGridView.DeselectRange(new ItemIndexRange(0, (uint)FavouriteSoundGridView.Items.Count));

            FileManager.itemViewHolder.SelectedSounds.Clear();
            soundsPivotSelected = sender.SelectedIndex == 0;
        }

        private void SoundListViewPivot_PivotItemLoaded(Pivot sender, PivotItemEventArgs args)
        {
            // Deselect all items in both ListViews
            SoundListView.DeselectRange(new ItemIndexRange(0, (uint)SoundListView.Items.Count));
            FavouriteSoundListView.DeselectRange(new ItemIndexRange(0, (uint)FavouriteSoundListView.Items.Count));

            FileManager.itemViewHolder.SelectedSounds.Clear();
            soundsPivotSelected = sender.SelectedIndex == 0;
        }

        private void SoundGridView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            GridView gridView = (GridView)sender;
            double desiredWidth = 200;
            double innerWidth = gridView.ActualWidth - 10;  // Left margin = 10, right margin = (columns * (12 - columns))
            double columns = Convert.ToInt32(innerWidth / desiredWidth);

            FileManager.itemViewHolder.SoundTileWidth = (innerWidth - columns * (12 - columns)) / columns;
            FileManager.itemViewHolder.TriggerSoundTileSizeChangedEvent(gridView, e);
        }
        #endregion
    }
}
