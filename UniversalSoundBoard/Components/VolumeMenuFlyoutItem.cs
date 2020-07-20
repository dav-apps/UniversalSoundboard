using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

namespace UniversalSoundboard.Components
{
    public class VolumeMenuFlyoutItem : MenuFlyoutItem
    {
        public event EventHandler<RoutedEventArgs> MuteButton_Click;
        public event EventHandler<RangeBaseValueChangedEventArgs> VolumeSlider_ValueChanged;

        public VolumeMenuFlyoutItem()
        {
            Style = (Style)Application.Current.Resources["VolumeMenuFlyoutItem"];
        }

        protected override void OnApplyTemplate()
        {
            Button MuteButton = GetTemplateChild("MuteButton") as Button;
            MuteButton.Click += (object sender, RoutedEventArgs e) => MuteButton_Click?.Invoke(sender, e);

            Slider VolumeSlider = GetTemplateChild("VolumeSlider") as Slider;
            VolumeSlider.ValueChanged += (object sender, RangeBaseValueChangedEventArgs e) => VolumeSlider_ValueChanged?.Invoke(sender, e);

            base.OnApplyTemplate();
        }

        protected override void OnPointerPressed(PointerRoutedEventArgs e){}

        protected override void OnTapped(TappedRoutedEventArgs e){}
    }
}
