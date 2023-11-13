using System.Collections.Generic;

namespace UniversalSoundboard.Models
{
    public class ListSoundsResponse
    {
        public ListResponse<SoundResponse> ListSounds { get; set; }
    }

    public class SoundResponse
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class ListResponse<T>
    {
        public int Total { get; set; }
        public List<T> Items { get; set; }
    }
}
