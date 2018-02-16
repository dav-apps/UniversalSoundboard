using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
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
        private bool _isExporting;                                  // If true the soundboard is currently being exported
        private bool _exported;                                     // If true the soundboard was exported in the app session
        private bool _isImporting;                                  // If true a soundboard is currently being imported
        private bool _imported;                                     // If true a soundboard was import in the app session
        private bool _areExportAndImportButtonsEnabled;             // If true shows the export and import buttons on the settings page
        private string _exportMessage;                              // The text describing the status of the export
        private string _importMessage;                              // The text describing the status of the import
        private string _soundboardSize;                             // The text shown on the settings page which describes the size of the soundboard
        private Thickness _windowTitleMargin;                       // The margin of the window title at the top left of the window
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
        private int _selectedCategory;                         // The index of the currently selected category in the category list

        public string title
        {
            get { return _title; }

            set
            {
                _title = value;
                NotifyPropertyChanged("title");
            }
        }

        public bool progressRingIsActive
        {
            get { return _progressRingIsActive; }

            set
            {
                _progressRingIsActive = value;
                NotifyPropertyChanged("progressRingIsActive");
            }
        }

        public ObservableCollection<Sound> sounds
        {
            get { return _sounds; }

            set
            {
                _sounds = value;
                NotifyPropertyChanged("sounds");
            }
        }

        public ObservableCollection<Sound> favouriteSounds
        {
            get { return _favouriteSounds; }

            set
            {
                _favouriteSounds = value;
                NotifyPropertyChanged("favouriteSounds");
            }
        }

        public ObservableCollection<Sound> allSounds
        {
            get { return _allSounds; }

            set
            {
                _allSounds = value;
                NotifyPropertyChanged("allSounds");
            }
        }

        public bool allSoundsChanged
        {
            get { return _allSoundsChanged; }

            set
            {
                _allSoundsChanged = value;
                NotifyPropertyChanged("allSoundsChanged");
            }
        }

        public ObservableCollection<Category> categories
        {
            get { return _categories; }

            set
            {
                _categories = value;
                NotifyPropertyChanged("categories");
            }
        }

        public string searchQuery
        {
            get { return _searchQuery; }

            set
            {
                _searchQuery = value;
                NotifyPropertyChanged("searchQuery");
            }
        }

        public Visibility editButtonVisibility
        {
            get { return _editButtonVisibility; }

            set
            {
                _editButtonVisibility = value;
                NotifyPropertyChanged("editButtonVisibility");
            }
        }

        public Visibility playAllButtonVisibility
        {
            get { return _playAllButtonVisibility; }

            set
            {
                _playAllButtonVisibility = value;
                NotifyPropertyChanged("playAllButtonVisibility");
            }
        }

        public bool normalOptionsVisibility
        {
            get { return _normalOptionsVisibility; }

            set
            {
                _normalOptionsVisibility = value;
                NotifyPropertyChanged("normalOptionsVisibility");
            }
        }

        public Type page
        {
            get { return _page; }

            set
            {
                _page = value;
                NotifyPropertyChanged("page");
            }
        }

        public ListViewSelectionMode selectionMode
        {
            get { return _selectionMode; }

            set
            {
                _selectionMode = value;
                NotifyPropertyChanged("selectionMode");
            }
        }

        public List<Sound> selectedSounds
        {
            get { return _selectedSounds; }

            set
            {
                _selectedSounds = value;
                NotifyPropertyChanged("selectedSounds");
            }
        }

        public ObservableCollection<PlayingSound> playingSounds
        {
            get { return _playingSounds; }

            set
            {
                _playingSounds = value;
                NotifyPropertyChanged("playingSounds");
            }
        }

        public Visibility playingSoundsListVisibility
        {
            get { return _playingSoundsListVisibility; }

            set
            {
                _playingSoundsListVisibility = value;
                NotifyPropertyChanged("playingSoundsListVisibility");
            }
        }

        public bool playOneSoundAtOnce
        {
            get { return _playOneSoundAtOnce; }

            set
            {
                _playOneSoundAtOnce = value;
                NotifyPropertyChanged("playOneSoundAtOnce");
            }
        }

        public bool showCategoryIcon
        {
            get { return _showCategoryIcon; }

            set
            {
                _showCategoryIcon = value;
                NotifyPropertyChanged("showCategoryIcon");
            }
        }

        public bool showSoundsPivot
        {
            get { return _showSoundsPivot; }

            set
            {
                _showSoundsPivot = value;
                NotifyPropertyChanged("showSoundsPivot");
            }
        }

        public bool isExporting
        {
            get { return _isExporting; }

            set
            {
                _isExporting = value;
                NotifyPropertyChanged("isExporting");
            }
        }

        public bool exported
        {
            get { return _exported; }

            set
            {
                _exported = value;
                NotifyPropertyChanged("exported");
            }
        }

        public bool isImporting
        {
            get { return _isImporting; }

            set
            {
                _isImporting = value;
                NotifyPropertyChanged("isImporting");
            }
        }

        public bool imported
        {
            get { return _imported; }

            set
            {
                _imported = value;
                NotifyPropertyChanged("imported");
            }
        }

        public bool areExportAndImportButtonsEnabled
        {
            get { return _areExportAndImportButtonsEnabled; }

            set
            {
                _areExportAndImportButtonsEnabled = value;
                NotifyPropertyChanged("areExportAndImportButtonsEnabled");
            }
        }

        public string exportMessage
        {
            get { return _exportMessage; }

            set
            {
                _exportMessage = value;
                NotifyPropertyChanged("exportMessage");
            }
        }

        public string importMessage
        {
            get { return _importMessage; }

            set
            {
                _importMessage = value;
                NotifyPropertyChanged("importMessage");
            }
        }

        public string soundboardSize
        {
            get { return _soundboardSize; }

            set
            {
                _soundboardSize = value;
                NotifyPropertyChanged("soundboardSize");
            }
        }

        public Thickness windowTitleMargin
        {
            get { return _windowTitleMargin; }

            set
            {
                _windowTitleMargin = value;
                NotifyPropertyChanged("windowTitleMargin");
            }
        }

        public bool searchAutoSuggestBoxVisibility
        {
            get { return _searchAutoSuggestBoxVisibility; }

            set
            {
                _searchAutoSuggestBoxVisibility = value;
                NotifyPropertyChanged("searchAutoSuggestBoxVisibility");
            }
        }

        public bool volumeButtonVisibility
        {
            get { return _volumeButtonVisibility; }

            set
            {
                _volumeButtonVisibility = value;
                NotifyPropertyChanged("volumeButtonVisibility");
            }
        }

        public bool addButtonVisibility
        {
            get { return _addButtonVisibility; }

            set
            {
                _addButtonVisibility = value;
                NotifyPropertyChanged("addButtonVisibility");
            }
        }

        public bool selectButtonVisibility
        {
            get { return _selectButtonVisibility; }

            set
            {
                _selectButtonVisibility = value;
                NotifyPropertyChanged("selectButtonVisibility");
            }
        }

        public bool searchButtonVisibility
        {
            get { return _searchButtonVisibility; }

            set
            {
                _searchButtonVisibility = value;
                NotifyPropertyChanged("searchButtonVisibility");
            }
        }

        public bool cancelButtonVisibility
        {
            get { return _cancelButtonVisibility; }

            set
            {
                _cancelButtonVisibility = value;
                NotifyPropertyChanged("cancelButtonVisibility");
            }
        }

        public bool shareButtonVisibility
        {
            get { return _shareButtonVisibility; }

            set
            {
                _shareButtonVisibility = value;
                NotifyPropertyChanged("shareButtonVisibility");
            }
        }

        public bool moreButtonVisibility
        {
            get { return _moreButtonVisibility; }

            set
            {
                _moreButtonVisibility = value;
                NotifyPropertyChanged("moreButtonVisibility");
            }
        }

        public bool topButtonsCollapsed
        {
            get { return _topButtonsCollapsed; }

            set
            {
                _topButtonsCollapsed = value;
                NotifyPropertyChanged("topButtonsCollapsed");
            }
        }

        public bool areSelectButtonsEnabled
        {
            get { return _areSelectButtonsEnabled; }

            set
            {
                _areSelectButtonsEnabled = value;
                NotifyPropertyChanged("areSelectButtonsEnabled");
            }
        }

        public int selectedCategory
        {
            get { return _selectedCategory; }

            set
            {
                _selectedCategory = value;
                NotifyPropertyChanged("selectedCategory");
            }
        }



        public event PropertyChangedEventHandler PropertyChanged;

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (object.Equals(storage, value)) return false;
            storage = value;
            this.OnPropertyChanged(propertyName);
            return true;
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void OnPropertyChanged(string propertyName)
        {
            var eventHandler = this.PropertyChanged;
            if (eventHandler != null)
                eventHandler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
