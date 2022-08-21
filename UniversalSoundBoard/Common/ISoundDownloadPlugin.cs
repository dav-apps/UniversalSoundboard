using System.Threading.Tasks;
using UniversalSoundboard.Models;

namespace UniversalSoundboard.Common
{
    public interface ISoundDownloadPlugin
    {
        string Url { get; }

        bool IsUrlMatch();
        Task<SoundDownloadResult> GetResult();
    }
}
