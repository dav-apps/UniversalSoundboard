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

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace UniversalSoundBoard
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        List<string> Suggestions;
        ObservableCollection<Category> Categories;
        ObservableCollection<Setting> SettingsListing;

        public MainPage()
        {
            this.InitializeComponent();
            Loaded += MainPage_Loaded;
            SystemNavigationManager.GetForCurrentView().BackRequested += onBackRequested;
            Suggestions = new List<string>();
            CreateCategoriesObservableCollection();

            SettingsListing = new ObservableCollection<Setting>();

            //SettingsListing.Add(new Setting { Icon = "\uE2AF", Text = "Log in" });
            SettingsListing.Add(new Setting { Icon = "\uE713", Text = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("Settings-Title"), Id = "Settings" });
        }

        async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            setDataContext();
            (App.Current as App)._itemViewHolder.page = typeof(SoundPage);
            await SoundManager.GetAllSounds();
            initializeLocalSettings();
            await initializePushNotificationSettings();
        }

        private void setDataContext()
        {
            ContentRoot.DataContext = (App.Current as App)._itemViewHolder;
        }

        private async void CreateCategoriesObservableCollection()
        {
            Categories = new ObservableCollection<Category>();
            Categories.Add(new Category { Name = "Home", Icon = "\uE10F" });
            await FileManager.GetCategoriesListAsync();
            foreach(Category cat in (App.Current as App)._itemViewHolder.categories)
            {
                Categories.Add(cat);
            }
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

        private void HamburgerButton_Click(object sender, RoutedEventArgs e)
        {
            SideBar.IsPaneOpen = !SideBar.IsPaneOpen;
        }

        private async void SearchAutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            string text = sender.Text;

            if ((App.Current as App)._itemViewHolder.page == typeof(SettingsPage))
            {
                (App.Current as App)._itemViewHolder.page = typeof(SoundPage);
            }

            if (String.IsNullOrEmpty(text))
            {
                await SoundManager.GetAllSounds();
                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;
                MenuItemsListView.SelectedItem = Categories.First();
                (App.Current as App)._itemViewHolder.title = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("AllSounds");
            }else
            {
                (App.Current as App)._itemViewHolder.title = text;
                (App.Current as App)._itemViewHolder.searchQuery = text;
                SoundManager.GetSoundsByName(text);
                Suggestions = (App.Current as App)._itemViewHolder.sounds.Where(p => p.Name.ToLower().StartsWith(text.ToLower())).Select(p => p.Name).ToList();
                SearchAutoSuggestBox.ItemsSource = Suggestions;
                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
                (App.Current as App)._itemViewHolder.editButtonVisibility = Visibility.Collapsed;
            }
        }

        private async void SearchAutoSuggestBox_TextChanged_Mobile(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            string text = sender.Text;

            if((App.Current as App)._itemViewHolder.page != typeof(SoundPage))
            {
                (App.Current as App)._itemViewHolder.page = typeof(SoundPage);
            }

            if (String.IsNullOrEmpty(text))
            {
                (App.Current as App)._itemViewHolder.title = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("AllSounds");
                MenuItemsListView.SelectedItem = Categories.First();
                await SoundManager.GetAllSounds();
            }else
            {
                (App.Current as App)._itemViewHolder.title = text;
                (App.Current as App)._itemViewHolder.searchQuery = text;

                Suggestions = (App.Current as App)._itemViewHolder.sounds.Where(p => p.Name.StartsWith(text)).Select(p => p.Name).ToList();
                SearchAutoSuggestBox.ItemsSource = Suggestions;
                (App.Current as App)._itemViewHolder.editButtonVisibility = Visibility.Collapsed;
                SoundManager.GetSoundsByName(text);
            }
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
            }
            
            (App.Current as App)._itemViewHolder.searchQuery = text;
            (App.Current as App)._itemViewHolder.editButtonVisibility = Visibility.Collapsed;
            SoundManager.GetSoundsByName(text);
        }

        private void SearchAutoSuggestBox_QuerySubmitted_Mobile(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if ((App.Current as App)._itemViewHolder.page != typeof(SoundPage))
            {
                (App.Current as App)._itemViewHolder.page = typeof(SoundPage);
            }

            string text = sender.Text;
            (App.Current as App)._itemViewHolder.title = text;
            (App.Current as App)._itemViewHolder.searchQuery = text;
            (App.Current as App)._itemViewHolder.editButtonVisibility = Visibility.Collapsed;
            SoundManager.GetSoundsByName(text);
        }

        private void IconsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private async void goBack()
        {
            (App.Current as App)._itemViewHolder.title = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("AllSounds");
            SearchAutoSuggestBox.Text = "";
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;
            (App.Current as App)._itemViewHolder.editButtonVisibility = Visibility.Collapsed;
            MenuItemsListView.SelectedItem = Categories.First();
            await SoundManager.GetAllSounds();
        }

        private async void goBack_Mobile()
        {
            resetSearchAreaMobile();

            if ((App.Current as App)._itemViewHolder.title != (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("AllSounds"))
            {
                (App.Current as App)._itemViewHolder.title = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("AllSounds");
                await SoundManager.GetAllSounds();
            }else
            {
                MenuItemsListView.SelectedItem = Categories.First();
                await SoundManager.GetAllSounds();
            }
        }

        private void onBackRequested(object sender, BackRequestedEventArgs e)
        {
            if(Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Mobile" && 
                SearchAutoSuggestBox.Visibility == Visibility.Collapsed && 
                (App.Current as App)._itemViewHolder.title == (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("AllSounds"))
            {
                App.Current.Exit();
            }else if((App.Current as App)._itemViewHolder.title != (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("AllSounds")
                && (App.Current as App)._itemViewHolder.multiSelectOptionsVisibility == Visibility.Visible
                && Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Mobile")
            {
                FileManager.resetMultiSelectArea();
            }else
            {
                chooseGoBack();
            }

            e.Handled = true;
        }

        public void chooseGoBack()
        {
            (App.Current as App)._itemViewHolder.selectedSounds.Clear();
            if (Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Mobile")
            {
                if (SearchAutoSuggestBox.Visibility == Visibility.Visible)
                {
                    resetSearchAreaMobile();
                }
                else
                {
                    if((App.Current as App)._itemViewHolder.page != typeof(SoundPage))
                    {
                        (App.Current as App)._itemViewHolder.page = typeof(SoundPage);
                    }
                    goBack_Mobile();
                }
            }
            else
            {
                if ((App.Current as App)._itemViewHolder.page != typeof(SoundPage))
                {
                    (App.Current as App)._itemViewHolder.page = typeof(SoundPage);
                }
                goBack();
            }
        }

        private void resetSearchAreaMobile()
        {
            if (Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Mobile")
            {
                MenuItemsListView.SelectedItem = Categories.First();
                //SearchAutoSuggestBox.Text = "";
                SearchAutoSuggestBox.Visibility = Visibility.Collapsed;
                AddButton.Visibility = Visibility.Visible;
                VolumeButton.Visibility = Visibility.Visible;
                SearchButton.Visibility = Visibility.Visible;
                (App.Current as App)._itemViewHolder.editButtonVisibility = Visibility.Collapsed;
            }
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

            if (files.Count > 0)
            {
                // Application now has read/write access to the picked file(s)
                foreach (StorageFile sound in files)
                {
                    await SoundManager.addSound(sound);
                }
                chooseGoBack();
            }
            AddButton.IsEnabled = true;
            (App.Current as App)._itemViewHolder.progressRingIsActive = false;
        }

        private async void MenuItemsListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            (App.Current as App)._itemViewHolder.selectedSounds.Clear();
            var category = (Category)e.ClickedItem;
            SideBar.IsPaneOpen = false;

            if((App.Current as App)._itemViewHolder.page == typeof(SettingsPage))
            {
                (App.Current as App)._itemViewHolder.page = typeof(SoundPage);
            }

            // Display all Sounds with the selected category
            if (category == Categories.First())
            {
                chooseGoBack();
            }
            else
            {
                resetSearchAreaMobile();

                (App.Current as App)._itemViewHolder.title = WebUtility.HtmlDecode(category.Name);
                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
                (App.Current as App)._itemViewHolder.editButtonVisibility = Visibility.Visible;
                await SoundManager.GetSoundsByCategory(category);
            }
        }

        private void SettingsMenuListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var setting = (Setting)e.ClickedItem;

            if (setting.Id == "Settings")
            {
                (App.Current as App)._itemViewHolder.page = typeof(SettingsPage);
                (App.Current as App)._itemViewHolder.title = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("Settings-Title");
                (App.Current as App)._itemViewHolder.editButtonVisibility = Visibility.Collapsed;
                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;

                // Reset multi select options and selected Sounds list
                FileManager.resetMultiSelectArea();
            }
            /* else if (setting.Text == "Log in")
             {
                 // TODO Add login page
                 SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
                 (App.Current as App)._itemViewHolder.page = typeof(LoginPage);
             }
             */

            SideBar.IsPaneOpen = false;
            resetSearchAreaMobile();
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

        private async void MultiSelectOptionsButton_Delete_Click(object sender, RoutedEventArgs e)
        {
            var deleteSoundsContentDialog = ContentDialogs.CreateDeleteSoundsContentDialogAsync();
            deleteSoundsContentDialog.PrimaryButtonClick += deleteSoundsContentDialog_PrimaryButtonClick;
            await deleteSoundsContentDialog.ShowAsync();
        }

        private void MultiSelectOptionsButton_ChangeCategory_GotFocus(object sender, RoutedEventArgs e)
        {
            createCategoriesFlyout();
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

        private void createCategoriesFlyout()
        {
            MultiSelectOptionsButton_ChangeCategory.Items.Clear();

            foreach (Category category in (App.Current as App)._itemViewHolder.categories)
            {
                var item = new MenuFlyoutItem { Text = category.Name };
                item.Click += MultiSelectOptionsButton_ChangeCategory_Item_Click;

                MultiSelectOptionsButton_ChangeCategory.Items.Add(item);
            }
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
            List<Category> categoriesList = await FileManager.GetCategoriesListAsync();

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

            // Reload page
            this.Frame.Navigate(this.GetType());
        }

        private async void DeleteCategoryContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            await FileManager.deleteCategory((App.Current as App)._itemViewHolder.title);
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;
            (App.Current as App)._itemViewHolder.title = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("AllSounds");
            (App.Current as App)._itemViewHolder.editButtonVisibility = Visibility.Collapsed;

            // Reload page
            this.Frame.Navigate(this.GetType());
        }

        private async void EditCategoryContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // Get categories List and save with new value
            List<Category> categoriesList = await FileManager.GetCategoriesListAsync();

            // Get combobox value
            ComboBoxItem typeItem = (ComboBoxItem)ContentDialogs.IconSelectionComboBox.SelectedItem;
            string icon = typeItem.Content.ToString();

            string newName = ContentDialogs.EditCategoryTextBox.Text;
            string oldName = (App.Current as App)._itemViewHolder.title;

            categoriesList.Find(p => p.Name == oldName).Icon = icon;
            categoriesList.Find(p => p.Name == oldName).Name = newName;

            await FileManager.SaveCategoriesListAsync(categoriesList);
            await FileManager.renameCategory(oldName, newName);

            // Reload page
            this.Frame.Navigate(this.GetType());
            (App.Current as App)._itemViewHolder.title = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("AllSounds");
            (App.Current as App)._itemViewHolder.editButtonVisibility = Visibility.Collapsed;
        }
    }
}
