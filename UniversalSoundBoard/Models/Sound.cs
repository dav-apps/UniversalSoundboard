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
        private const string DefaultLightSoundImageUri = "ms-appx:///Assets/Images/default.png";
        private const string DefaultDarkSoundImageUri = "ms-appx:///Assets/Images/default-dark.png";

        public Guid Uuid { get; }
        public string Name { get; set; }
        public List<Category> Categories { get; set; }
        public bool Favourite { get; set; }
        public int DefaultVolume { get; set; }
        public bool DefaultMuted { get; set; }
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

        public async Task<StorageFile> GetAudioFileAsync()
        {
            return await FileManager.GetAudioFileOfSoundAsync(Uuid);
        }

        public async Task<string> GetAudioFilePathAsync()
        {
            return await FileManager.GetAudioFilePathOfSoundAsync(Uuid);
        }

        public async Task<Uri> GetAudioUriAsync()
        {
            return await FileManager.GetAudioUriOfSoundAsync(Uuid);
        }

        public async Task<MemoryStream> GetAudioStreamAsync()
        {
            return await FileManager.GetAudioStreamOfSoundAsync(Uuid);
        }

        public async Task<string> GetAudioFileExtensionAsync()
        {
            return await FileManager.GetAudioFileExtensionAsync(Uuid);
        }

        public async Task DownloadFileAsync(Progress<int> progress)
        {
            await FileManager.DownloadAudioFileOfSoundAsync(Uuid, progress);
        }

        public async Task<DownloadStatus> GetAudioFileDownloadStatusAsync()
        {
            return await FileManager.GetSoundFileDownloadStatusAsync(Uuid);
        }

        public bool HasImageFile()
        {
            return
                Image.UriSource.ToString() != DefaultLightSoundImageUri
                && Image.UriSource.ToString() != DefaultDarkSoundImageUri;
        }

        public async Task<StorageFile> GetImageFileAsync()
        {
            return await FileManager.GetImageFileOfSoundAsync(Uuid);
        }

        public async Task<string> GetImageFileExtensionAsync()
        {
            return await FileManager.GetImageFileExtensionAsync(Uuid);
        }

        public static Uri GetDefaultImageUri()
        {
            Uri defaultImageUri;
            if (FileManager.itemViewHolder.CurrentTheme == FileManager.AppTheme.Dark)
                defaultImageUri = new Uri(DefaultDarkSoundImageUri, UriKind.Absolute);
            else
                defaultImageUri = new Uri(DefaultLightSoundImageUri, UriKind.Absolute);
            return defaultImageUri;
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
