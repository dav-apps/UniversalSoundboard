using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace UniversalSoundboard.Models
{
    public class SoundDownloadResult
    {
        private ObservableCollection<SoundDownloadItem> soundItems;

        public List<SoundDownloadItem> SoundItems
        {
            get => soundItems.ToList();
        }
        public bool CreateCategoryForPlaylist { get; set; }
        public string CategoryName { get; set; }

        public SoundDownloadResult(ObservableCollection<SoundDownloadItem> soundItems, string categoryName)
        {
            this.soundItems = soundItems;
            CategoryName = categoryName;
        }
    }
}
