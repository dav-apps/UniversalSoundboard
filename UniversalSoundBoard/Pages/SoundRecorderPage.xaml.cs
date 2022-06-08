using System;
using System.ComponentModel;
using UniversalSoundboard.Common;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Models;
using Windows.Devices.Enumeration;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UniversalSoundboard.Pages
{
    public sealed partial class SoundRecorderPage : Page
    {
        DeviceWatcherHelper deviceWatcherHelper = new DeviceWatcherHelper(DeviceClass.AudioCapture);
        DeviceInfo inputDevice;

        public SoundRecorderPage()
        {
            InitializeComponent();

            ContentRoot.DataContext = FileManager.itemViewHolder;

            FileManager.itemViewHolder.PropertyChanged += ItemViewHolder_PropertyChanged;
            deviceWatcherHelper.DevicesChanged += DeviceWatcherHelper_DevicesChanged;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateInputDeviceComboBox();
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

        private void DeviceWatcherHelper_DevicesChanged(object sender, EventArgs e)
        {
            UpdateInputDeviceComboBox();
        }

        private void InputDeviceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var inputDeviceId = (InputDeviceComboBox.SelectedItem as ComboBoxItem).Tag;
            int i = deviceWatcherHelper.Devices.FindIndex(d => d.Id.Equals(inputDeviceId));

            if (i != -1)
                inputDevice = deviceWatcherHelper.Devices[i];
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
