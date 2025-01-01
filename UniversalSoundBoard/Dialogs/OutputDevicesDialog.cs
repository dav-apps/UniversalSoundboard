using Sentry;
using System;
using System.Collections.Generic;
using System.Linq;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Pages;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using WinUI = Microsoft.UI.Xaml.Controls;

namespace UniversalSoundboard.Dialogs
{
    public class OutputDevicesDialog : Dialog
    {
        private StackPanel devicesStackPanel;
        private List<CheckBox> outputDeviceCheckboxes = new List<CheckBox>();

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
                Header = FileManager.loader.GetString("OutputDevicesDialog-AllowMultipleOutputDevices"),
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

            if (FileManager.itemViewHolder.MultipleOutputDevices)
                SentrySdk.CaptureMessage("OutputDevicesDialog-MultipleOutputDevices-Enabled");
            else
                SentrySdk.CaptureMessage("OutputDevicesDialog-MultipleOutputDevices-Disabled");
        }

        private async void DeviceWatcherHelper_DevicesChanged(object sender, EventArgs e)
        {
            await MainPage.dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                LoadDevices();
            });
        }

        private void LoadDevices()
        {
            devicesStackPanel.Children.Clear();
            outputDeviceCheckboxes.Clear();

            if (FileManager.itemViewHolder.MultipleOutputDevices)
            {
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

                    outputDeviceCheckboxes.Add(outputDeviceCheckbox);
                    devicesStackPanel.Children.Add(outputDeviceCheckbox);
                }

                UpdateOutputDeviceCheckboxes();
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

        private void UpdateOutputDeviceCheckboxes()
        {
            int checkedItems = 0;

            foreach (var checkbox in outputDeviceCheckboxes)
                if (checkbox.IsChecked.Value) checkedItems++;

            if (checkedItems == 1)
            {
                // Disable the single checked checkbox
                foreach (var checkbox in outputDeviceCheckboxes)
                {
                    if (checkbox.IsChecked.Value)
                    {
                        checkbox.IsEnabled = false;
                        break;
                    }
                }
            }
            else
            {
                // Enable all checkboxes
                foreach (var checkbox in outputDeviceCheckboxes)
                    checkbox.IsEnabled = true;
            }
        }

        private void OutputDeviceCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox checkbox = sender as CheckBox;
            string deviceId = checkbox.Tag as string;

            if (FileManager.itemViewHolder.OutputDevice.Contains(deviceId)) return;

            List<string> deviceIds = FileManager.itemViewHolder.OutputDevice.Split(",").ToList();
            deviceIds.Add(deviceId);
            FileManager.itemViewHolder.OutputDevice = string.Join(",", deviceIds);

            UpdateOutputDeviceCheckboxes();

            SentrySdk.CaptureMessage("OutputDevicesDialog-OutputDeviceCheckbox-Checked");
        }

        private void OutputDeviceCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckBox checkbox = sender as CheckBox;
            string deviceId = checkbox.Tag as string;

            if (!FileManager.itemViewHolder.OutputDevice.Contains(deviceId)) return;

            string[] deviceIds = FileManager.itemViewHolder.OutputDevice.Split(",");
            FileManager.itemViewHolder.OutputDevice = string.Join(",", deviceIds.Where(id => id != deviceId).ToArray());

            UpdateOutputDeviceCheckboxes();

            SentrySdk.CaptureMessage("OutputDevicesDialog-OutputDeviceCheckbox-Unchecked");
        }

        private void StandardOutputDeviceRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            FileManager.itemViewHolder.UseStandardOutputDevice = true;

            SentrySdk.CaptureMessage("OutputDevicesDialog-StandardOutputDeviceRadioButton-Checked");
        }

        private void OutputDeviceRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton radioButton = sender as RadioButton;
            string deviceId = radioButton.Tag as string;

            FileManager.itemViewHolder.UseStandardOutputDevice = false;
            FileManager.itemViewHolder.OutputDevice = deviceId;

            SentrySdk.CaptureMessage("OutputDevicesDialog-OutputDeviceRadioButton-Checked");
        }
    }
}
