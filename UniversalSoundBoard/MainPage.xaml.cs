using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using UniversalSoundBoard.Model;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using static UniversalSoundBoard.Model.Sound;
using Windows.UI.Notifications;
using NotificationsExtensions.Tiles; // NotificationsExtensions.Win10
using NotificationsExtensions;
using Windows.Media.Playback;
using Windows.Media.Core;
using Microsoft.Services.Store.Engagement;
using System.Diagnostics;
using Windows.UI;
using Windows.ApplicationModel.Core;
using Windows.System.Threading;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace UniversalSoundBoard
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        List<string> Suggestions;
        ObservableCollection<Setting> SettingsListing;
        int moreButtonClicked = 0;
        bool skipAutoSuggestBoxTextChanged = false;

        public MainPage()
        {
            this.InitializeComponent();
            Loaded += MainPage_Loaded;
            SystemNavigationManager.GetForCurrentView().BackRequested += onBackRequested;
            Suggestions = new List<string>();

            SettingsListing = new ObservableCollection<Setting>();
            AdjustLayout();
        }

        async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            setDataContext();
            initializeLocalSettings();
            (App.Current as App)._itemViewHolder.page = typeof(SoundPage);
            
            await CreateCategoriesObservableCollection();
            CreateSettingsListing();

            await SoundManager.GetAllSounds();
            await initializePushNotificationSettings();
        }

        private void setDataContext()
        {
            ContentRoot.DataContext = (App.Current as App)._itemViewHolder;
        }

        public static async Task CreateCategoriesObservableCollection()
        {
            (App.Current as App)._itemViewHolder.categories.Clear();
            (App.Current as App)._itemViewHolder.categories.Add(new Category { Name = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("AllSounds"), Icon = "\uE10F" });
            
            foreach(Category cat in await FileManager.GetCategoriesListAsync())
            {
                (App.Current as App)._itemViewHolder.categories.Add(cat);
            }
        }

        private void CreateSettingsListing()
        {
            // Create Settings List
            SettingsListing.Clear();
            //SettingsListing.Add(new Setting { Icon = "\uE2AF", Text = "Log in" });
            SettingsListing.Add(new Setting { Icon = "\uE713", Text = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("Settings-Title"), Id = "Settings" });
        }

        private async Task initializePushNotificationSettings()
        {
            StoreServicesEngagementManager engagementManager = StoreServicesEngagementManager.GetDefault();
            await engagementManager.RegisterNotificationChannelAsync();
        }

        private void initializeLocalSettings()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values["volume"] == null)
            {
                localSettings.Values["volume"] = FileManager.volume;
            }
            VolumeSlider.Value = (double)localSettings.Values["volume"] * 100;


            if (localSettings.Values["playingSoundsListVisible"] == null)
            {
                localSettings.Values["playingSoundsListVisible"] = FileManager.playingSoundsListVisible;
                (App.Current as App)._itemViewHolder.playingSoundsListVisibility = FileManager.playingSoundsListVisible ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                (App.Current as App)._itemViewHolder.playingSoundsListVisibility = (bool)localSettings.Values["playingSoundsListVisible"] ? Visibility.Visible : Visibility.Collapsed;
            }

            if (localSettings.Values["playOneSoundAtOnce"] == null)
            {
                localSettings.Values["playOneSoundAtOnce"] = FileManager.playOneSoundAtOnce;
                (App.Current as App)._itemViewHolder.playOneSoundAtOnce = FileManager.playOneSoundAtOnce;
            }
            else
            {
                (App.Current as App)._itemViewHolder.playOneSoundAtOnce = (bool)localSettings.Values["playOneSoundAtOnce"];
            }

            if (localSettings.Values["liveTile"] == null)
            {
                localSettings.Values["liveTile"] = FileManager.liveTile;
                if (FileManager.liveTile)
                {
                    FileManager.UpdateLiveTile();
                }
            }
            else
            {
                FileManager.UpdateLiveTile();
            }
        }

        private void SetDarkThemeLayout()
        {
            if ((App.Current as App).RequestedTheme == ApplicationTheme.Dark)
            {
                VolumeButton.Background = new SolidColorBrush(Colors.DimGray);
                AddButton.Background = new SolidColorBrush(Colors.DimGray);
                SearchButton.Background = new SolidColorBrush(Colors.DimGray);
                PlaySoundsButton.Background = new SolidColorBrush(Colors.DimGray);
                MultiSelectOptionsButton_More.Background = new SolidColorBrush(Colors.DimGray);
                CancelButton.Background = new SolidColorBrush(Colors.DimGray);
            }
        }

        private void AdjustLayout()
        {
            SetDarkThemeLayout();

            if(Window.Current.Bounds.Width < FileManager.tabletMaxWidth)        // If user is on tablet
            {
                if(Window.Current.Bounds.Width < FileManager.mobileMaxWidth)
                {   // If user is on mobile
                    // Hide title and show in SoundPage
                    TitleStackPanel.Visibility = Visibility.Collapsed;
                }
                else
                {   // If user is on tablet
                    TitleStackPanel.Visibility = Visibility.Visible;
                }
                SideBar.DisplayMode = SplitViewDisplayMode.Overlay;

                if (String.IsNullOrEmpty(SearchAutoSuggestBox.Text))
                {   // Hide search box
                    ResetSearchArea();
                }
                else
                {   // If user is searching
                    // Show only search box
                    SearchAutoSuggestBox.Visibility = Visibility.Visible;
                    SearchButton.Visibility = Visibility.Collapsed;
                    AddButton.Visibility = Visibility.Collapsed;
                    VolumeButton.Visibility = Visibility.Collapsed;
                }
            }
            else        // If User is on Desktop
            {
                TitleStackPanel.Visibility = Visibility.Visible;
                SideBar.DisplayMode = SplitViewDisplayMode.CompactOverlay;

                // Show Other buttons and search box
                SearchAutoSuggestBox.Visibility = Visibility.Visible;
                SearchButton.Visibility = Visibility.Collapsed;
                AddButton.Visibility = Visibility.Visible;
                VolumeButton.Visibility = Visibility.Visible;
            }

            CheckBackButtonVisibility();
        }

        private async Task GoBack()
        {
            if (!AreTopButtonsNormal())
            {
                ResetTopButtons();
            }
            else
            {
                if ((App.Current as App)._itemViewHolder.page == typeof(SettingsPage))
                {   // If Settings Page is visible
                    // Go to All sounds page
                    (App.Current as App)._itemViewHolder.page = typeof(SoundPage);
                    (App.Current as App)._itemViewHolder.title = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("AllSounds");
                    (App.Current as App)._itemViewHolder.editButtonVisibility = Visibility.Collapsed;
                }
                else if ((App.Current as App)._itemViewHolder.title == (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("AllSounds"))
                {   // If SoundPage shows AllSounds
                    CoreApplication.Exit();
                }
                else
                {   // If SoundPage shows Category or search results
                    // Top Buttons are normal, but page shows Category or search results
                    ShowAllSounds();
                }
            }

            CheckBackButtonVisibility();
        }

        private void CheckBackButtonVisibility()
        {
            if (AreTopButtonsNormal() &&
                (App.Current as App)._itemViewHolder.title == (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("AllSounds"))
            {       // Anything is normal, SoundPage shows All Sounds
                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;
            }
            else
            {
                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
            }
        }

        private bool AreTopButtonsNormal()
        {
            if ((App.Current as App)._itemViewHolder.multiSelectOptionsVisibility == Visibility.Visible ||
                    (Window.Current.Bounds.Width < FileManager.tabletMaxWidth && SearchAutoSuggestBox.Visibility == Visibility.Visible))
            {
                return false;
            }
            return true;
        }

        private void ResetTopButtons()
        {
            (App.Current as App)._itemViewHolder.selectedSounds.Clear();
            (App.Current as App)._itemViewHolder.multiSelectOptionsVisibility = Visibility.Collapsed;
            (App.Current as App)._itemViewHolder.normalOptionsVisibility = Visibility.Visible;
            (App.Current as App)._itemViewHolder.selectionMode = ListViewSelectionMode.None;

            ResetSearchArea();
        }

        private void ResetSearchArea()
        {
            if (Window.Current.Bounds.Width < FileManager.tabletMaxWidth)
            {
                // Clear text and show buttons
                SearchAutoSuggestBox.Visibility = Visibility.Collapsed;
                SearchButton.Visibility = Visibility.Visible;
                AddButton.Visibility = Visibility.Visible;
                VolumeButton.Visibility = Visibility.Visible;
            }
            skipAutoSuggestBoxTextChanged = true;
            SearchAutoSuggestBox.Text = "";
        }

        private async Task ShowAllSounds()
        {
            if (AreTopButtonsNormal())
            {
                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;
            }
            skipAutoSuggestBoxTextChanged = true;
            SearchAutoSuggestBox.Text = "";
            MenuItemsListView.SelectedItem = (App.Current as App)._itemViewHolder.categories.First();
            (App.Current as App)._itemViewHolder.editButtonVisibility = Visibility.Collapsed;
            (App.Current as App)._itemViewHolder.title = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("AllSounds");
            await SoundManager.GetAllSounds();
            skipAutoSuggestBoxTextChanged = false;
        }
        
        private async Task ShowCategory(Category category)
        {
            (App.Current as App)._itemViewHolder.title = WebUtility.HtmlDecode(category.Name);
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
            (App.Current as App)._itemViewHolder.editButtonVisibility = Visibility.Visible;
            MenuItemsListView.SelectedItem = category;
            await SoundManager.GetSoundsByCategory(category);
        }

        private void createCategoriesFlyout()
        {
            foreach(MenuFlyoutItem item in MultiSelectOptionsButton_ChangeCategory.Items)
            {   // Make each item invisible
                item.Visibility = Visibility.Collapsed;
            }

            for (int n = 0; n < (App.Current as App)._itemViewHolder.categories.Count; n++)
            {
                if (n != 0)
                {
                    if (moreButtonClicked == 0)
                    {   // Create the Flyout the first time
                        var item = new MenuFlyoutItem();
                        item.Click += MultiSelectOptionsButton_ChangeCategory_Item_Click;
                        item.Text = (App.Current as App)._itemViewHolder.categories.ElementAt(n).Name;
                        MultiSelectOptionsButton_ChangeCategory.Items.Add(item);
                    }
                    else if (MultiSelectOptionsButton_ChangeCategory.Items.ElementAt(n-1) != null)
                    {   // If the element is already there, set the new text
                        ((MenuFlyoutItem)MultiSelectOptionsButton_ChangeCategory.Items.ElementAt(n-1)).Text = (App.Current as App)._itemViewHolder.categories.ElementAt(n).Name;
                        ((MenuFlyoutItem)MultiSelectOptionsButton_ChangeCategory.Items.ElementAt(n - 1)).Visibility = Visibility.Visible;
                    }
                    else
                    {
                        var item = new MenuFlyoutItem();
                        item.Click += MultiSelectOptionsButton_ChangeCategory_Item_Click;
                        item.Text = (App.Current as App)._itemViewHolder.categories.ElementAt(n).Name;
                        MultiSelectOptionsButton_ChangeCategory.Items.Add(item);
                    }
                }
            }
            moreButtonClicked++;
        }

        private void HamburgerButton_Click(object sender, RoutedEventArgs e)
        {
            SideBar.IsPaneOpen = !SideBar.IsPaneOpen;
        }

        private async void SearchAutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (!skipAutoSuggestBoxTextChanged)
            {
                string text = sender.Text;

                if ((App.Current as App)._itemViewHolder.page == typeof(SettingsPage))
                {
                    (App.Current as App)._itemViewHolder.page = typeof(SoundPage);
                }

                if (String.IsNullOrEmpty(text))
                {
                    await ShowAllSounds();
                    MenuItemsListView.SelectedItem = (App.Current as App)._itemViewHolder.categories.First();
                    (App.Current as App)._itemViewHolder.title = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("AllSounds");
                }
                else
                {
                    (App.Current as App)._itemViewHolder.title = text;
                    (App.Current as App)._itemViewHolder.searchQuery = text;
                    SoundManager.GetSoundsByName(text);
                    Suggestions = (App.Current as App)._itemViewHolder.sounds.Where(p => p.Name.ToLower().StartsWith(text.ToLower())).Select(p => p.Name).ToList();
                    SearchAutoSuggestBox.ItemsSource = Suggestions;
                    SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
                    (App.Current as App)._itemViewHolder.editButtonVisibility = Visibility.Collapsed;
                }

                CheckBackButtonVisibility();
            }
            skipAutoSuggestBoxTextChanged = false;
        }

        private void SearchAutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if ((App.Current as App)._itemViewHolder.page != typeof(SoundPage))
            {
                (App.Current as App)._itemViewHolder.page = typeof(SoundPage);
            }

            string text = sender.Text;
            if(String.IsNullOrEmpty(text))
            {
                (App.Current as App)._itemViewHolder.title = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("AllSounds");
            }
            else
            {
                (App.Current as App)._itemViewHolder.title = text;
                (App.Current as App)._itemViewHolder.searchQuery = text;
                (App.Current as App)._itemViewHolder.editButtonVisibility = Visibility.Collapsed;
                SoundManager.GetSoundsByName(text);
            }

            CheckBackButtonVisibility();
        }

        private async void onBackRequested(object sender, BackRequestedEventArgs e)
        {
            await GoBack();

            e.Handled = true;
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
            AddButton.Visibility = Visibility.Collapsed;
            VolumeButton.Visibility = Visibility.Collapsed;
            SearchButton.Visibility = Visibility.Collapsed;
            SearchAutoSuggestBox.Visibility = Visibility.Visible;

            // slightly delay setting focus
            Task.Factory.StartNew(
                () => Dispatcher.RunAsync(CoreDispatcherPriority.Low,
                    () => SearchAutoSuggestBox.Focus(FocusState.Programmatic)));
        }

        private async void NewSoundFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            // Open file explorer
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation =
                Windows.Storage.Pickers.PickerLocationId.MusicLibrary;
            picker.FileTypeFilter.Add(".mp3");
            picker.FileTypeFilter.Add(".wav");

            var files = await picker.PickMultipleFilesAsync();
            (App.Current as App)._itemViewHolder.progressRingIsActive = true;
            AddButton.IsEnabled = false;

            if (files.Any())
            {
                Category category = new Category();
                // Get category if a category is selected
                if ((App.Current as App)._itemViewHolder.title != (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("Settings-Title") &&
                    String.IsNullOrEmpty(SearchAutoSuggestBox.Text) && (App.Current as App)._itemViewHolder.editButtonVisibility == Visibility.Visible)
                {
                    category.Name = (App.Current as App)._itemViewHolder.title;
                }

                // Application now has read/write access to the picked file(s)
                foreach (StorageFile soundFile in files)
                {
                    Sound sound = new Sound(soundFile.DisplayName, "", soundFile);
                    sound.CategoryName = category.Name;
                    await SoundManager.addSound(sound);
                }

                if (String.IsNullOrEmpty(category.Name))
                {
                    await ShowAllSounds();
                }
                else
                {
                    await ShowCategory(category);
                }
            }
            AddButton.IsEnabled = true;
            (App.Current as App)._itemViewHolder.progressRingIsActive = false;
        }

        private async void MenuItemsListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            (App.Current as App)._itemViewHolder.selectedSounds.Clear();
            var category = (Category)e.ClickedItem;
            SideBar.IsPaneOpen = false;

            ResetSearchArea();

            if ((App.Current as App)._itemViewHolder.page == typeof(SettingsPage))
            {
                (App.Current as App)._itemViewHolder.page = typeof(SoundPage);
            }

            // Display all Sounds with the selected category
            if (category == (App.Current as App)._itemViewHolder.categories.First())
            {
                await ShowAllSounds();
            }
            else
            {
                await ShowCategory(category);
            }
        }

        private void SettingsMenuListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var setting = (Setting)e.ClickedItem;
            
            // Reset multi select options and selected Sounds list
            ResetTopButtons();

            if (setting.Id == "Settings")
            {
                (App.Current as App)._itemViewHolder.page = typeof(SettingsPage);
                (App.Current as App)._itemViewHolder.title = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("Settings-Title");
                (App.Current as App)._itemViewHolder.editButtonVisibility = Visibility.Collapsed;
                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
            }
            /* else if (setting.Text == "Log in")
             {
                 // TODO Add login page
                 SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
                 (App.Current as App)._itemViewHolder.page = typeof(LoginPage);
             }
             */

            SideBar.IsPaneOpen = false;
        }

        private void VolumeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            // Change Volume of MediaPlayers
            double addedValue = e.NewValue - e.OldValue;

            foreach(PlayingSound playingSound in (App.Current as App)._itemViewHolder.playingSounds)
            {
                if((playingSound.MediaPlayer.Volume + addedValue / 100) > 1)
                {
                    playingSound.MediaPlayer.Volume = 1;
                }else if ((playingSound.MediaPlayer.Volume + addedValue / 100) < 0)
                {
                    playingSound.MediaPlayer.Volume = 0;
                }
                else
                {
                    playingSound.MediaPlayer.Volume += addedValue / 100;
                }
            }

            // Save new Volume
            var localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["volume"] = (double)VolumeSlider.Value / 100;
        }

        private async void CategoryDeleteButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var deleteCategoryContentDialog = ContentDialogs.CreateDeleteCategoryContentDialogAsync();
            deleteCategoryContentDialog.PrimaryButtonClick += DeleteCategoryContentDialog_PrimaryButtonClick;
            await deleteCategoryContentDialog.ShowAsync();
        }

        private async void CategoryEditButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var editCategoryContentDialog = await ContentDialogs.CreateEditCategoryContentDialogAsync();
            editCategoryContentDialog.PrimaryButtonClick += EditCategoryContentDialog_PrimaryButtonClick;
            await editCategoryContentDialog.ShowAsync();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            FileManager.resetMultiSelectArea();
        }

        private void PlaySoundsSimultaneously_Click(object sender, RoutedEventArgs e)
        {
            bool oldPlayOneSoundAtOnce = (App.Current as App)._itemViewHolder.playOneSoundAtOnce;
            (App.Current as App)._itemViewHolder.playOneSoundAtOnce = false;
            foreach(Sound sound in (App.Current as App)._itemViewHolder.selectedSounds)
            {
                SoundPage.playSound(sound);
            }
            (App.Current as App)._itemViewHolder.playOneSoundAtOnce = oldPlayOneSoundAtOnce;
        }

        private void StartPlaySoundsSuccessively(int rounds)
        {
            SoundPage.playSounds((App.Current as App)._itemViewHolder.selectedSounds, rounds);
        }
        
        private void PlaySoundsSuccessively_1x_Click(object sender, RoutedEventArgs e)
        {
            StartPlaySoundsSuccessively(1);
        }

        private void PlaySoundsSuccessively_2x_Click(object sender, RoutedEventArgs e)
        {
            StartPlaySoundsSuccessively(2);
        }

        private void PlaySoundsSuccessively_5x_Click(object sender, RoutedEventArgs e)
        {
            StartPlaySoundsSuccessively(5);
        }

        private void PlaySoundsSuccessively_10x_Click(object sender, RoutedEventArgs e)
        {
            StartPlaySoundsSuccessively(10);
        }

        private void PlaySoundsSuccessively_endless_Click(object sender, RoutedEventArgs e)
        {
            StartPlaySoundsSuccessively(int.MaxValue);
        }

        private void MultiSelectOptionsButton_More_Click(object sender, RoutedEventArgs e)
        {
            createCategoriesFlyout();
        }

        private async void MultiSelectOptionsButton_ChangeCategory_Item_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = (MenuFlyoutItem)sender;
            string category = selectedItem.Text;
            foreach (Sound sound in (App.Current as App)._itemViewHolder.selectedSounds)
            {
                await sound.setCategory(category);
            }
        }

        private async void MultiSelectOptionsButton_Delete_Click(object sender, RoutedEventArgs e)
        {
            var deleteSoundsContentDialog = ContentDialogs.CreateDeleteSoundsContentDialogAsync();
            deleteSoundsContentDialog.PrimaryButtonClick += deleteSoundsContentDialog_PrimaryButtonClick;
            await deleteSoundsContentDialog.ShowAsync();
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            AdjustLayout();
        }


        // Content Dialog Methods

        private async void deleteSoundsContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // Delete Sounds
            for (int i = 0; i < (App.Current as App)._itemViewHolder.selectedSounds.Count; i++)
            {
                await FileManager.deleteSound((App.Current as App)._itemViewHolder.selectedSounds.ElementAt(i));
            }
            // Clear selected sounds list
            (App.Current as App)._itemViewHolder.selectedSounds.Clear();
            await FileManager.UpdateGridView();
        }

        private async void NewCategoryFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            var newCategoryContentDialog = ContentDialogs.CreateNewCategoryContentDialog();
            newCategoryContentDialog.PrimaryButtonClick += NewCategoryContentDialog_PrimaryButtonClick;
            await newCategoryContentDialog.ShowAsync();
        }

        private async void NewCategoryContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // Get categories List and save with new value
            ObservableCollection<Category> categoriesList = await FileManager.GetCategoriesListAsync();

            // Get combobox value
            ComboBoxItem typeItem = (ComboBoxItem)ContentDialogs.IconSelectionComboBox.SelectedItem;
            string icon = typeItem.Content.ToString();

            Category category = new Category
            {
                Name = ContentDialogs.NewCategoryTextBox.Text,
                Icon = icon
            };

            categoriesList.Add(category);
            await FileManager.SaveCategoriesListAsync(categoriesList);

            // Show new category
            await ShowCategory(category);
            await CreateCategoriesObservableCollection();
        }

        private async void DeleteCategoryContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            await FileManager.deleteCategory((App.Current as App)._itemViewHolder.title);
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;
            (App.Current as App)._itemViewHolder.title = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("AllSounds");
            (App.Current as App)._itemViewHolder.editButtonVisibility = Visibility.Collapsed;

            // Reload page
            await CreateCategoriesObservableCollection();
            await ShowAllSounds();
        }

        private async void EditCategoryContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // Get categories List and save with new value
            ObservableCollection<Category> categoriesList = await FileManager.GetCategoriesListAsync();

            // Get combobox value
            ComboBoxItem typeItem = (ComboBoxItem)ContentDialogs.IconSelectionComboBox.SelectedItem;
            string icon = typeItem.Content.ToString();

            string newName = ContentDialogs.EditCategoryTextBox.Text;
            string oldName = (App.Current as App)._itemViewHolder.title;

            foreach (Category category in categoriesList)
            {
                if (category.Name == oldName)
                {
                    category.Name = newName;
                    category.Icon = icon;
                }
            }

            await FileManager.SaveCategoriesListAsync(categoriesList);
            await FileManager.renameCategory(oldName, newName);

            // Update page
            await CreateCategoriesObservableCollection();
            await ShowCategory(new Category() { Name = newName, Icon = icon });
        }
    }
}