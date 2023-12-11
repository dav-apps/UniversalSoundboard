using CommunityToolkit.WinUI.Collections;
using Microsoft.AppCenter.Analytics;
using MimeTypes;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Models;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace UniversalSoundboard.Pages
{
    public sealed partial class PublishSoundPage : Page
    {
        private ObservableCollection<DialogSoundListItem> SoundItems;
        private AdvancedCollectionView SoundsCollectionView;
        private ObservableCollection<string> Tags = new ObservableCollection<string>();
        private ObservableCollection<string> SelectedTags { get; set; }
        private Sound selectedItem = null;

        public PublishSoundPage()
        {
            InitializeComponent();

            SoundItems = new ObservableCollection<DialogSoundListItem>();
            SoundsCollectionView = new AdvancedCollectionView(SoundItems, true);
            SelectedTags = new ObservableCollection<string>();

            foreach (var sound in FileManager.itemViewHolder.AllSounds)
                SoundItems.Add(new DialogSoundListItem(sound));

            foreach (var tag in FileManager.itemViewHolder.Tags)
                Tags.Add(tag);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            SetThemeColors();

            Analytics.TrackEvent("PublishSoundPage-Navigation");
        }

        private void FilterAutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            SoundsCollectionView.Filter = item => ((DialogSoundListItem)item).Sound.Name.ToLower().Contains(sender.Text.ToLower());
        }

        private void SoundsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedItem = (SoundsListView.SelectedItem as DialogSoundListItem).Sound;

            SoundNameTextBox.Text = selectedItem.Name;
            SoundNameTextBox.IsEnabled = true;
            DescriptionRichEditBox.IsEnabled = true;
            TagsTokenBox.IsEnabled = true;
            PublishButton.IsEnabled = true;
        }

        private void SoundNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            PublishButton.IsEnabled = SoundNameTextBox.Text.Length > 2;
        }

        private void TagsTokenBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            Tags.Clear();

            var filteredTags = FileManager.itemViewHolder.Tags.FindAll(tag => tag.ToLower().Contains(sender.Text.ToLower()));

            foreach (var tag in filteredTags)
                Tags.Add(tag);
        }

        private async void PublishButton_Click(object sender, RoutedEventArgs e)
        {
            if (!await FileManager.DownloadFileOfSound(selectedItem)) return;

            Analytics.TrackEvent("PublishSoundPage-PublishButton-Click");

            FileManager.itemViewHolder.LoadingScreenMessage = FileManager.loader.GetString("PublishSoundPage-LoadingScreenMessage");
            FileManager.itemViewHolder.LoadingScreenVisible = true;

            // Create the sound on the server
            string description = null;
            DescriptionRichEditBox.Document.GetText(TextGetOptions.NoHidden, out description);

            var createSoundResult = await ApiManager.CreateSound(SoundNameTextBox.Text, description, SelectedTags.ToList());

            if (createSoundResult == null)
            {
                // Navigate back to the profile page
                MainPage.NavigateBack();
                FileManager.itemViewHolder.LoadingScreenVisible = false;
                FileManager.itemViewHolder.LoadingScreenMessage = "";
                return;
            }

            // Find the mime type of the selected sound file
            string mimeType = "audio/mpeg";

            try
            {
                mimeType = MimeTypeMap.GetMimeType(selectedItem.AudioFileTableObject.GetPropertyValue("ext"));
            }
            catch (Exception) { }

            // Upload the file
            await ApiManager.UploadSoundFile(createSoundResult.Uuid, selectedItem.AudioFile, mimeType);

            // Clear the cache for the StoreProfilePage
            ApiManager.ClearListSoundsCache();

            // Navigate back to the profile page
            MainPage.NavigateBack();
            FileManager.itemViewHolder.LoadingScreenVisible = false;
            FileManager.itemViewHolder.LoadingScreenMessage = "";
        }

        private void SetThemeColors()
        {
            RequestedTheme = FileManager.GetRequestedTheme();
            SolidColorBrush appThemeColorBrush = new SolidColorBrush(FileManager.GetApplicationThemeColor());
            ContentRoot.Background = appThemeColorBrush;
        }
    }
}
