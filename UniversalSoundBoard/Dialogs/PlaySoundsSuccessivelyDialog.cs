using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Models;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UniversalSoundboard.Dialogs
{
    public class PlaySoundsSuccessivelyDialog : Dialog
    {
        private ListView SoundsListView;
        private CheckBox RandomCheckBox;
        public ComboBox RepetitionsComboBox { get; private set; }
        private ObservableCollection<DialogSoundListItem> SoundItems;
        public bool Random { get => (bool)RandomCheckBox?.IsChecked; }
        public List<Sound> Sounds
        {
            get
            {
                List<Sound> sounds = new List<Sound>();

                foreach (var soundItem in SoundItems)
                    sounds.Add(soundItem.Sound);

                return sounds;
            }
        }

        public PlaySoundsSuccessivelyDialog(
            List<Sound> sounds,
            DataTemplate itemTemplate,
            Style listViewItemStyle
        ) : base(
                  FileManager.loader.GetString("PlaySoundsSuccessivelyDialog-Title"),
                  FileManager.loader.GetString("PlaySoundsSuccessivelyDialog-PrimaryButton"),
                  FileManager.loader.GetString("Actions-Cancel")
            )
        {
            SoundItems = new ObservableCollection<DialogSoundListItem>();

            foreach (var sound in sounds)
            {
                var soundItem = new DialogSoundListItem(sound);
                soundItem.RemoveButtonClick += SoundItem_RemoveButtonClick;
                SoundItems.Add(soundItem);
            }

            ContentDialog.IsPrimaryButtonEnabled = SoundItems.Count > 0;
            Content = GetContent(itemTemplate, listViewItemStyle);
        }

        private StackPanel GetContent(DataTemplate itemTemplate, Style listViewItemStyle)
        {
            StackPanel content = new StackPanel
            {
                Orientation = Orientation.Vertical
            };

            SoundsListView = new ListView
            {
                ItemTemplate = itemTemplate,
                ItemsSource = SoundItems,
                SelectionMode = ListViewSelectionMode.None,
                Height = 300,
                ItemContainerStyle = listViewItemStyle,
                CanReorderItems = true,
                AllowDrop = true
            };

            StackPanel repetitionsStackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(0, 10, 0, 0)
            };

            RepetitionsComboBox = new ComboBox
            {
                Margin = new Thickness(0, 10, 0, 0),
                IsEditable = true,
                Items =
                {
                    "0",
                    "1",
                    "2",
                    "3",
                    "4",
                    "5",
                    "6",
                    "7",
                    "8",
                    "9",
                    "10",
                    "15",
                    "20",
                    "25",
                    "30",
                    "40",
                    "50",
                    "100",
                    "∞"
                },
                SelectedIndex = 0
            };
            RepetitionsComboBox.TextSubmitted += RepetitionsComboBox_TextSubmitted;

            TextBlock repetitionsTextBlock = new TextBlock
            {
                Text = FileManager.loader.GetString("PlaySoundsSuccessivelyDialog-Repetitions")
            };

            repetitionsStackPanel.Children.Add(repetitionsTextBlock);
            repetitionsStackPanel.Children.Add(RepetitionsComboBox);

            RandomCheckBox = new CheckBox
            {
                Content = FileManager.loader.GetString("Shuffle"),
                Margin = new Thickness(0, 10, 0, 0)
            };

            content.Children.Add(SoundsListView);
            content.Children.Add(repetitionsStackPanel);
            content.Children.Add(RandomCheckBox);

            return content;
        }

        private void RepetitionsComboBox_TextSubmitted(ComboBox sender, ComboBoxTextSubmittedEventArgs args)
        {
            if (args.Text == "∞") return;
            if (!int.TryParse(args.Text, out int value) || value <= 0)
                RepetitionsComboBox.Text = "0";
        }

        private void SoundItem_RemoveButtonClick(object sender, EventArgs e)
        {
            var soundItem = sender as DialogSoundListItem;

            // Find and remove the selected item
            var selectedSoundItem = SoundItems.ToList().Find(item => item.Sound.Uuid == soundItem.Sound.Uuid);
            if (selectedSoundItem == null) return;

            SoundItems.Remove(selectedSoundItem);

            if (SoundItems.Count == 0)
                ContentDialog.IsPrimaryButtonEnabled = false;
        }
    }
}
