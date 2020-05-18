using UniversalSoundBoard.DataAccess;
using UniversalSoundBoard.Models;
using Windows.UI.Xaml.Controls;

namespace UniversalSoundboard.Components
{
    public sealed partial class SoundListItemTemplate : UserControl
    {
        public Sound Sound { get => DataContext as Sound; }

        public SoundListItemTemplate()
        {
            InitializeComponent();
            DataContextChanged += (s, e) => Bindings.Update();
            SetDataContext();
        }

        private void SetDataContext()
        {
            ContentRoot.DataContext = FileManager.itemViewHolder;
        }
    }
}
