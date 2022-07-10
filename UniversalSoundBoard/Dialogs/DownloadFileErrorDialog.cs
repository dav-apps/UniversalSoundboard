using UniversalSoundboard.DataAccess;

namespace UniversalSoundboard.Dialogs
{
    public class DownloadFileErrorDialog : Dialog
    {
        public DownloadFileErrorDialog() : base()
        {
            ContentDialog.Title = FileManager.loader.GetString("DownloadFileErrorDialog-Title");
            ContentDialog.Content = FileManager.loader.GetString("DownloadFileErrorDialog-Message");
            ContentDialog.CloseButtonText = FileManager.loader.GetString("Actions-Close");
        }
    }
}
