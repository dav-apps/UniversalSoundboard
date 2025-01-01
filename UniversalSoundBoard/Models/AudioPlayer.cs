using AudioEffectComponent;
using Sentry;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
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
        private bool audioFileChanged = true;
        private bool outputDevicesChanged = true;
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
        private bool isReverbEnabled = false;
        private double reverbDecay = 2;
        private bool isPitchShiftEnabled = false;
        private double pitchShiftFactor = 1;

        private List<AudioGraphContainer> AudioGraphContainers;

        public bool IsInitialized
        {
            get => isInitialized;
        }
        public StorageFile AudioFile
        {
            get => audioFile;
            set => setAudioFile(value);
        }
        public readonly ObservableCollection<DeviceInformation> OutputDevices;
        public bool IsPlaying { get => isPlaying; }
        public TimeSpan Duration
        {
            get
            {
                var audioGraphContainer = AudioGraphContainers.FirstOrDefault();
                return audioGraphContainer?.FileInputNode?.Duration ?? TimeSpan.Zero;
            }
        }
        public TimeSpan Position
        {
            get
            {
                var audioGraphContainer = AudioGraphContainers.FirstOrDefault();
                return audioGraphContainer?.FileInputNode?.Position ?? position;
            }
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
        public bool IsReverbEnabled
        {
            get => isReverbEnabled;
            set => setIsReverbEnabled(value);
        }
        public double ReverbDecay
        {
            get => reverbDecay;
            set => setReverbDecay(value);
        }
        public bool IsPitchShiftEnabled
        {
            get => isPitchShiftEnabled;
            set => setIsPitchShiftEnabled(value);
        }
        public double PitchShiftFactor
        {
            get => pitchShiftFactor;
            set => setPitchShiftFactor(value);
        }

        public event EventHandler<EventArgs> MediaEnded;
        public event EventHandler<AudioGraphUnrecoverableErrorOccurredEventArgs> UnrecoverableErrorOccurred;

        public AudioPlayer()
        {
            AudioGraphContainers = new List<AudioGraphContainer>();
            OutputDevices = new ObservableCollection<DeviceInformation>();
            OutputDevices.CollectionChanged += OutputDevices_CollectionChanged;
        }

        public AudioPlayer(StorageFile audioFile)
        {
            AudioGraphContainers = new List<AudioGraphContainer>();
            OutputDevices = new ObservableCollection<DeviceInformation>();
            OutputDevices.CollectionChanged += OutputDevices_CollectionChanged;

            this.audioFile = audioFile;
        }

        public async Task Init()
        {
            if (audioFile == null)
                throw new AudioPlayerInitException(AudioPlayerInitError.AudioFileNotSpecified);

            if (isInitializing) return;
            isInitializing = true;

            if (!isInitialized || outputDevicesChanged)
            {
                // Create the AudioGraph
                await InitAudioGraph();

                // Create the output node
                await InitDeviceOutputNodes();

                // Init the audio effects
                InitEffectDefinitions();
            }

            if (
                audioFileChanged
                || outputDevicesChanged
                || effectsChanged
            )
            {
                // Create the input node
                await InitFileInputNodes();

                foreach (var audioGraphContainer in AudioGraphContainers)
                    audioGraphContainer.FileInputNode.AddOutgoingConnection(audioGraphContainer.DeviceOutputNode);

                outputDevicesChanged = false;
                audioFileChanged = false;
                effectsChanged = false;
            }

            isInitialized = true;

            if (IsPlaying)
                foreach (var audioGraphContainer in AudioGraphContainers)
                    audioGraphContainer.AudioGraph.Start();

            isInitializing = false;
        }

        private async Task InitAudioGraph()
        {
            // Save the current position
            var currentPosition = AudioGraphContainers.FirstOrDefault()?.FileInputNode?.Position;

            if (currentPosition.HasValue)
                position = currentPosition.Value;

            // Stop all AudioGraphs
            try
            {
                foreach (var audioGraphContainer in AudioGraphContainers)
                {
                    audioGraphContainer.AudioGraph.Stop();
                    audioGraphContainer.AudioGraph.Dispose();
                }
            }
            catch (Exception e)
            {
                SentrySdk.CaptureException(e);
            }

            AudioGraphContainers.Clear();

            if (OutputDevices.Count > 0)
                foreach (var outputDevice in OutputDevices)
                    AudioGraphContainers.Add(new AudioGraphContainer(await CreateAudioGraph(outputDevice)));
            else
                AudioGraphContainers.Add(new AudioGraphContainer(await CreateAudioGraph(null)));
        }

        private async Task InitFileInputNodes()
        {
            foreach (var audioGraphContainer in AudioGraphContainers)
                audioGraphContainer.FileInputNode?.Stop();

            foreach (var audioGraphContainer in AudioGraphContainers)
            {
                var inputNodeResult = await audioGraphContainer.AudioGraph.CreateFileInputNodeAsync(audioFile);

                if (inputNodeResult.Status != AudioFileNodeCreationStatus.Success)
                {
                    isInitializing = false;
                    throw new FileInputNodeInitException(inputNodeResult.Status);
                }

                audioGraphContainer.FileInputNode?.Dispose();
                audioGraphContainer.FileInputNode = inputNodeResult.FileInputNode;

                audioGraphContainer.FileInputNode.Seek(position);

                if (isMuted)
                    audioGraphContainer.FileInputNode.OutgoingGain = 0;
                else
                    audioGraphContainer.FileInputNode.OutgoingGain = volume;

                audioGraphContainer.FileInputNode.PlaybackSpeedFactor = playbackRate;

                // Fade effect
                audioGraphContainer.FileInputNode.EffectDefinitions.Add(audioGraphContainer.FadeEffectDefinition);
                if (!isFadeInEnabled) audioGraphContainer.FileInputNode.DisableEffectsByDefinition(audioGraphContainer.FadeEffectDefinition);

                // Echo effect
                audioGraphContainer.FileInputNode.EffectDefinitions.Add(audioGraphContainer.EchoEffectDefinition);
                if (!isEchoEnabled) DisableEchoEffect();

                // Limiter effect
                audioGraphContainer.FileInputNode.EffectDefinitions.Add(audioGraphContainer.LimiterEffectDefinition);
                if (!isLimiterEnabled) DisableLimiterEffect();

                // Reverb effect
                audioGraphContainer.FileInputNode.EffectDefinitions.Add(audioGraphContainer.ReverbEffectDefinition);
                if (!IsReverbEnabled) DisableReverbEffect();

                // Pitch shift effect
                audioGraphContainer.FileInputNode.EffectDefinitions.Add(audioGraphContainer.PitchShiftEffectDefinition);
                UpdatePitchShiftEffect();

                audioGraphContainer.FileInputNode.FileCompleted += FileInputNode_FileCompleted;
            }
        }

        private async Task InitDeviceOutputNodes()
        {
            foreach (var audioGraphContainer in AudioGraphContainers)
            {
                if (audioGraphContainer.DeviceOutputNode != null)
                {
                    try
                    {
                        audioGraphContainer.DeviceOutputNode.Stop();
                    }
                    catch (Exception e)
                    {
                        SentrySdk.CaptureException(e);
                    }
                }

                var outputNodeResult = await audioGraphContainer.AudioGraph.CreateDeviceOutputNodeAsync();

                if (outputNodeResult.Status != AudioDeviceNodeCreationStatus.Success)
                {
                    isInitializing = false;
                    throw new DeviceOutputNodeInitException(outputNodeResult.Status);
                }

                audioGraphContainer.DeviceOutputNode = outputNodeResult.DeviceOutputNode;
            }
        }

        public void Play()
        {
            if (!isInitialized)
                throw new AudioPlayerNotInitializedException();

            if (isPlaying) return;

            foreach (var audioGraphContainer in AudioGraphContainers)
            {
                try
                {
                    audioGraphContainer.AudioGraph.Start();
                }
                catch (Exception e)
                {
                    SentrySdk.CaptureException(e);
                    throw new AudioIOException();
                }
            }

            isPlaying = true;
        }

        public void Pause()
        {
            if (!isInitialized)
                throw new AudioPlayerNotInitializedException();

            if (!isPlaying) return;

            foreach (var audioGraphContainer in AudioGraphContainers)
            {
                try
                {
                    audioGraphContainer.AudioGraph.Stop();
                }
                catch (Exception e)
                {
                    SentrySdk.CaptureException(e);
                    throw new AudioIOException();
                }
            }

            DisableFadeInEffect();
            isPlaying = false;
        }

        public async Task FadeOut(int milliseconds)
        {
            EnableFadeOutEffect();
            await Task.Delay(milliseconds);
        }

        #region Effect methods
        #region General effects
        private void InitEffectDefinitions()
        {
            foreach (var audioGraphContainer in AudioGraphContainers)
            {
                audioGraphContainer.FadeEffectDefinition = new AudioEffectDefinition(
                    typeof(FadeAudioEffect).FullName,
                    new PropertySet
                    {
                        { "IsFadeInEnabled", false },
                        { "IsFadeOutEnabled", false },
                        { "FadeInDuration", FadeInDuration },
                        { "FadeOutDuration", FadeOutDuration }
                    }
                );

                audioGraphContainer.EchoEffectDefinition = new EchoEffectDefinition(audioGraphContainer.AudioGraph)
                {
                    Delay = echoDelay,
                    WetDryMix = 0.7f,
                    Feedback = 0.5f
                };

                audioGraphContainer.LimiterEffectDefinition = new LimiterEffectDefinition(audioGraphContainer.AudioGraph)
                {
                    Loudness = (uint)limiterLoudness,
                    Release = 10
                };

                audioGraphContainer.ReverbEffectDefinition = new ReverbEffectDefinition(audioGraphContainer.AudioGraph)
                {
                    WetDryMix = 50,
                    ReflectionsDelay = 120,
                    ReverbDelay = 30,
                    RearDelay = 3,
                    DecayTime = reverbDecay
                };

                audioGraphContainer.PitchShiftEffectDefinition = new AudioEffectDefinition(
                    typeof(PitchShiftAudioEffect).FullName,
                    new PropertySet
                    {
                        { "Pitch", (float)(pitchShiftFactor / playbackRate) }
                    }
                );
            }
        }
        #endregion

        #region Fade in effect
        private void EnableFadeInEffect()
        {
            foreach (var audioGraphContainer in AudioGraphContainers)
            {
                if (
                    audioGraphContainer.FileInputNode == null
                    || audioGraphContainer.FadeEffectDefinition == null
                ) return;

                audioGraphContainer.FadeEffectDefinition.Properties["IsFadeInEnabled"] = true;
                audioGraphContainer.FadeEffectDefinition.Properties["IsFadeOutEnabled"] = false;

                try
                {
                    audioGraphContainer.FileInputNode.EnableEffectsByDefinition(audioGraphContainer.FadeEffectDefinition);
                }
                catch (Exception) { }
            }
        }

        private void DisableFadeInEffect()
        {
            foreach (var audioGraphContainer in AudioGraphContainers)
            {
                if (
                    audioGraphContainer.FileInputNode == null
                    || audioGraphContainer.FadeEffectDefinition == null
                ) return;

                audioGraphContainer.FadeEffectDefinition.Properties["IsFadeInEnabled"] = false;

                try
                {
                    audioGraphContainer.FileInputNode.DisableEffectsByDefinition(audioGraphContainer.FadeEffectDefinition);
                }
                catch (Exception) { }
            }
        }
        #endregion

        #region Fade out effect
        private void EnableFadeOutEffect()
        {
            foreach (var audioGraphContainer in AudioGraphContainers)
            {
                if (
                    audioGraphContainer.FileInputNode == null
                    || audioGraphContainer.FadeEffectDefinition == null
                ) return;

                audioGraphContainer.FadeEffectDefinition.Properties["IsFadeInEnabled"] = false;
                audioGraphContainer.FadeEffectDefinition.Properties["IsFadeOutEnabled"] = true;

                try
                {
                    audioGraphContainer.FileInputNode.EnableEffectsByDefinition(audioGraphContainer.FadeEffectDefinition);
                }
                catch (Exception) { }
            }
        }

        private void DisableFadeOutEffect()
        {
            foreach (var audioGraphContainer in AudioGraphContainers)
            {
                if (
                    audioGraphContainer.FileInputNode == null
                    || audioGraphContainer.FadeEffectDefinition == null
                ) return;

                audioGraphContainer.FadeEffectDefinition.Properties["IsFadeOutEnabled"] = false;

                try
                {
                    audioGraphContainer.FileInputNode.DisableEffectsByDefinition(audioGraphContainer.FadeEffectDefinition);
                }
                catch (Exception) { }
            }
        }
        #endregion

        #region Echo effect
        private void EnableEchoEffect()
        {
            foreach (var audioGraphContainer in AudioGraphContainers)
            {
                if (
                    audioGraphContainer.FileInputNode == null
                    || audioGraphContainer.EchoEffectDefinition == null
                ) return;

                try
                {
                    audioGraphContainer.FileInputNode.EnableEffectsByDefinition(audioGraphContainer.EchoEffectDefinition);
                }
                catch (Exception) { }
            }
        }

        private void DisableEchoEffect()
        {
            foreach (var audioGraphContainer in AudioGraphContainers)
            {
                if (
                    audioGraphContainer.FileInputNode == null
                    || audioGraphContainer.EchoEffectDefinition == null
                ) return;

                try
                {
                    audioGraphContainer.FileInputNode.DisableEffectsByDefinition(audioGraphContainer.EchoEffectDefinition);
                }
                catch (Exception) { }
            }
        }
        #endregion

        #region Limiter effect
        private void EnableLimiterEffect()
        {
            foreach (var audioGraphContainer in AudioGraphContainers)
            {
                if (
                    audioGraphContainer.FileInputNode == null
                    || audioGraphContainer.LimiterEffectDefinition == null
                ) return;

                try
                {
                    audioGraphContainer.FileInputNode.EnableEffectsByDefinition(audioGraphContainer.LimiterEffectDefinition);
                }
                catch (Exception) { }
            }
        }

        private void DisableLimiterEffect()
        {
            foreach (var audioGraphContainer in AudioGraphContainers)
            {
                if (
                    audioGraphContainer.FileInputNode == null
                    || audioGraphContainer.LimiterEffectDefinition == null
                ) return;

                try
                {
                    audioGraphContainer.FileInputNode.DisableEffectsByDefinition(audioGraphContainer.LimiterEffectDefinition);
                }
                catch (Exception) { }
            }
        }
        #endregion

        #region Reverb effect
        private void EnableReverbEffect()
        {
            foreach (var audioGraphContainer in AudioGraphContainers)
            {
                if (
                    audioGraphContainer.FileInputNode == null
                    || audioGraphContainer.ReverbEffectDefinition == null
                ) return;

                try
                {
                    audioGraphContainer.FileInputNode.EnableEffectsByDefinition(audioGraphContainer.ReverbEffectDefinition);
                }
                catch (Exception) { }
            }
        }

        private void DisableReverbEffect()
        {
            foreach (var audioGraphContainer in AudioGraphContainers)
            {
                if (
                    audioGraphContainer.FileInputNode == null
                    || audioGraphContainer.ReverbEffectDefinition == null
                ) return;

                try
                {
                    audioGraphContainer.FileInputNode.DisableEffectsByDefinition(audioGraphContainer.ReverbEffectDefinition);
                }
                catch (Exception) { }
            }
        }
        #endregion

        #region Pitch shift effect
        private void EnablePitchShiftEffect()
        {
            foreach (var audioGraphContainer in AudioGraphContainers)
            {
                if (
                    audioGraphContainer.FileInputNode == null
                    || audioGraphContainer.PitchShiftEffectDefinition == null
                ) return;

                try
                {
                    audioGraphContainer.FileInputNode.EnableEffectsByDefinition(audioGraphContainer.PitchShiftEffectDefinition);
                }
                catch (Exception) { }
            }
        }

        private void DisablePitchShiftEffect()
        {
            foreach (var audioGraphContainer in AudioGraphContainers)
            {
                if (
                    audioGraphContainer.FileInputNode == null
                    || audioGraphContainer.PitchShiftEffectDefinition == null
                ) return;

                try
                {
                    audioGraphContainer.FileInputNode.DisableEffectsByDefinition(audioGraphContainer.PitchShiftEffectDefinition);
                }
                catch (Exception) { }
            }
        }

        private void UpdatePitchShiftEffect()
        {
            if (!IsPitchShiftEnabled && playbackRate == 1)
            {
                DisablePitchShiftEffect();
            }
            else
            {
                double pitch = pitchShiftFactor;
                if (!isPitchShiftEnabled) pitch = 1;

                foreach (var audioGraphContainer in AudioGraphContainers)
                    audioGraphContainer.PitchShiftEffectDefinition.Properties["Pitch"] = (float)(pitch / playbackRate);

                EnablePitchShiftEffect();
            }
        }
        #endregion
        #endregion

        #region Setter methods
        private void setAudioFile(StorageFile value)
        {
            if (audioFile == value) return;

            audioFile = value;
            audioFileChanged = true;
        }

        private void setPosition(TimeSpan value)
        {
            foreach (var audioGraphContainer in AudioGraphContainers)
            {
                if (
                    audioGraphContainer.FileInputNode == null
                    || value > audioGraphContainer.FileInputNode.Duration
                ) continue;

                audioGraphContainer.FileInputNode.Seek(value);
            }
            
            position = value;
            DisableFadeInEffect();
        }

        private void setVolume(double value)
        {
            // Don't set the volume if the player is muted or if the volume didn't change
            if (isMuted || volume.Equals(value)) return;

            if (value > 1)
                value = 1;
            else if (value < 0)
                value = 0;

            foreach (var audioGraphContainer in AudioGraphContainers)
                if (audioGraphContainer.FileInputNode != null)
                    audioGraphContainer.FileInputNode.OutgoingGain = value;

            volume = value;
        }

        private void setIsMuted(bool value)
        {
            // Don't change the value if it didn't change
            if (isMuted.Equals(value)) return;

            foreach (var audioGraphContainer in AudioGraphContainers)
            {
                if (
                    audioGraphContainer.FileInputNode != null
                    && audioGraphContainer.DeviceOutputNode != null
                )
                {
                    if (value)
                        audioGraphContainer.FileInputNode.OutgoingGain = 0;
                    else
                        audioGraphContainer.FileInputNode.OutgoingGain = volume;
                }
            }

            isMuted = value;
        }

        private void setPlaybackRate(double value)
        {
            if (playbackRate.Equals(value))
                return;

            playbackRate = value;

            foreach (var audioGraphContainer in AudioGraphContainers)
                if (audioGraphContainer.FileInputNode != null)
                    audioGraphContainer.FileInputNode.PlaybackSpeedFactor = value;

            UpdatePitchShiftEffect();
        }

        private void setIsFadeInEnabled(bool value)
        {
            isFadeInEnabled = value;

            if (value)
                EnableFadeInEffect();
            else
                DisableFadeOutEffect();
        }

        private void setFadeInDuration(int value)
        {
            if (fadeInDuration.Equals(value))
                return;

            fadeInDuration = value;

            foreach (var audioGraphContainer in AudioGraphContainers)
                if (audioGraphContainer.FadeEffectDefinition != null)
                    audioGraphContainer.FadeEffectDefinition.Properties["FadeInDuration"] = value;
        }

        private void setIsFadeOutEnabled(bool value)
        {
            isFadeOutEnabled = value;

            if (value)
                EnableFadeOutEffect();
            else
                DisableFadeOutEffect();
        }

        private void setFadeOutDuration(int value)
        {
            if (fadeOutDuration.Equals(value))
                return;

            fadeOutDuration = value;

            foreach (var audioGraphContainer in AudioGraphContainers)
                if (audioGraphContainer.FadeEffectDefinition != null)
                    audioGraphContainer.FadeEffectDefinition.Properties["FadeOutDuration"] = value;
        }

        private void setIsEchoEnabled(bool value)
        {
            if (isEchoEnabled.Equals(value))
                return;

            isEchoEnabled = value;

            if (value)
                EnableEchoEffect();
            else
                DisableEchoEffect();
        }

        private void setEchoDelay(int value)
        {
            if (echoDelay.Equals(value))
                return;

            echoDelay = value;

            foreach (var audioGraphContainer in AudioGraphContainers)
                if (audioGraphContainer.EchoEffectDefinition != null)
                    audioGraphContainer.EchoEffectDefinition.Delay = value;
        }

        private void setIsLimiterEnabled(bool value)
        {
            if (isLimiterEnabled.Equals(value))
                return;

            isLimiterEnabled = value;

            if (value)
                EnableLimiterEffect();
            else
                DisableLimiterEffect();
        }

        private void setLimiterLoudness(int value)
        {
            if (limiterLoudness.Equals(value))
                return;

            limiterLoudness = value;

            foreach (var audioGraphContainer in AudioGraphContainers)
                if (audioGraphContainer.LimiterEffectDefinition != null)
                    audioGraphContainer.LimiterEffectDefinition.Loudness = (uint)value;
        }

        private void setIsReverbEnabled(bool value)
        {
            if (isReverbEnabled.Equals(value))
                return;

            isReverbEnabled = value;

            if (value)
                EnableReverbEffect();
            else
                DisableReverbEffect();
        }

        private void setReverbDecay(double value)
        {
            if (reverbDecay.Equals(value))
                return;

            reverbDecay = value;

            foreach (var audioGraphContainer in AudioGraphContainers)
                if (audioGraphContainer.ReverbEffectDefinition != null)
                    audioGraphContainer.ReverbEffectDefinition.DecayTime = value;
        }

        private void setIsPitchShiftEnabled(bool value)
        {
            if (isPitchShiftEnabled.Equals(value))
                return;

            isPitchShiftEnabled = value;
            UpdatePitchShiftEffect();
        }

        private void setPitchShiftFactor(double value)
        {
            pitchShiftFactor = value;
            UpdatePitchShiftEffect();
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

        private void OutputDevices_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            outputDevicesChanged = true;
        }
        #endregion

        #region Utility methods
        private async Task<AudioGraph> CreateAudioGraph(DeviceInformation outputDevice)
        {
            var settings = new AudioGraphSettings(AudioRenderCategory.Media)
            {
                PrimaryRenderDevice = outputDevice
            };

            var createAudioGraphResult = await AudioGraph.CreateAsync(settings);

            if (createAudioGraphResult.Status != AudioGraphCreationStatus.Success)
            {
                isInitializing = false;
                throw new AudioGraphInitException(createAudioGraphResult.Status);
            }

            createAudioGraphResult.Graph.UnrecoverableErrorOccurred += AudioGraph_UnrecoverableErrorOccurred;
            return createAudioGraphResult.Graph;
        }
        #endregion
    }
}
