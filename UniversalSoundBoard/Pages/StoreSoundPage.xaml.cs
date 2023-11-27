using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UniversalSoundboard.Components;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Dialogs;
using UniversalSoundboard.Models;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace UniversalSoundboard.Pages
{
    public sealed partial class StoreSoundPage : Page
    {
        private SoundResponse soundItem;
        private MediaPlayer mediaPlayer;
        private Uri sourceUri;
        private bool isPlaying = false;
        private bool isDownloading = false;
        private int downloadProgress = 0;

        public StoreSoundPage()
        {
            InitializeComponent();
            mediaPlayer = new MediaPlayer();
            mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            SetThemeColors();
            UpdatePlayPauseButtonUI();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter == null)
                return;

            soundItem = e.Parameter as SoundResponse;

            if (soundItem.Source != null)
                sourceUri = new Uri(soundItem.Source);

            mediaPlayer.Source = MediaSource.CreateFromUri(new Uri(soundItem.AudioFileUrl));
            Bindings.Update();

            // Load the entire sound from the API
            soundItem = await ApiManager.RetrieveSound(soundItem.Uuid);
            Bindings.Update();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            mediaPlayer.Pause();
        }

        private async void MediaPlayer_MediaEnded(MediaPlayer sender, object args)
        {
            isPlaying = false;

            await MainPage.dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                UpdatePlayPauseButtonUI();
            });
        }

        private void SetThemeColors()
        {
            RequestedTheme = FileManager.GetRequestedTheme();
            SolidColorBrush appThemeColorBrush = new SolidColorBrush(FileManager.GetApplicationThemeColor());
            ContentRoot.Background = appThemeColorBrush;
        }

        private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (isPlaying)
                mediaPlayer.Pause();
            else
                mediaPlayer.Play();

            isPlaying = !isPlaying;
            UpdatePlayPauseButtonUI();
        }

        private void UpdatePlayPauseButtonUI()
        {
            if (isPlaying)
            {
                PlayPauseButton.Content = "\uE103";
                PlayPauseButtonToolTip.Text = FileManager.loader.GetString("PauseButtonToolTip");
            }
            else
            {
                PlayPauseButton.Content = "\uE102";
                PlayPauseButtonToolTip.Text = FileManager.loader.GetString("PlayButtonToolTip");
            }
        }

        private async void AddToSoundboardButton_Click(object sender, RoutedEventArgs e)
        {
            var addToSoundboardDialog = new StoreAddToSoundboardDialog();
            addToSoundboardDialog.PrimaryButtonClick += AddToSoundboardDialog_PrimaryButtonClick;
            await addToSoundboardDialog.ShowAsync();
        }

        private async void AddToSoundboardDialog_PrimaryButtonClick(Dialog sender, ContentDialogButtonClickEventArgs args)
        {
            var dialog = sender as StoreAddToSoundboardDialog;

            // Get the selected categories
            List<Guid> categoryUuids = new List<Guid>();

            foreach (var item in dialog.SelectedItems)
                categoryUuids.Add((Guid)((CustomTreeViewNode)item).Tag);

            // Start downloading the audio file
            isDownloading = true;
            downloadProgress = 0;

            var progress = new Progress<int>((int value) => DownloadProgress(value));
            var cancellationTokenSource = new CancellationTokenSource();
            bool downloadSuccess = false;

            // Create a file in the cache
            StorageFolder cacheFolder = ApplicationData.Current.LocalCacheFolder;
            StorageFile targetFile = await cacheFolder.CreateFileAsync(string.Format("storedownload.{0}", soundItem.Type), CreationCollisionOption.GenerateUniqueName);

            await Task.Run(async () =>
            {
                downloadSuccess = (
                    await FileManager.DownloadBinaryDataToFile(
                        targetFile,
                        new Uri(soundItem.AudioFileUrl),
                        progress,
                        cancellationTokenSource.Token
                    )
                ).Key;
            });

            isDownloading = false;
            Bindings.Update();

            // Save the sound in the database
            Guid uuid = await FileManager.CreateSoundAsync(
                null,
                soundItem.Name,
                categoryUuids,
                targetFile,
                null,
                soundItem.Source
            );

            // Add the sound to the list
            await FileManager.AddSound(uuid);
        }

        private void DownloadProgress(int value)
        {
            downloadProgress = value;
            Bindings.Update();
        }

        private void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            MainPage.NavigateToPage(
                typeof(StoreProfilePage),
                soundItem.User.Id,
                new DrillInNavigationTransitionInfo()
            );
        }

        private void TagsGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            MainPage.NavigateToPage(
                typeof(StoreSearchPage),
                e.ClickedItem,
                new DrillInNavigationTransitionInfo()
            );
        }
    }
}
