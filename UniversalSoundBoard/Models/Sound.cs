using System;
using System.Collections.Generic;
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
        public List<Category> Categories { get; set; }
        public bool Favourite { get; set; }
        public BitmapImage Image { get; set; }

        public Sound()
        {
            Categories = new List<Category>();
        }

        public Sound(Guid uuid)
        {
            Uuid = uuid;
            Categories = new List<Category>();
        }

        public Sound(string name, List<Category> categories){
            Name = name;
            Categories = categories;
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

        public DownloadStatus GetAudioFileDownloadStatus()
        {
            return FileManager.GetSoundFileDownloadStatus(Uuid);
        }
    }

    public enum DownloadStatus
    {
        NoFileOrNotLoggedIn = 0,
        NotDownloaded = 1,
        Downloading = 2,
        Downloaded = 3
    }
}
