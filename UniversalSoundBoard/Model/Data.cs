using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.Media;
using Windows.Media.Playback;

namespace UniversalSoundBoard.Model
{
    public class Data
    {
        public ObservableCollection<Category> Categories { get; set; }
    }

    public class Setting
    {
        public string Icon { get; set; }
        public string Text { get; set; }
        public string Id { get; set; }
    }

    public class PlayingSound
    {
        public Sound CurrentSound { get; set; }
        public List<Sound> Sounds { get; }
        public MediaPlayer MediaPlayer { get; set; }
        public int repetitions { get; set; }

        public PlayingSound()
        {
            Sounds = new List<Sound>();
            repetitions = 0;
        }

        public PlayingSound(Sound sound, MediaPlayer player)
        {
            Sounds = new List<Sound>();
            this.Sounds.Add(sound);

            CurrentSound = sound;
            this.MediaPlayer = player;
            repetitions = 0;
        }

        public PlayingSound(List<Sound> sounds, MediaPlayer player)
        {
            Sounds = new List<Sound>();
            foreach (Sound sound in sounds)
            {
                this.Sounds.Add(sound);
            }
            CurrentSound = sounds.First();
            this.MediaPlayer = player;
            repetitions = 0;
        }

        public PlayingSound(Sound sound, MediaPlayer player, int repetitions)
        {
            Sounds = new List<Sound>();
            this.Sounds.Add(sound);

            CurrentSound = sound;
            this.MediaPlayer = player;
            this.repetitions = repetitions;
        }

        public PlayingSound(List<Sound> sounds, MediaPlayer player, int repetitions)
        {
            Sounds = new List<Sound>();
            foreach (Sound sound in sounds)
            {
                this.Sounds.Add(sound);
            }
            CurrentSound = sounds.First();
            this.MediaPlayer = player;
            this.repetitions = repetitions;
        }

        public void AddSound(Sound sound)
        {
            Sounds.Add(sound);
        }

        public static PlayingSound GetPlayingSoundByMediaPlayer(MediaPlayer player)
        {
            foreach(PlayingSound playingSound in (App.Current as App)._itemViewHolder.playingSounds)
            {
                if(playingSound.MediaPlayer == player)
                {
                    return playingSound;
                }
            }
            return null;
        }
    }
}
