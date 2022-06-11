using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using UniversalSoundboard.Common;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Models;
using Windows.Devices.Enumeration;
using Windows.Media.Devices;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace UniversalSoundboard.Pages
{
    public sealed partial class SoundRecorderPage : Page
    {
        DeviceWatcherHelper deviceWatcherHelper = new DeviceWatcherHelper(DeviceClass.AudioCapture);
        DeviceInfo inputDevice;
        StorageFile outputFile;
        AudioRecorder audioRecorder;
        private bool skipInputDeviceComboBoxChanged = false;

        public SoundRecorderPage()
        {
            InitializeComponent();

            ContentRoot.DataContext = FileManager.itemViewHolder;

            FileManager.itemViewHolder.PropertyChanged += ItemViewHolder_PropertyChanged;
            deviceWatcherHelper.DevicesChanged += DeviceWatcherHelper_DevicesChanged;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateInputDeviceComboBox();

            // Create an output file in the cache
            outputFile = await ApplicationData.Current.LocalCacheFolder.CreateFileAsync(Guid.NewGuid().ToString(), CreationCollisionOption.ReplaceExisting);
            audioRecorder = new AudioRecorder(outputFile);

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
                RecordButton.Foreground = new SolidColorBrush(Colors.White);
                RecordButton.Background = new SolidColorBrush(Color.FromArgb(255, 212, 64, 84));

                await audioRecorder.Stop();
                await audioRecorder.Init();
            }
            else
            {
                RecordButton.Content = "\uEE95";
                RecordButton.Foreground = new SolidColorBrush(Colors.Black);
                RecordButton.Background = new SolidColorBrush(Colors.Transparent);

                audioRecorder.Start();
            }
        }

        private async Task UpdateInputDevice()
        {
            audioRecorder.InputDevice = inputDevice.DeviceInformation;
            await audioRecorder.Init();
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
    }
}
