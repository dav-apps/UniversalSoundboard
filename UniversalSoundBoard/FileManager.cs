using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            await SoundManager.GetAllSounds();
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

            await SoundManager.GetAllSounds();
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

            (App.Current as App)._itemViewHolder.sounds.Clear();
            await SoundManager.GetAllSounds();
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

        public static async Task<List<string>> GetCategoriesListAsync()
        {
            StorageFile dataFile = await FileManager.createDataFolderAndJsonFileIfNotExistsAsync();
            string data = await FileIO.ReadTextAsync(dataFile);

            //Deserialize Json
            var serializer = new DataContractJsonSerializer(typeof(Data));
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(data));
            var dataReader = (Data)serializer.ReadObject(ms);

            string[] categoriesArray = dataReader.Categories;
            List<string> categoriesList = categoriesArray.ToList();
            (App.Current as App)._itemViewHolder.categories = categoriesList;
            return categoriesList;
        }

        public static async Task SaveCategoriesListAsync(List<string> categories)
        {
            StorageFile dataFile = await FileManager.createDataFolderAndJsonFileIfNotExistsAsync();

            Data data = new Data();
            data.Categories = categories.ToArray();


            DataContractJsonSerializer js = new DataContractJsonSerializer(typeof(Data));
            MemoryStream ms = new MemoryStream();
            js.WriteObject(ms, data);

            ms.Position = 0;
            StreamReader sr = new StreamReader(ms);
            string dataString = sr.ReadToEnd();

            await FileIO.WriteTextAsync(dataFile, dataString);

            await GetCategoriesListAsync();
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
    }
}
