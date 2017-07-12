using NotificationsExtensions;
using NotificationsExtensions.Tiles;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using UniversalSoundBoard.Model;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
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

            TileBinding binding = new TileBinding()
            {
                Branding = TileBranding.NameAndLogo,

                Content = new TileBindingContentAdaptive()
                {
                    PeekImage = new TilePeekImage()
                    {
                        Source = sound.ImageFile.Path
                    },
                    Children =
                    {
                        new AdaptiveText()
                        {
                            Text = sound.Name
                        }
                    },
                    TextStacking = TileTextStacking.Center
                }
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
            // And send the notification
            TileUpdateManager.CreateTileUpdaterForApplication().Update(notification);
        }

        public static void resetMultiSelectArea()
        {
            (App.Current as App)._itemViewHolder.selectionMode = ListViewSelectionMode.None;
            (App.Current as App)._itemViewHolder.selectedSounds.Clear();
            (App.Current as App)._itemViewHolder.multiSelectOptionsVisibility = Visibility.Collapsed;
            (App.Current as App)._itemViewHolder.normalOptionsVisibility = Visibility.Visible;
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
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
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
