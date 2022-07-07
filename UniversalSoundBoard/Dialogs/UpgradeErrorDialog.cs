using UniversalSoundboard.DataAccess;

namespace UniversalSoundboard.Dialogs
{
    internal class UpgradeErrorDialog : Dialog
    {
        public UpgradeErrorDialog()
            : base(
                  FileManager.loader.GetString("UpgradeErrorDialog-Title"),
                  FileManager.loader.GetString("UpgradeErrorDialog-Message"),
                  FileManager.loader.GetString("Actions-Close")
            ) { }
    }
}
