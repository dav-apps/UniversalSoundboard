using davClassLibrary.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using UniversalSoundBoard.DataAccess;
using UniversalSoundBoard.Models;
using UniversalSoundBoard.Pages;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace UniversalSoundBoard.Common
{
    public class ItemViewHolder : INotifyPropertyChanged
    {
        #region Constants for the localSettings keys
        private const string themeKey = "theme";
        private const string playingSoundsListVisibleKey = "playingSoundsListVisible";
        private const string playOneSoundAtOnceKey = "playOneSoundAtOnce";
        private const string liveTileKey = "liveTile";
        private const string showListViewKey = "showListView";
        private const string showCategoryIconKey = "showCategoryIcon";
        private const string showSoundsPivotKey = "showSoundsPivot";
        private const string savePlayingSoundsKey = "savePlayingSounds";
        private const string volumeKey = "volume";
        private const string showAcrylicBackgroundKey = "showAcrylicBackground";
        private const string soundOrderKey = "soundOrder";
        private const string soundOrderReversedKey = "soundOrderReversed";
        #endregion

        #region Constants for localSettings defaults
        private const FileManager.AppTheme themeDefault = FileManager.AppTheme.System;
        private const bool playingSoundsListVisibleDefault = true;
        private const bool playOneSoundAtOnceDefault = false;
        private const bool liveTileDefault = true;
        private const bool showListViewDefault = false;
        private const bool showCategoryIconDefault = true;
        private const bool showSoundsPivotDefault = true;
        private const bool savePlayingSoundsDefault = true;
        private const double volumeDefault = 1.0;
        private const bool showAcrylicBackgroundDefault = true;
        private const FileManager.SoundOrder soundOrderDefault = FileManager.SoundOrder.Custom;
        private const bool soundOrderReversedDefault = false;
        #endregion

        #region Variables
        #region State
        private string _title;                                              // The title text
        private Type _page;                                                 // The current page
        private string _searchQuery;                                        // The string entered into the search box
        private bool _allSoundsChanged;                                     // If there was made change to one or multiple sounds so that a reload of the sounds is required
        private Guid _selectedCategory;                                     // The index of the currently selected category in the category list
        private DavUser _user;                                              // The User object with username and avatar
        private bool _exporting;                                            // If true the soundboard is currently being exported
        private bool _exported;                                             // If true the soundboard was exported in the app session
        private bool _importing;                                            // If true a soundboard is currently being imported
        private bool _imported;                                             // If true a soundboard was import in the app session
        private string _exportMessage;                                      // The text describing the status of the export
        private string _importMessage;                                      // The text describing the status of the import
        private string _soundboardSize;                                     // The text shown on the settings page which describes the size of the soundboard
        #endregion

        #region Lists
        public List<Category> Categories { get; }                           // A list of all categories
        public ObservableCollection<Sound> AllSounds { get; }               // A list of all sounds, unsorted
        public ObservableCollection<Sound> Sounds { get; }                  // A list of the sounds which are displayed when the Sound pivot is selected, sorted by the selected sort option
        public ObservableCollection<Sound> FavouriteSounds { get; }         // A list of the favourite sound which are displayed when the Favourite pivot is selected, sorted by the selected sort option
        public ObservableCollection<Sound> SelectedSounds { get; }          // A list of the sounds which are selected
        public ObservableCollection<PlayingSound> PlayingSounds { get; }    // A list of the Playing Sounds which are displayed in the right menu
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
        private double _playingSoundsBarWidth;                              // Holds the width of the right playing sound bar to update the width of the acrylic background in the NavigationViewHeader
        private AcrylicBrush _playingSoundsBarAcrylicBackgroundBrush;       // This represents the background of the PlayingSoundsBar
        private bool _exportAndImportButtonsEnabled;                        // If true shows the export and import buttons on the settings page
        #endregion
        #endregion

        #region Settings
        private FileManager.AppTheme _theme;                                // The design theme of the app
        private bool _playingSoundsListVisible;                             // If true shows the Playing Sounds list at the right
        private bool _playOneSoundAtOnce;                                   // If true plays only one sound at a time
        private bool _liveTileEnabled;                                      // If true, show the live tile
        private bool _showListView;                                         // If true, shows the sounds on the SoundPage in a ListView
        private bool _showCategoryIcon;                                     // If true shows the icon of the category on the sound tile
        private bool _showSoundsPivot;                                      // If true shows the pivot to select Sounds or Favourite sounds
        private bool _savePlayingSounds;                                    // If true saves the PlayingSounds and loads them when starting the app
        private double _volume;                                             // The default volume for all PlayingSounds, between 0 and 1
        private bool _showAcrylicBackground;                                // If true the acrylic background is visible
        private FileManager.SoundOrder _soundOrder;                         // The selected sound order in the settings
        private bool _soundOrderReversed;                                   // If the sound order is descending (false) or ascending (true)
        #endregion

        #region Events
        public event EventHandler ThemeChangedEvent;                                // This is triggered when the user changes the theme in the settings
        public event EventHandler CategoriesUpdatedEvent;                           // Is triggered when all categories were loaded into the Categories ObservableCollection
        public event EventHandler<Guid> CategoryUpdatedEvent;                       // Is triggered when a category was updated
        public event EventHandler<Guid> CategoryRemovedEvent;                       // Is triggered when a category was removed
        public event EventHandler<RoutedEventArgs> SelectAllSoundsEvent;            // Trigger this event to select all sounds or deselect all sounds when all sounds are selected
        public event EventHandler<SizeChangedEventArgs> SoundTileSizeChangedEvent;  // This event is triggered when the size of the sound tiles in the GridViews has changed
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
            _user = new DavUser();
            _exporting = false;
            _exported = false;
            _importing = false;
            _imported = false;
            _exportMessage = "";
            _importMessage = "";
            _soundboardSize = "";
            #endregion

            #region Lists
            Categories = new List<Category>();
            AllSounds = new ObservableCollection<Sound>();
            Sounds = new ObservableCollection<Sound>();
            FavouriteSounds = new ObservableCollection<Sound>();
            SelectedSounds = new ObservableCollection<Sound>();
            PlayingSounds = new ObservableCollection<PlayingSound>();
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
            _selectAllFlyoutIcon = new SymbolIcon(Symbol.SelectAll);
            _soundTileWidth = 200;
            _playingSoundsBarWidth = 0;
            _playingSoundsBarAcrylicBackgroundBrush = new AcrylicBrush();
            _exportAndImportButtonsEnabled = true;
            #endregion

            #region Settings
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

            if (localSettings.Values[playingSoundsListVisibleKey] == null)
                _playingSoundsListVisible = playingSoundsListVisibleDefault;
            else
                _playingSoundsListVisible = (bool)localSettings.Values[playingSoundsListVisibleKey];

            if (localSettings.Values[playOneSoundAtOnceKey] == null)
                _playOneSoundAtOnce = playOneSoundAtOnceDefault;
            else
                _playOneSoundAtOnce = (bool)localSettings.Values[playOneSoundAtOnceKey];

            if (localSettings.Values[liveTileKey] == null)
                _liveTileEnabled = liveTileDefault;
            else
                _liveTileEnabled = (bool)localSettings.Values[liveTileKey];

            if (localSettings.Values[showListViewKey] == null)
                _showListView = showListViewDefault;
            else
                _showListView = (bool)localSettings.Values[showListViewKey];

            if (localSettings.Values[showCategoryIconKey] == null)
                _showCategoryIcon = showCategoryIconDefault;
            else
                _showCategoryIcon = (bool)localSettings.Values[showCategoryIconKey];

            if (localSettings.Values[showSoundsPivotKey] == null)
                _showSoundsPivot = showSoundsPivotDefault;
            else
                _showSoundsPivot = (bool)localSettings.Values[showSoundsPivotKey];

            if (localSettings.Values[savePlayingSoundsKey] == null)
                _savePlayingSounds = savePlayingSoundsDefault;
            else
                _savePlayingSounds = (bool)localSettings.Values[savePlayingSoundsKey];

            if (localSettings.Values[volumeKey] == null)
                _volume = volumeDefault;
            else
                _volume = (double)localSettings.Values[volumeKey];

            if (localSettings.Values[showAcrylicBackgroundKey] == null)
                _showAcrylicBackground = showAcrylicBackgroundDefault;
            else
                _showAcrylicBackground = (bool)localSettings.Values[showAcrylicBackgroundKey];

            if (localSettings.Values[soundOrderKey] == null)
                _soundOrder = soundOrderDefault;
            else
                _soundOrder = (FileManager.SoundOrder)localSettings.Values[soundOrderKey];

            if (localSettings.Values[soundOrderReversedKey] == null)
                _soundOrderReversed = soundOrderReversedDefault;
            else
                _soundOrderReversed = (bool)localSettings.Values[soundOrderReversedKey];
            #endregion
        }

        #region Access Modifiers
        #region State
        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                NotifyPropertyChanged("Title");
            }
        }

        public Type Page
        {
            get => _page;
            set
            {
                _page = value;
                NotifyPropertyChanged("Page");
            }
        }

        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                _searchQuery = value;
                NotifyPropertyChanged("SearchQuery");
            }
        }

        public bool AllSoundsChanged
        {
            get => _allSoundsChanged;
            set
            {
                _allSoundsChanged = value;
                NotifyPropertyChanged("AllSoundsChanged");
            }
        }

        public Guid SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                _selectedCategory = value;
                NotifyPropertyChanged("SelectedCategory");
            }
        }

        public DavUser User
        {
            get => _user;
            set
            {
                _user = value;
                NotifyPropertyChanged("User");
            }
        }

        public bool Exporting
        {
            get => _exporting;
            set
            {
                _exporting = value;
                NotifyPropertyChanged("Exporting");
            }
        }

        public bool Exported
        {
            get => _exported;
            set
            {
                _exported = value;
                NotifyPropertyChanged("Exported");
            }
        }

        public bool Importing
        {
            get => _importing;
            set
            {
                _importing = value;
                NotifyPropertyChanged("Importing");
            }
        }

        public bool Imported
        {
            get => _imported;
            set
            {
                _imported = value;
                NotifyPropertyChanged("Imported");
            }
        }

        public string ExportMessage
        {
            get => _exportMessage;
            set
            {
                _exportMessage = value;
                NotifyPropertyChanged("ExportMessage");
            }
        }

        public string ImportMessage
        {
            get => _importMessage;
            set
            {
                _importMessage = value;
                NotifyPropertyChanged("ImportMessage");
            }
        }

        public string SoundboardSize
        {
            get => _soundboardSize;
            set
            {
                _soundboardSize = value;
                NotifyPropertyChanged("SoundboardSize");
            }
        }
        #endregion

        #region Layout & Design
        public FileManager.AppTheme CurrentTheme
        {
            get => _currentTheme;
            set
            {
                _currentTheme = value;
                NotifyPropertyChanged("CurrentTheme");
            }
        }

        public bool ProgressRingIsActive
        {
            get => _progressRingIsActive;
            set
            {
                _progressRingIsActive = value;
                NotifyPropertyChanged("ProgressRingIsActive");
            }
        }

        public bool LoadingScreenVisible
        {
            get => _loadingScreenVisible;
            set
            {
                _loadingScreenVisible = value;
                NotifyPropertyChanged("LoadingScreenVisible");
            }
        }

        public string LoadingScreenMessage
        {
            get => _loadingScreenMessage;
            set
            {
                _loadingScreenMessage = value;
                NotifyPropertyChanged("LoadingScreenMessage");
            }
        }

        public bool BackButtonEnabled
        {
            get => _backButtonEnabled;
            set
            {
                _backButtonEnabled = value;
                NotifyPropertyChanged("BackButtonEnabled");
            }
        }

        public bool MultiSelectionEnabled
        {
            get => _multiSelectionEnabled;
            set
            {
                _multiSelectionEnabled = value;
                NotifyPropertyChanged("MultiSelectionEnabled");
            }
        }

        public bool PlayAllButtonVisible
        {
            get => _playAllButtonVisible;
            set
            {
                _playAllButtonVisible = value;
                NotifyPropertyChanged("PlayAllButtonVisible");
            }
        }

        public bool EditButtonVisible
        {
            get => _editButtonVisible;
            set
            {
                _editButtonVisible = value;
                NotifyPropertyChanged("EditButtonVisible");
            }
        }

        public bool TopButtonsCollapsed
        {
            get => _topButtonsCollapsed;
            set
            {
                _topButtonsCollapsed = value;
                NotifyPropertyChanged("TopButtonsCollapsed");
            }
        }

        public bool SearchAutoSuggestBoxVisible
        {
            get => _searchAutoSuggestBoxVisible;
            set
            {
                _searchAutoSuggestBoxVisible = value;
                NotifyPropertyChanged("SearchAutoSuggestBoxVisible");
            }
        }

        public bool SearchButtonVisible
        {
            get => _searchButtonVisible;
            set
            {
                _searchButtonVisible = value;
                NotifyPropertyChanged("SearchButtonVisible");
            }
        }

        public string SelectAllFlyoutText
        {
            get => _selectAllFlyoutText;
            set
            {
                _selectAllFlyoutText = value;
                NotifyPropertyChanged("SelectAllFlyoutText");
            }
        }

        public SymbolIcon SelectAllFlyoutIcon
        {
            get => _selectAllFlyoutIcon;
            set
            {
                _selectAllFlyoutIcon = value;
                NotifyPropertyChanged("SelectAllFlyoutIcon");
            }
        }

        public double SoundTileWidth
        {
            get => _soundTileWidth;
            set
            {
                _soundTileWidth = value;
                NotifyPropertyChanged("SoundTileWidth");
            }
        }

        public double PlayingSoundsBarWidth
        {
            get => _playingSoundsBarWidth;
            set
            {
                _playingSoundsBarWidth = value;
                NotifyPropertyChanged("PlayingSoundsBarWidth");
            }
        }

        public AcrylicBrush PlayingSoundsBarAcrylicBackgroundBrush
        {
            get => _playingSoundsBarAcrylicBackgroundBrush;
            set
            {
                _playingSoundsBarAcrylicBackgroundBrush = value;
                NotifyPropertyChanged("PlayingSoundsBarAcrylicBackgroundBrush");
            }
        }

        public bool ExportAndImportButtonsEnabled
        {
            get => _exportAndImportButtonsEnabled;
            set
            {
                _exportAndImportButtonsEnabled = value;
                NotifyPropertyChanged("ExportAndImportButtonsEnabled");
            }
        }
        #endregion
        #endregion

        #region Settings
        public FileManager.AppTheme Theme
        {
            get => _theme;
            set
            {
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
                NotifyPropertyChanged("Theme");
            }
        }

        public bool PlayingSoundsListVisible
        {
            get => _playingSoundsListVisible;
            set
            {
                localSettings.Values[playingSoundsListVisibleKey] = value;
                _playingSoundsListVisible = value;
                NotifyPropertyChanged("PlayingSoundsListVisible");
            }
        }

        public bool PlayOneSoundAtOnce
        {
            get => _playOneSoundAtOnce;
            set
            {
                localSettings.Values[playOneSoundAtOnceKey] = value;
                _playOneSoundAtOnce = value;
                NotifyPropertyChanged("PlayOneSoundAtOnce");
            }
        }

        public bool LiveTileEnabled
        {
            get => _liveTileEnabled;
            set
            {
                localSettings.Values[liveTileKey] = value;
                _liveTileEnabled = value;
                NotifyPropertyChanged("LiveTileEnabled");
            }
        }

        public bool ShowListView
        {
            get => _showListView;
            set
            {
                localSettings.Values[showListViewKey] = value;
                _showListView = value;
                NotifyPropertyChanged("ShowListView");
            }
        }

        public bool ShowCategoryIcon
        {
            get => _showCategoryIcon;
            set
            {
                localSettings.Values[showCategoryIconKey] = value;
                _showCategoryIcon = value;
                NotifyPropertyChanged("ShowCategoryIcon");
            }
        }

        public bool ShowSoundsPivot
        {
            get => _showSoundsPivot;
            set
            {
                localSettings.Values[showSoundsPivotKey] = value;
                _showSoundsPivot = value;
                NotifyPropertyChanged("ShowSoundsPivot");
            }
        }

        public bool SavePlayingSounds
        {
            get => _savePlayingSounds;
            set
            {
                localSettings.Values[savePlayingSoundsKey] = value;
                _savePlayingSounds = value;
                NotifyPropertyChanged("SavePlayingSounds");
            }
        }

        public double Volume
        {
            get => _volume;
            set
            {
                localSettings.Values[volumeKey] = value;
                _volume = value;
                NotifyPropertyChanged("Volume");
            }
        }

        public bool ShowAcrylicBackground
        {
            get => _showAcrylicBackground;
            set
            {
                localSettings.Values[showAcrylicBackgroundKey] = value;
                _showAcrylicBackground = value;
                NotifyPropertyChanged("ShowAcrylicBackground");
            }
        }

        public FileManager.SoundOrder SoundOrder
        {
            get => _soundOrder;
            set
            {
                localSettings.Values[soundOrderKey] = (int)value;
                _soundOrder = value;
                NotifyPropertyChanged("SoundOrder");
            }
        }

        public bool SoundOrderReversed
        {
            get => _soundOrderReversed;
            set
            {
                localSettings.Values[soundOrderReversedKey] = value;
                _soundOrderReversed = value;
                NotifyPropertyChanged("SoundOrderReversed");
            }
        }
        #endregion

        #region Events
        public void TriggerThemeChangedEvent()
        {
            ThemeChangedEvent?.Invoke(null, null);
        }

        public void TriggerCategoriesUpdatedEvent()
        {
            CategoriesUpdatedEvent?.Invoke(null, null);
        }

        public void TriggerCategoryUpdatedEvent(Guid uuid)
        {
            CategoryUpdatedEvent?.Invoke(null, uuid);
        }

        public void TriggerCategoryRemovedEvent(Guid uuid)
        {
            CategoryRemovedEvent?.Invoke(null, uuid);
        }

        public void TriggerSelectAllSoundsEvent(object sender, RoutedEventArgs e)
        {
            SelectAllSoundsEvent?.Invoke(sender, e);
        }

        public void TriggerSoundTileSizeChangedEvent(object sender, SizeChangedEventArgs e)
        {
            SoundTileSizeChangedEvent?.Invoke(sender, e);
        }
        #endregion

        #region NotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value)) return false;
            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
