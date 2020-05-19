using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using UniversalSoundBoard.Common;
using UniversalSoundBoard.DataAccess;
using UniversalSoundBoard.Models;
using Windows.ApplicationModel.DataTransfer;
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
        public static bool soundsPivotSelected = true;
        private bool skipSoundListSelectionChangedEvent = false;
        private bool isDragging = false;

        
        public SoundPage()
        {
            InitializeComponent();
            Loaded += SoundPage_Loaded;
            ContentRoot.DataContext = FileManager.itemViewHolder;
        }
        
        async void SoundPage_Loaded(object sender, RoutedEventArgs e)
        {
            FileManager.itemViewHolder.SelectAllSoundsEvent += ItemViewHolder_SelectAllSoundsEvent;
            FileManager.itemViewHolder.Sounds.CollectionChanged += ItemViewHolder_Sounds_CollectionChanged;
            FileManager.itemViewHolder.FavouriteSounds.CollectionChanged += ItemViewHolder_FavouriteSounds_CollectionChanged;

            await ShowPlayingSoundsListAsync();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            soundsPivotSelected = true;

            bool canReorderItems = FileManager.itemViewHolder.SoundOrder == FileManager.SoundOrder.Custom;
            SetReorderItems(canReorderItems);
        }

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

        private async void ItemViewHolder_Sounds_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            bool canReorderItems = !(
                e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add
                && !string.IsNullOrEmpty(FileManager.itemViewHolder.SearchQuery)
            );

            // Enable or disable the ability to drag sounds
            SetReorderItems(canReorderItems);

            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
                isDragging = true;

            if(
                (
                    e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add
                    || e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Move
                )
                && isDragging
            )
            {
                await UpdateSoundOrder(false);
                isDragging = false;
            }
        }

        private async void ItemViewHolder_FavouriteSounds_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
                isDragging = true;

            if (
                (
                    e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add
                    || e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Move
                )
                && isDragging
            )
            {
                await UpdateSoundOrder(true);
                isDragging = false;
            }
        }

        private async Task UpdateSoundOrder(bool showFavourites)
        {
            // Get the current category uuid
            int selectedCategoryIndex = FileManager.itemViewHolder.SelectedCategory;
            if (selectedCategoryIndex >= FileManager.itemViewHolder.Categories.Count) return;
            Guid currentCategoryUuid = selectedCategoryIndex == 0 ? Guid.Empty : FileManager.itemViewHolder.Categories[selectedCategoryIndex].Uuid;

            // Get the uuids of the sounds
            List<Guid> uuids = new List<Guid>();
            foreach (var sound in showFavourites ? FileManager.itemViewHolder.FavouriteSounds : FileManager.itemViewHolder.Sounds)
                uuids.Add(sound.Uuid);

            await DatabaseOperations.SetSoundOrderAsync(currentCategoryUuid, showFavourites, uuids);
            FileManager.UpdateCustomSoundOrder(currentCategoryUuid, showFavourites, uuids);
        }

        private void UpdateSelectAllFlyoutText()
        {
            var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
            bool allItemsSelected;
            bool itemsSelected;

            if (FileManager.itemViewHolder.ShowListView)
            {
                ListView listView = GetVisibleListView();
                allItemsSelected = listView.Items.Count == FileManager.itemViewHolder.SelectedSounds.Count && listView.Items.Count != 0;
                itemsSelected = listView.SelectedItems.Count > 0;
            }
            else
            {
                GridView gridView = GetVisibleGridView();
                allItemsSelected = gridView.Items.Count == FileManager.itemViewHolder.SelectedSounds.Count && gridView.Items.Count != 0;
                itemsSelected = gridView.SelectedItems.Count > 0;
            }

            if (allItemsSelected)
            {
                FileManager.itemViewHolder.SelectAllFlyoutText = loader.GetString("MoreButton_SelectAllFlyout-DeselectAll");
                FileManager.itemViewHolder.SelectAllFlyoutIcon = new SymbolIcon(Symbol.ClearSelection);
            }
            else
            {
                FileManager.itemViewHolder.SelectAllFlyoutText = loader.GetString("MoreButton_SelectAllFlyout-SelectAll");
                FileManager.itemViewHolder.SelectAllFlyoutIcon = new SymbolIcon(Symbol.SelectAll);
            }

            FileManager.itemViewHolder.AreSelectButtonsEnabled = itemsSelected;
        }
        
        private async Task ShowPlayingSoundsListAsync()
        {
            if (FileManager.itemViewHolder.PlayingSoundsListVisibility == Visibility.Visible)
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
        
        private async void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            await ShowPlayingSoundsListAsync();

            // Update the value of ItemViewHolder.PlayingSoundBarWidth
            FileManager.itemViewHolder.PlayingSoundsBarWidth = DrawerContent.ActualWidth;
        }
        
        private async void SoundGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            await SoundItemClick((Sound)e.ClickedItem);
        }

        private async void SoundListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            await SoundItemClick((Sound)e.ClickedItem);
        }

        private async Task SoundItemClick(Sound sound)
        {
            if (FileManager.itemViewHolder.SelectionMode == ListViewSelectionMode.None)
                await PlaySoundAsync(sound);
        }

        private async void SoundGridView_DragOver(object sender, DragEventArgs e)
        {
            if (!e.DataView.Contains("FileName")) return;

            var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
            var deferral = e.GetDeferral();
            e.AcceptedOperation = DataPackageOperation.Copy;

            e.DragUIOverride.IsCaptionVisible = true;
            e.DragUIOverride.IsContentVisible = true;
            e.DragUIOverride.IsGlyphVisible = true;
            
            // If the file types of all items are not supported, update the Caption
            bool fileTypesSupported = false;

            var storageItems = await e.DataView.GetStorageItemsAsync();
            foreach (var item in storageItems)
            {
                if (!item.IsOfType(StorageItemTypes.File)) continue;
                if (!FileManager.allowedFileTypes.Contains((item as StorageFile).FileType)) continue;
                fileTypesSupported = true;
            }

            if (!fileTypesSupported)
                e.DragUIOverride.Caption = loader.GetString("Drop-FileTypeNotSupported");
            else
                e.DragUIOverride.Caption = loader.GetString("Drop");

            deferral.Complete();
        }
        
        private async void SoundGridView_Drop(object sender, DragEventArgs e)
        {
            if (!e.DataView.Contains(StandardDataFormats.StorageItems)) return;

            var items = await e.DataView.GetStorageItemsAsync();
            if (!items.Any()) return;
            bool fileTypesSupported = false;

            // Check if the file types are supported
            foreach (var storageItem in items)
            {
                if (!storageItem.IsOfType(StorageItemTypes.File)) continue;
                if (!FileManager.allowedFileTypes.Contains((storageItem as StorageFile).FileType)) continue;
                fileTypesSupported = true;
            }

            if (!fileTypesSupported) return;
            
            FileManager.itemViewHolder.LoadingScreenMessage = new Windows.ApplicationModel.Resources.ResourceLoader().GetString("AddSoundsMessage");
            FileManager.itemViewHolder.LoadingScreenVisibility = true;
            
            List<Guid> categoryUuids = new List<Guid>();

            if(FileManager.itemViewHolder.SelectedCategory != 0)
            {
                try
                {
                    categoryUuids.Add(FileManager.itemViewHolder.Categories[FileManager.itemViewHolder.SelectedCategory].Uuid);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }

            foreach (StorageFile soundFile in items)
                if (FileManager.allowedFileTypes.Contains(soundFile.FileType))
                    await FileManager.AddSoundAsync(Guid.Empty, soundFile.DisplayName, categoryUuids, soundFile);

            FileManager.itemViewHolder.AllSoundsChanged = true;
            await FileManager.UpdateGridViewAsync();
            
            FileManager.itemViewHolder.LoadingScreenVisibility = false;
        }
        
        private void SoundGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (skipSoundListSelectionChangedEvent) return;
            GridView selectedGridView = sender as GridView;

            // If no items are selected, disable multi select buttons
            FileManager.itemViewHolder.AreSelectButtonsEnabled = selectedGridView.SelectedItems.Count > 0;

            // Add new item to selectedSounds list
            if(e.AddedItems.Count == 1)
                FileManager.itemViewHolder.SelectedSounds.Add((Sound)e.AddedItems.First());
            else if(e.RemovedItems.Count > 0)
                FileManager.itemViewHolder.SelectedSounds.Remove((Sound)e.RemovedItems.First());

            UpdateSelectAllFlyoutText();
        }

        private void SoundListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (skipSoundListSelectionChangedEvent) return;
            ListView selectedListView = sender as ListView;

            // If no items are selected, disable multi select buttons
            FileManager.itemViewHolder.AreSelectButtonsEnabled = selectedListView.SelectedItems.Count > 0;

            // Add new item to selectedSounds list
            if (e.AddedItems.Count == 1)
                FileManager.itemViewHolder.SelectedSounds.Add((Sound)e.AddedItems.First());
            else if (e.RemovedItems.Count > 0)
                FileManager.itemViewHolder.SelectedSounds.Remove((Sound)e.RemovedItems.First());

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
            if(DrawerContentGrid.Height > ActualHeight)
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
        
        public static async Task PlaySoundAsync(Sound sound)
        {
            List<Sound> soundList = new List<Sound>{ sound };

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
                Uuid = await FileManager.AddPlayingSoundAsync(Guid.Empty, soundList, 0, 0, false, player.Volume)
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
            if (player == null)
                return;

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
                Uuid = await FileManager.AddPlayingSoundAsync(Guid.Empty, sounds, 0, repetitions, randomly, player.Volume)
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
            SetSoundGridItemWidth(e, SoundGridView);
        }
        
        private void SetSoundGridItemWidth(SizeChangedEventArgs e, GridView gridView)
        {
            ItemsWrapGrid appItemsPanel = (ItemsWrapGrid)gridView.ItemsPanelRoot;
            int optimizedWidth = Window.Current.Bounds.Width > FileManager.tabletMaxWidth ? 200 : 150;

            // Calculate the size for the tiles, so that the entire space of the GridView is filled
            int number = (int)e.NewSize.Width / optimizedWidth;
            double newSoundTileWidth = (e.NewSize.Width - 12) / number;

            // Update the width of the sound tiles and trigger the change event
            appItemsPanel.ItemWidth = newSoundTileWidth;
            FileManager.itemViewHolder.SoundTileWidth = newSoundTileWidth;
            FileManager.itemViewHolder.TriggerSoundTileSizeChangedEvent(gridView, e);
        }

        #region ContentDialog Methods
        private async void CategoryDeleteButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var deleteCategoryContentDialog = ContentDialogs.CreateDeleteCategoryContentDialogAsync();
            deleteCategoryContentDialog.PrimaryButtonClick += DeleteCategoryContentDialog_PrimaryButtonClick;
            await deleteCategoryContentDialog.ShowAsync();
        }
        
        private async void DeleteCategoryContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            await FileManager.DeleteCategoryAsync(FileManager.itemViewHolder.Categories[FileManager.itemViewHolder.SelectedCategory].Uuid);

            await FileManager.CreateCategoriesListAsync();
            await FileManager.ShowAllSoundsAsync();
        }
        
        private async void CategoryEditButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var editCategoryContentDialog = ContentDialogs.CreateEditCategoryContentDialogAsync();
            editCategoryContentDialog.PrimaryButtonClick += EditCategoryContentDialog_PrimaryButtonClick;
            await editCategoryContentDialog.ShowAsync();
        }
        
        private async void EditCategoryContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // Get combobox value
            ComboBoxItem typeItem = (ComboBoxItem)ContentDialogs.IconSelectionComboBox.SelectedItem;
            string icon = typeItem.Content.ToString();
            string newName = ContentDialogs.EditCategoryTextBox.Text;
            
            Category selectedCategory = FileManager.itemViewHolder.Categories[FileManager.itemViewHolder.SelectedCategory];
            await FileManager.UpdateCategoryAsync(selectedCategory.Uuid, newName, icon);

            FileManager.itemViewHolder.Title = newName;
            await FileManager.CreateCategoriesListAsync();
            await FileManager.UpdateGridViewAsync();
        }
        #endregion
    }
}
