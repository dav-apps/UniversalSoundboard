using UniversalSoundboard.DataAccess;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UniversalSoundboard.Dialogs
{
    public class ReportSoundDialog : Dialog
    {
        private string description = "";
        public string Description { get => description; }

        public ReportSoundDialog()
            : base(
                FileManager.loader.GetString("ReportSoundDialog-Title"),
                FileManager.loader.GetString("Actions-Send"),
                FileManager.loader.GetString("Actions-Cancel")
            )
        {
            Content = GetContent();
            ContentDialog.IsPrimaryButtonEnabled = false;
        }

        private StackPanel GetContent()
        {
            StackPanel contentStackPanel = new StackPanel();

            TextBlock descriptionTextBlock = new TextBlock
            {
                Text = FileManager.loader.GetString("ReportSoundDialog-Description"),
                TextWrapping = TextWrapping.WrapWholeWords
            };

            RichEditBox descriptionBox = new RichEditBox
            {
                PlaceholderText = FileManager.loader.GetString("ReportSoundDialog-DescriptionBox-Placeholder"),
                Margin = new Thickness(0, 12, 0, 0),
                MaxHeight = 150
            };

            descriptionBox.TextChanged += DescriptionBox_TextChanged;

            contentStackPanel.Children.Add(descriptionTextBlock);
            contentStackPanel.Children.Add(descriptionBox);

            return contentStackPanel;
        }

        private void DescriptionBox_TextChanged(object sender, RoutedEventArgs e)
        {
            var descriptionBox = sender as RichEditBox;
            descriptionBox.Document.GetText(TextGetOptions.NoHidden, out description);

            ContentDialog.IsPrimaryButtonEnabled = description.Length > 4;
        }
    }
}
