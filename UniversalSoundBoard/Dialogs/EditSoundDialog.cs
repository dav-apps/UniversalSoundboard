using CommunityToolkit.WinUI.Controls;
using System.Collections.ObjectModel;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Models;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UniversalSoundboard.Dialogs
{
    public class EditSoundDialog : Dialog
    {
        private ObservableCollection<string> Tags;
        private ObservableCollection<string> SelectedTags;

        public EditSoundDialog(SoundResponse sound, DataTemplate itemTemplate)
            : base(
                  FileManager.loader.GetString("EditSoundDialog-Title"),
                  FileManager.loader.GetString("Actions-Save"),
                  FileManager.loader.GetString("Actions-Cancel")
                )
        {
            Tags = new ObservableCollection<string>();
            SelectedTags = new ObservableCollection<string>();

            foreach (var tag in FileManager.itemViewHolder.Tags)
                Tags.Add(tag);

            foreach (var tag in sound.Tags)
                SelectedTags.Add(tag);

            Content = GetContent(sound, itemTemplate);
        }

        private StackPanel GetContent(SoundResponse sound, DataTemplate itemTemplate)
        {
            StackPanel contentStackPanel = new StackPanel();

            TextBox NameTextBox = new TextBox
            {
                Text = sound.Name,
                Header = FileManager.loader.GetString("EditSoundDialog-NameHeader"),
                PlaceholderText = FileManager.loader.GetString("EditSoundDialog-NamePlaceholder"),
                Width = 300
            };

            RichEditBox DescriptionRichEditBox = new RichEditBox
            {
                Header = FileManager.loader.GetString("EditSoundDialog-DescriptionHeader"),
                PlaceholderText = FileManager.loader.GetString("EditSoundDialog-DescriptionPlaceholder"),
                Margin = new Thickness(0, 16, 0, 0),
                Width = 300
            };

            DescriptionRichEditBox.Document.SetText(TextSetOptions.None, sound.Description);

            var tagsTokenBox = new TokenizingTextBox
            {
                Header = FileManager.loader.GetString("EditSoundDialog-TagsHeader"),
                PlaceholderText = FileManager.loader.GetString("EditSoundDialog-TagsPlaceholder"),
                SuggestedItemTemplate = itemTemplate,
                TokenItemTemplate = itemTemplate,
                ItemsSource = SelectedTags,
                SuggestedItemsSource = Tags,
                MaximumTokens = 10,
                Width = 300,
                Margin = new Thickness(0, 16, 0, 0)
            };

            tagsTokenBox.TextChanged += TagsTokenBox_TextChanged;

            contentStackPanel.Children.Add(NameTextBox);
            contentStackPanel.Children.Add(DescriptionRichEditBox);
            contentStackPanel.Children.Add(tagsTokenBox);

            return contentStackPanel;
        }

        private void TagsTokenBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            Tags.Clear();

            var filteredTags = FileManager.itemViewHolder.Tags.FindAll(tag => tag.ToLower().Contains(sender.Text.ToLower()));

            foreach (var tag in filteredTags)
                Tags.Add(tag);
        }
    }
}
