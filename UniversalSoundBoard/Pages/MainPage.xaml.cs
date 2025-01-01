using davClassLibrary;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using UniversalSoundboard.Common;
using UniversalSoundboard.Components;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Dialogs;
using UniversalSoundboard.Models;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml;
using Windows.UI;
using WinUI = Microsoft.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.Services.Store;
using Sentry;
using System.Text;

namespace UniversalSoundboard.Pages
{
    public sealed partial class MainPage : Page
    {
        public static CoreDispatcher dispatcher;                            // Dispatcher for ShareTargetPage
        string appTitle = "UniversalSoundboard";                            // The app name displayed in the title bar
        private static Frame contentFrame;
        public static AppWindow soundRecorderAppWindow;
        public static AppWindow effectManagerAppWindow;
        public static Frame soundRecorderAppWindowContentFrame;
        public static Frame effectManagerAppWindowContentFrame;
        private readonly ObservableCollection<object> menuItems = new ObservableCollection<object>();
        private PlaySoundsSuccessivelyDialog playSoundsSuccessivelyDialog;
        private UpgradePlusDialog upgradePlusDialog = null;
        private Guid initialCategory = Guid.Empty;                          // The category that was selected before the sounds started loading
        private Guid selectedCategory = Guid.Empty;                         // The category that was right clicked for the flyout
        private List<string> Suggestions = new List<string>();              // The suggestions for the SearchAutoSuggestBox
        private List<StorageFile> sharedFiles = new List<StorageFile>();    // The files that get shared
        private MenuFlyout OutputDeviceButtonFlyout = null;
        bool selectionButtonsEnabled = false;                               // If true, the buttons for multi selection are enabled
        bool downloadFilesCanceled = false;
        bool mobileSearchVisible = false;                                   // If true, the app window is small, the search box is visible and the other top buttons are hidden
        bool playingSoundsLoaded = false;
        public static Style accentButtonStyle;
        public static Style infoButtonStyle;
        public static Style listViewItemStyle;
        public static DataTemplate soundFileDownloadProgressTemplate;
        public static double windowWidth = 500;
        public static uint screenWidth = 0;
        public static uint screenHeight = 0;
        public static bool pageLoaded = false;
        public static string SharedSoundUuid = null;

        public MainPage()
        {
            InitializeComponent();
            SetThemeColors();
            InitSentry();

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
            FileManager.itemViewHolder.TableObjectFileDownloadCompleted += ItemViewHolder_TableObjectFileDownloadCompleted;
            FileManager.itemViewHolder.SelectedSounds.CollectionChanged += SelectedSounds_CollectionChanged;
            FileManager.itemViewHolder.SoundDownload += ItemViewHolder_SoundDownload;
            FileManager.deviceWatcherHelper.DevicesChanged += DeviceWatcherHelper_DevicesChanged;
        }

        #region Page event handlers
        async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            contentFrame = ContentFrame;
            NavigateToPage(FileManager.itemViewHolder.Page);
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
            pageLoaded = true;

            if (SharedSoundUuid != null)
            {
                FileManager.NavigateToStoreSoundPage(SharedSoundUuid, "Share");
                SharedSoundUuid = null;
            }

            IncreaseAppStartCounter();
            UpdateOutputDeviceFlyout();
            await ContinuePlaylistDownload();
            await FileManager.StartHotkeyProcess();
            await LoadPlusInfo();
            AppStartTracking();
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
            if (e.PropertyName == ItemViewHolder.PageKey)
                NavigateToPage(FileManager.itemViewHolder.Page);

            switch (e.PropertyName)
            {
                case ItemViewHolder.PageKey:
                case ItemViewHolder.AppStateKey:
                    // Set the visibility of the NavigationViewHeader
                    var visibility = Visibility.Collapsed;

                    if (
                        FileManager.itemViewHolder.AppState == AppState.InitialSync
                        || FileManager.itemViewHolder.AppState == AppState.Empty
                    ) visibility = FileManager.itemViewHolder.Page == typeof(SoundPage) ? Visibility.Collapsed : Visibility.Visible;
                    else if (FileManager.itemViewHolder.AppState == AppState.Normal)
                        visibility = Visibility.Visible;
                    else if(FileManager.itemViewHolder.AppState == AppState.Loading)
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

            if (upgradePlusDialog != null && FileManager.IsUserOnPlus())
                upgradePlusDialog.Hide();
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

        private async void ItemViewHolder_TableObjectFileDownloadCompleted(object sender, TableObjectFileDownloadCompletedEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (Dialog.CurrentlyVisibleDialog != null && Dialog.CurrentlyVisibleDialog is DownloadFilesDialog)
                {
                    var dialog = Dialog.CurrentlyVisibleDialog as DownloadFilesDialog;

                    // Remove all downloaded sounds from the dialog
                    List<Sound> downloadedSounds = new List<Sound>();

                    foreach (var sound in dialog.Sounds)
                        if (sound.GetAudioFileDownloadStatus() == TableObjectFileDownloadStatus.Downloaded)
                            downloadedSounds.Add(sound);

                    foreach (var sound in downloadedSounds)
                        dialog.Sounds.Remove(sound);

                    if (dialog.Sounds.Count == 0)
                        dialog.Hide();
                }
            });
        }

        private void SelectedSounds_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            selectionButtonsEnabled = FileManager.itemViewHolder.SelectedSounds.Count > 0;
            Bindings.Update();
        }

        private async void ItemViewHolder_SoundDownload(object sender, EventArgs e)
        {
            await StartSoundDownload(sender as SoundDownloadDialog);
        }

        private async void DeviceWatcherHelper_DevicesChanged(object sender, EventArgs e)
        {
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                UpdateOutputDeviceFlyout();
            });
        }

        private async void OutputDeviceButton_Click(object sender, RoutedEventArgs e)
        {
            if (FileManager.itemViewHolder.MultipleOutputDevices)
            {
                OutputDevicesDialog outputDevicesDialog = new OutputDevicesDialog();
                outputDevicesDialog.CloseButtonClick += OutputDevicesDialog_CloseButtonClick;
                await outputDevicesDialog.ShowAsync();
            }
            else
            {
                OutputDeviceButtonFlyout.ShowAt(OutputDeviceButton);
            }
        }

        private async void OutputDeviceItem_Click(object sender, RoutedEventArgs e)
        {
            // Check if the user is on dav Plus or has purchased USB Plus
            bool usingPlus = Dav.IsLoggedIn && Dav.User.Plan > 0;
            bool purchasedPlus = FileManager.itemViewHolder.PlusPurchased;

            SentrySdk.CaptureMessage("OutputDeviceButton-ItemClick", scope =>
            {
                scope.SetTags(new Dictionary<string, string>
                {
                    { "usingPlus", usingPlus.ToString() },
                    { "purchasedPlus", purchasedPlus.ToString() }
                });
            });

            if (!usingPlus && !purchasedPlus)
            {
                upgradePlusDialog = new UpgradePlusDialog();
                upgradePlusDialog.UpgradePlusSucceeded += UpgradePlusDialog_UpgradePlusSucceeded;
                await upgradePlusDialog.ShowAsync();

                if (!FileManager.IsUserOnPlus())
                {
                    UpdateOutputDeviceFlyout();
                    return;
                }
            }

            string outputDevice = (string)(sender as ToggleMenuFlyoutItem).Tag;

            if (outputDevice == null)
            {
                FileManager.itemViewHolder.UseStandardOutputDevice = true;
            }
            else
            {
                FileManager.itemViewHolder.OutputDevice = outputDevice;
                FileManager.itemViewHolder.UseStandardOutputDevice = false;
            }

            await Task.Delay(100);
            UpdateOutputDeviceFlyout();
        }

        private async void ManageMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            // Check if the user is on dav Plus or has purchased USB Plus
            bool usingPlus = Dav.IsLoggedIn && Dav.User.Plan > 0;
            bool purchasedPlus = FileManager.itemViewHolder.PlusPurchased;

            SentrySdk.CaptureMessage("OutputDeviceButton-ManageOutputDevices-ItemClick", scope =>
            {
                scope.SetTags(new Dictionary<string, string>
                {
                    { "usingPlus", usingPlus.ToString() },
                    { "purchasedPlus", purchasedPlus.ToString() }
                });
            });

            if (!usingPlus && !purchasedPlus)
            {
                upgradePlusDialog = new UpgradePlusDialog();
                upgradePlusDialog.UpgradePlusSucceeded += UpgradePlusDialog_UpgradePlusSucceeded;
                await upgradePlusDialog.ShowAsync();

                if (!FileManager.IsUserOnPlus())
                    return;
            }

            OutputDevicesDialog outputDevicesDialog = new OutputDevicesDialog();
            outputDevicesDialog.CloseButtonClick += OutputDevicesDialog_CloseButtonClick;
            await outputDevicesDialog.ShowAsync();
        }

        private void UpgradePlusDialog_UpgradePlusSucceeded(object sender, EventArgs e)
        {
            FileManager.itemViewHolder.PlusPurchased = true;
        }

        private void OutputDevicesDialog_CloseButtonClick(Dialog sender, ContentDialogButtonClickEventArgs args)
        {
            UpdateOutputDeviceFlyout();
        }

        private async void WriteReviewInAppNotificationEventArgs_PrimaryButtonClick(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri("ms-windows-store://review/?ProductId=9NBLGGH51005"));
            FileManager.DismissInAppNotification(InAppNotificationType.WriteReview);

            SentrySdk.CaptureMessage("InAppNotification-WriteReview-PrimaryButtonClick", scope =>
            {
                scope.SetTag("AppStarts", FileManager.itemViewHolder.AppStartCounter.ToString());
            });
        }
        #endregion

        #region General methods
        private void InitSentry()
        {
            // Get the screen resolution
            var displayInfo = DisplayInformation.GetForCurrentView();
            screenWidth = displayInfo.ScreenWidthInRawPixels;
            screenHeight = displayInfo.ScreenHeightInRawPixels;

            SentrySdk.ConfigureScope(scope =>
            {
                // Collect settings
                string settings = "";
                settings += $"Screen resolution: {screenWidth}x{screenHeight}\n";
                settings += $"savePlayingSounds: {FileManager.itemViewHolder.SavePlayingSounds}\n";
                settings += $"openMultipleSounds: {FileManager.itemViewHolder.OpenMultipleSounds}\n";
                settings += $"multiSoundPlayback: {FileManager.itemViewHolder.MultiSoundPlayback}\n";
                settings += $"showSoundsPivot: {FileManager.itemViewHolder.ShowSoundsPivot}\n";
                settings += $"soundOrder: {FileManager.itemViewHolder.SoundOrder}\n";
                settings += $"showListView: {FileManager.itemViewHolder.ShowListView}\n";
                settings += $"showCategoriesIcons: {FileManager.itemViewHolder.ShowCategoriesIcons}\n";
                settings += $"showAcrylicBackground: {FileManager.itemViewHolder.ShowAcrylicBackground}\n";
                settings += $"isLoggedIn: {Dav.IsLoggedIn}\n";
                if (Dav.IsLoggedIn) settings += $"plan: {Dav.User.Plan}\n";
                settings += $"isFadeInEffectEnabled: {FileManager.itemViewHolder.IsFadeInEffectEnabled}\n";
                settings += $"isFadeOutEffectEnabled: {FileManager.itemViewHolder.IsFadeOutEffectEnabled}\n";
                settings += $"isEchoEffectEnabled: {FileManager.itemViewHolder.IsEchoEffectEnabled}\n";
                settings += $"isLimiterEffectEnabled: {FileManager.itemViewHolder.IsLimiterEffectEnabled}\n";
                settings += $"isReverbEffectEnabled: {FileManager.itemViewHolder.IsReverbEffectEnabled}\n";
                settings += $"isPitchShiftEffectEnabled: {FileManager.itemViewHolder.IsPitchShiftEffectEnabled}";

                scope.AddAttachment(Encoding.UTF8.GetBytes(settings), "settings.txt");

                scope.User = new SentryUser
                {
                    Id = FileManager.itemViewHolder.UserId.ToString(),
                    Email = Dav.IsLoggedIn ? Dav.User.Email : null
                };
            });
        }

        private void CustomiseTitleBar()
        {
            ApplicationViewTitleBar titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ButtonForegroundColor = FileManager.itemViewHolder.CurrentTheme == AppTheme.Dark ? Colors.White : Colors.Black;
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
            accentButtonStyle = Resources["AccentButtonStyle"] as Style;
            infoButtonStyle = Resources["InfoButtonStyle"] as Style;
            listViewItemStyle = Resources["ListViewItemStyle"] as Style;
            soundFileDownloadProgressTemplate = Resources["SoundFileDownloadProgressTemplate"] as DataTemplate;

            SideBar.ExpandedModeThresholdWidth = Constants.sideBarCollapsedMaxWidth;

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
            double width = Window.Current.Bounds.Width;

            FileManager.AdjustLayout();
            UpdateTopButtonVisibilityForMobileSearch();

            // Set the width of the title bar and the position of the title, depending on whether the Hamburger button of the NavigationView is visible
            if (SideBar.DisplayMode == WinUI.NavigationViewDisplayMode.Minimal)
            {
                TitleBar.Width = width - 96;
                WindowTitleTextBlock.Margin = new Thickness(97, 15, 0, 0);
            }
            else
            {
                TitleBar.Width = width - 48;
                WindowTitleTextBlock.Margin = new Thickness(65, 15, 0, 0);
            }

            // Update the margin of the profile image in the Sidebar
            if (SideBar.IsPaneOpen)
                AccountMenuItem.Margin = new Thickness(-2, 1, 0, 0);
            else
                AccountMenuItem.Margin = new Thickness(2, 1, 0, 0);

            // Update the width / visibility of the Store search field
            if (width < 650)
            {
                StoreSearchAutoSuggestBox.Visibility = Visibility.Collapsed;
                StoreSearchButton.Visibility = Visibility.Visible;
            }
            else if (width < 800)
            {
                StoreSearchAutoSuggestBox.Width = 200;
                StoreSearchAutoSuggestBox.Visibility = Visibility.Visible;
                StoreSearchButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                StoreSearchAutoSuggestBox.Width = 300;
                StoreSearchAutoSuggestBox.Visibility = Visibility.Visible;
                StoreSearchButton.Visibility = Visibility.Collapsed;
            }

            // Update the app title if the user is on UniversalSoundboard Plus, dav Plus or dav Pro
            if (FileManager.itemViewHolder.PlusPurchased)
            {
                appTitle = "UniversalSoundboard Plus";
            }
            else if (Dav.IsLoggedIn)
            {
                if (Dav.User.Plan == Plan.Plus)
                    appTitle = "UniversalSoundboard Plus";
                else if (Dav.User.Plan == Plan.Pro)
                    appTitle = "UniversalSoundboard Pro";
            }

            Bindings.Update();
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
            mobileSearchVisible = Window.Current.Bounds.Width < Constants.hideSearchBoxMaxWidth && FileManager.itemViewHolder.SearchAutoSuggestBoxVisible;
            Bindings.Update();
        }

        private async Task GoBack()
        {
            await FileManager.GoBackAsync();
            AdjustLayout();

            if (
                FileManager.itemViewHolder.SelectedCategory.Equals(Guid.Empty)
                && FileManager.itemViewHolder.Page == typeof(SoundPage)
            )
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
                SentrySdk.CaptureMessage("AppStarts", scope =>
                {
                    scope.SetTag("Count", count.ToString());
                });
            }

            if (!FileManager.itemViewHolder.AppReviewed && count % 100 == 0)
            {
                // Show InAppNotification for writing a review
                var args = new ShowInAppNotificationEventArgs(
                    InAppNotificationType.WriteReview,
                    FileManager.loader.GetString("InAppNotification-WriteReview"),
                    0,
                    false,
                    true,
                    FileManager.loader.GetString("Actions-WriteReview")
                );
                args.PrimaryButtonClick += WriteReviewInAppNotificationEventArgs_PrimaryButtonClick;

                FileManager.itemViewHolder.TriggerShowInAppNotificationEvent(this, args);
            }
        }

        private void UpdateOutputDeviceFlyout()
        {
            if (OutputDeviceButtonFlyout != null && OutputDeviceButtonFlyout.IsOpen)
                return;

            MenuFlyout menuFlyout = new MenuFlyout
            {
                Placement = FlyoutPlacementMode.Bottom
            };

            ToggleMenuFlyoutItem standardItem = new ToggleMenuFlyoutItem
            {
                Text = FileManager.loader.GetString("StandardOutputDevice"),
                IsChecked = true
            };

            standardItem.Click += OutputDeviceItem_Click;
            menuFlyout.Items.Add(standardItem);

            foreach (var device in FileManager.deviceWatcherHelper.Devices)
            {
                ToggleMenuFlyoutItem item = new ToggleMenuFlyoutItem
                {
                    Text = device.Name,
                    Tag = device.Id,
                    IsChecked = !FileManager.itemViewHolder.UseStandardOutputDevice && FileManager.itemViewHolder.OutputDevice == device.Id
                };

                item.Click += OutputDeviceItem_Click;
                menuFlyout.Items.Add(item);

                if (item.IsChecked)
                    standardItem.IsChecked = false;
            }

            menuFlyout.Items.Add(new MenuFlyoutSeparator());

            var manageMenuFlyoutItem = new MenuFlyoutItem
            {
                Text = FileManager.loader.GetString("ManageOutputDevices")
            };

            manageMenuFlyoutItem.Click += ManageMenuFlyoutItem_Click;

            menuFlyout.Items.Add(manageMenuFlyoutItem);

            OutputDeviceButtonFlyout = menuFlyout;
        }

        private async Task ContinuePlaylistDownload()
        {
            if (!FileManager.itemViewHolder.ShowContinuePlaylistDownloadIAN)
                return;

            FileManager.itemViewHolder.ShowContinuePlaylistDownloadIAN = false;

            // Check if there was a playlist download in the previous session
            var soundDownloadState = await SoundDownloadState.Load();
            var soundDownloadStateItems = await SoundDownloadStateItems.Load();

            if (soundDownloadState == null || soundDownloadStateItems == null)
                return;

            // Show IAN for continuing playlist download
            var showInAppNotificationEventArgs = new ShowInAppNotificationEventArgs(
                InAppNotificationType.ContinuePlaylistDownload,
                string.Format(FileManager.loader.GetString("InAppNotification-ContinuePlaylistDownload"), 1, soundDownloadStateItems.SoundItems.Count),
                0,
                false,
                true,
                FileManager.loader.GetString("Actions-ContinueDownload")
            );

            showInAppNotificationEventArgs.PrimaryButtonClick += async (object sender, RoutedEventArgs e) =>
            {
                // Hide the IAN
                FileManager.DismissInAppNotification(InAppNotificationType.ContinuePlaylistDownload);

                await ContinueSoundDownload(soundDownloadState, soundDownloadStateItems);
            };

            FileManager.itemViewHolder.TriggerShowInAppNotificationEvent(
                this,
                showInAppNotificationEventArgs
            );
        }

        private void AppStartTracking()
        {
            int numberOfSounds = FileManager.itemViewHolder.AllSounds.Count;
            string numberOfSoundsArea = ">= 200";

            if (numberOfSounds < 50)
                numberOfSoundsArea = "0 - 49";
            else if (numberOfSounds < 100)
                numberOfSoundsArea = "50 - 99";
            else if (numberOfSounds < 150)
                numberOfSoundsArea = "100 - 149";
            else if (numberOfSounds < 200)
                numberOfSoundsArea = "150 - 199";

            int numberOfCategories = FileManager.itemViewHolder.Categories.Count - 1;
            string numberOfCategoriesArea = ">= 20";

            if (numberOfCategories < 5)
                numberOfCategoriesArea = "0 - 4";
            else if (numberOfCategories < 10)
                numberOfCategoriesArea = "5 - 9";
            else if (numberOfCategories < 15)
                numberOfCategoriesArea = "10 - 14";
            else if (numberOfCategories < 20)
                numberOfCategoriesArea = "15 - 19";

            var appStartDict = new Dictionary<string, string>
            {
                { "Number of sounds", numberOfSoundsArea },
                { "Number of categories", numberOfCategoriesArea },
                { "Screen resolution", $"{screenWidth}x{screenHeight}" },
                { "savePlayingSounds", FileManager.itemViewHolder.SavePlayingSounds.ToString() },
                { "openMultipleSounds", FileManager.itemViewHolder.OpenMultipleSounds.ToString() },
                { "multiSoundPlayback", FileManager.itemViewHolder.MultiSoundPlayback.ToString() },
                { "showSoundsPivot", FileManager.itemViewHolder.ShowSoundsPivot.ToString() },
                { "soundOrder", FileManager.itemViewHolder.SoundOrder.ToString() },
                { "showListView", FileManager.itemViewHolder.ShowListView.ToString() },
                { "showCategoriesIcons", FileManager.itemViewHolder.ShowCategoriesIcons.ToString() },
                { "showAcrylicBackground", FileManager.itemViewHolder.ShowAcrylicBackground.ToString() },
                { "multipleOutputDevices", FileManager.itemViewHolder.MultipleOutputDevices.ToString() },
                { "isLoggedIn", Dav.IsLoggedIn.ToString() },
                { "isFadeInEffectEnabled", FileManager.itemViewHolder.IsFadeInEffectEnabled.ToString() },
                { "isFadeOutEffectEnabled", FileManager.itemViewHolder.IsFadeOutEffectEnabled.ToString() },
                { "isEchoEffectEnabled", FileManager.itemViewHolder.IsEchoEffectEnabled.ToString() },
                { "isLimiterEffectEnabled", FileManager.itemViewHolder.IsLimiterEffectEnabled.ToString() },
                { "isReverbEffectEnabled", FileManager.itemViewHolder.IsReverbEffectEnabled.ToString() },
                { "isPitchShiftEffectEnabled", FileManager.itemViewHolder.IsPitchShiftEffectEnabled.ToString() }
            };

            if (Dav.IsLoggedIn)
                appStartDict.Add("plan", Dav.User.Plan.ToString());

            SentrySdk.CaptureMessage("AppStart", scope =>
            {
                scope.SetTags(appStartDict);
            });
        }

        private async Task LoadPlusInfo()
        {
            if (FileManager.itemViewHolder.PlusPurchased) return;

            var context = StoreContext.GetDefault();

            string[] productKinds = { "Durable" };
            string[] storeIds = new string[] { Constants.UniversalSoundboardPlusAddonStoreId };

            StoreProductQueryResult queryResult = await context.GetStoreProductsAsync(new List<string>(productKinds), storeIds);
            if (queryResult.Products.Count == 0) return;

            FileManager.itemViewHolder.UpgradePlusPrice = queryResult.Products.First().Value.Price.FormattedPrice;
            FileManager.itemViewHolder.PlusPurchased = queryResult.Products.First().Value.IsInUserCollection;
        }

        public static void NavigateToPage(
            Type pageType,
            object parameter = null,
            NavigationTransitionInfo navigationTransitionInfo = null
        )
        {
            if (contentFrame.SourcePageType == pageType)
                return;

            if (navigationTransitionInfo == null)
                navigationTransitionInfo = new EntranceNavigationTransitionInfo();

            contentFrame.Navigate(pageType, parameter, navigationTransitionInfo);

            if (FileManager.itemViewHolder.Page != pageType)
                FileManager.itemViewHolder.Page = pageType;
        }

        public static void NavigateBack()
        {
            contentFrame.GoBack();
            FileManager.itemViewHolder.Page = contentFrame.CurrentSourcePageType;
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

            bool searchVisible = FileManager.itemViewHolder.SearchQuery.Length > 0;
            FileManager.ResetSearchArea();

            if (args.IsSettingsInvoked)
            {
                // Show the Settings page
                FileManager.NavigateToSettingsPage();
            }
            else if (args.InvokedItemContainer == StoreMenuItem)
            {
                // Show the Store page
                FileManager.NavigateToStorePage();
            }
            else if (
                args.InvokedItemContainer == LoginMenuItem
                || args.InvokedItemContainer == AccountMenuItem
            )
            {
                // Show the Account page
                FileManager.NavigateToAccountPage("SideBar");

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

                if (
                    FileManager.itemViewHolder.Page == typeof(SoundPage)
                    && (
                        categoryUuid.Equals(FileManager.itemViewHolder.SelectedCategory)
                        && !searchVisible
                    )
                ) return;

                // Set initialCategory if the playing sounds weren't still loaded
                if (FileManager.itemViewHolder.AppState == AppState.Loading && !playingSoundsLoaded)
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
            Category currentCategory = FileManager.FindCategory(FileManager.itemViewHolder.SelectedCategory);

            var editCategoryDialog = new EditCategoryDialog(currentCategory);
            editCategoryDialog.PrimaryButtonClick += EditCategoryContentDialog_PrimaryButtonClick;
            await editCategoryDialog.ShowAsync();
        }

        private async void EditCategoryContentDialog_PrimaryButtonClick(Dialog sender, ContentDialogButtonClickEventArgs args)
        {
            var dialog = sender as EditCategoryDialog;

            string newName = dialog.Name;
            string icon = dialog.Icon;

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
            Category currentCategory = FileManager.FindCategory(FileManager.itemViewHolder.SelectedCategory);

            var deleteCategoryDialog = new DeleteCategoryDialog(currentCategory.Name);
            deleteCategoryDialog.PrimaryButtonClick += DeleteCategoryContentDialog_PrimaryButtonClick;
            await deleteCategoryDialog.ShowAsync();
        }

        private async void DeleteCategoryContentDialog_PrimaryButtonClick(Dialog sender, ContentDialogButtonClickEventArgs args)
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

            var template = Resources["DialogSoundListItemTemplate"] as DataTemplate;
            var listViewItemStyle = Resources["ListViewItemStyle"] as Style;

            playSoundsSuccessivelyDialog = new PlaySoundsSuccessivelyDialog(sounds, template, listViewItemStyle);
            playSoundsSuccessivelyDialog.PrimaryButtonClick += PlaySoundsSuccessivelyContentDialog_PrimaryButtonClick;
            await playSoundsSuccessivelyDialog.ShowAsync();
        }

        private async void PlaySoundsButton_Click(object sender, RoutedEventArgs e)
        {
            // Check if Flyout with option to play sounds simultaneously should be shown
            if (FileManager.itemViewHolder.OpenMultipleSounds && FileManager.itemViewHolder.MultiSoundPlayback)
            {
                // Show flyout
                MenuFlyout flyout = new MenuFlyout();

                MenuFlyoutItem playSoundsSuccessivelyFlyoutItem = new MenuFlyoutItem
                {
                    Text = FileManager.loader.GetString("PlaySoundsSuccessively")
                };
                playSoundsSuccessivelyFlyoutItem.Click += PlaySoundsSuccessivelyFlyoutItem_Click;
                flyout.Items.Add(playSoundsSuccessivelyFlyoutItem);

                MenuFlyoutItem playSoundsSimultaneouslyFlyouItem = new MenuFlyoutItem
                {
                    Text = FileManager.loader.GetString("PlaySoundsSimultaneously"),
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

            var template = (DataTemplate)Resources["DialogSoundListItemTemplate"];
            var listViewItemStyle = Resources["ListViewItemStyle"] as Style;

            playSoundsSuccessivelyDialog = new PlaySoundsSuccessivelyDialog(sounds, template, listViewItemStyle);
            playSoundsSuccessivelyDialog.PrimaryButtonClick += PlaySoundsSuccessivelyContentDialog_PrimaryButtonClick;
            await playSoundsSuccessivelyDialog.ShowAsync();
        }

        private async void PlaySoundsSuccessivelyFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            await ShowPlaySelectedSoundsSuccessivelyDialog();
        }

        private void PlaySoundsSuccessivelyContentDialog_PrimaryButtonClick(Dialog sender, ContentDialogButtonClickEventArgs args)
        {
            bool randomly = playSoundsSuccessivelyDialog.Random;
            int rounds = int.MaxValue;

            if (playSoundsSuccessivelyDialog.RepetitionsComboBox.SelectedItem != playSoundsSuccessivelyDialog.RepetitionsComboBox.Items.Last())
                int.TryParse(playSoundsSuccessivelyDialog.RepetitionsComboBox.SelectedValue.ToString(), out rounds);

            FileManager.itemViewHolder.TriggerPlaySoundsEvent(
                this,
                new PlaySoundsEventArgs(
                    playSoundsSuccessivelyDialog.Sounds,
                    rounds,
                    randomly
                )
            );

            // Disable multi-selection mode
            FileManager.itemViewHolder.MultiSelectionEnabled = false;
        }

        private void PlaySoundsSimultaneouslyFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            foreach (Sound sound in FileManager.itemViewHolder.SelectedSounds)
                FileManager.itemViewHolder.TriggerPlaySoundEvent(this, new PlaySoundEventArgs(sound));

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
        private async void AddButtonSoundFilesFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            // Show file picker for new sounds
            var files = await PickFilesForAddSoundsContentDialog();
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
            await AddSelectedSoundFiles(dialog.SelectedFiles);
        }

        public static async Task<List<StorageFile>> PickFilesForAddSoundsContentDialog()
        {
            // Open file picker
            var picker = new Windows.Storage.Pickers.FileOpenPicker
            {
                ViewMode = Windows.Storage.Pickers.PickerViewMode.List,
                SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.MusicLibrary
            };

            foreach (var fileType in Constants.allowedFileTypes)
                picker.FileTypeFilter.Add(fileType);

            var files = await picker.PickMultipleFilesAsync();
            return files.ToList();
        }

        public static async Task AddSelectedSoundFiles(List<StorageFile> selectedFiles)
        {
            if (selectedFiles.Count == 0) return;
            FileManager.itemViewHolder.AddingSounds = true;

            // Show InAppNotification
            FileManager.itemViewHolder.TriggerShowInAppNotificationEvent(
                null,
                new ShowInAppNotificationEventArgs(
                    InAppNotificationType.AddSounds,
                    string.Format(FileManager.loader.GetString("InAppNotification-AddSounds"), 0, selectedFiles.Count),
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

            foreach (var file in selectedFiles)
            {
                // Create the sound and add it to the sound lists
                Guid uuid = await FileManager.CreateSoundAsync(null, file.DisplayName, selectedCategory, file);

                if (uuid.Equals(Guid.Empty))
                    notAddedSounds.Add(file.Name);
                else
                {
                    await FileManager.AddSound(uuid);

                    FileManager.SetInAppNotificationMessage(
                        InAppNotificationType.AddSounds,
                        string.Format(FileManager.loader.GetString("InAppNotification-AddSounds"), i + 1, selectedFiles.Count)
                    );

                    i++;
                }
            }

            FileManager.UpdatePlayAllButtonVisibility();

            if (FileManager.itemViewHolder.AppState == AppState.Empty)
                FileManager.itemViewHolder.AppState = AppState.Normal;

            if (notAddedSounds.Count > 0)
            {
                string message = notAddedSounds.Count == 1 ?
                    FileManager.loader.GetString("InAppNotification-AddSoundsErrorOneSound")
                    : string.Format(FileManager.loader.GetString("InAppNotification-AddSoundsErrorMultipleSounds"), notAddedSounds.Count);

                var inAppNotificationArgs = new ShowInAppNotificationEventArgs(
                    InAppNotificationType.AddSounds,
                    message,
                    0,
                    false,
                    true,
                    FileManager.loader.GetString("Actions-ShowDetails")
                );

                inAppNotificationArgs.PrimaryButtonClick += async (sender, args) =>
                {
                    FileManager.DismissInAppNotification(InAppNotificationType.AddSounds);
                    await new AddSoundsErrorDialog(notAddedSounds).ShowAsync();
                };

                FileManager.itemViewHolder.TriggerShowInAppNotificationEvent(null, inAppNotificationArgs);
            }
            else
            {
                string message = selectedFiles.Count == 1 ?
                    FileManager.loader.GetString("InAppNotification-AddSoundSuccessful")
                    : string.Format(FileManager.loader.GetString("InAppNotification-AddSoundsSuccessful"), selectedFiles.Count);

                FileManager.itemViewHolder.TriggerShowInAppNotificationEvent(
                    null,
                    new ShowInAppNotificationEventArgs(
                        InAppNotificationType.AddSounds,
                        message,
                        8000,
                        false,
                        true
                    )
                );
            }

            FileManager.itemViewHolder.AddingSounds = false;

            SentrySdk.CaptureMessage("AddSounds", scope =>
            {
                scope.SetTags(new Dictionary<string, string>
                {
                    { "AddedSounds", i.ToString() },
                    { "NotAddedSounds", notAddedSounds.Count.ToString() }
                });
            });
        }
        #endregion

        #region SoundDownload
        private async void DownloadSoundsFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            SentrySdk.CaptureMessage("AddButton-DownloadSounds");

            var soundDownloadListItemTemplate = Resources["SoundDownloadListItemTemplate"] as DataTemplate;

            var soundDownloadDialog = new SoundDownloadDialog(soundDownloadListItemTemplate);
            soundDownloadDialog.PrimaryButtonClick += SoundDownloadDialog_PrimaryButtonClick;
            await soundDownloadDialog.ShowAsync();
        }

        private async void SoundDownloadDialog_PrimaryButtonClick(Dialog sender, ContentDialogButtonClickEventArgs args)
        {
            await StartSoundDownload(sender as SoundDownloadDialog);
        }

        public async Task StartSoundDownload(SoundDownloadDialog dialog)
        {
            if (dialog.Result == null) return;

            await HandleSoundDownload(
                soundItems: dialog.Result.SoundItems,
                category: Guid.Empty,
                createCategoryForPlaylist: dialog.Result.CreateCategoryForPlaylist,
                categoryName: dialog.Result.CategoryName,
                notDownloadedSounds: new List<int>()
            );
        }

        public async Task ContinueSoundDownload(SoundDownloadState soundDownloadState, SoundDownloadStateItems soundDownloadStateItems)
        {
            List<SoundDownloadItem> soundItems = new List<SoundDownloadItem>();

            foreach (var soundItem in soundDownloadStateItems.SoundItems)
            {
                if (soundDownloadStateItems.Class == "SoundDownloadYoutubeItem")
                {
                    soundItems.Add(new SoundDownloadYoutubeItem(
                        soundItem.Name,
                        soundItem.Url,
                        soundItem.ImageFileUrl,
                        soundItem.AudioFileUrl,
                        soundItem.ImageFileExt,
                        soundItem.AudioFileExt,
                        soundItem.ImageFileSize,
                        soundItem.AudioFileSize,
                        soundItem.IsSelected
                    ));
                }
                else
                    soundItems.Add(soundItem);
            }

            await HandleSoundDownload(
                soundItems: soundItems,
                category: soundDownloadState.CategoryUuid,
                startIndex: soundDownloadState.CurrentIndex,
                notDownloadedSounds: soundDownloadState.NotDownloadedSounds
            );
        }

        public async Task HandleSoundDownload(
            List<SoundDownloadItem> soundItems,
            Guid category,
            List<int> notDownloadedSounds,
            bool createCategoryForPlaylist = false,
            string categoryName = null,
            int startIndex = 0
        )
        {
            List<SoundDownloadItem> selectedSoundItems = new List<SoundDownloadItem>();

            foreach (var item in soundItems)
                if (item.IsSelected) selectedSoundItems.Add(item);

            if (
                selectedSoundItems.Count == 0
                || startIndex >= selectedSoundItems.Count
            ) return;

            Guid currentCategoryUuid = !category.Equals(Guid.Empty) ? category : FileManager.itemViewHolder.SelectedCategory;
            var cancellationTokenSource = new CancellationTokenSource();

            // Disable the ability to add or download sounds
            FileManager.itemViewHolder.AddingSounds = true;

            if (selectedSoundItems.Count == 1)
            {
                // Single sound download
                var soundItem = selectedSoundItems.First();

                var showInAppNotificationEventArgs = new ShowInAppNotificationEventArgs(
                    InAppNotificationType.DownloadSound,
                    FileManager.loader.GetString("InAppNotification-DownloadSound"),
                    0,
                    true,
                    false,
                    FileManager.loader.GetString("Actions-Cancel")
                );

                showInAppNotificationEventArgs.PrimaryButtonClick += (object s, RoutedEventArgs e) =>
                {
                    cancellationTokenSource.Cancel();
                };

                // Show InAppNotification
                FileManager.itemViewHolder.TriggerShowInAppNotificationEvent(
                    this,
                    showInAppNotificationEventArgs
                );

                // Download the image file, if there is one
                StorageFile imageFile = await soundItem.DownloadImageFile();

                // Download the audio file
                var progress = new Progress<int>((int value) => FileManager.SetInAppNotificationProgress(InAppNotificationType.DownloadSound, false, value));

                StorageFile audioFile = await soundItem.DownloadAudioFile(progress, cancellationTokenSource.Token);

                if (await CheckSoundDownloadCancelled(cancellationTokenSource))
                    return;

                if (audioFile == null)
                {
                    ShowDownloadSoundErrorInAppNotification();
                    return;
                }

                FileManager.SetInAppNotificationProgress(InAppNotificationType.DownloadSound);

                // Save the sound in the database
                Guid uuid = await FileManager.CreateSoundAsync(
                    null,
                    soundItem.Name,
                    new List<Guid> { currentCategoryUuid },
                    audioFile,
                    imageFile,
                    soundItem.Url
                );

                if (uuid.Equals(Guid.Empty))
                {
                    ShowDownloadSoundErrorInAppNotification();
                    return;
                }

                // Add the sound to the list
                await FileManager.AddSound(uuid);
                FileManager.UpdatePlayAllButtonVisibility();

                FileManager.itemViewHolder.TriggerShowInAppNotificationEvent(
                    this,
                    new ShowInAppNotificationEventArgs(
                        InAppNotificationType.DownloadSound,
                        FileManager.loader.GetString("InAppNotification-DownloadSoundSuccessful"),
                        8000,
                        false,
                        true
                    )
                );
            }
            else
            {
                // Playlist download
                var showInAppNotificationEventArgs = new ShowInAppNotificationEventArgs(
                    InAppNotificationType.DownloadSounds,
                    string.Format(FileManager.loader.GetString("InAppNotification-DownloadSounds"), 1, selectedSoundItems.Count),
                    0,
                    true,
                    false,
                    FileManager.loader.GetString("Actions-Cancel")
                );

                showInAppNotificationEventArgs.PrimaryButtonClick += async (object s, RoutedEventArgs e) =>
                {
                    // Show Dialog for canceling Playlist download
                    var cancelDownloadDialog = new CancelYouTubePlaylistDownloadDialog();

                    cancelDownloadDialog.PrimaryButtonClick += (Dialog d, ContentDialogButtonClickEventArgs a) =>
                    {
                        cancellationTokenSource.Cancel();
                    };

                    await cancelDownloadDialog.ShowAsync();
                };

                // Show InAppNotification
                FileManager.itemViewHolder.TriggerShowInAppNotificationEvent(
                    this,
                    showInAppNotificationEventArgs
                );

                var soundDownloadState = new SoundDownloadState(0, currentCategoryUuid);

                // Create category for playlist, if the option is checked
                Category newCategory = null;

                if (createCategoryForPlaylist && categoryName != null)
                {
                    // Create category
                    List<string> iconsList = FileManager.GetIconsList();
                    int randomNumber = new Random().Next(iconsList.Count);

                    Guid categoryUuid = await FileManager.CreateCategoryAsync(null, null, categoryName, iconsList[randomNumber]);
                    newCategory = await FileManager.GetCategoryAsync(categoryUuid, false);

                    // Add the category to the Categories list
                    FileManager.AddCategory(newCategory, Guid.Empty);

                    // Add the category to the SideBar
                    AddCategoryMenuItem(menuItems, newCategory, Guid.Empty);

                    soundDownloadState.CategoryUuid = categoryUuid;
                }

                // Save the selected items
                await new SoundDownloadStateItems(selectedSoundItems).Save();
                FileManager.itemViewHolder.ShowContinuePlaylistDownloadIAN = true;

                // Go through each video of the playlist
                for (int i = startIndex; i < selectedSoundItems.Count; i++)
                {
                    // Save the current state
                    soundDownloadState.CurrentIndex = i;
                    soundDownloadState.NotDownloadedSounds = notDownloadedSounds;
                    await soundDownloadState.Save();

                    var currentSoundItem = selectedSoundItems[i];

                    FileManager.SetInAppNotificationMessage(
                        InAppNotificationType.DownloadSounds,
                        string.Format(FileManager.loader.GetString("InAppNotification-DownloadSounds"), i + 1, selectedSoundItems.Count)
                    );

                    StorageFile imageFile = null;

                    if (currentSoundItem.ImageFileUrl != null)
                    {
                        imageFile = await currentSoundItem.DownloadImageFile();

                        if (await CheckSoundDownloadCancelled(cancellationTokenSource))
                            return;

                        if (imageFile == null && currentSoundItem.ImageFileUrl != null)
                        {
                            notDownloadedSounds.Add(i);
                            continue;
                        }
                    }

                    var progress = new Progress<int>((int value) => FileManager.SetInAppNotificationProgress(InAppNotificationType.DownloadSounds, false, value));

                    StorageFile audioFile = await currentSoundItem.DownloadAudioFile(
                        progress,
                        cancellationTokenSource.Token
                    );

                    if (await CheckSoundDownloadCancelled(cancellationTokenSource))
                        return;

                    if (audioFile == null)
                    {
                        // Wait a few seconds and try it again
                        await Task.Delay(15000);

                        if (await CheckSoundDownloadCancelled(cancellationTokenSource))
                            return;

                        audioFile = await currentSoundItem.DownloadAudioFile(
                            progress,
                            cancellationTokenSource.Token
                        );

                        if (await CheckSoundDownloadCancelled(cancellationTokenSource))
                            return;

                        if (audioFile == null)
                        {
                            notDownloadedSounds.Add(i);
                            continue;
                        }
                    }

                    FileManager.SetInAppNotificationProgress(InAppNotificationType.DownloadSounds);

                    // Create the new sound in the database
                    Guid uuid = await FileManager.CreateSoundAsync(
                        null,
                        currentSoundItem.Name,
                        newCategory != null ? newCategory.Uuid : currentCategoryUuid,
                        audioFile,
                        imageFile,
                        currentSoundItem.Url
                    );

                    if (uuid.Equals(Guid.Empty))
                    {
                        notDownloadedSounds.Add(i);
                        continue;
                    }

                    // Add the sound to the list
                    await FileManager.AddSound(uuid);
                    FileManager.UpdatePlayAllButtonVisibility();
                }

                await SoundDownloadState.Delete();
                await SoundDownloadStateItems.Delete();
                FileManager.itemViewHolder.ShowContinuePlaylistDownloadIAN = false;

                if (notDownloadedSounds.Count > 0)
                {
                    string message = notDownloadedSounds.Count == 1 ?
                        FileManager.loader.GetString("InAppNotification-DownloadSoundsErrorOneSound")
                        : string.Format(FileManager.loader.GetString("InAppNotification-DownloadSoundsErrorMultipleSounds"), notDownloadedSounds.Count);

                    var inAppNotificationArgs = new ShowInAppNotificationEventArgs(
                        InAppNotificationType.DownloadSounds,
                        message,
                        0,
                        false,
                        true,
                        FileManager.loader.GetString("Actions-ShowDetails")
                    );

                    // Get the not downloaded sound items
                    List<SoundDownloadItem> notDownloadedSoundItems = new List<SoundDownloadItem>();

                    foreach (int index in notDownloadedSounds)
                        notDownloadedSoundItems.Add(selectedSoundItems.ElementAt(index));

                    inAppNotificationArgs.PrimaryButtonClick += async (s, a) =>
                    {
                        FileManager.DismissInAppNotification(InAppNotificationType.DownloadSounds);
                        await new DownloadSoundsErrorDialog(notDownloadedSoundItems).ShowAsync();
                    };

                    FileManager.itemViewHolder.TriggerShowInAppNotificationEvent(this, inAppNotificationArgs);

                    // Save not downloaded sounds in dict for analytics
                    var notDownloadedSoundsDict = new Dictionary<string, string>();

                    for (int i = 0; i < notDownloadedSounds.Count; i++)
                    {
                        notDownloadedSoundsDict.Add($"Sound-{i}", notDownloadedSoundItems[i].AudioFileUrl);
                        if (i > 10) break;
                    }

                    SentrySdk.CaptureMessage("PlaylistDownload-NotDownloadedSounds", scope =>
                    {
                        scope.SetTags(notDownloadedSoundsDict);
                    });
                }
                else
                {
                    FileManager.itemViewHolder.TriggerShowInAppNotificationEvent(
                        this,
                        new ShowInAppNotificationEventArgs(
                            InAppNotificationType.DownloadSounds,
                            string.Format(FileManager.loader.GetString("InAppNotification-DownloadSoundsSuccessful"), selectedSoundItems.Count),
                            8000,
                            false,
                            true
                        )
                    );
                }
            }

            FileManager.itemViewHolder.AddingSounds = false;
        }

        public void ShowDownloadSoundErrorInAppNotification()
        {
            FileManager.itemViewHolder.AddingSounds = false;

            var inAppNotificationEventArgs = new ShowInAppNotificationEventArgs(
                InAppNotificationType.DownloadSound,
                FileManager.loader.GetString("InAppNotification-DownloadSoundError"),
                0,
                false,
                true,
                FileManager.loader.GetString("Actions-ShowDetails")
            );
            inAppNotificationEventArgs.PrimaryButtonClick += async (sender, args) => await ShowDownloadErrorDialog();

            FileManager.itemViewHolder.TriggerShowInAppNotificationEvent(this, inAppNotificationEventArgs);
        }

        public async Task ShowDownloadErrorDialog()
        {
            await new DownloadFileErrorDialog().ShowAsync();
        }

        private async Task<bool> CheckSoundDownloadCancelled(CancellationTokenSource cancellationTokenSource)
        {
            if (cancellationTokenSource.IsCancellationRequested)
            {
                // Hide the IAN
                FileManager.DismissInAppNotification(InAppNotificationType.DownloadSound);
                FileManager.DismissInAppNotification(InAppNotificationType.DownloadSounds);
                FileManager.itemViewHolder.AddingSounds = false;

                // Delete the download state files
                await SoundDownloadState.Delete();
                await SoundDownloadStateItems.Delete();

                return true;
            }

            return false;
        }
        #endregion

        #region New Category
        private async void AddButtonCategoryFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            var newCategoryDialog = new NewCategoryDialog();
            newCategoryDialog.PrimaryButtonClick += NewCategoryContentDialog_PrimaryButtonClick;
            await newCategoryDialog.ShowAsync();
        }

        private async void NewCategoryContentDialog_PrimaryButtonClick(Dialog sender, ContentDialogButtonClickEventArgs args)
        {
            var dialog = sender as NewCategoryDialog;

            Guid categoryUuid = await FileManager.CreateCategoryAsync(null, null, dialog.Name, dialog.Icon);
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

        #region Tools button
        private async void ToolsButtonSoundRecorderFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            if (soundRecorderAppWindow == null)
            {
                soundRecorderAppWindow = await AppWindow.TryCreateAsync();
                soundRecorderAppWindow.RequestSize(new Size(500, 500));
                soundRecorderAppWindow.Title = FileManager.loader.GetString("SoundRecorder-Title");
                soundRecorderAppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
                soundRecorderAppWindow.TitleBar.ButtonForegroundColor = FileManager.itemViewHolder.CurrentTheme == AppTheme.Dark ? Colors.White : Colors.Black;
                soundRecorderAppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
                soundRecorderAppWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

                soundRecorderAppWindow.Closed += (AppWindow window, AppWindowClosedEventArgs args) => soundRecorderAppWindow = null;

                soundRecorderAppWindowContentFrame = new Frame();
                soundRecorderAppWindowContentFrame.Navigate(typeof(SoundRecorderPage));
                ElementCompositionPreview.SetAppWindowContent(soundRecorderAppWindow, soundRecorderAppWindowContentFrame);

                SentrySdk.CaptureMessage("MainPage-ToolsButton-SoundRecorder");
            }

            await soundRecorderAppWindow.TryShowAsync();
        }

        private async void ToolsButtonEffectManagerFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            if (effectManagerAppWindow == null)
            {
                effectManagerAppWindow = await AppWindow.TryCreateAsync();
                effectManagerAppWindow.RequestSize(new Size(500, 500));
                effectManagerAppWindow.Title = FileManager.loader.GetString("EffectManager-Title");
                effectManagerAppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
                effectManagerAppWindow.TitleBar.ButtonForegroundColor = FileManager.itemViewHolder.CurrentTheme == AppTheme.Dark ? Colors.White : Colors.Black;
                effectManagerAppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
                effectManagerAppWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

                effectManagerAppWindow.Closed += (AppWindow window, AppWindowClosedEventArgs args) => effectManagerAppWindow = null;

                effectManagerAppWindowContentFrame = new Frame();
                effectManagerAppWindowContentFrame.Navigate(typeof(EffectManagerPage));
                ElementCompositionPreview.SetAppWindowContent(effectManagerAppWindow, effectManagerAppWindowContentFrame);

                SentrySdk.CaptureMessage("MainPage-ToolsButton-EffectManager");
            }
            
            await effectManagerAppWindow.TryShowAsync();
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
                FileManager.itemViewHolder.Title = FileManager.loader.GetString("AllSounds");
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

            string description = FileManager.loader.GetString("ShareDialog-MultipleSounds");

            if (sharedFiles.Count == 1)
                description = sharedFiles.First().Name;

            DataRequest request = args.Request;
            request.Data.SetStorageItems(sharedFiles);
            request.Data.Properties.Title = FileManager.loader.GetString("ShareDialog-Title");
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
            List<Sound> soundsToDownload = new List<Sound>();

            if (Dav.IsLoggedIn)
            {
                // Get all sounds that need to be downloaded
                foreach (var sound in FileManager.itemViewHolder.SelectedSounds)
                {
                    var downloadStatus = sound.GetAudioFileDownloadStatus();

                    if (
                        downloadStatus == TableObjectFileDownloadStatus.Downloading
                        || downloadStatus == TableObjectFileDownloadStatus.NotDownloaded
                    ) soundsToDownload.Add(sound);
                }
            }

            if (soundsToDownload.Count > 0)
            {
                downloadFilesCanceled = false;

                var downloadFilesDialog = new DownloadFilesDialog(soundsToDownload, soundFileDownloadProgressTemplate, listViewItemStyle);
                downloadFilesDialog.CloseButtonClick += DownloadFilesContentDialog_CloseButtonClick;
                await downloadFilesDialog.ShowAsync();

                if (downloadFilesCanceled)
                {
                    downloadFilesCanceled = false;
                    return false;
                }
            }

            return true;
        }

        private void DownloadFilesContentDialog_CloseButtonClick(Dialog sender, ContentDialogButtonClickEventArgs args)
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

            var setCategoriesDialog = new SetCategoriesDialog(selectedSounds);
            setCategoriesDialog.PrimaryButtonClick += SetCategoriesContentDialog_PrimaryButtonClick;
            await setCategoriesDialog.ShowAsync();
        }

        private async void SetCategoriesContentDialog_PrimaryButtonClick(Dialog sender, ContentDialogButtonClickEventArgs args)
        {
            var dialog = sender as SetCategoriesDialog;
            FileManager.itemViewHolder.LoadingScreenMessage = FileManager.loader.GetString("UpdateSoundsMessage");
            FileManager.itemViewHolder.LoadingScreenVisible = true;

            // Get the selected categories
            List<Guid> categoryUuids = new List<Guid>();
            foreach (var item in dialog.SelectedItems)
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
            MenuFlyoutItem createSubCategoryFlyoutItem = new MenuFlyoutItem { Text = FileManager.loader.GetString("CategoryOptionsFlyout-AddSubCategory") };
            createSubCategoryFlyoutItem.Click += CreateSubCategoryFlyoutItem_Click;
            flyout.Items.Add(createSubCategoryFlyoutItem);

            // Position
            MenuFlyoutSubItem positionSubFlyoutItem = new MenuFlyoutSubItem { Text = FileManager.loader.GetString("CategoryOptionsFlyout-Position") };

            FontIcon arrowTop = new FontIcon { Glyph = "\uE74A" };
            FontIcon arrowBottom = new FontIcon { Glyph = "\uE74B" };
            FontIcon arrowTopLeft = new FontIcon { Glyph = "\uE742" };
            FontIcon arrowTopRight = new FontIcon { Glyph = "\uE742", FlowDirection = FlowDirection.RightToLeft, MirroredWhenRightToLeft = true };
            FontIcon arrowBottomLeft = new FontIcon { Glyph = "\uE741", FlowDirection = FlowDirection.RightToLeft, MirroredWhenRightToLeft = true };
            FontIcon arrowBottomRight = new FontIcon { Glyph = "\uE741" };

            // Move up
            MenuFlyoutItem moveUpFlyoutItem = new MenuFlyoutItem { Text = FileManager.loader.GetString("CategoryOptionsFlyout-Position-MoveUp") };
            moveUpFlyoutItem.Click += MoveUpFlyoutItem_Click;

            // Move down
            MenuFlyoutItem moveDownFlyoutItem = new MenuFlyoutItem { Text = FileManager.loader.GetString("CategoryOptionsFlyout-Position-MoveDown") };
            moveDownFlyoutItem.Click += MoveDownFlyoutItem_Click;

            // Move to category above
            MenuFlyoutItem moveToCategoryAboveItem = new MenuFlyoutItem { Text = FileManager.loader.GetString("CategoryOptionsFlyout-Position-MoveToCategoryAbove") };
            moveToCategoryAboveItem.Click += MoveToCategoryAboveItem_Click;

            // Move to category below
            MenuFlyoutItem moveToCategoryBelowItem = new MenuFlyoutItem { Text = FileManager.loader.GetString("CategoryOptionsFlyout-Position-MoveToCategoryBelow") };
            moveToCategoryBelowItem.Click += MoveToCategoryBelowItem_Click;

            // Move to parent category
            MenuFlyoutItem moveToParentCategoryItem = new MenuFlyoutItem { Text = FileManager.loader.GetString("CategoryOptionsFlyout-Position-MoveToParentCategory") };

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
            var newSubCategoryDialog = new NewCategoryDialog(true);
            newSubCategoryDialog.PrimaryButtonClick += NewSubCategoryContentDialog_PrimaryButtonClick;
            await newSubCategoryDialog.ShowAsync();
        }

        private async void NewSubCategoryContentDialog_PrimaryButtonClick(Dialog sender, ContentDialogButtonClickEventArgs args)
        {
            var dialog = sender as NewCategoryDialog;

            Guid categoryUuid = await FileManager.CreateCategoryAsync(null, selectedCategory, dialog.Name, dialog.Icon);
            Category newCategory = await FileManager.GetCategoryAsync(categoryUuid, false);
            
            // Add the category to the categories list
            FileManager.AddCategory(newCategory, selectedCategory);

            // Add the category to the MenuItems of the SideBar
            AddCategoryMenuItem(menuItems, newCategory, selectedCategory);

            // Navigate to the new category
            await FileManager.ShowCategoryAsync(newCategory.Uuid);

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

            var template = (DataTemplate)Resources["DialogSoundListItemTemplate"];
            var listViewItemStyle = Resources["ListViewItemStyle"] as Style;

            var exportSoundsDialog = new ExportSoundsDialog(sounds, template, listViewItemStyle);
            exportSoundsDialog.PrimaryButtonClick += ExportSoundsContentDialog_PrimaryButtonClick;
            await exportSoundsDialog.ShowAsync();
        }

        private async void ExportSoundsContentDialog_PrimaryButtonClick(Dialog sender, ContentDialogButtonClickEventArgs args)
        {
            var dialog = sender as ExportSoundsDialog;

            // Disable multi-selection mode
            FileManager.itemViewHolder.MultiSelectionEnabled = false;

            await FileManager.ExportSoundsAsync(dialog.Sounds, dialog.ExportSoundsAsZip, dialog.ExportSoundsFolder);
        }
        #endregion

        #region Delete Sounds
        private async void DeleteSoundsButton_Click(object sender, RoutedEventArgs e)
        {
            var deleteSoundsDialog = new DeleteSoundsDialog();
            deleteSoundsDialog.PrimaryButtonClick += DeleteSoundsContentDialog_PrimaryButtonClick;
            await deleteSoundsDialog.ShowAsync();
        }

        private async void DeleteSoundsContentDialog_PrimaryButtonClick(Dialog sender, ContentDialogButtonClickEventArgs args)
        {
            FileManager.itemViewHolder.LoadingScreenMessage = FileManager.loader.GetString("DeleteSoundsMessage");
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

            FileManager.UpdatePlayAllButtonVisibility();

            // Clear selected sounds list
            FileManager.itemViewHolder.SelectedSounds.Clear();
            FileManager.itemViewHolder.LoadingScreenVisible = false;

            // Disable multi-selection mode
            FileManager.itemViewHolder.MultiSelectionEnabled = false;
        }
        #endregion

        #region Store search
        private void StoreSearchAutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.QueryText.Length > 0)
            {
                NavigateToPage(
                    typeof(StoreSearchPage),
                    args.QueryText,
                    new DrillInNavigationTransitionInfo()
                );
            }
        }

        private void StoreSearchButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToPage(
                typeof(StoreSearchPage),
                null,
                new DrillInNavigationTransitionInfo()
            );
        }
        #endregion

        #region Publish sounds
        private async void PublishSoundsButton_Click(object sender, RoutedEventArgs e)
        {
            SentrySdk.CaptureMessage("MainPage-PublishSoundsButton-Click", scope =>
            {
                scope.SetTag("IsLoggedIn", Dav.IsLoggedIn.ToString());
            });

            if (Dav.IsLoggedIn)
                NavigateToPage(typeof(StoreProfilePage), null, new DrillInNavigationTransitionInfo());
            else
            {
                var publishSoundsLoginDialog = new PublishSoundsLoginDialog();
                publishSoundsLoginDialog.PrimaryButtonClick += PublishSoundsLoginDialog_PrimaryButtonClick;
                await publishSoundsLoginDialog.ShowAsync();
            }
        }

        private async void PublishSoundsLoginDialog_PrimaryButtonClick(Dialog sender, ContentDialogButtonClickEventArgs args)
        {
            bool result = await AccountPage.ShowLoginPage();

            SentrySdk.CaptureMessage("MainPage-PublishSoundsLoginDialog-PrimaryButtonClick", scope =>
            {
                scope.SetTag("Result", result.ToString());
            });
            
            if (result)
            {
                // Wait for the user data to load
                for (int i = 0; i < 5; i++)
                {
                    await Task.Delay(2000);
                    if (Dav.User.Id != 0) break;
                }

                if (Dav.User.Id != 0)
                    NavigateToPage(typeof(StoreProfilePage), null, new DrillInNavigationTransitionInfo());
            }
        }
        #endregion
    }
}