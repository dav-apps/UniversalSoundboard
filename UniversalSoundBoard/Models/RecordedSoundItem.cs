using Microsoft.AppCenter.Crashes;
using System;
using System.Threading.Tasks;
using UniversalSoundboard.Common;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Pages;
using Windows.Storage;
using Windows.UI.Core;

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

            FileManager.deviceWatcherHelper.DevicesChanged += DeviceWatcherHelper_DevicesChanged;
        }

        private async void AudioPlayer_MediaEnded(object sender, EventArgs e)
        {
            await MainPage.dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (Pause())
                    audioPlayer.Position = TimeSpan.Zero;
            });
        }

        private async void DeviceWatcherHelper_DevicesChanged(object sender, EventArgs args)
        {
            await MainPage.dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                if (
                    !audioPlayer.IsInitialized
                    && FileManager.deviceWatcherHelper.Devices.Count > 0
                )
                {
                    try
                    {
                        // Init the audio player
                        await audioPlayer.Init();
                    }
                    catch (AudioIOException e)
                    {
                        Crashes.TrackError(e);
                    }
                }
            });
        }

        public async Task<TimeSpan> GetDuration()
        {
            if (!audioPlayer.IsInitialized)
            {
                try
                {
                    await audioPlayer.Init();
                }
                catch (AudioIOException e)
                {
                    Crashes.TrackError(e);
                }
            }

            return audioPlayer.Duration;
        }

        public async Task<bool> Play()
        {
            if (!audioPlayer.IsInitialized)
            {
                try
                {
                    await audioPlayer.Init();
                }
                catch (AudioIOException e)
                {
                    Crashes.TrackError(e);
                    return false;
                }
            }

            try
            {
                audioPlayer.Play();
            }
            catch(AudioIOException e)
            {
                Crashes.TrackError(e);
                return false;
            }

            AudioPlayerStarted?.Invoke(this, EventArgs.Empty);
            return true;
        }

        public bool Pause()
        {
            if (!audioPlayer.IsInitialized) return false;

            try
            {
                audioPlayer.Pause();
            }
            catch (AudioIOException e)
            {
                Crashes.TrackError(e);
                return false;
            }
            
            AudioPlayerPaused?.Invoke(this, EventArgs.Empty);
            return true;
        }

        public async Task Remove()
        {
            if (audioPlayer.IsPlaying)
                audioPlayer.Pause();

            Removed?.Invoke(this, EventArgs.Empty);

            if (System.IO.File.Exists(File.Path))
                await File.DeleteAsync();
        }
    }
}
