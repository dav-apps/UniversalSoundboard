using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media;
using Windows.Media.Effects;
using Windows.Media.MediaProperties;

namespace AudioEffectComponent
{
    public sealed class FadeAudioEffect : IBasicAudioEffect
    {
        private const int FadeModeNone = 0;
        private const int FadeModeIn = 1;
        private const int FadeModeOut = 2;

        private uint sampleRate = 0;
        private uint channelCount = 0;
        private AudioEncodingProperties currentEncodingProperties;
        IPropertySet configuration;

        // Snapshot of the configuration for the audio thread.
        // ProcessFrame runs on the real-time audio thread and must never read the PropertySet,
        // so everything it needs is cached in these fields. They are only written from
        // ApplyConfiguration, which runs on the thread that changes the properties.
        private volatile int fadeMode = FadeModeNone;
        private volatile int fadeSampleCount = 1;
        private volatile float gainPerSample = 0f;
        private volatile int sampleIndex = 0;

        public bool TimeIndependent { get { return true; } }
        public bool UseInputFrameForOutput { get { return false; } }

        public bool IsFadeInEnabled
        {
            get
            {
                object val;

                if (configuration != null && configuration.TryGetValue("IsFadeInEnabled", out val))
                    return (bool)val;

                return false;
            }
        }

        public bool IsFadeOutEnabled
        {
            get
            {
                object val;

                if (configuration != null && configuration.TryGetValue("IsFadeOutEnabled", out val))
                    return (bool)val;

                return false;
            }
        }

        public int FadeInDuration
        {
            get
            {
                object val;

                if (configuration != null && configuration.TryGetValue("FadeInDuration", out val))
                    return (int)val;

                return 1000;
            }
        }

        public int FadeOutDuration
        {
            get
            {
                object val;

                if (configuration != null && configuration.TryGetValue("FadeOutDuration", out val))
                    return (int)val;

                return 1000;
            }
        }

        public IReadOnlyList<AudioEncodingProperties> SupportedEncodingProperties
        {
            get
            {
                var supportedEncodingProperties = new List<AudioEncodingProperties>();
                AudioEncodingProperties encodingProps1 = AudioEncodingProperties.CreatePcm(44100, 1, 32);
                encodingProps1.Subtype = MediaEncodingSubtypes.Float;
                AudioEncodingProperties encodingProps2 = AudioEncodingProperties.CreatePcm(48000, 1, 32);
                encodingProps2.Subtype = MediaEncodingSubtypes.Float;

                supportedEncodingProperties.Add(encodingProps1);
                supportedEncodingProperties.Add(encodingProps2);

                return supportedEncodingProperties;
            }
        }

        public void SetEncodingProperties(AudioEncodingProperties encodingProperties)
        {
            currentEncodingProperties = encodingProperties;
            sampleRate = encodingProperties.SampleRate;
            channelCount = encodingProperties.ChannelCount;

            if (configuration != null)
            {
                configuration.MapChanged -= Configuration_MapChanged;
                configuration.MapChanged += Configuration_MapChanged;
            }

            ApplyConfiguration();
        }

        private void Configuration_MapChanged(IObservableMap<string, object> sender, IMapChangedEventArgs<string> @event)
        {
            ApplyConfiguration();
        }

        /**
         * Reads the configuration into the fields that ProcessFrame uses.
         *
         * The ramp position is only reset when the fade mode actually changes. Enabling a fade
         * writes two properties, and other code paths rewrite the same values without a change,
         * so resetting on every MapChanged would restart the ramp several times per fade and
         * make it audibly jump.
         */
        private void ApplyConfiguration()
        {
            int newMode = FadeModeNone;

            if (IsFadeInEnabled)
                newMode = FadeModeIn;
            else if (IsFadeOutEnabled)
                newMode = FadeModeOut;

            int duration = newMode == FadeModeOut ? FadeOutDuration : FadeInDuration;
            int newCount = (int)(sampleRate * channelCount * ((double)duration / 1000));

            // Never let the sample count reach 0, ProcessFrame divides the ramp by it
            if (newCount < 1) newCount = 1;

            if (newMode != fadeMode)
            {
                // A new fade begins, start the ramp at the appropriate end
                sampleIndex = newMode == FadeModeOut ? newCount : 0;
            }
            else if (newCount != fadeSampleCount)
            {
                // Only the duration changed, keep the current position within the ramp
                sampleIndex = (int)((long)sampleIndex * newCount / fadeSampleCount);
            }

            fadeSampleCount = newCount;
            gainPerSample = 1f / newCount;
            fadeMode = newMode;
        }

        public void SetProperties(IPropertySet configuration)
        {
            this.configuration = configuration;
        }

        [ComImport]
        [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        unsafe interface IMemoryBufferByteAccess
        {
            void GetBuffer(out byte* buffer, out uint capacity);
        }

        unsafe public void ProcessFrame(ProcessAudioFrameContext context)
        {
            AudioFrame inputFrame = context.InputFrame;
            AudioFrame outputFrame = context.OutputFrame;

            using (
                AudioBuffer inputBuffer = inputFrame.LockBuffer(AudioBufferAccessMode.Read),
                outputBuffer = outputFrame.LockBuffer(AudioBufferAccessMode.Write)
            )
            using (
                IMemoryBufferReference inputReference = inputBuffer.CreateReference(),
                outputReference = outputBuffer.CreateReference()
            )
            {
                byte* inputDataInBytes;
                byte* outputDataInBytes;
                uint inputCapacity;
                uint outputCapacity;

                ((IMemoryBufferByteAccess)inputReference).GetBuffer(out inputDataInBytes, out inputCapacity);
                ((IMemoryBufferByteAccess)outputReference).GetBuffer(out outputDataInBytes, out outputCapacity);

                float* inputDataInFloat = (float*)inputDataInBytes;
                float* outputDataInFloat = (float*)outputDataInBytes;

                // Process audio data
                int dataInFloatLength = (int)inputBuffer.Length / sizeof(float);

                // Take a snapshot of the fade state once per callback. Reading the fields per
                // sample would let a configuration change tear the ramp apart mid-buffer, and
                // the branch does not need to be re-evaluated for every sample.
                int mode = fadeMode;
                int count = fadeSampleCount;
                float step = gainPerSample;
                int index = sampleIndex;

                if (mode == FadeModeIn)
                {
                    for (int i = 0; i < dataInFloatLength; i++)
                    {
                        float gain = index * step;
                        if (gain > 1f) gain = 1f;

                        outputDataInFloat[i] = inputDataInFloat[i] * gain;

                        if (index < count) index++;
                    }

                    sampleIndex = index;
                }
                else if (mode == FadeModeOut)
                {
                    for (int i = 0; i < dataInFloatLength; i++)
                    {
                        float gain = index * step;
                        if (gain > 1f) gain = 1f;

                        outputDataInFloat[i] = inputDataInFloat[i] * gain;

                        if (index > 0) index--;
                    }

                    sampleIndex = index;
                }
                else
                {
                    for (int i = 0; i < dataInFloatLength; i++)
                        outputDataInFloat[i] = inputDataInFloat[i];
                }
            }
        }

        public void Close(MediaEffectClosedReason reason)
        {
            // The PropertySet outlives this effect instance, so the handler has to be removed
            // here. Otherwise every closed effect stays subscribed to it for the lifetime of
            // the AudioGraphContainer.
            if (configuration != null)
                configuration.MapChanged -= Configuration_MapChanged;
        }

        public void DiscardQueuedFrames()
        {
            // Nothing to discard, the effect holds no queued audio. The ramp position must not
            // be touched here either: this is also called while a fade is running, and resetting
            // the sample counts to 0 would divide the ramp by zero on the next frame.
        }
    }
}
