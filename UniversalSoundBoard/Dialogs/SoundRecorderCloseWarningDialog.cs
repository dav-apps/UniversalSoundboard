using UniversalSoundboard.DataAccess;
using Windows.UI.Xaml.Controls;

namespace UniversalSoundboard.Dialogs
{
    public class SoundRecorderCloseWarningDialog : Dialog
    {
        public SoundRecorderCloseWarningDialog()
            : base(
                  FileManager.loader.GetString("SoundRecorderCloseWarningDialog-Title"),
                  FileManager.loader.GetString("SoundRecorderCloseWarningDialog-Message"),
                  FileManager.loader.GetString("Actions-CloseWindow"),
                  FileManager.loader.GetString("Actions-Cancel"),
                  ContentDialogButton.Close
            ) { }
    }
}
