using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace UniversalSoundBoard.Model
{
    public class ContentDialogs
    {
        public static TextBox NewCategoryTextBox;
        public static TextBox EditCategoryTextBox;
        public static ComboBox IconSelectionComboBox;
        public static ContentDialog NewCategoryContentDialog;
        public static ContentDialog EditCategoryContentDialog;


        public static ContentDialog CreateNewCategoryContentDialog()
        {
            var loader = new Windows.ApplicationModel.Resources.ResourceLoader();

            NewCategoryContentDialog = new ContentDialog
            {
                Title = loader.GetString("NewCategoryContentDialog-Title"),
                PrimaryButtonText = loader.GetString("NewCategoryContentDialog-PrimaryButton"),
                SecondaryButtonText = loader.GetString("ContentDialog-Cancel"),
                IsPrimaryButtonEnabled = false
            };

            StackPanel stackPanel = new StackPanel();
            stackPanel.Orientation = Orientation.Vertical;

            List<string> IconsList = FileManager.createIconsList();

            Random random = new Random();
            int randomNumber = random.Next(IconsList.Count);

            NewCategoryTextBox = new TextBox { Width = 300 };
            NewCategoryTextBox.Text = "";

            IconSelectionComboBox = new ComboBox
            {
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 25,
                Margin = new Thickness(15),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            foreach (string icon in IconsList)
            {
                IconSelectionComboBox.Items.Add(new ComboBoxItem { Content = icon, FontFamily = new FontFamily("Segoe MDL2 Assets"), FontSize = 25 });
            }
            IconSelectionComboBox.SelectedIndex = randomNumber;

            stackPanel.Children.Add(NewCategoryTextBox);
            stackPanel.Children.Add(IconSelectionComboBox);

            NewCategoryContentDialog.Content = stackPanel;

            NewCategoryTextBox.TextChanged += NewCategoryContentDialogTextBox_TextChanged;

            return NewCategoryContentDialog;
        }

        private static void NewCategoryContentDialogTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (NewCategoryTextBox.Text.Length < 3)
            {
                NewCategoryContentDialog.IsPrimaryButtonEnabled = false;
            }
            else
            {
                NewCategoryContentDialog.IsPrimaryButtonEnabled = true;
            }
        }

        public static ContentDialog CreateDeleteCategoryContentDialogAsync()
        {
            var loader = new Windows.ApplicationModel.Resources.ResourceLoader();

            ContentDialog DeleteCategoryContentDialog = new ContentDialog
            {
                Title = loader.GetString("DeleteCategoryContentDialog-Title") + (App.Current as App)._itemViewHolder.title,
                Content = loader.GetString("DeleteCategoryContentDialog-Content"),
                PrimaryButtonText = loader.GetString("DeleteCategoryContentDialog-PrimaryButton"),
                SecondaryButtonText = loader.GetString("ContentDialog-Cancel")
            };

            return DeleteCategoryContentDialog;
        }

        public static async Task<ContentDialog> CreateEditCategoryContentDialogAsync()
        {
            var loader = new Windows.ApplicationModel.Resources.ResourceLoader();

            EditCategoryContentDialog = new ContentDialog
            {
                Title = loader.GetString("EditCategoryContentDialog-Title"),
                PrimaryButtonText = loader.GetString("EditCategoryContentDialog-PrimaryButton"),
                SecondaryButtonText = loader.GetString("ContentDialog-Cancel"),
            };

            StackPanel stackPanel = new StackPanel();
            stackPanel.Orientation = Orientation.Vertical;

            List<string> IconsList = FileManager.createIconsList();

            EditCategoryTextBox = new TextBox { Width = 300 };
            EditCategoryTextBox.Text = (App.Current as App)._itemViewHolder.title;

            IconSelectionComboBox = new ComboBox
            {
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 25,
                Margin = new Thickness(15),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            foreach (string icon in IconsList)
            {
                ComboBoxItem item = new ComboBoxItem { Content = icon, FontFamily = new FontFamily("Segoe MDL2 Assets"), FontSize = 25 };
                if (icon == (await FileManager.GetCategoryByNameAsync((App.Current as App)._itemViewHolder.title)).Icon)
                {
                    item.IsSelected = true;
                }
                IconSelectionComboBox.Items.Add(item);
            }

            stackPanel.Children.Add(EditCategoryTextBox);
            stackPanel.Children.Add(IconSelectionComboBox);

            EditCategoryContentDialog.Content = stackPanel;

            EditCategoryTextBox.TextChanged += EditCategoryTextBox_TextChanged;

            return EditCategoryContentDialog;
        }

        private static void EditCategoryTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (EditCategoryTextBox.Text.Length < 3)
            {
                EditCategoryContentDialog.IsPrimaryButtonEnabled = false;
            }
            else
            {
                EditCategoryContentDialog.IsPrimaryButtonEnabled = true;
            }
        }
    }
}
