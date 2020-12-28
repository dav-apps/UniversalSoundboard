using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using UniversalSoundboard.Components;
using UniversalSoundBoard.DataAccess;
using UniversalSoundBoard.Models;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using WinUI = Microsoft.UI.Xaml.Controls;

namespace UniversalSoundBoard.Common
{
    public class ContentDialogs
    {
        #region Variables
        private static readonly ResourceLoader loader = new ResourceLoader();

        private static Sound selectedPropertiesSound;
        private static VolumeControl PropertiesVolumeControl;
        private static bool propertiesDefaultVolumeChanged = false;
        private static bool propertiesDefaultMutedChanged = false;

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
        public static ContentDialog DownloadFilesContentDialog;
        public static ContentDialog DownloadFileErrorContentDialog;
        public static ContentDialog ExportSoundsContentDialog;
        public static ContentDialog SetCategoryContentDialog;
        public static ContentDialog CategoryOrderContentDialog;
        public static ContentDialog PropertiesContentDialog;
        #endregion

        #region NewCategory
        public static ContentDialog CreateNewCategoryContentDialog(Guid parentUuid)
        {
            NewCategoryContentDialog = new ContentDialog
            {
                Title = loader.GetString("NewCategoryContentDialog-Title"),
                PrimaryButtonText = loader.GetString("NewCategoryContentDialog-PrimaryButton"),
                SecondaryButtonText = loader.GetString("ContentDialog-Cancel"),
                IsPrimaryButtonEnabled = false,
                RequestedTheme = FileManager.GetRequestedTheme()
            };

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
                SecondaryButtonText = loader.GetString("ContentDialog-Cancel"),
                RequestedTheme = FileManager.GetRequestedTheme()
            };

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
                SecondaryButtonText = loader.GetString("ContentDialog-Cancel"),
                RequestedTheme = FileManager.GetRequestedTheme()
            };

            return DeleteCategoryContentDialog;
        }
        #endregion

        #region RenameSound
        public static ContentDialog CreateRenameSoundContentDialog(Sound sound)
        {
            RenameSoundContentDialog = new ContentDialog
            {
                Title = loader.GetString("RenameSoundContentDialog-Title"),
                PrimaryButtonText = loader.GetString("RenameSoundContentDialog-PrimaryButton"),
                SecondaryButtonText = loader.GetString("ContentDialog-Cancel"),
                RequestedTheme = FileManager.GetRequestedTheme()
            };

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
                SecondaryButtonText = loader.GetString("ContentDialog-Cancel"),
                RequestedTheme = FileManager.GetRequestedTheme()
            };

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
                SecondaryButtonText = loader.GetString("ContentDialog-Cancel"),
                RequestedTheme = FileManager.GetRequestedTheme()
            };

            return DeleteSoundsContentDialog;
        }
        #endregion

        #region ExportData
        public static ContentDialog CreateExportDataContentDialog(Style buttonRevealStyle)
        {
            ExportDataContentDialog = new ContentDialog
            {
                Title = loader.GetString("ExportDataContentDialog-Title"),
                PrimaryButtonText = loader.GetString("ExportDataContentDialog-PrimaryButton"),
                SecondaryButtonText = loader.GetString("ContentDialog-Cancel"),
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
                Style = buttonRevealStyle,
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                Content = "\uE838",
                FontSize = 18,
                Width = 35,
                Height = 35
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
        public static ContentDialog CreateImportDataContentDialog(Style buttonRevealStyle)
        {
            ImportDataContentDialog = new ContentDialog
            {
                Title = loader.GetString("ImportDataContentDialog-Title"),
                PrimaryButtonText = loader.GetString("ImportDataContentDialog-PrimaryButton"),
                SecondaryButtonText = loader.GetString("ContentDialog-Cancel"),
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
                Style = buttonRevealStyle,
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

        public static ContentDialog CreateStartMessageImportDataContentDialog(Style buttonRevealStyle)
        {
            ImportDataContentDialog = new ContentDialog
            {
                Title = loader.GetString("ImportDataContentDialog-Title"),
                PrimaryButtonText = loader.GetString("ImportDataContentDialog-PrimaryButton"),
                SecondaryButtonText = loader.GetString("ContentDialog-Cancel"),
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
                Style = buttonRevealStyle,
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
                SecondaryButtonText = loader.GetString("ContentDialog-Cancel"),
                IsPrimaryButtonEnabled = SoundsList.Count > 0,
                RequestedTheme = FileManager.GetRequestedTheme()
            };

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
                SecondaryButtonText = loader.GetString("ContentDialog-Cancel"),
                RequestedTheme = FileManager.GetRequestedTheme()
            };

            return LogoutContentDialog;
        }
        #endregion

        #region DownloadFile
        public static ContentDialog CreateDownloadFileContentDialog(string filename)
        {
            DownloadFileContentDialog = new ContentDialog
            {
                Title = string.Format(loader.GetString("DownloadFileContentDialog-Title"), filename),
                SecondaryButtonText = loader.GetString("ContentDialog-Cancel"),
                RequestedTheme = FileManager.GetRequestedTheme()
            };

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
                SecondaryButtonText = loader.GetString("ContentDialog-Cancel"),
                RequestedTheme = FileManager.GetRequestedTheme()
            };

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
                CloseButtonText = loader.GetString("ContentDialog-Close"),
                RequestedTheme = FileManager.GetRequestedTheme()
            };

            return DownloadFileErrorContentDialog;
        }
        #endregion

        #region ExportSounds
        public static ContentDialog CreateExportSoundsContentDialog(List<Sound> sounds, DataTemplate itemTemplate, Style listViewItemStyle, Style buttonRevealStyle)
        {
            SoundsList.Clear();
            foreach (var sound in sounds)
                SoundsList.Add(sound);

            ExportSoundsContentDialog = new ContentDialog
            {
                Title = loader.GetString("ExportSoundsContentDialog-Title"),
                PrimaryButtonText = loader.GetString("Export"),
                SecondaryButtonText = loader.GetString("ContentDialog-Cancel"),
                IsPrimaryButtonEnabled = false,
                RequestedTheme = FileManager.GetRequestedTheme()
            };

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
                Style = buttonRevealStyle,
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
                RequestedTheme = FileManager.GetRequestedTheme()
            };

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

                SetCategoryContentDialog.PrimaryButtonText = loader.GetString("ContentDialog-Save");
                SetCategoryContentDialog.SecondaryButtonText = loader.GetString("ContentDialog-Cancel");
            }
            else
            {
                TextBlock noCategoriesTextBlock = new TextBlock
                {
                    Text = loader.GetString("SetCategoryContentDialog-NoCategoriesText")
                };
                content.Children.Add(noCategoriesTextBlock);

                SetCategoryContentDialog.SecondaryButtonText = loader.GetString("ContentDialog-Close");
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
                CloseButtonText = loader.GetString("ContentDialog-Close"),
                RequestedTheme = FileManager.GetRequestedTheme()
            };

            PropertiesContentDialog.CloseButtonClick += PropertiesContentDialog_CloseButtonClick;

            int fontSize = 15;
            int row = 0;
            Grid contentGrid = new Grid
            {
                Width = 500
            };

            // Create the columns
            var firstColumn = new ColumnDefinition { Width = new GridLength(160, GridUnitType.Pixel) };
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
                new Thickness(0, 17, 0, 0)
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
                Margin = new Thickness(0, 10, 0, 0)
            };
            PropertiesVolumeControl.ValueChanged += VolumeControl_ValueChanged;
            PropertiesVolumeControl.MuteChanged += VolumeControl_MuteChanged;

            volumeRelativePanel.Children.Add(PropertiesVolumeControl);

            contentGrid.Children.Add(volumeHeaderStackPanel);
            contentGrid.Children.Add(volumeDataStackPanel);
            #endregion

            PropertiesContentDialog.Content = contentGrid;

            return PropertiesContentDialog;
        }

        private static void VolumeControl_ValueChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            propertiesDefaultVolumeChanged = true;
        }

        private static void VolumeControl_MuteChanged(object sender, bool e)
        {
            propertiesDefaultMutedChanged = true;
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
    }
}
