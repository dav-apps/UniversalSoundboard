using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
