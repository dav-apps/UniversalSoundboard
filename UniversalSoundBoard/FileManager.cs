using System;
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
            StorageFile audioFile = await StorageFile.GetFileFromPathAsync(sound.AudioFile);
            StorageFile imageFile = await StorageFile.GetFileFromPathAsync(sound.ImageFile);

            await audioFile.RenameAsync(newName + audioFile.FileType);
            await imageFile.RenameAsync(newName + imageFile.FileType);

            await SoundManager.GetAllSounds();
        }
    }
}
