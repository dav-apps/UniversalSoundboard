using Sentry;
using System;
using System.Threading.Tasks;
using UniversalSoundboard.Pages;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.UI.Core;

namespace UniversalSoundboard.Models
{
    public class RecordedSoundItem
    {
        public Guid Uuid { get; set; }
        public string Name { get; set; }
        public StorageFile File { get; set; }
        public bool IsPlaying { get; private set; }

        private MediaPlayer audioPlayer;
        private MediaSource mediaSource;
        private bool isRemoved;

        public event EventHandler<EventArgs> AudioPlayerStarted;
        public event EventHandler<EventArgs> AudioPlayerPaused;
        public event EventHandler<EventArgs> Removed;

        public RecordedSoundItem(string name, StorageFile file)
        {
            Uuid = Guid.NewGuid();
            Name = name;
            File = file;
        }

        private async void AudioPlayer_MediaEnded(MediaPlayer sender, object args)
        {
            await MainPage.dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (isRemoved) return;
                if (Pause())
                    audioPlayer.PlaybackSession.Position = TimeSpan.Zero;
            });
        }

        private async void AudioPlayer_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
        {
            SentrySdk.CaptureException(args.ExtendedErrorCode ?? new Exception(args.ErrorMessage));
            await MainPage.dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (!isRemoved) Pause();
            });
        }

        public async Task<TimeSpan> GetDuration()
        {
            var properties = await File.Properties.GetMusicPropertiesAsync();
            return properties.Duration;
        }

        public Task<bool> Play()
        {
            if (isRemoved) return Task.FromResult(false);
            try
            {
                // Recording previews need no effects graph. MediaPlayer handles replay
                // and default output-device changes without rebuilding audio nodes.
                if (audioPlayer == null)
                {
                    audioPlayer = new MediaPlayer { AutoPlay = false };
                    audioPlayer.CommandManager.IsEnabled = false;
                    audioPlayer.MediaEnded += AudioPlayer_MediaEnded;
                    audioPlayer.MediaFailed += AudioPlayer_MediaFailed;
                    mediaSource = MediaSource.CreateFromStorageFile(File);
                    audioPlayer.Source = mediaSource;
                }
                audioPlayer.Play();
                IsPlaying = true;
            }
            catch (Exception e)
            {
                SentrySdk.CaptureException(e);
                return Task.FromResult(false);
            }

            AudioPlayerStarted?.Invoke(this, EventArgs.Empty);
            return Task.FromResult(true);
        }

        public bool Pause()
        {
            if (audioPlayer == null || isRemoved) return false;
            try
            {
                audioPlayer.Pause();
                IsPlaying = false;
            }
            catch (Exception e)
            {
                SentrySdk.CaptureException(e);
                return false;
            }

            AudioPlayerPaused?.Invoke(this, EventArgs.Empty);
            return true;
        }

        public async Task Remove()
        {
            if (isRemoved) return;
            isRemoved = true;
            IsPlaying = false;
            if (audioPlayer != null)
            {
                audioPlayer.MediaEnded -= AudioPlayer_MediaEnded;
                audioPlayer.MediaFailed -= AudioPlayer_MediaFailed;
                audioPlayer.Dispose();
                audioPlayer = null;
                mediaSource?.Dispose();
                mediaSource = null;
            }

            Removed?.Invoke(this, EventArgs.Empty);
            if (System.IO.File.Exists(File.Path))
                await File.DeleteAsync();
        }
    }
}
