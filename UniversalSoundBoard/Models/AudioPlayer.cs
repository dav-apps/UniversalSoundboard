using AudioEffectComponent;
using Sentry;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Threading;
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
        private double fadeGain = 1;
        private CancellationTokenSource fadeCancellationTokenSource;
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

                audioGraphContainer.FileInputNode.PlaybackSpeedFactor = playbackRate;

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

            // The nodes are new, so volume, muted and a running fade have to be applied again
            ApplyOutgoingGain();
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

            CancelFade();

            isFadeInEnabled = false;
            isFadeOutEnabled = false;

            // The graph is already stopped, so restoring the gain here is inaudible and the next
            // playback does not start out silent after a fade out
            fadeGain = 1;
            ApplyOutgoingGain();

            isPlaying = false;
        }

        /**
         * Fades the volume out and returns when the fade has finished.
         */
        public async Task FadeOut(int milliseconds)
        {
            var token = RestartFade();

            isFadeInEnabled = false;
            isFadeOutEnabled = true;

            // Start from the current gain, so interrupting a running fade in does not make the
            // volume jump up to full before fading out
            await RunFade(fadeGain, 0, milliseconds, token);

            if (!token.IsCancellationRequested)
                isFadeOutEnabled = false;
        }

        #region Effect methods
        #region General effects
        private void InitEffectDefinitions()
        {
            foreach (var audioGraphContainer in AudioGraphContainers)
            {
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

        #region Fade
        /**
         * The fades ramp the OutgoingGain of the input nodes instead of running a per sample audio
         * effect.
         *
         * A managed IBasicAudioEffect runs on the real time audio thread and allocates COM objects
         * for every audio quantum, and it has to be inserted into and removed from the node's effect
         * chain while playback is running. On a latency sensitive endpoint - a Bluetooth headset,
         * for example - that is the most fragile part of the pipeline. Ramping the gain keeps the
         * audio thread free of managed code entirely. At a 15 ms step a 10 s fade still has more
         * than 600 steps, and AudioGraph smooths gain changes on its own.
         */
        private const int fadeStepMilliseconds = 15;

        /**
         * Cancels a running fade and returns the token for the new one.
         */
        private CancellationToken RestartFade()
        {
            CancelFade();

            fadeCancellationTokenSource = new CancellationTokenSource();
            return fadeCancellationTokenSource.Token;
        }

        private void CancelFade()
        {
            if (fadeCancellationTokenSource == null) return;

            fadeCancellationTokenSource.Cancel();
            fadeCancellationTokenSource.Dispose();
            fadeCancellationTokenSource = null;
        }

        /**
         * Ramps the fade gain from one value to another. The progress is derived from the elapsed
         * time and not from the number of steps, so a delayed step does not stretch the fade.
         */
        private async Task RunFade(double from, double to, int milliseconds, CancellationToken token)
        {
            var stopwatch = Stopwatch.StartNew();

            while (!token.IsCancellationRequested)
            {
                double progress = milliseconds <= 0
                    ? 1
                    : (double)stopwatch.ElapsedMilliseconds / milliseconds;

                if (progress >= 1) break;

                fadeGain = from + (to - from) * progress;
                ApplyOutgoingGain();

                try
                {
                    await Task.Delay(fadeStepMilliseconds, token);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }

            if (token.IsCancellationRequested) return;

            fadeGain = to;
            ApplyOutgoingGain();
        }

        private async void RunFadeIn()
        {
            var token = RestartFade();

            // Drop to silence before the graph is started, so the fade in cannot begin with a burst
            fadeGain = 0;
            ApplyOutgoingGain();

            await RunFade(0, 1, fadeInDuration, token);

            if (!token.IsCancellationRequested)
                isFadeInEnabled = false;
        }

        /**
         * Applies volume, muted and the current fade gain to all input nodes.
         */
        private void ApplyOutgoingGain()
        {
            double gain = (isMuted ? 0 : volume) * fadeGain;

            foreach (var audioGraphContainer in AudioGraphContainers)
            {
                if (audioGraphContainer.FileInputNode == null) continue;

                try
                {
                    audioGraphContainer.FileInputNode.OutgoingGain = gain;
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

            // Seeking cancels a running fade in
            if (isFadeInEnabled)
            {
                isFadeInEnabled = false;
                CancelFade();
                fadeGain = 1;
                ApplyOutgoingGain();
            }
        }

        private void setVolume(double value)
        {
            // Don't set the volume if it didn't change
            if (volume.Equals(value)) return;

            if (value > 1)
                value = 1;
            else if (value < 0)
                value = 0;

            volume = value;
            ApplyOutgoingGain();
        }

        private void setIsMuted(bool value)
        {
            // Don't change the value if it didn't change
            if (isMuted.Equals(value)) return;

            isMuted = value;
            ApplyOutgoingGain();
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
            if (isFadeInEnabled.Equals(value))
                return;

            isFadeInEnabled = value;

            if (value)
            {
                RunFadeIn();
            }
            else
            {
                // The fade in was ended from the outside, jump to the full volume
                CancelFade();
                fadeGain = 1;
                ApplyOutgoingGain();
            }
        }

        private void setFadeInDuration(int value)
        {
            if (fadeInDuration.Equals(value))
                return;

            fadeInDuration = value;
        }

        private void setIsFadeOutEnabled(bool value)
        {
            if (isFadeOutEnabled.Equals(value))
                return;

            if (value)
            {
                // Not awaited on purpose - FadeOut is the API for starting a fade out and waiting
                // for it, this setter only exists so the property stays symmetric
                var fadeOutTask = FadeOut(fadeOutDuration);
            }
            else
            {
                isFadeOutEnabled = false;
                CancelFade();
                fadeGain = 1;
                ApplyOutgoingGain();
            }
        }

        private void setFadeOutDuration(int value)
        {
            if (fadeOutDuration.Equals(value))
                return;

            fadeOutDuration = value;
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
