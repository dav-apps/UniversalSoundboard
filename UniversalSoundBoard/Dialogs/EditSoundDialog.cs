using CommunityToolkit.WinUI.Controls;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Models;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UniversalSoundboard.Dialogs
{
    public class EditSoundDialog : Dialog
    {
        private ObservableCollection<string> tags;
        private ObservableCollection<string> selectedTags;
        private TextBox NameTextBox;
        private RichEditBox DescriptionRichEditBox;

        public string Name
        {
            get => NameTextBox?.Text;
        }
        public string Description
        {
            get
            {
                if (DescriptionRichEditBox == null) return null;

                string description = null;
                DescriptionRichEditBox.Document.GetText(TextGetOptions.NoHidden, out description);
                return description;
            }
        }
        public List<string> SelectedTags
        {
            get => selectedTags.ToList();
        }

        public EditSoundDialog(SoundResponse sound, DataTemplate itemTemplate)
            : base(
                  FileManager.loader.GetString("EditSoundDialog-Title"),
                  FileManager.loader.GetString("Actions-Save"),
                  FileManager.loader.GetString("Actions-Cancel")
                )
        {
            tags = new ObservableCollection<string>();
            selectedTags = new ObservableCollection<string>();

            foreach (var tag in FileManager.itemViewHolder.Tags)
                tags.Add(tag);

            foreach (var tag in sound.Tags)
                selectedTags.Add(tag);

            Content = GetContent(sound, itemTemplate);
        }

        private StackPanel GetContent(SoundResponse sound, DataTemplate itemTemplate)
        {
            StackPanel contentStackPanel = new StackPanel();

            NameTextBox = new TextBox
            {
                Text = sound.Name,
                Header = FileManager.loader.GetString("EditSoundDialog-NameHeader"),
                PlaceholderText = FileManager.loader.GetString("EditSoundDialog-NamePlaceholder"),
                Width = 300
            };

            DescriptionRichEditBox = new RichEditBox
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
                ItemsSource = selectedTags,
                SuggestedItemsSource = tags,
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
            tags.Clear();

            var filteredTags = FileManager.itemViewHolder.Tags.FindAll(tag => tag.ToLower().Contains(sender.Text.ToLower()));

            foreach (var tag in filteredTags)
                tags.Add(tag);
        }
    }
}
