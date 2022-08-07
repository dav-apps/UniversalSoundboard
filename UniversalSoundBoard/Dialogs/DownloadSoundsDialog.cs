﻿using davClassLibrary;
using DotNetTools.SharpGrabber;
using DotNetTools.SharpGrabber.Grabbed;
using Google.Apis.YouTube.v3.Data;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using UniversalSoundboard.Common;
using UniversalSoundboard.DataAccess;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using WinUI = Microsoft.UI.Xaml.Controls;

namespace UniversalSoundboard.Dialogs
{
    public class DownloadSoundsDialog : Dialog
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

        public DownloadSoundsDialog(Style infoButtonStyle)
            : base(
                  FileManager.loader.GetString("DownloadSoundsDialog-Title"),
                  FileManager.loader.GetString("Actions-Add"),
                  FileManager.loader.GetString("Actions-Cancel")
            )
        {
            ContentDialog.IsPrimaryButtonEnabled = false;
            Content = GetContent(infoButtonStyle);
        }

        private StackPanel GetContent(Style infoButtonStyle)
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

            AudioFileInfoTextBlock = new TextBlock
            {
                Margin = new Thickness(6, 10, 0, 0),
                Visibility = Visibility.Collapsed
            };

            containerStackPanel.Children.Add(descriptionTextBlock);
            containerStackPanel.Children.Add(UrlTextBox);
            containerStackPanel.Children.Add(LoadingMessageStackPanel);
            containerStackPanel.Children.Add(YoutubeInfoGrid);
            containerStackPanel.Children.Add(AudioFileInfoTextBlock);

            return containerStackPanel;
        }

        private async void DownloadSoundsUrlTextBox_TextChanged(object sender, TextChangedEventArgs args)
        {
            ContentDialog.IsPrimaryButtonEnabled = false;
            DownloadSoundsResult = DownloadSoundsResultType.None;
            HideAllMessageElementsInDownloadSoundsContentDialog();

            // Check if the input is a valid link
            string input = UrlTextBox.Text;

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
                        HideAllMessageElementsInDownloadSoundsContentDialog();
                        return;
                    }
                }
                catch (Exception e)
                {
                    HideAllMessageElementsInDownloadSoundsContentDialog();

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
                catch (Exception e)
                {
                    HideAllMessageElementsInDownloadSoundsContentDialog();

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
                Regex fileNameRegex = new Regex("^[\\w\\.\\+\\-_ ]+\\.\\w{3}$");
                AudioFileName = FileManager.loader.GetString("DownloadSoundsDialog-DefaultSoundName");
                bool defaultFileName = true;

                string lastPart = input.Split('/').Last();

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

        private void HideAllMessageElementsInDownloadSoundsContentDialog()
        {
            LoadingMessageStackPanel.Visibility = Visibility.Collapsed;
            YoutubeInfoGrid.Visibility = Visibility.Collapsed;
            YoutubeInfoDownloadPlaylistStackPanel.Visibility = Visibility.Collapsed;
            AudioFileInfoTextBlock.Visibility = Visibility.Collapsed;
        }
    }
}