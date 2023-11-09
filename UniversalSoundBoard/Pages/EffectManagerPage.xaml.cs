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
            // Fade in effect
            FadeInEffectEnableToggle.IsOn = FileManager.itemViewHolder.IsFadeInEffectEnabled;
            FadeInEffectDurationSlider.IsEnabled = FileManager.itemViewHolder.IsFadeInEffectEnabled;
            FadeInEffectDurationSlider.Value = FileManager.itemViewHolder.FadeInEffectDuration;

            // Fade out effect
            FadeOutEffectEnableToggle.IsOn = FileManager.itemViewHolder.IsFadeOutEffectEnabled;
            FadeOutEffectDurationSlider.IsEnabled = FileManager.itemViewHolder.IsFadeOutEffectEnabled;
            FadeOutEffectDurationSlider.Value = FileManager.itemViewHolder.FadeOutEffectDuration;

            // Echo effect
            EchoEffectEnableToggle.IsOn = FileManager.itemViewHolder.IsEchoEffectEnabled;
            EchoEffectDelaySlider.IsEnabled = FileManager.itemViewHolder.IsEchoEffectEnabled;
            EchoEffectDelaySlider.Value = FileManager.itemViewHolder.EchoEffectDelay;

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

        #region Fade in effect
        private void FadeInEffectEnableToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (!initialized) return;

            FileManager.itemViewHolder.IsFadeInEffectEnabled = FadeInEffectEnableToggle.IsOn;
            FadeInEffectDurationSlider.IsEnabled = FadeInEffectEnableToggle.IsOn;
        }

        private void FadeInEffectDurationSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (!initialized) return;
            FileManager.itemViewHolder.FadeInEffectDuration = (int)e.NewValue;
        }
        #endregion

        #region Fade out effect
        private void FadeOutEffectEnableToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (!initialized) return;

            FileManager.itemViewHolder.IsFadeOutEffectEnabled = FadeOutEffectEnableToggle.IsOn;
            FadeOutEffectDurationSlider.IsEnabled = FadeOutEffectEnableToggle.IsOn;
        }

        private void FadeOutEffectDurationSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (!initialized) return;
            FileManager.itemViewHolder.FadeOutEffectDuration = (int)e.NewValue;
        }
        #endregion

        #region Echo effect
        private void EchoEffectEnableToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (!initialized) return;

            FileManager.itemViewHolder.IsEchoEffectEnabled = EchoEffectEnableToggle.IsOn;
            EchoEffectDelaySlider.IsEnabled = EchoEffectEnableToggle.IsOn;
        }

        private void EchoEffectDelaySlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (!initialized) return;
            FileManager.itemViewHolder.EchoEffectDelay = (int)e.NewValue;
        }
        #endregion

        #region Limiter effect
        private void LimiterEffectEnableToggle_Toggled(object sender, RoutedEventArgs e)
        {

        }

        private void LimiterEffectLoudnessSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {

        }
        #endregion
    }
}
