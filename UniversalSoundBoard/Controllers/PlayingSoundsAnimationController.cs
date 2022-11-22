using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using UniversalSoundboard.Common;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Models;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace UniversalSoundboard.Controllers
{
    public class PlayingSoundsAnimationController
    {
        private const int animationDuration = 300;
        private const double maxBottomSoundsBarHeight = 210;

        private readonly Compositor compositor = Window.Current.Compositor;
        private bool playingSoundsLoaded = false;
        private bool playingSoundItemsLoaded = false;
        private bool showBottomPlayingSoundsBar = false;
        private double maxBottomPlayingSoundsBarHeight = 500;
        private double bottomSoundsBarHeight = 0;
        private BottomPlayingSoundsBarVerticalPosition bottomPlayingSoundsBarPosition = BottomPlayingSoundsBarVerticalPosition.Bottom;

        RelativePanel ContentRoot;
        GridView SoundGridView;
        GridView FavouriteSoundGridView;
        ListView SoundListView;
        ListView FavouriteSoundListView;
        GridView SoundGridView2;
        ListView SoundListView2;

        ListView PlayingSoundsBarListView;
        ListView BottomPlayingSoundsBarListView;
        ListView BottomSoundsBarListView;

        ColumnDefinition GridSplitterColDef;
        ColumnDefinition PlayingSoundsBarColDef;

        RelativePanel BottomPlayingSoundsBar;
        RelativePanel BottomSoundsBar;
        Grid BottomPlayingSoundsBarBackgroundGrid;
        Grid GridSplitterGrid;
        RowDefinition GridSplitterGridBottomRowDef;
        GridSplitter BottomPlayingSoundsBarGridSplitter;
        Grid BottomPseudoContentGrid;

        private bool BottomPlayingSoundsBarReachedTop
        {
            get => GetTotalBottomPlayingSoundsListContentHeight() >= maxBottomPlayingSoundsBarHeight;
        }

        public bool IsMobile { get; set; }

        public ObservableCollection<PlayingSoundItemContainer> PlayingSoundItemContainers { get; private set; }
        public ObservableCollection<PlayingSoundItemContainer> ReversedPlayingSoundItemContainers { get; private set; }
        public ObservableCollection<Sound> BottomSoundsBarSounds { get; private set; }
        private List<PlayingSoundItemContainer> PlayingSoundsToShowList;

        public PlayingSoundsAnimationController(
            RelativePanel contentRoot,
            GridView soundGridView,
            GridView favouriteSoundGridView,
            ListView soundListView,
            ListView favouriteSoundListView,
            GridView soundGridView2,
            ListView soundListView2,

            ListView playingSoundsBarListView,
            ListView bottomPlayingSoundsBarListView,
            ListView bottomSoundsBarListView,

            ColumnDefinition gridSplitterColDef,
            ColumnDefinition playingSoundsBarColDef,

            RelativePanel bottomPlayingSoundsBar,
            RelativePanel bottomSoundsBar,
            Grid bottomPlayingSoundsBarBackgroundGrid,
            Grid gridSplitterGrid,
            RowDefinition gridSplitterGridBottomRowDef,
            GridSplitter bottomPlayingSoundsBarGridSplitter,
            Grid bottomPseudoContentGrid
        )
        {
            ContentRoot = contentRoot;
            SoundGridView = soundGridView;
            FavouriteSoundGridView = favouriteSoundGridView;
            SoundListView = soundListView;
            FavouriteSoundListView = favouriteSoundListView;
            SoundGridView2 = soundGridView2;
            SoundListView2 = soundListView2;

            PlayingSoundsBarListView = playingSoundsBarListView;
            BottomPlayingSoundsBarListView = bottomPlayingSoundsBarListView;
            BottomSoundsBarListView = bottomSoundsBarListView;

            GridSplitterColDef = gridSplitterColDef;
            PlayingSoundsBarColDef = playingSoundsBarColDef;

            BottomPlayingSoundsBar = bottomPlayingSoundsBar;
            BottomSoundsBar = bottomSoundsBar;
            BottomPlayingSoundsBarBackgroundGrid = bottomPlayingSoundsBarBackgroundGrid;
            GridSplitterGrid = gridSplitterGrid;
            GridSplitterGridBottomRowDef = gridSplitterGridBottomRowDef;
            BottomPlayingSoundsBarGridSplitter = bottomPlayingSoundsBarGridSplitter;
            BottomPseudoContentGrid = bottomPseudoContentGrid;

            IsMobile = false;

            PlayingSoundItemContainers = new ObservableCollection<PlayingSoundItemContainer>();
            ReversedPlayingSoundItemContainers = new ObservableCollection<PlayingSoundItemContainer>();
            BottomSoundsBarSounds = new ObservableCollection<Sound>();
            PlayingSoundsToShowList = new List<PlayingSoundItemContainer>();

            ContentRoot.SizeChanged += ContentRoot_SizeChanged;
            BottomPseudoContentGrid.SizeChanged += BottomPseudoContentGrid_SizeChanged;
            BottomPlayingSoundsBarGridSplitter.ManipulationDelta += BottomPlayingSoundsBarGridSplitter_ManipulationDelta;
            BottomPlayingSoundsBarGridSplitter.ManipulationCompleted += BottomPlayingSoundsBarGridSplitter_ManipulationCompleted;
            FileManager.itemViewHolder.PlayingSoundsLoaded += ItemViewHolder_PlayingSoundsLoaded;
            FileManager.itemViewHolder.RemovePlayingSoundItem += ItemViewHolder_RemovePlayingSoundItem;
            FileManager.itemViewHolder.PlayingSounds.CollectionChanged += ItemViewHolder_PlayingSounds_CollectionChanged;
        }

        #region Event handlers
        private async void ContentRoot_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            bool oldIsMobile = IsMobile;

            maxBottomPlayingSoundsBarHeight = Window.Current.Bounds.Height * 0.5;
            IsMobile = Window.Current.Bounds.Width < FileManager.mobileMaxWidth;

            if (
                playingSoundItemsLoaded
                && IsMobile != oldIsMobile
                && FileManager.itemViewHolder.OpenMultipleSounds
            )
            {
                if (IsMobile)
                {
                    BottomPlayingSoundsBarBackgroundGrid.Translation = new Vector3(0);
                    BottomPlayingSoundsBarBackgroundGrid.Visibility = Visibility.Visible;
                    BottomPlayingSoundsBarGridSplitter.Opacity = 0;
                    showBottomPlayingSoundsBar = true;

                    // Show the PlayingSounds in the BottomPlayingSoundsBar
                    foreach (var item in ReversedPlayingSoundItemContainers)
                        PlayingSoundsToShowList.Add(item);

                    await Task.Delay(100);
                    await ShowAllPlayingSoundItems();
                }
                else if (FileManager.itemViewHolder.OpenMultipleSounds)
                {
                    GridSplitterGrid.Visibility = Visibility.Collapsed;
                    BottomPlayingSoundsBarBackgroundGrid.Visibility = Visibility.Collapsed;
                    BottomPlayingSoundsBar.Translation = new Vector3(-10000, 0, 0);
                    BottomPlayingSoundsBar.Height = double.NaN;
                    bottomSoundsBarHeight = 0;

                    // Show the PlayingSounds in the normal PlayingSoundsBar
                    foreach (var item in PlayingSoundItemContainers)
                    {
                        item.PlayingSoundItemTemplate.Opacity = 0;
                        item.PlayingSoundItemTemplate.Translation = new Vector3(0, -300, 0);
                        PlayingSoundsToShowList.Add(item);
                    }

                    await Task.Delay(100);
                    await ShowAllPlayingSoundItems();
                }
            }

            if (!playingSoundItemsLoaded || !IsMobile)
                UpdatePlayingSoundsList();
        }

        private void BottomPseudoContentGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Update the Paddings of the GridViews and ListViews
            AdaptSoundListScrollViewerForBottomPlayingSoundsBar();
        }

        private void BottomPlayingSoundsBarGridSplitter_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            BottomPlayingSoundsBar.Height = GridSplitterGridBottomRowDef.ActualHeight - bottomSoundsBarHeight;
            BottomPlayingSoundsBar.Translation = new Vector3(0, -(float)bottomSoundsBarHeight, 0);
            BottomPlayingSoundsBarBackgroundGrid.Translation = new Vector3(0, -(float)GridSplitterGridBottomRowDef.ActualHeight, 0);
        }

        private void BottomPlayingSoundsBarGridSplitter_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            SnapBottomPlayingSoundsBar();
        }

        private void ItemViewHolder_PlayingSoundsLoaded(object sender, EventArgs e)
        {
            Init();
        }

        private void ItemViewHolder_RemovePlayingSoundItem(object sender, RemovePlayingSoundItemEventArgs args)
        {
            // Find the item container with the uuid and set IsVisible to false
            // so that the item is not rendered when the BottomPlayingSoundsBar is made visible 
            var itemContainer = ReversedPlayingSoundItemContainers.ToList().Find(item => item.PlayingSound.Uuid.Equals(args.Uuid));
            if (itemContainer != null) itemContainer.IsVisible = false;
        }

        private async void ItemViewHolder_PlayingSounds_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!playingSoundsLoaded) return;

            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                showBottomPlayingSoundsBar = FileManager.itemViewHolder.PlayingSounds.Count == 1;

                if (showBottomPlayingSoundsBar)
                {
                    // Reset the BottomPlayingSoundsBar to enable loading PlayingSoundItems
                    BottomPlayingSoundsBar.Translation = new Vector3(-10000, 0, 0);
                    BottomPlayingSoundsBar.Height = double.NaN;
                }
                else if (FileManager.itemViewHolder.PlayingSounds.Count == 2)
                {
                    // Show the GridSplitter
                    BottomPlayingSoundsBarGridSplitter.Opacity = 0;
                    BottomPlayingSoundsBarGridSplitter.Visibility = Visibility.Visible;

                    UpdateGridSplitterRange();
                    ShowGridSplitter();
                }

                var playingSound = e.NewItems[0] as PlayingSound;

                var item1 = new PlayingSoundItemContainer(PlayingSoundsBarListView.Items.Count, playingSound, false);
                item1.Hide += PlayingSoundItemContainer_Hide;
                item1.Loaded += PlayingSoundItemContainer_Loaded;
                item1.ExpandSoundsList += PlayingSoundItemContainer_ExpandSoundsList;
                item1.CollapseSoundsList += PlayingSoundItemContainer_CollapseSoundsList;

                var item2 = new PlayingSoundItemContainer(PlayingSoundsBarListView.Items.Count, playingSound, true, !showBottomPlayingSoundsBar);
                item2.Hide += PlayingSoundItemContainer_Hide;
                item2.Loaded += PlayingSoundItemContainer_Loaded;
                item2.ExpandSoundsList += PlayingSoundItemContainer_ExpandSoundsList;
                item2.CollapseSoundsList += PlayingSoundItemContainer_CollapseSoundsList;

                if (IsMobile || !FileManager.itemViewHolder.OpenMultipleSounds)
                    PlayingSoundsToShowList.Add(item2);
                else
                    PlayingSoundsToShowList.Add(item1);

                PlayingSoundItemContainers.Add(item1);
                ReversedPlayingSoundItemContainers.Insert(0, item2);
            }
            else if (
                e.Action == NotifyCollectionChangedAction.Remove
                && FileManager.itemViewHolder.PlayingSounds.Count == 1
            )
            {
                await HideGridSplitter();
            }
        }

        private async void PlayingSoundItemContainer_Hide(object sender, EventArgs e)
        {
            PlayingSoundItemContainer itemContainer = sender as PlayingSoundItemContainer;

            if (
                itemContainer.IsInBottomPlayingSoundsBar
                && FileManager.itemViewHolder.PlayingSounds.Count == 1
            )
            {
                // The last item was removed on BottomPlayingSoundsBar
                // Hide the BottomPlayingSoundsBar first
                await HideBottomPlayingSoundsBar();
                AdaptSoundListScrollViewerForBottomPlayingSoundsBar(0);
                itemContainer.PlayingSoundItemTemplate.Content = null;
            }
            else if (itemContainer.IsInBottomPlayingSoundsBar)
            {
                if (
                    bottomPlayingSoundsBarPosition == BottomPlayingSoundsBarVerticalPosition.Bottom
                    || BottomPlayingSoundsBarReachedTop
                )
                {
                    bool thresholdReached = false;
                    double thresholdDiff = 0;
                    double diffUp = itemContainer.ContentHeight;

                    double nextItemHeight = GetBottomPlayingSoundItemContentHeight(1);
                    double itemHeightDiff = 0;

                    if (
                        bottomPlayingSoundsBarPosition == BottomPlayingSoundsBarVerticalPosition.Top
                        && BottomPlayingSoundsBarReachedTop
                    )
                    {
                        // Check if the BottomPlayingSoundsBar will have blank space at the bottom after removing the item
                        double totalHeight = GetTotalBottomPlayingSoundsListContentHeight();
                        thresholdDiff = BottomPlayingSoundsBar.ActualHeight - (totalHeight - bottomSoundsBarHeight - itemContainer.ContentHeight);
                        thresholdReached = thresholdDiff > 0;

                        if (thresholdReached)
                            diffUp = itemContainer.ContentHeight - thresholdDiff;
                    }
                    else if (bottomPlayingSoundsBarPosition == BottomPlayingSoundsBarVerticalPosition.Bottom)
                        itemHeightDiff = itemContainer.ContentHeight - nextItemHeight;

                    // Hide the removed item
                    var opacityAnimation = compositor.CreateScalarKeyFrameAnimation();
                    opacityAnimation.InsertKeyFrame(0.5f, 0);
                    opacityAnimation.Duration = TimeSpan.FromMilliseconds(animationDuration);
                    opacityAnimation.Target = "Opacity";

                    itemContainer.PlayingSoundItemTemplate.StartAnimation(opacityAnimation);

                    // Move all items below the removed item up
                    List<PlayingSoundItemContainer> movedItems = new List<PlayingSoundItemContainer>();

                    foreach (var item in ReversedPlayingSoundItemContainers)
                    {
                        if (
                            !item.IsVisible
                            || item.Index == itemContainer.Index
                            || item.Index > itemContainer.Index && !thresholdReached
                        ) continue;

                        var translationAnimation = compositor.CreateVector3KeyFrameAnimation();
                        translationAnimation.Duration = TimeSpan.FromMilliseconds(animationDuration);
                        translationAnimation.Target = "Translation";

                        if (item.Index > itemContainer.Index)
                        {
                            // Move the item down
                            translationAnimation.InsertKeyFrame(1.0f, new Vector3(0, item.PlayingSoundItemTemplate.Translation.Y + (float)thresholdDiff, 0));
                        }
                        else
                        {
                            // Move the item up
                            translationAnimation.InsertKeyFrame(1.0f, new Vector3(0, item.PlayingSoundItemTemplate.Translation.Y - (float)diffUp + (float)itemHeightDiff, 0));
                        }

                        item.PlayingSoundItemTemplate.StartAnimation(translationAnimation);
                        movedItems.Add(item);
                    }

                    if (thresholdReached)
                    {
                        // Move the GridSplitter down
                        var gridSplitterTranslationAnimation = compositor.CreateVector3KeyFrameAnimation();
                        gridSplitterTranslationAnimation.InsertKeyFrame(1.0f, new Vector3(0, (float)thresholdDiff, 0));
                        gridSplitterTranslationAnimation.Duration = TimeSpan.FromMilliseconds(animationDuration);
                        gridSplitterTranslationAnimation.Target = "Translation";

                        GridSplitterGrid.StartAnimation(gridSplitterTranslationAnimation);

                        // Move the background down
                        var backgroundTranslationAnimation = compositor.CreateVector3KeyFrameAnimation();
                        backgroundTranslationAnimation.InsertKeyFrame(1.0f, new Vector3(0, BottomPlayingSoundsBarBackgroundGrid.Translation.Y + (float)thresholdDiff, 0));
                        backgroundTranslationAnimation.Duration = TimeSpan.FromMilliseconds(animationDuration);
                        backgroundTranslationAnimation.Target = "Translation";

                        BottomPlayingSoundsBarBackgroundGrid.StartAnimation(backgroundTranslationAnimation);
                    }
                    else if (bottomPlayingSoundsBarPosition == BottomPlayingSoundsBarVerticalPosition.Bottom)
                    {
                        // Move the GridSplitter
                        var gridSplitterTranslationAnimation = compositor.CreateVector3KeyFrameAnimation();
                        gridSplitterTranslationAnimation.InsertKeyFrame(1.0f, new Vector3(0, (float)itemHeightDiff, 0));
                        gridSplitterTranslationAnimation.Duration = TimeSpan.FromMilliseconds(animationDuration);
                        gridSplitterTranslationAnimation.Target = "Translation";

                        GridSplitterGrid.StartAnimation(gridSplitterTranslationAnimation);

                        // Move the BottomPlayingSoundsBar background
                        var backgroundTranslationAnimation = compositor.CreateVector3KeyFrameAnimation();
                        backgroundTranslationAnimation.InsertKeyFrame(1.0f, new Vector3(0, -(float)(nextItemHeight + bottomSoundsBarHeight), 0));
                        backgroundTranslationAnimation.Duration = TimeSpan.FromMilliseconds(animationDuration);
                        backgroundTranslationAnimation.Target = "Translation";

                        BottomPlayingSoundsBarBackgroundGrid.StartAnimation(backgroundTranslationAnimation);
                    }

                    await Task.Delay(animationDuration);

                    // Adapt the elements to the new position
                    itemContainer.PlayingSoundItemTemplate.Content = null;

                    if (thresholdReached)
                    {
                        GridSplitterGridBottomRowDef.Height = new GridLength(BottomPlayingSoundsBar.ActualHeight - thresholdDiff + bottomSoundsBarHeight);
                        BottomPlayingSoundsBar.Height = BottomPlayingSoundsBar.ActualHeight - thresholdDiff;
                        GridSplitterGrid.Translation = new Vector3(0);
                    }
                    else if (bottomPlayingSoundsBarPosition == BottomPlayingSoundsBarVerticalPosition.Bottom)
                    {
                        // Adapt the elements to the new position
                        BottomPlayingSoundsBar.Height = nextItemHeight;
                        BottomPlayingSoundsBar.Translation = new Vector3(0, -(float)bottomSoundsBarHeight, 0);
                        GridSplitterGridBottomRowDef.Height = new GridLength(nextItemHeight + bottomSoundsBarHeight);
                        GridSplitterGrid.Translation = new Vector3(0);
                    }

                    foreach (var item in movedItems)
                        item.PlayingSoundItemTemplate.Translation = new Vector3(0);
                }
                else
                {
                    double newHeight = BottomPlayingSoundsBar.ActualHeight - itemContainer.ContentHeight + bottomSoundsBarHeight;

                    // Move the GridSplitter down
                    var translationAnimation = compositor.CreateVector3KeyFrameAnimation();
                    translationAnimation.InsertKeyFrame(1.0f, new Vector3(0, (float)itemContainer.ContentHeight, 0));
                    translationAnimation.Duration = TimeSpan.FromMilliseconds(animationDuration);
                    translationAnimation.Target = "Translation";

                    GridSplitterGrid.StartAnimation(translationAnimation);

                    // Move the BottomPlayingSoundsBar background down
                    var translationAnimation2 = compositor.CreateVector3KeyFrameAnimation();
                    translationAnimation2.InsertKeyFrame(1.0f, new Vector3(0, -(float)newHeight, 0));
                    translationAnimation2.Duration = TimeSpan.FromMilliseconds(animationDuration);
                    translationAnimation2.Target = "Translation";

                    BottomPlayingSoundsBarBackgroundGrid.StartAnimation(translationAnimation2);

                    // Hide the removed item
                    var opacityAnimation = compositor.CreateScalarKeyFrameAnimation();
                    opacityAnimation.InsertKeyFrame(0.5f, 0);
                    opacityAnimation.Duration = TimeSpan.FromMilliseconds(animationDuration);
                    opacityAnimation.Target = "Opacity";

                    itemContainer.PlayingSoundItemTemplate.StartAnimation(opacityAnimation);

                    // Move all items above the removed item down
                    List<PlayingSoundItemContainer> movedItems = new List<PlayingSoundItemContainer>();

                    foreach (var item in ReversedPlayingSoundItemContainers)
                    {
                        if (!item.IsVisible || item.Index <= itemContainer.Index)
                            continue;

                        var translationAnimation3 = compositor.CreateVector3KeyFrameAnimation();
                        translationAnimation3.InsertKeyFrame(1.0f, new Vector3(0, (float)itemContainer.ContentHeight, 0));
                        translationAnimation3.Duration = TimeSpan.FromMilliseconds(animationDuration);
                        translationAnimation3.Target = "Translation";

                        item.PlayingSoundItemTemplate.StartAnimation(translationAnimation3);
                        movedItems.Add(item);
                    }

                    await Task.Delay(animationDuration);

                    itemContainer.PlayingSoundItemTemplate.Content = null;

                    foreach (var item in movedItems)
                        item.PlayingSoundItemTemplate.Translation = new Vector3(0);

                    // Adapt the elements to the new position
                    GridSplitterGridBottomRowDef.Height = new GridLength(newHeight);
                    BottomPlayingSoundsBar.Height = newHeight - bottomSoundsBarHeight;
                    GridSplitterGrid.Translation = new Vector3(0);

                    // Update the position if only one PlayingSound is remaining
                    if (FileManager.itemViewHolder.PlayingSounds.Count == 2)
                        bottomPlayingSoundsBarPosition = BottomPlayingSoundsBarVerticalPosition.Bottom;
                }
            }
            else
            {
                // Item is in normal PlayingSoundsBar
                // Start the animation for hiding the PlayingSoundItem
                var animationGroup = compositor.CreateAnimationGroup();

                var translationAnimation = compositor.CreateVector3KeyFrameAnimation();
                translationAnimation.InsertKeyFrame(1.0f, new Vector3(0, -(float)itemContainer.ContentHeight, 0));
                translationAnimation.Duration = TimeSpan.FromMilliseconds(animationDuration);
                translationAnimation.Target = "Translation";

                var opacityAnimation = compositor.CreateScalarKeyFrameAnimation();
                opacityAnimation.InsertKeyFrame(0.5f, 0);
                opacityAnimation.Duration = TimeSpan.FromMilliseconds(animationDuration);
                opacityAnimation.Target = "Opacity";

                animationGroup.Add(translationAnimation);
                animationGroup.Add(opacityAnimation);

                itemContainer.PlayingSoundItemTemplate.StartAnimation(animationGroup);

                // Move all items below the removed item up
                List<PlayingSoundItemContainer> movedItems = new List<PlayingSoundItemContainer>();

                foreach (var item in PlayingSoundItemContainers)
                {
                    if (!item.IsVisible || item.Index <= itemContainer.Index)
                        continue;

                    translationAnimation = compositor.CreateVector3KeyFrameAnimation();
                    translationAnimation.InsertKeyFrame(
                        1.0f,
                        new Vector3(
                            0,
                            item.PlayingSoundItemTemplate.Translation.Y - (float)itemContainer.ContentHeight,
                            0
                        )
                    );
                    translationAnimation.Duration = TimeSpan.FromMilliseconds(animationDuration);
                    translationAnimation.Target = "Translation";

                    item.PlayingSoundItemTemplate.StartAnimation(translationAnimation);
                    movedItems.Add(item);
                }

                await Task.Delay(animationDuration);
                itemContainer.PlayingSoundItemTemplate.Content = null;
                
                foreach (var item in movedItems)
                    item.PlayingSoundItemTemplate.Translation = new Vector3(0);
            }

            itemContainer.IsVisible = false;

            // Find the corresponding PlayingSoundItem and remove it
            var playingSoundItem = FileManager.itemViewHolder.PlayingSoundItems.Find(item => item.Uuid.Equals(itemContainer.PlayingSound.Uuid));
            if (playingSoundItem != null) await playingSoundItem.Remove();

            FileManager.itemViewHolder.TriggerRemovePlayingSoundItemEvent(this, new RemovePlayingSoundItemEventArgs(itemContainer.PlayingSound.Uuid));

            UpdateGridSplitterRange();
        }

        private async void PlayingSoundItemContainer_Loaded(object sender, EventArgs e)
        {
            await ShowAllPlayingSoundItems();
        }

        private void PlayingSoundItemContainer_ExpandSoundsList(object sender, PlayingSoundSoundsListEventArgs args)
        {
            if (IsMobile) return;

            PlayingSoundItemContainer itemContainer = sender as PlayingSoundItemContainer;
            args.SoundsListViewStackPanel.Height = double.NaN;

            // Move all items below the current item down
            List<PlayingSoundItemContainer> movedItems = new List<PlayingSoundItemContainer>();

            foreach (var item in PlayingSoundItemContainers)
            {
                if (!item.IsVisible || item.Index <= itemContainer.Index)
                    continue;

                item.PlayingSoundItemTemplate.Translation = new Vector3(0, -(float)args.SoundsListViewStackPanel.ActualHeight, 0);
                movedItems.Add(item);
            }

            foreach (var item in movedItems)
            {
                var translationAnimation = compositor.CreateVector3KeyFrameAnimation();
                translationAnimation.InsertKeyFrame(1.0f, new Vector3(0));
                translationAnimation.Duration = TimeSpan.FromMilliseconds(animationDuration);
                translationAnimation.Target = "Translation";

                item.PlayingSoundItemTemplate.StartAnimation(translationAnimation);
            }

            // Make the list visible
            args.SoundsListViewStackPanel.Translation = new Vector3(0, -40, 0);

            var listAnimationGroup = compositor.CreateAnimationGroup();

            var listTranslationAnimation = compositor.CreateVector3KeyFrameAnimation();
            listTranslationAnimation.InsertKeyFrame(1.0f, new Vector3(0));
            listTranslationAnimation.Duration = TimeSpan.FromMilliseconds(animationDuration);
            listTranslationAnimation.Target = "Translation";

            var listOpacityAnimation = compositor.CreateScalarKeyFrameAnimation();
            listOpacityAnimation.InsertKeyFrame(0.5f, 1);
            listOpacityAnimation.Duration = TimeSpan.FromMilliseconds(animationDuration);
            listOpacityAnimation.Target = "Opacity";

            listAnimationGroup.Add(listTranslationAnimation);
            listAnimationGroup.Add(listOpacityAnimation);

            args.SoundsListViewStackPanel.StartAnimation(listAnimationGroup);
        }

        private async void PlayingSoundItemContainer_CollapseSoundsList(object sender, PlayingSoundSoundsListEventArgs args)
        {
            if (IsMobile) return;

            PlayingSoundItemContainer itemContainer = sender as PlayingSoundItemContainer;

            // Move all items below the current item up
            List<PlayingSoundItemContainer> movedItems = new List<PlayingSoundItemContainer>();

            foreach (var item in PlayingSoundItemContainers)
            {
                if (!item.IsVisible || item.Index <= itemContainer.Index)
                    continue;

                var translationAnimation = compositor.CreateVector3KeyFrameAnimation();
                translationAnimation.InsertKeyFrame(1.0f, new Vector3(0, -(float)args.SoundsListViewStackPanel.ActualHeight, 0));
                translationAnimation.Duration = TimeSpan.FromMilliseconds(animationDuration);
                translationAnimation.Target = "Translation";

                item.PlayingSoundItemTemplate.StartAnimation(translationAnimation);

                movedItems.Add(item);
            }

            // Hide the list
            var listAnimationGroup = compositor.CreateAnimationGroup();

            var listTranslationAnimation = compositor.CreateVector3KeyFrameAnimation();
            listTranslationAnimation.InsertKeyFrame(1.0f, new Vector3(0, -40, 0));
            listTranslationAnimation.Duration = TimeSpan.FromMilliseconds(animationDuration);
            listTranslationAnimation.Target = "Translation";

            var listOpacityAnimation = compositor.CreateScalarKeyFrameAnimation();
            listOpacityAnimation.InsertKeyFrame(0.5f, 0);
            listOpacityAnimation.Duration = TimeSpan.FromMilliseconds(animationDuration);
            listOpacityAnimation.Target = "Opacity";

            listAnimationGroup.Add(listTranslationAnimation);
            listAnimationGroup.Add(listOpacityAnimation);

            args.SoundsListViewStackPanel.StartAnimation(listAnimationGroup);

            await Task.Delay(animationDuration);

            args.SoundsListViewStackPanel.Height = 0;

            foreach (var item in movedItems)
                item.PlayingSoundItemTemplate.Translation = new Vector3(0);
        }
        #endregion

        public void Init()
        {
            showBottomPlayingSoundsBar = IsMobile || !FileManager.itemViewHolder.OpenMultipleSounds;
            LoadPlayingSoundItems();
            playingSoundsLoaded = true;
        }

        public async Task ShowBottomSoundsBar(List<Sound> sounds)
        {
            BottomSoundsBarSounds.Clear();

            foreach (var sound in sounds)
                BottomSoundsBarSounds.Add(sound);

            await Task.Delay(10);
            bottomSoundsBarHeight = BottomSoundsBar.ActualHeight;

            if (bottomSoundsBarHeight > maxBottomSoundsBarHeight)
                bottomSoundsBarHeight = maxBottomSoundsBarHeight;

            BottomSoundsBar.Height = bottomSoundsBarHeight;
            BottomSoundsBar.Translation = new Vector3(0, (float)bottomSoundsBarHeight, 0);

            double newTotalHeightDiff = 0;

            if (bottomPlayingSoundsBarPosition == BottomPlayingSoundsBarVerticalPosition.Top)
            {
                // Check if the BottomPlayingSoundsBar reaches the max height
                double newTotalHeight = BottomPlayingSoundsBar.ActualHeight + bottomSoundsBarHeight;

                if (newTotalHeight > maxBottomPlayingSoundsBarHeight)
                    newTotalHeightDiff = newTotalHeight - maxBottomPlayingSoundsBarHeight;
            }

            // Animate moving the BottomPlayingSoundsBar background up
            var backgroundTranslationAnimation = compositor.CreateVector3KeyFrameAnimation();
            backgroundTranslationAnimation.InsertKeyFrame(1.0f, new Vector3(0, (float)(BottomPlayingSoundsBarBackgroundGrid.Translation.Y - bottomSoundsBarHeight + newTotalHeightDiff), 0));
            backgroundTranslationAnimation.Duration = TimeSpan.FromMilliseconds(animationDuration);
            backgroundTranslationAnimation.Target = "Translation";

            BottomPlayingSoundsBarBackgroundGrid.StartAnimation(backgroundTranslationAnimation);

            // Animate moving the GridSplitter up
            var gridSplitterTranslationAnimation = compositor.CreateVector3KeyFrameAnimation();
            gridSplitterTranslationAnimation.InsertKeyFrame(1.0f, new Vector3(0, -(float)bottomSoundsBarHeight + (float)newTotalHeightDiff, 0));
            gridSplitterTranslationAnimation.Duration = TimeSpan.FromMilliseconds(animationDuration);
            gridSplitterTranslationAnimation.Target = "Translation";

            GridSplitterGrid.StartAnimation(gridSplitterTranslationAnimation);

            // Animate moving the BottomPlayingSoundsBar up
            var bottomPlayingSoundsBarTranslationAnimation = compositor.CreateVector3KeyFrameAnimation();
            bottomPlayingSoundsBarTranslationAnimation.InsertKeyFrame(1.0f, new Vector3(0, -(float)bottomSoundsBarHeight + (float)newTotalHeightDiff, 0));
            bottomPlayingSoundsBarTranslationAnimation.Duration = TimeSpan.FromMilliseconds(animationDuration);
            bottomPlayingSoundsBarTranslationAnimation.Target = "Translation";

            BottomPlayingSoundsBar.StartAnimation(bottomPlayingSoundsBarTranslationAnimation);

            // Animate showing the list
            var bottomSoundsBarTranslationAnimation = compositor.CreateVector3KeyFrameAnimation();
            bottomSoundsBarTranslationAnimation.InsertKeyFrame(1.0f, new Vector3(0));
            bottomSoundsBarTranslationAnimation.Duration = TimeSpan.FromMilliseconds(animationDuration);
            bottomSoundsBarTranslationAnimation.Target = "Translation";

            BottomSoundsBar.StartAnimation(bottomSoundsBarTranslationAnimation);

            await Task.Delay(animationDuration);

            // Adapt the elements to the new position
            GridSplitterGridBottomRowDef.Height = new GridLength(GridSplitterGridBottomRowDef.ActualHeight + bottomSoundsBarHeight);
            GridSplitterGrid.Translation = new Vector3(0);

            if (newTotalHeightDiff > 0)
            {
                BottomPlayingSoundsBar.Height = maxBottomPlayingSoundsBarHeight - bottomSoundsBarHeight;
                BottomPlayingSoundsBar.Translation = new Vector3(0, -(float)bottomSoundsBarHeight, 0);
            }

            UpdateGridSplitterRange();
        }

        public async Task HideBottomSoundsBar()
        {
            double bottomPlayingSoundsBarHeightDiff = bottomSoundsBarHeight;

            if (bottomPlayingSoundsBarPosition == BottomPlayingSoundsBarVerticalPosition.Top)
            {
                double totalBottomPlayingSoundsListContentHeight = GetTotalBottomPlayingSoundsListContentHeight() - bottomSoundsBarHeight;

                if (totalBottomPlayingSoundsListContentHeight > BottomPlayingSoundsBar.ActualHeight)
                {
                    double newBottomPlayingSoundsBarHeight = totalBottomPlayingSoundsListContentHeight;

                    if (newBottomPlayingSoundsBarHeight > maxBottomPlayingSoundsBarHeight)
                        newBottomPlayingSoundsBarHeight = maxBottomPlayingSoundsBarHeight;

                    bottomPlayingSoundsBarHeightDiff = BottomPlayingSoundsBar.ActualHeight - newBottomPlayingSoundsBarHeight;

                    BottomPlayingSoundsBar.Height = newBottomPlayingSoundsBarHeight;
                    BottomPlayingSoundsBar.Translation = new Vector3(0, BottomPlayingSoundsBar.Translation.Y - (float)bottomPlayingSoundsBarHeightDiff, 0);

                    bottomPlayingSoundsBarHeightDiff += bottomSoundsBarHeight;
                }
            }

            // Move the BottomSoundsBar down
            var bottomSoundsBarTranslationAnimation = compositor.CreateVector3KeyFrameAnimation();
            bottomSoundsBarTranslationAnimation.InsertKeyFrame(1.0f, new Vector3(0, (float)bottomSoundsBarHeight, 0));
            bottomSoundsBarTranslationAnimation.Duration = TimeSpan.FromMilliseconds(animationDuration);
            bottomSoundsBarTranslationAnimation.Target = "Translation";

            BottomSoundsBar.StartAnimation(bottomSoundsBarTranslationAnimation);

            // Move the BottomPlayingSoundsBar down
            var bottomPlayingSoundsBarTranslationAnimation = compositor.CreateVector3KeyFrameAnimation();
            bottomPlayingSoundsBarTranslationAnimation.InsertKeyFrame(1.0f, new Vector3(0));
            bottomPlayingSoundsBarTranslationAnimation.Duration = TimeSpan.FromMilliseconds(animationDuration);
            bottomPlayingSoundsBarTranslationAnimation.Target = "Translation";

            BottomPlayingSoundsBar.StartAnimation(bottomPlayingSoundsBarTranslationAnimation);

            // Move the GridSplitter down
            var gridSplitterTranslationAnimation = compositor.CreateVector3KeyFrameAnimation();
            gridSplitterTranslationAnimation.InsertKeyFrame(1.0f, new Vector3(0, (float)bottomPlayingSoundsBarHeightDiff, 0));
            gridSplitterTranslationAnimation.Duration = TimeSpan.FromMilliseconds(animationDuration);
            gridSplitterTranslationAnimation.Target = "Translation";

            GridSplitterGrid.StartAnimation(gridSplitterTranslationAnimation);

            // Move the BottomPlayingSoundsBar background down
            var backgroundTranslationAnimation = compositor.CreateVector3KeyFrameAnimation();
            backgroundTranslationAnimation.InsertKeyFrame(1.0f, new Vector3(0, (float)(BottomPlayingSoundsBarBackgroundGrid.Translation.Y + bottomPlayingSoundsBarHeightDiff), 0));
            backgroundTranslationAnimation.Duration = TimeSpan.FromMilliseconds(animationDuration);
            backgroundTranslationAnimation.Target = "Translation";

            BottomPlayingSoundsBarBackgroundGrid.StartAnimation(backgroundTranslationAnimation);

            await Task.Delay(animationDuration);

            // Adapt the elements to the new position
            GridSplitterGridBottomRowDef.Height = new GridLength(GridSplitterGridBottomRowDef.ActualHeight - bottomPlayingSoundsBarHeightDiff);
            GridSplitterGrid.Translation = new Vector3(0);
            bottomSoundsBarHeight = 0;

            UpdateGridSplitterRange();
        }

        private void LoadPlayingSoundItems()
        {
            playingSoundItemsLoaded = true;

            foreach (var playingSound in FileManager.itemViewHolder.PlayingSounds)
            {
                var item1 = new PlayingSoundItemContainer(PlayingSoundsBarListView.Items.Count, playingSound, false);
                item1.Hide += PlayingSoundItemContainer_Hide;
                item1.Loaded += PlayingSoundItemContainer_Loaded;
                item1.ExpandSoundsList += PlayingSoundItemContainer_ExpandSoundsList;
                item1.CollapseSoundsList += PlayingSoundItemContainer_CollapseSoundsList;

                var item2 = new PlayingSoundItemContainer(PlayingSoundsBarListView.Items.Count, playingSound, true, false);
                item2.Hide += PlayingSoundItemContainer_Hide;
                item2.Loaded += PlayingSoundItemContainer_Loaded;
                item2.ExpandSoundsList += PlayingSoundItemContainer_ExpandSoundsList;
                item2.CollapseSoundsList += PlayingSoundItemContainer_CollapseSoundsList;

                if (IsMobile || !FileManager.itemViewHolder.OpenMultipleSounds)
                    PlayingSoundsToShowList.Add(item2);
                else
                    PlayingSoundsToShowList.Add(item1);

                PlayingSoundItemContainers.Add(item1);
                ReversedPlayingSoundItemContainers.Insert(0, item2);
            }
        }

        private async Task ShowAllPlayingSoundItems()
        {
            // Check if all PlayingSoundItemContainers in the list were loaded
            int itemCount = PlayingSoundsToShowList.Count;
            int loadedItemCount = 0;

            foreach (var item in PlayingSoundsToShowList)
                if (item.IsLoaded) loadedItemCount++;

            if (
                itemCount == 0
                || itemCount != loadedItemCount
            ) return;

            if (showBottomPlayingSoundsBar)
            {
                showBottomPlayingSoundsBar = false;

                foreach (var item in PlayingSoundsToShowList)
                {
                    item.PlayingSoundItemTemplate.Translation = new Vector3(0);
                    item.PlayingSoundItemTemplate.Opacity = 1;
                }

                PlayingSoundsToShowList.Clear();

                // Show the bottom playing sounds bar
                UpdatePlayingSoundsList();
                InitBottomPlayingSoundsBarHeight();

                await ShowBottomPlayingSoundsBar();
            }
            else if (
                IsMobile
                && bottomPlayingSoundsBarPosition == BottomPlayingSoundsBarVerticalPosition.Top
                && !BottomPlayingSoundsBarReachedTop
            )
            {
                foreach (var itemToShow in PlayingSoundsToShowList)
                {
                    double newHeight = BottomPlayingSoundsBar.ActualHeight + itemToShow.ContentHeight + bottomSoundsBarHeight;

                    itemToShow.PlayingSoundItemTemplate.Translation = new Vector3(0);
                    BottomPlayingSoundsBar.Height = newHeight - bottomSoundsBarHeight;

                    // Move the GridSplitter up
                    var gridSplitterTranslationAnimation = compositor.CreateVector3KeyFrameAnimation();
                    gridSplitterTranslationAnimation.InsertKeyFrame(1.0f, new Vector3(0, -(float)(newHeight - BottomPlayingSoundsBar.ActualHeight - bottomSoundsBarHeight), 0));
                    gridSplitterTranslationAnimation.Duration = TimeSpan.FromMilliseconds(animationDuration);
                    gridSplitterTranslationAnimation.Target = "Translation";

                    GridSplitterGrid.StartAnimation(gridSplitterTranslationAnimation);

                    // Move the BottomPlayingSoundsBar background up
                    var backgroundGridTranslationAnimation = compositor.CreateVector3KeyFrameAnimation();
                    backgroundGridTranslationAnimation.InsertKeyFrame(1.0f, new Vector3(0, -(float)(newHeight), 0));
                    backgroundGridTranslationAnimation.Duration = TimeSpan.FromMilliseconds(animationDuration);
                    backgroundGridTranslationAnimation.Target = "Translation";

                    BottomPlayingSoundsBarBackgroundGrid.StartAnimation(backgroundGridTranslationAnimation);

                    // Show the new PlayingSound
                    var opacityAnimation = compositor.CreateScalarKeyFrameAnimation();
                    opacityAnimation.InsertKeyFrame(0.5f, 0);
                    opacityAnimation.InsertKeyFrame(1, 1);
                    opacityAnimation.Duration = TimeSpan.FromMilliseconds(animationDuration);
                    opacityAnimation.Target = "Opacity";

                    itemToShow.PlayingSoundItemTemplate.StartAnimation(opacityAnimation);

                    await Task.Delay(animationDuration);

                    // Adapt the elements to the new position
                    GridSplitterGridBottomRowDef.Height = new GridLength(newHeight);
                    BottomPlayingSoundsBar.Translation = new Vector3(0, -(float)bottomSoundsBarHeight, 0);
                    GridSplitterGrid.Translation = new Vector3(0);
                }

                PlayingSoundsToShowList.Clear();
            }
            else if (IsMobile)
            {
                foreach (var itemToShow in PlayingSoundsToShowList)
                {
                    bool thresholdReached = false;
                    double thresholdDiff = itemToShow.ContentHeight;

                    if (bottomPlayingSoundsBarPosition == BottomPlayingSoundsBarVerticalPosition.Top)
                    {
                        // Get the height needed to reach the max height of the BottomPlayingSoundsBar
                        thresholdDiff = maxBottomPlayingSoundsBarHeight - (GetTotalBottomPlayingSoundsListContentHeight() - itemToShow.ContentHeight);
                        thresholdReached = thresholdDiff > 0;

                        if (thresholdReached)
                        {
                            double newHeight = BottomPlayingSoundsBar.ActualHeight + thresholdDiff + bottomSoundsBarHeight;
                            BottomPlayingSoundsBar.Height = newHeight - bottomSoundsBarHeight;
                        }
                    }

                    itemToShow.PlayingSoundItemTemplate.Translation = new Vector3(0);
                    itemToShow.PlayingSoundItemTemplate.Opacity = 0;

                    // Move all other items up
                    List<PlayingSoundItemContainer> movedItems = new List<PlayingSoundItemContainer>();

                    foreach (var item in ReversedPlayingSoundItemContainers)
                    {
                        if (item.Index >= itemToShow.Index) continue;

                        if (thresholdReached)
                            item.PlayingSoundItemTemplate.Translation = new Vector3(0, -(float)(item.ContentHeight - thresholdDiff), 0);
                        else
                            item.PlayingSoundItemTemplate.Translation = new Vector3(0, -(float)item.ContentHeight, 0);

                        movedItems.Add(item);
                    }

                    // Show the new item
                    var opacityAnimation = compositor.CreateScalarKeyFrameAnimation();
                    opacityAnimation.Duration = TimeSpan.FromMilliseconds(animationDuration);
                    opacityAnimation.Target = "Opacity";

                    if (thresholdReached)
                    {
                        opacityAnimation.InsertKeyFrame(0.6f, 0);
                        opacityAnimation.InsertKeyFrame(1, 1);
                    }
                    else
                    {
                        opacityAnimation.InsertKeyFrame(0.7f, 1);
                    }

                    itemToShow.PlayingSoundItemTemplate.StartAnimation(opacityAnimation);

                    // Move all other items back to the original position
                    foreach (var item in movedItems)
                    {
                        var translationAnimation = compositor.CreateVector3KeyFrameAnimation();
                        translationAnimation.InsertKeyFrame(1.0f, new Vector3(0));
                        translationAnimation.Duration = TimeSpan.FromMilliseconds(animationDuration);
                        translationAnimation.Target = "Translation";

                        item.PlayingSoundItemTemplate.StartAnimation(translationAnimation);
                    }

                    if (thresholdReached)
                    {
                        // Move the GridSplitter up
                        var gridSplitterTranslationAnimation = compositor.CreateVector3KeyFrameAnimation();
                        gridSplitterTranslationAnimation.InsertKeyFrame(1.0f, new Vector3(0, -(float)thresholdDiff, 0));
                        gridSplitterTranslationAnimation.Duration = TimeSpan.FromMilliseconds(animationDuration);
                        gridSplitterTranslationAnimation.Target = "Translation";

                        GridSplitterGrid.StartAnimation(gridSplitterTranslationAnimation);

                        // Move the BottomPlayingSoundsBar background up
                        var backgroundTranslationAnimation = compositor.CreateVector3KeyFrameAnimation();
                        backgroundTranslationAnimation.InsertKeyFrame(1.0f, new Vector3(0, BottomPlayingSoundsBarBackgroundGrid.Translation.Y - (float)thresholdDiff, 0));
                        backgroundTranslationAnimation.Duration = TimeSpan.FromMilliseconds(animationDuration);
                        backgroundTranslationAnimation.Target = "Translation";

                        BottomPlayingSoundsBarBackgroundGrid.StartAnimation(backgroundTranslationAnimation);
                    }
                    else if (bottomPlayingSoundsBarPosition == BottomPlayingSoundsBarVerticalPosition.Bottom)
                    {
                        // Adapt the BottomPlayingSoundsBar to the new height of the first item
                        double firstItemDiff = GridSplitterGridBottomRowDef.ActualHeight - bottomSoundsBarHeight - itemToShow.ContentHeight;
                        BottomPlayingSoundsBar.Height = itemToShow.ContentHeight;

                        // Move the GridSplitter
                        var gridSplitterTranslationAnimation = compositor.CreateVector3KeyFrameAnimation();
                        gridSplitterTranslationAnimation.InsertKeyFrame(1.0f, new Vector3(0, (float)firstItemDiff, 0));
                        gridSplitterTranslationAnimation.Duration = TimeSpan.FromMilliseconds(animationDuration);
                        gridSplitterTranslationAnimation.Target = "Translation";

                        GridSplitterGrid.StartAnimation(gridSplitterTranslationAnimation);

                        // Move the BottomPlayingSoundsBar background
                        var backgroundTranslationAnimation = compositor.CreateVector3KeyFrameAnimation();
                        backgroundTranslationAnimation.InsertKeyFrame(1.0f, new Vector3(0, -(float)(itemToShow.ContentHeight + bottomSoundsBarHeight), 0));
                        backgroundTranslationAnimation.Duration = TimeSpan.FromMilliseconds(animationDuration);
                        backgroundTranslationAnimation.Target = "Translation";

                        BottomPlayingSoundsBarBackgroundGrid.StartAnimation(backgroundTranslationAnimation);
                    }

                    await Task.Delay(animationDuration);

                    // Adapt the elements to the new position
                    GridSplitterGridBottomRowDef.Height = new GridLength(BottomPlayingSoundsBar.ActualHeight + bottomSoundsBarHeight);
                    GridSplitterGrid.Translation = new Vector3(0);
                }

                PlayingSoundsToShowList.Clear();
            }
            else
            {
                // The normal PlayingSoundsBar is visible
                // Show all PlayingSounds in the list
                foreach (var item in PlayingSoundsToShowList)
                {
                    var translationAnimation = compositor.CreateVector3KeyFrameAnimation();
                    translationAnimation.InsertKeyFrame(1.0f, new Vector3(0));
                    translationAnimation.Duration = TimeSpan.FromMilliseconds(animationDuration);
                    translationAnimation.Target = "Translation";

                    var opacityAnimation = compositor.CreateScalarKeyFrameAnimation();
                    opacityAnimation.InsertKeyFrame(0.3f, 0);
                    opacityAnimation.InsertKeyFrame(1.0f, 1);
                    opacityAnimation.Duration = TimeSpan.FromMilliseconds(animationDuration);
                    opacityAnimation.Target = "Opacity";

                    var animationGroup = compositor.CreateAnimationGroup();
                    animationGroup.Add(translationAnimation);
                    animationGroup.Add(opacityAnimation);

                    item.PlayingSoundItemTemplate.StartAnimation(animationGroup);
                }

                PlayingSoundsToShowList.Clear();
            }

            UpdateGridSplitterRange();
        }

        private void InitBottomPlayingSoundsBarHeight()
        {
            // Update the min and max height of the bottom row def
            UpdateGridSplitterRange();

            if (BottomPlayingSoundsBarListView.Items.Count > 0)
            {
                // Get the height of the first PlayingSound item and set the height of the BottomPlayingSoundsBar
                double firstItemHeight = GetBottomPlayingSoundItemContentHeight();
                BottomPlayingSoundsBar.Height = firstItemHeight;
                GridSplitterGridBottomRowDef.MinHeight = firstItemHeight;
                GridSplitterGridBottomRowDef.Height = new GridLength(firstItemHeight);
            }
        }

        private void UpdateGridSplitterRange()
        {
            // Set the max height of the bottom row def
            double totalHeight = GetTotalBottomPlayingSoundsListContentHeight();

            if (totalHeight == 0)
                return;
            
            if (totalHeight > maxBottomPlayingSoundsBarHeight)
                totalHeight = maxBottomPlayingSoundsBarHeight;

            GridSplitterGridBottomRowDef.MaxHeight = totalHeight;

            // Set the min height of the bottom row def
            double firstItemHeight = GetBottomPlayingSoundItemContentHeight();
            if (firstItemHeight >= maxBottomPlayingSoundsBarHeight) firstItemHeight = GridSplitterGridBottomRowDef.ActualHeight;

            GridSplitterGridBottomRowDef.MinHeight = firstItemHeight + bottomSoundsBarHeight;
        }

        private double GetTotalBottomPlayingSoundsListContentHeight()
        {
            double totalHeight = bottomSoundsBarHeight;

            foreach (var item in ReversedPlayingSoundItemContainers)
                totalHeight += item.ContentHeight;

            return totalHeight;
        }

        private double GetBottomPlayingSoundItemContentHeight(int index = 0)
        {
            int currentIndex = 0;

            foreach (var item in ReversedPlayingSoundItemContainers)
            {
                if (item.IsVisible)
                {
                    if (currentIndex == index)
                        return item.ContentHeight;

                    currentIndex++;
                }
            }

            return 0;
        }

        private int GetNumberOfVisibleItemsInReversedPlayingSoundItemContainers()
        {
            int visibleItems = 0;

            foreach (var item in ReversedPlayingSoundItemContainers)
                if (item.IsVisible)
                    visibleItems++;

            return visibleItems;
        }

        private void UpdatePlayingSoundsList()
        {
            if (FileManager.itemViewHolder.PlayingSoundsListVisible)
            {
                // Set the max width of the sounds list and playing sounds list columns
                PlayingSoundsBarColDef.MaxWidth = ContentRoot.ActualWidth / 2;

                if (!FileManager.itemViewHolder.OpenMultipleSounds || IsMobile)
                {
                    // Hide the PlayingSoundsBar and the GridSplitter
                    PlayingSoundsBarColDef.MinWidth = 0;
                    PlayingSoundsBarColDef.Width = new GridLength(0);
                    GridSplitterColDef.Width = new GridLength(0);

                    int playingSoundItemsCount = GetNumberOfVisibleItemsInReversedPlayingSoundItemContainers();

                    // Update the visibility of the BottomPlayingSoundsBar
                    if (playingSoundItemsCount == 0)
                    {
                        BottomPlayingSoundsBar.Translation = new Vector3(-10000, 0, 0);
                        GridSplitterGrid.Visibility = Visibility.Collapsed;
                    }
                    else if (playingSoundItemsCount == 1)
                    {
                        BottomPlayingSoundsBar.Translation = new Vector3(0);
                        GridSplitterGrid.Visibility = Visibility.Visible;

                        // Set the height of the bottom row def, but hide the GridSplitter
                        BottomPlayingSoundsBarGridSplitter.Visibility = Visibility.Collapsed;
                        GridSplitterGridBottomRowDef.Height = new GridLength(GetBottomPlayingSoundItemContentHeight());
                    }
                    else
                    {
                        BottomPlayingSoundsBar.Translation = new Vector3(0);
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
                    BottomPlayingSoundsBar.Translation = new Vector3(-10000, 0, 0);
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
                BottomPlayingSoundsBar.Translation = new Vector3(-10000, 0, 0);
                GridSplitterGrid.Visibility = Visibility.Collapsed;
            }

            AdaptSoundListScrollViewerForBottomPlayingSoundsBar();
        }

        private void AdaptSoundListScrollViewerForBottomPlayingSoundsBar()
        {
            double bottomPlayingSoundsBarHeight = 0;

            if (IsMobile && FileManager.itemViewHolder.PlayingSounds.Count > 0)
                bottomPlayingSoundsBarHeight = GridSplitterGridBottomRowDef.ActualHeight + (FileManager.itemViewHolder.PlayingSounds.Count <= 1 ? 0 : 16);

            AdaptSoundListScrollViewerForBottomPlayingSoundsBar(bottomPlayingSoundsBarHeight);
        }

        private void AdaptSoundListScrollViewerForBottomPlayingSoundsBar(double height)
        {
            // Set the padding of the sound GridViews and ListViews, so that the ScrollViewer ends at the bottom bar and the list continues behind the bottom bar
            SoundGridView.Padding = new Thickness(
                SoundGridView.Padding.Left,
                SoundGridView.Padding.Top,
                SoundGridView.Padding.Right,
                height
            );
            FavouriteSoundGridView.Padding = new Thickness(
                FavouriteSoundGridView.Padding.Left,
                FavouriteSoundGridView.Padding.Top,
                FavouriteSoundGridView.Padding.Right,
                height
            );
            SoundListView.Padding = new Thickness(
                SoundListView.Padding.Left,
                SoundListView.Padding.Top,
                SoundListView.Padding.Right,
                height
            );
            FavouriteSoundListView.Padding = new Thickness(
                FavouriteSoundListView.Padding.Left,
                FavouriteSoundListView.Padding.Top,
                FavouriteSoundListView.Padding.Right,
                height
            );
            SoundGridView2.Padding = new Thickness(
                SoundGridView2.Padding.Left,
                SoundGridView2.Padding.Top,
                SoundGridView2.Padding.Right,
                height
            );
            SoundListView2.Padding = new Thickness(
                SoundListView2.Padding.Left,
                SoundListView2.Padding.Top,
                SoundListView2.Padding.Right,
                height
            );
        }

        private async Task ShowBottomPlayingSoundsBar()
        {
            double firstItemHeight = GetBottomPlayingSoundItemContentHeight();
            if (firstItemHeight == 0) return;

            BottomPlayingSoundsBar.Translation = new Vector3(0, (float)firstItemHeight, 0);
            BottomPlayingSoundsBar.Opacity = 1;

            // Animate showing the BottomPlayingSoundsBar
            var translationAnimation = Window.Current.Compositor.CreateVector3KeyFrameAnimation();
            translationAnimation.InsertKeyFrame(1.0f, new Vector3(0));
            translationAnimation.Duration = TimeSpan.FromMilliseconds(animationDuration);
            translationAnimation.Target = "Translation";

            BottomPlayingSoundsBar.StartAnimation(translationAnimation);

            // Animate showing the BottomPlayingSoundsBar background
            var translationAnimation2 = Window.Current.Compositor.CreateVector3KeyFrameAnimation();
            translationAnimation2.InsertKeyFrame(1.0f, new Vector3(0, -(float)firstItemHeight, 0));
            translationAnimation2.Duration = TimeSpan.FromMilliseconds(animationDuration);
            translationAnimation2.Target = "Translation";

            BottomPlayingSoundsBarBackgroundGrid.StartAnimation(translationAnimation2);

            await Task.Delay(animationDuration);

            // Animate showing the grid splitter
            ShowGridSplitter();

            await Task.Delay(animationDuration);
        }

        private async Task HideBottomPlayingSoundsBar()
        {
            double firstItemHeight = GetBottomPlayingSoundItemContentHeight();

            // Move the BottomPlayingSoundsBar down
            var bottomPlayingSoundsBarTranslationAnimation = Window.Current.Compositor.CreateVector3KeyFrameAnimation();
            bottomPlayingSoundsBarTranslationAnimation.InsertKeyFrame(1.0f, new Vector3(0, (float)firstItemHeight, 0));
            bottomPlayingSoundsBarTranslationAnimation.Duration = TimeSpan.FromMilliseconds(animationDuration);
            bottomPlayingSoundsBarTranslationAnimation.Target = "Translation";

            BottomPlayingSoundsBar.StartAnimation(bottomPlayingSoundsBarTranslationAnimation);

            // Move the BottomPlayingSoundsBar background down
            var backgroundTranslationAnimation = Window.Current.Compositor.CreateVector3KeyFrameAnimation();
            backgroundTranslationAnimation.InsertKeyFrame(1.0f, new Vector3(0));
            backgroundTranslationAnimation.Duration = TimeSpan.FromMilliseconds(animationDuration);
            backgroundTranslationAnimation.Target = "Translation";

            BottomPlayingSoundsBarBackgroundGrid.StartAnimation(backgroundTranslationAnimation);

            if (bottomSoundsBarHeight > 0)
            {
                // Move the BottomSoundsBar down
                var bottomSoundsBarTranslationAnimation = compositor.CreateVector3KeyFrameAnimation();
                bottomSoundsBarTranslationAnimation.InsertKeyFrame(1.0f, new Vector3(0, (float)(bottomSoundsBarHeight + firstItemHeight), 0));
                bottomSoundsBarTranslationAnimation.Duration = TimeSpan.FromMilliseconds(animationDuration);
                bottomSoundsBarTranslationAnimation.Target = "Translation";

                BottomSoundsBar.StartAnimation(bottomSoundsBarTranslationAnimation);
            }

            await Task.Delay(animationDuration);

            GridSplitterGrid.Visibility = Visibility.Collapsed;
            BottomPlayingSoundsBar.Translation = new Vector3(-10000, 0, 0);
            BottomPlayingSoundsBar.Height = double.NaN;
            bottomSoundsBarHeight = 0;
        }

        private void ShowGridSplitter()
        {
            var opacityAnimation = Window.Current.Compositor.CreateScalarKeyFrameAnimation();
            opacityAnimation.InsertKeyFrame(1.0f, 1);
            opacityAnimation.Duration = TimeSpan.FromMilliseconds(animationDuration);
            opacityAnimation.Target = "Opacity";

            BottomPlayingSoundsBarGridSplitter.StartAnimation(opacityAnimation);

            AdaptSoundListScrollViewerForBottomPlayingSoundsBar();
        }

        private async Task HideGridSplitter()
        {
            var opacityAnimation = Window.Current.Compositor.CreateScalarKeyFrameAnimation();
            opacityAnimation.InsertKeyFrame(1.0f, 0);
            opacityAnimation.Duration = TimeSpan.FromMilliseconds(animationDuration);
            opacityAnimation.Target = "Opacity";

            BottomPlayingSoundsBarGridSplitter.StartAnimation(opacityAnimation);

            await Task.Delay(animationDuration);

            BottomPlayingSoundsBarGridSplitter.Visibility = Visibility.Collapsed;
            BottomPlayingSoundsBarGridSplitter.Opacity = 1;
            AdaptSoundListScrollViewerForBottomPlayingSoundsBar();
        }

        private void SnapBottomPlayingSoundsBar()
        {
            double start = BottomPlayingSoundsBar.ActualHeight + bottomSoundsBarHeight;
            double currentPosition = start - GridSplitterGridBottomRowDef.MinHeight;
            double maxPosition = GridSplitterGridBottomRowDef.MaxHeight - GridSplitterGridBottomRowDef.MinHeight;

            if (currentPosition < maxPosition / 2)
            {
                StartSnapBottomPlayingSoundsBarAnimation(start, GridSplitterGridBottomRowDef.MinHeight);
                bottomPlayingSoundsBarPosition = BottomPlayingSoundsBarVerticalPosition.Bottom;
            }
            else
            {
                StartSnapBottomPlayingSoundsBarAnimation(start, GridSplitterGridBottomRowDef.MaxHeight);
                bottomPlayingSoundsBarPosition = BottomPlayingSoundsBarVerticalPosition.Top;
            }
        }

        private async void StartSnapBottomPlayingSoundsBarAnimation(double start, double end)
        {
            if (!playingSoundsLoaded || start == end)
                return;

            // Move the GridSplitter exactly above the BottomPlayingSoundsBar
            GridSplitterGrid.Translation = new Vector3(
                0,
                (float)(GridSplitterGridBottomRowDef.ActualHeight - BottomPlayingSoundsBar.ActualHeight - bottomSoundsBarHeight),
                0
            );

            // Animate the BottomPlayingSoundsBar
            var translationAnimation = Window.Current.Compositor.CreateVector3KeyFrameAnimation();
            translationAnimation.Duration = TimeSpan.FromMilliseconds(animationDuration);
            translationAnimation.Target = "Translation";

            if (start < end)
            {
                // BottomPlayingSoundsBar snaps to top
                BottomPlayingSoundsBar.Height = end;
                BottomPlayingSoundsBar.Translation = new Vector3(0, (float)(end - start), 0);

                translationAnimation.InsertKeyFrame(1.0f, new Vector3(0));
            }
            else
            {
                // BottomPlayingSoundsBar snaps to bottom
                translationAnimation.InsertKeyFrame(1.0f, new Vector3(0, (float)(start - end - bottomSoundsBarHeight), 0));
            }

            BottomPlayingSoundsBar.StartAnimation(translationAnimation);

            // Animate the BottomPlayingSoundsBar background
            var backgroundTranslationAnimation = Window.Current.Compositor.CreateVector3KeyFrameAnimation();
            backgroundTranslationAnimation.InsertKeyFrame(1.0f, new Vector3(0, -(float)end - 2, 0));
            backgroundTranslationAnimation.Duration = TimeSpan.FromMilliseconds(animationDuration);
            backgroundTranslationAnimation.Target = "Translation";

            BottomPlayingSoundsBarBackgroundGrid.StartAnimation(backgroundTranslationAnimation);

            // Animate the GridSplitter
            var translationAnimation3 = Window.Current.Compositor.CreateVector3KeyFrameAnimation();
            translationAnimation3.InsertKeyFrame(1.0f, new Vector3(0, -(float)(end - start - GridSplitterGrid.Translation.Y), 0));
            translationAnimation3.Duration = TimeSpan.FromMilliseconds(animationDuration);
            translationAnimation3.Target = "Translation";

            GridSplitterGrid.StartAnimation(translationAnimation3);

            await Task.Delay(animationDuration);

            // Adapt the elements to the new position
            GridSplitterGridBottomRowDef.Height = new GridLength(end);
            BottomPlayingSoundsBar.Height = end - bottomSoundsBarHeight;
            BottomPlayingSoundsBar.Translation = new Vector3(0, -(float)bottomSoundsBarHeight, 0);
            GridSplitterGrid.Translation = new Vector3(0);
        }
    }
}
