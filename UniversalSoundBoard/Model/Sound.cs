using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;

namespace UniversalSoundBoard.Model
{
    public class Sound{
        public string Name { get; set; }
        public string CategoryName { get; set; }
        public BitmapImage Image { get; set; }
        public StorageFile ImageFile { get; set; }
        public StorageFile AudioFile { get; set; }
        public StorageFile DetailsFile { get; set; }

        public Sound()
        {

        }

        public Sound(string name, string category){
            Name = name;
            CategoryName = category;
        }

        public Sound(string name, string category, StorageFile AudioFile)
        {
            Name = name;
            CategoryName = category;
            this.AudioFile = AudioFile;
        }

        public async Task setCategory(string category)
        {
            CategoryName = category;

            // Create / get details json and write category into it
            SoundDetails details = new SoundDetails { Category = category };
            await FileManager.WriteFile(await FileManager.createSoundDetailsFileIfNotExistsAsync(Name), details);
        }


        public class SoundManager{

            static ObservableCollection<Sound> allSounds = new ObservableCollection<Sound>();

            private static async Task GetSavedSounds(ObservableCollection<Sound> sounds)
            {
                allSounds.Clear();
                // Create images folder if not exists
                await FileManager.CreateImagesFolderIfNotExists();

                StorageFolder folder = ApplicationData.Current.LocalFolder;
                StorageFolder imagesFolder = await folder.GetFolderAsync("images");
                StorageFolder detailsFolder = await FileManager.createDetailsFolderIfNotExistsAsync();

                // Clear sounds ObservableCollection
                sounds.Clear();

                List<Sound> newSounds = new List<Sound>();

                foreach (var file in await folder.GetFilesAsync())
                {
                    if (file.ContentType == "audio/wav" || file.ContentType == "audio/mpeg")
                    {
                        Sound sound = new Sound();

                        sound.Name = file.DisplayName;
                        sound.AudioFile = file;


                        // Get Image for Sound
                        BitmapImage DefaultImage = new BitmapImage();
                        Uri defaultImageUri;
                        if ((App.Current as App).RequestedTheme == ApplicationTheme.Dark)
                        {
                            defaultImageUri = new Uri("ms-appx:///Assets/Images/default-dark.png", UriKind.Absolute);
                        }
                        else
                        {
                            defaultImageUri = new Uri("ms-appx:///Assets/Images/default.png", UriKind.Absolute);
                        }
                        
                        DefaultImage.UriSource = defaultImageUri;

                        sound.Image = DefaultImage;

                        // Add image
                        foreach (var image in await imagesFolder.GetFilesAsync())
                        {
                            if (image.DisplayName.Equals(file.DisplayName))
                            {
                                var Image = new BitmapImage();
                                Uri uri = new Uri(image.Path, UriKind.Absolute);
                                Image.UriSource = uri;
                                sound.Image = Image;
                                sound.ImageFile = image;
                            }
                        }

                        // Add details file
                        SoundDetails details = new SoundDetails();
                        StorageFile detailsFile = await FileManager.createSoundDetailsFileIfNotExistsAsync(sound.Name);
                        await details.ReadSoundDetailsFile(detailsFile);
                        sound.DetailsFile = detailsFile;
                        sound.CategoryName = WebUtility.HtmlDecode(details.Category);

                        newSounds.Add(sound);
                    }
                }

                // Add found Sounds to Sounds ObservableCollection
                foreach (var sound in newSounds)
                {
                    sounds.Add(sound);
                    allSounds.Add(sound);
                }
            }

            public static async Task GetAllSounds()
            {
                (App.Current as App)._itemViewHolder.playAllButtonVisibility = Visibility.Collapsed;
                (App.Current as App)._itemViewHolder.progressRingIsActive = true;
                await GetSavedSounds((App.Current as App)._itemViewHolder.sounds);
                (App.Current as App)._itemViewHolder.progressRingIsActive = false;
                ShowPlayAllButton();
            }

            public static void GetSoundsByName(string name)
            {
                (App.Current as App)._itemViewHolder.playAllButtonVisibility = Visibility.Collapsed;
                (App.Current as App)._itemViewHolder.progressRingIsActive = true;
                (App.Current as App)._itemViewHolder.sounds.Clear();

                // Get Saved Sounds
                ObservableCollection<Sound> allSounds = new ObservableCollection<Sound>();
                allSounds = SoundManager.allSounds;

                ObservableCollection<Sound> newSounds = new ObservableCollection<Sound>();

                (App.Current as App)._itemViewHolder.sounds.Clear();
                foreach (var sound in allSounds)
                {
                    if (sound.Name.ToLower().Contains(name.ToLower()))
                    {
                        (App.Current as App)._itemViewHolder.sounds.Add(sound);
                    }
                }

                (App.Current as App)._itemViewHolder.progressRingIsActive = false;
                ShowPlayAllButton();
            }

            public static async Task GetSoundsByCategory(Category category)
            {
                (App.Current as App)._itemViewHolder.playAllButtonVisibility = Visibility.Collapsed;
                (App.Current as App)._itemViewHolder.progressRingIsActive = true;
                (App.Current as App)._itemViewHolder.sounds.Clear();

                // Get Saved Sounds
                ObservableCollection<Sound> allSounds = new ObservableCollection<Sound>();
                await GetSavedSounds(allSounds);

                ObservableCollection<Sound> newSounds = new ObservableCollection<Sound>();

                (App.Current as App)._itemViewHolder.sounds.Clear();
                foreach (var sound in allSounds)
                {
                    if (sound.CategoryName == category.Name)
                    {
                        (App.Current as App)._itemViewHolder.sounds.Add(sound);
                    }
                }

                (App.Current as App)._itemViewHolder.progressRingIsActive = false;
                ShowPlayAllButton();
            }

            public static async Task addSound(Sound sound)
            {
                StorageFolder folder = ApplicationData.Current.LocalFolder;

                StorageFile newFile = await sound.AudioFile.CopyAsync(folder, sound.AudioFile.Name, NameCollisionOption.GenerateUniqueName);
                await FileManager.createSoundDetailsFileIfNotExistsAsync(sound.Name);

                await sound.setCategory(sound.CategoryName);
            }

            private static void ShowPlayAllButton()
            {
                if((App.Current as App)._itemViewHolder.page != typeof(SoundPage))
                {
                    (App.Current as App)._itemViewHolder.playAllButtonVisibility = Visibility.Collapsed;
                }
                else
                {
                    (App.Current as App)._itemViewHolder.playAllButtonVisibility = Visibility.Visible;
                }
            }
        }
    }
}
