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

        public int VolumeControlValue
        {
            get => volumeControl == null ? 1 : volumeControl.Value;
            set
            {
                if (volumeControl == null) return;
                volumeControl.Value = value;
            }
        }
        public bool VolumeControlMuted
        {
            get => volumeControl != null && volumeControl.Muted;
            set
            {
                if (volumeControl == null) return;
                volumeControl.Muted = value;
            }
        }

        public event EventHandler<RangeBaseValueChangedEventArgs> VolumeControl_ValueChanged;
        public event EventHandler<string> VolumeControl_IconChanged;
        public event EventHandler<RoutedEventArgs> VolumeControl_LostFocus;
        public event EventHandler<bool> VolumeControl_MuteChanged;

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
            volumeControl.MuteChanged += (object sender, bool muted) => VolumeControl_MuteChanged?.Invoke(sender, muted);

            base.OnApplyTemplate();
        }

        protected override void OnPointerPressed(PointerRoutedEventArgs e){}

        protected override void OnTapped(TappedRoutedEventArgs e){}
    }
}
