using System;
using System.Threading.Tasks;
using UniversalSoundboard.Common;
using Windows.Devices.Enumeration;
using Windows.Media.Audio;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Media.Render;
using Windows.Storage;

namespace UniversalSoundboard.Models
{
    public class AudioRecorder
    {
        private bool initialized = false;
        private bool isInitializing = false;
        private StorageFile audioFile;
        private DeviceInformation inputDevice;
        private bool isRecording = false;

        private AudioGraph AudioGraph;
        private AudioDeviceInputNode DeviceInputNode;
        private AudioFileOutputNode FileOutputNode;
        MediaEncodingProfile recordingFormat;

        public StorageFile AudioFile
        {
            get => audioFile;
            set => audioFile = value;
        }
        public DeviceInformation InputDevice
        {
            get => inputDevice;
            set => setInputDevice(value);
        }
        public bool IsRecording
        {
            get => isRecording;
        }

        public AudioRecorder() {
            InitRecordingFormat();
        }

        public AudioRecorder(StorageFile audioFile)
        {
            this.audioFile = audioFile;

            InitRecordingFormat();
        }

        public AudioRecorder(DeviceInformation inputDevice)
        {
            this.inputDevice = inputDevice;

            InitRecordingFormat();
        }

        public AudioRecorder(StorageFile audioFile, DeviceInformation inputDevice)
        {
            this.audioFile = audioFile;
            this.inputDevice = inputDevice;

            InitRecordingFormat();
        }

        private void InitRecordingFormat()
        {
            recordingFormat = MediaEncodingProfile.CreateWav(AudioEncodingQuality.Auto);
            recordingFormat.Audio = AudioEncodingProperties.CreatePcm(16000, 1, 16);
        }

        public async Task Init()
        {
            if (audioFile == null)
                throw new AudioRecorderInitException(AudioRecorderInitError.AudioFileNotSpecified);

            if (isRecording)
                throw new AudioRecorderInitException(AudioRecorderInitError.NotAllowedWhileRecording);

            if (isInitializing) return;
            isInitializing = true;

            if (!initialized)
            {
                // Create the AudioGraph
                await InitAudioGraph();

                // Create the input node
                await InitDeviceInputNode();

                initialized = true;
            }

            // Create the output node
            await InitFileOutputNode();

            isInitializing = false;
        }

        private async Task InitAudioGraph()
        {
            var settings = new AudioGraphSettings(AudioRenderCategory.Media);
            settings.EncodingProperties = recordingFormat.Audio;
            var createAudioGraphResult = await AudioGraph.CreateAsync(settings);

            if (createAudioGraphResult.Status != AudioGraphCreationStatus.Success)
            {
                isInitializing = false;
                throw new AudioGraphInitException(createAudioGraphResult.Status);
            }

            if (AudioGraph != null)
                AudioGraph.Stop();

            AudioGraph = createAudioGraphResult.Graph;
        }

        private async Task InitDeviceInputNode()
        {
            var inputNodeResult = await AudioGraph.CreateDeviceInputNodeAsync(MediaCategory.Media, recordingFormat.Audio, inputDevice);

            if (inputNodeResult.Status != AudioDeviceNodeCreationStatus.Success)
            {
                isInitializing = false;
                throw new DeviceInputNodeInitException(inputNodeResult.Status);
            }

            DeviceInputNode = inputNodeResult.DeviceInputNode;
        }

        private async Task InitFileOutputNode()
        {
            var outputNodeResult = await AudioGraph.CreateFileOutputNodeAsync(audioFile, recordingFormat);

            if (outputNodeResult.Status != AudioFileNodeCreationStatus.Success)
            {
                isInitializing = false;
                throw new FileOutputNodeInitException(outputNodeResult.Status);
            }

            FileOutputNode = outputNodeResult.FileOutputNode;
            DeviceInputNode.AddOutgoingConnection(FileOutputNode);
        }

        public void Start()
        {
            if (!initialized)
                throw new AudioRecorderNotInitializedException();

            if (isRecording) return;

            AudioGraph.Start();
            isRecording = true;
        }

        public async Task Stop()
        {
            if (!initialized)
                throw new AudioRecorderNotInitializedException();

            if (!isRecording) return;

            AudioGraph.Stop();
            await FileOutputNode.FinalizeAsync();
            isRecording = false;
        }

        private void setInputDevice(DeviceInformation inputDevice)
        {
            if (isRecording)
                throw new AudioRecorderInitException(AudioRecorderInitError.NotAllowedWhileRecording);

            if (this.inputDevice == inputDevice) return;

            this.inputDevice = inputDevice;
        }
    }
}
