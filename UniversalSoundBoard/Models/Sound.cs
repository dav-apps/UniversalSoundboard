using davClassLibrary.Models;
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
        public TableObject AudioFileTableObject { get; set; }
        public StorageFile AudioFile { get; set; }
        public TableObject ImageFileTableObject { get; set; }
        public StorageFile ImageFile { get; set; }

        public Sound(Guid uuid, string name)
        {
            Uuid = uuid;
            Name = name;
            Categories = new List<Category>();
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

        public async Task DownloadFileAsync(Progress<(Guid, int)> progress)
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
