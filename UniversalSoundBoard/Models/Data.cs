using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Windows.Media.Playback;

namespace UniversalSoundBoard.Models
{
    // The model for the old data.json file
    public class Data
    {
        public ObservableCollection<Category> Categories { get; set; }
    }
    
    public class PlayingSound
    {
        public string Uuid { get; set; }
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

        public PlayingSound(string uuid, List<Sound> sounds, MediaPlayer player, int repetitions, bool randomly, int current)
        {
            Uuid = uuid;
            Sounds = new List<Sound>();
            foreach (Sound sound in sounds)
            {
                Sounds.Add(sound);
            }

            try
            {
                CurrentSound = sounds[current];
            }
            catch(Exception e)
            {
                if(sounds.Count != 0)
                {
                    CurrentSound = sounds[0];
                }
            }
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

    // A representation of the database Sound table for exporting the Soundboard
    public class SoundData
    {
        public string Uuid { get; set; }
        public string Name { get; set; }
        public bool Favourite { get; set; }
        public string SoundExt { get; set; }
        public string ImageExt { get; set; }
        public string CategoryId { get; set; }
    }

    // The model for the new data.json file
    public class NewData
    {
        public List<SoundData> Sounds { get; set; }
        public List<Category> Categories { get; set; }
    }
}
