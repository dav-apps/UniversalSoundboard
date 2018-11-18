using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace UniversalSoundBoard.Components
{
    public sealed class CustomMediaTransportControls : MediaTransportControls
    {
        public event EventHandler<EventArgs> VolumeSlider_LostFocus;
        public event EventHandler<RangeBaseValueChangedEventArgs> VolumeSlider_ValueChanged;
        public event EventHandler<EventArgs> RemoveButton_Clicked;
        public event EventHandler<EventArgs> FavouriteFlyout_Clicked;
        public event EventHandler<EventArgs> CastButton_Clicked;
        public event EventHandler<EventArgs> MenuFlyoutButton_Clicked;
        public event EventHandler<EventArgs> Repeat_1x_Clicked;
        public event EventHandler<EventArgs> Repeat_2x_Clicked;
        public event EventHandler<EventArgs> Repeat_5x_Clicked;
        public event EventHandler<EventArgs> Repeat_10x_Clicked;
        public event EventHandler<EventArgs> Repeat_endless_Clicked;
        private AppBarButton MenuFlyoutButton;
        private MenuFlyoutItem VolumeMenuFlyoutItem;
        private Slider VolumeSlider2;
        private MenuFlyoutItem CastButton;
        private AppBarButton VolumeMuteButton;
        private StackPanel MediaControlsStackPanel;
        private StackPanel OptionsStackPanel;
        public bool NextButtonShouldBeVisible = false;
        public bool PreviousButtonShouldBeVisible = false;

        public CustomMediaTransportControls()
        {
            DefaultStyleKey = typeof(CustomMediaTransportControls);
        }

        protected override void OnApplyTemplate()
        {
            // This is where you would get your custom button and create an event handler for its click method.
            Button RemoveButton = GetTemplateChild("RemoveButton") as Button;
            RemoveButton.Click += RemoveButton_Click;

            MenuFlyoutButton = GetTemplateChild("MenuFlyoutButton") as AppBarButton;
            MenuFlyoutButton.Click += MenuFlyoutButton_Click;

            VolumeMuteButton = GetTemplateChild("VolumeMuteButton") as AppBarButton;

            VolumeMenuFlyoutItem = GetTemplateChild("VolumeMenuFlyoutItem") as MenuFlyoutItem;
            VolumeMenuFlyoutItem.Click += VolumeMenuFlyout_Click;

            Slider VolumeSlider = GetTemplateChild("VolumeSlider") as Slider;
            VolumeSlider.LostFocus += VolumeSlider_LostFocusEvent;

            VolumeSlider2 = GetTemplateChild("VolumeSlider2") as Slider;
            VolumeSlider2.LostFocus += VolumeSlider_LostFocusEvent;
            VolumeSlider2.ValueChanged += VolumeSlider2_ValueChanged;

            MenuFlyoutItem FavouriteFlyout = GetTemplateChild("FavouriteMenuFlyoutItem") as MenuFlyoutItem;
            FavouriteFlyout.Click += FavouriteFlyout_Click;

            CastButton = GetTemplateChild("CastMenuFlyoutItem") as MenuFlyoutItem;
            CastButton.Click += CastButton_Click;

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

            MediaControlsStackPanel = GetTemplateChild("MediaControlsStackPanel") as StackPanel;
            OptionsStackPanel = GetTemplateChild("OptionsStackPanel") as StackPanel;

            base.OnApplyTemplate();
        }

        public void SetVolumeButtonVisibility(bool visible)
        {
            VolumeMenuFlyoutItem.Visibility = visible ? Visibility.Collapsed : Visibility.Visible;
        }

        public void SetVolumeSliderValue(int value)
        {
            VolumeSlider2.Value = value;
        }

        private void VolumeMenuFlyout_Click(object sender, RoutedEventArgs e)
        {
            VolumeMenuFlyoutItem.ContextFlyout.ShowAt(MenuFlyoutButton);
        }

        public void AdjustLayout()
        {
            if (IsCompact)
            {
                IsNextTrackButtonVisible = NextButtonShouldBeVisible;
                IsPreviousTrackButtonVisible = PreviousButtonShouldBeVisible;

                // Reset the margins
                MediaControlsStackPanel.Margin = new Thickness(0);
                OptionsStackPanel.Margin = new Thickness(0);
                return;
            }
            var width = ActualWidth;

            if(NextButtonShouldBeVisible && PreviousButtonShouldBeVisible)
            {
                if (width < 192)
                {
                    // Hide both buttons
                    IsNextTrackButtonVisible = false;
                    IsPreviousTrackButtonVisible = false;
                }
                else if (width < 240)
                {
                    // Hide the next button and show the previous button
                    IsNextTrackButtonVisible = false;
                    IsPreviousTrackButtonVisible = true;
                }
                else
                {
                    // Show both buttons
                    IsNextTrackButtonVisible = true;
                    IsPreviousTrackButtonVisible = true;
                }
            }
            else if(NextButtonShouldBeVisible && !PreviousButtonShouldBeVisible)
            {
                if(width < 191)
                {
                    // Hide both buttons
                    IsNextTrackButtonVisible = false;
                    IsPreviousTrackButtonVisible = false;
                }
                else
                {
                    // Show the next button and hide the previous button
                    IsNextTrackButtonVisible = true;
                    IsPreviousTrackButtonVisible = false;
                }
            }
            else if(!NextButtonShouldBeVisible && PreviousButtonShouldBeVisible)
            {
                if(width < 191)
                {
                    // Hide both buttons
                    IsNextTrackButtonVisible = false;
                    IsPreviousTrackButtonVisible = false;
                }
                else
                {
                    // Show the previous button and hide the next button
                    IsNextTrackButtonVisible = false;
                    IsPreviousTrackButtonVisible = true;
                }
            }
            else
            {
                // Hide both buttons
                IsNextTrackButtonVisible = false;
                IsPreviousTrackButtonVisible = false;
            }

            // Calculate the width of the buttons; the ActualWidth property is not reliable
            double buttonWidth = 48;
            
            // Calculate the width of the MediaControlsStackPanel
            double mediaControlsStackPanelWidth = buttonWidth;      // The Play/Pause button is always visible
            if (IsNextTrackButtonVisible)
                mediaControlsStackPanelWidth += buttonWidth;
            if (IsPreviousTrackButtonVisible)
                mediaControlsStackPanelWidth += buttonWidth;

            // Calculate the width of the OptionsStackPanel
            double optionsStackPanelWidth = buttonWidth * 2;        // The options button and the remove button are both always visible

            // Set the margins
            if(VolumeMuteButton.Visibility == Visibility.Collapsed)
            {
                // Set only the margin of the Options
                // (Width of MediaControls) + (Margin) + (Width of OptionsStackPanel) = Width of base
                double margin = width - optionsStackPanelWidth - mediaControlsStackPanelWidth;
                MediaControlsStackPanel.Margin = new Thickness(0);
                OptionsStackPanel.Margin = new Thickness(margin, 0, 0, 0);
            }
            else
            {
                // Set the margin of the controls and the options
                // (Width of VolumeButton) + (Margin / 2) + (Width of MediaControlsStackPanel) + (Margin / 2) + (Width of OptionsStackPanel) = Width of base
                double margin = width - buttonWidth - mediaControlsStackPanelWidth - optionsStackPanelWidth;
                MediaControlsStackPanel.Margin = new Thickness(margin / 2, 0, 0, 0);
                OptionsStackPanel.Margin = new Thickness(margin / 2, 0, 0, 0);
            }
        }

        // Raise custom events
        private void VolumeSlider_LostFocusEvent(object sender, RoutedEventArgs e) => VolumeSlider_LostFocus?.Invoke(this, EventArgs.Empty);

        private void VolumeSlider2_ValueChanged(object sender, RangeBaseValueChangedEventArgs e) => VolumeSlider_ValueChanged?.Invoke(sender, e);

        private void RemoveButton_Click(object sender, RoutedEventArgs e) => RemoveButton_Clicked?.Invoke(this, EventArgs.Empty);

        private void FavouriteFlyout_Click(object sender, RoutedEventArgs e) => FavouriteFlyout_Clicked?.Invoke(this, EventArgs.Empty);

        private void CastButton_Click(object sender, RoutedEventArgs e) => CastButton_Clicked?.Invoke(CastButton, EventArgs.Empty);

        private void MenuFlyoutButton_Click(object sender, RoutedEventArgs e) => MenuFlyoutButton_Clicked?.Invoke(this, EventArgs.Empty);

        private void Repeat_1x_Click(object sender, RoutedEventArgs e) => Repeat_1x_Clicked?.Invoke(this, EventArgs.Empty);

        private void Repeat_2x_Click(object sender, RoutedEventArgs e) => Repeat_2x_Clicked?.Invoke(this, EventArgs.Empty);

        private void Repeat_5x_Click(object sender, RoutedEventArgs e) => Repeat_5x_Clicked?.Invoke(this, EventArgs.Empty);

        private void Repeat_10x_Click(object sender, RoutedEventArgs e) => Repeat_10x_Clicked?.Invoke(this, EventArgs.Empty);

        private void Repeat_endless_Click(object sender, RoutedEventArgs e) => Repeat_endless_Clicked?.Invoke(this, EventArgs.Empty);
    }
}
