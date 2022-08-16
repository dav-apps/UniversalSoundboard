using UniversalSoundboard.Models;
using Windows.UI.Xaml.Controls;

namespace UniversalSoundboard.Components
{
    public sealed partial class SoundDownloadListItemTemplate : UserControl
    {
        public SoundDownloadListItem SoundDownloadListItem { get => DataContext as SoundDownloadListItem; }
        public string SoundName { get => SoundDownloadListItem?.Name; }

        public SoundDownloadListItemTemplate()
        {
            InitializeComponent();
            DataContextChanged += (s, e) => Bindings.Update();
        }
    }
}
