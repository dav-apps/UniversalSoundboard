using DotNetTools.SharpGrabber.Grabbed;
using DotNetTools.SharpGrabber;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using System.Threading;
using UniversalSoundboard.DataAccess;
using Microsoft.AppCenter.Crashes;
using UniversalSoundboard.Common;

namespace UniversalSoundboard.Models
{
    public class SoundDownloadYoutubeItem : SoundDownloadItem
    {
        private GrabResult grabResult;

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
            var grabResult = await GrabVideoData();
            if (grabResult == null) return null;

            var imageResources = grabResult.Resources<GrabbedImage>();

            // Create a file in the cache
            StorageFolder cacheFolder = ApplicationData.Current.LocalCacheFolder;
            StorageFile targetFile = await cacheFolder.CreateFileAsync("download.jpg", CreationCollisionOption.GenerateUniqueName);

            bool imageDownloaded = await DownloadBestImageOfYoutubeVideo(targetFile, imageResources);

            if (!imageDownloaded)
                return null;

            return targetFile;
        }

        public override async Task<StorageFile> DownloadAudioFile(IProgress<int> progress, CancellationToken cancellationToken)
        {
            var grabResult = await GrabVideoData();
            if (grabResult == null) return null;

            var mediaResources = grabResult.Resources<GrabbedMedia>();

            // Create a file in the cache
            StorageFolder cacheFolder = ApplicationData.Current.LocalCacheFolder;
            StorageFile targetFile = await cacheFolder.CreateFileAsync("download.m4a", CreationCollisionOption.GenerateUniqueName);

            // Find the audio track with the highest sample rate
            GrabbedMedia bestAudio = FindBestAudioOfYoutubeVideo(mediaResources);

            if (bestAudio == null)
                return null;

            bool audioDownloaded = false;

            await Task.Run(async () =>
            {
                audioDownloaded = (
                    await FileManager.DownloadBinaryDataToFile(
                        targetFile,
                        bestAudio.ResourceUri,
                        progress,
                        cancellationToken
                    )
                ).Key;
            });

            if (!audioDownloaded)
                return null;

            return targetFile;
        }

        private async Task<GrabResult> GrabVideoData()
        {
            if (grabResult != null) return grabResult;

            var grabber = GrabberBuilder.New().UseDefaultServices().AddYouTube().Build();

            try
            {
                grabResult = await grabber.GrabAsync(new Uri(AudioFileUrl));

                if (grabResult == null)
                    throw new SoundDownloadException();
            }
            catch (Exception e)
            {
                Crashes.TrackError(e, new Dictionary<string, string>
                {
                    { "YoutubeLink", AudioFileUrl }
                });

                throw new SoundDownloadException();
            }

            return grabResult;
        }

        private GrabbedMedia FindBestAudioOfYoutubeVideo(IEnumerable<GrabbedMedia> mediaResources)
        {
            GrabbedMedia bestAudio = null;

            foreach (var media in mediaResources)
            {
                if (media.Format.Extension != "m4a") continue;

                int? bitrate = media.GetBitRate();
                if (!bitrate.HasValue) continue;

                if (bestAudio == null || bestAudio.GetBitRate().Value < bitrate.Value)
                    bestAudio = media;
            }

            return bestAudio;
        }

        private async Task<bool> DownloadBestImageOfYoutubeVideo(StorageFile targetFile, IEnumerable<GrabbedImage> imageResources)
        {
            // Find the thumbnail image with the highest resolution
            List<GrabbedImage> images = new List<GrabbedImage>();
            GrabbedImage maxresDefaultImage = null;
            GrabbedImage mqDefaultImage = null;
            GrabbedImage sdDefaultImage = null;
            GrabbedImage hqDefaultImage = null;
            GrabbedImage defaultImage = null;

            foreach (var media in imageResources)
            {
                string mediaFileName = media.ResourceUri.ToString().Split('/').Last();

                switch (mediaFileName)
                {
                    case "maxresdefault.jpg":
                        maxresDefaultImage = media;
                        break;
                    case "mqdefault.jpg":
                        mqDefaultImage = media;
                        break;
                    case "sddefault.jpg":
                        sdDefaultImage = media;
                        break;
                    case "hqdefault.jpg":
                        hqDefaultImage = media;
                        break;
                    case "default.jpg":
                        defaultImage = media;
                        break;
                }
            }

            if (maxresDefaultImage != null)
                images.Add(maxresDefaultImage);
            if (mqDefaultImage != null)
                images.Add(mqDefaultImage);
            if (sdDefaultImage != null)
                images.Add(sdDefaultImage);
            if (hqDefaultImage != null)
                images.Add(hqDefaultImage);
            if (defaultImage != null)
                images.Add(defaultImage);

            // Download the thumbnail image
            GrabbedImage currentImage;

            while (images.Count() > 0)
            {
                currentImage = images[0];
                images.RemoveAt(0);

                var imageFileDownloadResult = await FileManager.DownloadBinaryDataToFile(targetFile, currentImage.ResourceUri, null);

                if (!imageFileDownloadResult.Key)
                {
                    if (imageFileDownloadResult.Value == 404)
                    {
                        // Try to download the next image in the list
                        continue;
                    }

                    return false;
                }

                return true;
            }

            return false;
        }
    }
}
