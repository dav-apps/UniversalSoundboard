using UniversalSoundboard.DataAccess;
using Windows.UI.Xaml.Controls;

namespace UniversalSoundboard.Dialogs
{
    public class LogoutDialog : Dialog
    {
        public LogoutDialog()
            : base(
                  FileManager.loader.GetString("Logout"),
                  FileManager.loader.GetString("Logout"),
                  FileManager.loader.GetString("Actions-Cancel"),
                  ContentDialogButton.Close
            )
        {
            Content = FileManager.loader.GetString("Account-LogoutMessage");
        }
    }
}
