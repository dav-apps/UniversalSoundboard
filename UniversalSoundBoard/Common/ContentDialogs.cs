using DotNetTools.SharpGrabber;
using Google.Apis.YouTube.v3.Data;
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
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UniversalSoundboard.Common
{
    public class ContentDialogs
    {
        #region Variables
        private static readonly ResourceLoader loader = new ResourceLoader();

        private static List<KeyValuePair<AppWindowType, ContentDialog>> contentDialogQueue = new List<KeyValuePair<AppWindowType, ContentDialog>>();

        private static bool _contentDialogVisible = false;
        public static bool ContentDialogVisible { get => _contentDialogVisible; }

        public static ListView AddSoundsListView;
        public static ObservableCollection<SoundFileItem> AddSoundsSelectedFiles;
        public static TextBlock NoFilesSelectedTextBlock;
        public static TextBox DownloadSoundsUrlTextBox;
        public static StackPanel DownloadSoundsLoadingMessageStackPanel;
        public static Grid DownloadSoundsYoutubeInfoGrid;
        public static Image DownloadSoundsYoutubeInfoImage;
        public static TextBlock DownloadSoundsYoutubeInfoTextBlock;
        public static StackPanel DownloadSoundsYoutubeInfoDownloadPlaylistStackPanel;
        public static CheckBox DownloadSoundsYoutubeInfoDownloadPlaylistCheckbox;
        public static CheckBox DownloadSoundsYoutubeInfoCreateCategoryForPlaylistCheckbox;
        public static TextBlock DownloadSoundsAudioFileInfoTextBlock;
        public static GrabResult DownloadSoundsGrabResult;
        public static PlaylistItemListResponse DownloadSoundsPlaylistItemListResponse;
        public static string DownloadSoundsPlaylistId = "";
        public static string DownloadSoundsPlaylistTitle = "";
        public static DownloadSoundsResultType DownloadSoundsResult = DownloadSoundsResultType.None;
        public static string DownloadSoundsAudioFileName = "";
        public static string DownloadSoundsAudioFileType = "";
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
        public static List<Sound> downloadingFilesSoundsList = new List<Sound>();
        public static ListView ExportSoundsListView;
        public static TextBox ExportSoundsFolderTextBox;
        public static CheckBox ExportSoundsAsZipCheckBox;
        public static StorageFolder ExportSoundsFolder;
        public static ListView CategoriesListView;
        public static ComboBox DefaultSoundSettingsRepetitionsComboBox;
        public static ComboBox PlaybackSpeedComboBox;
        public static StackPanel davPlusHotkeyInfoStackPanel;
        public static TextBox RecordedSoundNameTextBox;
        public static ContentDialog AddSoundsContentDialog;
        public static ContentDialog DownloadSoundsContentDialog;
        public static ContentDialog NewCategoryContentDialog;
        public static ContentDialog EditCategoryContentDialog;
        public static ContentDialog DeleteCategoryContentDialog;
        public static ContentDialog AddSoundErrorContentDialog;
        public static ContentDialog AddSoundsErrorContentDialog;
        public static ContentDialog DownloadSoundsErrorContentDialog;
        public static ContentDialog RenameSoundContentDialog;
        public static ContentDialog DeleteSoundContentDialog;
        public static ContentDialog DeleteSoundsContentDialog;
        public static ContentDialog ExportDataContentDialog;
        public static ContentDialog ImportDataContentDialog;
        public static ContentDialog PlaySoundsSuccessivelyContentDialog;
        public static ContentDialog LogoutContentDialog;
        public static ContentDialog DownloadFilesContentDialog;
        public static ContentDialog DownloadFileErrorContentDialog;
        public static ContentDialog ExportSoundsContentDialog;
        public static ContentDialog SetCategoryContentDialog;
        public static ContentDialog CategoryOrderContentDialog;
        public static ContentDialog PropertiesContentDialog;
        public static ContentDialog DefaultSoundSettingsContentDialog;
        public static ContentDialog DavPlusHotkeysContentDialog;
        public static ContentDialog DavPlusOutputDeviceContentDialog;
        public static ContentDialog UpgradeErrorContentDialog;
        public static ContentDialog NoAudioDeviceContentDialog;
        public static ContentDialog AddRecordedSoundToSoundboardContentDialog;
        public static ContentDialog RemoveRecordedSoundContentDialog;
        public static ContentDialog SoundRecorderCloseWarningContentDialog;
        #endregion

        #region General methods
        public static async Task ShowContentDialogAsync(ContentDialog contentDialog, AppWindowType appWindowType = AppWindowType.Main)
        {
            contentDialog.Closed += async (e, s) =>
            {
                int i = contentDialogQueue.FindIndex(pair => pair.Value == contentDialog);

                if (i == -1)
                {
                    _contentDialogVisible = false;
                }
                else
                {
                    contentDialogQueue.RemoveAt(i);

                    if (contentDialogQueue.Count > 0)
                    {
                        // Show the next content dialog
                        _contentDialogVisible = true;
                        await contentDialogQueue.First().Value.ShowAsync();
                    }
                    else
                    {
                        _contentDialogVisible = false;
                    }
                }
            };

            contentDialogQueue.Add(new KeyValuePair<AppWindowType, ContentDialog>(appWindowType, contentDialog));

            if (appWindowType == AppWindowType.SoundRecorder && MainPage.soundRecorderAppWindowContentFrame != null)
                contentDialog.XamlRoot = MainPage.soundRecorderAppWindowContentFrame.XamlRoot;

            if (!_contentDialogVisible)
            {
                _contentDialogVisible = true;
                await contentDialog.ShowAsync();
            }
        }
        #endregion

        #region AddSounds
        public static ContentDialog CreateAddSoundsContentDialog(DataTemplate itemTemplate, List<StorageFile> selectedFiles)
        {
            AddSoundsContentDialog = new ContentDialog
            {
                Title = loader.GetString("AddSoundsContentDialog-Title"),
                PrimaryButtonText = loader.GetString("AddSoundsContentDialog-PrimaryButton"),
                CloseButtonText = loader.GetString("Actions-Cancel"),
                DefaultButton = ContentDialogButton.Primary,
                IsPrimaryButtonEnabled = selectedFiles.Count > 0,
                RequestedTheme = FileManager.GetRequestedTheme()
            };

            StackPanel containerStackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical
            };

            Button selectFilesButton = new Button
            {
                Content = loader.GetString("AddSoundsContentDialog-SelectFiles"),
                Margin = new Thickness(0, 10, 0, 10),
            };
            selectFilesButton.Click += SelectFilesButton_Click;

            NoFilesSelectedTextBlock = new TextBlock
            {
                Text = loader.GetString("AddSoundsContentDialog-NoFilesSelected"),
                Margin = new Thickness(0, 25, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                Visibility = selectedFiles.Count > 0 ? Visibility.Collapsed : Visibility.Visible
            };

            AddSoundsSelectedFiles = new ObservableCollection<SoundFileItem>();

            foreach (StorageFile file in selectedFiles)
            {
                SoundFileItem item = new SoundFileItem(file);
                item.Removed += SoundFileItem_Removed;
                AddSoundsSelectedFiles.Add(item);
            }

            AddSoundsListView = new ListView
            {
                ItemTemplate = itemTemplate,
                ItemsSource = AddSoundsSelectedFiles,
                SelectionMode = ListViewSelectionMode.None,
                Height = 250,
                CanReorderItems = true,
                AllowDrop = true
            };

            containerStackPanel.Children.Add(selectFilesButton);
            containerStackPanel.Children.Add(NoFilesSelectedTextBlock);
            containerStackPanel.Children.Add(AddSoundsListView);

            AddSoundsContentDialog.Content = containerStackPanel;

            return AddSoundsContentDialog;
        }

        private static async void SelectFilesButton_Click(object sender, RoutedEventArgs e)
        {
            // Open file explorer
            var picker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.List,
                SuggestedStartLocation = PickerLocationId.MusicLibrary
            };

            foreach (var fileType in FileManager.allowedFileTypes)
                picker.FileTypeFilter.Add(fileType);

            var files = await picker.PickMultipleFilesAsync();
            
            foreach (var file in files)
            {
                SoundFileItem item = new SoundFileItem(file);
                item.Removed += SoundFileItem_Removed;
                AddSoundsSelectedFiles.Add(item);
            }

            AddSoundsContentDialog.IsPrimaryButtonEnabled = AddSoundsSelectedFiles.Count > 0;
            NoFilesSelectedTextBlock.Visibility = AddSoundsSelectedFiles.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        private static void SoundFileItem_Removed(object sender, EventArgs e)
        {
            AddSoundsSelectedFiles.Remove((SoundFileItem)sender);
            AddSoundsContentDialog.IsPrimaryButtonEnabled = AddSoundsSelectedFiles.Count > 0;
            NoFilesSelectedTextBlock.Visibility = AddSoundsSelectedFiles.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
        }
        #endregion
    }
}
