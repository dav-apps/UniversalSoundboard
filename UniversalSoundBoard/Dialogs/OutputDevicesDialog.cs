using UniversalSoundboard.DataAccess;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace UniversalSoundboard.Dialogs
{
    public class OutputDevicesDialog : Dialog
    {
        public OutputDevicesDialog() : base("Output devices", FileManager.loader.GetString("Actions-Close"))
        {
            Content = GetContent();
        }

        private StackPanel GetContent()
        {
            StackPanel contentStackPanel = new StackPanel();

            // Add default output device toggle
            StackPanel defaultStackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };

            ToggleSwitch defaultToggle = new ToggleSwitch();

            TextBlock defaultTextBlock = new TextBlock
            {
                Text = FileManager.loader.GetString("StandardOutputDevice"),
                VerticalAlignment = VerticalAlignment.Center
            };

            defaultStackPanel.Children.Add(defaultToggle);
            defaultStackPanel.Children.Add(defaultTextBlock);

            Border border = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Margin = new Thickness(0, 8, 0, 8)
            };

            contentStackPanel.Children.Add(defaultStackPanel);
            contentStackPanel.Children.Add(border);

            foreach (var device in FileManager.deviceWatcherHelper.Devices)
            {
                StackPanel containerStackPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal
                };

                ToggleSwitch toggle = new ToggleSwitch();

                TextBlock deviceNameTextBlock = new TextBlock
                {
                    Text = device.Name,
                    VerticalAlignment = VerticalAlignment.Center
                };

                containerStackPanel.Children.Add(toggle);
                containerStackPanel.Children.Add(deviceNameTextBlock);
                contentStackPanel.Children.Add(containerStackPanel);
            }

            return contentStackPanel;
        }
    }
}
