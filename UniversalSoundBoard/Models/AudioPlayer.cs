using AudioEffectComponent;
using Microsoft.AppCenter.Crashes;
using System;
using System.Threading.Tasks;
using UniversalSoundboard.Common;
using Windows.Devices.Enumeration;
using Windows.Foundation.Collections;
using Windows.Media.Audio;
using Windows.Media.Effects;
using Windows.Media.Render;
using Windows.Storage;

namespace UniversalSoundboard.Models
{
    public class AudioPlayer
    {
        private bool isInitialized = false;
        private bool isInitializing = false;
        private StorageFile audioFile;
        private DeviceInformation outputDevice;
        private bool audioFileChanged = true;
        private bool outputDeviceChanged = true;
        private bool playbackRateChanged = true;
        private bool isPlaying = false;
        private TimeSpan position = TimeSpan.Zero;
        private double volume = 1;
        private bool isMuted = false;
        private double playbackRate = 1.0;

        private AudioGraph AudioGraph;
        private AudioFileInputNode FileInputNode;
        private AudioDeviceOutputNode DeviceOutputNode;

        public bool IsInitialized
        {
            get => isInitialized;
        }
        public StorageFile AudioFile
        {
            get => audioFile;
            set => setAudioFile(value);
        }
        public DeviceInformation OutputDevice
        {
            get => outputDevice;
            set => setOutputDevice(value);
        }
        public bool IsPlaying { get => isPlaying; }
        public TimeSpan Duration
        {
            get => FileInputNode?.Duration ?? TimeSpan.Zero;
        }
        public TimeSpan Position
        {
            get => FileInputNode?.Position ?? position;
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

        public event EventHandler<EventArgs> MediaEnded;
        public event EventHandler<AudioGraphUnrecoverableErrorOccurredEventArgs> UnrecoverableErrorOccurred;

        public AudioPlayer() { }

        public AudioPlayer(StorageFile audioFile)
        {
            this.audioFile = audioFile;
        }

        public AudioPlayer(DeviceInformation outputDevice)
        {
            this.outputDevice = outputDevice;
        }

        public AudioPlayer(StorageFile audioFile, DeviceInformation outputDevice)
        {
            this.audioFile = audioFile;
            this.outputDevice = outputDevice;
        }

        public async Task Init()
        {
            if (audioFile == null)
                throw new AudioPlayerInitException(AudioPlayerInitError.AudioFileNotSpecified);

            if (isInitializing) return;
            isInitializing = true;

            if (!isInitialized || outputDeviceChanged)
            {
                // Create the AudioGraph
                await InitAudioGraph();

                // Create the output node
                await InitDeviceOutputNode();
            }

            if (audioFileChanged || outputDeviceChanged || playbackRateChanged)
            {
                // Create the input node
                await InitFileInputNode();

                if (DeviceOutputNode != null)
                    FileInputNode.AddOutgoingConnection(DeviceOutputNode);

                outputDeviceChanged = false;
                audioFileChanged = false;
                playbackRateChanged = false;
            }

            isInitialized = true;

            if (IsPlaying)
                AudioGraph.Start();

            isInitializing = false;
        }

        private async Task InitAudioGraph()
        {
            var settings = new AudioGraphSettings(AudioRenderCategory.Media);
            settings.PrimaryRenderDevice = outputDevice;
            var createAudioGraphResult = await AudioGraph.CreateAsync(settings);

            if (createAudioGraphResult.Status != AudioGraphCreationStatus.Success)
            {
                isInitializing = false;
                throw new AudioGraphInitException(createAudioGraphResult.Status);
            }

            if (AudioGraph != null)
            {
                try
                {
                    AudioGraph.Stop();
                }
                catch (Exception e)
                {
                    Crashes.TrackError(e);
                }
            }

            AudioGraph = createAudioGraphResult.Graph;
            AudioGraph.UnrecoverableErrorOccurred += AudioGraph_UnrecoverableErrorOccurred;
        }

        private async Task InitFileInputNode()
        {
            FileInputNode?.Stop();

            var oldPosition = FileInputNode?.Position;
            var inputNodeResult = await AudioGraph.CreateFileInputNodeAsync(audioFile);

            if (inputNodeResult.Status != AudioFileNodeCreationStatus.Success)
            {
                isInitializing = false;
                throw new FileInputNodeInitException(inputNodeResult.Status);
            }

            FileInputNode?.Dispose();

            FileInputNode = inputNodeResult.FileInputNode;

            if (oldPosition.HasValue)
                FileInputNode.Seek(oldPosition.Value);
            else
                FileInputNode.Seek(position);

            if (isMuted)
                FileInputNode.OutgoingGain = 0;
            else
                FileInputNode.OutgoingGain = volume;

            FileInputNode.PlaybackSpeedFactor = playbackRate;

            if (playbackRate != 1f)
            {
                // Set the pitch for the current playback rate
                FileInputNode.EffectDefinitions.Add(new AudioEffectDefinition(
                    typeof(PitchShiftAudioEffect).FullName,
                    new PropertySet { { "Pitch", 1 / (float)playbackRate } }
                ));
            }

            FileInputNode.FileCompleted += FileInputNode_FileCompleted;
        }

        private async Task InitDeviceOutputNode()
        {
            if (DeviceOutputNode != null)
            {
                try
                {
                    DeviceOutputNode.Stop();
                }
                catch(Exception e)
                {
                    Crashes.TrackError(e);
                }
            }

            var outputNodeResult = await AudioGraph.CreateDeviceOutputNodeAsync();

            if (outputNodeResult.Status != AudioDeviceNodeCreationStatus.Success)
            {
                isInitializing = false;
                throw new DeviceOutputNodeInitException(outputNodeResult.Status);
            }

            DeviceOutputNode = outputNodeResult.DeviceOutputNode;
        }

        public void Play()
        {
            if (!isInitialized)
                throw new AudioPlayerNotInitializedException();

            if (isPlaying) return;

            try
            {
                AudioGraph.Start();
            }
            catch(Exception e)
            {
                Crashes.TrackError(e);
                throw new AudioIOException();
            }

            isPlaying = true;
        }

        public void Pause()
        {
            if (!isInitialized)
                throw new AudioPlayerNotInitializedException();

            if (!isPlaying) return;

            try
            {
                AudioGraph.Stop();
            }
            catch(Exception e)
            {
                Crashes.TrackError(e);
            }

            isPlaying = false;
        }

        #region Setter methods
        private void setAudioFile(StorageFile audioFile)
        {
            if (this.audioFile == audioFile) return;

            this.audioFile = audioFile;
            audioFileChanged = true;
        }

        private void setOutputDevice(DeviceInformation outputDevice)
        {
            if (this.outputDevice == outputDevice) return;

            this.outputDevice = outputDevice;
            outputDeviceChanged = true;
        }

        private void setPosition(TimeSpan position)
        {
            if (FileInputNode != null && position > FileInputNode.Duration)
                return;
            
            FileInputNode?.Seek(position);
            this.position = position;
        }

        private void setVolume(double volume)
        {
            // Don't set the volume if the player is muted or if the volume didn't change
            if (isMuted || volume == this.volume) return;

            if (volume > 1)
                volume = 1;
            else if (volume < 0)
                volume = 0;

            if (FileInputNode != null)
                FileInputNode.OutgoingGain = volume;

            this.volume = volume;
        }

        private void setIsMuted(bool muted)
        {
            // Don't change the value if it didn't change
            if (muted == isMuted) return;

            if (DeviceOutputNode != null)
            {
                if (muted)
                    FileInputNode.OutgoingGain = 0;
                else
                    FileInputNode.OutgoingGain = volume;
            }

            isMuted = muted;
        }

        private async void setPlaybackRate(double playbackRate)
        {
            // Don't change the value if it didn't change
            if (this.playbackRate == playbackRate)
                return;

            this.playbackRate = playbackRate;

            if (FileInputNode == null)
                return;

            FileInputNode.PlaybackSpeedFactor = playbackRate;
            playbackRateChanged = true;

            await Init();
        }
        #endregion

        #region Event Handlers
        private void AudioGraph_UnrecoverableErrorOccurred(AudioGraph sender, AudioGraphUnrecoverableErrorOccurredEventArgs args)
        {
            UnrecoverableErrorOccurred?.Invoke(this, args);
        }

        private void FileInputNode_FileCompleted(AudioFileInputNode sender, object args)
        {
            MediaEnded?.Invoke(this, new EventArgs());
        }
        #endregion
    }
}
