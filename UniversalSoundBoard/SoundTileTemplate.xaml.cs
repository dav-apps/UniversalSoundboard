using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using UniversalSoundBoard.Model;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.StartScreen;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using static UniversalSoundBoard.Model.Sound;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace UniversalSoundBoard
{
    public sealed partial class SoundTileTemplate : UserControl
    {
        public Sound Sound { get { return this.DataContext as Sound; } }
        int moreButtonClicked = 0;
        public string fallbackValue = "test";

        public SoundTileTemplate()
        {
            this.InitializeComponent();
            Loaded += SoundTileTemplate_Loaded;
            this.DataContextChanged += (s, e) => Bindings.Update(); // <-- only working with x:Bind !!!
            setDarkThemeLayout();
        }

        async void SoundTileTemplate_Loaded(object sender, RoutedEventArgs e)
        {
            setDataContext();
            await FileManager.GetCategoriesListAsync();
            createCategoriesFlyout();
        }

        private void setDataContext()
        {
            ContentRoot.DataContext = (App.Current as App)._itemViewHolder;
        }

        private void setDarkThemeLayout()
        {
            if((App.Current as App).RequestedTheme == ApplicationTheme.Dark)
            {
                ContentRoot.Background = new SolidColorBrush(Colors.Black);
                SoundTileOptionsButton.Background = new SolidColorBrush(Colors.Black);
            }
        }

        private async void SoundTileOptionsSetFavourite_Click(object sender, RoutedEventArgs e)
        {
            bool oldFav = Sound.Favourite;
            bool newFav = !Sound.Favourite;

            // Update all lists containing sounds with the new favourite value
            List<ObservableCollection<Sound>> soundLists = new List<ObservableCollection<Sound>>();
            soundLists.Add((App.Current as App)._itemViewHolder.sounds);
            soundLists.Add((App.Current as App)._itemViewHolder.allSounds);
            soundLists.Add((App.Current as App)._itemViewHolder.favouriteSounds);
            
            foreach (ObservableCollection<Sound> soundList in soundLists)
            {
                var sounds = soundList.Where(s => s.Name == this.Sound.Name);
                if (sounds.Count() > 0)
                {
                    sounds.First().Favourite = newFav;
                }
            }

            if (oldFav)
            {
                // Remove sound from favourites
                (App.Current as App)._itemViewHolder.favouriteSounds.Remove(Sound);
            }
            else
            {
                // Add to favourites
                (App.Current as App)._itemViewHolder.favouriteSounds.Add(Sound);
            }

            FavouriteSymbol.Visibility = newFav ? Visibility.Visible : Visibility.Collapsed;
            SetFavouritesMenuItemText();
            await FileManager.setSoundAsFavourite(this.Sound, newFav);
        }

        private async void SoundTileOptionsSetImage_Click(object sender, RoutedEventArgs e)
        {
            Sound sound = this.Sound;
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation =
                Windows.Storage.Pickers.PickerLocationId.MusicLibrary;
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");

            StorageFile file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                // Application now has read/write access to the picked file
                (App.Current as App)._itemViewHolder.progressRingIsActive = true;
                FileManager.addImage(file, sound);
                FileManager.UpdateLiveTile();
                (App.Current as App)._itemViewHolder.progressRingIsActive = false;
            }
        }

        private async void SoundTileOptionsDelete_Click(object sender, RoutedEventArgs e)
        {
            var DeleteSoundContentDialog = ContentDialogs.CreateDeleteSoundContentDialog(this.Sound.Name);
            DeleteSoundContentDialog.PrimaryButtonClick += DeleteSoundContentDialog_PrimaryButtonClick;

            await DeleteSoundContentDialog.ShowAsync();
        }

        private async void DeleteSoundContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            await FileManager.deleteSound(this.Sound);
            // UpdateGridView nicht in deleteSound, weil es auch in einer Schleife aufgerufen wird (löschen mehrerer Sounds)
            await FileManager.UpdateGridView();
        }

        private async void SoundTileOptionsRename_Click(object sender, RoutedEventArgs e)
        {
            var RenameSoundContentDialog = ContentDialogs.CreateRenameSoundContentDialog(this.Sound);
            RenameSoundContentDialog.PrimaryButtonClick += RenameSoundContentDialog_PrimaryButtonClick;
            await RenameSoundContentDialog.ShowAsync();
        }

        private async void RenameSoundContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // Save new name
            if(ContentDialogs.RenameSoundTextBox.Text != this.Sound.Name)
            {
                await FileManager.renameSound(this.Sound, ContentDialogs.RenameSoundTextBox.Text);
                await FileManager.UpdateGridView();
            }
        }

        private void createCategoriesFlyout()
        {
            foreach (ToggleMenuFlyoutItem item in CategoriesFlyoutSubItem.Items)
            {   // Make each item invisible
                item.Visibility = Visibility.Collapsed;
            }

            for (int n = 0; n < (App.Current as App)._itemViewHolder.categories.Count; n++)
            {
                if (n != 0)
                {
                    if (moreButtonClicked == 0)
                    {   // Create the Flyout the first time
                        var item = new ToggleMenuFlyoutItem();
                        item.Click += CategoryToggleMenuItem_Click;
                        item.Text = (App.Current as App)._itemViewHolder.categories.ElementAt(n).Name;
                        CategoriesFlyoutSubItem.Items.Add(item);
                    }
                    else if (CategoriesFlyoutSubItem.Items.ElementAt(n - 1) != null)
                    {   // If the element is already there, set the new text
                        ((ToggleMenuFlyoutItem)CategoriesFlyoutSubItem.Items.ElementAt(n - 1)).Text = (App.Current as App)._itemViewHolder.categories.ElementAt(n).Name;
                        ((ToggleMenuFlyoutItem)CategoriesFlyoutSubItem.Items.ElementAt(n - 1)).Visibility = Visibility.Visible;
                    }
                    else
                    {
                        var item = new ToggleMenuFlyoutItem();
                        item.Click += CategoryToggleMenuItem_Click;
                        item.Text = (App.Current as App)._itemViewHolder.categories.ElementAt(n).Name;
                        CategoriesFlyoutSubItem.Items.Add(item);
                    }
                }
            }
            moreButtonClicked++;
        }

        private async void CategoryToggleMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sound = this.Sound;
            var selectedItem = (ToggleMenuFlyoutItem) sender;
            string category = selectedItem.Text;
            await sound.setCategory(await FileManager.GetCategoryByNameAsync(category));
            
            unselectAllItemsOfCategoriesFlyoutSubItem();
            selectedItem.IsChecked = true;
        }

        private void SoundTileOptionsButton_Click(object sender, RoutedEventArgs e)
        {
            createCategoriesFlyout();
            SelectRightCategory();
            SetFavouritesMenuItemText();
        }

        private void SetFavouritesMenuItemText()
        {
            if (this.Sound.Favourite)
            {
                SoundTileOptionsSetFavourite.Text = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("SoundTile-UnsetFavourite");
            }
            else
            {
                SoundTileOptionsSetFavourite.Text = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("SoundTile-SetFavourite");
            }
        }

        private void SelectRightCategory()
        {
            unselectAllItemsOfCategoriesFlyoutSubItem();
            foreach (ToggleMenuFlyoutItem item in CategoriesFlyoutSubItem.Items)
            {
                if(this.Sound.Category != null)
                {
                    if (item.Text == this.Sound.Category.Name)
                    {
                        item.IsChecked = true;
                    }
                }
            }
        }

        private void unselectAllItemsOfCategoriesFlyoutSubItem()
        {
            // Clear MenuItems and select selected item
            for (int i = 0; i < CategoriesFlyoutSubItem.Items.Count; i++)
            {
                (CategoriesFlyoutSubItem.Items[i] as ToggleMenuFlyoutItem).IsChecked = false;
            }
        }
    }
}
