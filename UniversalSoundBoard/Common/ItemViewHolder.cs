using davClassLibrary.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using UniversalSoundboard.Models;
using UniversalSoundBoard.Models;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UniversalSoundBoard.Common
{
    public class ItemViewHolder : INotifyPropertyChanged
    {
        private string _title;                                      // Is the text of the title
        private bool _progressRingIsActive;                         // Shows the Progress Ring if true
        private string _searchQuery;                                // The string entered into the search box
        private Visibility _editButtonVisibility;                   // If true shows the edit button next to the title, only when a category is selected
        private Visibility _playAllButtonVisibility;                // If true shows the Play button next to the title, only when a category or All Sounds is selected
        private bool _normalOptionsVisibility;                      // If true shows the normal buttons at the top, e.g. Search bar and Volume Button. If false shows the multi select buttons
        private Type _page;                                         // The current page
        private ListViewSelectionMode _selectionMode;               // The selection mode of the GridView. Is either ListViewSelectionMode.None or ListViewSelectionMode.Multiple
        private ObservableCollection<Category> _categories;         // A list of all categories.
        private ObservableCollection<Sound> _sounds;                // A list of the sounds which are displayed when the Sound pivot is selected
        private ObservableCollection<Sound> _favouriteSounds;       // A list of the favourite sound which are displayed when the Favourite pivot is selected
        private ObservableCollection<Sound> _allSounds;             // A list of all sounds
        private bool _allSoundsChanged;                             // If there was made change to one or multiple sounds so that a reload of the sounds is required
        private List<Sound> _selectedSounds;                        // A list of the sounds which are selected
        private ObservableCollection<PlayingSound> _playingSounds;  // A list of the Playing Sounds which are displayed in the right menu
        private Visibility _playingSoundsListVisibility;            // If true shows the Playing Sounds list at the right
        private bool _playOneSoundAtOnce;                           // If true plays only one sound at a time
        private bool _showCategoryIcon;                             // If true shows the icon of the category on the sound tile
        private bool _showSoundsPivot;                              // If true shows the pivot to select Sounds or Favourite sounds
        private bool _savePlayingSounds;                            // If true saves the PlayingSounds and loads them when starting the app
        private bool _isExporting;                                  // If true the soundboard is currently being exported
        private bool _exported;                                     // If true the soundboard was exported in the app session
        private bool _isImporting;                                  // If true a soundboard is currently being imported
        private bool _imported;                                     // If true a soundboard was import in the app session
        private bool _areExportAndImportButtonsEnabled;             // If true shows the export and import buttons on the settings page
        private string _exportMessage;                              // The text describing the status of the export
        private string _importMessage;                              // The text describing the status of the import
        private string _soundboardSize;                             // The text shown on the settings page which describes the size of the soundboard
        private bool _searchAutoSuggestBoxVisibility;               // If true the search box is visible, if false multi selection is on or the search button shown
        private bool _volumeButtonVisibility;                       // If true the volume button at the top is visible
        private bool _addButtonVisibility;                          // If true the Add button at the top to add sounds or a category is visible
        private bool _selectButtonVisibility;                       // If true the button to switch to multi selection mode is visible
        private bool _searchButtonVisibility;                       // If true the search button at the top is visible
        private bool _cancelButtonVisibility;                       // If the the cancel button at the top to switch from multi selection mode to normal mode is visible
        private bool _shareButtonVisibility;                        // If true the Share button at the top to share sounds is visible
        private bool _moreButtonVisibility;                         // If true the More button at the top, when multi selection mode is on, is visible
        private bool _topButtonsCollapsed;                          // If true the buttons at the top show only the icon, if false they show the icon and text
        private bool _areSelectButtonsEnabled;                      // If false the buttons at the top in multi selection mode are disabled
        private int _selectedCategory;                              // The index of the currently selected category in the category list
        private string _upgradeDataStatusText;                      // The text that is shown on the splash screen when the old data is migrated
        private DavUser _user;                                      // The User object with username and avatar
        private bool _loginMenuItemVisibility;                      // If true, the LoginMenuItem in the NavigationView is visible
        private bool _isBackButtonEnabled;                          // If the Back Button is enabled
        private bool _loadingScreenVisibility;                      // If true, the big loading screen is visible
        private string _loadingScreenMessage;                       // The text that is shown in the loading screen
        public event EventHandler<RoutedEventArgs> SelectAllSoundsEvent;  // Trigger this event to select all sounds or deselect all sounds when all sounds are selected

        public string Title
        {
            get { return _title; }

            set
            {
                _title = value;
                NotifyPropertyChanged("Title");
            }
        }

        public bool ProgressRingIsActive
        {
            get { return _progressRingIsActive; }

            set
            {
                _progressRingIsActive = value;
                NotifyPropertyChanged("ProgressRingIsActive");
            }
        }

        public ObservableCollection<Sound> Sounds
        {
            get { return _sounds; }

            set
            {
                _sounds = value;
                NotifyPropertyChanged("Sounds");
            }
        }

        public ObservableCollection<Sound> FavouriteSounds
        {
            get { return _favouriteSounds; }

            set
            {
                _favouriteSounds = value;
                NotifyPropertyChanged("FavouriteSounds");
            }
        }

        public ObservableCollection<Sound> AllSounds
        {
            get { return _allSounds; }

            set
            {
                _allSounds = value;
                NotifyPropertyChanged("AllSounds");
            }
        }

        public bool AllSoundsChanged
        {
            get { return _allSoundsChanged; }

            set
            {
                _allSoundsChanged = value;
                NotifyPropertyChanged("AllSoundsChanged");
            }
        }

        public ObservableCollection<Category> Categories
        {
            get { return _categories; }

            set
            {
                _categories = value;
                NotifyPropertyChanged("Categories");
            }
        }

        public string SearchQuery
        {
            get { return _searchQuery; }

            set
            {
                _searchQuery = value;
                NotifyPropertyChanged("SearchQuery");
            }
        }

        public Visibility EditButtonVisibility
        {
            get { return _editButtonVisibility; }

            set
            {
                _editButtonVisibility = value;
                NotifyPropertyChanged("EditButtonVisibility");
            }
        }

        public Visibility PlayAllButtonVisibility
        {
            get { return _playAllButtonVisibility; }

            set
            {
                _playAllButtonVisibility = value;
                NotifyPropertyChanged("PlayAllButtonVisibility");
            }
        }

        public bool NormalOptionsVisibility
        {
            get { return _normalOptionsVisibility; }

            set
            {
                _normalOptionsVisibility = value;
                NotifyPropertyChanged("NormalOptionsVisibility");
            }
        }

        public Type Page
        {
            get { return _page; }

            set
            {
                _page = value;
                NotifyPropertyChanged("Page");
            }
        }

        public ListViewSelectionMode SelectionMode
        {
            get { return _selectionMode; }

            set
            {
                _selectionMode = value;
                NotifyPropertyChanged("SelectionMode");
            }
        }

        public List<Sound> SelectedSounds
        {
            get { return _selectedSounds; }

            set
            {
                _selectedSounds = value;
                NotifyPropertyChanged("SelectedSounds");
            }
        }

        public ObservableCollection<PlayingSound> PlayingSounds
        {
            get { return _playingSounds; }

            set
            {
                _playingSounds = value;
                NotifyPropertyChanged("PlayingSounds");
            }
        }

        public Visibility PlayingSoundsListVisibility
        {
            get { return _playingSoundsListVisibility; }

            set
            {
                _playingSoundsListVisibility = value;
                NotifyPropertyChanged("PlayingSoundsListVisibility");
            }
        }

        public bool PlayOneSoundAtOnce
        {
            get { return _playOneSoundAtOnce; }

            set
            {
                _playOneSoundAtOnce = value;
                NotifyPropertyChanged("PlayOneSoundAtOnce");
            }
        }

        public bool ShowCategoryIcon
        {
            get { return _showCategoryIcon; }

            set
            {
                _showCategoryIcon = value;
                NotifyPropertyChanged("ShowCategoryIcon");
            }
        }

        public bool ShowSoundsPivot
        {
            get { return _showSoundsPivot; }

            set
            {
                _showSoundsPivot = value;
                NotifyPropertyChanged("ShowSoundsPivot");
            }
        }

        public bool SavePlayingSounds
        {
            get { return _savePlayingSounds; }

            set
            {
                _savePlayingSounds = value;
                NotifyPropertyChanged("SavePlayingSounds");
            }
        }

        public bool IsExporting
        {
            get { return _isExporting; }

            set
            {
                _isExporting = value;
                NotifyPropertyChanged("IsExporting");
            }
        }

        public bool Exported
        {
            get { return _exported; }

            set
            {
                _exported = value;
                NotifyPropertyChanged("Exported");
            }
        }

        public bool IsImporting
        {
            get { return _isImporting; }

            set
            {
                _isImporting = value;
                NotifyPropertyChanged("IsImporting");
            }
        }

        public bool Imported
        {
            get { return _imported; }

            set
            {
                _imported = value;
                NotifyPropertyChanged("Imported");
            }
        }

        public bool AreExportAndImportButtonsEnabled
        {
            get { return _areExportAndImportButtonsEnabled; }

            set
            {
                _areExportAndImportButtonsEnabled = value;
                NotifyPropertyChanged("AreExportAndImportButtonsEnabled");
            }
        }

        public string ExportMessage
        {
            get { return _exportMessage; }

            set
            {
                _exportMessage = value;
                NotifyPropertyChanged("ExportMessage");
            }
        }

        public string ImportMessage
        {
            get { return _importMessage; }

            set
            {
                _importMessage = value;
                NotifyPropertyChanged("ImportMessage");
            }
        }

        public string SoundboardSize
        {
            get { return _soundboardSize; }

            set
            {
                _soundboardSize = value;
                NotifyPropertyChanged("SoundboardSize");
            }
        }

        public bool SearchAutoSuggestBoxVisibility
        {
            get { return _searchAutoSuggestBoxVisibility; }

            set
            {
                _searchAutoSuggestBoxVisibility = value;
                NotifyPropertyChanged("SearchAutoSuggestBoxVisibility");
            }
        }

        public bool VolumeButtonVisibility
        {
            get { return _volumeButtonVisibility; }

            set
            {
                _volumeButtonVisibility = value;
                NotifyPropertyChanged("VolumeButtonVisibility");
            }
        }

        public bool AddButtonVisibility
        {
            get { return _addButtonVisibility; }

            set
            {
                _addButtonVisibility = value;
                NotifyPropertyChanged("AddButtonVisibility");
            }
        }

        public bool SelectButtonVisibility
        {
            get { return _selectButtonVisibility; }

            set
            {
                _selectButtonVisibility = value;
                NotifyPropertyChanged("SelectButtonVisibility");
            }
        }

        public bool SearchButtonVisibility
        {
            get { return _searchButtonVisibility; }

            set
            {
                _searchButtonVisibility = value;
                NotifyPropertyChanged("SearchButtonVisibility");
            }
        }

        public bool CancelButtonVisibility
        {
            get { return _cancelButtonVisibility; }

            set
            {
                _cancelButtonVisibility = value;
                NotifyPropertyChanged("CancelButtonVisibility");
            }
        }

        public bool ShareButtonVisibility
        {
            get { return _shareButtonVisibility; }

            set
            {
                _shareButtonVisibility = value;
                NotifyPropertyChanged("ShareButtonVisibility");
            }
        }

        public bool MoreButtonVisibility
        {
            get { return _moreButtonVisibility; }

            set
            {
                _moreButtonVisibility = value;
                NotifyPropertyChanged("MoreButtonVisibility");
            }
        }

        public bool TopButtonsCollapsed
        {
            get { return _topButtonsCollapsed; }

            set
            {
                _topButtonsCollapsed = value;
                NotifyPropertyChanged("TopButtonsCollapsed");
            }
        }

        public bool AreSelectButtonsEnabled
        {
            get { return _areSelectButtonsEnabled; }

            set
            {
                _areSelectButtonsEnabled = value;
                NotifyPropertyChanged("AreSelectButtonsEnabled");
            }
        }

        public int SelectedCategory
        {
            get { return _selectedCategory; }

            set
            {
                _selectedCategory = value;
                NotifyPropertyChanged("SelectedCategory");
            }
        }

        public string UpgradeDataStatusText
        {
            get { return _upgradeDataStatusText; }

            set
            {
                _upgradeDataStatusText = value;
                NotifyPropertyChanged("UpgradeDataStatusText");
            }
        }

        public DavUser User
        {
            get { return _user; }

            set
            {
                _user = value;
                NotifyPropertyChanged("User");
            }
        }

        public bool LoginMenuItemVisibility
        {
            get { return _loginMenuItemVisibility; }

            set
            {
                _loginMenuItemVisibility = value;
                NotifyPropertyChanged("LoginMenuItemVisibility");
            }
        }

        public bool IsBackButtonEnabled
        {
            get { return _isBackButtonEnabled; }

            set
            {
                _isBackButtonEnabled = value;
                NotifyPropertyChanged("IsBackButtonEnabled");
            }
        }

        public bool LoadingScreenVisibility
        {
            get { return _loadingScreenVisibility; }

            set
            {
                _loadingScreenVisibility = value;
                NotifyPropertyChanged("LoadingScreenVisibility");
            }
        }

        public string LoadingScreenMessage
        {
            get { return _loadingScreenMessage; }

            set
            {
                _loadingScreenMessage = value;
                NotifyPropertyChanged("LoadingScreenMessage");
            }
        }

        public void TriggerSelectAllSoundsEvent(object sender, RoutedEventArgs e)
        {
            SelectAllSoundsEvent?.Invoke(sender, e);
        }



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
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
