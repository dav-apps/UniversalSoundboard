﻿using davClassLibrary;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Models;
using UniversalSoundboard.Pages;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace UniversalSoundboard.Common
{
    public class ItemViewHolder : INotifyPropertyChanged
    {
        #region Constants for the localSettings keys
        private const string userIdKey = "userId";
        private const string plusPurchasedKey = "plusPurchased";
        private const string savePlayingSoundsKey = "savePlayingSounds";
        private const string openMultipleSoundsKey = "openMultipleSounds";
        private const string multiSoundPlaybackKey = "multiSoundPlayback";
        private const string showSoundsPivotKey = "showSoundsPivot";
        private const string newSoundOrderKey = "newSoundOrder";
        private const string multipleOutputDevicesKey = "multipleOutputDevices";
        private const string useStandardOutputDeviceKey = "useStandardOutputDevice";
        private const string outputDeviceKey = "outputDevice";
        private const string showListViewKey = "showListView";
        private const string showCategoriesIconsKey = "showCategoryIcon";
        private const string showAcrylicBackgroundKey = "showAcrylicBackground";
        private const string liveTileKey = "liveTile";
        private const string themeKey = "theme";
        private const string playingSoundsBarWidthKey = "playingSoundsBarWidth";
        private const string mutedKey = "muted";
        private const string volumeKey = "volume";
        private const string appStartCounterKey = "appStartCounter";
        private const string appReviewedKey = "appReviewed";
        private const string showContinuePlaylistDownloadIANKey = "showContinuePlaylistDownloadIAN";
        private const string soundRecorderMinimizeWarningClosedKey = "soundRecorderMinimizeWarningClosed";
        private const string isFadeInEffectEnabledKey = "fadeInEffectEnabled";
        private const string fadeInEffectDurationKey = "fadeInEffectDuration";
        private const string isFadeOutEffectEnabledKey = "fadeOutEffectEnabled";
        private const string fadeOutEffectDurationKey = "fadeOutEffectDuration";
        private const string isEchoEffectEnabledKey = "isEchoEffectEnabled";
        private const string echoEffectDelayKey = "echoEffectDelay";
        private const string isLimiterEffectEnabledKey = "isLimiterEffectEnabled";
        private const string limiterEffectLoudnessKey = "limiterEffectLoudness";
        private const string isReverbEffectEnabledKey = "isReverbEffectEnabled";
        private const string reverbEffectDecayKey = "reverbEffectDecay";
        private const string isPitchShiftEffectEnabledKey = "isPitchShiftEffectEnabled";
        private const string pitchShiftEffectFactorKey = "pitchShiftEffectFactor";
        #endregion

        #region Constants for localSettings defaults
        private const bool plusPurchasedDefault = false;
        private const bool savePlayingSoundsDefault = true;
        private const bool openMultipleSoundsDefault = true;
        private const bool multiSoundPlaybackDefault = false;
        private const bool showSoundsPivotDefault = true;
        private const SoundOrder soundOrderDefault = SoundOrder.Custom;
        private const bool multipleOutputDevicesDefault = false;
        private const bool useStandardOutputDeviceDefault = true;
        private const string outputDeviceDefault = "";
        private const bool showListViewDefault = false;
        private const bool showCategoriesIconsDefault = false;
        private const bool showAcrylicBackgroundDefault = true;
        private const bool liveTileDefault = true;
        private const AppTheme themeDefault = AppTheme.System;
        private const double playingSoundsBarWidthDefault = 0.35;
        private const bool mutedDefault = false;
        private const int volumeDefault = 100;
        private const int appStartCounterDefault = 0;
        private const bool appReviewedDefault = false;
        private const bool showContinuePlaylistDownloadIANDefault = false;
        private const bool soundRecorderMinimizeWarningClosedDefault = false;
        private const bool isFadeInEffectEnabledDefault = false;
        private const int fadeInEffectDurationDefault = 1000;
        private const bool isFadeOutEffectEnabledDefault = false;
        private const int fadeOutEffectDurationDefault = 1000;
        private const bool isEchoEffectEnabledDefault = false;
        private const int echoEffectDelayDefault = 500;
        private const bool isLimiterEffectEnabledDefault = false;
        private const int limiterEffectLoudnessDefault = 1000;
        private const bool isReverbEffectEnabledDefault = false;
        private const double reverbEffectDecayDefault = 2;
        private const bool isPitchShiftEffectEnabledDefault = false;
        private const double pitchShiftEffectFactorDefault = 1;
        #endregion

        #region Variables
        #region State
        private AppState _appState;                 // The current state of the app
        private string _title;                      // The title text
        private Type _page;                         // The current page
        private string _searchQuery;                // The string entered into the search box
        private bool _allSoundsChanged;             // If there was made change to one or multiple sounds so that a reload of the sounds is required
        private Guid _selectedCategory;             // The index of the currently selected category in the category list
        private bool _exporting;                    // If true the soundboard is currently being exported
        private bool _importing;                    // If true a soundboard is currently being imported
        private string _exportMessage;              // The text describing the status of the export
        private string _importMessage;              // The text describing the status of the import
        private ulong _soundboardSize;              // The size of the entire soundboard in byte
        private Guid _activePlayingSound;           // The PlayingSound that was last activated and is currently visible in SystemMediaTransportControls
        private bool _addingSounds;                 // If true, sounds are currently being added and all buttons for adding or downloading sounds are disabled
        private string _upgradePlusPrice;           // The price for the universalsoundboard-plus addon in the MS Store
        #endregion

        #region Lists
        public List<Category> Categories { get; }                           // A list of all categories
        public ObservableCollection<Sound> AllSounds { get; }               // A list of all sounds, unsorted
        public ObservableCollection<Sound> Sounds { get; }                  // A list of the sounds which are displayed when the Sound pivot is selected, sorted by the selected sort option
        public ObservableCollection<Sound> FavouriteSounds { get; }         // A list of the favourite sound which are displayed when the Favourite pivot is selected, sorted by the selected sort option
        public ObservableCollection<Sound> SelectedSounds { get; }          // A list of the sounds which are selected
        public ObservableCollection<PlayingSound> PlayingSounds { get; }    // A list of the Playing Sounds which are displayed in the right menu
        public List<PlayingSoundItem> PlayingSoundItems { get; }            // A list of all PlayingSoundItems
        public List<Guid> HotkeySoundMapping { get; }                       // A list of the sounds which are mapped to a specific hotkey, with the position in the list as id
        public List<string> Tags { get; }                                   // List of all sound tags loaded from the API
        #endregion

        #region Layout & Design
        private AppTheme _currentTheme;                                     // The current theme of the app; is either Light or Dark
        private bool _progressRingIsActive;                                 // Shows the Progress Ring if true
        private bool _loadingScreenVisible;                                 // If true, the large loading screen is visible
        private string _loadingScreenMessage;                               // The text that is shown in the loading screen
        private bool _backButtonEnabled;                                    // If the Back Button is enabled
        private bool _multiSelectionEnabled;                                // If true, the GridView has multi selection and the multi selection buttons are visible
        private bool _playAllButtonVisible;                                 // If true shows the Play button next to the title, only when a category or All Sounds is selected
        private bool _editButtonVisible;                                    // If true shows the edit button next to the title, only when a category is selected
        private bool _topButtonsCollapsed;                                  // If true the buttons at the top show only the icon, if false they show the icon and text
        private bool _searchAutoSuggestBoxVisible;                          // If true the search box is visible, if false multi selection is on or the search button shown
        private bool _searchButtonVisible;                                  // If true the search button at the top is visible
        private string _selectAllFlyoutText;                                // The text of the Select All flyout item in the Navigation View Header
        private SymbolIcon _selectAllFlyoutIcon;                            // The icon of the Select All flyout item in the Navigation View Header
        private double _soundTileWidth;                                     // The width of all sound tiles in the GridViews
        private AcrylicBrush _playingSoundsBarAcrylicBackgroundBrush;       // This represents the background of the PlayingSoundsBar
        private AcrylicBrush _secondaryWindowAcrylicBackgroundBrush;        // This represents the background of the secondary windows
        private bool _exportAndImportButtonsEnabled;                        // If true shows the export and import buttons on the settings page
        #endregion
        #endregion

        #region Settings
        private Guid _userId;                               // A unique id for the current installation
        private bool _plusPurchased;                        // If true, the user has purchased the universalsoundboard-plus addon in the MS Store
        private bool _savePlayingSounds;                    // If true saves the PlayingSounds and loads them when starting the app
        private bool _openMultipleSounds;                   // If false, removes all PlayingSounds whenever the user opens a new one; if true, adds new opened sounds to existing PlayingSounds
        private bool _multiSoundPlayback;                   // If true, can play multiple sounds at the same time; if false, stops the currently playing sound when playing another sound
        private bool _showSoundsPivot;                      // If true shows the pivot to select Sounds or Favourite sounds
        private SoundOrder _soundOrder;                     // The selected sound order in the settings
        private bool _multipleOutputDevices;                // If true, the user can select multiple output devices
        private bool _useStandardOutputDevice;              // If true, the standard output device of the OS is used for playback
        private string _outputDevice;                       // The id of the selected output device
        private bool _showListView;                         // If true, shows the sounds on the SoundPage in a ListView
        private bool _showCategoriesIcons;                  // If true shows the icon of the category on the sound tile
        private bool _showAcrylicBackground;                // If true the acrylic background is visible
        private bool _liveTile;                             // If true, shows the live tile
        private AppTheme _theme;                            // The design theme of the app
        private double _playingSoundsBarWidth;              // The relative width of the PlayingSoundsBar in percent
        private bool _muted;                                // If true, the volume is muted
        private int _volume;                                // The volume of the entire app, between 0 and 100
        private int _appStartCounter;                       // Counts the number of app starts
        private bool _appReviewed;                          // If true, the user has followed the link to the MS Store to write a review
        private bool _showContinuePlaylistDownloadIAN;      // Determines whether to show the IAN for continuing the playlist download
        private bool _soundRecorderMinimizeWarningClosed;   // If true, the warning for minimizing the window on the Sound Recorder was closed by the user and should not be shown again
        private bool _isFadeInEffectEnabled;                // If true, the global fade in effect is enabled for all sounds
        private int _fadeInEffectDuration;                  // The duration of the global fade in effect
        private bool _isFadeOutEffectEnabled;               // If true, the global fade out effect is enabled for all sounds
        private int _fadeOutEffectDuration;                 // The duration of the global fade out effect
        private bool _isEchoEffectEnabled;                  // If true, the global echo effect is enabled for all sounds
        private int _echoEffectDelay;                       // The delay of the global echo effect in ms
        private bool _isLimiterEffectEnabled;               // If true, the global limiter effect is enabled for all sounds
        private int _limiterEffectLoudness;                 // The loudness of the global limiter effect
        private bool _isReverbEffectEnabled;                // If true, the global reverb effect is enabled for all sounds
        private double _reverbEffectDecay;                  // The decay of the global reverb effect
        private bool _isPitchShiftEffectEnabled;            // If true, the global pitch shift effect is enabled for all sounds
        private double _pitchShiftEffectFactor;             // The pitch factor of the global pitch shift effect. 0.5 = octave down, 1 = normal, 2 = octave up
        #endregion

        #region Events
        public event EventHandler<EventArgs> SoundsLoaded;                                                          // Is triggered when the sounds at startup were loaded
        public event EventHandler<EventArgs> PlayingSoundsLoaded;                                                   // Is triggered when the playing sounds at startup were loaded
        public event EventHandler<EventArgs> CategoriesLoaded;                                                      // Is triggered when all categories were loaded into the Categories List
        public event EventHandler<EventArgs> UserSyncFinished;                                                      // Is triggered when the user infos and the profile image were downloaded from the server
        public event EventHandler<EventArgs> UserPlanChanged;                                                       // Is triggered when the plan of the user was changed from within the app
        public event EventHandler<EventArgs> SoundPromotionStarted;                                                 // Is triggered when the user successfully paid for promoting a sound, by opening the app from the website
        public event EventHandler<CategoryEventArgs> CategoryAdded;                                                 // Is triggered when a category was added
        public event EventHandler<CategoryEventArgs> CategoryUpdated;                                               // Is triggered when a category was updated
        public event EventHandler<CategoryEventArgs> CategoryDeleted;                                               // Is triggered when a category was deleted
        public event EventHandler<SoundEventArgs> SoundDeleted;                                                     // Is triggered when a sound was deleted
        public event EventHandler<RoutedEventArgs> SelectAllSounds;                                                 // Trigger this event to select all sounds or deselect all sounds when all sounds are selected
        public event EventHandler<SizeChangedEventArgs> SoundTileSizeChanged;                                       // Is triggered when the size of the sound tiles in the GridViews has changed
        public event EventHandler<HidePlayingSoundItemEventArgs> HidePlayingSoundItem;                            // Is triggered when a PlayingSoundItem was removed and should be hidden on the BottomPlayingSoundsBar, if the BottomPlayingSoundsBar is not visible
        public event EventHandler<TableObjectFileDownloadProgressChangedEventArgs> TableObjectFileDownloadProgressChanged;  // Is triggered when the file of a TableObject is being downloaded and the progress changed
        public event EventHandler<TableObjectFileDownloadCompletedEventArgs> TableObjectFileDownloadCompleted;      // Is triggered from TriggerAction when the file of a TableObject was finished
        public event EventHandler<ShowInAppNotificationEventArgs> ShowInAppNotification;                            // Trigger this event to show the InAppNotification on the SoundPage
        public event EventHandler<EventArgs> SoundDownload;                                                         // Is triggered on SoundPage in the SoundDownloadDialog primary button click event handler
        public event EventHandler<PlaySoundEventArgs> PlaySound;                                                    // Is triggered to start playing a sound in SoundPage
        public event EventHandler<PlaySoundsEventArgs> PlaySounds;                                                  // Is triggered to start playing multiple sounds in SoundPage
        public event EventHandler<PlaySoundAfterPlayingSoundsLoadedEventArgs> PlaySoundAfterPlayingSoundsLoaded;    // Is triggered to start playing a sound on startup, when starting a sound from the Start Menu
        public event EventHandler<PlayLocalSoundAfterPlayingSoundsLoadedEventArgs> PlayLocalSoundAfterPlayingSoundsLoaded;  // Is triggered to start playing a local sound file
        #endregion

        #region Local variables
        readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        #endregion

        public ItemViewHolder()
        {
            // Set the default values
            #region State
            _title = FileManager.loader.GetString("AllSounds");
            _page = typeof(SoundPage);
            _searchQuery = "";
            _allSoundsChanged = true;
            _selectedCategory = Guid.Empty;
            _exporting = false;
            _importing = false;
            _exportMessage = "";
            _importMessage = "";
            _soundboardSize = 0;
            _activePlayingSound = Guid.Empty;
            _addingSounds = false;
            _upgradePlusPrice = "22,99 €";
            #endregion

            #region Lists
            Categories = new List<Category>();
            AllSounds = new ObservableCollection<Sound>();
            Sounds = new ObservableCollection<Sound>();
            FavouriteSounds = new ObservableCollection<Sound>();
            SelectedSounds = new ObservableCollection<Sound>();
            PlayingSounds = new ObservableCollection<PlayingSound>();
            PlayingSoundItems = new List<PlayingSoundItem>();
            HotkeySoundMapping = new List<Guid>();
            Tags = new List<string>();
            #endregion

            #region Layout & Design
            _currentTheme = AppTheme.Light;
            _progressRingIsActive = false;
            _loadingScreenVisible = false;
            _loadingScreenMessage = "";
            _backButtonEnabled = false;
            _playAllButtonVisible = false;
            _editButtonVisible = false;
            _topButtonsCollapsed = false;
            _searchAutoSuggestBoxVisible = true;
            _searchButtonVisible = false;
            _selectAllFlyoutText = FileManager.loader.GetString("MoreButton_SelectAllFlyout-SelectAll");
            _soundTileWidth = 200;
            _exportAndImportButtonsEnabled = true;

            try
            {
                _selectAllFlyoutIcon = new SymbolIcon(Symbol.SelectAll);
                _playingSoundsBarAcrylicBackgroundBrush = new AcrylicBrush();
                _secondaryWindowAcrylicBackgroundBrush = new AcrylicBrush();
            }
            catch { }
            #endregion

            #region Settings
            #region userId
            if (localSettings.Values[userIdKey] == null)
            {
                _userId = Guid.NewGuid();
                localSettings.Values[userIdKey] = _userId;
            }
            else
                _userId = (Guid)localSettings.Values[userIdKey];
            #endregion

            #region plusPurchased
            if (localSettings.Values[plusPurchasedKey] == null)
                _plusPurchased = plusPurchasedDefault;
            else
                _plusPurchased = (bool)localSettings.Values[plusPurchasedKey];
            #endregion

            #region savePlayingSounds
            if (localSettings.Values[savePlayingSoundsKey] == null)
                _savePlayingSounds = savePlayingSoundsDefault;
            else
                _savePlayingSounds = (bool)localSettings.Values[savePlayingSoundsKey];
            #endregion

            #region openMultipleSounds
            if (localSettings.Values[openMultipleSoundsKey] == null)
                _openMultipleSounds = openMultipleSoundsDefault;
            else
                _openMultipleSounds = (bool)localSettings.Values[openMultipleSoundsKey];
            #endregion

            #region multiSoundPlayback
            if (localSettings.Values[multiSoundPlaybackKey] == null)
                _multiSoundPlayback = multiSoundPlaybackDefault;
            else
                _multiSoundPlayback = (bool)localSettings.Values[multiSoundPlaybackKey];
            #endregion

            #region showSoundsPivot
            if (localSettings.Values[showSoundsPivotKey] == null)
                _showSoundsPivot = showSoundsPivotDefault;
            else
                _showSoundsPivot = (bool)localSettings.Values[showSoundsPivotKey];
            #endregion

            #region soundOrder
            if (localSettings.Values[newSoundOrderKey] == null)
                _soundOrder = soundOrderDefault;
            else
                _soundOrder = (SoundOrder)localSettings.Values[newSoundOrderKey];
            #endregion

            #region multipleOutputDevices
            if ((Dav.IsLoggedIn && Dav.User.Plan > 0) || PlusPurchased)
            {
                if (localSettings.Values[multipleOutputDevicesKey] == null)
                    _multipleOutputDevices = multipleOutputDevicesDefault;
                else
                    _multipleOutputDevices = (bool)localSettings.Values[multipleOutputDevicesKey];
            }
            else
            {
                _multipleOutputDevices = multipleOutputDevicesDefault;
            }
            #endregion

            #region useStandardOutputDevice
            if ((Dav.IsLoggedIn && Dav.User.Plan > 0) || PlusPurchased)
            {
                if (localSettings.Values[useStandardOutputDeviceKey] == null)
                    _useStandardOutputDevice = useStandardOutputDeviceDefault;
                else
                    _useStandardOutputDevice = (bool)localSettings.Values[useStandardOutputDeviceKey];
            }
            else
            {
                _useStandardOutputDevice = useStandardOutputDeviceDefault;
            }
            #endregion

            #region outputDevice
            if (localSettings.Values[outputDeviceKey] == null)
                _outputDevice = outputDeviceDefault;
            else
                _outputDevice = (string)localSettings.Values[outputDeviceKey];
            #endregion

            #region showListView
            if (localSettings.Values[showListViewKey] == null)
                _showListView = showListViewDefault;
            else
                _showListView = (bool)localSettings.Values[showListViewKey];
            #endregion

            #region showCategoriesIcons
            if (localSettings.Values[showCategoriesIconsKey] == null)
                _showCategoriesIcons = showCategoriesIconsDefault;
            else
                _showCategoriesIcons = (bool)localSettings.Values[showCategoriesIconsKey];
            #endregion

            #region showAcrylicBackground
            if (localSettings.Values[showAcrylicBackgroundKey] == null)
                _showAcrylicBackground = showAcrylicBackgroundDefault;
            else
                _showAcrylicBackground = (bool)localSettings.Values[showAcrylicBackgroundKey];
            #endregion

            #region liveTile
            if (localSettings.Values[liveTileKey] == null)
                _liveTile = liveTileDefault;
            else
                _liveTile = (bool)localSettings.Values[liveTileKey];
            #endregion

            #region theme
            if (localSettings.Values[themeKey] == null)
                _theme = themeDefault;
            else
            {
                switch ((string)localSettings.Values[themeKey])
                {
                    case "light":
                        _theme = AppTheme.Light;
                        break;
                    case "dark":
                        _theme = AppTheme.Dark;
                        break;
                    case "system":
                        _theme = AppTheme.System;
                        break;
                }
            }
            #endregion

            #region playingSoundsBarWidth
            if (localSettings.Values[playingSoundsBarWidthKey] == null)
                _playingSoundsBarWidth = playingSoundsBarWidthDefault;
            else
                _playingSoundsBarWidth = (double)localSettings.Values[playingSoundsBarWidthKey];
            #endregion

            #region muted
            if (localSettings.Values[mutedKey] == null)
                _muted = mutedDefault;
            else
                _muted = (bool)localSettings.Values[mutedKey];
            #endregion

            #region volume
            if (localSettings.Values[volumeKey] == null)
                _volume = volumeDefault;
            else
            {
                // Backwards compatibility for saving the volume as double
                try
                {
                    // Try to read the volume as int
                    _volume = (int)localSettings.Values[volumeKey];
                }
                catch
                {
                    // Overwrite the volume with the default
                    localSettings.Values[volumeKey] = volumeDefault;
                    _volume = volumeDefault;
                }
            }
            #endregion

            #region appStartCounter
            if (localSettings.Values[appStartCounterKey] == null)
                _appStartCounter = appStartCounterDefault;
            else
                _appStartCounter = (int)localSettings.Values[appStartCounterKey];
            #endregion

            #region appReviewed
            if (localSettings.Values[appReviewedKey] == null)
                _appReviewed = appReviewedDefault;
            else
                _appReviewed = (bool)localSettings.Values[appReviewedKey];
            #endregion

            #region showContinuePlaylistDownloadIAN
            if (localSettings.Values[showContinuePlaylistDownloadIANKey] == null)
                _showContinuePlaylistDownloadIAN = showContinuePlaylistDownloadIANDefault;
            else
                _showContinuePlaylistDownloadIAN = (bool)localSettings.Values[showContinuePlaylistDownloadIANKey];
            #endregion

            #region soundRecorderMinimizeWarningClosed
            if (localSettings.Values[soundRecorderMinimizeWarningClosedKey] == null)
                _soundRecorderMinimizeWarningClosed = soundRecorderMinimizeWarningClosedDefault;
            else
                _soundRecorderMinimizeWarningClosed = (bool)localSettings.Values[soundRecorderMinimizeWarningClosedKey];
            #endregion

            #region isFadeInEffectEnabled
            if (localSettings.Values[isFadeInEffectEnabledKey] == null)
                _isFadeInEffectEnabled = isFadeInEffectEnabledDefault;
            else
                _isFadeInEffectEnabled = (bool)localSettings.Values[isFadeInEffectEnabledKey];
            #endregion

            #region fadeInEffectDuration
            if (localSettings.Values[fadeInEffectDurationKey] == null)
                _fadeInEffectDuration = fadeInEffectDurationDefault;
            else
                _fadeInEffectDuration = (int)localSettings.Values[fadeInEffectDurationKey];
            #endregion

            #region isFadeOutEffectEnabled
            if (localSettings.Values[isFadeOutEffectEnabledKey] == null)
                _isFadeOutEffectEnabled = isFadeOutEffectEnabledDefault;
            else
                _isFadeOutEffectEnabled = (bool)localSettings.Values[isFadeOutEffectEnabledKey];
            #endregion

            #region fadeOutEffectDuration
            if (localSettings.Values[fadeOutEffectDurationKey] == null)
                _fadeOutEffectDuration = fadeOutEffectDurationDefault;
            else
                _fadeOutEffectDuration = (int)localSettings.Values[fadeOutEffectDurationKey];
            #endregion

            #region isEchoEffectEnabled
            if (localSettings.Values[isEchoEffectEnabledKey] == null)
                _isEchoEffectEnabled = isEchoEffectEnabledDefault;
            else
                _isEchoEffectEnabled = (bool)localSettings.Values[isEchoEffectEnabledKey];
            #endregion

            #region echoEffectDelay
            if (localSettings.Values[echoEffectDelayKey] == null)
                _echoEffectDelay = echoEffectDelayDefault;
            else
                _echoEffectDelay = (int)localSettings.Values[echoEffectDelayKey];
            #endregion

            #region isLimiterEffectEnabled
            if (localSettings.Values[isLimiterEffectEnabledKey] == null)
                _isLimiterEffectEnabled = isLimiterEffectEnabledDefault;
            else
                _isLimiterEffectEnabled = (bool)localSettings.Values[isLimiterEffectEnabledKey];
            #endregion

            #region limiterEffectLoudness
            if (localSettings.Values[limiterEffectLoudnessKey] == null)
                _limiterEffectLoudness = limiterEffectLoudnessDefault;
            else
                _limiterEffectLoudness = (int)localSettings.Values[limiterEffectLoudnessKey];
            #endregion

            #region isReverbEffectEnabled
            if (localSettings.Values[isReverbEffectEnabledKey] == null)
                _isReverbEffectEnabled = isReverbEffectEnabledDefault;
            else
                _isReverbEffectEnabled = (bool)localSettings.Values[isReverbEffectEnabledKey];
            #endregion

            #region reverbEffectDecay
            if (localSettings.Values[reverbEffectDecayKey] == null)
                _reverbEffectDecay = reverbEffectDecayDefault;
            else
                _reverbEffectDecay = (double)localSettings.Values[reverbEffectDecayKey];
            #endregion

            #region isPitchShiftEffectEnabled
            if (localSettings.Values[isPitchShiftEffectEnabledKey] == null)
                _isPitchShiftEffectEnabled = isPitchShiftEffectEnabledDefault;
            else
                _isPitchShiftEffectEnabled = (bool)localSettings.Values[isPitchShiftEffectEnabledKey];
            #endregion

            #region pitchShiftEffectFactor
            if (localSettings.Values[pitchShiftEffectFactorKey] == null)
                _pitchShiftEffectFactor = pitchShiftEffectFactorDefault;
            else
                _pitchShiftEffectFactor = (double)localSettings.Values[pitchShiftEffectFactorKey];
            #endregion
            #endregion
        }

        #region Access Modifiers
        #region State
        #region AppState
        public const string AppStateKey = "AppState";
        public AppState AppState
        {
            get => _appState;
            set
            {
                if (_appState.Equals(value)) return;
                _appState = value;
                NotifyPropertyChanged(AppStateKey);
            }
        }
        #endregion

        #region Title
        public const string TitleKey = "Title";
        public string Title
        {
            get => _title;
            set
            {
                if (_title.Equals(value)) return;
                _title = value;
                NotifyPropertyChanged(TitleKey);
            }
        }
        #endregion

        #region Page
        public const string PageKey = "Page";
        public Type Page
        {
            get => _page;
            set
            {
                if (_page.Equals(value)) return;
                _page = value;
                NotifyPropertyChanged(PageKey);
            }
        }
        #endregion

        #region SearchQuery
        public const string SearchQueryKey = "SearchQuery";
        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                if (_searchQuery.Equals(value)) return;
                _searchQuery = value;
                NotifyPropertyChanged(SearchQueryKey);
            }
        }
        #endregion

        #region AllSoundsChanged
        public const string AllSoundsChangedKey = "AllSoundsChanged";
        public bool AllSoundsChanged
        {
            get => _allSoundsChanged;
            set
            {
                if (_allSoundsChanged.Equals(value)) return;
                _allSoundsChanged = value;
                NotifyPropertyChanged(AllSoundsChangedKey);
            }
        }
        #endregion

        #region SelectedCategory
        public const string SelectedCategoryKey = "SelectedCategory";
        public Guid SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (_selectedCategory.Equals(value)) return;
                _selectedCategory = value;
                NotifyPropertyChanged(SelectedCategoryKey);
            }
        }
        #endregion

        #region Exporting
        public const string ExportingKey = "Exporting";
        public bool Exporting
        {
            get => _exporting;
            set
            {
                if (_exporting.Equals(value)) return;
                _exporting = value;
                NotifyPropertyChanged(ExportingKey);
            }
        }
        #endregion

        #region Importing
        public const string ImportingKey = "Importing";
        public bool Importing
        {
            get => _importing;
            set
            {
                if (_importing.Equals(value)) return;
                _importing = value;
                NotifyPropertyChanged(ImportingKey);
            }
        }
        #endregion

        #region ExportMessage
        public const string ExportMessageKey = "ExportMessage";
        public string ExportMessage
        {
            get => _exportMessage;
            set
            {
                if (_exportMessage.Equals(value)) return;
                _exportMessage = value;
                NotifyPropertyChanged(ExportMessageKey);
            }
        }
        #endregion

        #region ImportMessage
        public const string ImportMessageKey = "ImportMessage";
        public string ImportMessage
        {
            get => _importMessage;
            set
            {
                if (_importMessage.Equals(value)) return;
                _importMessage = value;
                NotifyPropertyChanged(ImportMessageKey);
            }
        }
        #endregion

        #region SoundboardSize
        public const string SoundboardSizeKey = "SoundboardSize";
        public ulong SoundboardSize
        {
            get => _soundboardSize;
            set
            {
                if (_soundboardSize.Equals(value)) return;
                _soundboardSize = value;
                NotifyPropertyChanged(SoundboardSizeKey);
            }
        }
        #endregion

        #region ActivePlayingSound
        public const string ActivePlayingSoundKey = "ActivePlayingSound";
        public Guid ActivePlayingSound
        {
            get => _activePlayingSound;
            set
            {
                if (_activePlayingSound.Equals(value)) return;
                _activePlayingSound = value;
                NotifyPropertyChanged(ActivePlayingSoundKey);
            }
        }
        #endregion

        #region AddingSounds
        public const string AddingSoundsKey = "AddingSounds";
        public bool AddingSounds
        {
            get => _addingSounds;
            set
            {
                if (_addingSounds.Equals(value)) return;
                _addingSounds = value;
                NotifyPropertyChanged(AddingSoundsKey);
            }
        }
        #endregion

        #region UpgradePlusPrice
        public const string UpgradePlusPriceKey = "UpgradePlusPrice";
        public string UpgradePlusPrice
        {
            get => _upgradePlusPrice;
            set
            {
                if (_upgradePlusPrice.Equals(value)) return;
                _upgradePlusPrice = value;
                NotifyPropertyChanged(UpgradePlusPriceKey);
            }
        }
        #endregion
        #endregion

        #region Layout & Design
        #region CurrentTheme
        public const string CurrentThemeKey = "CurrentTheme";
        public AppTheme CurrentTheme
        {
            get => _currentTheme;
            set
            {
                if (_currentTheme.Equals(value)) return;
                _currentTheme = value;
                NotifyPropertyChanged(CurrentThemeKey);
            }
        }
        #endregion

        #region ProgressRingIsActive
        public const string ProgressRingIsActiveKey = "ProgressRingIsActive";
        public bool ProgressRingIsActive
        {
            get => _progressRingIsActive;
            set
            {
                if (_progressRingIsActive.Equals(value)) return;
                _progressRingIsActive = value;
                NotifyPropertyChanged(ProgressRingIsActiveKey);
            }
        }
        #endregion

        #region LoadingScreenVisible
        public const string LoadingScreenVisibleKey = "LoadingScreenVisible";
        public bool LoadingScreenVisible
        {
            get => _loadingScreenVisible;
            set
            {
                if (_loadingScreenVisible.Equals(value)) return;
                _loadingScreenVisible = value;
                NotifyPropertyChanged(LoadingScreenVisibleKey);
            }
        }
        #endregion

        #region LoadingScreenMessage
        public const string LoadingScreenMessageKey = "LoadingScreenMessage";
        public string LoadingScreenMessage
        {
            get => _loadingScreenMessage;
            set
            {
                if (_loadingScreenMessage.Equals(value)) return;
                _loadingScreenMessage = value;
                NotifyPropertyChanged(LoadingScreenMessageKey);
            }
        }
        #endregion

        #region BackButtonEnabled
        public const string BackButtonEnabledKey = "BackButtonEnabled";
        public bool BackButtonEnabled
        {
            get => _backButtonEnabled;
            set
            {
                if (_backButtonEnabled.Equals(value)) return;
                _backButtonEnabled = value;
                NotifyPropertyChanged(BackButtonEnabledKey);
            }
        }
        #endregion

        #region MultiSelectionEnabled
        public const string MultiSelectionEnabledKey = "MultiSelectionEnabled";
        public bool MultiSelectionEnabled
        {
            get => _multiSelectionEnabled;
            set
            {
                if (_multiSelectionEnabled.Equals(value)) return;
                _multiSelectionEnabled = value;
                NotifyPropertyChanged(MultiSelectionEnabledKey);
            }
        }
        #endregion

        #region PlayAllButtonVisible
        public const string PlayAllButtonVisibleKey = "PlayAllButtonVisible";
        public bool PlayAllButtonVisible
        {
            get => _playAllButtonVisible;
            set
            {
                if (_playAllButtonVisible.Equals(value)) return;
                _playAllButtonVisible = value;
                NotifyPropertyChanged(PlayAllButtonVisibleKey);
            }
        }
        #endregion

        #region EditButtonVisible
        public const string EditButtonVisibleKey = "EditButtonVisible";
        public bool EditButtonVisible
        {
            get => _editButtonVisible;
            set
            {
                if (_editButtonVisible.Equals(value)) return;
                _editButtonVisible = value;
                NotifyPropertyChanged(EditButtonVisibleKey);
            }
        }
        #endregion

        #region TopButtonsCollapsed
        public const string TopButtonCollapsedKey = "TopButtonsCollapsed";
        public bool TopButtonsCollapsed
        {
            get => _topButtonsCollapsed;
            set
            {
                if (_topButtonsCollapsed.Equals(value)) return;
                _topButtonsCollapsed = value;
                NotifyPropertyChanged(TopButtonCollapsedKey);
            }
        }
        #endregion

        #region SearchAutoSuggestBoxVisible
        public const string SearchAutoSuggestBoxVisibleKey = "SearchAutoSuggestBoxVisible";
        public bool SearchAutoSuggestBoxVisible
        {
            get => _searchAutoSuggestBoxVisible;
            set
            {
                if (_searchAutoSuggestBoxVisible.Equals(value)) return;
                _searchAutoSuggestBoxVisible = value;
                NotifyPropertyChanged(SearchAutoSuggestBoxVisibleKey);
            }
        }
        #endregion

        #region SearchButtonVisible
        public const string SearchButtonVisibleKey = "SearchButtonVisible";
        public bool SearchButtonVisible
        {
            get => _searchButtonVisible;
            set
            {
                if (_searchButtonVisible.Equals(value)) return;
                _searchButtonVisible = value;
                NotifyPropertyChanged(SearchButtonVisibleKey);
            }
        }
        #endregion

        #region SelectAllFlyoutText
        public const string SelectAllFlyoutTextKey = "SelectAllFlyoutText";
        public string SelectAllFlyoutText
        {
            get => _selectAllFlyoutText;
            set
            {
                if (_selectAllFlyoutText.Equals(value)) return;
                _selectAllFlyoutText = value;
                NotifyPropertyChanged(SelectAllFlyoutTextKey);
            }
        }
        #endregion

        #region SelectAllFlyoutIcon
        public const string SelectAllFlyoutIconKey = "SelectAllFlyoutIcon";
        public SymbolIcon SelectAllFlyoutIcon
        {
            get => _selectAllFlyoutIcon;
            set
            {
                if (_selectAllFlyoutIcon.Equals(value)) return;
                _selectAllFlyoutIcon = value;
                NotifyPropertyChanged(SelectAllFlyoutIconKey);
            }
        }
        #endregion

        #region SoundTileWidth
        public const string SoundTileWidthKey = "SoundTileWidth";
        public double SoundTileWidth
        {
            get => _soundTileWidth;
            set
            {
                if (_soundTileWidth.Equals(value)) return;
                _soundTileWidth = value;
                NotifyPropertyChanged(SoundTileWidthKey);
            }
        }
        #endregion

        #region PlayingSoundsBarAcrylicBackgroundBrush
        public const string PlayingSoundsBarAcrylicBackgroundBrushKey = "PlayingSoundsBarAcrylicBackgroundBrush";
        public AcrylicBrush PlayingSoundsBarAcrylicBackgroundBrush
        {
            get => _playingSoundsBarAcrylicBackgroundBrush;
            set
            {
                if (_playingSoundsBarAcrylicBackgroundBrush.Equals(value)) return;
                _playingSoundsBarAcrylicBackgroundBrush = value;
                NotifyPropertyChanged(PlayingSoundsBarAcrylicBackgroundBrushKey);
            }
        }
        #endregion

        #region SecondaryWindowAcrylicBackgroundBrush
        public const string SecondaryWindowAcrylicBackgroundBrushKey = "SecondaryWindowAcrylicBackgroundBrush";
        public AcrylicBrush SecondaryWindowAcrylicBackgroundBrush
        {
            get => _secondaryWindowAcrylicBackgroundBrush;
            set
            {
                if (_secondaryWindowAcrylicBackgroundBrush.Equals(value)) return;
                _secondaryWindowAcrylicBackgroundBrush = value;
                NotifyPropertyChanged(SecondaryWindowAcrylicBackgroundBrushKey);
            }
        }
        #endregion

        #region ExportAndImportButtonsEnabled
        public const string ExportAndImportButtonsEnabledKey = "ExportAndImportButtonsEnabled";
        public bool ExportAndImportButtonsEnabled
        {
            get => _exportAndImportButtonsEnabled;
            set
            {
                if (_exportAndImportButtonsEnabled.Equals(value)) return;
                _exportAndImportButtonsEnabled = value;
                NotifyPropertyChanged(ExportAndImportButtonsEnabledKey);
            }
        }
        #endregion
        #endregion
        #endregion

        #region Settings
        #region UserId
        public const string UserIdKey = "UserId";
        public Guid UserId
        {
            get => _userId;
        }
        #endregion

        #region PlusPurchased
        public const string PlusPurchasedKey = "PlusPurchased";
        public bool PlusPurchased
        {
            get => _plusPurchased;
            set
            {
                if (_plusPurchased.Equals(value)) return;
                localSettings.Values[plusPurchasedKey] = value;
                _plusPurchased = value;
                NotifyPropertyChanged(PlusPurchasedKey);
            }
        }
        #endregion

        #region SavePlayingSounds
        public const string SavePlayingSoundsKey = "SavePlayingSounds";
        public bool SavePlayingSounds
        {
            get => _savePlayingSounds;
            set
            {
                if (_savePlayingSounds.Equals(value)) return;
                localSettings.Values[savePlayingSoundsKey] = value;
                _savePlayingSounds = value;
                NotifyPropertyChanged(SavePlayingSoundsKey);
            }
        }
        #endregion

        #region OpenMultipleSounds
        public const string OpenMultipleSoundsKey = "OpenMultipleSounds";
        public bool OpenMultipleSounds
        {
            get => _openMultipleSounds;
            set
            {
                if (_openMultipleSounds.Equals(value)) return;
                localSettings.Values[openMultipleSoundsKey] = value;
                _openMultipleSounds = value;
                NotifyPropertyChanged(OpenMultipleSoundsKey);
            }
        }
        #endregion

        #region MultiSoundPlayback
        public const string MultiSoundPlaybackKey = "MultiSoundPlayback";
        public bool MultiSoundPlayback
        {
            get => _multiSoundPlayback;
            set
            {
                if (_multiSoundPlayback.Equals(value)) return;
                localSettings.Values[multiSoundPlaybackKey] = value;
                _multiSoundPlayback = value;
                NotifyPropertyChanged(MultiSoundPlaybackKey);
            }
        }
        #endregion

        #region ShowSoundsPivot
        public const string ShowSoundsPivotKey = "ShowSoundsPivot";
        public bool ShowSoundsPivot
        {
            get => _showSoundsPivot;
            set
            {
                if (_showSoundsPivot.Equals(value)) return;
                localSettings.Values[showSoundsPivotKey] = value;
                _showSoundsPivot = value;
                NotifyPropertyChanged(ShowSoundsPivotKey);
            }
        }
        #endregion

        #region SoundOrder
        public const string SoundOrderKey = "SoundOrder";
        public SoundOrder SoundOrder
        {
            get => _soundOrder;
            set
            {
                if (_soundOrder.Equals(value)) return;
                localSettings.Values[newSoundOrderKey] = (int)value;
                _soundOrder = value;
                NotifyPropertyChanged(SoundOrderKey);
            }
        }
        #endregion

        #region MultipleOutputDevices
        public const string MultipleOutputDevicesKey = "MultipleOutputDevices";
        public bool MultipleOutputDevices
        {
            get => _multipleOutputDevices;
            set
            {
                if (_multipleOutputDevices.Equals(value)) return;
                localSettings.Values[multipleOutputDevicesKey] = value;
                _multipleOutputDevices = value;
                NotifyPropertyChanged(MultipleOutputDevicesKey);
            }
        }
        #endregion

        #region UseStandardOutputDevice
        public const string UseStandardOutputDeviceKey = "UseStandardOutputDevice";
        public bool UseStandardOutputDevice
        {
            get => _useStandardOutputDevice;
            set
            {
                if (_useStandardOutputDevice.Equals(value)) return;
                localSettings.Values[useStandardOutputDeviceKey] = value;
                _useStandardOutputDevice = value;
                NotifyPropertyChanged(UseStandardOutputDeviceKey);
            }
        }
        #endregion

        #region OutputDevice
        public const string OutputDeviceKey = "OutputDevice";
        public string OutputDevice
        {
            get => _outputDevice;
            set
            {
                if (_outputDevice.Equals(value)) return;
                localSettings.Values[outputDeviceKey] = value;
                _outputDevice = value;
                NotifyPropertyChanged(OutputDeviceKey);
            }
        }
        #endregion

        #region ShowListView
        public const string ShowListViewKey = "ShowListView";
        public bool ShowListView
        {
            get => _showListView;
            set
            {
                if (_showListView.Equals(value)) return;
                localSettings.Values[showListViewKey] = value;
                _showListView = value;
                NotifyPropertyChanged(ShowListViewKey);
            }
        }
        #endregion

        #region ShowCategoriesIcons
        public const string ShowCategoriesIconsKey = "ShowCategoriesIcons";
        public bool ShowCategoriesIcons
        {
            get => _showCategoriesIcons;
            set
            {
                if (_showCategoriesIcons.Equals(value)) return;
                localSettings.Values[showCategoriesIconsKey] = value;
                _showCategoriesIcons = value;
                NotifyPropertyChanged(ShowCategoriesIconsKey);
            }
        }
        #endregion

        #region ShowAcrylicBackground
        public const string ShowAcrylicBackgroundKey = "ShowAcrylicBackground";
        public bool ShowAcrylicBackground
        {
            get => _showAcrylicBackground;
            set
            {
                if (_showAcrylicBackground.Equals(value)) return;
                localSettings.Values[showAcrylicBackgroundKey] = value;
                _showAcrylicBackground = value;
                NotifyPropertyChanged(ShowAcrylicBackgroundKey);
            }
        }
        #endregion

        #region LiveTile
        public const string LiveTileKey = "LiveTile";
        public bool LiveTile
        {
            get => _liveTile;
            set
            {
                if (_liveTile.Equals(value)) return;
                localSettings.Values[liveTileKey] = value;
                _liveTile = value;
                NotifyPropertyChanged(LiveTileKey);
            }
        }
        #endregion

        #region Theme
        public const string ThemeKey = "Theme";
        public AppTheme Theme
        {
            get => _theme;
            set
            {
                if (_theme.Equals(value)) return;
                string themeString = "system";
                switch (value)
                {
                    case AppTheme.Light:
                        themeString = "light";
                        break;
                    case AppTheme.Dark:
                        themeString = "dark";
                        break;
                }

                localSettings.Values[themeKey] = themeString;
                _theme = value;
                NotifyPropertyChanged(ThemeKey);
            }
        }
        #endregion

        #region PlayingSoundsBarWidth
        public const string PlayingSoundsBarWidthKey = "PlayingSoundsBarWidth";
        public double PlayingSoundsBarWidth
        {
            get => _playingSoundsBarWidth;
            set
            {
                if (_playingSoundsBarWidth.Equals(value)) return;
                localSettings.Values[playingSoundsBarWidthKey] = value;
                _playingSoundsBarWidth = value;
                NotifyPropertyChanged(PlayingSoundsBarWidthKey);
            }
        }
        #endregion

        #region Muted
        public const string MutedKey = "Muted";
        public bool Muted
        {
            get => _muted;
            set
            {
                if (_muted.Equals(value)) return;
                localSettings.Values[mutedKey] = value;
                _muted = value;
                NotifyPropertyChanged(MutedKey);
            }
        }
        #endregion

        #region Volume
        public const string VolumeKey = "Volume";
        public int Volume
        {
            get => _volume;
            set
            {
                if (_volume.Equals(value)) return;
                localSettings.Values[volumeKey] = value;
                _volume = value;
                NotifyPropertyChanged(VolumeKey);
            }
        }
        #endregion

        #region AppStartCounter
        public const string AppStartCounterKey = "AppStartCounter";
        public int AppStartCounter
        {
            get => _appStartCounter;
            set
            {
                if (_appStartCounter.Equals(value)) return;
                localSettings.Values[appStartCounterKey] = value;
                _appStartCounter = value;
                NotifyPropertyChanged(AppStartCounterKey);
            }
        }
        #endregion

        #region AppReviewed
        public const string AppReviewedKey = "AppReviewed";
        public bool AppReviewed
        {
            get => _appReviewed;
            set
            {
                if (_appReviewed.Equals(value)) return;
                localSettings.Values[appReviewedKey] = value;
                _appReviewed = value;
                NotifyPropertyChanged(AppReviewedKey);
            }
        }
        #endregion

        #region ShowContinuePlaylistDownloadIAN
        public const string ShowContinuePlaylistDownloadIANKey = "ShowContinuePlaylistDownloadIAN";
        public bool ShowContinuePlaylistDownloadIAN
        {
            get => _showContinuePlaylistDownloadIAN;
            set
            {
                if (_showContinuePlaylistDownloadIAN.Equals(value)) return;
                localSettings.Values[showContinuePlaylistDownloadIANKey] = value;
                _showContinuePlaylistDownloadIAN = value;
                NotifyPropertyChanged(ShowContinuePlaylistDownloadIANKey);
            }
        }
        #endregion

        #region SoundRecorderMinimizeWarningClosed
        public const string SoundRecorderMinimizeWarningClosedKey = "SoundRecorderMinimizeWarningClosed";
        public bool SoundRecorderMinimizeWarningClosed
        {
            get => _soundRecorderMinimizeWarningClosed;
            set
            {
                if (_soundRecorderMinimizeWarningClosed.Equals(value)) return;
                localSettings.Values[soundRecorderMinimizeWarningClosedKey] = value;
                _soundRecorderMinimizeWarningClosed = value;
                NotifyPropertyChanged(SoundRecorderMinimizeWarningClosedKey);
            }
        }
        #endregion

        #region IsFadeInEffectEnabled
        public const string IsFadeInEffectEnabledKey = "IsFadeInEffectEnabled";
        public bool IsFadeInEffectEnabled
        {
            get => _isFadeInEffectEnabled;
            set
            {
                if (_isFadeInEffectEnabled.Equals(value)) return;
                localSettings.Values[isFadeInEffectEnabledKey] = value;
                _isFadeInEffectEnabled = value;
                NotifyPropertyChanged(IsFadeInEffectEnabledKey);
            }
        }
        #endregion

        #region FadeInEffectDuration
        public const string FadeInEffectDurationKey = "FadeInEffectDuration";
        public int FadeInEffectDuration
        {
            get => _fadeInEffectDuration;
            set
            {
                if (_fadeInEffectDuration.Equals(value)) return;
                localSettings.Values[fadeInEffectDurationKey] = value;
                _fadeInEffectDuration = value;
                NotifyPropertyChanged(FadeInEffectDurationKey);
            }
        }
        #endregion

        #region IsFadeOutEffectEnabled
        public const string IsFadeOutEffectEnabledKey = "IsFadeOutEffectEnabled";
        public bool IsFadeOutEffectEnabled
        {
            get => _isFadeOutEffectEnabled;
            set
            {
                if (_isFadeOutEffectEnabled.Equals(value)) return;
                localSettings.Values[isFadeOutEffectEnabledKey] = value;
                _isFadeOutEffectEnabled = value;
                NotifyPropertyChanged(IsFadeOutEffectEnabledKey);
            }
        }
        #endregion

        #region FadeOutEffectDuration
        public const string FadeOutEffectDurationKey = "FadeOutEffectDuration";
        public int FadeOutEffectDuration
        {
            get => _fadeOutEffectDuration;
            set
            {
                if (_fadeOutEffectDuration.Equals(value)) return;
                localSettings.Values[fadeOutEffectDurationKey] = value;
                _fadeOutEffectDuration = value;
                NotifyPropertyChanged(FadeOutEffectDurationKey);
            }
        }
        #endregion

        #region IsEchoEffectEnabled
        public const string IsEchoEffectEnabledKey = "IsEchoEffectEnabled";
        public bool IsEchoEffectEnabled
        {
            get => _isEchoEffectEnabled;
            set
            {
                if (_isEchoEffectEnabled.Equals(value)) return;
                localSettings.Values[isEchoEffectEnabledKey] = value;
                _isEchoEffectEnabled = value;
                NotifyPropertyChanged(IsEchoEffectEnabledKey);
            }
        }
        #endregion

        #region EchoEffectDelay
        public const string EchoEffectDelayKey = "EchoEffectDelay";
        public int EchoEffectDelay
        {
            get => _echoEffectDelay;
            set
            {
                if (_echoEffectDelay.Equals(value)) return;
                localSettings.Values[echoEffectDelayKey] = value;
                _echoEffectDelay = value;
                NotifyPropertyChanged(EchoEffectDelayKey);
            }
        }
        #endregion

        #region IsLimiterEffectEnabled
        public const string IsLimiterEffectEnabledKey = "IsLimiterEffectEnabled";
        public bool IsLimiterEffectEnabled
        {
            get => _isLimiterEffectEnabled;
            set
            {
                if (_isLimiterEffectEnabled.Equals(value)) return;
                localSettings.Values[isLimiterEffectEnabledKey] = value;
                _isLimiterEffectEnabled = value;
                NotifyPropertyChanged(IsLimiterEffectEnabledKey);
            }
        }
        #endregion

        #region LimiterEffectDelay
        public const string LimiterEffectLoudnessKey = "LimiterEffectLoudness";
        public int LimiterEffectLoudness
        {
            get => _limiterEffectLoudness;
            set
            {
                if (_limiterEffectLoudness.Equals(value)) return;
                localSettings.Values[limiterEffectLoudnessKey] = value;
                _limiterEffectLoudness = value;
                NotifyPropertyChanged(LimiterEffectLoudnessKey);
            }
        }
        #endregion

        #region IsReverbEffectEnabled
        public const string IsReverbEffectEnabledKey = "IsReverbEffectEnabled";
        public bool IsReverbEffectEnabled
        {
            get => _isReverbEffectEnabled;
            set
            {
                if (_isReverbEffectEnabled.Equals(value)) return;
                localSettings.Values[isReverbEffectEnabledKey] = value;
                _isReverbEffectEnabled = value;
                NotifyPropertyChanged(IsReverbEffectEnabledKey);
            }
        }
        #endregion

        #region ReverbEffectDecay
        public const string ReverbEffectDecayKey = "ReverbEffectDecay";
        public double ReverbEffectDecay
        {
            get => _reverbEffectDecay;
            set
            {
                if (_reverbEffectDecay.Equals(value)) return;
                localSettings.Values[reverbEffectDecayKey] = value;
                _reverbEffectDecay = value;
                NotifyPropertyChanged(ReverbEffectDecayKey);
            }
        }
        #endregion

        #region IsPitchShiftEffectEnabled
        public const string IsPitchShiftEffectEnabledKey = "IsPitchShiftEffectEnabled";
        public bool IsPitchShiftEffectEnabled
        {
            get => _isPitchShiftEffectEnabled;
            set
            {
                if (_isPitchShiftEffectEnabled.Equals(value)) return;
                localSettings.Values[isPitchShiftEffectEnabledKey] = value;
                _isPitchShiftEffectEnabled = value;
                NotifyPropertyChanged(IsPitchShiftEffectEnabledKey);
            }
        }
        #endregion

        #region PitchShiftEffectFactor
        public const string PitchShiftEffectFactorKey = "PitchShiftEffectFactor";
        public double PitchShiftEffectFactor
        {
            get => _pitchShiftEffectFactor;
            set
            {
                if (_pitchShiftEffectFactor.Equals(value)) return;
                localSettings.Values[pitchShiftEffectFactorKey] = value;
                _pitchShiftEffectFactor = value;
                NotifyPropertyChanged(PitchShiftEffectFactorKey);
            }
        }
        #endregion
        #endregion

        #region Events
        public void TriggerSoundsLoadedEvent(object sender)
        {
            SoundsLoaded?.Invoke(sender, new EventArgs());
        }

        public void TriggerPlayingSoundsLoadedEvent(object sender)
        {
            PlayingSoundsLoaded?.Invoke(sender, new EventArgs());
        }

        public void TriggerCategoriesLoadedEvent(object sender)
        {
            CategoriesLoaded?.Invoke(sender, new EventArgs());
        }

        public void TriggerUserSyncFinishedEvent(object sender, EventArgs args)
        {
            UserSyncFinished?.Invoke(sender, args);
        }

        public void TriggerUserPlanChangedEvent(object sender, EventArgs args)
        {
            UserPlanChanged?.Invoke(sender, args);
        }


        public void TriggerSoundPromotionStartedEvent(object sender, EventArgs args)
        {
            SoundPromotionStarted?.Invoke(sender, args);
        }

        public void TriggerCategoryAddedEvent(object sender, CategoryEventArgs args)
        {
            CategoryAdded?.Invoke(sender, args);
        }

        public void TriggerCategoryUpdatedEvent(object sender, CategoryEventArgs args)
        {
            CategoryUpdated?.Invoke(sender, args);
        }

        public void TriggerCategoryDeletedEvent(object sender, CategoryEventArgs args)
        {
            CategoryDeleted?.Invoke(sender, args);
        }

        public void TriggerSoundDeletedEvent(object sender, SoundEventArgs args)
        {
            SoundDeleted?.Invoke(sender, args);
        }

        public void TriggerSelectAllSoundsEvent(object sender, RoutedEventArgs e)
        {
            SelectAllSounds?.Invoke(sender, e);
        }

        public void TriggerSoundTileSizeChangedEvent(object sender, SizeChangedEventArgs e)
        {
            SoundTileSizeChanged?.Invoke(sender, e);
        }

        public void TriggerHidePlayingSoundItemEvent(object sender, HidePlayingSoundItemEventArgs args)
        {
            HidePlayingSoundItem?.Invoke(sender, args);
        }

        public void TriggerTableObjectFileDownloadProgressChangedEvent(object sender, TableObjectFileDownloadProgressChangedEventArgs args)
        {
            TableObjectFileDownloadProgressChanged?.Invoke(sender, args);
        }

        public void TriggerTableObjectFileDownloadCompletedEvent(object sender, TableObjectFileDownloadCompletedEventArgs args)
        {
            TableObjectFileDownloadCompleted?.Invoke(sender, args);
        }

        public void TriggerShowInAppNotificationEvent(object sender, ShowInAppNotificationEventArgs args)
        {
            var ianItem = FileManager.InAppNotificationItems.Find(item => item.InAppNotificationType == args.Type);
            var newIanItem = FileManager.CreateInAppNotificationItem(args);

            if (ianItem != null)
            {
                ianItem.InAppNotification.Dismiss();
                FileManager.InAppNotificationItems.Remove(ianItem);
            }

            FileManager.InAppNotificationItems.Add(newIanItem);
            ShowInAppNotification?.Invoke(sender, args);
        }

        public void TriggerSoundDownloadEvent(object sender, EventArgs args)
        {
            SoundDownload?.Invoke(sender, args);
        }

        public void TriggerPlaySoundEvent(object sender, PlaySoundEventArgs args)
        {
            PlaySound?.Invoke(sender, args);
        }

        public void TriggerPlaySoundsEvent(object sender, PlaySoundsEventArgs args)
        {
            PlaySounds?.Invoke(sender, args);
        }

        public void TriggerPlaySoundAfterPlayingSoundsLoadedEvent(object sender, PlaySoundAfterPlayingSoundsLoadedEventArgs args)
        {
            PlaySoundAfterPlayingSoundsLoaded?.Invoke(sender, args);
        }

        public void TriggerPlayLocalSoundAfterPlayingSoundsLoadedEvent(object sender, PlayLocalSoundAfterPlayingSoundsLoadedEventArgs args)
        {
            PlayLocalSoundAfterPlayingSoundsLoaded?.Invoke(sender, args);
        }
        #endregion

        #region NotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
