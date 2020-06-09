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
            DataContextChanged += (s, e) => Bindings.Update();
            ContentRoot.DataContext = FileManager.itemViewHolder;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            soundItem = new SoundItem(Sound, (DataTemplate)Resources["SetCategoryItemTemplate"]);
            soundItem.FavouriteChanged += SoundItem_FavouriteChanged;
        }

        private void SoundItem_FavouriteChanged(object sender, bool newFav)
        {
            Sound.Favourite = newFav;

            // Update the UI
            Sound.Favourite = newFav;
            Bindings.Update();
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
