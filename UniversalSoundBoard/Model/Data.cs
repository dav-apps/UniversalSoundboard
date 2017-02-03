using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.Media.Playback;

namespace UniversalSoundBoard.Model
{
    public class Data
    {
        public List<Category> Categories { get; set; }
    }

    public class Setting
    {
        public string Icon { get; set; }
        public string Text { get; set; }
        public string Id { get; set; }
    }

    public class PlayingSound
    {
        public Sound Sound { get; set; }
        public MediaPlayer Player { get; set; }

        public PlayingSound()
        {

        }

        public PlayingSound(Sound sound, MediaPlayer player)
        {
            this.Sound = sound;
            this.Player = player;
        }

        public static PlayingSound GetPlayingSoundByMediaPlayer(MediaPlayer player)
        {
            foreach(PlayingSound playingSound in (App.Current as App)._itemViewHolder.playingSounds)
            {
                if(playingSound.Player == player)
                {
                    return playingSound;
                }
            }
            return null;
        }
    }
}
