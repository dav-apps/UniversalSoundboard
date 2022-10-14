using davClassLibrary;
using Microsoft.AppCenter.Analytics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using UniversalSoundboard.Common;
using UniversalSoundboard.Components;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Dialogs;
using UniversalSoundboard.Models;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using WinUI = Microsoft.UI.Xaml.Controls;

namespace UniversalSoundboard.Pages
{
    public sealed partial class SoundPage : Page
    {
        public static bool soundsPivotSelected = true;
        private bool skipSoundListSelectionChangedEvent = false;
        Guid reorderedItem = Guid.Empty;
        public static VerticalPosition bottomPlayingSoundsBarPosition = VerticalPosition.Bottom;
        private static bool playingSoundsLoaded = false;
        public static bool playingSoundsRendered = false;
        private double maxBottomPlayingSoundsBarHeight = 500;
        private bool isManipulatingBottomPlayingSoundsBar = false;
        private bool snapBottomPlayingSoundsBarAnimationRunning = false;
        private static PlayingSound nextSinglePlayingSoundToOpen;
        ObservableCollection<PlayingSoundItemContainer> playingSoundItemContainers = new ObservableCollection<PlayingSoundItemContainer>();
        ObservableCollection<PlayingSoundItemContainer> reversedPlayingSoundItemContainers = new ObservableCollection<PlayingSoundItemContainer>();
        bool startMessageButtonsEnabled = true;
        bool canReorderItems = false;
        bool isBottomPlayingSoundsBarVisible = false;
        Visibility startMessageVisibility = Visibility.Collapsed;
        Visibility emptyCategoryMessageVisibility = Visibility.Collapsed;
        public static DataTemplate hotkeyItemTemplate;
        int initialBottomPlayingSoundsCount = 0;
        int loadedBottomPlayingSoundItems = 0;

        public SoundPage()
        {
            InitializeComponent();
            ContentRoot.DataContext = FileManager.itemViewHolder;

            // Subscribe to events
            FileManager.itemViewHolder.PropertyChanged += ItemViewHolder_PropertyChanged;
            FileManager.itemViewHolder.PlayingSoundsLoaded += ItemViewHolder_PlayingSoundsLoaded;
            FileManager.itemViewHolder.Sounds.CollectionChanged += ItemViewHolder_Sounds_CollectionChanged;
            FileManager.itemViewHolder.FavouriteSounds.CollectionChanged += ItemViewHolder_FavouriteSounds_CollectionChanged;
            FileManager.itemViewHolder.PlayingSounds.CollectionChanged += ItemViewHolder_PlayingSounds_CollectionChanged;
            FileManager.itemViewHolder.SelectAllSounds += ItemViewHolder_SelectAllSounds;
            FileManager.itemViewHolder.PlayingSoundItemShowSoundsListAnimationStarted += ItemViewHolder_PlayingSoundItemShowSoundsListAnimationStarted;
            FileManager.itemViewHolder.PlayingSoundItemShowSoundsListAnimationEnded += ItemViewHolder_PlayingSoundItemShowSoundsListAnimationEnded;
            FileManager.itemViewHolder.PlayingSoundItemHideSoundsListAnimationStarted += ItemViewHolder_PlayingSoundItemHideSoundsListAnimationStarted;
            FileManager.itemViewHolder.PlayingSoundItemHideSoundsListAnimationEnded += ItemViewHolder_PlayingSoundItemHideSoundsListAnimationEnded;
            FileManager.itemViewHolder.ShowInAppNotification += ItemViewHolder_ShowInAppNotification;

            // Show all currently active InAppNotifications
            ShowAllInAppNotifications();

            // Enable or disable StartMessage buttons
            startMessageButtonsEnabled = !FileManager.itemViewHolder.Importing && FileManager.itemViewHolder.AppState != AppState.InitialSync;
            Bindings.Update();
        }

        #region Page event handlers
        private void SoundPage_Loaded(object sender, RoutedEventArgs e)
        {
            hotkeyItemTemplate = (DataTemplate)Resources["HotkeyItemTemplate"];
            soundsPivotSelected = true;

            AdjustLayout();
            UpdateCanReorderItems();
            LoadPlayingSoundItems();
        }

        private void SoundPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            AdjustLayout();
            UpdatePlayingSoundsList();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            FileManager.itemViewHolder.PropertyChanged -= ItemViewHolder_PropertyChanged;
            FileManager.itemViewHolder.PlayingSoundsLoaded -= ItemViewHolder_PlayingSoundsLoaded;
            FileManager.itemViewHolder.Sounds.CollectionChanged -= ItemViewHolder_Sounds_CollectionChanged;
            FileManager.itemViewHolder.FavouriteSounds.CollectionChanged -= ItemViewHolder_FavouriteSounds_CollectionChanged;
            FileManager.itemViewHolder.PlayingSounds.CollectionChanged -= ItemViewHolder_PlayingSounds_CollectionChanged;
            FileManager.itemViewHolder.SelectAllSounds -= ItemViewHolder_SelectAllSounds;
            FileManager.itemViewHolder.PlayingSoundItemShowSoundsListAnimationStarted -= ItemViewHolder_PlayingSoundItemShowSoundsListAnimationStarted;
            FileManager.itemViewHolder.PlayingSoundItemShowSoundsListAnimationEnded -= ItemViewHolder_PlayingSoundItemShowSoundsListAnimationEnded;
            FileManager.itemViewHolder.PlayingSoundItemHideSoundsListAnimationStarted -= ItemViewHolder_PlayingSoundItemHideSoundsListAnimationStarted;
            FileManager.itemViewHolder.PlayingSoundItemHideSoundsListAnimationEnded -= ItemViewHolder_PlayingSoundItemHideSoundsListAnimationEnded;
            FileManager.itemViewHolder.ShowInAppNotification -= ItemViewHolder_ShowInAppNotification;

            // Remove all InAppNotifications from the ContentGrid
            foreach (var ianItem in FileManager.InAppNotificationItems)
                if (ianItem.Sent && ContentGrid.Children.Contains(ianItem.InAppNotification))
                    ContentGrid.Children.Remove(ianItem.InAppNotification);

            base.OnNavigatedFrom(e);
        }

        private void ItemViewHolder_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals(ItemViewHolder.CurrentThemeKey))
                RequestedTheme = FileManager.GetRequestedTheme();
            else if (e.PropertyName.Equals(ItemViewHolder.AppStateKey) || e.PropertyName.Equals(ItemViewHolder.SelectedCategoryKey))
                UpdateMessagesVisibilities();
            else if (e.PropertyName.Equals(ItemViewHolder.SoundOrderKey) || e.PropertyName.Equals(ItemViewHolder.MultiSelectionEnabledKey))
                UpdateCanReorderItems();
        }

        private void ItemViewHolder_PlayingSoundsLoaded(object sender, EventArgs e)
        {
            LoadPlayingSoundItems();
            playingSoundsLoaded = true;
        }

        private async void ItemViewHolder_Sounds_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            await HandleSoundsCollectionChanged(e, false);
            UpdateMessagesVisibilities();
        }

        private async void ItemViewHolder_FavouriteSounds_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            await HandleSoundsCollectionChanged(e, true);
        }

        private void ItemViewHolder_PlayingSounds_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!playingSoundsLoaded) return;

            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                bool onlyPlayingSound = FileManager.itemViewHolder.PlayingSounds.Count == 1;

                if (onlyPlayingSound)
                {
                    // Reset the BottomPlayingSoundsBar to enable loading PlayingSoundItems
                    BottomPlayingSoundsBar.Translation = new Vector3(-10000, 0, 0);
                    BottomPlayingSoundsBar.Height = double.NaN;
                    BottomPlayingSoundsBar.Visibility = Visibility.Visible;
                }

                var playingSound = e.NewItems[0] as PlayingSound;

                var item1 = new PlayingSoundItemContainer(PlayingSoundsListView.Items.Count, playingSound);
                item1.Show += PlayingSoundItemContainer_Show;
                item1.Hide += PlayingSoundItemContainer_Hide;

                var item2 = new PlayingSoundItemContainer(PlayingSoundsListView.Items.Count, playingSound, !onlyPlayingSound);
                item2.Show += PlayingSoundItemContainer_Show;
                item2.Hide += PlayingSoundItemContainer_Hide;
                item2.Loaded += PlayingSoundItemContainer2_Loaded;

                playingSoundItemContainers.Add(item1);
                reversedPlayingSoundItemContainers.Insert(0, item2);
            }
        }

        private void ItemViewHolder_SelectAllSounds(object sender, RoutedEventArgs e)
        {
            skipSoundListSelectionChangedEvent = true;

            if (FileManager.itemViewHolder.ShowListView)
            {
                // Get the visible ListView
                ListView listView = GetVisibleListView();
                FileManager.itemViewHolder.SelectedSounds.Clear();

                if (listView.SelectedItems.Count == listView.Items.Count)
                {
                    // All items are selected, deselect all items
                    listView.DeselectRange(new ItemIndexRange(0, (uint)listView.Items.Count));
                }
                else
                {
                    // Select all items
                    listView.SelectAll();

                    // Add all sounds to the selected sounds
                    foreach (var sound in listView.Items)
                        FileManager.itemViewHolder.SelectedSounds.Add(sound as Sound);
                }
            }
            else
            {
                // Get the visible GridView
                GridView gridView = GetVisibleGridView();
                FileManager.itemViewHolder.SelectedSounds.Clear();

                if (gridView.SelectedItems.Count == gridView.Items.Count)
                {
                    // All items are selected, deselect all items
                    gridView.DeselectRange(new ItemIndexRange(0, (uint)gridView.Items.Count));
                }
                else
                {
                    // Select all items
                    gridView.SelectAll();

                    // Add all sounds to the selected sounds
                    foreach (var sound in gridView.Items)
                        FileManager.itemViewHolder.SelectedSounds.Add(sound as Sound);
                }
            }

            skipSoundListSelectionChangedEvent = false;
            UpdateSelectAllFlyoutText();
        }

        private void ItemViewHolder_PlayingSoundItemShowSoundsListAnimationStarted(object sender, PlayingSoundItemEventArgs args)
        {
            // Update the max height for the GridSplitter with the new height
            double newMaxHeight = BottomPlayingSoundsBarListView.ActualHeight + args.HeightDifference;
            if (newMaxHeight >= maxBottomPlayingSoundsBarHeight) newMaxHeight = maxBottomPlayingSoundsBarHeight;

            GridSplitterGridBottomRowDef.MaxHeight = newMaxHeight;

            if (bottomPlayingSoundsBarPosition == VerticalPosition.Top)
            {
                // Update the BottomPlayingSoundsBar height and the bottom row def max height to the new height, so that the animation is able to play
                double newHeight = BottomPlayingSoundsBarListView.ActualHeight + args.HeightDifference;
                if (newHeight >= maxBottomPlayingSoundsBarHeight) newHeight = maxBottomPlayingSoundsBarHeight;

                BottomPlayingSoundsBar.Height = newHeight;
            }
            else
            {
                // Setup and start the animation for increasing the height of the BottomPlayingSoundsBar
                double newHeight = BottomPlayingSoundsBarListView.ActualHeight + args.HeightDifference;
                if (newHeight >= maxBottomPlayingSoundsBarHeight) newHeight = maxBottomPlayingSoundsBarHeight;

                ShowSoundsListViewStoryboardAnimation.From = BottomPlayingSoundsBarListView.ActualHeight;
                ShowSoundsListViewStoryboardAnimation.To = newHeight;
                ShowSoundsListViewStoryboard.Begin();
            }

            // Trigger the animation start
            FileManager.itemViewHolder.TriggerPlayingSoundItemStartSoundsListAnimationEvent(this);
        }

        private void ItemViewHolder_PlayingSoundItemShowSoundsListAnimationEnded(object sender, PlayingSoundItemEventArgs args)
        {
            // Update the min and max height of the bottom row def
            UpdateGridSplitterRange();
        }

        private void ItemViewHolder_PlayingSoundItemHideSoundsListAnimationStarted(object sender, PlayingSoundItemEventArgs args)
        {
            // Set the min height of the splitter to 0, so that the animation is able to play
            GridSplitterGridBottomRowDef.MinHeight = 0;

            if (bottomPlayingSoundsBarPosition == VerticalPosition.Bottom)
            {
                // Setup and start the animation for decreasing the height of the BottomPlayingSoundsBar
                HideSoundsListViewStoryboardAnimation.From = BottomPlayingSoundsBarListView.ActualHeight;
                HideSoundsListViewStoryboardAnimation.To = args.HeightDifference;
                HideSoundsListViewStoryboard.Begin();
            }

            // Trigger the animation start
            FileManager.itemViewHolder.TriggerPlayingSoundItemStartSoundsListAnimationEvent(this);
        }

        private void ItemViewHolder_PlayingSoundItemHideSoundsListAnimationEnded(object sender, PlayingSoundItemEventArgs args)
        {
            // Update the min and max height of the bottom row def
            UpdateGridSplitterRange();
        }

        private void PlayingSoundItemContainer_Show(object sender, EventArgs e)
        {
            // Update the animation with the actual PlayingSoundItem height
            //PlayingSoundItemContainer itemContainer = sender as PlayingSoundItemContainer;
            //AnimateIncreasingBottomPlayingSoundBar(itemContainer.ContentHeight);

            //await Task.Delay(200);

            //UpdateGridSplitterRange();
            //SnapBottomPlayingSoundsBar();
        }

        private async void PlayingSoundItemContainer_Hide(object sender, EventArgs e)
        {
            if (isBottomPlayingSoundsBarVisible)
            {
                if (!FileManager.itemViewHolder.OpenMultipleSounds || FileManager.itemViewHolder.PlayingSounds.Count == 1)
                {
                    // Start the animation for hiding the BottomPlayingSoundsBar background / BottomPseudoContentGrid
                    GridSplitterGridBottomRowDef.MinHeight = 0;
                    StartSnapBottomPlayingSoundsBarAnimation(GridSplitterGridBottomRowDef.ActualHeight, 0);
                }
                else
                {
                    PlayingSoundItemContainer itemContainer = sender as PlayingSoundItemContainer;
                    AnimateIncreasingBottomPlayingSoundBar(-itemContainer.ContentHeight);
                }
            }

            await Task.Delay(200);

            if (nextSinglePlayingSoundToOpen != null)
            {
                FileManager.itemViewHolder.PlayingSounds.Add(nextSinglePlayingSoundToOpen);
                nextSinglePlayingSoundToOpen = null;
            }
            else if (FileManager.itemViewHolder.OpenMultipleSounds)
            {
                // Snap the BottomPlayingSoundsBar; for the case that the height of the removed PlayingSound was different
                UpdateGridSplitterRange();
                SnapBottomPlayingSoundsBar();
            }
        }

        private async void PlayingSoundItemContainer_Loaded(object sender, EventArgs e)
        {
            loadedBottomPlayingSoundItems++;

            // Wait for all initial items to be loaded
            if (loadedBottomPlayingSoundItems == initialBottomPlayingSoundsCount)
            {
                if (
                    FileManager.itemViewHolder.PlayingSoundsListVisible
                    && (
                        !FileManager.itemViewHolder.OpenMultipleSounds
                        || isBottomPlayingSoundsBarVisible
                    )
                )
                {
                    // Show the bottom playing sounds bar
                    // Wait for all animations to have ended
                    await Task.Delay(200);

                    playingSoundsLoaded = true;
                    playingSoundsRendered = true;

                    UpdatePlayingSoundsList();
                    InitBottomPlayingSoundsBarHeight();

                    await ShowBottomPlayingSoundsBar();
                }
            }
        }

        private async void PlayingSoundItemContainer2_Loaded(object sender, EventArgs e)
        {
            if (
                FileManager.itemViewHolder.PlayingSounds.Count == 1
                && FileManager.itemViewHolder.PlayingSoundsListVisible
                && (
                    !FileManager.itemViewHolder.OpenMultipleSounds
                    || isBottomPlayingSoundsBarVisible
                )
            )
            {
                // Show the bottom playing sounds bar
                UpdatePlayingSoundsList();
                InitBottomPlayingSoundsBarHeight();

                await ShowBottomPlayingSoundsBar();
            }
        }

        private async Task ShowBottomPlayingSoundsBar()
        {
            BottomPlayingSoundsBar.Background = Application.Current.Resources["NavigationViewHeaderBackgroundBrush"] as AcrylicBrush;
            BottomPseudoContentGrid.Background = new SolidColorBrush(Colors.Transparent);

            double firstItemHeight = GetFirstBottomPlayingSoundItemContentHeight();

            BottomPlayingSoundsBar.Translation = new Vector3(0, (float)firstItemHeight, 0);
            BottomPlayingSoundsBar.Opacity = 1;

            // Animate showing the BottomPlayingSoundsBar
            var compositor = Window.Current.Compositor;

            var translationAnimation = compositor.CreateVector3KeyFrameAnimation();
            translationAnimation.InsertKeyFrame(1.0f, new Vector3(0));
            translationAnimation.Duration = TimeSpan.FromMilliseconds(300);
            translationAnimation.Target = "Translation";

            BottomPlayingSoundsBar.StartAnimation(translationAnimation);

            await Task.Delay(300);

            // Animate showing the grid splitter
            var opacityAnimation = compositor.CreateScalarKeyFrameAnimation();
            opacityAnimation.InsertKeyFrame(1.0f, 1);
            opacityAnimation.Duration = TimeSpan.FromMilliseconds(300);
            opacityAnimation.Target = "Opacity";

            GridSplitterGrid.StartAnimation(opacityAnimation);

            await Task.Delay(300);

            BottomPlayingSoundsBar.Background = new SolidColorBrush(Colors.Transparent);
            BottomPseudoContentGrid.Background = Application.Current.Resources["NavigationViewHeaderBackgroundBrush"] as AcrylicBrush;
        }

        private void ItemViewHolder_ShowInAppNotification(object sender, ShowInAppNotificationEventArgs args)
        {
            foreach(var ianItem in FileManager.InAppNotificationItems)
            {
                if (ianItem.Sent) continue;

                // Calculate the bottom margin
                double marginBottom = 10;
                if (!FileManager.itemViewHolder.OpenMultipleSounds && FileManager.itemViewHolder.PlayingSounds.Count >= 1) marginBottom = 70;
                else if (isBottomPlayingSoundsBarVisible && FileManager.itemViewHolder.PlayingSounds.Count >= 1) marginBottom = 57;

                foreach (var item in FileManager.InAppNotificationItems)
                {
                    if (!item.Sent) continue;
                    marginBottom = marginBottom + 10 + item.MessageTextBlock.ActualHeight;
                }

                ianItem.InAppNotification.Margin = new Thickness(20, 0, 20, marginBottom);
                
                ContentGrid.Children.Add(ianItem.InAppNotification);

                ianItem.InAppNotification.Show(ianItem.Duration);
                ianItem.Sent = true;
            }
        }
        #endregion

        #region Helper methods
        private void AdjustLayout()
        {
            maxBottomPlayingSoundsBarHeight = Window.Current.Bounds.Height * 0.6;
            isBottomPlayingSoundsBarVisible = Window.Current.Bounds.Width < FileManager.mobileMaxWidth;
        }

        private void LoadPlayingSoundItems()
        {
            if (isBottomPlayingSoundsBarVisible)
                BottomPlayingSoundsBar.Visibility = Visibility.Visible;

            initialBottomPlayingSoundsCount = FileManager.itemViewHolder.PlayingSounds.Count;

            foreach (var playingSound in FileManager.itemViewHolder.PlayingSounds)
            {
                var item1 = new PlayingSoundItemContainer(PlayingSoundsListView.Items.Count, playingSound);
                item1.Show += PlayingSoundItemContainer_Show;
                item1.Hide += PlayingSoundItemContainer_Hide;

                var item2 = new PlayingSoundItemContainer(PlayingSoundsListView.Items.Count, playingSound);
                item2.Show += PlayingSoundItemContainer_Show;
                item2.Hide += PlayingSoundItemContainer_Hide;
                item2.Loaded += PlayingSoundItemContainer_Loaded;

                playingSoundItemContainers.Add(item1);
                reversedPlayingSoundItemContainers.Insert(0, item2);
            }
        }

        private GridView GetVisibleGridView()
        {
            if (!FileManager.itemViewHolder.ShowSoundsPivot)
                return SoundGridView2;
            else if (SoundGridViewPivot.SelectedIndex == 1)
                return FavouriteSoundGridView;
            else
                return SoundGridView;
        }

        private ListView GetVisibleListView()
        {
            if (!FileManager.itemViewHolder.ShowSoundsPivot)
                return SoundListView2;
            else if (SoundListViewPivot.SelectedIndex == 1)
                return FavouriteSoundListView;
            else
                return SoundListView;
        }

        private void UpdateSelectAllFlyoutText()
        {
            int itemsCount = FileManager.itemViewHolder.ShowListView ? GetVisibleListView().Items.Count : GetVisibleGridView().Items.Count;
            
            if (itemsCount == FileManager.itemViewHolder.SelectedSounds.Count && itemsCount != 0)
            {
                FileManager.itemViewHolder.SelectAllFlyoutText = FileManager.loader.GetString("MoreButton_SelectAllFlyout-DeselectAll");
                FileManager.itemViewHolder.SelectAllFlyoutIcon = new SymbolIcon(Symbol.ClearSelection);
            }
            else
            {
                FileManager.itemViewHolder.SelectAllFlyoutText = FileManager.loader.GetString("MoreButton_SelectAllFlyout-SelectAll");
                FileManager.itemViewHolder.SelectAllFlyoutIcon = new SymbolIcon(Symbol.SelectAll);
            }
        }

        private void UpdatePlayingSoundsList()
        {
            if (FileManager.itemViewHolder.PlayingSoundsListVisible)
            {
                // Set the max width of the sounds list and playing sounds list columns
                PlayingSoundsBarColDef.MaxWidth = ContentRoot.ActualWidth / 2;
                
                if (!FileManager.itemViewHolder.OpenMultipleSounds || isBottomPlayingSoundsBarVisible)
                {
                    // Hide the PlayingSoundsBar and the GridSplitter
                    PlayingSoundsBarColDef.MinWidth = 0;
                    PlayingSoundsBarColDef.Width = new GridLength(0);
                    GridSplitterColDef.Width = new GridLength(0);

                    if (!playingSoundsLoaded)
                        return;

                    int playingSoundItemsCount = GetNumberOfVisibleItemsInReversedPlayingSoundItemContainers();

                    // Update the visibility of the BottomPlayingSoundsBar
                    if (playingSoundItemsCount == 0)
                    {
                        BottomPlayingSoundsBar.Visibility = Visibility.Collapsed;
                        GridSplitterGrid.Visibility = Visibility.Collapsed;
                    }
                    else if (playingSoundItemsCount == 1)
                    {
                        BottomPlayingSoundsBar.Visibility = Visibility.Visible;
                        GridSplitterGrid.Visibility = Visibility.Visible;
                        
                        // Set the height of the bottom row def, but hide the GridSplitter
                        BottomPlayingSoundsBarGridSplitter.Visibility = Visibility.Collapsed;
                        GridSplitterGridBottomRowDef.Height = new GridLength(GetFirstBottomPlayingSoundItemContentHeight());
                    }
                    else
                    {
                        BottomPlayingSoundsBar.Visibility = Visibility.Visible;
                        GridSplitterGrid.Visibility = Visibility.Visible;
                        BottomPlayingSoundsBarGridSplitter.Visibility = FileManager.itemViewHolder.OpenMultipleSounds ? Visibility.Visible : Visibility.Collapsed;
                    }
                }
                else
                {
                    // Show the PlayingSoundsBar and the GridSplitter
                    PlayingSoundsBarColDef.MinWidth = ContentRoot.ActualWidth / 3.8;
                    PlayingSoundsBarColDef.Width = new GridLength(ContentRoot.ActualWidth * FileManager.itemViewHolder.PlayingSoundsBarWidth);
                    GridSplitterColDef.Width = new GridLength(12);

                    // Hide the BottomPlayingSoundsBar
                    BottomPlayingSoundsBar.Visibility = Visibility.Collapsed;
                    GridSplitterGrid.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                // Hide the PlayingSoundsBar and the GridSplitter
                PlayingSoundsBarColDef.MinWidth = 0;
                PlayingSoundsBarColDef.Width = new GridLength(0);
                GridSplitterColDef.Width = new GridLength(0);

                // Hide the BottomPlayingSoundsBar
                BottomPlayingSoundsBar.Visibility = Visibility.Collapsed;
                GridSplitterGrid.Visibility = Visibility.Collapsed;
            }

            AdaptSoundListScrollViewerForBottomPlayingSoundsBar();
        }
        #endregion

        #region Functionality
        private async Task HandleSoundsCollectionChanged(NotifyCollectionChangedEventArgs e, bool favourites)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Remove:
                    reorderedItem = (e.OldItems[0] as Sound).Uuid;
                    break;
                case NotifyCollectionChangedAction.Add:
                    Sound addedSound = e.NewItems[0] as Sound;
                    bool updateOrder = reorderedItem.Equals(addedSound.Uuid);
                    reorderedItem = Guid.Empty;

                    // The user reordered the sounds as the item was first removed and now added again
                    if (updateOrder) await UpdateSoundOrder(favourites);
                    break;
            }
        }

        private async Task UpdateSoundOrder(bool showFavourites)
        {
            // Get the current category uuid
            Guid currentCategoryUuid = FileManager.itemViewHolder.SelectedCategory;

            // Get the uuids of the sounds
            List<Guid> uuids = new List<Guid>();
            foreach (var sound in showFavourites ? FileManager.itemViewHolder.FavouriteSounds : FileManager.itemViewHolder.Sounds)
                uuids.Add(sound.Uuid);

            await DatabaseOperations.SetSoundOrderAsync(currentCategoryUuid, showFavourites, uuids);
            FileManager.UpdateCustomSoundOrder(currentCategoryUuid, showFavourites, uuids);
        }

        public static async Task PlaySoundAsync(Sound sound, bool startPlaying = true, int? volume = null, bool? muted = null, int? playbackSpeed = null, TimeSpan? position = null)
        {
            List<Sound> soundList = new List<Sound> { sound };

            var createAudioPlayerResult = await FileManager.CreateAudioPlayer(soundList, 0);
            AudioPlayer player = createAudioPlayerResult.Item1;
            List<Sound> newSounds = createAudioPlayerResult.Item2;
            if (player == null || newSounds == null) return;
            if (position.HasValue) player.Position = position.Value;

            int v = volume.HasValue ? volume.Value : sound.DefaultVolume;
            bool m = muted.HasValue ? muted.Value : sound.DefaultMuted;
            int ps = playbackSpeed.HasValue ? playbackSpeed.Value : sound.DefaultPlaybackSpeed;

            double appVolume = ((double)FileManager.itemViewHolder.Volume) / 100;
            player.Volume = appVolume * ((double)v / 100);
            player.IsMuted = m || FileManager.itemViewHolder.Muted;
            player.PlaybackRate = (double)ps / 100;

            PlayingSound playingSound = new PlayingSound(player, sound)
            {
                Uuid = await FileManager.CreatePlayingSoundAsync(null, newSounds, 0, 0, false, v, m),
                Volume = v,
                Muted = m,
                PlaybackSpeed = ps,
                Repetitions = sound.DefaultRepetitions,
                StartPlaying = startPlaying,
                StartPosition = position,
                OutputDevice = Dav.IsLoggedIn && Dav.User.Plan > 0 ? sound.DefaultOutputDevice : null
            };

            if (FileManager.itemViewHolder.OpenMultipleSounds || FileManager.itemViewHolder.PlayingSoundItems.Count == 0)
            {
                FileManager.itemViewHolder.PlayingSounds.Add(playingSound);
            }
            else
            {
                nextSinglePlayingSoundToOpen = playingSound;

                foreach (PlayingSoundItem playingSoundItem in FileManager.itemViewHolder.PlayingSoundItems)
                    await playingSoundItem.TriggerRemove();
            }
        }

        public static async Task PlaySoundsAsync(List<Sound> sounds, int repetitions, bool randomly)
        {
            // If randomly is true, shuffle sounds
            if (randomly)
            {
                Random random = new Random();
                sounds = sounds.OrderBy(a => random.Next()).ToList();
            }

            var createAudioPlayerResult = await FileManager.CreateAudioPlayer(sounds, 0);
            AudioPlayer player = createAudioPlayerResult.Item1;
            List<Sound> newSounds = createAudioPlayerResult.Item2;
            if (player == null || newSounds == null) return;

            PlayingSound playingSound = new PlayingSound(
                await FileManager.CreatePlayingSoundAsync(null, newSounds, 0, repetitions, randomly, null, null),
                player,
                newSounds,
                0,
                repetitions,
                randomly
            );
            playingSound.StartPlaying = true;

            if (FileManager.itemViewHolder.OpenMultipleSounds || FileManager.itemViewHolder.PlayingSoundItems.Count == 0)
            {
                FileManager.itemViewHolder.PlayingSounds.Add(playingSound);
            }
            else
            {
                nextSinglePlayingSoundToOpen = playingSound;

                foreach (PlayingSoundItem playingSoundItem in FileManager.itemViewHolder.PlayingSoundItems)
                    await playingSoundItem.TriggerRemove();
            }
        }

        public static async Task PlayLocalSound(StorageFile file)
        {
            Sound sound = new Sound(Guid.NewGuid(), file.DisplayName)
            {
                AudioFile = file
            };

            AudioPlayer player = await FileManager.CreateAudioPlayerForLocalSound(sound);
            if (player == null) return;

            PlayingSound playingSound = new PlayingSound(Guid.NewGuid(), player, new List<Sound> { sound }, 0, 0, false);
            playingSound.LocalFile = true;
            playingSound.StartPlaying = true;

            if (FileManager.itemViewHolder.OpenMultipleSounds || FileManager.itemViewHolder.PlayingSoundItems.Count == 0)
            {
                FileManager.itemViewHolder.PlayingSounds.Add(playingSound);
            }
            else
            {
                nextSinglePlayingSoundToOpen = playingSound;

                foreach (PlayingSoundItem playingSoundItem in FileManager.itemViewHolder.PlayingSoundItems)
                    await playingSoundItem.TriggerRemove();
            }
        }

        public static async Task PlaySoundAfterPlayingSoundsLoadedAsync(Sound sound)
        {
            if (!playingSoundsLoaded)
            {
                await Task.Delay(10);
                await PlaySoundAfterPlayingSoundsLoadedAsync(sound);
                return;
            }

            await PlaySoundAsync(sound);
        }

        public static async Task PlayLocalSoundAfterPlayingSoundsLoadedAsync(StorageFile file)
        {
            if (!playingSoundsLoaded)
            {
                await Task.Delay(10);
                await PlayLocalSoundAfterPlayingSoundsLoadedAsync(file);
                return;
            }

            await PlayLocalSound(file);
        }

        public void ShowAllInAppNotifications()
        {
            double marginBottom = 10;

            if (!FileManager.itemViewHolder.OpenMultipleSounds && FileManager.itemViewHolder.PlayingSounds.Count >= 1) marginBottom = 70;
            else if (isBottomPlayingSoundsBarVisible && FileManager.itemViewHolder.PlayingSounds.Count >= 1) marginBottom = 57;

            foreach (var item in FileManager.InAppNotificationItems)
            {
                item.InAppNotification.Margin = new Thickness(20, 0, 20, marginBottom);

                var progressRing = new WinUI.ProgressRing
                {
                    Width = 20,
                    Height = 20,
                    Margin = new Thickness(0, 0, 10, 0),
                    IsIndeterminate = item.ProgressRing.IsIndeterminate,
                    Value = item.ProgressRing.Value,
                    Visibility = item.ProgressRing.Visibility
                };

                (item.InAppNotification.Content as Grid).Children.Remove(item.ProgressRing);
                (item.InAppNotification.Content as Grid).Children.Add(progressRing);
                item.ProgressRing = progressRing;

                ContentGrid.Children.Add(item.InAppNotification);

                item.InAppNotification.Show(item.Duration);
                item.Sent = true;

                // Calculate the bottom margin
                marginBottom = marginBottom + 10 + item.MessageTextBlock.ActualHeight;
            }
        }

        private void UpdateCanReorderItems()
        {
            if (FileManager.itemViewHolder.MultiSelectionEnabled)
                canReorderItems = false;
            else if (FileManager.itemViewHolder.SoundOrder == NewSoundOrder.Custom)
                canReorderItems = true;
            else
                canReorderItems = false;

            Bindings.Update();
        }

        private async Task<bool> CheckAudioDevices()
        {
            if (FileManager.deviceWatcherHelper.Devices.Count > 0) return true;

            await new NoAudioDeviceDialog().ShowAsync();

            return false;
        }
        #endregion

        #region UI
        private void InitBottomPlayingSoundsBarHeight()
        {
            // Update the min and max height of the bottom row def
            UpdateGridSplitterRange();

            if (BottomPlayingSoundsBarListView.Items.Count > 0)
            {
                // Get the height of the first PlayingSound item and set the height of the BottomPlayingSoundsBar
                double firstItemHeight = GetFirstBottomPlayingSoundItemContentHeight();
                BottomPlayingSoundsBar.Height = firstItemHeight;
                GridSplitterGridBottomRowDef.MinHeight = firstItemHeight;
                GridSplitterGridBottomRowDef.Height = new GridLength(firstItemHeight);
            }
        }

        private void SnapBottomPlayingSoundsBar()
        {
            double currentPosition = BottomPlayingSoundsBar.ActualHeight - GridSplitterGridBottomRowDef.MinHeight;
            double maxPosition = GridSplitterGridBottomRowDef.MaxHeight - GridSplitterGridBottomRowDef.MinHeight;

            if (currentPosition < maxPosition / 2)
            {
                StartSnapBottomPlayingSoundsBarAnimation(BottomPlayingSoundsBar.ActualHeight, GridSplitterGridBottomRowDef.MinHeight);
                bottomPlayingSoundsBarPosition = VerticalPosition.Bottom;
            }
            else
            {
                StartSnapBottomPlayingSoundsBarAnimation(BottomPlayingSoundsBar.ActualHeight, GridSplitterGridBottomRowDef.MaxHeight);
                bottomPlayingSoundsBarPosition = VerticalPosition.Top;
            }
        }

        private void StartSnapBottomPlayingSoundsBarAnimation(double start, double end)
        {
            if (!playingSoundsLoaded)
                return;

            if (end >= maxBottomPlayingSoundsBarHeight)
                end = maxBottomPlayingSoundsBarHeight;
            
            if (snapBottomPlayingSoundsBarAnimationRunning)
            {
                SnapBottomPlayingSoundsBarStoryboardAnimation.To = end;
            }
            else
            {
                SnapBottomPlayingSoundsBarStoryboardAnimation.From = start;
                SnapBottomPlayingSoundsBarStoryboardAnimation.To = end;
                SnapBottomPlayingSoundsBarStoryboard.Begin();
                snapBottomPlayingSoundsBarAnimationRunning = true;
            }
        }

        private void AnimateIncreasingBottomPlayingSoundBar(double addedHeight)
        {
            double newMaxHeight = BottomPlayingSoundsBar.ActualHeight + addedHeight;
            if (newMaxHeight < 0) newMaxHeight = 0;

            GridSplitterGridBottomRowDef.MaxHeight = newMaxHeight;
            StartSnapBottomPlayingSoundsBarAnimation(BottomPlayingSoundsBar.ActualHeight, GridSplitterGridBottomRowDef.MaxHeight);
        }

        private void AdaptSoundListScrollViewerForBottomPlayingSoundsBar()
        {
            double bottomPlayingSoundsBarHeight = 0;

            if (BottomPlayingSoundsBar.Visibility == Visibility.Visible && playingSoundsLoaded)
                bottomPlayingSoundsBarHeight = GridSplitterGridBottomRowDef.ActualHeight + (FileManager.itemViewHolder.PlayingSounds.Count == 1 ? 0 : 16);

            // Set the padding of the sound GridViews and ListViews, so that the ScrollViewer ends at the bottom bar and the list continues behind the bottom bar
            SoundGridView.Padding = new Thickness(
                SoundGridView.Padding.Left,
                SoundGridView.Padding.Top,
                SoundGridView.Padding.Right,
                bottomPlayingSoundsBarHeight
            );
            FavouriteSoundGridView.Padding = new Thickness(
                FavouriteSoundGridView.Padding.Left,
                FavouriteSoundGridView.Padding.Top,
                FavouriteSoundGridView.Padding.Right,
                bottomPlayingSoundsBarHeight
            );
            SoundListView.Padding = new Thickness(
                SoundListView.Padding.Left,
                SoundListView.Padding.Top,
                SoundListView.Padding.Right,
                bottomPlayingSoundsBarHeight
            );
            FavouriteSoundListView.Padding = new Thickness(
                FavouriteSoundListView.Padding.Left,
                FavouriteSoundListView.Padding.Top,
                FavouriteSoundListView.Padding.Right,
                bottomPlayingSoundsBarHeight
            );
            SoundGridView2.Padding = new Thickness(
                SoundGridView2.Padding.Left,
                SoundGridView2.Padding.Top,
                SoundGridView2.Padding.Right,
                bottomPlayingSoundsBarHeight
            );
            SoundListView2.Padding = new Thickness(
                SoundListView2.Padding.Left,
                SoundListView2.Padding.Top,
                SoundListView2.Padding.Right,
                bottomPlayingSoundsBarHeight
            );
        }

        private void UpdateGridSplitterRange()
        {
            // Set the max height of the bottom row def
            double totalHeight = GetTotalBottomPlayingSoundListContentHeight();

            if (totalHeight == 0)
                return;

            if (totalHeight >= maxBottomPlayingSoundsBarHeight)
                totalHeight = maxBottomPlayingSoundsBarHeight;

            GridSplitterGridBottomRowDef.MaxHeight = totalHeight;

            // Set the min height of the bottom row def
            double firstItemHeight = GetFirstBottomPlayingSoundItemContentHeight();
            if (firstItemHeight >= maxBottomPlayingSoundsBarHeight) firstItemHeight = GridSplitterGridBottomRowDef.ActualHeight;

            GridSplitterGridBottomRowDef.MinHeight = firstItemHeight;
        }

        private double GetTotalBottomPlayingSoundListContentHeight()
        {
            double totalHeight = 0;

            foreach (var item in reversedPlayingSoundItemContainers)
                totalHeight += item.ContentHeight;

            return totalHeight;
        }

        private double GetFirstBottomPlayingSoundItemContentHeight()
        {
            foreach (var item in reversedPlayingSoundItemContainers)
                if (item.IsVisible)
                    return item.ContentHeight;

            return 0;
        }

        private int GetNumberOfVisibleItemsInReversedPlayingSoundItemContainers()
        {
            int visibleItems = 0;

            foreach (var item in reversedPlayingSoundItemContainers)
                if (item.IsVisible)
                    visibleItems++;

            return visibleItems;
        }

        private void UpdateMessagesVisibilities()
        {
            startMessageVisibility = (
                    (
                        FileManager.itemViewHolder.AppState == AppState.Empty
                        || FileManager.itemViewHolder.AppState == AppState.InitialSync
                    )
                    && FileManager.itemViewHolder.SelectedCategory.Equals(Guid.Empty)
                ) ? Visibility.Visible : Visibility.Collapsed;

            startMessageButtonsEnabled = FileManager.itemViewHolder.AppState != AppState.InitialSync;

            if (FileManager.itemViewHolder.AllSounds.Count > 0)
                EmptyCategoryMessageRelativePanel.Margin = new Thickness(0, 220, 0, 25);
            else
                EmptyCategoryMessageRelativePanel.Margin = new Thickness(0, 110, 0, 25);

            emptyCategoryMessageVisibility = (
                !FileManager.itemViewHolder.SelectedCategory.Equals(Guid.Empty)
                && FileManager.itemViewHolder.Sounds.Count == 0
                && FileManager.itemViewHolder.AppState != AppState.Loading
            ) ? Visibility.Visible : Visibility.Collapsed;

            Bindings.Update();
        }
        #endregion

        #region Event handlers
        #region Start message event handlers
        private async void StartMessageNewSoundFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            // Show file picker for new sounds
            var files = await MainPage.PickFilesForAddSoundsContentDialog();
            if (files.Count == 0) return;

            // Show the dialog for adding the sounds
            var template = (DataTemplate)Resources["SoundFileItemTemplate"];

            var addSoundsDialog = new AddSoundsDialog(template, files);
            addSoundsDialog.PrimaryButtonClick += AddSoundsContentDialog_PrimaryButtonClick;
            await addSoundsDialog.ShowAsync();
        }

        private async void AddSoundsContentDialog_PrimaryButtonClick(Dialog sender, ContentDialogButtonClickEventArgs args)
        {
            var dialog = sender as AddSoundsDialog;
            await MainPage.AddSelectedSoundFiles(dialog.SelectedFiles);
        }

        private async void StartMessageDownloadSoundsFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            var soundDownloadListItemTemplate = Resources["SoundDownloadListItemTemplate"] as DataTemplate;

            var soundDownloadDialog = new SoundDownloadDialog(soundDownloadListItemTemplate);
            soundDownloadDialog.PrimaryButtonClick += DownloadSoundsContentDialog_PrimaryButtonClick;
            await soundDownloadDialog.ShowAsync();
        }

        private void DownloadSoundsContentDialog_PrimaryButtonClick(Dialog sender, ContentDialogButtonClickEventArgs args)
        {
            FileManager.itemViewHolder.TriggerSoundDownloadEvent(sender as SoundDownloadDialog, EventArgs.Empty);
        }

        private async void StartMessageLoginButton_Click(object sender, RoutedEventArgs e)
        {
            bool result = await AccountPage.ShowLoginPage();

            Analytics.TrackEvent("LoginButtonClick", new Dictionary<string, string>
            {
                { "Context", "StartMessage" },
                { "Result", result.ToString() }
            });
        }

        private async void StartMessageImportButton_Click(object sender, RoutedEventArgs e)
        {
            var startMessageImportSoundboardDialog = new ImportSoundboardDialog(true);
            startMessageImportSoundboardDialog.PrimaryButtonClick += StartMessageImportDataContentDialog_PrimaryButtonClick;
            await startMessageImportSoundboardDialog.ShowAsync();
        }

        private async void StartMessageImportDataContentDialog_PrimaryButtonClick(Dialog sender, ContentDialogButtonClickEventArgs args)
        {
            var dialog = sender as ImportSoundboardDialog;

            FileManager.itemViewHolder.AppState = AppState.Normal;
            await FileManager.ImportDataAsync(dialog.ImportFile, true);
            FileManager.UpdatePlayAllButtonVisibility();

            Analytics.TrackEvent("ImportData", new Dictionary<string, string>
            {
                { "Context", "StartMessage" }
            });
        }
        #endregion

        private async void SoundGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (FileManager.itemViewHolder.MultiSelectionEnabled) return;

            // Check if there is an audio device
            if (await CheckAudioDevices())
                await PlaySoundAsync((Sound)e.ClickedItem);
        }

        private async void SoundContentGrid_DragOver(object sender, DragEventArgs e)
        {
            if (!e.DataView.Contains("FileName")) return;

            var deferral = e.GetDeferral();
            e.AcceptedOperation = DataPackageOperation.Copy;

            e.DragUIOverride.IsCaptionVisible = true;
            e.DragUIOverride.IsContentVisible = true;
            e.DragUIOverride.IsGlyphVisible = true;

            // If the file types of all items are not supported, update the Caption
            bool fileTypesSupported = false;
            var storageItems = await e.DataView.GetStorageItemsAsync();

            foreach (var item in storageItems)
            {
                if (item.IsOfType(StorageItemTypes.File) && FileManager.allowedFileTypes.Contains((item as StorageFile).FileType))
                {
                    fileTypesSupported = true;
                    break;
                }
            }

            if (FileManager.itemViewHolder.AddingSounds)
                e.DragUIOverride.Caption = FileManager.loader.GetString("Drop-AlreadyAddingSounds");
            else if (fileTypesSupported)
                e.DragUIOverride.Caption = FileManager.loader.GetString("Drop");
            else
                e.DragUIOverride.Caption = FileManager.loader.GetString("Drop-FileTypeNotSupported");

            deferral.Complete();
        }

        private async void SoundContentGrid_Drop(object sender, DragEventArgs e)
        {
            if (
                !e.DataView.Contains(StandardDataFormats.StorageItems)
                || FileManager.itemViewHolder.AddingSounds
            ) return;

            var items = await e.DataView.GetStorageItemsAsync();
            if (!items.Any()) return;

            // Get all files with supported file type
            List<StorageFile> files = new List<StorageFile>();

            foreach (var storageItem in items)
            {
                if (
                    storageItem.IsOfType(StorageItemTypes.File)
                    && FileManager.allowedFileTypes.Contains((storageItem as StorageFile).FileType)
                ) files.Add((StorageFile)storageItem);
            }

            if (files.Count == 0) return;

            // Show the dialog for adding the sounds
            var template = (DataTemplate)Resources["SoundFileItemTemplate"];

            var addSoundsDialog = new AddSoundsDialog(template, files);
            addSoundsDialog.PrimaryButtonClick += AddSoundsContentDialog_PrimaryButtonClick;
            await addSoundsDialog.ShowAsync();
        }

        private void SoundGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (skipSoundListSelectionChangedEvent) return;

            // Add new items to selectedSounds list
            if (e.AddedItems.Count > 0)
            {
                // Add each item to SelectedSounds
                foreach (var item in e.AddedItems)
                    FileManager.itemViewHolder.SelectedSounds.Add(item as Sound);
            }
            else if (e.RemovedItems.Count > 0)
            {
                // Remove each item from SelectedSounds
                foreach (var item in e.RemovedItems)
                    FileManager.itemViewHolder.SelectedSounds.Remove(item as Sound);
            }

            UpdateSelectAllFlyoutText();
        }

        private void SoundGridViewPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Pivot pivot = (Pivot)sender;

            // Deselect all items in both GridViews
            SoundGridView.DeselectRange(new ItemIndexRange(0, (uint)SoundGridView.Items.Count));
            FavouriteSoundGridView.DeselectRange(new ItemIndexRange(0, (uint)FavouriteSoundGridView.Items.Count));

            FileManager.itemViewHolder.SelectedSounds.Clear();
            soundsPivotSelected = pivot.SelectedIndex == 0;
        }

        private void SoundListViewPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Pivot pivot = (Pivot)sender;

            // Deselect all items in both ListViews
            SoundListView.DeselectRange(new ItemIndexRange(0, (uint)SoundListView.Items.Count));
            FavouriteSoundListView.DeselectRange(new ItemIndexRange(0, (uint)FavouriteSoundListView.Items.Count));

            FileManager.itemViewHolder.SelectedSounds.Clear();
            soundsPivotSelected = pivot.SelectedIndex == 0;
        }

        private void SoundGridView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            GridView gridView = (GridView)sender;
            double desiredWidth = 200;
            double innerWidth = gridView.ActualWidth - 10;  // Left margin = 10, right margin = (innerWidth - (columns * [sound tile margin])) / columns
            int columns = Convert.ToInt32(innerWidth / desiredWidth);

            FileManager.itemViewHolder.SoundTileWidth = (innerWidth - (columns * 10)) / columns;
            FileManager.itemViewHolder.TriggerSoundTileSizeChangedEvent(gridView, e);
        }

        private void PlayingSoundsBarGridSplitter_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            // Calculate the width of the PlayingSoundsBar in percent
            FileManager.itemViewHolder.PlayingSoundsBarWidth = PlayingSoundsBar.ActualWidth / ContentRoot.ActualWidth;
        }

        private void BottomPlayingSoundsBarGridSplitter_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            isManipulatingBottomPlayingSoundsBar = true;
        }

        private void BottomPlayingSoundsBarGridSplitter_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            BottomPlayingSoundsBar.Height = GridSplitterGridBottomRowDef.ActualHeight;
        }

        private void BottomPlayingSoundsBarGridSplitter_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            isManipulatingBottomPlayingSoundsBar = false;
            SnapBottomPlayingSoundsBar();
        }

        private void BottomPseudoContentGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Update the Paddings of the GridViews and ListViews
            AdaptSoundListScrollViewerForBottomPlayingSoundsBar();
        }

        private void BottomPlayingSoundsBarListView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (isManipulatingBottomPlayingSoundsBar) return;
            GridSplitterGridBottomRowDef.Height = new GridLength(BottomPlayingSoundsBarListView.ActualHeight);
        }

        private void SnapBottomPlayingSoundsBarStoryboard_Completed(object sender, object e)
        {
            snapBottomPlayingSoundsBarAnimationRunning = false;
            UpdateGridSplitterRange();
        }
        #endregion
    }

    public enum VerticalPosition
    {
        Top,
        Bottom
    }
}
