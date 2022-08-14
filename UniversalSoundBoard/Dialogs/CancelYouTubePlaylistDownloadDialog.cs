using UniversalSoundboard.DataAccess;
using Windows.UI.Xaml.Controls;

namespace UniversalSoundboard.Dialogs
{
    public class CancelYouTubePlaylistDownloadDialog : Dialog
    {
        public CancelYouTubePlaylistDownloadDialog()
            : base(
                  FileManager.loader.GetString("CancelYouTubePlaylistDownloadDialog-Title"),
                  FileManager.loader.GetString("CancelYouTubePlaylistDownloadDialog-Message"),
                  FileManager.loader.GetString("Actions-StopDownload"),
                  FileManager.loader.GetString("Actions-ContinueDownload")
            )
        {
            ContentDialog.DefaultButton = ContentDialogButton.Close;
        }
    }
}
