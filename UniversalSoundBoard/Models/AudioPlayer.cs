using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniversalSoundboard.Common;
using Windows.Media.Audio;
using Windows.Media.Render;
using Windows.Storage;
using Windows.UI.Xaml;

namespace UniversalSoundboard.Models
{
    public class AudioPlayer
    {
        private bool initialized = false;
        private bool isPlaying = false;
        private double volume = 1;
        private bool isMuted = false;
        private double playbackRate = 1.0;
        private DispatcherTimer positionChangeTimer;

        private AudioGraph AudioGraph { get; set; }
        private AudioFileInputNode FileInputNode { get; set; }
        private AudioDeviceOutputNode DeviceOutputNode { get; set; }

        public bool IsPlaying { get => isPlaying; }
        public TimeSpan Duration
        {
            get => FileInputNode?.Duration ?? TimeSpan.Zero;
        }
        public TimeSpan Position
        {
            get => FileInputNode?.Position ?? TimeSpan.Zero;
            set => setPosition(value);
        }
        public double Volume
        {
            get => volume;
            set => setVolume(value);
        }
        public bool IsMuted
        {
            get => isMuted;
            set => setIsMuted(value);
        }
        public double PlaybackRate
        {
            get => playbackRate;
            set => setPlaybackRate(value);
        }

        public event EventHandler<PositionChangedEventArgs> PositionChanged;

        public AudioPlayer()
        {
            
        }

        public async Task Init(StorageFile audioFile)
        {
            if (initialized) return;

            var settings = new AudioGraphSettings(AudioRenderCategory.Media);

            // Create the AudioGraph
            var createAudioGraphResult = await AudioGraph.CreateAsync(settings);

            if (createAudioGraphResult.Status != AudioGraphCreationStatus.Success)
                throw new AudioPlayerInitException(createAudioGraphResult.Status);

            AudioGraph = createAudioGraphResult.Graph;

            // Create the input node
            var inputNodeResult = await AudioGraph.CreateFileInputNodeAsync(audioFile);

            if (inputNodeResult.Status != AudioFileNodeCreationStatus.Success)
                throw new AudioPlayerInitException(inputNodeResult.Status);

            FileInputNode = inputNodeResult.FileInputNode;

            // Create the output node
            var outputNodeResult = await AudioGraph.CreateDeviceOutputNodeAsync();

            if (outputNodeResult.Status != AudioDeviceNodeCreationStatus.Success)
                throw new AudioPlayerInitException(outputNodeResult.Status);

            DeviceOutputNode = outputNodeResult.DeviceOutputNode;
            FileInputNode.AddOutgoingConnection(DeviceOutputNode);

            // Init the timer for raising the positionChanged event
            positionChangeTimer = new DispatcherTimer();
            positionChangeTimer.Interval = TimeSpan.FromMilliseconds(100);
            positionChangeTimer.Tick += PositionChangeTimer_Tick;

            initialized = true;
        }

        public void Play()
        {
            if (!initialized)
                throw new AudioPlayerNotInitializedException();

            if (isPlaying) return;

            AudioGraph.Start();
            isPlaying = true;
            positionChangeTimer.Start();
        }

        public void Pause()
        {
            if (!initialized)
                throw new AudioPlayerNotInitializedException();

            if (!isPlaying) return;

            AudioGraph.Stop();
            isPlaying = false;
            positionChangeTimer.Stop();
        }

        #region Setter methods
        private void setPosition(TimeSpan position)
        {
            if (!initialized)
                throw new AudioPlayerNotInitializedException();

            FileInputNode.Seek(position);
        }

        private void setVolume(double volume)
        {
            if (!initialized)
                throw new AudioPlayerNotInitializedException();

            // Don't set the volume if the player is muted or if the volume didn't change
            if (isMuted || volume == this.volume) return;

            if (volume > 1)
                volume = 1;
            else if (volume < 0)
                volume = 0;

            DeviceOutputNode.OutgoingGain = volume;
            this.volume = volume;
        }

        private void setIsMuted(bool muted)
        {
            if (!initialized)
                throw new AudioPlayerNotInitializedException();

            // Don't change the value if it didn't change
            if (muted == isMuted) return;

            if (muted)
                DeviceOutputNode.OutgoingGain = 0;
            else
                DeviceOutputNode.OutgoingGain = volume;

            isMuted = muted;
        }

        private void setPlaybackRate(double playbackRate)
        {
            if (!initialized)
                throw new AudioPlayerNotInitializedException();

            // Don't change the value if it didn't change
            if (this.playbackRate == playbackRate) return;

            FileInputNode.PlaybackSpeedFactor = playbackRate;
            this.playbackRate = playbackRate;
        }
        #endregion

        #region Event Handlers
        private void PositionChangeTimer_Tick(object sender, object e)
        {
            PositionChanged?.Invoke(this, new PositionChangedEventArgs(FileInputNode.Position));
        }
        #endregion
    }
}
