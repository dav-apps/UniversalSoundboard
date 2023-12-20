using System;
using UniversalSoundboard.Common;
using UniversalSoundboard.DataAccess;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace UniversalSoundboard.Dialogs
{
    public class ExportSoundboardDialog : Dialog
    {
        private TextBox ExportFolderTextBox;
        public StorageFolder ExportFolder { get; private set; }

        public ExportSoundboardDialog()
            : base(
                  FileManager.loader.GetString("ExportSoundboardDialog-Title"),
                  FileManager.loader.GetString("Export"),
                  FileManager.loader.GetString("Actions-Cancel")
            )
        {
            ContentDialog.IsPrimaryButtonEnabled = false;
            Content = GetContent();
        }

        private StackPanel GetContent()
        {
            StackPanel content = new StackPanel
            {
                Orientation = Orientation.Vertical
            };

            TextBlock contentText = new TextBlock
            {
                Margin = new Thickness(0, 0, 0, 20),
                TextWrapping = TextWrapping.WrapWholeWords,
                Text = FileManager.loader.GetString("ExportSoundboardDialog-Text1")
            };

            // Create StackPanel with TextBox and Folder button
            StackPanel folderStackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };

            ExportFolderTextBox = new TextBox
            {
                IsReadOnly = true
            };

            Button folderButton = new Button
            {
                FontFamily = new FontFamily(Constants.FluentIconsFontFamily),
                Content = "\uE838",
                FontSize = 18,
                Width = 35,
                Height = 35,
                Padding = new Thickness(0)
            };
            folderButton.Tapped += ExportFolderButton_Tapped;

            folderStackPanel.Children.Add(folderButton);
            folderStackPanel.Children.Add(ExportFolderTextBox);

            TextBlock contentText2 = new TextBlock
            {
                Margin = new Thickness(0, 20, 0, 0),
                TextWrapping = TextWrapping.WrapWholeWords,
                Text = FileManager.loader.GetString("ExportSoundboardDialog-Text2")
            };

            content.Children.Add(contentText);
            content.Children.Add(folderStackPanel);
            content.Children.Add(contentText2);

            return content;
        }

        private async void ExportFolderButton_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
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
                ExportFolder = folder;
                ExportFolderTextBox.Text = folder.Path;
                ContentDialog.IsPrimaryButtonEnabled = true;
            }
        }
    }
}
