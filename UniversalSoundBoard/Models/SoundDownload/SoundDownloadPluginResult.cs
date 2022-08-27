using System.Collections.Generic;

namespace UniversalSoundboard.Models
{
    public class SoundDownloadPluginResult
    {
        public List<SoundDownloadItem> SoundItems;

        public SoundDownloadPluginResult(List<SoundDownloadItem> soundItems)
        {
            SoundItems = soundItems;
        }
    }
}
