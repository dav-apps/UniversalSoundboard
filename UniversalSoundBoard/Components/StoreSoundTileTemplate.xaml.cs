using UniversalSoundboard.Models;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UniversalSoundboard.Components
{
    public sealed partial class StoreSoundTileTemplate : UserControl
    {
        SoundResponse SoundItem { get; set; }

        public StoreSoundTileTemplate()
        {
            InitializeComponent();
        }

        private void UserControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (DataContext == null) return;

            SoundItem = DataContext as SoundResponse;
            Bindings.Update();
        }
    }
}
