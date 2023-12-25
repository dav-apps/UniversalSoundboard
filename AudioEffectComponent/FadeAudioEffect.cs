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
        private uint sampleRate = 0;
        private uint channelCount = 0;
        private int sampleIndex = 0;
        private int fadeInEffectSampleCount = 0;
        private int fadeOutEffectSampleCount = 0;
        private AudioEncodingProperties currentEncodingProperties;
        IPropertySet configuration;

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

            fadeInEffectSampleCount = (int)(sampleRate * channelCount * ((double)FadeInDuration / 1000));
            fadeOutEffectSampleCount = (int)(sampleRate * channelCount * ((double)FadeOutDuration / 1000));

            configuration.MapChanged -= Configuration_MapChanged;
            configuration.MapChanged += Configuration_MapChanged;
        }

        private void Configuration_MapChanged(IObservableMap<string, object> sender, IMapChangedEventArgs<string> @event)
        {
            fadeInEffectSampleCount = (int)(sampleRate * channelCount * ((double)FadeInDuration / 1000));
            fadeOutEffectSampleCount = (int)(sampleRate * channelCount * ((double)FadeOutDuration / 1000));

            if (IsFadeInEnabled)
                sampleIndex = 0;
            else if (IsFadeOutEnabled)
                sampleIndex = fadeOutEffectSampleCount;
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

                for (int i = 0; i < dataInFloatLength; i++)
                {
                    if (IsFadeInEnabled)
                    {
                        outputDataInFloat[i] = inputDataInFloat[i] * ((float)sampleIndex / fadeInEffectSampleCount);

                        if (sampleIndex < fadeInEffectSampleCount)
                            sampleIndex++;
                    }
                    else if (IsFadeOutEnabled)
                    {
                        outputDataInFloat[i] = inputDataInFloat[i] * ((float)sampleIndex / fadeOutEffectSampleCount);

                        if (sampleIndex > 0)
                            sampleIndex--;
                    }
                    else
                    {
                        outputDataInFloat[i] = inputDataInFloat[i];
                    }
                }
            }
        }

        public void Close(MediaEffectClosedReason reason) { }

        public void DiscardQueuedFrames()
        {
            fadeInEffectSampleCount = 0;
            fadeOutEffectSampleCount = 0;
            sampleIndex = 0;
        }
    }
}
