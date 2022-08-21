using System;
using System.Collections.Generic;

namespace UniversalSoundboard.Models
{
    public class SoundDownloadYoutubePluginResult : SoundDownloadPluginResult
    {
        public string Title { get; set; }
        public Uri ImageUrl { get; set; }
        public string PlaylistTitle { get; set; }
        public List<SoundDownloadListItem> SoundItems { get; set; }

        public SoundDownloadYoutubePluginResult(string title, Uri imageUrl, string playlistTitle, List<SoundDownloadListItem> soundItems)
        {
            Title = title;
            ImageUrl = imageUrl;
            PlaylistTitle = playlistTitle;
            SoundItems = soundItems;
        }
    }
}
