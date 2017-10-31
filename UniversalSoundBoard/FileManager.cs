using Microsoft.Toolkit.Uwp.Notifications;
using NotificationsExtensions;
using NotificationsExtensions.Tiles;
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
using UniversalSoundBoard.Model;
using Windows.ApplicationModel.Core;
using Windows.Data.Xml.Dom;
using Windows.Foundation;
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
using static UniversalSoundBoard.Model.Sound;

namespace UniversalSoundBoard
{
    public class FileManager
    {
        public const double volume = 1.0;
        public const bool liveTile = true;
        public const bool playingSoundsListVisible = false;
        public const bool playOneSoundAtOnce = true;
        public const bool darkTheme = false;
        public const bool showCategoryIcon = true;
        public const bool showSoundsPivot = true;
        public const int mobileMaxWidth = 550;
        public const int tabletMaxWidth = 650;
        public const int topButtonsCollapsedMaxWidth = 1400;
        public const int sideBarCollapsedMaxWidth = 1100;
        public const int moveSelectButtonMaxWidth = 850;
        public const int moveAddButtonMaxWidth = 800;
        public const int moveVolumeButtonMaxWidth = 750;
        public const int hideSearchBoxMaxWidth = 700;

        public static bool skipAutoSuggestBoxTextChanged = false;

        public static async void addImage(StorageFile file, Sound sound)
        {
            (App.Current as App)._itemViewHolder.allSoundsChanged = true;
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            StorageFolder imagesFolder = await folder.GetFolderAsync("images");

            if (file.ContentType.Equals("image/png")){
                // Copy new image and delete the old one
                StorageFile newFile = await file.CopyAsync(imagesFolder, sound.Name + ".png", NameCollisionOption.ReplaceExisting);

                StorageFile oldFile = (StorageFile)await imagesFolder.TryGetItemAsync(sound.Name + ".jpg");
                if(oldFile != null)
                {
                    await oldFile.DeleteAsync();
                }
            }
            else if (file.ContentType.Equals("image/jpeg")){
                StorageFile newFile = await file.CopyAsync(imagesFolder, sound.Name + ".jpg", NameCollisionOption.ReplaceExisting);

                StorageFile oldFile = (StorageFile)await imagesFolder.TryGetItemAsync(sound.Name + ".png");
                if (oldFile != null)
                {
                    await oldFile.DeleteAsync();
                }
            }

            // Update GridView
            await UpdateGridView();
        }
        
        public static async Task UpdateGridView()
        {
            // Save title and check at the end if another category was selected
            string title = (App.Current as App)._itemViewHolder.title;
            if((App.Current as App)._itemViewHolder.searchQuery == "")
            {
                if ((App.Current as App)._itemViewHolder.title == (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("AllSounds"))
                {
                    await SoundManager.GetAllSounds();
                    (App.Current as App)._itemViewHolder.editButtonVisibility = Visibility.Collapsed;
                }else if((App.Current as App)._itemViewHolder.page != typeof(SoundPage))
                {
                    (App.Current as App)._itemViewHolder.editButtonVisibility = Visibility.Collapsed;
                }
                else
                {
                    await SoundManager.GetSoundsByCategory(await GetCategoryByNameAsync((App.Current as App)._itemViewHolder.title));
                    (App.Current as App)._itemViewHolder.editButtonVisibility = Visibility.Visible;
                }
            }else
            {
                SoundManager.GetSoundsByName((App.Current as App)._itemViewHolder.searchQuery);
                (App.Current as App)._itemViewHolder.editButtonVisibility = Visibility.Collapsed;
            }

            // Check if another category was selected
            if(title != (App.Current as App)._itemViewHolder.title)
            {
                // Update UI
                await UpdateGridView();
            }
            SoundManager.ShowPlayAllButton();
        }

        public static async Task CreateImagesFolderIfNotExists()
        {
            // Create images folder if not exists
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            StorageFolder imagesFolder;
            if (await folder.TryGetItemAsync("images") == null)
            {
                imagesFolder = await folder.CreateFolderAsync("images");
            }
            else
            {
                imagesFolder = await folder.GetFolderAsync("images");
            }
        }

        public static async Task renameSound(Sound sound, string newName)
        {
            (App.Current as App)._itemViewHolder.allSoundsChanged = true;
            StorageFile audioFile = sound.AudioFile;
            StorageFile imageFile = sound.ImageFile;
            if (sound.DetailsFile == null)
            {
                sound.DetailsFile = await createSoundDetailsFileIfNotExistsAsync(sound.Name);
            }
            await sound.DetailsFile.RenameAsync(newName + sound.DetailsFile.FileType);

            await audioFile.RenameAsync(newName + audioFile.FileType);
            if(imageFile != null){
                await imageFile.RenameAsync(newName + imageFile.FileType);
            }

            await UpdateGridView();
        }

        public static async Task deleteSound(Sound sound)
        {
            (App.Current as App)._itemViewHolder.allSoundsChanged = true;
            await sound.AudioFile.DeleteAsync();
            if (sound.ImageFile != null)
            {
                await sound.ImageFile.DeleteAsync();
            }
            if(sound.DetailsFile == null)
            {
                await createSoundDetailsFileIfNotExistsAsync(sound.Name);
            }
            await sound.DetailsFile.DeleteAsync();
        }

        public static async Task setSoundAsFavourite(Sound sound, bool favourite)
        {
            // Check if details file of the sound exists
            if (sound.DetailsFile == null)
            {
                sound.DetailsFile = await createSoundDetailsFileIfNotExistsAsync(sound.Name);
            }

            // Create new details object and write to details file
            SoundDetails details = new SoundDetails
            {
                Category = sound.Category.Name,
                Favourite = favourite
            };
            await WriteFile(sound.DetailsFile, details);
        }

        public static async Task addSound(Sound sound)
        {
            (App.Current as App)._itemViewHolder.allSoundsChanged = true;
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            StorageFile newFile = await sound.AudioFile.CopyAsync(folder, sound.AudioFile.Name, NameCollisionOption.GenerateUniqueName);
            await createSoundDetailsFileIfNotExistsAsync(sound.Name);
            if (sound.Category != null)
            {
                await sound.setCategory(sound.Category);
            }
        }

        public static async Task<StorageFile> createDataFolderAndJsonFileIfNotExistsAsync()
        {
            StorageFolder root = ApplicationData.Current.LocalFolder;
            StorageFolder dataFolder;
            if (await root.TryGetItemAsync("data") == null)
            {
                dataFolder = await root.CreateFolderAsync("data");
            }else
            {
                dataFolder = await root.GetFolderAsync("data");
            }

            StorageFile dataFile;
            if (await dataFolder.TryGetItemAsync("data.json") == null)
            {
                dataFile = await dataFolder.CreateFileAsync("data.json");
                await FileIO.WriteTextAsync(dataFile, "{\"Categories\": []}");
            }else{
                dataFile = await dataFolder.GetFileAsync("data.json");
            }
            return dataFile;
        }

        public static async Task<StorageFolder> createDetailsFolderIfNotExistsAsync()
        {
            StorageFolder root = ApplicationData.Current.LocalFolder;
            StorageFolder detailsFolder;
            if (await root.TryGetItemAsync("soundDetails") == null)
            {
                return detailsFolder = await root.CreateFolderAsync("soundDetails");
            }else
            {
                return detailsFolder = await root.GetFolderAsync("soundDetails");
            }
        }

        public static async Task<StorageFile> createSoundDetailsFileIfNotExistsAsync(string soundName)
        {
            StorageFolder detailsFolder = await createDetailsFolderIfNotExistsAsync();
            StorageFile detailsFile;
            if (await detailsFolder.TryGetItemAsync(soundName + ".json") == null)
            {
                // Create file and write empty json
                detailsFile = await detailsFolder.CreateFileAsync(soundName + ".json");
                SoundDetails details = new SoundDetails();
                details.Category = "";

                await WriteFile(detailsFile, details);

                return detailsFile;
            }
            else
            {
                return detailsFile = await detailsFolder.GetFileAsync(soundName + ".json");
            }
        }

        public static async Task deleteExportAndImportFoldersAsync()
        {
            StorageFolder localDataFolder = ApplicationData.Current.LocalFolder;

            if (await localDataFolder.TryGetItemAsync("export") != null)
            {
                await (await localDataFolder.GetFolderAsync("export")).DeleteAsync();
            }

            if (await localDataFolder.TryGetItemAsync("import") != null)
            {
                await (await localDataFolder.GetFolderAsync("import")).DeleteAsync();
            }


            if(await localDataFolder.TryGetItemAsync("import.zip") != null)
            {
                await (await localDataFolder.GetFileAsync("import.zip")).DeleteAsync();
            }

            if (await localDataFolder.TryGetItemAsync("export.zip") != null)
            {
                await (await localDataFolder.GetFileAsync("export.zip")).DeleteAsync();
            }
        }

        public static async Task CreateCategoriesObservableCollection()
        {
            (App.Current as App)._itemViewHolder.categories.Clear();
            (App.Current as App)._itemViewHolder.categories.Add(new Category { Name = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("AllSounds"), Icon = "\uE10F" });

            foreach (Category cat in await GetCategoriesListAsync())
            {
                (App.Current as App)._itemViewHolder.categories.Add(cat);
            }
        }

        public static async Task<ObservableCollection<Category>> GetCategoriesListAsync()
        {
            StorageFile dataFile = await FileManager.createDataFolderAndJsonFileIfNotExistsAsync();
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

        public static async Task SaveCategoriesListAsync(ObservableCollection<Category> categories)
        {
            StorageFile dataFile = await FileManager.createDataFolderAndJsonFileIfNotExistsAsync();

            Data data = new Data();
            data.Categories = categories;

            foreach (var category in data.Categories)
            {
                category.Name = HTMLEncodeSpecialChars(category.Name);
            }

            DataContractJsonSerializer js = new DataContractJsonSerializer(typeof(Data));
            MemoryStream ms = new MemoryStream();
            js.WriteObject(ms, data);

            ms.Position = 0;
            StreamReader sr = new StreamReader(ms);
            string dataString = sr.ReadToEnd();

            await FileIO.WriteTextAsync(dataFile, dataString);

            await GetCategoriesListAsync();
        }

        public static async Task<Category> GetCategoryByNameAsync(string categoryName){
            if(categoryName != "" || (await GetCategoriesListAsync()).Count >= 1)
            {
                ObservableCollection<Category> categories = await GetCategoriesListAsync();
                foreach(Category category in categories)
                {
                    if(category.Name == categoryName)
                    {
                        return category;
                    }
                }
            }
            return null;
        }

        public async static Task renameCategory(string oldName, string newName)
        {
            foreach (var sound in (App.Current as App)._itemViewHolder.sounds)
            {
                if (sound.Category.Name == oldName)
                {
                    SoundDetails details = new SoundDetails();
                    details.Category = newName;
                    details.Favourite = sound.Favourite;
                    await WriteFile(sound.DetailsFile, details);
                }
            }
        }

        public static async Task deleteCategory(string name)
        {
            ObservableCollection<Category> categories = await GetCategoriesListAsync();

            Category deletedCategory = new Category();
            foreach(Category category in categories)
            {
                if (category.Name == name)
                {
                    deletedCategory = category;
                }
            }

            categories.Remove(deletedCategory);

            await SaveCategoriesListAsync(categories);
        }

        public static void UpdateLiveTile()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            bool isLiveTileOn = false;

            if (localSettings.Values["liveTile"] == null)
            {
                localSettings.Values["liveTile"] = liveTile;
                isLiveTileOn = liveTile;
            }else
            {
                isLiveTileOn = (bool)localSettings.Values["liveTile"];
            }

            if ((App.Current as App)._itemViewHolder.sounds.Count <= 0 || !isLiveTileOn)
            {
                TileUpdateManager.CreateTileUpdaterForApplication().Clear();
                return;
            }
            List<Sound> sounds = new List<Sound>();
            Sound sound = (App.Current as App)._itemViewHolder.sounds.Last();
            // Get sound with image
            foreach (var s in (App.Current as App)._itemViewHolder.allSounds)
            {
                if (s.ImageFile != null)
                {
                    sounds.Add(s);
                }
            }

            if (sounds.Count <= 0)
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

        public static void resetMultiSelectArea()
        {
            (App.Current as App)._itemViewHolder.selectionMode = ListViewSelectionMode.None;
            (App.Current as App)._itemViewHolder.selectedSounds.Clear();
            (App.Current as App)._itemViewHolder.normalOptionsVisibility = true;
        }

        public static async Task ExportData(StorageFolder destinationFolder)
        {
            var stringLoader = new Windows.ApplicationModel.Resources.ResourceLoader();
            (App.Current as App)._itemViewHolder.exported = false;
            (App.Current as App)._itemViewHolder.imported = false;
            (App.Current as App)._itemViewHolder.isExporting = true;
            (App.Current as App)._itemViewHolder.areExportAndImportButtonsEnabled = false;
            (App.Current as App)._itemViewHolder.exportMessage = stringLoader.GetString("ExportMessage-1"); // 1

            await deleteExportAndImportFoldersAsync();
            await createDataFolderAndJsonFileIfNotExistsAsync();
            await createDetailsFolderIfNotExistsAsync();

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

            (App.Current as App)._itemViewHolder.exportMessage = stringLoader.GetString("ExportMessage-2"); // 2

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
            (App.Current as App)._itemViewHolder.exportMessage = stringLoader.GetString("ExportMessage-3"); // 3

            // Create Zip file in local storage
            IAsyncAction asyncAction = Windows.System.Threading.ThreadPool.RunAsync(
                    async (workItem) =>
                    {
                        var t = Task.Run(() => ZipFile.CreateFromDirectory(exportFolder.Path, localDataFolder.Path + @"\export.zip"));
                        t.Wait();

                        // Get the created file and move it to the picked folder
                        await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                            CoreDispatcherPriority.High,
                            new DispatchedHandler(() =>
                            {
                                (App.Current as App)._itemViewHolder.exportMessage = stringLoader.GetString("ExportMessage-4"); // 4
                            }));

                        StorageFile exportZipFile = await localDataFolder.GetFileAsync("export.zip");
                        await exportZipFile.MoveAsync(destinationFolder, "UniversalSoundBoard " + DateTime.Today.ToString("dd.MM.yyyy") + ".zip", NameCollisionOption.GenerateUniqueName);

                        await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                            CoreDispatcherPriority.High,
                            new DispatchedHandler(async () =>
                            {
                                (App.Current as App)._itemViewHolder.exportMessage = stringLoader.GetString("ExportImportMessage-TidyUp"); // TidyUp
                                await deleteExportAndImportFoldersAsync();

                                (App.Current as App)._itemViewHolder.exportMessage = "";
                                (App.Current as App)._itemViewHolder.isExporting = false;
                                (App.Current as App)._itemViewHolder.exported = true;
                                (App.Current as App)._itemViewHolder.areExportAndImportButtonsEnabled = true;
                            }));
                        // SendExportSuccessfullNotification();
                    });
        }

        public static async Task ImportDataZip(StorageFile zipFile)
        {
            var stringLoader = new Windows.ApplicationModel.Resources.ResourceLoader();
            (App.Current as App)._itemViewHolder.isImporting = true;
            (App.Current as App)._itemViewHolder.exported = false;
            (App.Current as App)._itemViewHolder.imported = false;
            (App.Current as App)._itemViewHolder.areExportAndImportButtonsEnabled = false;
            (App.Current as App)._itemViewHolder.importMessage = stringLoader.GetString("ImportMessage-1"); // 1

            await deleteExportAndImportFoldersAsync();
            await CreateImportFolders();

            StorageFolder localDataFolder = ApplicationData.Current.LocalFolder;
            StorageFolder importFolder = await localDataFolder.GetFolderAsync("import");

            // Copy zip file into local storage
            StorageFile newZipFile = await zipFile.CopyAsync(localDataFolder, "import.zip", NameCollisionOption.ReplaceExisting);

            (App.Current as App)._itemViewHolder.importMessage = stringLoader.GetString("ImportMessage-2"); // 2

            IAsyncAction asyncAction = Windows.System.Threading.ThreadPool.RunAsync(
                    async (workItem) =>
                    {
                        // Extract zip file in local storage
                        var t = Task.Run(() => ZipFile.ExtractToDirectory(newZipFile.Path, importFolder.Path));
                        t.Wait();

                        await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                            CoreDispatcherPriority.High,
                            new DispatchedHandler(() =>
                            {

                                (App.Current as App)._itemViewHolder.importMessage = stringLoader.GetString("ImportMessage-3"); // 3
                            }));

                        await ImportData();

                        await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                            CoreDispatcherPriority.High,
                            new DispatchedHandler(async () =>
                            {
                                (App.Current as App)._itemViewHolder.importMessage = stringLoader.GetString("ExportImportMessage-TidyUp"); // TidyUp
                                await deleteExportAndImportFoldersAsync();

                                (App.Current as App)._itemViewHolder.importMessage = "";
                                (App.Current as App)._itemViewHolder.isImporting = false;
                                (App.Current as App)._itemViewHolder.imported = true;
                                (App.Current as App)._itemViewHolder.allSoundsChanged = true;
                                (App.Current as App)._itemViewHolder.areExportAndImportButtonsEnabled = true;

                                await SoundManager.GetAllSounds();
                                await FileManager.CreateCategoriesObservableCollection();
                                await setSoundBoardSizeTextAsync();
                            }));
                    });
        }

        private async static Task ImportData()
        {
            StorageFolder localDataFolder = ApplicationData.Current.LocalFolder;
            StorageFolder importFolder = await localDataFolder.GetFolderAsync("import");
            StorageFolder soundDetailsImportFolder = await importFolder.GetFolderAsync("soundDetails");
            StorageFolder imagesImportFolder = await importFolder.GetFolderAsync("images");
            StorageFolder dataImportFolder = await importFolder.GetFolderAsync("data");

            StorageFolder soundDetailsFolder = await localDataFolder.GetFolderAsync("soundDetails");
            StorageFolder imagesFolder = await localDataFolder.GetFolderAsync("images");

            // Copy Sound files into local storage
            foreach (var file in await importFolder.GetFilesAsync())
            {
                if (file.ContentType == "audio/wav" || file.ContentType == "audio/mpeg")
                {
                    if(await localDataFolder.TryGetItemAsync(file.Name) == null)
                    {
                        await file.CopyAsync(localDataFolder);
                    }
                }
            }

            // Copy detail files into local storage
            foreach (var file in await soundDetailsImportFolder.GetFilesAsync())
            {
                if (await soundDetailsFolder.TryGetItemAsync(file.Name) == null)
                {
                    await file.CopyAsync(soundDetailsFolder);
                }
            }

            // Copy images into local storage
            foreach (var file in await imagesImportFolder.GetFilesAsync())
            {
                if (file.ContentType == "image/jpeg" || file.ContentType == "image/png")
                {
                    if (await imagesFolder.TryGetItemAsync(file.Name) == null)
                    {
                        await file.CopyAsync(imagesFolder);
                    }
                }
            }

            // Read data.json and add categories
            StorageFile dataFile = await createDataFolderAndJsonFileIfNotExistsAsync();
            StorageFile dataImportFile = await dataImportFolder.GetFileAsync("data.json");

            // Read data file
            string data = await FileIO.ReadTextAsync(dataFile);
            var serializer = new DataContractJsonSerializer(typeof(Data));
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(data));
            var dataReader = (Data)serializer.ReadObject(ms);
            ObservableCollection<Category> categoriesList = dataReader.Categories;

            // Read import data file
            string importData = await FileIO.ReadTextAsync(dataImportFile);
            var serializer2 = new DataContractJsonSerializer(typeof(Data));
            var ms2 = new MemoryStream(Encoding.UTF8.GetBytes(importData));
            var dataReader2 = (Data)serializer.ReadObject(ms2);
            ObservableCollection<Category> importCategoriesList = dataReader2.Categories;

            // Add imported categories to original list
            foreach(Category category in importCategoriesList)
            {
                if(categoriesList.Where(cat => cat.Name.Equals(category.Name)).Count() == 0)
                {
                    categoriesList.Add(category);
                }
            }

            // Write data.json with new categories list
            Data categoryData = new Data();
            categoryData.Categories = categoriesList;
            await WriteFile(dataFile, categoryData);

            await GetCategoriesListAsync();
        }

        private static async Task CreateExportFoldersAsync()
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

        private async static Task CreateImportFolders()
        {
            StorageFolder localDataFolder = ApplicationData.Current.LocalFolder;

            StorageFolder importFolder;
            if (await localDataFolder.TryGetItemAsync("import") == null)
            {
                importFolder = await localDataFolder.CreateFolderAsync("import");
            }
            else
            {
                importFolder = await localDataFolder.GetFolderAsync("import");
            }
        }

        private static void SendExportSuccessfullNotification()
        {
            ToastContent content = new ToastContent()
            {
                Visual = new ToastVisual()
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
                        {
                            new Microsoft.Toolkit.Uwp.Notifications.AdaptiveText()
                            {
                                Text = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("ExportNotification-Title")
                            },

                            new Microsoft.Toolkit.Uwp.Notifications.AdaptiveText()
                            {
                                Text = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("ExportNotification-Message")
                            }
                        }
                    }
                }
            };

            XmlDocument xmlContent = content.GetXml();
            ToastNotification notification = new ToastNotification(xmlContent);
            ToastNotificationManager.CreateToastNotifier().Show(notification);
        }

        public static async Task setSoundBoardSizeTextAsync()
        {
            if ((App.Current as App)._itemViewHolder.progressRingIsActive)
            {
                await Task.Delay(1000);
                await setSoundBoardSizeTextAsync();
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

            (App.Current as App)._itemViewHolder.soundboardSize = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("SettingsSoundBoardSize") + totalSize.ToString("n2") + " GB.";
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

        public static async Task ShowCategory(Category category)
        {
            (App.Current as App)._itemViewHolder.page = typeof(SoundPage);
            ///SearchAutoSuggestBox.Text = "";
            (App.Current as App)._itemViewHolder.searchQuery = "";
            (App.Current as App)._itemViewHolder.searchQuery = "";
            (App.Current as App)._itemViewHolder.title = WebUtility.HtmlDecode(category.Name);
            SetBackButtonVisibility(true);
            (App.Current as App)._itemViewHolder.editButtonVisibility = Visibility.Visible;
            //SideBar.SelectedItem = SideBar.MenuItems.Last();
            await SoundManager.GetSoundsByCategory(category);
        }

        public static bool AreTopButtonsNormal()
        {
            if (!(App.Current as App)._itemViewHolder.normalOptionsVisibility ||
                    (Window.Current.Bounds.Width < tabletMaxWidth && (App.Current as App)._itemViewHolder.searchAutoSuggestBoxVisibility))
            {
                return false;
            }
            return true;
        }

        public static void ResetSearchArea()
        {
            if (Window.Current.Bounds.Width < FileManager.tabletMaxWidth)
            {
                // Clear text and show buttons
                (App.Current as App)._itemViewHolder.searchAutoSuggestBoxVisibility = false;
                (App.Current as App)._itemViewHolder.addButtonVisibility = true;
                (App.Current as App)._itemViewHolder.volumeButtonVisibility = true;
            }
            FileManager.skipAutoSuggestBoxTextChanged = true;
            (App.Current as App)._itemViewHolder.searchQuery = "";
        }

        public static void ResetTopButtons()
        {
            (App.Current as App)._itemViewHolder.selectedSounds.Clear();
            ///(App.Current as App)._itemViewHolder.multiSelectOptionsVisibility = Visibility.Collapsed;
            (App.Current as App)._itemViewHolder.normalOptionsVisibility = true;
            (App.Current as App)._itemViewHolder.selectionMode = ListViewSelectionMode.None;

            ResetSearchArea();
        }

        public static async Task ShowAllSounds()
        {
            if (AreTopButtonsNormal())
            {
                SetBackButtonVisibility(false);
            }
            skipAutoSuggestBoxTextChanged = true;
            (App.Current as App)._itemViewHolder.searchQuery = "";
            //SideBar.SelectedItem = (App.Current as App)._itemViewHolder.categories.First();
            (App.Current as App)._itemViewHolder.editButtonVisibility = Visibility.Collapsed;
            (App.Current as App)._itemViewHolder.title = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("AllSounds");
            (App.Current as App)._itemViewHolder.page = typeof(SoundPage);
            await SoundManager.GetAllSounds();
            skipAutoSuggestBoxTextChanged = false;
        }

        public static void CheckBackButtonVisibility()
        {
            if (FileManager.AreTopButtonsNormal() &&
                (App.Current as App)._itemViewHolder.title == (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("AllSounds"))
            {       // Anything is normal, SoundPage shows All Sounds
                FileManager.SetBackButtonVisibility(false);
            }
            else
            {
                FileManager.SetBackButtonVisibility(true);
            }
        }




        // Methods not related to project

        public static async Task<float> GetFileSizeInGBAsync(StorageFile file)
        {
            BasicProperties pro = await file.GetBasicPropertiesAsync();
            return (((pro.Size / 1024f) / 1024f)/ 1024f);
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

        public static List<string> createIconsList()
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
    }
}
