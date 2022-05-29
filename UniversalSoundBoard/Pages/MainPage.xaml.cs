using System;
using System.Linq;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI;
using Windows.ApplicationModel.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Media;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using WinUI = Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using UniversalSoundboard.Components;
using Windows.UI.Xaml.Controls.Primitives;
using System.ComponentModel;
using System.Collections.Specialized;
using UniversalSoundboard.Common;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Models;
using davClassLibrary;
using DotNetTools.SharpGrabber;
using DotNetTools.SharpGrabber.Grabbed;
using System.Collections.ObjectModel;
using Google.Apis.YouTube.v3.Data;
using Microsoft.AppCenter.Analytics;
using Windows.System;
using Windows.Graphics.Display;

namespace UniversalSoundboard.Pages
{
    public sealed partial class MainPage : Page
    {
        readonly ResourceLoader loader = new ResourceLoader();
        public static CoreDispatcher dispatcher;                            // Dispatcher for ShareTargetPage
        string appTitle = "UniversalSoundboard";                            // The app name displayed in the title bar
        private readonly ObservableCollection<object> menuItems = new ObservableCollection<object>();
        private Guid initialCategory = Guid.Empty;                          // The category that was selected before the sounds started loading
        private Guid selectedCategory = Guid.Empty;                         // The category that was right clicked for the flyout
        private List<string> Suggestions = new List<string>();              // The suggestions for the SearchAutoSuggestBox
        private List<StorageFile> sharedFiles = new List<StorageFile>();    // The files that get shared
        bool selectionButtonsEnabled = false;                               // If true, the buttons for multi selection are enabled
        bool downloadFilesDialogIsVisible = false;
        bool downloadFilesCanceled = false;
        bool downloadFilesFailed = false;
        bool mobileSearchVisible = false;                                   // If true, the app window is small, the search box is visible and the other top buttons are hidden
        bool playingSoundsLoaded = false;
        public static double windowWidth = 500;
        public static Style buttonRevealStyle;
        public static DataTemplate hotkeyButtonTemplate;
        public static uint screenWidth = 0;
        public static uint screenHeight = 0;

        public MainPage()
        {
            InitializeComponent();
            SetThemeColors();

            RootGrid.DataContext = FileManager.itemViewHolder;
            dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;

            SystemNavigationManager.GetForCurrentView().BackRequested += MainPage_BackRequested;
            FileManager.itemViewHolder.PropertyChanged += ItemViewHolder_PropertyChanged;
            FileManager.itemViewHolder.PlayingSounds.CollectionChanged += PlayingSounds_CollectionChanged;
            FileManager.itemViewHolder.PlayingSoundsLoaded += ItemViewHolder_PlayingSoundsLoaded;
            FileManager.itemViewHolder.CategoriesLoaded += ItemViewHolder_CategoriesLoaded;
            FileManager.itemViewHolder.UserSyncFinished += ItemViewHolder_UserSyncFinished;
            FileManager.itemViewHolder.UserPlanChanged += ItemViewHolder_UserPlanChanged;
            FileManager.itemViewHolder.CategoryUpdated += ItemViewHolder_CategoryUpdated;
            FileManager.itemViewHolder.CategoryDeleted += ItemViewHolder_CategoryDeleted;
            FileManager.itemViewHolder.TableObjectFileDownloadProgressChanged += ItemViewHolder_TableObjectFileDownloadProgressChanged;
            FileManager.itemViewHolder.TableObjectFileDownloadCompleted += ItemViewHolder_TableObjectFileDownloadCompleted;
            FileManager.itemViewHolder.SelectedSounds.CollectionChanged += SelectedSounds_CollectionChanged;
            FileManager.itemViewHolder.DownloadYoutubeVideo += ItemViewHolder_DownloadYoutubeVideo;
            FileManager.itemViewHolder.DownloadYoutubePlaylist += ItemViewHolder_DownloadYoutubePlaylist;
            FileManager.itemViewHolder.DownloadAudioFile += ItemViewHolder_DownloadAudioFile;

            // Get the screen resolution
            var displayInfo = DisplayInformation.GetForCurrentView();
            screenWidth = displayInfo.ScreenWidthInRawPixels;
            screenHeight = displayInfo.ScreenHeightInRawPixels;
        }

        #region Page event handlers
        async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            AdjustLayout();

            // Load the Categories
            await FileManager.LoadCategoriesAsync();

            // Select the first menu item
            await Task.Delay(2);
            SelectCategory(Guid.Empty);

            // Load the PlayingSounds
            await FileManager.LoadPlayingSoundsAsync();

            // Load the Sounds
            if (initialCategory.Equals(Guid.Empty))
                await FileManager.ShowAllSoundsAsync();
            else
                await FileManager.ShowCategoryAsync(initialCategory);

            FileManager.itemViewHolder.TriggerSoundsLoadedEvent(this);

            IncreaseAppStartCounter();
            await FileManager.StartHotkeyProcess();
            await Dav.SyncData();
        }

        async void MainPage_BackRequested(object sender, BackRequestedEventArgs e)
        {
            await GoBack();
            e.Handled = true;
        }

        private void MainPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            windowWidth = Window.Current.Bounds.Width;

            AdjustLayout();
            FileManager.UpdateInAppNotificationPositions();
        }

        private void TitleRelativePanel_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateTitleWidth();
        }

        private void TitleButtonStackPanel_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateTitleWidth();
        }

        private void ItemViewHolder_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case ItemViewHolder.PageKey:
                case ItemViewHolder.AppStateKey:
                    // Set the visibility of the NavigationViewHeader
                    var visibility = Visibility.Collapsed;

                    if (
                        FileManager.itemViewHolder.AppState == FileManager.AppState.InitialSync
                        || FileManager.itemViewHolder.AppState == FileManager.AppState.Empty
                    ) visibility = FileManager.itemViewHolder.Page == typeof(SoundPage) ? Visibility.Collapsed : Visibility.Visible;
                    else if (FileManager.itemViewHolder.AppState == FileManager.AppState.Normal)
                        visibility = Visibility.Visible;
                    else if(FileManager.itemViewHolder.AppState == FileManager.AppState.Loading)
                        visibility = playingSoundsLoaded ? Visibility.Visible : Visibility.Collapsed;

                    NavigationViewHeader.Visibility = visibility;
                    break;
                case ItemViewHolder.CurrentThemeKey:
                    SetThemeColors();
                    break;
                case ItemViewHolder.TitleKey:
                    UpdateTitleWidth();
                    break;
                case ItemViewHolder.SearchAutoSuggestBoxVisibleKey:
                    UpdateTopButtonVisibilityForMobileSearch();
                    break;
            }
        }

        private void PlayingSounds_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            FileManager.UpdateInAppNotificationPositions();
        }

        private void ItemViewHolder_PlayingSoundsLoaded(object sender, EventArgs e)
        {
            playingSoundsLoaded = true;
        }

        private void ItemViewHolder_CategoriesLoaded(object sender, EventArgs e)
        {
            LoadMenuItems();
        }

        private void ItemViewHolder_UserSyncFinished(object sender, EventArgs e)
        {
            Bindings.Update();
        }

        private void ItemViewHolder_UserPlanChanged(object sender, EventArgs e)
        {
            AdjustLayout();
        }

        private async void ItemViewHolder_CategoryUpdated(object sender, CategoryEventArgs args)
        {
            // Update the text and icon of the menu item of the category
            UpdateCategoryMenuItem(menuItems, await FileManager.GetCategoryAsync(args.Uuid));
        }

        private void ItemViewHolder_CategoryDeleted(object sender, CategoryEventArgs args)
        {
            // Remove the category from the SideBar
            RemoveCategoryMenuItem(menuItems, args.Uuid);
        }

        private async void ItemViewHolder_TableObjectFileDownloadProgressChanged(object sender, TableObjectFileDownloadProgressChangedEventArgs e)
        {
            if(e.Value == -1)
            {
                // Check if this value belongs to a sound in the Download dialog
                int i = ContentDialogs.downloadingFilesSoundsList.FindIndex(sound => sound.AudioFileTableObject.Uuid.Equals(e.Uuid));
                if(i != -1)
                {
                    // Show download error dialog
                    downloadFilesFailed = true;

                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        // Close the download files dialog
                        ContentDialogs.DownloadFilesContentDialog.Hide();
                    });
                }
            }
        }

        private async void ItemViewHolder_TableObjectFileDownloadCompleted(object sender, TableObjectFileDownloadCompletedEventArgs e)
        {
            if (downloadFilesDialogIsVisible && !downloadFilesFailed)
            {
                bool isDownloading = false;

                // Check if all files were downloaded
                foreach(var sound in ContentDialogs.downloadingFilesSoundsList)
                {
                    var downloadStatus = sound.GetAudioFileDownloadStatus();

                    if (
                        downloadStatus == TableObjectFileDownloadStatus.Downloading
                        || downloadStatus == TableObjectFileDownloadStatus.NotDownloaded
                    )
                    {
                        isDownloading = true;
                        break;
                    }
                }

                if (!isDownloading)
                {
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        // Close the download files dialog
                        ContentDialogs.DownloadFilesContentDialog.Hide();
                    });
                }
            }
        }

        private void SelectedSounds_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            selectionButtonsEnabled = FileManager.itemViewHolder.SelectedSounds.Count > 0;
            Bindings.Update();
        }

        private async void ItemViewHolder_DownloadYoutubeVideo(object sender, EventArgs e)
        {
            await DownloadSoundsContentDialog_YoutubeVideoDownload();
        }

        private async void ItemViewHolder_DownloadYoutubePlaylist(object sender, EventArgs e)
        {
            await DownloadSoundsContentDialog_YoutubePlaylistDownload();
        }

        private async void ItemViewHolder_DownloadAudioFile(object sender, EventArgs e)
        {
            await DownloadSoundsContentDialog_AudioFileDownload();
        }
        #endregion

        #region General methods
        private void CustomiseTitleBar()
        {
            ApplicationViewTitleBar titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ButtonForegroundColor = FileManager.itemViewHolder.CurrentTheme == FileManager.AppTheme.Dark ? Colors.White : Colors.Black;
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

            // Set custom title bar
            CoreApplicationViewTitleBar coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;
            Window.Current.SetTitleBar(TitleBar);
        }

        private void InitLayout()
        {
            // Init the static styles
            buttonRevealStyle = Resources["ButtonRevealStyle"] as Style;
            hotkeyButtonTemplate = Resources["HotkeyButtonTemplate"] as DataTemplate;

            SideBar.ExpandedModeThresholdWidth = FileManager.sideBarCollapsedMaxWidth;

            // Set the background of the sidebar content
            SideBar.Background = new SolidColorBrush(FileManager.GetApplicationThemeColor());

            // Initialize the acrylic background of the SideBar
            Application.Current.Resources["NavigationViewExpandedPaneBackground"] = new AcrylicBrush();

            FileManager.UpdateLayoutColors();

            // Set the value of the volume slider
            VolumeControl.Value = FileManager.itemViewHolder.Volume;
            VolumeControl.Muted = FileManager.itemViewHolder.Muted;

            // Set the VolumeButton icon
            VolumeButtonIcon.Text = VolumeControl.GetVolumeIcon(FileManager.itemViewHolder.Volume, FileManager.itemViewHolder.Muted);
        }

        private void AdjustLayout()
        {
            FileManager.AdjustLayout();
            UpdateTopButtonVisibilityForMobileSearch();

            // Set the width of the title bar and the position of the title, depending on whether the Hamburger button of the NavigationView is visible
            if (SideBar.DisplayMode == WinUI.NavigationViewDisplayMode.Minimal)
            {
                TitleBar.Width = Window.Current.Bounds.Width - 96;
                WindowTitleTextBlock.Margin = new Thickness(97, 15, 0, 0);
            }
            else
            {
                TitleBar.Width = Window.Current.Bounds.Width - 48;
                WindowTitleTextBlock.Margin = new Thickness(65, 15, 0, 0);
            }

            // Update the margin of the profile image in the Sidebar
            if (SideBar.IsPaneOpen)
                AccountMenuItem.Margin = new Thickness(-2, 1, 0, 0);
            else
                AccountMenuItem.Margin = new Thickness(2, 1, 0, 0);

            // Update the app title if the user is on dav Plus or Pro
            if (Dav.IsLoggedIn)
            {
                if (((int)Dav.User.Plan) == 1)
                    appTitle = "UniversalSoundboard Plus";
                else if (((int)Dav.User.Plan) == 2)
                    appTitle = "UniversalSoundboard Pro";

                Bindings.Update();
            }
        }

        private void UpdateTitleWidth()
        {
            double newTitleWidth = TitleRelativePanel.ActualWidth - TitleButtonStackPanel.ActualWidth - 10;
            if (newTitleWidth <= 0) return;

            TitleTextBlock.MaxWidth = newTitleWidth;
        }

        private void SetThemeColors()
        {
            InitLayout();
            RequestedTheme = FileManager.GetRequestedTheme();
            CustomiseTitleBar();
        }

        private void UpdateTopButtonVisibilityForMobileSearch()
        {
            // Hide the top buttons if the app window is small and the search box is visible
            mobileSearchVisible = Window.Current.Bounds.Width < FileManager.hideSearchBoxMaxWidth && FileManager.itemViewHolder.SearchAutoSuggestBoxVisible;
            Bindings.Update();
        }

        private async Task GoBack()
        {
            await FileManager.GoBackAsync();
            AdjustLayout();

            if (FileManager.itemViewHolder.SelectedCategory.Equals(Guid.Empty))
            {
                await Task.Delay(5);
                SelectCategory(Guid.Empty);
            }
        }

        private void SelectCategory(Guid categoryUuid)
        {
            foreach (WinUI.NavigationViewItem item in menuItems)
                SelectSubCategory(item, categoryUuid);
        }

        private void SelectSubCategory(WinUI.NavigationViewItem parent, Guid categoryUuid)
        {
            if (((Guid)parent.Tag).Equals(categoryUuid))
            {
                parent.IsSelected = true;
                return;
            }

            foreach (var menuItem in parent.MenuItems)
            {
                WinUI.NavigationViewItem item = (WinUI.NavigationViewItem)menuItem;
                SelectSubCategory(item, categoryUuid);
            }
        }

        private async Task SelectCurrentCategory()
        {
            await Task.Delay(2);
            SelectCategory(FileManager.itemViewHolder.SelectedCategory);
        }

        private void IncreaseAppStartCounter()
        {
            FileManager.itemViewHolder.AppStartCounter++;
            int count = FileManager.itemViewHolder.AppStartCounter;

            if (count % 20 == 0)
            {
                Analytics.TrackEvent("AppStarts", new Dictionary<string, string>
                {
                    { "Count", count.ToString() }
                });
            }

            if (!FileManager.itemViewHolder.AppReviewed && count % 100 == 0)
            {
                // Show InAppNotification for writing a review
                var args = new ShowInAppNotificationEventArgs(
                    FileManager.InAppNotificationType.WriteReview,
                    loader.GetString("InAppNotification-WriteReview"),
                    0,
                    false,
                    true,
                    loader.GetString("Actions-WriteReview")
                );
                args.PrimaryButtonClick += WriteReviewInAppNotificationEventArgs_PrimaryButtonClick;

                FileManager.itemViewHolder.TriggerShowInAppNotificationEvent(this, args);
            }
        }

        private async void WriteReviewInAppNotificationEventArgs_PrimaryButtonClick(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri("ms-windows-store://review/?ProductId=9NBLGGH51005"));
            FileManager.DismissInAppNotification(FileManager.InAppNotificationType.WriteReview);

            Analytics.TrackEvent("InAppNotification-WriteReview-PrimaryButtonClick", new Dictionary<string, string>
            {
                { "AppStarts", FileManager.itemViewHolder.AppStartCounter.ToString() }
            });
        }
        #endregion

        #region MenuItem methods
        private void LoadMenuItems()
        {
            foreach (var menuItem in CreateMenuItemsforCategories(FileManager.itemViewHolder.Categories.ToList()))
            {
                // Check if the menu items list already contains the item
                var itemUuid = (Guid)menuItem.Tag;
                bool containsItem = false;

                foreach(var mi in menuItems)
                {
                    Guid uuid = (Guid)((WinUI.NavigationViewItem)mi).Tag;

                    if (itemUuid.Equals(uuid))
                    {
                        containsItem = true;
                        break;
                    }
                }

                if (!containsItem)
                    menuItems.Add(menuItem);
            }
        }

        private List<WinUI.NavigationViewItem> CreateMenuItemsforCategories(List<Category> categories)
        {
            List<WinUI.NavigationViewItem> menuItems = new List<WinUI.NavigationViewItem>();

            foreach (var category in categories)
            {
                WinUI.NavigationViewItem item = new WinUI.NavigationViewItem
                {
                    Tag = category.Uuid,
                    Content = category.Name,
                    Icon = new FontIcon { Glyph = category.Icon }
                };

                item.RightTapped += NavigationViewMenuItem_RightTapped;

                foreach (var childItem in CreateMenuItemsforCategories(category.Children.ToList()))
                    item.MenuItems.Add(childItem);

                menuItems.Add(item);
            }

            return menuItems;
        }

        private bool AddCategoryMenuItem(IList<object> menuItems, Category category, Guid parent)
        {
            if (parent.Equals(Guid.Empty))
            {
                // Add the category menu item to the end of the menuItems
                WinUI.NavigationViewItem item = new WinUI.NavigationViewItem
                {
                    Tag = category.Uuid,
                    Content = category.Name,
                    Icon = new FontIcon { Glyph = category.Icon }
                };

                item.RightTapped += NavigationViewMenuItem_RightTapped;
                menuItems.Add(item);
                return true;
            }

            // Find the MenuItem of the parent category
            foreach (var menuItem in menuItems)
            {
                WinUI.NavigationViewItem item = (WinUI.NavigationViewItem)menuItem;

                if ((Guid)item.Tag == parent)
                {
                    WinUI.NavigationViewItem newItem = new WinUI.NavigationViewItem
                    {
                        Tag = category.Uuid,
                        Content = category.Name,
                        Icon = new FontIcon { Glyph = category.Icon }
                    };

                    newItem.RightTapped += NavigationViewMenuItem_RightTapped;
                    item.MenuItems.Add(newItem);
                    return true;
                }
                else
                {
                    if (AddCategoryMenuItem(item.MenuItems, category, parent))
                        return true;
                }
            }

            return false;
        }

        private bool UpdateCategoryMenuItem(IList<object> menuItems, Category updatedCategory)
        {
            foreach(var menuItem in menuItems)
            {
                WinUI.NavigationViewItem item = (WinUI.NavigationViewItem)menuItem;

                if((Guid)item.Tag == updatedCategory.Uuid)
                {
                    item.Content = updatedCategory.Name;
                    item.Icon = new FontIcon { Glyph = updatedCategory.Icon };
                    return true;
                }
                else
                {
                    if (UpdateCategoryMenuItem(item.MenuItems, updatedCategory))
                        return true;
                }
            }

            return false;
        }

        private bool RemoveCategoryMenuItem(IList<object> menuItems, Guid uuid)
        {
            bool categoryFound = false;
            int i = 0;

            foreach(var menuItem in menuItems)
            {
                WinUI.NavigationViewItem item = (WinUI.NavigationViewItem)menuItem;

                if((Guid)item.Tag == uuid)
                {
                    categoryFound = true;
                    break;
                }
                else
                {
                    if (RemoveCategoryMenuItem(item.MenuItems, uuid))
                        return true;
                }

                i++;
            }

            if (categoryFound)
            {
                // Remove the menu item
                menuItems.RemoveAt(i);
                return true;
            }
            return false;
        }

        /**
         * Returns a list of the positions of the menu item parents and the category itself of the searched category in the correct order
         */
        private List<int> GetCategoryMenuItemPositionPath(Guid searchedCategoryUuid)
        {
            List<int> positions = new List<int>();
            List<WinUI.NavigationViewItem> parentItems = new List<WinUI.NavigationViewItem>();
            int i = 0;

            foreach(var menuItem in menuItems)
            {
                WinUI.NavigationViewItem item = (WinUI.NavigationViewItem)menuItem;
                Guid uuid = (Guid)item.Tag;
                if (uuid.Equals(Guid.Empty)) continue;

                positions.Add(i);
                parentItems.Add(item);
                if (BuildCategoryMenuItemPositionPath(positions, parentItems, searchedCategoryUuid))
                    return positions;
                else
                {
                    // Remove the position and the item from positions and from parentItems
                    positions.RemoveAt(positions.Count - 1);
                    parentItems.Remove(item);
                }

                i++;
            }

            return positions;
        }

        /**
         * Goes through the nested menu items tree and builds the path of the positions in positions and the path of the menu items in parentItems
         * Returns true if the last menu item belongs to the searched category or contains the menu item of it within the child tree
         */
        private bool BuildCategoryMenuItemPositionPath(List<int> positions, List<WinUI.NavigationViewItem> parentItems, Guid searchedCategoryUuid)
        {
            // Check if the last item in parentItems is the searched category
            if (((Guid)parentItems.Last().Tag).Equals(searchedCategoryUuid)) return true;
            int i = 0;

            // Add each child of the last element in parentItems to parentItems and call this method
            foreach(var childItem in parentItems.Last().MenuItems)
            {
                WinUI.NavigationViewItem item = (WinUI.NavigationViewItem)childItem;

                positions.Add(i);
                parentItems.Add(item);

                if (BuildCategoryMenuItemPositionPath(positions, parentItems, searchedCategoryUuid))
                    return true;
                else
                {
                    // Remove the position and the item from positions and from parentItems
                    positions.RemoveAt(positions.Count - 1);
                    parentItems.Remove(item);
                }

                i++;
            }

            return false;
        }

        /**
         * Returns the count of the children of the category, defined by the given path
         */
        private int GetCategoryMenuItemChildrenCountByPath(List<int> positionPath)
        {
            List<WinUI.NavigationViewItem> currentItemList = new List<WinUI.NavigationViewItem>();
            
            // Copy the SideBar menu items into the current items list
            for(int i = 1; i < menuItems.Count; i++)
                currentItemList.Add((WinUI.NavigationViewItem)menuItems[i]);

            foreach (var position in positionPath)
            {
                var currentItem = currentItemList.ElementAt(position);
                currentItemList.Clear();

                foreach (var childItem in currentItem.MenuItems)
                    currentItemList.Add((WinUI.NavigationViewItem)childItem);
            }

            return currentItemList.Count;
        }

        /**
         * Finds the category menu item with the given uuid and moves it up or down within the list of menu items
         * Should be called with menuItems = SideBar.MenuItems
         */
        private bool MoveCategoryMenuItem(IList<object> menuItems, Guid searchedCategoryUuid, bool up)
        {
            bool categoryFound = false;
            int i = 0;

            foreach (var menuItem in menuItems)
            {
                WinUI.NavigationViewItem item = (WinUI.NavigationViewItem)menuItem;

                if (((Guid)item.Tag).Equals(searchedCategoryUuid))
                {
                    categoryFound = true;
                    break;
                }
                else
                {
                    if (MoveCategoryMenuItem(item.MenuItems, searchedCategoryUuid, up))
                        return true;
                }

                i++;
            }

            if (categoryFound)
            {
                // Move the menu item
                var selectedItem = menuItems[i];
                menuItems.Remove(selectedItem);
                if (up)
                    menuItems.Insert(i - 1, selectedItem);
                else
                    menuItems.Insert(i + 1, selectedItem);

                return true;
            }
            return false;
        }

        /**
         * Finds the category menu item with the given uuid and moves it into the children of the menu item above or below
         * Should be called with menuItems = SideBar.MenuItems
         */
        private bool MoveCategoryMenuItemToMenuItem(IList<object> menuItems, Guid searchedCategoryUuid, bool up)
        {
            for (int i = 0; i < menuItems.Count; i++)
            {
                var item = (WinUI.NavigationViewItem)menuItems[i];

                if (((Guid)item.Tag).Equals(searchedCategoryUuid))
                {
                    var movedElement = menuItems.ElementAt(i);

                    // Remove the item from the menu items
                    menuItems.RemoveAt(i);

                    // Add the menu item to the category above or below
                    if (up)
                        ((WinUI.NavigationViewItem)menuItems[i - 1]).MenuItems.Add(movedElement);
                    else
                        ((WinUI.NavigationViewItem)menuItems[i]).MenuItems.Insert(0, movedElement);

                    return true;
                }
                else if (MoveCategoryMenuItemToMenuItem(item.MenuItems, searchedCategoryUuid, up))
                    return true;
            }
            return false;
        }

        /**
         * Finds the category menu item with the given uuid and moves it into the children of the parent menu item above or below
         * Should be called with menuItems = SideBar.MenuItems
         */
        private bool MoveCategoryMenuItemToParent(IList<object> menuItems, Guid uuid, bool up)
        {
            for(int i = 0; i < menuItems.Count; i++)
            {
                var item = (WinUI.NavigationViewItem)menuItems[i];
                bool categoryFound = false;
                int j = 0;

                foreach(var childMenuItem in item.MenuItems)
                {
                    var childItem = (WinUI.NavigationViewItem)childMenuItem;

                    if(((Guid)childItem.Tag).Equals(uuid))
                    {
                        categoryFound = true;
                        break;
                    }
                    
                    j++;
                }

                if (categoryFound)
                {
                    var movedElement = item.MenuItems.ElementAt(j);

                    // Remove the child from the children of the menu item
                    item.MenuItems.RemoveAt(j);

                    // Add the element to the parent
                    if(up)
                        menuItems.Insert(i, movedElement);
                    else
                        menuItems.Insert(i + 1, movedElement);

                    return true;
                }
                else if (MoveCategoryMenuItemToParent(item.MenuItems, uuid, up))
                    return true;
            }
            return false;
        }
        #endregion

        #region SideBar
        private async void SideBar_ItemInvoked(WinUI.NavigationView sender, WinUI.NavigationViewItemInvokedEventArgs args)
        {
            FileManager.itemViewHolder.SelectedSounds.Clear();
            FileManager.ResetSearchArea();

            if (args.IsSettingsInvoked)
            {
                // Show the Settings page
                FileManager.NavigateToSettingsPage();
            }
            else if (
                args.InvokedItemContainer == LoginMenuItem
                || args.InvokedItemContainer == AccountMenuItem
            )
            {
                // Show the Account page
                FileManager.NavigateToAccountPage();

                // Close the SideBar if it was open on mobile
                if (
                    SideBar.DisplayMode == WinUI.NavigationViewDisplayMode.Compact
                    || SideBar.DisplayMode == WinUI.NavigationViewDisplayMode.Minimal
                ) SideBar.IsPaneOpen = false;
            }
            else
            {
                // Show the selected category
                if (args.InvokedItemContainer == null) return;
                Guid categoryUuid = (Guid)args.InvokedItemContainer.Tag;
                if (categoryUuid.Equals(FileManager.itemViewHolder.SelectedCategory) && FileManager.itemViewHolder.Page == typeof(SoundPage)) return;

                // Set initialCategory if the playing sounds weren't still loaded
                if (FileManager.itemViewHolder.AppState == FileManager.AppState.Loading && !playingSoundsLoaded)
                    initialCategory = categoryUuid;
                else if (Equals(categoryUuid, Guid.Empty))
                    await FileManager.ShowAllSoundsAsync();
                else
                    await FileManager.ShowCategoryAsync(categoryUuid);
            }
        }

        private async void SideBar_BackRequested(WinUI.NavigationView sender, WinUI.NavigationViewBackRequestedEventArgs args)
        {
            await GoBack();
        }

        private void SideBar_DisplayModeChanged(WinUI.NavigationView sender, WinUI.NavigationViewDisplayModeChangedEventArgs args)
        {
            AdjustLayout();
        }

        private void SideBar_PaneOpening(WinUI.NavigationView sender, object args)
        {
            AccountMenuItem.Margin = new Thickness(-2, 1, 0, 0);
        }

        private void SideBar_PaneClosing(WinUI.NavigationView sender, WinUI.NavigationViewPaneClosingEventArgs args)
        {
            AccountMenuItem.Margin = new Thickness(2, 1, 0, 0);
        }

        private void NavigationViewMenuItem_RightTapped(object sender, Windows.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {
            e.Handled = true;
            selectedCategory = (Guid)((WinUI.NavigationViewItem)sender).Tag;
            if (selectedCategory.Equals(Guid.Empty)) return;

            // Show flyout for the category menu item
            ShowCategoryOptionsFlyout((UIElement)sender, e.GetPosition(sender as UIElement));
        }
        #endregion

        #region Edit Category
        private async void CategoryEditButton_Click(object sender, RoutedEventArgs e)
        {
            var editCategoryContentDialog = ContentDialogs.CreateEditCategoryContentDialog();
            editCategoryContentDialog.PrimaryButtonClick += EditCategoryContentDialog_PrimaryButtonClick;
            await ContentDialogs.ShowContentDialogAsync(editCategoryContentDialog);
        }

        private async void EditCategoryContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // Get categories List and save with new value
            // Get combobox value
            ComboBoxItem typeItem = (ComboBoxItem)ContentDialogs.IconSelectionComboBox.SelectedItem;
            string icon = typeItem.Content.ToString();
            string newName = ContentDialogs.EditCategoryTextBox.Text;

            // Update the category in the database
            await FileManager.UpdateCategoryAsync(FileManager.itemViewHolder.SelectedCategory, newName, icon);

            // Update the title and reload the category in the categories list
            FileManager.itemViewHolder.Title = newName;
            await FileManager.ReloadCategory(FileManager.itemViewHolder.SelectedCategory);
        }
        #endregion

        #region Delete Category
        private async void CategoryDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var deleteCategoryContentDialog = ContentDialogs.CreateDeleteCategoryContentDialogAsync();
            deleteCategoryContentDialog.PrimaryButtonClick += DeleteCategoryContentDialog_PrimaryButtonClick;
            await ContentDialogs.ShowContentDialogAsync(deleteCategoryContentDialog);
        }

        private async void DeleteCategoryContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            await FileManager.DeleteCategoryAsync(FileManager.itemViewHolder.SelectedCategory);

            // Remove the category from the Categories list
            FileManager.RemoveCategory(FileManager.itemViewHolder.SelectedCategory);

            // Navigate to all Sounds
            SelectCategory(Guid.Empty);
            await FileManager.ShowAllSoundsAsync();
        }
        #endregion

        #region Play All / Play Sounds Successively / Play Sounds Simultaneously
        private async void CategoryPlayAllButton_Click(object sender, RoutedEventArgs e)
        {
            List<Sound> sounds = new List<Sound>();

            // Check if it should play all sounds or the favourite sounds
            if (SoundPage.soundsPivotSelected || !FileManager.itemViewHolder.ShowSoundsPivot)
                foreach (Sound sound in FileManager.itemViewHolder.Sounds)
                    sounds.Add(sound);
            else
                foreach (Sound sound in FileManager.itemViewHolder.FavouriteSounds)
                    sounds.Add(sound);

            var template = (DataTemplate)Resources["SoundItemTemplate"];
            var listViewItemStyle = Resources["ListViewItemStyle"] as Style;

            var playSoundsSuccessivelyContentDialog = ContentDialogs.CreatePlaySoundsSuccessivelyContentDialog(sounds, template, listViewItemStyle);
            playSoundsSuccessivelyContentDialog.PrimaryButtonClick += PlaySoundsSuccessivelyContentDialog_PrimaryButtonClick;
            await ContentDialogs.ShowContentDialogAsync(playSoundsSuccessivelyContentDialog);
        }

        private async void PlaySoundsButton_Click(object sender, RoutedEventArgs e)
        {
            // Check if Flyout with option to play sounds simultaneously should be shown
            if(
                (FileManager.itemViewHolder.PlayingSoundsListVisible && FileManager.itemViewHolder.OpenMultipleSounds && FileManager.itemViewHolder.MultiSoundPlayback)
                || (!FileManager.itemViewHolder.PlayingSoundsListVisible && FileManager.itemViewHolder.MultiSoundPlayback)
            )
            {
                // Show flyout
                MenuFlyout flyout = new MenuFlyout();

                MenuFlyoutItem playSoundsSuccessivelyFlyoutItem = new MenuFlyoutItem
                {
                    Text = loader.GetString("PlaySoundsSuccessively")
                };
                playSoundsSuccessivelyFlyoutItem.Click += PlaySoundsSuccessivelyFlyoutItem_Click;
                flyout.Items.Add(playSoundsSuccessivelyFlyoutItem);

                MenuFlyoutItem playSoundsSimultaneouslyFlyouItem = new MenuFlyoutItem
                {
                    Text = loader.GetString("PlaySoundsSimultaneously"),
                    IsEnabled = FileManager.itemViewHolder.SelectedSounds.Count <=5
                };
                playSoundsSimultaneouslyFlyouItem.Click += PlaySoundsSimultaneouslyFlyoutItem_Click;

                flyout.Items.Add(playSoundsSimultaneouslyFlyouItem);
                flyout.ShowAt(PlaySoundsButton, new FlyoutShowOptions { Placement = FlyoutPlacementMode.Bottom });
            }
            else
            {
                await ShowPlaySelectedSoundsSuccessivelyDialog();
            }
        }

        private async Task ShowPlaySelectedSoundsSuccessivelyDialog()
        {
            List<Sound> sounds = new List<Sound>();
            foreach (Sound sound in FileManager.itemViewHolder.SelectedSounds)
                sounds.Add(sound);

            var template = (DataTemplate)Resources["SoundItemTemplate"];
            var listViewItemStyle = Resources["ListViewItemStyle"] as Style;

            var playSoundsSuccessivelyContentDialog = ContentDialogs.CreatePlaySoundsSuccessivelyContentDialog(sounds, template, listViewItemStyle);
            playSoundsSuccessivelyContentDialog.PrimaryButtonClick += PlaySoundsSuccessivelyContentDialog_PrimaryButtonClick;
            await ContentDialogs.ShowContentDialogAsync(playSoundsSuccessivelyContentDialog);
        }

        private async void PlaySoundsSuccessivelyFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            await ShowPlaySelectedSoundsSuccessivelyDialog();
        }

        private async void PlaySoundsSuccessivelyContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            bool randomly = (bool)ContentDialogs.RandomCheckBox.IsChecked;
            int rounds = int.MaxValue;

            if (ContentDialogs.RepeatsComboBox.SelectedItem != ContentDialogs.RepeatsComboBox.Items.Last())
                if (int.TryParse(ContentDialogs.RepeatsComboBox.SelectedValue.ToString(), out rounds))
                    rounds--;

            await SoundPage.PlaySoundsAsync(ContentDialogs.SoundsList.ToList(), rounds, randomly);

            // Disable multi-selection mode
            FileManager.itemViewHolder.MultiSelectionEnabled = false;
        }

        private async void PlaySoundsSimultaneouslyFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            foreach (Sound sound in FileManager.itemViewHolder.SelectedSounds)
                await SoundPage.PlaySoundAsync(sound);

            // Disable multi-selection mode
            FileManager.itemViewHolder.MultiSelectionEnabled = false;
        }
        #endregion

        #region Volume Control
        private void VolumeControl_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            var volumeSlider = sender as Slider;

            // Save the new volume
            FileManager.itemViewHolder.Volume = Convert.ToInt32(volumeSlider.Value);
        }

        private void VolumeControl_IconChanged(object sender, string newIcon)
        {
            VolumeButtonIcon.Text = newIcon;
        }

        private void VolumeControl_MuteChanged(object sender, bool muted)
        {
            FileManager.itemViewHolder.Muted = muted;
        }
        #endregion

        #region AddSounds
        private async void NewSoundFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            // Show file picker for new sounds
            var files = await PickFilesForAddSoundsContentDialog();
            if (files.Count == 0) return;

            // Show the dialog for adding the sounds
            var template = (DataTemplate)Resources["SoundFileItemTemplate"];

            ContentDialog addSoundsContentDialog = ContentDialogs.CreateAddSoundsContentDialog(template, files);
            addSoundsContentDialog.PrimaryButtonClick += AddSoundsContentDialog_PrimaryButtonClick;
            await ContentDialogs.ShowContentDialogAsync(addSoundsContentDialog);
        }

        private async void AddSoundsContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            await AddSelectedSoundFiles();
        }

        public static async Task<List<StorageFile>> PickFilesForAddSoundsContentDialog()
        {
            // Open file picker
            var picker = new Windows.Storage.Pickers.FileOpenPicker
            {
                ViewMode = Windows.Storage.Pickers.PickerViewMode.List,
                SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.MusicLibrary
            };

            foreach (var fileType in FileManager.allowedFileTypes)
                picker.FileTypeFilter.Add(fileType);

            var files = await picker.PickMultipleFilesAsync();
            return files.ToList();
        }

        public static async Task AddSelectedSoundFiles()
        {
            if (ContentDialogs.AddSoundsSelectedFiles.Count == 0) return;
            var loader = new ResourceLoader();
            FileManager.itemViewHolder.AddingSounds = true;

            // Show InAppNotification
            FileManager.itemViewHolder.TriggerShowInAppNotificationEvent(
                null,
                new ShowInAppNotificationEventArgs(
                    FileManager.InAppNotificationType.AddSounds,
                    string.Format(loader.GetString("InAppNotification-AddSounds"), 0, ContentDialogs.AddSoundsSelectedFiles.Count),
                    0,
                    true
                )
            );

            List<string> notAddedSounds = new List<string>();
            int i = 0;

            // Get the category
            Guid? selectedCategory = null;
            if (!Equals(FileManager.itemViewHolder.SelectedCategory, Guid.Empty))
                selectedCategory = FileManager.itemViewHolder.SelectedCategory;

            foreach (var soundFileItem in ContentDialogs.AddSoundsSelectedFiles)
            {
                // Create the sound and add it to the sound lists
                Guid uuid = await FileManager.CreateSoundAsync(null, soundFileItem.File.DisplayName, selectedCategory, soundFileItem.File);

                if (uuid.Equals(Guid.Empty))
                    notAddedSounds.Add(soundFileItem.File.Name);
                else
                {
                    await FileManager.AddSound(uuid);

                    FileManager.SetInAppNotificationMessage(
                        FileManager.InAppNotificationType.AddSounds,
                        string.Format(loader.GetString("InAppNotification-AddSounds"), i + 1, ContentDialogs.AddSoundsSelectedFiles.Count)
                    );

                    i++;
                }
            }

            FileManager.UpdatePlayAllButtonVisibility();

            if (FileManager.itemViewHolder.AppState == FileManager.AppState.Empty)
                FileManager.itemViewHolder.AppState = FileManager.AppState.Normal;

            if (notAddedSounds.Count > 0)
            {
                string message = notAddedSounds.Count == 1 ?
                    loader.GetString("InAppNotification-AddSoundsErrorOneSound")
                    : string.Format(loader.GetString("InAppNotification-AddSoundsErrorMultipleSounds"), notAddedSounds.Count);

                var inAppNotificationArgs = new ShowInAppNotificationEventArgs(
                    FileManager.InAppNotificationType.AddSounds,
                    message,
                    0,
                    false,
                    true,
                    loader.GetString("Actions-ShowDetails")
                );

                inAppNotificationArgs.PrimaryButtonClick += async (sender, args) =>
                {
                    FileManager.DismissInAppNotification(FileManager.InAppNotificationType.AddSounds);

                    var addSoundsErrorContentDialog = ContentDialogs.CreateAddSoundsErrorContentDialog(notAddedSounds);
                    await ContentDialogs.ShowContentDialogAsync(addSoundsErrorContentDialog);
                };

                FileManager.itemViewHolder.TriggerShowInAppNotificationEvent(null, inAppNotificationArgs);
            }
            else
            {
                string message = ContentDialogs.AddSoundsSelectedFiles.Count == 1 ?
                    loader.GetString("InAppNotification-AddSoundSuccessful")
                    : string.Format(loader.GetString("InAppNotification-AddSoundsSuccessful"), ContentDialogs.AddSoundsSelectedFiles.Count);

                FileManager.itemViewHolder.TriggerShowInAppNotificationEvent(
                    null,
                    new ShowInAppNotificationEventArgs(
                        FileManager.InAppNotificationType.AddSounds,
                        message,
                        8000,
                        false,
                        true
                    )
                );
            }

            FileManager.itemViewHolder.AddingSounds = false;

            Analytics.TrackEvent("AddSounds", new Dictionary<string, string>
            {
                { "AddedSounds", i.ToString() },
                { "NotAddedSounds", notAddedSounds.Count.ToString() }
            });
        }
        #endregion

        #region DownloadSounds
        private async void DownloadSoundsFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            var infoButtonStyle = Application.Current.Resources["InfoButtonStyle"] as Style;
            var downloadSoundsContentDialog = ContentDialogs.CreateDownloadSoundsContentDialog(infoButtonStyle);
            downloadSoundsContentDialog.PrimaryButtonClick += DownloadSoundsContentDialog_PrimaryButtonClick;
            await ContentDialogs.ShowContentDialogAsync(downloadSoundsContentDialog);
        }

        private async void DownloadSoundsContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (ContentDialogs.DownloadSoundsResult == FileManager.DownloadSoundsResultType.Youtube)
            {
                if (ContentDialogs.DownloadSoundsYoutubeInfoDownloadPlaylistCheckbox.IsChecked == true)
                    await DownloadSoundsContentDialog_YoutubePlaylistDownload();
                else
                    await DownloadSoundsContentDialog_YoutubeVideoDownload();
            }
            else if (ContentDialogs.DownloadSoundsResult == FileManager.DownloadSoundsResultType.AudioFile)
                await DownloadSoundsContentDialog_AudioFileDownload();
        }

        public async Task DownloadSoundsContentDialog_YoutubeVideoDownload()
        {
            Guid currentCategoryUuid = FileManager.itemViewHolder.SelectedCategory;

            // Get the url in the text box
            var grabResult = ContentDialogs.DownloadSoundsGrabResult;
            if (grabResult == null) return;

            // Show InAppNotification
            FileManager.itemViewHolder.TriggerShowInAppNotificationEvent(
                this,
                new ShowInAppNotificationEventArgs(
                    FileManager.InAppNotificationType.DownloadSound,
                    loader.GetString("InAppNotification-DownloadSound"),
                    0,
                    true
                )
            );

            // Disable the ability to add or download sounds
            FileManager.itemViewHolder.AddingSounds = true;

            var mediaResources = grabResult.Resources<GrabbedMedia>();
            var imageResources = grabResult.Resources<GrabbedImage>();

            // Find the audio track with the highest sample rate
            GrabbedMedia bestAudio = FindBestAudioOfYoutubeVideo(mediaResources);

            if (bestAudio == null)
            {
                ShowDownloadSoundErrorInAppNotification();
                return;
            }

            StorageFolder cacheFolder = ApplicationData.Current.LocalCacheFolder;
            StorageFile imageFile = await cacheFolder.CreateFileAsync("download.jpg", CreationCollisionOption.GenerateUniqueName);
            bool imageDownloaded = await DownloadBestImageOfYoutubeVideo(imageFile, imageResources);

            if (!imageDownloaded)
            {
                ShowDownloadSoundErrorInAppNotification();
                return;
            }

            // Download the audio track
            StorageFile audioFile = await cacheFolder.CreateFileAsync("download.m4a", CreationCollisionOption.GenerateUniqueName);
            var progress = new Progress<int>((int value) => FileManager.SetInAppNotificationProgress(FileManager.InAppNotificationType.DownloadSound, false, value));
            bool audioFileDownloadResult = false;

            await Task.Run(async () =>
            {
                audioFileDownloadResult = (await FileManager.DownloadBinaryDataToFile(audioFile, bestAudio.ResourceUri, progress)).Key;
            });

            if (!audioFileDownloadResult)
            {
                // Wait a few seconds and try it again
                await Task.Delay(15000);

                await Task.Run(async () =>
                {
                    audioFileDownloadResult = (await FileManager.DownloadBinaryDataToFile(audioFile, bestAudio.ResourceUri, progress)).Key;
                });

                if (!audioFileDownloadResult)
                {
                    ShowDownloadSoundErrorInAppNotification();
                    return;
                }
            }

            // Add the files as a new sound
            Guid uuid = await FileManager.CreateSoundAsync(null, grabResult.Title, new List<Guid> { currentCategoryUuid }, audioFile, imageFile);

            if (uuid.Equals(Guid.Empty))
            {
                ShowDownloadSoundErrorInAppNotification();
                return;
            }

            await FileManager.AddSound(uuid);

            FileManager.itemViewHolder.TriggerShowInAppNotificationEvent(
                this,
                new ShowInAppNotificationEventArgs(
                    FileManager.InAppNotificationType.DownloadSound,
                    loader.GetString("InAppNotification-DownloadSoundSuccessful"),
                    8000,
                    false,
                    true
                )
            );

            FileManager.itemViewHolder.AddingSounds = false;

            Analytics.TrackEvent("YoutubeVideoDownload");
        }

        public async Task DownloadSoundsContentDialog_YoutubePlaylistDownload()
        {
            Guid currentCategoryUuid = FileManager.itemViewHolder.SelectedCategory;

            // Disable the ability to add or download sounds
            FileManager.itemViewHolder.AddingSounds = true;

            var grabber = GrabberBuilder.New().UseDefaultServices().AddYouTube().Build();
            List<KeyValuePair<string, string>> notDownloadedSounds = new List<KeyValuePair<string, string>>();
            var playlistItemResponse = ContentDialogs.DownloadSoundsPlaylistItemListResponse;

            // Show InAppNotification
            FileManager.itemViewHolder.TriggerShowInAppNotificationEvent(
                this,
                new ShowInAppNotificationEventArgs(
                    FileManager.InAppNotificationType.DownloadSounds,
                    string.Format(loader.GetString("InAppNotification-DownloadSounds"), 1, playlistItemResponse.PageInfo.TotalResults.GetValueOrDefault()),
                    0,
                    true
                )
            );

            // Load all items from all pages of the playlist
            List<PlaylistItem> playlistItems = new List<PlaylistItem>();
            
            foreach (var item in playlistItemResponse.Items)
                playlistItems.Add(item);

            while (playlistItemResponse.NextPageToken != null)
            {
                // Get the next page of the playlist
                var listOperation = FileManager.youtubeService.PlaylistItems.List("contentDetails");
                listOperation.PlaylistId = ContentDialogs.DownloadSoundsPlaylistId;
                listOperation.MaxResults = 50;
                listOperation.PageToken = playlistItemResponse.NextPageToken;

                try
                {
                    playlistItemResponse = await listOperation.ExecuteAsync();
                }
                catch (Exception)
                {
                    // Show InAppNotification for error
                    FileManager.itemViewHolder.TriggerShowInAppNotificationEvent(
                        this,
                        new ShowInAppNotificationEventArgs(
                            FileManager.InAppNotificationType.DownloadSounds,
                            loader.GetString("InAppNotification-DownloadSoundsPlaylistError"),
                            0,
                            false,
                            true
                        )
                    );
                    return;
                }

                foreach (var item in playlistItemResponse.Items)
                    playlistItems.Add(item);
            }

            // Create category for playlist, if the option is checked
            Category newCategory = null;

            if (ContentDialogs.DownloadSoundsYoutubeInfoCreateCategoryForPlaylistCheckbox.IsChecked.GetValueOrDefault())
            {
                // Create category
                List<string> iconsList = FileManager.GetIconsList();
                int randomNumber = new Random().Next(iconsList.Count);

                Guid categoryUuid = await FileManager.CreateCategoryAsync(null, null, ContentDialogs.DownloadSoundsPlaylistTitle, iconsList[randomNumber]);
                newCategory = await FileManager.GetCategoryAsync(categoryUuid, false);

                // Add the category to the Categories list
                FileManager.AddCategory(newCategory, Guid.Empty);

                // Add the category to the SideBar
                AddCategoryMenuItem(menuItems, newCategory, Guid.Empty);
            }

            // Go through each video of the playlist
            for (int i = 0; i < playlistItems.Count; i++)
            {
                FileManager.SetInAppNotificationMessage(
                    FileManager.InAppNotificationType.DownloadSounds,
                    string.Format(loader.GetString("InAppNotification-DownloadSounds"), i + 1, playlistItems.Count)
                );

                string videoLink = string.Format("https://youtube.com/watch?v={0}", playlistItems[i].ContentDetails.VideoId);

                GrabResult grabResult = await grabber.GrabAsync(new Uri(videoLink));
                if (grabResult == null)
                {
                    notDownloadedSounds.Add(new KeyValuePair<string, string>(null, videoLink));
                    continue;
                }

                var mediaResources = grabResult.Resources<GrabbedMedia>();
                var imageResources = grabResult.Resources<GrabbedImage>();

                // Find the audio track with the highest sample rate
                GrabbedMedia bestAudio = FindBestAudioOfYoutubeVideo(mediaResources);

                if (bestAudio == null)
                {
                    notDownloadedSounds.Add(new KeyValuePair<string, string>(grabResult.Title, videoLink));
                    continue;
                }

                StorageFolder cacheFolder = ApplicationData.Current.LocalCacheFolder;
                StorageFile imageFile = await cacheFolder.CreateFileAsync("download.jpg", CreationCollisionOption.GenerateUniqueName);
                bool imageDownloaded = await DownloadBestImageOfYoutubeVideo(imageFile, imageResources);

                if (!imageDownloaded)
                {
                    notDownloadedSounds.Add(new KeyValuePair<string, string>(grabResult.Title, videoLink));
                    continue;
                }

                // Download the audio track
                StorageFile audioFile = await cacheFolder.CreateFileAsync("download.m4a", CreationCollisionOption.GenerateUniqueName);
                var progress = new Progress<int>((int value) => FileManager.SetInAppNotificationProgress(FileManager.InAppNotificationType.DownloadSounds, false, value));
                bool audioFileDownloadResult = false;

                await Task.Run(async () =>
                {
                    audioFileDownloadResult = (await FileManager.DownloadBinaryDataToFile(audioFile, bestAudio.ResourceUri, progress)).Key;
                });

                if (!audioFileDownloadResult)
                {
                    // Wait a few seconds and try it again
                    await Task.Delay(15000);

                    await Task.Run(async () =>
                    {
                        audioFileDownloadResult = (await FileManager.DownloadBinaryDataToFile(audioFile, bestAudio.ResourceUri, progress)).Key;
                    });

                    if (!audioFileDownloadResult)
                    {
                        notDownloadedSounds.Add(new KeyValuePair<string, string>(grabResult.Title, videoLink));
                        continue;
                    }
                }

                FileManager.SetInAppNotificationProgress(FileManager.InAppNotificationType.DownloadSounds);

                // Add the files as a new sound
                Guid uuid = await FileManager.CreateSoundAsync(
                    null,
                    grabResult.Title,
                    newCategory != null ? newCategory.Uuid : currentCategoryUuid,
                    audioFile,
                    imageFile
                );

                if (uuid.Equals(Guid.Empty))
                {
                    notDownloadedSounds.Add(new KeyValuePair<string, string>(grabResult.Title, videoLink));
                    continue;
                }

                await FileManager.AddSound(uuid);
            }

            if (notDownloadedSounds.Count > 0)
            {
                string message = notDownloadedSounds.Count == 1 ?
                    loader.GetString("InAppNotification-DownloadSoundsErrorOneSound")
                    : string.Format(loader.GetString("InAppNotification-DownloadSoundsErrorMultipleSounds"), notDownloadedSounds.Count);

                var inAppNotificationArgs = new ShowInAppNotificationEventArgs(
                    FileManager.InAppNotificationType.DownloadSounds,
                    message,
                    0,
                    false,
                    true,
                    loader.GetString("Actions-ShowDetails")
                );

                inAppNotificationArgs.PrimaryButtonClick += async (sender, args) =>
                {
                    FileManager.DismissInAppNotification(FileManager.InAppNotificationType.DownloadSounds);

                    var downloadSoundsErrorContentDialog = ContentDialogs.CreateDownloadSoundsErrorContentDialog(notDownloadedSounds);
                    await ContentDialogs.ShowContentDialogAsync(downloadSoundsErrorContentDialog);
                };

                FileManager.itemViewHolder.TriggerShowInAppNotificationEvent(this, inAppNotificationArgs);
            }
            else
            {
                FileManager.itemViewHolder.TriggerShowInAppNotificationEvent(
                    this,
                    new ShowInAppNotificationEventArgs(
                        FileManager.InAppNotificationType.DownloadSounds,
                        string.Format(loader.GetString("InAppNotification-DownloadSoundsSuccessful"), playlistItems.Count),
                        8000,
                        false,
                        true
                    )
                );
            }

            FileManager.itemViewHolder.AddingSounds = false;

            Analytics.TrackEvent("YoutubePlaylistDownload");
        }

        private GrabbedMedia FindBestAudioOfYoutubeVideo(IEnumerable<GrabbedMedia> mediaResources)
        {
            GrabbedMedia bestAudio = null;

            foreach (var media in mediaResources)
            {
                if (media.Format.Extension != "m4a") continue;

                int? bitrate = media.GetBitRate();
                if (!bitrate.HasValue) continue;

                if (bestAudio == null || bestAudio.GetBitRate().Value < bitrate.Value)
                    bestAudio = media;
            }

            return bestAudio;
        }

        private async Task<bool> DownloadBestImageOfYoutubeVideo(StorageFile targetFile, IEnumerable<GrabbedImage> imageResources)
        {
            // Find the thumbnail image with the highest resolution
            List<GrabbedImage> images = new List<GrabbedImage>();
            GrabbedImage maxresDefaultImage = null;
            GrabbedImage mqDefaultImage = null;
            GrabbedImage sdDefaultImage = null;
            GrabbedImage hqDefaultImage = null;
            GrabbedImage defaultImage = null;

            foreach (var media in imageResources)
            {
                string mediaFileName = media.ResourceUri.ToString().Split('/').Last();

                switch (mediaFileName)
                {
                    case "maxresdefault.jpg":
                        maxresDefaultImage = media;
                        break;
                    case "mqdefault.jpg":
                        mqDefaultImage = media;
                        break;
                    case "sddefault.jpg":
                        sdDefaultImage = media;
                        break;
                    case "hqdefault.jpg":
                        hqDefaultImage = media;
                        break;
                    case "default.jpg":
                        defaultImage = media;
                        break;
                }
            }

            if (maxresDefaultImage != null)
                images.Add(maxresDefaultImage);
            if (mqDefaultImage != null)
                images.Add(mqDefaultImage);
            if (sdDefaultImage != null)
                images.Add(sdDefaultImage);
            if (hqDefaultImage != null)
                images.Add(hqDefaultImage);
            if (defaultImage != null)
                images.Add(defaultImage);

            // Download the thumbnail image
            GrabbedImage currentImage;

            while (images.Count() > 0)
            {
                currentImage = images[0];
                images.RemoveAt(0);

                var imageFileDownloadResult = await FileManager.DownloadBinaryDataToFile(targetFile, currentImage.ResourceUri, null);

                if (!imageFileDownloadResult.Key)
                {
                    if (imageFileDownloadResult.Value == 404)
                    {
                        // Try to download the next image in the list
                        continue;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return true;
                }
            }

            return false;
        }

        public async Task DownloadSoundsContentDialog_AudioFileDownload()
        {
            Guid currentCategoryUuid = FileManager.itemViewHolder.SelectedCategory;
            FileManager.itemViewHolder.AddingSounds = true;

            // Show InAppNotification
            FileManager.itemViewHolder.TriggerShowInAppNotificationEvent(
                this,
                new ShowInAppNotificationEventArgs(
                    FileManager.InAppNotificationType.DownloadSound,
                    loader.GetString("InAppNotification-DownloadSound"),
                    0,
                    true
                )
            );

            StorageFolder cacheFolder = ApplicationData.Current.LocalCacheFolder;
            StorageFile audioFile = await cacheFolder.CreateFileAsync(string.Format("download.{0}", ContentDialogs.DownloadSoundsAudioFileType), CreationCollisionOption.GenerateUniqueName);

            string soundUrl = ContentDialogs.DownloadSoundsUrlTextBox.Text;
            bool result = false;
            var progress = new Progress<int>((int value) => FileManager.SetInAppNotificationProgress(FileManager.InAppNotificationType.DownloadSound, false, value));

            await Task.Run(async () =>
            {
                result = (await FileManager.DownloadBinaryDataToFile(audioFile, new Uri(soundUrl), progress)).Key;
            });

            if (!result)
            {
                ShowDownloadSoundErrorInAppNotification();
                return;
            }

            FileManager.SetInAppNotificationProgress(FileManager.InAppNotificationType.DownloadSound);

            Guid uuid = await FileManager.CreateSoundAsync(
                null,
                ContentDialogs.DownloadSoundsAudioFileName,
                new List<Guid> { currentCategoryUuid },
                audioFile
            );

            if (uuid.Equals(Guid.Empty))
            {
                ShowDownloadSoundErrorInAppNotification();
                return;
            }

            await FileManager.AddSound(uuid);

            FileManager.itemViewHolder.TriggerShowInAppNotificationEvent(
                this,
                new ShowInAppNotificationEventArgs(
                    FileManager.InAppNotificationType.DownloadSound,
                    loader.GetString("InAppNotification-DownloadSoundSuccessful"),
                    8000,
                    false,
                    true
                )
            );

            FileManager.itemViewHolder.AddingSounds = false;

            Analytics.TrackEvent("AudioFileDownload", new Dictionary<string, string>
            {
                { "File extension", ContentDialogs.DownloadSoundsAudioFileType }
            });
        }

        public void ShowDownloadSoundErrorInAppNotification()
        {
            FileManager.itemViewHolder.AddingSounds = false;

            var inAppNotificationEventArgs = new ShowInAppNotificationEventArgs(
                FileManager.InAppNotificationType.DownloadSound,
                loader.GetString("InAppNotification-DownloadSoundError"),
                0,
                false,
                true,
                loader.GetString("Actions-ShowDetails")
            );
            inAppNotificationEventArgs.PrimaryButtonClick += async (sender, args) => await ShowDownloadErrorDialog();

            FileManager.itemViewHolder.TriggerShowInAppNotificationEvent(this, inAppNotificationEventArgs);
        }

        public async Task ShowDownloadErrorDialog()
        {
            ContentDialog downloadFileErrorContentDialog = ContentDialogs.CreateDownloadFileErrorContentDialog();
            await ContentDialogs.ShowContentDialogAsync(downloadFileErrorContentDialog);
        }
        #endregion

        #region New Category
        private async void NewCategoryFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            var newCategoryContentDialog = ContentDialogs.CreateNewCategoryContentDialog(Guid.Empty);
            newCategoryContentDialog.PrimaryButtonClick += NewCategoryContentDialog_PrimaryButtonClick;
            await ContentDialogs.ShowContentDialogAsync(newCategoryContentDialog);
        }

        private async void NewCategoryContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // Get combobox value
            ComboBoxItem typeItem = (ComboBoxItem)ContentDialogs.IconSelectionComboBox.SelectedItem;
            string icon = typeItem.Content.ToString();

            Guid categoryUuid = await FileManager.CreateCategoryAsync(null, null, ContentDialogs.NewCategoryTextBox.Text, icon);
            Category newCategory = await FileManager.GetCategoryAsync(categoryUuid, false);

            // Add the category to the Categories list
            FileManager.AddCategory(newCategory, Guid.Empty);

            // Add the category to the SideBar
            AddCategoryMenuItem(menuItems, newCategory, Guid.Empty);

            // Navigate to the new category
            await FileManager.ShowCategoryAsync(categoryUuid);

            // Select the new category in the SideBar
            await Task.Delay(2);
            SelectCategory(categoryUuid);
        }
        #endregion

        #region Search
        private async void SearchAutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.ProgrammaticChange) return;

            SelectCategory(Guid.Empty);
            string text = sender.Text;

            if (string.IsNullOrEmpty(text))
            {
                // Show all sounds
                await FileManager.ShowAllSoundsAsync();
                SearchAutoSuggestBox.Focus(FocusState.Programmatic);
            }
            else
            {
                // Show the search result
                FileManager.itemViewHolder.Title = text;
                FileManager.itemViewHolder.SearchQuery = text;
                FileManager.itemViewHolder.SelectedCategory = Guid.Empty;
                FileManager.itemViewHolder.BackButtonEnabled = true;
                FileManager.itemViewHolder.EditButtonVisible = false;

                // Update the suggestions
                Suggestions = FileManager.itemViewHolder.AllSounds.Where(s => s.Name.ToLower().Contains(text.ToLower())).Select(s => s.Name).ToList();
                Bindings.Update();

                // Load the searched sounds
                await FileManager.LoadSoundsByNameAsync(text);

                FileManager.UpdatePlayAllButtonVisibility();
                FileManager.UpdateBackButtonVisibility();
            }
        }

        private async void SearchAutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.ChosenSuggestion != null) return;

            string text = sender.Text;
            SelectCategory(Guid.Empty);

            if (string.IsNullOrEmpty(text))
            {
                FileManager.itemViewHolder.Title = new ResourceLoader().GetString("AllSounds");
            }
            else
            {
                FileManager.itemViewHolder.Title = text;
                FileManager.itemViewHolder.SearchQuery = text;
                FileManager.itemViewHolder.EditButtonVisible = false;
                await FileManager.LoadSoundsByNameAsync(text);
            }

            FileManager.UpdatePlayAllButtonVisibility();
            FileManager.UpdateBackButtonVisibility();
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            FileManager.itemViewHolder.BackButtonEnabled = true;
            FileManager.itemViewHolder.SearchButtonVisible = false;
            FileManager.itemViewHolder.SearchAutoSuggestBoxVisible = true;

            SearchAutoSuggestBox.Focus(FocusState.Programmatic);
        }
        #endregion

        #region CancelButton
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            FileManager.itemViewHolder.MultiSelectionEnabled = false;
            AdjustLayout();
        }
        #endregion

        #region SelectButton
        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            FileManager.itemViewHolder.MultiSelectionEnabled = true;
            AdjustLayout();
        }
        #endregion

        #region Select all
        private void MoreButton_SelectAllFlyout_Click(object sender, RoutedEventArgs e)
        {
            FileManager.itemViewHolder.TriggerSelectAllSoundsEvent(sender, e);
        }
        #endregion

        #region Share
        private async void MoreButton_ShareFlyout_Click(object sender, RoutedEventArgs e)
        {
            if (!await DownloadSelectedFiles()) return;

            sharedFiles.Clear();
            StorageFolder tempFolder = ApplicationData.Current.TemporaryFolder;

            // Copy the files into the temp folder
            foreach (Sound sound in FileManager.itemViewHolder.SelectedSounds)
            {
                StorageFile audioFile = sound.AudioFile;
                if (audioFile == null) return;

                string ext = sound.GetAudioFileExtension();
                if (string.IsNullOrEmpty(ext)) ext = "mp3";

                StorageFile tempFile = await audioFile.CopyAsync(tempFolder, sound.Name + "." + ext, NameCollisionOption.ReplaceExisting);
                sharedFiles.Add(tempFile);
            }

            DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested += DataTransferManager_DataRequested;
            dataTransferManager.TargetApplicationChosen += DataTransferManager_TargetApplicationChosen;
            DataTransferManager.ShowShareUI();
        }

        private void DataTransferManager_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            if (sharedFiles.Count == 0) return;

            string description = loader.GetString("ShareDialog-MultipleSounds");
            if (sharedFiles.Count == 1)
                description = sharedFiles.First().Name;

            DataRequest request = args.Request;
            request.Data.SetStorageItems(sharedFiles);
            request.Data.Properties.Title = loader.GetString("ShareDialog-Title");
            request.Data.Properties.Description = description;
        }

        private void DataTransferManager_TargetApplicationChosen(DataTransferManager sender, TargetApplicationChosenEventArgs args)
        {
            // Disable multi-selection mode
            FileManager.itemViewHolder.MultiSelectionEnabled = false;
        }
        #endregion

        #region File download
        private async Task<bool> DownloadSelectedFiles()
        {
            var selectedSounds = FileManager.itemViewHolder.SelectedSounds;
            bool showDownloadFilesDialog = false;

            if (Dav.IsLoggedIn)
            {
                // Check if any of the sounds needs to be downloaded
                foreach(var sound in selectedSounds)
                {
                    var downloadStatus = sound.GetAudioFileDownloadStatus();

                    if (
                        downloadStatus == TableObjectFileDownloadStatus.Downloading
                        || downloadStatus == TableObjectFileDownloadStatus.NotDownloaded
                    )
                    {
                        showDownloadFilesDialog = true;
                        break;
                    }
                }
            }

            if (showDownloadFilesDialog)
            {
                var itemTemplate = (DataTemplate)Resources["SoundFileDownloadProgressTemplate"];
                var itemStyle = Resources["ListViewItemStyle"] as Style;

                var downloadFilesContentDialog = ContentDialogs.CreateDownloadFilesContentDialog(selectedSounds.ToList(), itemTemplate, itemStyle);
                downloadFilesContentDialog.CloseButtonClick += DownloadFilesContentDialog_CloseButtonClick;

                downloadFilesDialogIsVisible = true;
                await ContentDialogs.ShowContentDialogAsync(downloadFilesContentDialog);
                downloadFilesDialogIsVisible = false;

                if (downloadFilesCanceled)
                {
                    downloadFilesCanceled = false;
                    return false;
                }
                else if (downloadFilesFailed)
                {
                    downloadFilesFailed = false;

                    // Show the error dialog
                    var errorContentDialog = ContentDialogs.CreateDownloadFileErrorContentDialog();
                    await ContentDialogs.ShowContentDialogAsync(errorContentDialog);

                    return false;
                }
            }

            return true;
        }

        private void DownloadFilesContentDialog_CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            downloadFilesCanceled = true;
        }
        #endregion

        #region Set categories
        private async void MoreButton_SetCategories_Click(object sender, RoutedEventArgs e)
        {
            // Show the Set Categories content dialog for multiple sounds
            List<Sound> selectedSounds = new List<Sound>();
            foreach (var sound in FileManager.itemViewHolder.SelectedSounds)
                selectedSounds.Add(sound);

            var SetCategoryContentDialog = ContentDialogs.CreateSetCategoriesContentDialog(selectedSounds);
            SetCategoryContentDialog.PrimaryButtonClick += SetCategoriesContentDialog_PrimaryButtonClick;
            await ContentDialogs.ShowContentDialogAsync(SetCategoryContentDialog);
        }

        private async void SetCategoriesContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            FileManager.itemViewHolder.LoadingScreenMessage = loader.GetString("UpdateSoundsMessage");
            FileManager.itemViewHolder.LoadingScreenVisible = true;

            // Get the selected categories
            List<Guid> categoryUuids = new List<Guid>();
            foreach (var item in ContentDialogs.CategoriesTreeView.SelectedItems)
                categoryUuids.Add((Guid)((CustomTreeViewNode)item).Tag);

            // Get the selected sounds
            List<Sound> selectedSounds = new List<Sound>();
            foreach (var sound in FileManager.itemViewHolder.SelectedSounds)
                selectedSounds.Add(sound);

            // Update and reload the sounds
            foreach (var sound in selectedSounds)
            {
                await FileManager.SetCategoriesOfSoundAsync(sound.Uuid, categoryUuids);
                await FileManager.ReloadSound(sound.Uuid);
            }

            FileManager.itemViewHolder.LoadingScreenVisible = false;

            // Disable multi-selection mode
            FileManager.itemViewHolder.MultiSelectionEnabled = false;
        }
        #endregion

        #region CategoryOptionsFlyout
        private void ShowCategoryOptionsFlyout(UIElement sender, Point position)
        {
            // Get the position of the selected category
            List<int> positionPath = GetCategoryMenuItemPositionPath(selectedCategory);
            int itemPosition = positionPath.ElementAt(positionPath.Count - 1);
            positionPath.RemoveAt(positionPath.Count - 1);

            int selectedItemChildCount = GetCategoryMenuItemChildrenCountByPath(positionPath);
            bool isFirstItem = itemPosition == 0;
            bool isLastItem = itemPosition == selectedItemChildCount - 1;
            bool isSubCategory = positionPath.Count > 0;

            // Create and show the MenuFlyout
            MenuFlyout flyout = new MenuFlyout();

            // Create subcategory
            MenuFlyoutItem createSubCategoryFlyoutItem = new MenuFlyoutItem { Text = loader.GetString("CategoryOptionsFlyout-AddSubCategory") };
            createSubCategoryFlyoutItem.Click += CreateSubCategoryFlyoutItem_Click;
            flyout.Items.Add(createSubCategoryFlyoutItem);

            // Position
            MenuFlyoutSubItem positionSubFlyoutItem = new MenuFlyoutSubItem { Text = loader.GetString("CategoryOptionsFlyout-Position") };

            FontIcon arrowTop = new FontIcon { Glyph = "\uE74A" };
            FontIcon arrowBottom = new FontIcon { Glyph = "\uE74B" };
            FontIcon arrowTopLeft = new FontIcon { Glyph = "\uE742" };
            FontIcon arrowTopRight = new FontIcon { Glyph = "\uE742", FlowDirection = FlowDirection.RightToLeft, MirroredWhenRightToLeft = true };
            FontIcon arrowBottomLeft = new FontIcon { Glyph = "\uE741", FlowDirection = FlowDirection.RightToLeft, MirroredWhenRightToLeft = true };
            FontIcon arrowBottomRight = new FontIcon { Glyph = "\uE741" };

            // Move up
            MenuFlyoutItem moveUpFlyoutItem = new MenuFlyoutItem { Text = loader.GetString("CategoryOptionsFlyout-Position-MoveUp") };
            moveUpFlyoutItem.Click += MoveUpFlyoutItem_Click;

            // Move down
            MenuFlyoutItem moveDownFlyoutItem = new MenuFlyoutItem { Text = loader.GetString("CategoryOptionsFlyout-Position-MoveDown") };
            moveDownFlyoutItem.Click += MoveDownFlyoutItem_Click;

            // Move to category above
            MenuFlyoutItem moveToCategoryAboveItem = new MenuFlyoutItem { Text = loader.GetString("CategoryOptionsFlyout-Position-MoveToCategoryAbove") };
            moveToCategoryAboveItem.Click += MoveToCategoryAboveItem_Click;

            // Move to category below
            MenuFlyoutItem moveToCategoryBelowItem = new MenuFlyoutItem { Text = loader.GetString("CategoryOptionsFlyout-Position-MoveToCategoryBelow") };
            moveToCategoryBelowItem.Click += MoveToCategoryBelowItem_Click;

            // Move to parent category
            MenuFlyoutItem moveToParentCategoryItem = new MenuFlyoutItem { Text = loader.GetString("CategoryOptionsFlyout-Position-MoveToParentCategory") };

            if (!isFirstItem)
            {
                // Move up
                moveUpFlyoutItem.Icon = arrowTop;
                positionSubFlyoutItem.Items.Add(moveUpFlyoutItem);
            }

            if(!isLastItem)
            {
                // Move down
                moveDownFlyoutItem.Icon = arrowBottom;
                positionSubFlyoutItem.Items.Add(moveDownFlyoutItem);
            }

            if(isFirstItem && !isLastItem && isSubCategory)
            {
                // Move to parent category
                moveToParentCategoryItem.Icon = arrowTopLeft;
                moveToParentCategoryItem.Click += MoveToParentCategoryAboveItem_Click;
                positionSubFlyoutItem.Items.Add(moveToParentCategoryItem);
            }

            if (!isFirstItem)
            {
                // Move to category above
                moveToCategoryAboveItem.Icon = arrowTopRight;
                positionSubFlyoutItem.Items.Add(moveToCategoryAboveItem);
            }

            if (!isLastItem)
            {
                // Move to category below
                moveToCategoryBelowItem.Icon = arrowBottomRight;
                positionSubFlyoutItem.Items.Add(moveToCategoryBelowItem);
            }

            if(isLastItem && isSubCategory)
            {
                // Move to parent category
                moveToParentCategoryItem.Icon = arrowBottomLeft;
                moveToParentCategoryItem.Click += MoveToParentCategoryBelowItem_Click;
                positionSubFlyoutItem.Items.Add(moveToParentCategoryItem);
            }

            if(positionSubFlyoutItem.Items.Count > 0)
                flyout.Items.Add(positionSubFlyoutItem);

            flyout.ShowAt(sender, position);
        }

        #region Create SubCategory
        private async void CreateSubCategoryFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            var newSubCategoryContentDialog = ContentDialogs.CreateNewCategoryContentDialog(selectedCategory);
            newSubCategoryContentDialog.PrimaryButtonClick += NewSubCategoryContentDialog_PrimaryButtonClick;
            await ContentDialogs.ShowContentDialogAsync(newSubCategoryContentDialog);
        }

        private async void NewSubCategoryContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // Get combobox value
            ComboBoxItem typeItem = (ComboBoxItem)ContentDialogs.IconSelectionComboBox.SelectedItem;
            string icon = typeItem.Content.ToString();

            Guid categoryUuid = await FileManager.CreateCategoryAsync(null, selectedCategory, ContentDialogs.NewCategoryTextBox.Text, icon);
            Category newCategory = await FileManager.GetCategoryAsync(categoryUuid, false);

            // Add the category to the categories list
            FileManager.AddCategory(newCategory, selectedCategory);

            // Add the category to the MenuItems of the SideBar
            AddCategoryMenuItem(menuItems, newCategory, selectedCategory);

            // Navigate to the new category
            await FileManager.ShowCategoryAsync(selectedCategory);

            // Select the new category in the SideBar
            await Task.Delay(2);
            SelectCategory(categoryUuid);
        }
        #endregion

        #region Position
        private async void MoveUpFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            MoveCategoryMenuItem(menuItems, selectedCategory, true);
            await FileManager.MoveCategoryAndSaveOrderAsync(FileManager.itemViewHolder.Categories, selectedCategory, Guid.Empty, true);
            await SelectCurrentCategory();
        }

        private async void MoveDownFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            MoveCategoryMenuItem(menuItems, selectedCategory, false);
            await FileManager.MoveCategoryAndSaveOrderAsync(FileManager.itemViewHolder.Categories, selectedCategory, Guid.Empty, false);
            await SelectCurrentCategory();
        }

        private async void MoveToCategoryAboveItem_Click(object sender, RoutedEventArgs e)
        {
            MoveCategoryMenuItemToMenuItem(menuItems, selectedCategory, true);
            await FileManager.MoveCategoryToCategoryAndSaveOrderAsync(FileManager.itemViewHolder.Categories, selectedCategory, true);
            await SelectCurrentCategory();

            if (!FileManager.itemViewHolder.SelectedCategory.Equals(Guid.Empty))
                await FileManager.LoadSoundsOfCategoryAsync(FileManager.itemViewHolder.SelectedCategory);
        }

        private async void MoveToCategoryBelowItem_Click(object sender, RoutedEventArgs e)
        {
            MoveCategoryMenuItemToMenuItem(menuItems, selectedCategory, false);
            await FileManager.MoveCategoryToCategoryAndSaveOrderAsync(FileManager.itemViewHolder.Categories, selectedCategory, false);
            await SelectCurrentCategory();

            if (!FileManager.itemViewHolder.SelectedCategory.Equals(Guid.Empty))
                await FileManager.LoadSoundsOfCategoryAsync(FileManager.itemViewHolder.SelectedCategory);
        }

        private async void MoveToParentCategoryAboveItem_Click(object sender, RoutedEventArgs e)
        {
            MoveCategoryMenuItemToParent(menuItems, selectedCategory, true);
            await FileManager.MoveCategoryToParentAndSaveOrderAsync(FileManager.itemViewHolder.Categories, Guid.Empty, selectedCategory, true);
            await SelectCurrentCategory();

            if (!FileManager.itemViewHolder.SelectedCategory.Equals(Guid.Empty))
                await FileManager.LoadSoundsOfCategoryAsync(FileManager.itemViewHolder.SelectedCategory);
        }

        private async void MoveToParentCategoryBelowItem_Click(object sender, RoutedEventArgs e)
        {
            MoveCategoryMenuItemToParent(menuItems, selectedCategory, false);
            await FileManager.MoveCategoryToParentAndSaveOrderAsync(FileManager.itemViewHolder.Categories, Guid.Empty, selectedCategory, false);
            await SelectCurrentCategory();

            if (!FileManager.itemViewHolder.SelectedCategory.Equals(Guid.Empty))
                await FileManager.LoadSoundsOfCategoryAsync(FileManager.itemViewHolder.SelectedCategory);
        }
        #endregion
        #endregion

        #region Export
        private async void MoreButton_ExportFlyout_Click(object sender, RoutedEventArgs e)
        {
            if (!await DownloadSelectedFiles()) return;

            List<Sound> sounds = new List<Sound>();
            foreach (var sound in FileManager.itemViewHolder.SelectedSounds)
                sounds.Add(sound);

            var template = (DataTemplate)Resources["SoundItemTemplate"];
            var listViewItemStyle = Resources["ListViewItemStyle"] as Style;

            var exportSoundsContentDialog = ContentDialogs.CreateExportSoundsContentDialog(sounds, template, listViewItemStyle);
            exportSoundsContentDialog.PrimaryButtonClick += ExportSoundsContentDialog_PrimaryButtonClick;
            await ContentDialogs.ShowContentDialogAsync(exportSoundsContentDialog);
        }

        private async void ExportSoundsContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // Disable multi-selection mode
            FileManager.itemViewHolder.MultiSelectionEnabled = false;

            await FileManager.ExportSoundsAsync(ContentDialogs.SoundsList.ToList(), ContentDialogs.ExportSoundsAsZipCheckBox.IsChecked.Value, ContentDialogs.ExportSoundsFolder);
        }
        #endregion

        #region Delete Sounds
        private async void DeleteSoundsButton_Click(object sender, RoutedEventArgs e)
        {
            var deleteSoundsContentDialog = ContentDialogs.CreateDeleteSoundsContentDialogAsync();
            deleteSoundsContentDialog.PrimaryButtonClick += DeleteSoundsContentDialog_PrimaryButtonClick;
            await ContentDialogs.ShowContentDialogAsync(deleteSoundsContentDialog);
        }

        private async void DeleteSoundsContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            FileManager.itemViewHolder.LoadingScreenMessage = loader.GetString("DeleteSoundsMessage");
            FileManager.itemViewHolder.LoadingScreenVisible = true;

            // Delete Sounds
            List<Guid> soundUuids = new List<Guid>();
            for (int i = 0; i < FileManager.itemViewHolder.SelectedSounds.Count; i++)
                soundUuids.Add(FileManager.itemViewHolder.SelectedSounds.ElementAt(i).Uuid);

            if (soundUuids.Count != 0)
            {
                await FileManager.DeleteSoundsAsync(soundUuids);

                // Remove deleted sounds from the lists
                foreach (var soundUuid in soundUuids)
                    FileManager.RemoveSound(soundUuid);
            }

            // Clear selected sounds list
            FileManager.itemViewHolder.SelectedSounds.Clear();
            FileManager.itemViewHolder.LoadingScreenVisible = false;

            // Disable multi-selection mode
            FileManager.itemViewHolder.MultiSelectionEnabled = false;
        }
        #endregion
    }
}