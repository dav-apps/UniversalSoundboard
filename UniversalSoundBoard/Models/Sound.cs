using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UniversalSoundBoard.DataAccess;
using UniversalSoundBoard.Pages;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;

namespace UniversalSoundBoard.Models
{
    public class Sound{
        public string Uuid { get; }
        public string Name { get; set; }
        public Category Category { get; set; }
        public bool Favourite { get; set; }
        public BitmapImage Image { get; set; }
        public StorageFile ImageFile { get; set; }
        public StorageFile AudioFile { get; set; }
        public StorageFile DetailsFile { get; set; }

        public Sound()
        {

        }

        public Sound(string uuid, string name, bool favourite)
        {
            Uuid = uuid;
            Name = name;
            Favourite = favourite;
        }

        public Sound(string name, Category category){
            Name = name;
            Category = category;
            Favourite = false;
        }

        public Sound(string name, Category category, StorageFile AudioFile)
        {
            Name = name;
            Category = category;
            this.AudioFile = AudioFile;
            Favourite = false;
        }

        public void SetCategory(Category category)
        {
            Category = category;

            //SoundDetails details = new SoundDetails { Category = category.Name };
            //await FileManager.WriteFile(await FileManager.createSoundDetailsFileIfNotExistsAsync(Name), details);
        }


        public class SoundManager{
            /*
            private static async Task GetSavedSounds(ObservableCollection<Sound> sounds)
            {
                (App.Current as App)._itemViewHolder.allSoundsChanged = false;
                (App.Current as App)._itemViewHolder.progressRingIsActive = true;
                // Clear sounds ObservableCollections
                sounds.Clear();
                (App.Current as App)._itemViewHolder.allSounds.Clear();
                (App.Current as App)._itemViewHolder.favouriteSounds.Clear();
                // Create images folder if not exists
                await FileManager.CreateImagesFolderIfNotExists();

                StorageFolder folder = ApplicationData.Current.LocalFolder;
                StorageFolder imagesFolder = await folder.GetFolderAsync("images");
                StorageFolder detailsFolder = await FileManager.createDetailsFolderIfNotExistsAsync();

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
                        sound.Category = await FileManager.GetCategoryByNameAsync(WebUtility.HtmlDecode(details.Category));
                        sound.Favourite = details.Favourite;

                        newSounds.Add(sound);
                    }
                }

                // Add found Sounds to Sounds ObservableCollection
                foreach (var sound in newSounds)
                {
                    sounds.Add(sound);
                    (App.Current as App)._itemViewHolder.allSounds.Add(sound);
                    if (sound.Favourite)
                    {
                        (App.Current as App)._itemViewHolder.favouriteSounds.Add(sound);
                    }
                }

                await FileManager.UpdateGridView();
                (App.Current as App)._itemViewHolder.progressRingIsActive = false;
            }
            */
            /*
            public static async Task GetAllSounds()
            {
                (App.Current as App)._itemViewHolder.playAllButtonVisibility = Visibility.Collapsed;
                if((App.Current as App)._itemViewHolder.allSoundsChanged)
                {
                    await GetSavedSounds((App.Current as App)._itemViewHolder.sounds);
                }
                else
                {
                    (App.Current as App)._itemViewHolder.sounds.Clear();
                    (App.Current as App)._itemViewHolder.favouriteSounds.Clear();
                    // Copy all Sounds into selected Sounds list
                    foreach (var sound in (App.Current as App)._itemViewHolder.allSounds)
                    {
                        (App.Current as App)._itemViewHolder.sounds.Add(sound);
                        if (sound.Favourite)
                        {
                            (App.Current as App)._itemViewHolder.favouriteSounds.Add(sound);
                        }
                    }
                }
                
                ShowPlayAllButton();
            }
            */
        }
    }
}
