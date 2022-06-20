﻿using System;
using System.Threading.Tasks;
using UniversalSoundboard.Pages;
using Windows.Storage;

namespace UniversalSoundboard.Models
{
    public class RecordedSoundItem
    {
        public Guid Uuid { get; set; }
        public string Name { get; set; }
        public StorageFile File { get; set; }
        public bool IsPlaying { get => audioPlayer.IsPlaying; }

        private AudioPlayer audioPlayer;

        public event EventHandler<EventArgs> AudioPlayerStarted;
        public event EventHandler<EventArgs> AudioPlayerPaused;
        public event EventHandler<EventArgs> Removed;

        public RecordedSoundItem(string name, StorageFile file)
        {
            Uuid = Guid.NewGuid();
            Name = name;
            File = file;

            audioPlayer = new AudioPlayer(file);
            audioPlayer.MediaEnded += AudioPlayer_MediaEnded;
        }

        public async Task<TimeSpan> GetDuration()
        {
            if (!audioPlayer.IsInitialized)
                await audioPlayer.Init();

            return audioPlayer.Duration;
        }

        public async Task Play()
        {
            if (!audioPlayer.IsInitialized)
                await audioPlayer.Init();

            audioPlayer.Play();
            AudioPlayerStarted?.Invoke(this, EventArgs.Empty);
        }

        public void Pause()
        {
            if (!audioPlayer.IsInitialized) return;

            audioPlayer.Pause();
            AudioPlayerPaused?.Invoke(this, EventArgs.Empty);
        }

        public async Task Remove()
        {
            if (audioPlayer.IsPlaying)
                audioPlayer.Pause();

            Removed?.Invoke(this, EventArgs.Empty);

            if (System.IO.File.Exists(File.Path))
                await File.DeleteAsync();
        }

        private async void AudioPlayer_MediaEnded(object sender, EventArgs e)
        {
            await MainPage.dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                audioPlayer.Pause();
                audioPlayer.Position = TimeSpan.Zero;
                AudioPlayerPaused?.Invoke(this, EventArgs.Empty);
            });
        }
    }
}
