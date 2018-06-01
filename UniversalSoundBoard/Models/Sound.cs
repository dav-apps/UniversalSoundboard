using System;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;

namespace UniversalSoundBoard.Models
{
    public class Sound{
        public Guid Uuid { get; }
        public string Name { get; set; }
        public Category Category { get; set; }
        public bool Favourite { get; set; }
        public BitmapImage Image { get; set; }
        public StorageFile ImageFile { get; set; }
        public StorageFile AudioFile { get; set; }

        public Sound(){}

        public Sound(Guid uuid)
        {
            Uuid = uuid;
        }

        public Sound(string name, Category category){
            Name = name;
            Category = category;
            Favourite = false;
        }

        public Sound(Guid uuid, string name, bool favourite)
        {
            Uuid = uuid;
            Name = name;
            Favourite = favourite;
        }

        public Sound(string name, Category category, StorageFile AudioFile)
        {
            Name = name;
            Category = category;
            this.AudioFile = AudioFile;
            Favourite = false;
        }
    }
}
