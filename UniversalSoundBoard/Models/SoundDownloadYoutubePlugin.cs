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

        public override async Task<SoundDownloadResult> GetResult()
        {
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
                var grabber = GrabberBuilder.New().UseDefaultServices().AddYouTube().Build();
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
                    }
                }
            }
            catch (Exception) { }

            return new SoundDownloadResult(grabResult.Title, imageUri, playlistTitle);
        }
    }
}
