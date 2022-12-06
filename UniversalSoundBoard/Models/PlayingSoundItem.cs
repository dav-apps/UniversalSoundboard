using davClassLibrary;
using Microsoft.AppCenter.Analytics;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using UniversalSoundboard.Common;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Pages;
using Windows.Devices.Enumeration;
using Windows.Media.Audio;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace UniversalSoundboard.Models
{
    /**
     * Base class for PlayingSoundItems that are displayed in the PlayingSoundsBar and BottomPlayingSoundsBar
     * 
     * Each PlayingSoundItemTemplate is assigned to a PlayingSoundItem class, so that all logic runs in the PlayingSoundItem
     * and the PlayingSoundItemTemplate only updates the UI.
     * Each PlayingSoundItem can belong to multiple (2 for both lists) PlayingSoundItemTemplates.
     * 
     * The PlayingSoundItems are stored as a list in ItemViewHolder. Whenever a new PlayingSoundItemTemplate is initialized,
     * it will check if a PlayingSoundItem already exists for the given PlayingSound in the list.
     * If so, the PlayingSoundItemTemplate will subscribe to the PlayingSoundItem and will receive all UI updates.
     * If not, the PlayingSoundItemTemplate will create a new PlayingSoundItem and subscribe to it.
     */
    public class PlayingSoundItem
    {
        public PlayingSound PlayingSound { get; private set; }
        public Guid Uuid { get => PlayingSound == null ? Guid.Empty : PlayingSound.Uuid; }
        public bool CurrentSoundIsDownloading { get => currentSoundIsDownloading; }
        public TimeSpan CurrentSoundTotalDuration { get => currentSoundTotalDuration; }
        private Sound CurrentSound
        {
            get
            {
                int current = PlayingSound.Current;

                if (current < 0)
                    current = 0;
                else if (current >= PlayingSound.Sounds.Count)
                    current = PlayingSound.Sounds.Count - 1;

                return PlayingSound.Sounds.ElementAt(current);
            }
        }

        #region Local variables
        private bool initialized = false;
        private bool skipSoundsCollectionChanged = false;
        private TimeSpan currentSoundTotalDuration = TimeSpan.Zero;
        private bool currentSoundIsDownloading = false;
        private readonly List<(Guid, int)> DownloadProgressList = new List<(Guid, int)>();
        private bool removed = false;
        private bool updateOutputDeviceRunning = false;
        private bool runUpdateOutputDeviceAgain = false;

        private Timer fadeOutTimer;
        private const int fadeOutTime = 300;
        private const int fadeOutFrames = 10;
        private int currentFadeOutFrame = 0;
        private double fadeOutVolumeDiff;

        private DispatcherTimer positionChangeTimer;
        private TimeSpan position;
        #endregion

        #region Events
        public event EventHandler<PlaybackStateChangedEventArgs> PlaybackStateChanged;
        public event EventHandler<PositionChangedEventArgs> PositionChanged;
        public event EventHandler<DurationChangedEventArgs> DurationChanged;
        public event EventHandler<CurrentSoundChangedEventArgs> CurrentSoundChanged;
        public event EventHandler<ButtonVisibilityChangedEventArgs> ButtonVisibilityChanged;
        public event EventHandler<LocalFileButtonVisibilityEventArgs> LocalFileButtonVisibilityChanged;
        public event EventHandler<OutputDeviceButtonVisibilityEventArgs> OutputDeviceButtonVisibilityChanged;
        public event EventHandler<RepetitionsChangedEventArgs> RepetitionsChanged;
        public event EventHandler<FavouriteChangedEventArgs> FavouriteChanged;
        public event EventHandler<VolumeChangedEventArgs> VolumeChanged;
        public event EventHandler<MutedChangedEventArgs> MutedChanged;
        public event EventHandler<PlaybackSpeedChangedEventArgs> PlaybackSpeedChanged;
        public event EventHandler<EventArgs> RemovePlayingSound;
        public event EventHandler<DownloadStatusChangedEventArgs> DownloadStatusChanged;
        public event EventHandler<EventArgs> CollapseSoundsList;
        #endregion

        public PlayingSoundItem(PlayingSound playingSound)
        {
            PlayingSound = playingSound;
        }

        public static PlayingSoundItem Subscribe(PlayingSound playingSound)
        {
            PlayingSoundItem playingSoundItem;

            // Try to find the appropriate PlayingSoundItem
            int i = FileManager.itemViewHolder.PlayingSoundItems.FindIndex(item => item.Uuid.Equals(playingSound.Uuid));

            if (i == -1)
            {
                // Create a new PlayingSoundItem
                playingSoundItem = new PlayingSoundItem(playingSound);
                FileManager.itemViewHolder.PlayingSoundItems.Add(playingSoundItem);
            }
            else
            {
                // Subscribe to the PlayingSoundItem
                playingSoundItem = FileManager.itemViewHolder.PlayingSoundItems.ElementAt(i);
            }

            return playingSoundItem;
        }

        public async void Init()
        {
            if (
                initialized
                || PlayingSound.AudioPlayer == null
            )
            {
                UpdateUI();
                return;
            }

            initialized = true;

            // Subscribe to ItemViewHolder events
            FileManager.itemViewHolder.PropertyChanged += ItemViewHolder_PropertyChanged;
            FileManager.itemViewHolder.SoundDeleted += ItemViewHolder_SoundDeleted;

            // Subscribe to MediaPlayer events
            PlayingSound.AudioPlayer.MediaEnded += AudioPlayer_MediaEnded;
            PlayingSound.AudioPlayer.UnrecoverableErrorOccurred += AudioPlayer_UnrecoverableErrorOccurred;

            // Subscribe to other events
            PlayingSound.Sounds.CollectionChanged += Sounds_CollectionChanged;
            FileManager.deviceWatcherHelper.DevicesChanged += DeviceWatcherHelper_DevicesChanged;

            // Set the initial UI element states
            UpdateUI();

            // Set the appropriate output device
            await UpdateOutputDevice();
            await InitAudioPlayer();
            InitPositionChangeTimer();

            if (Dav.IsLoggedIn && !PlayingSound.LocalFile)
            {
                // Add each sound file that is not downloaded to the download queue
                List<int> fileDownloads = new List<int>();

                for (int i = 0; i < PlayingSound.Sounds.Count; i++)
                {
                    // Check the download status of the file
                    if (PlayingSound.Sounds[i].AudioFileTableObject.FileDownloadStatus == TableObjectFileDownloadStatus.Downloaded)
                        continue;

                    if (i == 0)
                    {
                        fileDownloads.Add(PlayingSound.Current);
                        continue;
                    }

                    // First, add the next sound
                    int nextSoundIndex = PlayingSound.Current + i;
                    if (nextSoundIndex < PlayingSound.Sounds.Count)
                        fileDownloads.Add(nextSoundIndex);

                    // Second, add the previous sound
                    int previousSoundIndex = PlayingSound.Current - i;
                    if (previousSoundIndex >= 0)
                        fileDownloads.Add(previousSoundIndex);
                }

                fileDownloads.Reverse();

                foreach (int i in fileDownloads)
                    AddSoundToDownloadQueue(i);

                if (CheckFileDownload())
                {
                    await SetPlayPause(false);
                }
                else if (PlayingSound.StartPlaying)
                {
                    await SetPlayPause(true);
                }
            }
            else if (PlayingSound.StartPlaying)
            {
                await SetPlayPause(true);
            }
        }

        #region ItemViewHolder event handlers
        private async void ItemViewHolder_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (PlayingSound == null || PlayingSound.AudioPlayer == null) return;

            if (e.PropertyName == ItemViewHolder.VolumeKey)
                PlayingSound.AudioPlayer.Volume = (double)PlayingSound.Volume / 100 * FileManager.itemViewHolder.Volume / 100;
            else if (e.PropertyName == ItemViewHolder.MutedKey)
                PlayingSound.AudioPlayer.IsMuted = PlayingSound.Muted || FileManager.itemViewHolder.Muted;
            else if (e.PropertyName == ItemViewHolder.UseStandardOutputDeviceKey || e.PropertyName == ItemViewHolder.OutputDeviceKey)
                await UpdateOutputDevice();
        }

        private void ItemViewHolder_SoundDeleted(object sender, SoundEventArgs e)
        {
            // Find the deleted sound in the Sounds list of the PlayingSound
            int i = PlayingSound.Sounds.ToList().FindIndex(s => s.Uuid.Equals(e.Uuid));
            if (i == -1) return;

            // Remove the sound from the list
            PlayingSound.Sounds.RemoveAt(i);
        }
        #endregion
        
        #region AudioPlayer event handlers
        private async void AudioPlayer_MediaEnded(object sender, EventArgs e)
        {
            await MainPage.dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                // Check if the end of the sounds list was reached
                if (PlayingSound.Current + 1 < PlayingSound.Sounds.Count)
                {
                    // Move to the next sound
                    await MoveToSound(PlayingSound.Current + 1, true);
                }
                else
                {
                    // Move to the beginning of the sounds list
                    if (PlayingSound.Repetitions != int.MaxValue)
                        PlayingSound.Repetitions--;

                    if (PlayingSound.Repetitions < 0)
                    {
                        // Remove the PlayingSound
                        await TriggerRemove();
                        return;
                    }

                    // Update the UI
                    RepetitionsChanged?.Invoke(
                        this,
                        new RepetitionsChangedEventArgs(PlayingSound.Repetitions)
                    );

                    // Set the new repetitions
                    await FileManager.SetRepetitionsOfPlayingSoundAsync(PlayingSound.Uuid, PlayingSound.Repetitions);

                    if (PlayingSound.Sounds.Count > 1)
                    {
                        // If randomly is true, shuffle sounds
                        if (PlayingSound.Randomly)
                        {
                            Random random = new Random();
                            skipSoundsCollectionChanged = true;
                            int soundsCount = PlayingSound.Sounds.Count;

                            // Copy the sounds
                            List<Sound> sounds = new List<Sound>();
                            foreach (var sound in PlayingSound.Sounds)
                                sounds.Add(sound);

                            PlayingSound.Sounds.Clear();

                            // Add the sounds in random order
                            for (int i = 0; i < soundsCount; i++)
                            {
                                // Take a random sound from the sounds list and add it to the original sounds list
                                int randomIndex = random.Next(sounds.Count);

                                PlayingSound.Sounds.Add(sounds.ElementAt(randomIndex));
                                sounds.RemoveAt(randomIndex);
                            }

                            // Set the new sound order
                            await FileManager.SetSoundsListOfPlayingSoundAsync(PlayingSound.Uuid, PlayingSound.Sounds.ToList());

                            skipSoundsCollectionChanged = false;
                        }

                        await MoveToSound(0, true);
                    }
                    else
                    {
                        PlayingSound.AudioPlayer.Position = TimeSpan.Zero;
                        PlayingSound.AudioPlayer.PlaybackRate = (double)PlayingSound.PlaybackSpeed / 100;

                        await SetPlayPause(true);
                    }
                }
            });

            Analytics.TrackEvent("PlayingSoundItem-MediaEnded", new Dictionary<string, string>
            {
                { "Multiple sounds", (PlayingSound.Sounds.Count > 1).ToString() },
                { "Number of sounds", PlayingSound.Sounds.Count.ToString() },
                { "Local file", PlayingSound.LocalFile.ToString() }
            });
        }

        private async void AudioPlayer_PositionChanged(object sender, PositionChangedEventArgs e)
        {
            await MainPage.dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                PositionChanged?.Invoke(this, new PositionChangedEventArgs((sender as AudioPlayer).Position));
            });
        }

        private async void AudioPlayer_UnrecoverableErrorOccurred(object sender, AudioGraphUnrecoverableErrorOccurredEventArgs e)
        {
            await MainPage.dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                await UpdateOutputDevice();
            });
        }
        #endregion

        #region Other event handlers
        private async void DeviceWatcherHelper_DevicesChanged(object sender, EventArgs e)
        {
            await MainPage.dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                await UpdateOutputDevice();
            });
        }

        private async void Sounds_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (PlayingSound == null || PlayingSound.AudioPlayer == null || skipSoundsCollectionChanged) return;

            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                if (PlayingSound.Sounds.Count == 0)
                {
                    // Delete the PlayingSound
                    await Remove();
                    return;
                }

                if(e.OldStartingIndex < PlayingSound.Current)
                {
                    PlayingSound.Current--;
                    CurrentSoundChanged?.Invoke(this, new CurrentSoundChangedEventArgs(PlayingSound.Current));
                    await FileManager.SetCurrentOfPlayingSoundAsync(PlayingSound.Uuid, PlayingSound.Current);
                }
                else if(e.OldStartingIndex == PlayingSound.Current)
                {
                    if(PlayingSound.Current == PlayingSound.Sounds.Count)
                    {
                        // Move to the previous sound
                        await MoveToSound(PlayingSound.Current - 1);
                    }
                    else
                    {
                        // Move to the new current sound
                        await MoveToSound(PlayingSound.Current);
                    }
                }
                else if (PlayingSound.Sounds.Count == 1)
                {
                    if (e.OldStartingIndex == 0 && PlayingSound.Current == 0)
                        await MoveToSound(0);
                }

                // Update the PlayingSound
                await FileManager.SetSoundsListOfPlayingSoundAsync(PlayingSound.Uuid, PlayingSound.Sounds.ToList());
            }
        }

        private void PositionChangeTimer_Tick(object sender, object e)
        {
            if (PlayingSound.AudioPlayer.Position != position)
            {
                position = PlayingSound.AudioPlayer.Position;
                PositionChanged?.Invoke(this, new PositionChangedEventArgs(position));
            }
        }
        #endregion

        #region Functionality
        private async Task InitAudioPlayer()
        {
            if (PlayingSound.StartPosition.HasValue)
            {
                PlayingSound.AudioPlayer.Position = PlayingSound.StartPosition.Value;
                PlayingSound.StartPosition = null;
            }

            try
            {
                await PlayingSound.AudioPlayer.Init();
            }
            catch(AudioIOException) { }

            await MainPage.dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                currentSoundTotalDuration = PlayingSound.AudioPlayer.Duration;
                DurationChanged?.Invoke(this, new DurationChangedEventArgs(PlayingSound.AudioPlayer.Duration));
            });
        }

        private void InitPositionChangeTimer()
        {
            positionChangeTimer = new DispatcherTimer();
            positionChangeTimer.Interval = TimeSpan.FromMilliseconds(200);
            positionChangeTimer.Tick += PositionChangeTimer_Tick;
        }

        private async Task<bool> StartAudioPlayer()
        {
            try
            {
                if (!PlayingSound.AudioPlayer.IsInitialized)
                    await InitAudioPlayer();

                PlayingSound.AudioPlayer.Play();
                positionChangeTimer.Start();
            }
            catch (AudioIOException)
            {
                return false;
            }

            return true;
        }

        private async Task<bool> PauseAudioPlayer()
        {
            try
            {
                if (!PlayingSound.AudioPlayer.IsInitialized)
                    await InitAudioPlayer();

                PlayingSound.AudioPlayer.Pause();

                await MainPage.dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    positionChangeTimer.Stop();
                });
            }
            catch (AudioIOException)
            {
                return false;
            }

            return true;
        }

        public async Task MoveToSound(int index, bool startPlaying = false)
        {
            if (
                PlayingSound.Sounds.Count == 0
                || index >= PlayingSound.Sounds.Count
                || index < 0
            ) return;

            // Update PlayingSound.Current
            PlayingSound.Current = index;

            // Update the text of the current sound
            CurrentSoundChanged?.Invoke(
                this,
                new CurrentSoundChangedEventArgs(index)
            );

            // Update the visibility of the buttons
            UpdateButtonVisibility();
            UpdateFavouriteFlyoutItem();

            // Update the SystemMediaTransportControls
            FileManager.itemViewHolder.ActivePlayingSound = PlayingSound.Uuid;
            FileManager.UpdateSystemMediaTransportControls();

            if (CheckFileDownload()) return;

            // Set the new source of the MediaPlayer
            var audioFile = CurrentSound.AudioFile;

            if (audioFile == null)
            {
                await MoveToNext();
                return;
            }

            bool wasPlaying = PlayingSound.AudioPlayer.IsPlaying;
            PlayingSound.AudioPlayer.Pause();
            PlayingSound.AudioPlayer.Position = TimeSpan.Zero;
            PlayingSound.AudioPlayer.AudioFile = audioFile;
            PlayingSound.AudioPlayer.PlaybackRate = (double)PlayingSound.PlaybackSpeed / 100;
            await InitAudioPlayer();

            if (wasPlaying || startPlaying)
            {
                await SetPlayPause(true);
                PlayingSound.AudioPlayer.Play();
            }

            // Save the new Current
            await FileManager.SetCurrentOfPlayingSoundAsync(PlayingSound.Uuid, index);
        }

        /**
         * Stops all other PlayingSounds if MultiSoundPlayback is disabled
         */
        private async Task StopAllOtherPlayingSounds()
        {
            if (FileManager.itemViewHolder.MultiSoundPlayback || !FileManager.itemViewHolder.OpenMultipleSounds) return;

            // Cause all other PlayingSounds to stop playback
            foreach (var playingSoundItem in FileManager.itemViewHolder.PlayingSoundItems)
            {
                if (
                    playingSoundItem.PlayingSound.AudioPlayer == null
                    || playingSoundItem.Uuid.Equals(PlayingSound.Uuid)
                ) continue;

                await playingSoundItem.SetPlayPause(false, false);
            }
        }

        private void UpdateUI()
        {
            UpdateButtonVisibility();
            UpdateFavouriteFlyoutItem();
            UpdateVolumeControl();

            PlaybackStateChanged?.Invoke(this, new PlaybackStateChangedEventArgs(PlayingSound.AudioPlayer.IsPlaying || PlayingSound.StartPlaying));
            LocalFileButtonVisibilityChanged?.Invoke(this, new LocalFileButtonVisibilityEventArgs(PlayingSound.LocalFile ? Visibility.Visible : Visibility.Collapsed));
            RepetitionsChanged?.Invoke(this, new RepetitionsChangedEventArgs(PlayingSound.Repetitions));
            PlaybackSpeedChanged?.Invoke(this, new PlaybackSpeedChangedEventArgs(PlayingSound.PlaybackSpeed));
            PositionChanged?.Invoke(this, new PositionChangedEventArgs(PlayingSound.AudioPlayer.Position));

            if (CurrentSoundIsDownloading)
                DownloadStatusChanged?.Invoke(this, new DownloadStatusChangedEventArgs(true, -1));
        }

        private void UpdateButtonVisibility()
        {
            Visibility previousButtonVisibility = PlayingSound.Current > 0 ? Visibility.Visible : Visibility.Collapsed;
            Visibility nextButtonVisibility = PlayingSound.Current != PlayingSound.Sounds.Count - 1 ? Visibility.Visible : Visibility.Collapsed;
            Visibility expandButtonVisibility = PlayingSound.Sounds.Count > 1 ? Visibility.Visible : Visibility.Collapsed;

            ButtonVisibilityChanged?.Invoke(
                this,
                new ButtonVisibilityChangedEventArgs(
                    previousButtonVisibility,
                    nextButtonVisibility,
                    expandButtonVisibility
                )
            );
        }

        private void UpdateFavouriteFlyoutItem()
        {
            if (PlayingSound == null) return;

            FavouriteChanged?.Invoke(
                this,
                new FavouriteChangedEventArgs(CurrentSound.Favourite)
            );
        }

        private void UpdateVolumeControl()
        {
            if (PlayingSound == null) return;

            VolumeChanged?.Invoke(
                this,
                new VolumeChangedEventArgs(PlayingSound.Volume)
            );
            MutedChanged?.Invoke(
                this,
                new MutedChangedEventArgs(PlayingSound.Muted)
            );
        }

        private void AddSoundToDownloadQueue(int i)
        {
            if (PlayingSound.Sounds[i].AudioFileTableObject.FileDownloadStatus != TableObjectFileDownloadStatus.Downloaded)
            {
                DownloadProgressList.Add((PlayingSound.Sounds[i].AudioFileTableObject.Uuid, -2));
                PlayingSound.Sounds[i].AudioFileTableObject.ScheduleFileDownload(new Progress<(Guid, int)>(DownloadProgress));
            }
        }

        /**
         * Checks the current sound file download status and returns true, if the file is currently downloading
         */
        private bool CheckFileDownload()
        {
            Guid currentSoundUuid = CurrentSound.AudioFileTableObject.Uuid;

            // Get the current sound file download progress
            int i = DownloadProgressList.FindIndex(progress => progress.Item1.Equals(currentSoundUuid));

            if (i == -1)
            {
                DownloadStatusChanged?.Invoke(
                    this,
                    new DownloadStatusChangedEventArgs(false, 0)
                );
                currentSoundIsDownloading = false;
                return false;
            }

            int value = DownloadProgressList[i].Item2;

            if (value == -1 || value == 101)
            {
                DownloadStatusChanged?.Invoke(
                    this,
                    new DownloadStatusChangedEventArgs(false, 0)
                );
                currentSoundIsDownloading = false;
                return false;
            }

            // Update the download progress bar for the current sound
            DownloadStatusChanged?.Invoke(
                this,
                new DownloadStatusChangedEventArgs(true, value)
            );
            currentSoundIsDownloading = true;

            return true;
        }

        private async void DownloadProgress((Guid, int) value)
        {
            if (PlayingSound == null || PlayingSound.AudioPlayer == null) return;

            // Find the file in the download progress list
            int i = DownloadProgressList.FindIndex(progress => progress.Item1.Equals(value.Item1));
            if (i == -1) return;

            // Update the progress in the download progress list
            DownloadProgressList[i] = value;

            // Check if the download progress belongs to the current sound
            if (CurrentSound.AudioFileTableObject.Uuid.Equals(value.Item1))
            {
                // Show the download progress bar as the file is still downloading
                DownloadStatusChanged?.Invoke(
                    this,
                    new DownloadStatusChangedEventArgs(true, value.Item2)
                );

                if (value.Item2 == 101)
                {
                    currentSoundIsDownloading = false;
                    await Task.Delay(5);

                    // Set the source of the current sound
                    var audioFile = CurrentSound.AudioFile;

                    if (audioFile != null)
                    {
                        PlayingSound.AudioPlayer.AudioFile = audioFile;
                        await InitAudioPlayer();

                        // Start the current sound
                        if (PlayingSound.StartPlaying)
                        {
                            await SetPlayPause(true);
                            await StopAllOtherPlayingSounds();
                        }
                    }
                }
                else if (value.Item2 == -1)
                {
                    currentSoundIsDownloading = false;

                    // Move to the next sound
                    await MoveToNext();
                }
                else
                {
                    currentSoundIsDownloading = true;
                    return;
                }
            }
            else if (value.Item2 != 101 && value.Item2 != -1)
                return;

            // The file (not the current file) was successfully downloaded
            // Remove the file download progress from the list
            DownloadProgressList.RemoveAt(i);
        }

        private async Task StartFadeOut()
        {
            if (PlayingSound == null || PlayingSound.AudioPlayer == null) return;
            if (!PlayingSound.AudioPlayer.IsPlaying) return;

            currentFadeOutFrame = 0;
            double interval = fadeOutTime / (double)fadeOutFrames;
            fadeOutVolumeDiff = PlayingSound.AudioPlayer.Volume / fadeOutFrames;

            fadeOutTimer = new Timer();
            fadeOutTimer.Elapsed += async (object sender, ElapsedEventArgs e) => await FadeOut();

            fadeOutTimer.Interval = interval;
            fadeOutTimer.Start();

            await FadeOut();
        }

        private async Task FadeOut()
        {
            if (currentFadeOutFrame >= fadeOutFrames || PlayingSound.AudioPlayer == null)
            {
                if (PlayingSound.AudioPlayer != null && await PauseAudioPlayer())
                    PlayingSound.AudioPlayer.Position = TimeSpan.Zero;

                fadeOutTimer.Stop();
            }
            else
            {
                // Decrease the volume
                PlayingSound.AudioPlayer.Volume -= fadeOutVolumeDiff;
                currentFadeOutFrame++;
            }
        }
        #endregion

        #region Public methods
        public async Task ReloadPlayingSound()
        {
            var playingSound = await FileManager.GetPlayingSoundAsync(PlayingSound.Uuid);
            if (playingSound == null) return;
            if (PlayingSound.AudioPlayer.IsPlaying) return;

            // Update Current
            if(PlayingSound.Current != playingSound.Current)
                await MoveToSound(playingSound.Current);

            // Update Volume
            if (PlayingSound.Volume != playingSound.Volume)
            {
                PlayingSound.Volume = playingSound.Volume;
                VolumeChanged?.Invoke(
                    this,
                    new VolumeChangedEventArgs(PlayingSound.Volume)
                );
            }

            // Update Muted
            if(PlayingSound.Muted != playingSound.Muted)
            {
                PlayingSound.Muted = playingSound.Muted;
                PlayingSound.AudioPlayer.IsMuted = playingSound.Muted || FileManager.itemViewHolder.Muted;
                MutedChanged?.Invoke(
                    this,
                    new MutedChangedEventArgs(playingSound.Muted)
                );
            }

            // Update Repetitions
            if (PlayingSound.Repetitions != playingSound.Repetitions)
                PlayingSound.Repetitions = playingSound.Repetitions;

            // Update Sounds
            // Check if the sounds have changed
            bool soundsChanged = PlayingSound.Sounds.Count != playingSound.Sounds.Count;

            if (!soundsChanged)
            {
                int i = 0;
                foreach(var sound in PlayingSound.Sounds)
                {
                    if (!sound.Uuid.Equals(playingSound.Sounds[i].Uuid))
                    {
                        soundsChanged = true;
                        break;
                    }

                    i++;
                }
            }

            if (soundsChanged)
            {
                // Reload the sounds
                PlayingSound.Sounds.Clear();
                foreach (var sound in playingSound.Sounds)
                    PlayingSound.Sounds.Add(sound);
            }
        }

        /**
         * Toggles the MediaPlayer from Playing -> Paused or from Paused -> Playing
         */
        public async Task TogglePlayPause()
        {
            if (PlayingSound == null || PlayingSound.AudioPlayer == null) return;
            await SetPlayPause(!PlayingSound.AudioPlayer.IsPlaying);
        }

        /**
         * Plays or pauses the MediaPlayer
         */
        public async Task SetPlayPause(bool play, bool updateSmtc = true)
        {
            if (PlayingSound == null || PlayingSound.AudioPlayer == null) return;

            if (updateSmtc)
                FileManager.itemViewHolder.ActivePlayingSound = PlayingSound.Uuid;

            // Check if the file is currently downloading
            if (currentSoundIsDownloading && await PauseAudioPlayer())
            {
                PlayingSound.StartPlaying = play;
                PlaybackStateChanged?.Invoke(
                    this,
                    new PlaybackStateChangedEventArgs(PlayingSound.StartPlaying)
                );
                if (updateSmtc) FileManager.UpdateSystemMediaTransportControls(PlayingSound.StartPlaying);
            }
            else if (!currentSoundIsDownloading && play && await StartAudioPlayer())
            {
                PlaybackStateChanged?.Invoke(
                        this,
                        new PlaybackStateChangedEventArgs(true)
                    );
                await StopAllOtherPlayingSounds();
                if (updateSmtc) FileManager.UpdateSystemMediaTransportControls(true);
            }
            else if (!currentSoundIsDownloading && !play && await PauseAudioPlayer())
            {
                PlaybackStateChanged?.Invoke(
                    this,
                    new PlaybackStateChangedEventArgs(false)
                );
                if (updateSmtc) FileManager.UpdateSystemMediaTransportControls(false);
            }
        }

        public async Task MoveToPrevious()
        {
            if (PlayingSound == null || PlayingSound.AudioPlayer == null) return;

            if (PlayingSound.AudioPlayer.Position.Seconds >= 5)
            {
                // Move to the start of the sound
                PlayingSound.AudioPlayer.Position = TimeSpan.Zero;
            }
            else
            {
                // Move to the previous sound
                await MoveToSound(PlayingSound.Current - 1);
            }
        }

        public async Task MoveToNext()
        {
            if (PlayingSound == null || PlayingSound.AudioPlayer == null) return;
            await MoveToSound(PlayingSound.Current + 1);
        }

        public async Task AddSoundToSoundboard()
        {
            if (
                !PlayingSound.LocalFile
                || PlayingSound.Sounds.Count == 0
            ) return;

            // Update the UI
            LocalFileButtonVisibilityChanged?.Invoke(this, new LocalFileButtonVisibilityEventArgs(Visibility.Collapsed));

            // Create the sound
            var sound = PlayingSound.Sounds[0];
            await FileManager.CreateSoundAsync(sound.Uuid, sound.AudioFile.DisplayName, new List<Guid>(), sound.AudioFile, null);

            // Replace the sound in the PlayingSound
            var newSound = await FileManager.GetSoundAsync(sound.Uuid);
            PlayingSound.Sounds[0] = newSound;

            FileManager.AddSound(newSound);

            // Create the PlayingSound
            PlayingSound.LocalFile = false;
            await FileManager.CreatePlayingSoundAsync(
                PlayingSound.Uuid,
                PlayingSound.Sounds.ToList(),
                0,
                PlayingSound.Repetitions,
                PlayingSound.Randomly,
                PlayingSound.Volume,
                PlayingSound.Muted
            );
        }

        public void SetPosition(int position)
        {
            if (PlayingSound == null || PlayingSound.AudioPlayer == null) return;
            if (position >= currentSoundTotalDuration.TotalSeconds) return;

            PlayingSound.AudioPlayer.Position = new TimeSpan(0, 0, position);
        }

        public async Task SetVolume(int volume)
        {
            PlayingSound.Volume = volume;
            VolumeChanged?.Invoke(
                this,
                new VolumeChangedEventArgs(volume)
            );

            // Save the new volume
            await FileManager.SetVolumeOfPlayingSoundAsync(PlayingSound.Uuid, volume);
        }

        public async Task SetMuted(bool muted)
        {
            PlayingSound.Muted = muted;
            PlayingSound.AudioPlayer.IsMuted = muted || FileManager.itemViewHolder.Muted;
            MutedChanged?.Invoke(
                this,
                new MutedChangedEventArgs(muted)
            );

            // Save the new muted
            await FileManager.SetMutedOfPlayingSoundAsync(PlayingSound.Uuid, muted);
        }

        public async Task SetRepetitions(int repetitions)
        {
            PlayingSound.Repetitions = repetitions;

            // Update the UI
            RepetitionsChanged?.Invoke(this, new RepetitionsChangedEventArgs(repetitions));

            // Save the new repetitions
            await FileManager.SetRepetitionsOfPlayingSoundAsync(PlayingSound.Uuid, repetitions);
        }

        public async void SetPlaybackSpeed(int playbackSpeed)
        {
            if (playbackSpeed < 25 || playbackSpeed > 200) return;

            PlayingSound.PlaybackSpeed = playbackSpeed;
            PlayingSound.AudioPlayer.PlaybackRate = (double)playbackSpeed / 100;

            // Show or hide the playback speed button
            PlaybackSpeedChanged?.Invoke(this, new PlaybackSpeedChangedEventArgs(playbackSpeed));

            // Save the new playback speed
            await FileManager.SetPlaybackSpeedOfPlayingSoundAsync(PlayingSound.Uuid, playbackSpeed);
        }

        public async Task ToggleFavourite()
        {
            Sound currentSound = PlayingSound.Sounds.ElementAt(PlayingSound.Current);
            currentSound.Favourite = !currentSound.Favourite;

            // Update the UI
            FavouriteChanged?.Invoke(this, new FavouriteChangedEventArgs(currentSound.Favourite));

            // Save the new favourite and reload the sound
            await FileManager.SetFavouriteOfSoundAsync(currentSound.Uuid, currentSound.Favourite);
            await FileManager.ReloadSound(currentSound.Uuid);
        }

        public async Task UpdateOutputDevice()
        {
            if (PlayingSound.AudioPlayer == null) return;

            if (updateOutputDeviceRunning)
            {
                runUpdateOutputDeviceAgain = true;
                return;
            }

            updateOutputDeviceRunning = true;
            string deviceId = null;

            // Get the output device of the PlayingSound or from the settings
            if (!string.IsNullOrEmpty(PlayingSound.OutputDevice))
                deviceId = PlayingSound.OutputDevice;
            else if (!FileManager.itemViewHolder.UseStandardOutputDevice)
                deviceId = FileManager.itemViewHolder.OutputDevice;

            if (!string.IsNullOrEmpty(deviceId))
            {
                DeviceInformation deviceInfo = await FileManager.GetDeviceInformationById(deviceId);

                if (deviceInfo != null && deviceInfo.IsEnabled)
                {
                    if (PlayingSound.AudioPlayer.OutputDevice == null || PlayingSound.AudioPlayer.OutputDevice.Id != deviceInfo.Id)
                    {
                        try
                        {
                            PlayingSound.AudioPlayer.OutputDevice = deviceInfo;
                            await InitAudioPlayer();
                        }
                        catch (Exception) { }
                    }

                    OutputDeviceButtonVisibilityChanged?.Invoke(
                        this,
                        new OutputDeviceButtonVisibilityEventArgs(string.IsNullOrEmpty(PlayingSound.OutputDevice) ? Visibility.Collapsed : Visibility.Visible)
                    );

                    await UpdateOutputDeviceEnd();
                    return;
                }
            }

            if (PlayingSound.AudioPlayer.OutputDevice == null)
            {
                await UpdateOutputDeviceEnd();
                return;
            }

            try
            {
                PlayingSound.AudioPlayer.OutputDevice = null;
                await InitAudioPlayer();
            }
            catch (Exception) { }
            OutputDeviceButtonVisibilityChanged?.Invoke(this, new OutputDeviceButtonVisibilityEventArgs(Visibility.Collapsed));

            await UpdateOutputDeviceEnd();
        }

        private async Task UpdateOutputDeviceEnd()
        {
            updateOutputDeviceRunning = false;

            if (runUpdateOutputDeviceAgain)
            {
                runUpdateOutputDeviceAgain = false;
                await UpdateOutputDevice();
            }
        }

        public async Task TriggerRemove()
        {
            // Stop and reset the MediaPlayer
            await StartFadeOut();

            // Start the remove animation
            RemovePlayingSound?.Invoke(this, new EventArgs());
        }

        public async Task Remove()
        {
            if (removed) return;
            removed = true;

            // Remove the PlayingSound from the list
            FileManager.itemViewHolder.PlayingSounds.Remove(PlayingSound);

            // Remove this PlayingSoundItem from the PlayingSoundItems list
            FileManager.itemViewHolder.PlayingSoundItems.Remove(this);

            if (PlayingSound.AudioPlayer != null && await PauseAudioPlayer())
                PlayingSound.AudioPlayer.Position = TimeSpan.Zero;

            // Remove this PlayingSound from the SMTC, if it was active
            if (PlayingSound.Uuid.Equals(FileManager.itemViewHolder.ActivePlayingSound))
            {
                FileManager.itemViewHolder.ActivePlayingSound = Guid.Empty;
                FileManager.UpdateSystemMediaTransportControls();
            }

            // Delete the PlayingSound
            await FileManager.DeletePlayingSoundAsync(PlayingSound.Uuid);
        }

        public void RemoveSound(Guid uuid)
        {
            int index = PlayingSound.Sounds.ToList().FindIndex(s => s.Uuid.Equals(uuid));
            if (index == -1) return;

            PlayingSound.Sounds.RemoveAt(index);
        }

        public void TriggerCollapseSoundsListEvent(object sender, EventArgs args)
        {
            CollapseSoundsList?.Invoke(sender, args);
        }
        #endregion
    }
}
