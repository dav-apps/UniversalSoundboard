using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UniversalSoundBoard.Model;
using Windows.Media.Playback;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UniversalSoundBoard
{
    public class ItemViewHolder : INotifyPropertyChanged
    {
        private string _title;
        private bool _progressRingIsActive;
        private bool _multiSelectOptionsEnabled;
        private string _searchQuery;
        private Visibility _editButtonVisibility;
        private Visibility _playAllButtonVisibility;
        private Visibility _normalOptionsVisibility;
        private Visibility _multiSelectOptionsVisibility;
        private Type _page;
        private ListViewSelectionMode _selectionMode;
        private ObservableCollection<Category> _categories;
        private ObservableCollection<Sound> _sounds;
        private List<Sound> _selectedSounds;
        private ObservableCollection<PlayingSound> _playingSounds;
        private Visibility _playingSoundsListVisibility;
        private bool _playOneSoundAtOnce;

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

        public bool multiSelectOptionsEnabled
        {
            get { return _multiSelectOptionsEnabled; }

            set
            {
                _multiSelectOptionsEnabled = value;
                NotifyPropertyChanged("multiSelectOptionsEnabled");
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

        public Visibility normalOptionsVisibility
        {
            get { return _normalOptionsVisibility; }

            set
            {
                _normalOptionsVisibility = value;
                NotifyPropertyChanged("normalOptionsVisibility");
            }
        }

        public Visibility multiSelectOptionsVisibility
        {
            get { return _multiSelectOptionsVisibility; }

            set
            {
                _multiSelectOptionsVisibility = value;
                NotifyPropertyChanged("multiSelectOptionsVisibility");
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
