using UniversalSoundboard.DataAccess;
using Windows.UI.Xaml.Controls;

namespace UniversalSoundboard.Dialogs
{
    public class DeleteSoundsDialog : Dialog
    {
        public DeleteSoundsDialog()
            : base(
                  FileManager.loader.GetString("DeleteSoundsDialog-Title"),
                  FileManager.loader.GetString("DeleteSoundsDialog-Content"),
                  FileManager.loader.GetString("Actions-Delete"),
                  FileManager.loader.GetString("Actions-Cancel"),
                  ContentDialogButton.Close
            ) { }
    }
}
