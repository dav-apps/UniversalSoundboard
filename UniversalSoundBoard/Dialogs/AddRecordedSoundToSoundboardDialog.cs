using UniversalSoundboard.DataAccess;
using Windows.UI.Xaml.Controls;

namespace UniversalSoundboard.Dialogs
{
    public class AddRecordedSoundToSoundboardDialog : Dialog
    {
        private TextBox RecordedSoundNameTextBox;

        public string Name
        {
            get => RecordedSoundNameTextBox?.Text;
        }

        public AddRecordedSoundToSoundboardDialog(string recordedSoundName)
            : base(
                  FileManager.loader.GetString("AddRecordedSoundToSoundboardDialog-Title"),
                  FileManager.loader.GetString("Actions-Add"),
                  FileManager.loader.GetString("Actions-Cancel")
            )
        {
            Content = GetContent(recordedSoundName);
        }

        private StackPanel GetContent(string recordedSoundName)
        {
            StackPanel rootStackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical
            };

            RecordedSoundNameTextBox = new TextBox
            {
                Text = recordedSoundName,
                PlaceholderText = FileManager.loader.GetString("RenameSoundDialog-RenameSoundTextBoxPlaceholder"),
                Width = 300
            };
            RecordedSoundNameTextBox.TextChanged += RecordedSoundNameTextBox_TextChanged;

            rootStackPanel.Children.Add(RecordedSoundNameTextBox);
            return rootStackPanel;
        }

        private void RecordedSoundNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ContentDialog.IsPrimaryButtonEnabled = RecordedSoundNameTextBox.Text.Length >= 3;
        }
    }
}
