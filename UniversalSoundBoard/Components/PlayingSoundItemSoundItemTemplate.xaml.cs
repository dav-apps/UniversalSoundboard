using System;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Models;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace UniversalSoundboard.Components
{
    public sealed partial class PlayingSoundItemSoundItemTemplate : UserControl
    {
        public Sound Sound;
        public string name = "";

        public event EventHandler<EventArgs> Remove;

        public PlayingSoundItemSoundItemTemplate()
        {
            InitializeComponent();
            DataContextChanged += PlayingSoundItemSoundItemTemplate_DataContextChanged;
        }

        private void PlayingSoundItemSoundItemTemplate_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (DataContext == null) return;

            Sound = (Sound)DataContext;
            name = Sound.Name;
            Bindings.Update();
        }

        private void SwipeControl_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            MenuFlyout flyout = new MenuFlyout();

            MenuFlyoutItem removeFlyoutItem = new MenuFlyoutItem
            {
                Text = FileManager.loader.GetString("Remove"),
                Icon = new FontIcon { Glyph = "\uE106" }
            };
            removeFlyoutItem.Click += RemoveFlyoutItem_Click;

            flyout.Items.Add(removeFlyoutItem);
            flyout.ShowAt(sender as UIElement, e.GetPosition(sender as UIElement));
        }

        private void SoundsListViewRemoveSwipeItem_Invoked(SwipeItem sender, SwipeItemInvokedEventArgs args)
        {
            Remove?.Invoke(this, EventArgs.Empty);
        }

        private void RemoveFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            Remove?.Invoke(this, EventArgs.Empty);
        }
    }
}
