using davClassLibrary;
using DotNetTools.SharpGrabber;
using DotNetTools.SharpGrabber.Grabbed;
using Google.Apis.YouTube.v3.Data;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
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
using Windows.UI.Xaml.Media.Imaging;
using WinUI = Microsoft.UI.Xaml.Controls;

namespace UniversalSoundboard.Common
{
    public class ContentDialogs
    {
        #region Variables
        private static readonly ResourceLoader loader = new ResourceLoader();

        private static Sound defaultSoundSettingsSelectedSound;
        private static VolumeControl DefaultSoundSettingsVolumeControl;
        private static bool defaultSoundSettingsVolumeChanged = false;
        private static bool defaultSoundSettingsMutedChanged = false;
        private static bool defaultSoundSettingsRepetitionsChanged = false;
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
        public static ProgressBar downloadFileProgressBar;
        public static List<Sound> downloadingFilesSoundsList = new List<Sound>();
        public static ListView ExportSoundsListView;
        public static TextBox ExportSoundsFolderTextBox;
        public static CheckBox ExportSoundsAsZipCheckBox;
        public static StorageFolder ExportSoundsFolder;
        public static ListView CategoriesListView;
        public static WinUI.TreeView CategoriesTreeView;
        public static ComboBox DefaultSoundSettingsRepetitionsComboBox;
        public static ComboBox PlaybackSpeedComboBox;
        public static List<ObservableCollection<HotkeyItem>> PropertiesDialogHotkeys = new List<ObservableCollection<HotkeyItem>>();
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
        public static ContentDialog DownloadFileContentDialog;
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

        #region DownloadSounds
        public static ContentDialog CreateDownloadSoundsContentDialog(Style infoButtonStyle)
        {
            DownloadSoundsContentDialog = new ContentDialog
            {
                Title = loader.GetString("DownloadSoundsContentDialog-Title"),
                PrimaryButtonText = loader.GetString("Actions-Add"),
                CloseButtonText = loader.GetString("Actions-Cancel"),
                DefaultButton = ContentDialogButton.Primary,
                IsPrimaryButtonEnabled = false,
                RequestedTheme = FileManager.GetRequestedTheme()
            };

            StackPanel containerStackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Width = 400
            };

            TextBlock descriptionTextBlock = new TextBlock
            {
                Text = loader.GetString("DownloadSoundsContentDialog-Description"),
                TextWrapping = TextWrapping.WrapWholeWords
            };

            DownloadSoundsUrlTextBox = new TextBox
            {
                Margin = new Thickness(0, 20, 0, 0),
                PlaceholderText = loader.GetString("DownloadSoundsContentDialog-UrlTextBoxPlaceholder")
            };
            DownloadSoundsUrlTextBox.TextChanged += DownloadSoundsUrlTextBox_TextChanged;

            DownloadSoundsLoadingMessageStackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 10, 0, 0),
                Visibility = Visibility.Collapsed
            };

            WinUI.ProgressRing progressRing = new WinUI.ProgressRing
            {
                IsActive = true,
                Width = 20,
                Height = 20
            };

            TextBlock loadingMessage = new TextBlock
            {
                Text = loader.GetString("DownloadSoundsContentDialog-RetrievingInfo"),
                FontSize = 14,
                Margin = new Thickness(10, 0, 0, 0)
            };

            DownloadSoundsLoadingMessageStackPanel.Children.Add(progressRing);
            DownloadSoundsLoadingMessageStackPanel.Children.Add(loadingMessage);

            DownloadSoundsYoutubeInfoGrid = new Grid
            {
                Margin = new Thickness(0, 10, 0, 0),
                Visibility = Visibility.Collapsed
            };

            DownloadSoundsYoutubeInfoGrid.RowDefinitions.Add(new RowDefinition());
            DownloadSoundsYoutubeInfoGrid.RowDefinitions.Add(new RowDefinition());
            DownloadSoundsYoutubeInfoGrid.RowDefinitions.Add(new RowDefinition());

            DownloadSoundsYoutubeInfoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            DownloadSoundsYoutubeInfoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            DownloadSoundsYoutubeInfoImage = new Image
            {
                Height = 60
            };
            Grid.SetColumn(DownloadSoundsYoutubeInfoImage, 0);

            DownloadSoundsYoutubeInfoTextBlock = new TextBlock
            {
                TextWrapping = TextWrapping.WrapWholeWords,
                Margin = new Thickness(20, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(DownloadSoundsYoutubeInfoTextBlock, 1);

            DownloadSoundsYoutubeInfoDownloadPlaylistStackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(5, 10, 0, 0),
                Visibility = Visibility.Collapsed
            };
            Grid.SetRow(DownloadSoundsYoutubeInfoDownloadPlaylistStackPanel, 1);
            Grid.SetColumnSpan(DownloadSoundsYoutubeInfoDownloadPlaylistStackPanel, 2);

            DownloadSoundsYoutubeInfoDownloadPlaylistCheckbox = new CheckBox
            {
                Content = loader.GetString("DownloadSoundsContentDialog-DownloadPlaylist"),
                IsEnabled = Dav.IsLoggedIn && Dav.User.Plan > 0
            };
            DownloadSoundsYoutubeInfoDownloadPlaylistCheckbox.Checked += DownloadSoundsYoutubeInfoDownloadPlaylistCheckbox_Checked;
            DownloadSoundsYoutubeInfoDownloadPlaylistCheckbox.Unchecked += DownloadSoundsYoutubeInfoDownloadPlaylistCheckbox_Unchecked;

            DownloadSoundsYoutubeInfoDownloadPlaylistStackPanel.Children.Add(DownloadSoundsYoutubeInfoDownloadPlaylistCheckbox);

            if (!Dav.IsLoggedIn || Dav.User.Plan == 0)
            {
                var flyout = new Flyout();

                var flyoutStackPanel = new StackPanel
                {
                    MaxWidth = 300
                };

                var flyoutText = new TextBlock
                {
                    Text = loader.GetString("DownloadSoundsContentDialog-DavPlusPlaylistDownload"),
                    TextWrapping = TextWrapping.WrapWholeWords
                };

                flyoutStackPanel.Children.Add(flyoutText);
                flyout.Content = flyoutStackPanel;

                var downloadPlaylistInfoButton = new Button
                {
                    Style = infoButtonStyle,
                    Margin = new Thickness(10, 0, 0, 0),
                    Flyout = flyout
                };

                DownloadSoundsYoutubeInfoDownloadPlaylistStackPanel.Children.Add(downloadPlaylistInfoButton);
            }

            DownloadSoundsYoutubeInfoCreateCategoryForPlaylistCheckbox = new CheckBox
            {
                Content = loader.GetString("DownloadSoundsContentDialog-CreateCategoryForPlaylist"),
                Margin = new Thickness(5, 5, 0, 0),
                Visibility = Visibility.Collapsed
            };
            Grid.SetRow(DownloadSoundsYoutubeInfoCreateCategoryForPlaylistCheckbox, 2);
            Grid.SetColumnSpan(DownloadSoundsYoutubeInfoCreateCategoryForPlaylistCheckbox, 2);

            DownloadSoundsYoutubeInfoGrid.Children.Add(DownloadSoundsYoutubeInfoImage);
            DownloadSoundsYoutubeInfoGrid.Children.Add(DownloadSoundsYoutubeInfoTextBlock);
            DownloadSoundsYoutubeInfoGrid.Children.Add(DownloadSoundsYoutubeInfoDownloadPlaylistStackPanel);
            DownloadSoundsYoutubeInfoGrid.Children.Add(DownloadSoundsYoutubeInfoCreateCategoryForPlaylistCheckbox);

            DownloadSoundsAudioFileInfoTextBlock = new TextBlock
            {
                Margin = new Thickness(6, 10, 0, 0),
                Visibility = Visibility.Collapsed
            };

            containerStackPanel.Children.Add(descriptionTextBlock);
            containerStackPanel.Children.Add(DownloadSoundsUrlTextBox);
            containerStackPanel.Children.Add(DownloadSoundsLoadingMessageStackPanel);
            containerStackPanel.Children.Add(DownloadSoundsYoutubeInfoGrid);
            containerStackPanel.Children.Add(DownloadSoundsAudioFileInfoTextBlock);

            DownloadSoundsContentDialog.Content = containerStackPanel;
            return DownloadSoundsContentDialog;
        }

        private static async void DownloadSoundsUrlTextBox_TextChanged(object sender, TextChangedEventArgs args)
        {
            DownloadSoundsContentDialog.IsPrimaryButtonEnabled = false;
            DownloadSoundsResult = DownloadSoundsResultType.None;
            HideAllMessageElementsInDownloadSoundsContentDialog();

            // Check if the input is a valid link
            string input = DownloadSoundsUrlTextBox.Text;

            Regex urlRegex = new Regex("^(https?:\\/\\/)?[\\w.-]+(\\.[\\w.-]+)+[\\w\\-._~/?#@&%\\+,;=]+");
            Regex shortYoutubeUrlRegex = new Regex("^(https?:\\/\\/)?youtu.be\\/");
            Regex youtubeUrlRegex = new Regex("^(https?:\\/\\/)?((www|music).)?youtube.com\\/");

            bool isUrl = urlRegex.IsMatch(input);
            bool isShortYoutubeUrl = shortYoutubeUrlRegex.IsMatch(input);
            bool isYoutubeUrl = youtubeUrlRegex.IsMatch(input);

            if (!isUrl)
            {
                HideAllMessageElementsInDownloadSoundsContentDialog();
                return;
            }

            DownloadSoundsLoadingMessageStackPanel.Visibility = Visibility.Visible;

            if (isShortYoutubeUrl || isYoutubeUrl)
            {
                string videoId = null;
                DownloadSoundsPlaylistId = null;

                if (isShortYoutubeUrl)
                {
                    videoId = input.Split('/').Last();
                }
                else
                {
                    // Get the video id from the url params
                    var queryDictionary = HttpUtility.ParseQueryString(input.Split('?').Last());

                    videoId = queryDictionary.Get("v");
                    DownloadSoundsPlaylistId = queryDictionary.Get("list");
                }

                // Build the url
                string youtubeLink = string.Format("https://youtube.com/watch?v={0}", videoId);

                try
                {
                    var grabber = GrabberBuilder.New().UseDefaultServices().AddYouTube().Build();
                    DownloadSoundsGrabResult = await grabber.GrabAsync(new Uri(youtubeLink));

                    if (DownloadSoundsGrabResult == null)
                    {
                        HideAllMessageElementsInDownloadSoundsContentDialog();
                        return;
                    }
                }
                catch(Exception e)
                {
                    HideAllMessageElementsInDownloadSoundsContentDialog();

                    Crashes.TrackError(e, new Dictionary<string, string>
                    {
                        { "YoutubeLink", input }
                    });
                    return;
                }

                DownloadSoundsYoutubeInfoTextBlock.Text = DownloadSoundsGrabResult.Title;

                var imageResources = DownloadSoundsGrabResult.Resources<GrabbedImage>();
                GrabbedImage smallThumbnail = imageResources.ToList().Find(image => image.ResourceUri.ToString().Split('/').Last() == "default.jpg");

                if (smallThumbnail != null)
                {
                    DownloadSoundsYoutubeInfoImage.Source = new BitmapImage(smallThumbnail.ResourceUri);
                    DownloadSoundsYoutubeInfoGrid.Visibility = Visibility.Visible;
                }

                DownloadSoundsLoadingMessageStackPanel.Visibility = Visibility.Collapsed;
                DownloadSoundsResult = DownloadSoundsResultType.Youtube;
                DownloadSoundsContentDialog.IsPrimaryButtonEnabled = true;

                if (DownloadSoundsPlaylistId != null)
                {
                    // Get the playlist
                    var listOperation = FileManager.youtubeService.PlaylistItems.List("contentDetails");
                    listOperation.PlaylistId = DownloadSoundsPlaylistId;
                    listOperation.MaxResults = 50;

                    try
                    {
                        DownloadSoundsPlaylistItemListResponse = await listOperation.ExecuteAsync();
                    }
                    catch (Exception)
                    {
                        return;
                    }

                    if (DownloadSoundsPlaylistItemListResponse.Items.Count > 1)
                    {
                        // Get the name of the playlist
                        DownloadSoundsPlaylistTitle = "";
                        var playlistListOperation = FileManager.youtubeService.Playlists.List("snippet");
                        playlistListOperation.Id = DownloadSoundsPlaylistId;

                        try
                        {
                            var result = await playlistListOperation.ExecuteAsync();

                            if (result.Items.Count > 0)
                                DownloadSoundsPlaylistTitle = result.Items[0].Snippet.Title;
                        }
                        catch (Exception) { }

                        // Show the option to download the playlist
                        DownloadSoundsYoutubeInfoDownloadPlaylistStackPanel.Visibility = Visibility.Visible;
                    }
                }

                return;
            }
            else
            {
                // Make a GET request to see if this is an audio file
                WebResponse response;

                try
                {
                    var req = WebRequest.Create(input);
                    response = await req.GetResponseAsync();

                    // Check if the content type is a supported audio format
                    if (!FileManager.allowedAudioMimeTypes.Contains(response.ContentType))
                    {
                        HideAllMessageElementsInDownloadSoundsContentDialog();

                        Analytics.TrackEvent("AudioFileDownload-NotSupportedFormat", new Dictionary<string, string>
                        {
                            { "Link", input }
                        });
                        return;
                    }
                }
                catch(Exception e)
                {
                    HideAllMessageElementsInDownloadSoundsContentDialog();

                    Crashes.TrackError(e, new Dictionary<string, string>
                    {
                        { "Link", input }
                    });
                    return;
                }

                // Get file type and file size
                DownloadSoundsAudioFileType = FileManager.FileTypeToExt(response.ContentType);
                long fileSize = response.ContentLength;

                // Try to get the file name
                Regex fileNameRegex = new Regex("^[\\w\\.\\+\\-_ ]+\\.\\w{3}$");
                DownloadSoundsAudioFileName = loader.GetString("DownloadSoundsContentDialog-DefaultSoundName");
                bool defaultFileName = true;

                string lastPart = input.Split('/').Last();
                
                if (fileNameRegex.IsMatch(lastPart))
                {
                    var parts = lastPart.Split('.');
                    DownloadSoundsAudioFileName = string.Join(".", parts.Take(parts.Count() - 1));
                    defaultFileName = false;
                }

                DownloadSoundsAudioFileInfoTextBlock.Text = "";
                if (!defaultFileName) DownloadSoundsAudioFileInfoTextBlock.Text += string.Format("{0}: {1}\n", loader.GetString("FileName"), DownloadSoundsAudioFileName);
                DownloadSoundsAudioFileInfoTextBlock.Text += string.Format("{0}: {1}\n", loader.GetString("FileType"), DownloadSoundsAudioFileType);
                if (fileSize > 0) DownloadSoundsAudioFileInfoTextBlock.Text += string.Format("{0}: {1}", loader.GetString("FileSize"), FileManager.GetFormattedSize((ulong)fileSize));
                DownloadSoundsAudioFileInfoTextBlock.Visibility = Visibility.Visible;

                DownloadSoundsLoadingMessageStackPanel.Visibility = Visibility.Collapsed;
                DownloadSoundsYoutubeInfoGrid.Visibility = Visibility.Collapsed;
                DownloadSoundsResult = DownloadSoundsResultType.AudioFile;
                DownloadSoundsContentDialog.IsPrimaryButtonEnabled = true;
            }
        }

        private static void DownloadSoundsYoutubeInfoDownloadPlaylistCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            if (DownloadSoundsPlaylistTitle.Length > 0)
                DownloadSoundsYoutubeInfoCreateCategoryForPlaylistCheckbox.Visibility = Visibility.Visible;
        }

        private static void DownloadSoundsYoutubeInfoDownloadPlaylistCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            DownloadSoundsYoutubeInfoCreateCategoryForPlaylistCheckbox.Visibility = Visibility.Collapsed;
        }

        private static void HideAllMessageElementsInDownloadSoundsContentDialog()
        {
            DownloadSoundsLoadingMessageStackPanel.Visibility = Visibility.Collapsed;
            DownloadSoundsYoutubeInfoGrid.Visibility = Visibility.Collapsed;
            DownloadSoundsYoutubeInfoDownloadPlaylistStackPanel.Visibility = Visibility.Collapsed;
            DownloadSoundsAudioFileInfoTextBlock.Visibility = Visibility.Collapsed;
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

            NewCategoryParentUuid = parentUuid;
            if (!Equals(parentUuid, Guid.Empty))
                NewCategoryContentDialog.Title = loader.GetString("NewSubCategoryContentDialog-Title");

            StackPanel stackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical
            };

            List<string> IconsList = FileManager.GetIconsList();

            NewCategoryTextBox = new TextBox
            {
                Width = 300,
                PlaceholderText = loader.GetString("NewCategoryContentDialog-NewCategoryTextBoxPlaceholder")
            };

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
                PrimaryButtonText = loader.GetString("Actions-Save"),
                CloseButtonText = loader.GetString("Actions-Cancel"),
                DefaultButton = ContentDialogButton.Primary,
                RequestedTheme = FileManager.GetRequestedTheme()
            };

            StackPanel stackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical
            };

            EditCategoryTextBox = new TextBox
            {
                Text = currentCategory.Name,
                PlaceholderText = loader.GetString("NewCategoryContentDialog-NewCategoryTextBoxPlaceholder"),
                Width = 300
            };

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
                PrimaryButtonText = loader.GetString("Actions-Delete"),
                CloseButtonText = loader.GetString("Actions-Cancel"),
                DefaultButton = ContentDialogButton.Close,
                RequestedTheme = FileManager.GetRequestedTheme()
            };

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

            return AddSoundErrorContentDialog;
        }
        #endregion

        #region DownloadSoundsError
        public static ContentDialog CreateDownloadSoundsErrorContentDialog(List<KeyValuePair<string, string>> soundsList)
        {
            DownloadSoundsErrorContentDialog = new ContentDialog
            {
                Title = loader.GetString("DownloadSoundsErrorContentDialog-Title"),
                CloseButtonText = loader.GetString("Actions-Close"),
                DefaultButton = ContentDialogButton.Close,
                RequestedTheme = FileManager.GetRequestedTheme()
            };

            StackPanel containerStackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical
            };

            TextBlock descriptionTextBlock = new TextBlock
            {
                Text = loader.GetString("DownloadSoundsErrorContentDialog-Description"),
                Margin = new Thickness(0, 0, 0, 8)
            };

            containerStackPanel.Children.Add(descriptionTextBlock);

            ScrollViewer scrollViewer = new ScrollViewer
            {
                MaxHeight = 300
            };

            StackPanel scrollViewerContainerStackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical
            };

            foreach (var soundItem in soundsList)
            {
                scrollViewerContainerStackPanel.Children.Add(
                    new HyperlinkButton
                    {
                        Content = soundItem.Key != null ? soundItem.Key : soundItem.Value,
                        NavigateUri = new Uri(soundItem.Value)
                    }
                );
            }

            scrollViewer.Content = scrollViewerContainerStackPanel;
            containerStackPanel.Children.Add(scrollViewer);

            DownloadSoundsErrorContentDialog.Content = containerStackPanel;
            return DownloadSoundsErrorContentDialog;
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

            StackPanel stackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical
            };

            RenameSoundTextBox = new TextBox
            {
                Text = sound.Name,
                PlaceholderText = loader.GetString("RenameSoundContentDialog-RenameSoundTextBoxPlaceholder"),
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
                PrimaryButtonText = loader.GetString("Actions-Delete"),
                CloseButtonText = loader.GetString("Actions-Cancel"),
                DefaultButton = ContentDialogButton.Close,
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
                PrimaryButtonText = loader.GetString("Actions-Delete"),
                CloseButtonText = loader.GetString("Actions-Cancel"),
                DefaultButton = ContentDialogButton.Close,
                RequestedTheme = FileManager.GetRequestedTheme()
            };

            return DeleteSoundsContentDialog;
        }
        #endregion

        #region ExportData
        public static ContentDialog CreateExportDataContentDialog()
        {
            ExportDataContentDialog = new ContentDialog
            {
                Title = loader.GetString("ExportDataContentDialog-Title"),
                PrimaryButtonText = loader.GetString("Export"),
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
                FontFamily = new FontFamily(FileManager.FluentIconsFontFamily),
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
                PrimaryButtonText = loader.GetString("Import"),
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
                FontFamily = new FontFamily(FileManager.FluentIconsFontFamily),
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
                PrimaryButtonText = loader.GetString("Import"),
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
                FontFamily = new FontFamily(FileManager.FluentIconsFontFamily),
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

            return LogoutContentDialog;
        }
        #endregion

        #region DownloadFile
        public static ContentDialog CreateDownloadFileContentDialog(string filename)
        {
            DownloadFileContentDialog = new ContentDialog
            {
                Title = string.Format(loader.GetString("DownloadFileContentDialog-Title"), filename),
                CloseButtonText = loader.GetString("Actions-Cancel"),
                DefaultButton = ContentDialogButton.None,
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
                CloseButtonText = loader.GetString("Actions-Cancel"),
                DefaultButton = ContentDialogButton.None,
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
                CloseButtonText = loader.GetString("Actions-Close"),
                DefaultButton = ContentDialogButton.Close,
                RequestedTheme = FileManager.GetRequestedTheme()
            };

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
                FontFamily = new FontFamily(FileManager.FluentIconsFontFamily),
                Content = "\uE838",
                FontSize = 18,
                Width = 35,
                Height = 35,
                Padding = new Thickness(0)
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
            PropertiesContentDialog = new ContentDialog
            {
                Title = loader.GetString("SoundItemOptionsFlyout-Properties"),
                CloseButtonText = loader.GetString("Actions-Close"),
                RequestedTheme = FileManager.GetRequestedTheme()
            };

            int fontSize = 15;
            int row = 0;
            int contentGridWidth = 500;
            int leftColumnWidth = 210;

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

            if (audioFileType != null)
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

            if (imageFileType != null)
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

            if (imageFile != null)
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

            PropertiesContentDialog.Content = contentGrid;
            return PropertiesContentDialog;
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

        #region DefaultSoundSettings
        public static ContentDialog CreateDefaultSoundSettingsContentDialog(Sound sound)
        {
            defaultSoundSettingsSelectedSound = sound;
            defaultSoundSettingsVolumeChanged = false;
            defaultSoundSettingsMutedChanged = false;
            defaultSoundSettingsRepetitionsChanged = false;

            DefaultSoundSettingsContentDialog = new ContentDialog
            {
                Title = string.Format(loader.GetString("DefaultSoundSettingsContentDialog-Title"), sound.Name),
                CloseButtonText = loader.GetString("Actions-Close"),
                RequestedTheme = FileManager.GetRequestedTheme()
            };
            DefaultSoundSettingsContentDialog.CloseButtonClick += DefaultSoundOptionsContentDialog_CloseButtonClick;

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

            #region Description
            // Add the row
            var descriptionRow = new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) };
            contentGrid.RowDefinitions.Add(descriptionRow);

            StackPanel descriptionStackPanel = new StackPanel();
            Grid.SetRow(descriptionStackPanel, row);
            Grid.SetColumn(descriptionStackPanel, 0);
            Grid.SetColumnSpan(descriptionStackPanel, 2);

            TextBlock descriptionTextBlock = new TextBlock
            {
                Text = loader.GetString("DefaultSoundSettingsContentDialog-Description"),
                Margin = new Thickness(0, 0, 0, 0),
                FontSize = fontSize,
                TextWrapping = TextWrapping.WrapWholeWords
            };

            descriptionStackPanel.Children.Add(descriptionTextBlock);

            row++;
            contentGrid.Children.Add(descriptionStackPanel);
            #endregion

            #region Volume
            // Add the row
            var volumeRow = new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) };
            contentGrid.RowDefinitions.Add(volumeRow);

            StackPanel volumeHeaderStackPanel = GenerateTableCell(
                row,
                0,
                loader.GetString("DefaultSoundSettingsContentDialog-Volume"),
                fontSize,
                false,
                new Thickness(0, 16, 0, 0)
            );

            StackPanel volumeDataStackPanel = new StackPanel();
            Grid.SetRow(volumeDataStackPanel, row);
            Grid.SetColumn(volumeDataStackPanel, 1);

            RelativePanel volumeRelativePanel = new RelativePanel();
            volumeDataStackPanel.Children.Add(volumeRelativePanel);

            DefaultSoundSettingsVolumeControl = new VolumeControl
            {
                Value = sound.DefaultVolume,
                Muted = sound.DefaultMuted,
                Margin = new Thickness(8, 10, 0, 0)
            };
            DefaultSoundSettingsVolumeControl.ValueChanged += VolumeControl_ValueChanged;
            DefaultSoundSettingsVolumeControl.MuteChanged += VolumeControl_MuteChanged;

            volumeRelativePanel.Children.Add(DefaultSoundSettingsVolumeControl);

            row++;
            contentGrid.Children.Add(volumeHeaderStackPanel);
            contentGrid.Children.Add(volumeDataStackPanel);
            #endregion

            #region Repetitions
            // Add the row
            var repetitionsRow = new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) };
            contentGrid.RowDefinitions.Add(repetitionsRow);

            StackPanel repetitionsHeaderStackPanel = GenerateTableCell(
                row,
                0,
                loader.GetString("DefaultSoundSettingsContentDialog-Repetitions"),
                fontSize,
                false,
                new Thickness(0, 16, 0, 0)
            );

            RelativePanel repetitionsDataRelativePanel = new RelativePanel { Margin = new Thickness(0, 10, 0, 0) };
            Grid.SetRow(repetitionsDataRelativePanel, row);
            Grid.SetColumn(repetitionsDataRelativePanel, 1);

            List<int> defaultRepetitionsValues = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 15, 20, 25, 30, 40, 50, 100 };

            if (sound.DefaultRepetitions != int.MaxValue && !defaultRepetitionsValues.Contains(sound.DefaultRepetitions))
            {
                defaultRepetitionsValues.Add(sound.DefaultRepetitions);
                defaultRepetitionsValues.Sort();
            }

            DefaultSoundSettingsRepetitionsComboBox = new ComboBox
            {
                IsEditable = true
            };
            DefaultSoundSettingsRepetitionsComboBox.SelectionChanged += DefaultSoundSettingsRepetitionsComboBox_SelectionChanged;
            DefaultSoundSettingsRepetitionsComboBox.TextSubmitted += DefaultSoundSettingsRepetitionsComboBox_TextSubmitted;

            foreach (int value in defaultRepetitionsValues)
                DefaultSoundSettingsRepetitionsComboBox.Items.Add(value.ToString());

            DefaultSoundSettingsRepetitionsComboBox.Items.Add("∞");

            if (sound.DefaultRepetitions == int.MaxValue)
                DefaultSoundSettingsRepetitionsComboBox.SelectedValue = "∞";
            else
                DefaultSoundSettingsRepetitionsComboBox.SelectedValue = sound.DefaultRepetitions.ToString();

            RelativePanel.SetAlignVerticalCenterWithPanel(DefaultSoundSettingsRepetitionsComboBox, true);
            repetitionsDataRelativePanel.Children.Add(DefaultSoundSettingsRepetitionsComboBox);

            row++;
            contentGrid.Children.Add(repetitionsHeaderStackPanel);
            contentGrid.Children.Add(repetitionsDataRelativePanel);
            #endregion

            #region Playback speed
            // Add the row
            var playbackSpeedRow = new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) };
            contentGrid.RowDefinitions.Add(playbackSpeedRow);

            StackPanel playbackSpeedHeaderStackPanel = GenerateTableCell(
                row,
                0,
                loader.GetString("DefaultSoundSettingsContentDialog-PlaybackSpeed"),
                fontSize,
                false,
                new Thickness(0, 16, 0, 0)
            );

            RelativePanel playbackSpeedDataRelativePanel = new RelativePanel { Margin = new Thickness(0, 10, 0, 0) };
            Grid.SetRow(playbackSpeedDataRelativePanel, row);
            Grid.SetColumn(playbackSpeedDataRelativePanel, 1);

            // Create the ComboBox with the playback speed items
            PlaybackSpeedComboBox = new ComboBox();
            PlaybackSpeedComboBox.Items.Add("0.25×");
            PlaybackSpeedComboBox.Items.Add("0.5×");
            PlaybackSpeedComboBox.Items.Add("0.75×");
            PlaybackSpeedComboBox.Items.Add("1.0×");
            PlaybackSpeedComboBox.Items.Add("1.25×");
            PlaybackSpeedComboBox.Items.Add("1.5×");
            PlaybackSpeedComboBox.Items.Add("1.75×");
            PlaybackSpeedComboBox.Items.Add("2.0×");
            PlaybackSpeedComboBox.SelectionChanged += PlaybackSpeedComboBox_SelectionChanged;

            // Select the correct item
            switch (defaultSoundSettingsSelectedSound.DefaultPlaybackSpeed)
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

                foreach (Hotkey hotkey in defaultSoundSettingsSelectedSound.Hotkeys)
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
                    FontFamily = new FontFamily(FileManager.FluentIconsFontFamily),
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

            DefaultSoundSettingsContentDialog.Content = contentGrid;
            return DefaultSoundSettingsContentDialog;
        }

        private static void VolumeControl_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            defaultSoundSettingsVolumeChanged = true;
        }

        private static void VolumeControl_MuteChanged(object sender, bool e)
        {
            defaultSoundSettingsMutedChanged = true;
        }

        private static void DefaultSoundSettingsRepetitionsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            defaultSoundSettingsRepetitionsChanged = true;
        }

        private static void DefaultSoundSettingsRepetitionsComboBox_TextSubmitted(ComboBox sender, ComboBoxTextSubmittedEventArgs args)
        {
            defaultSoundSettingsRepetitionsChanged = true;

            if (args.Text == "∞") return;
            if (!int.TryParse(args.Text, out int value) || value < 0)
                DefaultSoundSettingsRepetitionsComboBox.Text = "1";
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

            defaultSoundSettingsSelectedSound.DefaultPlaybackSpeed = selectedPlaybackSpeed;

            await FileManager.SetDefaultPlaybackSpeedOfSoundAsync(defaultSoundSettingsSelectedSound.Uuid, selectedPlaybackSpeed);
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
            defaultSoundSettingsSelectedSound.Hotkeys.Add(e.Hotkey);
            HotkeyItem newHotkeyItem = new HotkeyItem(e.Hotkey);
            newHotkeyItem.RemoveHotkey += HotkeyItem_RemoveHotkey;
            PropertiesDialogHotkeys.Last().Add(newHotkeyItem);

            // Update the visibility of the dav Plus info text
            UpdateDavPlusHotkeyInfoStackPanelVisibility();

            // Save the hotkeys of the sound
            await FileManager.SetHotkeysOfSoundAsync(defaultSoundSettingsSelectedSound.Uuid, defaultSoundSettingsSelectedSound.Hotkeys);

            // Update the Hotkey process with the new hotkeys
            await FileManager.StartHotkeyProcess();
        }

        private static async void HotkeyItem_RemoveHotkey(object sender, HotkeyEventArgs e)
        {
            // Remove the hotkey from the list of hotkeys
            int index = defaultSoundSettingsSelectedSound.Hotkeys.FindIndex(h => h.Modifiers == e.Hotkey.Modifiers && h.Key == e.Hotkey.Key);
            if (index != -1) defaultSoundSettingsSelectedSound.Hotkeys.RemoveAt(index);

            PropertiesDialogHotkeys.Last().Remove((HotkeyItem)sender);

            // Update the visibility of the dav Plus info text
            UpdateDavPlusHotkeyInfoStackPanelVisibility();

            // Save the hotkeys of the sound
            await FileManager.SetHotkeysOfSoundAsync(defaultSoundSettingsSelectedSound.Uuid, defaultSoundSettingsSelectedSound.Hotkeys);

            // Update the Hotkey process with the new hotkeys
            await FileManager.StartHotkeyProcess();
        }

        private static async void DefaultSoundOptionsContentDialog_CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (
                !defaultSoundSettingsVolumeChanged
                && !defaultSoundSettingsMutedChanged
                && !defaultSoundSettingsRepetitionsChanged
            ) return;

            if (defaultSoundSettingsVolumeChanged || defaultSoundSettingsMutedChanged)
            {
                // Set the new values and update the DefaultVolume and DefaultMuted of all Sounds in all lists in ItemViewHolder
                defaultSoundSettingsSelectedSound.DefaultVolume = DefaultSoundSettingsVolumeControl.Value;
                defaultSoundSettingsSelectedSound.DefaultMuted = DefaultSoundSettingsVolumeControl.Muted;

                // Update the sound in the database
                await FileManager.SetDefaultVolumeOfSoundAsync(defaultSoundSettingsSelectedSound.Uuid, DefaultSoundSettingsVolumeControl.Value, DefaultSoundSettingsVolumeControl.Muted);
            }

            if (defaultSoundSettingsRepetitionsChanged)
            {
                // Get the selected repetitions
                string defaultRepetitionsString = DefaultSoundSettingsRepetitionsComboBox.Text;
                int defaultRepetitions = 0;

                if (defaultRepetitionsString == "∞")
                    defaultRepetitions = int.MaxValue;
                else
                    int.TryParse(defaultRepetitionsString, out defaultRepetitions);

                defaultSoundSettingsSelectedSound.DefaultRepetitions = defaultRepetitions;
                await FileManager.SetDefaultRepetitionsOfSoundAsync(defaultSoundSettingsSelectedSound.Uuid, defaultRepetitions);
            }

            await FileManager.ReloadSound(defaultSoundSettingsSelectedSound);
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

            return DavPlusOutputDeviceContentDialog;
        }
        #endregion

        #region UpgradeError
        public static ContentDialog CreateUpgradeErrorContentDialog()
        {
            UpgradeErrorContentDialog = new ContentDialog
            {
                Title = loader.GetString("UpgradeErrorContentDialog-Title"),
                Content = loader.GetString("UpgradeErrorContentDialog-Message"),
                CloseButtonText = loader.GetString("Actions-Close"),
                DefaultButton = ContentDialogButton.Close,
                RequestedTheme = FileManager.GetRequestedTheme()
            };

            return UpgradeErrorContentDialog;
        }
        #endregion

        #region AddRecordedSoundToSoundboard
        public static ContentDialog CreateAddRecordedSoundToSoundboardContentDialog(string recordedSoundName)
        {
            AddRecordedSoundToSoundboardContentDialog = new ContentDialog
            {
                Title = loader.GetString("AddRecordedSoundToSoundboardContentDialog-Title"),
                PrimaryButtonText = loader.GetString("Actions-Add"),
                CloseButtonText = loader.GetString("Actions-Cancel"),
                DefaultButton = ContentDialogButton.Primary,
                RequestedTheme = FileManager.GetRequestedTheme()
            };

            StackPanel rootStackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical
            };

            RecordedSoundNameTextBox = new TextBox
            {
                Text = recordedSoundName,
                PlaceholderText = loader.GetString("RenameSoundContentDialog-RenameSoundTextBoxPlaceholder"),
                Width = 300
            };

            rootStackPanel.Children.Add(RecordedSoundNameTextBox);

            AddRecordedSoundToSoundboardContentDialog.Content = rootStackPanel;
            RecordedSoundNameTextBox.TextChanged += RecordedSoundNameTextBox_TextChanged;

            return AddRecordedSoundToSoundboardContentDialog;
        }

        private static void RecordedSoundNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            AddRecordedSoundToSoundboardContentDialog.IsPrimaryButtonEnabled = RecordedSoundNameTextBox.Text.Length >= 3;
        }
        #endregion

        #region RemoveRecordedSound
        public static ContentDialog CreateRemoveRecordedSoundContentDialog(string recordedSoundName)
        {
            RemoveRecordedSoundContentDialog = new ContentDialog
            {
                Title = string.Format(loader.GetString("RemoveRecordedSoundContentDialog-Title"), recordedSoundName),
                Content = loader.GetString("RemoveRecordedSoundContentDialog-Message"),
                PrimaryButtonText = loader.GetString("Actions-Remove"),
                CloseButtonText = loader.GetString("Actions-Cancel"),
                DefaultButton = ContentDialogButton.Close,
                RequestedTheme = FileManager.GetRequestedTheme()
            };

            return RemoveRecordedSoundContentDialog;
        }
        #endregion

        #region SoundRecorderCloseWarning
        public static ContentDialog CreateSoundRecorderCloseWarningContentDialog()
        {
            SoundRecorderCloseWarningContentDialog = new ContentDialog
            {
                Title = loader.GetString("SoundRecorderCloseWarningContentDialog-Title"),
                Content = loader.GetString("SoundRecorderCloseWarningContentDialog-Message"),
                PrimaryButtonText = loader.GetString("Actions-CloseWindow"),
                CloseButtonText = loader.GetString("Actions-Cancel"),
                DefaultButton = ContentDialogButton.Close,
                RequestedTheme = FileManager.GetRequestedTheme()
            };

            return SoundRecorderCloseWarningContentDialog;
        }
        #endregion
    }
}
