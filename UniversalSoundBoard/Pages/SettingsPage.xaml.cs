using System;
using UniversalSoundboard.Common;
using UniversalSoundboard.DataAccess;
using Windows.ApplicationModel.Resources;
using Windows.UI.Notifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace UniversalSoundboard.Pages
{
    public sealed partial class SettingsPage : Page
    {
        readonly ResourceLoader loader = new ResourceLoader();
        bool initialized = false;
        string soundboardSize = "";

        public SettingsPage()
        {
            InitializeComponent();
            FileManager.itemViewHolder.PropertyChanged += ItemViewHolder_PropertyChanged;
        }

        private async void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            ContentRoot.DataContext = FileManager.itemViewHolder;
            SetThemeColors();
            InitSettings();

            await FileManager.CalculateSoundboardSizeAsync();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            FileManager.itemViewHolder.PropertyChanged -= ItemViewHolder_PropertyChanged;

            base.OnNavigatedFrom(e);
        }

        #region Functionality
        private void SetThemeColors()
        {
            RequestedTheme = FileManager.GetRequestedTheme();
            SolidColorBrush appThemeColorBrush = new SolidColorBrush(FileManager.GetApplicationThemeColor());
            ContentRoot.Background = appThemeColorBrush;
            SettingsGeneralStackPanel.Background = appThemeColorBrush;
            SettingsDesignStackPanel.Background = appThemeColorBrush;
            SettingsDataStackPanel.Background = appThemeColorBrush;
        }

        private void InitSettings()
        {
            // Init the settings UI elements
            SetShowPlayingSoundsListToggle();
            SetSavePlayingSoundsToggle();
            SetOpenMultipleSoundsToggle();
            SetMultiSoundPlaybackToggle();
            SetShowSoundsPivotToggle();
            SetSoundOrderComboBox();
            SetSoundOrderReversedComboBox();
            SetUseStandardOutputDeviceToggle();
            SetShowListViewToggle();
            SetShowCategoriesIconsToggle();
            SetShowAcrylicBackgroundToggle();
            SetLiveTileToggle();
            SetThemeRadioButton();
            initialized = true;
        }

        private void UpdateSoundboardSizeText()
        {
            if (FileManager.itemViewHolder.SoundboardSize == 0)
                soundboardSize = "";
            else
                soundboardSize = string.Format(loader.GetString("SettingsSoundBoardSize"), FileManager.GetFormattedSize(FileManager.itemViewHolder.SoundboardSize));

            Bindings.Update();
        }
        #endregion

        #region ShowPlayingSoundsList
        private void SetShowPlayingSoundsListToggle()
        {
            ShowPlayingSoundsListToggle.IsOn = FileManager.itemViewHolder.PlayingSoundsListVisible;
        }

        private void ShowPlayingSoundsListToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (!initialized) return;
            FileManager.itemViewHolder.PlayingSoundsListVisible = ShowPlayingSoundsListToggle.IsOn;

            SavePlayingSoundsStackPanel.Visibility = ShowPlayingSoundsListToggle.IsOn ? Visibility.Visible : Visibility.Collapsed;
            OpenMultipleSoundsStackPanel.Visibility = ShowPlayingSoundsListToggle.IsOn ? Visibility.Visible : Visibility.Collapsed;
            UpdateMultiSoundPlaybackVisibility();
        }
        #endregion

        #region SavePlayingSounds
        private void SetSavePlayingSoundsToggle()
        {
            SavePlayingSoundsToggle.IsOn = FileManager.itemViewHolder.SavePlayingSounds;
            SavePlayingSoundsStackPanel.Visibility = FileManager.itemViewHolder.PlayingSoundsListVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SavePlayingSoundsToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (!initialized) return;
            FileManager.itemViewHolder.SavePlayingSounds = SavePlayingSoundsToggle.IsOn;
        }
        #endregion

        #region OpenMultipleSounds
        private void SetOpenMultipleSoundsToggle()
        {
            OpenMultipleSoundsToggle.IsOn = FileManager.itemViewHolder.OpenMultipleSounds;
            OpenMultipleSoundsStackPanel.Visibility = FileManager.itemViewHolder.PlayingSoundsListVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OpenMultipleSoundsToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (!initialized) return;
            FileManager.itemViewHolder.OpenMultipleSounds = OpenMultipleSoundsToggle.IsOn;

            UpdateMultiSoundPlaybackVisibility();
        }
        #endregion

        #region MultiSoundPlayback
        private void SetMultiSoundPlaybackToggle()
        {
            MultiSoundPlaybackToggle.IsOn = FileManager.itemViewHolder.MultiSoundPlayback;
            UpdateMultiSoundPlaybackVisibility();
        }

        private void MultiSoundPlaybackToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (!initialized) return;
            FileManager.itemViewHolder.MultiSoundPlayback = MultiSoundPlaybackToggle.IsOn;
        }

        private void UpdateMultiSoundPlaybackVisibility()
        {
            MultiSoundPlaybackStackPanel.Visibility = !FileManager.itemViewHolder.PlayingSoundsListVisible || (FileManager.itemViewHolder.PlayingSoundsListVisible && FileManager.itemViewHolder.OpenMultipleSounds) ? Visibility.Visible : Visibility.Collapsed;
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

        #region SoundOrder
        private void SetSoundOrderComboBox()
        {
            SoundOrderComboBox.SelectedIndex = (int)FileManager.itemViewHolder.SoundOrder;
        }

        private void SetSoundOrderReversedComboBox()
        {
            SoundOrderReversedComboBox.SelectedIndex = FileManager.itemViewHolder.SoundOrderReversed ? 1 : 0;
            UpdateSoundOrderReversedComboBoxVisibility();
        }

        private void SoundOrderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!initialized) return;
            FileManager.itemViewHolder.SoundOrder = (FileManager.SoundOrder)SoundOrderComboBox.SelectedIndex;

            if (FileManager.itemViewHolder.SoundOrder == FileManager.SoundOrder.Custom)
                FileManager.itemViewHolder.AllSoundsChanged = true;

            UpdateSoundOrderReversedComboBoxVisibility();
        }

        private void SoundOrderReversedComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!initialized) return;
            FileManager.itemViewHolder.SoundOrderReversed = SoundOrderReversedComboBox.SelectedIndex != 0;
        }

        private void UpdateSoundOrderReversedComboBoxVisibility()
        {
            // Hide the combo box if custom sound order is selected
            SoundOrderReversedComboBox.Visibility = FileManager.itemViewHolder.SoundOrder == FileManager.SoundOrder.Custom ? Visibility.Collapsed : Visibility.Visible;
        }
        #endregion

        #region Output device
        private void SetUseStandardOutputDeviceToggle()
        {
            UseStandardOutputDeviceToggle.IsOn = FileManager.itemViewHolder.UseStandardOutputDevice;
        }

        private void UseStandardOutputDeviceToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (!initialized) return;
            FileManager.itemViewHolder.UseStandardOutputDevice = UseStandardOutputDeviceToggle.IsOn;
        }
        #endregion

        #region ShowListView
        private void SetShowListViewToggle()
        {
            ShowListViewToggle.IsOn = FileManager.itemViewHolder.ShowListView;
        }

        private void ShowListViewToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (!initialized) return;
            FileManager.itemViewHolder.ShowListView = ShowListViewToggle.IsOn;
        }
        #endregion

        #region ShowCategoriesIcons
        private void SetShowCategoriesIconsToggle()
        {
            ShowCategoriesIconsToggle.IsOn = FileManager.itemViewHolder.ShowCategoriesIcons;
        }

        private void ShowCategoriesIconsToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (!initialized) return;
            FileManager.itemViewHolder.ShowCategoriesIcons = ShowCategoriesIconsToggle.IsOn;
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

        #region LiveTile
        private void SetLiveTileToggle()
        {
            LiveTileToggle.IsOn = FileManager.itemViewHolder.LiveTile;
        }

        private void LiveTileToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (!initialized) return;
            FileManager.itemViewHolder.LiveTile = LiveTileToggle.IsOn;

            if (LiveTileToggle.IsOn)
                FileManager.UpdateLiveTileAsync();
            else
                TileUpdateManager.CreateTileUpdaterForApplication().Clear();
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
        }
        #endregion

        #region Event handlers
        private void ItemViewHolder_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals(ItemViewHolder.CurrentThemeKey))
                SetThemeColors();
            else if (e.PropertyName.Equals(ItemViewHolder.SoundboardSizeKey))
                UpdateSoundboardSizeText();
        }

        private async void ExportDataButton_Click(object sender, RoutedEventArgs e)
        {
            Style buttonRevealStyle = Resources["ButtonRevealStyle"] as Style;
            var ExportDataContentDialog = ContentDialogs.CreateExportDataContentDialog(buttonRevealStyle);
            ExportDataContentDialog.PrimaryButtonClick += ExportDataContentDialog_PrimaryButtonClickAsync;
            await ExportDataContentDialog.ShowAsync();
        }

        private async void ExportDataContentDialog_PrimaryButtonClickAsync(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            await FileManager.ExportDataAsync(ContentDialogs.ExportFolder);
        }

        private async void ImportDataButton_Click(object sender, RoutedEventArgs e)
        {
            Style buttonRevealStyle = Resources["ButtonRevealStyle"] as Style;
            var ImportDataContentDialog = ContentDialogs.CreateImportDataContentDialog(buttonRevealStyle);
            ImportDataContentDialog.PrimaryButtonClick += ImportDataContentDialog_PrimaryButtonClick;
            await ImportDataContentDialog.ShowAsync();
        }

        private async void ImportDataContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            await FileManager.ImportDataAsync(ContentDialogs.ImportFile, false);
        }
        #endregion
    }
}
