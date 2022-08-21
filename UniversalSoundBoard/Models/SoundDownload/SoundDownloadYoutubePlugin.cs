using DotNetTools.SharpGrabber;
using DotNetTools.SharpGrabber.Grabbed;
using Google.Apis.YouTube.v3.Data;
using Microsoft.AppCenter.Crashes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using UniversalSoundboard.Common;
using UniversalSoundboard.DataAccess;

namespace UniversalSoundboard.Models
{
    public class SoundDownloadYoutubePlugin : SoundDownloadPlugin
    {
        public SoundDownloadYoutubePlugin(string url) : base(url) { }

        public override bool IsUrlMatch()
        {
            return IsYoutubeUrl() || IsShortYoutubeUrl();
        }

        private bool IsYoutubeUrl()
        {
            Regex youtubeUrlRegex = new Regex("^(https?:\\/\\/)?((www|music).)?youtube.com\\/");
            return youtubeUrlRegex.IsMatch(Url);
        }

        private bool IsShortYoutubeUrl()
        {
            Regex shortYoutubeUrlRegex = new Regex("^(https?:\\/\\/)?youtu.be\\/");
            return shortYoutubeUrlRegex.IsMatch(Url);
        }

        public override async Task<SoundDownloadPluginResult> GetResult()
        {
            var grabber = GrabberBuilder.New().UseDefaultServices().AddYouTube().Build();
            string videoId = null;
            string playlistId = null;

            if (IsShortYoutubeUrl())
            {
                videoId = Url.Split('/').Last();
            }
            else
            {
                // Get the video id from the url params
                var queryDictionary = HttpUtility.ParseQueryString(Url.Split('?').Last());

                videoId = queryDictionary.Get("v");
                playlistId = queryDictionary.Get("list");
            }

            // Build the url
            string youtubeLink = string.Format("https://youtube.com/watch?v={0}", videoId);
            GrabResult grabResult;

            try
            {
                grabResult = await grabber.GrabAsync(new Uri(youtubeLink));

                if (grabResult == null)
                    throw new SoundDownloadException();
            }
            catch (Exception e)
            {
                Crashes.TrackError(e, new Dictionary<string, string>
                {
                    { "YoutubeLink", Url }
                });

                throw new SoundDownloadException();
            }

            string title = grabResult.Title;
            Uri imageUri = null;
            string playlistTitle = null;
            bool playlistLoadSuccessful = true;
            List<SoundDownloadListItem> soundItems = new List<SoundDownloadListItem>();

            var imageResources = grabResult.Resources<GrabbedImage>();
            GrabbedImage smallThumbnail = imageResources.ToList().Find(image => image.ResourceUri.ToString().Split('/').Last() == "default.jpg");

            if (smallThumbnail != null)
                imageUri = smallThumbnail.ResourceUri;

            try
            {
                if (playlistId != null)
                {
                    // Get the playlist
                    var listOperation = FileManager.youtubeService.PlaylistItems.List("contentDetails");
                    listOperation.PlaylistId = playlistId;
                    listOperation.MaxResults = 50;
                    PlaylistItemListResponse listResponse = await listOperation.ExecuteAsync();

                    if (listResponse.Items.Count > 1)
                    {
                        // Get the name of the playlist
                        var playlistListOperation = FileManager.youtubeService.Playlists.List("snippet");
                        playlistListOperation.Id = playlistId;

                        try
                        {
                            var result = await playlistListOperation.ExecuteAsync();

                            if (result.Items.Count > 0)
                                playlistTitle = result.Items[0].Snippet.Title;
                        }
                        catch (Exception) { }

                        // Load all items from all pages of the playlist
                        List<PlaylistItem> playlistItems = new List<PlaylistItem>();

                        foreach (var item in listResponse.Items)
                            playlistItems.Add(item);

                        while (listResponse.NextPageToken != null)
                        {
                            // Get the next page of the playlist
                            listOperation = FileManager.youtubeService.PlaylistItems.List("contentDetails");

                            listOperation.PlaylistId = playlistId;
                            listOperation.MaxResults = 50;
                            listOperation.PageToken = listResponse.NextPageToken;

                            try
                            {
                                listResponse = await listOperation.ExecuteAsync();
                            }
                            catch (Exception)
                            {
                                playlistLoadSuccessful = false;
                                break;
                            }

                            foreach (var item in listResponse.Items)
                                playlistItems.Add(item);
                        }

                        if (playlistLoadSuccessful)
                        {
                            // Add the playlist items to the sound items list of the result
                            foreach (var playlistItem in playlistItems)
                            {
                                string videoLink = string.Format("https://youtube.com/watch?v={0}", playlistItem.ContentDetails.VideoId);

                                grabResult = await grabber.GrabAsync(new Uri(videoLink));

                                if (grabResult == null)
                                {
                                    playlistLoadSuccessful = false;
                                    break;
                                }

                                var mediaResources = grabResult.Resources<GrabbedMedia>();
                                imageResources = grabResult.Resources<GrabbedImage>();

                                // Find the audio track with the highest sample rate
                                Uri bestAudio = FindBestAudioOfYoutubeVideo(mediaResources);

                                if (bestAudio == null)
                                {
                                    playlistLoadSuccessful = false;
                                    break;
                                }

                                var imageUrls = FindThumbnailImagesOfYoutubeVideo(imageResources);

                                soundItems.Add(new SoundDownloadListItem(grabResult.Title, bestAudio, imageUrls));
                            }
                        }
                    }
                }
            }
            catch (Exception) { }

            return new SoundDownloadYoutubePluginResult(
                grabResult.Title,
                imageUri,
                playlistTitle,
                playlistLoadSuccessful ? soundItems : new List<SoundDownloadListItem>()
            );
        }

        private Uri FindBestAudioOfYoutubeVideo(IEnumerable<GrabbedMedia> mediaResources)
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

            return bestAudio.ResourceUri;
        }

        private List<Uri> FindThumbnailImagesOfYoutubeVideo(IEnumerable<GrabbedImage> imageResources)
        {
            // Find the thumbnail image with the highest resolution
            List<Uri> images = new List<Uri>();
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
                images.Add(maxresDefaultImage.ResourceUri);
            if (mqDefaultImage != null)
                images.Add(mqDefaultImage.ResourceUri);
            if (sdDefaultImage != null)
                images.Add(sdDefaultImage.ResourceUri);
            if (hqDefaultImage != null)
                images.Add(hqDefaultImage.ResourceUri);
            if (defaultImage != null)
                images.Add(defaultImage.ResourceUri);

            return images;
        }
    }
}
