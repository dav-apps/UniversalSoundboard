using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UniversalSoundboard.Components
{
    public sealed class BasicMediaTransportControls : MediaTransportControls
    {
        private bool _timesVisible = true;
        public bool TimesVisible
        {
            get => _timesVisible;
            set
            {
                if (_timesVisible.Equals(value)) return;
                _timesVisible = value;

                // Update the visibility of the time elements
                TextBlock timeElapsedElementTextBlock = GetTemplateChild("TimeElapsedElement") as TextBlock;
                if(timeElapsedElementTextBlock != null)
                    timeElapsedElementTextBlock.Visibility = value ? Visibility.Visible : Visibility.Collapsed;

                TextBlock timeRemainingElementTextBlock = GetTemplateChild("TimeRemainingElement") as TextBlock;
                if (timeRemainingElementTextBlock != null)
                    timeRemainingElementTextBlock.Visibility = value ? Visibility.Visible : Visibility.Collapsed;

                SetTimelineLayout(_timelineLayoutCompact);
            }
        }

        private bool _timelineLayoutCompact = false;
        public bool TimelineLayoutCompact
        {
            get => _timelineLayoutCompact;
            set
            {
                if (_timelineLayoutCompact.Equals(value)) return;
                _timelineLayoutCompact = value;
                SetTimelineLayout(_timelineLayoutCompact);
            }
        }

        public BasicMediaTransportControls()
        {
            Style = (Style)Application.Current.Resources["BasicMediaTransportControls"];
        }

        protected override void OnApplyTemplate()
        {
            SetTimelineLayout(_timelineLayoutCompact);
            base.OnApplyTemplate();
        }

        private void SetTimelineLayout(bool compact)
        {
            Slider ProgressSlider = GetTemplateChild("ProgressSlider") as Slider;
            ProgressBar BufferingProgressBar = GetTemplateChild("BufferingProgressBar") as ProgressBar;
            TextBlock TimeElapsedElement = GetTemplateChild("TimeElapsedElement") as TextBlock;
            TextBlock TimeRemainingElement = GetTemplateChild("TimeRemainingElement") as TextBlock;

            if (compact)
            {
                // ProgressSlider
                RelativePanel.SetAlignLeftWithPanel(ProgressSlider, false);
                RelativePanel.SetAlignRightWithPanel(ProgressSlider, false);

                RelativePanel.SetRightOf(ProgressSlider, TimeElapsedElement);
                RelativePanel.SetLeftOf(ProgressSlider, TimeRemainingElement);

                ProgressSlider.Margin = new Thickness(8, 0, 8, 0);
                ProgressSlider.Height = 37;

                // BufferingProgressBar
                RelativePanel.SetAlignLeftWithPanel(BufferingProgressBar, false);
                RelativePanel.SetAlignRightWithPanel(BufferingProgressBar, false);
                RelativePanel.SetAlignTopWithPanel(BufferingProgressBar, false);

                RelativePanel.SetRightOf(BufferingProgressBar, TimeElapsedElement);
                RelativePanel.SetLeftOf(BufferingProgressBar, TimeRemainingElement);
                RelativePanel.SetAlignVerticalCenterWith(BufferingProgressBar, TimeElapsedElement);

                BufferingProgressBar.Margin = new Thickness(8, 2, 8, 0);

                // TimeElapsedElement
                RelativePanel.SetAlignBottomWith(TimeElapsedElement, null);
                RelativePanel.SetAlignLeftWithPanel(TimeElapsedElement, true);
                RelativePanel.SetAlignVerticalCenterWithPanel(TimeElapsedElement, true);

                TimeElapsedElement.Margin = new Thickness(0);

                // TimeRemainingElement
                RelativePanel.SetAlignBottomWith(TimeRemainingElement, null);
                RelativePanel.SetAlignRightWithPanel(TimeRemainingElement, true);
                RelativePanel.SetAlignVerticalCenterWithPanel(TimeRemainingElement, true);

                TimeRemainingElement.Margin = new Thickness(0);
            }
            else
            {
                // ProgressSlider
                RelativePanel.SetAlignLeftWithPanel(ProgressSlider, true);
                RelativePanel.SetAlignRightWithPanel(ProgressSlider, true);

                RelativePanel.SetRightOf(ProgressSlider, null);
                RelativePanel.SetLeftOf(ProgressSlider, null);

                ProgressSlider.Margin = new Thickness(0, 0, 0, _timesVisible ? 14 : 0);
                ProgressSlider.Height = 33;

                // BufferingProgressBar
                RelativePanel.SetAlignLeftWithPanel(BufferingProgressBar, true);
                RelativePanel.SetAlignRightWithPanel(BufferingProgressBar, true);
                RelativePanel.SetAlignTopWithPanel(BufferingProgressBar, true);

                RelativePanel.SetRightOf(BufferingProgressBar, null);
                RelativePanel.SetLeftOf(BufferingProgressBar, null);
                RelativePanel.SetAlignVerticalCenterWith(BufferingProgressBar, null);

                BufferingProgressBar.Margin = new Thickness(0, 18, 0, _timesVisible ? 14 : 0);

                // TimeElapsedElement
                RelativePanel.SetAlignBottomWith(TimeElapsedElement, ProgressSlider);
                RelativePanel.SetAlignLeftWithPanel(TimeElapsedElement, true);
                RelativePanel.SetAlignVerticalCenterWithPanel(TimeElapsedElement, false);

                TimeElapsedElement.Margin = new Thickness(0);

                // TimeRemainingElement
                RelativePanel.SetAlignBottomWith(TimeRemainingElement, ProgressSlider);
                RelativePanel.SetAlignRightWithPanel(TimeRemainingElement, true);
                RelativePanel.SetAlignVerticalCenterWithPanel(TimeRemainingElement, false);

                TimeRemainingElement.Margin = new Thickness(0);
            }
        }
    }
}
