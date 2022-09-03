using UniversalSoundboard.DataAccess;

namespace UniversalSoundboard.Dialogs
{
    public class DavPlusSoundRecorderDialog : Dialog
    {
        public DavPlusSoundRecorderDialog()
            : base(
                  FileManager.loader.GetString("DavPlusDialog-Title"),
                  FileManager.loader.GetString("DavPlusSoundRecorderDialog-Content"),
                  FileManager.loader.GetString("Actions-LearnMore"),
                  FileManager.loader.GetString("Actions-Close")
            ) { }
    }
}
