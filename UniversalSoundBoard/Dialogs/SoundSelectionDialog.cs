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
        private ObservableCollection<DialogSoundListItem> SoundItems;

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

            foreach (var sound in sounds)
                SoundItems.Add(new DialogSoundListItem(sound));

            Content = GetContent(itemTemplate);
            ContentDialog.IsPrimaryButtonEnabled = false;
        }

        private StackPanel GetContent(DataTemplate itemTemplate)
        {
            StackPanel contentStackPanel = new StackPanel();

            SoundsListView = new ListView
            {
                ItemTemplate = itemTemplate,
                ItemsSource = SoundItems,
                SelectionMode = ListViewSelectionMode.Single,
                Height = 250,
                CanReorderItems = true,
                AllowDrop = true
            };

            contentStackPanel.Children.Add(SoundsListView);

            return contentStackPanel;
        }
    }
}
