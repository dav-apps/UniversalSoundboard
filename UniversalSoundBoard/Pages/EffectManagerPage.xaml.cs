using System.ComponentModel;
using UniversalSoundboard.Common;
using UniversalSoundboard.DataAccess;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace UniversalSoundboard.Pages
{
    public sealed partial class EffectManagerPage : Page
    {
        bool initialized = false;

        public EffectManagerPage()
        {
            InitializeComponent();

            ContentRoot.DataContext = FileManager.itemViewHolder;

            FileManager.itemViewHolder.PropertyChanged += ItemViewHolder_PropertyChanged;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            RequestedTheme = FileManager.GetRequestedTheme();

            // Init the UI elements
            EchoEffectEnableToggle.IsOn = FileManager.itemViewHolder.IsEchoEffectEnabled;
            EchoEffectVolumeSlider.Value = FileManager.itemViewHolder.EchoEffectVolume * 100;
            EchoEffectDelaySlider.Value = FileManager.itemViewHolder.EchoEffectDelay;

            EchoEffectVolumeSlider.IsEnabled = FileManager.itemViewHolder.IsEchoEffectEnabled;
            EchoEffectDelaySlider.IsEnabled = FileManager.itemViewHolder.IsEchoEffectEnabled;

            initialized = true;
        }

        private void ItemViewHolder_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case ItemViewHolder.CurrentThemeKey:
                    RequestedTheme = FileManager.GetRequestedTheme();
                    break;
            }
        }

        private void EchoEffectEnableToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (!initialized) return;
            FileManager.itemViewHolder.IsEchoEffectEnabled = EchoEffectEnableToggle.IsOn;

            EchoEffectVolumeSlider.IsEnabled = EchoEffectEnableToggle.IsOn;
            EchoEffectDelaySlider.IsEnabled = EchoEffectEnableToggle.IsOn;
        }

        private void EchoEffectVolumeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (!initialized) return;
            FileManager.itemViewHolder.EchoEffectVolume = e.NewValue / 100;
        }

        private void EchoEffectDelaySlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (!initialized) return;
            FileManager.itemViewHolder.EchoEffectDelay = (int)e.NewValue;
        }
    }
}
