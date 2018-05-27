using System;
using System.Diagnostics;
using System.Threading.Tasks;
using UniversalSoundBoard.Common;
using UniversalSoundBoard.DataAccess;
using UniversalSoundBoard.Models;
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
        ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;


        public SettingsPage()
        {
            InitializeComponent();

            if(String.IsNullOrEmpty(themeAtBeginning))
            {
                themeAtBeginning = (string)localSettings.Values[FileManager.themeKey];
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
            SetSavePlayingSoundsToggle();
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
        
        private void SetShowSoundsPivotToggle()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
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
            SavePlayingSoundsStackPanel.Visibility = (App.Current as App)._itemViewHolder.playingSoundsListVisibility;
        }

        private void SetToggleMessageVisibility()
        {
            if (themeAtBeginning != (string)localSettings.Values[FileManager.themeKey])
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
            // Create a simple setting
            localSettings.Values[FileManager.liveTileKey] = LiveTileToggle.IsOn;
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
            localSettings.Values[FileManager.playingSoundsListVisibleKey] = PlayingSoundsListToggle.IsOn;
            (App.Current as App)._itemViewHolder.playingSoundsListVisibility = PlayingSoundsListToggle.IsOn ? Visibility.Visible : Visibility.Collapsed;

            SavePlayingSoundsStackPanel.Visibility = PlayingSoundsListToggle.IsOn ? Visibility.Visible : Visibility.Collapsed;

            Task.Run(() =>
            {
                FileManager.AddOrRemoveAllPlayingSounds();
            });
        }
        
        private void PlayOneSoundAtOnceToggle_Toggled(object sender, RoutedEventArgs e)
        {
            localSettings.Values[FileManager.playOneSoundAtOnceKey] = PlayOneSoundAtOnceToggle.IsOn;
            (App.Current as App)._itemViewHolder.playOneSoundAtOnce = PlayOneSoundAtOnceToggle.IsOn;
        }
        
        private void ThemeRadioButton_Checked(object sender, RoutedEventArgs e)
        {
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
            localSettings.Values[FileManager.showCategoryIconKey] = ShowCategoryToggle.IsOn;
            (App.Current as App)._itemViewHolder.showCategoryIcon = ShowCategoryToggle.IsOn;
        }
        
        private void ShowSoundsPivotToggle_Toggled(object sender, RoutedEventArgs e)
        {
            localSettings.Values[FileManager.showSoundsPivotKey] = ShowSoundsPivotToggle.IsOn;
            (App.Current as App)._itemViewHolder.showSoundsPivot = ShowSoundsPivotToggle.IsOn;
        }

        private void SavePlayingSoundsToggle_Toggled(object sender, RoutedEventArgs e)
        {
            localSettings.Values[FileManager.savePlayingSoundsKey] = SavePlayingSoundsToggle.IsOn;
            (App.Current as App)._itemViewHolder.savePlayingSounds = SavePlayingSoundsToggle.IsOn;

            Task.Run(() =>
            {
                FileManager.AddOrRemoveAllPlayingSounds();
            });
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
