using UniversalSoundBoard.DataAccess;
using UniversalSoundBoard.Models;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UniversalSoundboard.Components
{
    public sealed partial class SoundListItemTemplate : UserControl
    {
        public Sound Sound { get => DataContext as Sound; }
        SoundItem soundItem;

        public SoundListItemTemplate()
        {
            InitializeComponent();
            ContentRoot.DataContext = FileManager.itemViewHolder;

            DataContextChanged += SoundListItemTemplate_DataContextChanged;
        }

        private void SoundListItemTemplate_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            Bindings.Update();
            soundItem = new SoundItem(Sound);
        }

        private void ContentRoot_RightTapped(object sender, Windows.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {
            soundItem.ShowFlyout(sender, e.GetPosition(sender as UIElement));
        }

        private void ContentRoot_Holding(object sender, Windows.UI.Xaml.Input.HoldingRoutedEventArgs e)
        {
            soundItem.ShowFlyout(sender, e.GetPosition(sender as UIElement));
        }
    }
}
