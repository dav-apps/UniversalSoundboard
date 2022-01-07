using System;
using Windows.Storage;

namespace UniversalSoundboard.Components
{
    public class SoundFileItem
    {
        public StorageFile File { get; set; }
        public string FileName = "";

        public event EventHandler<EventArgs> Removed;

        public SoundFileItem(StorageFile file)
        {
            File = file;
            FileName = file.Name;
        }

        public void TriggerRemovedEvent(EventArgs args)
        {
            Removed?.Invoke(this, args);
        }
    }
}
