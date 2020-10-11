using davClassLibrary.DataAccess;
using davClassLibrary.Models;
using davClassLibrary.Providers;
using Microsoft.Toolkit.Uwp.Helpers;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using UniversalSoundboard.Common;
using UniversalSoundboard.Components;
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
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Notifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using static davClassLibrary.Models.TableObject;

namespace UniversalSoundBoard.DataAccess
{
    public class FileManager
    {
        #region Variables
        #region Design constants
        public const int mobileMaxWidth = 775;
        public const int topButtonsCollapsedMaxWidth = 1400;
        public const int sideBarCollapsedMaxWidth = 1100;
        public const int hideSearchBoxMaxWidth = 700;
        #endregion

        #region Colors for the background of PlayingSoundsBar and SideBar
        private static readonly double sideBarAcrylicBackgroundTintOpacity = 0.6;
        private static readonly double playingSoundsBarAcrylicBackgroundTintOpacity = 0.85;
        private static Color sideBarLightBackgroundColor = Color.FromArgb(255, 245, 245, 245);            // #f5f5f5
        private static Color sideBarDarkBackgroundColor = Color.FromArgb(255, 29, 34, 49);                // #1d2231
        private static Color playingSoundsBarLightBackgroundColor = Color.FromArgb(255, 253, 253, 253);   // #fdfdfd
        private static Color playingSoundsBarDarkBackgroundColor = Color.FromArgb(255, 15, 20, 35);       // #0f1423
        #endregion

        #region dav Keys
        private const string ApiKeyProduction = "gHgHKRbIjdguCM4cv5481hdiF5hZGWZ4x12Ur-7v";     // Prod
        public const string ApiKeyDevelopment = "eUzs3PQZYweXvumcWvagRHjdUroGe5Mo7kN1inHm";     // Dev
        public static string ApiKey => Environment == DavEnvironment.Production ? ApiKeyProduction : ApiKeyDevelopment;

        private const string WebsiteBaseUrlProduction = "https://dav-apps.herokuapp.com";
        private const string WebsiteBaseUrlDevelopment = "https://4de1c1ca4055.ngrok.io";
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
        #endregion

        #region Table property names
        public const string SoundTableNamePropertyName = "name";
        public const string SoundTableFavouritePropertyName = "favourite";
        public const string SoundTableSoundUuidPropertyName = "sound_uuid";
        public const string SoundTableImageUuidPropertyName = "image_uuid";
        public const string SoundTableCategoryUuidPropertyName = "category_uuid";
        public const string SoundTableDefaultVolumePropertyName = "default_volume";
        public const string SoundTableDefaultMutedPropertyName = "default_muted";

        public const string CategoryTableParentPropertyName = "parent";
        public const string CategoryTableNamePropertyName = "name";
        public const string CategoryTableIconPropertyName = "icon";

        public const string PlayingSoundTableSoundIdsPropertyName = "sound_ids";
        public const string PlayingSoundTableCurrentPropertyName = "current";
        public const string PlayingSoundTableRepetitionsPropertyName = "repetitions";
        public const string PlayingSoundTableRandomlyPropertyName = "randomly";
        public const string PlayingSoundTableVolumePropertyName = "volume";
        public const string PlayingSoundTableVolume2PropertyName = "volume2";
        public const string PlayingSoundTableMutedPropertyName = "muted";

        public const string OrderTableTypePropertyName = "type";
        public const string OrderTableCategoryPropertyName = "category";
        public const string OrderTableFavouritePropertyName = "favs";
        #endregion

        #region Other constants
        public const string TableObjectExtPropertyName = "ext";
        public const string CategoryOrderType = "0";
        public const string SoundOrderType = "1";

        public const string ImportFolderName = "import";
        public const string ImportZipFileName = "import.zip";
        public const string ExportFolderName = "export";
        public const string ExportZipFileName = "export.zip";
        public const string TileFolderName = "tile";

        public static List<string> allowedFileTypes = new List<string>
        {
            ".mp3",
            ".wav",
            ".ogg",
            ".wma",
            ".flac"
        };
        #endregion

        #region Enums
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

        public enum AppState
        {
            Loading,
            InitialSync,
            Empty,
            Normal
        }
        #endregion

        #region Local variables
        public static ItemViewHolder itemViewHolder;
        public static DavEnvironment Environment = DavEnvironment.Production;

        private static readonly ResourceLoader loader = new ResourceLoader();
        internal static bool syncFinished = false;
        private static bool isCalculatingSoundboardSize = false;
        private static bool isShowAllSoundsOrCategoryRunning = false;
        private static Guid nextCategoryToShow = Guid.Empty;

        // Save the custom order of the sounds in all categories to load them faster
        private static readonly Dictionary<Guid, List<Guid>> CustomSoundOrder = new Dictionary<Guid, List<Guid>>();
        private static readonly Dictionary<Guid, List<Guid>> CustomFavouriteSoundOrder = new Dictionary<Guid, List<Guid>>();
        private static readonly List<Guid> LoadedSoundOrders = new List<Guid>();
        #endregion
        #endregion

        #region Filesystem methods
        private async static Task<StorageFolder> GetImportFolderAsync()
        {
            StorageFolder cacheFolder = ApplicationData.Current.LocalCacheFolder;

            if (await cacheFolder.TryGetItemAsync(ImportFolderName) == null)
                return await cacheFolder.CreateFolderAsync(ImportFolderName);
            else
                return await cacheFolder.GetFolderAsync(ImportFolderName);
        }

        private static async Task<StorageFolder> GetExportFolderAsync()
        {
            StorageFolder cacheFolder = ApplicationData.Current.LocalCacheFolder;

            if (await cacheFolder.TryGetItemAsync(ExportFolderName) == null)
                return await cacheFolder.CreateFolderAsync(ExportFolderName);
            else
                return await cacheFolder.GetFolderAsync(ExportFolderName);
        }

        private static async Task<StorageFolder> GetTileFolderAsync()
        {
            StorageFolder cacheFolder = ApplicationData.Current.LocalCacheFolder;

            if (await cacheFolder.TryGetItemAsync(TileFolderName) == null)
                return await cacheFolder.CreateFolderAsync(TileFolderName);
            else
                return await cacheFolder.GetFolderAsync(TileFolderName);
        }

        public static string GetDavDataPath()
        {
            string path = Path.Combine(ApplicationData.Current.LocalFolder.Path, "dav");
            Directory.CreateDirectory(path);
            return path;
        }

        private static async Task ClearImportCacheAsync()
        {
            if (itemViewHolder.Importing) return;

            StorageFolder cacheFolder = ApplicationData.Current.LocalCacheFolder;

            // Delete the import folder
            var importFolder = await cacheFolder.TryGetItemAsync(ImportFolderName);
            if (importFolder != null)
                await importFolder.DeleteAsync();

            // Delete the import zip file
            var importZipFile = await cacheFolder.TryGetItemAsync(ImportZipFileName);
            if (importZipFile != null)
                await importZipFile.DeleteAsync();
        }

        private static async Task ClearExportCacheAsync()
        {
            if (itemViewHolder.Exporting) return;

            StorageFolder cacheFolder = ApplicationData.Current.LocalCacheFolder;

            // Delete the export folder
            var exportFolder = await cacheFolder.TryGetItemAsync(ExportFolderName);
            if (exportFolder != null)
                await exportFolder.DeleteAsync();

            // Delete the export zip file
            var exportZipFile = await cacheFolder.TryGetItemAsync(ExportZipFileName);
            if (exportZipFile != null)
                await exportZipFile.DeleteAsync();
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
        #endregion

        #region Export / Import
        public static async Task ExportDataAsync(StorageFolder destinationFolder)
        {
            await ClearExportCacheAsync();

            itemViewHolder.Exported = false;
            itemViewHolder.Imported = false;
            itemViewHolder.Exporting = true;
            itemViewHolder.ExportAndImportButtonsEnabled = false;

            StorageFolder localCacheFolder = ApplicationData.Current.LocalCacheFolder;
            StorageFolder exportFolder = await GetExportFolderAsync();
            var progress = new Progress<int>((int value) => itemViewHolder.ExportMessage = value + " %");

            await DatabaseOperations.ExportDataAsync(exportFolder, progress);

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

            await ClearExportCacheAsync();

            itemViewHolder.ExportMessage = "";
            itemViewHolder.Exported = true;
            itemViewHolder.ExportAndImportButtonsEnabled = true;
        }

        public static async Task ImportDataAsync(StorageFile zipFile, bool startMessage)
        {
            SetImportMessage(loader.GetString("ImportMessage-1"), startMessage);

            if (startMessage)
                itemViewHolder.LoadingScreenVisible = true;

            await ClearImportCacheAsync();

            // Extract the file into the local cache folder
            itemViewHolder.Importing = true;
            itemViewHolder.Exported = false;
            itemViewHolder.Imported = false;
            itemViewHolder.ExportAndImportButtonsEnabled = false;

            StorageFolder localCacheFolder = ApplicationData.Current.LocalCacheFolder;
            StorageFolder importFolder = await GetImportFolderAsync();

            await Task.Run(async () =>
            {
                StorageFile newZipFile = await zipFile.CopyAsync(localCacheFolder, ImportZipFileName, NameCollisionOption.ReplaceExisting);

                // Extract the zip file
                ZipFile.ExtractToDirectory(newZipFile.Path, importFolder.Path);
            });

            DataModel dataModel = await GetDataModelAsync(importFolder);
            Progress<int> progress = new Progress<int>((int value) => SetImportMessage(string.Format("{0} %", value), startMessage));

            switch (dataModel)
            {
                case DataModel.Old:
                    await UpgradeOldDataModelAsync(importFolder, true, progress);
                    break;
                case DataModel.New:
                    await UpgradeNewDataModelAsync(importFolder, true, progress);
                    break;
                default:
                    await Task.Run(() => DatabaseOperations.ImportDataAsync(importFolder, progress));
                    break;
            }

            SetImportMessage(loader.GetString("ExportImportMessage-TidyUp"), startMessage);     // TidyUp
            itemViewHolder.Importing = false;

            await ClearImportCacheAsync();

            SetImportMessage("", startMessage);
            itemViewHolder.Imported = true;
            itemViewHolder.AllSoundsChanged = true;
            itemViewHolder.ExportAndImportButtonsEnabled = true;

            if (startMessage)
                itemViewHolder.LoadingScreenVisible = false;

            await LoadCategoriesAsync();
            await LoadAllSoundsAsync();
            await LoadPlayingSoundsAsync();
            await CalculateSoundboardSizeAsync();
        }

        private static void SetImportMessage(string message, bool startMessage)
        {
            if (startMessage)
                itemViewHolder.LoadingScreenMessage = string.Format("{0}\n{1}", loader.GetString("ImportMessage-0"), message);
            else
                itemViewHolder.ImportMessage = message;
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
                                await DatabaseOperations.UpdateSoundAsync(soundUuid, null, null, null, null, imageUuid, null);
                                await imageFile.DeleteAsync();
                            }
                        }

                        if (soundData.Favourite)
                            await DatabaseOperations.UpdateSoundAsync(soundUuid, null, soundData.Favourite, null, null, null, null);

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
                                await DatabaseOperations.UpdateSoundAsync(soundUuid, null, null, null, null, imageUuid, null);
                                await imageFile.DeleteAsync();
                            }
                        }

                        if (sound.favourite)
                            await DatabaseOperations.UpdateSoundAsync(soundUuid, null, sound.favourite, null, null, null, null);

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
                            await DatabaseOperations.UpdateSoundAsync(soundUuid, null, null, null, null, imageUuid, null);

                            // Delete the image
                            await imageFile.DeleteAsync();
                            break;
                        }
                    }

                    if (favourite)
                        await DatabaseOperations.UpdateSoundAsync(soundUuid, null, favourite, null, null, null, null);

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
                await ClearExportCacheAsync();
                StorageFolder localCacheFolder = ApplicationData.Current.LocalCacheFolder;
                StorageFolder exportFolder = await GetExportFolderAsync();

                // Copy the selected files into the export folder
                foreach (var sound in sounds)
                    await CopySoundFileIntoFolderAsync(sound, exportFolder);

                // Create the zip file from the export folder
                StorageFile zipFile = await Task.Run(async () =>
                {
                    string exportFilePath = Path.Combine(localCacheFolder.Path, ExportZipFileName);
                    ZipFile.CreateFromDirectory(exportFolder.Path, exportFilePath);
                    return await StorageFile.GetFileFromPathAsync(exportFilePath);
                });

                // Move the zip file into the destination folder
                await zipFile.MoveAsync(destinationFolder, string.Format("UniversalSoundboard {0}.zip", DateTime.Today.ToString("dd.MM.yyyy")), NameCollisionOption.GenerateUniqueName);
                await ClearExportCacheAsync();
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
            string ext = sound.GetAudioFileExtension();

            if (string.IsNullOrEmpty(ext))
                ext = "mp3";

            StorageFile soundFile = await destinationFolder.CreateFileAsync(string.Format("{0}.{1}", sound.Name, ext), CreationCollisionOption.GenerateUniqueName);
            await FileIO.WriteBytesAsync(soundFile, await GetBytesAsync(sound.AudioFile));
        }
        #endregion

        #region Sound methods
        private static async Task<bool> LoadSoundsFromDatabaseAsync()
        {
            if (!itemViewHolder.AllSoundsChanged) return false;

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

            itemViewHolder.AllSoundsChanged = false;
            return true;
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

            Sound sound = new Sound(soundTableObject.Uuid, soundTableObject.GetPropertyValue(SoundTableNamePropertyName));

            // Get the audio file
            Guid? audioFileUuid = ConvertStringToGuid(soundTableObject.GetPropertyValue(SoundTableSoundUuidPropertyName));
            if(audioFileUuid.HasValue && !audioFileUuid.Equals(Guid.Empty))
            {
                var audioFileTableObject = await DatabaseOperations.GetTableObjectAsync(audioFileUuid.Value);
                if(audioFileTableObject != null)
                {
                    sound.AudioFileTableObject = audioFileTableObject;

                    if (!audioFileTableObject.IsFile) return null;

                    if(audioFileTableObject.FileDownloadStatus == TableObjectFileDownloadStatus.Downloaded)
                    {
                        try
                        {
                            var audioFile = await StorageFile.GetFileFromPathAsync(audioFileTableObject.File.FullName);
                            if (audioFile != null)
                                sound.AudioFile = audioFile;
                        }
                        catch { }
                    }else if(audioFileTableObject.FileDownloadStatus == TableObjectFileDownloadStatus.NoFileOrNotLoggedIn)
                        return null;
                }
                else
                    return null;
            }

            // Get favourite
            bool favourite = false;
            var favouriteString = soundTableObject.GetPropertyValue(SoundTableFavouritePropertyName);
            if (!string.IsNullOrEmpty(favouriteString))
                bool.TryParse(favouriteString, out favourite);

            sound.Favourite = favourite;

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

            // Get the image
            BitmapImage image = new BitmapImage { UriSource = Sound.GetDefaultImageUri() };

            Guid? imageFileUuid = ConvertStringToGuid(soundTableObject.GetPropertyValue(SoundTableImageUuidPropertyName));
            if (imageFileUuid.HasValue && !imageFileUuid.Equals(Guid.Empty))
            {
                var imageFileTableObject = await DatabaseOperations.GetTableObjectAsync(imageFileUuid.Value);
                if(imageFileTableObject != null)
                {
                    sound.ImageFileTableObject = imageFileTableObject;

                    if (
                        imageFileTableObject.IsFile
                        && imageFileTableObject.FileDownloadStatus == TableObjectFileDownloadStatus.Downloaded
                    )
                    {
                        string imageFilePath = imageFileTableObject.File.FullName;
                        image.UriSource = new Uri(imageFilePath);

                        // Get the image file
                        try
                        {
                            var imageFile = await StorageFile.GetFileFromPathAsync(imageFilePath);
                            if (imageFile != null)
                                sound.ImageFile = imageFile;
                        } catch { }
                    }
                }
            }

            sound.Image = image;

            // DefaultVolume
            int defaultVolume = 100;
            string defaultVolumeString = soundTableObject.GetPropertyValue(SoundTableDefaultVolumePropertyName);
            if (!string.IsNullOrEmpty(defaultVolumeString))
                int.TryParse(defaultVolumeString, out defaultVolume);

            sound.DefaultVolume = defaultVolume;

            // DefaultMuted
            bool defaultMuted = false;
            string defaultMutedString = soundTableObject.GetPropertyValue(SoundTableDefaultMutedPropertyName);
            if (!string.IsNullOrEmpty(defaultMutedString))
                bool.TryParse(defaultMutedString, out defaultMuted);

            sound.DefaultMuted = defaultMuted;

            return sound;
        }

        /**
         * Copies all sounds into the Sounds and FavouriteSounds lists
        */
        public static async Task LoadAllSoundsAsync()
        {
            if(itemViewHolder.AppState != AppState.InitialSync)
                itemViewHolder.AppState = AppState.Loading;

            bool soundsLoaded = await LoadSoundsFromDatabaseAsync();

            // Get all sounds and all favourite sounds
            List<Sound> allSounds = new List<Sound>();
            List<Sound> allFavouriteSounds = new List<Sound>();
            foreach (var sound in itemViewHolder.AllSounds)
            {
                allSounds.Add(sound);
                if (sound.Favourite) allFavouriteSounds.Add(sound);
            }

            // Sort the sounds
            var sortedSounds = await SortSoundsList(allSounds, itemViewHolder.SoundOrder, itemViewHolder.SoundOrderReversed, Guid.Empty, false);
            var sortedFavouriteSounds = await SortSoundsList(allFavouriteSounds, itemViewHolder.SoundOrder, itemViewHolder.SoundOrderReversed, Guid.Empty, true);

            itemViewHolder.Sounds.Clear();
            itemViewHolder.FavouriteSounds.Clear();

            // Hide the progress ring
            if (itemViewHolder.AppState == AppState.Loading)
                itemViewHolder.AppState = itemViewHolder.AllSounds.Count == 0 ? AppState.Empty : AppState.Normal;

            // Add the sounds to the lists
            foreach (var sound in sortedSounds)
                itemViewHolder.Sounds.Add(sound);

            foreach (var sound in sortedFavouriteSounds)
                itemViewHolder.FavouriteSounds.Add(sound);

            // Update the LiveTile if the Sounds were just loaded
            if(soundsLoaded)
                UpdateLiveTileAsync();
        }

        /**
         * Copies the sounds of the category into the Sounds and FavouriteSounds lists
        */
        public static async Task LoadSoundsOfCategoryAsync(Guid categoryUuid)
        {
            if (itemViewHolder.AppState != AppState.InitialSync)
                itemViewHolder.AppState = AppState.Loading;

            await LoadSoundsFromDatabaseAsync();

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

            // Sort the sounds
            var sortedSounds = await SortSoundsList(sounds, itemViewHolder.SoundOrder, itemViewHolder.SoundOrderReversed, categoryUuid, false);
            var sortedFavouriteSounds = await SortSoundsList(favouriteSounds, itemViewHolder.SoundOrder, itemViewHolder.SoundOrderReversed, categoryUuid, true);

            itemViewHolder.Sounds.Clear();
            itemViewHolder.FavouriteSounds.Clear();

            if (itemViewHolder.AppState == AppState.Loading)
                itemViewHolder.AppState = AppState.Normal;

            // Add the sounds to the lists
            foreach (var sound in sortedSounds)
                itemViewHolder.Sounds.Add(sound);

            foreach (var sound in sortedFavouriteSounds)
                itemViewHolder.FavouriteSounds.Add(sound);
        }

        /**
         * Copies the sounds starting with the name into the Sounds and FavouriteSounds lists
        */
        public static async Task LoadSoundsByNameAsync(string name)
        {
            if (itemViewHolder.AppState != AppState.InitialSync)
                itemViewHolder.AppState = AppState.Loading;

            await LoadSoundsFromDatabaseAsync();

            itemViewHolder.Sounds.Clear();
            itemViewHolder.FavouriteSounds.Clear();

            if (itemViewHolder.AppState == AppState.Loading)
                itemViewHolder.AppState = AppState.Normal;

            foreach (var sound in itemViewHolder.AllSounds)
            {
                if (!sound.Name.ToLower().Contains(name.ToLower())) continue;

                itemViewHolder.Sounds.Add(sound);
                if (sound.Favourite) itemViewHolder.FavouriteSounds.Add(sound);
            }
        }

        public static async Task ShowAllSoundsAsync()
        {
            nextCategoryToShow = Guid.Empty;
            if (isShowAllSoundsOrCategoryRunning) return;
            isShowAllSoundsOrCategoryRunning = true;

            itemViewHolder.Page = typeof(SoundPage);
            itemViewHolder.Title = loader.GetString("AllSounds");
            itemViewHolder.SelectedCategory = Guid.Empty;
            itemViewHolder.SearchQuery = "";
            itemViewHolder.BackButtonEnabled = false;
            itemViewHolder.EditButtonVisible = false;

            await LoadAllSoundsAsync();
            UpdatePlayAllButtonVisibility();

            isShowAllSoundsOrCategoryRunning = false;
            if (!nextCategoryToShow.Equals(Guid.Empty))
            {
                // If ShowAllSoundsAsync or ShowCategoryAsync was called in the meantime, run the appropriate method again
                await ShowCategoryAsync(nextCategoryToShow);
            }
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

            nextCategoryToShow = uuid;
            if (isShowAllSoundsOrCategoryRunning) return;
            isShowAllSoundsOrCategoryRunning = true;

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

            isShowAllSoundsOrCategoryRunning = false;
            if (!nextCategoryToShow.Equals(uuid))
            {
                // If ShowAllSoundsAsync or ShowCategoryAsync was called in the meantime, run the appropriate method again
                if (nextCategoryToShow.Equals(Guid.Empty))
                    await ShowAllSoundsAsync();
                else
                    await ShowCategoryAsync(nextCategoryToShow);
            }
        }

        public static async Task AddAllSounds()
        {
            var allSoundTableObjects = await DatabaseOperations.GetAllSoundsAsync();
            foreach(var soundTableObject in allSoundTableObjects)
                AddSound(await GetSoundAsync(soundTableObject));
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
            if (sound == null) return;

            // Add to AllSounds
            if(!itemViewHolder.AllSounds.ToList().Exists(s => s.Uuid == sound.Uuid))
                itemViewHolder.AllSounds.Add(sound);

            if (LoadedSoundOrders.Contains(Guid.Empty) && !CustomSoundOrder[Guid.Empty].Contains(sound.Uuid))
            {
                // Add the sound to the custom sound order dictionaries
                CustomSoundOrder[Guid.Empty].Add(sound.Uuid);
            }

            // Get all categories and all parent categories to which the sound belongs
            List<Category> parentCategories = new List<Category>();
            foreach (var category in sound.Categories)
            {
                // Add the sound to the category and the parent categories
                foreach (var c in GetCategoryPath(category))
                {
                    parentCategories.Add(c);

                    if (LoadedSoundOrders.Contains(c.Uuid) && CustomSoundOrder.ContainsKey(c.Uuid) && !CustomSoundOrder[c.Uuid].Contains(sound.Uuid))
                        CustomSoundOrder[c.Uuid].Add(sound.Uuid);
                }
            }

            // Check if the sound belongs to the selected category
            bool soundBelongsToSelectedCategory =
                itemViewHolder.SelectedCategory == Guid.Empty
                || parentCategories.Exists(c => c.Uuid == itemViewHolder.SelectedCategory);

            // Add to the current sounds
            if (soundBelongsToSelectedCategory && !itemViewHolder.Sounds.ToList().Exists(s => s.Uuid == sound.Uuid))
                itemViewHolder.Sounds.Add(sound);

            if (itemViewHolder.AllSounds.Count > 0 && itemViewHolder.AppState == AppState.Empty)
                itemViewHolder.AppState = AppState.Normal;
        }

        /**
         * Replaces the sound in all sound lists
         */
        public static async Task ReloadSound(Guid uuid)
        {
            var sound = await GetSoundAsync(uuid);
            if(sound != null) await ReloadSound(sound);
        }

        public static async Task ReloadSound(Sound updatedSound)
        {
            bool soundBelongsToSelectedCategory = true;

            if (!itemViewHolder.SelectedCategory.Equals(Guid.Empty))
            {
                // Get the subcategories of the selected category
                List<Guid> selectedCategoryChildren = new List<Guid>();

                foreach (var subcategory in await GetSubCategoriesOfCategory(itemViewHolder.SelectedCategory))
                    selectedCategoryChildren.Add(subcategory);

                // Check if the sound belongs to the selected category or a child of the selected category
                soundBelongsToSelectedCategory = updatedSound.Categories.Exists(c => 
                    selectedCategoryChildren.Exists(uuid => 
                        uuid.Equals(c.Uuid)));
            }

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
            else if (updatedSound.Favourite)
                itemViewHolder.FavouriteSounds.Add(updatedSound);

            // Replace in PlayingSounds
            foreach(var playingSound in itemViewHolder.PlayingSounds)
            {
                i = playingSound.Sounds.ToList().FindIndex(s => s.Uuid == updatedSound.Uuid);
                if (i != -1)
                    playingSound.Sounds[i] = updatedSound;
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

            itemViewHolder.TriggerSoundDeletedEvent(null, new SoundEventArgs(uuid));

            if (itemViewHolder.AllSounds.Count == 0 && itemViewHolder.AppState == AppState.Normal)
                itemViewHolder.AppState = AppState.Empty;
        }

        public static void UpdateLiveTileAsync()
        {
            if (itemViewHolder.AllSounds.Count == 0 || !itemViewHolder.LiveTile)
            {
                TileUpdateManager.CreateTileUpdaterForApplication().Clear();
                return;
            }

            // Get all sounds with an image
            List<Sound> sounds = itemViewHolder.AllSounds.Where(s => s.ImageFileTableObject != null && s.ImageFile != null).ToList();
            if (sounds.Count == 0)
            {
                TileUpdateManager.CreateTileUpdaterForApplication().Clear();
                return;
            }

            // Pick up to 12 random sounds with images
            List<StorageFile> images = new List<StorageFile>();
            Random random = new Random();

            for (int i = 0; i < 9; i++)
            {
                // Pick a random sound from the list
                int selectedSoundIndex = random.Next(sounds.Count);

                // Get the image of the sound and add it to the images list
                var selectedSound = sounds.ElementAt(selectedSoundIndex);
                images.Add(selectedSound.ImageFile);

                // Remove the selected sound from the sounds list
                sounds.RemoveAt(selectedSoundIndex);

                if (sounds.Count == 0) break;
            }

            var photos = new TileBindingContentPhotos();
            foreach(var image in images)
                photos.Images.Add(new TileBasicImage() { Source = image.Path });

            TileBinding binding = new TileBinding()
            {
                Branding = TileBranding.NameAndLogo,
                Content = photos
            };

            TileContent content = new TileContent()
            {
                Visual = new TileVisual()
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

        public static async Task RemoveNotLocallySavedSoundsAsync()
        {
            // Get each sound and check if the file exists
            foreach (var sound in itemViewHolder.AllSounds)
            {
                var soundFileTableObject = sound.AudioFileTableObject;
                if (soundFileTableObject != null && soundFileTableObject.FileDownloaded()) continue;

                // Completely remove the sound from the database so that it won't be deleted when the user logs in again
                var imageFileTableObject = sound.ImageFileTableObject;
                var soundTableObject = await DatabaseOperations.GetTableObjectAsync(sound.Uuid);

                if (soundFileTableObject != null)
                    await soundFileTableObject.DeleteImmediatelyAsync();

                if (imageFileTableObject != null)
                    await imageFileTableObject.DeleteImmediatelyAsync();

                if (soundTableObject != null)
                    await soundTableObject.DeleteImmediatelyAsync();

                RemoveSound(sound.Uuid);
            }
        }
        #endregion

        #region Category methods
        public static async Task LoadCategoriesAsync()
        {
            itemViewHolder.Categories.Clear();
            itemViewHolder.Categories.Add(new Category(Guid.Empty, loader.GetString("AllSounds"), "\uE10F"));

            foreach (Category cat in await GetAllCategoriesAsync())
                itemViewHolder.Categories.Add(cat);

            itemViewHolder.TriggerCategoriesLoadedEvent(null);
        }

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

            if (LoadedSoundOrders.Contains(category.Uuid))
            {
                // Add the category to the CustomSoundOrders
                if (!CustomSoundOrder.ContainsKey(category.Uuid))
                    CustomSoundOrder[category.Uuid] = new List<Guid>();

                if (!CustomFavouriteSoundOrder.ContainsKey(category.Uuid))
                    CustomFavouriteSoundOrder[category.Uuid] = new List<Guid>();
            }

            itemViewHolder.TriggerCategoryAddedEvent(null, new CategoryEventArgs(category.Uuid));
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
            itemViewHolder.TriggerCategoryUpdatedEvent(null, new CategoryEventArgs(updatedCategory.Uuid));
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
            itemViewHolder.TriggerCategoryDeletedEvent(null, new CategoryEventArgs(uuid));
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
            List<Guid> subcategories = new List<Guid> { categoryUuid };

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

        /**
         * Goes through all categories recursively and creates for each category a CustomTreeViewNode, with the Uuid of the category as Tag
         * Also adds each CustomTreeViewNode to selectedNodes, if selectedCategories contains the Uuid of the category
         */
        public static List<CustomTreeViewNode> CreateTreeViewNodesFromCategories(
            List<Category> categories,
            List<CustomTreeViewNode> selectedNodes,
            List<Guid> selectedCategories
        )
        {
            List<CustomTreeViewNode> nodes = new List<CustomTreeViewNode>();

            foreach (var category in categories)
            {
                CustomTreeViewNode node = new CustomTreeViewNode()
                {
                    Content = category.Name,
                    Tag = category.Uuid
                };

                foreach (var childNode in CreateTreeViewNodesFromCategories(category.Children, selectedNodes, selectedCategories))
                    node.Children.Add(childNode);

                nodes.Add(node);

                if (selectedCategories.Exists(c => c == category.Uuid))
                    selectedNodes.Add(node);
            }

            return nodes;
        }

        /**
         * Returns a list of all nested parents of the category in the correct order and the category itself
         */
        public static List<Category> GetCategoryPath(Category searchedCategory)
        {
            return GetCategoryPath(searchedCategory.Uuid);
        }

        public static List<Category> GetCategoryPath(Guid searchedCategory)
        {
            List<Category> parentCategories = new List<Category>();

            foreach (var currentCategory in itemViewHolder.Categories)
            {
                if (currentCategory.Uuid.Equals(Guid.Empty)) continue;

                parentCategories.Add(currentCategory);
                if (BuildCategoryPath(parentCategories, searchedCategory))
                    return parentCategories;
                else
                {
                    // Remove the category from the list
                    parentCategories.Remove(currentCategory);
                }
            }

            return parentCategories;
        }

        /**
         * Goes through the nested categories tree and builds the category path in the parentCategories list.
         * Returns true if the last parent category is the searched category or contains it within the child tree
         */
        public static bool BuildCategoryPath(List<Category> parentCategories, Guid searchedCategory)
        {
            // Check if the last category of the parent categories is the searched category
            if (parentCategories.Last().Uuid == searchedCategory) return true;

            // Add each child of the last parent category to the parent categories and call this method
            foreach (var childCategory in parentCategories.Last().Children)
            {
                parentCategories.Add(childCategory);

                if (BuildCategoryPath(parentCategories, searchedCategory))
                    return true;
                else
                {
                    // Remove the child category from the list
                    parentCategories.Remove(parentCategories.Last());
                }
            }

            return false;
        }

        /**
         * Finds the category with the given uuid, moves it up or down within the list and saves the new category order
         * Should be called with categories = itemViewHolder.Categories and with parentCategoryUuid = Guid.Empty, if the category is not a subcategory
         */
        public static async Task<bool> MoveCategoryAndSaveOrderAsync(List<Category> categories, Guid searchedCategoryUuid, Guid parentCategoryUuid, bool up)
        {
            bool categoryFound = false;
            int i = 0;

            foreach (var category in categories)
            {
                if (category.Uuid.Equals(searchedCategoryUuid))
                {
                    categoryFound = true;
                    break;
                }
                else
                {
                    if (await MoveCategoryAndSaveOrderAsync(category.Children, searchedCategoryUuid, category.Uuid, up))
                        return true;
                }

                i++;
            }

            if (categoryFound)
            {
                // Move the menu item
                var category = categories[i];
                categories.Remove(category);
                if (up)
                    categories.Insert(i - 1, category);
                else
                    categories.Insert(i + 1, category);

                await UpdateCustomCategoriesOrder(categories, parentCategoryUuid);
                return true;
            }
            return false;
        }

        /**
         * Finds the category with the given uuid, moves it into the Children of the category above or below and saves the new category orders and the new parent of the moved category
         * Should be called with categories = itemViewHolder.Categories
         */
        public static async Task<bool> MoveCategoryToCategoryAndSaveOrderAsync(List<Category> categories, Guid searchedCategoryUuid, bool up)
        {
            for (int i = 0; i < categories.Count; i++)
            {
                var currentCategory = categories[i];

                if (currentCategory.Uuid.Equals(searchedCategoryUuid))
                {
                    var movedElement = categories.ElementAt(i);
                    if (up && i == 0) return true;

                    // Remove the category from the categories
                    categories.RemoveAt(i);

                    // Add the category to the children of the category above or below
                    Category targetCategory;

                    if (up)
                    {
                        targetCategory = categories[i - 1];
                        targetCategory.Children.Add(movedElement);
                    }
                    else
                    {
                        targetCategory = categories[i];
                        targetCategory.Children.Insert(0, movedElement);
                    }

                    // Save the new order of the children
                    await UpdateCustomCategoriesOrder(targetCategory.Children, targetCategory.Uuid);

                    // Update the parent of the moved category
                    await UpdateParentOfCategoryAsync(movedElement.Uuid, targetCategory.Uuid);
                    ReloadCategory(movedElement);

                    return true;
                }
                else if (await MoveCategoryToCategoryAndSaveOrderAsync(currentCategory.Children, searchedCategoryUuid, up))
                    return true;
            }
            return false;
        }

        /**
         * Finds the category with the given uuid, moves it into the Children of the parent category above or below and saves the new category orders and the parent of the moved category
         * Should be called with categories = itemViewHolder.Categories
         */
        public static async Task<bool> MoveCategoryToParentAndSaveOrderAsync(List<Category> categories, Guid parentUuid, Guid searchedCategoryUuid, bool up)
        {
            for (int i = 0; i < categories.Count; i++)
            {
                bool categoryFound = false;
                int j = 0;
                var currentCategory = categories[i];

                foreach (var childCategory in currentCategory.Children)
                {
                    if (childCategory.Uuid.Equals(searchedCategoryUuid))
                    {
                        categoryFound = true;
                        break;
                    }

                    j++;
                }

                if (categoryFound)
                {
                    var movedElement = currentCategory.Children.ElementAt(j);

                    // Remove the child from the children
                    currentCategory.Children.RemoveAt(j);
                    await UpdateCustomCategoriesOrder(currentCategory.Children, currentCategory.Uuid);

                    // Add the element to the parent
                    if (up)
                        categories.Insert(i, movedElement);
                    else
                        categories.Insert(i + 1, movedElement);

                    await UpdateCustomCategoriesOrder(categories, parentUuid);
                    await UpdateParentOfCategoryAsync(movedElement.Uuid, parentUuid);
                    ReloadCategory(movedElement);

                    return true;
                }
                else if (await MoveCategoryToParentAndSaveOrderAsync(currentCategory.Children, currentCategory.Uuid, searchedCategoryUuid, up))
                    return true;
            }
            return false;
        }
        #endregion

        #region PlayingSound methods
        public static async Task LoadPlayingSoundsAsync()
        {
            var allPlayingSounds = await GetAllPlayingSoundsAsync();
            foreach (PlayingSound playingSound in allPlayingSounds)
            {
                // Delete each PlayingSound if the PlayingSoundBar is hidden or if the user doesn't want to save PlayingSounds
                if(
                    !itemViewHolder.PlayingSoundsListVisible
                    || !itemViewHolder.SavePlayingSounds
                )
                {
                    await DeletePlayingSoundAsync(playingSound.Uuid);
                }
                else if (playingSound.MediaPlayer != null)
                {
                    var currentPlayingSoundList = itemViewHolder.PlayingSounds.Where(p => p.Uuid == playingSound.Uuid);

                    if (currentPlayingSoundList.Count() > 0)
                    {
                        var currentPlayingSound = currentPlayingSoundList.First();
                        int index = itemViewHolder.PlayingSounds.IndexOf(currentPlayingSound);

                        // Update the current playing sound if it is currently not playing
                        if (currentPlayingSound.MediaPlayer.TimelineController.State != MediaTimelineControllerState.Running)
                        {
                            // Check if the playing sound changed
                            bool soundWasUpdated = (
                                currentPlayingSound.Randomly != playingSound.Randomly
                                || currentPlayingSound.Repetitions != playingSound.Repetitions
                                || currentPlayingSound.Sounds.Count != playingSound.Sounds.Count
                            );

                            if (currentPlayingSound.MediaPlayer != null && playingSound.MediaPlayer != null && !soundWasUpdated)
                                soundWasUpdated = currentPlayingSound.MediaPlayer.Volume != playingSound.MediaPlayer.Volume;

                            // Replace the playing sound if it has changed
                            if (soundWasUpdated)
                                itemViewHolder.PlayingSounds[index] = playingSound;
                        }
                    }
                    else
                    {
                        // Add the new playing sound
                        itemViewHolder.PlayingSounds.Add(playingSound);
                    }
                }
            }

            // Remove old playing sounds
            foreach (var ps in itemViewHolder.PlayingSounds)
            {
                // Remove the playing sound from ItemViewHolder if it does not exist in the new playing sounds list
                if (allPlayingSounds.Where(p => p.Uuid == ps.Uuid).Count() == 0)
                    itemViewHolder.PlayingSounds.Remove(ps);
            }

            itemViewHolder.TriggerPlayingSoundsLoadedEvent(null);
        }

        public static async Task ReloadPlayingSoundAsync(Guid uuid)
        {
            var playingSound = await GetPlayingSoundAsync(uuid);
            if (playingSound == null) return;

            var currentPlayingSoundList = itemViewHolder.PlayingSounds.Where(p => p.Uuid == playingSound.Uuid);
            if (currentPlayingSoundList.Count() == 0) return;

            var currentPlayingSound = currentPlayingSoundList.First();
            if (currentPlayingSound.MediaPlayer.TimelineController.State == MediaTimelineControllerState.Running) return;

            // Replace the playing sound
            int index = itemViewHolder.PlayingSounds.IndexOf(currentPlayingSound);
            if (index != -1) itemViewHolder.PlayingSounds[index] = playingSound;
        }

        public static void RemovePlayingSound(Guid uuid)
        {
            PlayingSound playingSound = itemViewHolder.PlayingSounds.ToList().Find(ps => ps.Uuid == uuid);
            if (playingSound == null) return;

            // Check if the PlayingSound is currently playing
            if (playingSound.MediaPlayer.TimelineController.State == MediaTimelineControllerState.Running) return;

            // Remove the playing sound
            itemViewHolder.PlayingSounds.Remove(playingSound);
        }

        public static (MediaPlayer, List<Sound>) CreateMediaPlayer(List<Sound> sounds, int current)
        {
            if (sounds.Count == 0) return (null, null);

            if (current >= sounds.Count)
                current = sounds.Count - 1;
            else if (current < 0)
                current = 0;

            MediaPlayer player = new MediaPlayer();
            List<Sound> newSounds = new List<Sound>();

            foreach (Sound sound in sounds)
                if (sound.GetAudioFileDownloadStatus() != TableObjectFileDownloadStatus.NoFileOrNotLoggedIn)
                    newSounds.Add(sound);

            player.CommandManager.IsEnabled = false;
            player.TimelineController = new MediaTimelineController();
            if(current < newSounds.Count && newSounds[current].AudioFile != null)
                player.Source = MediaSource.CreateFromStorageFile(newSounds[current].AudioFile);

            // Set the volume
            double appVolume = ((double)itemViewHolder.Volume) / 100;

            if (newSounds.Count == 1)
            {
                double defaultSoundVolume = ((double)newSounds[0].DefaultVolume) / 100;
                player.Volume = appVolume * defaultSoundVolume;
                player.IsMuted = newSounds[0].DefaultMuted;
            }
            else
                player.Volume = appVolume;

            return (player, newSounds);
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

            await newAudioFile.DeleteAsync();
            return soundTableObject.Uuid;
        }

        public static async Task RenameSoundAsync(Guid uuid, string newName)
        {
            await DatabaseOperations.UpdateSoundAsync(uuid, newName, null, null, null, null, null);
        }

        public static async Task SetCategoriesOfSoundAsync(Guid soundUuid, List<Guid> categoryUuids)
        {
            await DatabaseOperations.UpdateSoundAsync(soundUuid, null, null, null, null, null, categoryUuids);
        }

        public static async Task SetSoundAsFavouriteAsync(Guid uuid, bool favourite)
        {
            await DatabaseOperations.UpdateSoundAsync(uuid, null, favourite, null, null, null, null);
        }

        public static async Task SetDefaultVolumeOfSoundAsync(Guid uuid, int defaultVolume, bool defaultMuted)
        {
            await DatabaseOperations.UpdateSoundAsync(uuid, null, null, defaultVolume, defaultMuted, null, null);
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
                await DatabaseOperations.UpdateSoundAsync(soundUuid, null, null, null, null, imageFileUuid, null);
            }
            else
            {
                // Update the existing image file
                await DatabaseOperations.UpdateImageFileAsync(imageUuid.Value, newImageFile);
            }

            // Delete the image file
            await newImageFile.DeleteAsync();
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
            List<Category> categories = new List<Category>();
            List<Category> sortedCategories = new List<Category>();

            // Get all categories
            foreach (var categoryTableObject in categoriesTableObjectList)
            {
                string parent = categoryTableObject.GetPropertyValue(CategoryTableParentPropertyName);
                bool isRootCategory = parent == null;

                if (!isRootCategory)
                {
                    // Try to parse the uuid
                    Guid? parentUuid = ConvertStringToGuid(parent);
                    if (parentUuid.HasValue && parentUuid.Value.Equals(Guid.Empty))
                        isRootCategory = true;
                }

                if (isRootCategory)
                    categories.Add(await GetCategoryAsync(categoryTableObject.Uuid));
            }

            // Sort all categories
            foreach (var category in await SortCategoriesByCustomOrder(categories, Guid.Empty))
                sortedCategories.Add(category);

            return sortedCategories;
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
                List<Category> sortedChildren = new List<Category>();

                // Get the child categories
                foreach (var childObject in childrenTableObjects)
                {
                    Category category = await GetCategoryAsync(childObject.Uuid);
                    if (category != null) children.Add(category);
                }

                // Sort the child categories
                foreach (var category in await SortCategoriesByCustomOrder(children, uuid))
                    sortedChildren.Add(category);

                return new Category(
                    categoryTableObject.Uuid,
                    categoryTableObject.GetPropertyValue(CategoryTableNamePropertyName),
                    categoryTableObject.GetPropertyValue(CategoryTableIconPropertyName),
                    sortedChildren
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
            await DatabaseOperations.UpdateCategoryAsync(uuid, null, name, icon);
        }

        public static async Task UpdateParentOfCategoryAsync(Guid uuid, Guid parent)
        {
            await DatabaseOperations.UpdateCategoryAsync(uuid, parent, null, null);
        }

        public static async Task DeleteCategoryAsync(Guid uuid)
        {
            await DeleteSubCategoryAsync(uuid);
        }

        private static async Task DeleteSubCategoryAsync(Guid uuid)
        {
            // Get the category and its children
            Category category = await GetCategoryAsync(uuid);

            // Call this method for each children
            foreach (var subCategory in category.Children)
                await DeleteSubCategoryAsync(subCategory.Uuid);

            // Delete the SoundOrder table objects
            await DatabaseOperations.DeleteSoundOrderAsync(uuid, false);
            await DatabaseOperations.DeleteSoundOrderAsync(uuid, true);

            // Delete the CategoryOrder table objects
            await DatabaseOperations.DeleteCategoryOrderAsync(uuid);

            // Delete the category itself
            await DatabaseOperations.DeleteCategoryAsync(uuid);
        }
        #endregion

        #region PlayingSound CRUD methods
        public static async Task<Guid> CreatePlayingSoundAsync(Guid? uuid, List<Sound> sounds, int current, int repetitions, bool randomly, int? volume, bool? muted)
        {
            List<Guid> soundUuids = new List<Guid>();
            foreach (Sound sound in sounds)
                soundUuids.Add(sound.Uuid);

            return (await DatabaseOperations.CreatePlayingSoundAsync(uuid ?? Guid.NewGuid(), soundUuids, current, repetitions, randomly, volume ?? 100, muted ?? false)).Uuid;
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
            await DatabaseOperations.UpdatePlayingSoundAsync(uuid, null, current, null, null, null, null);
        }

        public static async Task SetRepetitionsOfPlayingSoundAsync(Guid uuid, int repetitions)
        {
            await DatabaseOperations.UpdatePlayingSoundAsync(uuid, null, null, repetitions, null, null, null);
        }

        public static async Task SetSoundsListOfPlayingSoundAsync(Guid uuid, List<Sound> sounds)
        {
            List<Guid> soundUuids = new List<Guid>();
            foreach (Sound sound in sounds)
                soundUuids.Add(sound.Uuid);

            await DatabaseOperations.UpdatePlayingSoundAsync(uuid, soundUuids, null, null, null, null, null);
        }

        public static async Task SetVolumeOfPlayingSoundAsync(Guid uuid, int volume)
        {
            if (volume >= 100)
                volume = 100;
            else if (volume <= 0)
                volume = 0;

            await DatabaseOperations.UpdatePlayingSoundAsync(uuid, null, null, null, null, volume, null);
        }

        public static async Task SetMutedOfPlayingSoundAsync(Guid uuid, bool muted)
        {
            await DatabaseOperations.UpdatePlayingSoundAsync(uuid, null, null, null, null, null, muted);
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

            // Get the properties of the table object
            int current = 0;
            string currentString = tableObject.GetPropertyValue(PlayingSoundTableCurrentPropertyName);
            if(!string.IsNullOrEmpty(currentString))
                int.TryParse(currentString, out current);

            int repetitions = 1;
            string repetitionsString = tableObject.GetPropertyValue(PlayingSoundTableRepetitionsPropertyName);
            if(!string.IsNullOrEmpty(repetitionsString))
                int.TryParse(repetitionsString, out repetitions);

            bool randomly = false;
            string randomlyString = tableObject.GetPropertyValue(PlayingSoundTableRandomlyPropertyName);
            if(!string.IsNullOrEmpty(randomlyString))
                bool.TryParse(randomlyString, out randomly);

            // Backwards compatibility for volume saved as double
            // Read from volume (double, 0 - 1), save to volume2 (int, 0 - 100)
            string volumeString = tableObject.GetPropertyValue(PlayingSoundTableVolumePropertyName);
            string volume2String = tableObject.GetPropertyValue(PlayingSoundTableVolume2PropertyName);
            double volume = 100;

            // Use volume2 if it exists, otherwise use volume
            if(!string.IsNullOrEmpty(volume2String))
            {
                double.TryParse(volume2String, out volume);
            }
            else if(!string.IsNullOrEmpty(volumeString))
            {
                double.TryParse(volumeString, out volume);
                volume *= 100;
            }

            bool muted = false;
            string mutedString = tableObject.GetPropertyValue(PlayingSoundTableMutedPropertyName);
            if(!string.IsNullOrEmpty(mutedString))
                bool.TryParse(mutedString, out muted);

            // Create the media player
            var createMediaPlayerResult = CreateMediaPlayer(sounds, current);
            MediaPlayer player = createMediaPlayerResult.Item1;
            List<Sound> newSounds = createMediaPlayerResult.Item2;

            if (player != null)
            {
                player.Volume = (double)itemViewHolder.Volume / 100 * (volume / 100);
                player.IsMuted = itemViewHolder.Muted || muted;

                return new PlayingSound(tableObject.Uuid, player, newSounds, current, repetitions, randomly, Convert.ToInt32(volume), muted);
            }
            else
            {
                // Remove the PlayingSound from the DB
                await DeletePlayingSoundAsync(tableObject.Uuid);
                return null;
            }
        }
        #endregion

        #region Category order methods
        public static async Task<List<Category>> SortCategoriesByCustomOrder(List<Category> categories, Guid parentCategoryUuid)
        {
            if (categories.Count <= 1) return categories;

            // Get the order table objects
            var tableObjects = await DatabaseOperations.GetAllOrdersAsync();
            bool rootCategories = parentCategoryUuid.Equals(Guid.Empty);

            // Get the order table objects with the type Category (0) and the right parent category
            var categoryOrderTableObjects = tableObjects.FindAll((TableObject obj) =>
            {
                // Check if the object is of type Category
                if (obj.GetPropertyValue(OrderTableTypePropertyName) != CategoryOrderType) return false;

                // Check if the object has the correct parent category uuid
                string categoryUuidString = obj.GetPropertyValue(OrderTableCategoryPropertyName);
                Guid? cUuid = ConvertStringToGuid(categoryUuidString);
                if (!cUuid.HasValue)
                {
                    // Return true if the object belongs to no category and the searched category is the root category
                    return rootCategories;
                }
                
                return cUuid.Value.Equals(parentCategoryUuid);
            });

            if (categoryOrderTableObjects.Count > 0)
            {
                bool saveNewOrder = false;
                TableObject lastOrderTableObject = categoryOrderTableObjects.Last();
                List<Guid> uuids = new List<Guid>();
                List<Category> sortedCategories = new List<Category>();

                // Add all categories to newCategories
                List<Category> newCategories = new List<Category>();
                foreach (var category in categories)
                    newCategories.Add(category);

                foreach (var property in lastOrderTableObject.Properties)
                {
                    if (!int.TryParse(property.Name, out int index)) continue;

                    Guid? categoryUuid = ConvertStringToGuid(property.Value);
                    if (!categoryUuid.HasValue) continue;

                    // Check if this uuid is already in the uuids list
                    if (uuids.Contains(categoryUuid.Value))
                    {
                        saveNewOrder = true;
                        continue;
                    }

                    // Get the category from the list
                    var category = categories.Find(c => c.Uuid == categoryUuid);
                    if (category == null) continue;

                    sortedCategories.Add(category);
                    uuids.Add(category.Uuid);
                    newCategories.Remove(category);
                }

                // Add the categories, that are not in the order, at the end
                foreach (var category in newCategories)
                {
                    sortedCategories.Add(category);
                    uuids.Add(category.Uuid);
                    saveNewOrder = true;
                }

                // If there are multiple order objects, merge them
                while (categoryOrderTableObjects.Count > 1)
                {
                    saveNewOrder = true;

                    // Merge the first order table object into the last one
                    var firstOrderTableObject = categoryOrderTableObjects.First();

                    // Go through each uuid of the order object
                    foreach (var property in firstOrderTableObject.Properties)
                    {
                        // Make sure the property is an index of the order
                        if (!int.TryParse(property.Name, out int index)) continue;

                        Guid? categoryUuid = ConvertStringToGuid(property.Value);
                        if (!categoryUuid.HasValue) continue;

                        //  Check if the uuid already exists in the first order object
                        if (uuids.Contains(categoryUuid.Value)) continue;

                        // Get the category from the list
                        var category = categories.Find(c => c.Uuid == categoryUuid);
                        if (category == null) continue;

                        // Add the property to uuids and sortedCategories
                        sortedCategories.Add(category);
                        uuids.Add(category.Uuid);
                    }

                    // Delete the object and remove it from the list
                    await firstOrderTableObject.DeleteAsync();
                    categoryOrderTableObjects.Remove(firstOrderTableObject);
                }

                if (saveNewOrder)
                    await DatabaseOperations.SetCategoryOrderAsync(parentCategoryUuid, uuids);

                return sortedCategories;
            }
            else
            {
                // Create the category order table object with the current order
                List<Guid> uuids = new List<Guid>();

                foreach (var category in categories)
                    uuids.Add(category.Uuid);

                await DatabaseOperations.SetCategoryOrderAsync(parentCategoryUuid, uuids);
                return categories;
            }
        }

        public static async Task UpdateCustomCategoriesOrder(List<Category> categories, Guid parentCategoryUuid)
        {
            List<Guid> categoryUuids = new List<Guid>();
            foreach (var category in categories)
                categoryUuids.Add(category.Uuid);

            await UpdateCustomCategoriesOrder(categoryUuids, parentCategoryUuid);
        }

        public static async Task UpdateCustomCategoriesOrder(List<Guid> categoryUuids, Guid parentCategoryUuid)
        {
            await DatabaseOperations.SetCategoryOrderAsync(parentCategoryUuid, categoryUuids);
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
            if (sounds.Count <= 1) return sounds;

            // Load the sound orders, if that didn't already happen
            await LoadCustomSoundOrderForCategoryAsync(categoryUuid);

            // Check if the order exists
            if (
                (favourites && !CustomFavouriteSoundOrder.ContainsKey(categoryUuid))
                || (!favourites && !CustomSoundOrder.ContainsKey(categoryUuid))
            ) return sounds;

            List<Sound> sortedSounds = new List<Sound>();
            List<Sound> soundsCopy = new List<Sound>();

            // Copy the sounds list
            foreach (var sound in sounds)
                soundsCopy.Add(sound);

            // Sort the sounds
            foreach (var uuid in favourites ? CustomFavouriteSoundOrder[categoryUuid] : CustomSoundOrder[categoryUuid])
            {
                var i = soundsCopy.FindIndex(s => s.Uuid == uuid);
                if (i == -1) continue;

                sortedSounds.Add(soundsCopy.ElementAt(i));
                soundsCopy.RemoveAt(i);
            }

            // Add the remaining sounds in the list to the end of the sorted sounds
            foreach (var sound in soundsCopy)
                sortedSounds.Add(sound);

            return sortedSounds;
        }

        /**
         * Loads the sound order for the given category and saves it for later access
         */
        private static async Task LoadCustomSoundOrderForCategoryAsync(Guid categoryUuid)
        {
            if (categoryUuid.Equals(Guid.Empty))
                await LoadCustomSoundOrderForCategoryAsync(new Category { Uuid = Guid.Empty });
            else
            {
                var category = await GetCategoryAsync(categoryUuid);
                if(category != null) await LoadCustomSoundOrderForCategoryAsync(category);
            }
        }

        private static async Task LoadCustomSoundOrderForCategoryAsync(Category category)
        {
            if (LoadedSoundOrders.Contains(category.Uuid)) return;

            if (category.Uuid.Equals(Guid.Empty))
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

            LoadedSoundOrders.Add(category.Uuid);
        }

        /**
         * Sorts the sounds list by the given configuration using the sound order table objects
         */
        public static async Task<List<Sound>> SortSoundsListByCustomOrderAsync(List<Sound> sounds, Guid categoryUuid, bool favourite)
        {
            // Get the order table objects
            var tableObjects = await DatabaseOperations.GetAllOrdersAsync();

            // Get the order table objects with the type Sound (1), the right category uuid and the same favourite
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
                && itemViewHolder.SearchAutoSuggestBoxVisible
            )
            {
                // Reset search
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
            if(
                !itemViewHolder.SelectedCategory.Equals(Guid.Empty)
                || !string.IsNullOrEmpty(itemViewHolder.SearchQuery)
            )
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
                    (Window.Current.Bounds.Width >= hideSearchBoxMaxWidth && itemViewHolder.SearchAutoSuggestBoxVisible)
                    || (Window.Current.Bounds.Width < hideSearchBoxMaxWidth && itemViewHolder.SearchButtonVisible)
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
            else if (GetRequestedTheme() == ElementTheme.Dark)
            {   // If the acrylic background is disabled and the theme is dark
                // Remove the transparency effect
                (Application.Current.Resources["NavigationViewExpandedPaneBackground"] as AcrylicBrush).BackgroundSource = AcrylicBackgroundSource.Backdrop;
                itemViewHolder.PlayingSoundsBarAcrylicBackgroundBrush.BackgroundSource = AcrylicBackgroundSource.Backdrop;

                // Set the background color
                (Application.Current.Resources["NavigationViewExpandedPaneBackground"] as AcrylicBrush).TintColor = sideBarDarkBackgroundColor;
                itemViewHolder.PlayingSoundsBarAcrylicBackgroundBrush.TintColor = playingSoundsBarDarkBackgroundColor;
            }
            else
            {   // If the acrylic background is disabled and the theme is Light
                // Remove the transparency effect
                (Application.Current.Resources["NavigationViewExpandedPaneBackground"] as AcrylicBrush).BackgroundSource = AcrylicBackgroundSource.Backdrop;
                itemViewHolder.PlayingSoundsBarAcrylicBackgroundBrush.BackgroundSource = AcrylicBackgroundSource.Backdrop;

                // Set the background color
                (Application.Current.Resources["NavigationViewExpandedPaneBackground"] as AcrylicBrush).TintColor = sideBarLightBackgroundColor;
                itemViewHolder.PlayingSoundsBarAcrylicBackgroundBrush.TintColor = playingSoundsBarLightBackgroundColor;
            }

            // Set the color for the NavigationViewHeader background
            (Application.Current.Resources["NavigationViewHeaderBackgroundBrush"] as AcrylicBrush).TintColor = appThemeColor;
            (Application.Current.Resources["NavigationViewHeaderBackgroundBrush"] as AcrylicBrush).FallbackColor = appThemeColor;
            (Application.Current.Resources["NavigationViewHeaderBackgroundBrush"] as AcrylicBrush).TintOpacity = itemViewHolder.ShowAcrylicBackground ? 0.75 : 1;
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
        public static async Task CalculateSoundboardSizeAsync()
        {
            if (isCalculatingSoundboardSize) return;
            else if (itemViewHolder.AppState != AppState.Normal)
            {
                // Call this method again after some time
                await Task.Delay(3000);
                await CalculateSoundboardSizeAsync();
                return;
            }

            isCalculatingSoundboardSize = true;

            // Get all sounds
            List<Sound> sounds = new List<Sound>();
            foreach (var sound in itemViewHolder.AllSounds)
                sounds.Add(sound);

            // Calculate the size of each sound
            itemViewHolder.SoundboardSize = 0;

            foreach(var sound in sounds)
            {
                var audioFile = sound.AudioFile;
                if (audioFile != null)
                    itemViewHolder.SoundboardSize += await GetFileSizeAsync(audioFile);

                var imageFile = sound.ImageFile;
                if (imageFile != null)
                    itemViewHolder.SoundboardSize += await GetFileSizeAsync(imageFile);
            }

            isCalculatingSoundboardSize = false;
        }

        public static string GetFormattedSize(ulong size, bool rounded = false) 
        {
            ulong gbMin = 1000000000;
            ulong mbMin = 1000000;
            ulong kbMin = 1000;

            if (size >= gbMin)
            {
                // GB
                double gb = size / (double)gbMin;
                return string.Format("{0} {1}", gb.ToString(rounded ? "N0" : "N2"), loader.GetString("Sizes-GB"));
            }
            else if (size >= mbMin)
            {
                // MB
                double mb = size / (double)mbMin;
                return string.Format("{0} {1}", mb.ToString(rounded ? "N0" : "N1"), loader.GetString("Sizes-MB"));
            }
            else
            {
                // KB
                double kb = size / (double)kbMin;
                return string.Format("{0} {1}", kb.ToString("N0"), loader.GetString("Sizes-KB"));
            }
        }

        public static Color GetApplicationThemeColor()
        {
            return itemViewHolder.CurrentTheme == AppTheme.Dark ? ((Color)Application.Current.Resources["DarkThemeBackgroundColor"]) : ((Color)Application.Current.Resources["LightThemeBackgroundColor"]);
        }

        public static ElementTheme GetRequestedTheme()
        {
            return itemViewHolder.CurrentTheme == AppTheme.Dark ? ElementTheme.Dark : ElementTheme.Light;
        }

        public static async Task LogException(Exception e)
        {
            string name = e.GetType().FullName;
            string message = e.Message;
            string stackTrace = e.StackTrace;

            string appVersion = $"{SystemInformation.ApplicationVersion.Major}.{SystemInformation.ApplicationVersion.Minor}.{SystemInformation.ApplicationVersion.Build}";
            string osVersion = SystemInformation.OperatingSystemVersion.ToString();
            string deviceFamily = SystemInformation.DeviceFamily;
            string locale = SystemInformation.Culture.Name;

            await AppsController.CreateExceptionLog(AppId, ApiKey, name, message, stackTrace, appVersion, osVersion, deviceFamily, locale);
        }
        #endregion

        #region Helper Methods
        public static async Task<ulong> GetFileSizeAsync(StorageFile file)
        {
            return (await file.GetBasicPropertiesAsync()).Size;
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

            // Deserialize Json
            var serializer = new DataContractJsonSerializer(typeof(NewData));
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(data));
            var dataReader = (NewData)serializer.ReadObject(ms);

            return dataReader;
        }

        public static async Task<List<TableObjectData>> GetTableObjectDataFromFile(StorageFile dataFile)
        {
            string data = await FileIO.ReadTextAsync(dataFile);

            // Deserialize Json
            var serializer = new DataContractJsonSerializer(typeof(List<TableObjectData>));
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(data));
            return (List<TableObjectData>)serializer.ReadObject(ms);
        }
    }
    #endregion
}
