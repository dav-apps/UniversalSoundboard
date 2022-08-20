using davClassLibrary;
using DotNetTools.SharpGrabber;
using DotNetTools.SharpGrabber.Grabbed;
using Google.Apis.YouTube.v3.Data;
using HtmlAgilityPack;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.Toolkit.Uwp.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using UniversalSoundboard.Common;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Models;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using WinUI = Microsoft.UI.Xaml.Controls;

namespace UniversalSoundboard.Dialogs
{
    public class SoundDownloadDialog : Dialog
    {
        private TextBox UrlTextBox;
        private StackPanel LoadingMessageStackPanel;
        private Grid YoutubeInfoGrid;
        private Image YoutubeInfoImage;
        private TextBlock YoutubeInfoTextBlock;
        private StackPanel YoutubeInfoDownloadPlaylistStackPanel;
        private CheckBox YoutubeInfoDownloadPlaylistCheckbox;
        private CheckBox YoutubeInfoCreateCategoryForPlaylistCheckbox;
        private TextBlock AudioFileInfoTextBlock;
        private StackPanel SoundListStackPanel;
        private ListView SoundListView;
        private TextBlock SoundListNumberTextBlock;
        private Button SoundListSelectAllButton;
        private ObservableCollection<SoundDownloadListItem> SoundItems;
        public DownloadSoundsResultType DownloadSoundsResult { get; private set; }
        public string PlaylistId { get; private set; }
        public GrabResult GrabResult { get; private set; }
        public PlaylistItemListResponse PlaylistItemListResponse { get; private set; }
        public string PlaylistTitle { get; private set; }
        public string AudioFileName { get; private set; }
        public string AudioFileType { get; private set; }

        public string Url
        {
            get => UrlTextBox?.Text;
        }
        public bool DownloadPlaylist
        {
            get => (bool)YoutubeInfoDownloadPlaylistCheckbox.IsChecked;
        }
        public bool CreateCategoryForPlaylist
        {
            get => (bool)YoutubeInfoCreateCategoryForPlaylistCheckbox.IsChecked;
        }

        public SoundDownloadDialog(Style infoButtonStyle, DataTemplate soundDownloadListItemTemplate)
            : base(
                  FileManager.loader.GetString("DownloadSoundsDialog-Title"),
                  FileManager.loader.GetString("Actions-Add"),
                  FileManager.loader.GetString("Actions-Cancel")
            )
        {
            SoundItems = new ObservableCollection<SoundDownloadListItem>();
            ContentDialog.IsPrimaryButtonEnabled = false;
            Content = GetContent(infoButtonStyle, soundDownloadListItemTemplate);
        }

        private StackPanel GetContent(Style infoButtonStyle, DataTemplate soundDownloadListItemTemplate)
        {
            StackPanel containerStackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Width = 400
            };

            TextBlock descriptionTextBlock = new TextBlock
            {
                Text = FileManager.loader.GetString("DownloadSoundsDialog-Description"),
                TextWrapping = TextWrapping.WrapWholeWords
            };

            UrlTextBox = new TextBox
            {
                Margin = new Thickness(0, 20, 0, 0),
                PlaceholderText = FileManager.loader.GetString("DownloadSoundsDialog-UrlTextBoxPlaceholder")
            };
            UrlTextBox.TextChanged += DownloadSoundsUrlTextBox_TextChanged;

			CreateLoadingMessageStackPanel();
			CreateYoutubeInfoGrid(infoButtonStyle);

            AudioFileInfoTextBlock = new TextBlock
            {
                Margin = new Thickness(6, 10, 0, 0),
                Visibility = Visibility.Collapsed
            };

            CreateSoundListStackPanel(soundDownloadListItemTemplate);

            containerStackPanel.Children.Add(descriptionTextBlock);
            containerStackPanel.Children.Add(UrlTextBox);
            containerStackPanel.Children.Add(LoadingMessageStackPanel);
            containerStackPanel.Children.Add(YoutubeInfoGrid);
            containerStackPanel.Children.Add(AudioFileInfoTextBlock);
            containerStackPanel.Children.Add(SoundListStackPanel);

            return containerStackPanel;
        }

        private void CreateLoadingMessageStackPanel()
        {
            LoadingMessageStackPanel = new StackPanel
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
                Text = FileManager.loader.GetString("DownloadSoundsDialog-RetrievingInfo"),
                FontSize = 14,
                Margin = new Thickness(10, 0, 0, 0)
            };

            LoadingMessageStackPanel.Children.Add(progressRing);
            LoadingMessageStackPanel.Children.Add(loadingMessage);
        }

        private void CreateYoutubeInfoGrid(Style infoButtonStyle)
        {
            YoutubeInfoGrid = new Grid
            {
                Margin = new Thickness(0, 10, 0, 0),
                Visibility = Visibility.Collapsed
            };

            YoutubeInfoGrid.RowDefinitions.Add(new RowDefinition());
            YoutubeInfoGrid.RowDefinitions.Add(new RowDefinition());
            YoutubeInfoGrid.RowDefinitions.Add(new RowDefinition());

            YoutubeInfoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            YoutubeInfoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            YoutubeInfoImage = new Image
            {
                Height = 60
            };
            Grid.SetColumn(YoutubeInfoImage, 0);

            YoutubeInfoTextBlock = new TextBlock
            {
                TextWrapping = TextWrapping.WrapWholeWords,
                Margin = new Thickness(20, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(YoutubeInfoTextBlock, 1);

            YoutubeInfoDownloadPlaylistStackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(5, 10, 0, 0),
                Visibility = Visibility.Collapsed
            };
            Grid.SetRow(YoutubeInfoDownloadPlaylistStackPanel, 1);
            Grid.SetColumnSpan(YoutubeInfoDownloadPlaylistStackPanel, 2);

            YoutubeInfoDownloadPlaylistCheckbox = new CheckBox
            {
                Content = FileManager.loader.GetString("DownloadSoundsDialog-DownloadPlaylist"),
                IsEnabled = Dav.IsLoggedIn && Dav.User.Plan > 0
            };
            YoutubeInfoDownloadPlaylistCheckbox.Checked += DownloadSoundsYoutubeInfoDownloadPlaylistCheckbox_Checked;
            YoutubeInfoDownloadPlaylistCheckbox.Unchecked += DownloadSoundsYoutubeInfoDownloadPlaylistCheckbox_Unchecked;

            YoutubeInfoDownloadPlaylistStackPanel.Children.Add(YoutubeInfoDownloadPlaylistCheckbox);

            if (!Dav.IsLoggedIn || Dav.User.Plan == 0)
            {
                var flyout = new Flyout();

                var flyoutStackPanel = new StackPanel
                {
                    MaxWidth = 300
                };

                var flyoutText = new TextBlock
                {
                    Text = FileManager.loader.GetString("DownloadSoundsDialog-DavPlusPlaylistDownload"),
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

                YoutubeInfoDownloadPlaylistStackPanel.Children.Add(downloadPlaylistInfoButton);
            }

            YoutubeInfoCreateCategoryForPlaylistCheckbox = new CheckBox
            {
                Content = FileManager.loader.GetString("DownloadSoundsDialog-CreateCategoryForPlaylist"),
                Margin = new Thickness(5, 5, 0, 0),
                Visibility = Visibility.Collapsed
            };
            Grid.SetRow(YoutubeInfoCreateCategoryForPlaylistCheckbox, 2);
            Grid.SetColumnSpan(YoutubeInfoCreateCategoryForPlaylistCheckbox, 2);

            YoutubeInfoGrid.Children.Add(YoutubeInfoImage);
            YoutubeInfoGrid.Children.Add(YoutubeInfoTextBlock);
            YoutubeInfoGrid.Children.Add(YoutubeInfoDownloadPlaylistStackPanel);
            YoutubeInfoGrid.Children.Add(YoutubeInfoCreateCategoryForPlaylistCheckbox);
        }

        private void CreateSoundListStackPanel(DataTemplate soundDownloadListItemTemplate)
        {
            SoundListStackPanel = new StackPanel
            {
                Visibility = Visibility.Collapsed
            };

            SoundListView = new ListView
            {
                ItemTemplate = soundDownloadListItemTemplate,
                ItemsSource = SoundItems,
                Margin = new Thickness(0, 10, 0, 0),
                Height = 250,
                SelectionMode = ListViewSelectionMode.Multiple
            };

            SoundListView.SelectionChanged += SoundListView_SelectionChanged;

            SoundListNumberTextBlock = new TextBlock
            {
                Text = string.Format(FileManager.loader.GetString("DownloadSoundsDialog-SelectedSounds"), SoundItems.Count, SoundItems.Count)
            };

            RelativePanel.SetAlignVerticalCenterWithPanel(SoundListNumberTextBlock, true);

            SoundListSelectAllButton = new Button
            {
                Content = FileManager.loader.GetString("Actions-DeselectAll"),
                FontSize = 13,
                Margin = new Thickness(10, 0, 0, 0),
                Padding = new Thickness(5, 3, 5, 3)
            };

            SoundListSelectAllButton.Click += SoundListSelectAllButton_Click;

            RelativePanel.SetAlignVerticalCenterWithPanel(SoundListSelectAllButton, true);
            RelativePanel.SetAlignRightWithPanel(SoundListSelectAllButton, true);

            RelativePanel soundListNumberRelativePanel = new RelativePanel
            {
                Margin = new Thickness(10, 10, 0, 0)
            };

            soundListNumberRelativePanel.Children.Add(SoundListNumberTextBlock);
            soundListNumberRelativePanel.Children.Add(SoundListSelectAllButton);

            SoundListStackPanel.Children.Add(SoundListView);
            SoundListStackPanel.Children.Add(soundListNumberRelativePanel);
        }

        private void UpdateSoundListNumberText()
        {
            SoundListNumberTextBlock.Text = string.Format(
                FileManager.loader.GetString("DownloadSoundsDialog-SelectedSounds"),
                SoundListView.SelectedItems.Count,
                SoundItems.Count
            );

            if (SoundListView.SelectedItems.Count == SoundItems.Count)
                SoundListSelectAllButton.Content = FileManager.loader.GetString("Actions-DeselectAll");
            else
                SoundListSelectAllButton.Content = FileManager.loader.GetString("Actions-SelectAll");

            ContentDialog.IsPrimaryButtonEnabled = SoundListView.SelectedItems.Count > 0;
        }

        private void SoundListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateSoundListNumberText();
        }

        private void SoundListSelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (SoundListView.SelectedItems.Count == SoundItems.Count)
                SoundListView.DeselectAll();
            else
                SoundListView.SelectAll();
        }

        private async void DownloadSoundsUrlTextBox_TextChanged(object sender, TextChangedEventArgs args)
        {
            ContentDialog.IsPrimaryButtonEnabled = false;
            DownloadSoundsResult = DownloadSoundsResultType.None;
            HideAllMessageElements();

            // Check if the input is a valid link
            string input = UrlTextBox.Text;

            Regex urlRegex = new Regex("^(https?:\\/\\/)?[\\w.-]+(\\.[\\w.-]+)+[\\w\\-._~/?#@&%\\+,;=]+");
            Regex shortYoutubeUrlRegex = new Regex("^(https?:\\/\\/)?youtu.be\\/");
            Regex youtubeUrlRegex = new Regex("^(https?:\\/\\/)?((www|music).)?youtube.com\\/");
            Regex zopharRegex = new Regex("^(https?:\\/\\/)?(www.)?zophar.net\\/music\\/[\\w\\-]+\\/[\\w\\-]+");

            bool isUrl = urlRegex.IsMatch(input);
            bool isShortYoutubeUrl = shortYoutubeUrlRegex.IsMatch(input);
            bool isYoutubeUrl = youtubeUrlRegex.IsMatch(input);
            bool isZopharUrl = zopharRegex.IsMatch(input);

            if (!isUrl)
            {
                HideAllMessageElements();
                return;
            }

            LoadingMessageStackPanel.Visibility = Visibility.Visible;

            if (isShortYoutubeUrl || isYoutubeUrl)
            {
                string videoId = null;
                PlaylistId = null;

                if (isShortYoutubeUrl)
                {
                    videoId = input.Split('/').Last();
                }
                else
                {
                    // Get the video id from the url params
                    var queryDictionary = HttpUtility.ParseQueryString(input.Split('?').Last());

                    videoId = queryDictionary.Get("v");
                    PlaylistId = queryDictionary.Get("list");
                }

                // Build the url
                string youtubeLink = string.Format("https://youtube.com/watch?v={0}", videoId);

                try
                {
                    var grabber = GrabberBuilder.New().UseDefaultServices().AddYouTube().Build();
                    GrabResult = await grabber.GrabAsync(new Uri(youtubeLink));

                    if (GrabResult == null)
                    {
                        HideAllMessageElements();
                        return;
                    }
                }
                catch (Exception e)
                {
                    HideAllMessageElements();

                    Crashes.TrackError(e, new Dictionary<string, string>
                    {
                        { "YoutubeLink", input }
                    });

                    return;
                }

                YoutubeInfoTextBlock.Text = GrabResult.Title;

                var imageResources = GrabResult.Resources<GrabbedImage>();
                GrabbedImage smallThumbnail = imageResources.ToList().Find(image => image.ResourceUri.ToString().Split('/').Last() == "default.jpg");

                if (smallThumbnail != null)
                {
                    YoutubeInfoImage.Source = new BitmapImage(smallThumbnail.ResourceUri);
                    YoutubeInfoGrid.Visibility = Visibility.Visible;
                }

                LoadingMessageStackPanel.Visibility = Visibility.Collapsed;
                DownloadSoundsResult = DownloadSoundsResultType.Youtube;
                ContentDialog.IsPrimaryButtonEnabled = true;

                if (PlaylistId != null)
                {
                    // Get the playlist
                    var listOperation = FileManager.youtubeService.PlaylistItems.List("contentDetails");
                    listOperation.PlaylistId = PlaylistId;
                    listOperation.MaxResults = 50;

                    try
                    {
                        PlaylistItemListResponse = await listOperation.ExecuteAsync();
                    }
                    catch (Exception)
                    {
                        return;
                    }

                    if (PlaylistItemListResponse.Items.Count > 1)
                    {
                        // Get the name of the playlist
                        PlaylistTitle = "";
                        var playlistListOperation = FileManager.youtubeService.Playlists.List("snippet");
                        playlistListOperation.Id = PlaylistId;

                        try
                        {
                            var result = await playlistListOperation.ExecuteAsync();

                            if (result.Items.Count > 0)
                                PlaylistTitle = result.Items[0].Snippet.Title;
                        }
                        catch (Exception) { }

                        // Show the option to download the playlist
                        YoutubeInfoDownloadPlaylistStackPanel.Visibility = Visibility.Visible;
                    }
                }

                return;
            }
            else if (isZopharUrl)
            {
                var web = new HtmlWeb();
                var document = await web.LoadFromWebAsync(input);

                // Get the tracklist
                var tracklistNode = document.DocumentNode.SelectNodes("//table[@id='tracklist']/*");

                if (tracklistNode == null)
                {
                    HideAllMessageElements();
                    return;
                }

                foreach (var node in tracklistNode)
                {
                    // Get the name
                    var nameNode = node.SelectSingleNode("./td[@class='name']");
                    if (nameNode == null) continue;

                    string name = nameNode.InnerText;

                    // Get the length
                    var lengthNode = node.SelectSingleNode("./td[@class='length']");
                    if (lengthNode == null) continue;

                    string length = lengthNode.InnerText;

                    // Get the download link
                    var downloadNode = node.SelectSingleNode("./td[@class='download']/a");
                    if (downloadNode == null) continue;

                    string downloadLink = downloadNode.GetAttributeValue("href", null);
                    if (downloadLink == null) continue;

                    SoundItems.Add(new SoundDownloadListItem(name, downloadLink, length));
                }

                LoadingMessageStackPanel.Visibility = Visibility.Collapsed;
                SoundListStackPanel.Visibility = Visibility.Visible;
                SoundListView.SelectAll();

                // Get the header
                var headerNode = document.DocumentNode.SelectSingleNode("//div[@id='music_info']/h2");
                string categoryName = null;

                if (headerNode != null)
                    categoryName = headerNode.InnerText;

                // Get the cover
                var coverNode = document.DocumentNode.SelectSingleNode("//div[@id='music_cover']/img");
                string imgSource = null;

                if (coverNode != null)
                    imgSource = coverNode.GetAttributeValue("src", null);


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
                        HideAllMessageElements();

                        Analytics.TrackEvent("AudioFileDownload-NotSupportedFormat", new Dictionary<string, string>
                        {
                            { "Link", input }
                        });

                        return;
                    }
                }
                catch (Exception e)
                {
                    HideAllMessageElements();

                    Crashes.TrackError(e, new Dictionary<string, string>
                    {
                        { "Link", input }
                    });

                    return;
                }

                // Get file type and file size
                AudioFileType = FileManager.FileTypeToExt(response.ContentType);
                long fileSize = response.ContentLength;

                // Try to get the file name
                Regex fileNameRegex = new Regex("^.+\\.\\w{3}$");
                AudioFileName = FileManager.loader.GetString("DownloadSoundsDialog-DefaultSoundName");
                bool defaultFileName = true;

                string lastPart = HttpUtility.UrlDecode(input.Split('/').Last());

                if (fileNameRegex.IsMatch(lastPart))
                {
                    var parts = lastPart.Split('.');
                    AudioFileName = string.Join(".", parts.Take(parts.Count() - 1));
                    defaultFileName = false;
                }

                AudioFileInfoTextBlock.Text = "";
                if (!defaultFileName) AudioFileInfoTextBlock.Text += string.Format("{0}: {1}\n", FileManager.loader.GetString("FileName"), AudioFileName);
                AudioFileInfoTextBlock.Text += string.Format("{0}: {1}\n", FileManager.loader.GetString("FileType"), AudioFileType);
                if (fileSize > 0) AudioFileInfoTextBlock.Text += string.Format("{0}: {1}", FileManager.loader.GetString("FileSize"), FileManager.GetFormattedSize((ulong)fileSize));
                AudioFileInfoTextBlock.Visibility = Visibility.Visible;

                LoadingMessageStackPanel.Visibility = Visibility.Collapsed;
                YoutubeInfoGrid.Visibility = Visibility.Collapsed;
                DownloadSoundsResult = DownloadSoundsResultType.AudioFile;
                ContentDialog.IsPrimaryButtonEnabled = true;
            }
        }

        private void DownloadSoundsYoutubeInfoDownloadPlaylistCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            if (PlaylistTitle.Length > 0)
                YoutubeInfoCreateCategoryForPlaylistCheckbox.Visibility = Visibility.Visible;
        }

        private void DownloadSoundsYoutubeInfoDownloadPlaylistCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            YoutubeInfoCreateCategoryForPlaylistCheckbox.Visibility = Visibility.Collapsed;
        }

        private void HideAllMessageElements()
        {
            LoadingMessageStackPanel.Visibility = Visibility.Collapsed;
            YoutubeInfoGrid.Visibility = Visibility.Collapsed;
            YoutubeInfoDownloadPlaylistStackPanel.Visibility = Visibility.Collapsed;
            AudioFileInfoTextBlock.Visibility = Visibility.Collapsed;
            SoundListStackPanel.Visibility = Visibility.Collapsed;
            SoundItems.Clear();
        }
    }
}
