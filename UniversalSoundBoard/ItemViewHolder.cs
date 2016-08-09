using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UniversalSoundBoard.Model;

namespace UniversalSoundBoard
{
    public class ItemViewHolder : INotifyPropertyChanged
    {
        private string _title;
        private bool _progressRingIsActive;
        private ObservableCollection<Sound> _sounds;
        private Uri _mediaElementSource;
        private List<Category> _categories;
        private string _searchQuery;

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

        public Uri mediaElementSource
        {
            get { return _mediaElementSource; }

            set
            {
                _mediaElementSource = value;
                NotifyPropertyChanged("mediaElementSource");
            }
        }

        public List<Category> categories
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
