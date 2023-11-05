using UniversalSoundboard.DataAccess;
using Windows.UI.Xaml.Controls;

namespace UniversalSoundboard.Pages
{
    public sealed partial class EffectManagerPage : Page
    {
        bool initialized = false;

        public EffectManagerPage()
        {
            InitializeComponent();

            ContentRoot.DataContext = FileManager.itemViewHolder;
        }

        private void Page_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            // Init the UI elements
            EchoEffectEnableToggle.IsOn = FileManager.itemViewHolder.IsEchoEffectEnabled;
            EchoEffectVolumeSlider.Value = FileManager.itemViewHolder.EchoEffectVolume * 100;
            EchoEffectDelaySlider.Value = FileManager.itemViewHolder.EchoEffectDelay;
            initialized = true;
        }

        private void EchoEffectEnableToggle_Toggled(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (!initialized) return;
            FileManager.itemViewHolder.IsEchoEffectEnabled = EchoEffectEnableToggle.IsOn;
        }

        private void EchoEffectVolumeSlider_ValueChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (!initialized) return;
            FileManager.itemViewHolder.EchoEffectVolume = e.NewValue / 100;
        }

        private void EchoEffectDelaySlider_ValueChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (!initialized) return;
            FileManager.itemViewHolder.EchoEffectDelay = (int)e.NewValue;
        }
    }
}
