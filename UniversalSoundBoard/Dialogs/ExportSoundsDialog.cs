using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Models;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace UniversalSoundboard.Dialogs
{
    public class ExportSoundsDialog : Dialog
    {
        private ListView ExportSoundsListView;
        private TextBox ExportSoundsFolderTextBox;
        public StorageFolder ExportSoundsFolder { get; private set; }
        private CheckBox ExportSoundsAsZipCheckBox;
        private ObservableCollection<DialogSoundListItem> SoundItems;
        public bool ExportSoundsAsZip { get => (bool)ExportSoundsAsZipCheckBox?.IsChecked; }
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

        public ExportSoundsDialog(
            List<Sound> sounds,
            DataTemplate itemTemplate,
            Style listViewItemStyle
        ) : base(
                  FileManager.loader.GetString("ExportSoundsDialog-Title"),
                  FileManager.loader.GetString("Export"),
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

            ContentDialog.IsPrimaryButtonEnabled = false;
            Content = GetContent(itemTemplate, listViewItemStyle);
        }

        private StackPanel GetContent(DataTemplate itemTemplate, Style listViewItemStyle)
        {
            StackPanel content = new StackPanel
            {
                Orientation = Orientation.Vertical
            };

            ExportSoundsListView = new ListView
            {
                ItemTemplate = itemTemplate,
                ItemsSource = SoundItems,
                SelectionMode = ListViewSelectionMode.None,
                Height = 300,
                ItemContainerStyle = listViewItemStyle,
                CanReorderItems = true,
                AllowDrop = true
            };

            // Create StackPanel with TextBox and Folder button
            StackPanel folderStackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 20, 0, 0)
            };

            ExportSoundsFolderTextBox = new TextBox
            {
                IsReadOnly = true
            };

            Button folderButton = new Button
            {
                FontFamily = new FontFamily(FileManager.FluentIconsFontFamily),
                Content = "\uE838",
                FontSize = 18,
                Width = 35,
                Height = 35,
                Padding = new Thickness(0)
            };
            folderButton.Tapped += ExportSoundsFolderButton_Tapped;

            ExportSoundsAsZipCheckBox = new CheckBox
            {
                Content = FileManager.loader.GetString("SaveAsZip"),
                Margin = new Thickness(0, 20, 0, 0)
            };

            folderStackPanel.Children.Add(folderButton);
            folderStackPanel.Children.Add(ExportSoundsFolderTextBox);

            content.Children.Add(ExportSoundsListView);
            content.Children.Add(folderStackPanel);
            content.Children.Add(ExportSoundsAsZipCheckBox);

            return content;
        }

        private async void ExportSoundsFolderButton_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            var folderPicker = new FolderPicker
            {
                SuggestedStartLocation = PickerLocationId.Downloads
            };

            folderPicker.FileTypeFilter.Add("*");
            StorageFolder folder = await folderPicker.PickSingleFolderAsync();

            if (folder != null)
            {
                StorageApplicationPermissions.FutureAccessList.AddOrReplace("PickedFolderToken", folder);

                // Set TextBox text and StorageFolder variable and make primary button clickable
                ExportSoundsFolder = folder;
                ExportSoundsFolderTextBox.Text = folder.Path;
                if (SoundItems.Count > 0)
                    ContentDialog.IsPrimaryButtonEnabled = true;
            }
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
