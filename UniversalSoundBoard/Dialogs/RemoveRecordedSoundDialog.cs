using UniversalSoundboard.DataAccess;
using Windows.UI.Xaml.Controls;

namespace UniversalSoundboard.Dialogs
{
    public class RemoveRecordedSoundDialog : Dialog
    {
        public RemoveRecordedSoundDialog(string recordedSoundName)
            : base(
                  string.Format(FileManager.loader.GetString("RemoveRecordedSoundDialog-Title"), recordedSoundName),
                  FileManager.loader.GetString("RemoveRecordedSoundDialog-Message"),
                  FileManager.loader.GetString("Actions-Remove"),
                  FileManager.loader.GetString("Actions-Cancel"),
                  ContentDialogButton.Close
            ) { }
    }
}
