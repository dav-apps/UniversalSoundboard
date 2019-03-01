using davClassLibrary;
using davClassLibrary.DataAccess;
using davClassLibrary.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using UniversalSoundboard.Models;
using UniversalSoundBoard.Models;
using UniversalSoundBoard.Pages;
using Windows.ApplicationModel.Core;
using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Notifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace UniversalSoundBoard.DataAccess
{
    public class FileManager
    {
        #region Variables
        // Variables for the localSettings keys
        public const string davKey = "dav";
        public const string playingSoundsListVisibleKey = "playingSoundsListVisible";
        public const string playOneSoundAtOnceKey = "playOneSoundAtOnce";
        public const string liveTileKey = "liveTile";
        public const string showCategoryIconKey = "showCategoryIcon";
        public const string showSoundsPivotKey = "showSoundsPivot";
        public const string savePlayingSoundsKey = "savePlayingSounds";
        public const string themeKey = "theme";
        public const string showAcrylicBackgroundKey = "showAcrylicBackground";
        public const string soundOrderKey = "soundOrder";
        public const string soundOrderReversedKey = "soundOrderReversed";

        // Variables for defaults
        public const double volumeDefault = 1.0;
        public const bool liveTileDefault = true;
        public const bool playingSoundsListVisibleDefault = true;
        public const bool playOneSoundAtOnceDefault = false;
        public const string themeDefault = "system";
        public const bool showCategoryIconDefault = true;
        public const bool showSoundsPivotDefault = true;
        public const bool savePlayingSoundsDefault = true;
        public const bool showAcrylicBackgroundDefault = true;
        public const SoundOrder soundOrderDefault = SoundOrder.Custom;
        public const bool soundOrderReversedDefault = false;
        
        // Design constants
        public const int mobileMaxWidth = 550;
        public const int tabletMaxWidth = 650;
        public const int topButtonsCollapsedMaxWidth = 1400;
        public const int sideBarCollapsedMaxWidth = 1100;
        public const int moveSelectButtonMaxWidth = 850;
        public const int moveAddButtonMaxWidth = 800;
        public const int moveVolumeButtonMaxWidth = 750;
        public const int hideSearchBoxMaxWidth = 700;

        // Colors for the background of PlayingSoundsBar and SideBar
        private static double sideBarAcrylicBackgroundTintOpacity = 0.6;
        private static double playingSoundsBarAcrylicBackgroundTintOpacity = 0.85;
        private static Color sideBarLightBackgroundColor = Color.FromArgb(255, 245, 245, 245);            // #f5f5f5
        private static Color sideBarDarkBackgroundColor = Color.FromArgb(255, 29, 34, 49);                // #1d2231
        private static Color playingSoundsBarLightBackgroundColor = Color.FromArgb(255, 253, 253, 253);   // #fdfdfd
        private static Color playingSoundsBarDarkBackgroundColor = Color.FromArgb(255, 15, 20, 35);       // #0f1423


        public static DavEnvironment Environment = DavEnvironment.Production;

        // dav Keys
        private const string ApiKeyProduction = "gHgHKRbIjdguCM4cv5481hdiF5hZGWZ4x12Ur-7v";  // Prod
        public const string ApiKeyDevelopment = "eUzs3PQZYweXvumcWvagRHjdUroGe5Mo7kN1inHm";    // Dev
        public static string ApiKey => Environment == DavEnvironment.Production ? ApiKeyProduction : ApiKeyDevelopment;

        private const string LoginImplicitUrlProduction = "https://dav-apps.tech/login_implicit";
        private const string LoginImplicitUrlDevelopment = "https://89e877d0.ngrok.io/login_implicit";
        public static string LoginImplicitUrl => Environment == DavEnvironment.Production ? LoginImplicitUrlProduction : LoginImplicitUrlDevelopment;

        private const int AppIdProduction = 1;                 // Dev: 8; Prod: 1
        private const int AppIdDevelopment = 8;
        public static int AppId => Environment == DavEnvironment.Production ? AppIdProduction : AppIdDevelopment;

        private const int SoundFileTableIdProduction = 6;      // Dev: 11; Prod: 6
        private const int SoundFileTableIdDevelopment = 11;
        public static int SoundFileTableId => Environment == DavEnvironment.Production ? SoundFileTableIdProduction : SoundFileTableIdDevelopment;

        private const int ImageFileTableIdProduction = 7;      // Dev: 15; Prod: 7
        private const int ImageFileTableIdDevelopment = 15;
        public static int ImageFileTableId => Environment == DavEnvironment.Production ? ImageFileTableIdProduction : ImageFileTableIdDevelopment;

        private const int CategoryTableIdProduction = 8;       // Dev: 16; Prod: 8
        private const int CategoryTableIdDevelopment = 16;
        public static int CategoryTableId => Environment == DavEnvironment.Production ? CategoryTableIdProduction : CategoryTableIdDevelopment;

        private const int SoundTableIdProduction = 5;          // Dev: 17; Prod: 5
        private const int SoundTableIdDevelopment = 17;
        public static int SoundTableId => Environment == DavEnvironment.Production ? SoundTableIdProduction : SoundTableIdDevelopment;

        private const int PlayingSoundTableIdProduction = 9;   // Dev: 18; Prod: 9
        private const int PlayingSoundTableIdDevelopment = 18;
        public static int PlayingSoundTableId => Environment == DavEnvironment.Production ? PlayingSoundTableIdProduction : PlayingSoundTableIdDevelopment;

        private const int OrderTableIdProduction = 12;  // Dev: 27; Prod: 12
        private const int OrderTableIdDevelopment = 27;
        public static int OrderTableId => Environment == DavEnvironment.Production ? OrderTableIdProduction : OrderTableIdDevelopment;

        // Table property names
        public const string SoundTableNamePropertyName = "name";
        public const string SoundTableFavouritePropertyName = "favourite";
        public const string SoundTableSoundUuidPropertyName = "sound_uuid";
        public const string SoundTableImageUuidPropertyName = "image_uuid";
        public const string SoundTableCategoryUuidPropertyName = "category_uuid";

        public const string CategoryTableNamePropertyName = "name";
        public const string CategoryTableIconPropertyName = "icon";

        public const string PlayingSoundTableSoundIdsPropertyName = "sound_ids";
        public const string PlayingSoundTableCurrentPropertyName = "current";
        public const string PlayingSoundTableRepetitionsPropertyName = "repetitions";
        public const string PlayingSoundTableRandomlyPropertyName = "randomly";
        public const string PlayingSoundTableVolumePropertyName = "volume";

        public const string OrderTableTypePropertyName = "type";
        public const string OrderTableCategoryPropertyName = "category";
        public const string OrderTableFavouritePropertyName = "favs";

        // Other constants
        public const string TableObjectExtPropertyName = "ext";
        public const string CategoryOrderType = "0";
        public const string SoundOrderType = "1";

        public static List<string> allowedFileTypes = new List<string>
        {
            ".mp3",
            ".wav",
            ".ogg",
            ".wma",
            ".flac"
        };

        private enum DataModel
        {
            Old,
            New,
            Dav
        };

        public enum SoundOrder
        {
            Custom = 0,
            Name = 1,
            CreationDate = 2
        };

        // Local variables
        internal static bool skipAutoSuggestBoxTextChanged = false;
        private static bool updatingGridView = false;
        internal static bool syncFinished = false;

        // Save the custom order of the sounds in all categories to load them faster
        private static Dictionary<Guid, List<Guid>> CustomSoundOrder = new Dictionary<Guid, List<Guid>>();
        private static Dictionary<Guid, List<Guid>> CustomFavouriteSoundOrder = new Dictionary<Guid, List<Guid>>();

        // If true LoadSelectedSounds is currently running
        private static bool isLoadingSelectedCategory = false;

        // Set this when calling LoadAllSounds, LoadSoundsByCategory or LoadSoundsByName
        // Is either a uuid (category), Guid.Empty (all sounds) or null (search)
        private static Guid? selectedCategory = Guid.Empty;
        private static string searchQuery = "";
        #endregion

        #region Filesystem Methods
        private static async Task<StorageFolder> GetExportFolderAsync()
        {
            StorageFolder localFolder = ApplicationData.Current.LocalCacheFolder;
            string exportFolderName = "export";

            if (await localFolder.TryGetItemAsync(exportFolderName) == null)
                return await localFolder.CreateFolderAsync(exportFolderName);
            else
                return await localFolder.GetFolderAsync(exportFolderName);
        }

        private async static Task<StorageFolder> GetImportFolderAsync()
        {
            StorageFolder localDataFolder = ApplicationData.Current.LocalCacheFolder;
            string importFolderName = "import";

            if (await localDataFolder.TryGetItemAsync(importFolderName) == null)
                return await localDataFolder.CreateFolderAsync(importFolderName);
            else
                return await localDataFolder.GetFolderAsync(importFolderName);
        }

        public static string GetDavDataPath()
        {
            string path = Path.Combine(ApplicationData.Current.LocalFolder.Path, "dav");
            Directory.CreateDirectory(path);
            return path;
        }

        private static async Task ClearCacheAsync()
        {
            if ((App.Current as App)._itemViewHolder.IsExporting || (App.Current as App)._itemViewHolder.IsImporting)
                return;

            StorageFolder cacheFolder = ApplicationData.Current.LocalCacheFolder;
            foreach (var item in await cacheFolder.GetItemsAsync())
                await item.DeleteAsync();
        }

        private static async Task<DataModel> GetDataModelAsync(StorageFolder root)
        {
            // Old format: Archive has a soundDetails and a data folder
            // New format: Archive has the folders sounds, images and a data.json file
            // dav format: Archive has a data.json file, but no folders with sounds and images
            DataModel dataModel = DataModel.Dav;
            if (await root.TryGetItemAsync("soundDetails") != null || await root.TryGetItemAsync("data") != null)
            {
                // Old format
                dataModel = DataModel.Old;
            }
            else if (await root.TryGetItemAsync("sounds") != null || await root.TryGetItemAsync("images") != null)
            {
                // New format
                dataModel = DataModel.New;
            }
            else
            {
                // dav format
                dataModel = DataModel.Dav;
            }

            return dataModel;
        }

        public static async Task RemoveNotLocallySavedSoundsAsync()
        {
            // Get each sound and check if the file exists
            foreach(var sound in (App.Current as App)._itemViewHolder.AllSounds)
            {
                var soundFileTableObject = await GetSoundFileTableObjectAsync(sound.Uuid);
                if(soundFileTableObject != null && soundFileTableObject.FileDownloaded())
                    continue;

                // Completely remove the sound from the database so that it won't be deleted when the user logs in again
                var imageFileTableObject = await GetImageFileTableObjectAsync(sound.Uuid);
                var soundTableObject = await DatabaseOperations.GetObjectAsync(sound.Uuid);

                if (soundFileTableObject != null)
                {
                    await Dav.Database.DeleteTableObjectAsync(soundFileTableObject);
                    await Dav.Database.DeleteTableObjectAsync(soundFileTableObject);
                }
                if(imageFileTableObject != null)
                {
                    await Dav.Database.DeleteTableObjectAsync(imageFileTableObject);
                    await Dav.Database.DeleteTableObjectAsync(imageFileTableObject);
                }
                if(soundTableObject != null)
                {
                    await Dav.Database.DeleteTableObjectAsync(soundTableObject);
                    await Dav.Database.DeleteTableObjectAsync(soundTableObject);
                }
            }

            (App.Current as App)._itemViewHolder.AllSoundsChanged = true;
        }
        #endregion

        #region Database Methods
        public static async Task MigrateDataAsync()
        {
            if (await UsesDavDataModelAsync())
                return;

            StorageFolder localDataFolder = ApplicationData.Current.LocalFolder;

            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                (App.Current as App)._itemViewHolder.IsImporting = true;
                (App.Current as App)._itemViewHolder.UpgradeDataStatusText =
                                    new Windows.ApplicationModel.Resources.ResourceLoader().GetString("UpgradeDataStatusMessage-Preparing");
            });

            // Check if the data model is new or old
            DataModel dataModel = await GetDataModelAsync(localDataFolder);
            Progress<int> progress = new Progress<int>(UpgradeDataProgress);

            if(dataModel == DataModel.Old)
                await UpgradeOldDataModelAsync(localDataFolder, false, progress);
            else if(dataModel == DataModel.New)
                await UpgradeNewDataModelAsync(localDataFolder, false, progress);

            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () => {
                await CreateCategoriesListAsync();
                (App.Current as App)._itemViewHolder.AllSoundsChanged = true;
                (App.Current as App)._itemViewHolder.IsImporting = false;
                await ClearCacheAsync();
            });
        }

        private static void UpgradeDataProgress(int value)
        {
            var x = CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                (App.Current as App)._itemViewHolder.UpgradeDataStatusText = value + " %";
            });
        }

        public static async Task ExportDataAsync(StorageFolder destinationFolder)
        {
            await ClearCacheAsync();

            var stringLoader = new Windows.ApplicationModel.Resources.ResourceLoader();
            (App.Current as App)._itemViewHolder.Exported = false;
            (App.Current as App)._itemViewHolder.Imported = false;
            (App.Current as App)._itemViewHolder.IsExporting = true;
            (App.Current as App)._itemViewHolder.AreExportAndImportButtonsEnabled = false;

            StorageFolder localCacheFolder = ApplicationData.Current.LocalCacheFolder;
            StorageFolder exportFolder = await GetExportFolderAsync();
            var progress = new Progress<int>(ExportProgress);

            await DataManager.ExportData(new DirectoryInfo(exportFolder.Path), progress);

            (App.Current as App)._itemViewHolder.ExportMessage = stringLoader.GetString("ExportMessage-3");

            // Create the zip file
            StorageFile zipFile = await Task.Run(async () =>
            {
                string exportFilePath = Path.Combine(localCacheFolder.Path, "export.zip");
                ZipFile.CreateFromDirectory(exportFolder.Path, exportFilePath);
                return await StorageFile.GetFileFromPathAsync(exportFilePath);
            });

            (App.Current as App)._itemViewHolder.ExportMessage = stringLoader.GetString("ExportMessage-4");

            // Copy the file into the destination folder
            await zipFile.MoveAsync(destinationFolder, "UniversalSoundboard " + DateTime.Today.ToString("dd.MM.yyyy") + ".zip", NameCollisionOption.GenerateUniqueName);

            (App.Current as App)._itemViewHolder.ExportMessage = stringLoader.GetString("ExportImportMessage-TidyUp");
            (App.Current as App)._itemViewHolder.IsExporting = false;

            await ClearCacheAsync();

            (App.Current as App)._itemViewHolder.ExportMessage = "";
            (App.Current as App)._itemViewHolder.Exported = true;
            (App.Current as App)._itemViewHolder.AreExportAndImportButtonsEnabled = true;
        }

        private static void ExportProgress(int value)
        {
            (App.Current as App)._itemViewHolder.ExportMessage = value + " %";
        }

        public static async Task ImportDataAsync(StorageFile zipFile)
        {
            var stringLoader = new Windows.ApplicationModel.Resources.ResourceLoader();
            (App.Current as App)._itemViewHolder.ImportMessage = stringLoader.GetString("ImportMessage-1");

            await ClearCacheAsync();

            // Extract the file into the local cache folder
            (App.Current as App)._itemViewHolder.IsImporting = true;
            (App.Current as App)._itemViewHolder.Exported = false;
            (App.Current as App)._itemViewHolder.Imported = false;
            (App.Current as App)._itemViewHolder.AreExportAndImportButtonsEnabled = false;

            StorageFolder localCacheFolder = ApplicationData.Current.LocalCacheFolder;
            StorageFolder importFolder = await GetImportFolderAsync();

            await Task.Run(async () =>
            {
                StorageFile newZipFile = await zipFile.CopyAsync(localCacheFolder, "import.zip", NameCollisionOption.ReplaceExisting);

                // Extract the zip file
                ZipFile.ExtractToDirectory(newZipFile.Path, importFolder.Path);
            });

            DataModel dataModel = await GetDataModelAsync(importFolder);
            Progress<int> progress = new Progress<int>(ImportProgress);

            switch (dataModel)
            {
                case DataModel.Old:
                    await UpgradeOldDataModelAsync(importFolder, true, progress);
                    break;
                case DataModel.New:
                    await UpgradeNewDataModelAsync(importFolder, true, progress);
                    break;
                default:
                    await Task.Run(() => DataManager.ImportData(new DirectoryInfo(importFolder.Path), progress));
                    break;
            }

            (App.Current as App)._itemViewHolder.ImportMessage = stringLoader.GetString("ExportImportMessage-TidyUp"); // TidyUp
            (App.Current as App)._itemViewHolder.IsImporting = false;

            await ClearCacheAsync();

            (App.Current as App)._itemViewHolder.ImportMessage = "";
            (App.Current as App)._itemViewHolder.Imported = true;
            (App.Current as App)._itemViewHolder.AllSoundsChanged = true;
            (App.Current as App)._itemViewHolder.AreExportAndImportButtonsEnabled = true;

            await CreateCategoriesListAsync();
            await LoadAllSoundsAsync();
            await CreatePlayingSoundsListAsync();
            await SetSoundBoardSizeTextAsync();
        }

        private static void ImportProgress(int value)
        {
            (App.Current as App)._itemViewHolder.ImportMessage = value + " %";
        }

        private static async Task UpgradeNewDataModelAsync(StorageFolder root, bool import, IProgress<int> progress)
        {
            // New data format
            StorageFolder soundsFolder = await root.TryGetItemAsync("sounds") as StorageFolder;
            StorageFolder imagesFolder = await root.TryGetItemAsync("images") as StorageFolder;
            StorageFile dataFile = await root.TryGetItemAsync("data.json") as StorageFile;
            StorageFile databaseFile = await root.TryGetItemAsync("universalsoundboard.db") as StorageFile;

            if (import && dataFile != null)
            {
                // Get the data from the data file
                NewData newData = await GetDataFromFileAsync(dataFile);

                foreach (Category category in newData.Categories)
                    await DatabaseOperations.AddCategoryAsync(category.Uuid, category.Name, category.Icon);

                if (soundsFolder == null) return;
                int i = 0;
                int soundDataCount = newData.Sounds.Count;

                foreach (SoundData soundData in newData.Sounds)
                {
                    if (await soundsFolder.TryGetItemAsync(soundData.Uuid + "." + soundData.SoundExt) is StorageFile audioFile)
                    {
                        Guid soundUuid = ConvertStringToGuid(soundData.Uuid).GetValueOrDefault();
                        Guid categoryUuid = ConvertStringToGuid(soundData.CategoryId).GetValueOrDefault();

                        soundUuid = await AddSoundAsync(soundUuid, WebUtility.HtmlDecode(soundData.Name), categoryUuid, audioFile);

                        if (imagesFolder != null)
                        {
                            StorageFile imageFile = await imagesFolder.TryGetItemAsync(soundData.Uuid + "." + soundData.ImageExt) as StorageFile;
                            if (imageFile != null)
                            {
                                // Set the image of the sound
                                Guid imageUuid = Guid.NewGuid();
                                await DatabaseOperations.AddImageFileAsync(imageUuid, imageFile);
                                await DatabaseOperations.UpdateSoundAsync(soundUuid, null, null, null, imageUuid.ToString(), null);
                                await imageFile.DeleteAsync();
                            }
                        }

                        if (soundData.Favourite)
                            await DatabaseOperations.UpdateSoundAsync(soundUuid, null, soundData.Favourite.ToString(), null, null, null);

                        await audioFile.DeleteAsync();
                    }

                    i++;
                    progress.Report((int)Math.Round(100.0 / soundDataCount * i));
                }
            }
            else if (databaseFile != null)
            {
                // Get the data from the database file
                // Add the categories
                List<Category> categories = DatabaseOperations.GetAllCategoriesFromDatabaseFile(databaseFile);

                foreach (var category in categories)
                {
                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () => {
                        await AddCategoryAsync(category.Uuid, category.Name, category.Icon);
                    });
                }

                // Add the sounds
                if (soundsFolder == null) return;
                int i = 0;

                List<OldSoundDatabaseModel> sounds = DatabaseOperations.GetAllSoundsFromDatabaseFile(databaseFile);

                foreach(var sound in sounds)
                {
                    StorageFile audioFile = await soundsFolder.TryGetItemAsync(sound.uuid + "." + sound.sound_ext) as StorageFile;
                    if (audioFile != null)
                    {
                        Guid soundGuid = Guid.Empty;
                        Guid.TryParse(sound.uuid, out soundGuid);
                        Guid categoryGuid = Guid.Empty;
                        Guid.TryParse(sound.category_id, out categoryGuid);
                        Guid soundUuid = await AddSoundAsync(soundGuid, WebUtility.HtmlDecode(sound.name), categoryGuid, audioFile);

                        if (imagesFolder != null && !string.IsNullOrEmpty(sound.image_ext))
                        {
                            StorageFile imageFile = await imagesFolder.TryGetItemAsync(sound.uuid + "." + sound.image_ext) as StorageFile;
                            if (imageFile != null)
                            {
                                // Add image
                                Guid imageUuid = Guid.NewGuid();
                                await DatabaseOperations.AddImageFileAsync(imageUuid, imageFile);
                                await DatabaseOperations.UpdateSoundAsync(soundUuid, null, null, null, imageUuid.ToString(), null);
                                await imageFile.DeleteAsync();
                            }
                        }

                        if (sound.favourite)
                            await DatabaseOperations.UpdateSoundAsync(soundUuid, null, sound.favourite.ToString(), null, null, null);

                        await audioFile.DeleteAsync();
                    }

                    i++;
                    progress.Report((int)Math.Round(100.0 / sounds.Count * i));
                }
            }

            if (soundsFolder != null)
                await soundsFolder.DeleteAsync();
            if (imagesFolder != null)
                await imagesFolder.DeleteAsync();
            if (databaseFile != null)
                await databaseFile.DeleteAsync();
            if (dataFile != null)
                await dataFile.DeleteAsync();
        }

        private static async Task UpgradeOldDataModelAsync(StorageFolder root, bool import, IProgress<int> progress)
        {
            // Old data format
            StorageFolder soundDetailsFolder = await root.TryGetItemAsync("soundDetails") as StorageFolder;   // root/soundDetails
            StorageFolder imagesFolder = await root.TryGetItemAsync("images") as StorageFolder;               // root/images
            StorageFolder dataFolder = await root.TryGetItemAsync("data") as StorageFolder;                   // root/data
            StorageFile dataFile = null;
            if (dataFolder != null)
                dataFile = await dataFolder.TryGetItemAsync("data.json") as StorageFile;        // root/data/data.json

            // Get the categories
            if (dataFile != null)
            {
                foreach (Category cat in await GetCategoriesListAsync(dataFile))
                    await DatabaseOperations.AddCategoryAsync(Guid.NewGuid(), cat.Name, cat.Icon);

                await dataFile.DeleteAsync();
            }

            if(dataFolder != null)
                await dataFolder.DeleteAsync();

            int i = 0;
            var filesList = await root.GetFilesAsync();
            int filesCount = filesList.Count;

            // Get the sound files
            foreach (var file in filesList)
            {
                if (file.ContentType == "audio/wav" || file.ContentType == "audio/mpeg")
                {
                    StorageFile soundDetailsFile = null;
                    string name = file.DisplayName;
                    Guid soundUuid = Guid.Empty;
                    string categoryName = null;
                    Guid categoryUuid = Guid.Empty;
                    bool favourite = false;

                    // Get the soundDetails file of the sound and get favourite and category information
                    if (soundDetailsFolder != null)
                    {
                        soundDetailsFile = await soundDetailsFolder.TryGetItemAsync(name + ".json") as StorageFile;
                        if (soundDetailsFile != null)
                        {
                            SoundDetails soundDetails = new SoundDetails();
                            await soundDetails.ReadSoundDetailsFileAsync(soundDetailsFile);
                            categoryName = soundDetails.Category;
                            favourite = soundDetails.Favourite;
                        }
                    }

                    // Find the category of the sound
                    if (!string.IsNullOrEmpty(categoryName))
                    {
                        foreach (Category category in await GetAllCategoriesAsync())
                        {
                            if (category.Name == categoryName)
                            {
                                categoryUuid = category.Uuid;
                                break;
                            }
                        }
                    }

                    // Save the sound
                    soundUuid = await AddSoundAsync(Guid.Empty, name, categoryUuid, file);

                    // Get the image file of the sound
                    foreach (StorageFile imageFile in await imagesFolder.GetFilesAsync())
                    {
                        if (name == imageFile.DisplayName)
                        {
                            Guid imageUuid = Guid.NewGuid();
                            await DatabaseOperations.AddImageFileAsync(imageUuid, imageFile);
                            await DatabaseOperations.UpdateSoundAsync(soundUuid, null, null, null, imageUuid.ToString(), null);

                            // Delete the image
                            await imageFile.DeleteAsync();
                            break;
                        }
                    }

                    if (favourite)
                        await DatabaseOperations.UpdateSoundAsync(soundUuid, null, favourite.ToString(), null, null, null);

                    // Delete the sound and soundDetails file
                    if (soundDetailsFile != null)
                        await soundDetailsFile.DeleteAsync();
                    await file.DeleteAsync();
                }

                i++;
                progress.Report((int)Math.Round(100.0 / filesCount * i));
            }

            // Delete the old folders
            if (import)
                await root.DeleteAsync();
            else
            {
                if(imagesFolder != null)
                    await imagesFolder.DeleteAsync();
                if(soundDetailsFolder != null)
                    await soundDetailsFolder.DeleteAsync();
            }
        }

        public static async Task ExportSoundsAsync(List<Sound> sounds, bool saveAsZip, StorageFolder destinationFolder)
        {
            // Show the loading screen
            var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
            (App.Current as App)._itemViewHolder.LoadingScreenMessage = loader.GetString("ExportSoundsMessage");
            (App.Current as App)._itemViewHolder.LoadingScreenVisibility = true;

            if (saveAsZip)
            {
                await ClearCacheAsync();
                StorageFolder localCacheFolder = ApplicationData.Current.LocalCacheFolder;
                StorageFolder exportFolder = await GetExportFolderAsync();

                // Copy the selected files into the export folder
                foreach (var sound in sounds)
                    await CopySoundFileIntoFolderAsync(sound, exportFolder);

                // Create the zip file from the export folder
                StorageFile zipFile = await Task.Run(async () =>
                {
                    string exportFilePath = Path.Combine(localCacheFolder.Path, "export.zip");
                    ZipFile.CreateFromDirectory(exportFolder.Path, exportFilePath);
                    return await StorageFile.GetFileFromPathAsync(exportFilePath);
                });

                // Move the zip file into the destination folder
                await zipFile.MoveAsync(destinationFolder, "UniversalSoundboard " + DateTime.Today.ToString("dd.MM.yyyy") + ".zip", NameCollisionOption.GenerateUniqueName);
                await ClearCacheAsync();
            }
            else
            {
                // Copy the files directly into the folder
                foreach(var sound in sounds)
                    await CopySoundFileIntoFolderAsync(sound, destinationFolder);
            }

            (App.Current as App)._itemViewHolder.LoadingScreenVisibility = false;
        }

        private static async Task CopySoundFileIntoFolderAsync(Sound sound, StorageFolder destinationFolder)
        {
            string ext = await sound.GetAudioFileExtensionAsync();

            if (string.IsNullOrEmpty(ext))
                ext = "mp3";

            StorageFile soundFile = await destinationFolder.CreateFileAsync(sound.Name + "." + ext, CreationCollisionOption.GenerateUniqueName);
            await FileIO.WriteBytesAsync(soundFile, await GetBytesAsync(await sound.GetAudioFileAsync()));
        }

        // Load the sounds from the database and return them
        private static async Task<List<Sound>> GetSavedSoundsAsync()
        {
            List<TableObject> soundsTableObjectList = await DatabaseOperations.GetAllSoundsAsync();
            List<Sound> sounds = new List<Sound>();

            foreach (var soundTableObject in soundsTableObjectList)
            {
                Guid? soundFileUuid = ConvertStringToGuid(soundTableObject.GetPropertyValue(SoundTableSoundUuidPropertyName));
                if (DatabaseOperations.GetObjectAsync(soundFileUuid.GetValueOrDefault()) == null) continue;

                sounds.Add(await GetSoundAsync(soundTableObject.Uuid));
            }

            (App.Current as App)._itemViewHolder.AllSoundsChanged = false;
            return sounds;
        }

        // Load all sounds into the sounds list
        public static async Task LoadAllSoundsAsync()
        {
            selectedCategory = Guid.Empty;
            await LoadSelectedCategory();
        }

        public static async Task LoadSoundsByCategoryAsync(Guid uuid)
        {
            selectedCategory = uuid;
            await LoadSelectedCategory();
        }

        // Copy the sounds by the name from the all sounds list into the Sounds list which is bound to the GridView
        public static async Task LoadSoundsByNameAsync(string name)
        {
            selectedCategory = null;
            searchQuery = name;
            await LoadSelectedCategory();
        }

        private static async Task LoadSelectedCategory()
        {
            if (isLoadingSelectedCategory) return;
            isLoadingSelectedCategory = true;

            Guid? selectedCategoryAtBeginning = selectedCategory;
            (App.Current as App)._itemViewHolder.ProgressRingIsActive = true;
            (App.Current as App)._itemViewHolder.PlayAllButtonVisibility = Visibility.Collapsed;
            await UpdateAllSoundsListAsync();

            (App.Current as App)._itemViewHolder.Sounds.Clear();
            (App.Current as App)._itemViewHolder.FavouriteSounds.Clear();
            List<Sound> sounds = new List<Sound>();
            List<Sound> favouriteSounds = new List<Sound>();

            if (selectedCategoryAtBeginning == Guid.Empty)
            {
                // Load all sounds
                foreach (var sound in GetAllSounds(false))
                    sounds.Add(sound);

                foreach (var sound in GetAllSounds(true))
                    favouriteSounds.Add(sound);

                // Sort the sounds
                foreach (var sound in SortSoundsList(sounds,
                                                    (App.Current as App)._itemViewHolder.SoundOrder,
                                                    (App.Current as App)._itemViewHolder.SoundOrderReversed,
                                                    Guid.Empty,
                                                    false)){
                    (App.Current as App)._itemViewHolder.Sounds.Add(sound);
                }

                foreach (var sound in SortSoundsList(favouriteSounds,
                                                    (App.Current as App)._itemViewHolder.SoundOrder,
                                                    (App.Current as App)._itemViewHolder.SoundOrderReversed,
                                                    Guid.Empty,
                                                    true)){
                    (App.Current as App)._itemViewHolder.FavouriteSounds.Add(sound);
                }
            }
            else if(selectedCategoryAtBeginning == null)
            {
                // Load sounds by name
                foreach (var sound in (App.Current as App)._itemViewHolder.AllSounds)
                {
                    if (sound.Name.ToLower().Contains(searchQuery.ToLower()))
                    {
                        (App.Current as App)._itemViewHolder.Sounds.Add(sound);
                        if (sound.Favourite)
                            (App.Current as App)._itemViewHolder.FavouriteSounds.Add(sound);
                    }
                }
            }
            else
            {
                // Load sounds by category
                foreach (var sound in GetSoundsByCategory(selectedCategoryAtBeginning.Value, false))
                    sounds.Add(sound);

                foreach (var sound in GetSoundsByCategory(selectedCategoryAtBeginning.Value, true))
                    favouriteSounds.Add(sound);

                // Sort the sounds
                foreach (var sound in SortSoundsList(sounds,
                                                    (App.Current as App)._itemViewHolder.SoundOrder,
                                                    (App.Current as App)._itemViewHolder.SoundOrderReversed,
                                                    selectedCategoryAtBeginning.Value,
                                                    false)){
                    (App.Current as App)._itemViewHolder.Sounds.Add(sound);
                }

                foreach (var sound in SortSoundsList(favouriteSounds,
                                                    (App.Current as App)._itemViewHolder.SoundOrder,
                                                    (App.Current as App)._itemViewHolder.SoundOrderReversed,
                                                    selectedCategoryAtBeginning.Value,
                                                    true)){
                    (App.Current as App)._itemViewHolder.FavouriteSounds.Add(sound);
                }
            }

            isLoadingSelectedCategory = false;

            // If the selection changed, load the new selected category
            if (selectedCategoryAtBeginning != selectedCategory)
                await LoadSelectedCategory();

            ShowPlayAllButton();
            (App.Current as App)._itemViewHolder.ProgressRingIsActive = false;
        }

        // When the sounds list was changed, load all sounds from the database
        private static async Task UpdateAllSoundsListAsync()
        {
            if ((App.Current as App)._itemViewHolder.AllSoundsChanged)
            {
                (App.Current as App)._itemViewHolder.AllSounds.Clear();
                foreach (Sound sound in await GetSavedSoundsAsync())
                    (App.Current as App)._itemViewHolder.AllSounds.Add(sound);

                await UpdateLiveTileAsync();

                if((App.Current as App)._itemViewHolder.SoundOrder == SoundOrder.Custom)
                    await LoadCustomSoundOrderAsync();
            }
        }

        // Create the lists in the Custom Sound order Dictionaries
        private static async Task LoadCustomSoundOrderAsync()
        {
            if ((App.Current as App)._itemViewHolder.SoundOrder != SoundOrder.Custom) return;

            // Create an entry for each category in the Dictionary and save the sound order there
            foreach(var category in (App.Current as App)._itemViewHolder.Categories)
            {
                if(category.Uuid == Guid.Empty)
                {
                    // All Sounds
                    List<Sound> allSounds = GetAllSounds(false);
                    List<Sound> allFavouriteSounds = GetAllSounds(true);

                    CustomSoundOrder[Guid.Empty] = new List<Guid>();
                    CustomFavouriteSoundOrder[Guid.Empty] = new List<Guid>();

                    // Add all sounds to the dictionary
                    foreach (var sound in await SortSoundsListByCustomOrderAsync(allSounds, Guid.Empty, false))
                        CustomSoundOrder[Guid.Empty].Add(sound.Uuid);

                    foreach (var sound in await SortSoundsListByCustomOrderAsync(allFavouriteSounds, Guid.Empty, true))
                        CustomFavouriteSoundOrder[Guid.Empty].Add(sound.Uuid);
                }
                else
                {
                    List<Sound> sounds = GetSoundsByCategory(category.Uuid, false);
                    List<Sound> favouriteSounds = GetSoundsByCategory(category.Uuid, true);

                    CustomSoundOrder[category.Uuid] = new List<Guid>();
                    CustomFavouriteSoundOrder[category.Uuid] = new List<Guid>();

                    // Add the sounds to the dictionary
                    foreach (var sound in await SortSoundsListByCustomOrderAsync(sounds, category.Uuid, false))
                        CustomSoundOrder[category.Uuid].Add(sound.Uuid);

                    foreach (var sound in await SortSoundsListByCustomOrderAsync(favouriteSounds, category.Uuid, true))
                        CustomFavouriteSoundOrder[category.Uuid].Add(sound.Uuid);
                }
            }
        }

        // Get all sounds from the all sounds list
        private static List<Sound> GetAllSounds(bool favourites)
        {
            List<Sound> sounds = new List<Sound>();

            foreach (var sound in (App.Current as App)._itemViewHolder.AllSounds)
            {
                if (!favourites || (favourites && sound.Favourite))
                    sounds.Add(sound);
            }

            return sounds;
        }

        // Get the sounds of the category from the all sounds list
        private static List<Sound> GetSoundsByCategory(Guid categoryUuid, bool favourites)
        {
            List<Sound> sounds = new List<Sound>();

            foreach (var sound in (App.Current as App)._itemViewHolder.AllSounds)
            {
                // Check if the sound belongs to the category
                if (sound.Categories.Exists(c => c.Uuid == categoryUuid) && (!favourites || (favourites && sound.Favourite)))
                    sounds.Add(sound);
            }

            return sounds;
        }

        public static async Task<Guid> AddSoundAsync(Guid uuid, string name, Guid categoryUuid, StorageFile audioFile)
        {
            // Generate a new uuid if necessary
            if (Equals(uuid, Guid.Empty))
                uuid = Guid.NewGuid();
            else if (DatabaseOperations.GetObjectAsync(uuid) != null)
                return uuid;

            // Generate a uuid for the soundFile
            Guid soundFileUuid = Guid.NewGuid();

            // Copy the file into the local app folder
            StorageFile newAudioFile = await audioFile.CopyAsync(ApplicationData.Current.LocalCacheFolder, "newSound" + audioFile.FileType, NameCollisionOption.ReplaceExisting);

            await DatabaseOperations.AddSoundFileAsync(soundFileUuid, newAudioFile);
            await DatabaseOperations.AddSoundAsync(uuid, name, soundFileUuid.ToString(), Equals(categoryUuid, Guid.Empty) ? null : categoryUuid.ToString());

            await ClearCacheAsync();
            return uuid;
        }

        public static async Task<Sound> GetSoundAsync(Guid uuid)
        {
            var soundTableObject = await DatabaseOperations.GetObjectAsync(uuid);

            if (soundTableObject == null || soundTableObject.TableId != SoundTableId)
                return null;

            Sound sound = new Sound(soundTableObject.Uuid)
            {
                Name = soundTableObject.GetPropertyValue(SoundTableNamePropertyName)
            };

            // Get favourite
            var favouriteString = soundTableObject.GetPropertyValue(SoundTableFavouritePropertyName);
            bool favourite = false;
            if (!string.IsNullOrEmpty(favouriteString))
            {
                bool.TryParse(favouriteString, out favourite);
                sound.Favourite = favourite;
            }

            // Get the categories
            var categoryUuidsString = soundTableObject.GetPropertyValue(SoundTableCategoryUuidPropertyName);
            sound.Categories = new List<Category>();
            if (!string.IsNullOrEmpty(categoryUuidsString))
            {
                foreach(var cUuidString in categoryUuidsString.Split(","))
                {
                    Guid? cUuid = ConvertStringToGuid(cUuidString);
                    if(cUuid.HasValue)
                    {
                        var category = await GetCategoryAsync(cUuid.Value);
                        if(category != null)
                            sound.Categories.Add(category);
                    }
                }
            }

            // Get Image for Sound
            BitmapImage image = new BitmapImage();

            Uri defaultImageUri;
            if ((App.Current as App).RequestedTheme == ApplicationTheme.Dark)
                defaultImageUri = new Uri("ms-appx:///Assets/Images/default-dark.png", UriKind.Absolute);
            else
                defaultImageUri = new Uri("ms-appx:///Assets/Images/default.png", UriKind.Absolute);
            image.UriSource = defaultImageUri;

            string imageFileUuidString = soundTableObject.GetPropertyValue(SoundTableImageUuidPropertyName);
            Guid? imageFileUuid = ConvertStringToGuid(imageFileUuidString);
            if (imageFileUuid.HasValue && !Equals(imageFileUuid, Guid.Empty))
            {
                var imageFile = await GetTableObjectFileAsync(imageFileUuid.Value);

                if (imageFile != null)
                    image.UriSource = new Uri(imageFile.Path);
            }
            sound.Image = image;

            return sound;
        }

        public static async Task RenameSoundAsync(Guid uuid, string newName)
        {
            await DatabaseOperations.UpdateSoundAsync(uuid, newName, null, null, null, null);
            (App.Current as App)._itemViewHolder.AllSoundsChanged = true;
        }

        public static async Task SetCategoriesOfSoundAsync(Guid soundUuid, List<Guid> categoryUuids)
        {
            List<string> categoryUuidsString = new List<string>();
            foreach (var uuid in categoryUuids)
                categoryUuidsString.Add(uuid.ToString());

            await DatabaseOperations.UpdateSoundAsync(soundUuid, null, null, null, null, categoryUuidsString);
            (App.Current as App)._itemViewHolder.AllSoundsChanged = true;
        }

        public static async Task SetSoundAsFavouriteAsync(Guid uuid, bool favourite)
        {
            await DatabaseOperations.UpdateSoundAsync(uuid, null, favourite.ToString(), null, null, null);
            (App.Current as App)._itemViewHolder.AllSoundsChanged = true;
        }

        public static async Task UpdateImageOfSoundAsync(Guid soundUuid, StorageFile file)
        {
            var soundTableObject = await DatabaseOperations.GetObjectAsync(soundUuid);
            if (soundTableObject == null || soundTableObject.TableId != SoundTableId)
                return;
            
            Guid? imageUuid = ConvertStringToGuid(soundTableObject.GetPropertyValue(SoundTableImageUuidPropertyName));
            StorageFile newImageFile = await file.CopyAsync(ApplicationData.Current.LocalCacheFolder, "newImage" + file.FileType, NameCollisionOption.ReplaceExisting);

            if (!imageUuid.HasValue || Equals(imageUuid, Guid.Empty))
            {
                // Create new image file
                Guid imageFileUuid = Guid.NewGuid();
                await DatabaseOperations.AddImageFileAsync(imageFileUuid, newImageFile);
                await DatabaseOperations.UpdateSoundAsync(soundUuid, null, null, null, imageFileUuid.ToString(), null);
            }
            else
            {
                // Update the existing image file
                await DatabaseOperations.UpdateImageFileAsync(imageUuid.Value, newImageFile);
            }

            (App.Current as App)._itemViewHolder.AllSoundsChanged = true;
        }
        
        public static async Task DeleteSoundAsync(Guid uuid)
        {
            await DatabaseOperations.DeleteSoundAsync(uuid);
            (App.Current as App)._itemViewHolder.AllSoundsChanged = true;
        }
        
        public static async Task DeleteSoundsAsync(List<Guid> sounds)
        {
            foreach (Guid uuid in sounds)
                await DatabaseOperations.DeleteSoundAsync(uuid);

            (App.Current as App)._itemViewHolder.AllSoundsChanged = true;
        }

        public static async Task<List<Sound>> SortSoundsListByCustomOrderAsync(List<Sound> sounds, Guid categoryUuid, bool favourite)
        {
            // Get the order table objects
            var tableObjects = await DatabaseOperations.GetAllOrdersAsync();
            
            // Get the order objects with the type Sound (1), the right category uuid and the same favourite
            var soundOrderTableObjects = tableObjects.FindAll((TableObject obj) =>
            {
                // Check if the object is of type Sound
                if (obj.GetPropertyValue(OrderTableTypePropertyName) != SoundOrderType) return false;

                // Check if the object has the right category uuid
                string categoryUuidString = obj.GetPropertyValue(OrderTableCategoryPropertyName);
                Guid? cUuid = ConvertStringToGuid(categoryUuidString);
                if (!cUuid.HasValue) return false;

                string favString = obj.GetPropertyValue(OrderTableFavouritePropertyName);
                bool.TryParse(favString, out bool fav);

                return Equals(categoryUuid, cUuid) && favourite == fav;
            });

            if(soundOrderTableObjects.Count > 0)
            {
                bool saveNewOrder = false;
                // Remove sounds from the order when [the user is not logged in] or [the user is logged in and the sounds are synced]
                bool removeNonExistentSounds = !(App.Current as App)._itemViewHolder.User.IsLoggedIn ||
                                                ((App.Current as App)._itemViewHolder.User.IsLoggedIn && syncFinished);
                var lastOrderTableObject = soundOrderTableObjects.Last();
                List<Guid> uuids = new List<Guid>();
                List<Sound> sortedSounds = new List<Sound>();

                // Add the sounds, that are not in the order, at the end
                List<Sound> newSounds = new List<Sound>();
                foreach (var sound in sounds)
                    newSounds.Add(sound);

                foreach(var property in lastOrderTableObject.Properties)
                {
                    if (!int.TryParse(property.Name, out int index)) continue;

                    Guid? soundUuid = ConvertStringToGuid(property.Value);
                    if (!soundUuid.HasValue) continue;

                    // Check if this uuid is already in the uuids list
                    if (uuids.Contains(soundUuid.Value))
                    {
                        if (removeNonExistentSounds)
                            saveNewOrder = true;
                        continue;
                    }

                    if(!removeNonExistentSounds)
                        uuids.Add(soundUuid.Value);

                    // Get the sound from the list
                    var sound = sounds.Find(s => s.Uuid == soundUuid);
                    if (sound == null)
                    {
                        if (removeNonExistentSounds) saveNewOrder = true;
                        continue;
                    }

                    if (removeNonExistentSounds)
                        uuids.Add(soundUuid.Value);
                    sortedSounds.Add(sound);
                    newSounds.Remove(sound);
                }

                // Add the new sounds at the end
                foreach(var sound in newSounds)
                {
                    sortedSounds.Add(sound);
                    uuids.Add(sound.Uuid);
                    saveNewOrder = true;
                }

                // If there are multiple order objects, merge them
                while(soundOrderTableObjects.Count > 1)
                {
                    saveNewOrder = true;

                    // Merge the first order object into the last one
                    var firstOrderTableObject = soundOrderTableObjects.First();

                    // Go through each uuid of the order object
                    foreach(var property in firstOrderTableObject.Properties)
                    {
                        // Make sure the property is an index of the order
                        if (!int.TryParse(property.Name, out int index)) continue;

                        Guid? soundUuid = ConvertStringToGuid(property.Value);
                        if (!soundUuid.HasValue) continue;

                        //  Check if the uuid already exists in the first order object
                        if (uuids.Contains(soundUuid.Value)) continue;
                        uuids.Add(soundUuid.Value);

                        // Get the sound from the list
                        var sound = sounds.Find(s => s.Uuid == soundUuid);
                        if (sound == null) continue;

                        sortedSounds.Add(sound);
                    }

                    // Delete the object and remove it from the list
                    await firstOrderTableObject.Delete();
                    soundOrderTableObjects.Remove(firstOrderTableObject);
                }

                if (saveNewOrder)
                    await DatabaseOperations.SetSoundOrderAsync(categoryUuid, favourite, uuids);
                return sortedSounds;
            }
            else
            {
                // Create the sound order table object with the current order
                List<Guid> uuids = new List<Guid>();

                foreach (var sound in sounds)
                    uuids.Add(sound.Uuid);

                await DatabaseOperations.SetSoundOrderAsync(categoryUuid, favourite, uuids);
                return sounds;
            }
        }

        public static async Task<Category> AddCategoryAsync(Guid uuid, string name, string icon)
        {
            if (Equals(uuid, Guid.Empty))
                uuid = Guid.NewGuid();

            if (DatabaseOperations.GetObjectAsync(uuid) != null)
                return null;

            await DatabaseOperations.AddCategoryAsync(uuid, name, icon);

            await CreateCategoriesListAsync();
            return new Category(uuid, name, icon);
        }

        private static async Task<List<Category>> GetAllCategoriesAsync()
        {
            List<TableObject> categoriesTableObjectList = await DatabaseOperations.GetAllCategoriesAsync();
            List<Category> categoriesList = new List<Category>();

            foreach (var categoryTableObject in categoriesTableObjectList)
                categoriesList.Add(await GetCategoryAsync(categoryTableObject.Uuid));
            
            return await SortCategoriesListAsync(categoriesList);
        }

        private static async Task<Category> GetCategoryAsync(Guid uuid)
        {
            var categoryTableObject = await DatabaseOperations.GetObjectAsync(uuid);

            if (categoryTableObject == null || categoryTableObject.TableId != CategoryTableId)
                return null;

            return new Category(categoryTableObject.Uuid,
                                categoryTableObject.GetPropertyValue(CategoryTableNamePropertyName),
                                categoryTableObject.GetPropertyValue(CategoryTableIconPropertyName));
        }

        public static async Task UpdateCategoryAsync(Guid uuid, string name, string icon)
        {
            await DatabaseOperations.UpdateCategoryAsync(uuid, name, icon);
            await CreateCategoriesListAsync();
        }

        public static async Task DeleteCategoryAsync(Guid categoryUuid)
        {
            var categoryTableObject = await DatabaseOperations.GetObjectAsync(categoryUuid);

            if (categoryTableObject == null || categoryTableObject.TableId != CategoryTableId)
                return;

            await DatabaseOperations.DeleteObjectAsync(categoryUuid);

            // Delete the SoundOrder table objects of all deleted categories
            var tableObjects = await DatabaseOperations.GetAllOrdersAsync();
            foreach(var tableObject in tableObjects)
            {
                if (tableObject.GetPropertyValue(OrderTableTypePropertyName) != SoundOrderType) continue;
                Guid cUuid = ConvertStringToGuid(tableObject.GetPropertyValue(OrderTableCategoryPropertyName)) ?? Guid.Empty;
                if (cUuid == Guid.Empty) continue;
                
                if (!await DatabaseOperations.ObjectExistsAsync(cUuid))
                    await tableObject.Delete();
            }

            await CreateCategoriesListAsync();
        }

        private static async Task<List<Category>> SortCategoriesListAsync(List<Category> categories)
        {
            // Get the order table objects
            var tableObjects = await DatabaseOperations.GetAllOrdersAsync();

            // Check if the order table object with the type of category (0) exists
            var categoryOrderTableObjects = tableObjects.FindAll(obj => obj.GetPropertyValue(OrderTableTypePropertyName) == CategoryOrderType);

            if (categoryOrderTableObjects.Count > 0)
            {
                var lastOrderTableObject = categoryOrderTableObjects.Last();
                List<Guid> uuids = new List<Guid>();
                List<Category> sortedCategories = new List<Category>();

                List<Category> newCategories = new List<Category>();
                foreach (var category in categories)
                    newCategories.Add(category);

                foreach(var property in lastOrderTableObject.Properties)
                {
                    if (!int.TryParse(property.Name, out int index)) continue;

                    Guid? categoryUuid = ConvertStringToGuid(property.Value);
                    if (!categoryUuid.HasValue) continue;

                    // Check if this uuid is already in the uuids list
                    if (uuids.Contains(categoryUuid.Value))
                        continue;

                    // Get the category from the list
                    var category = categories.Find(c => c.Uuid == categoryUuid);
                    if (category == null) continue;

                    sortedCategories.Add(category);
                    uuids.Add(category.Uuid);
                    newCategories.Remove(category);
                }

                // Add the new categories at the end
                foreach (var category in newCategories)
                {
                    sortedCategories.Add(category);
                    uuids.Add(category.Uuid);
                }

                // If there are multiple order objects, merge them
                while (categoryOrderTableObjects.Count > 1)
                {
                    // Merge the first order object into the last one
                    var firstOrderTableObject = categoryOrderTableObjects.First();

                    // Go through each uuid of the order object
                    foreach (var property in firstOrderTableObject.Properties)
                    {
                        // Make sure the property is an index of the order
                        if (!int.TryParse(property.Name, out int index)) continue;

                        Guid? categoryUuid = ConvertStringToGuid(property.Value);
                        if (!categoryUuid.HasValue) continue;

                        //  Check if the uuid already exist in the first order object
                        if (uuids.Contains(categoryUuid.Value)) continue;

                        // Get the category from the list
                        var category = categories.Find(c => c.Uuid == categoryUuid);
                        if (category == null) continue;

                        // Add the property to uuids and sortedCategories
                        sortedCategories.Add(category);
                        uuids.Add(category.Uuid);
                    }

                    // Delete the object and remove it from the list
                    await firstOrderTableObject.Delete();
                    categoryOrderTableObjects.Remove(firstOrderTableObject);
                }
                
                await DatabaseOperations.SetCategoryOrderAsync(uuids);
                return sortedCategories;
            }
            else
            {
                // Create the category order table object with the current order
                List<Guid> uuids = new List<Guid>();

                foreach (var category in categories)
                    uuids.Add(category.Uuid);

                await DatabaseOperations.SetCategoryOrderAsync(uuids);
                return categories;
            }
        }

        public static async Task SetCategoryOrderAsync(List<Guid> uuids)
        {
            await DatabaseOperations.SetCategoryOrderAsync(uuids);
        }

        public static async Task<Guid> AddPlayingSoundAsync(Guid uuid, List<Sound> sounds, int current, int repetitions, bool randomly, double volume)
        {
            if (Equals(uuid, Guid.Empty))
                uuid = Guid.NewGuid();

            if (await DatabaseOperations.ObjectExistsAsync(uuid)) return uuid;

            if (!(App.Current as App)._itemViewHolder.SavePlayingSounds ||
                (App.Current as App)._itemViewHolder.PlayingSoundsListVisibility != Visibility.Visible)
                return uuid;

            if (volume >= 1)
                volume = 1;
            else if (volume <= 0)
                volume = 0;

            List<string> soundIds = new List<string>();
            foreach (Sound sound in sounds)
            {
                soundIds.Add(sound.Uuid.ToString());
            }

            await DatabaseOperations.AddPlayingSoundAsync(uuid, soundIds, current, repetitions, randomly, volume);

            return uuid;
        }

        public static async Task<List<PlayingSound>> GetAllPlayingSoundsAsync()
        {
            List<TableObject> playingSoundObjects = await DatabaseOperations.GetAllPlayingSoundsAsync();
            List<PlayingSound> playingSounds = new List<PlayingSound>();

            foreach (var obj in playingSoundObjects)
            {
                var playingSound = await ConvertTableObjectToPlayingSoundAsync(obj);
                if (playingSound != null) playingSounds.Add(playingSound);
            }
            return playingSounds;
        }

        public static async Task<PlayingSound> GetPlayingSoundAsync(Guid uuid)
        {
            var tableObject = await DatabaseOperations.GetObjectAsync(uuid);
            if (tableObject == null) return null;
            if (tableObject.TableId != PlayingSoundTableId) return null;

            return await ConvertTableObjectToPlayingSoundAsync(tableObject);
        }

        private static async Task<PlayingSound> ConvertTableObjectToPlayingSoundAsync(TableObject tableObject)
        {
            List<Sound> sounds = new List<Sound>();
            string soundIds = tableObject.GetPropertyValue(PlayingSoundTableSoundIdsPropertyName);

            // Get the sounds
            if (!string.IsNullOrEmpty(soundIds))
            {
                foreach (string uuidString in soundIds.Split(","))
                {
                    // Convert the uuid string into a Guid
                    Guid uuid = ConvertStringToGuid(uuidString) ?? Guid.Empty;

                    if (!Equals(uuid, Guid.Empty))
                    {
                        var sound = await GetSoundAsync(uuid);
                        if (sound != null)
                            sounds.Add(sound);
                    }
                }

                if (sounds.Count == 0)
                {
                    // Delete the playing sound
                    await DeletePlayingSoundAsync(tableObject.Uuid);
                    return null;
                }
            }
            else
            {
                // Delete the playing sound
                await DeletePlayingSoundAsync(tableObject.Uuid);
                return null;
            }

            // Get the properties of the table objects
            string currentString = tableObject.GetPropertyValue(PlayingSoundTableCurrentPropertyName);
            int.TryParse(currentString, out int current);

            string volumeString = tableObject.GetPropertyValue(PlayingSoundTableVolumePropertyName);
            double.TryParse(volumeString, out double volume);

            string repetitionsString = tableObject.GetPropertyValue(PlayingSoundTableRepetitionsPropertyName);
            int.TryParse(repetitionsString, out int repetitions);

            string randomlyString = tableObject.GetPropertyValue(PlayingSoundTableRandomlyPropertyName);
            bool.TryParse(randomlyString, out bool randomly);

            // Create the media player
            MediaPlayer player = await CreateMediaPlayerAsync(sounds, current);

            if (player != null)
            {
                player.Volume = volume;
                player.AutoPlay = false;

                PlayingSound playingSound = new PlayingSound(tableObject.Uuid, sounds, player, repetitions, randomly, current);
                return playingSound;
            }
            else
            {
                // Remove the PlayingSound from the DB
                await DeletePlayingSoundAsync(tableObject.Uuid);
                return null;
            }
        }

        public static async Task AddOrRemoveAllPlayingSoundsAsync()
        {
            // Check the settings if all playingSounds should be removed or added
            bool savePlayingSounds = true;
            bool playingSoundListVisible = true;

            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            var savePlayingSoundsSetting = localSettings.Values[savePlayingSoundsKey];
            var playingSoundsListVisibleSetting = localSettings.Values[playingSoundsListVisibleKey];

            if (savePlayingSoundsSetting != null)
                savePlayingSounds = (bool)savePlayingSoundsSetting;
            if (playingSoundsListVisibleSetting != null)
                playingSoundListVisible = (bool)playingSoundsListVisibleSetting;

            savePlayingSounds = savePlayingSounds && playingSoundListVisible;

            foreach (PlayingSound ps in (App.Current as App)._itemViewHolder.PlayingSounds)
            {
                if (savePlayingSounds)
                {
                    // Check, if the playingSound is already saved
                    if (await DatabaseOperations.GetObjectAsync(ps.Uuid) == null)
                    {
                        // Add the playingSound
                        List<string> soundIds = new List<string>();
                        foreach (Sound sound in ps.Sounds)
                            soundIds.Add(sound.Uuid.ToString());

                        await DatabaseOperations.AddPlayingSoundAsync(ps.Uuid, soundIds, ((int)((MediaPlaybackList)ps.MediaPlayer.Source).CurrentItemIndex), ps.Repetitions, ps.Randomly, ps.MediaPlayer.Volume);
                    }
                }
                else
                {
                    // Check, if the playingSound is saved
                    if (await DatabaseOperations.GetObjectAsync(ps.Uuid) != null)
                    {
                        // Remove the playingSound
                        await DatabaseOperations.DeleteObjectAsync(ps.Uuid);
                    }
                }
            }
        }

        public static async Task SetCurrentOfPlayingSoundAsync(Guid uuid, int current)
        {
            await DatabaseOperations.UpdatePlayingSoundAsync(uuid, null, current.ToString(), null, null, null);
        }

        public static async Task SetRepetitionsOfPlayingSoundAsync(Guid uuid, int repetitions)
        {
            await DatabaseOperations.UpdatePlayingSoundAsync(uuid, null, null, repetitions.ToString(), null, null);
        }

        public static async Task SetSoundsListOfPlayingSoundAsync(Guid uuid, List<Sound> sounds)
        {
            List<string> soundIds = new List<string>();
            foreach (Sound sound in sounds)
                soundIds.Add(sound.Uuid.ToString());

            await DatabaseOperations.UpdatePlayingSoundAsync(uuid, soundIds, null, null, null, null);
        }

        public static async Task SetVolumeOfPlayingSoundAsync(Guid uuid, double volume)
        {
            if (volume >= 1)
                volume = 1;
            else if (volume <= 0)
                volume = 0;

            await DatabaseOperations.UpdatePlayingSoundAsync(uuid, null, null, null, null, volume.ToString());
        }

        public static async Task DeletePlayingSoundAsync(Guid uuid)
        {
            await DatabaseOperations.DeleteObjectAsync(uuid);
        }
        
        private static async Task<StorageFile> GetTableObjectFileAsync(Guid uuid)
        {
            var fileTableObject = await DatabaseOperations.GetObjectAsync(uuid);

            if (fileTableObject == null) return null;
            if (!fileTableObject.IsFile) return null;
            if (fileTableObject.File == null) return null;

            try
            {
                return await StorageFile.GetFileFromPathAsync(fileTableObject.File.FullName);
            }
            catch(FileNotFoundException e)
            {
                Debug.WriteLine(e.Message);
                return null;
            }
        }
        
        public static async Task<StorageFile> GetAudioFileOfSoundAsync(Guid soundUuid)
        {
            var soundFileTableObject = await GetSoundFileTableObjectAsync(soundUuid);
            if (soundFileTableObject == null) return null;
            if (soundFileTableObject.File == null) return null;

            if (File.Exists(soundFileTableObject.File.FullName))
                return await StorageFile.GetFileFromPathAsync(soundFileTableObject.File.FullName);
            else
                return null;
        }

        public static async Task<string> GetAudioFileExtensionAsync(Guid soundUuid)
        {
            var soundFileTableObject = await GetSoundFileTableObjectAsync(soundUuid);
            if (soundFileTableObject == null) return null;
            return soundFileTableObject.GetPropertyValue(TableObjectExtPropertyName);
        }

        public static async Task<StorageFile> GetImageFileOfSoundAsync(Guid soundUuid)
        {
            var imageFileTableObject = await GetImageFileTableObjectAsync(soundUuid);
            if (imageFileTableObject == null) return null;
            if (imageFileTableObject.File == null) return null;

            if (File.Exists(imageFileTableObject.File.FullName))
                return await StorageFile.GetFileFromPathAsync(imageFileTableObject.File.FullName);
            else
                return null;
        }

        public static async Task<Uri> GetAudioUriOfSoundAsync(Guid soundUuid)
        {
            var soundTableObject = await GetSoundFileTableObjectAsync(soundUuid);
            if (soundTableObject != null)
                return soundTableObject.GetFileUri();
            else
                return null;
        }

        public static async Task<MemoryStream> GetAudioStreamOfSoundAsync(Guid soundUuid)
        {
            var soundTableObject = await GetSoundFileTableObjectAsync(soundUuid);
            if (soundTableObject != null)
                return await soundTableObject.GetFileStream();
            else
                return null;
        }

        public static async Task DownloadAudioFileOfSoundAsync(Guid soundUuid, Progress<int> progress)
        {
            var soundTableObject = await GetSoundFileTableObjectAsync(soundUuid);
            if (soundTableObject != null)
                soundTableObject.DownloadFile(progress);
        }
        
        public static async Task<DownloadStatus> GetSoundFileDownloadStatusAsync(Guid soundUuid)
        {
            var soundTableObject = await GetSoundFileTableObjectAsync(soundUuid);
            if (soundTableObject != null)
            {
                switch (soundTableObject.DownloadStatus)
                {
                    case TableObject.TableObjectDownloadStatus.NoFileOrNotLoggedIn:
                        return DownloadStatus.NoFileOrNotLoggedIn;
                    case TableObject.TableObjectDownloadStatus.NotDownloaded:
                        return DownloadStatus.NotDownloaded;
                    case TableObject.TableObjectDownloadStatus.Downloading:
                        return DownloadStatus.Downloading;
                    case TableObject.TableObjectDownloadStatus.Downloaded:
                        return DownloadStatus.Downloaded;
                }
            }
            return DownloadStatus.NoFileOrNotLoggedIn;
        }

        private static async Task<TableObject> GetSoundFileTableObjectAsync(Guid soundUuid)
        {
            var soundTableObject = await DatabaseOperations.GetObjectAsync(soundUuid);
            if (soundTableObject == null) return null;
            Guid soundFileUuid = ConvertStringToGuid(soundTableObject.GetPropertyValue(SoundTableSoundUuidPropertyName)) ?? Guid.Empty;
            if (Equals(soundFileUuid, Guid.Empty)) return null;
            var soundFileTableObject = await DatabaseOperations.GetObjectAsync(soundFileUuid);
            if (soundFileTableObject == null) return null;
            return soundFileTableObject;
        }

        private static async Task<TableObject> GetImageFileTableObjectAsync(Guid soundUuid)
        {
            var soundTableObject = await DatabaseOperations.GetObjectAsync(soundUuid);
            if (soundTableObject == null) return null;
            Guid imageFileUuid = ConvertStringToGuid(soundTableObject.GetPropertyValue(SoundTableImageUuidPropertyName)) ?? Guid.Empty;
            if (Equals(imageFileUuid, Guid.Empty)) return null;
            var imageFileTableObject = await DatabaseOperations.GetObjectAsync(imageFileUuid);
            if (imageFileTableObject == null) return null;
            return imageFileTableObject;
        }
        #endregion

        #region UI Methods
        public static bool AreTopButtonsNormal()
        {
            return (App.Current as App)._itemViewHolder.NormalOptionsVisibility
                    && (((App.Current as App)._itemViewHolder.SearchAutoSuggestBoxVisibility && Window.Current.Bounds.Width >= hideSearchBoxMaxWidth)
                        || ((App.Current as App)._itemViewHolder.SearchButtonVisibility && Window.Current.Bounds.Width < hideSearchBoxMaxWidth));
        }

        public static void CheckBackButtonVisibility()
        {
            // Is false if SoundPage shows All Sounds and button are normal
            (App.Current as App)._itemViewHolder.IsBackButtonEnabled = !(AreTopButtonsNormal()
                                                                        && (App.Current as App)._itemViewHolder.SelectedCategory == 0
                                                                        && string.IsNullOrEmpty((App.Current as App)._itemViewHolder.SearchQuery));
        }
        
        // Go to the Sounds page and show all sounds
        public static async Task ShowAllSoundsAsync()
        {
            if (AreTopButtonsNormal())
                (App.Current as App)._itemViewHolder.IsBackButtonEnabled = false;
            skipAutoSuggestBoxTextChanged = true;
            (App.Current as App)._itemViewHolder.SearchQuery = "";
            (App.Current as App)._itemViewHolder.SelectedCategory = 0;
            (App.Current as App)._itemViewHolder.EditButtonVisibility = Visibility.Collapsed;
            (App.Current as App)._itemViewHolder.Title = new Windows.ApplicationModel.Resources.ResourceLoader().GetString("AllSounds");
            (App.Current as App)._itemViewHolder.Page = typeof(SoundPage);
            await LoadAllSoundsAsync();
            ShowPlayAllButton();
            skipAutoSuggestBoxTextChanged = false;
        }

        public static void AdjustLayout()
        {
            double width = Window.Current.Bounds.Width;

            (App.Current as App)._itemViewHolder.TopButtonsCollapsed = width < topButtonsCollapsedMaxWidth;
            (App.Current as App)._itemViewHolder.SelectButtonVisibility = !(width < moveSelectButtonMaxWidth);
            (App.Current as App)._itemViewHolder.AddButtonVisibility = !(width < moveAddButtonMaxWidth);
            (App.Current as App)._itemViewHolder.VolumeButtonVisibility = !(width < moveVolumeButtonMaxWidth);
            (App.Current as App)._itemViewHolder.ShareButtonVisibility = !(width < moveAddButtonMaxWidth);
            (App.Current as App)._itemViewHolder.CancelButtonVisibility = !(width < hideSearchBoxMaxWidth);
            (App.Current as App)._itemViewHolder.MoreButtonVisibility = width < moveSelectButtonMaxWidth || !(App.Current as App)._itemViewHolder.NormalOptionsVisibility;

            if (string.IsNullOrEmpty((App.Current as App)._itemViewHolder.SearchQuery))
            {
                (App.Current as App)._itemViewHolder.SearchAutoSuggestBoxVisibility = !(width < hideSearchBoxMaxWidth);
                (App.Current as App)._itemViewHolder.SearchButtonVisibility = (width < hideSearchBoxMaxWidth);
            }

            CheckBackButtonVisibility();
        }

        public static void ShowPlayAllButton()
        {
            if ((App.Current as App)._itemViewHolder.Page != typeof(SoundPage) ||
                (App.Current as App)._itemViewHolder.ProgressRingIsActive ||
                (App.Current as App)._itemViewHolder.Sounds.Count == 0)
            {
                (App.Current as App)._itemViewHolder.PlayAllButtonVisibility = Visibility.Collapsed;
            }
            else
            {
                (App.Current as App)._itemViewHolder.PlayAllButtonVisibility = Visibility.Visible;
            }
        }

        public static async Task UpdateGridViewAsync()
        {
            if (updatingGridView) return;
            updatingGridView = true;
            int selectedCategoryIndex = (App.Current as App)._itemViewHolder.SelectedCategory;
            Category selectedCategory = (App.Current as App)._itemViewHolder.Categories[selectedCategoryIndex];

            if (selectedCategory != null)
            {
                if ((App.Current as App)._itemViewHolder.SearchQuery == "")
                {
                    if (selectedCategoryIndex == 0)
                    {
                        await LoadAllSoundsAsync();
                        (App.Current as App)._itemViewHolder.EditButtonVisibility = Visibility.Collapsed;
                    }
                    else if ((App.Current as App)._itemViewHolder.Page != typeof(SoundPage))
                    {
                        (App.Current as App)._itemViewHolder.EditButtonVisibility = Visibility.Collapsed;
                    }
                    else
                    {
                        await LoadSoundsByCategoryAsync(selectedCategory.Uuid);
                        (App.Current as App)._itemViewHolder.EditButtonVisibility = Visibility.Visible;
                    }
                }
                else
                {
                    (App.Current as App)._itemViewHolder.EditButtonVisibility = Visibility.Collapsed;
                    await LoadSoundsByNameAsync((App.Current as App)._itemViewHolder.SearchQuery);
                }
            }
            else
            {
                await LoadAllSoundsAsync();
                (App.Current as App)._itemViewHolder.EditButtonVisibility = Visibility.Collapsed;
            }

            updatingGridView = false;

            // Check if another category was selected
            if (selectedCategoryIndex != (App.Current as App)._itemViewHolder.SelectedCategory)
            {
                // Update UI
                await UpdateGridViewAsync();
            }
            ShowPlayAllButton();
        }

        public static async Task ShowCategoryAsync(Guid uuid)
        {
            var categoryTableObject = await DatabaseOperations.GetObjectAsync(uuid);

            if (categoryTableObject != null && categoryTableObject.TableId == CategoryTableId)
            {
                Category category = new Category(categoryTableObject.Uuid, categoryTableObject.GetPropertyValue(CategoryTableNamePropertyName), categoryTableObject.GetPropertyValue(CategoryTableIconPropertyName));

                skipAutoSuggestBoxTextChanged = true;
                (App.Current as App)._itemViewHolder.SearchQuery = "";
                (App.Current as App)._itemViewHolder.Page = typeof(SoundPage);
                (App.Current as App)._itemViewHolder.Title = WebUtility.HtmlDecode(category.Name);
                (App.Current as App)._itemViewHolder.IsBackButtonEnabled = true;
                (App.Current as App)._itemViewHolder.EditButtonVisibility = Visibility.Visible;
                await LoadSoundsByCategoryAsync(category.Uuid);
                SelectCategory(category.Uuid);
            }
            else
                await ShowAllSoundsAsync();
        }

        public static void ResetSearchArea()
        {
            skipAutoSuggestBoxTextChanged = true;

            if (Window.Current.Bounds.Width < hideSearchBoxMaxWidth)
            {
                // Clear text and show buttons
                (App.Current as App)._itemViewHolder.SearchAutoSuggestBoxVisibility = false;
                (App.Current as App)._itemViewHolder.SearchButtonVisibility = true;
            }
            AdjustLayout();
        }

        public static void SwitchSelectionMode()
        {
            if ((App.Current as App)._itemViewHolder.SelectionMode == ListViewSelectionMode.None)
            {   // If Normal view
                (App.Current as App)._itemViewHolder.SelectionMode = ListViewSelectionMode.Multiple;
                (App.Current as App)._itemViewHolder.NormalOptionsVisibility = false;
                (App.Current as App)._itemViewHolder.AreSelectButtonsEnabled = false;
            }
            else
            {   // If selection view
                (App.Current as App)._itemViewHolder.SelectionMode = ListViewSelectionMode.None;
                (App.Current as App)._itemViewHolder.SelectedSounds.Clear();
                (App.Current as App)._itemViewHolder.NormalOptionsVisibility = true;
                (App.Current as App)._itemViewHolder.AreSelectButtonsEnabled = true;

                if (!string.IsNullOrEmpty((App.Current as App)._itemViewHolder.SearchQuery))
                {
                    (App.Current as App)._itemViewHolder.SearchAutoSuggestBoxVisibility = true;
                    (App.Current as App)._itemViewHolder.SearchButtonVisibility = false;
                }
            }
            AdjustLayout();
        }

        public static void ResetTopButtons()
        {
            if ((App.Current as App)._itemViewHolder.SelectionMode != ListViewSelectionMode.None)
                SwitchSelectionMode();
            else
                ResetSearchArea();
        }

        public static async Task GoBackAsync()
        {
            if (!AreTopButtonsNormal())
            {
                ResetTopButtons();
            }
            else
            {
                if ((App.Current as App)._itemViewHolder.Page != typeof(SoundPage))
                {   // If Settings Page or AccountPage is visible
                    // Go to All sounds page
                    (App.Current as App)._itemViewHolder.Page = typeof(SoundPage);
                    (App.Current as App)._itemViewHolder.SelectedCategory = 0;
                    (App.Current as App)._itemViewHolder.Title = new Windows.ApplicationModel.Resources.ResourceLoader().GetString("AllSounds");
                    (App.Current as App)._itemViewHolder.EditButtonVisibility = Visibility.Collapsed;
                    await ShowAllSoundsAsync();
                }
                else if ((App.Current as App)._itemViewHolder.SelectedCategory == 0 &&
                        string.IsNullOrEmpty((App.Current as App)._itemViewHolder.SearchQuery))
                {   // If SoundPage shows AllSounds

                }
                else
                {   // If SoundPage shows Category or search results
                    // Top Buttons are normal, but page shows Category or search results
                    await ShowAllSoundsAsync();
                }
            }

            CheckBackButtonVisibility();
        }

        public static void UpdateLayoutColors()
        {
            // Set the background of the SideBar and the PlayingSoundsBar
            if ((App.Current as App)._itemViewHolder.ShowAcrylicBackground)
            {   // If the acrylic background is enabled
                Color appThemeColor = GetApplicationThemeColor();

                // Add the transparency effect
                (Application.Current.Resources["NavigationViewExpandedPaneBackground"] as AcrylicBrush).BackgroundSource = AcrylicBackgroundSource.HostBackdrop;
                (App.Current as App)._itemViewHolder.PlayingSoundsBarAcrylicBackgroundBrush.BackgroundSource = AcrylicBackgroundSource.HostBackdrop;

                // Set the default tint opacity
                (Application.Current.Resources["NavigationViewExpandedPaneBackground"] as AcrylicBrush).TintOpacity = sideBarAcrylicBackgroundTintOpacity;
                (App.Current as App)._itemViewHolder.PlayingSoundsBarAcrylicBackgroundBrush.TintOpacity = playingSoundsBarAcrylicBackgroundTintOpacity;

                // Set the tint color
                (Application.Current.Resources["NavigationViewExpandedPaneBackground"] as AcrylicBrush).TintColor = appThemeColor;
                (App.Current as App)._itemViewHolder.PlayingSoundsBarAcrylicBackgroundBrush.TintColor = appThemeColor;
            }
            else if ((App.Current as App).RequestedTheme == ApplicationTheme.Light)
            {   // If the acrylic background is disabled and the theme is Light
                // Remove the transparency effect
                (Application.Current.Resources["NavigationViewExpandedPaneBackground"] as AcrylicBrush).BackgroundSource = AcrylicBackgroundSource.Backdrop;
                (App.Current as App)._itemViewHolder.PlayingSoundsBarAcrylicBackgroundBrush.BackgroundSource = AcrylicBackgroundSource.Backdrop;

                // Set the background color
                (Application.Current.Resources["NavigationViewExpandedPaneBackground"] as AcrylicBrush).TintColor = sideBarLightBackgroundColor;
                (App.Current as App)._itemViewHolder.PlayingSoundsBarAcrylicBackgroundBrush.TintColor = playingSoundsBarLightBackgroundColor;
            }
            else
            {   // If the acrylic background is disabled and the theme is dark
                // Remove the transparency effect
                (Application.Current.Resources["NavigationViewExpandedPaneBackground"] as AcrylicBrush).BackgroundSource = AcrylicBackgroundSource.Backdrop;
                (App.Current as App)._itemViewHolder.PlayingSoundsBarAcrylicBackgroundBrush.BackgroundSource = AcrylicBackgroundSource.Backdrop;

                // Set the background color
                (Application.Current.Resources["NavigationViewExpandedPaneBackground"] as AcrylicBrush).TintColor = sideBarDarkBackgroundColor;
                (App.Current as App)._itemViewHolder.PlayingSoundsBarAcrylicBackgroundBrush.TintColor = playingSoundsBarDarkBackgroundColor;
            }
        }
        #endregion

        #region General Methods
        public static async Task CreateCategoriesListAsync()
        {
            int selectedCategory = (App.Current as App)._itemViewHolder.SelectedCategory;
            
            (App.Current as App)._itemViewHolder.Categories.Clear();
            (App.Current as App)._itemViewHolder.Categories.Add(new Category(Guid.Empty, new Windows.ApplicationModel.Resources.ResourceLoader().GetString("AllSounds"), "\uE10F"));

            foreach (Category cat in await GetAllCategoriesAsync())
                (App.Current as App)._itemViewHolder.Categories.Add(cat);
            (App.Current as App)._itemViewHolder.SelectedCategory = selectedCategory;
        }

        public static async Task UpdatePlayingSoundListItemAsync(Guid uuid)
        {
            var playingSound = await GetPlayingSoundAsync(uuid);
            if (playingSound == null) return;

            var currentPlayingSoundList = (App.Current as App)._itemViewHolder.PlayingSounds.Where(p => p.Uuid == playingSound.Uuid);
            if (currentPlayingSoundList.Count() == 0) return;
            var currentPlayingSound = currentPlayingSoundList.First();
            if (currentPlayingSound.MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing) return;
            int index = (App.Current as App)._itemViewHolder.PlayingSounds.IndexOf(currentPlayingSound);

            // Replace the playing sound
            (App.Current as App)._itemViewHolder.PlayingSounds.RemoveAt(index);
            (App.Current as App)._itemViewHolder.PlayingSounds.Insert(index, playingSound);
        }

        public static async Task CreatePlayingSoundsListAsync()
        {
            var allPlayingSounds = await GetAllPlayingSoundsAsync();
            foreach (PlayingSound ps in allPlayingSounds)
            {
                if (ps.MediaPlayer != null)
                {
                    var currentPlayingSoundList = (App.Current as App)._itemViewHolder.PlayingSounds.Where(p => p.Uuid == ps.Uuid);

                    if (currentPlayingSoundList.Count() > 0)
                    {
                        var currentPlayingSound = currentPlayingSoundList.First();
                        int index = (App.Current as App)._itemViewHolder.PlayingSounds.IndexOf(currentPlayingSound);

                        // Update the current playing sound if it is currently not playing
                        if (currentPlayingSound.MediaPlayer.PlaybackSession.PlaybackState != MediaPlaybackState.Playing)
                        {
                            // Check if the playing sound changed
                            bool soundWasUpdated = false;

                            soundWasUpdated = currentPlayingSound.Randomly != ps.Randomly ||
                                                currentPlayingSound.Repetitions != ps.Repetitions ||
                                                currentPlayingSound.Sounds.Count != ps.Sounds.Count;

                            if (currentPlayingSound.MediaPlayer != null && ps.MediaPlayer != null && !soundWasUpdated)
                                soundWasUpdated = currentPlayingSound.MediaPlayer.Volume != ps.MediaPlayer.Volume;

                            if (soundWasUpdated)
                            {
                                // Replace the playing sound
                                (App.Current as App)._itemViewHolder.PlayingSounds.RemoveAt(index);
                                (App.Current as App)._itemViewHolder.PlayingSounds.Insert(index, ps);
                            }
                        }
                    }
                    else
                    {
                        // Add the new playing sound
                        (App.Current as App)._itemViewHolder.PlayingSounds.Add(ps);
                    }
                }
            }
            
            // Remove old playing sounds
            foreach(var ps in (App.Current as App)._itemViewHolder.PlayingSounds)
            {
                // Remove the playing sound from ItemViewHolder if it does not exist in the new playing sounds list
                if (allPlayingSounds.Where(p => p.Uuid == ps.Uuid).Count() == 0)
                    (App.Current as App)._itemViewHolder.PlayingSounds.Remove(ps);
            }
        }

        public static void SelectCategory(Guid uuid)
        {
            for (int i = 0; i < (App.Current as App)._itemViewHolder.Categories.Count(); i++)
                if (Equals((App.Current as App)._itemViewHolder.Categories[i].Uuid, uuid))
                    (App.Current as App)._itemViewHolder.SelectedCategory = i;
        }

        private static List<Sound> SortSoundsList(List<Sound> sounds, SoundOrder order, bool reversed, Guid categoryUuid, bool favourite)
        {
            List<Sound> sortedSounds = new List<Sound>();

            switch (order)
            {
                case SoundOrder.Name:
                    sounds.Sort((x, y) => string.Compare(x.Name, y.Name));

                    foreach (var sound in sounds)
                        sortedSounds.Add(sound);

                    if (reversed)
                        sortedSounds.Reverse();

                    break;
                case SoundOrder.CreationDate:
                    foreach (var sound in sounds)
                        sortedSounds.Add(sound);

                    if (reversed)
                        sortedSounds.Reverse();

                    break;
                default:
                    // Custom order
                    foreach (var sound in SortSoundsByCustomOrder(sounds, categoryUuid, favourite))
                        sortedSounds.Add(sound);

                    break;
            }

            return sortedSounds;
        }

        private static List<Sound> SortSoundsByCustomOrder(List<Sound> sounds, Guid categoryUuid, bool favourites)
        {
            List<Sound> sortedSounds = new List<Sound>();

            if (favourites)
            {
                if (!CustomFavouriteSoundOrder.ContainsKey(categoryUuid)) return sounds;

                foreach (var uuid in CustomFavouriteSoundOrder[categoryUuid])
                {
                    var sound = sounds.Find(s => s.Uuid == uuid);
                    if (sound != null)
                        sortedSounds.Add(sound);
                }
            }
            else
            {
                if (!CustomSoundOrder.ContainsKey(categoryUuid)) return sounds;

                foreach (var uuid in CustomSoundOrder[categoryUuid])
                {
                    var sound = sounds.Find(s => s.Uuid == uuid);
                    if (sound != null)
                        sortedSounds.Add(sound);
                }
            }

            return sortedSounds;
        }

        public static async Task UpdateLiveTileAsync()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            bool isLiveTileOn = false;

            if (localSettings.Values["liveTile"] == null)
            {
                localSettings.Values["liveTile"] = liveTileDefault;
                isLiveTileOn = liveTileDefault;
            }
            else
            {
                isLiveTileOn = (bool)localSettings.Values["liveTile"];
            }

            if ((App.Current as App)._itemViewHolder.AllSounds.Count == 0 || !isLiveTileOn)
            {
                TileUpdateManager.CreateTileUpdaterForApplication().Clear();
                return;
            }

            List<Sound> sounds = new List<Sound>();
            // Get sound with image
            foreach (Sound s in (App.Current as App)._itemViewHolder.AllSounds)
            {
                if(await s.GetImageFileAsync() != null)
                    sounds.Add(s);
            }

            Sound sound;
            if (sounds.Count == 0) return;

            Random random = new Random();
            sound = sounds.ElementAt(random.Next(sounds.Count));
            StorageFile imageFile = await sound.GetImageFileAsync();
            if (imageFile == null) return;

            NotificationsExtensions.Tiles.TileBinding binding = new NotificationsExtensions.Tiles.TileBinding()
            {
                Branding = NotificationsExtensions.Tiles.TileBranding.NameAndLogo,

                Content = new NotificationsExtensions.Tiles.TileBindingContentAdaptive()
                {
                    PeekImage = new NotificationsExtensions.Tiles.TilePeekImage()
                    {
                        Source = imageFile.Path
                    },
                    Children =
                    {
                        new NotificationsExtensions.AdaptiveText()
                        {
                            Text = sound.Name
                        }
                    },
                    TextStacking = NotificationsExtensions.Tiles.TileTextStacking.Center
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

            // Create the tile notification
            var notification = new TileNotification(content.GetXml());

            // Send the notification
            TileUpdateManager.CreateTileUpdaterForApplication().Update(notification);
        }

        public static async Task SetSoundBoardSizeTextAsync()
        {
            if ((App.Current as App)._itemViewHolder.ProgressRingIsActive)
            {
                await Task.Delay(1000);
                await SetSoundBoardSizeTextAsync();
            }

            // Copy AllSounds
            List<Sound> allSounds = new List<Sound>();
            foreach (var sound in (App.Current as App)._itemViewHolder.AllSounds)
                allSounds.Add(sound);

            float totalSize = 0;
            foreach (Sound sound in allSounds)
            {
                float size = 0;
                var soundAudioFile = await sound.GetAudioFileAsync();
                if(soundAudioFile != null)
                    size = await GetFileSizeInGBAsync(soundAudioFile);

                var soundImageFile = await sound.GetImageFileAsync();
                if(soundImageFile != null)
                    size += await GetFileSizeInGBAsync(soundImageFile);

                totalSize += size;
            }

            (App.Current as App)._itemViewHolder.SoundboardSize = string.Format(new Windows.ApplicationModel.Resources.ResourceLoader().GetString("SettingsSoundBoardSize"), totalSize.ToString("n2") + " GB");
        }

        public static async Task<MediaPlayer> CreateMediaPlayerAsync(List<Sound> sounds, int current)
        {
            if (sounds.Count == 0) return null;

            MediaPlayer player = new MediaPlayer();
            MediaPlaybackList mediaPlaybackList = new MediaPlaybackList();

            foreach (Sound sound in sounds)
            {
                // Check if the sound was downloaded
                StorageFile audioFile = await sound.GetAudioFileAsync();
                MediaPlaybackItem mediaPlaybackItem;

                if (audioFile == null)
                {
                    Uri soundUri = await sound.GetAudioUriAsync();
                    if (soundUri == null) continue;

                    mediaPlaybackItem = new MediaPlaybackItem(MediaSource.CreateFromUri(soundUri));
                }
                else
                    mediaPlaybackItem = new MediaPlaybackItem(MediaSource.CreateFromStorageFile(audioFile));

                MediaItemDisplayProperties props = mediaPlaybackItem.GetDisplayProperties();
                props.Type = MediaPlaybackType.Music;
                props.MusicProperties.Title = sound.Name;

                if(sound.Categories.Count > 0)
                    props.MusicProperties.Artist = sound.Categories.First().Name;

                var imageFile = await sound.GetImageFileAsync();
                if (imageFile != null)
                    props.Thumbnail = RandomAccessStreamReference.CreateFromFile(imageFile);

                mediaPlaybackItem.ApplyDisplayProperties(props);

                mediaPlaybackList.Items.Add(mediaPlaybackItem);
            }

            player.Source = mediaPlaybackList;
            player.AutoPlay = true;

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

            if (mediaPlaybackList.Items.Count == 0)
                return null;

            if (mediaPlaybackList.Items.Count >= current + 1)
                mediaPlaybackList.MoveTo((uint)current);
            else
                mediaPlaybackList.MoveTo(0);

            return player;
        }

        public static async Task<bool> UsesDavDataModelAsync()
        {
            StorageFolder localStorageFolder = ApplicationData.Current.LocalFolder;

            // If there is a images, sounds or soundDetails folder
            var soundsFolder = await localStorageFolder.TryGetItemAsync("sounds");
            var imagesFolder = await localStorageFolder.TryGetItemAsync("images");
            var soundDetailsFolder = await localStorageFolder.TryGetItemAsync("soundDetails");

            return soundsFolder == null && imagesFolder == null && soundDetailsFolder == null;
        }

        public static Color GetApplicationThemeColor()
        {
            return (App.Current as App).RequestedTheme == ApplicationTheme.Dark ? ((Color)Application.Current.Resources["DarkThemeBackgroundColor"]) : ((Color)Application.Current.Resources["LightThemeBackgroundColor"]);
        }
        #endregion

        #region Helper Methods
        public static async Task<float> GetFileSizeInGBAsync(StorageFile file)
        {
            BasicProperties pro = await file.GetBasicPropertiesAsync();
            return (((pro.Size / 1024f) / 1024f) / 1024f);
        }

        public static string HTMLEncodeSpecialChars(string text)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in text)
            {
                if (c > 127) // special chars
                    sb.Append(string.Format("&#{0};", (int)c));
                else
                    sb.Append(c);
            }
            return sb.ToString();
        }

        public static List<string> GetIconsList()
        {
            List<string> Icons = new List<string>
            {
                "\uE710",
                "\uE70F",
                "\uE711",
                "\uE71E",
                "\uE73E",
                "\uE73A",
                "\uE77B",
                "\uE716",
                "\uE8B8",
                "\uE7EE",
                "\uE76E",
                "\uE899",
                "\uE8E1",
                "\uE8E0",
                "\uE734",
                "\uE735",
                "\uE717",
                "\uE715",
                "\uE8C3",
                "\uE910",
                "\uE722",
                "\uE714",
                "\uE8AA",
                "\uE786",
                "\uE720",
                "\uE8D6",
                "\uE90B",
                "\uE767",
                "\uE74F",
                "\uE768",
                "\uE769",
                "\uE8B1",
                "\uE718",
                "\uE77A",
                "\uE72C",
                "\uE895",
                "\uE7AD",
                "\uE8EB",
                "\uE72D",
                "\uE8BD",
                "\uE90A",
                "\uE8F3",
                "\uE7C1",
                "\uE77F",
                "\uE7C3",
                "\uE7EF",
                "\uE730",
                "\uE890",
                "\uE81D",
                "\uE896",
                "\uE897",
                "\uE8C9",
                "\uE71C",
                "\uE71B",
                "\uE723",
                "\uE8D7",
                "\uE713",
                "\uE765",
                "\uE8EA",
                "\uE74E",
                "\uE74D",
                "\uE8EC",
                "\uE8EF",
                "\uE8F1",
                "\uE719",
                "\uE90F",
                "\uE8C6",
                "\uE774",
                "\uE909",
                "\uE707",
                "\uE8F0",
                "\uE80F",
                "\uE913",
                "\uE753"
            };

            return Icons;
        }

        public static Guid? ConvertStringToGuid(string uuidString)
        {
            Guid uuid = Guid.Empty;
            if (!Guid.TryParse(uuidString, out uuid)) return null;
            return uuid;
        }

        // http://windowsapptutorials.com/tips/convert-storage-file-to-byte-array-in-universal-windows-apps/
        public static async Task<byte[]> GetBytesAsync(StorageFile file)
        {
            byte[] fileBytes = null;
            if (file == null) return null;
            using (var stream = await file.OpenReadAsync())
            {
                fileBytes = new byte[stream.Size];
                using (var reader = new DataReader(stream))
                {
                    await reader.LoadAsync((uint)stream.Size);
                    reader.ReadBytes(fileBytes);
                }
            }
            return fileBytes;
        }
        #endregion

        #region Old Methods
        public static async Task<ObservableCollection<Category>> GetCategoriesListAsync(StorageFile dataFile)
        {
            string data = await FileIO.ReadTextAsync(dataFile);

            //Deserialize Json
            var serializer = new DataContractJsonSerializer(typeof(Data));
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(data));
            var dataReader = (Data)serializer.ReadObject(ms);

            ObservableCollection<Category> categoriesList = dataReader.Categories;
            foreach (Category category in categoriesList)
            {
                category.Name = WebUtility.HtmlDecode(category.Name);
            }

            return categoriesList;
        }

        public static async Task WriteFileAsync(StorageFile file, object objectToWrite)
        {
            DataContractJsonSerializer js = new DataContractJsonSerializer(objectToWrite.GetType());
            MemoryStream ms = new MemoryStream();
            js.WriteObject(ms, objectToWrite);

            ms.Position = 0;
            StreamReader sr = new StreamReader(ms);
            string data = sr.ReadToEnd();

            await FileIO.WriteTextAsync(file, data);
        }

        public static async Task<NewData> GetDataFromFileAsync(StorageFile dataFile)
        {
            string data = await FileIO.ReadTextAsync(dataFile);

            //Deserialize Json
            var serializer = new DataContractJsonSerializer(typeof(NewData));
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(data));
            var dataReader = (NewData)serializer.ReadObject(ms);

            return dataReader;
        }
        #endregion
    }
}
