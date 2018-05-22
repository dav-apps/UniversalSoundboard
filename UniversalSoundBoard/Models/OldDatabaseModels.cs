using SQLite;
using System;

namespace UniversalSoundboard.Models
{
    public class OldCategoryDatabaseModel
    {
        [PrimaryKey, AutoIncrement]
        public int id { get; set; }
        public Guid uuid { get; set; }
        public string name { get; set; }
        public string icon { get; set; }
    }

    public class OldSoundDatabaseModel
    {
        [PrimaryKey, AutoIncrement]
        public int id { get; set; }
        public Guid uuid { get; set; }
        public string name { get; set; }
        public bool favourite { get; set; }
        public string sound_ext { get; set; }
        public string image_ext { get; set; }
        public Guid category_id { get; set; }
    }

    public class OldPlayingSoundDatabaseModel
    {
        [PrimaryKey, AutoIncrement]
        public int id { get; set; }
        public Guid uuid { get; set; }
        public string sound_ids { get; set; }
        public int current { get; set; }
        public int repetitions { get; set; }
        public bool randomly { get; set; }
        public double volume { get; set; }
    }
}
