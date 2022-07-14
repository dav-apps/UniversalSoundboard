using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UniversalSoundboard.Components;
using UniversalSoundboard.DataAccess;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UniversalSoundboard.Dialogs
{
    public class AddSoundsDialog : Dialog
    {
        private ObservableCollection<SoundFileItem> SelectedFileItems;
        private TextBlock NoFilesSelectedTextBlock;
        private ListView AddSoundsListView;

        public List<StorageFile> SelectedFiles
        {
            get
            {
                List<StorageFile> files = new List<StorageFile>();

                foreach (var fileItem in SelectedFileItems)
                    files.Add(fileItem.File);

                return files;
            }
        }

        public AddSoundsDialog(
            DataTemplate itemTemplate,
            List<StorageFile> selectedFiles
        ) : base(
                  FileManager.loader.GetString("AddSoundsDialog-Title"),
                  FileManager.loader.GetString("AddSoundsDialog-PrimaryButton"),
                  FileManager.loader.GetString("Actions-Cancel")
            )
        {
            SelectedFileItems = new ObservableCollection<SoundFileItem>();

            foreach (StorageFile file in selectedFiles)
            {
                SoundFileItem item = new SoundFileItem(file);
                item.Removed += SoundFileItem_Removed;
                SelectedFileItems.Add(item);
            }

            ContentDialog.IsPrimaryButtonEnabled = SelectedFileItems.Count > 0;
            Content = GetContent(itemTemplate);
        }

        private StackPanel GetContent(DataTemplate itemTemplate)
        {
            StackPanel containerStackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical
            };

            Button selectFilesButton = new Button
            {
                Content = FileManager.loader.GetString("AddSoundsDialog-SelectFiles"),
                Margin = new Thickness(0, 10, 0, 10),
            };
            selectFilesButton.Click += SelectFilesButton_Click;

            NoFilesSelectedTextBlock = new TextBlock
            {
                Text = FileManager.loader.GetString("AddSoundsDialog-NoFilesSelected"),
                Margin = new Thickness(0, 25, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                Visibility = SelectedFileItems.Count > 0 ? Visibility.Collapsed : Visibility.Visible
            };

            AddSoundsListView = new ListView
            {
                ItemTemplate = itemTemplate,
                ItemsSource = SelectedFileItems,
                SelectionMode = ListViewSelectionMode.None,
                Height = 250,
                CanReorderItems = true,
                AllowDrop = true
            };

            containerStackPanel.Children.Add(selectFilesButton);
            containerStackPanel.Children.Add(NoFilesSelectedTextBlock);
            containerStackPanel.Children.Add(AddSoundsListView);

            return containerStackPanel;
        }

        private async void SelectFilesButton_Click(object sender, RoutedEventArgs e)
        {
            // Open file explorer
            var picker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.List,
                SuggestedStartLocation = PickerLocationId.MusicLibrary
            };

            foreach (var fileType in FileManager.allowedFileTypes)
                picker.FileTypeFilter.Add(fileType);

            var files = await picker.PickMultipleFilesAsync();

            foreach (var file in files)
            {
                SoundFileItem item = new SoundFileItem(file);
                item.Removed += SoundFileItem_Removed;
                SelectedFileItems.Add(item);
            }

            ContentDialog.IsPrimaryButtonEnabled = SelectedFileItems.Count > 0;
            NoFilesSelectedTextBlock.Visibility = SelectedFileItems.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        private void SoundFileItem_Removed(object sender, EventArgs e)
        {
            SelectedFileItems.Remove((SoundFileItem)sender);
            ContentDialog.IsPrimaryButtonEnabled = SelectedFileItems.Count > 0;
            NoFilesSelectedTextBlock.Visibility = SelectedFileItems.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
        }
    }
}
