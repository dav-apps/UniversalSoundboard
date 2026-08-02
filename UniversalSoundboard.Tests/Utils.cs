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
                Constants.AppId,
                new List<string>
                {
                    Constants.OrderTableName,
                    Constants.CategoryTableName,
                    Constants.SoundFileTableName,
                    Constants.SoundTableName,
                    Constants.PlayingSoundTableName,
                    Constants.ImageFileTableName
                },
                new List<string>
                {
                    Constants.SoundFileTableName,
                    Constants.SoundTableName
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
