using System;
using UniversalSoundBoard.Models;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Microsoft.Toolkit.Uwp.UI.Animations;
using UniversalSoundBoard.DataAccess;
using Windows.Foundation;
using UniversalSoundboard.Components;

namespace UniversalSoundBoard.Components
{
    public sealed partial class SoundTileTemplate : UserControl
    {
        public Sound Sound { get => DataContext as Sound; }
        SoundItem soundItem;
        private double visibleNameMarginBottom = 0;
        double soundTileNameContainerHeight = 0;
        int soundTileNameLines = 0;
        double soundTileWidth = 200;


        public SoundTileTemplate()
        {
            InitializeComponent();
            DataContextChanged += (s, e) => Bindings.Update();
            ContentRoot.DataContext = FileManager.itemViewHolder;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            soundItem = new SoundItem(Sound, (DataTemplate)Resources["SetCategoryItemTemplate"]);
            soundItem.FavouriteChanged += SoundItem_FavouriteChanged;

            UpdateSizes();
            FileManager.itemViewHolder.SoundTileSizeChangedEvent += ItemViewHolder_SoundTileSizeChangedEvent;
        }

        private void SoundItem_FavouriteChanged(object sender, bool newFav)
        {
            Sound.Favourite = newFav;

            // TODO: Update the UI
            //FavouriteSymbol.Visibility = newFav ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ItemViewHolder_SoundTileSizeChangedEvent(object sender, SizeChangedEventArgs e)
        {
            UpdateSizes();
        }

        private void UpdateSizes()
        {
            SetupNameAnimations();
            UserControlClipRect.Rect = new Rect(0, 0, FileManager.itemViewHolder.SoundTileWidth, 200);
            soundTileWidth = FileManager.itemViewHolder.SoundTileWidth;
            Bindings.Update();
        }

        private void SetupNameAnimations()
        {
            soundTileNameContainerHeight = SoundTileName.ActualHeight;
            Bindings.Update();

            // Calculate the margin that the text needs to move up to be completely visible
            double lineHeight = 26.6015625;
            soundTileNameLines = Convert.ToInt32(SoundTileName.ActualHeight / lineHeight);
            visibleNameMarginBottom = -(soundTileNameLines - 1) * lineHeight;

            SoundTileNameContainer.Margin = new Thickness(0, 0, 0, visibleNameMarginBottom);
            ShowNameStoryboardAnimation.To = visibleNameMarginBottom;

            SoundTileNameContainerAcrylicBrush.TintColor = FileManager.GetApplicationThemeColor();
        }

        private void ContentRoot_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            soundItem.ShowFlyout(sender, e.GetPosition(sender as UIElement));
        }

        private void ContentRoot_Holding(object sender, HoldingRoutedEventArgs e)
        {
            soundItem.ShowFlyout(sender, e.GetPosition(sender as UIElement));
        }

        private void ContentRoot_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            // Scale the image
            SoundTileImage.Scale(1.1f, 1.1f, Convert.ToInt32(SoundTileImage.ActualWidth / 2), Convert.ToInt32(SoundTileImage.ActualHeight / 2), 400, 0, EasingType.Quintic).Start();

            // Show the animation of the name
            ShowNameStoryboard.Begin();
        }

        private void ContentRoot_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            // Scale the image
            SoundTileImage.Scale(1, 1, Convert.ToInt32(SoundTileImage.ActualWidth / 2), Convert.ToInt32(SoundTileImage.ActualHeight / 2), 400, 0, EasingType.Quintic).Start();

            // Show the animation of the name
            HideNameStoryboard.Begin();
        }
    }
}
