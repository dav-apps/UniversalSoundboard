using System.Collections.Generic;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Models;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace UniversalSoundboard.Dialogs
{
    public class EditCategoryDialog : Dialog
    {
        private TextBox EditCategoryTextBox;
        private ComboBox IconSelectionComboBox;

        public string Name
        {
            get => EditCategoryTextBox.Text;
        }
        public string Icon
        {
            get => (IconSelectionComboBox.SelectedItem as ComboBoxItem).Content.ToString();
        }

        public EditCategoryDialog(Category category)
            : base(
                  FileManager.loader.GetString("EditCategoryDialog-Title"),
                  FileManager.loader.GetString("Actions-Save"),
                  FileManager.loader.GetString("Actions-Cancel")
            )
        {
            Content = GetContent(category);
        }

        private StackPanel GetContent(Category category)
        {
            StackPanel stackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical
            };

            EditCategoryTextBox = new TextBox
            {
                Text = category.Name,
                PlaceholderText = FileManager.loader.GetString("NewCategoryDialog-NewCategoryTextBoxPlaceholder"),
                Width = 300
            };
            EditCategoryTextBox.TextChanged += EditCategoryTextBox_TextChanged;

            IconSelectionComboBox = new ComboBox
            {
                FontFamily = new FontFamily(FileManager.FluentIconsFontFamily),
                FontSize = 25,
                Margin = new Thickness(15),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // Select the icon of the sound
            List<string> IconsList = FileManager.GetIconsList();

            foreach (string icon in IconsList)
            {
                ComboBoxItem item = new ComboBoxItem { Content = icon, FontFamily = new FontFamily(FileManager.FluentIconsFontFamily), FontSize = 25 };
                if (icon == category.Icon)
                    item.IsSelected = true;

                IconSelectionComboBox.Items.Add(item);
            }

            stackPanel.Children.Add(EditCategoryTextBox);
            stackPanel.Children.Add(IconSelectionComboBox);

            return stackPanel;
        }

        private void EditCategoryTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ContentDialog.IsPrimaryButtonEnabled = EditCategoryTextBox.Text.Length >= 2;
        }
    }
}
