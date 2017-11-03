using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace UniversalSoundBoard.Model
{
    public class ContentDialogs
    {
        public static TextBox NewCategoryTextBox;
        public static TextBox EditCategoryTextBox;
        public static TextBox RenameSoundTextBox;
        public static TextBox ExportFolderTextBox;
        public static TextBox ImportFolderTextBox;
        public static StorageFolder ExportFolder;
        public static StorageFile ImportFile;
        public static ComboBox IconSelectionComboBox;
        public static CheckBox RandomCheckBox;
        public static ListView SoundsListView;
        public static ComboBox RepeatsComboBox;
        public static ObservableCollection<Sound> SoundsList;
        public static ContentDialog NewCategoryContentDialog;
        public static ContentDialog EditCategoryContentDialog;
        public static ContentDialog DeleteSoundContentDialog;
        public static ContentDialog RenameSoundContentDialog;
        public static ContentDialog DeleteSoundsContentDialog;
        public static ContentDialog ExportDataContentDialog;
        public static ContentDialog ImportDataContentDialog;
        public static ContentDialog PlaySoundsSuccessivelyContentDialog;


        public static ContentDialog CreatePlaySoundsSuccessivelyContentDialog(ObservableCollection<Sound> sounds, DataTemplate itemTemplate, Style listViewItemStyle)
        {
            SoundsList = sounds;
            var loader = new Windows.ApplicationModel.Resources.ResourceLoader();

            PlaySoundsSuccessivelyContentDialog = new ContentDialog
            {
                Title = loader.GetString("PlaySoundsSuccessivelyContentDialog-Title"),
                PrimaryButtonText = loader.GetString("PlaySoundsSuccessivelyContentDialog-PrimaryButton"),
                SecondaryButtonText = loader.GetString("ContentDialog-Cancel")
            };

            StackPanel content = new StackPanel();
            content.Orientation = Orientation.Vertical;

            SoundsListView = new ListView();
            SoundsListView.ItemTemplate = itemTemplate;
            SoundsListView.ItemsSource = SoundsList;
            SoundsListView.SelectionMode = ListViewSelectionMode.None;
            SoundsListView.Height = 300;
            SoundsListView.ItemContainerStyle = listViewItemStyle;
            SoundsListView.CanReorderItems = true;
            SoundsListView.CanDrag = true;
            SoundsListView.AllowDrop = true;

            RepeatsComboBox = new ComboBox();
            RepeatsComboBox.Margin = new Thickness(0, 10, 0, 0);
            RepeatsComboBox.Items.Add("1");
            RepeatsComboBox.Items.Add("2");
            RepeatsComboBox.Items.Add("3");
            RepeatsComboBox.Items.Add("4");
            RepeatsComboBox.Items.Add("5");
            RepeatsComboBox.Items.Add("6");
            RepeatsComboBox.Items.Add("7");
            RepeatsComboBox.Items.Add("8");
            RepeatsComboBox.Items.Add("9");
            RepeatsComboBox.Items.Add("10");
            RepeatsComboBox.Items.Add("15");
            RepeatsComboBox.Items.Add("20");
            RepeatsComboBox.Items.Add("25");
            RepeatsComboBox.Items.Add("30");
            RepeatsComboBox.Items.Add("40");
            RepeatsComboBox.Items.Add("50");
            RepeatsComboBox.Items.Add("100");
            RepeatsComboBox.Items.Add("∞");
            RepeatsComboBox.SelectedIndex = 0;

            RandomCheckBox = new CheckBox();
            RandomCheckBox.Content = loader.GetString("Shuffle");
            RandomCheckBox.Margin = new Thickness(0, 10, 0, 0);

            content.Children.Add(SoundsListView);
            content.Children.Add(RepeatsComboBox);
            content.Children.Add(RandomCheckBox);

            PlaySoundsSuccessivelyContentDialog.Content = content;
            return PlaySoundsSuccessivelyContentDialog;
        }

        public static ContentDialog CreateImportDataContentDialog()
        {
            var loader = new Windows.ApplicationModel.Resources.ResourceLoader();

            ImportDataContentDialog = new ContentDialog
            {
                Title = loader.GetString("ImportDataContentDialog-Title"),
                PrimaryButtonText = loader.GetString("ImportDataContentDialog-PrimaryButton"),
                IsPrimaryButtonEnabled = false,
                SecondaryButtonText = loader.GetString("ContentDialog-Cancel")
            };

            StackPanel content = new StackPanel();
            content.Orientation = Orientation.Vertical;

            TextBlock contentText = new TextBlock();
            contentText.Margin = new Thickness(0, 0, 0, 30);
            contentText.TextWrapping = TextWrapping.WrapWholeWords;
            contentText.Text = loader.GetString("ImportDataContentDialog-Text1");


            // Create StackPanel with TextBox and Folder button
            StackPanel folderStackPanel = new StackPanel();
            folderStackPanel.Orientation = Orientation.Horizontal;

            ImportFolderTextBox = new TextBox();
            ImportFolderTextBox.IsReadOnly = true;
            Button folderButton = new Button();
            folderButton.FontFamily = new FontFamily("Segoe MDL2 Assets");
            folderButton.Content = "\uE838";
            folderButton.FontSize = 18;
            folderButton.Width = 35;
            folderButton.Height = 35;
            folderButton.Tapped += ImportFolderButton_Tapped;

            folderStackPanel.Children.Add(folderButton);
            folderStackPanel.Children.Add(ImportFolderTextBox);


            TextBlock contentText2 = new TextBlock();
            contentText2.Margin = new Thickness(0, 30, 0, 0);
            contentText2.TextWrapping = TextWrapping.WrapWholeWords;
            contentText2.Text = loader.GetString("ImportDataContentDialog-Text2");


            content.Children.Add(contentText);
            content.Children.Add(folderStackPanel);
            content.Children.Add(contentText2);

            ImportDataContentDialog.Content = content;

            return ImportDataContentDialog;
        }

        private async static void ImportFolderButton_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            picker.ViewMode = PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
            picker.FileTypeFilter.Add(".zip");

            StorageFile file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                // Set TextBox text and StorageFile variable and make primary button clickable
                ImportFile = file;
                ImportFolderTextBox.Text = file.Path;
                ImportDataContentDialog.IsPrimaryButtonEnabled = true;
            }
        }

        public static ContentDialog CreateExportDataContentDialog()
        {
            var loader = new Windows.ApplicationModel.Resources.ResourceLoader();

            ExportDataContentDialog = new ContentDialog
            {
                Title = loader.GetString("ExportDataContentDialog-Title"),
                PrimaryButtonText = loader.GetString("ExportDataContentDialog-PrimaryButton"),
                IsPrimaryButtonEnabled = false,
                SecondaryButtonText = loader.GetString("ContentDialog-Cancel")
            };

            StackPanel content = new StackPanel();
            content.Orientation = Orientation.Vertical;

            TextBlock contentText = new TextBlock();
            contentText.Margin = new Thickness(0, 0, 0, 30);
            contentText.TextWrapping = TextWrapping.WrapWholeWords;
            contentText.Text = loader.GetString("ExportDataContentDialog-Text1");


            // Create StackPanel with TextBox and Folder button
            StackPanel folderStackPanel = new StackPanel();
            folderStackPanel.Orientation = Orientation.Horizontal;

            ExportFolderTextBox = new TextBox();
            ExportFolderTextBox.IsReadOnly = true;
            Button folderButton = new Button();
            folderButton.FontFamily = new FontFamily("Segoe MDL2 Assets");
            folderButton.Content = "\uE838";
            folderButton.FontSize = 18;
            folderButton.Width = 35;
            folderButton.Height = 35;
            folderButton.Tapped += ExportFolderButton_Tapped;

            folderStackPanel.Children.Add(folderButton);
            folderStackPanel.Children.Add(ExportFolderTextBox);


            TextBlock contentText2 = new TextBlock();
            contentText2.Margin = new Thickness(0, 30, 0, 0);
            contentText2.TextWrapping = TextWrapping.WrapWholeWords;
            contentText2.Text = loader.GetString("ExportDataContentDialog-Text2");


            content.Children.Add(contentText);
            content.Children.Add(folderStackPanel);
            content.Children.Add(contentText2);

            ExportDataContentDialog.Content = content;

            return ExportDataContentDialog;
        }

        private static async void ExportFolderButton_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            var folderPicker = new FolderPicker();
            folderPicker.SuggestedStartLocation = PickerLocationId.Downloads;
            folderPicker.FileTypeFilter.Add("*");

            StorageFolder folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                Windows.Storage.AccessCache.StorageApplicationPermissions.
                FutureAccessList.AddOrReplace("PickedFolderToken", folder);

                // Set TextBox text and StorageFolder variable and make primary button clickable
                ExportFolder = folder;
                ExportFolderTextBox.Text = folder.Path;
                ExportDataContentDialog.IsPrimaryButtonEnabled = true;
            }
        }

        public static ContentDialog CreateDeleteSoundsContentDialogAsync()
        {
            var loader = new Windows.ApplicationModel.Resources.ResourceLoader();

            DeleteSoundsContentDialog = new ContentDialog
            {
                Title = loader.GetString("DeleteSoundsContentDialog-Title"),
                Content = loader.GetString("DeleteSoundsContentDialog-Content"),
                PrimaryButtonText = loader.GetString("DeleteSoundsContentDialog-PrimaryButton"),
                SecondaryButtonText = loader.GetString("ContentDialog-Cancel")
            };

            return DeleteSoundsContentDialog;
        }

        public static ContentDialog CreateRenameSoundContentDialog(Sound sound)
        {
            var loader = new Windows.ApplicationModel.Resources.ResourceLoader();

            RenameSoundContentDialog = new ContentDialog
            {
                Title = loader.GetString("RenameSoundContentDialog-Title"),
                PrimaryButtonText = loader.GetString("RenameSoundContentDialog-PrimaryButton"),
                SecondaryButtonText = loader.GetString("ContentDialog-Cancel"),
            };

            StackPanel stackPanel = new StackPanel();
            stackPanel.Orientation = Orientation.Vertical;

            RenameSoundTextBox = new TextBox { Width = 300 };
            RenameSoundTextBox.Text = sound.Name;

            stackPanel.Children.Add(RenameSoundTextBox);

            RenameSoundContentDialog.Content = stackPanel;

            RenameSoundTextBox.TextChanged += RenameSoundTextBox_TextChanged;

            return RenameSoundContentDialog;
        }

        private static void RenameSoundTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (RenameSoundTextBox.Text.Length < 3)
            {
                RenameSoundContentDialog.IsPrimaryButtonEnabled = false;
            }
            else
            {
                RenameSoundContentDialog.IsPrimaryButtonEnabled = true;
            }
        }

        public static ContentDialog CreateDeleteSoundContentDialog(string soundName)
        {
            var loader = new Windows.ApplicationModel.Resources.ResourceLoader();

            DeleteSoundContentDialog = new ContentDialog
            {
                Title = loader.GetString("DeleteSoundContentDialog-Title") + soundName,
                Content = loader.GetString("DeleteSoundContentDialog-Content"),
                PrimaryButtonText = loader.GetString("DeleteSoundContentDialog-PrimaryButton"),
                SecondaryButtonText = loader.GetString("ContentDialog-Cancel")
            };

            return DeleteSoundContentDialog;
        }

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
