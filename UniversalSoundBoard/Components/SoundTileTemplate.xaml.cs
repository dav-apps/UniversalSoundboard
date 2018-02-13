using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UniversalSoundBoard.Models;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Microsoft.Toolkit.Uwp.UI.Animations;
using UniversalSoundBoard.DataAccess;
using UniversalSoundBoard.Common;

namespace UniversalSoundBoard.Components
{
    public sealed partial class SoundTileTemplate : UserControl
    {
        public Sound Sound { get { return this.DataContext as Sound; } }
        int moreButtonClicked = 0;
        MenuFlyout OptionsFlyout;
        MenuFlyoutItem SetFavouriteFlyout;
        MenuFlyoutSubItem CategoriesFlyoutSubItem;

        public SoundTileTemplate()
        {
            this.InitializeComponent();
            Loaded += SoundTileTemplate_Loaded;
            this.DataContextChanged += (s, e) => Bindings.Update(); // <-- only working with x:Bind !!!
            setDarkThemeLayout();
            setDataContext();
            createFlyout();
        }

        async void SoundTileTemplate_Loaded(object sender, RoutedEventArgs e)
        {
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

            for (int n = 1; n < (App.Current as App)._itemViewHolder.categories.Count; n++)
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
                    // This does not work
                    var item = new ToggleMenuFlyoutItem();
                    item.Click += CategoryToggleMenuItem_Click;
                    item.Text = (App.Current as App)._itemViewHolder.categories.ElementAt(n).Name;
                    CategoriesFlyoutSubItem.Items.Add(item);
                }
            }

            if(moreButtonClicked == 0)
            {
                // Add some more invisible MenuFlyoutItems
                for(int i = 0; i < 10; i++)
                {
                    ToggleMenuFlyoutItem item = new ToggleMenuFlyoutItem { Visibility = Visibility.Collapsed };
                    item.Click += CategoryToggleMenuItem_Click;
                    CategoriesFlyoutSubItem.Items.Add(item);
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

        private void SetFavouritesMenuItemText()
        {
            var loader = new Windows.ApplicationModel.Resources.ResourceLoader();

            if (Sound.Favourite)
                SetFavouriteFlyout.Text = loader.GetString("SoundTile-UnsetFavourite");
            else
                SetFavouriteFlyout.Text = loader.GetString("SoundTile-SetFavourite");
        }

        private void createFlyout()
        {
            var loader = new Windows.ApplicationModel.Resources.ResourceLoader();

            OptionsFlyout = new MenuFlyout();
            OptionsFlyout.Opened += Flyout_Opened;
            CategoriesFlyoutSubItem = new MenuFlyoutSubItem { Text = loader.GetString("SoundTile-Category") };
            SetFavouriteFlyout = new MenuFlyoutItem();
            SetFavouriteFlyout.Click += SoundTileOptionsSetFavourite_Click;
            MenuFlyoutItem ShareFlyoutItem = new MenuFlyoutItem { Text = loader.GetString("Share") };
            ShareFlyoutItem.Click += ShareFlyoutItem_Click;
            MenuFlyoutSeparator separator = new MenuFlyoutSeparator();
            MenuFlyoutItem SetImageFlyout = new MenuFlyoutItem { Text = loader.GetString("SoundTile-ChangeImage") };
            SetImageFlyout.Click += SoundTileOptionsSetImage_Click;
            MenuFlyoutItem RenameFlyout = new MenuFlyoutItem { Text = loader.GetString("SoundTile-Rename") };
            RenameFlyout.Click += SoundTileOptionsRename_Click;
            MenuFlyoutItem DeleteFlyout = new MenuFlyoutItem { Text = loader.GetString("SoundTile-Delete") };
            DeleteFlyout.Click += SoundTileOptionsDelete_Click;

            OptionsFlyout.Items.Add(CategoriesFlyoutSubItem);
            OptionsFlyout.Items.Add(SetFavouriteFlyout);
            OptionsFlyout.Items.Add(ShareFlyoutItem);
            OptionsFlyout.Items.Add(separator);
            OptionsFlyout.Items.Add(SetImageFlyout);
            OptionsFlyout.Items.Add(RenameFlyout);
            OptionsFlyout.Items.Add(DeleteFlyout);
        }

        private void ShareFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested += DataTransferManager_DataRequested;
            DataTransferManager.ShowShareUI();
        }

        private void DataTransferManager_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
            List<StorageFile> sounds = new List<StorageFile>();
            sounds.Add(Sound.AudioFile);

            DataRequest request = args.Request;
            request.Data.SetStorageItems(sounds);

            request.Data.Properties.Title = loader.GetString("ShareDialog-Title");
            request.Data.Properties.Description = Sound.AudioFile.Name;
        }

        private void Flyout_Opened(object sender, object e)
        {
            createCategoriesFlyout();
            SelectRightCategory();
            SetFavouritesMenuItemText();
        }

        private void ContentRoot_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            OptionsFlyout.ShowAt(sender as UIElement, e.GetPosition(sender as UIElement));
        }

        private void ContentRoot_Holding(object sender, HoldingRoutedEventArgs e)
        {
            OptionsFlyout.ShowAt(sender as UIElement, e.GetPosition(sender as UIElement));
        }

        private void ContentRoot_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            SoundTileImage.Scale(1.1f, 1.1f, Convert.ToInt32(SoundTileImage.ActualWidth / 2), Convert.ToInt32(SoundTileImage.ActualHeight / 2), 2000, 0, EasingType.Quintic).Start();
        }

        private void ContentRoot_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            SoundTileImage.Scale(1, 1, Convert.ToInt32(SoundTileImage.ActualWidth / 2), Convert.ToInt32(SoundTileImage.ActualHeight / 2), 1000, 0, EasingType.Quintic).Start();
        }
    }
}
