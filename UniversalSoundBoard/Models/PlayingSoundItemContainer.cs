using System;

namespace UniversalSoundboard.Models
{
    public class PlayingSoundItemContainer
    {
        public int Index { get; }
        public PlayingSound PlayingSound { get; }
        public bool IsVisible { get; set; }
        public double ContentHeight { get; set; }

        public event EventHandler<EventArgs> Show;
        public event EventHandler<EventArgs> Hide;
        public event EventHandler<EventArgs> Loaded;

        public PlayingSoundItemContainer(int index, PlayingSound playingSound)
        {
            Index = index;
            PlayingSound = playingSound;
            IsVisible = true;
            ContentHeight = 0;
        }

        public void TriggerShowEvent(EventArgs args)
        {
            Show?.Invoke(this, args);
        }
        
        public void TriggerHideEvent(EventArgs args)
        {
            Hide?.Invoke(this, args);
        }

        public void TriggerLoadedEvent(EventArgs args)
        {
            Loaded?.Invoke(this, args);
        }
    }
}
