using System;
using UniversalSoundboard.Common;
using UniversalSoundboard.DataAccess;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace UniversalSoundboard.Dialogs
{
    public class ImportSoundboardDialog : Dialog
    {
        private TextBox ImportFolderTextBox;
        public StorageFile ImportFile { get; private set; }

        public ImportSoundboardDialog(bool startMessage = false)
            : base(
                  FileManager.loader.GetString("ImportSoundboardDialog-Title"),
                  FileManager.loader.GetString("Import"),
                  FileManager.loader.GetString("Actions-Cancel")
            )
        {
            ContentDialog.IsPrimaryButtonEnabled = false;
            Content = GetContent(startMessage);
        }

        public StackPanel GetContent(bool startMessage)
        {
            StackPanel content = new StackPanel
            {
                Orientation = Orientation.Vertical
            };

            TextBlock contentText = new TextBlock
            {
                Margin = new Thickness(0, 0, 0, 20),
                TextWrapping = TextWrapping.WrapWholeWords,
                Text = FileManager.loader.GetString("ImportSoundboardDialog-Text1")
            };

            // Create StackPanel with TextBox and Folder button
            StackPanel folderStackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };

            ImportFolderTextBox = new TextBox
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
            folderButton.Tapped += ImportFolderButton_Tapped;

            folderStackPanel.Children.Add(folderButton);
            folderStackPanel.Children.Add(ImportFolderTextBox);

            TextBlock contentText2 = new TextBlock
            {
                Margin = new Thickness(0, 20, 0, 0),
                TextWrapping = TextWrapping.WrapWholeWords,
                Text = FileManager.loader.GetString(startMessage ? "ImportSoundboardDialog-StartMessage-Text2" : "ImportSoundboardDialog-Text2")
            };

            content.Children.Add(contentText);
            content.Children.Add(folderStackPanel);
            content.Children.Add(contentText2);

            return content;
        }

        private async void ImportFolderButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var picker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.ComputerFolder
            };
            picker.FileTypeFilter.Add(".zip");

            StorageFile file = await picker.PickSingleFileAsync();

            if (file != null)
            {
                // Set TextBox text and StorageFile variable and make primary button clickable
                ImportFile = file;
                ImportFolderTextBox.Text = file.Path;
                ContentDialog.IsPrimaryButtonEnabled = true;
            }
        }
    }
}
