using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;

namespace UniversalSoundboard.Components
{
    public sealed partial class VolumeControl : UserControl
    {
        private bool skipVolumeSliderValueChanged = false;

        public new Brush Background { get; set; }
        public new Thickness Padding { get; set; }

        public event EventHandler<RangeBaseValueChangedEventArgs> ValueChanged;
        public event EventHandler<string> IconChanged;
        public new event EventHandler<RoutedEventArgs> LostFocus;

        public double Value
        {
            get => VolumeSlider.Value;
            set
            {
                skipVolumeSliderValueChanged = true;
                VolumeSlider.Value = value;
                skipVolumeSliderValueChanged = false;
            }
        }

        public VolumeControl()
        {
            InitializeComponent();
            UpdateVolumeIcon(VolumeSlider.Value);
        }

        private void VolumeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            UpdateVolumeIcon(e.NewValue);
            if (skipVolumeSliderValueChanged) return;
            ValueChanged?.Invoke(sender, e);
        }

        private void VolumeSlider_LostFocus(object sender, RoutedEventArgs e) => LostFocus?.Invoke(sender, e);

        private void UpdateVolumeIcon(double volume)
        {
            string oldIcon = MuteButton.Content as string;
            string newIcon = GetVolumeIcon(volume);
            MuteButton.Content = newIcon;

            // If the icon changed, invoke the IconChanged event with the new icon
            if (!oldIcon.Equals(newIcon))
                IconChanged?.Invoke(this, newIcon);
        }

        public static string GetVolumeIcon(double volume)
        {
            if (volume <= 0)
                return "\uE74F";
            else if (volume <= 32)
                return "\uE993";
            else if (volume <= 65)
                return "\uE994";
            else
                return "\uE995";
        }
    }
}
