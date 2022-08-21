using System.Threading.Tasks;
using UniversalSoundboard.Common;

namespace UniversalSoundboard.Models
{
    public abstract class SoundDownloadPlugin : ISoundDownloadPlugin
    {
        public string Url { get; }

        public SoundDownloadPlugin(string url)
        {
            Url = url;
        }

        public abstract bool IsUrlMatch();

        public abstract Task<SoundDownloadPluginResult> GetResult();
    }
}
