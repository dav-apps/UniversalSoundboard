using UniversalSoundboard.Common;
using UniversalSoundboard.DataAccess;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace UniversalSoundboard.Components
{
    public sealed partial class HotkeyItemTemplate : UserControl
    {
        HotkeyItem HotkeyItem { get; set; }

        private string Text
        {
            get => HotkeyItem?.Text ?? "";
        }
        private SolidColorBrush Background = new SolidColorBrush();
        private SolidColorBrush BorderBrush = new SolidColorBrush();

        public HotkeyItemTemplate()
        {
            InitializeComponent();
            DataContextChanged += HotkeyItemTemplate_DataContextChanged;
        }

        private void HotkeyItemTemplate_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (DataContext == null) return;

            HotkeyItem = DataContext as HotkeyItem;
            Bindings.Update();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (FileManager.itemViewHolder.CurrentTheme == AppTheme.Dark)
            {
                Background = new SolidColorBrush(Color.FromArgb(13, 255, 255, 255));
                BorderBrush = new SolidColorBrush(Color.FromArgb(25, 0, 0, 0));
            }
            else
            {
                Background = new SolidColorBrush(Color.FromArgb(15, 0, 0, 0));
                BorderBrush = new SolidColorBrush(Color.FromArgb(15, 0, 0, 0));
            }

            Bindings.Update();
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            HotkeyItem.Remove();
        }
    }
}
