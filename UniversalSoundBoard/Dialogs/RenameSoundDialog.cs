using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Models;
using Windows.UI.Xaml.Controls;

namespace UniversalSoundboard.Dialogs
{
    public class RenameSoundDialog : Dialog
    {
        private TextBox RenameSoundTextBox;

        public string SoundName
        {
            get => RenameSoundTextBox?.Text;
        }

        public RenameSoundDialog(Sound sound)
            : base(
                  FileManager.loader.GetString("RenameSoundDialog-Title"),
                  FileManager.loader.GetString("RenameSoundDialog-PrimaryButton"),
                  FileManager.loader.GetString("Actions-Cancel")
            )
        {
            Content = GetContent(sound);
        }

        private StackPanel GetContent(Sound sound)
        {
            StackPanel stackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical
            };

            RenameSoundTextBox = new TextBox
            {
                Text = sound.Name,
                PlaceholderText = FileManager.loader.GetString("RenameSoundDialog-RenameSoundTextBoxPlaceholder"),
                Width = 300
            };

            stackPanel.Children.Add(RenameSoundTextBox);

            ContentDialog.Content = stackPanel;
            RenameSoundTextBox.TextChanged += RenameSoundTextBox_TextChanged;

            return stackPanel;
        }

        private void RenameSoundTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ContentDialog.IsPrimaryButtonEnabled = RenameSoundTextBox.Text.Length >= 3;
        }
    }
}
