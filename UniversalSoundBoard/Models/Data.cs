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
    
    public class PlayingSound
    {
        public Sound CurrentSound { get; set; }
        public List<Sound> Sounds { get; set; }
        public MediaPlayer MediaPlayer { get; set; }
        public int Repetitions { get; set; }
        public bool Randomly { get; set; }

        public PlayingSound()
        {
            Sounds = new List<Sound>();
            Repetitions = 0;
            Randomly = false;
        }

        public PlayingSound(Sound sound, MediaPlayer player)
        {
            Sounds = new List<Sound>();
            Sounds.Add(sound);

            CurrentSound = sound;
            MediaPlayer = player;
            Repetitions = 0;
            Randomly = false;
        }

        public PlayingSound(List<Sound> sounds, MediaPlayer player)
        {
            Sounds = new List<Sound>();
            foreach (Sound sound in sounds)
            {
                Sounds.Add(sound);
            }
            CurrentSound = sounds.First();
            MediaPlayer = player;
            Repetitions = 0;
            Randomly = false;
        }

        public PlayingSound(Sound sound, MediaPlayer player, int repetitions)
        {
            Sounds = new List<Sound>();
            Sounds.Add(sound);

            CurrentSound = sound;
            MediaPlayer = player;
            Repetitions = repetitions;
            Randomly = false;
        }

        public PlayingSound(List<Sound> sounds, MediaPlayer player, int repetitions, bool randomly)
        {
            Sounds = new List<Sound>();
            foreach (Sound sound in sounds)
            {
                Sounds.Add(sound);
            }
            CurrentSound = sounds.First();
            MediaPlayer = player;
            Repetitions = repetitions;
            Randomly = randomly;
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
