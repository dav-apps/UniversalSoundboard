using System.Collections.Generic;

namespace UniversalSoundboard.Models
{
    public class SoundDownloadZopharPluginResult : SoundDownloadPluginResult
    {
        public string PlaylistTitle { get; set; }
        public string PlaylistImageUrl { get; set; }

        public SoundDownloadZopharPluginResult(
            string playlistTitle,
            string playlistImageUrl,
            List<SoundDownloadItem> soundItems
        ) : base(soundItems)
        {
            PlaylistTitle = playlistTitle;
            PlaylistImageUrl = playlistImageUrl;
        }
    }
}
