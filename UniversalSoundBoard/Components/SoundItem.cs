using davClassLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UniversalSoundboard.Common;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Models;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI.Notifications;
using Windows.UI.StartScreen;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UniversalSoundboard.Components
{
    public class SoundItem
    {
        private Sound sound;

        public event EventHandler<EventArgs> ImageUpdated;

        private readonly ResourceLoader loader = new ResourceLoader();
        private bool downloadFileWasCanceled = false;
        private bool downloadFileThrewError = false;
        private bool downloadFileIsExecuting = false;
        private List<StorageFile> soundFiles = new List<StorageFile>();

        public SoundItem(Sound sound)
        {
            if (sound == null) return;

            this.sound = sound;
            this.sound.ImageDownloaded += Sound_ImageDownloaded;
        }

        private void Sound_ImageDownloaded(object sender, EventArgs e)
        {
            ImageUpdated?.Invoke(this, new EventArgs());
        }

        public void ShowFlyout(object sender, Point position)
        {
            if (sound == null) return;
            var flyout = new SoundItemOptionsFlyout(sound.Uuid, sound.Favourite, sound.ImageFile != null && sound.ImageFileTableObject != null);

            flyout.SetCategoriesFlyoutItemClick += OptionsFlyout_SetCategoriesFlyoutItemClick;
            flyout.SetFavouriteFlyoutItemClick += OptionsFlyout_SetFavouriteFlyoutItemClick;
            flyout.ShareFlyoutItemClick += OptionsFlyout_ShareFlyoutItemClick;
            flyout.ExportSoundFlyoutItemClick += OptionsFlyout_ExportSoundFlyoutItemClick;
            flyout.ExportImageFlyoutItemClick += OptionsFlyout_ExportImageFlyoutItemClick;
            flyout.PinFlyoutItemClick += OptionsFlyout_PinFlyoutItemClick;
            flyout.SetImageFlyoutItemClick += OptionsFlyout_SetImageFlyoutItemClick;
            flyout.RenameFlyoutItemClick += OptionsFlyout_RenameFlyoutItemClick;
            flyout.DeleteFlyoutItemClick += OptionsFlyout_DeleteFlyoutItemClick;
            flyout.PropertiesFlyoutItemClick += Flyout_PropertiesFlyoutItemClick;

            flyout.ShowAt(sender as UIElement, position);
        }

        #region SetCategories
        private async void OptionsFlyout_SetCategoriesFlyoutItemClick(object sender, RoutedEventArgs e)
        {
            var SetCategoriesContentDialog = ContentDialogs.CreateSetCategoriesContentDialog(new List<Sound> { sound });
            SetCategoriesContentDialog.PrimaryButtonClick += SetCategoriesContentDialog_PrimaryButtonClick;
            await SetCategoriesContentDialog.ShowAsync();
        }

        private async void SetCategoriesContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // Get the selected categories
            List<Guid> categoryUuids = new List<Guid>();
            foreach (var item in ContentDialogs.CategoriesTreeView.SelectedItems)
                categoryUuids.Add((Guid)((CustomTreeViewNode)item).Tag);

            // Update and reload the sound
            await FileManager.SetCategoriesOfSoundAsync(sound.Uuid, categoryUuids);
            await FileManager.ReloadSound(sound.Uuid);
        }
        #endregion

        #region SetFavourite
        private async void OptionsFlyout_SetFavouriteFlyoutItemClick(object sender, RoutedEventArgs e)
        {
            bool newFav = !sound.Favourite;

            await FileManager.SetFavouriteOfSoundAsync(sound.Uuid, newFav);
            await FileManager.ReloadSound(sound.Uuid);
        }
        #endregion

        #region Share
        private async void OptionsFlyout_ShareFlyoutItemClick(object sender, RoutedEventArgs e)
        {
            if (!await DownloadFile()) return;

            // Copy the file into the temp folder
            soundFiles.Clear();
            StorageFolder tempFolder = ApplicationData.Current.TemporaryFolder;
            var audioFile = sound.AudioFile;
            if (audioFile == null) return;
            string ext = sound.GetAudioFileExtension();

            if (string.IsNullOrEmpty(ext))
                ext = "mp3";

            StorageFile tempFile = await audioFile.CopyAsync(tempFolder, string.Format("{0}.{1}", sound.Name, ext), NameCollisionOption.ReplaceExisting);
            soundFiles.Add(tempFile);

            DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested += DataTransferManager_DataRequested;
            DataTransferManager.ShowShareUI();
        }

        private void DataTransferManager_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            if (soundFiles.Count == 0) return;

            DataRequest request = args.Request;
            request.Data.SetStorageItems(soundFiles);
            request.Data.Properties.Title = loader.GetString("ShareDialog-Title");
            request.Data.Properties.Description = soundFiles.First().Name;
        }
        #endregion

        #region Export sound
        private async void OptionsFlyout_ExportSoundFlyoutItemClick(object sender, RoutedEventArgs e)
        {
            if (!await DownloadFile()) return;

            // Open a folder picker and save the file there
            var savePicker = new Windows.Storage.Pickers.FileSavePicker
            {
                SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.MusicLibrary
            };

            string ext = sound.GetAudioFileExtension();

            if (string.IsNullOrEmpty(ext))
                ext = "mp3";

            savePicker.FileTypeChoices.Add("Audio", new List<string>() { "." + ext });
            savePicker.SuggestedFileName = sound.Name;

            StorageFile file = await savePicker.PickSaveFileAsync();

            if (file != null)
            {
                CachedFileManager.DeferUpdates(file);
                var audioFile = sound.AudioFile;
                await FileIO.WriteBytesAsync(file, await FileManager.GetBytesAsync(audioFile));
                await CachedFileManager.CompleteUpdatesAsync(file);
            }
        }
        #endregion

        #region Export image
        private async void OptionsFlyout_ExportImageFlyoutItemClick(object sender, RoutedEventArgs e)
        {
            // Open a folder picker and save the file there
            var savePicker = new Windows.Storage.Pickers.FileSavePicker
            {
                SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary
            };

            string ext = sound.GetImageFileExtension();

            if (string.IsNullOrEmpty(ext))
                ext = "jpg";

            savePicker.FileTypeChoices.Add("Image", new List<string> { "." + ext });
            savePicker.SuggestedFileName = sound.Name;

            StorageFile file = await savePicker.PickSaveFileAsync();

            if (file != null)
            {
                CachedFileManager.DeferUpdates(file);
                var imageFile = sound.ImageFile;
                await FileIO.WriteBytesAsync(file, await FileManager.GetBytesAsync(imageFile));
                await CachedFileManager.CompleteUpdatesAsync(file);
            }
        }
        #endregion

        #region Pin
        private async void OptionsFlyout_PinFlyoutItemClick(object sender, RoutedEventArgs e)
        {
            bool isPinned = SecondaryTile.Exists(sound.Uuid.ToString());

            if (isPinned)
            {
                SecondaryTile tile = new SecondaryTile(sound.Uuid.ToString());
                await tile.RequestDeleteAsync();
            }
            else
            {
                // Initialize the tile
                SecondaryTile tile = new SecondaryTile(
                    sound.Uuid.ToString(),
                    sound.Name,
                    sound.Uuid.ToString(),
                    new Uri("ms-appx:///Assets/Icons/Square150x150Logo.png"),
                    TileSize.Default
                );

                // Set the logos for all tile sizes
                tile.VisualElements.Wide310x150Logo = new Uri("ms-appx:///Assets/Icons/Wide310x150Logo.png");
                tile.VisualElements.Square310x310Logo = new Uri("ms-appx:///Assets/Icons/Square310x310Logo.png");
                tile.VisualElements.Square71x71Logo = new Uri("ms-appx:///Assets/Icons/Square71x71Logo.png");
                tile.VisualElements.Square44x44Logo = new Uri("ms-appx:///Assets/Icons/Square44x44Logo.png");

                // Show the display name on all sizes
                tile.VisualElements.ShowNameOnSquare150x150Logo = true;
                tile.VisualElements.ShowNameOnWide310x150Logo = true;
                tile.VisualElements.ShowNameOnSquare310x310Logo = true;

                // Pin the tile
                if (!await tile.RequestCreateAsync()) return;

                // Update the tile with the image of the sound
                var imageFile = sound.ImageFile;
                if (imageFile == null)
                    imageFile = await StorageFile.GetFileFromApplicationUriAsync(Sound.GetDefaultImageUri());

                NotificationsExtensions.Tiles.TileBinding binding = new NotificationsExtensions.Tiles.TileBinding()
                {
                    Branding = NotificationsExtensions.Tiles.TileBranding.NameAndLogo,

                    Content = new NotificationsExtensions.Tiles.TileBindingContentAdaptive()
                    {
                        BackgroundImage = new NotificationsExtensions.Tiles.TileBackgroundImage()
                        {
                            Source = imageFile.Path,
                            AlternateText = sound.Name
                        }
                    }
                };

                NotificationsExtensions.Tiles.TileContent content = new NotificationsExtensions.Tiles.TileContent()
                {
                    Visual = new NotificationsExtensions.Tiles.TileVisual()
                    {
                        TileSmall = binding,
                        TileMedium = binding,
                        TileWide = binding,
                        TileLarge = binding
                    }
                };

                var notification = new TileNotification(content.GetXml());
                TileUpdateManager.CreateTileUpdaterForSecondaryTile(tile.TileId).Update(notification);
            }
        }
        #endregion

        #region SetImage
        private async void OptionsFlyout_SetImageFlyoutItemClick(object sender, RoutedEventArgs e)
        {
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
                await FileManager.UpdateImageOfSoundAsync(sound.Uuid, file);
                await FileManager.ReloadSound(sound.Uuid);
                ImageUpdated?.Invoke(this, new EventArgs());
            }
        }
        #endregion

        #region Rename
        private async void OptionsFlyout_RenameFlyoutItemClick(object sender, RoutedEventArgs e)
        {
            var RenameSoundContentDialog = ContentDialogs.CreateRenameSoundContentDialog(sound);
            RenameSoundContentDialog.PrimaryButtonClick += RenameSoundContentDialog_PrimaryButtonClick;
            await RenameSoundContentDialog.ShowAsync();
        }

        private async void RenameSoundContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // Save new name
            if (ContentDialogs.RenameSoundTextBox.Text != sound.Name)
            {
                await FileManager.RenameSoundAsync(sound.Uuid, ContentDialogs.RenameSoundTextBox.Text);
                await FileManager.ReloadSound(sound.Uuid);
            }
        }
        #endregion

        #region Delete
        private async void OptionsFlyout_DeleteFlyoutItemClick(object sender, RoutedEventArgs e)
        {
            var DeleteSoundContentDialog = ContentDialogs.CreateDeleteSoundContentDialog(sound.Name);
            DeleteSoundContentDialog.PrimaryButtonClick += DeleteSoundContentDialog_PrimaryButtonClick;
            await DeleteSoundContentDialog.ShowAsync();
        }

        private async void DeleteSoundContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            await FileManager.DeleteSoundAsync(sound.Uuid);
            FileManager.RemoveSound(sound.Uuid);
        }
        #endregion

        #region Properties
        private async void Flyout_PropertiesFlyoutItemClick(object sender, RoutedEventArgs e)
        {
            var propertiesContentDialog = await ContentDialogs.CreatePropertiesContentDialog(sound);
            await propertiesContentDialog.ShowAsync();
        }
        #endregion

        #region File download
        private async Task<bool> DownloadFile()
        {
            var downloadStatus = sound.GetAudioFileDownloadStatus();
            if (downloadStatus == TableObjectFileDownloadStatus.NoFileOrNotLoggedIn) return false;

            if (downloadStatus != TableObjectFileDownloadStatus.Downloaded)
            {
                // Download the file and show the download dialog
                downloadFileIsExecuting = true;
                Progress<(Guid, int)> progress = new Progress<(Guid, int)>(FileDownloadProgress);
                sound.ScheduleAudioFileDownload(progress);

                ContentDialogs.CreateDownloadFileContentDialog($"{sound.Name}.{sound.GetAudioFileExtension()}");
                ContentDialogs.downloadFileProgressBar.IsIndeterminate = true;
                ContentDialogs.DownloadFileContentDialog.CloseButtonClick += DownloadFileContentDialog_CloseButtonClick;
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

        private void FileDownloadProgress((Guid, int) value)
        {
            if (!downloadFileIsExecuting) return;

            if (value.Item2 < 0)
            {
                // There was an error
                downloadFileThrewError = true;
                downloadFileIsExecuting = false;
                ContentDialogs.DownloadFileContentDialog.Hide();
            }
            else if (value.Item2 > 100)
            {
                // Download was successful
                downloadFileThrewError = false;
                downloadFileIsExecuting = false;
                ContentDialogs.DownloadFileContentDialog.Hide();
            }
            else
            {
                ContentDialogs.downloadFileProgressBar.IsIndeterminate = false;
                ContentDialogs.downloadFileProgressBar.Value = value.Item2;
            }
        }

        private void DownloadFileContentDialog_CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            downloadFileWasCanceled = true;
            downloadFileIsExecuting = false;
        }
        #endregion
    }

    public class SoundItemOptionsFlyout
    {
        private readonly ResourceLoader loader;

        private MenuFlyout optionsFlyout;
        private MenuFlyoutItem setFavouriteFlyoutItem;
        private MenuFlyoutItem pinFlyoutItem;

        public event EventHandler<object> FlyoutOpened;
        public event RoutedEventHandler SetCategoriesFlyoutItemClick;
        public event RoutedEventHandler SetFavouriteFlyoutItemClick;
        public event RoutedEventHandler ShareFlyoutItemClick;
        public event RoutedEventHandler ExportSoundFlyoutItemClick;
        public event RoutedEventHandler ExportImageFlyoutItemClick;
        public event RoutedEventHandler PinFlyoutItemClick;
        public event RoutedEventHandler SetImageFlyoutItemClick;
        public event RoutedEventHandler RenameFlyoutItemClick;
        public event RoutedEventHandler DeleteFlyoutItemClick;
        public event RoutedEventHandler PropertiesFlyoutItemClick;

        public SoundItemOptionsFlyout(Guid soundUuid, bool favourite, bool hasImage) {
            loader = new ResourceLoader();

            // Create the flyout
            optionsFlyout = new MenuFlyout();
            optionsFlyout.Opened += (object sender, object e) => FlyoutOpened?.Invoke(sender, e);

            // Set categories
            MenuFlyoutItem setCategoriesFlyoutItem = new MenuFlyoutItem {
                Text = loader.GetString("SoundItemOptionsFlyout-SetCategories"),
                Icon = new FontIcon { Glyph = "\uE179" }
            };
            setCategoriesFlyoutItem.Click += (object sender, RoutedEventArgs e) => SetCategoriesFlyoutItemClick?.Invoke(sender, e);
            optionsFlyout.Items.Add(setCategoriesFlyoutItem);

            // Set favourite
            setFavouriteFlyoutItem = new MenuFlyoutItem {
                Text = loader.GetString(favourite ? "SoundItemOptionsFlyout-UnsetFavourite" : "SoundItemOptionsFlyout-SetFavourite"),
                Icon = new FontIcon { Glyph = favourite ? "\uE195" : "\uE113" }
            };
            setFavouriteFlyoutItem.Click += (object sender, RoutedEventArgs e) => SetFavouriteFlyoutItemClick?.Invoke(sender, e);
            optionsFlyout.Items.Add(setFavouriteFlyoutItem);

            // Share
            MenuFlyoutItem shareFlyoutItem = new MenuFlyoutItem {
                Text = loader.GetString("Share"),
                Icon = new FontIcon { Glyph = "\uE72D" }
            };
            shareFlyoutItem.Click += (object sender, RoutedEventArgs e) => ShareFlyoutItemClick?.Invoke(sender, e);
            optionsFlyout.Items.Add(shareFlyoutItem);

            // Export
            if (hasImage)
            {
                MenuFlyoutSubItem exportFlyoutItem = new MenuFlyoutSubItem
                {
                    Text = loader.GetString("Export"),
                    Icon = new FontIcon { Glyph = "\uEDE1" }
                };

                MenuFlyoutItem exportSoundFileFlyoutItem = new MenuFlyoutItem
                {
                    Text = loader.GetString("Export-Sound")
                };
                exportSoundFileFlyoutItem.Click += (object sender, RoutedEventArgs e) => ExportSoundFlyoutItemClick?.Invoke(sender, e);

                MenuFlyoutItem exportImageFileFlyoutItem = new MenuFlyoutItem
                {
                    Text = loader.GetString("Export-Image")
                };
                exportImageFileFlyoutItem.Click += (object sender, RoutedEventArgs e) => ExportImageFlyoutItemClick?.Invoke(sender, e);

                exportFlyoutItem.Items.Add(exportSoundFileFlyoutItem);
                exportFlyoutItem.Items.Add(exportImageFileFlyoutItem);

                optionsFlyout.Items.Add(exportFlyoutItem);
            }
            else
            {
                MenuFlyoutItem exportFlyoutItem = new MenuFlyoutItem
                {
                    Text = loader.GetString("Export"),
                    Icon = new FontIcon { Glyph = "\uEDE1" }
                };
                exportFlyoutItem.Click += (object sender, RoutedEventArgs e) => ExportSoundFlyoutItemClick?.Invoke(sender, e);
                optionsFlyout.Items.Add(exportFlyoutItem);
            }

            // Pin
            bool pinned = SecondaryTile.Exists(soundUuid.ToString());
            pinFlyoutItem = new MenuFlyoutItem {
                Text = loader.GetString(pinned ? "Unpin" : "Pin"),
                Icon = new FontIcon { Glyph = pinned ? "\uE196" : "\uE141" }
            };
            pinFlyoutItem.Click += (object sender, RoutedEventArgs e) => PinFlyoutItemClick?.Invoke(sender, e);
            optionsFlyout.Items.Add(pinFlyoutItem);

            // Separator
            optionsFlyout.Items.Add(new MenuFlyoutSeparator());

            // Set image
            MenuFlyoutItem setImageFlyoutItem = new MenuFlyoutItem {
                Text = loader.GetString("SoundItemOptionsFlyout-SetImage"),
                Icon = new FontIcon { Glyph = "\uEB9F" }
            };
            setImageFlyoutItem.Click += (object sender, RoutedEventArgs e) => SetImageFlyoutItemClick?.Invoke(sender, e);
            optionsFlyout.Items.Add(setImageFlyoutItem);

            // Rename
            MenuFlyoutItem renameFlyoutItem = new MenuFlyoutItem {
                Text = loader.GetString("SoundItemOptionsFlyout-Rename"),
                Icon = new FontIcon { Glyph = "\uE13E" }
            };
            renameFlyoutItem.Click += (object sender, RoutedEventArgs e) => RenameFlyoutItemClick?.Invoke(sender, e);
            optionsFlyout.Items.Add(renameFlyoutItem);

            // Delete
            MenuFlyoutItem deleteFlyoutItem = new MenuFlyoutItem {
                Text = loader.GetString("SoundItemOptionsFlyout-Delete"),
                Icon = new FontIcon { Glyph = "\uE107" }
            };
            deleteFlyoutItem.Click += (object sender, RoutedEventArgs e) => DeleteFlyoutItemClick?.Invoke(sender, e);
            optionsFlyout.Items.Add(deleteFlyoutItem);

            // Separator
            optionsFlyout.Items.Add(new MenuFlyoutSeparator());

            // Properties
            MenuFlyoutItem propertiesFlyoutItem = new MenuFlyoutItem
            {
                Text = loader.GetString("SoundItemOptionsFlyout-Properties"),
                Icon = new FontIcon { Glyph = "\uE946" }
            };
            propertiesFlyoutItem.Click += (object sender, RoutedEventArgs e) => PropertiesFlyoutItemClick?.Invoke(sender, e);
            optionsFlyout.Items.Add(propertiesFlyoutItem);
        }

        public void ShowAt(UIElement sender, Point position)
        {
            optionsFlyout.ShowAt(sender, position);
        }
    }
}
