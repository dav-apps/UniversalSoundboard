using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniversalSoundboard.Common;
using Windows.Media.Audio;
using Windows.Media.Render;
using Windows.Storage;

namespace UniversalSoundboard.Models
{
    public class AudioPlayer
    {
        private bool initialized = false;
        private double volume = 1;
        private bool isMuted = false;

        private AudioGraph AudioGraph { get; set; }
        private AudioFileInputNode FileInputNode { get; set; }
        private AudioDeviceOutputNode DeviceOutputNode { get; set; }
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

        public AudioPlayer()
        {
            
        }

        public async Task Init(StorageFile audioFile)
        {
            if (initialized) return;

            var settings = new AudioGraphSettings(AudioRenderCategory.Media);

            // Create the AudioGraph
            var createAudioGraphResult = await AudioGraph.CreateAsync(settings);

            if (createAudioGraphResult.Status != AudioGraphCreationStatus.Success)
                throw new AudioPlayerInitException(createAudioGraphResult.Status);

            AudioGraph = createAudioGraphResult.Graph;

            // Create the input node
            var inputNodeResult = await AudioGraph.CreateFileInputNodeAsync(audioFile);

            if (inputNodeResult.Status != AudioFileNodeCreationStatus.Success)
                throw new AudioPlayerInitException(inputNodeResult.Status);

            FileInputNode = inputNodeResult.FileInputNode;

            // Create the output node
            var outputNodeResult = await AudioGraph.CreateDeviceOutputNodeAsync();

            if (outputNodeResult.Status != AudioDeviceNodeCreationStatus.Success)
                throw new AudioPlayerInitException(outputNodeResult.Status);

            DeviceOutputNode = outputNodeResult.DeviceOutputNode;
            FileInputNode.AddOutgoingConnection(DeviceOutputNode);
            initialized = true;
        }

        public void Play()
        {
            if (!initialized)
                throw new AudioPlayerNotInitializedException();

            AudioGraph.Start();
        }

        private void setVolume(double volume)
        {
            if (!initialized)
                throw new AudioPlayerNotInitializedException();

            // Don't set the volume if the player is muted or if the volume didn't change
            if (isMuted || volume == this.volume) return;

            if (volume > 1)
                volume = 1;
            else if (volume < 0)
                volume = 0;

            DeviceOutputNode.OutgoingGain = volume;
            this.volume = volume;
        }

        private void setIsMuted(bool muted)
        {
            if (!initialized)
                throw new AudioPlayerNotInitializedException();

            // Don't change anything if the value didn't change
            if (muted == isMuted) return;

            if (muted)
                DeviceOutputNode.OutgoingGain = 0;
            else
                DeviceOutputNode.OutgoingGain = volume;

            isMuted = muted;
        }
    }
}
