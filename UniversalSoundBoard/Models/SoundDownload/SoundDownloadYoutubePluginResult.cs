using System.Collections.Generic;

namespace UniversalSoundboard.Models
{
    public class SoundDownloadYoutubePluginResult : SoundDownloadPluginResult
    {
        public string PlaylistTitle { get; set; }
        public string ImageUrl { get; set; }

        public SoundDownloadYoutubePluginResult(
            string playlistTitle,
            string imageUrl,
            List<SoundDownloadItem> soundItems
        ) : base(soundItems)
        {
            PlaylistTitle = playlistTitle;
            ImageUrl = imageUrl;
            SoundItems = soundItems;
        }
    }
}
