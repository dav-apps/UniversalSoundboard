using UniversalSoundboard.Models;
using Windows.UI.Xaml.Controls;

namespace UniversalSoundboard.Components
{
    public sealed partial class SoundDownloadListItemTemplate : UserControl
    {
        public SoundDownloadItem SoundDownloadItem { get => DataContext as SoundDownloadItem; }
        public string SoundName { get => SoundDownloadItem?.Name; }

        public SoundDownloadListItemTemplate()
        {
            InitializeComponent();
            DataContextChanged += (s, e) => Bindings.Update();
        }
    }
}
