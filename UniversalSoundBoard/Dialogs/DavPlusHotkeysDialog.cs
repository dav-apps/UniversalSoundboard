using UniversalSoundboard.DataAccess;

namespace UniversalSoundboard.Dialogs
{
    public class DavPlusHotkeysDialog : Dialog
    {
        public DavPlusHotkeysDialog()
            : base(
                  FileManager.loader.GetString("DavPlusDialog-Title"),
                  FileManager.loader.GetString("DavPlusHotkeysDialog-Content"),
                  FileManager.loader.GetString("Actions-LearnMore"),
                  FileManager.loader.GetString("Actions-Close")
            ) { }
    }
}
