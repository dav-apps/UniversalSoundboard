namespace UniversalSoundboard.Models
{
    public class PlayingSoundItemContainer
    {
        public int Index { get; }
        public PlayingSound PlayingSound { get; }
        public bool IsVisible { get; set; }
        public double ContentHeight { get; set; }

        public PlayingSoundItemContainer(int index, PlayingSound playingSound)
        {
            Index = index;
            PlayingSound = playingSound;
            IsVisible = true;
            ContentHeight = 0;
        }
    }
}
