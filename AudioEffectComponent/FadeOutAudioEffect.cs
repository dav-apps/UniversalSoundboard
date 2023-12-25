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
    public sealed class FadeOutAudioEffect : IBasicAudioEffect
    {
        private int sampleIndex = 0;
        private int effectSampleCount = 0;
        private AudioEncodingProperties currentEncodingProperties;
        IPropertySet configuration;

        public bool TimeIndependent { get { return true; } }
        public bool UseInputFrameForOutput { get { return false; } }

        public bool IsEnabled
        {
            get
            {
                object val;

                if (configuration != null && configuration.TryGetValue("IsEnabled", out val))
                    return (bool)val;

                return false;
            }
        }

        // Duration of the fade out in ms
        public float Duration
        {
            get
            {
                object val;

                if (configuration != null && configuration.TryGetValue("Duration", out val))
                    return (float)val;

                return 1000f;
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
            effectSampleCount = (int)(encodingProperties.SampleRate * encodingProperties.ChannelCount * ((double)Duration / 1000));
            sampleIndex = effectSampleCount;

            configuration.MapChanged -= Configuration_MapChanged;
            configuration.MapChanged += Configuration_MapChanged;
        }

        private void Configuration_MapChanged(IObservableMap<string, object> sender, IMapChangedEventArgs<string> @event)
        {
            effectSampleCount = (int)(currentEncodingProperties.SampleRate * currentEncodingProperties.ChannelCount * ((double)Duration / 1000));

            if (IsEnabled)
                sampleIndex = effectSampleCount;
            else
                sampleIndex = 0;
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
                    if (IsEnabled)
                    {
                        outputDataInFloat[i] = inputDataInFloat[i] * ((float)sampleIndex / effectSampleCount);

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
            effectSampleCount = 0;
            sampleIndex = 0;
        }
    }
}
