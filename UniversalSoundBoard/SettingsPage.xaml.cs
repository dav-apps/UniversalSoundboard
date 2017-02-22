using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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
        public SettingsPage()
        {
            this.InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            setDataContext();
        }

        private void setDataContext()
        {
            ContentRoot.DataContext = (App.Current as App)._itemViewHolder;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            setPlayingSoundsListVisibilityToggle();
            setLiveTileToggle();
        }

        private void LiveTileToggle_Toggled(object sender, RoutedEventArgs e)
        {
            var localSettings = ApplicationData.Current.LocalSettings;

            // Create a simple setting
            localSettings.Values["liveTile"] = LiveTileToggle.IsOn;
            if (!LiveTileToggle.IsOn)
            {
                TileUpdateManager.CreateTileUpdaterForApplication().Clear();
            }else
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

        private void setLiveTileToggle()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            var value = localSettings.Values["liveTile"];

            if (value != null)
            {
                LiveTileToggle.IsOn = (bool)localSettings.Values["liveTile"];
            }
            else
            {
                LiveTileToggle.IsOn = false;
                localSettings.Values["liveTile"] = false;
                TileUpdateManager.CreateTileUpdaterForApplication().Clear();
            }
        }

        private void setPlayingSoundsListVisibilityToggle()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            PlayingSoundsListToggle.IsOn = (bool)localSettings.Values["playingSoundsListVisible"];
        }
    }
}
