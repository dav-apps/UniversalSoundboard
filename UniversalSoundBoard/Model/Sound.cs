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
        public BitmapImage ImageFile { get; set; }

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
         //   ImageFile = StorageFile.GetFileFromApplicationUriAsync("ms-appx:///Assets/Images/default.png");

            /*
            var Image = new BitmapImage();
            Uri uri = new Uri("ms-appx:///Assets/Images/default.png", UriKind.Absolute);
            Image.UriSource = uri;
          //  DetailImage.Source = largeImage;
            ImageFile = Image;
            */

            // Get Image
            GetSoundImage(name);
        }

        private async void GetSoundImage(string name)
        {
            await FileManager.CreateImagesFolderIfNotExists();
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            StorageFolder imagesFolder = await folder.GetFolderAsync("images");

            var Image = new BitmapImage();

            StorageFile file1 = (StorageFile) await imagesFolder.TryGetItemAsync(name + ".png");
            if(file1 != null){
                Uri uri = new Uri(file1.Path, UriKind.Absolute);
                Image.UriSource = uri;

                ImageFile = Image;
                return;
            }
            file1 = (StorageFile)await imagesFolder.TryGetItemAsync(name + ".jpg");
            if (file1 != null)
            {
                Uri uri2 = new Uri(file1.Path, UriKind.Absolute);
                Image.UriSource = uri2;

                ImageFile = Image;
                return;
            }
            Uri uri3 = new Uri("ms-appx:///Assets/Images/default.png", UriKind.Absolute);
            Image.UriSource = uri3;

            ImageFile = Image;
        }

        public class SoundManager{

            private static async void GetSavedSounds(ObservableCollection<Sound> sounds)
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
                            var Image = new BitmapImage();
                            Uri uri = new Uri(image.Path, UriKind.Absolute);
                            Image.UriSource = uri;
                            sound.ImageFile = Image;
                        }
                    }

                    newSounds.Add(sound);
                }

                // Add found Sounds to Sounds ObservableCollection
                foreach (var sound in newSounds)
                {
                    sounds.Add(new Sound(sound.Name, sound.Category, sound.AudioFile));
                }
            }

            public static void GetAllSounds()
            {
                (App.Current as App)._itemViewHolder.progressRingIsActive = true;
                GetSavedSounds((App.Current as App)._itemViewHolder.sounds);
                (App.Current as App)._itemViewHolder.progressRingIsActive = false;
            }

            public static void GetSoundsByName(string name)
            {
                (App.Current as App)._itemViewHolder.progressRingIsActive = true;

                /*
                StorageFolder folder = ApplicationData.Current.LocalFolder;
                (App.Current as App)._itemViewHolder.sounds.Clear();

                List<Sound> newSounds = new List<Sound>();

                foreach (var file in await folder.GetFilesAsync())
                {
                    if (file.ContentType == "audio/wav" || file.ContentType == "audio/mpeg")
                    {
                        if (file.Name.StartsWith(name))
                        {
                            newSounds.Add(new Sound(file.Name, SoundCategory.Games, file.Path));
                        }
                    }
                }
                */



                // Get Saved Sounds
                ObservableCollection<Sound> allSounds = new ObservableCollection<Sound>();
                GetSavedSounds(allSounds);

                ObservableCollection<Sound> newSounds = new ObservableCollection<Sound>();

                //   newSounds = allSounds.Where(p => p.Name.StartsWith(name)).Select(p => p.Name).ToList();
                (App.Current as App)._itemViewHolder.sounds.Clear();
                foreach (var sound in allSounds)
                {
                    if (sound.Name.StartsWith(name))
                    {
                        (App.Current as App)._itemViewHolder.sounds.Add(sound);
                    }
                }
                /*
                (App.Current as App)._itemViewHolder.sounds.Clear();
                // Add found Sounds to Sounds ObservableCollection
                foreach (var sound in newSounds)
                {
                    
                }
                */
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
