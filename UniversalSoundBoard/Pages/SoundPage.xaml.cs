using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UniversalSoundBoard.Common;
using UniversalSoundBoard.DataAccess;
using UniversalSoundBoard.Models;
using Windows.ApplicationModel.DataTransfer;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace UniversalSoundBoard.Pages
{
    public sealed partial class SoundPage : Page
    {
        public static bool soundsPivotSelected = true;

        
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
        }
        
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            soundsPivotSelected = true;
        }
        
        private void SetSoundsPivotVisibility()
        {
            SoundGridView2.Visibility = (App.Current as App)._itemViewHolder.showSoundsPivot ? Visibility.Collapsed : Visibility.Visible;
        }
        
        private void SetDataContext()
        {
            ContentRoot.DataContext = (App.Current as App)._itemViewHolder;
        }
        
        private void ShowPlayingSoundsList()
        {
            if ((App.Current as App)._itemViewHolder.playingSoundsListVisibility == Visibility.Visible)
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
        }
        
        private void SoundGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var sound = (Sound)e.ClickedItem;
            if ((App.Current as App)._itemViewHolder.selectionMode == ListViewSelectionMode.None)
            {
                PlaySound(sound);
            }
        }
        
        private void SoundGridView_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;

            e.DragUIOverride.Caption = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("Drop");
            e.DragUIOverride.IsCaptionVisible = true;
            e.DragUIOverride.IsContentVisible = true;
            e.DragUIOverride.IsGlyphVisible = true;
        }
        
        private async void SoundGridView_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();

                (App.Current as App)._itemViewHolder.progressRingIsActive = true;
                if (items.Any())
                {
                    Category category = (App.Current as App)._itemViewHolder.categories[(App.Current as App)._itemViewHolder.selectedCategory];

                    foreach (StorageFile soundFile in items)
                    {
                        await FileManager.AddSound(null, soundFile.DisplayName, category.Uuid, soundFile);
                        (App.Current as App)._itemViewHolder.allSoundsChanged = true;
                        await FileManager.UpdateGridView();
                    }

                    if ((App.Current as App)._itemViewHolder.selectedCategory == 0)
                    {
                        await FileManager.ShowAllSounds();
                    }
                    else
                    {
                        await FileManager.ShowCategory((App.Current as App)._itemViewHolder.categories[(App.Current as App)._itemViewHolder.selectedCategory].Uuid);
                    }
                }
                (App.Current as App)._itemViewHolder.progressRingIsActive = false;
            }
        }
        
        private void SoundGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GridView selectedGridview = sender as GridView;

            // If no items are selected, disable multi select buttons
            if (selectedGridview.SelectedItems.Count > 0)
            {
                (App.Current as App)._itemViewHolder.areSelectButtonsEnabled = true;
            }
            else
            {
                (App.Current as App)._itemViewHolder.areSelectButtonsEnabled = false;
            }

            // Add new item to selectedSounds list
            if(e.AddedItems.Count == 1)
            {
                (App.Current as App)._itemViewHolder.selectedSounds.Add((Sound)e.AddedItems.First());
            }else
            {
                (App.Current as App)._itemViewHolder.selectedSounds.Remove((Sound)e.RemovedItems.First());
            }
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
        
        public static void PlaySound(Sound sound)
        {
            List<Sound> soundList = new List<Sound>();
            soundList.Add(sound);
            MediaPlayer player = FileManager.CreateMediaPlayer(soundList, 0);

            // If PlayOneSoundAtOnce is true, remove all sounds from PlayingSounds List
            if ((App.Current as App)._itemViewHolder.playOneSoundAtOnce)
            {
                List<PlayingSound> removedPlayingSounds = new List<PlayingSound>();
                foreach (PlayingSound pSound in (App.Current as App)._itemViewHolder.playingSounds)
                {
                    removedPlayingSounds.Add(pSound);
                }

                RemoveSoundsFromPlayingSoundsList(removedPlayingSounds);
            }

            PlayingSound playingSound = new PlayingSound(sound, player);
            playingSound.Uuid = FileManager.AddPlayingSound(null, soundList, 0, 0, false);
            (App.Current as App)._itemViewHolder.playingSounds.Add(playingSound);
        }
        
        public static void PlaySounds(List<Sound> sounds, int repetitions, bool randomly)
        {
            if(sounds.Count < 1)
            {
                return;
            }

            // If randomly is true, shuffle sounds
            if (randomly)
            {
                Random random = new Random();
                sounds = sounds.OrderBy(a => random.Next()).ToList();
            }

            MediaPlayer player = FileManager.CreateMediaPlayer(sounds, 0);

            // If PlayOneSoundAtOnce is true, remove all sounds from PlayingSounds List
            if ((App.Current as App)._itemViewHolder.playOneSoundAtOnce)
            {
                List<PlayingSound> removedPlayingSounds = new List<PlayingSound>();
                foreach (PlayingSound pSound in (App.Current as App)._itemViewHolder.playingSounds)
                {
                    removedPlayingSounds.Add(pSound);
                }

                RemoveSoundsFromPlayingSoundsList(removedPlayingSounds);
            }

            PlayingSound playingSound = new PlayingSound(null, sounds, player, repetitions, randomly, 0);
            playingSound.Uuid = FileManager.AddPlayingSound(null, sounds, 0, 0, false);
            (App.Current as App)._itemViewHolder.playingSounds.Add(playingSound);
        }
        
        public static void RemovePlayingSound(PlayingSound playingSound)
        {
            FileManager.DeletePlayingSound(playingSound.Uuid);
            (App.Current as App)._itemViewHolder.playingSounds.Remove(playingSound);
        }
        
        private static void RemoveUnusedSounds()
        {
            List<PlayingSound> removedPlayingSounds = new List<PlayingSound>();
            foreach (PlayingSound playingSound in (App.Current as App)._itemViewHolder.playingSounds)
            {
                if (playingSound.MediaPlayer.PlaybackSession.PlaybackState != MediaPlaybackState.Playing &&
                    playingSound.MediaPlayer.PlaybackSession.PlaybackState != MediaPlaybackState.Paused)
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
                removedPlayingSounds[i].MediaPlayer = null;
                RemovePlayingSound(removedPlayingSounds[i]);
            }
        }
        
        private void SoundsPivot_PivotItemLoaded(Pivot sender, PivotItemEventArgs args)
        {
            (App.Current as App)._itemViewHolder.selectedSounds.Clear();
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
            FileManager.DeleteCategory((App.Current as App)._itemViewHolder.categories[(App.Current as App)._itemViewHolder.selectedCategory].Uuid);

            FileManager.CreateCategoriesObservableCollection();
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
            
            Category selectedCategory = (App.Current as App)._itemViewHolder.categories[(App.Current as App)._itemViewHolder.selectedCategory];
            FileManager.UpdateCategory(selectedCategory.Uuid, newName, icon);

            (App.Current as App)._itemViewHolder.title = newName;
            FileManager.CreateCategoriesObservableCollection();
            await FileManager.UpdateGridView();
        }
        #endregion
    }
}
