using System.Linq;
using UniversalSoundBoard.Models;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI;
using Windows.ApplicationModel.Core;
using Windows.UI.ViewManagement;
using UniversalSoundBoard.DataAccess;
using System.Threading.Tasks;
using System.Diagnostics;
using UniversalSoundboard.Pages;
using UniversalSoundboard.DataAccess;
using System;

namespace UniversalSoundBoard.Pages
{
    public sealed partial class MainPage : Page
    {
        int sideBarCollapsedMaxWidth = FileManager.sideBarCollapsedMaxWidth;
        public static CoreDispatcher dispatcher;


        public MainPage()
        {
            InitializeComponent();
            Loaded += MainPage_Loaded;
            SystemNavigationManager.GetForCurrentView().BackRequested += OnBackRequested;
            dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;

            CustomiseTitleBar();
        }
        
        async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            SetDataContext();
            InitializeLocalSettings();
            (App.Current as App)._itemViewHolder.page = typeof(SoundPage);
            SideBar.MenuItemsSource = (App.Current as App)._itemViewHolder.categories;

            InitializeAccountSettings();

            AddSavedPlayingSounds();
            await FileManager.ShowAllSounds();
        }
        
        private void SetDataContext()
        {
            WindowTitleTextBox.DataContext = (App.Current as App)._itemViewHolder;
            SideBar.DataContext = (App.Current as App)._itemViewHolder;
        }

        public async Task InitializeAccountSettings()
        {
            if (!String.IsNullOrEmpty(ApiManager.GetJwt()))
            {
                var user = await ApiManager.GetUser();

                if (user.TotalStorage != 0)
                    (App.Current as App)._itemViewHolder.user = user;

                (App.Current as App)._itemViewHolder.loginMenuItemVisibility = false;
                await ApiManager.SyncSoundboard();
            }
            else
            {
                (App.Current as App)._itemViewHolder.loginMenuItemVisibility = true;
            }
        }

        private async Task AddSavedPlayingSounds()
        {
            foreach (PlayingSound ps in await FileManager.GetAllPlayingSounds())
            {
                if(ps.MediaPlayer != null)
                {
                    //ps.MediaPlayer.AutoPlay = false;
                    (App.Current as App)._itemViewHolder.playingSounds.Add(ps);
                }
            }
        }
        
        private void InitializeLocalSettings()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values[FileManager.playingSoundsListVisibleKey] == null)
            {
                localSettings.Values[FileManager.playingSoundsListVisibleKey] = FileManager.playingSoundsListVisible;
                (App.Current as App)._itemViewHolder.playingSoundsListVisibility = FileManager.playingSoundsListVisible ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                (App.Current as App)._itemViewHolder.playingSoundsListVisibility = (bool)localSettings.Values[FileManager.playingSoundsListVisibleKey] ? Visibility.Visible : Visibility.Collapsed;
            }

            if (localSettings.Values[FileManager.playOneSoundAtOnceKey] == null)
            {
                localSettings.Values[FileManager.playOneSoundAtOnceKey] = FileManager.playOneSoundAtOnce;
                (App.Current as App)._itemViewHolder.playOneSoundAtOnce = FileManager.playOneSoundAtOnce;
            }
            else
            {
                (App.Current as App)._itemViewHolder.playOneSoundAtOnce = (bool)localSettings.Values[FileManager.playOneSoundAtOnceKey];
            }

            if (localSettings.Values[FileManager.liveTileKey] == null)
            {
                localSettings.Values[FileManager.liveTileKey] = FileManager.liveTile;
            }

            if (localSettings.Values[FileManager.showCategoryIconKey] == null)
            {
                localSettings.Values[FileManager.showCategoryIconKey] = FileManager.showCategoryIcon;
                (App.Current as App)._itemViewHolder.showCategoryIcon = FileManager.showCategoryIcon;
            }
            else
            {
                (App.Current as App)._itemViewHolder.showCategoryIcon = (bool)localSettings.Values[FileManager.showCategoryIconKey];
            }

            if (localSettings.Values[FileManager.showSoundsPivotKey] == null)
            {
                localSettings.Values[FileManager.showSoundsPivotKey] = FileManager.showSoundsPivot;
                (App.Current as App)._itemViewHolder.showSoundsPivot = FileManager.showSoundsPivot;
            }
            else
            {
                (App.Current as App)._itemViewHolder.showSoundsPivot = (bool)localSettings.Values[FileManager.showSoundsPivotKey];
            }

            if(localSettings.Values[FileManager.savePlayingSoundsKey] == null)
            {
                localSettings.Values[FileManager.savePlayingSoundsKey] = FileManager.savePlayingSounds;
                (App.Current as App)._itemViewHolder.savePlayingSounds = FileManager.savePlayingSounds;
            }
        }
        
        private void CustomiseTitleBar()
        {
            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;
            ApplicationViewTitleBar titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ButtonForegroundColor = ((App.Current as App).RequestedTheme == ApplicationTheme.Dark) ? Colors.White : Colors.Black;
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
        }
        
        private void OnBackRequested(object sender, BackRequestedEventArgs e)
        {
            FileManager.GoBack();
            e.Handled = true;
        }
        
        private async void SideBar_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            (App.Current as App)._itemViewHolder.selectedSounds.Clear();

            FileManager.ResetSearchArea();

            // Display all Sounds with the selected category
            if (args.IsSettingsInvoked == true)
            {
                (App.Current as App)._itemViewHolder.page = typeof(SettingsPage);
                (App.Current as App)._itemViewHolder.title = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("Settings-Title");
                (App.Current as App)._itemViewHolder.editButtonVisibility = Visibility.Collapsed;
                (App.Current as App)._itemViewHolder.playAllButtonVisibility = Visibility.Collapsed;
                FileManager.SetBackButtonVisibility(true);
            }
            else
            {
                // Find the selected category in the categories list and set selectedCategory
                var category = (Category)args.InvokedItem;
                for(int i = 0; i < (App.Current as App)._itemViewHolder.categories.Count(); i++)
                {
                    if ((App.Current as App)._itemViewHolder.categories[i].Uuid == category.Uuid)
                    {
                        (App.Current as App)._itemViewHolder.selectedCategory = i;
                    }
                }

                if ((App.Current as App)._itemViewHolder.selectedCategory == 0)
                {
                    await FileManager.ShowAllSounds();
                }
                else
                {
                    await FileManager.ShowCategory(category.Uuid);
                }
            }
        }

        private void LogInMenuItem_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            (App.Current as App)._itemViewHolder.title = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("Account-Title");
            (App.Current as App)._itemViewHolder.page = typeof(AccountPage);
            (App.Current as App)._itemViewHolder.editButtonVisibility = Visibility.Collapsed;
            (App.Current as App)._itemViewHolder.playAllButtonVisibility = Visibility.Collapsed;
            FileManager.SetBackButtonVisibility(true);
        }
    }
}