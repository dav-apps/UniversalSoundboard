using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using UniversalSoundBoard.Model;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Notifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// Die Elementvorlage "Leere Seite" ist unter http://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace UniversalSoundBoard
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        bool darkThemeToggledAtBeginning;

        public SettingsPage()
        {
            this.InitializeComponent();
            AdjustLayout();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            setDataContext();

            darkThemeToggledAtBeginning = (App.Current as App).RequestedTheme == ApplicationTheme.Dark ? true : false;
            setToggleMessageVisibility();
            await setSoundBoardSizeText();
        }

        private void setDataContext()
        {
            ContentRoot.DataContext = (App.Current as App)._itemViewHolder;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            setThemeToggle();
            setLiveTileToggle();
            setPlayingSoundsListVisibilityToggle();
            setPlayOneSoundAtOnceToggle();
            setShowCategoryIconToggle();
            setShowSoundsPivotToggle();
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            AdjustLayout();
        }

        private void AdjustLayout()
        {
            if (Window.Current.Bounds.Width < FileManager.mobileMaxWidth)       // If user in on mobile
            {
                TitleRow.Height = GridLength.Auto;
            }
            else
            {
                TitleRow.Height = new GridLength(0);
            }
        }

        private void setThemeToggle()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values["darkTheme"] != null)
            {
                ThemeToggle.IsOn = (bool)localSettings.Values["darkTheme"];
            }
            else if (Application.Current.RequestedTheme == ApplicationTheme.Dark)
            {
                ThemeToggle.IsOn = true;
            }
            ThemeChangeMessageTextBlock.Visibility = Visibility.Collapsed;
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
            if (darkThemeToggledAtBeginning != ThemeToggle.IsOn)
            {
                ThemeChangeMessageTextBlock.Visibility = Visibility.Visible;
            }
            else
            {
                ThemeChangeMessageTextBlock.Visibility = Visibility.Collapsed;
            }
        }

        private async Task setSoundBoardSizeText()
        {
            if ((App.Current as App)._itemViewHolder.progressRingIsActive)
            {
                await Task.Delay(1000);
                await setSoundBoardSizeText();
            }

            float totalSize = 0;
            foreach (Sound sound in (App.Current as App)._itemViewHolder.allSounds)
            {
                float size;
                size = await FileManager.GetFileSizeInGBAsync(sound.AudioFile);
                if (sound.ImageFile != null)
                {
                    size += await FileManager.GetFileSizeInGBAsync(sound.ImageFile);
                }
                totalSize += size;
            }

            SoundBoardSizeTextBlock.Text = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("SettingsSoundBoardSize") + totalSize.ToString("n2") + "GB.";
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

        private void ThemeToggle_Toggled(object sender, RoutedEventArgs e)
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["darkTheme"] = ThemeToggle.IsOn;
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
    }
}
