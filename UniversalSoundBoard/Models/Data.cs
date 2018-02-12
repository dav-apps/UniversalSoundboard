using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.Media.Playback;

namespace UniversalSoundBoard.Models
{
    public class Data
    {
        // For reading the categories list from the file
        public ObservableCollection<Category> Categories { get; set; }
    }

    public class Setting
    {
        // Class for the List below in the Hamburger menu
        public string Icon { get; set; }
        public string Text { get; set; }
        public string Id { get; set; }
    }

    public class PlayingSound
    {
        public Sound CurrentSound { get; set; }
        public List<Sound> Sounds { get; set; }
        public MediaPlayer MediaPlayer { get; set; }
        public int repetitions { get; set; }
        public bool randomly { get; set; }

        public PlayingSound()
        {
            Sounds = new List<Sound>();
            repetitions = 0;
            randomly = false;
        }

        public PlayingSound(Sound sound, MediaPlayer player)
        {
            Sounds = new List<Sound>();
            this.Sounds.Add(sound);

            CurrentSound = sound;
            this.MediaPlayer = player;
            repetitions = 0;
            randomly = false;
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
            randomly = false;
        }

        public PlayingSound(Sound sound, MediaPlayer player, int repetitions)
        {
            Sounds = new List<Sound>();
            this.Sounds.Add(sound);

            CurrentSound = sound;
            this.MediaPlayer = player;
            this.repetitions = repetitions;
            randomly = false;
        }

        public PlayingSound(List<Sound> sounds, MediaPlayer player, int repetitions, bool randomly)
        {
            Sounds = new List<Sound>();
            foreach (Sound sound in sounds)
            {
                this.Sounds.Add(sound);
            }
            CurrentSound = sounds.First();
            this.MediaPlayer = player;
            this.repetitions = repetitions;
            this.randomly = randomly;
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
