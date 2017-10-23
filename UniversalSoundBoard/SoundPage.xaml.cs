using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using UniversalSoundBoard.Model;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using static UniversalSoundBoard.Model.Sound;

// Die Elementvorlage "Leere Seite" ist unter http://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace UniversalSoundBoard
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class SoundPage : Page
    {
        static bool soundsPivotSelected = false;

        public SoundPage()
        {
            this.InitializeComponent();
            Loaded += SoundPage_Loaded;

            AdjustLayout();
        }

        void SoundPage_Loaded(object sender, RoutedEventArgs e)
        {
            setDataContext();
            SetSoundsPivotVisibility();
        }

        private void SetSoundsPivotVisibility()
        {
            SoundGridView2.Visibility = (App.Current as App)._itemViewHolder.showSoundsPivot ? Visibility.Collapsed : Visibility.Visible;
        }

        private void setDataContext()
        {
            ContentRoot.DataContext = (App.Current as App)._itemViewHolder;
        }

        private void AdjustLayout()
        {
            if (Window.Current.Bounds.Width < FileManager.mobileMaxWidth)       // If user is on mobile
            {
                // Hide title and show in SoundPage
                TitleStackPanel.Visibility = Visibility.Visible;
            }
            else
            {
                TitleStackPanel.Visibility = Visibility.Collapsed;
            }
            // Show PlayingSounds list
            ShowPlayingSoundsList();
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
            AdjustLayout();
        }

        private void SoundGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var sound = (Sound)e.ClickedItem;
            if ((App.Current as App)._itemViewHolder.selectionMode == ListViewSelectionMode.None)
            {
                playSound(sound);
            }
        }

        private async void SoundGridView_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();

                (App.Current as App)._itemViewHolder.progressRingIsActive = true;
                if (items.Any())
                {
                    Category category = new Category();
                    // Get category if a category is selected
                    if ((App.Current as App)._itemViewHolder.title != (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("Settings-Title") &&
                        String.IsNullOrEmpty((App.Current as App)._itemViewHolder.searchQuery) && (App.Current as App)._itemViewHolder.editButtonVisibility == Visibility.Visible)
                    {
                        category.Name = (App.Current as App)._itemViewHolder.title;
                    }

                    // Application now has read/write access to the picked file(s)
                    foreach (StorageFile soundFile in items)
                    {
                        Sound sound = new Sound(soundFile.DisplayName, category, soundFile);
                        await FileManager.addSound(sound);
                    }

                    if (String.IsNullOrEmpty(category.Name))
                    {
                        await ShowAllSounds();
                    }
                    else
                    {
                        await ShowCategory(category);
                    }
                }
                (App.Current as App)._itemViewHolder.progressRingIsActive = false;
            }
        }

        private void SoundGridView_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;

            // Drag adorner ... change what mouse / icon looks like
            // as you're dragging the file into the app:
            // http://igrali.com/2015/05/15/drag-and-drop-photos-into-windows-10-universal-apps/
            e.DragUIOverride.Caption = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("Drop");
            e.DragUIOverride.IsCaptionVisible = true;
            e.DragUIOverride.IsContentVisible = true;
            e.DragUIOverride.IsGlyphVisible = true;
        }

        private void SoundGridView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            //switchSelectionMode();
        }

        private void SoundGridView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            //switchSelectionMode();
        }

        private void SoundGridView_Holding(object sender, HoldingRoutedEventArgs e)
        {
            //switchSelectionMode();
        }

        private void switchSelectionMode()
        {
            if ((App.Current as App)._itemViewHolder.selectionMode == ListViewSelectionMode.None)
            {
                (App.Current as App)._itemViewHolder.selectionMode = ListViewSelectionMode.Multiple;
                (App.Current as App)._itemViewHolder.normalOptionsVisibility = Visibility.Collapsed;
                (App.Current as App)._itemViewHolder.multiSelectOptionsVisibility = Visibility.Visible;
            }
        }

        private void SoundGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GridView selectedGridview;
            if (soundsPivotSelected)
            {
                selectedGridview = SoundGridView;
            }
            else if(!soundsPivotSelected && (App.Current as App)._itemViewHolder.showSoundsPivot)
            {
                selectedGridview = FavouriteSoundGridView;
            }
            else
            {
                selectedGridview = SoundGridView2;
            }

            // If no items are selected, disable multi select buttons
            if (selectedGridview.SelectedItems.Count > 0)
            {
                (App.Current as App)._itemViewHolder.multiSelectOptionsEnabled = true;
            }
            else
            {
                (App.Current as App)._itemViewHolder.multiSelectOptionsEnabled = false;
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
            if(DrawerContentGrid.Height > this.ActualHeight)
            {
                DrawerContentGrid.Height = this.ActualHeight;
            }
        }

        private void HandleGrid_OnManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            var themeBrush = Application.Current.Resources["AppBarBorderThemeBrush"] as SolidColorBrush;

            if (themeBrush != null) HandleGrid.Background = themeBrush;
        }

        private void HandleGrid_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (DrawerContentGrid.Height == this.ActualHeight)
            {
                DrawerContentGrid.Height = HandleGrid.ActualHeight;
            }
            else if (DrawerContentGrid.Height > this.ActualHeight/2)
            {
                DrawerContentGrid.Height = this.ActualHeight;
            }else if (DrawerContentGrid.Height <= HandleGrid.ActualHeight)
            {
                DrawerContentGrid.Height = this.ActualHeight;
            }
            else
            {
                DrawerContentGrid.Height = HandleGrid.ActualHeight;
            }
        }

        public static void playSound(Sound sound)
        {
            MediaPlayer player = new MediaPlayer();
            MediaPlaybackList mediaPlaybackList = new MediaPlaybackList();

            MediaPlaybackItem mediaPlaybackItem = new MediaPlaybackItem(MediaSource.CreateFromStorageFile(sound.AudioFile));

            MediaItemDisplayProperties props = mediaPlaybackItem.GetDisplayProperties();
            props.Type = MediaPlaybackType.Music;
            props.MusicProperties.Title = sound.Name;
            if (sound.Category != null)
            {
                props.MusicProperties.Artist = sound.Category.Name;
            }
            if(sound.ImageFile != null)
            {
                props.Thumbnail = RandomAccessStreamReference.CreateFromFile(sound.ImageFile);
            }

            mediaPlaybackItem.ApplyDisplayProperties(props);
            mediaPlaybackList.Items.Add(mediaPlaybackItem);
            player.Source = mediaPlaybackList;


            // Set volume
            var localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values["volume"] != null)
            {
                player.Volume = (double)localSettings.Values["volume"];
            }
            else
            {
                localSettings.Values["volume"] = 1.0;
                player.Volume = 1.0;
            }

            // If PlayOneSoundAtOnce is true, remove all sounds from PlayingSounds List
            if((App.Current as App)._itemViewHolder.playOneSoundAtOnce)
            {
                List<PlayingSound> removedPlayingSounds = new List<PlayingSound>();
                foreach (PlayingSound pSound in (App.Current as App)._itemViewHolder.playingSounds)
                {
                    removedPlayingSounds.Add(pSound);
                }

                RemoveSoundsFromPlayingSoundsList(removedPlayingSounds);
            }

            PlayingSound playingSound = new PlayingSound(sound, player);
            (App.Current as App)._itemViewHolder.playingSounds.Add(playingSound);
        }

        public static void playSounds(List<Sound> sounds, int repetitions)
        {
            if(sounds.Count < 1)
            {
                return;
            }
            MediaPlayer player = new MediaPlayer();
            MediaPlaybackList mediaPlaybackList = new MediaPlaybackList();

            foreach(Sound sound in sounds)
            {
                MediaPlaybackItem mediaPlaybackItem = new MediaPlaybackItem(MediaSource.CreateFromStorageFile(sound.AudioFile));

                MediaItemDisplayProperties props = mediaPlaybackItem.GetDisplayProperties();
                props.Type = MediaPlaybackType.Music;
                props.MusicProperties.Title = sound.Name;
                if (sound.Category != null)
                {
                    props.MusicProperties.Artist = sound.Category.Name;
                }
                
                if (sound.ImageFile != null)
                {
                    props.Thumbnail = RandomAccessStreamReference.CreateFromFile(sound.ImageFile);
                }

                mediaPlaybackItem.ApplyDisplayProperties(props);

                mediaPlaybackList.Items.Add(mediaPlaybackItem);
            }
            
            player.Source = mediaPlaybackList;


            // Set volume
            var localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values["volume"] != null)
            {
                player.Volume = (double)localSettings.Values["volume"];
            }
            else
            {
                localSettings.Values["volume"] = 1.0;
                player.Volume = 1.0;
            }

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

            PlayingSound playingSound = new PlayingSound(sounds, player, repetitions);
            (App.Current as App)._itemViewHolder.playingSounds.Add(playingSound);
        }

        public static void RemovePlayingSound(PlayingSound playingSound)
        {
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

        private async Task ShowAllSounds()
        {
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;
            (App.Current as App)._itemViewHolder.searchQuery = "";
            (App.Current as App)._itemViewHolder.editButtonVisibility = Visibility.Collapsed;
            (App.Current as App)._itemViewHolder.title = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("AllSounds");
            await SoundManager.GetAllSounds();
        }

        private async Task ShowCategory(Category category)
        {
            (App.Current as App)._itemViewHolder.title = WebUtility.HtmlDecode(category.Name);
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
            (App.Current as App)._itemViewHolder.editButtonVisibility = Visibility.Visible;
            await SoundManager.GetSoundsByCategory(category);
        }

        public static void StartPlaySoundsSuccessively(int rounds, bool allSounds)
        {
            // If allSounds is true, play all sounds. Else, play only selected sounds
            if (allSounds)
            {
                // If favourite sounds is selected or Favourite sounds are hiding
                if (soundsPivotSelected || !(App.Current as App)._itemViewHolder.showSoundsPivot)
                {
                    playSounds((App.Current as App)._itemViewHolder.sounds.ToList(), rounds);
                }
                else
                {
                    playSounds((App.Current as App)._itemViewHolder.favouriteSounds.ToList(), rounds);
                }
            }
            else
            {
                playSounds((App.Current as App)._itemViewHolder.selectedSounds, rounds);
            }
        }

        public static void PlayAllSoundsSimultaneously()
        {
            bool oldPlayOneSoundAtOnce = (App.Current as App)._itemViewHolder.playOneSoundAtOnce;
            (App.Current as App)._itemViewHolder.playOneSoundAtOnce = false;

            // Select favourite sounds list if favourite sounds tab is selected
            ObservableCollection<Sound> sounds = new ObservableCollection<Sound>();
            if (soundsPivotSelected || !(App.Current as App)._itemViewHolder.showSoundsPivot)
            {
                sounds = (App.Current as App)._itemViewHolder.sounds;
            }
            else
            {
                sounds = (App.Current as App)._itemViewHolder.favouriteSounds;
            }

            foreach (Sound sound in sounds)
            {
                SoundPage.playSound(sound);
            }
            (App.Current as App)._itemViewHolder.playOneSoundAtOnce = oldPlayOneSoundAtOnce;
        }

        private void PlayAllSoundsSimultaneouslyFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            PlayAllSoundsSimultaneously();
        }

        private void PlayAllSoundsSuccessivelyFlyoutItem_1x_Click(object sender, RoutedEventArgs e)
        {
            StartPlaySoundsSuccessively(1, true);
        }

        private void PlayAllSoundsSuccessivelyFlyoutItem_2x_Click(object sender, RoutedEventArgs e)
        {
            StartPlaySoundsSuccessively(2, true);
        }

        private void PlayAllSoundsSuccessivelyFlyoutItem_5x_Click(object sender, RoutedEventArgs e)
        {
            StartPlaySoundsSuccessively(5, true);
        }

        private void PlayAllSoundsSuccessivelyFlyoutItem_10x_Click(object sender, RoutedEventArgs e)
        {
            StartPlaySoundsSuccessively(10, true);
        }

        private void PlayAllSoundsSuccessivelyFlyoutItem_endless_Click(object sender, RoutedEventArgs e)
        {
            StartPlaySoundsSuccessively(int.MaxValue, true);
        }

        private async void SoundsPivot_PivotItemLoaded(Pivot sender, PivotItemEventArgs args)
        {
            await FileManager.UpdateGridView();
            (App.Current as App)._itemViewHolder.selectedSounds.Clear();
            soundsPivotSelected = !soundsPivotSelected;
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
                optimizedWidth = 300;
            }

            ItemsWrapGrid appItemsPanel = (ItemsWrapGrid)gridView.ItemsPanelRoot;

            double margin = 12.0;
            var number = (int)e.NewSize.Width / (int)optimizedWidth;
            appItemsPanel.ItemWidth = (e.NewSize.Width - margin) / (double)number;
        }



        // Content Dialog Methods
        private async void CategoryDeleteButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var deleteCategoryContentDialog = ContentDialogs.CreateDeleteCategoryContentDialogAsync();
            deleteCategoryContentDialog.PrimaryButtonClick += DeleteCategoryContentDialog_PrimaryButtonClick;
            await deleteCategoryContentDialog.ShowAsync();
        }

        private async void DeleteCategoryContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            await FileManager.deleteCategory((App.Current as App)._itemViewHolder.title);
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;
            (App.Current as App)._itemViewHolder.title = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("AllSounds");
            (App.Current as App)._itemViewHolder.editButtonVisibility = Visibility.Collapsed;

            // Reload page
            await FileManager.CreateCategoriesObservableCollection();
            await SoundManager.GetAllSounds();
        }

        private async void CategoryEditButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var editCategoryContentDialog = await ContentDialogs.CreateEditCategoryContentDialogAsync();
            editCategoryContentDialog.PrimaryButtonClick += EditCategoryContentDialog_PrimaryButtonClick;
            await editCategoryContentDialog.ShowAsync();
        }

        private async void EditCategoryContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // Get categories List and save with new value
            ObservableCollection<Category> categoriesList = await FileManager.GetCategoriesListAsync();

            // Get combobox value
            ComboBoxItem typeItem = (ComboBoxItem)ContentDialogs.IconSelectionComboBox.SelectedItem;
            string icon = typeItem.Content.ToString();

            string newName = ContentDialogs.EditCategoryTextBox.Text;
            string oldName = (App.Current as App)._itemViewHolder.title;

            foreach(Category category in categoriesList)
            {
                if(category.Name == oldName)
                {
                    category.Name = newName;
                    category.Icon = icon;
                }
            }

            await FileManager.SaveCategoriesListAsync(categoriesList);
            await FileManager.renameCategory(oldName, newName);

            (App.Current as App)._itemViewHolder.title = newName;
            await FileManager.UpdateGridView();
            await FileManager.CreateCategoriesObservableCollection();
        }
    }
}
