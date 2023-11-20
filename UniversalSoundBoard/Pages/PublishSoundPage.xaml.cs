using CommunityToolkit.WinUI.Collections;
using MimeTypes;
using System;
using System.Collections.ObjectModel;
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
        Sound selectedItem = null;

        public PublishSoundPage()
        {
            InitializeComponent();

            SoundItems = new ObservableCollection<DialogSoundListItem>();
            SoundsCollectionView = new AdvancedCollectionView(SoundItems, true);

            foreach (var sound in FileManager.itemViewHolder.AllSounds)
                SoundItems.Add(new DialogSoundListItem(sound));
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            SetThemeColors();
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
            PublishButton.IsEnabled = true;
        }

        private async void PublishButton_Click(object sender, RoutedEventArgs e)
        {
            FileManager.itemViewHolder.LoadingScreenMessage = "Sound wird hochgeladen...";
            FileManager.itemViewHolder.LoadingScreenVisible = true;

            // Create the sound on the server
            string description = null;
            DescriptionRichEditBox.Document.GetText(TextGetOptions.None, out description);

            var createSoundResult = await ApiManager.CreateSound(SoundNameTextBox.Text, description);

            if (createSoundResult == null)
            {
                // Navigate back to the profile page
                MainPage.NavigateBack();
                FileManager.itemViewHolder.LoadingScreenVisible = false;
                FileManager.itemViewHolder.LoadingScreenMessage = "";
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
