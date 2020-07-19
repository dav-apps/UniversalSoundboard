using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UniversalSoundBoard.DataAccess;
using UniversalSoundBoard.Models;
using Windows.Media.Playback;

namespace UniversalSoundboard.Models
{
    public class PlayingSound
    {
        public Guid Uuid { get; set; }
        public ObservableCollection<Sound> Sounds { get; set; }
        public MediaPlayer MediaPlayer { get; set; }
        public int Repetitions { get; set; }
        public bool Randomly { get; set; }
        public int Current { get; set; }

        public PlayingSound()
        {
            Sounds = new ObservableCollection<Sound>();
            Repetitions = 0;
            Randomly = false;
            Current = 0;
        }

        public PlayingSound(Sound sound, MediaPlayer player)
        {
            Sounds = new ObservableCollection<Sound>
            {
                sound
            };

            MediaPlayer = player;
            Repetitions = 0;
            Randomly = false;
            Current = 0;
        }

        public PlayingSound(List<Sound> sounds, MediaPlayer player)
        {
            Sounds = new ObservableCollection<Sound>();
            foreach (Sound sound in sounds)
                Sounds.Add(sound);

            MediaPlayer = player;
            Repetitions = 0;
            Randomly = false;
            Current = 0;
        }

        public PlayingSound(Sound sound, MediaPlayer player, int repetitions)
        {
            Sounds = new ObservableCollection<Sound>
            {
                sound
            };

            MediaPlayer = player;
            Repetitions = repetitions;
            Randomly = false;
        }

        public PlayingSound(Guid uuid, List<Sound> sounds, MediaPlayer player, int repetitions, bool randomly, int current)
        {
            Uuid = uuid;
            Sounds = new ObservableCollection<Sound>();
            foreach (Sound sound in sounds)
                Sounds.Add(sound);

            MediaPlayer = player;
            Repetitions = repetitions;
            Randomly = randomly;
            Current = current;
        }

        public void AddSound(Sound sound)
        {
            Sounds.Add(sound);
        }

        public static PlayingSound GetPlayingSoundByMediaPlayer(MediaPlayer player)
        {
            foreach (PlayingSound playingSound in FileManager.itemViewHolder.PlayingSounds)
                if (playingSound.MediaPlayer == player)
                    return playingSound;

            return null;
        }
    }
}
