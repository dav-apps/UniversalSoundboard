using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UniversalSoundBoard
{
    public sealed class CustomMediaTransportControls : MediaTransportControls
    {
        public event EventHandler<EventArgs> Removed;
        public event EventHandler<EventArgs> FavouriteFlyout_Clicked;
        public event EventHandler<EventArgs> Repeat_1x_Clicked;
        public event EventHandler<EventArgs> Repeat_2x_Clicked;
        public event EventHandler<EventArgs> Repeat_5x_Clicked;
        public event EventHandler<EventArgs> Repeat_10x_Clicked;
        public event EventHandler<EventArgs> Repeat_endless_Clicked;

        public CustomMediaTransportControls()
        {
            this.DefaultStyleKey = typeof(CustomMediaTransportControls);
        }

        protected override void OnApplyTemplate()
        {
            // This is where you would get your custom button and create an event handler for its click method.
            Button RemoveButton = GetTemplateChild("RemoveButton") as Button;
            RemoveButton.Click += RemoveButton_Click;

            AppBarButton FavouriteFlyout = GetTemplateChild("FavouriteFlyout") as AppBarButton;
            FavouriteFlyout.Click += FavouriteFlyout_Click;

            MenuFlyoutItem Repeat_1x = GetTemplateChild("Repeat_1x") as MenuFlyoutItem;
            Repeat_1x.Click += Repeat_1x_Click;
            MenuFlyoutItem Repeat_2x = GetTemplateChild("Repeat_2x") as MenuFlyoutItem;
            Repeat_2x.Click += Repeat_2x_Click;
            MenuFlyoutItem Repeat_5x = GetTemplateChild("Repeat_5x") as MenuFlyoutItem;
            Repeat_5x.Click += Repeat_5x_Click;
            MenuFlyoutItem Repeat_10x = GetTemplateChild("Repeat_10x") as MenuFlyoutItem;
            Repeat_10x.Click += Repeat_10x_Click;
            MenuFlyoutItem Repeat_endless = GetTemplateChild("Repeat_endless") as MenuFlyoutItem;
            Repeat_endless.Click += Repeat_endless_Click;

            base.OnApplyTemplate();
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            // Raise an event on the custom control when 'Removed' is clicked
            Removed?.Invoke(this, EventArgs.Empty);
        }

        private void FavouriteFlyout_Click(object sender, RoutedEventArgs e)
        {
            FavouriteFlyout_Clicked?.Invoke(this, EventArgs.Empty);
        }

        private void Repeat_1x_Click(object sender, RoutedEventArgs e)
        {
            Repeat_1x_Clicked?.Invoke(this, EventArgs.Empty);
        }

        private void Repeat_2x_Click(object sender, RoutedEventArgs e)
        {
            Repeat_2x_Clicked?.Invoke(this, EventArgs.Empty);
        }

        private void Repeat_5x_Click(object sender, RoutedEventArgs e)
        {
            Repeat_5x_Clicked?.Invoke(this, EventArgs.Empty);
        }

        private void Repeat_10x_Click(object sender, RoutedEventArgs e)
        {
            Repeat_10x_Clicked?.Invoke(this, EventArgs.Empty);
        }

        private void Repeat_endless_Click(object sender, RoutedEventArgs e)
        {
            Repeat_endless_Clicked?.Invoke(this, EventArgs.Empty);
        }
    }
}
