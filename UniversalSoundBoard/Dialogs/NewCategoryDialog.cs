using System;
using System.Collections.Generic;
using UniversalSoundboard.DataAccess;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace UniversalSoundboard.Dialogs
{
    public class NewCategoryDialog : Dialog
    {
        private TextBox NewCategoryTextBox;
        private ComboBox IconSelectionComboBox;

        public string Name
        {
            get => NewCategoryTextBox.Text;
        }
        public string Icon
        {
            get => (IconSelectionComboBox.SelectedItem as ComboBoxItem).Content.ToString();
        }

        public NewCategoryDialog(bool subCategory = false)
            : base(
                  subCategory ? FileManager.loader.GetString("NewCategoryDialog-SubCategory-Title") : FileManager.loader.GetString("NewCategoryDialog-Title"),
                  FileManager.loader.GetString("NewCategoryDialog-PrimaryButton"),
                  FileManager.loader.GetString("Actions-Cancel")
            )
        {
            Content = GetContent();
        }

        private StackPanel GetContent()
        {
            StackPanel stackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical
            };

            List<string> IconsList = FileManager.GetIconsList();

            NewCategoryTextBox = new TextBox
            {
                Width = 300,
                PlaceholderText = FileManager.loader.GetString("NewCategoryDialog-NewCategoryTextBoxPlaceholder")
            };
            NewCategoryTextBox.TextChanged += NewCategoryContentDialogTextBox_TextChanged;

            IconSelectionComboBox = new ComboBox
            {
                FontFamily = new FontFamily(FileManager.FluentIconsFontFamily),
                FontSize = 25,
                Margin = new Thickness(15),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            foreach (string icon in IconsList)
                IconSelectionComboBox.Items.Add(new ComboBoxItem { Content = icon, FontFamily = new FontFamily(FileManager.FluentIconsFontFamily), FontSize = 25 });

            Random random = new Random();
            int randomNumber = random.Next(IconsList.Count);
            IconSelectionComboBox.SelectedIndex = randomNumber;

            stackPanel.Children.Add(NewCategoryTextBox);
            stackPanel.Children.Add(IconSelectionComboBox);

            return stackPanel;
        }

        private void NewCategoryContentDialogTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ContentDialog.IsPrimaryButtonEnabled = NewCategoryTextBox.Text.Length >= 2;
        }
    }
}
