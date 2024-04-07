using Windows.Media.Audio;
using Windows.Media.Effects;

namespace UniversalSoundboard.Models
{
    public class AudioGraphContainer
    {
        public AudioGraph AudioGraph { get; set; }
        public AudioFileInputNode FileInputNode { get; set; }
        public AudioDeviceOutputNode DeviceOutputNode { get; set; }
        public AudioEffectDefinition FadeEffectDefinition { get; set; }
        public EchoEffectDefinition EchoEffectDefinition { get; set; }
        public LimiterEffectDefinition LimiterEffectDefinition { get; set; }
        public ReverbEffectDefinition ReverbEffectDefinition { get; set; }
        public AudioEffectDefinition PitchShiftEffectDefinition { get; set; }

        public AudioGraphContainer(AudioGraph audioGraph)
        {
            AudioGraph = audioGraph;
        }
    }
}
