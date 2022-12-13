using davClassLibrary;
using davClassLibrary.Models;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.Toolkit.Uwp.Helpers;
using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UniversalSoundboard.Common;
using UniversalSoundboard.Components;
using UniversalSoundboard.Dialogs;
using UniversalSoundboard.Models;
using UniversalSoundboard.Pages;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.Resources;
using Windows.Devices.Enumeration;
using Windows.Foundation.Metadata;
using Windows.Media;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Notifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using WinUI = Microsoft.UI.Xaml.Controls;

namespace UniversalSoundboard.DataAccess
{
    public class FileManager
    {
        #region Variables
        #region Design constants
        public const int mobileMaxWidth = 775;
        public const int topButtonsCollapsedMaxWidth = 1600;
        public const int sideBarCollapsedMaxWidth = 1100;
        public const int hideSearchBoxMaxWidth = 800;
        #endregion

        #region Colors for the background of PlayingSoundsBar and SideBar
        private static readonly double sideBarAcrylicBackgroundTintOpacity = 0.6;
        private static readonly double playingSoundsBarAcrylicBackgroundTintOpacity = 0.85;
        private static readonly double secondaryWindowAcrylicBackgroundTintOpacity = 0.85;
        private static Color sideBarLightBackgroundColor = Color.FromArgb(255, 245, 245, 245);            // #f5f5f5
        private static Color sideBarDarkBackgroundColor = Color.FromArgb(255, 29, 34, 49);                // #1d2231
        private static Color playingSoundsBarLightBackgroundColor = Color.FromArgb(255, 253, 253, 253);   // #fdfdfd
        private static Color playingSoundsBarDarkBackgroundColor = Color.FromArgb(255, 15, 20, 35);       // #0f1423
        private static Color secondaryWindowLightBackgroundColor = Color.FromArgb(255, 255, 255, 255);    // #ffffff
        private static Color secondaryWindowDarkBackgroundColor = Color.FromArgb(255, 13, 18, 33);        // #0d1221
        #endregion

        #region dav Keys
        public static string ApiKey => Environment == davClassLibrary.Environment.Production ? Env.DavApiKeyProd : Env.DavApiKeyDev;

        private const string WebsiteBaseUrlProduction = "https://dav-login-7ymir.ondigitalocean.app";
        private const string WebsiteBaseUrlDevelopment = "https://2361f4e2f9f3.ngrok.io";
        public static string WebsiteBaseUrl => Environment == davClassLibrary.Environment.Production ? WebsiteBaseUrlProduction : WebsiteBaseUrlDevelopment;

        private const int AppIdProduction = 1;                 // Dev: 2; Prod: 1
        private const int AppIdDevelopment = 2;
        public static int AppId => Environment == davClassLibrary.Environment.Production ? AppIdProduction : AppIdDevelopment;

        private const int SoundFileTableIdProduction = 6;      // Dev: 2; Prod: 6
        private const int SoundFileTableIdDevelopment = 2;
        public static int SoundFileTableId => Environment == davClassLibrary.Environment.Production ? SoundFileTableIdProduction : SoundFileTableIdDevelopment;

        private const int ImageFileTableIdProduction = 7;      // Dev: 3; Prod: 7
        private const int ImageFileTableIdDevelopment = 3;
        public static int ImageFileTableId => Environment == davClassLibrary.Environment.Production ? ImageFileTableIdProduction : ImageFileTableIdDevelopment;

        private const int CategoryTableIdProduction = 8;        // Dev: 4; Prod: 8
        private const int CategoryTableIdDevelopment = 4;
        public static int CategoryTableId => Environment == davClassLibrary.Environment.Production ? CategoryTableIdProduction : CategoryTableIdDevelopment;

        private const int SoundTableIdProduction = 5;           // Dev: 1; Prod: 5
        private const int SoundTableIdDevelopment = 1;
        public static int SoundTableId => Environment == davClassLibrary.Environment.Production ? SoundTableIdProduction : SoundTableIdDevelopment;

        private const int PlayingSoundTableIdProduction = 9;    // Dev: 5; Prod: 9
        private const int PlayingSoundTableIdDevelopment = 5;
        public static int PlayingSoundTableId => Environment == davClassLibrary.Environment.Production ? PlayingSoundTableIdProduction : PlayingSoundTableIdDevelopment;

        private const int OrderTableIdProduction = 12;          // Dev: 6; Prod: 12
        private const int OrderTableIdDevelopment = 6;
        public static int OrderTableId => Environment == davClassLibrary.Environment.Production ? OrderTableIdProduction : OrderTableIdDevelopment;
        #endregion

        #region Table property names
        public const string SoundTableNamePropertyName = "name";
        public const string SoundTableFavouritePropertyName = "favourite";
        public const string SoundTableSoundUuidPropertyName = "sound_uuid";
        public const string SoundTableImageUuidPropertyName = "image_uuid";
        public const string SoundTableCategoryUuidPropertyName = "category_uuid";
        public const string SoundTableDefaultVolumePropertyName = "default_volume";
        public const string SoundTableDefaultMutedPropertyName = "default_muted";
        public const string SoundTableDefaultPlaybackSpeedPropertyName = "default_playback_speed";
        public const string SoundTableDefaultRepetitionsPropertyName = "default_repetitions";
        public const string SoundTableDefaultOutputDevicePropertyName = "default_output_device";
        public const string SoundTableHotkeysPropertyName = "hotkeys";

        public const string CategoryTableParentPropertyName = "parent";
        public const string CategoryTableNamePropertyName = "name";
        public const string CategoryTableIconPropertyName = "icon";

        public const string PlayingSoundTableSoundIdsPropertyName = "sound_ids";
        public const string PlayingSoundTableCurrentPropertyName = "current";
        public const string PlayingSoundTableRepetitionsPropertyName = "repetitions";
        public const string PlayingSoundTableRandomlyPropertyName = "randomly";
        public const string PlayingSoundTableVolumePropertyName = "volume2";
        public const string PlayingSoundTableMutedPropertyName = "muted";
        public const string PlayingSoundTableOutputDevicePropertyName = "output_device";
        public const string PlayingSoundTablePlaybackSpeedPropertyName = "playback_speed";

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
        public const string ExportDataFileName = "data.json";
        public const string TileFolderName = "tile";

        public const string FluentIconsFontFamily = "/Assets/Fonts/SegoeFluentIcons.ttf#Segoe Fluent Icons";

        public static List<string> allowedFileTypes = new List<string>
        {
            ".mp3",
            ".m4a",
            ".wav",
            ".ogg",
            ".wma",
            ".flac"
        };

        public static List<string> allowedAudioMimeTypes = new List<string>
        {
            "audio/mpeg",   // .mp3
            "audio/mp4",    // .m4a
            "audio/wav",    // .wav
            "audio/ogg"     // .ogg
        };
        #endregion

        #region Local variables
        public static ItemViewHolder itemViewHolder;
        public static davClassLibrary.Environment Environment = davClassLibrary.Environment.Production;

        public static readonly ResourceLoader loader = new ResourceLoader();
        public static HttpClient httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(60) };
        public static YouTubeService youtubeService = new YouTubeService(new BaseClientService.Initializer { ApiKey = Env.YoutubeApiKey });

        public static readonly DeviceWatcherHelper deviceWatcherHelper = new DeviceWatcherHelper(DeviceClass.AudioRender);

        internal static bool syncFinished = false;
        private static bool isCalculatingSoundboardSize = false;
        private static bool isShowAllSoundsOrCategoryRunning = false;
        private static Guid nextCategoryToShow = Guid.Empty;
        private static SystemMediaTransportControls systemMediaTransportControls;
        private static StorageFolder exportDestinationFolder;

        // Save the custom order of the sounds in all categories to load them faster
        private static readonly Dictionary<Guid, List<Guid>> CustomSoundOrder = new Dictionary<Guid, List<Guid>>();
        private static readonly Dictionary<Guid, List<Guid>> CustomFavouriteSoundOrder = new Dictionary<Guid, List<Guid>>();
        private static readonly List<Guid> LoadedSoundOrders = new List<Guid>();

        public static List<InAppNotificationItem> InAppNotificationItems = new List<InAppNotificationItem>();
        #endregion
        #endregion

        #region Filesystem methods
        public static string GetDavDataPath()
        {
            string path = Path.Combine(ApplicationData.Current.LocalFolder.Path, "dav");
            Directory.CreateDirectory(path);
            return path;
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
            // Show InAppNotification
            itemViewHolder.TriggerShowInAppNotificationEvent(
                null,
                new ShowInAppNotificationEventArgs(
                    InAppNotificationType.Export,
                    loader.GetString("InAppNotification-SoundboardExport"),
                    0,
                    true
                )
            );

            itemViewHolder.Exporting = true;
            itemViewHolder.ExportAndImportButtonsEnabled = false;

            StorageFolder localCacheFolder = ApplicationData.Current.LocalCacheFolder;
            StorageFolder exportFolder = await localCacheFolder.CreateFolderAsync(ExportFolderName, CreationCollisionOption.GenerateUniqueName);

            var progress = new Progress<int>((int value) =>
            {
                itemViewHolder.ExportMessage = value + " %";
                SetInAppNotificationProgress(InAppNotificationType.Export, false, value);
            });

            await DatabaseOperations.ExportDataAsync(exportFolder, progress);

            itemViewHolder.ExportMessage = loader.GetString("ExportMessage-3");
            SetInAppNotificationProgress(InAppNotificationType.Export);

            // Create the zip file
            StorageFile zipFile = await Task.Run(async () =>
            {
                StorageFile file = await localCacheFolder.CreateFileAsync(ExportZipFileName, CreationCollisionOption.GenerateUniqueName);
                await file.DeleteAsync();
                ZipFile.CreateFromDirectory(exportFolder.Path, file.Path);
                return file;
            });

            itemViewHolder.ExportMessage = loader.GetString("ExportMessage-4");

            // Copy the file into the destination folder
            await zipFile.MoveAsync(destinationFolder, string.Format("UniversalSoundboard {0}.zip", DateTime.Today.ToString("dd.MM.yyyy")), NameCollisionOption.GenerateUniqueName);

            itemViewHolder.ExportMessage = loader.GetString("ExportImportMessage-TidyUp");
            itemViewHolder.Exporting = false;

            // Delete the files in the cache
            await exportFolder.DeleteAsync();

            itemViewHolder.ExportMessage = "";
            itemViewHolder.ExportAndImportButtonsEnabled = true;

            exportDestinationFolder = destinationFolder;

            ShowInAppNotificationEventArgs args = new ShowInAppNotificationEventArgs(
                InAppNotificationType.Export,
                loader.GetString("InAppNotification-SoundboardExportSuccessful"),
                8000,
                false,
                true,
                loader.GetString("Actions-OpenFolder")
            );
            args.PrimaryButtonClick += ExportSounds_InAppNotification_PrimaryButtonClick;

            itemViewHolder.TriggerShowInAppNotificationEvent(null, args);
        }

        public static async Task ImportDataAsync(StorageFile zipFile, bool startMessage)
        {
            SetImportMessage(loader.GetString("ImportMessage-1"), startMessage);

            if (startMessage)
                itemViewHolder.LoadingScreenVisible = true;
            else
            {
                // Show InAppNotification
                itemViewHolder.TriggerShowInAppNotificationEvent(
                    null,
                    new ShowInAppNotificationEventArgs(
                        InAppNotificationType.Import,
                        loader.GetString("InAppNotification-SoundboardImport"),
                        0,
                        true
                    )
                );
            }

            // Extract the file into the local cache folder
            itemViewHolder.Importing = true;
            itemViewHolder.ExportAndImportButtonsEnabled = false;

            StorageFolder localCacheFolder = ApplicationData.Current.LocalCacheFolder;
            StorageFolder importFolder = await localCacheFolder.CreateFolderAsync(ImportFolderName, CreationCollisionOption.GenerateUniqueName);
            StorageFile newZipFile = await zipFile.CopyAsync(localCacheFolder, ImportZipFileName, NameCollisionOption.ReplaceExisting);

            await Task.Run(() =>
            {
                // Extract the zip file
                ZipFile.ExtractToDirectory(newZipFile.Path, importFolder.Path);
            });

            DataModel dataModel = await GetDataModelAsync(importFolder);
            Progress<int> progress = new Progress<int>((int value) =>
            {
                SetImportMessage(string.Format("{0} %", value), startMessage);
                SetInAppNotificationProgress(InAppNotificationType.Import, false, value);
            });

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
            SetInAppNotificationProgress(InAppNotificationType.Import);
            itemViewHolder.Importing = false;

            // Delete the files in the cache
            await newZipFile.DeleteAsync();
            await importFolder.DeleteAsync();

            SetImportMessage("", startMessage);
            itemViewHolder.AllSoundsChanged = true;
            itemViewHolder.ExportAndImportButtonsEnabled = true;

            if (startMessage)
                itemViewHolder.LoadingScreenVisible = false;
            else
            {
                ShowInAppNotificationEventArgs args = new ShowInAppNotificationEventArgs(
                    InAppNotificationType.Import,
                    loader.GetString("InAppNotification-SoundboardImportSuccessful"),
                    8000,
                    false,
                    true
                );

                itemViewHolder.TriggerShowInAppNotificationEvent(null, args);
            }

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
                        if (soundUuid.Equals(Guid.Empty)) continue;

                        if (imagesFolder != null)
                        {
                            StorageFile imageFile = await imagesFolder.TryGetItemAsync(soundData.Uuid + "." + soundData.ImageExt) as StorageFile;
                            if (imageFile != null)
                            {
                                // Set the image of the sound
                                Guid imageUuid = Guid.NewGuid();
                                await DatabaseOperations.CreateImageFileAsync(imageUuid, imageFile);
                                await SetImageUuidOfSoundAsync(soundUuid, imageUuid);
                                await imageFile.DeleteAsync();
                            }
                        }

                        if (soundData.Favourite)
                            await SetFavouriteOfSoundAsync(soundUuid, soundData.Favourite);

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
                        if (soundUuid.Equals(Guid.Empty)) continue;

                        if (imagesFolder != null && !string.IsNullOrEmpty(sound.image_ext))
                        {
                            StorageFile imageFile = await imagesFolder.TryGetItemAsync(sound.uuid + "." + sound.image_ext) as StorageFile;
                            if (imageFile != null)
                            {
                                // Add image
                                Guid imageUuid = Guid.NewGuid();
                                await DatabaseOperations.CreateImageFileAsync(imageUuid, imageFile);
                                await SetImageUuidOfSoundAsync(soundUuid, imageUuid);
                                await imageFile.DeleteAsync();
                            }
                        }

                        if (sound.favourite)
                            await SetFavouriteOfSoundAsync(soundUuid, sound.favourite);

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
                    if (soundUuid.Equals(Guid.Empty)) continue;

                    // Get the image file of the sound
                    foreach (StorageFile imageFile in await imagesFolder.GetFilesAsync())
                    {
                        if (name == imageFile.DisplayName)
                        {
                            Guid imageUuid = Guid.NewGuid();
                            await DatabaseOperations.CreateImageFileAsync(imageUuid, imageFile);
                            await SetImageUuidOfSoundAsync(soundUuid, imageUuid);

                            // Delete the image
                            await imageFile.DeleteAsync();
                            break;
                        }
                    }

                    if (favourite)
                        await SetFavouriteOfSoundAsync(soundUuid, favourite);

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
            // Show InAppNotification
            itemViewHolder.TriggerShowInAppNotificationEvent(
                null,
                new ShowInAppNotificationEventArgs(
                    InAppNotificationType.SoundsExport,
                    loader.GetString("ExportSoundsMessage"),
                    0,
                    true
                )
            );

            if (saveAsZip)
            {
                StorageFolder localCacheFolder = ApplicationData.Current.LocalCacheFolder;
                StorageFolder exportFolder = await localCacheFolder.CreateFolderAsync(ExportFolderName, CreationCollisionOption.GenerateUniqueName);

                // Copy the selected files into the export folder
                foreach (var sound in sounds)
                    await CopySoundFileIntoFolderAsync(sound, exportFolder);

                // Create the zip file from the export folder
                StorageFile zipFile = await Task.Run(async () =>
                {
                    StorageFile file = await localCacheFolder.CreateFileAsync(ExportZipFileName, CreationCollisionOption.GenerateUniqueName);
                    await file.DeleteAsync();
                    ZipFile.CreateFromDirectory(exportFolder.Path, file.Path);
                    return file;
                });

                // Move the zip file into the destination folder
                await zipFile.MoveAsync(destinationFolder, string.Format("UniversalSoundboard {0}.zip", DateTime.Today.ToString("dd.MM.yyyy")), NameCollisionOption.GenerateUniqueName);

                // Delete the files in the cache
                await exportFolder.DeleteAsync();
            }
            else
            {
                // Copy the files directly into the folder
                foreach(var sound in sounds)
                    await CopySoundFileIntoFolderAsync(sound, destinationFolder);
            }

            exportDestinationFolder = destinationFolder;

            ShowInAppNotificationEventArgs args = new ShowInAppNotificationEventArgs(
                InAppNotificationType.SoundsExport,
                loader.GetString("InAppNotification-SoundsExportSuccessful"),
                8000,
                false,
                true,
                loader.GetString("Actions-OpenFolder")
            );
            args.PrimaryButtonClick += ExportSounds_InAppNotification_PrimaryButtonClick;

            itemViewHolder.TriggerShowInAppNotificationEvent(null, args);
        }

        private static async void ExportSounds_InAppNotification_PrimaryButtonClick(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchFolderAsync(exportDestinationFolder);
        }

        private static async Task CopySoundFileIntoFolderAsync(Sound sound, StorageFolder destinationFolder)
        {
            if (!File.Exists(sound.AudioFile.Path)) return;
            string ext = sound.GetAudioFileExtension();

            if (string.IsNullOrEmpty(ext))
                ext = "mp3";

            string fileName = string.Format("{0}.{1}", RemoveSpecialCharsFromString(sound.Name), ext);
            StorageFile soundFile = await destinationFolder.CreateFileAsync(fileName, CreationCollisionOption.GenerateUniqueName);
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

            Sound sound = new Sound(soundTableObject.Uuid, soundTableObject.GetPropertyValue(SoundTableNamePropertyName) ?? loader.GetString("UntitledSound"));

            // Get the audio file
            Guid? audioFileUuid = ConvertStringToGuid(soundTableObject.GetPropertyValue(SoundTableSoundUuidPropertyName));

            if (audioFileUuid.HasValue && !audioFileUuid.Equals(Guid.Empty))
            {
                var audioFileTableObject = await DatabaseOperations.GetTableObjectAsync(audioFileUuid.Value);

                if (audioFileTableObject != null)
                {
                    sound.AudioFileTableObject = audioFileTableObject;

                    if (!audioFileTableObject.IsFile) return null;

                    if (audioFileTableObject.FileDownloadStatus == TableObjectFileDownloadStatus.Downloaded)
                    {
                        try
                        {
                            var audioFile = await StorageFile.GetFileFromPathAsync(audioFileTableObject.File.FullName);
                            if (audioFile != null)
                                sound.AudioFile = audioFile;
                        }
                        catch { }
                    } else if (audioFileTableObject.FileDownloadStatus == TableObjectFileDownloadStatus.NoFileOrNotLoggedIn)
                        return null;
                }
                else
                {
                    if (!Dav.IsLoggedIn)
                    {
                        // Delete the sound table object
                        await soundTableObject.DeleteAsync();
                    }

                    return null;
                }
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

            // DefaultPlaybackSpeed
            int defaultPlaybackSpeed = 100;
            string defaultPlaybackSpeedString = soundTableObject.GetPropertyValue(SoundTableDefaultPlaybackSpeedPropertyName);
            if (!string.IsNullOrEmpty(defaultPlaybackSpeedString))
                int.TryParse(defaultPlaybackSpeedString, out defaultPlaybackSpeed);

            sound.DefaultPlaybackSpeed = defaultPlaybackSpeed;

            // DefaultRepetitions
            int defaultRepetitions = 0;
            string defaultRepetitionsString = soundTableObject.GetPropertyValue(SoundTableDefaultRepetitionsPropertyName);
            if (!string.IsNullOrEmpty(defaultRepetitionsString))
                int.TryParse(defaultRepetitionsString, out defaultRepetitions);

            sound.DefaultRepetitions = defaultRepetitions;

            // DefaultOutputDevice
            sound.DefaultOutputDevice = soundTableObject.GetPropertyValue(SoundTableDefaultOutputDevicePropertyName);

            // Hotkeys
            string hotkeysString = soundTableObject.GetPropertyValue(SoundTableHotkeysPropertyName);
            if (!string.IsNullOrEmpty(hotkeysString))
            {
                foreach(string hotkeyCombinationString in hotkeysString.Split(','))
                {
                    Hotkey hotkey = new Hotkey();
                    string[] hotkeyValues = hotkeyCombinationString.Split(':');

                    if (int.TryParse(hotkeyValues[0], out int modifiers) && modifiers < 15)
                        hotkey.Modifiers = (Modifiers)modifiers;

                    if (int.TryParse(hotkeyValues[1], out int key))
                        hotkey.Key = (VirtualKey)key;

                    if (!hotkey.IsEmpty())
                        sound.Hotkeys.Add(hotkey);
                }
            }

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
            var sortedSounds = await SortSoundsList(allSounds, itemViewHolder.SoundOrder, Guid.Empty, false);
            var sortedFavouriteSounds = await SortSoundsList(allFavouriteSounds, itemViewHolder.SoundOrder, Guid.Empty, true);

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
            if(soundsLoaded && SystemInformation.Instance.OperatingSystemVersion.Build < 22000)
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
            var sortedSounds = await SortSoundsList(sounds, itemViewHolder.SoundOrder, categoryUuid, false);
            var sortedFavouriteSounds = await SortSoundsList(favouriteSounds, itemViewHolder.SoundOrder, categoryUuid, true);

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
                (itemViewHolder.SelectedCategory == Guid.Empty && string.IsNullOrEmpty(itemViewHolder.SearchQuery))
                || (!string.IsNullOrEmpty(itemViewHolder.SearchQuery) && sound.Name.ToLower().Contains(itemViewHolder.SearchQuery.ToLower()))
                || parentCategories.Exists(c => c.Uuid == itemViewHolder.SelectedCategory);

            // Add to the current sounds
            if (soundBelongsToSelectedCategory && !itemViewHolder.Sounds.ToList().Exists(s => s.Uuid == sound.Uuid))
            {
                // Add the sound at the correct position
                switch (itemViewHolder.SoundOrder)
                {
                    case NewSoundOrder.NameAscending:
                        var soundsList = itemViewHolder.Sounds.ToList();
                        soundsList.Add(sound);
                        soundsList.Sort((x, y) => string.Compare(x.Name, y.Name));

                        itemViewHolder.Sounds.Insert(soundsList.IndexOf(sound), sound);
                        break;
                    case NewSoundOrder.NameDescending:
                        var soundsList2 = itemViewHolder.Sounds.ToList();
                        soundsList2.Add(sound);
                        soundsList2.Sort((x, y) => string.Compare(y.Name, x.Name));

                        itemViewHolder.Sounds.Insert(soundsList2.IndexOf(sound), sound);
                        break;
                    case NewSoundOrder.CreationDateAscending:
                        itemViewHolder.Sounds.Add(sound);
                        break;
                    case NewSoundOrder.CreationDateDescending:
                        itemViewHolder.Sounds.Insert(0, sound);
                        break;
                    default:
                        itemViewHolder.Sounds.Add(sound);
                        break;
                }
            }

            if (itemViewHolder.AllSounds.Count > 0 && (itemViewHolder.AppState == AppState.Empty || itemViewHolder.AppState == AppState.InitialSync))
                itemViewHolder.AppState = AppState.Normal;
        }

        /**
         * Replaces the sound in all sound lists
         */
        public static async Task<bool> ReloadSound(Guid uuid)
        {
            var sound = await GetSoundAsync(uuid);
            if (sound != null) return await ReloadSound(sound);
            else return false;
        }

        public static async Task<bool> ReloadSound(Sound updatedSound)
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
            if (i == -1) return false;
            else itemViewHolder.AllSounds[i] = updatedSound;

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
            else if (updatedSound.Favourite && soundBelongsToSelectedCategory)
                itemViewHolder.FavouriteSounds.Add(updatedSound);

            // Replace in PlayingSounds
            foreach(var playingSound in itemViewHolder.PlayingSounds)
            {
                i = playingSound.Sounds.ToList().FindIndex(s => s.Uuid == updatedSound.Uuid);
                if (i != -1)
                    playingSound.Sounds[i] = updatedSound;
            }

            return true;
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
            TileUpdater tileUpdater;

            try
            {
                tileUpdater = TileUpdateManager.CreateTileUpdaterForApplication();
            }
            catch(Exception e)
            {
                Crashes.TrackError(e);
                return;
            }

            if (itemViewHolder.AllSounds.Count == 0 || !itemViewHolder.LiveTile)
            {
                tileUpdater.Clear();
                return;
            }

            // Get all sounds with an image
            List<Sound> sounds = itemViewHolder.AllSounds.Where(s => s.ImageFileTableObject != null && s.ImageFile != null).ToList();
            if (sounds.Count == 0)
            {
                tileUpdater.Clear();
                return;
            }

            // Pick up to 9 random sounds with images
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
            tileUpdater.Update(notification);
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
            var categories = await GetAllCategoriesAsync();

            itemViewHolder.Categories.Clear();
            itemViewHolder.Categories.Add(new Category(Guid.Empty, loader.GetString("AllSounds"), "\uE10F"));

            foreach (Category cat in categories)
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
                // Delete each PlayingSoundif the user doesn't want to save PlayingSounds
                if (!itemViewHolder.SavePlayingSounds)
                {
                    await DeletePlayingSoundAsync(playingSound.Uuid);
                }
                else if (playingSound.AudioPlayer != null)
                {
                    var currentPlayingSoundList = itemViewHolder.PlayingSounds.Where(p => p.Uuid == playingSound.Uuid);

                    if (currentPlayingSoundList.Count() > 0)
                    {
                        var currentPlayingSound = currentPlayingSoundList.First();
                        int index = itemViewHolder.PlayingSounds.IndexOf(currentPlayingSound);

                        // Update the current playing sound if it is currently not playing
                        if (!currentPlayingSound.AudioPlayer.IsPlaying)
                        {
                            // Check if the playing sound changed
                            bool soundWasUpdated = (
                                currentPlayingSound.Randomly != playingSound.Randomly
                                || currentPlayingSound.Repetitions != playingSound.Repetitions
                                || currentPlayingSound.Sounds.Count != playingSound.Sounds.Count
                            );

                            if (currentPlayingSound.AudioPlayer != null && playingSound.AudioPlayer != null && !soundWasUpdated)
                                soundWasUpdated = currentPlayingSound.AudioPlayer.Volume != playingSound.AudioPlayer.Volume;

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

            itemViewHolder.TriggerPlayingSoundsLoadedEvent(EventArgs.Empty);
        }

        public static async Task ReloadPlayingSoundAsync(Guid uuid)
        {
            int i = itemViewHolder.PlayingSoundItems.FindIndex(item => item.Uuid.Equals(uuid));
            if (i == -1)
            {
                // Add the PlayingSound to the list
                var playingSound = await GetPlayingSoundAsync(uuid);
                if (playingSound == null) return;

                // Check if the PlayingSound is already in the list
                i = itemViewHolder.PlayingSounds.ToList().FindIndex(p => p.Uuid.Equals(uuid));
                if (i != -1) return;

                itemViewHolder.PlayingSounds.Add(playingSound);
                return;
            }

            // Replace the PlayingSound in the PlayingSoundItem
            await itemViewHolder.PlayingSoundItems.ElementAt(i).ReloadPlayingSound();
        }

        public static void RemovePlayingSound(Guid uuid)
        {
            PlayingSound playingSound = itemViewHolder.PlayingSounds.ToList().Find(ps => ps.Uuid == uuid);
            if (playingSound == null) return;

            // Check if the PlayingSound is currently playing
            if (
                playingSound.AudioPlayer != null
                && playingSound.AudioPlayer.IsPlaying
            ) return;

            // Remove the playing sound
            itemViewHolder.PlayingSounds.Remove(playingSound);
        }
        
        public static (AudioPlayer, List<Sound>) CreateAudioPlayer(List<Sound> sounds, int current)
        {
            if (sounds.Count == 0) return (null, null);

            if (current >= sounds.Count)
                current = sounds.Count - 1;
            else if (current < 0)
                current = 0;

            var audioPlayer = new AudioPlayer();
            List<Sound> newSounds = new List<Sound>();

            foreach (Sound sound in sounds)
                if (sound.GetAudioFileDownloadStatus() != TableObjectFileDownloadStatus.NoFileOrNotLoggedIn)
                    newSounds.Add(sound);

            if (current < newSounds.Count && newSounds[current].AudioFile != null)
                audioPlayer.AudioFile = newSounds[current].AudioFile;

            // Set the volume
            double appVolume = ((double)itemViewHolder.Volume) / 100;

            if (newSounds.Count == 1)
            {
                double defaultSoundVolume = ((double)newSounds[0].DefaultVolume) / 100;
                audioPlayer.Volume = appVolume * defaultSoundVolume;
                audioPlayer.IsMuted = newSounds[0].DefaultMuted || itemViewHolder.Muted;
            }
            else
            {
                audioPlayer.Volume = appVolume;
                audioPlayer.IsMuted = itemViewHolder.Muted;
            }

            return (audioPlayer, newSounds);
        }

        public static async Task<AudioPlayer> CreateAudioPlayerForLocalSound(Sound sound)
        {
            if (sound == null) return null;

            AudioPlayer player = new AudioPlayer(sound.AudioFile);

            try
            {
                await player.Init();
            }
            catch (AudioIOException e)
            {
                Crashes.TrackError(e);
            }

            player.Volume = ((double)itemViewHolder.Volume) / 100;
            player.IsMuted = itemViewHolder.Muted;

            return player;
        }

        /**
         * Update the SMTC to show the appropriate infos and state of the currently active PlayingSound
         */
        public static void UpdateSystemMediaTransportControls(bool? playing = null)
        {
            if (systemMediaTransportControls == null)
            {
                systemMediaTransportControls = SystemMediaTransportControls.GetForCurrentView();
                systemMediaTransportControls.ButtonPressed += SystemMediaTransportControls_ButtonPressed;
            }

            if (itemViewHolder.PlayingSounds.Count == 0)
            {
                systemMediaTransportControls.IsEnabled = false;
                return;
            }

            // Get the last used or currently active PlayingSound
            PlayingSound lastActivePlayingSound = itemViewHolder.PlayingSounds.ToList().Find(ps => ps.Uuid.Equals(itemViewHolder.ActivePlayingSound));

            if (lastActivePlayingSound == null)
                lastActivePlayingSound = itemViewHolder.PlayingSounds.Last();

            if (lastActivePlayingSound.Current >= lastActivePlayingSound.Sounds.Count)
            {
                systemMediaTransportControls.IsEnabled = false;
                return;
            }

            Sound currentSound = lastActivePlayingSound.Sounds[lastActivePlayingSound.Current];

            // Set the infos of the SMTC
            systemMediaTransportControls.IsEnabled = true;
            systemMediaTransportControls.IsPlayEnabled = true;
            systemMediaTransportControls.IsPauseEnabled = true;
            systemMediaTransportControls.IsPreviousEnabled = lastActivePlayingSound.Current != 0 && lastActivePlayingSound.Sounds.Count > 1;
            systemMediaTransportControls.IsNextEnabled = lastActivePlayingSound.Current != lastActivePlayingSound.Sounds.Count - 1;

            SystemMediaTransportControlsDisplayUpdater updater = systemMediaTransportControls.DisplayUpdater;

            updater.ClearAll();
            updater.Type = MediaPlaybackType.Music;
            updater.MusicProperties.Title = currentSound.Name;

            if (currentSound.Categories.Count > 0)
                updater.MusicProperties.Artist = currentSound.Categories.First().Name;

            if (currentSound.ImageFileTableObject != null && currentSound.ImageFile != null)
                updater.Thumbnail = RandomAccessStreamReference.CreateFromFile(currentSound.ImageFile);

            updater.Update();

            if (playing.HasValue)
                systemMediaTransportControls.PlaybackStatus = playing.Value ? MediaPlaybackStatus.Playing : MediaPlaybackStatus.Paused;
            else
                systemMediaTransportControls.PlaybackStatus = lastActivePlayingSound.AudioPlayer?.IsPlaying == true ? MediaPlaybackStatus.Playing : MediaPlaybackStatus.Paused;
        }

        private static async void SystemMediaTransportControls_ButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            if (itemViewHolder.PlayingSounds.Count == 0) return;

            // Get the last used or currently active PlayingSound
            PlayingSound lastActivePlayingSound = itemViewHolder.PlayingSounds.ToList().Find(ps => ps.Uuid.Equals(itemViewHolder.ActivePlayingSound));

            if (lastActivePlayingSound == null)
                lastActivePlayingSound = itemViewHolder.PlayingSounds.Last();

            // Get the PlayingSoundItem of the PlayingSound
            int i = itemViewHolder.PlayingSoundItems.FindIndex(item => item.Uuid.Equals(lastActivePlayingSound.Uuid));
            if (i == -1) return;

            PlayingSoundItem playingSoundItem = itemViewHolder.PlayingSoundItems.ElementAt(i);

            await MainPage.dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                switch (args.Button)
                {
                    case SystemMediaTransportControlsButton.Previous:
                        await playingSoundItem.MoveToPrevious();
                        break;
                    case SystemMediaTransportControlsButton.Next:
                        await playingSoundItem.MoveToNext();
                        break;
                    default:
                        await playingSoundItem.SetPlayPause(args.Button == SystemMediaTransportControlsButton.Play);
                        break;
                }
            });
        }
        #endregion

        #region Sound CRUD methods
        public static async Task<Guid> CreateSoundAsync(Guid? uuid, string name, Guid? categoryUuid, StorageFile audioFile, StorageFile imageFile = null)
        {
            List<Guid> categoryUuidList = new List<Guid>();
            if (categoryUuid.HasValue) categoryUuidList.Add(categoryUuid.Value);

            return await CreateSoundAsync(uuid, name, categoryUuidList, audioFile, imageFile);
        }

        public static async Task<Guid> CreateSoundAsync(Guid? uuid, string name, List<Guid> categoryUuids, StorageFile audioFile, StorageFile imageFile = null)
        {
            // Copy the audio and image files into the local cache
            StorageFile newAudioFile = audioFile;
            StorageFile newImageFile = imageFile;
            StorageFolder audioFileParent = await audioFile.GetParentAsync();
            StorageFolder imageFileParent = imageFile == null ? null : await imageFile.GetParentAsync();

            try
            {
                if (audioFileParent == null || audioFileParent.Path != ApplicationData.Current.LocalCacheFolder.Path)
                    newAudioFile = await audioFile.CopyAsync(ApplicationData.Current.LocalCacheFolder, audioFile.Name, NameCollisionOption.ReplaceExisting);

                if (imageFile != null && (imageFileParent == null || imageFileParent.Path != ApplicationData.Current.LocalCacheFolder.Path))
                    await imageFile.CopyAsync(ApplicationData.Current.LocalCacheFolder, imageFile.Name, NameCollisionOption.ReplaceExisting);
            }
            catch(Exception e)
            {
                var properties = new Dictionary<string, string>
                {
                    { "FilePath", audioFile.Path }
                };
                Crashes.TrackError(e, properties);

                return Guid.Empty;
            }

            var soundFileTableObject = await DatabaseOperations.CreateSoundFileAsync(Guid.NewGuid(), newAudioFile);
            var soundTableObject = await DatabaseOperations.CreateSoundAsync(
                uuid ?? Guid.NewGuid(),
                name,
                false,
                soundFileTableObject.Uuid,
                categoryUuids.Where(categoryUuid => categoryUuid != null && !categoryUuid.Equals(Guid.Empty)).ToList()
            );
            await newAudioFile.DeleteAsync();

            if (newImageFile != null)
            {
                Guid imageUuid = Guid.NewGuid();
                await DatabaseOperations.CreateImageFileAsync(imageUuid, newImageFile);
                await SetImageUuidOfSoundAsync(soundTableObject.Uuid, imageUuid);
                await newImageFile.DeleteAsync();
            }

            return soundTableObject.Uuid;
        }

        public static async Task RenameSoundAsync(Guid uuid, string newName)
        {
            // Get the sound table object
            var soundTableObject = await Dav.Database.GetTableObjectAsync(uuid);
            if (soundTableObject == null || soundTableObject.TableId != SoundTableId) return;

            await soundTableObject.SetPropertyValueAsync(SoundTableNamePropertyName, newName);
        }

        public static async Task SetCategoriesOfSoundAsync(Guid uuid, List<Guid> categoryUuids)
        {
            // Get the sound table object
            var soundTableObject = await Dav.Database.GetTableObjectAsync(uuid);
            if (soundTableObject == null || soundTableObject.TableId != SoundTableId) return;

            await soundTableObject.SetPropertyValueAsync(SoundTableCategoryUuidPropertyName, string.Join(",", categoryUuids));
        }

        public static async Task SetFavouriteOfSoundAsync(Guid uuid, bool favourite)
        {
            // Get the sound table object
            var soundTableObject = await Dav.Database.GetTableObjectAsync(uuid);
            if (soundTableObject == null || soundTableObject.TableId != SoundTableId) return;

            await soundTableObject.SetPropertyValueAsync(SoundTableFavouritePropertyName, favourite.ToString());
        }

        public static async Task SetDefaultVolumeOfSoundAsync(Guid uuid, int defaultVolume, bool defaultMuted)
        {
            // Get the sound table object
            var soundTableObject = await Dav.Database.GetTableObjectAsync(uuid);
            if (soundTableObject == null || soundTableObject.TableId != SoundTableId) return;

            await soundTableObject.SetPropertyValuesAsync(new Dictionary<string, string>
            {
                { SoundTableDefaultVolumePropertyName, defaultVolume.ToString() },
                { SoundTableDefaultMutedPropertyName, defaultMuted.ToString() }
            });
        }

        public static async Task SetDefaultPlaybackSpeedOfSoundAsync(Guid uuid, int defaultPlaybackSpeed)
        {
            // Get the sound table object
            var soundTableObject = await Dav.Database.GetTableObjectAsync(uuid);
            if (soundTableObject == null || soundTableObject.TableId != SoundTableId) return;

            await soundTableObject.SetPropertyValueAsync(SoundTableDefaultPlaybackSpeedPropertyName, defaultPlaybackSpeed.ToString());
        }

        public static async Task SetDefaultRepetitionsOfSoundAsync(Guid uuid, int defaultRepetitions)
        {
            // Get the sound table object
            var soundTableObject = await Dav.Database.GetTableObjectAsync(uuid);
            if (soundTableObject == null || soundTableObject.TableId != SoundTableId) return;

            await soundTableObject.SetPropertyValueAsync(SoundTableDefaultRepetitionsPropertyName, defaultRepetitions.ToString());
        }

        public static async Task SetDefaultOutputDeviceOfSoundAsync(Guid uuid, string defaultOutputDevice)
        {
            // Get the sound table object
            var soundTableObject = await Dav.Database.GetTableObjectAsync(uuid);
            if (soundTableObject == null || soundTableObject.TableId != SoundTableId) return;

            await soundTableObject.SetPropertyValueAsync(SoundTableDefaultOutputDevicePropertyName, defaultOutputDevice);
        }

        public static async Task SetHotkeysOfSoundAsync(Guid uuid, List<Hotkey> hotkeys)
        {
            // Get the sound table object
            var soundTableObject = await Dav.Database.GetTableObjectAsync(uuid);
            if (soundTableObject == null || soundTableObject.TableId != SoundTableId) return;

            List<string> hotkeyStrings = new List<string>();

            foreach (Hotkey hotkey in hotkeys)
            {
                if (hotkey.IsEmpty())
                    continue;

                hotkeyStrings.Add(hotkey.ToDataString());
            }

            await soundTableObject.SetPropertyValueAsync(SoundTableHotkeysPropertyName, string.Join(",", hotkeyStrings));
        }

        public static async Task SetImageUuidOfSoundAsync(Guid uuid, Guid imageUuid)
        {
            // Get the sound table object
            var soundTableObject = await DatabaseOperations.GetTableObjectAsync(uuid);
            if (soundTableObject == null || soundTableObject.TableId != SoundTableId) return;

            await soundTableObject.SetPropertyValueAsync(SoundTableImageUuidPropertyName, imageUuid.ToString());
        }

        public static async Task UpdateImageOfSoundAsync(Guid uuid, StorageFile file)
        {
            // Get the sound table object
            var soundTableObject = await DatabaseOperations.GetTableObjectAsync(uuid);
            if (soundTableObject == null || soundTableObject.TableId != SoundTableId) return;

            Guid? imageUuid = ConvertStringToGuid(soundTableObject.GetPropertyValue(SoundTableImageUuidPropertyName));
            StorageFile newImageFile = await file.CopyAsync(ApplicationData.Current.LocalCacheFolder, "newImage" + file.FileType, NameCollisionOption.ReplaceExisting);

            if (!imageUuid.HasValue || Equals(imageUuid, Guid.Empty))
            {
                // Create new image file
                Guid imageFileUuid = Guid.NewGuid();
                await DatabaseOperations.CreateImageFileAsync(imageFileUuid, newImageFile);
                await soundTableObject.SetPropertyValueAsync(SoundTableImageUuidPropertyName, imageFileUuid.ToString());
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
            // Get the playing sound table object
            var playingSoundTableObject = await Dav.Database.GetTableObjectAsync(uuid);
            if (playingSoundTableObject == null || playingSoundTableObject.TableId != PlayingSoundTableId) return;

            await playingSoundTableObject.SetPropertyValueAsync(PlayingSoundTableCurrentPropertyName, current.ToString());
        }

        public static async Task SetRepetitionsOfPlayingSoundAsync(Guid uuid, int repetitions)
        {
            // Get the playing sound table object
            var playingSoundTableObject = await Dav.Database.GetTableObjectAsync(uuid);
            if (playingSoundTableObject == null || playingSoundTableObject.TableId != PlayingSoundTableId) return;

            await playingSoundTableObject.SetPropertyValueAsync(PlayingSoundTableRepetitionsPropertyName, repetitions.ToString());
        }

        public static async Task SetSoundsListOfPlayingSoundAsync(Guid uuid, List<Sound> sounds)
        {
            // Get the playing sound table object
            var playingSoundTableObject = await Dav.Database.GetTableObjectAsync(uuid);
            if (playingSoundTableObject == null || playingSoundTableObject.TableId != PlayingSoundTableId) return;

            List<Guid> soundUuids = new List<Guid>();
            foreach (Sound sound in sounds)
                soundUuids.Add(sound.Uuid);

            await playingSoundTableObject.SetPropertyValueAsync(PlayingSoundTableSoundIdsPropertyName, string.Join(",", soundUuids));
        }

        public static async Task SetVolumeOfPlayingSoundAsync(Guid uuid, int volume)
        {
            // Get the playing sound table object
            var playingSoundTableObject = await Dav.Database.GetTableObjectAsync(uuid);
            if (playingSoundTableObject == null || playingSoundTableObject.TableId != PlayingSoundTableId) return;

            if (volume > 100)
                volume = 100;
            else if (volume < 0)
                volume = 0;

            await playingSoundTableObject.SetPropertyValueAsync(PlayingSoundTableVolumePropertyName, volume.ToString());
        }

        public static async Task SetMutedOfPlayingSoundAsync(Guid uuid, bool muted)
        {
            // Get the playing sound table object
            var playingSoundTableObject = await Dav.Database.GetTableObjectAsync(uuid);
            if (playingSoundTableObject == null || playingSoundTableObject.TableId != PlayingSoundTableId) return;

            await playingSoundTableObject.SetPropertyValueAsync(PlayingSoundTableMutedPropertyName, muted.ToString());
        }

        public static async Task SetOutputDeviceOfPlayingSoundAsync(Guid uuid, string outputDevice)
        {
            // Get the playing sound table object
            var playingSoundTableObject = await Dav.Database.GetTableObjectAsync(uuid);
            if (playingSoundTableObject == null || playingSoundTableObject.TableId != PlayingSoundTableId) return;

            await playingSoundTableObject.SetPropertyValueAsync(PlayingSoundTableOutputDevicePropertyName, outputDevice);
        }

        public static async Task SetPlaybackSpeedOfPlayingSoundAsync(Guid uuid, int playbackSpeed)
        {
            // Get the playing sound table object
            var playingSoundTableObject = await Dav.Database.GetTableObjectAsync(uuid);
            if (playingSoundTableObject == null || playingSoundTableObject.TableId != PlayingSoundTableId) return;

            if (playbackSpeed > 200)
                playbackSpeed = 200;
            else if (playbackSpeed < 25)
                playbackSpeed = 25;

            await playingSoundTableObject.SetPropertyValueAsync(PlayingSoundTablePlaybackSpeedPropertyName, playbackSpeed.ToString());
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
            if (!string.IsNullOrEmpty(currentString))
                int.TryParse(currentString, out current);

            int repetitions = 1;
            string repetitionsString = tableObject.GetPropertyValue(PlayingSoundTableRepetitionsPropertyName);
            if (!string.IsNullOrEmpty(repetitionsString))
                int.TryParse(repetitionsString, out repetitions);

            bool randomly = false;
            string randomlyString = tableObject.GetPropertyValue(PlayingSoundTableRandomlyPropertyName);
            if (!string.IsNullOrEmpty(randomlyString))
                bool.TryParse(randomlyString, out randomly);

            double volume = 100;
            string volumeString = tableObject.GetPropertyValue(PlayingSoundTableVolumePropertyName);
            if (!string.IsNullOrEmpty(volumeString))
                double.TryParse(volumeString, out volume);

            bool muted = false;
            string mutedString = tableObject.GetPropertyValue(PlayingSoundTableMutedPropertyName);
            if (!string.IsNullOrEmpty(mutedString))
                bool.TryParse(mutedString, out muted);

            string outputDevice = null;

            if (Dav.IsLoggedIn && Dav.User.Plan > 0)
                outputDevice = tableObject.GetPropertyValue(PlayingSoundTableOutputDevicePropertyName);

            int playbackSpeed = 100;
            string playbackSpeedString = tableObject.GetPropertyValue(PlayingSoundTablePlaybackSpeedPropertyName);
            if (!string.IsNullOrEmpty(playbackSpeedString))
                int.TryParse(playbackSpeedString, out playbackSpeed);

            // Create the audio player
            var createAudioPlayerResult = CreateAudioPlayer(sounds, current);
            AudioPlayer player = createAudioPlayerResult.Item1;
            List<Sound> newSounds = createAudioPlayerResult.Item2;

            if (player != null)
            {
                player.Volume = (double)itemViewHolder.Volume / 100 * (volume / 100);
                player.IsMuted = itemViewHolder.Muted || muted;

                return new PlayingSound(tableObject.Uuid, player, newSounds, current, repetitions, randomly, Convert.ToInt32(volume), muted, outputDevice, playbackSpeed);
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

                List<Property> sortedOrderProperties = new List<Property>();

                foreach (var property in lastOrderTableObject.Properties)
                {
                    if (!int.TryParse(property.Name, out int index)) continue;
                    sortedOrderProperties.Add(property);
                }

                sortedOrderProperties.Sort((a, b) =>
                {
                    int.TryParse(a.Name, out int aName);
                    int.TryParse(b.Name, out int bName);
                    return aName.CompareTo(bName);
                });

                foreach (var property in sortedOrderProperties)
                {
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
        private static async Task<List<Sound>> SortSoundsList(List<Sound> sounds, NewSoundOrder order, Guid categoryUuid, bool favourite)
        {
            List<Sound> sortedSounds = new List<Sound>();

            switch (order)
            {
                case NewSoundOrder.NameAscending:
                    sounds.Sort((x, y) => string.Compare(x.Name, y.Name));

                    foreach (var sound in sounds)
                        sortedSounds.Add(sound);

                    break;
                case NewSoundOrder.NameDescending:
                    sounds.Sort((x, y) => string.Compare(y.Name, x.Name));

                    foreach (var sound in sounds)
                        sortedSounds.Add(sound);

                    break;
                case NewSoundOrder.CreationDateAscending:
                    foreach (var sound in sounds)
                        sortedSounds.Add(sound);

                    break;
                case NewSoundOrder.CreationDateDescending:
                    foreach (var sound in sounds)
                        sortedSounds.Add(sound);

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
                bool removeNonExistentSounds = !Dav.IsLoggedIn || (Dav.IsLoggedIn && syncFinished);

                bool saveNewOrder = false;
                TableObject lastOrderTableObject = soundOrderTableObjects.Last();
                List<Guid> uuids = new List<Guid>();
                List<Sound> sortedSounds = new List<Sound>();

                // Add all sounds to newSounds
                List<Sound> newSounds = new List<Sound>();
                foreach (var sound in sounds)
                    newSounds.Add(sound);

                List<Property> sortedOrderProperties = new List<Property>();

                foreach (var property in lastOrderTableObject.Properties)
                {
                    if (!int.TryParse(property.Name, out int index)) continue;
                    sortedOrderProperties.Add(property);
                }

                sortedOrderProperties.Sort((a, b) =>
                {
                    int.TryParse(a.Name, out int aName);
                    int.TryParse(b.Name, out int bName);
                    return aName.CompareTo(bName);
                });

                foreach (var property in sortedOrderProperties)
                {
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

        #region InAppNotification methods
        public static InAppNotificationItem CreateInAppNotificationItem(ShowInAppNotificationEventArgs args)
        {
            InAppNotification inAppNotification = new InAppNotification
            {
                ShowDismissButton = args.Dismissable
            };

            Grid rootGrid = new Grid();
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            rootGrid.ColumnDefinitions.Add(new ColumnDefinition());
            rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            WinUI.ProgressRing progressRing = new WinUI.ProgressRing
            {
                Width = 20,
                Height = 20,
                Margin = new Thickness(0, 0, 10, 0),
                Visibility = args.ShowProgressRing ? Visibility.Visible : Visibility.Collapsed
            };

            TextBlock messageTextBlock = new TextBlock
            {
                Text = args.Message,
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.WrapWholeWords
            };
            Grid.SetColumn(messageTextBlock, 1);

            StackPanel buttonStackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(buttonStackPanel, 2);

            Button primaryButton = new Button
            {
                Content = args.PrimaryButtonText ?? "",
                Height = 32,
                Visibility = args.PrimaryButtonText != null ? Visibility.Visible : Visibility.Collapsed
            };

            Button secondaryButton = new Button
            {
                Content = args.SecondaryButtonText ?? "",
                Height = 32,
                Margin = new Thickness(10, 0, 0, 0),
                Visibility = args.SecondaryButtonText != null ? Visibility.Visible : Visibility.Collapsed
            };

            primaryButton.Click += (object s, RoutedEventArgs e) => args.TriggerPrimaryButtonClickEvent(s, e);
            secondaryButton.Click += (object s, RoutedEventArgs e) => args.TriggerSecondaryButtonClickEvent(s, e);

            buttonStackPanel.Children.Add(primaryButton);
            buttonStackPanel.Children.Add(secondaryButton);

            rootGrid.Children.Add(progressRing);
            rootGrid.Children.Add(messageTextBlock);
            rootGrid.Children.Add(buttonStackPanel);

            inAppNotification.Content = rootGrid;

            var inAppNotificationItem = new InAppNotificationItem(inAppNotification, args.Type, args.Duration, progressRing, messageTextBlock, primaryButton, secondaryButton);

            inAppNotification.Closed += (object sender, InAppNotificationClosedEventArgs e) =>
            {
                if (e.DismissKind != InAppNotificationDismissKind.Programmatic)
                    InAppNotificationItems.Remove(inAppNotificationItem);

                // Update the position of each InAppNotification
                UpdateInAppNotificationPositions();
            };

            return inAppNotificationItem;
        }

        public static void UpdateInAppNotificationPositions()
        {
            double marginBottom = 10;
            if (!itemViewHolder.OpenMultipleSounds && itemViewHolder.PlayingSounds.Count >= 1) marginBottom = 70;
            else if (MainPage.windowWidth < mobileMaxWidth && itemViewHolder.PlayingSounds.Count >= 1) marginBottom = 57;

            foreach (var ianItem in InAppNotificationItems)
            {
                ianItem.InAppNotification.Margin = new Thickness(20, 0, 20, marginBottom);
                marginBottom = marginBottom + 10 + ianItem.MessageTextBlock.ActualHeight;
            }
        }

        public static void SetInAppNotificationMessage(InAppNotificationType type, string message)
        {
            var ianItem = InAppNotificationItems.Find(item => item.InAppNotificationType == type);
            if (ianItem == null) return;

            ianItem.MessageTextBlock.Text = message;
        }

        public static void SetInAppNotificationProgress(InAppNotificationType type, bool isIndeterminate = true, int progress = 0)
        {
            var ianItem = InAppNotificationItems.Find(item => item.InAppNotificationType == type);
            if (ianItem == null) return;

            if (isIndeterminate)
                ianItem.ProgressRing.IsIndeterminate = true;
            else
            {
                ianItem.ProgressRing.IsIndeterminate = false;

                if (progress > 100) progress = 100;
                else if (progress < 0) progress = 0;

                ianItem.ProgressRing.Value = progress;
            }
        }

        public static void DismissInAppNotification(InAppNotificationType type)
        {
            var ianItem = InAppNotificationItems.Find(item => item.InAppNotificationType == type);
            if (ianItem == null) return;

            InAppNotificationItems.Remove(ianItem);
            ianItem.InAppNotification.Dismiss();
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
                itemViewHolder.SecondaryWindowAcrylicBackgroundBrush.BackgroundSource = AcrylicBackgroundSource.HostBackdrop;

                // Set the default tint opacity
                (Application.Current.Resources["NavigationViewExpandedPaneBackground"] as AcrylicBrush).TintOpacity = sideBarAcrylicBackgroundTintOpacity;
                itemViewHolder.PlayingSoundsBarAcrylicBackgroundBrush.TintOpacity = playingSoundsBarAcrylicBackgroundTintOpacity;
                itemViewHolder.SecondaryWindowAcrylicBackgroundBrush.TintOpacity = secondaryWindowAcrylicBackgroundTintOpacity;

                // Set the tint color
                (Application.Current.Resources["NavigationViewExpandedPaneBackground"] as AcrylicBrush).TintColor = appThemeColor;
                itemViewHolder.PlayingSoundsBarAcrylicBackgroundBrush.TintColor = appThemeColor;
                itemViewHolder.SecondaryWindowAcrylicBackgroundBrush.TintColor = appThemeColor;
            }
            else if (GetRequestedTheme() == ElementTheme.Dark)
            {   // If the acrylic background is disabled and the theme is dark
                // Remove the transparency effect
                (Application.Current.Resources["NavigationViewExpandedPaneBackground"] as AcrylicBrush).BackgroundSource = AcrylicBackgroundSource.Backdrop;
                itemViewHolder.PlayingSoundsBarAcrylicBackgroundBrush.BackgroundSource = AcrylicBackgroundSource.Backdrop;
                itemViewHolder.SecondaryWindowAcrylicBackgroundBrush.BackgroundSource = AcrylicBackgroundSource.Backdrop;

                // Set the background color
                (Application.Current.Resources["NavigationViewExpandedPaneBackground"] as AcrylicBrush).TintColor = sideBarDarkBackgroundColor;
                itemViewHolder.PlayingSoundsBarAcrylicBackgroundBrush.TintColor = playingSoundsBarDarkBackgroundColor;
                itemViewHolder.SecondaryWindowAcrylicBackgroundBrush.TintColor = secondaryWindowDarkBackgroundColor;
            }
            else
            {   // If the acrylic background is disabled and the theme is Light
                // Remove the transparency effect
                (Application.Current.Resources["NavigationViewExpandedPaneBackground"] as AcrylicBrush).BackgroundSource = AcrylicBackgroundSource.Backdrop;
                itemViewHolder.PlayingSoundsBarAcrylicBackgroundBrush.BackgroundSource = AcrylicBackgroundSource.Backdrop;
                itemViewHolder.SecondaryWindowAcrylicBackgroundBrush.BackgroundSource = AcrylicBackgroundSource.Backdrop;

                // Set the background color
                (Application.Current.Resources["NavigationViewExpandedPaneBackground"] as AcrylicBrush).TintColor = sideBarLightBackgroundColor;
                itemViewHolder.PlayingSoundsBarAcrylicBackgroundBrush.TintColor = playingSoundsBarLightBackgroundColor;
                itemViewHolder.SecondaryWindowAcrylicBackgroundBrush.TintColor = secondaryWindowLightBackgroundColor;
            }

            // Set the fallback color
            itemViewHolder.SecondaryWindowAcrylicBackgroundBrush.FallbackColor = GetRequestedTheme() == ElementTheme.Dark ? secondaryWindowDarkBackgroundColor : secondaryWindowLightBackgroundColor;

            // Set the color for the NavigationViewHeader background
            (Application.Current.Resources["NavigationViewHeaderBackgroundBrush"] as AcrylicBrush).TintColor = appThemeColor;
            (Application.Current.Resources["NavigationViewHeaderBackgroundBrush"] as AcrylicBrush).FallbackColor = appThemeColor;
            (Application.Current.Resources["NavigationViewHeaderBackgroundBrush"] as AcrylicBrush).TintOpacity = itemViewHolder.ShowAcrylicBackground ? 0.75 : 1;

            // Set the color for the BottomSoundsBar background
            (Application.Current.Resources["BottomSoundsBarBackgroundBrush"] as AcrylicBrush).TintColor = appThemeColor;
            (Application.Current.Resources["BottomSoundsBarBackgroundBrush"] as AcrylicBrush).FallbackColor = appThemeColor;
            (Application.Current.Resources["BottomSoundsBarBackgroundBrush"] as AcrylicBrush).TintOpacity = itemViewHolder.ShowAcrylicBackground ? 0.5 : 1;
        }

        public static void NavigateToAccountPage(string context = null)
        {
            itemViewHolder.Page = typeof(AccountPage);
            itemViewHolder.Title = loader.GetString("Account-Title");
            itemViewHolder.EditButtonVisible = false;
            itemViewHolder.PlayAllButtonVisible = false;
            itemViewHolder.BackButtonEnabled = true;

            if (context != null)
            {
                Analytics.TrackEvent("NavigateToAccountPage", new Dictionary<string, string>
                {
                    { "Context", context }
                });
            }
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

        public static Color GetApplicationThemeColor()
        {
            return itemViewHolder.CurrentTheme == AppTheme.Dark ? ((Color)Application.Current.Resources["DarkThemeBackgroundColor"]) : ((Color)Application.Current.Resources["LightThemeBackgroundColor"]);
        }

        public static ElementTheme GetRequestedTheme()
        {
            return itemViewHolder.CurrentTheme == AppTheme.Dark ? ElementTheme.Dark : ElementTheme.Light;
        }

        public static async Task StartHotkeyProcess()
        {
            if (!ApiInformation.IsApiContractPresent("Windows.ApplicationModel.FullTrustAppContract", 1, 0))
                return;

            Process process = Process.GetCurrentProcess();
            ApplicationData.Current.LocalSettings.Values["ProcessID"] = process.Id;

            // Get all hotkeys and save them in the local settings
            itemViewHolder.HotkeySoundMapping.Clear();
            List<string> hotkeyStrings = new List<string>();

            foreach (var sound in itemViewHolder.Sounds)
            {
                foreach (var hotkey in sound.Hotkeys)
                {
                    if (hotkey.IsEmpty())
                        continue;

                    hotkeyStrings.Add(hotkey.ToDataString());
                    itemViewHolder.HotkeySoundMapping.Add(sound.Uuid);
                }
            }

            ApplicationData.Current.LocalSettings.Values["Hotkeys"] = string.Join(",", hotkeyStrings);

            await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
        }

        public static async Task HandleHotkeyPressed(int id)
        {
            if (id >= itemViewHolder.HotkeySoundMapping.Count)
                return;

            Sound sound = await GetSoundAsync(itemViewHolder.HotkeySoundMapping.ElementAt(id));
            if (sound == null) return;

            if (Dav.IsLoggedIn && Dav.User.Plan > 0)
            {
                itemViewHolder.TriggerPlaySoundEvent(null, new PlaySoundEventArgs(sound));
            }
            else
            {
                // Show dialog which explains that this feature is only for Plus users
                var davPlusHotkeysDialog = new DavPlusHotkeysDialog();
                davPlusHotkeysDialog.PrimaryButtonClick += DavPlusHotkeysContentDialog_PrimaryButtonClick;
                await davPlusHotkeysDialog.ShowAsync();
            }
        }

        private static void DavPlusHotkeysContentDialog_PrimaryButtonClick(Dialog sender, ContentDialogButtonClickEventArgs args)
        {
            // Navigate to the Account page
            NavigateToAccountPage("DavPlusHotkeysDialog");
        }
        #endregion

        #region Helper Methods
        public static async Task<ulong> GetFileSizeAsync(StorageFile file)
        {
            return (await file.GetBasicPropertiesAsync()).Size;
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

        public static Modifiers GetKeyAsModifiers(VirtualKey key)
        {
            switch (key)
            {
                case VirtualKey.Control:
                case VirtualKey.LeftControl:
                case VirtualKey.RightControl:
                    return Modifiers.Control;
                case VirtualKey.Menu:
                case VirtualKey.LeftMenu:
                case VirtualKey.RightMenu:
                    return Modifiers.Alt;
                case VirtualKey.Shift:
                case VirtualKey.LeftShift:
                case VirtualKey.RightShift:
                    return Modifiers.Shift;
                case VirtualKey.LeftWindows:
                case VirtualKey.RightWindows:
                    return Modifiers.Windows;
                default:
                    return Modifiers.None;
            }
        }

        public static Hotkey KeyListToHotkey(List<VirtualKey> keys)
        {
            Hotkey hotkey = new Hotkey();

            for (int i = 0; i < keys.Count; i++)
            {
                VirtualKey currentKey = keys[i];
                Modifiers currentKeyModifier = GetKeyAsModifiers(currentKey);

                if (currentKeyModifier == Modifiers.None)
                {
                    hotkey.Key = currentKey;
                    break;
                }

                if (i == 0)
                    hotkey.Modifiers += (int)currentKeyModifier;
                else if (i == 1)
                    hotkey.Modifiers += (int)currentKeyModifier;
                else if (i == 2)
                    hotkey.Modifiers += (int)currentKeyModifier;
                else if (i > 2)
                    break;
            }

            return hotkey;
        }

        public static string VirtualKeyModifiersToString(VirtualKeyModifiers modifiers)
        {
            switch (modifiers)
            {
                case VirtualKeyModifiers.Control:
                    return loader.GetString("Keys-Control");
                case VirtualKeyModifiers.Menu:
                    return "Alt";
                case VirtualKeyModifiers.Shift:
                    return "Shift";
                case VirtualKeyModifiers.Windows:
                    return "Windows";
                default:
                    return "";
            }
        }

        public static string VirtualKeyToString(VirtualKey key)
        {
            switch (key)
            {
                case VirtualKey.Number0:
                    return "0";
                case VirtualKey.Number1:
                    return "1";
                case VirtualKey.Number2:
                    return "2";
                case VirtualKey.Number3:
                    return "3";
                case VirtualKey.Number4:
                    return "4";
                case VirtualKey.Number5:
                    return "5";
                case VirtualKey.Number6:
                    return "6";
                case VirtualKey.Number7:
                    return "7";
                case VirtualKey.Number8:
                    return "8";
                case VirtualKey.Number9:
                    return "9";
                case VirtualKey.Divide:
                    return loader.GetString("Keys-Divide");
                case VirtualKey.Multiply:
                    return loader.GetString("Keys-Multiply");
                case VirtualKey.Add:
                    return loader.GetString("Keys-Add");
                case VirtualKey.Subtract:
                    return loader.GetString("Keys-Subtract");
                case VirtualKey.Delete:
                    return loader.GetString("Keys-Delete");
                case VirtualKey.Insert:
                    return loader.GetString("Keys-Insert");
                case VirtualKey.End:
                    return loader.GetString("Keys-End");
                case VirtualKey.Home:
                    return loader.GetString("Keys-Home");
                case VirtualKey.PageUp:
                    return loader.GetString("Keys-PageUp");
                case VirtualKey.PageDown:
                    return loader.GetString("Keys-PageDown");
                case VirtualKey.Left:
                    return loader.GetString("Keys-Left");
                case VirtualKey.Right:
                    return loader.GetString("Keys-Right");
                case VirtualKey.Up:
                    return loader.GetString("Keys-Up");
                case VirtualKey.Down:
                    return loader.GetString("Keys-Down");
                default:
                    return key.ToString();
            }
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

        public static async Task<DeviceInformation> GetDeviceInformationById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            try
            {
                return await DeviceInformation.CreateFromIdAsync(id);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static async Task<KeyValuePair<bool, int>> DownloadBinaryDataToFile(
            StorageFile targetFile,
            Uri uri
        )
        {
            return await DownloadBinaryDataToFile(targetFile, uri, null, CancellationToken.None);
        }

        public static async Task<KeyValuePair<bool, int>> DownloadBinaryDataToFile(
            StorageFile targetFile,
            Uri uri,
            IProgress<int> progress
        )
        {
            return await DownloadBinaryDataToFile(targetFile, uri, progress, CancellationToken.None);
        }

        public static async Task<KeyValuePair<bool, int>> DownloadBinaryDataToFile(
            StorageFile targetFile,
            Uri uri,
            IProgress<int> progress,
            CancellationToken cancellationToken
        )
        {
            Stream fileStream = null;

            try
            {
                var response = await httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
                if (!response.IsSuccessStatusCode) return new KeyValuePair<bool, int>(false, (int)response.StatusCode);
                long contentLength = response.Content.Headers.ContentLength.GetValueOrDefault();

                using (var responseStream = await response.Content.ReadAsStreamAsync())
                {
                    fileStream = await targetFile.OpenStreamForWriteAsync();
                    var buffer = new byte[8192];
                    int read;
                    long offset = 0;

                    do
                    {
                        read = await responseStream.ReadAsync(buffer, 0, buffer.Length);
                        await fileStream.WriteAsync(buffer, 0, read);
                        offset += read;

                        if (progress != null && offset != 0 && read != 0 && contentLength != 0)
                            progress.Report((int)Math.Floor((double)offset / contentLength * 100));
                    } while (read != 0 && !cancellationToken.IsCancellationRequested);

                    await fileStream.FlushAsync();
                    fileStream.Close();
                }

                if (cancellationToken.IsCancellationRequested)
                    return new KeyValuePair<bool, int>(false, -1);
                else
                    return new KeyValuePair<bool, int>(true, -1);
            }
            catch (Exception)
            {
                if (fileStream != null) fileStream.Close();
                return new KeyValuePair<bool, int>(false, -1);
            }
        }

        public static string RemoveSpecialCharsFromString(string input)
        {
            return new string(input.Where(c => 
                char.IsLetterOrDigit(c)
                || c == '.'
                || c == ','
                || c == ';'
                || c == '_'
                || c == '-'
                || c == '+'
                || c == ' '
                || c == '%'
                || c == '('
                || c == ')'
                || c == '{'
                || c == '}'
                || c == '['
                || c == ']'
                || c == '#'
                || c == '\''
                || c == '~'
                || c == '&'
                || c == '$'
                || c == '§'
                || c == '!'
            ).ToArray());
        }

        public static string FileTypeToExt(string fileType)
        {
            switch(fileType)
            {
                case "audio/mp4":
                    return "mp4";
                case "audio/wav":
                    return "wav";
                case "audio/ogg":
                    return "ogg";
                default:
                    return "mp3";
            }
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
        #endregion
    }
}
