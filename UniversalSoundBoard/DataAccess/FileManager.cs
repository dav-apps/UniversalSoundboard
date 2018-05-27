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
using UniversalSoundBoard.Models;
using UniversalSoundBoard.Pages;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
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
        public const int mobileMaxWidth = 550;
        public const int tabletMaxWidth = 650;
        public const int topButtonsCollapsedMaxWidth = 1400;
        public const int sideBarCollapsedMaxWidth = 1100;
        public const int moveSelectButtonMaxWidth = 850;
        public const int moveAddButtonMaxWidth = 800;
        public const int moveVolumeButtonMaxWidth = 750;
        public const int hideSearchBoxMaxWidth = 700;

        public static bool skipAutoSuggestBoxTextChanged = false;

        // dav Keys
        //public const string ApiKey = "gHgHKRbIjdguCM4cv5481hdiF5hZGWZ4x12Ur-7v";  // Prod
        public const string ApiKey = "eUzs3PQZYweXvumcWvagRHjdUroGe5Mo7kN1inHm";    // Dev
        public const string LoginImplicitUrl = "https://4f27d098.ngrok.io/login_implicit";
        public const int AppId = 8;                 // Dev: 8; Prod: 
        public const int SoundFileTableId = 11;      // Dev: 11; Prod: 
        public const int ImageFileTableId = 15;      // Dev: 15; Prod: 
        public const int CategoryTableId = 16;       // Dev: 16; Prod:
        public const int SoundTableId = 17;          // Dev: 17; Prod:
        public const int PlayingSoundTableId = 18;   // Dev: 18; Prod:

        public const string SoundTableNamePropertyName = "name";
        public const string SoundTableFavouritePropertyName = "favourite";
        public const string SoundTableSoundUuidPropertyName = "sound_uuid";
        public const string SoundTableSoundExtPropertyName = "sound_ext";
        public const string SoundTableImageUuidPropertyName = "image_uuid";
        public const string SoundTableImageExtPropertyName = "image_ext";
        public const string SoundTableCategoryUuidPropertyName = "category_uuid";

        public const string CategoryTableNamePropertyName = "name";
        public const string CategoryTableIconPropertyName = "icon";

        public const string PlayingSoundTableSoundIdsPropertyName = "sound_ids";
        public const string PlayingSoundTableCurrentPropertyName = "current";
        public const string PlayingSoundTableRepetitionsPropertyName = "repetitions";
        public const string PlayingSoundTableRandomlyPropertyName = "randomly";
        public const string PlayingSoundTableVolumePropertyName = "volume";
        #endregion

        #region Filesystem Methods
        public static async Task<StorageFolder> GetOldDataFolderAsync(){
            StorageFolder localStorageFolder = ApplicationData.Current.LocalFolder;
            StorageFolder oldDataFolder;
            string oldDataFolderName = "oldData";
            string dataFolderName = "data";
            string imagesFolderName = "images";
            string soundDetailsFolderName = "soundDetails";

            if (await localStorageFolder.TryGetItemAsync(oldDataFolderName) == null)
            {
                oldDataFolder = await localStorageFolder.CreateFolderAsync(oldDataFolderName);
            }
            else
            {
                oldDataFolder = await localStorageFolder.GetFolderAsync(oldDataFolderName);
            }

            // Create data folder
            if(await oldDataFolder.TryGetItemAsync(dataFolderName) == null)
            {
                await oldDataFolder.CreateFolderAsync(dataFolderName);
            }

            // Create images folder
            if(await oldDataFolder.TryGetItemAsync(imagesFolderName) == null)
            {
                await oldDataFolder.CreateFolderAsync(imagesFolderName);
            }

            // Create sound details folder
            if(await oldDataFolder.TryGetItemAsync(soundDetailsFolderName) == null)
            {
                await oldDataFolder.CreateFolderAsync(soundDetailsFolderName);
            }
            
            return oldDataFolder;
        }

        private static async Task<StorageFolder> CreateExportFoldersAsync()
        {
            StorageFolder localFolder = ApplicationData.Current.LocalCacheFolder;
            StorageFolder exportFolder;
            string exportFolderName = "export";
            string imagesFolderName = "images";
            string soundsFolderName = "sounds";
            string dataFileName = "data.json";

            if (await localFolder.TryGetItemAsync(exportFolderName) == null)
            {
                exportFolder = await localFolder.CreateFolderAsync(exportFolderName);
            }
            else
            {
                exportFolder = await localFolder.GetFolderAsync(exportFolderName);
            }

            if (await exportFolder.TryGetItemAsync(imagesFolderName) == null)
            {
                await exportFolder.CreateFolderAsync(imagesFolderName);
            }

            if (await exportFolder.TryGetItemAsync(soundsFolderName) == null)
            {
                await exportFolder.CreateFolderAsync(soundsFolderName);
            }

            if(await exportFolder.TryGetItemAsync(dataFileName) == null)
            {
                await exportFolder.CreateFileAsync(dataFileName);
            }
            
            return exportFolder;
        }

        public static async Task DeleteExportAndImportFoldersAsync()
        {
            StorageFolder localFolder = ApplicationData.Current.LocalCacheFolder;

            if (await localFolder.TryGetItemAsync("export") != null)
            {
                await (await localFolder.GetFolderAsync("export")).DeleteAsync();
            }

            if (await localFolder.TryGetItemAsync("import") != null)
            {
                await (await localFolder.GetFolderAsync("import")).DeleteAsync();
            }


            if (await localFolder.TryGetItemAsync("import.zip") != null)
            {
                await (await localFolder.GetFileAsync("import.zip")).DeleteAsync();
            }

            if (await localFolder.TryGetItemAsync("export.zip") != null)
            {
                await (await localFolder.GetFileAsync("export.zip")).DeleteAsync();
            }
        }
        #endregion

        #region General Methods
        public static async Task<bool> UsesOldDataModel()
        {
            bool oldModel = false;
            StorageFolder localStorageFolder = ApplicationData.Current.LocalFolder;
            int filesCount = 1;
            try
            {
                filesCount = (await localStorageFolder.GetFilesAsync()).Count;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
            
            if (await localStorageFolder.TryGetItemAsync("data") != null ||
                await localStorageFolder.TryGetItemAsync("soundDetails") != null ||
                filesCount > 1 ||
                (await localStorageFolder.TryGetItemAsync("oldData")) != null)
            {
                oldModel = true;
            }
            
            return oldModel;
        }
        
        public static async Task MigrateToNewDataModel()
        {
            // Set the Status message
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                (App.Current as App)._itemViewHolder.upgradeDataStatusText =
                                    new Windows.ApplicationModel.Resources.ResourceLoader().GetString("UpgradeDataStatusMessage-Preparing");
            });
            bool oldModel = await UsesOldDataModel();
            StorageFolder localStorageFolder = ApplicationData.Current.LocalFolder;

            if (oldModel)
            {
                StorageFolder oldDataFolder = await GetOldDataFolderAsync();

                StorageFolder dataOldDataFolder = await oldDataFolder.GetFolderAsync("data");
                StorageFolder imagesOldDataFolder = await oldDataFolder.GetFolderAsync("images");
                StorageFolder soundDetailsOldDataFolder = await oldDataFolder.GetFolderAsync("soundDetails");

                // Move all data into oldData folder
                // Move data folder
                StorageFolder dataFolder = await localStorageFolder.TryGetItemAsync("data") as StorageFolder;
                if(dataFolder != null)
                {
                    StorageFile dataFile = await dataFolder.TryGetItemAsync("data.json") as StorageFile;
                    if(dataFile != null)
                    {
                        await dataFile.MoveAsync(dataOldDataFolder);
                        await dataFolder.DeleteAsync();
                    }
                }

                // Rename images folder to oldImages and move images to oldData folder
                string oldImagesFolderName = "oldImages";
                StorageFolder imagesFolder = await localStorageFolder.TryGetItemAsync("images") as StorageFolder;
                StorageFolder oldImagesFolder = await localStorageFolder.TryGetItemAsync(oldImagesFolderName) as StorageFolder;

                if(oldImagesFolder == null && imagesFolder != null)
                {   // If only the images folder exists
                    await imagesFolder.RenameAsync(oldImagesFolderName);
                }

                imagesFolder = await GetImagesFolderAsync();
                oldImagesFolder = await localStorageFolder.TryGetItemAsync(oldImagesFolderName) as StorageFolder;

                if(oldImagesFolder != null)
                {
                    foreach (StorageFile imageFile in await oldImagesFolder.GetFilesAsync())
                    {
                        await imageFile.MoveAsync(imagesOldDataFolder);
                    }
                }

                // Move soundDetails folder
                StorageFolder soundDetailsFolder = await localStorageFolder.TryGetItemAsync("soundDetails") as StorageFolder;
                if(soundDetailsFolder != null)
                {
                    foreach (StorageFile detailsFile in await soundDetailsFolder.GetFilesAsync())
                    {
                        await detailsFile.MoveAsync(soundDetailsOldDataFolder);
                    }
                    await soundDetailsFolder.DeleteAsync();
                }

                // Move sound files
                foreach(StorageFile file in await localStorageFolder.GetFilesAsync())
                {
                    if (file.ContentType == "audio/wav" || file.ContentType == "audio/mpeg")
                    {
                        await file.MoveAsync(oldDataFolder);
                    }
                }

                Progress<int> progress = new Progress<int>();
                await ImportOldData(oldDataFolder, progress);

                // Delete oldImages folder
                oldImagesFolder = await localStorageFolder.TryGetItemAsync(oldImagesFolderName) as StorageFolder;
                if (oldImagesFolder != null)
                    await oldImagesFolder.DeleteAsync();

                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                    (App.Current as App)._itemViewHolder.allSoundsChanged = true;
                });
            }
        }
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
            if ((App.Current as App)._itemViewHolder.isExporting || (App.Current as App)._itemViewHolder.isImporting)
                return;

            StorageFolder cacheFolder = ApplicationData.Current.LocalCacheFolder;
            foreach (var item in await cacheFolder.GetItemsAsync())
            {
                await item.DeleteAsync();
            }
        }
        #endregion

        #region Database Methods
        public static async Task ExportData(StorageFolder destinationFolder)
        {
            await ClearCacheAsync();

            var stringLoader = new Windows.ApplicationModel.Resources.ResourceLoader();
            (App.Current as App)._itemViewHolder.exported = false;
            (App.Current as App)._itemViewHolder.imported = false;
            (App.Current as App)._itemViewHolder.isExporting = true;
            (App.Current as App)._itemViewHolder.areExportAndImportButtonsEnabled = false;

            StorageFolder localCacheFolder = ApplicationData.Current.LocalCacheFolder;
            StorageFolder exportFolder = await GetExportFolderAsync();
            var progress = new Progress<int>(ExportProgress);

            await DavDatabase.ExportData(new DirectoryInfo(exportFolder.Path), progress);

            (App.Current as App)._itemViewHolder.exportMessage = stringLoader.GetString("ExportMessage-3");

            // Create the zip file
            StorageFile zipFile = await Task.Run(async () =>
            {
                string exportFilePath = Path.Combine(localCacheFolder.Path, "export.zip");
                ZipFile.CreateFromDirectory(exportFolder.Path, exportFilePath);
                return await StorageFile.GetFileFromPathAsync(exportFilePath);
            });

            (App.Current as App)._itemViewHolder.exportMessage = stringLoader.GetString("ExportMessage-4");

            // Copy the file into the destination folder
            await zipFile.MoveAsync(destinationFolder, "UniversalSoundBoard " + DateTime.Today.ToString("dd.MM.yyyy") + ".zip", NameCollisionOption.ReplaceExisting);

            (App.Current as App)._itemViewHolder.exportMessage = stringLoader.GetString("ExportImportMessage-TidyUp");
            (App.Current as App)._itemViewHolder.isExporting = false;

            await ClearCacheAsync();

            (App.Current as App)._itemViewHolder.exportMessage = "";
            (App.Current as App)._itemViewHolder.exported = true;
            (App.Current as App)._itemViewHolder.areExportAndImportButtonsEnabled = true;
        }

        private static void ExportProgress(int value)
        {
            (App.Current as App)._itemViewHolder.exportMessage = value + " %";
        }

        public static async Task ImportData(StorageFile zipFile)
        {
            var stringLoader = new Windows.ApplicationModel.Resources.ResourceLoader();
            (App.Current as App)._itemViewHolder.importMessage = stringLoader.GetString("ImportMessage-1");

            await ClearCacheAsync();

            // Extract the file into the local cache folder
            (App.Current as App)._itemViewHolder.isImporting = true;
            (App.Current as App)._itemViewHolder.exported = false;
            (App.Current as App)._itemViewHolder.imported = false;
            (App.Current as App)._itemViewHolder.areExportAndImportButtonsEnabled = false;

            StorageFolder localCacheFolder = ApplicationData.Current.LocalCacheFolder;
            StorageFolder importFolder = await GetImportFolderAsync();

            await Task.Run(async () =>
            {
                StorageFile newZipFile = await zipFile.CopyAsync(localCacheFolder, "import.zip", NameCollisionOption.ReplaceExisting);

                // Extract the zip file
                ZipFile.ExtractToDirectory(newZipFile.Path, importFolder.Path);
            });

            // Check which format the file is in
            // Old format: Archive has a soundDetails and a data folder
            // New format: Archive has the folders sounds, images and a data.json file
            // dav format: Archive has a data.json file, but no folders with sounds and images

            Progress<int> progress = new Progress<int>(ImportProgress);
            if (await importFolder.TryGetItemAsync("soundDetails") != null || await importFolder.TryGetItemAsync("data") != null)
            {
                // Old format
                await ImportOldData(importFolder, progress);
            }
            else if (await importFolder.TryGetItemAsync("sounds") != null || await importFolder.TryGetItemAsync("images") != null)
            {
                // New format
                await ImportNewData(importFolder, progress);
            }
            else
            {
                // dav format
                DavDatabase.ImportData(new DirectoryInfo(importFolder.Path), progress);
            }

            (App.Current as App)._itemViewHolder.importMessage = stringLoader.GetString("ExportImportMessage-TidyUp"); // TidyUp
            (App.Current as App)._itemViewHolder.isImporting = false;

            await ClearCacheAsync();

            (App.Current as App)._itemViewHolder.importMessage = "";
            (App.Current as App)._itemViewHolder.imported = true;
            (App.Current as App)._itemViewHolder.allSoundsChanged = true;
            (App.Current as App)._itemViewHolder.areExportAndImportButtonsEnabled = true;

            CreateCategoriesObservableCollection();
            await GetAllSounds();
            await SetSoundBoardSizeTextAsync();
        }

        private static void ImportProgress(int value)
        {
            (App.Current as App)._itemViewHolder.importMessage = value + " %";
        }

        private static async Task ImportNewData(StorageFolder root, IProgress<int> progress)
        {
            // New data format
            StorageFolder soundsImportFolder = await root.TryGetItemAsync("sounds") as StorageFolder;
            StorageFolder imagesImportFolder = await root.TryGetItemAsync("images") as StorageFolder;
            StorageFile dataImportFile = await root.TryGetItemAsync("data.json") as StorageFile;
            ObservableCollection<SoundData> soundDatas = new ObservableCollection<SoundData>();

            // Read data.json and add data to database
            if(dataImportFile != null)
            {
                NewData newData = await GetDataFromFile(dataImportFile);

                foreach (SoundData soundData in newData.Sounds)
                {
                    soundData.Name = WebUtility.HtmlDecode(soundData.Name);
                    soundDatas.Add(soundData);
                }

                foreach (Category category in newData.Categories)
                {
                    AddCategory(category.Uuid, category.Name, category.Icon);
                }
            }

            if (soundsImportFolder == null) return;
            int i = 0;

            foreach (SoundData soundData in soundDatas)
            {
                if (await soundsImportFolder.TryGetItemAsync(soundData.Uuid + "." + soundData.SoundExt) is StorageFile audioFile)
                {
                    Guid soundGuid = Guid.Empty;
                    Guid.TryParse(soundData.Uuid, out soundGuid);
                    Guid categoryGuid = Guid.Empty;
                    Guid.TryParse(soundData.CategoryId, out categoryGuid);

                    Guid soundUuid = await AddSound(soundGuid, soundData.Name, categoryGuid, audioFile);

                    if (imagesImportFolder != null)
                    {
                        StorageFile imageFile = await imagesImportFolder.TryGetItemAsync(soundData.Uuid + "." + soundData.ImageExt) as StorageFile;
                        if (imageFile != null)
                        {
                            // Add image
                            await UpdateImageOfSound(soundUuid, imageFile);
                        }
                    }

                    if (soundData.Favourite)
                    {
                        SetSoundAsFavourite(soundUuid, soundData.Favourite);
                    }
                }

                i++;
                progress.Report((int)Math.Round(100.0 / soundDatas.Count * i));
            }
        }

        private static async Task ImportOldData(StorageFolder root, IProgress<int> progress)
        {
            // Old data format
            StorageFolder soundDetailsImportFolder = await root.TryGetItemAsync("soundDetails") as StorageFolder;   // root/soundDetails
            StorageFolder imagesImportFolder = await root.TryGetItemAsync("images") as StorageFolder;               // root/images
            StorageFolder dataImportFolder = await root.TryGetItemAsync("data") as StorageFolder;                   // root/data
            StorageFile dataImportFile = null;
            if (dataImportFolder != null)
                dataImportFile = await dataImportFolder.TryGetItemAsync("data.json") as StorageFile;        // root/data/data.json

            List<Category> categories = new List<Category>();

            // Get the categories
            if (dataImportFile != null)
            {
                foreach (Category cat in await GetCategoriesListAsync(dataImportFile))
                {
                    categories.Add(AddCategory(Guid.Empty, cat.Name, cat.Icon));
                }

                await dataImportFolder.DeleteAsync();

                CreateCategoriesObservableCollection();
            }

            int i = 0;
            var filesList = await root.GetFilesAsync();
            int filesCount = filesList.Count;

            // Get the sound files
            foreach (var file in filesList)
            {
                double percent = Math.Round(((double)i / filesCount) * 100);
                (App.Current as App)._itemViewHolder.upgradeDataStatusText = percent + " %";

                if (file.ContentType == "audio/wav" || file.ContentType == "audio/mpeg")
                {
                    StorageFile soundDetailsFile = null;
                    string name = file.DisplayName;
                    Guid uuid = Guid.Empty;
                    string categoryName = null;
                    Guid categoryUuid = Guid.Empty;
                    bool favourite = false;

                    // Get the soundDetails file of the sound and get favourite and category information
                    if (soundDetailsImportFolder != null)
                    {
                        soundDetailsFile = await soundDetailsImportFolder.TryGetItemAsync(name + ".json") as StorageFile;
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
                        foreach (Category category in (App.Current as App)._itemViewHolder.categories)
                        {
                            if (category.Name == categoryName)
                            {
                                categoryUuid = category.Uuid;
                                break;
                            }
                        }
                    }

                    // Save the sound
                    uuid = await AddSound(Guid.Empty, name, categoryUuid, file);

                    // Get the image file of the sound
                    foreach (StorageFile imageFile in await imagesImportFolder.GetFilesAsync())
                    {
                        if (name == imageFile.DisplayName)
                        {
                            await UpdateImageOfSound(uuid, imageFile);

                            // Delete the image
                            await imageFile.DeleteAsync();
                            break;
                        }
                    }

                    if (favourite)
                        SetSoundAsFavourite(uuid, favourite);

                    // Delete the soundDetails file
                    if (soundDetailsFile != null)
                        await soundDetailsFile.DeleteAsync();
                }

                i++;
                progress.Report((int)Math.Round(100.0 / filesCount * i));
            }

            // Delete the oldData folder
            await root.DeleteAsync();
        }

        // Load the sounds from the database and return them
        private static async Task<List<Sound>> GetSavedSounds()
        {
            List<TableObject> soundsTableObjectList = DatabaseOperations.GetAllSounds();
            List<Sound> sounds = new List<Sound>();

            foreach (var soundTableObject in soundsTableObjectList)
            {
                sounds.Add(await GetSound(soundTableObject.Uuid));
            }

            (App.Current as App)._itemViewHolder.allSoundsChanged = false;
            return sounds;
        }

        // Load all sounds into the sounds list
        public static async Task<List<Sound>> GetAllSounds()
        {
            await UpdateAllSoundsList();

            (App.Current as App)._itemViewHolder.sounds.Clear();
            (App.Current as App)._itemViewHolder.favouriteSounds.Clear();

            foreach (var sound in (App.Current as App)._itemViewHolder.allSounds)
            {
                (App.Current as App)._itemViewHolder.sounds.Add(sound);

                if (sound.Favourite)
                    (App.Current as App)._itemViewHolder.favouriteSounds.Add(sound);
            }

            return (App.Current as App)._itemViewHolder.sounds.ToList();
        }

        // Get the sounds of the category from the all sounds list
        public static async Task LoadSoundsByCategory(Guid uuid)
        {
            (App.Current as App)._itemViewHolder.playAllButtonVisibility = Visibility.Collapsed;

            await UpdateAllSoundsList();

            (App.Current as App)._itemViewHolder.sounds.Clear();
            (App.Current as App)._itemViewHolder.favouriteSounds.Clear();
            foreach (var sound in (App.Current as App)._itemViewHolder.allSounds)
            {
                if (sound.Category != null)
                {
                    if (sound.Category.Uuid == uuid)
                    {
                        (App.Current as App)._itemViewHolder.sounds.Add(sound);
                        if (sound.Favourite)
                            (App.Current as App)._itemViewHolder.favouriteSounds.Add(sound);
                    }
                }
            }

            ShowPlayAllButton();
        }

        // Get the sounds by the name from the all sounds list
        public static async Task LoadSoundsByName(string name)
        {
            (App.Current as App)._itemViewHolder.playAllButtonVisibility = Visibility.Collapsed;

            await UpdateAllSoundsList();

            (App.Current as App)._itemViewHolder.sounds.Clear();
            (App.Current as App)._itemViewHolder.favouriteSounds.Clear();
            foreach (var sound in (App.Current as App)._itemViewHolder.allSounds)
            {
                if (sound.Name.ToLower().Contains(name.ToLower()))
                {
                    (App.Current as App)._itemViewHolder.sounds.Add(sound);
                    if (sound.Favourite)
                    {
                        (App.Current as App)._itemViewHolder.favouriteSounds.Add(sound);
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
            DatabaseOperations.AddSound(uuid, name, soundFileUuid.ToString(), newAudioFile.FileType.Replace(".", ""), Equals(categoryUuid, Guid.Empty) ? null : categoryUuid.ToString());

            await ClearCacheAsync();
            return uuid;
        }

        public static async Task<Sound> GetSound(Guid uuid)
        {
            var soundTableObject = DatabaseOperations.GetObject(uuid);

            if (soundTableObject == null || soundTableObject.TableId != SoundTableId)
                return null;

            StorageFolder soundsFolder = await GetSoundsFolderAsync();
            StorageFolder imagesFolder = await GetImagesFolderAsync();

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
                {
                    sound.ImageFile = imageFile;
                    image.UriSource = new Uri(imageFile.Path);
                }
            }
            sound.Image = image;

            // Add the sound file to the sound
            string soundFileUuidString = soundTableObject.GetPropertyValue(SoundTableSoundUuidPropertyName);
            Guid soundFileUuid = ConvertStringToGuid(soundFileUuidString);
            if (!Equals(soundFileUuid, Guid.Empty))
            {
                var soundFile = await GetTableObjectFile(soundFileUuid);

                if (soundFile != null)
                    sound.AudioFile = soundFile;
                else
                    Debug.WriteLine("Can't find the sound file of the sound " + soundTableObject.Uuid);
            }
            else
            {
                Debug.WriteLine("Can't find the sound file of the sound " + soundTableObject.Uuid);
            }

            return sound;
        }

        public static void RenameSound(Guid uuid, string newName)
        {
            DatabaseOperations.UpdateSound(uuid, newName, null, null, null, null, null, null);
            (App.Current as App)._itemViewHolder.allSoundsChanged = true;
        }

        public static void SetCategoryOfSound(Guid soundUuid, Guid categoryUuid)
        {
            DatabaseOperations.UpdateSound(soundUuid, null, null, null, null, null, null, categoryUuid.ToString());
            (App.Current as App)._itemViewHolder.allSoundsChanged = true;
        }

        public static void SetSoundAsFavourite(Guid uuid, bool favourite)
        {
            DatabaseOperations.UpdateSound(uuid, null, favourite.ToString(), null, null, null, null, null);
            (App.Current as App)._itemViewHolder.allSoundsChanged = true;
        }

        public static async Task UpdateImageOfSound(Guid soundUuid, StorageFile file)
        {
            var soundTableObject = DatabaseOperations.GetObject(soundUuid);
            if (soundTableObject == null || soundTableObject.TableId != SoundTableId)
                return;

            string imageExt = file.FileType.Replace(".", "");
            Guid imageUuid = ConvertStringToGuid(soundTableObject.GetPropertyValue(SoundTableImageUuidPropertyName));
            StorageFile newImageFile = await file.CopyAsync(ApplicationData.Current.LocalCacheFolder, "newImage" + file.FileType, NameCollisionOption.ReplaceExisting);

            if (Equals(imageUuid, Guid.Empty))
            {
                // Create new image file
                Guid imageFileUuid = Guid.NewGuid();
                DatabaseOperations.AddImageFile(imageFileUuid, newImageFile);
                DatabaseOperations.UpdateSound(soundUuid, null, null, null, null, imageFileUuid.ToString(), imageExt, null);
            }
            else
            {
                // Update the existing image file
                DatabaseOperations.UpdateImageFile(imageUuid, newImageFile);
                DatabaseOperations.UpdateSound(soundUuid, null, null, null, null, null, imageExt, null);
            }

            await ClearCacheAsync();
            (App.Current as App)._itemViewHolder.allSoundsChanged = true;
        }

        public static void DeleteSound(Guid uuid)
        {
            // Find the sound and image file and delete them
            DatabaseOperations.DeleteSound(uuid);
            (App.Current as App)._itemViewHolder.allSoundsChanged = true;
        }

        public static void DeleteSounds(List<Guid> sounds)
        {
            foreach (Guid uuid in sounds)
            {
                DatabaseOperations.DeleteSound(uuid);
            }

            (App.Current as App)._itemViewHolder.allSoundsChanged = true;
        }

        public static Category AddCategory(Guid uuid, string name, string icon)
        {
            if (Equals(uuid, Guid.Empty))
                uuid = Guid.NewGuid();

            if (DatabaseOperations.GetObject(uuid) != null)
                return null;

            DatabaseOperations.AddCategory(uuid, name, icon);

            CreateCategoriesObservableCollection();
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
            CreateCategoriesObservableCollection();
        }

        public static void DeleteCategory(Guid uuid)
        {
            var categoryTableObject = DatabaseOperations.GetObject(uuid);

            if (categoryTableObject == null || categoryTableObject.TableId != CategoryTableId)
                return;

            DatabaseOperations.DeleteObject(uuid);
            CreateCategoriesObservableCollection();
        }

        public static Guid AddPlayingSound(Guid uuid, List<Sound> sounds, int current, int repetitions, bool randomly, double volume)
        {
            if (Equals(uuid, Guid.Empty))
                uuid = Guid.NewGuid();

            if (DatabaseOperations.ObjectExists(uuid)) return uuid;

            if (!(App.Current as App)._itemViewHolder.savePlayingSounds ||
                (App.Current as App)._itemViewHolder.playingSoundsListVisibility != Visibility.Visible)
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
                List<Sound> sounds = new List<Sound>();
                string soundIds = obj.GetPropertyValue(PlayingSoundTableSoundIdsPropertyName);

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

                    if(sounds.Count == 0)
                    {
                        // Delete the playing sound
                        DeletePlayingSound(obj.Uuid);
                    }
                }
                else
                {
                    // Delete the playing sound
                    DeletePlayingSound(obj.Uuid);
                }

                // Get the properties of the table objects
                int current = 0;
                string currentString = obj.GetPropertyValue(PlayingSoundTableCurrentPropertyName);
                int.TryParse(currentString, out current);

                double volume = 1.0;
                string volumeString = obj.GetPropertyValue(PlayingSoundTableVolumePropertyName);
                double.TryParse(volumeString, out volume);

                int repetitions = 1;
                string repetitionsString = obj.GetPropertyValue(PlayingSoundTableRepetitionsPropertyName);
                int.TryParse(repetitionsString, out repetitions);

                bool randomly = false;
                string randomlyString = obj.GetPropertyValue(PlayingSoundTableRandomlyPropertyName);
                bool.TryParse(randomlyString, out randomly);

                // Create the media player
                MediaPlayer player = CreateMediaPlayer(sounds, current);
                player.Volume = volume;
                player.AutoPlay = false;
                if (player != null)
                {
                    PlayingSound playingSound = new PlayingSound(obj.Uuid, sounds, player, repetitions, randomly, current);
                    playingSounds.Add(playingSound);
                }
                else
                {
                    // Remove the PlayingSound from the DB
                    DatabaseOperations.DeleteObject(obj.Uuid);
                }
            }
            return playingSounds;
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

            foreach (PlayingSound ps in (App.Current as App)._itemViewHolder.playingSounds)
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

            return await StorageFile.GetFileFromPathAsync(fileTableObject.File.FullName);
        }
        #endregion

        #region UI Methods
        public static bool AreTopButtonsNormal()
        {
            if ((App.Current as App)._itemViewHolder.normalOptionsVisibility)
            {
                if ((App.Current as App)._itemViewHolder.searchAutoSuggestBoxVisibility && Window.Current.Bounds.Width >= hideSearchBoxMaxWidth)
                {
                    return true;
                }

                if ((App.Current as App)._itemViewHolder.searchButtonVisibility && Window.Current.Bounds.Width < hideSearchBoxMaxWidth)
                {
                    return true;
                }
            }

            return false;
        }

        public static void CheckBackButtonVisibility()
        {
            if (AreTopButtonsNormal() &&
                (App.Current as App)._itemViewHolder.selectedCategory == 0 &&
                String.IsNullOrEmpty((App.Current as App)._itemViewHolder.searchQuery))
            {       // Anything is normal, SoundPage shows All Sounds
                SetBackButtonVisibility(false);
            }
            else
            {
                SetBackButtonVisibility(true);
            }
        }

        public static void SetBackButtonVisibility(bool visible)
        {
            if (visible)
            {
                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
                (App.Current as App)._itemViewHolder.windowTitleMargin = new Thickness(60, 7, 0, 0);
            }
            else
            {
                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;
                (App.Current as App)._itemViewHolder.windowTitleMargin = new Thickness(12, 7, 0, 0);
            }
        }

        // Go to the Sounds page and show all sounds
        public static async Task ShowAllSounds()
        {
            if (AreTopButtonsNormal())
            {
                SetBackButtonVisibility(false);
            }
            skipAutoSuggestBoxTextChanged = true;
            (App.Current as App)._itemViewHolder.searchQuery = "";
            (App.Current as App)._itemViewHolder.selectedCategory = 0;
            (App.Current as App)._itemViewHolder.editButtonVisibility = Visibility.Collapsed;
            (App.Current as App)._itemViewHolder.title = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("AllSounds");
            (App.Current as App)._itemViewHolder.page = typeof(SoundPage);
            await GetAllSounds();
            ShowPlayAllButton();
            skipAutoSuggestBoxTextChanged = false;
        }

        public static void AdjustLayout()
        {
            double width = Window.Current.Bounds.Width;

            (App.Current as App)._itemViewHolder.topButtonsCollapsed = (width < topButtonsCollapsedMaxWidth);
            (App.Current as App)._itemViewHolder.selectButtonVisibility = !(width < moveSelectButtonMaxWidth);
            (App.Current as App)._itemViewHolder.addButtonVisibility = !(width < moveAddButtonMaxWidth);
            (App.Current as App)._itemViewHolder.volumeButtonVisibility = !(width < moveVolumeButtonMaxWidth);
            (App.Current as App)._itemViewHolder.shareButtonVisibility = !(width < moveAddButtonMaxWidth);
            (App.Current as App)._itemViewHolder.cancelButtonVisibility = !(width < hideSearchBoxMaxWidth);
            (App.Current as App)._itemViewHolder.moreButtonVisibility = (width < moveSelectButtonMaxWidth
                                                                        || !(App.Current as App)._itemViewHolder.normalOptionsVisibility);

            if (String.IsNullOrEmpty((App.Current as App)._itemViewHolder.searchQuery))
            {
                (App.Current as App)._itemViewHolder.searchAutoSuggestBoxVisibility = !(width < hideSearchBoxMaxWidth);
                (App.Current as App)._itemViewHolder.searchButtonVisibility = (width < hideSearchBoxMaxWidth);
            }

            CheckBackButtonVisibility();
        }

        public static void ShowPlayAllButton()
        {
            if ((App.Current as App)._itemViewHolder.page != typeof(SoundPage)
                || (App.Current as App)._itemViewHolder.progressRingIsActive
                || (App.Current as App)._itemViewHolder.page != typeof(SoundPage))
            {
                (App.Current as App)._itemViewHolder.playAllButtonVisibility = Visibility.Collapsed;
            }
            else
            {
                (App.Current as App)._itemViewHolder.playAllButtonVisibility = Visibility.Visible;
            }
        }

        public static async Task UpdateGridView()
        {
            int selectedCategoryIndex = (App.Current as App)._itemViewHolder.selectedCategory;
            Category selectedCategory = (App.Current as App)._itemViewHolder.categories[selectedCategoryIndex];

            if (selectedCategory != null)
            {
                if ((App.Current as App)._itemViewHolder.searchQuery == "")
                {
                    if (selectedCategoryIndex == 0)
                    {
                        await GetAllSounds();
                        (App.Current as App)._itemViewHolder.editButtonVisibility = Visibility.Collapsed;
                    }
                    else if ((App.Current as App)._itemViewHolder.page != typeof(SoundPage))
                    {
                        (App.Current as App)._itemViewHolder.editButtonVisibility = Visibility.Collapsed;
                    }
                    else
                    {
                        await LoadSoundsByCategory(selectedCategory.Uuid);
                        (App.Current as App)._itemViewHolder.editButtonVisibility = Visibility.Visible;
                    }
                }
                else
                {
                    (App.Current as App)._itemViewHolder.editButtonVisibility = Visibility.Collapsed;
                    await LoadSoundsByName((App.Current as App)._itemViewHolder.searchQuery);
                }
            }
            else
            {
                await GetAllSounds();
                (App.Current as App)._itemViewHolder.editButtonVisibility = Visibility.Collapsed;
            }

            // Check if another category was selected
            if (selectedCategoryIndex != (App.Current as App)._itemViewHolder.selectedCategory)
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
                (App.Current as App)._itemViewHolder.searchQuery = "";
                (App.Current as App)._itemViewHolder.page = typeof(SoundPage);
                (App.Current as App)._itemViewHolder.title = WebUtility.HtmlDecode(category.Name);
                SetBackButtonVisibility(true);
                (App.Current as App)._itemViewHolder.editButtonVisibility = Visibility.Visible;
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
                (App.Current as App)._itemViewHolder.searchAutoSuggestBoxVisibility = false;
                (App.Current as App)._itemViewHolder.searchButtonVisibility = true;
            }
            AdjustLayout();
        }

        public static void SwitchSelectionMode()
        {
            if ((App.Current as App)._itemViewHolder.selectionMode == ListViewSelectionMode.None)
            {   // If Normal view
                (App.Current as App)._itemViewHolder.selectionMode = ListViewSelectionMode.Multiple;
                (App.Current as App)._itemViewHolder.normalOptionsVisibility = false;
                (App.Current as App)._itemViewHolder.areSelectButtonsEnabled = false;
            }
            else
            {   // If selection view
                (App.Current as App)._itemViewHolder.selectionMode = ListViewSelectionMode.None;
                (App.Current as App)._itemViewHolder.selectedSounds.Clear();
                (App.Current as App)._itemViewHolder.normalOptionsVisibility = true;
                (App.Current as App)._itemViewHolder.areSelectButtonsEnabled = true;

                if (!String.IsNullOrEmpty((App.Current as App)._itemViewHolder.searchQuery))
                {
                    (App.Current as App)._itemViewHolder.searchAutoSuggestBoxVisibility = true;
                    (App.Current as App)._itemViewHolder.searchButtonVisibility = false;
                }
            }
            AdjustLayout();
        }

        public static void ResetTopButtons()
        {
            if ((App.Current as App)._itemViewHolder.selectionMode != ListViewSelectionMode.None)
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
                if ((App.Current as App)._itemViewHolder.page != typeof(SoundPage))
                {   // If Settings Page or AccountPage is visible
                    // Go to All sounds page
                    (App.Current as App)._itemViewHolder.page = typeof(SoundPage);
                    (App.Current as App)._itemViewHolder.selectedCategory = 0;
                    (App.Current as App)._itemViewHolder.title = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("AllSounds");
                    (App.Current as App)._itemViewHolder.editButtonVisibility = Visibility.Collapsed;
                    ShowAllSounds();
                }
                else if ((App.Current as App)._itemViewHolder.selectedCategory == 0 &&
                        String.IsNullOrEmpty((App.Current as App)._itemViewHolder.searchQuery))
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
        public static void CreateCategoriesObservableCollection()
        {
            int selectedCategory = (App.Current as App)._itemViewHolder.selectedCategory;
            (App.Current as App)._itemViewHolder.categories.Clear();
            (App.Current as App)._itemViewHolder.categories.Add(new Category(Guid.Empty, (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("AllSounds"), "\uE10F"));

            foreach (Category cat in GetAllCategories())
            {
                (App.Current as App)._itemViewHolder.categories.Add(cat);
            }
            (App.Current as App)._itemViewHolder.selectedCategory = selectedCategory;
        }

        // When the sounds list was changed, load all sounds from the database
        private static async Task UpdateAllSoundsList()
        {
            (App.Current as App)._itemViewHolder.progressRingIsActive = true;
            if ((App.Current as App)._itemViewHolder.allSoundsChanged)
            {
                (App.Current as App)._itemViewHolder.allSounds.Clear();
                foreach (Sound sound in await GetSavedSounds())
                {
                    (App.Current as App)._itemViewHolder.allSounds.Add(sound);
                }
                UpdateLiveTile();
            }
            (App.Current as App)._itemViewHolder.progressRingIsActive = false;
        }

        public static void SelectCategory(Guid uuid)
        {
            for (int i = 0; i < (App.Current as App)._itemViewHolder.categories.Count(); i++)
            {
                if (Equals((App.Current as App)._itemViewHolder.categories[i].Uuid, uuid))
                {
                    (App.Current as App)._itemViewHolder.selectedCategory = i;
                }
            }
        }

        public static void UpdateLiveTile()
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

            if ((App.Current as App)._itemViewHolder.allSounds.Count == 0 || !isLiveTileOn)
            {
                TileUpdateManager.CreateTileUpdaterForApplication().Clear();
                return;
            }

            List<Sound> sounds = new List<Sound>();
            // Get sound with image
            foreach (Sound s in (App.Current as App)._itemViewHolder.allSounds.Where(s => s.ImageFile != null))
            {
                sounds.Add(s);
            }

            Sound sound;
            if (sounds.Count == 0)
            {
                return;
            }
            else
            {
                Random random = new Random();
                sound = sounds.ElementAt(random.Next(sounds.Count));
            }

            NotificationsExtensions.Tiles.TileBinding binding = new NotificationsExtensions.Tiles.TileBinding()
            {
                Branding = NotificationsExtensions.Tiles.TileBranding.NameAndLogo,

                Content = new NotificationsExtensions.Tiles.TileBindingContentAdaptive()
                {
                    PeekImage = new NotificationsExtensions.Tiles.TilePeekImage()
                    {
                        Source = sound.ImageFile.Path
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
            if ((App.Current as App)._itemViewHolder.progressRingIsActive)
            {
                await Task.Delay(1000);
                await SetSoundBoardSizeTextAsync();
            }

            float totalSize = 0;
            foreach (Sound sound in (App.Current as App)._itemViewHolder.allSounds)
            {
                float size;
                size = await GetFileSizeInGBAsync(sound.AudioFile);
                if (sound.ImageFile != null)
                {
                    size += await GetFileSizeInGBAsync(sound.ImageFile);
                }
                totalSize += size;
            }

            (App.Current as App)._itemViewHolder.soundboardSize = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("SettingsSoundBoardSize") + totalSize.ToString("n2") + " GB.";
        }

        public static MediaPlayer CreateMediaPlayer(List<Sound> sounds, int current)
        {
            if (sounds.Count == 0)
                return null;

            MediaPlayer player = new MediaPlayer();
            MediaPlaybackList mediaPlaybackList = new MediaPlaybackList();

            foreach (Sound sound in sounds)
            {
                MediaPlaybackItem mediaPlaybackItem = new MediaPlaybackItem(MediaSource.CreateFromStorageFile(sound.AudioFile));

                MediaItemDisplayProperties props = mediaPlaybackItem.GetDisplayProperties();
                props.Type = MediaPlaybackType.Music;
                props.MusicProperties.Title = sound.Name;
                if (sound.Category != null)
                {
                    props.MusicProperties.Artist = sound.Category.Name;
                }
                if (sound.ImageFile != null)
                {
                    props.Thumbnail = RandomAccessStreamReference.CreateFromFile(sound.ImageFile);
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

            mediaPlaybackList.MoveTo((uint)current);

            return player;
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
            List<string> Icons = new List<string>();
            Icons.Add("\uE707");
            Icons.Add("\uE70F");
            Icons.Add("\uE710");
            Icons.Add("\uE711");
            Icons.Add("\uE713");
            Icons.Add("\uE714");
            Icons.Add("\uE715");
            Icons.Add("\uE716");
            Icons.Add("\uE717");
            Icons.Add("\uE718");
            Icons.Add("\uE719");
            Icons.Add("\uE71B");
            Icons.Add("\uE71C");
            Icons.Add("\uE71E");
            Icons.Add("\uE720");
            Icons.Add("\uE722");
            Icons.Add("\uE723");
            Icons.Add("\uE72C");
            Icons.Add("\uE72D");
            Icons.Add("\uE730");
            Icons.Add("\uE734");
            Icons.Add("\uE735");
            Icons.Add("\uE73A");
            Icons.Add("\uE73E");
            Icons.Add("\uE74D");
            Icons.Add("\uE74E");
            Icons.Add("\uE74F");
            Icons.Add("\uE753");
            Icons.Add("\uE765");
            Icons.Add("\uE767");
            Icons.Add("\uE768");
            Icons.Add("\uE769");
            Icons.Add("\uE76E");
            Icons.Add("\uE774");
            Icons.Add("\uE77A");
            Icons.Add("\uE77B");
            Icons.Add("\uE77F");
            Icons.Add("\uE786");
            Icons.Add("\uE7AD");
            Icons.Add("\uE7C1");
            Icons.Add("\uE7C3");
            Icons.Add("\uE7EE");
            Icons.Add("\uE7EF");
            Icons.Add("\uE80F");
            Icons.Add("\uE81D");
            Icons.Add("\uE890");
            Icons.Add("\uE894");
            Icons.Add("\uE895");
            Icons.Add("\uE896");
            Icons.Add("\uE897");
            Icons.Add("\uE899");
            Icons.Add("\uE8AA");
            Icons.Add("\uE8B1");
            Icons.Add("\uE8B8");
            Icons.Add("\uE8BD");
            Icons.Add("\uE8C3");
            Icons.Add("\uE8C6");
            Icons.Add("\uE8C9");
            Icons.Add("\uE8D6");
            Icons.Add("\uE8D7");
            Icons.Add("\uE8E1");
            Icons.Add("\uE8E0");
            Icons.Add("\uE8EA");
            Icons.Add("\uE8EB");
            Icons.Add("\uE8EC");
            Icons.Add("\uE8EF");
            Icons.Add("\uE8F0");
            Icons.Add("\uE8F1");
            Icons.Add("\uE8F3");
            Icons.Add("\uE8FB");
            Icons.Add("\uE909");
            Icons.Add("\uE90A");
            Icons.Add("\uE90B");
            Icons.Add("\uE90F");
            Icons.Add("\uE910");
            Icons.Add("\uE913");

            return Icons;
        }

        public static Guid ConvertStringToGuid(string uuidString)
        {
            Guid uuid = Guid.Empty;
            Guid.TryParse(uuidString, out uuid);
            return uuid;
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

        public static async Task<StorageFolder> GetSoundsFolderAsync()
        {
            StorageFolder root = ApplicationData.Current.LocalFolder;
            StorageFolder detailsFolder;
            string soundsFolderName = "sounds";
            if (await root.TryGetItemAsync(soundsFolderName) == null)
            {
                return detailsFolder = await root.CreateFolderAsync(soundsFolderName);
            }
            else
            {
                return detailsFolder = await root.GetFolderAsync(soundsFolderName);
            }
        }

        public static async Task<StorageFolder> GetImagesFolderAsync()
        {
            // Create images folder if not exists
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            StorageFolder imagesFolder;
            string imagesFolderName = "images";
            if (await folder.TryGetItemAsync(imagesFolderName) == null)
            {
                return imagesFolder = await folder.CreateFolderAsync(imagesFolderName);
            }
            else
            {
                return imagesFolder = await folder.GetFolderAsync(imagesFolderName);
            }
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
