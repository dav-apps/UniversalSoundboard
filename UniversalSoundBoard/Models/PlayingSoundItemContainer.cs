namespace UniversalSoundboard.Models
{
    public class PlayingSoundItemContainer
    {
        public int Index { get; }
        public PlayingSound PlayingSound { get; }

        public PlayingSoundItemContainer(int index, PlayingSound playingSound)
        {
            Index = index;
            PlayingSound = playingSound;
        }
    }
}
