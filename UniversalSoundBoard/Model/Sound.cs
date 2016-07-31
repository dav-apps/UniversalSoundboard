using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;

namespace UniversalSoundBoard.Model
{
    public class Sound{
        public string Name { get; set; }
        public SoundCategory Category { get; set; }
        public string AudioFile { get; set; }
        public string ImageFile { get; set; }

        public Sound()
        {

        }

        public Sound(string name, SoundCategory category){
            Name = name;
            Category = category;
         //   AudioFile = String.Format("Assets/Audio/{0}/{1}.mp3", category, name);
         //   ImageFile = String.Format("Assets/Images/{0}/{1}.png", category, name);
        }

        public Sound(string name, SoundCategory category, string AudioFilePath)
        {
            Name = name;
            Category = category;
            AudioFile = AudioFilePath;
            // Get Image
            GetSoundImage(name);
        }

        private async Task GetSoundImage(string name)
        {
            await FileManager.CreateImagesFolderIfNotExists();
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            StorageFolder imagesFolder = await folder.GetFolderAsync("images");

            StorageFile file = (StorageFile) await imagesFolder.TryGetItemAsync(name + ".png");
            if(file != null){
                ImageFile = file.Path;
                return;
            }
            file = (StorageFile)await imagesFolder.TryGetItemAsync(name + ".jpg");
            if (file != null)
            {
                ImageFile = file.Path;
                return;
            }
            // If no file with the name exists
            ImageFile = "ms-appx:///Assets/Images/default.png";
        }

        public class SoundManager{

            private static async Task GetSavedSounds(ObservableCollection<Sound> sounds)
            {
                // Create images folder if not exists
                await FileManager.CreateImagesFolderIfNotExists();

                StorageFolder folder = ApplicationData.Current.LocalFolder;
                StorageFolder imagesFolder = await folder.GetFolderAsync("images");

                // Clear sounds ObservableCollection
                sounds.Clear();

                List<Sound> newSounds = new List<Sound>();

                foreach (var file in await folder.GetFilesAsync())
                {
                    Sound sound = new Sound();
                    
                    if (file.ContentType == "audio/wav" || file.ContentType == "audio/mpeg")
                    {
                        sound.Name = file.DisplayName;
                        sound.Category = SoundCategory.None;
                        sound.AudioFile = file.Path;
                    }

                    // Get Image for Sound
                    foreach (var image in await imagesFolder.GetFilesAsync())
                    {
                        if (image.DisplayName.Equals(file.DisplayName))
                        {
                            sound.ImageFile = image.Path;
                        }
                    }

                    newSounds.Add(sound);
                }

                // Add found Sounds to Sounds ObservableCollection
                foreach (var sound in newSounds)
                {
                    sounds.Add(sound);
                }
            }

            public static async Task GetAllSounds()
            {
                (App.Current as App)._itemViewHolder.progressRingIsActive = true;
                await GetSavedSounds((App.Current as App)._itemViewHolder.sounds);
                (App.Current as App)._itemViewHolder.progressRingIsActive = false;
            }

            public static async Task GetSoundsByName(string name)
            {
                (App.Current as App)._itemViewHolder.progressRingIsActive = true;
                
                // Get Saved Sounds
                ObservableCollection<Sound> allSounds = new ObservableCollection<Sound>();
                await GetSavedSounds(allSounds);

                ObservableCollection<Sound> newSounds = new ObservableCollection<Sound>();

                (App.Current as App)._itemViewHolder.sounds.Clear();
                foreach (var sound in allSounds)
                {
                    if (sound.Name.StartsWith(name))
                    {
                        (App.Current as App)._itemViewHolder.sounds.Add(sound);
                    }
                }

                (App.Current as App)._itemViewHolder.progressRingIsActive = false;
            }
        }
    }

    public enum SoundCategory{
        None,
        Animals,
        Warnings,
        Cartoons,
        Games
    }
}
