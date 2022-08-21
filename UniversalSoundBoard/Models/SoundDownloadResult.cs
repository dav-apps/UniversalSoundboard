using System;
using System.Collections.Generic;

namespace UniversalSoundboard.Models
{
    public class SoundDownloadResult
    {
        public string Title { get; }
        public Uri ImageUrl { get; }
        public string PlaylistTitle { get; }
        public List<SoundDownloadListItem> SoundItems { get; }

        public SoundDownloadResult(string title, Uri imageUrl, string playlistTitle, List<SoundDownloadListItem> soundItems)
        {
            Title = title;
            ImageUrl = imageUrl;
            PlaylistTitle = playlistTitle;
            SoundItems = soundItems;
        }
    }
}
