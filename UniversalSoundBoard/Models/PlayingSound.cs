using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.Media.Playback;

namespace UniversalSoundboard.Models
{
    public class PlayingSound
    {
        public Guid Uuid { get; set; }
        public MediaPlayer MediaPlayer { get; set; }
        public ObservableCollection<Sound> Sounds { get; set; }
        public int Current { get; set; }
        public int Repetitions { get; set; }
        public bool Randomly { get; set; }
        public int Volume { get; set; }
        public bool Muted { get; set; }
        public string OutputDevice { get; set; }
        public int PlaybackSpeed { get; set; }
        public bool StartPlaying { get; set; }
        public TimeSpan? StartPosition { get; set; }

        public PlayingSound()
        {
            Sounds = new ObservableCollection<Sound>();
            Current = 0;
            Repetitions = 0;
            Randomly = false;
            Volume = 100;
            Muted = false;
            PlaybackSpeed = 100;
            StartPlaying = false;
        }

        public PlayingSound(MediaPlayer player, Sound sound)
        {
            MediaPlayer = player;
            Sounds = new ObservableCollection<Sound> { sound };
            Current = 0;
            Repetitions = 0;
            Randomly = false;
            Volume = 100;
            Muted = false;
            PlaybackSpeed = 100;
            StartPlaying = false;
        }

        public PlayingSound(Guid uuid, MediaPlayer player, List<Sound> sounds, int current, int repetitions, bool randomly)
        {
            Uuid = uuid;
            MediaPlayer = player;
            Sounds = new ObservableCollection<Sound>(sounds);
            Current = current;
            Repetitions = repetitions;
            Randomly = randomly;
            Volume = 100;
            Muted = false;
            PlaybackSpeed = 100;
            StartPlaying = false;
        }

        public PlayingSound(Guid uuid, MediaPlayer player, List<Sound> sounds, int current, int repetitions, bool randomly, int volume, bool muted, string outputDevice, int playbackSpeed)
        {
            Uuid = uuid;
            MediaPlayer = player;
            Sounds = new ObservableCollection<Sound>(sounds);
            Current = current;
            Repetitions = repetitions;
            Randomly = randomly;
            Volume = volume;
            Muted = muted;
            OutputDevice = outputDevice;
            PlaybackSpeed = playbackSpeed;
            StartPlaying = false;
        }
    }
}
