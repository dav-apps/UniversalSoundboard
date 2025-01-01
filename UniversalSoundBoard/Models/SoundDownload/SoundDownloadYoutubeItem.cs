using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using System.Threading;
using UniversalSoundboard.DataAccess;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Videos.Streams;
using Sentry;

namespace UniversalSoundboard.Models
{
    public class SoundDownloadYoutubeItem : SoundDownloadItem
    {
        YoutubeClient youtube = new YoutubeClient();

        public SoundDownloadYoutubeItem(
            string name,
            string url,
            string imageFileUrl,
            string audioFileUrl,
            string imageFileExt,
            string audioFileExt,
            long imageFileSize,
            long audioFileSize,
            bool isSelected = true
        ) : base(
            name,
            url,
            imageFileUrl,
            audioFileUrl,
            imageFileExt,
            audioFileExt,
            imageFileSize,
            audioFileSize,
            isSelected
        ) { }

        public override async Task<StorageFile> DownloadImageFile()
        {
            var videoResult = await youtube.Videos.GetAsync(AudioFileUrl);

            // Create a file in the cache
            StorageFolder cacheFolder = ApplicationData.Current.LocalCacheFolder;
            StorageFile targetFile = await cacheFolder.CreateFileAsync("download.jpg", CreationCollisionOption.GenerateUniqueName);

            bool imageDownloaded = await DownloadBestImageOfYoutubeVideo(targetFile, videoResult.Thumbnails);

            if (!imageDownloaded)
                return null;

            return targetFile;
        }

        public override async Task<StorageFile> DownloadAudioFile(IProgress<int> progress, CancellationToken cancellationToken)
        {
            try
            {
                var manifest = await youtube.Videos.Streams.GetManifestAsync(AudioFileUrl);
                var result = manifest.GetAudioOnlyStreams().GetWithHighestBitrate();

                // Create a file in the cache
                StorageFolder cacheFolder = ApplicationData.Current.LocalCacheFolder;
                StorageFile targetFile = await cacheFolder.CreateFileAsync("download.m4a", CreationCollisionOption.GenerateUniqueName);

                await youtube.Videos.Streams.DownloadAsync(
                    result,
                    targetFile.Path,
                    new Progress<double>((double value) => progress.Report((int)(value * 100))),
                    cancellationToken
                );

                return targetFile;
            }
            catch (Exception e)
            {
                SentrySdk.CaptureException(e);
                return null;
            }
        }

        private async Task<bool> DownloadBestImageOfYoutubeVideo(StorageFile targetFile, IReadOnlyList<Thumbnail> thumbnails)
        {
            Thumbnail selectedThumbnail = thumbnails.First();

            foreach (var thumbnail in thumbnails)
            {
                if (
                    thumbnail.Resolution.Width > selectedThumbnail.Resolution.Width
                    && thumbnail.Resolution.Height > selectedThumbnail.Resolution.Height
                ) selectedThumbnail = thumbnail;
            }

            var imageFileDownloadResult = await FileManager.DownloadBinaryDataToFile(targetFile, new Uri(selectedThumbnail.Url), null);

            if (!imageFileDownloadResult.Key)
            {
                // Try again
                imageFileDownloadResult = await FileManager.DownloadBinaryDataToFile(targetFile, new Uri(selectedThumbnail.Url), null);

                if (!imageFileDownloadResult.Key) return false;
            }

            return true;
        }
    }
}
