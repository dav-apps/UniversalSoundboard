using UniversalSoundboard.DataAccess;

namespace UniversalSoundboard.Dialogs
{
    public class AddSoundErrorDialog : Dialog
    {
        public AddSoundErrorDialog()
            : base(
                  FileManager.loader.GetString("AddSoundErrorDialog-Title"),
                  FileManager.loader.GetString("Actions-Close")
            )
        {
            Content = FileManager.loader.GetString("AddSoundErrorDialog-Content");
        }
    }
}
