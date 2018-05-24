using davClassLibrary.Common;
using UniversalSoundBoard.DataAccess;

namespace UniversalSoundboard.Common
{
    public class RetrieveConstants : IRetrieveConstants
    {
        public string GetApiKey()
        {
            return FileManager.ApiKey;
        }

        public string GetDataPath()
        {
            return FileManager.GetDavDataPath();
        }

        public int GetAppId()
        {
            return FileManager.AppId;
        }
    }
}
