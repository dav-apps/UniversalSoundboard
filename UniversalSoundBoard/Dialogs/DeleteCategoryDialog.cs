using UniversalSoundboard.DataAccess;
using Windows.UI.Xaml.Controls;

namespace UniversalSoundboard.Dialogs
{
    public class DeleteCategoryDialog : Dialog
    {
        public DeleteCategoryDialog(string categoryName)
            : base(
                  string.Format(FileManager.loader.GetString("DeleteCategoryDialog-Title"), categoryName),
                  FileManager.loader.GetString("DeleteCategoryDialog-Content"),
                  FileManager.loader.GetString("Actions-Delete"),
                  FileManager.loader.GetString("Actions-Cancel"),
                  ContentDialogButton.Close
            )
        { }
    }
}
