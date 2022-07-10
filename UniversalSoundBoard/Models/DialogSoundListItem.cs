using System;

namespace UniversalSoundboard.Models
{
    public class DialogSoundListItem
    {
        public Sound Sound;

        public event EventHandler<EventArgs> RemoveButtonClick;

        public DialogSoundListItem(Sound sound)
        {
            Sound = sound;
        }

        public void TriggerRemoveButtonClickEvent()
        {
            RemoveButtonClick?.Invoke(this, EventArgs.Empty);
        }
    }
}
