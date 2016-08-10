using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using UniversalSoundBoard.Model;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;
using static UniversalSoundBoard.Model.Sound;

namespace UniversalSoundBoard
{
    public class FileManager
    {

        public static async void addImage(StorageFile file, Sound sound)
        {
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
            if((App.Current as App)._itemViewHolder.searchQuery == "")
            {
                if ((App.Current as App)._itemViewHolder.title == "All Sounds")
                {
                    await SoundManager.GetAllSounds();
                    (App.Current as App)._itemViewHolder.editButtonVisibility = Visibility.Collapsed;
                }
                else
                {
                    await SoundManager.GetSoundsByCategory(await GetCategoryByNameAsync((App.Current as App)._itemViewHolder.title));
                    (App.Current as App)._itemViewHolder.editButtonVisibility = Visibility.Visible;
                }
            }else
            {
                await SoundManager.GetSoundsByName((App.Current as App)._itemViewHolder.searchQuery);
                (App.Current as App)._itemViewHolder.editButtonVisibility = Visibility.Collapsed;
            }
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

            await UpdateGridView();
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

        public static async Task<List<Category>> GetCategoriesListAsync()
        {
            StorageFile dataFile = await FileManager.createDataFolderAndJsonFileIfNotExistsAsync();
            string data = await FileIO.ReadTextAsync(dataFile);

            //Deserialize Json
            var serializer = new DataContractJsonSerializer(typeof(Data));
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(data));
            var dataReader = (Data)serializer.ReadObject(ms);

            List<Category> categoriesList = dataReader.Categories;
            foreach(Category category in categoriesList)
            {
                category.Name = WebUtility.HtmlDecode(category.Name);
            }
            (App.Current as App)._itemViewHolder.categories = null;
            (App.Current as App)._itemViewHolder.categories = categoriesList;

            return categoriesList;
        }

        public static async Task SaveCategoriesListAsync(List<Category> categories)
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
                List<Category> categories = await GetCategoriesListAsync();
                return categories.Find(p => p.Name == categoryName);
            }
            return new Category { Icon = "Empty", Name = "Empty" };
        }

        public async static Task renameCategory(string oldName, string newName)
        {
            foreach (var sound in (App.Current as App)._itemViewHolder.sounds)
            {
                if (sound.CategoryName == oldName)
                {
                    StorageFile file = sound.DetailsFile;
                    SoundDetails details = new SoundDetails();
                    details.Category = newName;
                    await WriteFile(sound.DetailsFile, details);
                }
            }
        }

        public static async Task deleteCategory(string name)
        {
            List<Category> categories = await GetCategoriesListAsync();
            Category deletedCategory = categories.Find(p => p.Name == name);
            categories.Remove(deletedCategory);

            await SaveCategoriesListAsync(categories);
        }

        // Methods not related to project

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
    }
}
