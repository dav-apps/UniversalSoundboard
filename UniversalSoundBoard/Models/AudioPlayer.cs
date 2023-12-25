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

        private AudioGraph AudioGraph;
        private AudioFileInputNode FileInputNode;
        private AudioDeviceOutputNode DeviceOutputNode;

        private AudioEffectDefinition fadeEffectDefinition;
        private EchoEffectDefinition echoEffectDefinition;
        private LimiterEffectDefinition limiterEffectDefinition;
        private ReverbEffectDefinition reverbEffectDefinition;
        private AudioEffectDefinition pitchShiftEffectDefinition;

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
                || effectsChanged
            )
            {
                // Create the input node
                await InitFileInputNode();

                if (DeviceOutputNode != null)
                    FileInputNode.AddOutgoingConnection(DeviceOutputNode);

                outputDeviceChanged = false;
                audioFileChanged = false;
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

            // Fade effect
            FileInputNode.EffectDefinitions.Add(fadeEffectDefinition);

            // Echo effect
            FileInputNode.EffectDefinitions.Add(echoEffectDefinition);
            if (!isEchoEnabled) DisableEchoEffect();

            // Limiter effect
            FileInputNode.EffectDefinitions.Add(limiterEffectDefinition);
            if (!isLimiterEnabled) DisableLimiterEffect();

            // Reverb effect
            FileInputNode.EffectDefinitions.Add(reverbEffectDefinition);
            if (!isReverbEnabled) DisableReverbEffect();

            // Pitch shift effect
            FileInputNode.EffectDefinitions.Add(pitchShiftEffectDefinition);
            UpdatePitchShiftEffect();

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
            fadeEffectDefinition = new AudioEffectDefinition(
                typeof(FadeAudioEffect).FullName,
                new PropertySet
                {
                    { "IsFadeInEnabled", false },
                    { "IsFadeOutEnabled", false },
                    { "FadeInDuration", FadeInDuration },
                    { "FadeOutDuration", FadeOutDuration }
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

            reverbEffectDefinition = new ReverbEffectDefinition(AudioGraph)
            {
                WetDryMix = 50,
                ReflectionsDelay = 120,
                ReverbDelay = 30,
                RearDelay = 3,
                DecayTime = reverbDecay
            };

            pitchShiftEffectDefinition = new AudioEffectDefinition(
                typeof(PitchShiftAudioEffect).FullName,
                new PropertySet
                {
                    { "Pitch", (float)(pitchShiftFactor / playbackRate) }
                }
            );
        }
        #endregion

        #region Fade in effect
        private void EnableFadeInEffect()
        {
            if (FileInputNode == null || fadeEffectDefinition == null)
                return;

            fadeEffectDefinition.Properties["IsFadeInEnabled"] = true;
            fadeEffectDefinition.Properties["IsFadeOutEnabled"] = false;
        }

        private void DisableFadeInEffect()
        {
            if (FileInputNode == null || fadeEffectDefinition == null)
                return;

            fadeEffectDefinition.Properties["IsFadeInEnabled"] = false;
        }
        #endregion

        #region Fade out effect
        private void EnableFadeOutEffect()
        {
            if (FileInputNode == null || fadeEffectDefinition == null)
                return;

            fadeEffectDefinition.Properties["IsFadeInEnabled"] = false;
            fadeEffectDefinition.Properties["IsFadeOutEnabled"] = true;
        }

        private void DisableFadeOutEffect()
        {
            if (FileInputNode == null || fadeEffectDefinition == null)
                return;

            fadeEffectDefinition.Properties["IsFadeOutEnabled"] = false;
        }
        #endregion

        #region Echo effect
        private void EnableEchoEffect()
        {
            if (FileInputNode == null || echoEffectDefinition == null)
                return;

            try
            {
                FileInputNode.EnableEffectsByDefinition(echoEffectDefinition);
            }
            catch (Exception) { }
        }

        private void DisableEchoEffect()
        {
            if (FileInputNode == null || echoEffectDefinition == null)
                return;

            try
            {
                FileInputNode.DisableEffectsByDefinition(echoEffectDefinition);
            }
            catch (Exception) { }
        }
        #endregion

        #region Limiter effect
        private void EnableLimiterEffect()
        {
            if (FileInputNode == null || limiterEffectDefinition == null)
                return;

            try
            {
                FileInputNode.EnableEffectsByDefinition(limiterEffectDefinition);
            }
            catch (Exception) { }
        }

        private void DisableLimiterEffect()
        {
            if (FileInputNode == null || limiterEffectDefinition == null)
                return;
            
            try
            {
                FileInputNode.DisableEffectsByDefinition(limiterEffectDefinition);
            }
            catch (Exception) { }
        }
        #endregion

        #region Reverb effect
        private void EnableReverbEffect()
        {
            if (FileInputNode == null || reverbEffectDefinition == null)
                return;

            try
            {
                FileInputNode.EnableEffectsByDefinition(reverbEffectDefinition);
            }
            catch (Exception) { }
        }

        private void DisableReverbEffect()
        {
            if (FileInputNode == null || reverbEffectDefinition == null)
                return;

            try
            {
                FileInputNode.DisableEffectsByDefinition(reverbEffectDefinition);
            }
            catch (Exception) { }
        }
        #endregion

        #region Pitch shift effect
        private void EnablePitchShiftEffect()
        {
            if (FileInputNode == null || pitchShiftEffectDefinition == null)
                return;

            try
            {
                FileInputNode.EnableEffectsByDefinition(pitchShiftEffectDefinition);
            }
            catch (Exception) { }
        }

        private void DisablePitchShiftEffect()
        {
            if (FileInputNode == null || pitchShiftEffectDefinition == null)
                return;

            try
            {
                FileInputNode.DisableEffectsByDefinition(pitchShiftEffectDefinition);
            }
            catch (Exception) { }
        }

        private void UpdatePitchShiftEffect()
        {
            if (pitchShiftEffectDefinition == null) return;

            if (!IsPitchShiftEnabled && playbackRate == 1)
            {
                DisablePitchShiftEffect();
            }
            else
            {
                double pitch = pitchShiftFactor;
                if (!isPitchShiftEnabled) pitch = 1;

                pitchShiftEffectDefinition.Properties["Pitch"] = (float)(pitch / playbackRate);
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

        private void setOutputDevice(DeviceInformation value)
        {
            if (outputDevice == value) return;

            outputDevice = value;
            outputDeviceChanged = true;
        }

        private void setPosition(TimeSpan value)
        {
            if (FileInputNode != null && value > FileInputNode.Duration)
                return;
            
            FileInputNode?.Seek(value);
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

            if (FileInputNode != null)
                FileInputNode.OutgoingGain = value;

            volume = value;
        }

        private void setIsMuted(bool value)
        {
            // Don't change the value if it didn't change
            if (isMuted.Equals(value)) return;

            if (FileInputNode != null && DeviceOutputNode != null)
            {
                if (value)
                    FileInputNode.OutgoingGain = 0;
                else
                    FileInputNode.OutgoingGain = volume;
            }

            isMuted = value;
        }

        private void setPlaybackRate(double value)
        {
            if (playbackRate.Equals(value))
                return;

            playbackRate = value;

            if (FileInputNode == null)
                return;

            FileInputNode.PlaybackSpeedFactor = value;
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

            if (fadeEffectDefinition != null)
                fadeEffectDefinition.Properties["FadeInDuration"] = value;
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

            if (fadeEffectDefinition != null)
                fadeEffectDefinition.Properties["FadeOutDuration"] = value;
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

            if (echoEffectDefinition != null)
                echoEffectDefinition.Delay = value;
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

            if (limiterEffectDefinition != null)
                limiterEffectDefinition.Loudness = (uint)value;
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

            if (reverbEffectDefinition != null)
                reverbEffectDefinition.DecayTime = value;
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
        #endregion
    }
}
