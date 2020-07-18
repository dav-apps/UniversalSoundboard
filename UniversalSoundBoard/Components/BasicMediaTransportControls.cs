using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UniversalSoundboard.Components
{
    public sealed class BasicMediaTransportControls : MediaTransportControls
    {
        public BasicMediaTransportControls()
        {
            Style = (Style)Application.Current.Resources["BasicMediaTransportControls"];
        }
    }
}
