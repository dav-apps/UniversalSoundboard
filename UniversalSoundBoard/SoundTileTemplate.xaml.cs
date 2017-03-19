using System;
using System.Collections.Generic;
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
            await FileManager.renameSound(this.Sound, ContentDialogs.RenameSoundTextBox.Text);
        }

        private async void SoundTileOptionsPin_Click(object sender, RoutedEventArgs e)
        {
            string secondaryTileId = "Kachel";

            // Prepare package images for all four tile sizes in our tile to be pinned as well as for the square30x30 logo used in the Apps view.

            Uri square150x150Logo = new Uri("ms-appx:///Assets/Images/default.png");
            Uri wide310x150Logo = new Uri("ms-appx:///Assets/Images/default.png");
            Uri square310x310Logo = new Uri("ms-appx:///Assets/Images/default.png");
            Uri square30x30Logo = new Uri("ms-appx:///Assets/Images/default.png");

            if (this.Sound.ImageFile != null)
            {
                //square150x150Logo = new Uri(this.Sound.ImageFile.Path);
                square150x150Logo = new Uri("file:///C:/default.png", UriKind.Absolute);
                //wide310x150Logo = new Uri(this.Sound.ImageFile.Path);
                //square310x310Logo = new Uri(this.Sound.ImageFile.Path);
                //square30x30Logo = new Uri(this.Sound.ImageFile.Path);
            }

            Debug.WriteLine(Uri.IsWellFormedUriString("file:///C:/default.png", UriKind.Absolute));

            // During creation of secondary tile, an application may set additional arguments on the tile that will be passed in during activation.
            // These arguments should be meaningful to the application. In this sample, we'll pass in the date and time the secondary tile was pinned.
            string tileActivationArguments = secondaryTileId + " WasPinnedAt=" + DateTime.Now.ToLocalTime().ToString();

            // Create a Secondary tile with all the required arguments.
            // Note the last argument specifies what size the Secondary tile should show up as by default in the Pin to start fly out.
            // It can be set to TileSize.Square150x150, TileSize.Wide310x150, or TileSize.Default.  
            // If set to TileSize.Wide310x150, then the asset for the wide size must be supplied as well.
            // TileSize.Default will default to the wide size if a wide size is provided, and to the medium size otherwise. 
            //  SecondaryTile secondaryTile = new SecondaryTile(secondaryTileId,
            //    this.Sound.Name,
            //                                                tileActivationArguments,
            //                                                square150x150Logo,
            //                                                TileSize.Square150x150);


            SecondaryTile secondaryTile = new SecondaryTile();
            secondaryTile.TileId = secondaryTileId;
            secondaryTile.DisplayName = this.Sound.Name;
            secondaryTile.Arguments = tileActivationArguments;
            secondaryTile.VisualElements.Square150x150Logo = square150x150Logo;
            //secondaryTile.VisualElements.Square150x150Logo = this.Sound.Image.UriSource;

            if (!(Windows.Foundation.Metadata.ApiInformation.IsTypePresent(("Windows.Phone.UI.Input.HardwareButtons"))))
            {
                secondaryTile.VisualElements.Wide310x150Logo = wide310x150Logo;
                secondaryTile.VisualElements.Square310x310Logo = square310x310Logo;
            }

            // The display of the secondary tile name can be controlled for each tile size.
            // The default is false.
            secondaryTile.VisualElements.ShowNameOnSquare150x150Logo = true;

            if (!(Windows.Foundation.Metadata.ApiInformation.IsTypePresent(("Windows.Phone.UI.Input.HardwareButtons"))))
            {
                secondaryTile.VisualElements.ShowNameOnWide310x150Logo = true;
                secondaryTile.VisualElements.ShowNameOnSquare310x310Logo = true;
            }

            // Specify a foreground text value.
            // The tile background color is inherited from the parent unless a separate value is specified.
            secondaryTile.VisualElements.ForegroundText = ForegroundText.Dark;

            // Set this to false if roaming doesn't make sense for the secondary tile.
            // The default is true;
            secondaryTile.RoamingEnabled = false;

            // OK, the tile is created and we can now attempt to pin the tile.
            await secondaryTile.RequestCreateAsync();
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
            string category = selectedItem.Text;
            await sound.setCategory(category);
            
            unselectAllItemsOfCategoriesFlyoutSubItem();
            selectedItem.IsChecked = true;
        }

        private void CategoriesFlyoutSubItem_GotFocus(object sender, RoutedEventArgs e)
        {
            unselectAllItemsOfCategoriesFlyoutSubItem();
            foreach (ToggleMenuFlyoutItem item in CategoriesFlyoutSubItem.Items)
            {
                if (item.Text == this.Sound.CategoryName)
                {
                    item.IsChecked = true;
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
