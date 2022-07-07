using UniversalSoundboard.DataAccess;

namespace UniversalSoundboard.Dialogs
{
    internal class NoAudioDeviceDialog : Dialog
    {
        public NoAudioDeviceDialog()
            : base(
                  FileManager.loader.GetString("NoAudioDeviceDialog-Title"),
                  FileManager.loader.GetString("NoAudioDeviceDialog-Message"),
                  FileManager.loader.GetString("Actions-Close")
            ) { }
    }
}
