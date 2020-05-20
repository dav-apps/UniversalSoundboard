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
using Microsoft.Toolkit.Uwp.UI.Animations;
using UniversalSoundBoard.DataAccess;
using UniversalSoundBoard.Common;
using Windows.UI.StartScreen;
using Windows.UI.Notifications;
using System.Threading.Tasks;
using Windows.Foundation;
using UniversalSoundboard.Components;

namespace UniversalSoundBoard.Components
{
    public sealed partial class SoundTileTemplate : UserControl
    {
        public Sound Sound { get => DataContext as Sound; }
        public SoundItemOptionsFlyout OptionsFlyout;
        private bool downloadFileWasCanceled = false;
        private bool downloadFileThrewError = false;
        private bool downloadFileIsExecuting = false;
        private List<StorageFile> soundFiles = new List<StorageFile>();
        private double visibleNameMarginBottom = 0;
        double soundTileNameContainerHeight = 0;
        int soundTileNameLines = 0;
        double soundTileWidth = 200;


        public SoundTileTemplate()
        {
            InitializeComponent();
            DataContextChanged += (s, e) => Bindings.Update();
            ContentRoot.DataContext = FileManager.itemViewHolder;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateSizes();
            InitOptionsFlyout();
            FileManager.itemViewHolder.SoundTileSizeChangedEvent += ItemViewHolder_SoundTileSizeChangedEvent;
        }

        private void ItemViewHolder_SoundTileSizeChangedEvent(object sender, SizeChangedEventArgs e)
        {
            UpdateSizes();
        }

        private void InitOptionsFlyout()
        {
            if (Sound == null) return;
            OptionsFlyout = new SoundItemOptionsFlyout(Sound.Uuid, Sound.Favourite);

            OptionsFlyout.FlyoutOpened += Flyout_Opened;
            OptionsFlyout.SetCategoryFlyoutItemClick += SoundTileOptionsSetCategoryFlyoutItem_Click;
            OptionsFlyout.SetFavouriteFlyoutItemClick += SoundTileOptionsSetFavourite_Click;
            OptionsFlyout.ShareFlyoutItemClick += ShareFlyoutItem_Click;
            OptionsFlyout.ExportFlyoutItemClick += ExportFlyoutItem_Click;
            OptionsFlyout.PinFlyoutItemClick += PinFlyoutItem_Click;
            OptionsFlyout.SetImageFlyoutItemClick += SoundTileOptionsSetImage_Click;
            OptionsFlyout.RenameFlyoutItemClick += SoundTileOptionsRename_Click;
            OptionsFlyout.DeleteFlyoutItemClick += SoundTileOptionsDelete_Click;
        }

        private void UpdateSizes()
        {
            SetupNameAnimations();
            UserControlClipRect.Rect = new Rect(0, 0, FileManager.itemViewHolder.SoundTileWidth, 200);
            soundTileWidth = FileManager.itemViewHolder.SoundTileWidth;
            Bindings.Update();
        }

        private void SetupNameAnimations()
        {
            soundTileNameContainerHeight = SoundTileName.ActualHeight;
            Bindings.Update();

            // Calculate the margin that the text needs to move up to be completely visible
            double lineHeight = 26.6015625;
            soundTileNameLines = Convert.ToInt32(SoundTileName.ActualHeight / lineHeight);
            visibleNameMarginBottom = -(soundTileNameLines - 1) * lineHeight;

            SoundTileNameContainer.Margin = new Thickness(0, 0, 0, visibleNameMarginBottom);
            ShowNameStoryboardAnimation.To = visibleNameMarginBottom;

            SoundTileNameContainerAcrylicBrush.TintColor = FileManager.GetApplicationThemeColor();
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
            // Scale the image
            SoundTileImage.Scale(1.1f, 1.1f, Convert.ToInt32(SoundTileImage.ActualWidth / 2), Convert.ToInt32(SoundTileImage.ActualHeight / 2), 400, 0, EasingType.Quintic).Start();

            // Show the animation of the name
            ShowNameStoryboard.Begin();
        }

        private void ContentRoot_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            // Scale the image
            SoundTileImage.Scale(1, 1, Convert.ToInt32(SoundTileImage.ActualWidth / 2), Convert.ToInt32(SoundTileImage.ActualHeight / 2), 400, 0, EasingType.Quintic).Start();

            // Show the animation of the name
            HideNameStoryboard.Begin();
        }

        private void Flyout_Opened(object sender, object e)
        {
            SetFavouritesMenuItemText();
            SetPinFlyoutText();
        }

        private async void SoundTileOptionsSetCategoryFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            var itemTemplate = (DataTemplate)Resources["SetCategoryItemTemplate"];
            List<Sound> soundsList = new List<Sound> { Sound };
            var SetCategoryContentDialog = ContentDialogs.CreateSetCategoryContentDialog(soundsList, itemTemplate);
            SetCategoryContentDialog.PrimaryButtonClick += SetCategoryContentDialog_PrimaryButtonClick;
            await SetCategoryContentDialog.ShowAsync();
        }

        private async void SetCategoryContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // Get the selected categories from the SelectedCategories Dictionary in ContentDialogs
            List<Guid> categoryUuids = new List<Guid>();
            foreach (var entry in ContentDialogs.SelectedCategories)
                if (entry.Value) categoryUuids.Add(entry.Key);

            await FileManager.SetCategoriesOfSoundAsync(Sound.Uuid, categoryUuids);
            await FileManager.UpdateGridViewAsync();
        }

        private async void SoundTileOptionsSetFavourite_Click(object sender, RoutedEventArgs e)
        {
            bool newFav = !Sound.Favourite;

            // Update all lists containing sounds with the new favourite value
            List<ObservableCollection<Sound>> soundLists = new List<ObservableCollection<Sound>>
            {
                FileManager.itemViewHolder.Sounds,
                FileManager.itemViewHolder.AllSounds,
                FileManager.itemViewHolder.FavouriteSounds
            };

            foreach (ObservableCollection<Sound> soundList in soundLists)
            {
                var sounds = soundList.Where(s => s.Uuid == Sound.Uuid);
                if (sounds.Count() > 0)
                    sounds.First().Favourite = newFav;
            }

            if (newFav)
            {
                // Add to favourites
                FileManager.itemViewHolder.FavouriteSounds.Add(Sound);
            }
            else
            {
                // Remove sound from favourites
                FileManager.itemViewHolder.FavouriteSounds.Remove(Sound);
            }

            //FavouriteSymbol.Visibility = newFav ? Visibility.Visible : Visibility.Collapsed;
            SetFavouritesMenuItemText();
            await FileManager.SetSoundAsFavouriteAsync(Sound.Uuid, newFav);
        }

        private async void ShareFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            if (!await DownloadFile()) return;

            // Copy the file into the temp folder
            soundFiles.Clear();
            StorageFolder tempFolder = ApplicationData.Current.TemporaryFolder;
            var audioFile = await Sound.GetAudioFileAsync();
            if (audioFile == null) return;
            string ext = await Sound.GetAudioFileExtensionAsync();

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
            var savePicker = new Windows.Storage.Pickers.FileSavePicker
            {
                SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.MusicLibrary
            };

            string ext = await Sound.GetAudioFileExtensionAsync();

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
                var audioFile = await Sound.GetAudioFileAsync();
                await FileIO.WriteBytesAsync(file, await FileManager.GetBytesAsync(audioFile));
                await CachedFileManager.CompleteUpdatesAsync(file);
            }
        }

        private async void PinFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            bool isPinned = SecondaryTile.Exists(Sound.Uuid.ToString());

            if (isPinned)
            {
                SecondaryTile tile = new SecondaryTile(Sound.Uuid.ToString());
                await tile.RequestDeleteAsync();
            }
            else
            {
                // Initialize the tile
                SecondaryTile tile = new SecondaryTile(
                    Sound.Uuid.ToString(),
                    Sound.Name,
                    Sound.Uuid.ToString(),
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
                var imageFile = await Sound.GetImageFileAsync();
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
                            AlternateText = Sound.Name
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
                FileManager.itemViewHolder.ProgressRingIsActive = true;
                await FileManager.UpdateImageOfSoundAsync(sound.Uuid, file);
                FileManager.itemViewHolder.ProgressRingIsActive = false;
                await FileManager.UpdateGridViewAsync();
            }
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
            if (ContentDialogs.RenameSoundTextBox.Text != Sound.Name)
            {
                await FileManager.RenameSoundAsync(Sound.Uuid, ContentDialogs.RenameSoundTextBox.Text);
                await FileManager.UpdateGridViewAsync();
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
            await FileManager.UpdateGridViewAsync();
        }

        private void SetFavouritesMenuItemText()
        {
            OptionsFlyout.SetFavourite(Sound.Favourite);
        }

        private void SetPinFlyoutText()
        {
            OptionsFlyout.UpdatePinText();
        }

        private async Task<bool> DownloadFile()
        {
            var downloadStatus = await Sound.GetAudioFileDownloadStatusAsync();
            if (downloadStatus == DownloadStatus.NoFileOrNotLoggedIn) return false;

            if (downloadStatus != DownloadStatus.Downloaded)
            {
                // Download the file and show the download dialog
                downloadFileIsExecuting = true;
                Progress<int> progress = new Progress<int>(FileDownloadProgress);
                await Sound.DownloadFileAsync(progress);

                ContentDialogs.CreateDownloadFileContentDialog(Sound.Name + "." + Sound.GetAudioFileExtensionAsync());
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
    }
}
