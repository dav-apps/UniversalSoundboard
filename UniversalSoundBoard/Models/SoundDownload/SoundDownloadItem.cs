using System;
using System.Threading;
using System.Threading.Tasks;
using UniversalSoundboard.DataAccess;
using Windows.Storage;

namespace UniversalSoundboard.Models
{
    public class SoundDownloadItem
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public string ImageFileUrl { get; set; }
        public string AudioFileUrl { get; set; }
        public string ImageFileExt { get; set; }
        public string AudioFileExt { get; set; }
        public long ImageFileSize { get; set; }
        public long AudioFileSize { get; set; }
        public bool IsSelected { get; set; }

        public SoundDownloadItem
        (
            string name,
            string url,
            string imageFileUrl,
            string audioFileUrl,
            string imageFileExt,
            string audioFileExt,
            long imageFileSize,
            long audioFileSize,
            bool isSelected = true
        )
        {
            Name = name;
            Url = url;
            ImageFileUrl = imageFileUrl;
            AudioFileUrl = audioFileUrl;
            ImageFileExt = imageFileExt;
            AudioFileExt = audioFileExt;
            ImageFileSize = imageFileSize;
            AudioFileSize = audioFileSize;
            IsSelected = isSelected;
        }

        public virtual async Task<StorageFile> DownloadImageFile()
        {
            if (ImageFileUrl == null)
                return null;

            // Assuming the file is a plain image file
            bool downloadSuccess = false;

            // Create a file in the cache
            StorageFolder cacheFolder = ApplicationData.Current.LocalCacheFolder;
            StorageFile targetFile = await cacheFolder.CreateFileAsync(string.Format("download.{0}", ImageFileExt), CreationCollisionOption.GenerateUniqueName);

            await Task.Run(async () =>
            {
                downloadSuccess = (
                    await FileManager.DownloadBinaryDataToFile(
                        targetFile,
                        new Uri(ImageFileUrl)
                    )
                ).Key;
            });

            if (!downloadSuccess)
                return null;

            return targetFile;
        }

        public virtual async Task<StorageFile> DownloadAudioFile(IProgress<int> progress, CancellationToken cancellationToken)
        {
            if (AudioFileUrl == null)
                return null;
                
            // Assuming the file is a plain audio file
            bool downloadSuccess = false;

            // Create a file in the cache
            StorageFolder cacheFolder = ApplicationData.Current.LocalCacheFolder;
            StorageFile targetFile = await cacheFolder.CreateFileAsync(string.Format("download.{0}", AudioFileExt), CreationCollisionOption.GenerateUniqueName);

            await Task.Run(async () =>
            {
                downloadSuccess = (
                    await FileManager.DownloadBinaryDataToFile(
                        targetFile,
                        new Uri(AudioFileUrl),
                        progress,
                        cancellationToken
                    )
                ).Key;
            });

            if (cancellationToken.IsCancellationRequested || !downloadSuccess)
                return null;

            return targetFile;
        }
    }
}
