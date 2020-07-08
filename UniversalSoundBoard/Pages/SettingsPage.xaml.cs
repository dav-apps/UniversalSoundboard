﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UniversalSoundBoard.Common;
using UniversalSoundBoard.DataAccess;
using Windows.UI.Notifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace UniversalSoundBoard.Pages
{
    public sealed partial class SettingsPage : Page
    {
        bool initialized = false;

        public SettingsPage()
        {
            InitializeComponent();
            FileManager.itemViewHolder.ThemeChangedEvent += ItemViewHolder_ThemeChangedEvent;
        }

        private async void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            ContentRoot.DataContext = FileManager.itemViewHolder;
            SetThemeColors();
            await FileManager.SetSoundBoardSizeTextAsync();
        }
        
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // Init the settings UI elements
            SetLiveTileToggle();
            SetPlayingSoundsListVisibilityToggle();
            SetPlayOneSoundAtOnceToggle();
            SetShowListViewToggle();
            SetShowCategoryIconToggle();
            SetShowAcrylicBackgroundToggle();
            SetShowSoundsPivotToggle();
            SetThemeRadioButton();
            SetSavePlayingSoundsToggle();
            SetSoundOrderComboBox();
            SetSoundOrderReversedComboBox();
            initialized = true;
        }

        private void ItemViewHolder_ThemeChangedEvent(object sender, EventArgs e)
        {
            SetThemeColors();
        }

        private void SetThemeColors()
        {
            RequestedTheme = FileManager.GetRequestedTheme();
            SolidColorBrush appThemeColorBrush = new SolidColorBrush(FileManager.GetApplicationThemeColor());
            ContentRoot.Background = appThemeColorBrush;
            SettingsGeneralStackPanel.Background = appThemeColorBrush;
            SettingsDesignStackPanel.Background = appThemeColorBrush;
            SettingsDataStackPanel.Background = appThemeColorBrush;
        }

        #region LiveTile
        private void SetLiveTileToggle()
        {
            LiveTileToggle.IsOn = FileManager.itemViewHolder.LiveTileEnabled;
        }

        private void LiveTileToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (!initialized) return;
            FileManager.itemViewHolder.LiveTileEnabled = LiveTileToggle.IsOn;

            if (LiveTileToggle.IsOn)
                Task.Run(FileManager.UpdateLiveTileAsync);
            else
                TileUpdateManager.CreateTileUpdaterForApplication().Clear();
        }
        #endregion

        #region PlayingSoundsListVisibility
        private void SetPlayingSoundsListVisibilityToggle()
        {
            PlayingSoundsListToggle.IsOn = FileManager.itemViewHolder.PlayingSoundsListVisible;
        }

        private async void PlayingSoundsListToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (!initialized) return;
            FileManager.itemViewHolder.PlayingSoundsListVisible = PlayingSoundsListToggle.IsOn;

            SavePlayingSoundsStackPanel.Visibility = PlayingSoundsListToggle.IsOn ? Visibility.Visible : Visibility.Collapsed;

            await FileManager.AddOrRemoveAllPlayingSoundsAsync();
        }
        #endregion

        #region PlayOneSoundAtOnce
        private void SetPlayOneSoundAtOnceToggle()
        {
            PlayOneSoundAtOnceToggle.IsOn = FileManager.itemViewHolder.PlayOneSoundAtOnce;
        }

        private void PlayOneSoundAtOnceToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (!initialized) return;
            FileManager.itemViewHolder.PlayOneSoundAtOnce = PlayOneSoundAtOnceToggle.IsOn;
        }
        #endregion

        #region ShowListView
        private void SetShowListViewToggle()
        {
            ShowListViewToggle.IsOn = FileManager.itemViewHolder.ShowListView;
        }

        private void ListViewToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (!initialized) return;
            FileManager.itemViewHolder.ShowListView = ShowListViewToggle.IsOn;
        }
        #endregion

        #region ShowCategories
        private void SetShowCategoryIconToggle()
        {
            ShowCategoryToggle.IsOn = FileManager.itemViewHolder.ShowCategoryIcon;
        }

        private void ShowCategoryToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (!initialized) return;
            FileManager.itemViewHolder.ShowCategoryIcon = ShowCategoryToggle.IsOn;
        }
        #endregion

        #region ShowAcrylicBackground
        private void SetShowAcrylicBackgroundToggle()
        {
            ShowAcrylicBackgroundToggle.IsOn = FileManager.itemViewHolder.ShowAcrylicBackground;
        }

        private void ShowAcrylicBackgroundToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (!initialized) return;
            FileManager.itemViewHolder.ShowAcrylicBackground = ShowAcrylicBackgroundToggle.IsOn;

            // Update the UI
            FileManager.UpdateLayoutColors();
        }
        #endregion

        #region ShowSoundsPivot
        private void SetShowSoundsPivotToggle()
        {
            ShowSoundsPivotToggle.IsOn = FileManager.itemViewHolder.ShowSoundsPivot;
        }

        private void ShowSoundsPivotToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (!initialized) return;
            FileManager.itemViewHolder.ShowSoundsPivot = ShowSoundsPivotToggle.IsOn;
        }
        #endregion

        #region Theme
        private void SetThemeRadioButton()
        {
            switch (FileManager.itemViewHolder.Theme)
            {
                case FileManager.AppTheme.Light:
                    LightThemeRadioButton.IsChecked = true;
                    break;
                case FileManager.AppTheme.Dark:
                    DarkThemeRadioButton.IsChecked = true;
                    break;
                case FileManager.AppTheme.System:
                    SystemThemeRadioButton.IsChecked = true;
                    break;
            }
        }

        private void ThemeRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (!initialized) return;
            FileManager.AppTheme themeBefore = FileManager.itemViewHolder.CurrentTheme;
            RadioButton radioButton = sender as RadioButton;

            if (radioButton == LightThemeRadioButton)
            {
                FileManager.itemViewHolder.Theme = FileManager.AppTheme.Light;
                FileManager.itemViewHolder.CurrentTheme = FileManager.AppTheme.Light;
            }
            else if (radioButton == DarkThemeRadioButton)
            {
                FileManager.itemViewHolder.Theme = FileManager.AppTheme.Dark;
                FileManager.itemViewHolder.CurrentTheme = FileManager.AppTheme.Dark;
            }
            else if (radioButton == SystemThemeRadioButton)
            {
                FileManager.itemViewHolder.Theme = FileManager.AppTheme.System;
                FileManager.itemViewHolder.CurrentTheme = (App.Current as App).RequestedTheme == ApplicationTheme.Dark ? FileManager.AppTheme.Dark : FileManager.AppTheme.Light;
            }

            // Call the theme updated event if the theme has changed
            if (FileManager.itemViewHolder.CurrentTheme != themeBefore)
            {
                FileManager.itemViewHolder.TriggerThemeChangedEvent();
                SetThemeColors();
            }
        }
        #endregion

        #region SavePlayingSounds
        private void SetSavePlayingSoundsToggle()
        {
            SavePlayingSoundsToggle.IsOn = FileManager.itemViewHolder.SavePlayingSounds;
            SavePlayingSoundsStackPanel.Visibility = FileManager.itemViewHolder.PlayingSoundsListVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void SavePlayingSoundsToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (!initialized) return;
            FileManager.itemViewHolder.SavePlayingSounds = SavePlayingSoundsToggle.IsOn;

            await FileManager.AddOrRemoveAllPlayingSoundsAsync();
        }
        #endregion

        #region SoundOrder
        private void SetSoundOrderComboBox()
        {
            SoundOrderComboBox.SelectedIndex = (int)FileManager.itemViewHolder.SoundOrder;
        }

        private void SetSoundOrderReversedComboBox()
        {
            SoundOrderReversedComboBox.SelectedIndex = FileManager.itemViewHolder.SoundOrderReversed ? 1 : 0;

            // Disable the combo box if custom sound order is selected
            if (FileManager.itemViewHolder.SoundOrder == FileManager.SoundOrder.Custom)
                SoundOrderReversedComboBox.IsEnabled = false;
        }
        #endregion

        #region Events
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

            foreach (var category in ContentDialogs.CategoryOrderList)
                uuids.Add(category.Uuid);

            //await FileManager.SetCategoryOrderAsync(uuids);
            await FileManager.CreateCategoriesListAsync();
        }

        private void SoundOrderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!initialized) return;
            FileManager.itemViewHolder.SoundOrder = (FileManager.SoundOrder)SoundOrderComboBox.SelectedIndex;

            SoundOrderReversedComboBox.IsEnabled = FileManager.itemViewHolder.SoundOrder != FileManager.SoundOrder.Custom;
            if (FileManager.itemViewHolder.SoundOrder == FileManager.SoundOrder.Custom)
                FileManager.itemViewHolder.AllSoundsChanged = true;
        }

        private void SoundOrderReversedComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!initialized) return;
            FileManager.itemViewHolder.SoundOrderReversed = SoundOrderReversedComboBox.SelectedIndex != 0;
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
        #endregion
    }
}
