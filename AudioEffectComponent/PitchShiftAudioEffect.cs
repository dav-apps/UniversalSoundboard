using System;
using System.Collections.Generic;
using Windows.Media.Effects;
using Windows.Media.MediaProperties;
using Windows.Foundation.Collections;
using System.Runtime.InteropServices;
using Windows.Media;
using Windows.Foundation;

namespace AudioEffectComponent
{
    public sealed class PitchShiftAudioEffect : IBasicAudioEffect
    {
        private AudioEncodingProperties currentEncodingProperties;
        IPropertySet configuration;

        public bool TimeIndependent { get { return true; } }
        public bool UseInputFrameForOutput { get { return false; } }

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
        }

        public void SetProperties(IPropertySet configuration)
        {
            this.configuration = configuration;
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
                ((IMemoryBufferByteAccess)inputReference).GetBuffer(out byte* inputDataInBytes, out uint inputCapacity);
                ((IMemoryBufferByteAccess)outputReference).GetBuffer(out byte* outputDataInBytes, out uint outputCapacity);

                float* inputDataInFloat = (float*)inputDataInBytes;
                float* outputDataInFloat = (float*)outputDataInBytes;

                // Convert the audio data to an array, as the input for PitchShift
                int dataInFloatLength = (int)inputBuffer.Length / sizeof(float);

                if (Pitch == 1)
                {
                    for (int i = 0; i < dataInFloatLength; i++)
                        outputDataInFloat[i] = inputDataInFloat[i];
                }
                else
                {
                    float[] inputDataArray = new float[dataInFloatLength];

                    for (int i = 0; i < dataInFloatLength; i++)
                        inputDataArray[i] = inputDataInFloat[i];

                    PitchShifter.PitchShift(Pitch, dataInFloatLength, currentEncodingProperties.SampleRate, inputDataArray);

                    // Copy the data to the output
                    for (int i = 0; i < dataInFloatLength; i++)
                        outputDataInFloat[i] = inputDataArray[i];
                }
            }
        }

        public void Close(MediaEffectClosedReason reason) { }

        public void DiscardQueuedFrames() { }
    }
}
