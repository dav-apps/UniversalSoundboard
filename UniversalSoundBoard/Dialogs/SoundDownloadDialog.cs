using Microsoft.AppCenter.Analytics;
using Microsoft.Toolkit.Uwp.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
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
        private TextBlock AudioFileInfoTextBlock;
        private StackPanel SoundListStackPanel;
        private ListView SoundListView;
        private TextBlock SoundListNumberTextBlock;
        private Button SoundListSelectAllButton;
        private CheckBox CreateCategoryForPlaylistCheckbox;
        private ObservableCollection<SoundDownloadItem> SoundItems;

        public SoundDownloadResult Result { get; private set; }

        public SoundDownloadDialog(DataTemplate soundDownloadListItemTemplate)
            : base(
                  FileManager.loader.GetString("SoundDownloadDialog-Title"),
                  FileManager.loader.GetString("Actions-Add"),
                  FileManager.loader.GetString("Actions-Cancel")
            )
        {
            SoundItems = new ObservableCollection<SoundDownloadItem>();
            ContentDialog.IsPrimaryButtonEnabled = false;
            Content = GetContent(soundDownloadListItemTemplate);
            ContentDialog.Opened += ContentDialog_Opened;
        }

        private void ContentDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
        {
            if (UrlTextBox != null)
                UrlTextBox.Focus(FocusState.Keyboard);
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

            HyperlinkButton infoHyperlinkButton = new HyperlinkButton
            {
                Content = FileManager.loader.GetString("SoundDownloadDialog-DescriptionLink"),
                NavigateUri = new Uri("https://github.com/dav-apps/UniversalSoundboard/wiki/Sound-downloads"),
                Padding = new Thickness(0)
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
            containerStackPanel.Children.Add(infoHyperlinkButton);
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

            CreateCategoryForPlaylistCheckbox.Checked += CreateCategoryForPlaylistCheckbox_Checked;
            CreateCategoryForPlaylistCheckbox.Unchecked += CreateCategoryForPlaylistCheckbox_Unchecked;

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

        private void UpdateSelectedSoundItems()
        {
            // De-select all items
            foreach (var item in SoundItems)
                item.IsSelected = false;

            // Select all selected items
            foreach (var range in SoundListView.SelectedRanges)
                for (int i = range.FirstIndex; i < range.LastIndex + 1; i++)
                    SoundItems.ElementAt(i).IsSelected = true;
        }

        private async void DownloadSoundsUrlTextBox_TextChanged(object sender, TextChangedEventArgs args)
        {
            ContentDialog.IsPrimaryButtonEnabled = false;
            HideAllMessageElements();

            string input = UrlTextBox.Text;

            var audioFilePlugin = new SoundDownloadPlugin(input);
            var youtubePlugin = new SoundDownloadYoutubePlugin(input);
            var zopharPlugin = new SoundDownloadZopharPlugin(input);
            
            // Check if the input is a valid link
            if (!audioFilePlugin.IsUrlMatch())
            {
                HideAllMessageElements();
                return;
            }

            LoadingMessageStackPanel.Visibility = Visibility.Visible;

            if (youtubePlugin.IsUrlMatch())
            {
                try
                {
                    var result = await youtubePlugin.GetResult() as SoundDownloadYoutubePluginResult;

                    LoadingMessageStackPanel.Visibility = Visibility.Collapsed;
                    YoutubeInfoImage.Source = new BitmapImage(new Uri(result.ImageUrl));
                    
                    var selectedSoundItem = result.SoundItems.Find(item => item.IsSelected);
                    if (selectedSoundItem != null) YoutubeInfoTextBlock.Text = selectedSoundItem.Name;

                    YoutubeInfoGrid.Visibility = Visibility.Visible;

                    if (result.SoundItems.Count > 1)
                    {
                        SoundItems.Clear();
                        int selectedItemIndex = 0;
                        int i = 0;

                        foreach (var soundItem in result.SoundItems)
                        {
                            SoundItems.Add(soundItem);

                            if (soundItem.IsSelected)
                            {
                                SoundListView.SelectedItems.Add(soundItem);

                                if (selectedItemIndex == 0)
                                    selectedItemIndex = i;
                            }

                            i++;
                        }

                        SoundListStackPanel.Visibility = Visibility.Visible;
                        UpdateSoundListNumberText();
                        await Task.Delay(10);
                        await SoundListView.SmoothScrollIntoViewWithIndexAsync(selectedItemIndex, ScrollItemPlacement.Center);
                    }
                    else if (result.SoundItems.Count == 1)
                    {
                        SoundItems.Add(result.SoundItems.First());
                    }

                    ContentDialog.IsPrimaryButtonEnabled = true;
                    Result = new SoundDownloadResult(SoundItems, result.PlaylistTitle);
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
                    var result = await zopharPlugin.GetResult() as SoundDownloadZopharPluginResult;

                    LoadingMessageStackPanel.Visibility = Visibility.Collapsed;

                    if (result.SoundItems.Count == 0)
                        throw new SoundDownloadException();

                    SoundItems.Clear();
                    
                    foreach (var soundItem in result.SoundItems)
                        SoundItems.Add(soundItem);

                    SoundListStackPanel.Visibility = Visibility.Visible;
                    SoundListView.SelectAll();
                    Result = new SoundDownloadResult(SoundItems, result.PlaylistTitle);
                }
                catch (SoundDownloadException)
                {
                    HideAllMessageElements();
                    return;
                }
            }
            else
            {
                try
                {
                    var result = await audioFilePlugin.GetResult();

                    if (result.SoundItems.Count == 0)
                        throw new SoundDownloadException();

                    var soundItem = result.SoundItems.First();

                    LoadingMessageStackPanel.Visibility = Visibility.Collapsed;
                    YoutubeInfoGrid.Visibility = Visibility.Collapsed;
                    AudioFileInfoTextBlock.Text = "";

                    string audioFileName = FileManager.loader.GetString("SoundDownloadDialog-DefaultSoundName");

                    if (soundItem.Name != null)
                    {
                        AudioFileInfoTextBlock.Text += string.Format("{0}: {1}\n", FileManager.loader.GetString("FileName"), soundItem.Name);
                        audioFileName = soundItem.Name;
                    }

                    AudioFileInfoTextBlock.Text += string.Format("{0}: {1}\n", FileManager.loader.GetString("FileType"), soundItem.AudioFileExt);

                    if (soundItem.AudioFileSize > 0)
                        AudioFileInfoTextBlock.Text += string.Format("{0}: {1}", FileManager.loader.GetString("FileSize"), FileManager.GetFormattedSize((ulong)soundItem.AudioFileSize));

                    AudioFileInfoTextBlock.Visibility = Visibility.Visible;
                    ContentDialog.IsPrimaryButtonEnabled = true;

                    Result = new SoundDownloadResult(new ObservableCollection<SoundDownloadItem> { soundItem }, null);
                }
                catch (SoundDownloadException)
                {
                    HideAllMessageElements();
                    return;
                }
            }

            Analytics.TrackEvent("SoundDownloadDialog-UrlChanged", new Dictionary<string, string>
            {
                { "Url", input }
            });
        }

        private void SoundListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateSoundListNumberText();
            UpdateSelectedSoundItems();
        }

        private void SoundListSelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (SoundListView.SelectedItems.Count == SoundItems.Count)
                SoundListView.DeselectAll();
            else
                SoundListView.SelectAll();
        }

        private void CreateCategoryForPlaylistCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            if (Result == null) return;
            Result.CreateCategoryForPlaylist = true;
        }

        private void CreateCategoryForPlaylistCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (Result == null) return;
            Result.CreateCategoryForPlaylist = false;
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
