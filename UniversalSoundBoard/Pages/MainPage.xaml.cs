using System;
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
using UniversalSoundboard.Pages;
using Windows.UI.Xaml.Media;
using UniversalSoundBoard.Common;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

namespace UniversalSoundBoard.Pages
{
    public sealed partial class MainPage : Page
    {
        int sideBarCollapsedMaxWidth = FileManager.sideBarCollapsedMaxWidth;
        public static CoreDispatcher dispatcher;

        bool skipVolumeSliderValueChangedEvent = false;
        private bool downloadFileIsExecuting = false;
        private bool downloadFileWasCanceled = false;
        private bool downloadFileThrewError = false;
        int moreButtonClicked = 0;
        public static ObservableCollection<Sound> PlaySoundsList;
        private List<string> Suggestions;
        private List<StorageFile> selectedFiles = new List<StorageFile>();


        public MainPage()
        {
            InitializeComponent();
            Loaded += MainPage_Loaded;
            dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
            PlaySoundsList = new ObservableCollection<Sound>();
            Suggestions = new List<string>();

            CustomiseTitleBar();
            SetDarkThemeLayout();
            AdjustLayout();
        }
        
        async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            SetDataContext();
            SystemNavigationManager.GetForCurrentView().BackRequested += MainPage_BackRequested;

            InitializeLocalSettings();
            (App.Current as App)._itemViewHolder.Page = typeof(SoundPage);
            SideBar.MenuItemsSource = (App.Current as App)._itemViewHolder.Categories;

            InitializeAccountSettings();

            FileManager.CreatePlayingSoundsList();
            await FileManager.ShowAllSounds();
        }
        
        private void SetDataContext()
        {
            RootGrid.DataContext = (App.Current as App)._itemViewHolder;
        }
        
        private void MainPage_BackRequested(object sender, BackRequestedEventArgs e)
        {
            FileManager.GoBack();
            e.Handled = true;
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            AdjustLayout();
        }

        public void InitializeAccountSettings()
        {
            (App.Current as App)._itemViewHolder.LoginMenuItemVisibility = !(App.Current as App)._itemViewHolder.User.IsLoggedIn;
        }
        
        private void InitializeLocalSettings()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values[FileManager.playingSoundsListVisibleKey] == null)
            {
                localSettings.Values[FileManager.playingSoundsListVisibleKey] = FileManager.playingSoundsListVisible;
                (App.Current as App)._itemViewHolder.PlayingSoundsListVisibility = FileManager.playingSoundsListVisible ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                (App.Current as App)._itemViewHolder.PlayingSoundsListVisibility = (bool)localSettings.Values[FileManager.playingSoundsListVisibleKey] ? Visibility.Visible : Visibility.Collapsed;
            }

            if (localSettings.Values[FileManager.playOneSoundAtOnceKey] == null)
            {
                localSettings.Values[FileManager.playOneSoundAtOnceKey] = FileManager.playOneSoundAtOnce;
                (App.Current as App)._itemViewHolder.PlayOneSoundAtOnce = FileManager.playOneSoundAtOnce;
            }
            else
            {
                (App.Current as App)._itemViewHolder.PlayOneSoundAtOnce = (bool)localSettings.Values[FileManager.playOneSoundAtOnceKey];
            }

            if (localSettings.Values[FileManager.liveTileKey] == null)
            {
                localSettings.Values[FileManager.liveTileKey] = FileManager.liveTile;
            }

            if (localSettings.Values[FileManager.showCategoryIconKey] == null)
            {
                localSettings.Values[FileManager.showCategoryIconKey] = FileManager.showCategoryIcon;
                (App.Current as App)._itemViewHolder.ShowCategoryIcon = FileManager.showCategoryIcon;
            }
            else
            {
                (App.Current as App)._itemViewHolder.ShowCategoryIcon = (bool)localSettings.Values[FileManager.showCategoryIconKey];
            }

            if (localSettings.Values[FileManager.showSoundsPivotKey] == null)
            {
                localSettings.Values[FileManager.showSoundsPivotKey] = FileManager.showSoundsPivot;
                (App.Current as App)._itemViewHolder.ShowSoundsPivot = FileManager.showSoundsPivot;
            }
            else
            {
                (App.Current as App)._itemViewHolder.ShowSoundsPivot = (bool)localSettings.Values[FileManager.showSoundsPivotKey];
            }

            if(localSettings.Values[FileManager.savePlayingSoundsKey] == null)
            {
                localSettings.Values[FileManager.savePlayingSoundsKey] = FileManager.savePlayingSounds;
                (App.Current as App)._itemViewHolder.SavePlayingSounds = FileManager.savePlayingSounds;
            }
            else
            {
                (App.Current as App)._itemViewHolder.SavePlayingSounds = (bool)localSettings.Values[FileManager.savePlayingSoundsKey];
            }

            if(localSettings.Values[FileManager.showAcrylicBackgroundKey] == null)
            {
                localSettings.Values[FileManager.showAcrylicBackgroundKey] = FileManager.showAcrylicBackground;
                (App.Current as App)._itemViewHolder.ShowAcrylicBackground = FileManager.showAcrylicBackground;
            }
            else
            {
                (App.Current as App)._itemViewHolder.ShowAcrylicBackground = (bool)localSettings.Values[FileManager.showAcrylicBackgroundKey];
            }

            if (localSettings.Values["volume"] == null)
            {
                localSettings.Values["volume"] = FileManager.volume;
            }
            double volume = (double)localSettings.Values["volume"] * 100;
            VolumeSlider.Value = volume;
            VolumeSlider2.Value = volume;
        }
        
        private void CustomiseTitleBar()
        {
            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;
            ApplicationViewTitleBar titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ButtonForegroundColor = ((App.Current as App).RequestedTheme == ApplicationTheme.Dark) ? Colors.White : Colors.Black;
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
        }

        private void SetDarkThemeLayout()
        {
            Color appThemeColor = FileManager.GetApplicationThemeColor();

            // Set the tint color of the SideBarAcrylicBrush
            (Application.Current.Resources["SideBarAcrylicBrush"] as AcrylicBrush).TintColor = appThemeColor;

            // Set the tint color of the PlayingSoundsBarAcrylicBrush
            (Application.Current.Resources["PlayingSoundsBarAcrylicBrush"] as AcrylicBrush).TintColor = appThemeColor;

            // Set the acrylic background of the sidebar
            Application.Current.Resources["NavigationViewExpandedPaneBackground"] = (AcrylicBrush) Application.Current.Resources["SideBarAcrylicBrush"];

            // Set the background of the sidebar content
            SideBar.Background = new SolidColorBrush(appThemeColor);

            // Set the background of the MediaTransportControls
            (Application.Current.Resources["MediaTransportControlsPanelBackground"] as SolidColorBrush).Color = Colors.Transparent;

            // Set the colors of the NavigationViewHeader elements
            SolidColorBrush buttonBrush = new SolidColorBrush((App.Current as App).RequestedTheme == ApplicationTheme.Dark ? Colors.DimGray : Colors.White);
            VolumeButton.Background = buttonBrush;
            AddButton.Background = buttonBrush;
            SearchButton.Background = buttonBrush;
            PlaySoundsButton.Background = buttonBrush;
            MoreButton.Background = buttonBrush;
            CancelButton.Background = buttonBrush;

            // Set the acrylic background of the PlayingSoundsBar in the NavigationViewHeader
            NavigationViewHeaderAcrylicBackgroundStackPanel.Background = (AcrylicBrush)Application.Current.Resources["PlayingSoundsBarAcrylicBrush"];
        }

        private void AdjustLayout()
        {
            FileManager.AdjustLayout();

            // Workaround for the weird problem with the changing position of the Search box when the volume button is invisible
            if ((App.Current as App)._itemViewHolder.VolumeButtonVisibility)
                SearchAutoSuggestBox.Margin = new Thickness(3, 0, 3, 0);
            else
                SearchAutoSuggestBox.Margin = new Thickness(3, 8, 3, 0);
            
            // Set the margin of the title and the App name when the NavigationView disappears completely
            if (Window.Current.Bounds.Width <= 640)
            {
                TitleStackPanel.Margin = new Thickness(104, 0, 0, 3);
                WindowTitleTextBox.Margin = new Thickness(17, 8, 0, 0);
            }
            else
            {
                TitleStackPanel.Margin = new Thickness(17, 0, 0, 3);
                WindowTitleTextBox.Margin = new Thickness(57, 8, 0, 0);
            }
        }

        #region EventHandlers
        private void SideBar_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
        {
            FileManager.GoBack();
        }
        
        private async void SideBar_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            (App.Current as App)._itemViewHolder.SelectedSounds.Clear();

            FileManager.ResetSearchArea();

            // Display all Sounds with the selected category
            if (args.IsSettingsInvoked == true)
            {
                (App.Current as App)._itemViewHolder.Page = typeof(SettingsPage);
                (App.Current as App)._itemViewHolder.Title = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("Settings-Title");
                (App.Current as App)._itemViewHolder.EditButtonVisibility = Visibility.Collapsed;
                (App.Current as App)._itemViewHolder.PlayAllButtonVisibility = Visibility.Collapsed;
                (App.Current as App)._itemViewHolder.IsBackButtonEnabled = true;
            }
            else
            {
                // Find the selected category in the categories list and set selectedCategory
                var category = (Category)sender.SelectedItem;

                for (int i = 0; i < (App.Current as App)._itemViewHolder.Categories.Count(); i++)
                    if ((App.Current as App)._itemViewHolder.Categories[i].Uuid == category.Uuid)
                        (App.Current as App)._itemViewHolder.SelectedCategory = i;

                if ((App.Current as App)._itemViewHolder.SelectedCategory == 0)
                    await FileManager.ShowAllSounds();
                else
                    await FileManager.ShowCategory(category.Uuid);
            }
        }

        private void LogInMenuItem_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            (App.Current as App)._itemViewHolder.Title = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("Account-Title");
            (App.Current as App)._itemViewHolder.Page = typeof(AccountPage);
            (App.Current as App)._itemViewHolder.EditButtonVisibility = Visibility.Collapsed;
            (App.Current as App)._itemViewHolder.PlayAllButtonVisibility = Visibility.Collapsed;
            (App.Current as App)._itemViewHolder.IsBackButtonEnabled = true;

            if (SideBar.DisplayMode == NavigationViewDisplayMode.Compact ||
                SideBar.DisplayMode == NavigationViewDisplayMode.Minimal)
                SideBar.IsPaneOpen = false;
        }

        private async void CategoryEditButton_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            var editCategoryContentDialog = ContentDialogs.CreateEditCategoryContentDialogAsync();
            editCategoryContentDialog.PrimaryButtonClick += EditCategoryContentDialog_PrimaryButtonClick;
            await editCategoryContentDialog.ShowAsync();
        }

        private async void CategoryDeleteButton_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            var deleteCategoryContentDialog = ContentDialogs.CreateDeleteCategoryContentDialogAsync();
            deleteCategoryContentDialog.PrimaryButtonClick += DeleteCategoryContentDialog_PrimaryButtonClick;
            await deleteCategoryContentDialog.ShowAsync();
        }

        private async void CategoryPlayAllButton_Click(object sender, RoutedEventArgs e)
        {
            PlaySoundsList.Clear();
            // If favourite sounds is selected or Favourite sounds are hiding
            if (SoundPage.soundsPivotSelected || !(App.Current as App)._itemViewHolder.ShowSoundsPivot)
            {
                foreach (Sound sound in (App.Current as App)._itemViewHolder.Sounds)
                {
                    PlaySoundsList.Add(sound);
                }
            }
            else
            {
                foreach (Sound sound in (App.Current as App)._itemViewHolder.FavouriteSounds)
                {
                    PlaySoundsList.Add(sound);
                }
            }

            var template = (DataTemplate)Resources["SoundItemTemplate"];
            var listViewItemStyle = this.Resources["ListViewItemStyle"] as Style;
            var playSoundsSuccessivelyContentDialog = ContentDialogs.CreatePlaySoundsSuccessivelyContentDialog(PlaySoundsList, template, listViewItemStyle);
            playSoundsSuccessivelyContentDialog.PrimaryButtonClick += PlaySoundsSuccessivelyContentDialog_PrimaryButtonClick;
            await playSoundsSuccessivelyContentDialog.ShowAsync();
        }
        
        private void VolumeSlider_ValueChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            var volumeSlider = sender as Slider;
            double newValue = e.NewValue;
            double oldValue = e.OldValue;

            if (!skipVolumeSliderValueChangedEvent)
            {
                // Change Volume of MediaPlayers
                double addedValue = newValue - oldValue;

                foreach (PlayingSound playingSound in (App.Current as App)._itemViewHolder.PlayingSounds)
                {
                    if ((playingSound.MediaPlayer.Volume + addedValue / 100) > 1)
                    {
                        playingSound.MediaPlayer.Volume = 1;
                    }
                    else if ((playingSound.MediaPlayer.Volume + addedValue / 100) < 0)
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
                localSettings.Values["volume"] = volumeSlider.Value / 100;
            }

            skipVolumeSliderValueChangedEvent = true;
            if (volumeSlider == VolumeSlider)
                VolumeSlider2.Value = newValue;
            else
                VolumeSlider.Value = newValue;
            skipVolumeSliderValueChangedEvent = false;
        }
        
        private void VolumeSlider_LostFocus(object sender, RoutedEventArgs e)
        {
            // Save the new volume of all playing sounds
            foreach (PlayingSound playingSound in (App.Current as App)._itemViewHolder.PlayingSounds)
            {
                FileManager.SetVolumeOfPlayingSound(playingSound.Uuid, playingSound.MediaPlayer.Volume);
            }
        }
        
        private async void NewSoundFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            // Open file explorer
            var picker = new Windows.Storage.Pickers.FileOpenPicker
            {
                ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail,
                SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.MusicLibrary
            };
            foreach (var fileType in FileManager.allowedFileTypes)
                picker.FileTypeFilter.Add(fileType);

            var files = await picker.PickMultipleFilesAsync();

            if (files.Any())
            {
                (App.Current as App)._itemViewHolder.LoadingScreenMessage = new Windows.ApplicationModel.Resources.ResourceLoader().GetString("AddSoundsMessage");
                (App.Current as App)._itemViewHolder.LoadingScreenVisibility = true;
                AddButton.IsEnabled = false;

                // Application now has read/write access to the picked file(s)
                foreach (StorageFile soundFile in files)
                {
                    int selectedCategory = (App.Current as App)._itemViewHolder.SelectedCategory;
                    Guid categoryUuid = Guid.Empty;

                    try
                    {
                        categoryUuid = selectedCategory == 0 ? Guid.Empty : (App.Current as App)._itemViewHolder.Categories[selectedCategory].Uuid;
                    }
                    catch (Exception exception)
                    {
                        Debug.WriteLine(exception.Message);
                    }

                    await FileManager.AddSound(Guid.Empty, soundFile.DisplayName, categoryUuid, soundFile);
                    (App.Current as App)._itemViewHolder.AllSoundsChanged = true;
                }

                await FileManager.UpdateGridView();
            }
            AddButton.IsEnabled = true;
            (App.Current as App)._itemViewHolder.LoadingScreenVisibility = false;
        }
        
        private async void NewCategoryFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            var newCategoryContentDialog = ContentDialogs.CreateNewCategoryContentDialog();
            newCategoryContentDialog.PrimaryButtonClick += NewCategoryContentDialog_PrimaryButtonClick;
            await newCategoryContentDialog.ShowAsync();
        }

        private async void SearchAutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (!FileManager.skipAutoSuggestBoxTextChanged)
            {
                string text = sender.Text;

                if ((App.Current as App)._itemViewHolder.Page != typeof(SoundPage))
                {
                    (App.Current as App)._itemViewHolder.Page = typeof(SoundPage);
                }

                if (String.IsNullOrEmpty(text))
                {
                    await FileManager.ShowAllSounds();
                    (App.Current as App)._itemViewHolder.SelectedCategory = 0;
                    (App.Current as App)._itemViewHolder.Title = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("AllSounds");
                }
                else
                {
                    (App.Current as App)._itemViewHolder.Title = text;
                    (App.Current as App)._itemViewHolder.SearchQuery = text;
                    (App.Current as App)._itemViewHolder.SelectedCategory = 0;
                    FileManager.LoadSoundsByName(text);
                    Suggestions = (App.Current as App)._itemViewHolder.AllSounds.Where(p => p.Name.ToLower().StartsWith(text.ToLower())).Select(p => p.Name).ToList();
                    SearchAutoSuggestBox.ItemsSource = Suggestions;
                    (App.Current as App)._itemViewHolder.IsBackButtonEnabled = true;
                    (App.Current as App)._itemViewHolder.EditButtonVisibility = Visibility.Collapsed;
                }
            }
            FileManager.skipAutoSuggestBoxTextChanged = false;
        }

        private void SearchAutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if ((App.Current as App)._itemViewHolder.Page != typeof(SoundPage))
            {
                (App.Current as App)._itemViewHolder.Page = typeof(SoundPage);
            }

            string text = sender.Text;
            if (String.IsNullOrEmpty(text))
            {
                (App.Current as App)._itemViewHolder.Title = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("AllSounds");
            }
            else
            {
                (App.Current as App)._itemViewHolder.Title = text;
                (App.Current as App)._itemViewHolder.SearchQuery = text;
                (App.Current as App)._itemViewHolder.EditButtonVisibility = Visibility.Collapsed;
                FileManager.LoadSoundsByName(text);
            }

            FileManager.CheckBackButtonVisibility();
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            (App.Current as App)._itemViewHolder.IsBackButtonEnabled = true;
            (App.Current as App)._itemViewHolder.SearchButtonVisibility = false;
            (App.Current as App)._itemViewHolder.SearchAutoSuggestBoxVisibility = true;

            // slightly delay setting focus
            Task.Factory.StartNew(
                () => Dispatcher.RunAsync(CoreDispatcherPriority.Low,
                    () => SearchAutoSuggestBox.Focus(FocusState.Programmatic)));
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            FileManager.SwitchSelectionMode();
        }

        private async void ShareButton_Click(object sender, RoutedEventArgs e)
        {
            if (!await DownloadSelectedFiles()) return;

            // Copy the files into the temp folder
            selectedFiles.Clear();
            StorageFolder tempFolder = ApplicationData.Current.TemporaryFolder;
            foreach (Sound sound in (App.Current as App)._itemViewHolder.SelectedSounds)
            {
                StorageFile audioFile = await sound.GetAudioFile();
                if (audioFile == null) return;
                string ext = sound.GetAudioFileExtension();

                if (string.IsNullOrEmpty(ext))
                    ext = "mp3";

                StorageFile tempFile = await audioFile.CopyAsync(tempFolder, sound.Name + "." + ext, NameCollisionOption.ReplaceExisting);
                selectedFiles.Add(tempFile);
            }

            DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested += DataTransferManager_DataRequested;
            DataTransferManager.ShowShareUI();
        }

        private void DataTransferManager_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            if (selectedFiles.Count == 0) return;

            var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
            string description = loader.GetString("ShareDialog-MultipleSounds");
            if (selectedFiles.Count == 1)
                description = selectedFiles.First().Name;

            DataRequest request = args.Request;
            request.Data.SetStorageItems(selectedFiles);
            request.Data.Properties.Title = loader.GetString("ShareDialog-Title");
            request.Data.Properties.Description = description;
        }

        private void DownloadFileContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            downloadFileWasCanceled = true;
            downloadFileIsExecuting = false;
        }

        private void PlaySoundsSimultaneouslyFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            bool oldPlayOneSoundAtOnce = (App.Current as App)._itemViewHolder.PlayOneSoundAtOnce;
            (App.Current as App)._itemViewHolder.PlayOneSoundAtOnce = false;
            foreach (Sound sound in (App.Current as App)._itemViewHolder.SelectedSounds)
            {
                SoundPage.PlaySound(sound);
            }
            (App.Current as App)._itemViewHolder.PlayOneSoundAtOnce = oldPlayOneSoundAtOnce;
        }

        private async void PlaySoundsSuccessivelyFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            PlaySoundsList.Clear();
            foreach (Sound sound in (App.Current as App)._itemViewHolder.SelectedSounds)
                PlaySoundsList.Add(sound);

            var template = (DataTemplate)Resources["SoundItemTemplate"];
            var listViewItemStyle = Resources["ListViewItemStyle"] as Style;
            var playSoundsSuccessivelyContentDialog = ContentDialogs.CreatePlaySoundsSuccessivelyContentDialog(PlaySoundsList, template, listViewItemStyle);
            playSoundsSuccessivelyContentDialog.PrimaryButtonClick += PlaySoundsSuccessivelyContentDialog_PrimaryButtonClick;
            await playSoundsSuccessivelyContentDialog.ShowAsync();
        }

        private void MoreButton_Click(object sender, RoutedEventArgs e)
        {
            CreateCategoriesFlyout();
            moreButtonClicked++;
        }

        private async void MoreButton_ChangeCategory_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = (MenuFlyoutItem)sender;
            string uuidString = selectedItem.Tag.ToString();
            Guid uuid = FileManager.ConvertStringToGuid(uuidString);

            foreach (Sound sound in (App.Current as App)._itemViewHolder.SelectedSounds)
            {
                // Set the category of the sound
                FileManager.SetCategoryOfSound(sound.Uuid, uuid);
            }
            await FileManager.UpdateGridView();
        }

        private void VolumeFlyout_Click(object sender, RoutedEventArgs e)
        {
            VolumeFlyout.ContextFlyout.ShowAt(MoreButton);
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            FileManager.SwitchSelectionMode();
            FileManager.AdjustLayout();
        }

        private void MoreButton_SelectAllFlyout_Click(object sender, RoutedEventArgs e)
        {
            (App.Current as App)._itemViewHolder.TriggerSelectAllSoundsEvent(sender, e);
        }

        private async void MoreButton_ExportFlyout_Click(object sender, RoutedEventArgs e)
        {
            if (!await DownloadSelectedFiles()) return;

            ObservableCollection<Sound> sounds = new ObservableCollection<Sound>();
            foreach (var sound in (App.Current as App)._itemViewHolder.SelectedSounds)
                sounds.Add(sound);

            var template = (DataTemplate)Resources["SoundItemTemplate"];
            var listViewItemStyle = Resources["ListViewItemStyle"] as Style;
            var exportSoundsContentDialog = ContentDialogs.CreateExportSoundsContentDialog(sounds, template, listViewItemStyle);
            exportSoundsContentDialog.PrimaryButtonClick += ExportSoundsContentDialog_PrimaryButtonClick;
            await exportSoundsContentDialog.ShowAsync();
        }

        private async void MoreButton_DeleteSoundsFlyout_Click(object sender, RoutedEventArgs e)
        {
            var deleteSoundsContentDialog = ContentDialogs.CreateDeleteSoundsContentDialogAsync();
            deleteSoundsContentDialog.PrimaryButtonClick += DeleteSoundsContentDialog_PrimaryButtonClick;
            await deleteSoundsContentDialog.ShowAsync();
        }
        #endregion

        #region ContentDialogs
        private async void EditCategoryContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // Get categories List and save with new value
            // Get combobox value
            ComboBoxItem typeItem = (ComboBoxItem)ContentDialogs.IconSelectionComboBox.SelectedItem;
            string icon = typeItem.Content.ToString();
            string newName = ContentDialogs.EditCategoryTextBox.Text;

            Category oldCategory = (App.Current as App)._itemViewHolder.Categories[(App.Current as App)._itemViewHolder.SelectedCategory];
            Category newCategory = new Category(oldCategory.Uuid, newName, icon);

            FileManager.UpdateCategory(newCategory.Uuid, newCategory.Name, newCategory.Icon);
            (App.Current as App)._itemViewHolder.Title = newName;

            // Update page
            (App.Current as App)._itemViewHolder.AllSoundsChanged = true;
            await FileManager.UpdateGridView();
        }

        private async void DeleteCategoryContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            try
            {
                FileManager.DeleteCategory((App.Current as App)._itemViewHolder.Categories[(App.Current as App)._itemViewHolder.SelectedCategory].Uuid);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
            (App.Current as App)._itemViewHolder.AllSoundsChanged = true;

            // Reload page
            await FileManager.ShowAllSounds();
        }

        private void PlaySoundsSuccessivelyContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            List<Sound> sounds = ContentDialogs.SoundsList.ToList();
            bool randomly = (bool)ContentDialogs.RandomCheckBox.IsChecked;
            int rounds = int.MaxValue;
            if (ContentDialogs.RepeatsComboBox.SelectedItem != ContentDialogs.RepeatsComboBox.Items.Last())
            {
                int.TryParse(ContentDialogs.RepeatsComboBox.SelectedValue.ToString(), out rounds);
            }

            SoundPage.PlaySounds(sounds, rounds, randomly);
        }

        private async void NewCategoryContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // Get combobox value
            ComboBoxItem typeItem = (ComboBoxItem)ContentDialogs.IconSelectionComboBox.SelectedItem;
            string icon = typeItem.Content.ToString();

            Category category = new Category
            {
                Name = ContentDialogs.NewCategoryTextBox.Text,
                Icon = icon
            };

            FileManager.AddCategory(Guid.Empty, category.Name, category.Icon);
            FileManager.CreateCategoriesList();

            // Show new category
            await FileManager.ShowCategory((App.Current as App)._itemViewHolder.Categories.Last().Uuid);
        }

        private async void ExportSoundsContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            List<Sound> soundsList = new List<Sound>();
            foreach (var sound in ContentDialogs.SoundsList)
                soundsList.Add(sound);

            await FileManager.ExportSounds(soundsList, ContentDialogs.ExportSoundsAsZipCheckBox.IsChecked.Value, ContentDialogs.ExportSoundsFolder);
        }

        private async void DeleteSoundsContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            (App.Current as App)._itemViewHolder.LoadingScreenMessage = new Windows.ApplicationModel.Resources.ResourceLoader().GetString("DeleteSoundsMessage");
            (App.Current as App)._itemViewHolder.LoadingScreenVisibility = true;

            // Delete Sounds
            List<Guid> soundUuids = new List<Guid>();
            for (int i = 0; i < (App.Current as App)._itemViewHolder.SelectedSounds.Count; i++)
                soundUuids.Add((App.Current as App)._itemViewHolder.SelectedSounds.ElementAt(i).Uuid);

            if (soundUuids.Count != 0)
                await FileManager.DeleteSoundsAsync(soundUuids);

            // Clear selected sounds list
            (App.Current as App)._itemViewHolder.SelectedSounds.Clear();
            (App.Current as App)._itemViewHolder.LoadingScreenVisibility = false;

            await FileManager.UpdateGridView();
        }
        #endregion

        private async Task<bool> DownloadSelectedFiles()
        {
            // Check if all sounds are available locally
            var selectedSounds = (App.Current as App)._itemViewHolder.SelectedSounds;
            foreach (var sound in selectedSounds)
            {
                var downloadStatus = sound.GetAudioFileDownloadStatus();
                if (downloadStatus == DownloadStatus.NoFileOrNotLoggedIn) continue;

                if (downloadStatus != DownloadStatus.Downloaded)
                {
                    // Download the file and show the download dialog
                    downloadFileIsExecuting = true;
                    Progress<int> progress = new Progress<int>(FileDownloadProgress);
                    sound.DownloadFile(progress);

                    ContentDialogs.CreateDownloadFileContentDialog(sound.Name + "." + sound.GetAudioFileExtension());
                    ContentDialogs.downloadFileProgressBar.IsIndeterminate = true;
                    ContentDialogs.DownloadFileContentDialog.SecondaryButtonClick += DownloadFileContentDialog_SecondaryButtonClick;
                    await ContentDialogs.DownloadFileContentDialog.ShowAsync();
                }

                if (downloadFileWasCanceled)
                {
                    downloadFileWasCanceled = false;
                    return false;
                }
                if (downloadFileThrewError) break;
            }

            if (downloadFileThrewError)
            {
                var errorContentDialog = ContentDialogs.CreateDownloadFileErrorContentDialog();
                await errorContentDialog.ShowAsync();
                downloadFileThrewError = false;
                return false;
            }
            return true;
        }

        private void FileDownloadProgress(int value)
        {
            if (!downloadFileIsExecuting) return;

            if (value < 0)
            {
                // There was an error
                downloadFileThrewError = true;
                downloadFileIsExecuting = false;
                ContentDialogs.DownloadFileContentDialog.Hide();
            }
            else if (value > 100)
            {
                // Hide the download dialog
                ContentDialogs.DownloadFileContentDialog.Hide();
            }
        }

        private void CreateCategoriesFlyout()
        {
            if (moreButtonClicked == 0)
            {
                // Add some more invisible MenuFlyoutItems
                for (int i = 0; i < (App.Current as App)._itemViewHolder.Categories.Count + 10; i++)
                {
                    MenuFlyoutItem item = new MenuFlyoutItem { Visibility = Visibility.Collapsed };
                    item.Click += MoreButton_ChangeCategory_Click;
                    MoreButton_ChangeCategoryFlyout.Items.Add(item);
                }
            }

            foreach (MenuFlyoutItem item in MoreButton_ChangeCategoryFlyout.Items)
            {   // Make each item invisible
                item.Visibility = Visibility.Collapsed;
            }

            for (int n = 1; n < (App.Current as App)._itemViewHolder.Categories.Count; n++)
            {
                if (MoreButton_ChangeCategoryFlyout.Items.ElementAt(n - 1) != null)
                {   // If the element is already there, set the new text
                    Category cat = (App.Current as App)._itemViewHolder.Categories.ElementAt(n);
                    FontIcon icon = new FontIcon();
                    icon.Glyph = cat.Icon;

                    ((MenuFlyoutItem)MoreButton_ChangeCategoryFlyout.Items.ElementAt(n - 1)).Text = cat.Name;
                    ((MenuFlyoutItem)MoreButton_ChangeCategoryFlyout.Items.ElementAt(n - 1)).Tag = cat.Uuid;
                    ((MenuFlyoutItem)MoreButton_ChangeCategoryFlyout.Items.ElementAt(n - 1)).Visibility = Visibility.Visible;
                    ((MenuFlyoutItem)MoreButton_ChangeCategoryFlyout.Items.ElementAt(n - 1)).Icon = icon;
                }
                else
                {
                    var item = new MenuFlyoutItem();
                    FontIcon icon = new FontIcon();
                    icon.Glyph = (App.Current as App)._itemViewHolder.Categories.ElementAt(n).Icon;

                    item.Click += MoreButton_ChangeCategory_Click;
                    item.Text = (App.Current as App)._itemViewHolder.Categories.ElementAt(n).Name;
                    item.Tag = (App.Current as App)._itemViewHolder.Categories.ElementAt(n).Uuid;
                    item.Icon = icon;
                    MoreButton_ChangeCategoryFlyout.Items.Add(item);
                }
            }
        }
    }
}