using System;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.UI.StartScreen;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UniversalSoundboard.Components
{
    public class SoundItemManager
    {

    }

    public class SoundItemOptionsFlyout
    {
        private readonly ResourceLoader loader;
        private Guid soundUuid;

        private MenuFlyout optionsFlyout;
        private MenuFlyoutItem setFavouriteFlyoutItem;
        private MenuFlyoutItem pinFlyoutItem;

        public event EventHandler<object> FlyoutOpened;
        public event RoutedEventHandler SetCategoryFlyoutItemClick;
        public event RoutedEventHandler SetFavouriteFlyoutItemClick;
        public event RoutedEventHandler ShareFlyoutItemClick;
        public event RoutedEventHandler ExportFlyoutItemClick;
        public event RoutedEventHandler PinFlyoutItemClick;
        public event RoutedEventHandler SetImageFlyoutItemClick;
        public event RoutedEventHandler RenameFlyoutItemClick;
        public event RoutedEventHandler DeleteFlyoutItemClick;

        public SoundItemOptionsFlyout(Guid soundUuid, bool favourite) {
            loader = new ResourceLoader();
            this.soundUuid = soundUuid;

            // Create the flyout
            optionsFlyout = new MenuFlyout();
            optionsFlyout.Opened += (object sender, object e) => FlyoutOpened.Invoke(sender, e);

            // Set categories
            MenuFlyoutItem setCategoriesFlyoutItem = new MenuFlyoutItem { Text = loader.GetString("SoundItemOptionsFlyout-SetCategories") };
            setCategoriesFlyoutItem.Click += (object sender, RoutedEventArgs e) => SetCategoryFlyoutItemClick.Invoke(sender, e);
            optionsFlyout.Items.Add(setCategoriesFlyoutItem);

            // Set favourite
            setFavouriteFlyoutItem = new MenuFlyoutItem { Text = loader.GetString(favourite ? "SoundItemOptionsFlyout-UnsetFavourite" : "SoundItemOptionsFlyout-SetFavourite") };
            setFavouriteFlyoutItem.Click += (object sender, RoutedEventArgs e) => SetFavouriteFlyoutItemClick.Invoke(sender, e);
            optionsFlyout.Items.Add(setFavouriteFlyoutItem);

            // Share
            MenuFlyoutItem shareFlyoutItem = new MenuFlyoutItem { Text = loader.GetString("Share") };
            shareFlyoutItem.Click += (object sender, RoutedEventArgs e) => ShareFlyoutItemClick.Invoke(sender, e);
            optionsFlyout.Items.Add(shareFlyoutItem);

            // Export
            MenuFlyoutItem exportFlyoutItem = new MenuFlyoutItem { Text = loader.GetString("Export") };
            exportFlyoutItem.Click += (object sender, RoutedEventArgs e) => ExportFlyoutItemClick.Invoke(sender, e);
            optionsFlyout.Items.Add(exportFlyoutItem);

            // Pin
            pinFlyoutItem = new MenuFlyoutItem { Text = loader.GetString(SecondaryTile.Exists(soundUuid.ToString()) ? "Unpin" : "Pin") };
            pinFlyoutItem.Click += (object sender, RoutedEventArgs e) => PinFlyoutItemClick.Invoke(sender, e);
            optionsFlyout.Items.Add(pinFlyoutItem);

            // Separator
            optionsFlyout.Items.Add(new MenuFlyoutSeparator());

            // Set image
            MenuFlyoutItem setImageFlyout = new MenuFlyoutItem { Text = loader.GetString("SoundItemOptionsFlyout-SetImage") };
            setImageFlyout.Click += (object sender, RoutedEventArgs e) => SetImageFlyoutItemClick.Invoke(sender, e);
            optionsFlyout.Items.Add(setImageFlyout);

            // Rename
            MenuFlyoutItem renameFlyout = new MenuFlyoutItem { Text = loader.GetString("SoundItemOptionsFlyout-Rename") };
            renameFlyout.Click += (object sender, RoutedEventArgs e) => RenameFlyoutItemClick.Invoke(sender, e);
            optionsFlyout.Items.Add(renameFlyout);

            // Delete
            MenuFlyoutItem deleteFlyout = new MenuFlyoutItem { Text = loader.GetString("SoundItemOptionsFlyout-Delete") };
            deleteFlyout.Click += (object sender, RoutedEventArgs e) => DeleteFlyoutItemClick.Invoke(sender, e);
            optionsFlyout.Items.Add(deleteFlyout);
        }

        public void SetFavourite(bool favourite)
        {
            setFavouriteFlyoutItem.Text = loader.GetString(favourite ? "SoundItemOptionsFlyout-UnsetFavourite" : "SoundItemOptionsFlyout-SetFavourite");
        }

        public void UpdatePinText()
        {
            pinFlyoutItem.Text = loader.GetString(SecondaryTile.Exists(soundUuid.ToString()) ? "Unpin" : "Pin");
        }

        public void ShowAt(UIElement sender, Point position)
        {
            optionsFlyout.ShowAt(sender, position);
        }
    }
}
