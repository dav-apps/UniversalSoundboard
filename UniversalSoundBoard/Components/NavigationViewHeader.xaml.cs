using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using UniversalSoundBoard.Common;
using UniversalSoundBoard.DataAccess;
using UniversalSoundBoard.Models;
using UniversalSoundBoard.Pages;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace UniversalSoundBoard.Components
{
    public sealed partial class NavigationViewHeader : UserControl
    {
        bool skipVolumeSliderValueChangedEvent = false;
        public static ObservableCollection<Sound> PlaySoundsList;
        List<string> Suggestions;
        int moreButtonClicked = 0;

        
        public NavigationViewHeader()
        {
            this.InitializeComponent();
            SetDataContext();
            InitializeLocalSettings();
            SetDarkThemeLayout();
            FileManager.AdjustLayout();
            Suggestions = new List<string>();
            PlaySoundsList = new ObservableCollection<Sound>();
        }
        
        private void SetDataContext()
        {
            ContentRoot.DataContext = (App.Current as App)._itemViewHolder;
        }
        
        private void InitializeLocalSettings()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values["volume"] == null)
            {
                localSettings.Values["volume"] = FileManager.volume;
            }
            double volume = (double)localSettings.Values["volume"] * 100;
            VolumeSlider.Value = volume;
            VolumeSlider2.Value = volume;
        }
        
        private void SetDarkThemeLayout()
        {
            if ((App.Current as App).RequestedTheme == ApplicationTheme.Dark)
            {
                VolumeButton.Background = new SolidColorBrush(Colors.DimGray);
                AddButton.Background = new SolidColorBrush(Colors.DimGray);
                SearchButton.Background = new SolidColorBrush(Colors.DimGray);
                PlaySoundsButton.Background = new SolidColorBrush(Colors.DimGray);
                MoreButton.Background = new SolidColorBrush(Colors.DimGray);
                CancelButton.Background = new SolidColorBrush(Colors.DimGray);
            }
        }
        
        private void CreateCategoriesFlyout()
        {
            if (moreButtonClicked == 0)
            {
                // Add some more invisible MenuFlyoutItems
                for (int i = 0; i < (App.Current as App)._itemViewHolder.categories.Count + 10; i++)
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

            for (int n = 1; n < (App.Current as App)._itemViewHolder.categories.Count; n++)
            {
                if(MoreButton_ChangeCategoryFlyout.Items.ElementAt(n - 1) != null)
                {   // If the element is already there, set the new text
                    Category cat = (App.Current as App)._itemViewHolder.categories.ElementAt(n);
                    ((MenuFlyoutItem)MoreButton_ChangeCategoryFlyout.Items.ElementAt(n - 1)).Text = cat.Name;
                    ((MenuFlyoutItem)MoreButton_ChangeCategoryFlyout.Items.ElementAt(n - 1)).Tag = cat.Uuid;
                    ((MenuFlyoutItem)MoreButton_ChangeCategoryFlyout.Items.ElementAt(n - 1)).Visibility = Visibility.Visible;
                }
                else
                {
                    var item = new MenuFlyoutItem();
                    item.Click += MoreButton_ChangeCategory_Click;
                    item.Text = (App.Current as App)._itemViewHolder.categories.ElementAt(n).Name;
                    item.Tag = (App.Current as App)._itemViewHolder.categories.ElementAt(n).Uuid;
                    MoreButton_ChangeCategoryFlyout.Items.Add(item);
                }
            }
        }

        #region EventHandlers
        
        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            FileManager.AdjustLayout();
        }
        
        private async void NewSoundFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            // Open file explorer
            var picker = new Windows.Storage.Pickers.FileOpenPicker
            {
                ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail,
                SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.MusicLibrary
            };
            picker.FileTypeFilter.Add(".mp3");
            picker.FileTypeFilter.Add(".wav");

            var files = await picker.PickMultipleFilesAsync();
            (App.Current as App)._itemViewHolder.progressRingIsActive = true;
            AddButton.IsEnabled = false;

            if (files.Any())
            {
                // Application now has read/write access to the picked file(s)
                foreach (StorageFile soundFile in files)
                {
                    Sound sound = new Sound(soundFile.DisplayName, (App.Current as App)._itemViewHolder.categories[(App.Current as App)._itemViewHolder.selectedCategory], soundFile);
                    await FileManager.AddSound(sound);
                }

                await FileManager.UpdateGridView();
            }
            AddButton.IsEnabled = true;
            (App.Current as App)._itemViewHolder.progressRingIsActive = false;
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

                if ((App.Current as App)._itemViewHolder.page == typeof(SettingsPage))
                {
                    (App.Current as App)._itemViewHolder.page = typeof(SoundPage);
                }

                if (String.IsNullOrEmpty(text))
                {
                    await FileManager.ShowAllSounds();
                    (App.Current as App)._itemViewHolder.selectedCategory = 0;
                    (App.Current as App)._itemViewHolder.title = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("AllSounds");
                }
                else
                {
                    (App.Current as App)._itemViewHolder.title = text;
                    (App.Current as App)._itemViewHolder.searchQuery = text;
                    (App.Current as App)._itemViewHolder.selectedCategory = 0;
                    FileManager.GetSoundsByName(text);
                    Suggestions = (App.Current as App)._itemViewHolder.allSounds.Where(p => p.Name.ToLower().StartsWith(text.ToLower())).Select(p => p.Name).ToList();
                    SearchAutoSuggestBox.ItemsSource = Suggestions;
                    FileManager.SetBackButtonVisibility(true);
                    (App.Current as App)._itemViewHolder.editButtonVisibility = Visibility.Collapsed;
                }
            }
            FileManager.skipAutoSuggestBoxTextChanged = false;
        }
        
        private void SearchAutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if ((App.Current as App)._itemViewHolder.page != typeof(SoundPage))
            {
                (App.Current as App)._itemViewHolder.page = typeof(SoundPage);
            }

            string text = sender.Text;
            if (String.IsNullOrEmpty(text))
            {
                (App.Current as App)._itemViewHolder.title = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("AllSounds");
            }
            else
            {
                (App.Current as App)._itemViewHolder.title = text;
                (App.Current as App)._itemViewHolder.searchQuery = text;
                (App.Current as App)._itemViewHolder.editButtonVisibility = Visibility.Collapsed;
                FileManager.GetSoundsByName(text);
            }

            FileManager.CheckBackButtonVisibility();
        }
        
        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            FileManager.SetBackButtonVisibility(true);
            (App.Current as App)._itemViewHolder.searchButtonVisibility = false;
            (App.Current as App)._itemViewHolder.searchAutoSuggestBoxVisibility = true;

            // slightly delay setting focus
            Task.Factory.StartNew(
                () => Dispatcher.RunAsync(CoreDispatcherPriority.Low,
                    () => SearchAutoSuggestBox.Focus(FocusState.Programmatic)));
        }
        
        private void VolumeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            var volumeSlider = sender as Slider;
            double newValue = e.NewValue;
            double oldValue = e.OldValue;

            if (!skipVolumeSliderValueChangedEvent)
            {
                // Change Volume of MediaPlayers
                double addedValue = newValue - oldValue;

                foreach (PlayingSound playingSound in (App.Current as App)._itemViewHolder.playingSounds)
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
        
        private async void CategoryEditButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var editCategoryContentDialog = ContentDialogs.CreateEditCategoryContentDialogAsync();
            editCategoryContentDialog.PrimaryButtonClick += EditCategoryContentDialog_PrimaryButtonClick;
            await editCategoryContentDialog.ShowAsync();
        }
        
        private async void CategoryDeleteButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var deleteCategoryContentDialog = ContentDialogs.CreateDeleteCategoryContentDialogAsync();
            deleteCategoryContentDialog.PrimaryButtonClick += DeleteCategoryContentDialog_PrimaryButtonClick;
            await deleteCategoryContentDialog.ShowAsync();
        }
        
        private async void PlaySoundsSuccessivelyFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            PlaySoundsList.Clear();
            foreach(Sound sound in (App.Current as App)._itemViewHolder.selectedSounds)
            {
                PlaySoundsList.Add(sound);
            }

            var template = (DataTemplate)Resources["SoundItemTemplate"];
            var listViewItemStyle = Resources["ListViewItemStyle"] as Style;
            var playSoundsSuccessivelyContentDialog = ContentDialogs.CreatePlaySoundsSuccessivelyContentDialog(PlaySoundsList, template, listViewItemStyle);
            playSoundsSuccessivelyContentDialog.PrimaryButtonClick += PlaySoundsSuccessivelyContentDialog_PrimaryButtonClick;
            await playSoundsSuccessivelyContentDialog.ShowAsync();
        }
        
        private async void CategoryPlayAllButton_Click(object sender, RoutedEventArgs e)
        {
            PlaySoundsList.Clear();
            // If favourite sounds is selected or Favourite sounds are hiding
            if (SoundPage.soundsPivotSelected || !(App.Current as App)._itemViewHolder.showSoundsPivot)
            {
                foreach(Sound sound in (App.Current as App)._itemViewHolder.sounds)
                {
                    PlaySoundsList.Add(sound);
                }
            }
            else
            {
                foreach (Sound sound in (App.Current as App)._itemViewHolder.favouriteSounds)
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
        
        private void PlaySoundsSimultaneouslyFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            bool oldPlayOneSoundAtOnce = (App.Current as App)._itemViewHolder.playOneSoundAtOnce;
            (App.Current as App)._itemViewHolder.playOneSoundAtOnce = false;
            foreach (Sound sound in (App.Current as App)._itemViewHolder.selectedSounds)
            {
                SoundPage.playSound(sound);
            }
            (App.Current as App)._itemViewHolder.playOneSoundAtOnce = oldPlayOneSoundAtOnce;
        }
        
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            FileManager.SwitchSelectionMode();
        }
        
        private void MoreButton_Click(object sender, RoutedEventArgs e)
        {
            CreateCategoriesFlyout();
            moreButtonClicked++;
        }
        
        private async void MoreButton_DeleteSoundsFlyout_Click(object sender, RoutedEventArgs e)
        {
            var deleteSoundsContentDialog = ContentDialogs.CreateDeleteSoundsContentDialogAsync();
            deleteSoundsContentDialog.PrimaryButtonClick += DeleteSoundsContentDialog_PrimaryButtonClick;
            await deleteSoundsContentDialog.ShowAsync();
        }
        
        private async void MoreButton_ChangeCategory_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = (MenuFlyoutItem)sender;
            string uuid = selectedItem.Tag.ToString();
            
            foreach (Sound sound in (App.Current as App)._itemViewHolder.selectedSounds)
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
        
        private void ShareButton_Click(object sender, RoutedEventArgs e)
        {
            DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested += DataTransferManager_DataRequested;
            DataTransferManager.ShowShareUI();
        }
        
        private async void DataTransferManager_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            if((App.Current as App)._itemViewHolder.selectedSounds.Count > 0)
            {
                var deferral = args.Request.GetDeferral();
                var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
                List<StorageFile> selectedFiles = new List<StorageFile>();

                // Copy file into the temp folder and share it
                foreach (Sound sound in (App.Current as App)._itemViewHolder.selectedSounds)
                {
                    StorageFolder tempFolder = ApplicationData.Current.TemporaryFolder;
                    StorageFile tempFile = await sound.AudioFile.CopyAsync(tempFolder, sound.Name + sound.AudioFile.FileType, NameCollisionOption.ReplaceExisting);
                    selectedFiles.Add(tempFile);
                }
                
                string description = loader.GetString("ShareDialog-MultipleSounds");
                if(selectedFiles.Count == 1)
                    description = selectedFiles.First().Name;

                DataRequest request = args.Request;
                request.Data.SetStorageItems(selectedFiles);
                request.Data.Properties.Title = loader.GetString("ShareDialog-Title");
                request.Data.Properties.Description = description;
                deferral.Complete();
            }
        }
        #endregion EventHandlers

        #region ContentDialogs
        
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

            FileManager.AddCategory(category.Name, category.Icon);

            // Show new category
            await FileManager.ShowCategory((App.Current as App)._itemViewHolder.categories.Last().Uuid);
        }
        
        private async void EditCategoryContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // Get categories List and save with new value
            // Get combobox value
            ComboBoxItem typeItem = (ComboBoxItem)ContentDialogs.IconSelectionComboBox.SelectedItem;
            string icon = typeItem.Content.ToString();
            string newName = ContentDialogs.EditCategoryTextBox.Text;

            Category oldCategory = (App.Current as App)._itemViewHolder.categories[(App.Current as App)._itemViewHolder.selectedCategory];
            Category newCategory = new Category(oldCategory.Uuid, newName, icon);

            FileManager.UpdateCategory(newCategory.Uuid, newCategory.Name, newCategory.Icon);
            (App.Current as App)._itemViewHolder.title = newName;

            // Update page
            await FileManager.GetAllSounds();
            await FileManager.ShowCategory(oldCategory.Uuid);
        }
        
        private async void DeleteCategoryContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            FileManager.DeleteCategory((App.Current as App)._itemViewHolder.categories[(App.Current as App)._itemViewHolder.selectedCategory].Uuid);

            // Reload page
            await FileManager.ShowAllSounds();
        }
        
        private async void DeleteSoundsContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            (App.Current as App)._itemViewHolder.progressRingIsActive = true;

            // Delete Sounds
            for (int i = 0; i < (App.Current as App)._itemViewHolder.selectedSounds.Count; i++)
            {
                await FileManager.DeleteSound((App.Current as App)._itemViewHolder.selectedSounds.ElementAt(i).Uuid);
            }
            // Clear selected sounds list
            (App.Current as App)._itemViewHolder.selectedSounds.Clear();
            await FileManager.UpdateGridView();

            (App.Current as App)._itemViewHolder.progressRingIsActive = false;
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

            SoundPage.playSounds(sounds, rounds, randomly);
        }
        #endregion ContentDialogs
    }
}
