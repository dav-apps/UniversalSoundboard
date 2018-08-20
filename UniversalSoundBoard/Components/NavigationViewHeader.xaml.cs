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
            InitializeComponent();
            SetDataContext();
            InitializeLocalSettings();
            SetDarkThemeLayout();
            FileManager.AdjustLayout();
            Suggestions = new List<string>();
            PlaySoundsList = new ObservableCollection<Sound>();
            AdjustLayout();
        }
        
        private void SetDataContext()
        {
            ContentRoot.DataContext = (App.Current as App)._itemViewHolder;
        }

        private void AdjustLayout()
        {
            FileManager.AdjustLayout();

            // Workaround for the weird problem with the changing position of the Search box when the volume button is invisible
            if ((App.Current as App)._itemViewHolder.VolumeButtonVisibility)
                SearchAutoSuggestBox.Margin = new Thickness(10, 3, 0, 0);
            else
                SearchAutoSuggestBox.Margin = new Thickness(10, 11, 0, 0);

            // Dynamic margin of the title
            if (Window.Current.Bounds.Width <= 640)
                TitleStackPanel.Margin = new Thickness(-105, 0, 0, 0);
            else
                TitleStackPanel.Margin = new Thickness(-13, 0, 0, 0);
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

        #region EventHandlers
        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            AdjustLayout();
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
            (App.Current as App)._itemViewHolder.ProgressRingIsActive = true;
            AddButton.IsEnabled = false;

            if (files.Any())
            {
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
            (App.Current as App)._itemViewHolder.ProgressRingIsActive = false;
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
        
        private void VolumeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
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
            foreach(Sound sound in (App.Current as App)._itemViewHolder.SelectedSounds)
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
            if (SoundPage.soundsPivotSelected || !(App.Current as App)._itemViewHolder.ShowSoundsPivot)
            {
                foreach(Sound sound in (App.Current as App)._itemViewHolder.Sounds)
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
        
        private void ShareButton_Click(object sender, RoutedEventArgs e)
        {
            DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested += DataTransferManager_DataRequested;
            DataTransferManager.ShowShareUI();
        }
        
        private async void DataTransferManager_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            if((App.Current as App)._itemViewHolder.SelectedSounds.Count > 0)
            {
                var deferral = args.Request.GetDeferral();
                var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
                List<StorageFile> selectedFiles = new List<StorageFile>();
                StorageFolder tempFolder = ApplicationData.Current.TemporaryFolder;

                // Copy file into the temp folder and share it
                foreach (Sound sound in (App.Current as App)._itemViewHolder.SelectedSounds)
                {
                    StorageFile audioFile = await sound.GetAudioFile();
                    StorageFile tempFile;
                    if (audioFile == null)
                    {
                        audioFile = await StorageFile.CreateStreamedFileFromUriAsync(sound.Uuid.ToString(), sound.GetAudioUri(), null);
                    }
                    tempFile = await audioFile.CopyAsync(tempFolder, sound.Name + "." + sound.GetAudioFileExtension(), NameCollisionOption.ReplaceExisting);
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

            FileManager.AddCategory(Guid.Empty, category.Name, category.Icon);
            FileManager.CreateCategoriesList();

            // Show new category
            await FileManager.ShowCategory((App.Current as App)._itemViewHolder.Categories.Last().Uuid);
        }
        
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
            catch(Exception e)
            {
                Debug.WriteLine(e.Message);
            }
            (App.Current as App)._itemViewHolder.AllSoundsChanged = true;
            
            // Reload page
            await FileManager.ShowAllSounds();
        }
        
        private async void DeleteSoundsContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            (App.Current as App)._itemViewHolder.ProgressRingIsActive = true;

            // Delete Sounds
            List<Guid> soundUuids = new List<Guid>();
            for (int i = 0; i < (App.Current as App)._itemViewHolder.SelectedSounds.Count; i++)
            {
                soundUuids.Add((App.Current as App)._itemViewHolder.SelectedSounds.ElementAt(i).Uuid);
            }
            if (soundUuids.Count != 0)
                FileManager.DeleteSounds(soundUuids);

            // Clear selected sounds list
            (App.Current as App)._itemViewHolder.SelectedSounds.Clear();
            (App.Current as App)._itemViewHolder.ProgressRingIsActive = false;

            await FileManager.UpdateGridView();
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
        #endregion ContentDialogs
    }
}
