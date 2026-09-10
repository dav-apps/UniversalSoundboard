using Google.Apis.YouTube.v3.Data;
using Sentry;
using System;
using System.Collections.Generic;
using System.Linq;

using System.Threading.Tasks;
using System.Threading;
using UniversalSoundboard.Common;
using UniversalSoundboard.DataAccess;
using YoutubeExplode;

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
            return YoutubeUrl.IsYoutubeHost(url, false);
        }

        public static bool IsShortYoutubeUrl(string url)
        {
            return YoutubeUrl.IsYoutubeHost(url, true);
        }

        public override Task<SoundDownloadPluginResult> GetResult() => GetResult(CancellationToken.None);

        public async Task<SoundDownloadPluginResult> GetResult(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            // Incomplete input is normal while typing, not an exception to send to Sentry.
            if (!YoutubeUrl.TryParse(Url, out var videoId, out var playlistId))
                throw new SoundDownloadException();
            string title = null;
            string imageUri = null;

            // Build the url
            string youtubeLink = string.Format("https://youtube.com/watch?v={0}", videoId);

            try
            {
                var youtube = new YoutubeClient();
                var videoResult = await youtube.Videos.GetAsync(youtubeLink, cancellationToken);
                
                title = videoResult.Title;
                imageUri = videoResult.Thumbnails.Last().Url;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception e)
            {
                cancellationToken.ThrowIfCancellationRequested();
                SentrySdk.CaptureException(e, scope =>
                {
                    scope.SetTag("download.source", "youtube");
                });

                throw new SoundDownloadException();
            }

            string playlistTitle = null;
            bool playlistLoadSuccessful = true;
            List<SoundDownloadItem> soundItems = new List<SoundDownloadItem>();

            try
            {
                if (playlistId != null)
                {
                    // Get the playlist
                    var listOperation = FileManager.youtubeService.PlaylistItems.List("contentDetails,snippet");
                    listOperation.PlaylistId = playlistId;
                    listOperation.MaxResults = 50;
                    listOperation.Fields = "nextPageToken,items(contentDetails/videoId,snippet/title)";

                    PlaylistItemListResponse listResponse = await listOperation.ExecuteAsync(cancellationToken);

                    if (listResponse.Items.Count > 1)
                    {
                        // Get the name of the playlist
                        var playlistListOperation = FileManager.youtubeService.Playlists.List("snippet");
                        playlistListOperation.Id = playlistId;

                        try
                        {
                            var result = await playlistListOperation.ExecuteAsync(cancellationToken);

                            if (result.Items.Count > 0)
                                playlistTitle = result.Items[0].Snippet.Title;
                        }
                        catch (OperationCanceledException) { throw; }
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
                                listResponse = await listOperation.ExecuteAsync(cancellationToken);
                            }
                            catch (OperationCanceledException) { throw; }
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
                            title,
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
            catch (OperationCanceledException) { throw; }
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
