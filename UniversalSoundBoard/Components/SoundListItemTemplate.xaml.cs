using UniversalSoundBoard.DataAccess;
using UniversalSoundBoard.Models;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UniversalSoundboard.Components
{
    public sealed partial class SoundListItemTemplate : UserControl
    {
        public Sound Sound { get => DataContext as Sound; }
        public SoundItemOptionsFlyout OptionsFlyout;

        public SoundListItemTemplate()
        {
            InitializeComponent();
            DataContextChanged += (s, e) => Bindings.Update();
            ContentRoot.DataContext = FileManager.itemViewHolder;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            InitOptionsFlyout();
        }

        private void InitOptionsFlyout()
        {
            if (Sound == null) return;
            OptionsFlyout = new SoundItemOptionsFlyout(Sound.Uuid, Sound.Favourite);

            OptionsFlyout.FlyoutOpened += OptionsFlyout_FlyoutOpened;
            OptionsFlyout.SetCategoryFlyoutItemClick += OptionsFlyout_SetCategoryFlyoutItemClick;
            OptionsFlyout.SetFavouriteFlyoutItemClick += OptionsFlyout_SetFavouriteFlyoutItemClick;
            OptionsFlyout.ShareFlyoutItemClick += OptionsFlyout_ShareFlyoutItemClick;
            OptionsFlyout.ExportFlyoutItemClick += OptionsFlyout_ExportFlyoutItemClick;
            OptionsFlyout.PinFlyoutItemClick += OptionsFlyout_PinFlyoutItemClick;
            OptionsFlyout.SetImageFlyoutItemClick += OptionsFlyout_SetImageFlyoutItemClick;
            OptionsFlyout.RenameFlyoutItemClick += OptionsFlyout_RenameFlyoutItemClick;
            OptionsFlyout.DeleteFlyoutItemClick += OptionsFlyout_DeleteFlyoutItemClick;
        }

        private void ContentRoot_RightTapped(object sender, Windows.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {
            OptionsFlyout.ShowAt(sender as UIElement, e.GetPosition(sender as UIElement));
        }

        private void ContentRoot_Holding(object sender, Windows.UI.Xaml.Input.HoldingRoutedEventArgs e)
        {
            OptionsFlyout.ShowAt(sender as UIElement, e.GetPosition(sender as UIElement));
        }

        private void OptionsFlyout_FlyoutOpened(object sender, object e)
        {

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
