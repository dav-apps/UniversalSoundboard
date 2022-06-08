using System.ComponentModel;
using UniversalSoundboard.Common;
using UniversalSoundboard.DataAccess;
using Windows.UI.Xaml.Controls;

namespace UniversalSoundboard.Pages
{
    public sealed partial class SoundRecorderPage : Page
    {
        public SoundRecorderPage()
        {
            InitializeComponent();

            RootGrid.DataContext = FileManager.itemViewHolder;

            FileManager.itemViewHolder.PropertyChanged += ItemViewHolder_PropertyChanged;
        }

        private void ItemViewHolder_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case ItemViewHolder.CurrentThemeKey:
                    RequestedTheme = FileManager.GetRequestedTheme();
                    break;
            }
        }
    }
}
