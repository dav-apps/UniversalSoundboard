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

        private void DeviceWatcherHelper_DevicesChanged(object sender, System.EventArgs e)
        {
            LoadDevices();
        }

        private void LoadDevices()
        {
            devicesStackPanel.Children.Clear();

            if (FileManager.itemViewHolder.MultipleOutputDevices)
            {
                devicesStackPanel.Children.Add(new CheckBox
                {
                    Content = FileManager.loader.GetString("StandardOutputDevice"),
                    IsChecked = FileManager.itemViewHolder.UseStandardOutputDevice
                });

                foreach (var device in FileManager.deviceWatcherHelper.Devices)
                {
                    devicesStackPanel.Children.Add(new CheckBox
                    {
                        Content = device.Name,
                        Tag = device.Id
                    });
                }
            }
            else
            {
                WinUI.RadioButtons radioButtons = new WinUI.RadioButtons();

                radioButtons.Items.Add(FileManager.loader.GetString("StandardOutputDevice"));
                radioButtons.SelectedIndex = 0;
                int i = 1;

                foreach (var device in FileManager.deviceWatcherHelper.Devices)
                {
                    radioButtons.Items.Add(device.Name);

                    if (!FileManager.itemViewHolder.UseStandardOutputDevice && FileManager.itemViewHolder.OutputDevice == device.Id)
                        radioButtons.SelectedIndex = i;

                    i++;
                }

                devicesStackPanel.Children.Add(radioButtons);
            }
        }
    }
}
