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
        private bool isReverbEnabled = false;
        private double reverbDecay = 2;
        private bool isPitchShiftEnabled = false;
        private double pitchShiftFactor = 1;

        private AudioGraph AudioGraph;
        private AudioFileInputNode FileInputNode;
        private AudioDeviceOutputNode DeviceOutputNode;

        private AudioEffectDefinition fadeInEffectDefinition;
        private AudioEffectDefinition fadeOutEffectDefinition;
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
            if (!isFadeInEnabled) DisableEffect(fadeInEffectDefinition);

            // Fade out effect
            FileInputNode.EffectDefinitions.Add(fadeOutEffectDefinition);
            DisableEffect(fadeOutEffectDefinition);

            FileInputNode.EffectDefinitions.Add(echoEffectDefinition);
            if (!isEchoEnabled) DisableEchoEffect();

            FileInputNode.EffectDefinitions.Add(limiterEffectDefinition);
            if (!isLimiterEnabled) DisableLimiterEffect();

            FileInputNode.EffectDefinitions.Add(reverbEffectDefinition);
            if (!isReverbEnabled) DisableReverbEffect();

            FileInputNode.EffectDefinitions.Add(pitchShiftEffectDefinition);
            if (!isPitchShiftEnabled) DisablePitchShiftEffect();

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

            DisableEffect(fadeInEffectDefinition);
            isPlaying = false;
        }

        public async Task FadeOut(int milliseconds)
        {
            EnableEffect(fadeOutEffectDefinition);
            await Task.Delay(milliseconds);
        }

        #region Effect methods
        #region General effects
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
                    { "Pitch", (float)pitchShiftFactor }
                }
            );
        }

        private void EnableEffect(AudioEffectDefinition effectDefinition)
        {
            if (FileInputNode == null || effectDefinition == null)
                return;

            try
            {
                FileInputNode.EnableEffectsByDefinition(effectDefinition);
            }
            catch (Exception) { }
        }

        private void DisableEffect(AudioEffectDefinition effectDefinition)
        {
            if (FileInputNode == null || effectDefinition == null)
                return;

            try
            {
                FileInputNode.DisableEffectsByDefinition(effectDefinition);
            }
            catch (Exception) { }
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
            DisableEffect(fadeInEffectDefinition);
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

        private async void setPlaybackRate(double value)
        {
            // Don't change the value if it didn't change
            if (playbackRate.Equals(value))
                return;

            playbackRate = value;

            if (FileInputNode == null)
                return;

            FileInputNode.PlaybackSpeedFactor = value;
            playbackRateChanged = true;

            await Init();
        }

        private void setIsFadeInEnabled(bool value)
        {
            if (isFadeInEnabled.Equals(value))
                return;

            isFadeInEnabled = value;
        }

        private void setFadeInDuration(int value)
        {
            if (fadeInDuration.Equals(value))
                return;

            fadeInDuration = value;

            if (fadeInEffectDefinition != null)
                fadeInEffectDefinition.Properties["Duration"] = value;
        }

        private void setIsFadeOutEnabled(bool value)
        {
            if (isFadeOutEnabled.Equals(value))
                return;

            isFadeOutEnabled = value;
        }

        private void setFadeOutDuration(int value)
        {
            if (fadeOutDuration.Equals(value))
                return;

            fadeOutDuration = value;

            if (fadeOutEffectDefinition != null)
                fadeOutEffectDefinition.Properties["Duration"] = (float)value;
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

            if (isPitchShiftEnabled)
                EnablePitchShiftEffect();
            else // TODO: Check for playback rate
                DisablePitchShiftEffect();
        }

        private void setPitchShiftFactor(double value)
        {
            if (pitchShiftFactor.Equals(value))
                return;

            pitchShiftFactor = value;

            if (pitchShiftEffectDefinition != null)
                pitchShiftEffectDefinition.Properties["Pitch"] = (float)value;
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
