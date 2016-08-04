using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.ServiceModel.Channels;
using System.Text;
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

            createCategoriesFlyout();
        }

        void SoundTileTemplate_Loaded(object sender, RoutedEventArgs e)
        {
            setDataContext();
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
            }
        }

        private async void SoundTileOptionsDelete_Click(object sender, RoutedEventArgs e)
        {
            await FileManager.deleteSound(this.Sound);
        }

        private async void SoundTileOptionsRename_Click(object sender, RoutedEventArgs e)
        {
            await RenameContentDialog.ShowAsync();
        }

        private void RenameContentDialogTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(RenameContentDialogTextBox.Text.Length < 3)
            {
                RenameContentDialog.IsPrimaryButtonEnabled = false;
            }
            else
            {
                RenameContentDialog.IsPrimaryButtonEnabled = true;
            }
        }

        private async void RenameContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // Save new name
            await FileManager.renameSound(this.Sound, RenameContentDialogTextBox.Text);
        }

        private async void createCategoriesFlyout()
        {
            /*
            foreach(string category in await FileManager.GetCategoriesListAsync())
            {
                var item = new ToggleMenuFlyoutItem { Text = category };
                item.Click += CategoryToggleMenuItem_Click;
                if(this.Sound.Category != null)
                {
                    if(this.Sound.Category == category)
                    {
                        item.IsChecked = true;
                    }
                }
                CategoriesFlyoutSubItem.Items.Add(item);
            }
            */
        }

        private void CategoryToggleMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sound = this.Sound;
            var item = (ToggleMenuFlyoutItem) sender;
            sound.Category = item.Text;
        }
    }
}
