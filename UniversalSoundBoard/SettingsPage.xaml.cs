using SharpCompress.Archives;
using SharpCompress.Writers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using UniversalSoundBoard.Model;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Notifications;
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
    public sealed partial class SettingsPage : Page
    {
        bool darkThemeToggledAtBeginning;

        public SettingsPage()
        {
            this.InitializeComponent();
            AdjustLayout();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            setDataContext();

            darkThemeToggledAtBeginning = (App.Current as App).RequestedTheme == ApplicationTheme.Dark ? true : false;
            setToggleMessageVisibility();
            await setSoundBoardSizeText();
        }

        private void setDataContext()
        {
            ContentRoot.DataContext = (App.Current as App)._itemViewHolder;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            setThemeToggle();
            setLiveTileToggle();
            setPlayingSoundsListVisibilityToggle();
            setPlayOneSoundAtOnceToggle();
            setShowCategoryIconToggle();
            setShowSoundsPivotToggle();
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            AdjustLayout();
        }

        private void AdjustLayout()
        {
            if (Window.Current.Bounds.Width < FileManager.mobileMaxWidth)       // If user in on mobile
            {
                TitleRow.Height = GridLength.Auto;
            }
            else
            {
                TitleRow.Height = new GridLength(0);
            }
        }

        private void setThemeToggle()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values["darkTheme"] != null)
            {
                ThemeToggle.IsOn = (bool)localSettings.Values["darkTheme"];
            }
            else if (Application.Current.RequestedTheme == ApplicationTheme.Dark)
            {
                ThemeToggle.IsOn = true;
            }
            ThemeChangeMessageTextBlock.Visibility = Visibility.Collapsed;
        }

        private void setLiveTileToggle()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            LiveTileToggle.IsOn = (bool)localSettings.Values["liveTile"];
        }

        private void setPlayingSoundsListVisibilityToggle()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            PlayingSoundsListToggle.IsOn = (bool)localSettings.Values["playingSoundsListVisible"];
        }

        private void setPlayOneSoundAtOnceToggle()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            PlayOneSoundAtOnceToggle.IsOn = (bool)localSettings.Values["playOneSoundAtOnce"];
        }

        private void setShowCategoryIconToggle()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            ShowCategoryToggle.IsOn = (bool)localSettings.Values["showCategoryIcon"];
        }

        private void setShowSoundsPivotToggle()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            ShowSoundsPivotToggle.IsOn = (bool)localSettings.Values["showSoundsPivot"];
        }

        private void setToggleMessageVisibility()
        {
            if (darkThemeToggledAtBeginning != ThemeToggle.IsOn)
            {
                ThemeChangeMessageTextBlock.Visibility = Visibility.Visible;
            }
            else
            {
                ThemeChangeMessageTextBlock.Visibility = Visibility.Collapsed;
            }
        }

        private async Task setSoundBoardSizeText()
        {
            if ((App.Current as App)._itemViewHolder.progressRingIsActive)
            {
                await Task.Delay(1000);
                await setSoundBoardSizeText();
            }

            float totalSize = 0;
            foreach (Sound sound in (App.Current as App)._itemViewHolder.allSounds)
            {
                float size;
                size = await FileManager.GetFileSizeInGBAsync(sound.AudioFile);
                if (sound.ImageFile != null)
                {
                    size += await FileManager.GetFileSizeInGBAsync(sound.ImageFile);
                }
                totalSize += size;
            }

            SoundBoardSizeTextBlock.Text = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("SettingsSoundBoardSize") + totalSize.ToString("n2") + "GB.";
        }

        private void LiveTileToggle_Toggled(object sender, RoutedEventArgs e)
        {
            var localSettings = ApplicationData.Current.LocalSettings;

            // Create a simple setting
            localSettings.Values["liveTile"] = LiveTileToggle.IsOn;
            if (!LiveTileToggle.IsOn)
            {
                TileUpdateManager.CreateTileUpdaterForApplication().Clear();
            }
            else
            {
                FileManager.UpdateLiveTile();
            }
        }

        private void PlayingSoundsListToggle_Toggled(object sender, RoutedEventArgs e)
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["playingSoundsListVisible"] = PlayingSoundsListToggle.IsOn;
            (App.Current as App)._itemViewHolder.playingSoundsListVisibility = PlayingSoundsListToggle.IsOn ? Visibility.Visible : Visibility.Collapsed;
        }

        private void PlayOneSoundAtOnceToggle_Toggled(object sender, RoutedEventArgs e)
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["playOneSoundAtOnce"] = PlayOneSoundAtOnceToggle.IsOn;
            (App.Current as App)._itemViewHolder.playOneSoundAtOnce = PlayOneSoundAtOnceToggle.IsOn;
        }

        private void ThemeToggle_Toggled(object sender, RoutedEventArgs e)
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["darkTheme"] = ThemeToggle.IsOn;
            setToggleMessageVisibility();
        }

        private void ShowCategoryToggle_Toggled(object sender, RoutedEventArgs e)
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["showCategoryIcon"] = ShowCategoryToggle.IsOn;
            (App.Current as App)._itemViewHolder.showCategoryIcon = ShowCategoryToggle.IsOn;
        }

        private void ShowSoundsPivotToggle_Toggled(object sender, RoutedEventArgs e)
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["showSoundsPivot"] = ShowSoundsPivotToggle.IsOn;
            (App.Current as App)._itemViewHolder.showSoundsPivot = ShowSoundsPivotToggle.IsOn;
        }

        private async void ExportDataButton_Click(object sender, RoutedEventArgs e)
        {
            var folderPicker = new Windows.Storage.Pickers.FolderPicker();
            folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Downloads;
            folderPicker.FileTypeFilter.Add("*");

            StorageFolder folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                // Application now has read/write access to all contents in the picked folder
                // (including other sub-folder contents)
                Windows.Storage.AccessCache.StorageApplicationPermissions.
                FutureAccessList.AddOrReplace("PickedFolderToken", folder);

                ExportDataProgressRing.IsActive = true;
                await ExportData(folder);
            }
        }

        private async Task ExportData(StorageFolder destinationFolder)
        {
            await FileManager.createDataFolderAndJsonFileIfNotExistsAsync();
            await FileManager.createDetailsFolderIfNotExistsAsync();

            // Copy all data into the folder
            await SoundManager.GetAllSounds();

            // Create folders in export folder
            await CreateExportFoldersAsync();

            StorageFolder localDataFolder = ApplicationData.Current.LocalFolder;
            StorageFolder exportFolder = await localDataFolder.GetFolderAsync("export");
            StorageFolder imagesExportFolder = await exportFolder.GetFolderAsync("images");
            StorageFolder soundDetailsExportFolder = await exportFolder.GetFolderAsync("soundDetails");
            StorageFolder dataFolder = await localDataFolder.GetFolderAsync("data");
            StorageFolder dataExportFolder = await exportFolder.GetFolderAsync("data");
            StorageFile dataFile = await dataFolder.GetFileAsync("data.json");

            // Copy the files into the export folder
            foreach (Sound sound in (App.Current as App)._itemViewHolder.allSounds)
            {
                await sound.AudioFile.CopyAsync(exportFolder, sound.AudioFile.Name, NameCollisionOption.ReplaceExisting);
                await sound.DetailsFile.CopyAsync(soundDetailsExportFolder, sound.DetailsFile.Name, NameCollisionOption.ReplaceExisting);
                if (sound.ImageFile != null)
                {
                    await sound.ImageFile.CopyAsync(imagesExportFolder, sound.ImageFile.Name, NameCollisionOption.ReplaceExisting);
                }
            }
            await dataFile.CopyAsync(dataExportFolder, dataFile.Name, NameCollisionOption.ReplaceExisting);
            // Create Zip file in local storage

            IAsyncAction asyncAction = Windows.System.Threading.ThreadPool.RunAsync(
                    async (workItem) =>
                    {
                        var t = Task.Run(() => ZipFile.CreateFromDirectory(exportFolder.Path, destinationFolder.Path + @"\SoundBoard.zip"));
                        t.Wait();

                        await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                            CoreDispatcherPriority.High,
                            new DispatchedHandler(() =>
                            {
                                ExportDataProgressRing.IsActive = false;
                            }));
                    });

            /*
            IAsyncAction asyncAction = Windows.System.Threading.ThreadPool.RunAsync(
                (workItem) =>
                {
                    
                });
                */

            /*
            IAsyncAction asyncAction = Windows.System.Threading.ThreadPool.RunAsync(
                (workItem) =>
                {
                    using (var archive = SharpCompress.Archives.Zip.ZipArchive.Create())
                    {
                        archive.AddAllFromDirectory(exportFolder.Path);
                        archive.SaveTo(destinationFolder.Path + @"\SoundBoard.zip", new WriterOptions(SharpCompress.Common.CompressionType.BZip2));
                    }
                });
            */
        }

        private async Task CreateExportFoldersAsync()
        {
            StorageFolder localDataFolder = ApplicationData.Current.LocalFolder;

            StorageFolder exportFolder;
            if (await localDataFolder.TryGetItemAsync("export") == null)
            {
                exportFolder = await localDataFolder.CreateFolderAsync("export");
            }
            else
            {
                exportFolder = await localDataFolder.GetFolderAsync("export");
            }

            if (await exportFolder.TryGetItemAsync("images") == null)
            {
                await exportFolder.CreateFolderAsync("images");
            }

            if (await exportFolder.TryGetItemAsync("soundDetails") == null)
            {
                await exportFolder.CreateFolderAsync("soundDetails");
            }
            
            if (await exportFolder.TryGetItemAsync("data") == null)
            {
                await exportFolder.CreateFolderAsync("data");
            }
        }
    }
}
