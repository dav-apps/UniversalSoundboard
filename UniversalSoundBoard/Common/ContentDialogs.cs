using davClassLibrary;
using DotNetTools.SharpGrabber;
using DotNetTools.SharpGrabber.Grabbed;
using Google.Apis.YouTube.v3.Data;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
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
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using WinUI = Microsoft.UI.Xaml.Controls;

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
    }
}
