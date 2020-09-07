using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using UniversalSoundboard.Common;
using UniversalSoundboard.Models;
using UniversalSoundBoard.Common;
using UniversalSoundBoard.DataAccess;
using UniversalSoundBoard.Models;
using Windows.Media.Playback;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace UniversalSoundboard.Components
{
    /**
     * Base class for PlayingSoundItems that are displayed in the PlayingSoundsBar and BottomPlayingSoundsBar
     * 
     * Each PlayingSoundItemTemplate is assigned to a PlayingSoundItem class, so that all logic runs in the PlayingSoundItem
     * and the PlayingSoundItemTemplate only updates the UI.
     * Each PlayingSoundItem can have multiple (2 for both lists) PlayingSoundItemTemplates.
     * 
     * The PlayingSoundItems are stored as a list in ItemViewHolder. Whenever a new PlayingSoundItemTemplate is initialized,
     * it will check if a PlayingSoundItem already exists for the given PlayingSound in the list.
     * If so, the PlayingSoundItemTemplate will subscribe to the PlayingSoundItem and will receive all UI updates.
     * If not, the PlayingSoundItemTemplate will create a new PlayingSoundItem and subscribe to it.
     */
    public class PlayingSoundItem
    {
        private PlayingSound PlayingSound;
        public Guid Uuid { get => PlayingSound == null ? Guid.Empty : PlayingSound.Uuid; }
        public bool SoundsListVisible { get => soundsListVisible; }

        #region Local variables
        private CoreDispatcher dispatcher;
        private bool initialized = false;
        private bool skipSoundsCollectionChanged = false;
        private bool soundsListVisible = false;
        private bool showSoundsListAnimationTriggered = false;
        private bool hideSoundsListAnimationTriggered = false;

        private Visibility PreviousButtonVisibility = Visibility.Visible;
        private Visibility NextButtonVisibility = Visibility.Visible;
        private Visibility ExpandButtonVisibility = Visibility.Visible;
        #endregion

        #region Events
        public event EventHandler<PlaybackStateChangedEventArgs> PlaybackStateChanged;
        public event EventHandler<CurrentSoundChangedEventArgs> CurrentSoundChanged;
        public event EventHandler<ButtonVisibilityChangedEventArgs> ButtonVisibilityChanged;
        public event EventHandler<ExpandButtonContentChangedEventArgs> ExpandButtonContentChanged;
        public event EventHandler<EventArgs> ShowSoundsList;
        public event EventHandler<EventArgs> HideSoundsList;
        public event EventHandler<FavouriteChangedEventArgs> FavouriteChanged;
        public event EventHandler<VolumeChangedEventArgs> VolumeChanged;
        public event EventHandler<MutedChangedEventArgs> MutedChanged;
        public event EventHandler<EventArgs> RemovePlayingSound;
        #endregion

        public PlayingSoundItem(PlayingSound playingSound, CoreDispatcher dispatcher)
        {
            PlayingSound = playingSound;
            this.dispatcher = dispatcher;
        }

        public void Init()
        {
            if (!initialized)
            {
                initialized = true;

                // Subscribe to ItemViewHolder events
                FileManager.itemViewHolder.PropertyChanged += ItemViewHolder_PropertyChanged;
                FileManager.itemViewHolder.SoundDeleted += ItemViewHolder_SoundDeleted;
                FileManager.itemViewHolder.PlayingSoundItemStartSoundsListAnimation += ItemViewHolder_PlayingSoundItemStartSoundsListAnimation;

                // Subscribe to MediaPlayer events
                PlayingSound.MediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
                PlayingSound.MediaPlayer.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;
                ((MediaPlaybackList)PlayingSound.MediaPlayer.Source).CurrentItemChanged += PlayingSoundItem_CurrentItemChanged;
                PlayingSound.MediaPlayer.CommandManager.PreviousReceived += CommandManager_PreviousReceived;

                // Subscribe to other event handlers
                PlayingSound.Sounds.CollectionChanged += Sounds_CollectionChanged;

                // Stop all other PlayingSounds if this PlayingSound was just started
                if (
                    PlayingSound.MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing
                    || PlayingSound.MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Opening
                ) StopAllOtherPlayingSounds();
            }

            // Set the correct visibilities and icons for the buttons
            UpdateButtonVisibility();
            UpdateFavouriteFlyoutItem();
            UpdateVolumeControl();
            ExpandButtonContentChanged?.Invoke(this, new ExpandButtonContentChangedEventArgs(false));
            PlaybackStateChanged?.Invoke(
                this,
                new PlaybackStateChangedEventArgs(
                    PlayingSound.MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing
                    || PlayingSound.MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Opening
                )
            );
        }

        #region ItemViewHolder event handlers
        private void ItemViewHolder_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (PlayingSound == null || PlayingSound.MediaPlayer == null) return;

            if (e.PropertyName == ItemViewHolder.VolumeKey)
                PlayingSound.MediaPlayer.Volume = (double)PlayingSound.Volume / 100 * FileManager.itemViewHolder.Volume / 100;
            else if (e.PropertyName == ItemViewHolder.MutedKey)
                PlayingSound.MediaPlayer.IsMuted = PlayingSound.Muted || FileManager.itemViewHolder.Muted;
        }

        private void ItemViewHolder_SoundDeleted(object sender, SoundEventArgs e)
        {
            // Find the deleted sound in the Sounds list of the PlayingSound
            int i = PlayingSound.Sounds.ToList().FindIndex(s => s.Uuid.Equals(e.Uuid));
            if (i == -1) return;

            // Remove the sound from the list
            PlayingSound.Sounds.RemoveAt(i);
        }

        private void ItemViewHolder_PlayingSoundItemStartSoundsListAnimation(object sender, EventArgs e)
        {
            if (showSoundsListAnimationTriggered)
            {
                showSoundsListAnimationTriggered = false;
                ShowSoundsList?.Invoke(this, new EventArgs());
            }
            else if (hideSoundsListAnimationTriggered)
            {
                hideSoundsListAnimationTriggered = false;
                HideSoundsList?.Invoke(this, new EventArgs());
            }
        }
        #endregion
        
        #region MediaPlayer event handlers
        private async void MediaPlayer_MediaEnded(MediaPlayer sender, object args)
        {
            await dispatcher.RunAsync(CoreDispatcherPriority.Low, async () =>
            {
                PlayingSound.Repetitions--;

                if (PlayingSound.Repetitions <= 0)
                {
                    // Remove the PlayingSound
                    RemovePlayingSound?.Invoke(this, new EventArgs());
                    return;
                }

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

                        // Copy the MediaPlaybackList
                        List<MediaPlaybackItem> mediaPlaybackItems = new List<MediaPlaybackItem>();
                        foreach (var item in ((MediaPlaybackList)PlayingSound.MediaPlayer.Source).Items)
                            mediaPlaybackItems.Add(item);

                        PlayingSound.Sounds.Clear();
                        ((MediaPlaybackList)PlayingSound.MediaPlayer.Source).Items.Clear();

                        // Add the sounds in random order
                        for (int i = 0; i < soundsCount; i++)
                        {
                            // Take a random sound from the sounds list and add it to the original sounds list
                            int randomIndex = random.Next(sounds.Count);

                            PlayingSound.Sounds.Add(sounds.ElementAt(randomIndex));
                            sounds.RemoveAt(randomIndex);

                            ((MediaPlaybackList)PlayingSound.MediaPlayer.Source).Items.Add(mediaPlaybackItems.ElementAt(randomIndex));
                            mediaPlaybackItems.RemoveAt(randomIndex);
                        }

                        // Set the new sound order
                        await FileManager.SetSoundsListOfPlayingSoundAsync(PlayingSound.Uuid, PlayingSound.Sounds.ToList());

                        skipSoundsCollectionChanged = false;
                    }

                    ((MediaPlaybackList)PlayingSound.MediaPlayer.Source).MoveTo(0);
                    await FileManager.SetCurrentOfPlayingSoundAsync(PlayingSound.Uuid, 0);
                }

                PlayingSound.MediaPlayer.Play();
            });
        }

        private async void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
        {
            await dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                if (PlayingSound == null || PlayingSound.MediaPlayer == null) return;

                PlaybackStateChanged?.Invoke(
                    this,
                    new PlaybackStateChangedEventArgs(
                        PlayingSound.MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Opening
                        || PlayingSound.MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing
                    )
                );
            });
        }

        private async void PlayingSoundItem_CurrentItemChanged(MediaPlaybackList sender, CurrentMediaPlaybackItemChangedEventArgs args)
        {
            await dispatcher.RunAsync(CoreDispatcherPriority.Low, async () =>
            {
                await MoveToSound((int)sender.CurrentItemIndex);
            });
        }

        private void CommandManager_PreviousReceived(MediaPlaybackCommandManager sender, MediaPlaybackCommandManagerPreviousReceivedEventArgs args)
        {
            args.Handled = true;
            MoveToPrevious();
        }
        #endregion

        #region Other event handlers
        private async void Sounds_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (PlayingSound.MediaPlayer == null || skipSoundsCollectionChanged) return;

            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                // Remove the item at the start position of the removed items
                ((MediaPlaybackList)PlayingSound.MediaPlayer.Source).Items.RemoveAt(e.OldStartingIndex);

                if (e.OldStartingIndex <= PlayingSound.Current && PlayingSound.Current > 0)
                {
                    PlayingSound.Current--;
                    CurrentSoundChanged?.Invoke(this, new CurrentSoundChangedEventArgs(PlayingSound.Current));
                    await FileManager.SetCurrentOfPlayingSoundAsync(PlayingSound.Uuid, PlayingSound.Current);
                }

                if(PlayingSound.Sounds.Count == 0)
                {
                    // Delete the PlayingSound
                    await Remove();
                }
                else
                {
                    // Update the PlayingSound
                    await FileManager.SetSoundsListOfPlayingSoundAsync(PlayingSound.Uuid, PlayingSound.Sounds.ToList());
                }
            }
        }
        #endregion

        #region Functionality
        public async Task MoveToSound(int index)
        {
            if (
                PlayingSound.Sounds.Count <= 1
                || PlayingSound.Current == index
                || index >= PlayingSound.Sounds.Count
                || index < 0
            ) return;

            // Update PlayingSound.Current
            PlayingSound.Current = index;

            // Move to the selected sound
            if (((MediaPlaybackList)PlayingSound.MediaPlayer.Source).CurrentItemIndex != index)
                ((MediaPlaybackList)PlayingSound.MediaPlayer.Source).MoveTo(Convert.ToUInt32(index));

            // Update the text of the current sound
            CurrentSoundChanged?.Invoke(
                this,
                new CurrentSoundChangedEventArgs(index)
            );

            // Update the visibility of the buttons
            UpdateButtonVisibility();
            UpdateFavouriteFlyoutItem();

            // Save the new Current
            await FileManager.SetCurrentOfPlayingSoundAsync(PlayingSound.Uuid, index);
        }

        /**
         * Stops all other PlayingSounds if MultiSoundPlayback is disabled
         */
        private void StopAllOtherPlayingSounds()
        {
            if (FileManager.itemViewHolder.MultiSoundPlayback) return;

            // Cause all other PlayingSounds to stop playback
            foreach (var playingSoundItem in FileManager.itemViewHolder.PlayingSoundItems)
            {
                if (playingSoundItem.PlayingSound.MediaPlayer == null || playingSoundItem.Uuid.Equals(PlayingSound.Uuid) || !playingSoundItem.PlayingSound.MediaPlayer.PlaybackSession.CanPause) continue;
                playingSoundItem.PlayingSound.MediaPlayer.Pause();
            }
        }

        private void UpdateButtonVisibility()
        {
            PreviousButtonVisibility = PlayingSound.Current > 0 ? Visibility.Visible : Visibility.Collapsed;
            NextButtonVisibility = PlayingSound.Current != PlayingSound.Sounds.Count - 1 ? Visibility.Visible : Visibility.Collapsed;
            ExpandButtonVisibility = PlayingSound.Sounds.Count > 1 ? Visibility.Visible : Visibility.Collapsed;

            TriggerButtonVisibilityChangedEvent();
        }

        private void UpdateFavouriteFlyoutItem()
        {
            if (PlayingSound == null) return;

            Sound currentSound = PlayingSound.Sounds.ElementAt(PlayingSound.Current);
            FavouriteChanged?.Invoke(this, new FavouriteChangedEventArgs(currentSound.Favourite));
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

        private void TriggerButtonVisibilityChangedEvent()
        {
            ButtonVisibilityChanged?.Invoke(
                this,
                new ButtonVisibilityChangedEventArgs(
                    PreviousButtonVisibility,
                    NextButtonVisibility,
                    ExpandButtonVisibility
                )
            );
        }
        #endregion

        #region Public methods
        /**
         * Toggles the MediaPlayer from Playing -> Paused or from Paused -> Playing
         */
        public void TogglePlayPause()
        {
            if (PlayingSound.MediaPlayer == null) return;

            if (PlayingSound.MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Opening || PlayingSound.MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
                PlayingSound.MediaPlayer.Pause();
            else
            {
                PlayingSound.MediaPlayer.Play();
                StopAllOtherPlayingSounds();
            }
        }

        public void MoveToPrevious()
        {
            if (PlayingSound == null || PlayingSound.MediaPlayer == null) return;
            if (PlayingSound.MediaPlayer.PlaybackSession.Position.Seconds >= 5)
            {
                // Move to the start of the sound
                PlayingSound.MediaPlayer.PlaybackSession.Position = new TimeSpan(0);
            }
            else
            {
                // Move to the previous sound
                ((MediaPlaybackList)PlayingSound.MediaPlayer.Source).MovePrevious();
            }
        }

        public void MoveToNext()
        {
            if (PlayingSound == null || PlayingSound.MediaPlayer == null) return;
            ((MediaPlaybackList)PlayingSound.MediaPlayer.Source).MoveNext();
        }

        public void ExpandSoundsList(double heightDifference)
        {
            if (soundsListVisible) return;
            soundsListVisible = true;
            showSoundsListAnimationTriggered = true;

            // Trigger the event to start the animation and wait for SoundPage to start the animation
            FileManager.itemViewHolder.TriggerPlayingSoundItemShowSoundsListAnimationStartedEvent(
                this,
                new PlayingSoundItemEventArgs(
                    PlayingSound.Uuid,
                    heightDifference
                )
            );

            // Update the ExpandButton content
            ExpandButtonContentChanged?.Invoke(this, new ExpandButtonContentChangedEventArgs(true));
        }

        public void CollapseSoundsList(double heightDifference)
        {
            if (!soundsListVisible) return;
            soundsListVisible = false;
            hideSoundsListAnimationTriggered = true;

            // Trigger the event to start the animation and wait for SoundPage to start the animation
            FileManager.itemViewHolder.TriggerPlayingSoundItemHideSoundsListAnimationStartedEvent(
                this,
                new PlayingSoundItemEventArgs(
                    PlayingSound.Uuid,
                    heightDifference
                )
            );

            // Update the ExpandButton content
            ExpandButtonContentChanged?.Invoke(this, new ExpandButtonContentChangedEventArgs(false));
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
            PlayingSound.MediaPlayer.IsMuted = muted || FileManager.itemViewHolder.Muted;
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
            await FileManager.SetRepetitionsOfPlayingSoundAsync(PlayingSound.Uuid, repetitions);
        }

        public async Task ToggleFavourite()
        {
            Sound currentSound = PlayingSound.Sounds.ElementAt(PlayingSound.Current);
            currentSound.Favourite = !currentSound.Favourite;

            // Save the new favourite and reload the sound
            await FileManager.SetSoundAsFavouriteAsync(currentSound.Uuid, currentSound.Favourite);
            await FileManager.ReloadSound(currentSound.Uuid);

            FavouriteChanged?.Invoke(this, new FavouriteChangedEventArgs(currentSound.Favourite));
        }

        public async Task Remove()
        {
            // Remove the PlayingSound from the list
            FileManager.itemViewHolder.PlayingSounds.Remove(PlayingSound);

            // Disable the MediaPlayer
            if (PlayingSound.MediaPlayer != null)
            {
                PlayingSound.MediaPlayer.Pause();
                PlayingSound.MediaPlayer.SystemMediaTransportControls.IsEnabled = false;
                PlayingSound.MediaPlayer = null;
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
        #endregion
    }
}
