using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UniversalSoundBoard.Common;
using UniversalSoundBoard.DataAccess;
using Windows.Storage;
using Windows.UI.Notifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace UniversalSoundBoard.Pages
{
    public sealed partial class SettingsPage : Page
    {
        static string themeAtBeginning;
        ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        bool initialized = false;

        public SettingsPage()
        {
            InitializeComponent();

            if(string.IsNullOrEmpty(themeAtBeginning))
                themeAtBeginning = (string)localSettings.Values[FileManager.themeKey];
        }
        
        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            SetDataContext();
            await FileManager.SetSoundBoardSizeTextAsync();
        }
        
        private void SetDataContext()
        {
            ContentRoot.DataContext = (App.Current as App)._itemViewHolder;
        }
        
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            SetDarkThemeLayout();
            SetLiveTileToggle();
            SetPlayingSoundsListVisibilityToggle();
            SetPlayOneSoundAtOnceToggle();
            SetShowCategoryIconToggle();
            SetShowAcrylicBackgroundToggle();
            SetShowSoundsPivotToggle();
            SetThemeRadioButton();
            SetSavePlayingSoundsToggle();
            SetSoundOrderComboBox();
            SetSoundOrderReversedComboBox();
            initialized = true;
        }

        private void SetDarkThemeLayout()
        {
            SolidColorBrush appThemeColorBrush = new SolidColorBrush(FileManager.GetApplicationThemeColor());
            ContentRoot.Background = appThemeColorBrush;
            SettingsGeneralStackPanel.Background = appThemeColorBrush;
            SettingsDesignStackPanel.Background = appThemeColorBrush;
            SettingsDataStackPanel.Background = appThemeColorBrush;
        }

        private void SetLiveTileToggle()
        {
            LiveTileToggle.IsOn = (bool)localSettings.Values[FileManager.liveTileKey];
        }
        
        private void SetPlayingSoundsListVisibilityToggle()
        {
            PlayingSoundsListToggle.IsOn = (bool)localSettings.Values[FileManager.playingSoundsListVisibleKey];
        }
        
        private void SetPlayOneSoundAtOnceToggle()
        {
            PlayOneSoundAtOnceToggle.IsOn = (bool)localSettings.Values[FileManager.playOneSoundAtOnceKey];
        }
        
        private void SetShowCategoryIconToggle()
        {
            ShowCategoryToggle.IsOn = (bool)localSettings.Values[FileManager.showCategoryIconKey];
        }

        private void SetShowAcrylicBackgroundToggle()
        {
            ShowAcrylicBackgroundToggle.IsOn = (bool)localSettings.Values[FileManager.showAcrylicBackgroundKey];
        }
        
        private void SetShowSoundsPivotToggle()
        {
            ShowSoundsPivotToggle.IsOn = (bool)localSettings.Values[FileManager.showSoundsPivotKey];
        }

        private void SetThemeRadioButton()
        {
            if (localSettings.Values[FileManager.themeKey] != null)
            {
                switch ((string)localSettings.Values[FileManager.themeKey])
                {
                    case "light":
                        LightThemeRadioButton.IsChecked = true;
                        break;
                    case "dark":
                        DarkThemeRadioButton.IsChecked = true;
                        break;
                    case "system":
                        SystemThemeRadioButton.IsChecked = true;
                        break;
                }
            }

            SetToggleMessageVisibility();
        }

        private void SetSavePlayingSoundsToggle()
        {
            SavePlayingSoundsToggle.IsOn = (bool)localSettings.Values[FileManager.savePlayingSoundsKey];
            SavePlayingSoundsStackPanel.Visibility = (App.Current as App)._itemViewHolder.PlayingSoundsListVisibility;
        }

        private void SetSoundOrderComboBox()
        {
            SoundOrderComboBox.SelectedIndex = (int)localSettings.Values[FileManager.soundOrderKey];
        }

        private void SetSoundOrderReversedComboBox()
        {
            SoundOrderReversedComboBox.SelectedIndex = (bool)localSettings.Values[FileManager.soundOrderReversedKey] ? 1 : 0;

            // Disable the combo box if custom sound order is selected
            if ((App.Current as App)._itemViewHolder.SoundOrder == FileManager.SoundOrder.Custom)
                SoundOrderReversedComboBox.IsEnabled = false;
        }

        private void SetToggleMessageVisibility()
        {
            ThemeChangeMessageTextBlock.Visibility = themeAtBeginning != (string)localSettings.Values[FileManager.themeKey] ? Visibility.Visible : Visibility.Collapsed;
        }
        
        private void LiveTileToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (!initialized) return;
            localSettings.Values[FileManager.liveTileKey] = LiveTileToggle.IsOn;
            if (!LiveTileToggle.IsOn)
                TileUpdateManager.CreateTileUpdaterForApplication().Clear();
            else
                Task.Run(FileManager.UpdateLiveTileAsync);
        }
        
        private async void PlayingSoundsListToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (!initialized) return;
            localSettings.Values[FileManager.playingSoundsListVisibleKey] = PlayingSoundsListToggle.IsOn;
            (App.Current as App)._itemViewHolder.PlayingSoundsListVisibility = PlayingSoundsListToggle.IsOn ? Visibility.Visible : Visibility.Collapsed;

            SavePlayingSoundsStackPanel.Visibility = PlayingSoundsListToggle.IsOn ? Visibility.Visible : Visibility.Collapsed;

            await FileManager.AddOrRemoveAllPlayingSoundsAsync();
        }
        
        private void PlayOneSoundAtOnceToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (!initialized) return;
            localSettings.Values[FileManager.playOneSoundAtOnceKey] = PlayOneSoundAtOnceToggle.IsOn;
            (App.Current as App)._itemViewHolder.PlayOneSoundAtOnce = PlayOneSoundAtOnceToggle.IsOn;
        }
        
        private void ThemeRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (!initialized) return;
            RadioButton radioButton = sender as RadioButton;
            if(radioButton == LightThemeRadioButton)
                localSettings.Values[FileManager.themeKey] = "light";
            else if (radioButton == DarkThemeRadioButton)
                localSettings.Values[FileManager.themeKey] = "dark";
            else if (radioButton == SystemThemeRadioButton)
                localSettings.Values[FileManager.themeKey] = "system";

            SetToggleMessageVisibility();
        }
        
        private void ShowCategoryToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (!initialized) return;
            localSettings.Values[FileManager.showCategoryIconKey] = ShowCategoryToggle.IsOn;
            (App.Current as App)._itemViewHolder.ShowCategoryIcon = ShowCategoryToggle.IsOn;
        }

        private void ShowAcrylicBackgroundToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (!initialized) return;
            localSettings.Values[FileManager.showAcrylicBackgroundKey] = ShowAcrylicBackgroundToggle.IsOn;
            (App.Current as App)._itemViewHolder.ShowAcrylicBackground = ShowAcrylicBackgroundToggle.IsOn;
            
            // Update the UI
            FileManager.UpdateLayoutColors();
        }

        private void ShowSoundsPivotToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (!initialized) return;
            localSettings.Values[FileManager.showSoundsPivotKey] = ShowSoundsPivotToggle.IsOn;
            (App.Current as App)._itemViewHolder.ShowSoundsPivot = ShowSoundsPivotToggle.IsOn;
        }

        private async void SavePlayingSoundsToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (!initialized) return;
            localSettings.Values[FileManager.savePlayingSoundsKey] = SavePlayingSoundsToggle.IsOn;
            (App.Current as App)._itemViewHolder.SavePlayingSounds = SavePlayingSoundsToggle.IsOn;

            await FileManager.AddOrRemoveAllPlayingSoundsAsync();
        }

        private async void ChangeCategoryOrderButton_Click(object sender, RoutedEventArgs e)
        {
            // Show the CategoryOrderContentDialog
            var itemTemplate = (DataTemplate)Resources["CategoryOrderItemTemplate"];
            var CategoryOrderContentDialog = ContentDialogs.CreateCategoryOrderContentDialog(itemTemplate);
            CategoryOrderContentDialog.PrimaryButtonClick += CategoryOrderContentDialog_PrimaryButtonClick;
            await CategoryOrderContentDialog.ShowAsync();
        }

        private async void CategoryOrderContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // Save the new order of the categories
            List<Guid> uuids = new List<Guid>();

            foreach(var category in ContentDialogs.CategoryOrderList)
                uuids.Add(category.Uuid);

            await FileManager.SetCategoryOrderAsync(uuids);
            await FileManager.CreateCategoriesListAsync();
        }

        private void SoundOrderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!initialized) return;
            localSettings.Values[FileManager.soundOrderKey] = SoundOrderComboBox.SelectedIndex;
            (App.Current as App)._itemViewHolder.SoundOrder = (FileManager.SoundOrder)SoundOrderComboBox.SelectedIndex;

            SoundOrderReversedComboBox.IsEnabled = (App.Current as App)._itemViewHolder.SoundOrder != FileManager.SoundOrder.Custom;
        }

        private void SoundOrderReversedComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!initialized) return;
            localSettings.Values[FileManager.soundOrderReversedKey] = SoundOrderReversedComboBox.SelectedIndex != 0;
            (App.Current as App)._itemViewHolder.SoundOrderReversed = SoundOrderReversedComboBox.SelectedIndex != 0;
        }

        private async void ExportDataButton_Click(object sender, RoutedEventArgs e)
        {
            var ExportDataContentDialog = ContentDialogs.CreateExportDataContentDialog();
            ExportDataContentDialog.PrimaryButtonClick += ExportDataContentDialog_PrimaryButtonClickAsync;

            await ExportDataContentDialog.ShowAsync();
        }
        
        private async void ExportDataContentDialog_PrimaryButtonClickAsync(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            await FileManager.ExportDataAsync(ContentDialogs.ExportFolder);
        }
        
        private async void ImportDataButton_Click(object sender, RoutedEventArgs e)
        {
            var ImportDataContentDialog = ContentDialogs.CreateImportDataContentDialog();
            ImportDataContentDialog.PrimaryButtonClick += ImportDataContentDialog_PrimaryButtonClick;
            await ImportDataContentDialog.ShowAsync();
        }
        
        private async void ImportDataContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            await FileManager.ImportDataAsync(ContentDialogs.ImportFile);
        }
    }
}
