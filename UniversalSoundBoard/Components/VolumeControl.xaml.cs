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
        private bool muted = false;

        public new Brush Background { get; set; }
        public new Thickness Padding { get; set; }
        public bool Muted
        {
            get => muted;
            set
            {
                muted = value;
                UpdateVolumeIcon();
            }
        }
        public int Value
        {
            get => Convert.ToInt32(VolumeSlider.Value);
            set
            {
                skipVolumeSliderValueChanged = true;
                VolumeSlider.Value = value;
                skipVolumeSliderValueChanged = false;
            }
        }
        public double SliderWidth
        {
            get => VolumeSlider.Width;
            set
            {
                VolumeSlider.Width = value;
            }
        }

        public event EventHandler<RangeBaseValueChangedEventArgs> ValueChanged;
        public event EventHandler<string> IconChanged;
        public new event EventHandler<RoutedEventArgs> LostFocus;
        public event EventHandler<bool> MuteChanged;

        public VolumeControl()
        {
            InitializeComponent();
            UpdateVolumeIcon();
        }

        private void VolumeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (muted)
            {
                muted = false;
                MuteChanged?.Invoke(this, false);
            }

            UpdateVolumeIcon();
            if (skipVolumeSliderValueChanged) return;
            ValueChanged?.Invoke(sender, e);
        }

        private void VolumeSlider_LostFocus(object sender, RoutedEventArgs e) => LostFocus?.Invoke(this, e);

        private void MuteButton_Click(object sender, RoutedEventArgs e)
        {
            muted = !muted;
            UpdateVolumeIcon();
            MuteChanged?.Invoke(this, muted);
        }

        private void UpdateVolumeIcon()
        {
            string oldIcon = MuteButton.Content as string;
            string newIcon = GetVolumeIcon(VolumeSlider.Value, muted);
            MuteButton.Content = newIcon;

            // If the icon changed, invoke the IconChanged event with the new icon
            if (!oldIcon.Equals(newIcon))
                IconChanged?.Invoke(this, newIcon);
        }

        public static string GetVolumeIcon(double volume, bool muted = false)
        {
            if (volume <= 0 || muted)
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
