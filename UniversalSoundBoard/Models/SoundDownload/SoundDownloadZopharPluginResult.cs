using System.Collections.Generic;

namespace UniversalSoundboard.Models
{
    public class SoundDownloadZopharPluginResult : SoundDownloadPluginResult
    {
        public string PlaylistTitle { get; set; }
        public List<SoundDownloadListItem> SoundItems { get; set; }

        public SoundDownloadZopharPluginResult(string playlistTitle, List<SoundDownloadListItem> soundItems)
        {
            PlaylistTitle = playlistTitle;
            SoundItems = soundItems;
        }
    }
}
