using System;
using System.IO;
using System.Threading.Tasks;
using UniversalSoundBoard.DataAccess;
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

        public async Task<StorageFile> GetAudioFile()
        {
            return await FileManager.GetAudioFileOfSound(Uuid);
        }

        public Uri GetAudioUri()
        {
            return FileManager.GetAudioUriOfSound(Uuid);
        }

        public async Task<MemoryStream> GetAudioStream()
        {
            return await FileManager.GetAudioStreamOfSound(Uuid);
        }

        public async Task<StorageFile> GetImageFile()
        {
            return await FileManager.GetImageFileOfSound(Uuid);
        }

        public string GetAudioFileExtension()
        {
            return FileManager.GetAudioFileExtension(Uuid);
        }

        public void DownloadFile(Progress<int> progress)
        {
            FileManager.DownloadAudioFileOfSound(Uuid, progress);
        }
    }
}
