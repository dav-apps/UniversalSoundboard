using UniversalSoundboard.DataAccess;

namespace UniversalSoundboard.Dialogs
{
    public class ShareErrorDialog : Dialog
    {
        public ShareErrorDialog()
        {
            ContentDialog.Title = FileManager.loader.GetString("ShareDialog-Title");
            ContentDialog.Content = FileManager.loader.GetString("ShareDialog-Error");
            ContentDialog.CloseButtonText = FileManager.loader.GetString("Actions-Close");
        }
    }
}
