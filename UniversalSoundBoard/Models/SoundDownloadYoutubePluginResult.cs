using System;

namespace UniversalSoundboard.Models
{
    public class SoundDownloadYoutubePluginResult : SoundDownloadPluginResult
    {
        public string Title { get; set; }
        public Uri ImageUrl { get; set; }
        public string PlaylistTitle { get; set; }

        public SoundDownloadYoutubePluginResult(string title, Uri imageUrl, string playlistTitle)
        {
            Title = title;
            ImageUrl = imageUrl;
            PlaylistTitle = playlistTitle;
        }
    }
}
