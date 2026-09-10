using System;
using System.Collections.Generic;
using Windows.Media.Effects;
using Windows.Media.MediaProperties;
using Windows.Foundation.Collections;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using WinRT;
using Windows.Media;
using Windows.Foundation;

namespace AudioEffectComponent
{
    public sealed partial class PitchShiftAudioEffect : IBasicAudioEffect
    {
        private AudioEncodingProperties currentEncodingProperties;
        PropertySet configuration;
        private PitchShifter[] shifters = Array.Empty<PitchShifter>();
        private readonly float[] channelBuffer = new float[2048];
        private bool bypassed = true;

        public bool TimeIndependent { get { return false; } }
        public bool UseInputFrameForOutput { get { return false; } }

        public IReadOnlyList<AudioEncodingProperties> SupportedEncodingProperties
        {
            get
            {
                var supportedEncodingProperties = new List<AudioEncodingProperties>();
                foreach (uint sampleRate in new uint[] { 44100, 48000 })
                foreach (uint channels in new uint[] { 1, 2 })
                {
                    var properties = AudioEncodingProperties.CreatePcm(sampleRate, channels, 32);
                    properties.Subtype = MediaEncodingSubtypes.Float;
                    supportedEncodingProperties.Add(properties);
                }

                return supportedEncodingProperties;
            }
        }

        public void SetEncodingProperties(AudioEncodingProperties encodingProperties)
        {
            currentEncodingProperties = encodingProperties;
            shifters = new PitchShifter[encodingProperties.ChannelCount];
            for (int channel = 0; channel < shifters.Length; channel++)
                shifters[channel] = new PitchShifter();
            bypassed = true;
        }

        public void SetProperties(IPropertySet configuration)
        {
            // Use the SDK's concrete, AOT-safe map projection instead of dynamic
            // dispatch through IPropertySet's inherited IDictionary interface.
            this.configuration = configuration?.As<PropertySet>();
        }

        public float Pitch
        {
            get
            {
                if (configuration != null && configuration.TryGetValue("Pitch", out object val))
                    return (float)val;

                return 1f;
            }
        }

        [GeneratedComInterface]
        [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal unsafe partial interface IMemoryBufferByteAccess
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
                inputReference.As<IMemoryBufferByteAccess>().GetBuffer(out byte* inputDataInBytes, out uint inputCapacity);
                outputReference.As<IMemoryBufferByteAccess>().GetBuffer(out byte* outputDataInBytes, out uint outputCapacity);

                float* inputDataInFloat = (float*)inputDataInBytes;
                float* outputDataInFloat = (float*)outputDataInBytes;

                // Convert the audio data to an array, as the input for PitchShift
                int dataInFloatLength = (int)inputBuffer.Length / sizeof(float);

                float pitch = Pitch;
                if (!float.IsFinite(pitch) || pitch <= 0) pitch = 1;
                if (pitch == 1)
                {
                    if (!bypassed) DiscardQueuedFrames();
                    bypassed = true;
                    for (int i = 0; i < dataInFloatLength; i++)
                        outputDataInFloat[i] = inputDataInFloat[i];
                }
                else
                {
                    bypassed = false;
                    int channels = shifters.Length;
                    int frames = dataInFloatLength / channels;
                    for (int offset = 0; offset < frames; offset += channelBuffer.Length)
                    {
                        int count = Math.Min(channelBuffer.Length, frames - offset);
                        for (int channel = 0; channel < channels; channel++)
                        {
                            for (int i = 0; i < count; i++)
                                channelBuffer[i] = inputDataInFloat[(offset + i) * channels + channel];
                            shifters[channel].PitchShift(pitch, count, currentEncodingProperties.SampleRate, channelBuffer.AsSpan(0, count));
                            for (int i = 0; i < count; i++)
                                outputDataInFloat[(offset + i) * channels + channel] = channelBuffer[i];
                        }
                    }
                }
            }
        }

        public void Close(MediaEffectClosedReason reason) { }

        public void DiscardQueuedFrames()
        {
            foreach (var shifter in shifters) shifter.Reset();
        }
    }
}
