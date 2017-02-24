using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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
        public SoundPage()
        {
            this.InitializeComponent();
            Loaded += SoundPage_Loaded;

            if((App.Current as App)._itemViewHolder.playingSoundsListVisibility != Visibility.Visible)
            {
                if(Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily != "Windows.Mobile")
                {
                    SecondColDef.Width = new GridLength(0);
                }
            }else
            {
                List<PlayingSound> removedPlayingSounds = new List<PlayingSound>();
                foreach(PlayingSound playingSound in (App.Current as App)._itemViewHolder.playingSounds)
                {
                    if(playingSound.MediaPlayer.PlaybackSession.PlaybackState != MediaPlaybackState.Playing)
                    {
                        removedPlayingSounds.Add(playingSound);
                    }
                }

                RemoveSoundsFromPlayingSoundsList(removedPlayingSounds);
            }
        }

        void SoundPage_Loaded(object sender, RoutedEventArgs e)
        {
            setDataContext();
        }

        private void setDataContext()
        {
            ContentRoot.DataContext = (App.Current as App)._itemViewHolder;
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
                if (items.Any()){

                    StorageFolder folder = ApplicationData.Current.LocalFolder;

                    if (items.Count > 0)
                    {
                        foreach (StorageFile sound in items)
                        {
                            if (sound.ContentType == "audio/wav" || sound.ContentType == "audio/mpeg")
                            {
                                await SoundManager.addSound(sound);
                            }
                        }
                        await FileManager.UpdateGridView();
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
            switchSelectionMode();
        }

        private void SoundGridView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            switchSelectionMode();
        }

        private void SoundGridView_Holding(object sender, HoldingRoutedEventArgs e)
        {
            switchSelectionMode();
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
            // If no items are selected, disable multi select buttons
            if (SoundGridView.SelectedItems.Count > 0)
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
            props.MusicProperties.Artist = sound.CategoryName;
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

            player.Play();

            PlayingSound playingSound = new PlayingSound(sound, player);
            (App.Current as App)._itemViewHolder.playingSounds.Add(playingSound);
        }

        public static void playSounds(List<Sound> sounds, int repetitions)
        {
            MediaPlayer player = new MediaPlayer();
            MediaPlaybackList mediaPlaybackList = new MediaPlaybackList();

            foreach(Sound sound in sounds)
            {
                MediaPlaybackItem mediaPlaybackItem = new MediaPlaybackItem(MediaSource.CreateFromStorageFile(sound.AudioFile));

                MediaItemDisplayProperties props = mediaPlaybackItem.GetDisplayProperties();
                props.Type = MediaPlaybackType.Music;
                props.MusicProperties.Title = sound.Name;
                props.MusicProperties.Artist = sound.CategoryName;
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

            player.Play();

            PlayingSound playingSound = new PlayingSound(sounds.First(), player, repetitions);
            (App.Current as App)._itemViewHolder.playingSounds.Add(playingSound);
        }

        public static void RemovePlayingSound(PlayingSound playingSound)
        {
            playingSound.MediaPlayer.Pause();
            playingSound.MediaPlayer.Source = null;
            (App.Current as App)._itemViewHolder.playingSounds.Remove(playingSound);
        }

        private static void RemoveSoundsFromPlayingSoundsList(List<PlayingSound> removedPlayingSounds)
        {
            for (int i = 0; i < removedPlayingSounds.Count; i++)
            {
                RemovePlayingSound(removedPlayingSounds[i]);
            }
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
            this.Frame.Navigate(this.GetType());
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
            List<Category> categoriesList = await FileManager.GetCategoriesListAsync();

            // Get combobox value
            ComboBoxItem typeItem = (ComboBoxItem)ContentDialogs.IconSelectionComboBox.SelectedItem;
            string icon = typeItem.Content.ToString();

            string newName = ContentDialogs.EditCategoryTextBox.Text;
            string oldName = (App.Current as App)._itemViewHolder.title;

            categoriesList.Find(p => p.Name == oldName).Icon = icon;
            categoriesList.Find(p => p.Name == oldName).Name = newName;

            await FileManager.SaveCategoriesListAsync(categoriesList);
            await FileManager.renameCategory(oldName, newName);

            // Reload page
            this.Frame.Navigate(this.GetType());
            (App.Current as App)._itemViewHolder.title = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("AllSounds");
            (App.Current as App)._itemViewHolder.editButtonVisibility = Visibility.Collapsed;
        }
    }
}
