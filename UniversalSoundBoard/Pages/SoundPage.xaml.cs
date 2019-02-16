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
using Windows.UI;
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

            ShowPlayingSoundsList();
        }
        
        void SoundPage_Loaded(object sender, RoutedEventArgs e)
        {
            SetDataContext();
            SetSoundsPivotVisibility();
            (App.Current as App)._itemViewHolder.SelectAllSoundsEvent += _itemViewHolder_SelectAllSoundsEvent;
            (App.Current as App)._itemViewHolder.Sounds.CollectionChanged += ItemViewHolder_Sounds_CollectionChanged;
            (App.Current as App)._itemViewHolder.FavouriteSounds.CollectionChanged += ItemViewHolder_FavouriteSounds_CollectionChanged;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            soundsPivotSelected = true;
        }

        private GridView GetVisibleGridView()
        {
            if (!(App.Current as App)._itemViewHolder.ShowSoundsPivot)
                return SoundGridView2;
            else if (SoundsPivot.SelectedIndex == 1)
                return FavouriteSoundGridView;
            else
                return SoundGridView;
        }

        private void _itemViewHolder_SelectAllSoundsEvent(object sender, RoutedEventArgs e)
        {
            skipSoundListSelectionChangedEvent = true;

            // Get the visible GridView
            var gridView = GetVisibleGridView();
            (App.Current as App)._itemViewHolder.SelectedSounds.Clear();

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
                    (App.Current as App)._itemViewHolder.SelectedSounds.Add(sound as Sound);
            }
            
            skipSoundListSelectionChangedEvent = false;
            UpdateSelectAllFlyoutText();
        }

        private void ItemViewHolder_Sounds_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
                isDragging = true;

            if((e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add || 
                e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Move) && 
                isDragging)
            {
                UpdateSoundOrder(false);
                isDragging = false;
            }
        }

        private void ItemViewHolder_FavouriteSounds_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
                isDragging = true;

            if ((e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add ||
                e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Move) && 
                isDragging)
            {
                UpdateSoundOrder(true);
                isDragging = false;
            }
        }

        private void UpdateSoundOrder(bool showFavourites)
        {
            // Get the current category uuid
            int selectedCategoryIndex = (App.Current as App)._itemViewHolder.SelectedCategory;
            if (selectedCategoryIndex >= (App.Current as App)._itemViewHolder.Categories.Count) return;
            Guid currentCategoryUuid = selectedCategoryIndex == 0 ? Guid.Empty : (App.Current as App)._itemViewHolder.Categories[selectedCategoryIndex].Uuid;

            // Get the uuids of the sounds
            List<Guid> uuids = new List<Guid>();
            foreach (var sound in showFavourites ? (App.Current as App)._itemViewHolder.FavouriteSounds : (App.Current as App)._itemViewHolder.Sounds)
                uuids.Add(sound.Uuid);

            DatabaseOperations.SetSoundOrder(currentCategoryUuid, showFavourites, uuids);
        }

        private void UpdateSelectAllFlyoutText()
        {
            var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
            var gridView = GetVisibleGridView();

            if (gridView.Items.Count == (App.Current as App)._itemViewHolder.SelectedSounds.Count
                && gridView.Items.Count != 0)
            {
                (App.Current as App)._itemViewHolder.SelectAllFlyoutText = loader.GetString("MoreButton_SelectAllFlyout-DeselectAll");
                (App.Current as App)._itemViewHolder.SelectAllFlyoutIcon = new SymbolIcon(Symbol.ClearSelection);
            }
            else
            {
                (App.Current as App)._itemViewHolder.SelectAllFlyoutText = loader.GetString("MoreButton_SelectAllFlyout-SelectAll");
                (App.Current as App)._itemViewHolder.SelectAllFlyoutIcon = new SymbolIcon(Symbol.SelectAll);
            }

            (App.Current as App)._itemViewHolder.AreSelectButtonsEnabled = gridView.SelectedItems.Count > 0;
        }
        
        private void SetSoundsPivotVisibility()
        {
            SoundGridView2.Visibility = (App.Current as App)._itemViewHolder.ShowSoundsPivot ? Visibility.Collapsed : Visibility.Visible;
        }
        
        private void SetDataContext()
        {
            ContentRoot.DataContext = (App.Current as App)._itemViewHolder;
        }
        
        private void ShowPlayingSoundsList()
        {
            if ((App.Current as App)._itemViewHolder.PlayingSoundsListVisibility == Visibility.Visible)
            {
                // Remove unused PlayingSounds
                RemoveUnusedSounds();

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
        
        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ShowPlayingSoundsList();

            // Update the value of ItemViewHolder.PlayingSoundBarWidth
            (App.Current as App)._itemViewHolder.PlayingSoundsBarWidth = DrawerContent.ActualWidth;
        }
        
        private async void SoundGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var sound = (Sound)e.ClickedItem;
            if ((App.Current as App)._itemViewHolder.SelectionMode == ListViewSelectionMode.None)
            {
                await PlaySound(sound);
            }
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
            
            (App.Current as App)._itemViewHolder.LoadingScreenMessage = new Windows.ApplicationModel.Resources.ResourceLoader().GetString("AddSoundsMessage");
            (App.Current as App)._itemViewHolder.LoadingScreenVisibility = true;

            Guid categoryUuid = Guid.Empty;
            try
            {
                categoryUuid = (App.Current as App)._itemViewHolder.Categories[(App.Current as App)._itemViewHolder.SelectedCategory].Uuid;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            foreach (StorageFile soundFile in items)
            {
                if (FileManager.allowedFileTypes.Contains(soundFile.FileType))
                    await FileManager.AddSound(Guid.Empty, soundFile.DisplayName, categoryUuid, soundFile);
            }

            (App.Current as App)._itemViewHolder.AllSoundsChanged = true;
            await FileManager.UpdateGridView();

            if ((App.Current as App)._itemViewHolder.SelectedCategory == 0)
                await FileManager.ShowAllSounds();
            else
                await FileManager.ShowCategory((App.Current as App)._itemViewHolder.Categories[(App.Current as App)._itemViewHolder.SelectedCategory].Uuid);

            (App.Current as App)._itemViewHolder.LoadingScreenVisibility = false;
        }
        
        private void SoundGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (skipSoundListSelectionChangedEvent) return;
            GridView selectedGridview = sender as GridView;

            // If no items are selected, disable multi select buttons
            (App.Current as App)._itemViewHolder.AreSelectButtonsEnabled = selectedGridview.SelectedItems.Count > 0;

            // Add new item to selectedSounds list
            if(e.AddedItems.Count == 1)
                (App.Current as App)._itemViewHolder.SelectedSounds.Add((Sound)e.AddedItems.First());
            else if(e.RemovedItems.Count > 0)
                (App.Current as App)._itemViewHolder.SelectedSounds.Remove((Sound)e.RemovedItems.First());

            UpdateSelectAllFlyoutText();
        }
        
        private void HandleGrid_OnManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            var themeBrush = Application.Current.Resources["AppBarToggleButtonBackgroundCheckedPointerOver"] as SolidColorBrush;
            if (themeBrush != null) HandleGrid.Background = themeBrush;
        }
        
        private void HandleGrid_OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            DrawerContentGrid.Height = DrawerContentGrid.ActualHeight + -e.Delta.Translation.Y;
            if(DrawerContentGrid.Height > ActualHeight)
            {
                DrawerContentGrid.Height = ActualHeight;
            }
        }
        
        private void HandleGrid_OnManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            var themeBrush = Application.Current.Resources["AppBarBorderThemeBrush"] as SolidColorBrush;
            if (themeBrush != null) HandleGrid.Background = themeBrush;
        }
        
        private void HandleGrid_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (DrawerContentGrid.Height == ActualHeight)
            {
                DrawerContentGrid.Height = HandleGrid.ActualHeight;
            }
            else if (DrawerContentGrid.Height > ActualHeight / 2)
            {
                DrawerContentGrid.Height = ActualHeight;
            }else if (DrawerContentGrid.Height <= HandleGrid.ActualHeight)
            {
                DrawerContentGrid.Height = ActualHeight;
            }
            else
            {
                DrawerContentGrid.Height = HandleGrid.ActualHeight;
            }
        }
        
        public static async Task PlaySound(Sound sound)
        {
            List<Sound> soundList = new List<Sound>();
            soundList.Add(sound);

            MediaPlayer player = await FileManager.CreateMediaPlayer(soundList, 0);
            if (player == null)
                return;

            // If PlayOneSoundAtOnce is true, remove all sounds from PlayingSounds List
            if ((App.Current as App)._itemViewHolder.PlayOneSoundAtOnce)
            {
                List<PlayingSound> removedPlayingSounds = new List<PlayingSound>();
                foreach (PlayingSound pSound in (App.Current as App)._itemViewHolder.PlayingSounds)
                {
                    removedPlayingSounds.Add(pSound);
                }

                RemoveSoundsFromPlayingSoundsList(removedPlayingSounds);
            }

            PlayingSound playingSound = new PlayingSound(sound, player);
            playingSound.Uuid = FileManager.AddPlayingSound(Guid.Empty, soundList, 0, 0, false, player.Volume);
            (App.Current as App)._itemViewHolder.PlayingSounds.Add(playingSound);
        }
        
        public static async Task PlaySounds(List<Sound> sounds, int repetitions, bool randomly)
        {
            // If randomly is true, shuffle sounds
            if (randomly)
            {
                Random random = new Random();
                sounds = sounds.OrderBy(a => random.Next()).ToList();
            }

            MediaPlayer player = await FileManager.CreateMediaPlayer(sounds, 0);
            if (player == null)
                return;

            // If PlayOneSoundAtOnce is true, remove all sounds from PlayingSounds List
            if ((App.Current as App)._itemViewHolder.PlayOneSoundAtOnce)
            {
                List<PlayingSound> removedPlayingSounds = new List<PlayingSound>();
                foreach (PlayingSound pSound in (App.Current as App)._itemViewHolder.PlayingSounds)
                {
                    removedPlayingSounds.Add(pSound);
                }

                RemoveSoundsFromPlayingSoundsList(removedPlayingSounds);
            }

            PlayingSound playingSound = new PlayingSound(Guid.Empty, sounds, player, repetitions, randomly, 0);
            playingSound.Uuid = FileManager.AddPlayingSound(Guid.Empty, sounds, 0, repetitions, false, player.Volume);
            (App.Current as App)._itemViewHolder.PlayingSounds.Add(playingSound);
        }
        
        public static void RemovePlayingSound(PlayingSound playingSound)
        {
            FileManager.DeletePlayingSound(playingSound.Uuid);
            (App.Current as App)._itemViewHolder.PlayingSounds.Remove(playingSound);
        }
        
        private static void RemoveUnusedSounds()
        {
            List<PlayingSound> removedPlayingSounds = new List<PlayingSound>();
            foreach (PlayingSound playingSound in (App.Current as App)._itemViewHolder.PlayingSounds)
            {
                if (playingSound.MediaPlayer.PlaybackSession.PlaybackState != MediaPlaybackState.Playing &&
                    playingSound.MediaPlayer.PlaybackSession.PlaybackState != MediaPlaybackState.Paused && 
                    playingSound.MediaPlayer.PlaybackSession.PlaybackState != MediaPlaybackState.Opening)
                {
                    removedPlayingSounds.Add(playingSound);
                }
            }

            RemoveSoundsFromPlayingSoundsList(removedPlayingSounds);
        }
        
        private static void RemoveSoundsFromPlayingSoundsList(List<PlayingSound> removedPlayingSounds)
        {
            for (int i = 0; i < removedPlayingSounds.Count; i++)
            {
                removedPlayingSounds[i].MediaPlayer.Pause();
                removedPlayingSounds[i].MediaPlayer.SystemMediaTransportControls.IsEnabled = false;
                removedPlayingSounds[i].MediaPlayer = null;
                RemovePlayingSound(removedPlayingSounds[i]);
            }
        }
        
        private void SoundsPivot_PivotItemLoaded(Pivot sender, PivotItemEventArgs args)
        {
            // Deselect all items in both GridViews
            SoundGridView.DeselectRange(new ItemIndexRange(0, (uint)SoundGridView.Items.Count));
            FavouriteSoundGridView.DeselectRange(new ItemIndexRange(0, (uint)FavouriteSoundGridView.Items.Count));

            (App.Current as App)._itemViewHolder.SelectedSounds.Clear();
            soundsPivotSelected = (sender.SelectedIndex == 0);
        }
        
        private void SoundGridView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SetSoundGridItemWidth(e, SoundGridView);
        }
        
        private void FavouriteSoundGridView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SetSoundGridItemWidth(e, FavouriteSoundGridView);
        }
        
        private void SoundGridView2_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SetSoundGridItemWidth(e, SoundGridView2);
        }
        
        private void SetSoundGridItemWidth(SizeChangedEventArgs e, GridView gridView)
        {
            double optimizedWidth = 130.0;

            if (Window.Current.Bounds.Width > FileManager.tabletMaxWidth)
            {
                optimizedWidth = 180;
            }

            if (Window.Current.Bounds.Width > (FileManager.tabletMaxWidth * 2.3))
            {
                optimizedWidth = 220;
            }

            ItemsWrapGrid appItemsPanel = (ItemsWrapGrid)gridView.ItemsPanelRoot;

            double margin = 12.0;
            var number = (int)e.NewSize.Width / (int)optimizedWidth;
            appItemsPanel.ItemWidth = (e.NewSize.Width - margin) / number;
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
            FileManager.DeleteCategory((App.Current as App)._itemViewHolder.Categories[(App.Current as App)._itemViewHolder.SelectedCategory].Uuid);

            FileManager.CreateCategoriesList();
            await FileManager.ShowAllSounds();
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
            
            Category selectedCategory = (App.Current as App)._itemViewHolder.Categories[(App.Current as App)._itemViewHolder.SelectedCategory];
            FileManager.UpdateCategory(selectedCategory.Uuid, newName, icon);

            (App.Current as App)._itemViewHolder.Title = newName;
            FileManager.CreateCategoriesList();
            await FileManager.UpdateGridView();
        }
        #endregion
    }
}
