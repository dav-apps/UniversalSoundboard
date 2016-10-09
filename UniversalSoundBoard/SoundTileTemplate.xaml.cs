using System;
using System.Collections.Generic;
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
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace UniversalSoundBoard
{
    public sealed partial class SoundTileTemplate : UserControl
    {
        public Sound Sound { get { return this.DataContext as Sound; } }

        public SoundTileTemplate()
        {
            this.InitializeComponent();
            Loaded += SoundTileTemplate_Loaded;
            this.DataContextChanged += (s, e) => Bindings.Update(); // <-- only working with x:Bind !!!
            //  this.DataContextChanged += (s, e) => { ViewModel = DataContext as ProfilesViewModel; }
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

        private async void SoundTileOptionsSetImage_Click(object sender, RoutedEventArgs e)
        {
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
                FileManager.addImage(file, this.Sound);
                FileManager.UpdateLiveTile();
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
            await FileManager.renameSound(this.Sound, ContentDialogs.RenameSoundTextBox.Text);
        }

        private void createCategoriesFlyout()
        {
            CategoriesFlyoutSubItem.Items.Clear();

            foreach (Category category in (App.Current as App)._itemViewHolder.categories)
            {
                var item = new ToggleMenuFlyoutItem { Text = category.Name };
                item.Click += CategoryToggleMenuItem_Click;
               /* if (category.Name.Equals(this.Sound.CategoryName))          // Not working properly
                {
                    item.IsChecked = true;
                }*/
                CategoriesFlyoutSubItem.Items.Add(item);
            }
        }

        private async void CategoryToggleMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sound = this.Sound;
            var selectedItem = (ToggleMenuFlyoutItem) sender;
            //Category category = await FileManager.GetCategoryByNameAsync(selectedItem.Text);
            string category = selectedItem.Text;
            sound.CategoryName = category;

            unselectAllItemsOfCategoriesFlyoutSubItem();
            selectedItem.IsChecked = true;

            // Create / get details json and write category into it
            SoundDetails details = new SoundDetails { Category = category };
            await FileManager.WriteFile(await FileManager.createSoundDetailsFileIfNotExistsAsync(this.Sound.Name), details);
        }

        private void CategoriesFlyoutSubItem_GotFocus(object sender, RoutedEventArgs e)
        {
            unselectAllItemsOfCategoriesFlyoutSubItem();
            if((App.Current as App)._itemViewHolder.title != "All Sounds" && (App.Current as App)._itemViewHolder.searchQuery == "")
            {
                foreach(ToggleMenuFlyoutItem item in CategoriesFlyoutSubItem.Items)
                {
                    if(item.Text == (App.Current as App)._itemViewHolder.title)
                    {
                        item.IsChecked = true;
                    }
                }
            }else
            {
                foreach(ToggleMenuFlyoutItem item in CategoriesFlyoutSubItem.Items)
                {
                    if(item.Text == this.Sound.CategoryName)
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
