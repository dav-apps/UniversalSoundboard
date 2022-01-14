using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using UniversalSoundboard.Components;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Models;
using UniversalSoundboard.Pages;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace UniversalSoundboard.Common
{
    public class ItemViewHolder : INotifyPropertyChanged
    {
        #region Constants for the localSettings keys
        private const string playingSoundsListVisibleKey = "playingSoundsListVisible";
        private const string savePlayingSoundsKey = "savePlayingSounds";
        private const string openMultipleSoundsKey = "openMultipleSounds";
        private const string multiSoundPlaybackKey = "multiSoundPlayback";
        private const string showSoundsPivotKey = "showSoundsPivot";
        private const string soundOrderKey = "soundOrder";
        private const string soundOrderReversedKey = "soundOrderReversed";
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
        #endregion

        #region Constants for localSettings defaults
        private const bool playingSoundsListVisibleDefault = true;
        private const bool savePlayingSoundsDefault = true;
        private const bool openMultipleSoundsDefault = true;
        private const bool multiSoundPlaybackDefault = false;
        private const bool showSoundsPivotDefault = true;
        private const FileManager.SoundOrder soundOrderDefault = FileManager.SoundOrder.Custom;
        private const bool soundOrderReversedDefault = false;
        private const bool useStandardOutputDeviceDefault = true;
        private const string outputDeviceDefault = "";
        private const bool showListViewDefault = false;
        private const bool showCategoriesIconsDefault = false;
        private const bool showAcrylicBackgroundDefault = true;
        private const bool liveTileDefault = true;
        private const FileManager.AppTheme themeDefault = FileManager.AppTheme.System;
        private const double playingSoundsBarWidthDefault = 0.35;
        private const bool mutedDefault = false;
        private const int volumeDefault = 100;
        #endregion

        #region Variables
        #region State
        private FileManager.AppState _appState;                             // The current state of the app
        private string _title;                                              // The title text
        private Type _page;                                                 // The current page
        private string _searchQuery;                                        // The string entered into the search box
        private bool _allSoundsChanged;                                     // If there was made change to one or multiple sounds so that a reload of the sounds is required
        private Guid _selectedCategory;                                     // The index of the currently selected category in the category list
        private bool _exporting;                                            // If true the soundboard is currently being exported
        private bool _importing;                                            // If true a soundboard is currently being imported
        private string _exportMessage;                                      // The text describing the status of the export
        private string _importMessage;                                      // The text describing the status of the import
        private ulong _soundboardSize;                                      // The size of the entire soundboard in byte
        private Guid _activePlayingSound;                                   // The PlayingSound that was last activated and is currently visible in SystemMediaTransportControls
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
        #endregion

        #region Layout & Design
        private FileManager.AppTheme _currentTheme;                         // The current theme of the app; is either Light or Dark
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
        private bool _exportAndImportButtonsEnabled;                        // If true shows the export and import buttons on the settings page
        #endregion
        #endregion

        #region Settings
        private bool _playingSoundsListVisible;                             // If true shows the Playing Sounds list at the right
        private bool _savePlayingSounds;                                    // If true saves the PlayingSounds and loads them when starting the app
        private bool _openMultipleSounds;                                   // If false, removes all PlayingSounds whenever the user opens a new one; if true, adds new opened sounds to existing PlayingSounds
        private bool _multiSoundPlayback;                                   // If true, can play multiple sounds at the same time; if false, stops the currently playing sound when playing another sound
        private bool _showSoundsPivot;                                      // If true shows the pivot to select Sounds or Favourite sounds
        private FileManager.SoundOrder _soundOrder;                         // The selected sound order in the settings
        private bool _soundOrderReversed;                                   // If the sound order is descending (false) or ascending (true)
        private bool _useStandardOutputDevice;                              // If true, the standard output device of the OS is used for playback
        private string _outputDevice;                                       // The id of the selected output device
        private bool _showListView;                                         // If true, shows the sounds on the SoundPage in a ListView
        private bool _showCategoriesIcons;                                  // If true shows the icon of the category on the sound tile
        private bool _showAcrylicBackground;                                // If true the acrylic background is visible
        private bool _liveTile;                                             // If true, shows the live tile
        private FileManager.AppTheme _theme;                                // The design theme of the app
        private double _playingSoundsBarWidth;                              // The relative width of the PlayingSoundsBar in percent
        private bool _muted;                                                // If true, the volume is muted
        private int _volume;                                                // The volume of the entire app, between 0 and 100
        #endregion

        #region Events
        public event EventHandler<EventArgs> SoundsLoaded;                                                      // Is triggered when the sounds at startup were loaded
        public event EventHandler<EventArgs> PlayingSoundsLoaded;                                               // Is triggered when the playing sounds at startup were loaded
        public event EventHandler<EventArgs> CategoriesLoaded;                                                  // Is triggered when all categories were loaded into the Categories List
        public event EventHandler<EventArgs> UserSyncFinished;                                                  // Is triggered when the user infos and the profile image were downloaded from the server
        public event EventHandler<CategoryEventArgs> CategoryAdded;                                             // Is triggered when a category was added
        public event EventHandler<CategoryEventArgs> CategoryUpdated;                                           // Is triggered when a category was updated
        public event EventHandler<CategoryEventArgs> CategoryDeleted;                                           // Is triggered when a category was deleted
        public event EventHandler<SoundEventArgs> SoundDeleted;                                                 // Is triggered when a sound was deleted
        public event EventHandler<RoutedEventArgs> SelectAllSounds;                                             // Trigger this event to select all sounds or deselect all sounds when all sounds are selected
        public event EventHandler<SizeChangedEventArgs> SoundTileSizeChanged;                                   // Is triggered when the size of the sound tiles in the GridViews has changed
        public event EventHandler<EventArgs> PlayingSoundItemStartSoundsListAnimation;                          // Is triggered from the SoundPage for starting the appropriate animation
        public event EventHandler<PlayingSoundItemEventArgs> PlayingSoundItemShowSoundsListAnimationStarted;    // Is triggered when the animation of a PlayingSound item to show the sounds list started
        public event EventHandler<PlayingSoundItemEventArgs> PlayingSoundItemShowSoundsListAnimationEnded;      // Is triggered when the animation of a PlayingSound item to show the sounds list ended
        public event EventHandler<PlayingSoundItemEventArgs> PlayingSoundItemHideSoundsListAnimationStarted;    // Is triggered when the animation of a PlayingSound item to hide the sounds list started
        public event EventHandler<PlayingSoundItemEventArgs> PlayingSoundItemHideSoundsListAnimationEnded;      // Is triggered when the animation of a PlayingSound item to hide the sounds list ended
        public event EventHandler<PlayingSoundItemEventArgs> ShowPlayingSoundItemStarted;                       // Is triggered when the PlayingSound appearing animation has started
        public event EventHandler<PlayingSoundItemEventArgs> ShowPlayingSoundItemEnded;                         // Is triggered when the PlayingSound appearing animation has ended
        public event EventHandler<PlayingSoundItemEventArgs> RemovePlayingSoundItemStarted;                     // Is triggered before a PlayingSound is removed
        public event EventHandler<PlayingSoundItemEventArgs> RemovePlayingSoundItemEnded;                       // Is triggered after a PlayingSound was removed
        public event EventHandler<TableObjectFileDownloadProgressChangedEventArgs> TableObjectFileDownloadProgressChanged;  // Is triggered when the file of a TableObject is being downloaded and the progress changed
        public event EventHandler<TableObjectFileDownloadCompletedEventArgs> TableObjectFileDownloadCompleted;  // Is triggered from TriggerAction when the file of a TableObject was finished
        public event EventHandler<ShowInAppNotificationEventArgs> ShowInAppNotification;                        // Trigger this event to show the InAppNotification on the SoundPage
        public event EventHandler<SetInAppNotificationMessageEventArgs> SetInAppNotificationMessage;            // Trigger this event to set the message of the currently visible InAppNotification
        public event EventHandler<SetInAppNotificationProgressEventArgs> SetInAppNotificationProgress;          // Trigger this event to set the progress of the progress ring of the currently visible InAppNotification
        public event EventHandler<EventArgs> DismissInAppNotification;                                          // Trigger this event to dismiss the InAppNotification if it is currently visible
        #endregion

        #region Local variables
        readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        #endregion

        public ItemViewHolder()
        {
            ResourceLoader loader = new ResourceLoader();

            // Set the default values
            #region State
            _title = loader.GetString("AllSounds");
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
            #endregion

            #region Layout & Design
            _currentTheme = FileManager.AppTheme.Light;
            _progressRingIsActive = false;
            _loadingScreenVisible = false;
            _loadingScreenMessage = "";
            _backButtonEnabled = false;
            _playAllButtonVisible = false;
            _editButtonVisible = false;
            _topButtonsCollapsed = false;
            _searchAutoSuggestBoxVisible = true;
            _searchButtonVisible = false;
            _selectAllFlyoutText = loader.GetString("MoreButton_SelectAllFlyout-SelectAll");
            _soundTileWidth = 200;
            _exportAndImportButtonsEnabled = true;

            try
            {
                _selectAllFlyoutIcon = new SymbolIcon(Symbol.SelectAll);
                _playingSoundsBarAcrylicBackgroundBrush = new AcrylicBrush();
            }
            catch { }
            #endregion

            #region Settings
            #region playingSoundsListVisible
            if (localSettings.Values[playingSoundsListVisibleKey] == null)
                _playingSoundsListVisible = playingSoundsListVisibleDefault;
            else
                _playingSoundsListVisible = (bool)localSettings.Values[playingSoundsListVisibleKey];
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
            if (localSettings.Values[soundOrderKey] == null)
                _soundOrder = soundOrderDefault;
            else
                _soundOrder = (FileManager.SoundOrder)localSettings.Values[soundOrderKey];
            #endregion

            #region soundOrderReversed
            if (localSettings.Values[soundOrderReversedKey] == null)
                _soundOrderReversed = soundOrderReversedDefault;
            else
                _soundOrderReversed = (bool)localSettings.Values[soundOrderReversedKey];
            #endregion

            #region useStandardOutputDevice
            if (localSettings.Values[useStandardOutputDeviceKey] == null)
                _useStandardOutputDevice = useStandardOutputDeviceDefault;
            else
                _useStandardOutputDevice = (bool)localSettings.Values[useStandardOutputDeviceKey];
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
                        _theme = FileManager.AppTheme.Light;
                        break;
                    case "dark":
                        _theme = FileManager.AppTheme.Dark;
                        break;
                    case "system":
                        _theme = FileManager.AppTheme.System;
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
            #endregion
        }

        #region Access Modifiers
        #region State
        #region AppState
        public const string AppStateKey = "AppState";
        public FileManager.AppState AppState
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
        #endregion

        #region Layout & Design
        #region CurrentTheme
        public const string CurrentThemeKey = "CurrentTheme";
        public FileManager.AppTheme CurrentTheme
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
        #region PlayingSoundsListVisible
        public const string PlayingSoundsListVisibleKey = "PlayingSoundsListVisible";
        public bool PlayingSoundsListVisible
        {
            get => _playingSoundsListVisible;
            set
            {
                if (_playingSoundsListVisible.Equals(value)) return;
                localSettings.Values[playingSoundsListVisibleKey] = value;
                _playingSoundsListVisible = value;
                NotifyPropertyChanged(PlayingSoundsListVisibleKey);
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
        public FileManager.SoundOrder SoundOrder
        {
            get => _soundOrder;
            set
            {
                if (_soundOrder.Equals(value)) return;
                localSettings.Values[soundOrderKey] = (int)value;
                _soundOrder = value;
                NotifyPropertyChanged(SoundOrderKey);
            }
        }
        #endregion

        #region SoundOrderReversed
        public const string SoundOrderReversedKey = "SoundOrderReversed";
        public bool SoundOrderReversed
        {
            get => _soundOrderReversed;
            set
            {
                if (_soundOrderReversed.Equals(value)) return;
                localSettings.Values[soundOrderReversedKey] = value;
                _soundOrderReversed = value;
                NotifyPropertyChanged(SoundOrderReversedKey);
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
        public FileManager.AppTheme Theme
        {
            get => _theme;
            set
            {
                if (_theme.Equals(value)) return;
                string themeString = "system";
                switch (value)
                {
                    case FileManager.AppTheme.Light:
                        themeString = "light";
                        break;
                    case FileManager.AppTheme.Dark:
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

        public void TriggerUserSyncFinished(object sender, EventArgs args)
        {
            UserSyncFinished?.Invoke(sender, args);
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

        public void TriggerPlayingSoundItemStartSoundsListAnimationEvent(object sender)
        {
            PlayingSoundItemStartSoundsListAnimation?.Invoke(sender, new EventArgs());
        }

        public void TriggerPlayingSoundItemShowSoundsListAnimationStartedEvent(object sender, PlayingSoundItemEventArgs args)
        {
            PlayingSoundItemShowSoundsListAnimationStarted?.Invoke(sender, args);
        }

        public void TriggerPlayingSoundItemShowSoundsListAnimationEndedEvent(object sender, PlayingSoundItemEventArgs args)
        {
            PlayingSoundItemShowSoundsListAnimationEnded?.Invoke(sender, args);
        }

        public void TriggerPlayingSoundItemHideSoundsListAnimationStartedEvent(object sender, PlayingSoundItemEventArgs args)
        {
            PlayingSoundItemHideSoundsListAnimationStarted?.Invoke(sender, args);
        }

        public void TriggerPlayingSoundItemHideSoundsListAnimationEndedEvent(object sender, PlayingSoundItemEventArgs args)
        {
            PlayingSoundItemHideSoundsListAnimationEnded?.Invoke(sender, args);
        }

        public void TriggerShowPlayingSoundItemStartedEvent(object sender, PlayingSoundItemEventArgs args)
        {
            ShowPlayingSoundItemStarted?.Invoke(sender, args);
        }

        public void TriggerShowPlayingSoundItemEndedEvent(object sender, PlayingSoundItemEventArgs args)
        {
            ShowPlayingSoundItemEnded?.Invoke(sender, args);
        }

        public void TriggerRemovePlayingSoundItemStartedEvent(object sender, PlayingSoundItemEventArgs args)
        {
            RemovePlayingSoundItemStarted?.Invoke(sender, args);
        }

        public void TriggerRemovePlayingSoundItemEndedEvent(object sender, PlayingSoundItemEventArgs args)
        {
            RemovePlayingSoundItemEnded?.Invoke(sender, args);
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
            FileManager.SetLastInAppNotificationEventArgs(args);
            ShowInAppNotification?.Invoke(sender, args);
        }

        public void TriggerSetInAppNotificationMessageEvent(object sender, SetInAppNotificationMessageEventArgs args)
        {
            FileManager.SetMessageOfLastInAppNotificationEventArgs(args.Message);
            SetInAppNotificationMessage?.Invoke(sender, args);
        }

        public void TriggerSetInAppNotificationProgressEvent(object sender, SetInAppNotificationProgressEventArgs args)
        {
            SetInAppNotificationProgress?.Invoke(sender, args);
        }

        public void TriggerDismissInAppNotificationEvent(object sender, EventArgs args)
        {
            DismissInAppNotification?.Invoke(sender, args);
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
