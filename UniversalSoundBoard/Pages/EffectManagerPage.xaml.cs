using Sentry;
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

            // Limiter effect
            LimiterEffectEnableToggle.IsOn = FileManager.itemViewHolder.IsLimiterEffectEnabled;
            LimiterEffectLoudnessSlider.IsEnabled = FileManager.itemViewHolder.IsLimiterEffectEnabled;
            LimiterEffectLoudnessSlider.Value = FileManager.itemViewHolder.LimiterEffectLoudness;

            // Reverb effect
            ReverbEffectEnableToggle.IsOn = FileManager.itemViewHolder.IsReverbEffectEnabled;
            ReverbEffectDecaySlider.IsEnabled = FileManager.itemViewHolder.IsReverbEffectEnabled;
            ReverbEffectDecaySlider.Value = FileManager.itemViewHolder.ReverbEffectDecay;

            // Pitch shift effect
            PitchShiftEffectEnableToggle.IsOn = FileManager.itemViewHolder.IsPitchShiftEffectEnabled;
            PitchShiftEffectFactorSlider.IsEnabled = FileManager.itemViewHolder.IsPitchShiftEffectEnabled;
            PitchShiftEffectFactorSlider.Value = FileManager.itemViewHolder.PitchShiftEffectFactor * 100;

            initialized = true;
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double windowWidth = ContentRoot.ActualWidth;
            double containerWidth = 300;

            if (windowWidth < 550)
                containerWidth = 225;
            else if (windowWidth < 650)
                containerWidth = 250;
            else if (windowWidth < 750)
                containerWidth = 275;

            FadeInContainer.Width = containerWidth;
            FadeOutContainer.Width = containerWidth;
            EchoContainer.Width = containerWidth;
            LimiterContainer.Width = containerWidth;
            ReverbContainer.Width = containerWidth;
            PitchContainer.Width = containerWidth;
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

            if (FadeInEffectEnableToggle.IsOn)
                SentrySdk.CaptureMessage("EffectManagerPage-FadeInEffect-Enabled");
            else
                SentrySdk.CaptureMessage("EffectManagerPage-FadeInEffect-Disabled");
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

            if (FadeOutEffectEnableToggle.IsOn)
                SentrySdk.CaptureMessage("EffectManagerPage-FadeOutEffect-Enabled");
            else
                SentrySdk.CaptureMessage("EffectManagerPage-FadeOutEffect-Disabled");
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

            if (EchoEffectEnableToggle.IsOn)
                SentrySdk.CaptureMessage("EffectManagerPage-EchoEffect-Enabled");
            else
                SentrySdk.CaptureMessage("EffectManagerPage-EchoEffect-Disabled");
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
            if (!initialized) return;

            FileManager.itemViewHolder.IsLimiterEffectEnabled = LimiterEffectEnableToggle.IsOn;
            LimiterEffectLoudnessSlider.IsEnabled = LimiterEffectEnableToggle.IsOn;

            if (LimiterEffectEnableToggle.IsOn)
                SentrySdk.CaptureMessage("EffectManagerPage-LimiterEffect-Enabled");
            else
                SentrySdk.CaptureMessage("EffectManagerPage-LimiterEffect-Disabled");
        }

        private void LimiterEffectLoudnessSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (!initialized) return;
            FileManager.itemViewHolder.LimiterEffectLoudness = (int)e.NewValue;
        }
        #endregion

        #region Reverb effect
        private void ReverbEffectEnableToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (!initialized) return;

            FileManager.itemViewHolder.IsReverbEffectEnabled = ReverbEffectEnableToggle.IsOn;
            ReverbEffectDecaySlider.IsEnabled = ReverbEffectEnableToggle.IsOn;

            if (ReverbEffectEnableToggle.IsOn)
                SentrySdk.CaptureMessage("EffectManagerPage-ReverbEffect-Enabled");
            else
                SentrySdk.CaptureMessage("EffectManagerPage-ReverbEffect-Disabled");
        }

        private void ReverbEffectDecaySlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (!initialized) return;
            FileManager.itemViewHolder.ReverbEffectDecay = e.NewValue;
        }
        #endregion

        #region Pitch shift effect
        private void PitchShiftEffectEnableToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (!initialized) return;

            FileManager.itemViewHolder.IsPitchShiftEffectEnabled = PitchShiftEffectEnableToggle.IsOn;
            PitchShiftEffectFactorSlider.IsEnabled = PitchShiftEffectEnableToggle.IsOn;

            if (PitchShiftEffectEnableToggle.IsOn)
                SentrySdk.CaptureMessage("EffectManagerPage-PitchShiftEffect-Enabled");
            else
                SentrySdk.CaptureMessage("EffectManagerPage-PitchShiftEffect-Disabled");
        }

        private void PitchShiftEffectFactorSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (!initialized) return;
            FileManager.itemViewHolder.PitchShiftEffectFactor = e.NewValue / 100;
        }
        #endregion
    }
}
