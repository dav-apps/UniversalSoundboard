using Microsoft.AppCenter.Crashes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UniversalSoundboard.Common;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Dialogs;
using UniversalSoundboard.Models;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Media;
using Windows.Media.Devices;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using WinUI = Microsoft.UI.Xaml.Controls;

namespace UniversalSoundboard.Pages
{
    public sealed partial class SoundRecorderPage : Page
    {
        DeviceWatcherHelper inputDeviceWatcherHelper = new DeviceWatcherHelper(DeviceClass.AudioCapture);
        DeviceInfo inputDevice;
        StorageFile outputFile;
        AudioRecorder audioRecorder;
        DispatcherTimer timer;
        List<List<float>> channelValues = new List<List<float>>();
        ObservableCollection<RecordedSoundItem> recordedSoundItems = new ObservableCollection<RecordedSoundItem>();
        private bool skipInputDeviceComboBoxChanged = false;
        private bool skipRecordedSoundItemRemoved = false;
        private int channelCount = 2;
        private int soundItemsCounter = 0;

        public SoundRecorderPage()
        {
            InitializeComponent();

            ContentRoot.DataContext = FileManager.itemViewHolder;

            FileManager.itemViewHolder.PropertyChanged += ItemViewHolder_PropertyChanged;
            inputDeviceWatcherHelper.DevicesChanged += InputDeviceWatcherHelper_DevicesChanged;
            MainPage.soundRecorderAppWindow.CloseRequested += SoundRecorderAppWindow_CloseRequested;

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(50);
            timer.Tick += Timer_Tick;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            RecordButtonToolTip.Text = FileManager.loader.GetString("StartRecording");

            UpdateInputDeviceComboBox();
            await InitAudioRecorder();

            // Set up the default input device
            var defaultDeviceId = MediaDevice.GetDefaultAudioCaptureId(AudioDeviceRole.Default);

            int i = inputDeviceWatcherHelper.Devices.FindIndex(d => d.Id == defaultDeviceId);
            skipInputDeviceComboBoxChanged = true;

            if (inputDeviceWatcherHelper.Devices.Count == 0)
            {
                inputDevice = null;
                InputDeviceComboBox.SelectedIndex = -1;
            }
            else if (i == -1)
            {
                inputDevice = inputDeviceWatcherHelper.Devices.First();
                InputDeviceComboBox.SelectedIndex = 0;
            }
            else
            {
                inputDevice = inputDeviceWatcherHelper.Devices.ElementAt(i);
                InputDeviceComboBox.SelectedIndex = i;
            }

            await UpdateInputDevice();
            skipInputDeviceComboBoxChanged = false;
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SetSize();
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

        private async void InputDeviceWatcherHelper_DevicesChanged(object sender, EventArgs args)
        {
            await MainPage.dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                if (
                    audioRecorder == null
                    || audioRecorder.IsDisposed
                ) return;

                UpdateInputDeviceComboBox();

                if (
                    !audioRecorder.IsInitialized
                    && inputDeviceWatcherHelper.Devices.Count > 0
                )
                {
                    try
                    {
                        // Init the audio recorder
                        await audioRecorder.Init(true, true);
                    }
                    catch(AudioIOException e)
                    {
                        Crashes.TrackError(e);
                    }
                }
                
                if (audioRecorder.IsInitialized)
                {
                    if (audioRecorder.IsRecording)
                    {
                        // Stop the recording
                        await StopRecording();
                    }

                    DeviceInfo newInputDevice;

                    if (inputDeviceWatcherHelper.Devices.Count == 0)
                        newInputDevice = null;
                    else
                        newInputDevice = inputDeviceWatcherHelper.Devices[InputDeviceComboBox.SelectedIndex];

                    if (newInputDevice != inputDevice)
                    {
                        inputDevice = newInputDevice;

                        // Create a new AudioRecorder with the newly selected input device
                        audioRecorder.Dispose();
                        audioRecorder = new AudioRecorder(outputFile);
                        audioRecorder.QuantumStarted += AudioRecorder_QuantumStarted;
                        audioRecorder.UnrecoverableErrorOccurred += AudioRecorder_UnrecoverableErrorOccurred;
                        await UpdateInputDevice();
                    }
                }
            });
        }

        private async void SoundRecorderAppWindow_CloseRequested(AppWindow sender, AppWindowCloseRequestedEventArgs args)
        {
            if (recordedSoundItems.Count > 0)
            {
                args.Cancel = true;

                // Show warning dialog
                var soundRecorderCloseWarningDialog = new SoundRecorderCloseWarningDialog();
                soundRecorderCloseWarningDialog.PrimaryButtonClick += SoundRecorderCloseWarningContentDialog_PrimaryButtonClick;
                await soundRecorderCloseWarningDialog.ShowAsync(AppWindowType.SoundRecorder);
            }
            else
            {
                await ClearPageData();
            }
        }

        private async void SoundRecorderCloseWarningContentDialog_PrimaryButtonClick(Dialog sender, ContentDialogButtonClickEventArgs args)
        {
            await ClearPageData();
            await MainPage.soundRecorderAppWindow.CloseAsync();
        }

        private async void InputDeviceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (
                skipInputDeviceComboBoxChanged
                || InputDeviceComboBox.SelectedItem == null
            ) return;

            var inputDeviceId = (InputDeviceComboBox.SelectedItem as ComboBoxItem).Tag;
            int i = inputDeviceWatcherHelper.Devices.FindIndex(d => d.Id.Equals(inputDeviceId));
            if (i == -1) return;

            inputDevice = inputDeviceWatcherHelper.Devices[i];
            await UpdateInputDevice();
        }

        private async void RecordButton_Click(object sender, RoutedEventArgs e)
        {
            if (!audioRecorder.IsInitialized) return;

            if (audioRecorder.IsRecording)
            {
                await StopRecording();
            }
            else
            {
                StartRecording();
            }
        }

        private void StartRecording()
        {
            RecordButton.Content = "\uEE95";
            RecordButtonToolTip.Text = FileManager.loader.GetString("StopRecording");

            timer.Start();
            audioRecorder.Start();

            InputDeviceComboBox.IsEnabled = false;

            if (recordedSoundItems.Count > 0)
                ExpandRecorder();

            soundItemsCounter++;
        }

        private async Task StopRecording()
        {
            RecordButton.Content = "\uE7C8";
            RecordButtonToolTip.Text = FileManager.loader.GetString("StartRecording");

            timer.Stop();
            await audioRecorder.Stop();

            InputDeviceComboBox.IsEnabled = true;
            WaveformCanvas.Children.Clear();

            // Add the new recorded sounds to the list
            var recordedSoundItem = new RecordedSoundItem(string.Format(FileManager.loader.GetString("Recording"), soundItemsCounter), outputFile);
            recordedSoundItem.AudioPlayerStarted += RecordedSoundItem_AudioPlayerStarted;
            recordedSoundItem.Removed += RecordedSoundItem_Removed;
            recordedSoundItems.Insert(0, recordedSoundItem);

            await InitAudioRecorder();

            // Show the list of recorded sounds
            RelativePanel.SetAlignBottomWithPanel(RecordingRelativePanel, false);
            ShrinkRecorderStoryboardAnimation.From = RecordingRelativePanel.ActualHeight;
            ShrinkRecorderStoryboardAnimation.To = RecordingRelativePanel.ActualHeight / 1.5;
            ShrinkRecorderStoryboard.Begin();
        }

        private void RecordedSoundItem_AudioPlayerStarted(object sender, EventArgs e)
        {
            RecordedSoundItem clickedItem = (RecordedSoundItem)sender;

            foreach (var item in recordedSoundItems)
            {
                if (item.Uuid.Equals(clickedItem.Uuid)) continue;

                item.Pause();
            }
        }

        private void RecordedSoundItem_Removed(object sender, EventArgs e)
        {
            if (skipRecordedSoundItemRemoved) return;

            var recordedSoundItem = (RecordedSoundItem)sender;
            bool removeResult = recordedSoundItems.Remove(recordedSoundItem);

            if (removeResult && recordedSoundItems.Count == 0)
                ExpandRecorder();
        }

        private void AudioRecorder_QuantumStarted(object sender, AudioRecorderQuantumStartedEventArgs e)
        {
            ProcessFrameOutput(e.AudioFrame);
        }

        private async void AudioRecorder_UnrecoverableErrorOccurred(object sender, Windows.Media.Audio.AudioGraphUnrecoverableErrorOccurredEventArgs e)
        {
            await MainPage.dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                await StopRecording();
            });
        }

        private void Timer_Tick(object sender, object e)
        {
            WaveformCanvas.Children.Clear();
            DrawWaveform();
        }

        private void ExpandRecorderStoryboard_Completed(object sender, object e)
        {
            RecordingRelativePanel.Height = double.NaN;
            RelativePanel.SetAlignBottomWithPanel(RecordingRelativePanel, true);
        }

        private void MinimizeWarningInfoBar_Closed(WinUI.InfoBar sender, WinUI.InfoBarClosedEventArgs args)
        {
            FileManager.itemViewHolder.SoundRecorderMinimizeWarningClosed = true;
        }

        private async Task InitAudioRecorder()
        {
            // Create an output file in the cache
            outputFile = await ApplicationData.Current.LocalCacheFolder.CreateFileAsync(
                string.Format("{0}.wav", Guid.NewGuid()),
                CreationCollisionOption.ReplaceExisting
            );
            audioRecorder = new AudioRecorder(outputFile);
            audioRecorder.QuantumStarted += AudioRecorder_QuantumStarted;
            audioRecorder.UnrecoverableErrorOccurred += AudioRecorder_UnrecoverableErrorOccurred;

            try
            {
                await audioRecorder.Init(true, true);
            }
            catch(AudioIOException e)
            {
                Crashes.TrackError(e);
                return;
            }

            channelValues.Clear();
            channelCount = audioRecorder.ChannelCount;

            for (int c = 0; c < channelCount; c++)
                channelValues.Add(new List<float>());
        }

        private void SetSize()
        {
            if (audioRecorder == null) return;

            if (!audioRecorder.IsRecording && recordedSoundItems.Count > 0)
            {
                RelativePanel.SetAlignBottomWithPanel(RecordingRelativePanel, false);
                RecordingRelativePanel.Height = ContentRoot.ActualHeight / 1.5;
            }
            else
            {
                RelativePanel.SetAlignBottomWithPanel(RecordingRelativePanel, true);
                RecordingRelativePanel.Height = ContentRoot.ActualHeight;
            }
        }

        private async Task ClearPageData()
        {
            skipRecordedSoundItemRemoved = true;

            foreach (var recordedSoundItem in recordedSoundItems)
                await recordedSoundItem.Remove();

            recordedSoundItems.Clear();
            audioRecorder.Dispose();

            if (System.IO.File.Exists(outputFile.Path))
                await outputFile.DeleteAsync();
        }

        private void ExpandRecorder()
        {
            ExpandRecorderStoryboardAnimation.From = RecordingRelativePanel.ActualHeight;
            ExpandRecorderStoryboardAnimation.To = ContentRoot.ActualHeight;
            ExpandRecorderStoryboard.Begin();
        }

        private void DrawWaveform()
        {
            if (channelValues.Count < 2) return;

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
                    Y2 = (canvasHeight / 2) - (firstChannelValues[i] * 8 * (canvasHeight / 2))
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
                    Y2 = (canvasHeight / 2) + (secondChannelValues[i] * 8 * (canvasHeight / 2))
                };

                WaveformCanvas.Children.Add(line);
            }
        }

        private async Task UpdateInputDevice()
        {
            if (audioRecorder == null) return;

            try
            {
                audioRecorder.InputDevice = inputDevice?.DeviceInformation;
                await audioRecorder.Init(true, true);
            }
            catch(AudioIOException e)
            {
                Crashes.TrackError(e);
            }
        }

        private void UpdateInputDeviceComboBox()
        {
            InputDeviceComboBox.Items.Clear();
            int selectedItemIndex = inputDeviceWatcherHelper.Devices.Count == 0 ? -1 : 0;

            for (int i = 0; i < inputDeviceWatcherHelper.Devices.Count; i++)
            {
                var device = inputDeviceWatcherHelper.Devices[i];
                if (inputDevice != null && inputDevice.Id == device.Id) selectedItemIndex = i;

                InputDeviceComboBox.Items.Add(new ComboBoxItem
                {
                    Content = device.Name,
                    Tag = device.Id
                });
            }

            skipInputDeviceComboBoxChanged = true;
            InputDeviceComboBox.SelectedIndex = selectedItemIndex;
            RecordButton.IsEnabled = inputDeviceWatcherHelper.Devices.Count > 0;
            skipInputDeviceComboBoxChanged = false;
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
            if (
                audioRecorder.SamplesPerQuantum == 0
                || channelValues.Count < 2
            ) return;

            using (AudioBuffer buffer = frame.LockBuffer(AudioBufferAccessMode.Read))
            using (IMemoryBufferReference reference = buffer.CreateReference())
            {
                byte* dataInBytes;
                uint capacityInBytes;
                ((IMemoryBufferByteAccess)reference).GetBuffer(out dataInBytes, out capacityInBytes);

                var dataInFloat = (float*)dataInBytes;
                float firstChannelSum = 0;
                float secondChannelSum = 0;
                
                for (int i = 0; i < capacityInBytes / sizeof(float); i++)
                {
                    float val = dataInFloat[i];

                    if (float.IsNaN(val) || float.IsInfinity(val) || val > 10) continue;

                    if (i % 2 == 0)
                        firstChannelSum += Math.Abs(val);
                    else
                        secondChannelSum += Math.Abs(val);
                }

                channelValues[0].Insert(0, firstChannelSum / audioRecorder.SamplesPerQuantum);
                channelValues[1].Insert(0, secondChannelSum / audioRecorder.SamplesPerQuantum);
            }
        }
    }
}
