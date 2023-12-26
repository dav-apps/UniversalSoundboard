using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace UniversalSoundboard.Models
{
    public class PlayingSound
    {
        public Guid Uuid { get; set; }
        public AudioPlayer AudioPlayer { get; set; }
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
        public bool LocalFile { get; set; }
        public bool FadeIn { get; set; }

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
            LocalFile = false;
            FadeIn = true;
        }

        public PlayingSound(AudioPlayer player, Sound sound)
        {
            AudioPlayer = player;
            Sounds = new ObservableCollection<Sound> { sound };
            Current = 0;
            Repetitions = 0;
            Randomly = false;
            Volume = 100;
            Muted = false;
            PlaybackSpeed = 100;
            StartPlaying = false;
            LocalFile = false;
            FadeIn = true;
        }

        public PlayingSound(Guid uuid, AudioPlayer player, List<Sound> sounds, int current, int repetitions, bool randomly)
        {
            Uuid = uuid;
            AudioPlayer = player;
            Sounds = new ObservableCollection<Sound>(sounds);
            Current = current;
            Repetitions = repetitions;
            Randomly = randomly;
            Volume = 100;
            Muted = false;
            PlaybackSpeed = 100;
            StartPlaying = false;
            LocalFile = false;
            FadeIn = true;
        }

        public PlayingSound(Guid uuid, AudioPlayer player, List<Sound> sounds, int current, int repetitions, bool randomly, int volume, bool muted, string outputDevice, int playbackSpeed)
        {
            Uuid = uuid;
            AudioPlayer = player;
            Sounds = new ObservableCollection<Sound>(sounds);
            Current = current;
            Repetitions = repetitions;
            Randomly = randomly;
            Volume = volume;
            Muted = muted;
            OutputDevice = outputDevice;
            PlaybackSpeed = playbackSpeed;
            StartPlaying = false;
            LocalFile = false;
            FadeIn = true;
        }
    }
}
