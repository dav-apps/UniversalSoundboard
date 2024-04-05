using System;
using System.Collections.Generic;
using System.Linq;
using UniversalSoundboard.DataAccess;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using WinUI = Microsoft.UI.Xaml.Controls;

namespace UniversalSoundboard.Dialogs
{
    public class OutputDevicesDialog : Dialog
    {
        private StackPanel devicesStackPanel;

        public OutputDevicesDialog()
            : base(
                  FileManager.loader.GetString("OutputDevicesDialog-Title"),
                  FileManager.loader.GetString("Actions-Close")
            )
        {
            Content = GetContent();

            FileManager.deviceWatcherHelper.DevicesChanged += DeviceWatcherHelper_DevicesChanged;
        }

        private StackPanel GetContent()
        {
            StackPanel contentStackPanel = new StackPanel();

            ToggleSwitch multipleOutputDevicesToggle = new ToggleSwitch
            {
                Header = "Erlaube mehrere Ausgabegeräte",
                IsOn = FileManager.itemViewHolder.MultipleOutputDevices
            };

            multipleOutputDevicesToggle.Toggled += MultipleOutputDevicesToggle_Toggled;

            contentStackPanel.Children.Add(multipleOutputDevicesToggle);

            devicesStackPanel = new StackPanel
            {
                Margin = new Thickness(0, 16, 0, 0)
            };

            LoadDevices();

            contentStackPanel.Children.Add(devicesStackPanel);

            return contentStackPanel;
        }

        private void MultipleOutputDevicesToggle_Toggled(object sender, RoutedEventArgs e)
        {
            FileManager.itemViewHolder.MultipleOutputDevices = (sender as ToggleSwitch).IsOn;
            LoadDevices();
        }

        private void DeviceWatcherHelper_DevicesChanged(object sender, EventArgs e)
        {
            LoadDevices();
        }

        private void LoadDevices()
        {
            devicesStackPanel.Children.Clear();

            if (FileManager.itemViewHolder.MultipleOutputDevices)
            {
                var standardOutputDeviceCheckbox = new CheckBox
                {
                    Content = FileManager.loader.GetString("StandardOutputDevice"),
                    IsChecked = FileManager.itemViewHolder.UseStandardOutputDevice
                };

                standardOutputDeviceCheckbox.Checked += StandardOutputDeviceCheckbox_Checked;
                standardOutputDeviceCheckbox.Unchecked += StandardOutputDeviceCheckbox_Unchecked;

                devicesStackPanel.Children.Add(standardOutputDeviceCheckbox);

                foreach (var device in FileManager.deviceWatcherHelper.Devices)
                {
                    var outputDeviceCheckbox = new CheckBox
                    {
                        Content = device.Name,
                        Tag = device.Id,
                        IsChecked = FileManager.itemViewHolder.OutputDevice.Contains(device.Id)
                    };

                    outputDeviceCheckbox.Checked += OutputDeviceCheckbox_Checked;
                    outputDeviceCheckbox.Unchecked += OutputDeviceCheckbox_Unchecked;

                    devicesStackPanel.Children.Add(outputDeviceCheckbox);
                }
            }
            else
            {
                WinUI.RadioButtons radioButtons = new WinUI.RadioButtons();

                RadioButton standardOutputDeviceRadioButton = new RadioButton
                {
                    Content = FileManager.loader.GetString("StandardOutputDevice")
                };

                standardOutputDeviceRadioButton.Checked += StandardOutputDeviceRadioButton_Checked;

                radioButtons.Items.Add(standardOutputDeviceRadioButton);
                radioButtons.SelectedItem = standardOutputDeviceRadioButton;

                foreach (var device in FileManager.deviceWatcherHelper.Devices)
                {
                    RadioButton outputDeviceRadioButton = new RadioButton
                    {
                        Content = device.Name,
                        Tag = device.Id
                    };

                    outputDeviceRadioButton.Checked += OutputDeviceRadioButton_Checked;

                    radioButtons.Items.Add(outputDeviceRadioButton);

                    if (
                        !FileManager.itemViewHolder.UseStandardOutputDevice
                        && FileManager.itemViewHolder.OutputDevice.StartsWith(device.Id)
                    ) radioButtons.SelectedItem = outputDeviceRadioButton;
                }

                devicesStackPanel.Children.Add(radioButtons);
            }
        }

        private void StandardOutputDeviceCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            FileManager.itemViewHolder.UseStandardOutputDevice = true;
        }

        private void StandardOutputDeviceCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            FileManager.itemViewHolder.UseStandardOutputDevice = false;
        }

        private void OutputDeviceCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox checkbox = sender as CheckBox;
            string deviceId = checkbox.Tag as string;

            if (FileManager.itemViewHolder.OutputDevice.Contains(deviceId)) return;

            List<string> deviceIds = FileManager.itemViewHolder.OutputDevice.Split(",").ToList();
            deviceIds.Add(deviceId);
            FileManager.itemViewHolder.OutputDevice = string.Join(",", deviceIds);
        }

        private void OutputDeviceCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckBox checkbox = sender as CheckBox;
            string deviceId = checkbox.Tag as string;

            if (!FileManager.itemViewHolder.OutputDevice.Contains(deviceId)) return;

            string[] deviceIds = FileManager.itemViewHolder.OutputDevice.Split(",");
            FileManager.itemViewHolder.OutputDevice = string.Join(",", deviceIds.Where(id => id != deviceId).ToArray());
        }

        private void StandardOutputDeviceRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            FileManager.itemViewHolder.UseStandardOutputDevice = true;
        }

        private void OutputDeviceRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton radioButton = sender as RadioButton;
            string deviceId = radioButton.Tag as string;

            FileManager.itemViewHolder.UseStandardOutputDevice = false;
            FileManager.itemViewHolder.OutputDevice = deviceId;
        }
    }
}
