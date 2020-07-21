using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace UniversalSoundboard.Components
{
    public sealed partial class VolumeControl : UserControl
    {
        public event EventHandler<RangeBaseValueChangedEventArgs> ValueChanged;
        public new event EventHandler<RoutedEventArgs> LostFocus;

        public double Value
        {
            get => VolumeSlider.Value;
            set => VolumeSlider.Value = value;
        }

        public VolumeControl()
        {
            InitializeComponent();
        }

        private void VolumeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e) => ValueChanged?.Invoke(sender, e);

        private void VolumeSlider_LostFocus(object sender, RoutedEventArgs e) => LostFocus?.Invoke(sender, e);
    }
}
