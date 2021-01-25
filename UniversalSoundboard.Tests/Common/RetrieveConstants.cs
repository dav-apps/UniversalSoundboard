using davClassLibrary.Common;
using System;
using System.Collections.Generic;
using UniversalSoundboard.DataAccess;

namespace UniversalSoundboard.Tests.Common
{
    class RetrieveConstants : IRetrieveConstants
    {
        public string GetApiKey()
        {
            return FileManager.ApiKey;
        }

        public int GetAppId()
        {
            return FileManager.AppId;
        }

        public string GetDataPath()
        {
            return FileManager.GetDavDataPath();
        }

        public List<int> GetParallelTableIds()
        {
            return new List<int>
            {
                FileManager.SoundTableId,
                FileManager.SoundFileTableId
            };
        }

        public List<int> GetTableIds()
        {
            return new List<int>
            {
                FileManager.OrderTableId,
                FileManager.CategoryTableId,
                FileManager.SoundTableId,
                FileManager.SoundFileTableId,
                FileManager.PlayingSoundTableId,
                FileManager.ImageFileTableId
            };
        }
    }
}
