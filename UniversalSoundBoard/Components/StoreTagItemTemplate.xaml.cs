using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UniversalSoundboard.Components
{
    public sealed partial class StoreTagItemTemplate : UserControl
    {
        public new string Tag { get; set; }

        public StoreTagItemTemplate()
        {
            InitializeComponent();
        }

        private void UserControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (DataContext == null) return;

            Tag = DataContext as string;
            Bindings.Update();
        }
    }
}
