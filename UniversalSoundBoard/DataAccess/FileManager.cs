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
using UniversalSoundboard.Pages;
using UniversalSoundBoard.Common;
using UniversalSoundBoard.Models;
using UniversalSoundBoard.Pages;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.Resources;
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
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace UniversalSoundBoard.DataAccess
{
    public class FileManager
    {
        #region Variables
        // Design constants
        public const int mobileMaxWidth = 550;
        public const int tabletMaxWidth = 650;
        public const int topButtonsCollapsedMaxWidth = 1400;
        public const int sideBarCollapsedMaxWidth = 1100;
        public const int hideSearchBoxMaxWidth = 700;

        // Colors for the background of PlayingSoundsBar and SideBar
        private static double sideBarAcrylicBackgroundTintOpacity = 0.6;
        private static double playingSoundsBarAcrylicBackgroundTintOpacity = 0.85;
        private static Color sideBarLightBackgroundColor = Color.FromArgb(255, 245, 245, 245);            // #f5f5f5
        private static Color sideBarDarkBackgroundColor = Color.FromArgb(255, 29, 34, 49);                // #1d2231
        private static Color playingSoundsBarLightBackgroundColor = Color.FromArgb(255, 253, 253, 253);   // #fdfdfd
        private static Color playingSoundsBarDarkBackgroundColor = Color.FromArgb(255, 15, 20, 35);       // #0f1423

        public static ItemViewHolder itemViewHolder;
        public static DavEnvironment Environment = DavEnvironment.Production;

        // dav Keys
        private const string ApiKeyProduction = "gHgHKRbIjdguCM4cv5481hdiF5hZGWZ4x12Ur-7v";     // Prod
        public const string ApiKeyDevelopment = "eUzs3PQZYweXvumcWvagRHjdUroGe5Mo7kN1inHm";     // Dev
        public static string ApiKey => Environment == DavEnvironment.Production ? ApiKeyProduction : ApiKeyDevelopment;

        private const string WebsiteBaseUrlProduction = "https://dav-apps.herokuapp.com";
        private const string WebsiteBaseUrlDevelopment = "https://e6f0a91c.ngrok.io";
        public static string WebsiteBaseUrl => Environment == DavEnvironment.Production ? WebsiteBaseUrlProduction : WebsiteBaseUrlDevelopment;

        private const int AppIdProduction = 1;                 // Dev: 4; Prod: 1
        private const int AppIdDevelopment = 4;
        public static int AppId => Environment == DavEnvironment.Production ? AppIdProduction : AppIdDevelopment;

        private const int SoundFileTableIdProduction = 6;      // Dev: 6; Prod: 6
        private const int SoundFileTableIdDevelopment = 6;
        public static int SoundFileTableId => Environment == DavEnvironment.Production ? SoundFileTableIdProduction : SoundFileTableIdDevelopment;

        private const int ImageFileTableIdProduction = 7;      // Dev: 7; Prod: 7
        private const int ImageFileTableIdDevelopment = 7;
        public static int ImageFileTableId => Environment == DavEnvironment.Production ? ImageFileTableIdProduction : ImageFileTableIdDevelopment;

        private const int CategoryTableIdProduction = 8;       // Dev: 8; Prod: 8
        private const int CategoryTableIdDevelopment = 8;
        public static int CategoryTableId => Environment == DavEnvironment.Production ? CategoryTableIdProduction : CategoryTableIdDevelopment;

        private const int SoundTableIdProduction = 5;          // Dev: 5; Prod: 5
        private const int SoundTableIdDevelopment = 5;
        public static int SoundTableId => Environment == DavEnvironment.Production ? SoundTableIdProduction : SoundTableIdDevelopment;

        private const int PlayingSoundTableIdProduction = 9;   // Dev: 9; Prod: 9
        private const int PlayingSoundTableIdDevelopment = 9;
        public static int PlayingSoundTableId => Environment == DavEnvironment.Production ? PlayingSoundTableIdProduction : PlayingSoundTableIdDevelopment;

        private const int OrderTableIdProduction = 12;  // Dev: 10; Prod: 12
        private const int OrderTableIdDevelopment = 10;
        public static int OrderTableId => Environment == DavEnvironment.Production ? OrderTableIdProduction : OrderTableIdDevelopment;

        // Table property names
        public const string SoundTableNamePropertyName = "name";
        public const string SoundTableFavouritePropertyName = "favourite";
        public const string SoundTableSoundUuidPropertyName = "sound_uuid";
        public const string SoundTableImageUuidPropertyName = "image_uuid";
        public const string SoundTableCategoryUuidPropertyName = "category_uuid";

        public const string CategoryTableParentPropertyName = "parent";
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

        public enum AppTheme
        {
            System,
            Light,
            Dark
        }

        // Local variables
        private static readonly ResourceLoader loader = new ResourceLoader();
        internal static bool syncFinished = false;
        private static bool customSoundOrdersLoaded = false;

        // Save the custom order of the sounds in all categories to load them faster
        private static Dictionary<Guid, List<Guid>> CustomSoundOrder = new Dictionary<Guid, List<Guid>>();
        private static Dictionary<Guid, List<Guid>> CustomFavouriteSoundOrder = new Dictionary<Guid, List<Guid>>();
        #endregion

        #region Filesystem methods
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
            if (itemViewHolder.Exporting || itemViewHolder.Importing)
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

        private static async Task<StorageFile> GetTableObjectFileAsync(Guid uuid)
        {
            var fileTableObject = await DatabaseOperations.GetTableObjectAsync(uuid);

            if (
                fileTableObject == null
                || !fileTableObject.IsFile
                || fileTableObject.File == null
            ) return null;

            try
            {
                return await StorageFile.GetFileFromPathAsync(fileTableObject.File.FullName);
            }
            catch (FileNotFoundException e)
            {
                Debug.WriteLine(e.Message);
                return null;
            }
        }

        public static async Task<StorageFile> GetAudioFileOfSoundAsync(Guid soundUuid)
        {
            var soundFileTableObject = await GetSoundFileTableObjectAsync(soundUuid);
            if (soundFileTableObject == null || soundFileTableObject.File == null) return null;

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
            if (imageFileTableObject == null || imageFileTableObject.File == null) return null;

            if (File.Exists(imageFileTableObject.File.FullName))
                return await StorageFile.GetFileFromPathAsync(imageFileTableObject.File.FullName);
            else
                return null;
        }

        public static async Task<Uri> GetAudioUriOfSoundAsync(Guid soundUuid)
        {
            var soundTableObject = await GetSoundFileTableObjectAsync(soundUuid);
            if (soundTableObject == null) return null;

            return soundTableObject.GetFileUri();
        }

        public static async Task<MemoryStream> GetAudioStreamOfSoundAsync(Guid soundUuid)
        {
            var soundTableObject = await GetSoundFileTableObjectAsync(soundUuid);
            if (soundTableObject == null) return null;

            return await soundTableObject.GetFileStreamAsync();
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
            if (soundTableObject == null) return DownloadStatus.NoFileOrNotLoggedIn;

            switch (soundTableObject.FileDownloadStatus)
            {
                case TableObject.TableObjectFileDownloadStatus.NoFileOrNotLoggedIn:
                    return DownloadStatus.NoFileOrNotLoggedIn;
                case TableObject.TableObjectFileDownloadStatus.NotDownloaded:
                    return DownloadStatus.NotDownloaded;
                case TableObject.TableObjectFileDownloadStatus.Downloading:
                    return DownloadStatus.Downloading;
                case TableObject.TableObjectFileDownloadStatus.Downloaded:
                    return DownloadStatus.Downloaded;
                default:
                    return DownloadStatus.NoFileOrNotLoggedIn;
            }
        }

        private static async Task<TableObject> GetSoundFileTableObjectAsync(Guid soundUuid)
        {
            var soundTableObject = await DatabaseOperations.GetTableObjectAsync(soundUuid);
            if (soundTableObject == null) return null;

            Guid soundFileUuid = ConvertStringToGuid(soundTableObject.GetPropertyValue(SoundTableSoundUuidPropertyName)) ?? Guid.Empty;
            if (Equals(soundFileUuid, Guid.Empty)) return null;

            var soundFileTableObject = await DatabaseOperations.GetTableObjectAsync(soundFileUuid);
            if (soundFileTableObject == null) return null;

            return soundFileTableObject;
        }

        private static async Task<TableObject> GetImageFileTableObjectAsync(Guid soundUuid)
        {
            var soundTableObject = await DatabaseOperations.GetTableObjectAsync(soundUuid);
            if (soundTableObject == null) return null;

            Guid imageFileUuid = ConvertStringToGuid(soundTableObject.GetPropertyValue(SoundTableImageUuidPropertyName)) ?? Guid.Empty;
            if (Equals(imageFileUuid, Guid.Empty)) return null;

            var imageFileTableObject = await DatabaseOperations.GetTableObjectAsync(imageFileUuid);
            if (imageFileTableObject == null) return null;

            return imageFileTableObject;
        }
        #endregion

        #region Export / Import
        public static async Task ExportDataAsync(StorageFolder destinationFolder)
        {
            await ClearCacheAsync();

            itemViewHolder.Exported = false;
            itemViewHolder.Imported = false;
            itemViewHolder.Exporting = true;
            itemViewHolder.ExportAndImportButtonsEnabled = false;

            StorageFolder localCacheFolder = ApplicationData.Current.LocalCacheFolder;
            StorageFolder exportFolder = await GetExportFolderAsync();
            var progress = new Progress<int>((int value) => itemViewHolder.ExportMessage = value + " %");

            await DataManager.ExportDataAsync(new DirectoryInfo(exportFolder.Path), progress);

            itemViewHolder.ExportMessage = loader.GetString("ExportMessage-3");

            // Create the zip file
            StorageFile zipFile = await Task.Run(async () =>
            {
                string exportFilePath = Path.Combine(localCacheFolder.Path, "export.zip");
                ZipFile.CreateFromDirectory(exportFolder.Path, exportFilePath);
                return await StorageFile.GetFileFromPathAsync(exportFilePath);
            });

            itemViewHolder.ExportMessage = loader.GetString("ExportMessage-4");

            // Copy the file into the destination folder
            await zipFile.MoveAsync(destinationFolder, string.Format("UniversalSoundboard {0}.zip", DateTime.Today.ToString("dd.MM.yyyy")), NameCollisionOption.GenerateUniqueName);

            itemViewHolder.ExportMessage = loader.GetString("ExportImportMessage-TidyUp");
            itemViewHolder.Exporting = false;

            await ClearCacheAsync();

            itemViewHolder.ExportMessage = "";
            itemViewHolder.Exported = true;
            itemViewHolder.ExportAndImportButtonsEnabled = true;
        }

        public static async Task ImportDataAsync(StorageFile zipFile)
        {
            itemViewHolder.ImportMessage = loader.GetString("ImportMessage-1");

            await ClearCacheAsync();

            // Extract the file into the local cache folder
            itemViewHolder.Importing = true;
            itemViewHolder.Exported = false;
            itemViewHolder.Imported = false;
            itemViewHolder.ExportAndImportButtonsEnabled = false;

            StorageFolder localCacheFolder = ApplicationData.Current.LocalCacheFolder;
            StorageFolder importFolder = await GetImportFolderAsync();

            await Task.Run(async () =>
            {
                StorageFile newZipFile = await zipFile.CopyAsync(localCacheFolder, "import.zip", NameCollisionOption.ReplaceExisting);

                // Extract the zip file
                ZipFile.ExtractToDirectory(newZipFile.Path, importFolder.Path);
            });

            DataModel dataModel = await GetDataModelAsync(importFolder);
            Progress<int> progress = new Progress<int>((int value) => itemViewHolder.ImportMessage = value + " %");

            switch (dataModel)
            {
                case DataModel.Old:
                    await UpgradeOldDataModelAsync(importFolder, true, progress);
                    break;
                case DataModel.New:
                    await UpgradeNewDataModelAsync(importFolder, true, progress);
                    break;
                default:
                    await Task.Run(() => DataManager.ImportDataAsync(new DirectoryInfo(importFolder.Path), progress));
                    break;
            }

            itemViewHolder.ImportMessage = loader.GetString("ExportImportMessage-TidyUp");  // TidyUp
            itemViewHolder.Importing = false;

            await ClearCacheAsync();

            itemViewHolder.ImportMessage = "";
            itemViewHolder.Imported = true;
            itemViewHolder.AllSoundsChanged = true;
            itemViewHolder.ExportAndImportButtonsEnabled = true;

            await CreateCategoriesListAsync();
            await LoadAllSoundsAsync();
            await CreatePlayingSoundsListAsync();
            await SetSoundBoardSizeTextAsync();
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
                    await DatabaseOperations.CreateCategoryAsync(category.Uuid, Guid.Empty, category.Name, category.Icon);

                if (soundsFolder == null) return;
                int i = 0;
                int soundDataCount = newData.Sounds.Count;

                foreach (SoundData soundData in newData.Sounds)
                {
                    if (await soundsFolder.TryGetItemAsync(soundData.Uuid + "." + soundData.SoundExt) is StorageFile audioFile)
                    {
                        Guid soundUuid = await CreateSoundAsync(ConvertStringToGuid(soundData.Uuid), WebUtility.HtmlDecode(soundData.Name), ConvertStringToGuid(soundData.CategoryId), audioFile);

                        if (imagesFolder != null)
                        {
                            StorageFile imageFile = await imagesFolder.TryGetItemAsync(soundData.Uuid + "." + soundData.ImageExt) as StorageFile;
                            if (imageFile != null)
                            {
                                // Set the image of the sound
                                Guid imageUuid = Guid.NewGuid();
                                await DatabaseOperations.CreateImageFileAsync(imageUuid, imageFile);
                                await DatabaseOperations.UpdateSoundAsync(soundUuid, null, null, imageUuid, null);
                                await imageFile.DeleteAsync();
                            }
                        }

                        if (soundData.Favourite)
                            await DatabaseOperations.UpdateSoundAsync(soundUuid, null, soundData.Favourite, null, null);

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
                        await CreateCategoryAsync(category.Uuid, null, category.Name, category.Icon);
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
                        Guid soundUuid = await CreateSoundAsync(ConvertStringToGuid(sound.uuid), WebUtility.HtmlDecode(sound.name), ConvertStringToGuid(sound.category_id), audioFile);

                        if (imagesFolder != null && !string.IsNullOrEmpty(sound.image_ext))
                        {
                            StorageFile imageFile = await imagesFolder.TryGetItemAsync(sound.uuid + "." + sound.image_ext) as StorageFile;
                            if (imageFile != null)
                            {
                                // Add image
                                Guid imageUuid = Guid.NewGuid();
                                await DatabaseOperations.CreateImageFileAsync(imageUuid, imageFile);
                                await DatabaseOperations.UpdateSoundAsync(soundUuid, null, null, imageUuid, null);
                                await imageFile.DeleteAsync();
                            }
                        }

                        if (sound.favourite)
                            await DatabaseOperations.UpdateSoundAsync(soundUuid, null, sound.favourite, null, null);

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
                    await DatabaseOperations.CreateCategoryAsync(Guid.NewGuid(), Guid.Empty, cat.Name, cat.Icon);

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
                    List<Guid> categoryUuids = new List<Guid>();
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
                    Category category = null;
                    if (!string.IsNullOrEmpty(categoryName))
                        category = (await GetAllCategoriesAsync()).ToList().Find(c => c.Name == categoryName);

                    // Save the sound
                    soundUuid = await CreateSoundAsync(null, name, category?.Uuid, file);

                    // Get the image file of the sound
                    foreach (StorageFile imageFile in await imagesFolder.GetFilesAsync())
                    {
                        if (name == imageFile.DisplayName)
                        {
                            Guid imageUuid = Guid.NewGuid();
                            await DatabaseOperations.CreateImageFileAsync(imageUuid, imageFile);
                            await DatabaseOperations.UpdateSoundAsync(soundUuid, null, null, imageUuid, null);

                            // Delete the image
                            await imageFile.DeleteAsync();
                            break;
                        }
                    }

                    if (favourite)
                        await DatabaseOperations.UpdateSoundAsync(soundUuid, null, favourite, null, null);

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
            itemViewHolder.LoadingScreenMessage = loader.GetString("ExportSoundsMessage");
            itemViewHolder.LoadingScreenVisible = true;

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
                await zipFile.MoveAsync(destinationFolder, string.Format("UniversalSoundboard {0}.zip", DateTime.Today.ToString("dd.MM.yyyy")), NameCollisionOption.GenerateUniqueName);
                await ClearCacheAsync();
            }
            else
            {
                // Copy the files directly into the folder
                foreach(var sound in sounds)
                    await CopySoundFileIntoFolderAsync(sound, destinationFolder);
            }

            itemViewHolder.LoadingScreenVisible = false;
        }

        private static async Task CopySoundFileIntoFolderAsync(Sound sound, StorageFolder destinationFolder)
        {
            string ext = await sound.GetAudioFileExtensionAsync();

            if (string.IsNullOrEmpty(ext))
                ext = "mp3";

            StorageFile soundFile = await destinationFolder.CreateFileAsync(string.Format("{0}.{1}", sound.Name, ext), CreationCollisionOption.GenerateUniqueName);
            await FileIO.WriteBytesAsync(soundFile, await GetBytesAsync(await sound.GetAudioFileAsync()));
        }
        #endregion

        #region Sound methods
        private static async Task LoadSoundsFromDatabase()
        {
            if (!itemViewHolder.AllSoundsChanged) return;

            // Get all sound table objects
            List<TableObject> soundTableObjects = await DatabaseOperations.GetAllSoundsAsync();
            List<Sound> sounds = new List<Sound>();

            foreach(var soundObject in soundTableObjects)
            {
                var sound = await GetSoundAsync(soundObject);
                if (sound != null) sounds.Add(sound);
            }

            // Add all sounds to the AllSounds list in itemViewHolder
            itemViewHolder.AllSounds.Clear();

            foreach (var sound in sounds)
                itemViewHolder.AllSounds.Add(sound);

            await UpdateLiveTileAsync();

            itemViewHolder.AllSoundsChanged = false;
        }

        public static async Task<Sound> GetSoundAsync(Guid uuid)
        {
            var soundTableObject = await DatabaseOperations.GetTableObjectAsync(uuid);
            if (soundTableObject == null || soundTableObject.TableId != SoundTableId) return null;

            return await GetSoundAsync(soundTableObject);
        }

        public static async Task<Sound> GetSoundAsync(TableObject soundTableObject)
        {
            if (soundTableObject == null || soundTableObject.TableId != SoundTableId) return null;

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
                foreach (var cUuidString in categoryUuidsString.Split(","))
                {
                    Guid? cUuid = ConvertStringToGuid(cUuidString);
                    if (cUuid.HasValue)
                    {
                        var category = FindCategory(cUuid.Value);
                        if (category != null)
                            sound.Categories.Add(category);
                    }
                }
            }

            // Get Image for Sound
            BitmapImage image = new BitmapImage { UriSource = Sound.GetDefaultImageUri() };

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

        /**
         * Copies all sounds into the Sounds and FavouriteSounds lists
        */
        public static async Task LoadAllSoundsAsync()
        {
            await LoadSoundsFromDatabase();

            // Get all sounds and all favourite sounds
            List<Sound> allSounds = new List<Sound>();
            List<Sound> allFavouriteSounds = new List<Sound>();
            foreach (var sound in itemViewHolder.AllSounds)
            {
                allSounds.Add(sound);
                if (sound.Favourite) allFavouriteSounds.Add(sound);
            }

            itemViewHolder.Sounds.Clear();
            itemViewHolder.FavouriteSounds.Clear();

            foreach (var sound in await SortSoundsList(allSounds, itemViewHolder.SoundOrder, itemViewHolder.SoundOrderReversed, Guid.Empty, false))
                itemViewHolder.Sounds.Add(sound);

            foreach (var sound in await SortSoundsList(allFavouriteSounds, itemViewHolder.SoundOrder, itemViewHolder.SoundOrderReversed, Guid.Empty, true))
                itemViewHolder.FavouriteSounds.Add(sound);
        }

        /**
         * Copies the sounds of the category into the Sounds and FavouriteSounds lists
        */
        public static async Task LoadSoundsOfCategoryAsync(Guid categoryUuid)
        {
            await LoadSoundsFromDatabase();

            List<Sound> sounds = new List<Sound>();
            List<Sound> favouriteSounds = new List<Sound>();

            // Get the category and all its subcategories
            List<Guid> categoryUuids = await GetSubCategoriesOfCategory(categoryUuid);

            foreach(var sound in itemViewHolder.AllSounds)
            {
                if (!sound.Categories.Exists(c => categoryUuids.Exists(uuid => c.Uuid == uuid))) continue;
                sounds.Add(sound);
                if (sound.Favourite) favouriteSounds.Add(sound);
            }

            itemViewHolder.Sounds.Clear();
            itemViewHolder.FavouriteSounds.Clear();

            foreach (var sound in await SortSoundsList(sounds, itemViewHolder.SoundOrder, itemViewHolder.SoundOrderReversed, categoryUuid, false))
                itemViewHolder.Sounds.Add(sound);

            foreach (var sound in await SortSoundsList(favouriteSounds, itemViewHolder.SoundOrder, itemViewHolder.SoundOrderReversed, categoryUuid, true))
                itemViewHolder.FavouriteSounds.Add(sound);
        }

        /**
         * Copies the sounds starting with the name into the Sounds and FavouriteSounds lists
        */
        public static async Task LoadSoundsByNameAsync(string name)
        {
            await LoadSoundsFromDatabase();

            itemViewHolder.Sounds.Clear();
            itemViewHolder.FavouriteSounds.Clear();

            foreach(var sound in itemViewHolder.AllSounds)
            {
                if (!sound.Name.StartsWith(name)) continue;

                itemViewHolder.Sounds.Add(sound);
                if (sound.Favourite) itemViewHolder.FavouriteSounds.Add(sound);
            }
        }

        public static async Task ShowAllSoundsAsync()
        {
            itemViewHolder.Page = typeof(SoundPage);
            itemViewHolder.Title = loader.GetString("AllSounds");
            itemViewHolder.SelectedCategory = Guid.Empty;
            itemViewHolder.SearchQuery = "";
            itemViewHolder.BackButtonEnabled = false;
            itemViewHolder.EditButtonVisible = false;

            await LoadAllSoundsAsync();
            UpdatePlayAllButtonVisibility();
        }

        public static async Task ShowCategoryAsync(Guid uuid)
        {
            // Get the category from the database
            var category = await GetCategoryAsync(uuid, false);

            if (category == null)
            {
                await ShowAllSoundsAsync();
                return;
            }

            // Show the category
            itemViewHolder.Page = typeof(SoundPage);
            itemViewHolder.Title = category.Name;
            itemViewHolder.SelectedCategory = category.Uuid;
            itemViewHolder.SearchQuery = "";
            itemViewHolder.BackButtonEnabled = true;
            itemViewHolder.EditButtonVisible = true;

            // Load the sounds of the category
            await LoadSoundsOfCategoryAsync(category.Uuid);
            UpdatePlayAllButtonVisibility();
        }

        /**
         * Adds the sound to all appropriate sound lists
         */
        public static async Task AddSound(Guid uuid)
        {
            var sound = await GetSoundAsync(uuid);
            if(sound != null) AddSound(sound);
        }

        public static void AddSound(Sound sound)
        {
            // Check if the sound belongs to the selected category
            bool soundBelongsToSelectedCategory = itemViewHolder.SelectedCategory == Guid.Empty || sound.Categories.Exists(c => c.Uuid == itemViewHolder.SelectedCategory);

            // Add to AllSounds
            itemViewHolder.AllSounds.Add(sound);

            // Add to Sounds
            if (soundBelongsToSelectedCategory)
                itemViewHolder.Sounds.Add(sound);
        }

        /**
         * Updates the Sound in all sound lists
         */
        public static async Task ReloadSound(Guid uuid)
        {
            var sound = await GetSoundAsync(uuid);
            if(sound != null) ReloadSound(sound);
        }

        public static void ReloadSound(Sound updatedSound)
        {
            // Check if the sound belongs to the selected category
            bool soundBelongsToSelectedCategory = itemViewHolder.SelectedCategory == Guid.Empty || updatedSound.Categories.Exists(c => c.Uuid == itemViewHolder.SelectedCategory);

            // Replace in AllSounds
            int i = itemViewHolder.AllSounds.ToList().FindIndex(s => s.Uuid == updatedSound.Uuid);
            if (i != -1)
                itemViewHolder.AllSounds[i] = updatedSound;

            // Replace in Sounds
            i = itemViewHolder.Sounds.ToList().FindIndex(s => s.Uuid == updatedSound.Uuid);
            if (i != -1)
            {
                if (soundBelongsToSelectedCategory)
                    itemViewHolder.Sounds[i] = updatedSound;
                else
                    itemViewHolder.Sounds.RemoveAt(i);
            }

            // Replace in FavouriteSounds
            i = itemViewHolder.FavouriteSounds.ToList().FindIndex(s => s.Uuid == updatedSound.Uuid);
            if (i != -1)
            {
                if (updatedSound.Favourite && soundBelongsToSelectedCategory)
                    itemViewHolder.FavouriteSounds[i] = updatedSound;
                else
                    itemViewHolder.FavouriteSounds.RemoveAt(i);
            }
        }

        /**
         * Removes the sound from all sound lists
         */
        public static void RemoveSound(Guid uuid)
        {
            // Remove in AllSounds
            int i = itemViewHolder.AllSounds.ToList().FindIndex(s => s.Uuid == uuid);
            if (i != -1)
                    itemViewHolder.AllSounds.RemoveAt(i);

            // Remove in Sounds
            i = itemViewHolder.Sounds.ToList().FindIndex(s => s.Uuid == uuid);
            if (i != -1)
                itemViewHolder.Sounds.RemoveAt(i);

            // Remove in FavouriteSounds
            i = itemViewHolder.FavouriteSounds.ToList().FindIndex(s => s.Uuid == uuid);
            if (i != -1)
                itemViewHolder.FavouriteSounds.RemoveAt(i);
        }

        public static async Task UpdateLiveTileAsync()
        {
            if (itemViewHolder.AllSounds.Count == 0 || !itemViewHolder.LiveTileEnabled)
            {
                TileUpdateManager.CreateTileUpdaterForApplication().Clear();
                return;
            }

            // Get all sounds with an image
            List<Sound> sounds = itemViewHolder.AllSounds.Where(s => s.HasImageFile()).ToList();
            if (sounds.Count == 0) return;

            // Pick a random sound
            Random random = new Random();
            Sound soundWithImage = sounds[random.Next(sounds.Count)];

            StorageFile imageFile = await soundWithImage.GetImageFileAsync();
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
                            Text = soundWithImage.Name
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
        #endregion

        #region Category methods
        /**
         * Adds the category to the Categories list, either at the end or as a child of another category
         */
        public static async Task AddCategory(Guid uuid, Guid parent)
        {
            var category = await GetCategoryAsync(uuid);
            if (category != null) AddCategory(category, parent);
        }

        public static void AddCategory(Category category, Guid parent)
        {
            if (parent.Equals(Guid.Empty))
            {
                // Add the category to the end of the Categories list
                itemViewHolder.Categories.Add(category);
            }
            else
            {
                // Find the parent category
                var parentCategory = FindCategory(parent);
                if (parentCategory == null) return;

                // Add the category to the children of the parent
                parentCategory.Children.Add(category);
            }
        }

        /**
         * Updates the category in the categories list and in the SideBar
         */
        public static async Task ReloadCategory(Guid uuid)
        {
            var category = await GetCategoryAsync(uuid);
            if (category != null) ReloadCategory(category);
        }

        public static void ReloadCategory(Category updatedCategory)
        {
            // Replace the category in the categories list with the updated category
            ReplaceCategory(itemViewHolder.Categories, updatedCategory);
        }

        private static bool ReplaceCategory(List<Category> categoriesList, Category updatedCategory)
        {
            for(int i = 0; i < categoriesList.Count; i++)
            {
                if (categoriesList[i].Uuid == updatedCategory.Uuid)
                {
                    categoriesList[i] = updatedCategory;
                    return true;
                }
                else
                {
                    if (ReplaceCategory(categoriesList[i].Children, updatedCategory))
                        return true;
                }
            }

            return false;
        }

        public static void RemoveCategory(Guid uuid)
        {
            RemoveCategoryInList(itemViewHolder.Categories, uuid);
        }

        private static bool RemoveCategoryInList(List<Category> categoriesList, Guid uuid)
        {
            bool categoryFound = false;
            int i = 0;

            foreach(var category in categoriesList)
            {
                if (category.Uuid == uuid)
                {
                    categoryFound = true;
                    break;
                }
                else
                {
                    if (RemoveCategoryInList(category.Children, uuid))
                        return true;
                }

                i++;
            }

            if (categoryFound)
            {
                // Remove the category from the list
                categoriesList.RemoveAt(i);
                return true;
            }
            return false;
        }

        public static Category FindCategory(Guid uuid)
        {
            return FindCategoryInList(itemViewHolder.Categories, uuid);
        }

        public static Category FindCategoryInList(List<Category> categoriesList, Guid uuid)
        {
            foreach(var category in categoriesList)
            {
                if (category.Uuid == uuid)
                    return category;
                else
                {
                    var childCategory = FindCategoryInList(category.Children, uuid);
                    if (childCategory != null) return childCategory;
                }
            }

            return null;
        }

        private static async Task<List<Guid>> GetSubCategoriesOfCategory(Guid categoryUuid)
        {
            List<Guid> subcategories = new List<Guid>();
            subcategories.Add(categoryUuid);

            // Get the category
            Category category = await GetCategoryAsync(categoryUuid);

            foreach (var subcategory in category.Children)
            {
                subcategories.Add(subcategory.Uuid);

                if (subcategory.Children.Count > 0)
                    foreach (var childcategory in await GetSubCategoriesOfCategory(subcategory.Uuid))
                        subcategories.Add(childcategory);
            }

            return subcategories;
        }
        #endregion

        #region Sound CRUD methods
        public static async Task<Guid> CreateSoundAsync(Guid? uuid, string name, Guid? categoryUuid, StorageFile audioFile)
        {
            List<Guid> categoryUuidList = new List<Guid>();
            if (categoryUuid.HasValue) categoryUuidList.Add(categoryUuid.Value);

            return await CreateSoundAsync(uuid, name, categoryUuidList, audioFile);
        }

        public static async Task<Guid> CreateSoundAsync(Guid? uuid, string name, List<Guid> categoryUuids, StorageFile audioFile)
        {
            // Copy the file into the local app folder
            StorageFile newAudioFile = await audioFile.CopyAsync(ApplicationData.Current.LocalCacheFolder, "newSound" + audioFile.FileType, NameCollisionOption.ReplaceExisting);

            var soundFileTableObject = await DatabaseOperations.CreateSoundFileAsync(Guid.NewGuid(), newAudioFile);
            var soundTableObject = await DatabaseOperations.CreateSoundAsync(uuid ?? Guid.NewGuid(), name, false, soundFileTableObject.Uuid, categoryUuids);

            await ClearCacheAsync();
            return soundTableObject.Uuid;
        }

        public static async Task RenameSoundAsync(Guid uuid, string newName)
        {
            await DatabaseOperations.UpdateSoundAsync(uuid, newName, null, null, null);
        }

        public static async Task SetCategoriesOfSoundAsync(Guid soundUuid, List<Guid> categoryUuids)
        {
            await DatabaseOperations.UpdateSoundAsync(soundUuid, null, null, null, categoryUuids);
        }

        public static async Task SetSoundAsFavouriteAsync(Guid uuid, bool favourite)
        {
            await DatabaseOperations.UpdateSoundAsync(uuid, null, favourite, null, null);
        }

        public static async Task UpdateImageOfSoundAsync(Guid soundUuid, StorageFile file)
        {
            var soundTableObject = await DatabaseOperations.GetTableObjectAsync(soundUuid);
            if (soundTableObject == null || soundTableObject.TableId != SoundTableId) return;

            Guid? imageUuid = ConvertStringToGuid(soundTableObject.GetPropertyValue(SoundTableImageUuidPropertyName));
            StorageFile newImageFile = await file.CopyAsync(ApplicationData.Current.LocalCacheFolder, "newImage" + file.FileType, NameCollisionOption.ReplaceExisting);

            if (!imageUuid.HasValue || Equals(imageUuid, Guid.Empty))
            {
                // Create new image file
                Guid imageFileUuid = Guid.NewGuid();
                await DatabaseOperations.CreateImageFileAsync(imageFileUuid, newImageFile);
                await DatabaseOperations.UpdateSoundAsync(soundUuid, null, null, imageFileUuid, null);
            }
            else
            {
                // Update the existing image file
                await DatabaseOperations.UpdateImageFileAsync(imageUuid.Value, newImageFile);
            }
        }

        public static async Task DeleteSoundAsync(Guid uuid)
        {
            await DatabaseOperations.DeleteSoundAsync(uuid);
        }

        public static async Task DeleteSoundsAsync(List<Guid> sounds)
        {
            foreach (Guid uuid in sounds)
                await DatabaseOperations.DeleteSoundAsync(uuid);
        }
        #endregion

        #region Category CRUD methods
        public static async Task<Guid> CreateCategoryAsync(Guid? uuid, Guid? parent, string name, string icon)
        {
            var categoryTableObject = await DatabaseOperations.CreateCategoryAsync(uuid ?? Guid.NewGuid(), parent, name, icon);
            return categoryTableObject.Uuid;
        }

        internal static async Task<List<Category>> GetAllCategoriesAsync()
        {
            List<TableObject> categoriesTableObjectList = await DatabaseOperations.GetAllCategoriesAsync();
            List<Category> categoriesList = new List<Category>();

            foreach (var categoryTableObject in categoriesTableObjectList)
            {
                if (categoryTableObject.GetPropertyValue(CategoryTableParentPropertyName) == null)
                    categoriesList.Add(await GetCategoryAsync(categoryTableObject.Uuid));
            }

            // TODO: Sort categories
            return categoriesList;
        }

        public static async Task<Category> GetCategoryAsync(Guid uuid, bool withChildren = true)
        {
            var categoryTableObject = await DatabaseOperations.GetTableObjectAsync(uuid);
            if (categoryTableObject == null || categoryTableObject.TableId != CategoryTableId) return null;

            if (withChildren)
            {
                // Get the children of the category
                List<TableObject> childrenTableObjects = await DatabaseOperations.GetTableObjectsByPropertyAsync(CategoryTableParentPropertyName, categoryTableObject.Uuid.ToString());

                List<Category> children = new List<Category>();
                foreach (var childObject in childrenTableObjects)
                {
                    Category category = await GetCategoryAsync(childObject.Uuid);
                    if (category != null) children.Add(category);
                }

                return new Category(
                    categoryTableObject.Uuid,
                    categoryTableObject.GetPropertyValue(CategoryTableNamePropertyName),
                    categoryTableObject.GetPropertyValue(CategoryTableIconPropertyName),
                    children
                );
            }
            else
            {
                return new Category(
                    categoryTableObject.Uuid,
                    categoryTableObject.GetPropertyValue(CategoryTableNamePropertyName),
                    categoryTableObject.GetPropertyValue(CategoryTableIconPropertyName)
                );
            }
        }

        public static async Task UpdateCategoryAsync(Guid uuid, string name, string icon)
        {
            await DatabaseOperations.UpdateCategoryAsync(uuid, name, icon);
        }

        public static async Task DeleteCategoryAsync(Guid categoryUuid)
        {
            await DatabaseOperations.DeleteCategoryAsync(categoryUuid);
            // TODO: Delete SoundOrder table objects
            // TODO: Update categories list
        }
        #endregion

        #region PlayingSound CRUD methods
        public static async Task<Guid> CreatePlayingSoundAsync(Guid? uuid, List<Sound> sounds, int current, int repetitions, bool randomly, double volume)
        {
            if (
                !itemViewHolder.SavePlayingSounds
                || !itemViewHolder.PlayingSoundsListVisible
            ) return uuid.GetValueOrDefault();

            if (volume >= 1)
                volume = 1;
            else if (volume <= 0)
                volume = 0;

            List<Guid> soundUuids = new List<Guid>();
            foreach (Sound sound in sounds)
                soundUuids.Add(sound.Uuid);

            var playingSoundTableObject = await DatabaseOperations.CreatePlayingSoundAsync(uuid ?? Guid.NewGuid(), soundUuids, current, repetitions, randomly, volume);

            return playingSoundTableObject.Uuid;
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
            var playingSoundTableObject = await DatabaseOperations.GetPlayingSoundAsync(uuid);
            if (playingSoundTableObject == null) return null;

            return await ConvertTableObjectToPlayingSoundAsync(playingSoundTableObject);
        }

        public static async Task SetCurrentOfPlayingSoundAsync(Guid uuid, int current)
        {
            await DatabaseOperations.UpdatePlayingSoundAsync(uuid, null, current, null, null, null);
        }

        public static async Task SetRepetitionsOfPlayingSoundAsync(Guid uuid, int repetitions)
        {
            await DatabaseOperations.UpdatePlayingSoundAsync(uuid, null, null, repetitions, null, null);
        }

        public static async Task SetSoundsListOfPlayingSoundAsync(Guid uuid, List<Sound> sounds)
        {
            List<Guid> soundUuids = new List<Guid>();
            foreach (Sound sound in sounds)
                soundUuids.Add(sound.Uuid);

            await DatabaseOperations.UpdatePlayingSoundAsync(uuid, soundUuids, null, null, null, null);
        }

        public static async Task SetVolumeOfPlayingSoundAsync(Guid uuid, double volume)
        {
            if (volume >= 1)
                volume = 1;
            else if (volume <= 0)
                volume = 0;

            await DatabaseOperations.UpdatePlayingSoundAsync(uuid, null, null, null, null, volume);
        }

        public static async Task DeletePlayingSoundAsync(Guid uuid)
        {
            await DatabaseOperations.DeleteTableObjectAsync(uuid);
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
            if (itemViewHolder.SavePlayingSounds && itemViewHolder.PlayingSoundsListVisible)
            {
                // Save all PlayingSounds
                foreach (PlayingSound ps in itemViewHolder.PlayingSounds)
                {
                    // Check, if the playingSound is already saved
                    if (await DatabaseOperations.GetTableObjectAsync(ps.Uuid) == null)
                    {
                        // Add the playingSound
                        List<Guid> soundUuids = new List<Guid>();
                        foreach (Sound sound in ps.Sounds)
                            soundUuids.Add(sound.Uuid);

                        await DatabaseOperations.CreatePlayingSoundAsync(
                            ps.Uuid,
                            soundUuids,
                            (int)((MediaPlaybackList)ps.MediaPlayer.Source).CurrentItemIndex,
                            ps.Repetitions,
                            ps.Randomly,
                            ps.MediaPlayer.Volume
                        );
                    }
                }
            }
            else
            {
                // Delete all PlayingSounds
                foreach (PlayingSound ps in itemViewHolder.PlayingSounds)
                    await DatabaseOperations.DeletePlayingSound(ps.Uuid);
            }
        }
        #endregion

        #region Sound order methods
        /**
         * Sorts the sounds list by the given sound order
         */
        private static async Task<List<Sound>> SortSoundsList(List<Sound> sounds, SoundOrder order, bool reversed, Guid categoryUuid, bool favourite)
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
                    foreach (var sound in await SortSoundsByCustomOrder(sounds, categoryUuid, favourite))
                        sortedSounds.Add(sound);

                    break;
            }

            return sortedSounds;
        }

        /**
         * Uses the custom sound order Dictionaries to sort the given sounds list
         */
        private static async Task<List<Sound>> SortSoundsByCustomOrder(List<Sound> sounds, Guid categoryUuid, bool favourites)
        {
            // Load the sound orders, if that didn't already happen
            await LoadCustomSoundOrderAsync();

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

        /**
         * Prepares the sound order Dictionaries for all sounds and for each category
         */
        private static async Task LoadCustomSoundOrderAsync()
        {
            if (customSoundOrdersLoaded) return;
            customSoundOrdersLoaded = true;

            // Create an entry for each category in the Dictionary and save the sound order there
            foreach (var category in itemViewHolder.Categories)
                await LoadCustomSoundOrderForCategoryAsync(category);
        }

        private static async Task LoadCustomSoundOrderForCategoryAsync(Category category)
        {
            if (category.Uuid == Guid.Empty)
            {
                // Get all sounds and all favourite sounds
                List<Sound> allSounds = new List<Sound>();
                List<Sound> allFavouriteSounds = new List<Sound>();

                foreach (var sound in itemViewHolder.AllSounds)
                {
                    allSounds.Add(sound);
                    if (sound.Favourite) allFavouriteSounds.Add(sound);
                }

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
                // Get the sounds and favourite sounds of the category and all its subcategories
                List<Sound> sounds = new List<Sound>();
                List<Sound> favouriteSounds = new List<Sound>();

                List<Guid> categoryUuids = await GetSubCategoriesOfCategory(category.Uuid);

                foreach (var sound in itemViewHolder.AllSounds)
                {
                    if (!sound.Categories.Exists(c => categoryUuids.Exists(uuid => c.Uuid == uuid))) continue;
                    sounds.Add(sound);
                    if (sound.Favourite) favouriteSounds.Add(sound);
                }

                CustomSoundOrder[category.Uuid] = new List<Guid>();
                CustomFavouriteSoundOrder[category.Uuid] = new List<Guid>();

                // Add the sounds to the dictionary
                foreach (var sound in await SortSoundsListByCustomOrderAsync(sounds, category.Uuid, false))
                    CustomSoundOrder[category.Uuid].Add(sound.Uuid);

                foreach (var sound in await SortSoundsListByCustomOrderAsync(favouriteSounds, category.Uuid, true))
                    CustomFavouriteSoundOrder[category.Uuid].Add(sound.Uuid);

                // Call this method for each child
                foreach (var childCategory in category.Children)
                    await LoadCustomSoundOrderForCategoryAsync(childCategory);
            }
        }

        /**
         * Sorts the sounds list by the given configuration using the sound order table objects
         */
        public static async Task<List<Sound>> SortSoundsListByCustomOrderAsync(List<Sound> sounds, Guid categoryUuid, bool favourite)
        {
            // Get the order table objects
            var tableObjects = await DatabaseOperations.GetAllOrdersAsync();

            // Get the order objects with the type Sound (1), the right category uuid and the same favourite
            var soundOrderTableObjects = tableObjects.FindAll((TableObject obj) =>
            {
                // Check if the object is of type Sound
                if (obj.GetPropertyValue(OrderTableTypePropertyName) != SoundOrderType) return false;

                // Check if the object has the correct category uuid
                string categoryUuidString = obj.GetPropertyValue(OrderTableCategoryPropertyName);
                Guid? cUuid = ConvertStringToGuid(categoryUuidString);
                if (!cUuid.HasValue) return false;

                // Get the favourite value
                string favString = obj.GetPropertyValue(OrderTableFavouritePropertyName);
                bool.TryParse(favString, out bool fav);

                return categoryUuid.Equals(cUuid) && favourite == fav;
            });

            if (soundOrderTableObjects.Count > 0)
            {
                // Remove sounds from the order if [the user is not logged in] or [the user is logged in and the sounds are synced]
                bool removeNonExistentSounds =
                    !itemViewHolder.User.IsLoggedIn
                    || (itemViewHolder.User.IsLoggedIn && syncFinished);

                bool saveNewOrder = false;
                TableObject lastOrderTableObject = soundOrderTableObjects.Last();
                List<Guid> uuids = new List<Guid>();
                List<Sound> sortedSounds = new List<Sound>();

                // Add all sounds to newSounds
                List<Sound> newSounds = new List<Sound>();
                foreach (var sound in sounds)
                    newSounds.Add(sound);

                foreach (var property in lastOrderTableObject.Properties)
                {
                    if (!int.TryParse(property.Name, out int index)) continue;

                    Guid? soundUuid = ConvertStringToGuid(property.Value);
                    if (!soundUuid.HasValue) continue;

                    // Check if this uuid is already in the uuids list
                    if (uuids.Contains(soundUuid.Value))
                    {
                        if (removeNonExistentSounds) saveNewOrder = true;
                        continue;
                    }

                    if (!removeNonExistentSounds)
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

                // Add the sounds, that are not in the order, at the end
                foreach (var sound in newSounds)
                {
                    sortedSounds.Add(sound);
                    uuids.Add(sound.Uuid);
                    saveNewOrder = true;
                }

                // If there are multiple order objects, merge them
                while (soundOrderTableObjects.Count > 1)
                {
                    saveNewOrder = true;

                    // Merge the first order object into the last one
                    var firstOrderTableObject = soundOrderTableObjects.First();

                    // Go through each uuid of the order object
                    foreach (var property in firstOrderTableObject.Properties)
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
                    await firstOrderTableObject.DeleteAsync();
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

        /**
         * Update the custom sound order after the user dragged a sound in the grid or list
         */
        public static void UpdateCustomSoundOrder(Guid categoryUuid, bool favourites, List<Guid> uuids)
        {
            if (favourites)
            {
                if (CustomFavouriteSoundOrder.ContainsKey(categoryUuid))
                    CustomFavouriteSoundOrder[categoryUuid].Clear();
                else
                    CustomFavouriteSoundOrder[categoryUuid] = new List<Guid>();

                CustomFavouriteSoundOrder[categoryUuid] = uuids;
            }
            else
            {
                if (CustomSoundOrder.ContainsKey(categoryUuid))
                    CustomSoundOrder[categoryUuid].Clear();
                else
                    CustomSoundOrder[categoryUuid] = new List<Guid>();

                CustomSoundOrder[categoryUuid] = uuids;
            }
        }
        #endregion

        #region UI methods
        /**
         * Determines the current state of the app and navigates one step back
         */
        public static async Task GoBackAsync()
        {
            // Sound page not visible?
            if(itemViewHolder.Page != typeof(SoundPage))
            {
                // Navigate to All Sounds
                itemViewHolder.Page = typeof(SoundPage);
                itemViewHolder.SelectedCategory = Guid.Empty;
                itemViewHolder.Title = loader.GetString("AllSounds");
                itemViewHolder.EditButtonVisible = false;
                await LoadAllSoundsAsync();
                UpdatePlayAllButtonVisibility();
                UpdateBackButtonVisibility();
                return;
            }

            // Is on mobile and search box visible?
            if (
                Window.Current.Bounds.Width < hideSearchBoxMaxWidth
                && itemViewHolder.SearchAutoSuggestBoxVisible == true
            )
            {
                // Reset search
                itemViewHolder.SearchQuery = "";
                itemViewHolder.SearchAutoSuggestBoxVisible = false;
                itemViewHolder.SearchButtonVisible = true;
                UpdateBackButtonVisibility();
                return;
            }

            // Multi selection enabled?
            if(itemViewHolder.MultiSelectionEnabled)
            {
                // Disable multi selection
                itemViewHolder.MultiSelectionEnabled = false;
                itemViewHolder.SelectedSounds.Clear();
                UpdateBackButtonVisibility();
                return;
            }

            // Search? or Category?
            if(!Equals(itemViewHolder.SelectedCategory, Guid.Empty) || !string.IsNullOrEmpty(itemViewHolder.SearchQuery))
            {
                // -> Go to all Sounds
                itemViewHolder.SelectedCategory = Guid.Empty;
                itemViewHolder.Title = loader.GetString("AllSounds");
                itemViewHolder.EditButtonVisible = false;
                itemViewHolder.SearchQuery = "";
                await LoadAllSoundsAsync();
                UpdatePlayAllButtonVisibility();
                UpdateBackButtonVisibility();
                return;
            }
        }

        public static void UpdateBackButtonVisibility()
        {
            // Is false if SoundPage shows All Sounds and buttons are normal
            itemViewHolder.BackButtonEnabled = !(
                AreTopButtonsNormal()
                && Equals(itemViewHolder.SelectedCategory, Guid.Empty)
                && string.IsNullOrEmpty(itemViewHolder.SearchQuery)
            );
        }

        public static bool AreTopButtonsNormal()
        {
            return
                !itemViewHolder.MultiSelectionEnabled
                && (
                    (itemViewHolder.SearchAutoSuggestBoxVisible && Window.Current.Bounds.Width >= hideSearchBoxMaxWidth)
                    || (itemViewHolder.SearchButtonVisible && Window.Current.Bounds.Width < hideSearchBoxMaxWidth)
                );
        }

        public static void UpdatePlayAllButtonVisibility()
        {
            itemViewHolder.PlayAllButtonVisible =
                itemViewHolder.Page == typeof(SoundPage)
                && !itemViewHolder.ProgressRingIsActive
                && itemViewHolder.Sounds.Count > 0;
        }

        public static void ResetSearchArea()
        {
            if (Window.Current.Bounds.Width < hideSearchBoxMaxWidth)
            {
                // Clear text and show buttons
                itemViewHolder.SearchAutoSuggestBoxVisible = false;
                itemViewHolder.SearchButtonVisible = true;
            }

            itemViewHolder.SearchQuery = "";
        }

        public static void AdjustLayout()
        {
            double width = Window.Current.Bounds.Width;
            itemViewHolder.TopButtonsCollapsed = width < topButtonsCollapsedMaxWidth;

            if (string.IsNullOrEmpty(itemViewHolder.SearchQuery))
            {
                itemViewHolder.SearchAutoSuggestBoxVisible = !(width < hideSearchBoxMaxWidth);
                itemViewHolder.SearchButtonVisible = width < hideSearchBoxMaxWidth;
            }

            UpdateBackButtonVisibility();
        }

        public static void UpdateLayoutColors()
        {
            Color appThemeColor = GetApplicationThemeColor();

            // Set the background of the SideBar and the PlayingSoundsBar
            if (itemViewHolder.ShowAcrylicBackground)
            {   // If the acrylic background is enabled
                // Add the transparency effect
                (Application.Current.Resources["NavigationViewExpandedPaneBackground"] as AcrylicBrush).BackgroundSource = AcrylicBackgroundSource.HostBackdrop;
                itemViewHolder.PlayingSoundsBarAcrylicBackgroundBrush.BackgroundSource = AcrylicBackgroundSource.HostBackdrop;

                // Set the default tint opacity
                (Application.Current.Resources["NavigationViewExpandedPaneBackground"] as AcrylicBrush).TintOpacity = sideBarAcrylicBackgroundTintOpacity;
                itemViewHolder.PlayingSoundsBarAcrylicBackgroundBrush.TintOpacity = playingSoundsBarAcrylicBackgroundTintOpacity;

                // Set the tint color
                (Application.Current.Resources["NavigationViewExpandedPaneBackground"] as AcrylicBrush).TintColor = appThemeColor;
                itemViewHolder.PlayingSoundsBarAcrylicBackgroundBrush.TintColor = appThemeColor;
            }
            else if ((App.Current as App).RequestedTheme == ApplicationTheme.Light)
            {   // If the acrylic background is disabled and the theme is Light
                // Remove the transparency effect
                (Application.Current.Resources["NavigationViewExpandedPaneBackground"] as AcrylicBrush).BackgroundSource = AcrylicBackgroundSource.Backdrop;
                itemViewHolder.PlayingSoundsBarAcrylicBackgroundBrush.BackgroundSource = AcrylicBackgroundSource.Backdrop;

                // Set the background color
                (Application.Current.Resources["NavigationViewExpandedPaneBackground"] as AcrylicBrush).TintColor = sideBarLightBackgroundColor;
                itemViewHolder.PlayingSoundsBarAcrylicBackgroundBrush.TintColor = playingSoundsBarLightBackgroundColor;
            }
            else
            {   // If the acrylic background is disabled and the theme is dark
                // Remove the transparency effect
                (Application.Current.Resources["NavigationViewExpandedPaneBackground"] as AcrylicBrush).BackgroundSource = AcrylicBackgroundSource.Backdrop;
                itemViewHolder.PlayingSoundsBarAcrylicBackgroundBrush.BackgroundSource = AcrylicBackgroundSource.Backdrop;

                // Set the background color
                (Application.Current.Resources["NavigationViewExpandedPaneBackground"] as AcrylicBrush).TintColor = sideBarDarkBackgroundColor;
                itemViewHolder.PlayingSoundsBarAcrylicBackgroundBrush.TintColor = playingSoundsBarDarkBackgroundColor;
            }

            // Set the color for the NavigationViewHeader background
            (Application.Current.Resources["NavigationViewHeaderBackgroundBrush"] as AcrylicBrush).TintColor = appThemeColor;
        }

        public static void NavigateToAccountPage()
        {
            itemViewHolder.Page = typeof(AccountPage);
            itemViewHolder.Title = loader.GetString("Account-Title");
            itemViewHolder.EditButtonVisible = false;
            itemViewHolder.PlayAllButtonVisible = false;
            itemViewHolder.BackButtonEnabled = true;
        }

        public static void NavigateToSettingsPage()
        {
            itemViewHolder.Page = typeof(SettingsPage);
            itemViewHolder.Title = loader.GetString("Settings-Title");
            itemViewHolder.EditButtonVisible = false;
            itemViewHolder.PlayAllButtonVisible = false;
            itemViewHolder.BackButtonEnabled = true;
        }
        #endregion

        #region General methods
        public static async Task CreateCategoriesListAsync()
        {
            itemViewHolder.Categories.Clear();
            itemViewHolder.Categories.Add(new Category(Guid.Empty, loader.GetString("AllSounds"), "\uE10F"));

            foreach (Category cat in await GetAllCategoriesAsync())
                itemViewHolder.Categories.Add(cat);

            itemViewHolder.TriggerCategoriesUpdatedEvent(itemViewHolder.Categories, null);
        }

        public static async Task UpdatePlayingSoundListItemAsync(Guid uuid)
        {
            var playingSound = await GetPlayingSoundAsync(uuid);
            if (playingSound == null) return;

            var currentPlayingSoundList = itemViewHolder.PlayingSounds.Where(p => p.Uuid == playingSound.Uuid);
            if (currentPlayingSoundList.Count() == 0) return;

            var currentPlayingSound = currentPlayingSoundList.First();
            if (currentPlayingSound.MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing) return;

            // Replace the playing sound
            int index = itemViewHolder.PlayingSounds.IndexOf(currentPlayingSound);
            if(index != -1) itemViewHolder.PlayingSounds[index] = playingSound;
        }

        public static async Task CreatePlayingSoundsListAsync()
        {
            var allPlayingSounds = await GetAllPlayingSoundsAsync();
            foreach (PlayingSound ps in allPlayingSounds)
            {
                if (ps.MediaPlayer != null)
                {
                    var currentPlayingSoundList = itemViewHolder.PlayingSounds.Where(p => p.Uuid == ps.Uuid);

                    if (currentPlayingSoundList.Count() > 0)
                    {
                        var currentPlayingSound = currentPlayingSoundList.First();
                        int index = itemViewHolder.PlayingSounds.IndexOf(currentPlayingSound);

                        // Update the current playing sound if it is currently not playing
                        if (currentPlayingSound.MediaPlayer.PlaybackSession.PlaybackState != MediaPlaybackState.Playing)
                        {
                            // Check if the playing sound changed
                            bool soundWasUpdated = (
                                currentPlayingSound.Randomly != ps.Randomly
                                || currentPlayingSound.Repetitions != ps.Repetitions
                                || currentPlayingSound.Sounds.Count != ps.Sounds.Count
                            );

                            if (currentPlayingSound.MediaPlayer != null && ps.MediaPlayer != null && !soundWasUpdated)
                                soundWasUpdated = currentPlayingSound.MediaPlayer.Volume != ps.MediaPlayer.Volume;

                            // Replace the playing sound if it has changed
                            if (soundWasUpdated)
                                itemViewHolder.PlayingSounds[index] = ps;
                        }
                    }
                    else
                    {
                        // Add the new playing sound
                        itemViewHolder.PlayingSounds.Add(ps);
                    }
                }
            }
            
            // Remove old playing sounds
            foreach(var ps in itemViewHolder.PlayingSounds)
            {
                // Remove the playing sound from ItemViewHolder if it does not exist in the new playing sounds list
                if (allPlayingSounds.Where(p => p.Uuid == ps.Uuid).Count() == 0)
                    itemViewHolder.PlayingSounds.Remove(ps);
            }
        }

        public static async Task SetSoundBoardSizeTextAsync()
        {
            // TODO
            if (itemViewHolder.ProgressRingIsActive)
            {
                await Task.Delay(1000);
                await SetSoundBoardSizeTextAsync();
            }

            // Copy AllSounds
            List<Sound> allSounds = new List<Sound>();
            foreach (var sound in itemViewHolder.AllSounds)
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

            itemViewHolder.SoundboardSize = string.Format(loader.GetString("SettingsSoundBoardSize"), totalSize.ToString("n2") + " GB");
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
            player.AutoPlay = false;

            // Set the volume
            player.Volume = itemViewHolder.Volume;
            
            if (mediaPlaybackList.Items.Count == 0)
                return null;
            else if (mediaPlaybackList.Items.Count == 1)
                return player;

            if (mediaPlaybackList.Items.Count >= current + 1)
            {
                try { mediaPlaybackList.MoveTo((uint)current); }
                catch { }
            }

            return player;
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
            return pro.Size / 1000000000f;
        }

        public static List<string> GetIconsList()
        {
            return new List<string>
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
        }

        public static Guid? ConvertStringToGuid(string uuidString)
        {
            if (!Guid.TryParse(uuidString, out Guid uuid)) return null;
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

            // Deserialize Json
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

        public static async Task<NewData> GetDataFromFileAsync(StorageFile dataFile)
        {
            string data = await FileIO.ReadTextAsync(dataFile);

            // Deserialize Json
            var serializer = new DataContractJsonSerializer(typeof(NewData));
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(data));
            var dataReader = (NewData)serializer.ReadObject(ms);

            return dataReader;
        }
        #endregion
    }
}
