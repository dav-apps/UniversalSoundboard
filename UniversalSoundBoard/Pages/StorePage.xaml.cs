using CommunityToolkit.WinUI;
using Sentry;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using UniversalSoundboard.Components;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Models;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace UniversalSoundboard.Pages
{
    public sealed partial class StorePage : Page
    {
        List<SoundResponse> soundsOfTheDay = new List<SoundResponse>();
        List<SoundResponse> recentlyAddedSounds = new List<SoundResponse>();
        ObservableCollection<string> tags = new ObservableCollection<string>();
        MediaPlayer mediaPlayer;
        StoreSoundTileTemplate currentSoundItemTemplate;
        bool soundsOfTheDayLoading = true;
        bool recentlyAddedSoundsLoading = true;
        bool tagsLoading = true;
        bool loadFailed;
        bool loading;

        public StorePage()
        {
            InitializeComponent();

            mediaPlayer = new MediaPlayer
            {
                Volume = (double)FileManager.itemViewHolder.Volume / 100
            };

            mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            SetThemeColors();
            LoadSounds();
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

        private void SetThemeColors()
        {
            RequestedTheme = FileManager.GetRequestedTheme();
            SolidColorBrush appThemeColorBrush = new SolidColorBrush(FileManager.GetApplicationThemeColor());
            ContentRoot.Background = appThemeColorBrush;
        }

        private async void LoadSounds()
        {
            if (loading) return;
            loading = true;
            loadFailed = false;
            soundsOfTheDayLoading = recentlyAddedSoundsLoading = tagsLoading = true;
            Bindings.Update();
            await Task.WhenAll(
                LoadSection(LoadSoundsOfTheDay, "Store.LatestRandom"),
                LoadSection(LoadRecentlyAddedSounds, "Store.RecentlyAdded"),
                LoadSection(LoadTags, "Store.Tags"));
            soundsOfTheDayLoading = recentlyAddedSoundsLoading = tagsLoading = false;
            loading = false;
            Bindings.Update();
        }

        private async Task LoadSection(Func<Task> load, string operation)
        {
            try { await load(); }
            catch (Exception exception)
            {
                loadFailed = true;
                SentrySdk.CaptureException(exception, scope => scope.SetTag("store.operation", operation));
            }
        }

        private void RetryButton_Click(object sender, RoutedEventArgs e) => LoadSounds();

        private async Task LoadSoundsOfTheDay()
        {
            var soundsOfTheDayResult = await ApiManager.ListSounds(random: true);
            if (soundsOfTheDayResult?.Items == null) { loadFailed = true; return; }

            soundsOfTheDay = soundsOfTheDayResult.Items;
            soundsOfTheDayLoading = false;

            Bindings.Update();
        }

        private async Task LoadRecentlyAddedSounds()
        {
            var recentlyAddedSoundsResult = await ApiManager.ListSounds(latest: true);
            if (recentlyAddedSoundsResult?.Items == null) { loadFailed = true; return; }

            recentlyAddedSounds = recentlyAddedSoundsResult.Items;
            recentlyAddedSoundsLoading = false;

            Bindings.Update();
        }

        private async Task LoadTags()
        {
            // Get all tags from the API
            int totalTags = 0;

            do
            {
                var listTagsResult = await ApiManager.ListTags(limit: 500, offset: FileManager.itemViewHolder.Tags.Count);
                if (listTagsResult?.Items == null) { loadFailed = true; break; }
                if (listTagsResult.Items.Count == 0) break;

                totalTags = listTagsResult.Total;

                foreach (var item in listTagsResult.Items)
                    FileManager.itemViewHolder.Tags.Add(item.Name);

            } while (FileManager.itemViewHolder.Tags.Count < totalTags);

            // Copy the tags list
            List<string> originalTags = new List<string>();

            foreach (string tag in FileManager.itemViewHolder.Tags)
                originalTags.Add(tag);
            
            // Select tags randomly
            Random random = new Random();
            tags.Clear();

            for (int i = 0; i < originalTags.Count; i++)
            {
                int randomIndex = random.Next(originalTags.Count);

                tags.Add(originalTags.ElementAt(randomIndex));
                originalTags.RemoveAt(randomIndex);
            }

            tagsLoading = false;
            Bindings.Update();
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

        private async void SoundsOfTheDayGridView_StoreSoundTileTemplate_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            await Task.Delay(1);
            var delta = e.GetCurrentPoint((UIElement)sender).Properties.MouseWheelDelta;
            ScrollHorizontalGridView(SoundsOfTheDayGridView, delta);
        }

        private async void RecentlyAddedSoundsGridView_StoreSoundTileTemplate_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            await Task.Delay(1);
            var delta = e.GetCurrentPoint((UIElement)sender).Properties.MouseWheelDelta;
            ScrollHorizontalGridView(RecentlyAddedSoundsGridView, delta);
        }

        private void ScrollHorizontalGridView(GridView gridView, int delta)
        {
            ScrollViewer scrollViewer = gridView.FindDescendant<ScrollViewer>();
            scrollViewer?.ChangeView(scrollViewer.HorizontalOffset - delta, null, null);
        }

        private void SoundsGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var soundItem = e.ClickedItem as SoundResponse;
            var gridView = sender as GridView;

            SentrySdk.CaptureMessage("StorePage-SoundsGridView-ItemClick", scope =>
            {
                scope.SetTags(new Dictionary<string, string>
                {
                    { "SoundUuid", soundItem.Uuid },
                    { "SoundName", soundItem.Name },
                    { "Section", gridView.Name }
                });
            });

            MainPage.NavigateToPage(
                typeof(StoreSoundPage),
                soundItem,
                new DrillInNavigationTransitionInfo()
            );
        }

        private void TagsGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            SentrySdk.CaptureMessage("StorePage-TagsGridView-ItemClick", scope =>
            {
                scope.SetTag("Tag", (string)e.ClickedItem);
            });

            MainPage.NavigateToPage(
                typeof(StoreSearchPage),
                e.ClickedItem,
                new DrillInNavigationTransitionInfo()
            );
        }
    }
}
