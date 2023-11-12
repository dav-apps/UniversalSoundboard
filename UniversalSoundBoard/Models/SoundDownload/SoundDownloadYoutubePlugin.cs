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
            return IsYoutubeUrl(Url) || IsShortYoutubeUrl(Url);
        }

        public static bool IsYoutubeUrl(string url)
        {
            Regex youtubeUrlRegex = new Regex("^(https?:\\/\\/)?((www|music).)?youtube.com\\/");
            return youtubeUrlRegex.IsMatch(url);
        }

        public static bool IsShortYoutubeUrl(string url)
        {
            Regex shortYoutubeUrlRegex = new Regex("^(https?:\\/\\/)?youtu.be\\/");
            return shortYoutubeUrlRegex.IsMatch(url);
        }

        public override async Task<SoundDownloadPluginResult> GetResult()
        {
            var grabber = GrabberBuilder.New().UseDefaultServices().AddYouTube().Build();
            GrabResult grabResult;
            string videoId = null;
            string playlistId = null;

            if (IsShortYoutubeUrl(Url))
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
            string imageUri = null;
            string playlistTitle = null;
            bool playlistLoadSuccessful = true;
            List<SoundDownloadItem> soundItems = new List<SoundDownloadItem>();

            var imageResources = grabResult.Resources<GrabbedImage>();
            GrabbedImage smallThumbnail = imageResources.ToList().Find(image => image.ResourceUri.ToString().Split('/').Last() == "default.jpg");

            if (smallThumbnail != null)
                imageUri = smallThumbnail.ResourceUri.ToString();

            try
            {
                if (playlistId != null)
                {
                    // Get the playlist
                    var listOperation = FileManager.youtubeService.PlaylistItems.List("contentDetails,snippet");
                    listOperation.PlaylistId = playlistId;
                    listOperation.MaxResults = 50;
                    listOperation.Fields = "nextPageToken,items(contentDetails/videoId,snippet/title)";

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
                            listOperation = FileManager.youtubeService.PlaylistItems.List("contentDetails,snippet");

                            listOperation.PlaylistId = playlistId;
                            listOperation.MaxResults = 50;
                            listOperation.PageToken = listResponse.NextPageToken;
                            listOperation.Fields = "nextPageToken,items(contentDetails/videoId,snippet/title)";

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
                                string playlistItemVideoId = playlistItem.ContentDetails.VideoId;
                                string videoTitle = playlistItem.Snippet.Title;
                                string videoUrl = string.Format("https://youtube.com/watch?v={0}", playlistItemVideoId);

                                soundItems.Add(
                                    new SoundDownloadYoutubeItem(
                                        videoTitle,
                                        videoUrl,
                                        videoUrl,
                                        videoUrl,
                                        "jpg",
                                        "m4a",
                                        0,
                                        0,
                                        videoId == playlistItemVideoId
                                    )
                                );
                            }
                        }
                    }
                }
                else
                {
                    soundItems.Add(
                        new SoundDownloadYoutubeItem(
                            grabResult.Title,
                            youtubeLink,
                            youtubeLink,
                            youtubeLink,
                            "jpg",
                            "m4a",
                            0,
                            0
                        )
                    );
                }
            }
            catch (Exception)
            {
                throw new SoundDownloadException();
            }

            return new SoundDownloadYoutubePluginResult(
                playlistTitle,
                imageUri,
                soundItems
            );
        }
    }
}
