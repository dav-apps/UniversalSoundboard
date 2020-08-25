using System;
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
            FileManager.itemViewHolder.PropertyChanged += ItemViewHolder_PropertyChanged;
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
            SetOpenMultipleSoundsToggle();
            SetMultiSoundPlaybackToggle();
            SetShowListViewToggle();
            SetShowCategoriesIconsToggle();
            SetShowAcrylicBackgroundToggle();
            SetShowSoundsPivotToggle();
            SetThemeRadioButton();
            SetSavePlayingSoundsToggle();
            SetSoundOrderComboBox();
            SetSoundOrderReversedComboBox();
            initialized = true;
        }

        private void ItemViewHolder_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if(e.PropertyName.Equals(ItemViewHolder.CurrentThemeKey))
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

        #region ShowPlayingSoundsList
        private void SetPlayingSoundsListVisibilityToggle()
        {
            ShowPlayingSoundsListToggle.IsOn = FileManager.itemViewHolder.PlayingSoundsListVisible;
        }

        private void ShowPlayingSoundsListToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (!initialized) return;
            FileManager.itemViewHolder.PlayingSoundsListVisible = ShowPlayingSoundsListToggle.IsOn;

            SavePlayingSoundsStackPanel.Visibility = ShowPlayingSoundsListToggle.IsOn ? Visibility.Visible : Visibility.Collapsed;
        }
        #endregion

        #region OpenMultipleSounds
        private void SetOpenMultipleSoundsToggle()
        {
            OpenMultipleSoundsToggle.IsOn = FileManager.itemViewHolder.OpenMultipleSounds;
        }

        private void OpenMultipleSoundsToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (!initialized) return;
            FileManager.itemViewHolder.OpenMultipleSounds = OpenMultipleSoundsToggle.IsOn;
        }
        #endregion

        #region MultiSoundPlayback
        private void SetMultiSoundPlaybackToggle()
        {
            MultiSoundPlaybackToggle.IsOn = FileManager.itemViewHolder.MultiSoundPlayback;
        }

        private void MultiSoundPlaybackToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (!initialized) return;
            FileManager.itemViewHolder.MultiSoundPlayback = MultiSoundPlaybackToggle.IsOn;
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

        #region ShowCategoriesIcons
        private void SetShowCategoriesIconsToggle()
        {
            ShowCategoriesIconsToggle.IsOn = FileManager.itemViewHolder.ShowCategoryIcon;
        }

        private void ShowCategoriesIconsToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (!initialized) return;
            FileManager.itemViewHolder.ShowCategoryIcon = ShowCategoriesIconsToggle.IsOn;
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

            //await FileManager.AddOrRemoveAllPlayingSoundsAsync();
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
