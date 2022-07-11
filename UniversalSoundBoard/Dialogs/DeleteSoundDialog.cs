using UniversalSoundboard.DataAccess;
using Windows.UI.Xaml.Controls;

namespace UniversalSoundboard.Dialogs
{
    public class DeleteSoundDialog : Dialog
    {
        public DeleteSoundDialog(string soundName)
            : base(
                  string.Format(FileManager.loader.GetString("DeleteSoundDialog-Title"), soundName),
                  FileManager.loader.GetString("DeleteSoundDialog-Content"),
                  FileManager.loader.GetString("Actions-Delete"),
                  FileManager.loader.GetString("Actions-Cancel"),
                  ContentDialogButton.Close
            )
        { }
    }
}
