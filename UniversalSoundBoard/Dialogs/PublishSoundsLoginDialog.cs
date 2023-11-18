using UniversalSoundboard.DataAccess;
using Windows.UI.Xaml.Controls;

namespace UniversalSoundboard.Dialogs
{
    public class PublishSoundsLoginDialog : Dialog
    {
        public PublishSoundsLoginDialog()
            : base(
                  FileManager.loader.GetString("PublishSoundsLoginDialog-Title"),
                  FileManager.loader.GetString("Actions-Login"),
                  FileManager.loader.GetString("Actions-Cancel"),
                  ContentDialogButton.Primary
            )
        {
            Content = FileManager.loader.GetString("PublishSoundsLoginDialog-Description");
        }
    }
}
