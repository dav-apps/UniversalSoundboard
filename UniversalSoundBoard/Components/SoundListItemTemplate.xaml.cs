using UniversalSoundBoard.Common;
using UniversalSoundBoard.DataAccess;
using UniversalSoundBoard.Models;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

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
            FileManager.itemViewHolder.PropertyChanged += ItemViewHolder_PropertyChanged;
        }

        private void SoundListItemTemplate_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            soundItem = new SoundItem(Sound);
            soundItem.ImageUpdated += SoundItem_ImageUpdated;
            Bindings.Update();
        }

        private void ItemViewHolder_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals(ItemViewHolder.CurrentThemeKey))
            {
                RequestedTheme = FileManager.GetRequestedTheme();

                // Set the appropriate default image
                if (Sound != null && !Sound.HasImageFile())
                {
                    Sound.Image = new BitmapImage { UriSource = Sound.GetDefaultImageUri() };
                    Bindings.Update();
                }
            }
        }

        private void SoundItem_ImageUpdated(object sender, System.EventArgs e)
        {
            Sound.Image = new BitmapImage { UriSource = Sound.GetDefaultImageUri() };
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
