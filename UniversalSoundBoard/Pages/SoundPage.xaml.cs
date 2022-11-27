using davClassLibrary;
using Microsoft.AppCenter.Analytics;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using UniversalSoundboard.Common;
using UniversalSoundboard.Components;
using UniversalSoundboard.Controllers;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Dialogs;
using UniversalSoundboard.Models;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using WinUI = Microsoft.UI.Xaml.Controls;

namespace UniversalSoundboard.Pages
{
    public sealed partial class SoundPage : Page
    {
        public static bool soundsPivotSelected = true;
        private bool skipSoundListSelectionChangedEvent = false;
        Guid reorderedItem = Guid.Empty;
        private static bool playingSoundsLoaded = false;
        bool startMessageButtonsEnabled = true;
        bool canReorderItems = false;
        bool isMobile = false;
        Visibility startMessageVisibility = Visibility.Collapsed;
        Visibility emptyCategoryMessageVisibility = Visibility.Collapsed;
        public static DataTemplate hotkeyItemTemplate;
        private PlayingSoundsAnimationController PlayingSoundsAnimationController;
        public static List<StorageFile> LocalSoundsToPlayAfterPlayingSoundsLoaded = new List<StorageFile>();

        public static bool PlayingSoundsLoaded { get => playingSoundsLoaded; }

        public SoundPage()
        {
            InitializeComponent();
            ContentRoot.DataContext = FileManager.itemViewHolder;

            // Subscribe to events
            FileManager.itemViewHolder.PropertyChanged += ItemViewHolder_PropertyChanged;
            FileManager.itemViewHolder.Sounds.CollectionChanged += ItemViewHolder_Sounds_CollectionChanged;
            FileManager.itemViewHolder.FavouriteSounds.CollectionChanged += ItemViewHolder_FavouriteSounds_CollectionChanged;
            FileManager.itemViewHolder.PlayingSoundsLoaded += ItemViewHolder_PlayingSoundsLoaded;
            FileManager.itemViewHolder.SelectAllSounds += ItemViewHolder_SelectAllSounds;
            FileManager.itemViewHolder.ShowInAppNotification += ItemViewHolder_ShowInAppNotification;
            FileManager.itemViewHolder.PlaySound += ItemViewHolder_PlaySound;
            FileManager.itemViewHolder.PlaySounds += ItemViewHolder_PlaySounds;
            FileManager.itemViewHolder.PlaySoundAfterPlayingSoundsLoaded += ItemViewHolder_PlaySoundAfterPlayingSoundsLoaded;
            FileManager.itemViewHolder.PlayLocalSoundAfterPlayingSoundsLoaded += ItemViewHolder_PlayLocalSoundAfterPlayingSoundsLoaded;

            PlayingSoundsAnimationController = new PlayingSoundsAnimationController(
                ContentRoot,
                SoundGridView,
                FavouriteSoundGridView,
                SoundListView,
                FavouriteSoundListView,
                SoundGridView2,
                SoundListView2,
                PlayingSoundsBarListView,
                BottomPlayingSoundsBarListView,
                BottomSoundsBarListView,
                GridSplitterColDef,
                PlayingSoundsBarColDef,
                BottomPlayingSoundsBar,
                BottomSoundsBar,
                BottomPlayingSoundsBarBackgroundGrid,
                GridSplitterGrid,
                GridSplitterGridBottomRowDef,
                BottomPlayingSoundsBarGridSplitter,
                BottomPseudoContentGrid
            );

            // Show all currently active InAppNotifications
            ShowAllInAppNotifications();

            // Enable or disable StartMessage buttons
            startMessageButtonsEnabled = !FileManager.itemViewHolder.Importing && FileManager.itemViewHolder.AppState != AppState.InitialSync;
            Bindings.Update();
        }

        #region Page event handlers
        private async void SoundPage_Loaded(object sender, RoutedEventArgs e)
        {
            hotkeyItemTemplate = (DataTemplate)Resources["HotkeyItemTemplate"];
            soundsPivotSelected = true;

            UpdateCanReorderItems();
            AdjustLayout();

            if (playingSoundsLoaded)
                PlayingSoundsAnimationController.Init();
            else
            {
                foreach (var item in LocalSoundsToPlayAfterPlayingSoundsLoaded)
                    await PlayLocalSoundAfterPlayingSoundsLoadedAsync(item);

                LocalSoundsToPlayAfterPlayingSoundsLoaded.Clear();
            }
        }

        private void SoundPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            AdjustLayout();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            FileManager.itemViewHolder.PropertyChanged -= ItemViewHolder_PropertyChanged;
            FileManager.itemViewHolder.Sounds.CollectionChanged -= ItemViewHolder_Sounds_CollectionChanged;
            FileManager.itemViewHolder.FavouriteSounds.CollectionChanged -= ItemViewHolder_FavouriteSounds_CollectionChanged;
            FileManager.itemViewHolder.SelectAllSounds -= ItemViewHolder_SelectAllSounds;
            FileManager.itemViewHolder.ShowInAppNotification -= ItemViewHolder_ShowInAppNotification;
            FileManager.itemViewHolder.PlaySound -= ItemViewHolder_PlaySound;
            FileManager.itemViewHolder.PlaySounds -= ItemViewHolder_PlaySounds;
            FileManager.itemViewHolder.PlaySoundAfterPlayingSoundsLoaded -= ItemViewHolder_PlaySoundAfterPlayingSoundsLoaded;
            FileManager.itemViewHolder.PlayLocalSoundAfterPlayingSoundsLoaded -= ItemViewHolder_PlayLocalSoundAfterPlayingSoundsLoaded;

            // Remove all InAppNotifications from the ContentGrid
            foreach (var ianItem in FileManager.InAppNotificationItems)
                if (ianItem.Sent && ContentGrid.Children.Contains(ianItem.InAppNotification))
                    ContentGrid.Children.Remove(ianItem.InAppNotification);

            base.OnNavigatedFrom(e);
        }

        private void ItemViewHolder_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals(ItemViewHolder.CurrentThemeKey))
                RequestedTheme = FileManager.GetRequestedTheme();
            else if (e.PropertyName.Equals(ItemViewHolder.AppStateKey) || e.PropertyName.Equals(ItemViewHolder.SelectedCategoryKey))
                UpdateMessagesVisibilities();
            else if (e.PropertyName.Equals(ItemViewHolder.SoundOrderKey) || e.PropertyName.Equals(ItemViewHolder.MultiSelectionEnabledKey))
                UpdateCanReorderItems();
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

        private void ItemViewHolder_PlayingSoundsLoaded(object sender, EventArgs e)
        {
            playingSoundsLoaded = true;
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

        private void ItemViewHolder_ShowInAppNotification(object sender, ShowInAppNotificationEventArgs args)
        {
            foreach (var ianItem in FileManager.InAppNotificationItems)
            {
                if (ianItem.Sent) continue;

                // Calculate the bottom margin
                double marginBottom = 10;
                if (!FileManager.itemViewHolder.OpenMultipleSounds && FileManager.itemViewHolder.PlayingSounds.Count >= 1) marginBottom = 70;
                else if (isMobile && FileManager.itemViewHolder.PlayingSounds.Count >= 1) marginBottom = 57;

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

        private async void ItemViewHolder_PlaySound(object sender, PlaySoundEventArgs args)
        {
            await PlaySoundAsync(
                args.Sound,
                args.StartPlaying,
                args.Volume,
                args.Muted,
                args.PlaybackSpeed,
                args.Position
            );
        }

        private async void ItemViewHolder_PlaySounds(object sender, PlaySoundsEventArgs args)
        {
            await PlaySoundsAsync(args.Sounds, args.Repetitions, args.Randomly);
        }

        private async void ItemViewHolder_PlaySoundAfterPlayingSoundsLoaded(object sender, PlaySoundAfterPlayingSoundsLoadedEventArgs args)
        {
            await PlaySoundAfterPlayingSoundsLoadedAsync(args.Sound);
        }

        private async void ItemViewHolder_PlayLocalSoundAfterPlayingSoundsLoaded(object sender, PlayLocalSoundAfterPlayingSoundsLoadedEventArgs args)
        {
            await PlayLocalSoundAfterPlayingSoundsLoadedAsync(args.File);
        }
        #endregion

        #region Helper methods
        private void AdjustLayout()
        {
            isMobile = Window.Current.Bounds.Width < FileManager.mobileMaxWidth;
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

        public async Task PlaySoundAsync(
            Sound sound,
            bool startPlaying = true,
            int? volume = null,
            bool? muted = null,
            int? playbackSpeed = null,
            TimeSpan? position = null
        )
        {
            List<Sound> soundList = new List<Sound> { sound };

            var createAudioPlayerResult = FileManager.CreateAudioPlayer(soundList, 0);
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

            await ShowNextPlayingSound(playingSound);
        }

        public async Task PlaySoundsAsync(List<Sound> sounds, int repetitions, bool randomly)
        {
            // If randomly is true, shuffle sounds
            if (randomly)
            {
                Random random = new Random();
                sounds = sounds.OrderBy(a => random.Next()).ToList();
            }

            var createAudioPlayerResult = FileManager.CreateAudioPlayer(sounds, 0);
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

            await ShowNextPlayingSound(playingSound);
        }

        public async Task PlayLocalSound(StorageFile file)
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

            await ShowNextPlayingSound(playingSound);
        }

        public async Task PlaySoundAfterPlayingSoundsLoadedAsync(Sound sound)
        {
            if (!playingSoundsLoaded)
            {
                await Task.Delay(10);
                await PlaySoundAfterPlayingSoundsLoadedAsync(sound);
                return;
            }

            await PlaySoundAsync(sound);
        }

        public async Task PlayLocalSoundAfterPlayingSoundsLoadedAsync(StorageFile file)
        {
            if (!playingSoundsLoaded)
            {
                await Task.Delay(10);
                await PlayLocalSoundAfterPlayingSoundsLoadedAsync(file);
                return;
            }

            await PlayLocalSound(file);
        }

        private async Task ShowNextPlayingSound(PlayingSound playingSound)
        {
            if (!FileManager.itemViewHolder.OpenMultipleSounds)
            {
                for (int i = 0; i < FileManager.itemViewHolder.PlayingSoundItems.Count; i++)
                {
                    var playingSoundItem = FileManager.itemViewHolder.PlayingSoundItems[i];
                    await playingSoundItem.TriggerRemove();
                }

                await Task.Delay(400);
            }

            // Show the next PlayingSound
            FileManager.itemViewHolder.PlayingSounds.Add(playingSound);
        }

        public void ShowAllInAppNotifications()
        {
            double marginBottom = 10;

            if (!FileManager.itemViewHolder.OpenMultipleSounds && FileManager.itemViewHolder.PlayingSounds.Count >= 1) marginBottom = 70;
            else if (isMobile && FileManager.itemViewHolder.PlayingSounds.Count >= 1) marginBottom = 57;

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

        private async void PlayingSoundItemTemplate_Expand(object sender, EventArgs e)
        {
            PlayingSoundItemTemplate itemTemplate = sender as PlayingSoundItemTemplate;
            PlayingSoundItem playingSoundItem = itemTemplate.PlayingSoundItem;
            
            if (playingSoundItem == null)
                return;

            await PlayingSoundsAnimationController.ShowBottomSoundsBar(playingSoundItem);
        }

        private async void PlayingSoundItemTemplate_Collapse(object sender, EventArgs e)
        {
            await PlayingSoundsAnimationController.HideBottomSoundsBar();
        }

        private void PlayingSoundItemSoundItemTemplate_Remove(object sender, EventArgs args)
        {
            PlayingSoundItemSoundItemTemplate itemTemplate = sender as PlayingSoundItemSoundItemTemplate;
            PlayingSoundsAnimationController.RemoveSoundInBottomSoundsBar(itemTemplate.Sound);
        }
        #endregion
    }
}
