using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UniversalSoundBoard.DataAccess;
using UniversalSoundBoard.Models;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace UniversalSoundBoard.Common
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
        public static ProgressBar downloadFileProgressBar;
        public static ContentDialog NewCategoryContentDialog;
        public static ContentDialog EditCategoryContentDialog;
        public static ContentDialog DeleteCategoryContentDialog;
        public static ContentDialog RenameSoundContentDialog;
        public static ContentDialog DeleteSoundContentDialog;
        public static ContentDialog DeleteSoundsContentDialog;
        public static ContentDialog ExportDataContentDialog;
        public static ContentDialog ImportDataContentDialog;
        public static ContentDialog PlaySoundsSuccessivelyContentDialog;
        public static ContentDialog LogoutContentDialog;
        public static ContentDialog DownloadFileContentDialog;
        public static ContentDialog DownloadFileErrorContentDialog;
        

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

            List<string> IconsList = FileManager.GetIconsList();

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

            Random random = new Random();
            int randomNumber = random.Next(IconsList.Count);
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

        public static ContentDialog CreateEditCategoryContentDialogAsync()
        {
            var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
            Category currentCategory = (App.Current as App)._itemViewHolder.Categories[(App.Current as App)._itemViewHolder.SelectedCategory];

            EditCategoryContentDialog = new ContentDialog
            {
                Title = loader.GetString("EditCategoryContentDialog-Title"),
                PrimaryButtonText = loader.GetString("EditCategoryContentDialog-PrimaryButton"),
                SecondaryButtonText = loader.GetString("ContentDialog-Cancel"),
            };

            StackPanel stackPanel = new StackPanel();
            stackPanel.Orientation = Orientation.Vertical;

            EditCategoryTextBox = new TextBox { Width = 300 };
            EditCategoryTextBox.Text = currentCategory.Name;

            IconSelectionComboBox = new ComboBox
            {
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 25,
                Margin = new Thickness(15),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // Select the icon of the sound
            List<string> IconsList = FileManager.GetIconsList();
            foreach (string icon in IconsList)
            {
                ComboBoxItem item = new ComboBoxItem { Content = icon, FontFamily = new FontFamily("Segoe MDL2 Assets"), FontSize = 25 };
                if (icon == currentCategory.Icon)
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

        public static ContentDialog CreateDeleteCategoryContentDialogAsync()
        {
            var loader = new Windows.ApplicationModel.Resources.ResourceLoader();

            DeleteCategoryContentDialog = new ContentDialog
            {
                Title = loader.GetString("DeleteCategoryContentDialog-Title") + (App.Current as App)._itemViewHolder.Categories[(App.Current as App)._itemViewHolder.SelectedCategory].Name,
                Content = loader.GetString("DeleteCategoryContentDialog-Content"),
                PrimaryButtonText = loader.GetString("DeleteCategoryContentDialog-PrimaryButton"),
                SecondaryButtonText = loader.GetString("ContentDialog-Cancel")
            };

            return DeleteCategoryContentDialog;
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

            RenameSoundTextBox = new TextBox {
                Width = 300,
                Text = sound.Name
            };

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

        public static ContentDialog CreateExportDataContentDialog()
        {
            var loader = new Windows.ApplicationModel.Resources.ResourceLoader();

            ExportDataContentDialog = new ContentDialog
            {
                Title = loader.GetString("ExportDataContentDialog-Title"),
                PrimaryButtonText = loader.GetString("ExportDataContentDialog-PrimaryButton"),
                SecondaryButtonText = loader.GetString("ContentDialog-Cancel"),
                IsPrimaryButtonEnabled = false
            };

            StackPanel content = new StackPanel();
            content.Orientation = Orientation.Vertical;

            TextBlock contentText = new TextBlock
            {
                Margin = new Thickness(0, 0, 0, 30),
                TextWrapping = TextWrapping.WrapWholeWords,
                Text = loader.GetString("ExportDataContentDialog-Text1")
            };


            // Create StackPanel with TextBox and Folder button
            StackPanel folderStackPanel = new StackPanel();
            folderStackPanel.Orientation = Orientation.Horizontal;

            ExportFolderTextBox = new TextBox();
            ExportFolderTextBox.IsReadOnly = true;
            Button folderButton = new Button
            {
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                Content = "\uE838",
                FontSize = 18,
                Width = 35,
                Height = 35
            };
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

        public static ContentDialog CreateImportDataContentDialog()
        {
            var loader = new Windows.ApplicationModel.Resources.ResourceLoader();

            ImportDataContentDialog = new ContentDialog
            {
                Title = loader.GetString("ImportDataContentDialog-Title"),
                PrimaryButtonText = loader.GetString("ImportDataContentDialog-PrimaryButton"),
                SecondaryButtonText = loader.GetString("ContentDialog-Cancel"),
                IsPrimaryButtonEnabled = false
            };

            StackPanel content = new StackPanel();
            content.Orientation = Orientation.Vertical;

            TextBlock contentText = new TextBlock
            {
                Margin = new Thickness(0, 0, 0, 30),
                TextWrapping = TextWrapping.WrapWholeWords,
                Text = loader.GetString("ImportDataContentDialog-Text1")
            };


            // Create StackPanel with TextBox and Folder button
            StackPanel folderStackPanel = new StackPanel();
            folderStackPanel.Orientation = Orientation.Horizontal;

            ImportFolderTextBox = new TextBox();
            ImportFolderTextBox.IsReadOnly = true;
            Button folderButton = new Button
            {
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                Content = "\uE838",
                FontSize = 18,
                Width = 35,
                Height = 35
            };
            folderButton.Tapped += ImportFolderButton_Tapped;

            folderStackPanel.Children.Add(folderButton);
            folderStackPanel.Children.Add(ImportFolderTextBox);


            TextBlock contentText2 = new TextBlock
            {
                Margin = new Thickness(0, 30, 0, 0),
                TextWrapping = TextWrapping.WrapWholeWords,
                Text = loader.GetString("ImportDataContentDialog-Text2")
            };


            content.Children.Add(contentText);
            content.Children.Add(folderStackPanel);
            content.Children.Add(contentText2);

            ImportDataContentDialog.Content = content;

            return ImportDataContentDialog;
        }

        private async static void ImportFolderButton_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
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
                ImportDataContentDialog.IsPrimaryButtonEnabled = true;
            }
        }

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

            if (SoundsList.Count == 0)
                PlaySoundsSuccessivelyContentDialog.IsPrimaryButtonEnabled = false;

            StackPanel content = new StackPanel();
            content.Orientation = Orientation.Vertical;

            SoundsListView = new ListView
            {
                ItemTemplate = itemTemplate,
                ItemsSource = SoundsList,
                SelectionMode = ListViewSelectionMode.None,
                Height = 300,
                ItemContainerStyle = listViewItemStyle,
                CanReorderItems = true,
                CanDrag = true,
                AllowDrop = true
            };

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

            RandomCheckBox = new CheckBox
            {
                Content = loader.GetString("Shuffle"),
                Margin = new Thickness(0, 10, 0, 0)
            };

            content.Children.Add(SoundsListView);
            content.Children.Add(RepeatsComboBox);
            content.Children.Add(RandomCheckBox);

            PlaySoundsSuccessivelyContentDialog.Content = content;
            return PlaySoundsSuccessivelyContentDialog;
        }

        public static ContentDialog CreateLogoutContentDialog()
        {
            var loader = new Windows.ApplicationModel.Resources.ResourceLoader();

            LogoutContentDialog = new ContentDialog
            {
                Title = loader.GetString("Logout"),
                Content = loader.GetString("Account-LogoutMessage"),
                PrimaryButtonText = loader.GetString("Logout"),
                SecondaryButtonText = loader.GetString("ContentDialog-Cancel")
            };

            return LogoutContentDialog;
        }

        public static ContentDialog CreateDownloadFileContentDialog(string filename)
        {
            var loader = new Windows.ApplicationModel.Resources.ResourceLoader();

            DownloadFileContentDialog = new ContentDialog
            {
                Title = string.Format(loader.GetString("DownloadFileContentDialog-Title"), filename),
                SecondaryButtonText = loader.GetString("ContentDialog-Cancel")
            };

            StackPanel content = new StackPanel();
            content.Margin = new Thickness(0, 30, 0, 0);
            content.Orientation = Orientation.Vertical;
            downloadFileProgressBar = new ProgressBar();
            content.Children.Add(downloadFileProgressBar);
            DownloadFileContentDialog.Content = content;

            return DownloadFileContentDialog;
        }

        public static ContentDialog CreateDownloadFileErrorContentDialog()
        {
            var loader = new Windows.ApplicationModel.Resources.ResourceLoader();

            DownloadFileErrorContentDialog = new ContentDialog
            {
                Title = loader.GetString("DownloadFileErrorContentDialog-Title"),
                Content = loader.GetString("DownloadFileErrorContentDialog-Message"),
                CloseButtonText = loader.GetString("ContentDialog-Okay")
            };

            return DownloadFileErrorContentDialog;
        }
    }
}
