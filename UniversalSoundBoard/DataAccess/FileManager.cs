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
using Windows.UI.Xaml.Media.Imaging;

namespace UniversalSoundBoard.DataAccess
{
    public class FileManager
    {
        #region Variables
        // Variables for the localSettings keys
        public const string playingSoundsListVisibleKey = "playingSoundsListVisible";
        public const string playOneSoundAtOnceKey = "playOneSoundAtOnce";
        public const string liveTileKey = "liveTile";
        public const string showCategoryIconKey = "showCategoryIcon";
        public const string showSoundsPivotKey = "showSoundsPivot";
        public const string savePlayingSoundsKey = "savePlayingSounds";
        public const string themeKey = "theme";
        public const string showAcrylicBackgroundKey = "showAcrylicBackground";
        public const string davKey = "dav";

        // Variables for defaults
        public const double volume = 1.0;
        public const bool liveTile = true;
        public const bool playingSoundsListVisible = true;
        public const bool playOneSoundAtOnce = false;
        public const string theme = "system";
        public const bool showCategoryIcon = true;
        public const bool showSoundsPivot = true;
        public const bool savePlayingSounds = true;
        public const bool showAcrylicBackground = true;
        public const int mobileMaxWidth = 550;
        public const int tabletMaxWidth = 650;
        public const int topButtonsCollapsedMaxWidth = 1400;
        public const int sideBarCollapsedMaxWidth = 1100;
        public const int moveSelectButtonMaxWidth = 850;
        public const int moveAddButtonMaxWidth = 800;
        public const int moveVolumeButtonMaxWidth = 750;
        public const int hideSearchBoxMaxWidth = 700;

        public static bool skipAutoSuggestBoxTextChanged = false;

        public static DavEnvironment Environment = DavEnvironment.Production;

        // dav Keys
        private const string ApiKeyProduction = "gHgHKRbIjdguCM4cv5481hdiF5hZGWZ4x12Ur-7v";  // Prod
        public const string ApiKeyDevelopment = "eUzs3PQZYweXvumcWvagRHjdUroGe5Mo7kN1inHm";    // Dev
        public static string ApiKey => Environment == DavEnvironment.Production ? ApiKeyProduction : ApiKeyDevelopment;

        private const string LoginImplicitUrlProduction = "https://dav-apps.tech/login_implicit";
        private const string LoginImplicitUrlDevelopment = "https://8a102870.ngrok.io/login_implicit";
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

        public const string TableObjectExtPropertyName = "ext";

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
        private static bool updatingGridView = false;
        #endregion
        
        #region Filesystem Methods
        private static async Task<StorageFolder> GetExportFolderAsync()
        {
            StorageFolder localFolder = ApplicationData.Current.LocalCacheFolder;
            string exportFolderName = "export";

            if (await localFolder.TryGetItemAsync(exportFolderName) == null)
            {
                return await localFolder.CreateFolderAsync(exportFolderName);
            }
            else
            {
                return await localFolder.GetFolderAsync(exportFolderName);
            }
        }

        private async static Task<StorageFolder> GetImportFolderAsync()
        {
            StorageFolder localDataFolder = ApplicationData.Current.LocalCacheFolder;
            string importFolderName = "import";

            StorageFolder importFolder;
            if (await localDataFolder.TryGetItemAsync(importFolderName) == null)
            {
                importFolder = await localDataFolder.CreateFolderAsync(importFolderName);
            }
            else
            {
                importFolder = await localDataFolder.GetFolderAsync(importFolderName);
            }
            return importFolder;
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
            {
                await item.DeleteAsync();
            }
        }

        private static async Task<DataModel> GetDataModel(StorageFolder root)
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

        public static void RemoveNotLocallySavedSounds()
        {
            // Get each sound and check if the file exists
            foreach(var sound in (App.Current as App)._itemViewHolder.AllSounds)
            {
                var soundFileTableObject = GetSoundFileTableObject(sound.Uuid);
                if(soundFileTableObject != null)
                {
                    if (soundFileTableObject.FileDownloaded())
                    {
                        continue;
                    }
                }

                // Completely remove the sound from the database so that it won't be deleted when the user logs in again
                var imageFileTableObject = GetImageFileTableObject(sound.Uuid);
                var soundTableObject = DatabaseOperations.GetObject(sound.Uuid);

                if (soundFileTableObject != null)
                {
                    Dav.Database.DeleteTableObject(soundFileTableObject);
                    Dav.Database.DeleteTableObject(soundFileTableObject);
                }
                if(imageFileTableObject != null)
                {
                    Dav.Database.DeleteTableObject(imageFileTableObject);
                    Dav.Database.DeleteTableObject(imageFileTableObject);
                }
                if(soundTableObject != null)
                {
                    Dav.Database.DeleteTableObject(soundTableObject);
                    Dav.Database.DeleteTableObject(soundTableObject);
                }
            }

            (App.Current as App)._itemViewHolder.AllSoundsChanged = true;
        }
        #endregion

        #region Database Methods
        public static async Task MigrateData()
        {
            if (await UsesDavDataModel())
                return;

            StorageFolder localDataFolder = ApplicationData.Current.LocalFolder;

            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                (App.Current as App)._itemViewHolder.IsImporting = true;
                (App.Current as App)._itemViewHolder.UpgradeDataStatusText =
                                    new Windows.ApplicationModel.Resources.ResourceLoader().GetString("UpgradeDataStatusMessage-Preparing");
            });

            // Check if the data model is new or old
            DataModel dataModel = await GetDataModel(localDataFolder);
            Progress<int> progress = new Progress<int>(UpgradeDataProgress);

            if(dataModel == DataModel.Old)
            {
                await UpgradeOldDataModel(localDataFolder, false, progress);
            }
            else if(dataModel == DataModel.New)
            {
                await UpgradeNewDataModel(localDataFolder, false, progress);
            }

            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () => {
                CreateCategoriesList();
                (App.Current as App)._itemViewHolder.AllSoundsChanged = true;
                (App.Current as App)._itemViewHolder.IsImporting = false;
                await ClearCacheAsync();
            });
        }

        private static void UpgradeDataProgress(int value)
        {
            CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                (App.Current as App)._itemViewHolder.UpgradeDataStatusText = value + " %";
            });
        }

        public static async Task ExportData(StorageFolder destinationFolder)
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

        public static async Task ImportData(StorageFile zipFile)
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

            DataModel dataModel = await GetDataModel(importFolder);
            Progress<int> progress = new Progress<int>(ImportProgress);

            switch (dataModel)
            {
                case DataModel.Old:
                    await UpgradeOldDataModel(importFolder, true, progress);
                    break;
                case DataModel.New:
                    await UpgradeNewDataModel(importFolder, true, progress);
                    break;
                default:
                    DataManager.ImportData(new DirectoryInfo(importFolder.Path), progress);
                    break;
            }

            (App.Current as App)._itemViewHolder.ImportMessage = stringLoader.GetString("ExportImportMessage-TidyUp"); // TidyUp
            (App.Current as App)._itemViewHolder.IsImporting = false;

            await ClearCacheAsync();

            (App.Current as App)._itemViewHolder.ImportMessage = "";
            (App.Current as App)._itemViewHolder.Imported = true;
            (App.Current as App)._itemViewHolder.AllSoundsChanged = true;
            (App.Current as App)._itemViewHolder.AreExportAndImportButtonsEnabled = true;

            CreateCategoriesList();
            await GetAllSounds();
            await SetSoundBoardSizeTextAsync();
        }

        private static void ImportProgress(int value)
        {
            (App.Current as App)._itemViewHolder.ImportMessage = value + " %";
        }

        private static async Task UpgradeNewDataModel(StorageFolder root, bool import, IProgress<int> progress)
        {
            // New data format
            StorageFolder soundsFolder = await root.TryGetItemAsync("sounds") as StorageFolder;
            StorageFolder imagesFolder = await root.TryGetItemAsync("images") as StorageFolder;
            StorageFile dataFile = await root.TryGetItemAsync("data.json") as StorageFile;
            StorageFile databaseFile = await root.TryGetItemAsync("universalsoundboard.db") as StorageFile;

            if (import && dataFile != null)
            {
                // Get the data from the data file
                NewData newData = await GetDataFromFile(dataFile);

                foreach (Category category in newData.Categories)
                    DatabaseOperations.AddCategory(category.Uuid, category.Name, category.Icon);

                if (soundsFolder == null) return;
                int i = 0;
                int soundDataCount = newData.Sounds.Count;

                foreach (SoundData soundData in newData.Sounds)
                {
                    if (await soundsFolder.TryGetItemAsync(soundData.Uuid + "." + soundData.SoundExt) is StorageFile audioFile)
                    {
                        Guid soundUuid = ConvertStringToGuid(soundData.Uuid);
                        Guid categoryUuid = ConvertStringToGuid(soundData.CategoryId);

                        soundUuid = await AddSound(soundUuid, WebUtility.HtmlDecode(soundData.Name), categoryUuid, audioFile);

                        if (imagesFolder != null)
                        {
                            StorageFile imageFile = await imagesFolder.TryGetItemAsync(soundData.Uuid + "." + soundData.ImageExt) as StorageFile;
                            if (imageFile != null)
                            {
                                // Set the image of the sound
                                Guid imageUuid = Guid.NewGuid();
                                DatabaseOperations.AddImageFile(imageUuid, imageFile);
                                DatabaseOperations.UpdateSound(soundUuid, null, null, null, imageUuid.ToString(), null);
                                await imageFile.DeleteAsync();
                            }
                        }

                        if (soundData.Favourite)
                            DatabaseOperations.UpdateSound(soundUuid, null, soundData.Favourite.ToString(), null, null, null);

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
                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                        AddCategory(category.Uuid, category.Name, category.Icon);
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
                        Guid soundUuid = await AddSound(soundGuid, WebUtility.HtmlDecode(sound.name), categoryGuid, audioFile);

                        if (imagesFolder != null && !String.IsNullOrEmpty(sound.image_ext))
                        {
                            StorageFile imageFile = await imagesFolder.TryGetItemAsync(sound.uuid + "." + sound.image_ext) as StorageFile;
                            if (imageFile != null)
                            {
                                // Add image
                                Guid imageUuid = Guid.NewGuid();
                                DatabaseOperations.AddImageFile(imageUuid, imageFile);
                                DatabaseOperations.UpdateSound(soundUuid, null, null, null, imageUuid.ToString(), null);
                                await imageFile.DeleteAsync();
                            }
                        }

                        if (sound.favourite)
                            DatabaseOperations.UpdateSound(soundUuid, null, sound.favourite.ToString(), null, null, null);

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

        private static async Task UpgradeOldDataModel(StorageFolder root, bool import, IProgress<int> progress)
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
                    DatabaseOperations.AddCategory(Guid.NewGuid(), cat.Name, cat.Icon);

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
                            await soundDetails.ReadSoundDetailsFile(soundDetailsFile);
                            categoryName = soundDetails.Category;
                            favourite = soundDetails.Favourite;
                        }
                    }

                    // Find the category of the sound
                    if (!String.IsNullOrEmpty(categoryName))
                    {
                        foreach (Category category in GetAllCategories())
                        {
                            if (category.Name == categoryName)
                            {
                                categoryUuid = category.Uuid;
                                break;
                            }
                        }
                    }

                    // Save the sound
                    soundUuid = await AddSound(Guid.Empty, name, categoryUuid, file);

                    // Get the image file of the sound
                    foreach (StorageFile imageFile in await imagesFolder.GetFilesAsync())
                    {
                        if (name == imageFile.DisplayName)
                        {
                            Guid imageUuid = Guid.NewGuid();
                            DatabaseOperations.AddImageFile(imageUuid, imageFile);
                            DatabaseOperations.UpdateSound(soundUuid, null, null, null, imageUuid.ToString(), null);

                            // Delete the image
                            await imageFile.DeleteAsync();
                            break;
                        }
                    }

                    if (favourite)
                    {
                        DatabaseOperations.UpdateSound(soundUuid, null, favourite.ToString(), null, null, null);
                    }

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

        public static async Task ExportSounds(List<Sound> sounds, bool saveAsZip, StorageFolder destinationFolder)
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
                    await CopySoundFileIntoFolder(sound, exportFolder);

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
                    await CopySoundFileIntoFolder(sound, destinationFolder);
            }

            (App.Current as App)._itemViewHolder.LoadingScreenVisibility = false;
        }

        private static async Task CopySoundFileIntoFolder(Sound sound, StorageFolder destinationFolder)
        {
            string ext = sound.GetAudioFileExtension();

            if (string.IsNullOrEmpty(ext))
                ext = "mp3";

            StorageFile soundFile = await destinationFolder.CreateFileAsync(sound.Name + "." + ext, CreationCollisionOption.GenerateUniqueName);
            await FileIO.WriteBytesAsync(soundFile, await GetBytesAsync(await sound.GetAudioFile()));
        }

        // Load the sounds from the database and return them
        private static async Task<List<Sound>> GetSavedSounds()
        {
            List<TableObject> soundsTableObjectList = DatabaseOperations.GetAllSounds();
            List<Sound> sounds = new List<Sound>();

            foreach (var soundTableObject in soundsTableObjectList)
            {
                Guid soundFileUuid = ConvertStringToGuid(soundTableObject.GetPropertyValue(SoundTableSoundUuidPropertyName));
                if (DatabaseOperations.GetObject(soundFileUuid) == null) continue;

                sounds.Add(await GetSound(soundTableObject.Uuid));
            }

            (App.Current as App)._itemViewHolder.AllSoundsChanged = false;
            return sounds;
        }

        // Load all sounds into the sounds list
        public static async Task<List<Sound>> GetAllSounds()
        {
            await UpdateAllSoundsList();

            (App.Current as App)._itemViewHolder.Sounds.Clear();
            (App.Current as App)._itemViewHolder.FavouriteSounds.Clear();

            foreach (var sound in (App.Current as App)._itemViewHolder.AllSounds)
            {
                (App.Current as App)._itemViewHolder.Sounds.Add(sound);

                if (sound.Favourite)
                    (App.Current as App)._itemViewHolder.FavouriteSounds.Add(sound);
            }

            return (App.Current as App)._itemViewHolder.Sounds.ToList();
        }

        // Get the sounds of the category from the all sounds list
        public static async Task LoadSoundsByCategory(Guid uuid)
        {
            (App.Current as App)._itemViewHolder.PlayAllButtonVisibility = Visibility.Collapsed;

            await UpdateAllSoundsList();

            (App.Current as App)._itemViewHolder.Sounds.Clear();
            (App.Current as App)._itemViewHolder.FavouriteSounds.Clear();
            foreach (var sound in (App.Current as App)._itemViewHolder.AllSounds)
            {
                if (sound.Category != null)
                {
                    if (sound.Category.Uuid == uuid)
                    {
                        (App.Current as App)._itemViewHolder.Sounds.Add(sound);
                        if (sound.Favourite)
                            (App.Current as App)._itemViewHolder.FavouriteSounds.Add(sound);
                    }
                }
            }

            ShowPlayAllButton();
        }

        // Get the sounds by the name from the all sounds list
        public static async Task LoadSoundsByName(string name)
        {
            (App.Current as App)._itemViewHolder.PlayAllButtonVisibility = Visibility.Collapsed;

            await UpdateAllSoundsList();

            (App.Current as App)._itemViewHolder.Sounds.Clear();
            (App.Current as App)._itemViewHolder.FavouriteSounds.Clear();
            foreach (var sound in (App.Current as App)._itemViewHolder.AllSounds)
            {
                if (sound.Name.ToLower().Contains(name.ToLower()))
                {
                    (App.Current as App)._itemViewHolder.Sounds.Add(sound);
                    if (sound.Favourite)
                    {
                        (App.Current as App)._itemViewHolder.FavouriteSounds.Add(sound);
                    }
                }
            }

            ShowPlayAllButton();
        }

        public static async Task<Guid> AddSound(Guid uuid, string name, Guid categoryUuid, StorageFile audioFile)
        {
            // Generate a new uuid if necessary
            if (Equals(uuid, Guid.Empty))
                uuid = Guid.NewGuid();
            else if (DatabaseOperations.GetObject(uuid) != null)
                return uuid;

            // Generate a uuid for the soundFile
            Guid soundFileUuid = Guid.NewGuid();

            // Copy the file into the local app folder
            StorageFile newAudioFile = await audioFile.CopyAsync(ApplicationData.Current.LocalCacheFolder, "newSound" + audioFile.FileType, NameCollisionOption.ReplaceExisting);

            DatabaseOperations.AddSoundFile(soundFileUuid, newAudioFile);
            DatabaseOperations.AddSound(uuid, name, soundFileUuid.ToString(), Equals(categoryUuid, Guid.Empty) ? null : categoryUuid.ToString());

            await ClearCacheAsync();
            return uuid;
        }

        public static async Task<Sound> GetSound(Guid uuid)
        {
            var soundTableObject = DatabaseOperations.GetObject(uuid);

            if (soundTableObject == null || soundTableObject.TableId != SoundTableId)
                return null;

            Sound sound = new Sound(soundTableObject.Uuid)
            {
                Name = soundTableObject.GetPropertyValue(SoundTableNamePropertyName)
            };

            // Get favourite
            var favouriteString = soundTableObject.GetPropertyValue(SoundTableFavouritePropertyName);
            bool favourite = false;
            if (!String.IsNullOrEmpty(favouriteString))
            {
                bool.TryParse(favouriteString, out favourite);
                sound.Favourite = favourite;
            }

            // Get the category
            var categoryUuidString = soundTableObject.GetPropertyValue(SoundTableCategoryUuidPropertyName);
            Guid categoryUuid = Guid.Empty;
            if (!String.IsNullOrEmpty(categoryUuidString))
            {
                if (Guid.TryParse(categoryUuidString, out categoryUuid))
                {
                    sound.Category = GetCategory(categoryUuid);
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
            Guid imageFileUuid = ConvertStringToGuid(imageFileUuidString);
            if (!Equals(imageFileUuid, Guid.Empty))
            {
                var imageFile = await GetTableObjectFile(imageFileUuid);

                if (imageFile != null)
                    image.UriSource = new Uri(imageFile.Path);
            }
            sound.Image = image;

            return sound;
        }

        public static void RenameSound(Guid uuid, string newName)
        {
            DatabaseOperations.UpdateSound(uuid, newName, null, null, null, null);
            (App.Current as App)._itemViewHolder.AllSoundsChanged = true;
        }

        public static void SetCategoryOfSound(Guid soundUuid, Guid categoryUuid)
        {
            DatabaseOperations.UpdateSound(soundUuid, null, null, null, null, categoryUuid.ToString());
            (App.Current as App)._itemViewHolder.AllSoundsChanged = true;
        }

        public static void SetSoundAsFavourite(Guid uuid, bool favourite)
        {
            DatabaseOperations.UpdateSound(uuid, null, favourite.ToString(), null, null, null);
            (App.Current as App)._itemViewHolder.AllSoundsChanged = true;
        }

        public static async Task UpdateImageOfSound(Guid soundUuid, StorageFile file)
        {
            var soundTableObject = DatabaseOperations.GetObject(soundUuid);
            if (soundTableObject == null || soundTableObject.TableId != SoundTableId)
                return;
            
            Guid imageUuid = ConvertStringToGuid(soundTableObject.GetPropertyValue(SoundTableImageUuidPropertyName));
            StorageFile newImageFile = await file.CopyAsync(ApplicationData.Current.LocalCacheFolder, "newImage" + file.FileType, NameCollisionOption.ReplaceExisting);

            if (Equals(imageUuid, Guid.Empty))
            {
                // Create new image file
                Guid imageFileUuid = Guid.NewGuid();
                DatabaseOperations.AddImageFile(imageFileUuid, newImageFile);
                DatabaseOperations.UpdateSound(soundUuid, null, null, null, imageFileUuid.ToString(), null);
            }
            else
            {
                // Update the existing image file
                DatabaseOperations.UpdateImageFile(imageUuid, newImageFile);
            }

            (App.Current as App)._itemViewHolder.AllSoundsChanged = true;
        }
        
        public static async Task DeleteSoundAsync(Guid uuid)
        {
            // Find the sound and image file and delete them
            await Task.Run(() => DatabaseOperations.DeleteSound(uuid));
            (App.Current as App)._itemViewHolder.AllSoundsChanged = true;
        }
        
        public static async Task DeleteSoundsAsync(List<Guid> sounds)
        {
            foreach (Guid uuid in sounds)
                await Task.Run(() => DatabaseOperations.DeleteSound(uuid));

            (App.Current as App)._itemViewHolder.AllSoundsChanged = true;
        }

        public static Category AddCategory(Guid uuid, string name, string icon)
        {
            if (Equals(uuid, Guid.Empty))
                uuid = Guid.NewGuid();

            if (DatabaseOperations.GetObject(uuid) != null)
                return null;

            DatabaseOperations.AddCategory(uuid, name, icon);

            CreateCategoriesList();
            return new Category(uuid, name, icon);
        }

        private static List<Category> GetAllCategories()
        {
            List<TableObject> categoriesTableObjectList = DatabaseOperations.GetAllCategories();
            List<Category> categoriesList = new List<Category>();

            foreach (var categoryTableObject in categoriesTableObjectList)
                categoriesList.Add(GetCategory(categoryTableObject.Uuid));

            return categoriesList;
        }

        private static Category GetCategory(Guid uuid)
        {
            var categoryTableObject = DatabaseOperations.GetObject(uuid);

            if (categoryTableObject == null || categoryTableObject.TableId != CategoryTableId)
                return null;

            return new Category(categoryTableObject.Uuid,
                                categoryTableObject.GetPropertyValue(CategoryTableNamePropertyName),
                                categoryTableObject.GetPropertyValue(CategoryTableIconPropertyName));
        }

        public static void UpdateCategory(Guid uuid, string name, string icon)
        {
            DatabaseOperations.UpdateCategory(uuid, name, icon);
            CreateCategoriesList();
        }

        public static void DeleteCategory(Guid uuid)
        {
            var categoryTableObject = DatabaseOperations.GetObject(uuid);

            if (categoryTableObject == null || categoryTableObject.TableId != CategoryTableId)
                return;

            DatabaseOperations.DeleteObject(uuid);
            CreateCategoriesList();
        }

        public static Guid AddPlayingSound(Guid uuid, List<Sound> sounds, int current, int repetitions, bool randomly, double volume)
        {
            if (Equals(uuid, Guid.Empty))
                uuid = Guid.NewGuid();

            if (DatabaseOperations.ObjectExists(uuid)) return uuid;

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

            DatabaseOperations.AddPlayingSound(uuid, soundIds, current, repetitions, randomly, volume);

            return uuid;
        }

        public static async Task<List<PlayingSound>> GetAllPlayingSounds()
        {
            List<TableObject> playingSoundObjects = DatabaseOperations.GetAllPlayingSounds();
            List<PlayingSound> playingSounds = new List<PlayingSound>();

            foreach (var obj in playingSoundObjects)
            {
                var playingSound = await ConvertTableObjectToPlayingSound(obj);
                if (playingSound != null) playingSounds.Add(playingSound);
            }
            return playingSounds;
        }

        public static async Task<PlayingSound> GetPlayingSound(Guid uuid)
        {
            var tableObject = DatabaseOperations.GetObject(uuid);
            if (tableObject == null) return null;
            if (tableObject.TableId != PlayingSoundTableId) return null;

            return await ConvertTableObjectToPlayingSound(tableObject);
        }

        private static async Task<PlayingSound> ConvertTableObjectToPlayingSound(TableObject tableObject)
        {
            List<Sound> sounds = new List<Sound>();
            string soundIds = tableObject.GetPropertyValue(PlayingSoundTableSoundIdsPropertyName);

            // Get the sounds
            if (!String.IsNullOrEmpty(soundIds))
            {
                foreach (string uuidString in soundIds.Split(","))
                {
                    // Convert the uuid string into a Guid
                    Guid uuid = ConvertStringToGuid(uuidString);

                    if (!Equals(uuid, Guid.Empty))
                    {
                        var sound = await GetSound(uuid);
                        if (sound != null)
                            sounds.Add(sound);
                    }
                }

                if (sounds.Count == 0)
                {
                    // Delete the playing sound
                    DeletePlayingSound(tableObject.Uuid);
                    return null;
                }
            }
            else
            {
                // Delete the playing sound
                DeletePlayingSound(tableObject.Uuid);
                return null;
            }

            // Get the properties of the table objects
            int current = 0;
            string currentString = tableObject.GetPropertyValue(PlayingSoundTableCurrentPropertyName);
            int.TryParse(currentString, out current);

            double volume = 1.0;
            string volumeString = tableObject.GetPropertyValue(PlayingSoundTableVolumePropertyName);
            double.TryParse(volumeString, out volume);

            int repetitions = 1;
            string repetitionsString = tableObject.GetPropertyValue(PlayingSoundTableRepetitionsPropertyName);
            int.TryParse(repetitionsString, out repetitions);

            bool randomly = false;
            string randomlyString = tableObject.GetPropertyValue(PlayingSoundTableRandomlyPropertyName);
            bool.TryParse(randomlyString, out randomly);

            // Create the media player
            MediaPlayer player = await CreateMediaPlayer(sounds, current);

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
                DeletePlayingSound(tableObject.Uuid);
                return null;
            }
        }

        public static void AddOrRemoveAllPlayingSounds()
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
                    if (DatabaseOperations.GetObject(ps.Uuid) == null)
                    {
                        // Add the playingSound
                        List<string> soundIds = new List<string>();
                        foreach (Sound sound in ps.Sounds)
                        {
                            soundIds.Add(sound.Uuid.ToString());
                        }

                        DatabaseOperations.AddPlayingSound(ps.Uuid, soundIds, ((int)((MediaPlaybackList)ps.MediaPlayer.Source).CurrentItemIndex), ps.Repetitions, ps.Randomly, ps.MediaPlayer.Volume);
                    }
                }
                else
                {
                    // Check, if the playingSound is saved
                    if (DatabaseOperations.GetObject(ps.Uuid) != null)
                    {
                        // Remove the playingSound
                        DatabaseOperations.DeleteObject(ps.Uuid);
                    }
                }
            }
        }

        public static void SetCurrentOfPlayingSound(Guid uuid, int current)
        {
            DatabaseOperations.UpdatePlayingSound(uuid, null, current.ToString(), null, null, null);
        }

        public static void SetRepetitionsOfPlayingSound(Guid uuid, int repetitions)
        {
            DatabaseOperations.UpdatePlayingSound(uuid, null, null, repetitions.ToString(), null, null);
        }

        public static void SetSoundsListOfPlayingSound(Guid uuid, List<Sound> sounds)
        {
            List<string> soundIds = new List<string>();
            foreach (Sound sound in sounds)
            {
                soundIds.Add(sound.Uuid.ToString());
            }

            DatabaseOperations.UpdatePlayingSound(uuid, soundIds, null, null, null, null);
        }

        public static void SetVolumeOfPlayingSound(Guid uuid, double volume)
        {
            if (volume >= 1)
                volume = 1;
            else if (volume <= 0)
                volume = 0;

            DatabaseOperations.UpdatePlayingSound(uuid, null, null, null, null, volume.ToString());
        }

        public static void DeletePlayingSound(Guid uuid)
        {
            DatabaseOperations.DeleteObject(uuid);
        }
        
        private static async Task<StorageFile> GetTableObjectFile(Guid uuid)
        {
            var fileTableObject = DatabaseOperations.GetObject(uuid);

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
        
        public static async Task<StorageFile> GetAudioFileOfSound(Guid soundUuid)
        {
            var soundFileTableObject = GetSoundFileTableObject(soundUuid);
            if (soundFileTableObject == null) return null;
            if (soundFileTableObject.File == null) return null;

            if (File.Exists(soundFileTableObject.File.FullName))
                return await StorageFile.GetFileFromPathAsync(soundFileTableObject.File.FullName);
            else
                return null;
        }

        public static string GetAudioFileExtension(Guid soundUuid)
        {
            var soundFileTableObject = GetSoundFileTableObject(soundUuid);
            if (soundFileTableObject == null) return null;
            return soundFileTableObject.GetPropertyValue(TableObjectExtPropertyName);
        }

        public static async Task<StorageFile> GetImageFileOfSound(Guid soundUuid)
        {
            var imageFileTableObject = GetImageFileTableObject(soundUuid);
            if (imageFileTableObject == null) return null;
            if (imageFileTableObject.File == null) return null;

            if (File.Exists(imageFileTableObject.File.FullName))
                return await StorageFile.GetFileFromPathAsync(imageFileTableObject.File.FullName);
            else
                return null;
        }

        public static Uri GetAudioUriOfSound(Guid soundUuid)
        {
            var soundTableObject = GetSoundFileTableObject(soundUuid);
            if (soundTableObject != null)
                return soundTableObject.GetFileUri();
            else
                return null;
        }

        public static async Task<MemoryStream> GetAudioStreamOfSound(Guid soundUuid)
        {
            var soundTableObject = GetSoundFileTableObject(soundUuid);
            if (soundTableObject != null)
                return await soundTableObject.GetFileStream();
            else
                return null;
        }

        public static void DownloadAudioFileOfSound(Guid soundUuid, Progress<int> progress)
        {
            var soundTableObject = GetSoundFileTableObject(soundUuid);
            if (soundTableObject != null)
                soundTableObject.DownloadFile(progress);
        }
        
        public static DownloadStatus GetSoundFileDownloadStatus(Guid soundUuid)
        {
            var soundTableObject = GetSoundFileTableObject(soundUuid);
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

        private static TableObject GetSoundFileTableObject(Guid soundUuid)
        {
            var soundTableObject = DatabaseOperations.GetObject(soundUuid);
            if (soundTableObject == null) return null;
            Guid soundFileUuid = ConvertStringToGuid(soundTableObject.GetPropertyValue(SoundTableSoundUuidPropertyName));
            if (Equals(soundFileUuid, Guid.Empty)) return null;
            var soundFileTableObject = DatabaseOperations.GetObject(soundFileUuid);
            if (soundFileTableObject == null) return null;
            return soundFileTableObject;
        }

        private static TableObject GetImageFileTableObject(Guid soundUuid)
        {
            var soundTableObject = DatabaseOperations.GetObject(soundUuid);
            if (soundTableObject == null) return null;
            Guid imageFileUuid = ConvertStringToGuid(soundTableObject.GetPropertyValue(SoundTableImageUuidPropertyName));
            if (Equals(imageFileUuid, Guid.Empty)) return null;
            var imageFileTableObject = DatabaseOperations.GetObject(imageFileUuid);
            if (imageFileTableObject == null) return null;
            return imageFileTableObject;
        }
        #endregion

        #region UI Methods
        public static bool AreTopButtonsNormal()
        {
            if ((App.Current as App)._itemViewHolder.NormalOptionsVisibility)
            {
                if ((App.Current as App)._itemViewHolder.SearchAutoSuggestBoxVisibility && Window.Current.Bounds.Width >= hideSearchBoxMaxWidth)
                {
                    return true;
                }

                if ((App.Current as App)._itemViewHolder.SearchButtonVisibility && Window.Current.Bounds.Width < hideSearchBoxMaxWidth)
                {
                    return true;
                }
            }

            return false;
        }

        public static void CheckBackButtonVisibility()
        {
            if (AreTopButtonsNormal() &&
                (App.Current as App)._itemViewHolder.SelectedCategory == 0 &&
                String.IsNullOrEmpty((App.Current as App)._itemViewHolder.SearchQuery))
            {       // Anything is normal, SoundPage shows All Sounds
                (App.Current as App)._itemViewHolder.IsBackButtonEnabled = false;
            }
            else
            {
                (App.Current as App)._itemViewHolder.IsBackButtonEnabled = true;
            }
        }
        
        // Go to the Sounds page and show all sounds
        public static async Task ShowAllSounds()
        {
            if (AreTopButtonsNormal())
                (App.Current as App)._itemViewHolder.IsBackButtonEnabled = false;
            skipAutoSuggestBoxTextChanged = true;
            (App.Current as App)._itemViewHolder.SearchQuery = "";
            (App.Current as App)._itemViewHolder.SelectedCategory = 0;
            (App.Current as App)._itemViewHolder.EditButtonVisibility = Visibility.Collapsed;
            (App.Current as App)._itemViewHolder.Title = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("AllSounds");
            (App.Current as App)._itemViewHolder.Page = typeof(SoundPage);
            await GetAllSounds();
            ShowPlayAllButton();
            skipAutoSuggestBoxTextChanged = false;
        }

        public static void AdjustLayout()
        {
            double width = Window.Current.Bounds.Width;

            (App.Current as App)._itemViewHolder.TopButtonsCollapsed = (width < topButtonsCollapsedMaxWidth);
            (App.Current as App)._itemViewHolder.SelectButtonVisibility = !(width < moveSelectButtonMaxWidth);
            (App.Current as App)._itemViewHolder.AddButtonVisibility = !(width < moveAddButtonMaxWidth);
            (App.Current as App)._itemViewHolder.VolumeButtonVisibility = !(width < moveVolumeButtonMaxWidth);
            (App.Current as App)._itemViewHolder.ShareButtonVisibility = !(width < moveAddButtonMaxWidth);
            (App.Current as App)._itemViewHolder.CancelButtonVisibility = !(width < hideSearchBoxMaxWidth);
            (App.Current as App)._itemViewHolder.MoreButtonVisibility = (width < moveSelectButtonMaxWidth
                                                                        || !(App.Current as App)._itemViewHolder.NormalOptionsVisibility);

            if (String.IsNullOrEmpty((App.Current as App)._itemViewHolder.SearchQuery))
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

        public static async Task UpdateGridView()
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
                        await GetAllSounds();
                        (App.Current as App)._itemViewHolder.EditButtonVisibility = Visibility.Collapsed;
                    }
                    else if ((App.Current as App)._itemViewHolder.Page != typeof(SoundPage))
                    {
                        (App.Current as App)._itemViewHolder.EditButtonVisibility = Visibility.Collapsed;
                    }
                    else
                    {
                        await LoadSoundsByCategory(selectedCategory.Uuid);
                        (App.Current as App)._itemViewHolder.EditButtonVisibility = Visibility.Visible;
                    }
                }
                else
                {
                    (App.Current as App)._itemViewHolder.EditButtonVisibility = Visibility.Collapsed;
                    await LoadSoundsByName((App.Current as App)._itemViewHolder.SearchQuery);
                }
            }
            else
            {
                await GetAllSounds();
                (App.Current as App)._itemViewHolder.EditButtonVisibility = Visibility.Collapsed;
            }

            updatingGridView = false;

            // Check if another category was selected
            if (selectedCategoryIndex != (App.Current as App)._itemViewHolder.SelectedCategory)
            {
                // Update UI
                await UpdateGridView();
            }
            ShowPlayAllButton();
        }

        public static async Task ShowCategory(Guid uuid)
        {
            var categoryTableObject = DatabaseOperations.GetObject(uuid);

            if (categoryTableObject != null && categoryTableObject.TableId == CategoryTableId)
            {
                Category category = new Category(categoryTableObject.Uuid, categoryTableObject.GetPropertyValue(CategoryTableNamePropertyName), categoryTableObject.GetPropertyValue(CategoryTableIconPropertyName));

                skipAutoSuggestBoxTextChanged = true;
                (App.Current as App)._itemViewHolder.SearchQuery = "";
                (App.Current as App)._itemViewHolder.Page = typeof(SoundPage);
                (App.Current as App)._itemViewHolder.Title = WebUtility.HtmlDecode(category.Name);
                (App.Current as App)._itemViewHolder.IsBackButtonEnabled = true;
                (App.Current as App)._itemViewHolder.EditButtonVisibility = Visibility.Visible;
                await LoadSoundsByCategory(category.Uuid);
                SelectCategory(category.Uuid);
            }
            else
            {
                await ShowAllSounds();
            }
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

                if (!String.IsNullOrEmpty((App.Current as App)._itemViewHolder.SearchQuery))
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
            {
                SwitchSelectionMode();
            }
            else
            {
                ResetSearchArea();
            }
        }

        public static void GoBack()
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
                    (App.Current as App)._itemViewHolder.Title = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("AllSounds");
                    (App.Current as App)._itemViewHolder.EditButtonVisibility = Visibility.Collapsed;
                    ShowAllSounds();
                }
                else if ((App.Current as App)._itemViewHolder.SelectedCategory == 0 &&
                        String.IsNullOrEmpty((App.Current as App)._itemViewHolder.SearchQuery))
                {   // If SoundPage shows AllSounds

                }
                else
                {   // If SoundPage shows Category or search results
                    // Top Buttons are normal, but page shows Category or search results
                    ShowAllSounds();
                }
            }

            CheckBackButtonVisibility();
        }
        #endregion

        #region General Methods
        public static void CreateCategoriesList()
        {
            int selectedCategory = (App.Current as App)._itemViewHolder.SelectedCategory;
            (App.Current as App)._itemViewHolder.Categories.Clear();
            (App.Current as App)._itemViewHolder.Categories.Add(new Category(Guid.Empty, (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("AllSounds"), "\uE10F"));

            foreach (Category cat in GetAllCategories())
            {
                (App.Current as App)._itemViewHolder.Categories.Add(cat);
            }
            (App.Current as App)._itemViewHolder.SelectedCategory = selectedCategory;
        }

        public static async Task UpdatePlayingSoundListItem(Guid uuid)
        {
            var playingSound = await GetPlayingSound(uuid);
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

        public static async Task CreatePlayingSoundsList()
        {
            var allPlayingSounds = await GetAllPlayingSounds();
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
                            {
                                soundWasUpdated = currentPlayingSound.MediaPlayer.Volume != ps.MediaPlayer.Volume;
                                Debug.WriteLine("Volume");
                            }

                            if (soundWasUpdated)
                            {
                                // Replace the playing sound
                                (App.Current as App)._itemViewHolder.PlayingSounds.RemoveAt(index);
                                (App.Current as App)._itemViewHolder.PlayingSounds.Insert(index, ps);
                            }

                            Debug.WriteLine(soundWasUpdated);
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
                {
                    (App.Current as App)._itemViewHolder.PlayingSounds.Remove(ps);
                }
            }
        }

        // When the sounds list was changed, load all sounds from the database
        private static async Task UpdateAllSoundsList()
        {
            (App.Current as App)._itemViewHolder.ProgressRingIsActive = true;
            if ((App.Current as App)._itemViewHolder.AllSoundsChanged)
            {
                (App.Current as App)._itemViewHolder.AllSounds.Clear();
                foreach (Sound sound in await GetSavedSounds())
                {
                    (App.Current as App)._itemViewHolder.AllSounds.Add(sound);
                }
                UpdateLiveTile();
            }
            (App.Current as App)._itemViewHolder.ProgressRingIsActive = false;
        }

        public static void SelectCategory(Guid uuid)
        {
            for (int i = 0; i < (App.Current as App)._itemViewHolder.Categories.Count(); i++)
            {
                if (Equals((App.Current as App)._itemViewHolder.Categories[i].Uuid, uuid))
                {
                    (App.Current as App)._itemViewHolder.SelectedCategory = i;
                }
            }
        }

        public static async Task UpdateLiveTile()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            bool isLiveTileOn = false;

            if (localSettings.Values["liveTile"] == null)
            {
                localSettings.Values["liveTile"] = liveTile;
                isLiveTileOn = liveTile;
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
                if(await s.GetImageFile() != null)
                    sounds.Add(s);
            }

            Sound sound;
            if (sounds.Count == 0) return;

            Random random = new Random();
            sound = sounds.ElementAt(random.Next(sounds.Count));
            StorageFile imageFile = await sound.GetImageFile();
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
            // And send the notification
            TileUpdateManager.CreateTileUpdaterForApplication().Update(notification);
        }

        public static async Task SetSoundBoardSizeTextAsync()
        {
            if ((App.Current as App)._itemViewHolder.ProgressRingIsActive)
            {
                await Task.Delay(1000);
                await SetSoundBoardSizeTextAsync();
            }

            float totalSize = 0;
            foreach (Sound sound in (App.Current as App)._itemViewHolder.AllSounds)
            {
                float size = 0;
                var soundAudioFile = await sound.GetAudioFile();
                if(soundAudioFile != null)
                    size = await GetFileSizeInGBAsync(soundAudioFile);

                var soundImageFile = await sound.GetImageFile();
                if(soundImageFile != null)
                    size += await GetFileSizeInGBAsync(soundImageFile);

                totalSize += size;
            }

            (App.Current as App)._itemViewHolder.SoundboardSize = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("SettingsSoundBoardSize") + totalSize.ToString("n2") + " GB.";
        }

        public static async Task<MediaPlayer> CreateMediaPlayer(List<Sound> sounds, int current)
        {
            if (sounds.Count == 0) return null;

            MediaPlayer player = new MediaPlayer();
            MediaPlaybackList mediaPlaybackList = new MediaPlaybackList();

            foreach (Sound sound in sounds)
            {
                // Check if the sound was downloaded
                StorageFile audioFile = await sound.GetAudioFile();
                MediaPlaybackItem mediaPlaybackItem;

                if (audioFile == null)
                {
                    Uri soundUri = sound.GetAudioUri();
                    if (soundUri == null) continue;

                    mediaPlaybackItem = new MediaPlaybackItem(MediaSource.CreateFromUri(soundUri));
                }
                else
                    mediaPlaybackItem = new MediaPlaybackItem(MediaSource.CreateFromStorageFile(audioFile));

                MediaItemDisplayProperties props = mediaPlaybackItem.GetDisplayProperties();
                props.Type = MediaPlaybackType.Music;
                props.MusicProperties.Title = sound.Name;

                if (sound.Category != null)
                {
                    props.MusicProperties.Artist = sound.Category.Name;
                }

                var imageFile = await sound.GetImageFile();
                if (imageFile != null)
                {
                    props.Thumbnail = RandomAccessStreamReference.CreateFromFile(imageFile);
                }

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

        public static async Task<bool> UsesDavDataModel()
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
                    sb.Append(String.Format("&#{0};", (int)c));
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

        public static Guid ConvertStringToGuid(string uuidString)
        {
            Guid uuid = Guid.Empty;
            Guid.TryParse(uuidString, out uuid);
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

        public static async Task WriteFile(StorageFile file, Object objectToWrite)
        {
            DataContractJsonSerializer js = new DataContractJsonSerializer(objectToWrite.GetType());
            MemoryStream ms = new MemoryStream();
            js.WriteObject(ms, objectToWrite);

            ms.Position = 0;
            StreamReader sr = new StreamReader(ms);
            string data = sr.ReadToEnd();

            await FileIO.WriteTextAsync(file, data);
        }

        public static async Task<NewData> GetDataFromFile(StorageFile dataFile)
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
