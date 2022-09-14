using Microsoft.AppCenter.Crashes;
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
        private bool isInitialized = false;
        private bool isInitializing = false;
        private StorageFile audioFile;
        private DeviceInformation inputDevice;
        private bool isRecording = false;
        private bool isDisposed = false;

        private AudioGraph AudioGraph;
        private AudioDeviceInputNode DeviceInputNode;
        private AudioFileOutputNode FileOutputNode;
        private AudioFrameOutputNode FrameOutputNode;
        private MediaEncodingProfile recordingFormat;

        public int SamplesPerQuantum
        {
            get => AudioGraph != null ? AudioGraph.SamplesPerQuantum : 0;
        }
        public int ChannelCount
        {
            get => AudioGraph != null ? (int)AudioGraph.EncodingProperties.ChannelCount : 2;
        }

        public event EventHandler<AudioRecorderQuantumStartedEventArgs> QuantumStarted;

        public StorageFile AudioFile
        {
            get => audioFile;
            set => audioFile = value;
        }
        public DeviceInformation InputDevice
        {
            get => inputDevice;
            set => SetInputDevice(value);
        }
        public bool IsInitialized
        {
            get => isInitialized;
        }
        public bool IsRecording
        {
            get => isRecording;
        }

        public AudioRecorder()
        {
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
            recordingFormat.Audio = AudioEncodingProperties.CreatePcm(16000, 2, 16);
        }

        public async Task Init(bool fileOutput = true, bool frameOutput = false)
        {
            if (isDisposed)
                throw new AudioRecorderDisposedException();

            if (isInitialized) return;

            if (!fileOutput && !frameOutput)
                throw new AudioRecorderInitException(AudioRecorderInitError.NoOutputSpecified);

            if (fileOutput && audioFile == null)
                throw new AudioRecorderInitException(AudioRecorderInitError.AudioFileNotSpecified);

            if (isRecording)
                throw new AudioRecorderInitException(AudioRecorderInitError.NotAllowedWhileRecording);

            if (isInitializing) return;
            isInitializing = true;

            // Create the AudioGraph
            await InitAudioGraph();

            // Create the input node
            await InitDeviceInputNode();

            // Create the output nodes
            if (fileOutput)
                await InitFileOutputNode();

            if (frameOutput)
                InitFrameOutputNode();

            isInitialized = true;
            isInitializing = false;
        }

        private async Task InitAudioGraph()
        {
            if (AudioGraph != null)
                return;

            var settings = new AudioGraphSettings(AudioRenderCategory.Media);
            settings.EncodingProperties = recordingFormat.Audio;
            var createAudioGraphResult = await AudioGraph.CreateAsync(settings);

            if (createAudioGraphResult.Status != AudioGraphCreationStatus.Success)
            {
                isInitializing = false;
                throw new AudioGraphInitException(createAudioGraphResult.Status);
            }

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
        
        private void InitFrameOutputNode()
        {
            FrameOutputNode = AudioGraph.CreateFrameOutputNode(recordingFormat.Audio);
            DeviceInputNode.AddOutgoingConnection(FrameOutputNode);

            AudioGraph.QuantumStarted += (AudioGraph sender, object args) =>
            {
                QuantumStarted?.Invoke(this, new AudioRecorderQuantumStartedEventArgs(FrameOutputNode.GetFrame()));
            };
        }

        public void Start()
        {
            if (isDisposed)
                throw new AudioRecorderDisposedException();

            if (!isInitialized)
                throw new AudioRecorderNotInitializedException();

            if (isRecording) return;

            AudioGraph.Start();
            isRecording = true;
        }

        public async Task Stop()
        {
            if (isDisposed)
                throw new AudioRecorderDisposedException();

            if (!isInitialized)
                throw new AudioRecorderNotInitializedException();

            if (!isRecording) return;

            AudioGraph.Stop();
            await FileOutputNode.FinalizeAsync();
            AudioGraph.ResetAllNodes();
            isInitialized = false;
            isRecording = false;
        }

        public void Dispose()
        {
            if (isDisposed) return;

            if (AudioGraph != null)
            {
                try
                {
                    AudioGraph.Stop();
                    AudioGraph.Dispose();
                }
                catch(AudioIOException e)
                {
                    Crashes.TrackError(e);
                }
            }

            isInitialized = false;
            isRecording = false;
            isDisposed = true;
        }
        
        private void SetInputDevice(DeviceInformation inputDevice)
        {
            if (isRecording)
                throw new AudioRecorderInitException(AudioRecorderInitError.NotAllowedWhileRecording);

            if (this.inputDevice == inputDevice) return;

            this.inputDevice = inputDevice;
        }
    }
}
