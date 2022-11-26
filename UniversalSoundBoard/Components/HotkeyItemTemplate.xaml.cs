using UniversalSoundboard.Common;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Models;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace UniversalSoundboard.Components
{
    public sealed partial class HotkeyItemTemplate : UserControl
    {
        HotkeyItem HotkeyItem { get; set; }

        private string text
        {
            get => HotkeyItem?.Text ?? "";
        }
        private SolidColorBrush background = new SolidColorBrush();
        private SolidColorBrush borderBrush = new SolidColorBrush();

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
                background = new SolidColorBrush(Color.FromArgb(13, 255, 255, 255));
                borderBrush = new SolidColorBrush(Color.FromArgb(25, 0, 0, 0));
            }
            else
            {
                background = new SolidColorBrush(Color.FromArgb(15, 0, 0, 0));
                borderBrush = new SolidColorBrush(Color.FromArgb(15, 0, 0, 0));
            }

            Bindings.Update();
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            HotkeyItem.Remove();
        }
    }
}
