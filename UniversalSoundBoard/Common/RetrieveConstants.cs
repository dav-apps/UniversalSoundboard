using System.Collections.Generic;
using davClassLibrary.Common;
using UniversalSoundboard.DataAccess;

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

        public List<int> GetTableIds()
        {
            return new List<int>
            {
                FileManager.OrderTableId,
                FileManager.CategoryTableId,
                FileManager.SoundFileTableId,
                FileManager.SoundTableId,
                FileManager.PlayingSoundTableId,
                FileManager.ImageFileTableId
            };
        }

        public List<int> GetParallelTableIds()
        {
            return new List<int>
            {
                FileManager.SoundFileTableId,
                FileManager.SoundTableId
            };
        }
    }
}
