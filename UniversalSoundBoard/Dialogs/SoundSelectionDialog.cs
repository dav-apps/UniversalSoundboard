using CommunityToolkit.WinUI.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Models;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UniversalSoundboard.Dialogs
{
    public class SoundSelectionDialog : Dialog
    {
        private ListView SoundsListView;
        private AdvancedCollectionView SoundsCollectionView;
        private ObservableCollection<DialogSoundListItem> SoundItems;
        public DialogSoundListItem SelectedSoundItem = null;

        public SoundSelectionDialog(
            List<Sound> sounds,
            DataTemplate itemTemplate
        ) : base(
                  FileManager.loader.GetString("SoundSelectionDialog-Title"),
                  FileManager.loader.GetString("Actions-Select"),
                  FileManager.loader.GetString("Actions-Cancel")
            )
        {
            SoundItems = new ObservableCollection<DialogSoundListItem>();
            SoundsCollectionView = new AdvancedCollectionView(SoundItems, true);

            foreach (var sound in sounds)
                SoundItems.Add(new DialogSoundListItem(sound));

            Content = GetContent(itemTemplate);
            ContentDialog.IsPrimaryButtonEnabled = false;
        }

        private StackPanel GetContent(DataTemplate itemTemplate)
        {
            StackPanel contentStackPanel = new StackPanel();

            AutoSuggestBox filterAutoSuggestBox = new AutoSuggestBox
            {
                PlaceholderText = FileManager.loader.GetString("SoundSelectionDialog-FilterAutoSuggestBox-Placeholder"),
                QueryIcon = new SymbolIcon(Symbol.Find)
            };

            filterAutoSuggestBox.TextChanged += FilterAutoSuggestBox_TextChanged;

            SoundsListView = new ListView
            {
                ItemTemplate = itemTemplate,
                ItemsSource = SoundsCollectionView,
                SelectionMode = ListViewSelectionMode.Single,
                Height = 250,
                Width = 400,
                Margin = new Thickness(0, 12, 0, 0)
            };

            SoundsListView.SelectionChanged += SoundsListView_SelectionChanged;

            contentStackPanel.Children.Add(filterAutoSuggestBox);
            contentStackPanel.Children.Add(SoundsListView);

            return contentStackPanel;
        }

        private void FilterAutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            SoundsCollectionView.Filter = item => ((DialogSoundListItem)item).Sound.Name.ToLower().Contains(sender.Text.ToLower());
        }

        private void SoundsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedSoundItem = SoundsListView.SelectedItem as DialogSoundListItem;
            ContentDialog.IsPrimaryButtonEnabled = true;
        }
    }
}
