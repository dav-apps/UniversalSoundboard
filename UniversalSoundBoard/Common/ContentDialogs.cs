using davClassLibrary;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using UniversalSoundboard.Components;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Models;
using UniversalSoundboard.Pages;
using Windows.ApplicationModel.Resources;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using WinUI = Microsoft.UI.Xaml.Controls;

namespace UniversalSoundboard.Common
{
    public class ContentDialogs
    {
        #region Variables
        private static readonly ResourceLoader loader = new ResourceLoader();

        private static Sound selectedPropertiesSound;
        private static VolumeControl PropertiesVolumeControl;
        private static bool propertiesDefaultVolumeChanged = false;
        private static bool propertiesDefaultMutedChanged = false;

        public static bool ContentDialogOpen = false;
        public static TextBox NewSoundUrlTextBox;
        public static TextBox NewCategoryTextBox;
        public static Guid NewCategoryParentUuid;
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
        public static ObservableCollection<Sound> SoundsList = new ObservableCollection<Sound>();
        public static ProgressBar downloadFileProgressBar;
        public static List<Sound> downloadingFilesSoundsList = new List<Sound>();
        public static ListView ExportSoundsListView;
        public static TextBox ExportSoundsFolderTextBox;
        public static CheckBox ExportSoundsAsZipCheckBox;
        public static StorageFolder ExportSoundsFolder;
        public static ListView CategoriesListView;
        public static WinUI.TreeView CategoriesTreeView;
        public static ComboBox PlaybackSpeedComboBox;
        public static List<ObservableCollection<HotkeyItem>> PropertiesDialogHotkeys = new List<ObservableCollection<HotkeyItem>>();
        public static StackPanel davPlusHotkeyInfoStackPanel;
        public static ContentDialog NewSoundFromUrlContentDialog;
        public static ContentDialog NewCategoryContentDialog;
        public static ContentDialog EditCategoryContentDialog;
        public static ContentDialog DeleteCategoryContentDialog;
        public static ContentDialog AddSoundErrorContentDialog;
        public static ContentDialog AddSoundsErrorContentDialog;
        public static ContentDialog RenameSoundContentDialog;
        public static ContentDialog DeleteSoundContentDialog;
        public static ContentDialog DeleteSoundsContentDialog;
        public static ContentDialog ExportDataContentDialog;
        public static ContentDialog ImportDataContentDialog;
        public static ContentDialog PlaySoundsSuccessivelyContentDialog;
        public static ContentDialog LogoutContentDialog;
        public static ContentDialog DownloadFileContentDialog;
        public static ContentDialog DownloadFilesContentDialog;
        public static ContentDialog DownloadFileErrorContentDialog;
        public static ContentDialog ExportSoundsContentDialog;
        public static ContentDialog SetCategoryContentDialog;
        public static ContentDialog CategoryOrderContentDialog;
        public static ContentDialog PropertiesContentDialog;
        public static ContentDialog DavPlusHotkeysContentDialog;
        public static ContentDialog DavPlusOutputDeviceContentDialog;
        #endregion

        #region General methods
        private static void ContentDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
        {
            ContentDialogOpen = true;
        }

        private static void ContentDialog_Closed(ContentDialog sender, ContentDialogClosedEventArgs args)
        {
            ContentDialogOpen = false;
        }
        #endregion

        #region NewSoundFromUrl
        public static ContentDialog CreateNewSoundFromUrlContentDialog()
        {
            NewSoundFromUrlContentDialog = new ContentDialog
            {
                Title = "Neuer Sound von URL",
                PrimaryButtonText = "Hinzufügen",
                CloseButtonText = loader.GetString("Actions-Cancel"),
                DefaultButton = ContentDialogButton.Primary,
                IsPrimaryButtonEnabled = false,
                RequestedTheme = FileManager.GetRequestedTheme()
            };
            NewSoundFromUrlContentDialog.Opened += ContentDialog_Opened;
            NewSoundFromUrlContentDialog.Closed += ContentDialog_Closed;

            StackPanel containerStackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical
            };

            NewSoundUrlTextBox = new TextBox();
            NewSoundUrlTextBox.TextChanged += NewSoundUrlTextBox_TextChanged;
            containerStackPanel.Children.Add(NewSoundUrlTextBox);

            NewSoundFromUrlContentDialog.Content = containerStackPanel;
            return NewSoundFromUrlContentDialog;
        }

        private static void NewSoundUrlTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            NewSoundFromUrlContentDialog.IsPrimaryButtonEnabled = NewSoundUrlTextBox.Text.Length > 2;
        }
        #endregion

        #region NewCategory
        public static ContentDialog CreateNewCategoryContentDialog(Guid parentUuid)
        {
            NewCategoryContentDialog = new ContentDialog
            {
                Title = loader.GetString("NewCategoryContentDialog-Title"),
                PrimaryButtonText = loader.GetString("NewCategoryContentDialog-PrimaryButton"),
                CloseButtonText = loader.GetString("Actions-Cancel"),
                DefaultButton = ContentDialogButton.Primary,
                IsPrimaryButtonEnabled = false,
                RequestedTheme = FileManager.GetRequestedTheme()
            };
            NewCategoryContentDialog.Opened += ContentDialog_Opened;
            NewCategoryContentDialog.Closed += ContentDialog_Closed;

            NewCategoryParentUuid = parentUuid;
            if (!Equals(parentUuid, Guid.Empty))
                NewCategoryContentDialog.Title = loader.GetString("NewSubCategoryContentDialog-Title");

            StackPanel stackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical
            };

            List<string> IconsList = FileManager.GetIconsList();

            NewCategoryTextBox = new TextBox { Width = 300 };

            IconSelectionComboBox = new ComboBox
            {
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 25,
                Margin = new Thickness(15),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            foreach (string icon in IconsList)
                IconSelectionComboBox.Items.Add(new ComboBoxItem { Content = icon, FontFamily = new FontFamily("Segoe MDL2 Assets"), FontSize = 25 });

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
            NewCategoryContentDialog.IsPrimaryButtonEnabled = NewCategoryTextBox.Text.Length >= 2;
        }
        #endregion

        #region EditCategory
        public static ContentDialog CreateEditCategoryContentDialog()
        {
            Category currentCategory = FileManager.FindCategory(FileManager.itemViewHolder.SelectedCategory);

            EditCategoryContentDialog = new ContentDialog
            {
                Title = loader.GetString("EditCategoryContentDialog-Title"),
                PrimaryButtonText = loader.GetString("EditCategoryContentDialog-PrimaryButton"),
                CloseButtonText = loader.GetString("Actions-Cancel"),
                DefaultButton = ContentDialogButton.Primary,
                RequestedTheme = FileManager.GetRequestedTheme()
            };
            EditCategoryContentDialog.Opened += ContentDialog_Opened;
            EditCategoryContentDialog.Closed += ContentDialog_Closed;

            StackPanel stackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical
            };

            EditCategoryTextBox = new TextBox
            {
                Text = currentCategory.Name,
                Width = 300
            };

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
                    item.IsSelected = true;

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
            EditCategoryContentDialog.IsPrimaryButtonEnabled = EditCategoryTextBox.Text.Length >= 2;
        }
        #endregion

        #region DeleteCategory
        public static ContentDialog CreateDeleteCategoryContentDialogAsync()
        {
            Category currentCategory = FileManager.FindCategory(FileManager.itemViewHolder.SelectedCategory);

            DeleteCategoryContentDialog = new ContentDialog
            {
                Title = loader.GetString("DeleteCategoryContentDialog-Title") + currentCategory.Name,
                Content = loader.GetString("DeleteCategoryContentDialog-Content"),
                PrimaryButtonText = loader.GetString("DeleteCategoryContentDialog-PrimaryButton"),
                CloseButtonText = loader.GetString("Actions-Cancel"),
                DefaultButton = ContentDialogButton.Close,
                RequestedTheme = FileManager.GetRequestedTheme()
            };
            DeleteCategoryContentDialog.Opened += ContentDialog_Opened;
            DeleteCategoryContentDialog.Closed += ContentDialog_Closed;

            return DeleteCategoryContentDialog;
        }
        #endregion

        #region AddSoundError
        public static ContentDialog CreateAddSoundErrorContentDialog()
        {
            AddSoundErrorContentDialog = new ContentDialog
            {
                Title = loader.GetString("AddSoundErrorContentDialog-Title"),
                Content = loader.GetString("AddSoundErrorContentDialog-Content"),
                CloseButtonText = loader.GetString("Actions-Close"),
                DefaultButton = ContentDialogButton.Close,
                RequestedTheme = FileManager.GetRequestedTheme()
            };
            AddSoundErrorContentDialog.Opened += ContentDialog_Opened;
            AddSoundErrorContentDialog.Closed += ContentDialog_Closed;

            return AddSoundErrorContentDialog;
        }
        #endregion

        #region AddSoundsError
        public static ContentDialog CreateAddSoundsErrorContentDialog(List<string> soundsList)
        {
            string soundNames = "";
            foreach(var name in soundsList)
                soundNames += $"\n- {name}";

            string content = string.Format(loader.GetString("AddSoundsErrorContentDialog-Content"), soundNames);

            AddSoundsErrorContentDialog = new ContentDialog
            {
                Title = loader.GetString("AddSoundsErrorContentDialog-Title"),
                Content = content,
                CloseButtonText = loader.GetString("Actions-Close"),
                DefaultButton = ContentDialogButton.Close,
                RequestedTheme = FileManager.GetRequestedTheme()
            };
            AddSoundsErrorContentDialog.Opened += ContentDialog_Opened;
            AddSoundsErrorContentDialog.Closed += ContentDialog_Closed;

            return AddSoundsErrorContentDialog;
        }
        #endregion

        #region RenameSound
        public static ContentDialog CreateRenameSoundContentDialog(Sound sound)
        {
            RenameSoundContentDialog = new ContentDialog
            {
                Title = loader.GetString("RenameSoundContentDialog-Title"),
                PrimaryButtonText = loader.GetString("RenameSoundContentDialog-PrimaryButton"),
                CloseButtonText = loader.GetString("Actions-Cancel"),
                DefaultButton = ContentDialogButton.Primary,
                RequestedTheme = FileManager.GetRequestedTheme()
            };
            RenameSoundContentDialog.Opened += ContentDialog_Opened;
            RenameSoundContentDialog.Closed += ContentDialog_Closed;

            StackPanel stackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical
            };

            RenameSoundTextBox = new TextBox
            {
                Text = sound.Name,
                Width = 300
            };

            stackPanel.Children.Add(RenameSoundTextBox);

            RenameSoundContentDialog.Content = stackPanel;
            RenameSoundTextBox.TextChanged += RenameSoundTextBox_TextChanged;

            return RenameSoundContentDialog;
        }

        private static void RenameSoundTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RenameSoundContentDialog.IsPrimaryButtonEnabled = RenameSoundTextBox.Text.Length >= 3;
        }
        #endregion

        #region DeleteSound
        public static ContentDialog CreateDeleteSoundContentDialog(string soundName)
        {
            DeleteSoundContentDialog = new ContentDialog
            {
                Title = loader.GetString("DeleteSoundContentDialog-Title") + soundName,
                Content = loader.GetString("DeleteSoundContentDialog-Content"),
                PrimaryButtonText = loader.GetString("DeleteSoundContentDialog-PrimaryButton"),
                CloseButtonText = loader.GetString("Actions-Cancel"),
                DefaultButton = ContentDialogButton.Close,
                RequestedTheme = FileManager.GetRequestedTheme()
            };
            DeleteSoundContentDialog.Opened += ContentDialog_Opened;
            DeleteSoundContentDialog.Closed += ContentDialog_Closed;

            return DeleteSoundContentDialog;
        }
        #endregion

        #region DeleteSounds
        public static ContentDialog CreateDeleteSoundsContentDialogAsync()
        {
            DeleteSoundsContentDialog = new ContentDialog
            {
                Title = loader.GetString("DeleteSoundsContentDialog-Title"),
                Content = loader.GetString("DeleteSoundsContentDialog-Content"),
                PrimaryButtonText = loader.GetString("DeleteSoundsContentDialog-PrimaryButton"),
                CloseButtonText = loader.GetString("Actions-Cancel"),
                DefaultButton = ContentDialogButton.Close,
                RequestedTheme = FileManager.GetRequestedTheme()
            };
            DeleteSoundsContentDialog.Opened += ContentDialog_Opened;
            DeleteSoundsContentDialog.Closed += ContentDialog_Closed;

            return DeleteSoundsContentDialog;
        }
        #endregion

        #region ExportData
        public static ContentDialog CreateExportDataContentDialog()
        {
            ExportDataContentDialog = new ContentDialog
            {
                Title = loader.GetString("ExportDataContentDialog-Title"),
                PrimaryButtonText = loader.GetString("ExportDataContentDialog-PrimaryButton"),
                CloseButtonText = loader.GetString("Actions-Cancel"),
                DefaultButton = ContentDialogButton.Primary,
                IsPrimaryButtonEnabled = false,
                RequestedTheme = FileManager.GetRequestedTheme()
            };
            ExportDataContentDialog.Opened += ContentDialog_Opened;
            ExportDataContentDialog.Closed += ContentDialog_Closed;

            StackPanel content = new StackPanel
            {
                Orientation = Orientation.Vertical
            };

            TextBlock contentText = new TextBlock
            {
                Margin = new Thickness(0, 0, 0, 20),
                TextWrapping = TextWrapping.WrapWholeWords,
                Text = loader.GetString("ExportDataContentDialog-Text1")
            };

            // Create StackPanel with TextBox and Folder button
            StackPanel folderStackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };

            ExportFolderTextBox = new TextBox
            {
                IsReadOnly = true
            };

            Button folderButton = new Button
            {
                Style = MainPage.buttonRevealStyle,
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                Content = "\uE838",
                FontSize = 18,
                Width = 35,
                Height = 35,
                Padding = new Thickness(0)
            };
            folderButton.Tapped += ExportFolderButton_Tapped;

            folderStackPanel.Children.Add(folderButton);
            folderStackPanel.Children.Add(ExportFolderTextBox);

            TextBlock contentText2 = new TextBlock
            {
                Margin = new Thickness(0, 20, 0, 0),
                TextWrapping = TextWrapping.WrapWholeWords,
                Text = loader.GetString("ExportDataContentDialog-Text2")
            };

            content.Children.Add(contentText);
            content.Children.Add(folderStackPanel);
            content.Children.Add(contentText2);

            ExportDataContentDialog.Content = content;

            return ExportDataContentDialog;
        }

        private static async void ExportFolderButton_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            var folderPicker = new FolderPicker
            {
                SuggestedStartLocation = PickerLocationId.Downloads
            };

            folderPicker.FileTypeFilter.Add("*");
            StorageFolder folder = await folderPicker.PickSingleFolderAsync();

            if (folder != null)
            {
                StorageApplicationPermissions.FutureAccessList.AddOrReplace("PickedFolderToken", folder);

                // Set TextBox text and StorageFolder variable and make primary button clickable
                ExportFolder = folder;
                ExportFolderTextBox.Text = folder.Path;
                ExportDataContentDialog.IsPrimaryButtonEnabled = true;
            }
        }
        #endregion

        #region ImportData
        public static ContentDialog CreateImportDataContentDialog()
        {
            ImportDataContentDialog = new ContentDialog
            {
                Title = loader.GetString("ImportDataContentDialog-Title"),
                PrimaryButtonText = loader.GetString("ImportDataContentDialog-PrimaryButton"),
                CloseButtonText = loader.GetString("Actions-Cancel"),
                DefaultButton = ContentDialogButton.Primary,
                IsPrimaryButtonEnabled = false,
                RequestedTheme = FileManager.GetRequestedTheme()
            };
            ImportDataContentDialog.Opened += ContentDialog_Opened;
            ImportDataContentDialog.Closed += ContentDialog_Closed;

            StackPanel content = new StackPanel
            {
                Orientation = Orientation.Vertical
            };

            TextBlock contentText = new TextBlock
            {
                Margin = new Thickness(0, 0, 0, 20),
                TextWrapping = TextWrapping.WrapWholeWords,
                Text = loader.GetString("ImportDataContentDialog-Text1")
            };

            // Create StackPanel with TextBox and Folder button
            StackPanel folderStackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };

            ImportFolderTextBox = new TextBox
            {
                IsReadOnly = true
            };

            Button folderButton = new Button
            {
                Style = MainPage.buttonRevealStyle,
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                Content = "\uE838",
                FontSize = 18,
                Width = 35,
                Height = 35,
                Padding = new Thickness(0)
            };
            folderButton.Tapped += ImportFolderButton_Tapped;

            folderStackPanel.Children.Add(folderButton);
            folderStackPanel.Children.Add(ImportFolderTextBox);

            TextBlock contentText2 = new TextBlock
            {
                Margin = new Thickness(0, 20, 0, 0),
                TextWrapping = TextWrapping.WrapWholeWords,
                Text = loader.GetString("ImportDataContentDialog-Text2")
            };

            content.Children.Add(contentText);
            content.Children.Add(folderStackPanel);
            content.Children.Add(contentText2);

            ImportDataContentDialog.Content = content;

            return ImportDataContentDialog;
        }

        public static ContentDialog CreateStartMessageImportDataContentDialog()
        {
            ImportDataContentDialog = new ContentDialog
            {
                Title = loader.GetString("ImportDataContentDialog-Title"),
                PrimaryButtonText = loader.GetString("ImportDataContentDialog-PrimaryButton"),
                CloseButtonText = loader.GetString("Actions-Cancel"),
                DefaultButton = ContentDialogButton.Primary,
                IsPrimaryButtonEnabled = false,
                RequestedTheme = FileManager.GetRequestedTheme()
            };

            StackPanel content = new StackPanel
            {
                Orientation = Orientation.Vertical
            };

            TextBlock contentText = new TextBlock
            {
                Margin = new Thickness(0, 0, 0, 20),
                TextWrapping = TextWrapping.WrapWholeWords,
                Text = loader.GetString("ImportDataContentDialog-Text1")
            };

            // Create StackPanel with TextBox and Folder button
            StackPanel folderStackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };

            ImportFolderTextBox = new TextBox
            {
                IsReadOnly = true
            };

            Button folderButton = new Button
            {
                Style = MainPage.buttonRevealStyle,
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                Content = "\uE838",
                FontSize = 18,
                Width = 35,
                Height = 35,
                Padding = new Thickness(0)
            };
            folderButton.Tapped += ImportFolderButton_Tapped;

            folderStackPanel.Children.Add(folderButton);
            folderStackPanel.Children.Add(ImportFolderTextBox);

            TextBlock contentText2 = new TextBlock
            {
                Margin = new Thickness(0, 20, 0, 0),
                TextWrapping = TextWrapping.WrapWholeWords,
                Text = loader.GetString("StartMessageImportDataContentDialog-Text2")
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
        #endregion

        #region PlaySoundsSuccessively
        public static ContentDialog CreatePlaySoundsSuccessivelyContentDialog(List<Sound> sounds, DataTemplate itemTemplate, Style listViewItemStyle)
        {
            SoundsList.Clear();
            foreach (var sound in sounds)
                SoundsList.Add(sound);

            PlaySoundsSuccessivelyContentDialog = new ContentDialog
            {
                Title = loader.GetString("PlaySoundsSuccessivelyContentDialog-Title"),
                PrimaryButtonText = loader.GetString("PlaySoundsSuccessivelyContentDialog-PrimaryButton"),
                CloseButtonText = loader.GetString("Actions-Cancel"),
                DefaultButton = ContentDialogButton.Primary,
                IsPrimaryButtonEnabled = SoundsList.Count > 0,
                RequestedTheme = FileManager.GetRequestedTheme()
            };
            PlaySoundsSuccessivelyContentDialog.Opened += ContentDialog_Opened;
            PlaySoundsSuccessivelyContentDialog.Closed += ContentDialog_Closed;

            StackPanel content = new StackPanel
            {
                Orientation = Orientation.Vertical
            };

            SoundsListView = new ListView
            {
                ItemTemplate = itemTemplate,
                ItemsSource = SoundsList,
                SelectionMode = ListViewSelectionMode.None,
                Height = 300,
                ItemContainerStyle = listViewItemStyle,
                CanReorderItems = true,
                AllowDrop = true
            };

            RepeatsComboBox = new ComboBox
            {
                Margin = new Thickness(0, 10, 0, 0),
                IsEditable = true,
                Items =
                {
                    "1",
                    "2",
                    "3",
                    "4",
                    "5",
                    "6",
                    "7",
                    "8",
                    "9",
                    "10",
                    "15",
                    "20",
                    "25",
                    "30",
                    "40",
                    "50",
                    "100",
                    "∞"
                },
                SelectedIndex = 0
            };
            RepeatsComboBox.TextSubmitted += RepeatsComboBox_TextSubmitted;

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

        private static void RepeatsComboBox_TextSubmitted(ComboBox sender, ComboBoxTextSubmittedEventArgs args)
        {
            if (args.Text == "∞") return;
            if (!int.TryParse(args.Text, out int value) || value <= 0)
                RepeatsComboBox.Text = "1";
        }
        #endregion

        #region Logout
        public static ContentDialog CreateLogoutContentDialog()
        {
            LogoutContentDialog = new ContentDialog
            {
                Title = loader.GetString("Logout"),
                Content = loader.GetString("Account-LogoutMessage"),
                PrimaryButtonText = loader.GetString("Logout"),
                CloseButtonText = loader.GetString("Actions-Cancel"),
                DefaultButton = ContentDialogButton.Close,
                RequestedTheme = FileManager.GetRequestedTheme()
            };
            LogoutContentDialog.Opened += ContentDialog_Opened;
            LogoutContentDialog.Closed += ContentDialog_Closed;

            return LogoutContentDialog;
        }
        #endregion

        #region DownloadFile
        public static ContentDialog CreateDownloadFileContentDialog(string filename)
        {
            DownloadFileContentDialog = new ContentDialog
            {
                Title = string.Format(loader.GetString("DownloadFileContentDialog-Title"), filename),
                CloseButtonText = loader.GetString("ContentDialog-Cancel"),
                DefaultButton = ContentDialogButton.None,
                RequestedTheme = FileManager.GetRequestedTheme()
            };
            DownloadFileContentDialog.Opened += ContentDialog_Opened;
            DownloadFileContentDialog.Closed += ContentDialog_Closed;

            StackPanel content = new StackPanel
            {
                Margin = new Thickness(0, 30, 0, 0),
                Orientation = Orientation.Vertical
            };

            downloadFileProgressBar = new ProgressBar();
            content.Children.Add(downloadFileProgressBar);
            DownloadFileContentDialog.Content = content;

            return DownloadFileContentDialog;
        }
        #endregion

        #region DownloadFiles
        public static ContentDialog CreateDownloadFilesContentDialog(List<Sound> sounds, DataTemplate itemTemplate, Style itemStyle)
        {
            downloadingFilesSoundsList = sounds;

            DownloadFilesContentDialog = new ContentDialog
            {
                Title = loader.GetString("DownloadFilesContentDialog-Title"),
                CloseButtonText = loader.GetString("ContentDialog-Cancel"),
                DefaultButton = ContentDialogButton.None,
                RequestedTheme = FileManager.GetRequestedTheme()
            };
            DownloadFilesContentDialog.Opened += ContentDialog_Opened;
            DownloadFilesContentDialog.Closed += ContentDialog_Closed;

            ListView progressListView = new ListView
            {
                ItemTemplate = itemTemplate,
                ItemsSource = sounds,
                ItemContainerStyle = itemStyle,
                SelectionMode = ListViewSelectionMode.None
            };

            Grid containerGrid = new Grid
            {
                Width = 500
            };

            containerGrid.Children.Add(progressListView);
            DownloadFilesContentDialog.Content = containerGrid;

            return DownloadFilesContentDialog;
        }
        #endregion

        #region DownloadFileError
        public static ContentDialog CreateDownloadFileErrorContentDialog()
        {
            DownloadFileErrorContentDialog = new ContentDialog
            {
                Title = loader.GetString("DownloadFileErrorContentDialog-Title"),
                Content = loader.GetString("DownloadFileErrorContentDialog-Message"),
                CloseButtonText = loader.GetString("Actions-Close"),
                DefaultButton = ContentDialogButton.Close,
                RequestedTheme = FileManager.GetRequestedTheme()
            };
            DownloadFileErrorContentDialog.Opened += ContentDialog_Opened;
            DownloadFileErrorContentDialog.Closed += ContentDialog_Closed;

            return DownloadFileErrorContentDialog;
        }
        #endregion

        #region ExportSounds
        public static ContentDialog CreateExportSoundsContentDialog(List<Sound> sounds, DataTemplate itemTemplate, Style listViewItemStyle)
        {
            SoundsList.Clear();
            foreach (var sound in sounds)
                SoundsList.Add(sound);

            ExportSoundsContentDialog = new ContentDialog
            {
                Title = loader.GetString("ExportSoundsContentDialog-Title"),
                PrimaryButtonText = loader.GetString("Export"),
                CloseButtonText = loader.GetString("Actions-Cancel"),
                DefaultButton = ContentDialogButton.Primary,
                IsPrimaryButtonEnabled = false,
                RequestedTheme = FileManager.GetRequestedTheme()
            };
            ExportSoundsContentDialog.Opened += ContentDialog_Opened;
            ExportSoundsContentDialog.Closed += ContentDialog_Closed;

            if (SoundsList.Count == 0)
                ExportSoundsContentDialog.IsPrimaryButtonEnabled = false;

            StackPanel content = new StackPanel
            {
                Orientation = Orientation.Vertical
            };

            ExportSoundsListView = new ListView
            {
                ItemTemplate = itemTemplate,
                ItemsSource = SoundsList,
                SelectionMode = ListViewSelectionMode.None,
                Height = 300,
                ItemContainerStyle = listViewItemStyle,
                CanReorderItems = true,
                AllowDrop = true
            };

            // Create StackPanel with TextBox and Folder button
            StackPanel folderStackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 20, 0, 0)
            };

            ExportSoundsFolderTextBox = new TextBox
            {
                IsReadOnly = true
            };

            Button folderButton = new Button
            {
                Style = MainPage.buttonRevealStyle,
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                Content = "\uE838",
                FontSize = 18,
                Width = 35,
                Height = 35
            };
            folderButton.Tapped += ExportSoundsFolderButton_Tapped;

            ExportSoundsAsZipCheckBox = new CheckBox
            {
                Content = loader.GetString("SaveAsZip"),
                Margin = new Thickness(0, 20, 0, 0)
            };

            folderStackPanel.Children.Add(folderButton);
            folderStackPanel.Children.Add(ExportSoundsFolderTextBox);

            content.Children.Add(ExportSoundsListView);
            content.Children.Add(folderStackPanel);
            content.Children.Add(ExportSoundsAsZipCheckBox);

            ExportSoundsContentDialog.Content = content;
            return ExportSoundsContentDialog;
        }

        private async static void ExportSoundsFolderButton_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            var folderPicker = new FolderPicker
            {
                SuggestedStartLocation = PickerLocationId.Downloads
            };

            folderPicker.FileTypeFilter.Add("*");
            StorageFolder folder = await folderPicker.PickSingleFolderAsync();

            if (folder != null)
            {
                StorageApplicationPermissions.FutureAccessList.AddOrReplace("PickedFolderToken", folder);

                // Set TextBox text and StorageFolder variable and make primary button clickable
                ExportSoundsFolder = folder;
                ExportSoundsFolderTextBox.Text = folder.Path;
                if(SoundsList.Count > 0)
                    ExportSoundsContentDialog.IsPrimaryButtonEnabled = true;
            }
        }
        #endregion

        #region SetCategories
        public static ContentDialog CreateSetCategoriesContentDialog(List<Sound> sounds)
        {
            if (sounds.Count == 0) return null;

            string title = string.Format(loader.GetString("SetCategoryForMultipleSoundsContentDialog-Title"), sounds.Count);
            if (sounds.Count == 1) title = string.Format(loader.GetString("SetCategoryContentDialog-Title"), sounds[0].Name);

            SetCategoryContentDialog = new ContentDialog
            {
                Title = title,
                DefaultButton = ContentDialogButton.Primary,
                RequestedTheme = FileManager.GetRequestedTheme()
            };
            SetCategoryContentDialog.Opened += ContentDialog_Opened;
            SetCategoryContentDialog.Closed += ContentDialog_Closed;

            StackPanel content = new StackPanel
            {
                Orientation = Orientation.Vertical
            };

            CategoriesTreeView = new WinUI.TreeView
            {
                Height = 300,
                SelectionMode = WinUI.TreeViewSelectionMode.Multiple,
                CanDrag = false,
                CanDragItems = false,
                CanReorderItems = false,
                AllowDrop = false
            };

            // Get all categories
            List<Category> categories = new List<Category>();
            for (int i = 1; i < FileManager.itemViewHolder.Categories.Count; i++)
                categories.Add(FileManager.itemViewHolder.Categories[i]);

            // Find the intersection of the categories of all sounds
            List<Guid> soundCategories = new List<Guid>();
            foreach(var category in sounds.First().Categories)
                if (sounds.TrueForAll(s => s.Categories.Exists(c => c.Uuid == category.Uuid)))
                    soundCategories.Add(category.Uuid);

            // Create the nodes and add them to the tree view
            List<CustomTreeViewNode> selectedNodes = new List<CustomTreeViewNode>();
            foreach (var node in FileManager.CreateTreeViewNodesFromCategories(categories, selectedNodes, soundCategories))
                CategoriesTreeView.RootNodes.Add(node);

            foreach (var node in selectedNodes)
                CategoriesTreeView.SelectedNodes.Add(node);

            if(categories.Count > 0)
            {
                content.Children.Add(CategoriesTreeView);

                SetCategoryContentDialog.PrimaryButtonText = loader.GetString("Actions-Save");
                SetCategoryContentDialog.CloseButtonText = loader.GetString("Actions-Cancel");
            }
            else
            {
                TextBlock noCategoriesTextBlock = new TextBlock
                {
                    Text = loader.GetString("SetCategoryContentDialog-NoCategoriesText")
                };
                content.Children.Add(noCategoriesTextBlock);

                SetCategoryContentDialog.CloseButtonText = loader.GetString("Actions-Close");
            }

            SetCategoryContentDialog.Content = content;
            return SetCategoryContentDialog;
        }
        #endregion

        #region Properties
        public static async Task<ContentDialog> CreatePropertiesContentDialog(Sound sound)
        {
            selectedPropertiesSound = sound;
            propertiesDefaultVolumeChanged = false;
            propertiesDefaultMutedChanged = false;

            PropertiesContentDialog = new ContentDialog
            {
                Title = loader.GetString("SoundItemOptionsFlyout-Properties"),
                CloseButtonText = loader.GetString("Actions-Close"),
                DefaultButton = ContentDialogButton.Close,
                RequestedTheme = FileManager.GetRequestedTheme()
            };
            PropertiesContentDialog.Opened += ContentDialog_Opened;
            PropertiesContentDialog.Closed += ContentDialog_Closed;
            PropertiesContentDialog.CloseButtonClick += PropertiesContentDialog_CloseButtonClick;

            int fontSize = 15;
            int row = 0;
            int contentGridWidth = 500;
            int leftColumnWidth = 210;
            int rightColumnWidth = contentGridWidth - leftColumnWidth;

            Grid contentGrid = new Grid { Width = contentGridWidth };

            // Create the columns
            var firstColumn = new ColumnDefinition { Width = new GridLength(leftColumnWidth, GridUnitType.Pixel) };
            var secondColumn = new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) };

            contentGrid.ColumnDefinitions.Add(firstColumn);
            contentGrid.ColumnDefinitions.Add(secondColumn);

            #region Name
            // Add the row
            var nameRow = new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) };
            contentGrid.RowDefinitions.Add(nameRow);
            
            StackPanel nameHeaderStackPanel = GenerateTableCell(
                row,
                0,
                loader.GetString("PropertiesContentDialog-Name"),
                fontSize,
                false,
                null
            );

            StackPanel nameDataStackPanel = GenerateTableCell(
                row,
                1,
                sound.Name,
                fontSize,
                true,
                null
            );

            row++;
            contentGrid.Children.Add(nameHeaderStackPanel);
            contentGrid.Children.Add(nameDataStackPanel);
            #endregion

            #region File type
            string audioFileType = sound.GetAudioFileExtension();
            if(audioFileType != null)
            {
                // Add the row
                var fileTypeRow = new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) };
                contentGrid.RowDefinitions.Add(fileTypeRow);

                StackPanel fileTypeHeaderStackPanel = GenerateTableCell(
                    row,
                    0,
                    loader.GetString("PropertiesContentDialog-FileType"),
                    fontSize,
                    false,
                    null
                );

                StackPanel fileTypeDataStackPanel = GenerateTableCell(
                    row,
                    1,
                    audioFileType,
                    fontSize,
                    true,
                    null
                );

                row++;
                contentGrid.Children.Add(fileTypeHeaderStackPanel);
                contentGrid.Children.Add(fileTypeDataStackPanel);
            }
            #endregion

            #region Image file type
            string imageFileType = sound.GetImageFileExtension();
            if(imageFileType != null)
            {
                // Add the row
                var imageFileTypeRow = new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) };
                contentGrid.RowDefinitions.Add(imageFileTypeRow);

                StackPanel imageFileTypeHeaderStackPanel = GenerateTableCell(
                    row,
                    0,
                    loader.GetString("PropertiesContentDialog-ImageFileType"),
                    fontSize,
                    false,
                    null
                );

                StackPanel imageFileTypeDataStackPanel = GenerateTableCell(
                    row,
                    1,
                    imageFileType,
                    fontSize,
                    true,
                    null
                );

                row++;
                contentGrid.Children.Add(imageFileTypeHeaderStackPanel);
                contentGrid.Children.Add(imageFileTypeDataStackPanel);
            }
            #endregion

            #region Size
            var audioFile = sound.AudioFile;
            if (audioFile != null)
            {
                // Add the row
                var sizeRow = new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) };
                contentGrid.RowDefinitions.Add(sizeRow);

                StackPanel sizeHeaderStackPanel = GenerateTableCell(
                    row,
                    0,
                    loader.GetString("PropertiesContentDialog-Size"),
                    fontSize,
                    false,
                    null
                );

                StackPanel sizeDataStackPanel = GenerateTableCell(
                    row,
                    1,
                    FileManager.GetFormattedSize(await FileManager.GetFileSizeAsync(audioFile)),
                    fontSize,
                    true,
                    null
                );

                row++;
                contentGrid.Children.Add(sizeHeaderStackPanel);
                contentGrid.Children.Add(sizeDataStackPanel);
            }
            #endregion

            #region Image size
            var imageFile = sound.ImageFile;
            if(imageFile != null)
            {
                // Add the row
                var imageSizeRow = new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) };
                contentGrid.RowDefinitions.Add(imageSizeRow);

                StackPanel imageSizeHeaderStackPanel = GenerateTableCell(
                    row,
                    0,
                    loader.GetString("PropertiesContentDialog-ImageSize"),
                    fontSize,
                    false,
                    null
                );

                StackPanel imageSizeDataStackPanel = GenerateTableCell(
                    row,
                    1,
                    FileManager.GetFormattedSize(await FileManager.GetFileSizeAsync(imageFile)),
                    fontSize,
                    true,
                    null
                );

                row++;
                contentGrid.Children.Add(imageSizeHeaderStackPanel);
                contentGrid.Children.Add(imageSizeDataStackPanel);
            }
            #endregion

            #region Volume
            // Add the row
            var volumeRow = new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) };
            contentGrid.RowDefinitions.Add(volumeRow);

            StackPanel volumeHeaderStackPanel = GenerateTableCell(
                row,
                0,
                loader.GetString("PropertiesContentDialog-Volume"),
                fontSize,
                false,
                new Thickness(0, 16, 0, 0)
            );

            StackPanel volumeDataStackPanel = new StackPanel();
            Grid.SetRow(volumeDataStackPanel, row);
            Grid.SetColumn(volumeDataStackPanel, 1);

            RelativePanel volumeRelativePanel = new RelativePanel();
            volumeDataStackPanel.Children.Add(volumeRelativePanel);

            PropertiesVolumeControl = new VolumeControl
            {
                Value = sound.DefaultVolume,
                Muted = sound.DefaultMuted,
                Margin = new Thickness(8, 10, 0, 0)
            };
            PropertiesVolumeControl.ValueChanged += VolumeControl_ValueChanged;
            PropertiesVolumeControl.MuteChanged += VolumeControl_MuteChanged;

            volumeRelativePanel.Children.Add(PropertiesVolumeControl);

            row++;
            contentGrid.Children.Add(volumeHeaderStackPanel);
            contentGrid.Children.Add(volumeDataStackPanel);
            #endregion

            #region Playback speed
            // Add the row
            var playbackSpeedRow = new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) };
            contentGrid.RowDefinitions.Add(playbackSpeedRow);

            StackPanel playbackSpeedHeaderStackPanel = GenerateTableCell(
                row,
                0,
                loader.GetString("PropertiesContentDialog-PlaybackSpeed"),
                fontSize,
                false,
                new Thickness(0, 16, 0, 0)
            );

            RelativePanel playbackSpeedDataRelativePanel = new RelativePanel { Margin = new Thickness(0, 10, 0, 0) };
            Grid.SetRow(playbackSpeedDataRelativePanel, row);
            Grid.SetColumn(playbackSpeedDataRelativePanel, 1);

            // Create the ComboBox with the playback speed items
            PlaybackSpeedComboBox = new ComboBox();
            PlaybackSpeedComboBox.Items.Add("0.25x");
            PlaybackSpeedComboBox.Items.Add("0.5x");
            PlaybackSpeedComboBox.Items.Add("0.75x");
            PlaybackSpeedComboBox.Items.Add("1.0x");
            PlaybackSpeedComboBox.Items.Add("1.25x");
            PlaybackSpeedComboBox.Items.Add("1.5x");
            PlaybackSpeedComboBox.Items.Add("1.75x");
            PlaybackSpeedComboBox.Items.Add("2.0x");
            PlaybackSpeedComboBox.SelectionChanged += PlaybackSpeedComboBox_SelectionChanged;

            // Select the correct item
            switch (selectedPropertiesSound.DefaultPlaybackSpeed)
            {
                case 25:
                    PlaybackSpeedComboBox.SelectedIndex = 0;
                    break;
                case 50:
                    PlaybackSpeedComboBox.SelectedIndex = 1;
                    break;
                case 75:
                    PlaybackSpeedComboBox.SelectedIndex = 2;
                    break;
                case 125:
                    PlaybackSpeedComboBox.SelectedIndex = 4;
                    break;
                case 150:
                    PlaybackSpeedComboBox.SelectedIndex = 5;
                    break;
                case 175:
                    PlaybackSpeedComboBox.SelectedIndex = 6;
                    break;
                case 200:
                    PlaybackSpeedComboBox.SelectedIndex = 7;
                    break;
                default:
                    PlaybackSpeedComboBox.SelectedIndex = 3;
                    break;
            }

            RelativePanel.SetAlignVerticalCenterWithPanel(PlaybackSpeedComboBox, true);
            playbackSpeedDataRelativePanel.Children.Add(PlaybackSpeedComboBox);

            row++;
            contentGrid.Children.Add(playbackSpeedHeaderStackPanel);
            contentGrid.Children.Add(playbackSpeedDataRelativePanel);
            #endregion

            #region Hotkeys
            if (ApiInformation.IsApiContractPresent("Windows.ApplicationModel.FullTrustAppContract", 1, 0))
            {
                // Add the row
                var hotkeysRow = new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) };
                contentGrid.RowDefinitions.Add(hotkeysRow);

                StackPanel hotkeysStackPanel = GenerateTableCell(
                    row,
                    0,
                    loader.GetString("PropertiesContentDialog-Hotkeys"),
                    fontSize,
                    false,
                    new Thickness(0, 16, 0, 0)
                );

                StackPanel hotkeysDataStackPanel = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    Margin = new Thickness(0, 10, 0, 0)
                };
                Grid.SetRow(hotkeysDataStackPanel, row);
                Grid.SetColumn(hotkeysDataStackPanel, 1);

                // Hotkey button list
                HotkeyItem addHotkeyItem = new HotkeyItem();
                addHotkeyItem.HotkeyAdded += AddHotkeyItem_HotkeyAdded;

                PropertiesDialogHotkeys.Add(new ObservableCollection<HotkeyItem>());
                PropertiesDialogHotkeys.Last().Add(addHotkeyItem);

                WinUI.ItemsRepeater hotkeyItemsRepeater = new WinUI.ItemsRepeater
                {
                    ItemTemplate = MainPage.hotkeyButtonTemplate,
                    ItemsSource = PropertiesDialogHotkeys.Last(),
                    Layout = new WrapLayout { HorizontalSpacing = 5, VerticalSpacing = 5 },
                    Width = rightColumnWidth
                };

                ScrollViewer hotkeyItemsScrollViewer = new ScrollViewer { MaxHeight = 117.5 };
                hotkeyItemsScrollViewer.Content = hotkeyItemsRepeater;

                foreach (Hotkey hotkey in selectedPropertiesSound.Hotkeys)
                {
                    if (hotkey.IsEmpty())
                        continue;

                    HotkeyItem hotkeyItem = new HotkeyItem(hotkey);
                    hotkeyItem.RemoveHotkey += HotkeyItem_RemoveHotkey;
                    PropertiesDialogHotkeys.Last().Add(hotkeyItem);
                }

                davPlusHotkeyInfoStackPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    MaxWidth = rightColumnWidth,
                    Margin = new Thickness(0, 6, 0, 0)
                };

                // Set the correct visibility for the dav Plus message
                UpdateDavPlusHotkeyInfoStackPanelVisibility();

                TextBlock davPlusHotkeyInfoTextBlock = new TextBlock
                {
                    Text = loader.GetString("PropertiesContentDialog-HotkeysRestricted"),
                    FontSize = 13,
                    Foreground = new SolidColorBrush(Colors.Red),
                    VerticalAlignment = VerticalAlignment.Center,
                    TextWrapping = TextWrapping.WrapWholeWords,
                    MaxWidth = rightColumnWidth - 40
                };

                Button infoButton = new Button
                {
                    Style = MainPage.buttonRevealStyle,
                    Content = "\uE946",
                    FontFamily = new FontFamily("Segoe MDL2 Assets"),
                    FontSize = 14,
                    Width = 32,
                    Height = 32,
                    CornerRadius = new CornerRadius(18),
                    Padding = new Thickness(1, 0, 0, 0),
                    Background = new SolidColorBrush(Colors.Transparent),
                    Margin = new Thickness(10, 0, 0, 0)
                };

                TextBlock infoButtonTextBlock = new TextBlock
                {
                    Text = loader.GetString("DavPlusHotkeysContentDialog-Content"),
                    TextWrapping = TextWrapping.WrapWholeWords,
                    MaxWidth = 300
                };

                Flyout infoButtonFlyout = new Flyout { Content = infoButtonTextBlock };
                infoButton.Flyout = infoButtonFlyout;

                davPlusHotkeyInfoStackPanel.Children.Add(davPlusHotkeyInfoTextBlock);
                davPlusHotkeyInfoStackPanel.Children.Add(infoButton);

                hotkeysDataStackPanel.Children.Add(hotkeyItemsScrollViewer);
                hotkeysDataStackPanel.Children.Add(davPlusHotkeyInfoStackPanel);

                row++;
                contentGrid.Children.Add(hotkeysStackPanel);
                contentGrid.Children.Add(hotkeysDataStackPanel);
            }
            #endregion

            PropertiesContentDialog.Content = contentGrid;

            return PropertiesContentDialog;
        }

        private static void VolumeControl_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            propertiesDefaultVolumeChanged = true;
        }

        private static void VolumeControl_MuteChanged(object sender, bool e)
        {
            propertiesDefaultMutedChanged = true;
        }

        private static async void PlaybackSpeedComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int selectedPlaybackSpeed = 100;

            switch (PlaybackSpeedComboBox.SelectedIndex)
            {
                case 0:
                    selectedPlaybackSpeed = 25;
                    break;
                case 1:
                    selectedPlaybackSpeed = 50;
                    break;
                case 2:
                    selectedPlaybackSpeed = 75;
                    break;
                case 3:
                    selectedPlaybackSpeed = 100;
                    break;
                case 4:
                    selectedPlaybackSpeed = 125;
                    break;
                case 5:
                    selectedPlaybackSpeed = 150;
                    break;
                case 6:
                    selectedPlaybackSpeed = 175;
                    break;
                case 7:
                    selectedPlaybackSpeed = 200;
                    break;
            }

            selectedPropertiesSound.DefaultPlaybackSpeed = selectedPlaybackSpeed;

            await FileManager.SetDefaultPlaybackSpeedOfSoundAsync(selectedPropertiesSound.Uuid, selectedPlaybackSpeed);
        }

        private static void UpdateDavPlusHotkeyInfoStackPanelVisibility()
        {
            if (Dav.IsLoggedIn && Dav.User.Plan > 0)
                davPlusHotkeyInfoStackPanel.Visibility = Visibility.Collapsed;
            else
                davPlusHotkeyInfoStackPanel.Visibility = PropertiesDialogHotkeys.Last().Count > 1 ? Visibility.Visible : Visibility.Collapsed;
        }

        private static async void AddHotkeyItem_HotkeyAdded(object sender, HotkeyEventArgs e)
        {
            // Add the new hotkey to the sound and list of hotkeys
            selectedPropertiesSound.Hotkeys.Add(e.Hotkey);
            HotkeyItem newHotkeyItem = new HotkeyItem(e.Hotkey);
            newHotkeyItem.RemoveHotkey += HotkeyItem_RemoveHotkey;
            PropertiesDialogHotkeys.Last().Add(newHotkeyItem);

            // Update the visibility of the dav Plus info text
            UpdateDavPlusHotkeyInfoStackPanelVisibility();

            // Save the hotkeys of the sound
            await FileManager.SetHotkeysOfSoundAsync(selectedPropertiesSound.Uuid, selectedPropertiesSound.Hotkeys);

            // Update the Hotkey process with the new hotkeys
            await FileManager.StartHotkeyProcess();
        }

        private static async void HotkeyItem_RemoveHotkey(object sender, HotkeyEventArgs e)
        {
            // Remove the hotkey from the list of hotkeys
            int index = selectedPropertiesSound.Hotkeys.FindIndex(h => h.Modifiers == e.Hotkey.Modifiers && h.Key == e.Hotkey.Key);
            if (index != -1) selectedPropertiesSound.Hotkeys.RemoveAt(index);

            PropertiesDialogHotkeys.Last().Remove((HotkeyItem)sender);

            // Update the visibility of the dav Plus info text
            UpdateDavPlusHotkeyInfoStackPanelVisibility();

            // Save the hotkeys of the sound
            await FileManager.SetHotkeysOfSoundAsync(selectedPropertiesSound.Uuid, selectedPropertiesSound.Hotkeys);

            // Update the Hotkey process with the new hotkeys
            await FileManager.StartHotkeyProcess();
        }

        private static async void PropertiesContentDialog_CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (!propertiesDefaultVolumeChanged && !propertiesDefaultMutedChanged) return;

            // Set the new values and update the DefaultVolume and DefaultMuted of all Sounds in all lists in ItemViewHolder
            selectedPropertiesSound.DefaultVolume = PropertiesVolumeControl.Value;
            selectedPropertiesSound.DefaultMuted = PropertiesVolumeControl.Muted;
            await FileManager.ReloadSound(selectedPropertiesSound);

            // Update the sound in the database
            await FileManager.SetDefaultVolumeOfSoundAsync(selectedPropertiesSound.Uuid, PropertiesVolumeControl.Value, PropertiesVolumeControl.Muted);
        }

        private static StackPanel GenerateTableCell(int row, int column, string text, int fontSize, bool isTextSelectionEnabled, Thickness? margin)
        {
            StackPanel contentStackPanel = new StackPanel();
            Grid.SetRow(contentStackPanel, row);
            Grid.SetColumn(contentStackPanel, column);

            TextBlock contentTextBlock = new TextBlock
            {
                Text = text,
                Margin = margin ?? new Thickness(0, 10, 0, 0),
                FontSize = fontSize,
                TextWrapping = TextWrapping.WrapWholeWords,
                IsTextSelectionEnabled = isTextSelectionEnabled
            };

            contentStackPanel.Children.Add(contentTextBlock);
            return contentStackPanel;
        }
        #endregion

        #region DavPlusHotkeys
        public static ContentDialog CreateDavPlusHotkeysContentDialog()
        {
            DavPlusHotkeysContentDialog = new ContentDialog
            {
                Title = loader.GetString("DavPlusContentDialog-Title"),
                Content = loader.GetString("DavPlusHotkeysContentDialog-Content"),
                PrimaryButtonText = loader.GetString("Actions-LearnMore"),
                CloseButtonText = loader.GetString("Actions-Close"),
                DefaultButton = ContentDialogButton.Primary,
                RequestedTheme = FileManager.GetRequestedTheme()
            };
            DavPlusHotkeysContentDialog.Opened += ContentDialog_Opened;
            DavPlusHotkeysContentDialog.Closed += ContentDialog_Closed;

            return DavPlusHotkeysContentDialog;
        }
        #endregion

        #region DavPlusOutputDevice
        public static ContentDialog CreateDavPlusOutputDeviceContentDialog()
        {
            DavPlusOutputDeviceContentDialog = new ContentDialog
            {
                Title = loader.GetString("DavPlusContentDialog-Title"),
                Content = loader.GetString("DavPlusOutputDeviceContentDialog-Content"),
                PrimaryButtonText = loader.GetString("Actions-LearnMore"),
                CloseButtonText = loader.GetString("Actions-Close"),
                DefaultButton = ContentDialogButton.Primary,
                RequestedTheme = FileManager.GetRequestedTheme()
            };
            DavPlusOutputDeviceContentDialog.Opened += ContentDialog_Opened;
            DavPlusOutputDeviceContentDialog.Closed += ContentDialog_Closed;

            return DavPlusOutputDeviceContentDialog;
        }
        #endregion
    }
}
