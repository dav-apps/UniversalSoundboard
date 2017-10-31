using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using UniversalSoundBoard.Model;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using static UniversalSoundBoard.Model.Sound;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UniversalSoundBoard
{
    public sealed partial class NavigationViewHeader : UserControl
    {
        List<string> Suggestions;
        int moreButtonClicked = 0;

        public NavigationViewHeader()
        {
            this.InitializeComponent();
            setDataContext();
            initializeLocalSettings();
            SetDarkThemeLayout();
            FileManager.AdjustLayout();
            Suggestions = new List<string>();
        }

        private void setDataContext()
        {
            ContentRoot.DataContext = (App.Current as App)._itemViewHolder;
        }

        private void initializeLocalSettings()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values["volume"] == null)
            {
                localSettings.Values["volume"] = FileManager.volume;
            }
            VolumeSlider.Value = (double)localSettings.Values["volume"] * 100;
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

        private void createCategoriesFlyout()
        {
            foreach (MenuFlyoutItem item in MoreButton_ChangeCategoryFlyout.Items)
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
                        item.Click += MoreButton_ChangeCategory_Click;
                        item.Text = (App.Current as App)._itemViewHolder.categories.ElementAt(n).Name;
                        MoreButton_ChangeCategoryFlyout.Items.Add(item);
                    }
                    else if (MoreButton_ChangeCategoryFlyout.Items.ElementAt(n - 1) != null)
                    {   // If the element is already there, set the new text
                        ((MenuFlyoutItem)MoreButton_ChangeCategoryFlyout.Items.ElementAt(n - 1)).Text = (App.Current as App)._itemViewHolder.categories.ElementAt(n).Name;
                        ((MenuFlyoutItem)MoreButton_ChangeCategoryFlyout.Items.ElementAt(n - 1)).Visibility = Visibility.Visible;
                    }
                    else
                    {
                        var item = new MenuFlyoutItem();
                        item.Click += MoreButton_ChangeCategory_Click;
                        item.Text = (App.Current as App)._itemViewHolder.categories.ElementAt(n).Name;
                        MoreButton_ChangeCategoryFlyout.Items.Add(item);
                    }
                }
            }
            moreButtonClicked++;
        }

        #region EventHandlers
        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            FileManager.AdjustLayout();
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
                    Sound sound = new Sound(soundFile.DisplayName, category, soundFile);
                    await FileManager.addSound(sound);
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
                    //await ShowAllSounds();
                    //SideBar.SelectedItem = (App.Current as App)._itemViewHolder.categories.First();
                    (App.Current as App)._itemViewHolder.title = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("AllSounds");
                }
                else
                {
                    (App.Current as App)._itemViewHolder.title = text;
                    (App.Current as App)._itemViewHolder.searchQuery = text;
                    SoundManager.GetSoundsByName(text);
                    Suggestions = (App.Current as App)._itemViewHolder.sounds.Where(p => p.Name.ToLower().StartsWith(text.ToLower())).Select(p => p.Name).ToList();
                    SearchAutoSuggestBox.ItemsSource = Suggestions;
                    FileManager.SetBackButtonVisibility(true);
                    (App.Current as App)._itemViewHolder.editButtonVisibility = Visibility.Collapsed;
                }

                //CheckBackButtonVisibility();
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
                SoundManager.GetSoundsByName(text);
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

            if (volumeSlider == VolumeSlider)
            {
                VolumeSlider2.Value = newValue;
            }
            else
            {
                VolumeSlider.Value = newValue;
            }

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

        private async void CategoryEditButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var editCategoryContentDialog = await ContentDialogs.CreateEditCategoryContentDialogAsync();
            editCategoryContentDialog.PrimaryButtonClick += EditCategoryContentDialog_PrimaryButtonClick;
            await editCategoryContentDialog.ShowAsync();
        }

        private async void CategoryDeleteButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var deleteCategoryContentDialog = ContentDialogs.CreateDeleteCategoryContentDialogAsync();
            deleteCategoryContentDialog.PrimaryButtonClick += DeleteCategoryContentDialog_PrimaryButtonClick;
            await deleteCategoryContentDialog.ShowAsync();
        }

        private void PlayAllSoundsSimultaneouslyFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            SoundPage.PlayAllSoundsSimultaneously();
        }

        private void PlayAllSoundsSuccessivelyFlyoutItem_1x_Click(object sender, RoutedEventArgs e)
        {
            SoundPage.StartPlaySoundsSuccessively(1, true);
        }

        private void PlayAllSoundsSuccessivelyFlyoutItem_2x_Click(object sender, RoutedEventArgs e)
        {
            SoundPage.StartPlaySoundsSuccessively(2, true);
        }

        private void PlayAllSoundsSuccessivelyFlyoutItem_5x_Click(object sender, RoutedEventArgs e)
        {
            SoundPage.StartPlaySoundsSuccessively(5, true);
        }

        private void PlayAllSoundsSuccessivelyFlyoutItem_10x_Click(object sender, RoutedEventArgs e)
        {
            SoundPage.StartPlaySoundsSuccessively(10, true);
        }

        private void PlayAllSoundsSuccessivelyFlyoutItem_endless_Click(object sender, RoutedEventArgs e)
        {
            SoundPage.StartPlaySoundsSuccessively(int.MaxValue, true);
        }

        private void PlaySoundsSuccessivelyFlyoutItem_1x_Click(object sender, RoutedEventArgs e)
        {
            SoundPage.StartPlaySoundsSuccessively(1, false);
        }

        private void PlaySoundsSuccessivelyFlyoutItem_2x_Click(object sender, RoutedEventArgs e)
        {
            SoundPage.StartPlaySoundsSuccessively(2, false);
        }

        private void PlaySoundsSuccessivelyFlyoutItem_5x_Click(object sender, RoutedEventArgs e)
        {
            SoundPage.StartPlaySoundsSuccessively(5, false);
        }

        private void PlaySoundsSuccessivelyFlyoutItem_10x_Click(object sender, RoutedEventArgs e)
        {
            SoundPage.StartPlaySoundsSuccessively(10, false);
        }

        private void PlaySoundsSuccessivelyFlyoutItem_endless_Click(object sender, RoutedEventArgs e)
        {
            SoundPage.StartPlaySoundsSuccessively(int.MaxValue, false);
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
            FileManager.switchSelectionMode();
        }

        private void MoreButton_Click(object sender, RoutedEventArgs e)
        {
            createCategoriesFlyout();
        }

        private async void MoreButton_DeleteSoundsFlyout_Click(object sender, RoutedEventArgs e)
        {
            var deleteSoundsContentDialog = ContentDialogs.CreateDeleteSoundsContentDialogAsync();
            deleteSoundsContentDialog.PrimaryButtonClick += deleteSoundsContentDialog_PrimaryButtonClick;
            await deleteSoundsContentDialog.ShowAsync();
        }

        private async void MoreButton_ChangeCategory_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = (MenuFlyoutItem)sender;
            string category = selectedItem.Text;
            foreach (Sound sound in (App.Current as App)._itemViewHolder.selectedSounds)
            {
                await sound.setCategory(await FileManager.GetCategoryByNameAsync(category));
            }
        }

        private void VolumeFlyout_Click(object sender, RoutedEventArgs e)
        {
            VolumeFlyout.ContextFlyout.ShowAt(MoreButton);
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            FileManager.switchSelectionMode();
            FileManager.AdjustLayout();
        }
        #endregion EventHandlers

        #region ContentDialogs
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
            await FileManager.CreateCategoriesObservableCollection();

            // Show new category
            await FileManager.ShowCategory(category);
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
            await FileManager.CreateCategoriesObservableCollection();
            await FileManager.ShowCategory(new Category() { Name = newName, Icon = icon });
        }

        private async void DeleteCategoryContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            await FileManager.deleteCategory((App.Current as App)._itemViewHolder.title);
            FileManager.SetBackButtonVisibility(false);
            (App.Current as App)._itemViewHolder.title = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("AllSounds");
            (App.Current as App)._itemViewHolder.editButtonVisibility = Visibility.Collapsed;

            // Reload page
            await FileManager.CreateCategoriesObservableCollection();
            await FileManager.ShowAllSounds();
        }

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
        #endregion ContentDialogs
    }
}
