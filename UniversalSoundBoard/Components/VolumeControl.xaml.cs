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
        }

        private void VolumeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (skipVolumeSliderValueChanged) return;
            ValueChanged?.Invoke(sender, e);
        }

        private void VolumeSlider_LostFocus(object sender, RoutedEventArgs e) => LostFocus?.Invoke(sender, e);
    }
}
