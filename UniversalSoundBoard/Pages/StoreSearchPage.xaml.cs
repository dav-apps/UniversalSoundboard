using Microsoft.AppCenter.Analytics;
using System;
using System.Collections.Generic;
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
    public sealed partial class StoreSearchPage : Page
    {
        const int itemsPerPage = 15;
        ObservableCollection<SoundResponse> sounds = new ObservableCollection<SoundResponse>();
        MediaPlayer mediaPlayer;
        StoreSoundTileTemplate currentSoundItemTemplate;
        bool isLoading = false;
        bool loadMoreButtonVisible = false;
        int currentPage = 0;

        public StoreSearchPage()
        {
            InitializeComponent();
            ContentRoot.DataContext = FileManager.itemViewHolder;

            mediaPlayer = new MediaPlayer
            {
                Volume = (double)FileManager.itemViewHolder.Volume / 100
            };

            mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            SetThemeColors();

            if (SearchAutoSuggestBox.Text.Length == 0)
            {
                // Focus on the search input
                SearchAutoSuggestBox.Focus(FocusState.Programmatic);
            }
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            string searchText = e.Parameter as string;

            if (searchText != null)
            {
                Analytics.TrackEvent("StoreSearchPage-Navigation", new Dictionary<string, string>
                {
                    { "SearchQuery", searchText }
                });

                SearchAutoSuggestBox.Text = searchText;
                isLoading = true;
                await LoadSounds(searchText);
            }
            else
            {
                Analytics.TrackEvent("StoreSearchPage-Navigation");
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            mediaPlayer.Pause();
        }

        private async void MediaPlayer_MediaEnded(MediaPlayer sender, object args)
        {
            await MainPage.dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                currentSoundItemTemplate.PlaybackStopped();
            });
        }

        private async void SearchAutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.QueryText.Length == 0)
                sounds.Clear();
            else
                await LoadSounds(args.QueryText);
        }

        private void SoundsGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var item = e.ClickedItem as SoundResponse;

            Analytics.TrackEvent("StoreSearchPage-SoundsGridView-ItemClick", new Dictionary<string, string>
            {
                { "SoundUuid", item.Uuid },
                { "SoundName", item.Name }
            });

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

        private async void LoadMoreButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadSounds(SearchAutoSuggestBox.Text, true);
        }

        private void SetThemeColors()
        {
            RequestedTheme = FileManager.GetRequestedTheme();
            SolidColorBrush appThemeColorBrush = new SolidColorBrush(FileManager.GetApplicationThemeColor());
            ContentRoot.Background = appThemeColorBrush;
        }

        private async Task LoadSounds(string queryText, bool nextPage = false)
        {
            if (nextPage)
            {
                Analytics.TrackEvent("StoreSearchPage-LoadMoreButton-Click", new Dictionary<string, string>
                {
                    { "SearchQuery", queryText }
                });
            }
            else
            {
                sounds.Clear();

                Analytics.TrackEvent("StoreSearchPage-Search", new Dictionary<string, string>
                {
                    { "SearchQuery", queryText }
                });
            }

            currentPage = nextPage ? currentPage + 1 : 0;
            isLoading = true;
            loadMoreButtonVisible = false;
            Bindings.Update();

            ListResponse<SoundResponse> listSoundsResponse = await ApiManager.ListSounds(
                query: queryText,
                limit: itemsPerPage,
                offset: currentPage * itemsPerPage
            );

            isLoading = false;
            loadMoreButtonVisible = true;
            Bindings.Update();

            if (listSoundsResponse.Items == null) return;

            foreach (var sound in listSoundsResponse.Items)
                sounds.Add(sound);
        }
    }
}
