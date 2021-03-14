using davClassLibrary.Common;
using davClassLibrary.Models;
using System;

namespace UniversalSoundboard.Tests.Common
{
    class Callbacks : ICallbacks
    {
        public void DeleteTableObject(Guid uuid, int tableId) { }

        public void UpdateAllOfTable(int tableId, bool changed) { }

        public void UpdateTableObject(TableObject tableObject, bool fileDownloaded) { }

        public void TableObjectDownloadProgress(Guid uuid, int progress) { }

        public void UserSyncFinished() { }

        public void SyncFinished() { }
    }
}
