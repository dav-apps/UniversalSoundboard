using UniversalSoundboard.DataAccess;

namespace UniversalSoundboard.Dialogs
{
    public class UpgradePlusDialog : Dialog
    {
        public UpgradePlusDialog()
            : base(
                  new UpgradePlusContentDialog(),
                  FileManager.loader.GetString("Actions-Close")
            ) { }
    }
}
