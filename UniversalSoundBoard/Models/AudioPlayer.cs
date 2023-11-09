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
        private bool effectsChanged = true;
        private bool isPlaying = false;
        private TimeSpan position = TimeSpan.Zero;
        private double volume = 1;
        private bool isMuted = false;
        private double playbackRate = 1.0;
        private bool isFadeInEnabled = false;
        private int fadeInDuration = 1000;
        private bool isFadeOutEnabled = false;
        private int fadeOutDuration = 1000;
        private bool isEchoEnabled = false;
        private int echoDelay = 1000;
        private bool isLimiterEnabled = false;
        private int limiterLoudness = 1000;

        private AudioGraph AudioGraph;
        private AudioFileInputNode FileInputNode;
        private AudioDeviceOutputNode DeviceOutputNode;

        private AudioEffectDefinition fadeInEffectDefinition;
        private AudioEffectDefinition fadeOutEffectDefinition;
        private EchoEffectDefinition echoEffectDefinition;
        private LimiterEffectDefinition limiterEffectDefinition;

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
        public bool IsFadeInEnabled
        {
            get => isFadeInEnabled;
            set => setIsFadeInEnabled(value);
        }
        public int FadeInDuration
        {
            get => fadeInDuration;
            set => setFadeInDuration(value);
        }
        public bool IsFadeOutEnabled
        {
            get => isFadeOutEnabled;
            set => setIsFadeOutEnabled(value);
        }
        public int FadeOutDuration
        {
            get => fadeOutDuration;
            set => setFadeOutDuration(value);
        }
        public bool IsEchoEnabled
        {
            get => isEchoEnabled;
            set => setIsEchoEnabled(value);
        }
        public int EchoDelay
        {
            get => echoDelay;
            set => setEchoDelay(value);
        }
        public bool IsLimiterEnabled
        {
            get => isLimiterEnabled;
            set => setIsLimiterEnabled(value);
        }
        public int LimiterLoudness
        {
            get => limiterLoudness;
            set => setLimiterLoudness(value);
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

                // Init the audio effects
                InitEffectDefinitions();
            }

            if (
                audioFileChanged
                || outputDeviceChanged
                || playbackRateChanged
                || effectsChanged
            )
            {
                // Create the input node
                await InitFileInputNode();

                if (DeviceOutputNode != null)
                    FileInputNode.AddOutgoingConnection(DeviceOutputNode);

                outputDeviceChanged = false;
                audioFileChanged = false;
                playbackRateChanged = false;
                effectsChanged = false;
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

            // Fade in effect
            FileInputNode.EffectDefinitions.Add(fadeInEffectDefinition);
            if (!isFadeInEnabled) FileInputNode.DisableEffectsByDefinition(fadeInEffectDefinition);

            // Fade out effect
            FileInputNode.EffectDefinitions.Add(fadeOutEffectDefinition);
            FileInputNode.DisableEffectsByDefinition(fadeOutEffectDefinition);

            FileInputNode.EffectDefinitions.Add(echoEffectDefinition);
            if (!isEchoEnabled) FileInputNode.DisableEffectsByDefinition(echoEffectDefinition);

            FileInputNode.EffectDefinitions.Add(limiterEffectDefinition);
            if (!isLimiterEnabled) FileInputNode.DisableEffectsByDefinition(limiterEffectDefinition);

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

        private void InitEffectDefinitions()
        {
            fadeInEffectDefinition = new AudioEffectDefinition(
                typeof(FadeInAudioEffect).FullName,
                new PropertySet
                {
                    { "Duration", (float)fadeInDuration }
                }
            );

            fadeOutEffectDefinition = new AudioEffectDefinition(
                typeof(FadeOutAudioEffect).FullName,
                new PropertySet
                {
                    { "Duration", (float)fadeOutDuration }
                }
            );

            echoEffectDefinition = new EchoEffectDefinition(AudioGraph)
            {
                Delay = echoDelay,
                WetDryMix = 0.7f,
                Feedback = 0.5f
            };

            limiterEffectDefinition = new LimiterEffectDefinition(AudioGraph)
            {
                Loudness = (uint)limiterLoudness,
                Release = 10
            };
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

            FileInputNode.DisableEffectsByDefinition(fadeInEffectDefinition);
            isPlaying = false;
        }

        public async Task FadeOut(int milliseconds)
        {
            FileInputNode.EnableEffectsByDefinition(fadeOutEffectDefinition);
            await Task.Delay(milliseconds);
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
            FileInputNode.DisableEffectsByDefinition(fadeInEffectDefinition);
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

            if (FileInputNode != null && DeviceOutputNode != null)
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

        private void setIsFadeInEnabled(bool isFadeInEnabled)
        {
            if (this.isFadeInEnabled == isFadeInEnabled)
                return;

            this.isFadeInEnabled = isFadeInEnabled;
        }

        private void setFadeInDuration(int fadeInDuration)
        {
            if (this.fadeInDuration == fadeInDuration)
                return;

            this.fadeInDuration = fadeInDuration;

            if (fadeInEffectDefinition != null)
                fadeInEffectDefinition.Properties["Duration"] = fadeInDuration;
        }

        private void setIsFadeOutEnabled(bool isFadeOutEnabled)
        {
            if (this.isFadeOutEnabled = isFadeOutEnabled)
                return;

            this.isFadeOutEnabled = isFadeOutEnabled;
        }

        private void setFadeOutDuration(int fadeOutDuration)
        {
            if (this.fadeOutDuration.Equals(fadeOutDuration))
                return;

            this.fadeOutDuration = fadeOutDuration;

            if (fadeOutEffectDefinition != null)
                fadeOutEffectDefinition.Properties["Duration"] = (float)fadeOutDuration;
        }

        private void setIsEchoEnabled(bool isEchoEnabled)
        {
            if (this.isEchoEnabled == isEchoEnabled)
                return;

            this.isEchoEnabled = isEchoEnabled;

            if (FileInputNode != null)
            {
                if (isEchoEnabled)
                    FileInputNode.EnableEffectsByDefinition(echoEffectDefinition);
                else
                    FileInputNode.DisableEffectsByDefinition(echoEffectDefinition);
            }
        }

        private void setEchoDelay(int echoDelay)
        {
            if (this.echoDelay == echoDelay)
                return;

            this.echoDelay = echoDelay;

            if (echoEffectDefinition != null)
                echoEffectDefinition.Delay = echoDelay;
        }

        private void setIsLimiterEnabled(bool isLimiterEnabled)
        {
            if (this.isLimiterEnabled == isLimiterEnabled)
                return;

            this.isLimiterEnabled = isLimiterEnabled;

            if (FileInputNode != null)
            {
                if (isLimiterEnabled)
                    FileInputNode.EnableEffectsByDefinition(limiterEffectDefinition);
                else
                    FileInputNode.DisableEffectsByDefinition(limiterEffectDefinition);
            }
        }

        private void setLimiterLoudness(int limiterLoudness)
        {
            if (this.limiterLoudness == limiterLoudness)
                return;

            this.limiterLoudness = limiterLoudness;

            if (limiterEffectDefinition != null)
                limiterEffectDefinition.Loudness = (uint)limiterLoudness;
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
