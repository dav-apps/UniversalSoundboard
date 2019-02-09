using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UniversalSoundBoard.Models;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Microsoft.Toolkit.Uwp.UI.Animations;
using UniversalSoundBoard.DataAccess;
using UniversalSoundBoard.Common;
using Windows.UI.StartScreen;
using Windows.UI.Notifications;
using System.Threading.Tasks;

namespace UniversalSoundBoard.Components
{
    public sealed partial class SoundTileTemplate : UserControl
    {
        public Sound Sound { get => DataContext as Sound; }
        int moreButtonClicked = 0;
        MenuFlyout OptionsFlyout;
        MenuFlyoutItem SetFavouriteFlyout;
        MenuFlyoutItem SetCategoryFlyoutItem;
        MenuFlyoutItem PinFlyoutItem;
        private bool downloadFileWasCanceled = false;
        private bool downloadFileThrewError = false;
        private bool downloadFileIsExecuting = false;
        private List<StorageFile> soundFiles = new List<StorageFile>();


        public SoundTileTemplate()
        {
            InitializeComponent();
            DataContextChanged += (s, e) => Bindings.Update();
            SetDarkThemeLayout();
            SetDataContext();
            CreateFlyout();
        }

        private void SetDataContext()
        {
            ContentRoot.DataContext = (App.Current as App)._itemViewHolder;
        }
        
        private void SetDarkThemeLayout()
        {
            ContentRoot.Background = new SolidColorBrush(FileManager.GetApplicationThemeColor());
        }

        private async void SoundTileOptionsSetCategoryFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            var itemTemplate = (DataTemplate)Resources["SetCategoryItemTemplate"];
            var SetCategoryContentDialog = ContentDialogs.CreateSetCategoryContentDialog(Sound, itemTemplate);
            SetCategoryContentDialog.PrimaryButtonClick += SetCategoryContentDialog_PrimaryButtonClick;
            await SetCategoryContentDialog.ShowAsync();
        }

        private async void SetCategoryContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // Get the selected categories from the SelectedCategories Dictionary in ContentDialogs
            List<Guid> categoryUuids = new List<Guid>();
            foreach (var entry in ContentDialogs.SelectedCategories)
                if (entry.Value) categoryUuids.Add(entry.Key);
            
            FileManager.SetCategoriesOfSound(Sound.Uuid, categoryUuids);
            await FileManager.UpdateGridView();
        }

        private void SoundTileOptionsSetFavourite_Click(object sender, RoutedEventArgs e)
        {
            bool newFav = !Sound.Favourite;

            // Update all lists containing sounds with the new favourite value
            List<ObservableCollection<Sound>> soundLists = new List<ObservableCollection<Sound>>
            {
                (App.Current as App)._itemViewHolder.Sounds,
                (App.Current as App)._itemViewHolder.AllSounds,
                (App.Current as App)._itemViewHolder.FavouriteSounds
            };

            foreach (ObservableCollection<Sound> soundList in soundLists)
            {
                var sounds = soundList.Where(s => s.Uuid == Sound.Uuid);
                if (sounds.Count() > 0)
                {
                    sounds.First().Favourite = newFav;
                }
            }

            if (newFav)
            {
                // Add to favourites
                (App.Current as App)._itemViewHolder.FavouriteSounds.Add(Sound);
            }
            else
            {
                // Remove sound from favourites
                (App.Current as App)._itemViewHolder.FavouriteSounds.Remove(Sound);
            }

            FavouriteSymbol.Visibility = newFav ? Visibility.Visible : Visibility.Collapsed;
            SetFavouritesMenuItemText();
            FileManager.SetSoundAsFavourite(Sound.Uuid, newFav);
        }
        
        private async void SoundTileOptionsSetImage_Click(object sender, RoutedEventArgs e)
        {
            Sound sound = Sound;
            var picker = new Windows.Storage.Pickers.FileOpenPicker
            {
                ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail,
                SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.MusicLibrary
            };
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");

            StorageFile file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                // Application now has read/write access to the picked file
                (App.Current as App)._itemViewHolder.ProgressRingIsActive = true;
                await FileManager.UpdateImageOfSound(sound.Uuid, file);
                (App.Current as App)._itemViewHolder.ProgressRingIsActive = false;
                await FileManager.UpdateGridView();
            }
        }
        
        private async void SoundTileOptionsDelete_Click(object sender, RoutedEventArgs e)
        {
            var DeleteSoundContentDialog = ContentDialogs.CreateDeleteSoundContentDialog(Sound.Name);
            DeleteSoundContentDialog.PrimaryButtonClick += DeleteSoundContentDialog_PrimaryButtonClick;
            await DeleteSoundContentDialog.ShowAsync();
        }
        
        private async void DeleteSoundContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            await FileManager.DeleteSoundAsync(Sound.Uuid);

            // UpdateGridView nicht in deleteSound, weil es auch in einer Schleife aufgerufen wird (löschen mehrerer Sounds)
            await FileManager.UpdateGridView();
        }
        
        private async void SoundTileOptionsRename_Click(object sender, RoutedEventArgs e)
        {
            var RenameSoundContentDialog = ContentDialogs.CreateRenameSoundContentDialog(Sound);
            RenameSoundContentDialog.PrimaryButtonClick += RenameSoundContentDialog_PrimaryButtonClick;
            await RenameSoundContentDialog.ShowAsync();
        }
        
        private async void RenameSoundContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // Save new name
            if(ContentDialogs.RenameSoundTextBox.Text != Sound.Name)
            {
                FileManager.RenameSound(Sound.Uuid, ContentDialogs.RenameSoundTextBox.Text);
                await FileManager.UpdateGridView();
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

        private void SetPinFlyoutText()
        {
            var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
            bool isPinned = SecondaryTile.Exists(Sound.Uuid.ToString());
            PinFlyoutItem.Text = isPinned ? loader.GetString("Unpin") : loader.GetString("Pin");
        }
        
        private void CreateFlyout()
        {
            var loader = new Windows.ApplicationModel.Resources.ResourceLoader();

            // Check if the sound is pinned to start
            bool isPinned = false;
            if (Sound != null)
                isPinned = SecondaryTile.Exists(Sound.Uuid.ToString());

            OptionsFlyout = new MenuFlyout();
            OptionsFlyout.Opened += Flyout_Opened;
            SetCategoryFlyoutItem = new MenuFlyoutItem { Text = loader.GetString("SoundTile-SetCategory") };
            SetCategoryFlyoutItem.Click += SoundTileOptionsSetCategoryFlyoutItem_Click;
            SetFavouriteFlyout = new MenuFlyoutItem();
            SetFavouriteFlyout.Click += SoundTileOptionsSetFavourite_Click;
            MenuFlyoutItem ShareFlyoutItem = new MenuFlyoutItem { Text = loader.GetString("Share") };
            ShareFlyoutItem.Click += ShareFlyoutItem_Click;
            MenuFlyoutItem ExportFlyoutItem = new MenuFlyoutItem { Text = loader.GetString("Export") };
            ExportFlyoutItem.Click += ExportFlyoutItem_Click;
            PinFlyoutItem = new MenuFlyoutItem();
            PinFlyoutItem.Text = isPinned ? loader.GetString("Unpin") : loader.GetString("Pin");
            PinFlyoutItem.Click += PinFlyoutItem_Click;
            MenuFlyoutSeparator separator = new MenuFlyoutSeparator();
            MenuFlyoutItem SetImageFlyout = new MenuFlyoutItem { Text = loader.GetString("SoundTile-ChangeImage") };
            SetImageFlyout.Click += SoundTileOptionsSetImage_Click;
            MenuFlyoutItem RenameFlyout = new MenuFlyoutItem { Text = loader.GetString("SoundTile-Rename") };
            RenameFlyout.Click += SoundTileOptionsRename_Click;
            MenuFlyoutItem DeleteFlyout = new MenuFlyoutItem { Text = loader.GetString("SoundTile-Delete") };
            DeleteFlyout.Click += SoundTileOptionsDelete_Click;

            OptionsFlyout.Items.Add(SetCategoryFlyoutItem);
            OptionsFlyout.Items.Add(SetFavouriteFlyout);
            OptionsFlyout.Items.Add(ShareFlyoutItem);
            OptionsFlyout.Items.Add(ExportFlyoutItem);
            OptionsFlyout.Items.Add(PinFlyoutItem);
            OptionsFlyout.Items.Add(separator);
            OptionsFlyout.Items.Add(SetImageFlyout);
            OptionsFlyout.Items.Add(RenameFlyout);
            OptionsFlyout.Items.Add(DeleteFlyout);
        }

        private async void PinFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            bool isPinned = SecondaryTile.Exists(Sound.Uuid.ToString());

            // Check if the should be pinned or unpinned
            if (isPinned)
            {
                // Initialize a secondary tile with the same tile ID you want removed
                SecondaryTile toBeDeleted = new SecondaryTile(Sound.Uuid.ToString());

                // And then unpin the tile
                await toBeDeleted.RequestDeleteAsync();
            }
            else
            {
                // Construct the tile
                string tileId = Sound.Uuid.ToString();
                string displayName = Sound.Name;
                string arguments = Sound.Uuid.ToString();
                
                SecondaryTile tile = new SecondaryTile(
                        tileId,
                        displayName,
                        arguments,
                        new Uri("ms-appx:///Assets/Generierte Assets/Square150x150Logo.png"),
                        TileSize.Default);

                tile.VisualElements.ForegroundText = ForegroundText.Light;

                // Enable wide and large tile sizes
                tile.VisualElements.Wide310x150Logo = new Uri("ms-appx:///Assets/Generierte Assets/Wide310x150Logo.png");
                tile.VisualElements.Square310x310Logo = new Uri("ms-appx:///Assets/Generierte Assets/Square310x310Logo.png");

                // Add a small size logo for better looking small tile
                tile.VisualElements.Square71x71Logo = new Uri("ms-appx:///Assets/Generierte Assets/Square71x71Logo.png");

                // Add a unique corner logo for the secondary tile
                tile.VisualElements.Square44x44Logo = new Uri("ms-appx:///Assets/Generierte Assets/Square44x44Logo.png");

                // Show the display name on all sizes
                tile.VisualElements.ShowNameOnSquare150x150Logo = true;
                tile.VisualElements.ShowNameOnWide310x150Logo = true;
                tile.VisualElements.ShowNameOnSquare310x310Logo = true;

                // Add the tile to the Start Menu
                isPinned = await tile.RequestCreateAsync();
                var imageFile = await Sound.GetImageFile();

                if(imageFile != null)
                {
                    // Update the tile with the appropriate image and text
                    NotificationsExtensions.Tiles.TileBinding binding = new NotificationsExtensions.Tiles.TileBinding()
                    {
                        Branding = NotificationsExtensions.Tiles.TileBranding.NameAndLogo,

                        Content = new NotificationsExtensions.Tiles.TileBindingContentAdaptive()
                        {
                            BackgroundImage = new NotificationsExtensions.Tiles.TileBackgroundImage()
                            {
                                Source = imageFile.Path,
                                AlternateText = Sound.Name
                            }
                        }
                    };

                    NotificationsExtensions.Tiles.TileContent content = new NotificationsExtensions.Tiles.TileContent()
                    {
                        Visual = new NotificationsExtensions.Tiles.TileVisual()
                        {
                            TileMedium = binding,
                            TileWide = binding,
                            TileLarge = binding
                        }
                    };

                    var notification = new TileNotification(content.GetXml());
                    TileUpdateManager.CreateTileUpdaterForSecondaryTile(tile.TileId).Update(notification);
                }
            }
        }

        private async void ShareFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            if (!await DownloadFile()) return;

            // Copy the file into the temp folder
            soundFiles.Clear();
            StorageFolder tempFolder = ApplicationData.Current.TemporaryFolder;
            var audioFile = await Sound.GetAudioFile();
            if (audioFile == null) return;
            string ext = Sound.GetAudioFileExtension();

            if (string.IsNullOrEmpty(ext))
                ext = "mp3";

            StorageFile tempFile = await audioFile.CopyAsync(tempFolder, Sound.Name + "." + ext, NameCollisionOption.ReplaceExisting);
            soundFiles.Add(tempFile);
            
            DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested += DataTransferManager_DataRequested;
            DataTransferManager.ShowShareUI();
        }

        private async void ExportFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            if (!await DownloadFile()) return;

            // Open a folder picker and save the file there
            var savePicker = new Windows.Storage.Pickers.FileSavePicker();
            savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.MusicLibrary;

            string ext = Sound.GetAudioFileExtension();

            if (string.IsNullOrEmpty(ext))
                ext = "mp3";

            // Dropdown of file types the user can save the file as
            savePicker.FileTypeChoices.Add("Audio", new List<string>() { "." + ext });
            // Default file name if the user does not type one in or select a file to replace
            savePicker.SuggestedFileName = Sound.Name;

            StorageFile file = await savePicker.PickSaveFileAsync();

            if (file != null)
            {
                CachedFileManager.DeferUpdates(file);
                var audioFile = await Sound.GetAudioFile();
                await FileIO.WriteBytesAsync(file, await FileManager.GetBytesAsync(audioFile));
                await CachedFileManager.CompleteUpdatesAsync(file);
            }
        }

        private async Task<bool> DownloadFile()
        {
            var downloadStatus = Sound.GetAudioFileDownloadStatus();
            if (downloadStatus == DownloadStatus.NoFileOrNotLoggedIn) return false;

            if (downloadStatus != DownloadStatus.Downloaded)
            {
                // Download the file and show the download dialog
                downloadFileIsExecuting = true;
                Progress<int> progress = new Progress<int>(FileDownloadProgress);
                Sound.DownloadFile(progress);

                ContentDialogs.CreateDownloadFileContentDialog(Sound.Name + "." + Sound.GetAudioFileExtension());
                ContentDialogs.downloadFileProgressBar.IsIndeterminate = true;
                ContentDialogs.DownloadFileContentDialog.SecondaryButtonClick += DownloadFileContentDialog_SecondaryButtonClick;
                await ContentDialogs.DownloadFileContentDialog.ShowAsync();
            }

            if (downloadFileWasCanceled)
            {
                downloadFileWasCanceled = false;
                return false;
            }

            if (downloadFileThrewError)
            {
                var errorContentDialog = ContentDialogs.CreateDownloadFileErrorContentDialog();
                await errorContentDialog.ShowAsync();
                downloadFileThrewError = false;
                return false;
            }
            return true;
        }

        private void DownloadFileContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            downloadFileWasCanceled = true;
            downloadFileIsExecuting = false;
        }
        
        private void FileDownloadProgress(int value)
        {
            if (!downloadFileIsExecuting) return;

            if (value < 0)
            {
                // There was an error
                downloadFileThrewError = true;
                downloadFileIsExecuting = false;
                ContentDialogs.DownloadFileContentDialog.Hide();
            }
            else if (value > 100)
            {
                // Hide the download dialog
                ContentDialogs.DownloadFileContentDialog.Hide();
            }
        }
        
        private void DataTransferManager_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            if (soundFiles.Count == 0) return;
            var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
            
            DataRequest request = args.Request;
            request.Data.SetStorageItems(soundFiles);
            request.Data.Properties.Title = loader.GetString("ShareDialog-Title");
            request.Data.Properties.Description = soundFiles.First().Name;
        }
        
        private void Flyout_Opened(object sender, object e)
        {
            SetFavouritesMenuItemText();
            SetPinFlyoutText();

            moreButtonClicked++;
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
