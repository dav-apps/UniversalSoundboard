using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

namespace UniversalSoundboard.Components
{
    public class VolumeMenuFlyoutItem : MenuFlyoutItem
    {
        private VolumeControl volumeControl;

        public double VolumeControlValue
        {
            get => volumeControl == null ? 1 : volumeControl.Value;
            set
            {
                if (volumeControl == null) return;
                volumeControl.Value = value;
            }
        }

        public event EventHandler<RangeBaseValueChangedEventArgs> VolumeControl_ValueChanged;
        public event EventHandler<string> VolumeControl_IconChanged;
        public event EventHandler<RoutedEventArgs> VolumeControl_LostFocus;

        public VolumeMenuFlyoutItem()
        {
            Style = (Style)Application.Current.Resources["VolumeMenuFlyoutItem"];
        }

        protected override void OnApplyTemplate()
        {
            volumeControl = GetTemplateChild("VolumeControl") as VolumeControl;
            volumeControl.ValueChanged += (object sender, RangeBaseValueChangedEventArgs e) => VolumeControl_ValueChanged?.Invoke(sender, e);
            volumeControl.IconChanged += (object sender, string newIcon) => VolumeControl_IconChanged?.Invoke(sender, newIcon);
            volumeControl.LostFocus += (object sender, RoutedEventArgs e) => VolumeControl_LostFocus?.Invoke(sender, e);

            base.OnApplyTemplate();
        }

        protected override void OnPointerPressed(PointerRoutedEventArgs e){}

        protected override void OnTapped(TappedRoutedEventArgs e){}
    }
}
