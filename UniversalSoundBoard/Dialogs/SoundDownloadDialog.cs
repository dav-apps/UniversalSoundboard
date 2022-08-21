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
        private CheckBox CreateCategoryForPlaylistCheckbox;
        private TextBlock AudioFileInfoTextBlock;
        private StackPanel SoundListStackPanel;
        private ListView SoundListView;
        private TextBlock SoundListNumberTextBlock;
        private Button SoundListSelectAllButton;
        private ObservableCollection<SoundDownloadListItem> SoundItems;
        public string AudioFileName { get; private set; }
        public string AudioFileType { get; private set; }

        public string Url
        {
            get => UrlTextBox?.Text;
        }
        public bool CreateCategoryForPlaylist
        {
            get => (bool)CreateCategoryForPlaylistCheckbox.IsChecked;
        }

        public SoundDownloadDialog(DataTemplate soundDownloadListItemTemplate)
            : base(
                  FileManager.loader.GetString("SoundDownloadDialog-Title"),
                  FileManager.loader.GetString("Actions-Add"),
                  FileManager.loader.GetString("Actions-Cancel")
            )
        {
            SoundItems = new ObservableCollection<SoundDownloadListItem>();
            ContentDialog.IsPrimaryButtonEnabled = false;
            Content = GetContent(soundDownloadListItemTemplate);
        }

        private StackPanel GetContent(DataTemplate soundDownloadListItemTemplate)
        {
            StackPanel containerStackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Width = 400
            };

            TextBlock descriptionTextBlock = new TextBlock
            {
                Text = FileManager.loader.GetString("SoundDownloadDialog-Description"),
                TextWrapping = TextWrapping.WrapWholeWords
            };

            UrlTextBox = new TextBox
            {
                Margin = new Thickness(0, 20, 0, 0),
                PlaceholderText = FileManager.loader.GetString("SoundDownloadDialog-UrlTextBoxPlaceholder")
            };
            UrlTextBox.TextChanged += DownloadSoundsUrlTextBox_TextChanged;

			CreateLoadingMessageStackPanel();
			CreateYoutubeInfoGrid();

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
                Text = FileManager.loader.GetString("SoundDownloadDialog-RetrievingInfo"),
                FontSize = 14,
                Margin = new Thickness(10, 0, 0, 0)
            };

            LoadingMessageStackPanel.Children.Add(progressRing);
            LoadingMessageStackPanel.Children.Add(loadingMessage);
        }

        private void CreateYoutubeInfoGrid()
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

            YoutubeInfoGrid.Children.Add(YoutubeInfoImage);
            YoutubeInfoGrid.Children.Add(YoutubeInfoTextBlock);
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
                Text = string.Format(FileManager.loader.GetString("SoundDownloadDialog-SelectedSounds"), SoundItems.Count, SoundItems.Count)
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
                Margin = new Thickness(4, 10, 0, 0)
            };

            soundListNumberRelativePanel.Children.Add(SoundListNumberTextBlock);
            soundListNumberRelativePanel.Children.Add(SoundListSelectAllButton);

            CreateCategoryForPlaylistCheckbox = new CheckBox
            {
                Content = FileManager.loader.GetString("SoundDownloadDialog-CreateCategoryForPlaylist"),
                Margin = new Thickness(4, 12, 0, 0)
            };

            SoundListStackPanel.Children.Add(SoundListView);
            SoundListStackPanel.Children.Add(soundListNumberRelativePanel);
            SoundListStackPanel.Children.Add(CreateCategoryForPlaylistCheckbox);
        }

        private void UpdateSoundListNumberText()
        {
            SoundListNumberTextBlock.Text = string.Format(
                FileManager.loader.GetString("SoundDownloadDialog-SelectedSounds"),
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
            HideAllMessageElements();

            // Check if the input is a valid link
            string input = UrlTextBox.Text;
            Regex urlRegex = new Regex("^(https?:\\/\\/)?[\\w.-]+(\\.[\\w.-]+)+[\\w\\-._~/?#@&%\\+,;=]+");
            
            bool isUrl = urlRegex.IsMatch(input);

            if (!isUrl)
            {
                HideAllMessageElements();
                return;
            }

            LoadingMessageStackPanel.Visibility = Visibility.Visible;

            var youtubePlugin = new SoundDownloadYoutubePlugin(input);
            var zopharPlugin = new SoundDownloadZopharPlugin(input);

            if (youtubePlugin.IsUrlMatch())
            {
                try
                {
                    var result = await youtubePlugin.GetResult();

                    LoadingMessageStackPanel.Visibility = Visibility.Collapsed;
                    YoutubeInfoImage.Source = new BitmapImage(result.ImageUrl);
                    YoutubeInfoTextBlock.Text = result.Title;
                    YoutubeInfoGrid.Visibility = Visibility.Visible;
                    ContentDialog.IsPrimaryButtonEnabled = true;
                }
                catch (SoundDownloadException)
                {
                    HideAllMessageElements();
                    return;
                }
            }
            else if (zopharPlugin.IsUrlMatch())
            {
                try
                {
                    var result = await zopharPlugin.GetResult();

                    LoadingMessageStackPanel.Visibility = Visibility.Collapsed;
                    SoundItems.Clear();
                    
                    foreach (var soundItem in result.SoundItems)
                        SoundItems.Add(soundItem);

                    SoundListStackPanel.Visibility = Visibility.Visible;
                    SoundListView.SelectAll();
                }
                catch (SoundDownloadException)
                {
                    HideAllMessageElements();
                    return;
                }
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
                AudioFileName = FileManager.loader.GetString("SoundDownloadDialog-DefaultSoundName");
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
                ContentDialog.IsPrimaryButtonEnabled = true;
            }
        }

        private void HideAllMessageElements()
        {
            LoadingMessageStackPanel.Visibility = Visibility.Collapsed;
            YoutubeInfoGrid.Visibility = Visibility.Collapsed;
            AudioFileInfoTextBlock.Visibility = Visibility.Collapsed;
            SoundListStackPanel.Visibility = Visibility.Collapsed;
            SoundItems.Clear();
        }
    }
}
