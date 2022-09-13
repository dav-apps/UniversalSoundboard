using System;
using System.Threading.Tasks;
using UniversalSoundboard.Common;
using Windows.Devices.Enumeration;
using Windows.Media.Audio;
using Windows.Media.Render;
using Windows.Storage;
using Windows.UI.Xaml;

namespace UniversalSoundboard.Models
{
    public class AudioPlayer
    {
        private bool isInitialized = false;
        private bool isInitializing = false;
        private StorageFile audioFile;
        private DeviceInformation outputDevice;
        private bool outputDeviceChanged = false;
        private bool isPlaying = false;
        private TimeSpan position = TimeSpan.Zero;
        private double volume = 1;
        private bool isMuted = false;
        private double playbackRate = 1.0;
        private DispatcherTimer positionChangeTimer;

        private AudioGraph AudioGraph;
        private AudioFileInputNode FileInputNode;
        private AudioDeviceOutputNode DeviceOutputNode;

        public bool IsInitialized
        {
            get => isInitialized;
        }
        public StorageFile AudioFile
        {
            get => audioFile;
            set => audioFile = value;
        }
        public DeviceInformation OutputDevice
        {
            get => outputDevice;
            set => setOutputDevice(value);
        }
        public bool IsPlaying { get => isPlaying; }
        public TimeSpan Duration
        {
            get => FileInputNode?.Duration ?? TimeSpan.Zero;
        }
        public TimeSpan Position
        {
            get => FileInputNode?.Position ?? position;
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

        public event EventHandler<PositionChangedEventArgs> PositionChanged;
        public event EventHandler<EventArgs> MediaEnded;

        public AudioPlayer()
        {
            InitPositionChangeTimer();
        }

        public AudioPlayer(StorageFile audioFile)
        {
            this.audioFile = audioFile;

            InitPositionChangeTimer();
        }

        public AudioPlayer(DeviceInformation outputDevice)
        {
            this.outputDevice = outputDevice;

            InitPositionChangeTimer();
        }

        public AudioPlayer(StorageFile audioFile, DeviceInformation outputDevice)
        {
            this.audioFile = audioFile;
            this.outputDevice = outputDevice;

            InitPositionChangeTimer();
        }

        public async Task Init()
        {
            if (audioFile == null)
                throw new AudioPlayerInitException(AudioPlayerInitError.AudioFileNotSpecified);

            if (isInitializing) return;
            isInitializing = true;

            positionChangeTimer.Stop();

            if (!isInitialized || outputDeviceChanged)
            {
                // Create the AudioGraph
                await InitAudioGraph();

                // Create the output node
                await InitDeviceOutputNode();

                isInitialized = true;
            }

            // Create the input node
            await InitFileInputNode();

            if (IsPlaying)
            {
                AudioGraph.Start();
                positionChangeTimer.Start();
            }

            isInitializing = false;
        }

        private async Task InitAudioGraph()
        {
            var settings = new AudioGraphSettings(AudioRenderCategory.Media);
            settings.PrimaryRenderDevice = outputDevice;
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

        private async Task InitFileInputNode()
        {
            if (FileInputNode != null)
                FileInputNode.Stop();

            var inputNodeResult = await AudioGraph.CreateFileInputNodeAsync(audioFile);

            if (inputNodeResult.Status != AudioFileNodeCreationStatus.Success)
            {
                isInitializing = false;
                throw new FileInputNodeInitException(inputNodeResult.Status);
            }

            FileInputNode = inputNodeResult.FileInputNode;
            FileInputNode.Seek(position);
            FileInputNode.OutgoingGain = volume;
            FileInputNode.PlaybackSpeedFactor = playbackRate;

            FileInputNode.AddOutgoingConnection(DeviceOutputNode);
            FileInputNode.FileCompleted += FileInputNode_FileCompleted;
        }

        private async Task InitDeviceOutputNode()
        {
            if (DeviceOutputNode != null)
                DeviceOutputNode.Stop();

            var outputNodeResult = await AudioGraph.CreateDeviceOutputNodeAsync();

            if (outputNodeResult.Status != AudioDeviceNodeCreationStatus.Success)
            {
                isInitializing = false;
                throw new DeviceOutputNodeInitException(outputNodeResult.Status);
            }

            DeviceOutputNode = outputNodeResult.DeviceOutputNode;
        }

        private void InitPositionChangeTimer()
        {
            positionChangeTimer = new DispatcherTimer();
            positionChangeTimer.Interval = TimeSpan.FromMilliseconds(200);
            positionChangeTimer.Tick += PositionChangeTimer_Tick;
        }

        public void Play()
        {
            if (!isInitialized)
                throw new AudioPlayerNotInitializedException();

            if (isPlaying) return;

            AudioGraph.Start();
            isPlaying = true;
            positionChangeTimer.Start();
        }

        public void Pause()
        {
            if (!isInitialized)
                throw new AudioPlayerNotInitializedException();

            if (!isPlaying) return;

            AudioGraph.Stop();
            isPlaying = false;
            positionChangeTimer.Stop();
        }

        #region Setter methods
        private void setOutputDevice(DeviceInformation outputDevice)
        {
            if (this.outputDevice == outputDevice) return;

            this.outputDevice = outputDevice;
            outputDeviceChanged = true;
        }

        private void setPosition(TimeSpan position)
        {
            FileInputNode?.Seek(position);
            this.position = position;
            PositionChanged?.Invoke(this, new PositionChangedEventArgs(position));
        }

        private void setVolume(double volume)
        {
            // Don't set the volume if the player is muted or if the volume didn't change
            if (isMuted || volume == this.volume) return;

            if (volume > 1)
                volume = 1;
            else if (volume < 0)
                volume = 0;

            if (DeviceOutputNode != null)
                DeviceOutputNode.OutgoingGain = volume;

            this.volume = volume;
        }

        private void setIsMuted(bool muted)
        {
            // Don't change the value if it didn't change
            if (muted == isMuted) return;

            if (DeviceOutputNode != null)
            {
                if (muted)
                    DeviceOutputNode.OutgoingGain = 0;
                else
                    DeviceOutputNode.OutgoingGain = volume;
            }

            isMuted = muted;
        }

        private void setPlaybackRate(double playbackRate)
        {
            // Don't change the value if it didn't change
            if (this.playbackRate == playbackRate) return;

            FileInputNode.PlaybackSpeedFactor = playbackRate;
            this.playbackRate = playbackRate;
        }
        #endregion

        #region Event Handlers
        private void PositionChangeTimer_Tick(object sender, object e)
        {
            position = FileInputNode.Position;
            PositionChanged?.Invoke(this, new PositionChangedEventArgs(position));
        }

        private void FileInputNode_FileCompleted(AudioFileInputNode sender, object args)
        {
            MediaEnded?.Invoke(this, new EventArgs());
        }
        #endregion
    }
}
