using UniversalSoundboard.DataAccess;

namespace UniversalSoundboard.Dialogs
{
    public class DavPlusOutputDeviceDialog : Dialog
    {
        public DavPlusOutputDeviceDialog()
            : base(
                  FileManager.loader.GetString("DavPlusContentDialog-Title"),
                  FileManager.loader.GetString("DavPlusOutputDeviceContentDialog-Content"),
                  FileManager.loader.GetString("Actions-LearnMore"),
                  FileManager.loader.GetString("Actions-Close")
            ) { }
    }
}
