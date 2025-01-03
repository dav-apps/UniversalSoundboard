﻿using davClassLibrary;
using Microsoft.Toolkit.Uwp.Helpers;
using Sentry;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using UniversalSoundboard.Common;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Dialogs;
using UniversalSoundboard.Models;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.UI.Notifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace UniversalSoundboard.Pages
{
    public sealed partial class SettingsPage : Page
    {
        bool initialized = false;
        bool hotkeysEnabled = false;
        string soundboardSize = "";
        string userId = "";
        Visibility soundboardSizeVisibility = Visibility.Collapsed;
        Visibility liveTileSettingVisibility = Visibility.Visible;
        ObservableCollection<Sound> SoundsWithHotkeysList = new ObservableCollection<Sound>();

        public SettingsPage()
        {
            InitializeComponent();
            FileManager.itemViewHolder.PropertyChanged += ItemViewHolder_PropertyChanged;

            // Hide the setting for Live Tiles on Windows 11+
            if (SystemInformation.Instance.OperatingSystemVersion.Build >= 22000)
                liveTileSettingVisibility = Visibility.Collapsed;
        }

        private async void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            ContentRoot.DataContext = FileManager.itemViewHolder;
            SetThemeColors();
            InitSettings();
            await FileManager.CalculateSoundboardSizeAsync();

            hotkeysEnabled = (Dav.IsLoggedIn && Dav.User.Plan > 0) || FileManager.itemViewHolder.PlusPurchased;
            Bindings.Update();
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
        }

        private void InitSettings()
        {
            // Init the settings UI elements
            SetSavePlayingSoundsToggle();
            SetOpenMultipleSoundsToggle();
            SetMultiSoundPlaybackToggle();
            SetShowSoundsPivotToggle();
            SetSoundOrderComboBox();
            LoadHotkeys();
            SetShowListViewToggle();
            SetShowCategoriesIconsToggle();
            SetShowAcrylicBackgroundToggle();
            SetLiveTileToggle();
            SetThemeComboBox();
            initialized = true;
        }

        private void UpdateSoundboardSizeText()
        {
            if (FileManager.itemViewHolder.SoundboardSize == 0)
            {
                soundboardSize = "";
                soundboardSizeVisibility = Visibility.Collapsed;
            }
            else
            {
                soundboardSize = string.Format(FileManager.loader.GetString("SettingsSoundBoardSize"), FileManager.GetFormattedSize(FileManager.itemViewHolder.SoundboardSize));
                soundboardSizeVisibility = Visibility.Visible;
            }

            Bindings.Update();
        }
        #endregion

        #region SavePlayingSounds
        private void SetSavePlayingSoundsToggle()
        {
            SavePlayingSoundsToggle.IsOn = FileManager.itemViewHolder.SavePlayingSounds;
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
            MultiSoundPlaybackStackPanel.Visibility = FileManager.itemViewHolder.OpenMultipleSounds ? Visibility.Visible : Visibility.Collapsed;
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

        private void SoundOrderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!initialized) return;
            FileManager.itemViewHolder.SoundOrder = (SoundOrder)SoundOrderComboBox.SelectedIndex;

            if (FileManager.itemViewHolder.SoundOrder == SoundOrder.Custom)
                FileManager.itemViewHolder.AllSoundsChanged = true;
        }
        #endregion

        #region Hotkeys
        private void LoadHotkeys()
        {
            foreach (var sound in FileManager.itemViewHolder.AllSounds)
                if (sound.Hotkeys.Count > 0)
                    SoundsWithHotkeysList.Add(sound);

            if (SoundsWithHotkeysList.Count == 0)
                NoHotkeysTextBlock.Visibility = Visibility.Visible;
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
        private void SetThemeComboBox()
        {
            ThemeComboBox.SelectedIndex = (int)FileManager.itemViewHolder.Theme;
        }

        private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!initialized) return;

            switch (ThemeComboBox.SelectedIndex)
            {
                case 0:
                    FileManager.itemViewHolder.Theme = AppTheme.System;
                    FileManager.itemViewHolder.CurrentTheme = (App.Current as App).RequestedTheme == ApplicationTheme.Dark ? AppTheme.Dark : AppTheme.Light;
                    break;
                case 1:
                    FileManager.itemViewHolder.Theme = AppTheme.Light;
                    FileManager.itemViewHolder.CurrentTheme = AppTheme.Light;
                    break;
                case 2:
                    FileManager.itemViewHolder.Theme = AppTheme.Dark;
                    FileManager.itemViewHolder.CurrentTheme = AppTheme.Dark;
                    break;
            }
        }
        #endregion

        #region Event handlers
        private void ItemViewHolder_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals(ItemViewHolder.CurrentThemeKey))
                SetThemeColors();
            else if (e.PropertyName.Equals(ItemViewHolder.SoundboardSizeKey))
                UpdateSoundboardSizeText();
        }

        private void SettingsHotkeysSoundItemTemplate_Remove(object sender, SoundEventArgs args)
        {
            // Remove the sound from the hotkeys sound list
            int i = SoundsWithHotkeysList.ToList().FindIndex(sound => sound.Uuid.Equals(args.Uuid));
            if (i != -1) SoundsWithHotkeysList.RemoveAt(i);

            if (SoundsWithHotkeysList.Count == 0)
                NoHotkeysTextBlock.Visibility = Visibility.Visible;
        }

        private async void ExportDataButton_Click(object sender, RoutedEventArgs e)
        {
            var exportSoundboardDialog = new ExportSoundboardDialog();
            exportSoundboardDialog.PrimaryButtonClick += ExportDataContentDialog_PrimaryButtonClickAsync;
            await exportSoundboardDialog.ShowAsync();
        }

        private async void ExportDataContentDialog_PrimaryButtonClickAsync(Dialog sender, ContentDialogButtonClickEventArgs args)
        {
            var dialog = sender as ExportSoundboardDialog;
            await FileManager.ExportDataAsync(dialog.ExportFolder);
            SentrySdk.CaptureMessage("ExportData");
        }

        private async void ImportDataButton_Click(object sender, RoutedEventArgs e)
        {
            var importSoundboardDialog = new ImportSoundboardDialog();
            importSoundboardDialog.PrimaryButtonClick += ImportDataContentDialog_PrimaryButtonClick;
            await importSoundboardDialog.ShowAsync();
        }

        private async void ImportDataContentDialog_PrimaryButtonClick(Dialog sender, ContentDialogButtonClickEventArgs args)
        {
            var dialog = sender as ImportSoundboardDialog;

            await FileManager.ImportDataAsync(dialog.ImportFile, false);

            SentrySdk.CaptureMessage("ImportData", scope =>
            {
                scope.SetTag("Context", "Settings");
            });
        }

        private async void ReviewButton_Click(object sender, RoutedEventArgs e)
        {
            await SystemInformation.LaunchStoreForReviewAsync();
            FileManager.itemViewHolder.AppReviewed = true;
            SentrySdk.CaptureMessage("SettingsPage-ReviewButtonClick");
        }

        private async void SendFeedbackButton_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri("https://forms.gle/Y2fJnwDTyWMwBRbk7"));
            SentrySdk.CaptureMessage("SettingsPage-SendFeedbackButtonClick");
        }

        private async void CreateIssueButton_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri("https://github.com/dav-apps/UniversalSoundboard/issues"));
            SentrySdk.CaptureMessage("SettingsPage-CreateIssueButtonClick");
        }
        #endregion

        private void VersionStackPanel_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            userId = FileManager.itemViewHolder.UserId.ToString();
            Bindings.Update();

            FlyoutBase.ShowAttachedFlyout(VersionTextBlock);
        }

        private void CopyUserIdButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            DataPackage dataPackage = new DataPackage
            {
                RequestedOperation = DataPackageOperation.Copy
            };

            dataPackage.SetText(userId);
            Clipboard.SetContent(dataPackage);
        }
    }
}
