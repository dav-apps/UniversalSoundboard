using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using UniversalSoundBoard.Model;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Notifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using static UniversalSoundBoard.Model.Sound;

// Die Elementvorlage "Leere Seite" ist unter http://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace UniversalSoundBoard
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        static string themeAtBeginning;

        public SettingsPage()
        {
            this.InitializeComponent();

            if(String.IsNullOrEmpty(themeAtBeginning))
            {
                var localSettings = ApplicationData.Current.LocalSettings;
                themeAtBeginning = (string)localSettings.Values["theme"];
            }
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            setDataContext();
            await FileManager.setSoundBoardSizeTextAsync();
        }

        private void setDataContext()
        {
            ContentRoot.DataContext = (App.Current as App)._itemViewHolder;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            setLiveTileToggle();
            setPlayingSoundsListVisibilityToggle();
            setPlayOneSoundAtOnceToggle();
            setShowCategoryIconToggle();
            setShowSoundsPivotToggle();
            setThemeRadioButton();
        }

        private void setThemeRadioButton()
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

            setToggleMessageVisibility();
        }

        private void setLiveTileToggle()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            LiveTileToggle.IsOn = (bool)localSettings.Values["liveTile"];
        }

        private void setPlayingSoundsListVisibilityToggle()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            PlayingSoundsListToggle.IsOn = (bool)localSettings.Values["playingSoundsListVisible"];
        }

        private void setPlayOneSoundAtOnceToggle()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            PlayOneSoundAtOnceToggle.IsOn = (bool)localSettings.Values["playOneSoundAtOnce"];
        }

        private void setShowCategoryIconToggle()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            ShowCategoryToggle.IsOn = (bool)localSettings.Values["showCategoryIcon"];
        }

        private void setShowSoundsPivotToggle()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            ShowSoundsPivotToggle.IsOn = (bool)localSettings.Values["showSoundsPivot"];
        }

        private void setToggleMessageVisibility()
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

            setToggleMessageVisibility();
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
