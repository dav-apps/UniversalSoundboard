using davClassLibrary;
using davClassLibrary.Common;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Tests.Common;

namespace UniversalSoundboard.Tests
{
    internal class Utils
    {
        internal static void GlobalSetup()
        {
            ProjectInterface.LocalDataSettings = new LocalDataSettings();
            ProjectInterface.Callbacks = new Callbacks();
            FileManager.itemViewHolder = new UniversalSoundboard.Common.ItemViewHolder();

            Dav.Init(
                Environment.Test,
                FileManager.AppId,
                new List<int>
                {
                    FileManager.OrderTableId,
                    FileManager.CategoryTableId,
                    FileManager.SoundTableId,
                    FileManager.SoundFileTableId,
                    FileManager.PlayingSoundTableId,
                    FileManager.ImageFileTableId
                },
                new List<int>
                {
                    FileManager.SoundTableId,
                    FileManager.SoundFileTableId
                },
                FileManager.GetDavDataPath()
            );
        }

        internal static async Task Setup()
        {
            // Delete all files and folders in the test folder except the database file
            var davFolder = new DirectoryInfo(FileManager.GetDavDataPath());
            foreach (var folder in davFolder.GetDirectories())
                folder.Delete(true);

            // Clear the database
            var database = new davClassLibrary.DataAccess.DavDatabase();
            await database.DropAsync();
        }
    }
}
