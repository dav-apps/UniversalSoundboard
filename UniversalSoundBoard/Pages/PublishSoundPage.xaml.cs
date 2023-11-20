using CommunityToolkit.WinUI.Collections;
using System.Collections.ObjectModel;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Models;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace UniversalSoundboard.Pages
{
    public sealed partial class PublishSoundPage : Page
    {
        private ObservableCollection<DialogSoundListItem> SoundItems;
        private AdvancedCollectionView SoundsCollectionView;

        public PublishSoundPage()
        {
            InitializeComponent();

            SoundItems = new ObservableCollection<DialogSoundListItem>();
            SoundsCollectionView = new AdvancedCollectionView(SoundItems, true);

            foreach (var sound in FileManager.itemViewHolder.AllSounds)
                SoundItems.Add(new DialogSoundListItem(sound));
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            SetThemeColors();
        }

        private void FilterAutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            SoundsCollectionView.Filter = item => ((DialogSoundListItem)item).Sound.Name.ToLower().Contains(sender.Text.ToLower());
        }

        private void SetThemeColors()
        {
            RequestedTheme = FileManager.GetRequestedTheme();
            SolidColorBrush appThemeColorBrush = new SolidColorBrush(FileManager.GetApplicationThemeColor());
            ContentRoot.Background = appThemeColorBrush;
        }
    }
}
