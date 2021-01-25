using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace UniversalSoundboard.Models
{
    // The model for the old data.json file
    public class Data
    {
        public ObservableCollection<Category> Categories { get; set; }
    }
    
    // A representation of the database Sound table for exporting the Soundboard
    public class SoundData
    {
        public string Uuid { get; set; }
        public string Name { get; set; }
        public bool Favourite { get; set; }
        public string SoundExt { get; set; }
        public string ImageExt { get; set; }
        public string CategoryId { get; set; }
    }

    // The model for the new data.json file
    public class NewData
    {
        public List<SoundData> Sounds { get; set; }
        public List<Category> Categories { get; set; }
    }
}
