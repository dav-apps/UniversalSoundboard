using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UniversalSoundboard.Common;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Models;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Media;
using Windows.Media.Devices;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace UniversalSoundboard.Pages
{
    public sealed partial class SoundRecorderPage : Page
    {
        DeviceWatcherHelper deviceWatcherHelper = new DeviceWatcherHelper(DeviceClass.AudioCapture);
        DeviceInfo inputDevice;
        StorageFile outputFile;
        AudioRecorder audioRecorder;
        List<List<float>> channelValues = new List<List<float>>();
        DispatcherTimer timer;
        private bool skipInputDeviceComboBoxChanged = false;
        private int channelCount = 2;

        public SoundRecorderPage()
        {
            InitializeComponent();

            ContentRoot.DataContext = FileManager.itemViewHolder;

            FileManager.itemViewHolder.PropertyChanged += ItemViewHolder_PropertyChanged;
            deviceWatcherHelper.DevicesChanged += DeviceWatcherHelper_DevicesChanged;

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(50);
            timer.Tick += Timer_Tick;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateInputDeviceComboBox();

            // Create an output file in the cache
            outputFile = await ApplicationData.Current.LocalCacheFolder.CreateFileAsync(Guid.NewGuid().ToString(), CreationCollisionOption.ReplaceExisting);
            audioRecorder = new AudioRecorder(outputFile);
            audioRecorder.QuantumStarted += AudioRecorder_QuantumStarted;

            channelValues.Clear();
            channelCount = audioRecorder.ChannelCount;

            for (int c = 0; c < channelCount; c++)
                channelValues.Add(new List<float>());

            // Set up the default input device
            var defaultDeviceId = MediaDevice.GetDefaultAudioCaptureId(AudioDeviceRole.Default);

            int i = deviceWatcherHelper.Devices.FindIndex(d => d.Id == defaultDeviceId);
            skipInputDeviceComboBoxChanged = true;

            if (i == -1)
            {
                inputDevice = deviceWatcherHelper.Devices.First();
                InputDeviceComboBox.SelectedIndex = 0;
            }
            else
            {
                inputDevice = deviceWatcherHelper.Devices.ElementAt(i);
                InputDeviceComboBox.SelectedIndex = i;
            }

            await UpdateInputDevice();
            skipInputDeviceComboBoxChanged = false;
        }

        private void ItemViewHolder_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case ItemViewHolder.CurrentThemeKey:
                    RequestedTheme = FileManager.GetRequestedTheme();
                    break;
            }
        }

        private async void DeviceWatcherHelper_DevicesChanged(object sender, EventArgs e)
        {
            await MainPage.dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => UpdateInputDeviceComboBox());
        }

        private async void InputDeviceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (skipInputDeviceComboBoxChanged) return;

            var inputDeviceId = (InputDeviceComboBox.SelectedItem as ComboBoxItem).Tag;
            int i = deviceWatcherHelper.Devices.FindIndex(d => d.Id.Equals(inputDeviceId));
            if (i == -1) return;

            inputDevice = deviceWatcherHelper.Devices[i];
            await UpdateInputDevice();
        }

        private async void RecordButton_Click(object sender, RoutedEventArgs e)
        {
            if (audioRecorder.IsRecording)
            {
                RecordButton.Content = "\uE7C8";

                timer.Stop();
                await audioRecorder.Stop();
                await audioRecorder.Init(true, true);

                InputDeviceComboBox.IsEnabled = true;
                WaveformCanvas.Children.Clear();
            }
            else
            {
                RecordButton.Content = "\uEE95";

                timer.Start();
                audioRecorder.Start();

                InputDeviceComboBox.IsEnabled = false;
            }
        }

        private void AudioRecorder_QuantumStarted(object sender, AudioRecorderQuantumStartedEventArgs e)
        {
            ProcessFrameOutput(e.AudioFrame);
        }

        private void Timer_Tick(object sender, object e)
        {
            WaveformCanvas.Children.Clear();
            DrawWaveform();
        }

        private void DrawWaveform()
        {
            // Draw the waveform for the first 500 processed sample values
            double canvasWidth = WaveformCanvas.ActualWidth;
            double canvasHeight = WaveformCanvas.ActualHeight;

            // Add line for x-axis
            WaveformCanvas.Children.Add(new Line
            {
                Stroke = new SolidColorBrush(Color.FromArgb(255, 191, 191, 191)),
                X1 = 0,
                X2 = canvasWidth,
                Y1 = canvasHeight / 2,
                Y2 = canvasHeight / 2
            });

            int lineWidth = 5;
            int maxSamples = (int)canvasWidth / lineWidth;

            int firstChannelValueCount = channelValues[0].Count;
            int secondChannelValueCount = channelValues[1].Count;

            if (firstChannelValueCount > maxSamples) firstChannelValueCount = maxSamples;
            if (secondChannelValueCount > maxSamples) secondChannelValueCount = maxSamples;

            List<float> firstChannelValues = channelValues[0].GetRange(0, firstChannelValueCount);
            List<float> secondChannelValues = channelValues[1].GetRange(0, secondChannelValueCount);

            for (int i = 0; i < firstChannelValues.Count; i++)
            {
                double xPos = canvasWidth - (i * lineWidth);

                var line = new Line
                {
                    Stroke = new SolidColorBrush(Color.FromArgb(255, 191, 191, 191)),
                    X1 = xPos,
                    X2 = xPos,
                    Y1 = canvasHeight / 2,
                    Y2 = (canvasHeight / 2) - (firstChannelValues[i] * 20 * (canvasHeight / 2))
                };

                WaveformCanvas.Children.Add(line);
            }

            for (int i = 0; i < secondChannelValueCount; i++)
            {
                double xPos = canvasWidth - (i * lineWidth);

                var line = new Line
                {
                    Stroke = new SolidColorBrush(Color.FromArgb(255, 191, 191, 191)),
                    X1 = xPos,
                    X2 = xPos,
                    Y1 = canvasHeight / 2,
                    Y2 = (canvasHeight / 2) + (secondChannelValues[i] * 20 * (canvasHeight / 2))
                };

                WaveformCanvas.Children.Add(line);
            }
        }

        private async Task UpdateInputDevice()
        {
            audioRecorder.InputDevice = inputDevice.DeviceInformation;
            await audioRecorder.Init(true, true);
        }

        private void UpdateInputDeviceComboBox()
        {
            InputDeviceComboBox.Items.Clear();
            int selectedItemIndex = -1;

            for (int i = 0; i < deviceWatcherHelper.Devices.Count; i++)
            {
                var device = deviceWatcherHelper.Devices[i];
                if (inputDevice != null && inputDevice.Id == device.Id) selectedItemIndex = i;

                InputDeviceComboBox.Items.Add(new ComboBoxItem
                {
                    Content = device.Name,
                    Tag = device.Id
                });
            }

            InputDeviceComboBox.SelectedIndex = selectedItemIndex;
        }

        [ComImport]
        [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        unsafe interface IMemoryBufferByteAccess
        {
            void GetBuffer(out byte* buffer, out uint capacity);
        }

        unsafe private void ProcessFrameOutput(AudioFrame frame)
        {
            using (AudioBuffer buffer = frame.LockBuffer(AudioBufferAccessMode.Read))
            using (IMemoryBufferReference reference = buffer.CreateReference())
            {
                byte* dataInBytes;
                uint capacityInBytes;
                ((IMemoryBufferByteAccess)reference).GetBuffer(out dataInBytes, out capacityInBytes);

                var dataInFloat = (float*)dataInBytes;
                float firstChannelSum = 0;
                float secondChannelSum = 0;

                for (int i = 0; i < audioRecorder.SamplesPerQuantum; i++)
                {
                    float val = dataInFloat[i];

                    if (float.IsNaN(val) || float.IsInfinity(val)) continue;

                    if (i % 2 == 0)
                        firstChannelSum += Math.Abs(dataInFloat[i]);
                    else
                        secondChannelSum += Math.Abs(dataInFloat[i]);
                }

                channelValues[0].Insert(0, firstChannelSum / audioRecorder.SamplesPerQuantum);
                channelValues[1].Insert(0, secondChannelSum / audioRecorder.SamplesPerQuantum);
            }
        }
    }
}
