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
                _timesVisible = value;

                // Update the visibility of TimeTextGrid
                Grid timeTextGrid = GetTemplateChild("TimeTextGrid") as Grid;
                if(timeTextGrid != null)
                    timeTextGrid.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public BasicMediaTransportControls()
        {
            Style = (Style)Application.Current.Resources["BasicMediaTransportControls"];
        }
    }
}
