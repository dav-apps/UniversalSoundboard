using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

namespace UniversalSoundboard.Components
{
    public class VolumeMenuFlyoutItem : MenuFlyoutItem
    {
        public event EventHandler<RangeBaseValueChangedEventArgs> VolumeControl_ValueChanged;
        public event EventHandler<RoutedEventArgs> VolumeControl_LostFocus;

        public VolumeMenuFlyoutItem()
        {
            Style = (Style)Application.Current.Resources["VolumeMenuFlyoutItem"];
        }

        protected override void OnApplyTemplate()
        {
            VolumeControl VolumeControl = GetTemplateChild("VolumeControl") as VolumeControl;
            VolumeControl.ValueChanged += (object sender, RangeBaseValueChangedEventArgs e) => VolumeControl_ValueChanged?.Invoke(sender, e);
            VolumeControl.LostFocus += (object sender, RoutedEventArgs e) => VolumeControl_LostFocus?.Invoke(sender, e);

            base.OnApplyTemplate();
        }

        protected override void OnPointerPressed(PointerRoutedEventArgs e){}

        protected override void OnTapped(TappedRoutedEventArgs e){}
    }
}
