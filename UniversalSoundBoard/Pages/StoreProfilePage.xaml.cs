using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using UniversalSoundboard.Components;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Models;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace UniversalSoundboard.Pages
{
    public sealed partial class StoreProfilePage : Page
    {
        const int itemsPerPage = 15;
        ObservableCollection<SoundResponse> sounds = new ObservableCollection<SoundResponse>();
        MediaPlayer mediaPlayer;
        StoreSoundTileTemplate currentSoundItemTemplate;
        private int userId = 0;
        private string numberOfSoundsText = "";
        private bool numberOfSoundsTextVisible = false;
        private bool isLoadMoreButtonVisible = false;
        private bool isLoading = true;
        private int currentPage = 0;

        public StoreProfilePage()
        {
            InitializeComponent();
            mediaPlayer = new MediaPlayer();
            mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            SetThemeColors();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter != null)
                userId = (int)e.Parameter;

            await LoadSounds();
        }

        private async void MediaPlayer_MediaEnded(MediaPlayer sender, object args)
        {
            await MainPage.dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                currentSoundItemTemplate.PlaybackStopped();
            });
        }

        private void SetThemeColors()
        {
            RequestedTheme = FileManager.GetRequestedTheme();
            SolidColorBrush appThemeColorBrush = new SolidColorBrush(FileManager.GetApplicationThemeColor());
            ContentRoot.Background = appThemeColorBrush;
        }

        private async Task LoadSounds(bool nextPage = false)
        {
            currentPage = nextPage ? currentPage + 1 : 0;
            isLoading = true;
            isLoadMoreButtonVisible = false;
            Bindings.Update();

            ListResponse<SoundResponse> listSoundsResponse;

            if (userId == 0)
            {
                listSoundsResponse = await ApiManager.ListSounds(
                    mine: true,
                    limit: itemsPerPage,
                    offset: currentPage * itemsPerPage
                );
            }
            else
            {
                listSoundsResponse = await ApiManager.ListSounds(
                    userId: userId,
                    limit: itemsPerPage,
                    offset: currentPage * itemsPerPage
                );
            }

            isLoading = false;
            Bindings.Update();

            if (listSoundsResponse.Items == null) return;

            isLoadMoreButtonVisible = listSoundsResponse.Total > currentPage * itemsPerPage + itemsPerPage;
            numberOfSoundsText = string.Format(FileManager.loader.GetString("StoreProfilePage-NumberOfSounds"), listSoundsResponse.Total);
            numberOfSoundsTextVisible = listSoundsResponse.Total > 1;
            Bindings.Update();

            foreach (var sound in listSoundsResponse.Items)
                sounds.Add(sound);
        }

        private void SoundsGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var item = e.ClickedItem as SoundResponse;

            if (item.AudioFileUrl != null)
            {
                MainPage.NavigateToPage(
                    typeof(StoreSoundPage),
                    item,
                    new DrillInNavigationTransitionInfo()
                );
            }
        }

        private void StoreSoundTileTemplate_Play(object sender, EventArgs e)
        {
            if (currentSoundItemTemplate != null)
                currentSoundItemTemplate.PlaybackStopped();

            currentSoundItemTemplate = sender as StoreSoundTileTemplate;
            mediaPlayer.Pause();
            mediaPlayer.Source = MediaSource.CreateFromUri(new Uri(currentSoundItemTemplate.SoundItem.AudioFileUrl));
            mediaPlayer.Play();
        }

        private void StoreSoundTileTemplate_Pause(object sender, EventArgs e)
        {
            mediaPlayer.Pause();
        }

        private void PublishSoundButton_Click(object sender, RoutedEventArgs e)
        {
            MainPage.NavigateToPage(typeof(PublishSoundPage), new DrillInNavigationTransitionInfo());
        }

        private async void LoadMoreButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadSounds(true);
        }
    }
}
