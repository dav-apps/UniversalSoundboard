using System;
using UniversalSoundBoard.Common;
using UniversalSoundBoard.DataAccess;
using Windows.Storage;
using Windows.UI.Notifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace UniversalSoundBoard.Pages
{
    public sealed partial class SettingsPage : Page
    {
        static string themeAtBeginning;

        
        public SettingsPage()
        {
            InitializeComponent();

            if(String.IsNullOrEmpty(themeAtBeginning))
            {
                var localSettings = ApplicationData.Current.LocalSettings;
                themeAtBeginning = (string)localSettings.Values["theme"];
            }
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
            SetLiveTileToggle();
            SetPlayingSoundsListVisibilityToggle();
            SetPlayOneSoundAtOnceToggle();
            SetShowCategoryIconToggle();
            SetShowSoundsPivotToggle();
            SetThemeRadioButton();
        }
        
        private void SetThemeRadioButton()
        {
            var localSettings = ApplicationData.Current.LocalSettings;

            if (localSettings.Values["theme"] != null)
            {
                switch ((string)localSettings.Values["theme"])
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
        
        private void SetLiveTileToggle()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            LiveTileToggle.IsOn = (bool)localSettings.Values["liveTile"];
        }
        
        private void SetPlayingSoundsListVisibilityToggle()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            PlayingSoundsListToggle.IsOn = (bool)localSettings.Values["playingSoundsListVisible"];
        }
        
        private void SetPlayOneSoundAtOnceToggle()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            PlayOneSoundAtOnceToggle.IsOn = (bool)localSettings.Values["playOneSoundAtOnce"];
        }
        
        private void SetShowCategoryIconToggle()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            ShowCategoryToggle.IsOn = (bool)localSettings.Values["showCategoryIcon"];
        }
        
        private void SetShowSoundsPivotToggle()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            ShowSoundsPivotToggle.IsOn = (bool)localSettings.Values["showSoundsPivot"];
        }
        
        private void SetToggleMessageVisibility()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            if (themeAtBeginning != (string)localSettings.Values["theme"])
            {
                ThemeChangeMessageTextBlock.Visibility = Visibility.Visible;
            }
            else
            {
                ThemeChangeMessageTextBlock.Visibility = Visibility.Collapsed;
            }
        }
        
        private void LiveTileToggle_Toggled(object sender, RoutedEventArgs e)
        {
            var localSettings = ApplicationData.Current.LocalSettings;

            // Create a simple setting
            localSettings.Values["liveTile"] = LiveTileToggle.IsOn;
            if (!LiveTileToggle.IsOn)
            {
                TileUpdateManager.CreateTileUpdaterForApplication().Clear();
            }
            else
            {
                FileManager.UpdateLiveTile();
            }
        }
        
        private void PlayingSoundsListToggle_Toggled(object sender, RoutedEventArgs e)
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["playingSoundsListVisible"] = PlayingSoundsListToggle.IsOn;
            (App.Current as App)._itemViewHolder.playingSoundsListVisibility = PlayingSoundsListToggle.IsOn ? Visibility.Visible : Visibility.Collapsed;
        }
        
        private void PlayOneSoundAtOnceToggle_Toggled(object sender, RoutedEventArgs e)
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["playOneSoundAtOnce"] = PlayOneSoundAtOnceToggle.IsOn;
            (App.Current as App)._itemViewHolder.playOneSoundAtOnce = PlayOneSoundAtOnceToggle.IsOn;
        }
        
        private void ThemeRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            var localSettings = ApplicationData.Current.LocalSettings;

            RadioButton radioButton = sender as RadioButton;
            if(radioButton == LightThemeRadioButton)
                localSettings.Values["theme"] = "light";
            else if (radioButton == DarkThemeRadioButton)
                localSettings.Values["theme"] = "dark";
            else if (radioButton == SystemThemeRadioButton)
                localSettings.Values["theme"] = "system";

            SetToggleMessageVisibility();
        }
        
        private void ShowCategoryToggle_Toggled(object sender, RoutedEventArgs e)
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["showCategoryIcon"] = ShowCategoryToggle.IsOn;
            (App.Current as App)._itemViewHolder.showCategoryIcon = ShowCategoryToggle.IsOn;
        }
        
        private void ShowSoundsPivotToggle_Toggled(object sender, RoutedEventArgs e)
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["showSoundsPivot"] = ShowSoundsPivotToggle.IsOn;
            (App.Current as App)._itemViewHolder.showSoundsPivot = ShowSoundsPivotToggle.IsOn;
        }
        
        private async void ExportDataButton_Click(object sender, RoutedEventArgs e)
        {
            var ExportDataContentDialog = ContentDialogs.CreateExportDataContentDialog();
            ExportDataContentDialog.PrimaryButtonClick += ExportDataContentDialog_PrimaryButtonClickAsync;

            await ExportDataContentDialog.ShowAsync();
        }
        
        private async void ExportDataContentDialog_PrimaryButtonClickAsync(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            await FileManager.ExportData(ContentDialogs.ExportFolder);
        }
        
        private async void ImportDataButton_Click(object sender, RoutedEventArgs e)
        {
            var ImportDataContentDialog = ContentDialogs.CreateImportDataContentDialog();
            ImportDataContentDialog.PrimaryButtonClick += ImportDataContentDialog_PrimaryButtonClick;
            await ImportDataContentDialog.ShowAsync();
        }
        
        private async void ImportDataContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            await FileManager.ImportDataZip(ContentDialogs.ImportFile);
        }
    }
}
