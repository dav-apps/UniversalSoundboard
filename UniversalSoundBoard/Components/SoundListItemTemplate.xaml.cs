using UniversalSoundBoard.DataAccess;
using UniversalSoundBoard.Models;
using Windows.Foundation;
using Windows.UI.Xaml;
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
            ContentRoot.DataContext = FileManager.itemViewHolder;
        }

        private void ContentRoot_RightTapped(object sender, Windows.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {
            ShowFlyout(sender, e.GetPosition(sender as UIElement));
        }

        private void ContentRoot_Holding(object sender, Windows.UI.Xaml.Input.HoldingRoutedEventArgs e)
        {
            ShowFlyout(sender, e.GetPosition(sender as UIElement));
        }

        private void ShowFlyout(object sender, Point position)
        {
            var flyout = new SoundItemOptionsFlyout(Sound.Uuid, Sound.Favourite);

            flyout.SetCategoryFlyoutItemClick += OptionsFlyout_SetCategoryFlyoutItemClick;
            flyout.SetFavouriteFlyoutItemClick += OptionsFlyout_SetFavouriteFlyoutItemClick;
            flyout.ShareFlyoutItemClick += OptionsFlyout_ShareFlyoutItemClick;
            flyout.ExportFlyoutItemClick += OptionsFlyout_ExportFlyoutItemClick;
            flyout.PinFlyoutItemClick += OptionsFlyout_PinFlyoutItemClick;
            flyout.SetImageFlyoutItemClick += OptionsFlyout_SetImageFlyoutItemClick;
            flyout.RenameFlyoutItemClick += OptionsFlyout_RenameFlyoutItemClick;
            flyout.DeleteFlyoutItemClick += OptionsFlyout_DeleteFlyoutItemClick;

            flyout.ShowAt(sender as UIElement, position);
        }

        private void OptionsFlyout_SetCategoryFlyoutItemClick(object sender, RoutedEventArgs e)
        {

        }

        private void OptionsFlyout_SetFavouriteFlyoutItemClick(object sender, RoutedEventArgs e)
        {
            
        }

        private void OptionsFlyout_ShareFlyoutItemClick(object sender, RoutedEventArgs e)
        {
            
        }

        private void OptionsFlyout_ExportFlyoutItemClick(object sender, RoutedEventArgs e)
        {
            
        }

        private void OptionsFlyout_PinFlyoutItemClick(object sender, RoutedEventArgs e)
        {
            
        }

        private void OptionsFlyout_SetImageFlyoutItemClick(object sender, RoutedEventArgs e)
        {
            
        }

        private void OptionsFlyout_RenameFlyoutItemClick(object sender, RoutedEventArgs e)
        {
            
        }

        private void OptionsFlyout_DeleteFlyoutItemClick(object sender, RoutedEventArgs e)
        {
            
        }
    }
}
